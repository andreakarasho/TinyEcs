#define CLAY_IMPLEMENTATION
#include "../clay/clay.h"

#include "lua.h"
#include "lauxlib.h"
#include <string.h>
#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <math.h>  // For INFINITY

// global arena pointer so we can free it later
static void* g_ClayArenaMem = NULL;
static size_t g_ClayArenaCap = 0;

static Clay_ElementId clay__last_id = (Clay_ElementId){0, 0, 0, 0};	// Cache last element id declared

// ---- Helpers
static Clay_String Clay_CopyLuaString(lua_State *L, int index) {
    size_t len = 0;
    const char *txt = luaL_checklstring(L, index, &len);
    Clay_Context* ctx = Clay_GetCurrentContext();
    if (!ctx) {
		luaL_error(L, "Clay context is null (did you call clay.initialize()?)");
	}
    Clay_String tmp = { .chars = txt, .length = (int32_t)len, .isStaticallyAllocated = false };
    return Clay__WriteStringToCharBuffer(&ctx->dynamicStringData, tmp);
}

// ---- Measure bridge (safe, no baseChars arithmetic, no Clay calls inside) ----
static lua_State *g_LuaState = NULL;
static int g_MeasureTextRef = LUA_NOREF;

static Clay_Dimensions Bridge_MeasureTextFunction(Clay_StringSlice s, Clay_TextElementConfig* cfg, void* userdata) {
    if (g_MeasureTextRef == LUA_NOREF) {
        // Clay_TextElementConfig contains members such as fontId, fontSize, letterSpacing etc
        // Note: Clay_String->chars is not guaranteed to be null terminated
        return (Clay_Dimensions) {
                .width = s.length * cfg->fontSize, // <- this will only work for monospace fonts, see the renderers/ directory for more advanced text measurement
                .height = cfg->fontSize
        };
    }

	lua_State *L = g_LuaState;
	
    // push Lua function
    lua_rawgeti(L, LUA_REGISTRYINDEX, g_MeasureTextRef);

    // Arg 1: the text slice (use lstring; chars may not be NUL-terminated)
    lua_pushlstring(L, s.chars, (size_t)s.length);

	// Arg 2: config table or nil
	if (cfg) {
		lua_newtable(L);

		// textColor = { r, g, b, a }
		lua_newtable(L);
		lua_pushnumber(L, cfg->textColor.r); lua_setfield(L, -2, "r");
		lua_pushnumber(L, cfg->textColor.g); lua_setfield(L, -2, "g");
		lua_pushnumber(L, cfg->textColor.b); lua_setfield(L, -2, "b");
		lua_pushnumber(L, cfg->textColor.a); lua_setfield(L, -2, "a");
		lua_setfield(L, -2, "textColor");

		lua_pushnumber(L, (lua_Number)cfg->fontId); lua_setfield(L, -2, "fontId");
		lua_pushnumber(L, (lua_Number)cfg->fontSize); lua_setfield(L, -2, "fontSize");
		lua_pushnumber(L, (lua_Number)cfg->letterSpacing); lua_setfield(L, -2, "letterSpacing");
		lua_pushnumber(L, (lua_Number)cfg->lineHeight); lua_setfield(L, -2, "lineHeight");
		lua_pushnumber(L, (lua_Number)cfg->wrapMode); lua_setfield(L, -2, "wrapMode");
		lua_pushnumber(L, (lua_Number)cfg->textAlignment); lua_setfield(L, -2, "textAlignment");
	} else {
		lua_pushnil(L);
	}

    Clay_Dimensions out = (Clay_Dimensions){0,0};

	// Expect **2 results** (width, height)
    if (lua_pcall(L, 2, 2, 0) != LUA_OK) {
        const char* err = lua_tostring(L, -1);
        fprintf(stderr, "[clay] measureText error: %s\n", err ? err : "(unknown)");
        lua_pop(L, 1);
        return out;
    }

    // Get height (top of stack)
    if (lua_isnumber(L, -1))
        out.height = (float)lua_tonumber(L, -1);
    lua_pop(L, 1);

    // Get width (next)
    if (lua_isnumber(L, -1))
        out.width = (float)lua_tonumber(L, -1);
    lua_pop(L, 1);

    if (out.width <= 0)  out.width  = 1.0f;
    if (out.height <= 0) out.height = 1.0f;

    return out;
}

static int l_Clay_SetMeasureTextFunction(lua_State *L) {
    if (!lua_isfunction(L,1) && !lua_isnil(L,1))
        return luaL_error(L, "setMeasureTextFunction(func|nil, [userData])");

	g_LuaState = L;
	
    if (g_MeasureTextRef != LUA_NOREF) {
        luaL_unref(L, LUA_REGISTRYINDEX, g_MeasureTextRef);
        g_MeasureTextRef = LUA_NOREF;
    }

    if (lua_isfunction(L,1)) {
        lua_pushvalue(L,1);
        g_MeasureTextRef = luaL_ref(L, LUA_REGISTRYINDEX);
    }

    Clay_SetMeasureTextFunction(Bridge_MeasureTextFunction, NULL);

    return 0;
}

static void readSizingAxisFromLua(lua_State *L, int index, Clay_SizingAxis *out) {
    lua_getfield(L, index, "type");
    out->type = (Clay__SizingType)luaL_checkinteger(L, -1);
    lua_pop(L, 1);

    switch (out->type) {
        case CLAY__SIZING_TYPE_FIXED:
        case CLAY__SIZING_TYPE_FIT:
        case CLAY__SIZING_TYPE_GROW: {
            lua_getfield(L, index, "minMax");
            if (lua_istable(L, -1)) {
                lua_getfield(L, -1, "min");
                out->size.minMax.min = (float)luaL_optnumber(L, -1, 0.0);
                lua_pop(L, 1);
                lua_getfield(L, -1, "max");
                out->size.minMax.max = (float)luaL_optnumber(L, -1, INFINITY);
                lua_pop(L, 1);
            } else {
                out->size.minMax.min = 0.0f;
                out->size.minMax.max = INFINITY;
            }
            lua_pop(L, 1);
            break;
        }

        case CLAY__SIZING_TYPE_PERCENT: {
            lua_getfield(L, index, "percent");
            out->size.percent = (float)luaL_optnumber(L, -1, 100.0);
            lua_pop(L, 1);
            break;
        }

        default:
            break;
    }
}

// -----------------------------------------------------------------------------
// Core API wrappers
// -----------------------------------------------------------------------------
static int l_Clay_GetCurrentContext(lua_State *L) {
    Clay_Context* ctx = Clay_GetCurrentContext();
    if (ctx) {
        lua_pushlightuserdata(L, ctx);
    } else {
        lua_pushnil(L);
    }
    return 1;
}

static int l_Clay_MinMemorySize(lua_State *L) {
    uint32_t size = Clay_MinMemorySize();
    lua_pushinteger(L, size);
    return 1;
}

static int l_Clay_CreateArenaWithCapacityAndMemory(lua_State *L) {
    size_t capacity = (size_t)luaL_checkinteger(L, 1);
    void* memory = lua_islightuserdata(L, 2) ? lua_touserdata(L, 2) : NULL;
    Clay_Arena arena = Clay_CreateArenaWithCapacityAndMemory(capacity, memory);
    lua_newtable(L);
    lua_pushinteger(L, (lua_Integer)arena.capacity); lua_setfield(L, -2, "capacity");
    lua_pushlightuserdata(L, arena.memory); lua_setfield(L, -2, "memory");
    return 1;
}

// -----------------------------------------------------------------------------
// Element creation and management
// -----------------------------------------------------------------------------
static int l_Clay_CreateElement(lua_State *L) {
    // -------------------------------------------------------------------------
    // FIRST PARAMETER: id table like { "LabelString", 0 }
    if (!lua_istable(L, 1))
        return luaL_error(L, "createElement(idTable, config?, callback?) requires first argument from clay.id()");

	//id
    lua_rawgeti(L, 1, 1);
    uint32_t id = 0;
    if (lua_isnumber(L, -1))
        id = (uint32_t)lua_tointeger(L, -1);
    lua_pop(L, 1);

	//offset
    lua_rawgeti(L, 1, 2);
    uint32_t offset = 0;
    if (lua_isnumber(L, -1))
        offset = (uint32_t)lua_tointeger(L, -1);
    lua_pop(L, 1);
    
	//baseId
    lua_rawgeti(L, 1, 3);
    uint32_t baseId = 0;
    if (lua_isnumber(L, -1))
        baseId = (uint32_t)lua_tointeger(L, -1);
    lua_pop(L, 1);
    
    //stringId
    lua_rawgeti(L, 1, 4);
    if (!lua_isstring(L, -1))
        return luaL_error(L, "table[4] must be a string");
    const char *label = lua_tostring(L, -1);
    lua_pop(L, 1);
    
	Clay_Context* ctx = Clay_GetCurrentContext();
	if (!ctx) {
		luaL_error(L, "Clay context is null (did you call clay.initialize()?)");
	}
	Clay_String luaStr = { .chars = label, .length = (int32_t)strlen(label), .isStaticallyAllocated = false };
	Clay_String s = Clay__WriteStringToCharBuffer(&ctx->dynamicStringData, luaStr);

    Clay_ElementId elid = (Clay_ElementId){id, offset, baseId, s};

    Clay_ElementDeclaration decl = { 0 };
    decl.layout = CLAY_LAYOUT_DEFAULT;
	
	// Open Element
	// -------------------------------------------------------------------------
	Clay__OpenElementWithId(elid);
    // -------------------------------------------------------------------------
    // CONFIG TABLE
    if (lua_istable(L, 2)) {

        // ---------------- layout ----------------
        lua_getfield(L, 2, "layout");
        if (lua_istable(L, -1)) {

            // layoutDirection
            lua_getfield(L, -1, "layoutDirection");
            if (lua_isnumber(L, -1))
                decl.layout.layoutDirection = (int)lua_tointeger(L, -1);
            lua_pop(L, 1);

            // childGap
            lua_getfield(L, -1, "childGap");
            if (lua_isnumber(L, -1))
                decl.layout.childGap = (float)lua_tonumber(L, -1);
            lua_pop(L, 1);

			// childAlignment = { x = clay.ALIGN_X_LEFT, y = clay.ALIGN_Y_CENTER }
			lua_getfield(L, -1, "childAlignment");
			if (lua_istable(L, -1)) {
				lua_getfield(L, -1, "x");
				if (lua_isnumber(L, -1))
					decl.layout.childAlignment.x = (Clay_LayoutAlignmentX)lua_tointeger(L, -1);
				lua_pop(L, 1);

				lua_getfield(L, -1, "y");
				if (lua_isnumber(L, -1))
					decl.layout.childAlignment.y = (Clay_LayoutAlignmentY)lua_tointeger(L, -1);
				lua_pop(L, 1);
			}
			lua_pop(L, 1);

            // padding
            lua_getfield(L, -1, "padding");
            if (lua_istable(L, -1)) {
                lua_getfield(L, -1, "left");   decl.layout.padding.left   = (float)luaL_optnumber(L, -1, 0.0); lua_pop(L, 1);
                lua_getfield(L, -1, "right");  decl.layout.padding.right  = (float)luaL_optnumber(L, -1, 0.0); lua_pop(L, 1);
                lua_getfield(L, -1, "top");    decl.layout.padding.top    = (float)luaL_optnumber(L, -1, 0.0); lua_pop(L, 1);
                lua_getfield(L, -1, "bottom"); decl.layout.padding.bottom = (float)luaL_optnumber(L, -1, 0.0); lua_pop(L, 1);
            }
            lua_pop(L, 1);

            // sizing
            lua_getfield(L, -1, "sizing");
            if (lua_istable(L, -1)) {
                lua_getfield(L, -1, "width");
                if (lua_istable(L, -1))
                    readSizingAxisFromLua(L, lua_gettop(L), &decl.layout.sizing.width);
                else if (lua_isnumber(L, -1)) {
                    float w = (float)lua_tonumber(L, -1);
                    decl.layout.sizing.width.type = CLAY__SIZING_TYPE_FIXED;
                    decl.layout.sizing.width.size.minMax.min = w;
                    decl.layout.sizing.width.size.minMax.max = w;
                }
                lua_pop(L, 1);

                lua_getfield(L, -1, "height");
                if (lua_istable(L, -1))
                    readSizingAxisFromLua(L, lua_gettop(L), &decl.layout.sizing.height);
                else if (lua_isnumber(L, -1)) {
                    float h = (float)lua_tonumber(L, -1);
                    decl.layout.sizing.height.type = CLAY__SIZING_TYPE_FIXED;
                    decl.layout.sizing.height.size.minMax.min = h;
                    decl.layout.sizing.height.size.minMax.max = h;
                }
                lua_pop(L, 1);
            }
            lua_pop(L, 1);
        }
        lua_pop(L, 1); // layout

        // ---------------- backgroundColor ----------------
        lua_getfield(L, 2, "backgroundColor");
        if (lua_istable(L, -1)) {
            lua_getfield(L, -1, "r"); decl.backgroundColor.r = (float)luaL_optnumber(L, -1, 0); lua_pop(L, 1);
            lua_getfield(L, -1, "g"); decl.backgroundColor.g = (float)luaL_optnumber(L, -1, 0); lua_pop(L, 1);
            lua_getfield(L, -1, "b"); decl.backgroundColor.b = (float)luaL_optnumber(L, -1, 0); lua_pop(L, 1);
            lua_getfield(L, -1, "a"); decl.backgroundColor.a = (float)luaL_optnumber(L, -1, 255); lua_pop(L, 1);
        }
        lua_pop(L, 1);

        // ---------------- cornerRadius ----------------
        lua_getfield(L, 2, "cornerRadius");
        if (lua_istable(L, -1)) {
            lua_getfield(L, -1, "topLeft");     decl.cornerRadius.topLeft     = (float)luaL_optnumber(L, -1, 0.0); lua_pop(L, 1);
            lua_getfield(L, -1, "topRight");    decl.cornerRadius.topRight    = (float)luaL_optnumber(L, -1, 0.0); lua_pop(L, 1);
            lua_getfield(L, -1, "bottomLeft");  decl.cornerRadius.bottomLeft  = (float)luaL_optnumber(L, -1, 0.0); lua_pop(L, 1);
            lua_getfield(L, -1, "bottomRight"); decl.cornerRadius.bottomRight = (float)luaL_optnumber(L, -1, 0.0); lua_pop(L, 1);
        }
        lua_pop(L, 1);

		// ---------------- border ----------------
		lua_getfield(L, 2, "border");
		if (lua_istable(L, -1)) {
			// border.color = { r, g, b, a }
			lua_getfield(L, -1, "color");
			if (lua_istable(L, -1)) {
				lua_getfield(L, -1, "r"); decl.border.color.r = (float)luaL_optnumber(L, -1, 0); lua_pop(L, 1);
				lua_getfield(L, -1, "g"); decl.border.color.g = (float)luaL_optnumber(L, -1, 0); lua_pop(L, 1);
				lua_getfield(L, -1, "b"); decl.border.color.b = (float)luaL_optnumber(L, -1, 0); lua_pop(L, 1);
				lua_getfield(L, -1, "a"); decl.border.color.a = (float)luaL_optnumber(L, -1, 255); lua_pop(L, 1);
			}
			lua_pop(L, 1);

			// border.width = { left, right, top, bottom }
			lua_getfield(L, -1, "width");
			if (lua_istable(L, -1)) {
				lua_getfield(L, -1, "left");   decl.border.width.left   = (uint16_t)luaL_optinteger(L, -1, 0); lua_pop(L, 1);
				lua_getfield(L, -1, "right");  decl.border.width.right  = (uint16_t)luaL_optinteger(L, -1, 0); lua_pop(L, 1);
				lua_getfield(L, -1, "top");    decl.border.width.top    = (uint16_t)luaL_optinteger(L, -1, 0); lua_pop(L, 1);
				lua_getfield(L, -1, "bottom"); decl.border.width.bottom = (uint16_t)luaL_optinteger(L, -1, 0); lua_pop(L, 1);
			}
			lua_pop(L, 1);
		}
		lua_pop(L, 1);

        // ---------------- image ----------------
        lua_getfield(L, 2, "image");
        if (lua_istable(L, -1)) {
            lua_getfield(L, -1, "imageData");
            decl.image.imageData = lua_islightuserdata(L, -1) ? lua_touserdata(L, -1) : NULL;
            lua_pop(L, 1);
        }
        lua_pop(L, 1);
        
                // ---------------- aspectRatio ----------------
        // Accept either a table { aspectRatio = 1.78 } or a raw number 1.78
        lua_getfield(L, 2, "aspectRatio");
        if (lua_istable(L, -1)) {
            lua_getfield(L, -1, "aspectRatio");
            if (lua_isnumber(L, -1)) {
                decl.aspectRatio.aspectRatio = (float)lua_tonumber(L, -1);
            }
            lua_pop(L, 1);
        } else if (lua_isnumber(L, -1)) {
            decl.aspectRatio.aspectRatio = (float)lua_tonumber(L, -1);
        }
        lua_pop(L, 1);

        // ---------------- clip ----------------
        // { horizontal=true|false, vertical=true|false, childOffset={x=..., y=...} }
        lua_getfield(L, 2, "clip");
        if (lua_istable(L, -1)) {
            lua_getfield(L, -1, "horizontal");
            decl.clip.horizontal = lua_toboolean(L, -1);
            lua_pop(L, 1);

            lua_getfield(L, -1, "vertical");
            decl.clip.vertical = lua_toboolean(L, -1);
            lua_pop(L, 1);

            lua_getfield(L, -1, "childOffset");
            if (lua_istable(L, -1)) {
                lua_getfield(L, -1, "x"); decl.clip.childOffset.x = (float)luaL_optnumber(L, -1, 0.0); lua_pop(L, 1);
                lua_getfield(L, -1, "y"); decl.clip.childOffset.y = (float)luaL_optnumber(L, -1, 0.0); lua_pop(L, 1);
                
            // This is important. Clay_GetScrollOffset() will not work from lua because it's called before element open, 
            // instead we just default to it if omitted
            } else if (decl.clip.horizontal || decl.clip.vertical) {
				decl.clip.childOffset = Clay_GetScrollOffset();
			}
            lua_pop(L, 1);
        }
        lua_pop(L, 1);

        // ---------------- floating ----------------
        // {
        //   offset={x,y}, expand={width,height}, parentId=uint32,
        //   zIndex=int16, attachPoints={ element=CLAY_ATTACH_POINT_*, parent=CLAY_ATTACH_POINT_* },
        //   pointerCaptureMode=CLAY_POINTER_CAPTURE_MODE_*, attachTo=CLAY_ATTACH_TO_*,
        //   clipTo=CLAY_CLIP_TO_*
        // }
        lua_getfield(L, 2, "floating");
        if (lua_istable(L, -1)) {
            // offset
            lua_getfield(L, -1, "offset");
            if (lua_istable(L, -1)) {
                lua_getfield(L, -1, "x"); decl.floating.offset.x = (float)luaL_optnumber(L, -1, 0.0); lua_pop(L, 1);
                lua_getfield(L, -1, "y"); decl.floating.offset.y = (float)luaL_optnumber(L, -1, 0.0); lua_pop(L, 1);
            }
            lua_pop(L, 1);

            // expand
            lua_getfield(L, -1, "expand");
            if (lua_istable(L, -1)) {
                lua_getfield(L, -1, "width");  decl.floating.expand.width  = (float)luaL_optnumber(L, -1, 0.0); lua_pop(L, 1);
                lua_getfield(L, -1, "height"); decl.floating.expand.height = (float)luaL_optnumber(L, -1, 0.0); lua_pop(L, 1);
            }
            lua_pop(L, 1);

            // parentId
            lua_getfield(L, -1, "parentId");
            if (lua_isnumber(L, -1)) { decl.floating.parentId = (uint32_t)lua_tointeger(L, -1); }
            lua_pop(L, 1);

            // zIndex
            lua_getfield(L, -1, "zIndex");
            if (lua_isnumber(L, -1)) { decl.floating.zIndex = (int16_t)lua_tointeger(L, -1); }
            lua_pop(L, 1);

            // attachPoints
            lua_getfield(L, -1, "attachPoints");
            if (lua_istable(L, -1)) {
                lua_getfield(L, -1, "element");
                if (lua_isnumber(L, -1)) decl.floating.attachPoints.element = (int)lua_tointeger(L, -1);
                lua_pop(L, 1);

                lua_getfield(L, -1, "parent");
                if (lua_isnumber(L, -1)) decl.floating.attachPoints.parent = (int)lua_tointeger(L, -1);
                lua_pop(L, 1);
            }
            lua_pop(L, 1);

            // pointerCaptureMode
            lua_getfield(L, -1, "pointerCaptureMode");
            if (lua_isnumber(L, -1)) decl.floating.pointerCaptureMode = (int)lua_tointeger(L, -1);
            lua_pop(L, 1);

            // attachTo
            lua_getfield(L, -1, "attachTo");
            if (lua_isnumber(L, -1)) decl.floating.attachTo = (int)lua_tointeger(L, -1);
            lua_pop(L, 1);

            // clipTo
            lua_getfield(L, -1, "clipTo");
            if (lua_isnumber(L, -1)) decl.floating.clipTo = (int)lua_tointeger(L, -1);
            lua_pop(L, 1);
        }
        lua_pop(L, 1);

        // ---------------- custom ----------------
        // { customData = lightuserdata }
        lua_getfield(L, 2, "custom");
        if (lua_istable(L, -1)) {
            lua_getfield(L, -1, "customData");
            decl.custom.customData = lua_islightuserdata(L, -1) ? lua_touserdata(L, -1) : NULL;
            lua_pop(L, 1);
        }
        lua_pop(L, 1);

        // ---------------- userData ----------------
        lua_getfield(L, 2, "userData");
        if (lua_islightuserdata(L, -1)) {
            decl.userData = lua_touserdata(L, -1);
        }
        lua_pop(L, 1);
    }

    // -------------------------------------------------------------------------
    //Clay__ConfigureOpenElementPtr(&decl);
    Clay__ConfigureOpenElement(CLAY__CONFIG_WRAPPER(Clay_ElementDeclaration, decl));

    // Children callback
    if (lua_isfunction(L, 3)) {
        lua_getglobal(L, "debug");
        lua_getfield(L, -1, "traceback");
        lua_remove(L, -2); // remove 'debug' table
        int traceback_index = lua_gettop(L);

        lua_pushvalue(L, 3);
        if (lua_pcall(L, 0, 0, traceback_index) != LUA_OK) {
            const char *err = lua_tostring(L, -1);
            lua_pop(L, 1);
            Clay__CloseElement();
            return luaL_error(L, "createElement callback failed:\n%s", err);
        }
        lua_pop(L, 1);
    }

    Clay__CloseElement();
    lua_pushboolean(L, 1);
    return 1;
}

static int l_Clay_CreateTextElement(lua_State *L) {
	Clay_String s = Clay_CopyLuaString(L, 1);
	
    Clay_TextElementConfig *cfg = CLAY_TEXT_CONFIG((Clay_TextElementConfig){0});
    cfg->fontId = 1;
    cfg->fontSize = 16;
    cfg->textColor = (Clay_Color){255,255,255,255};
    cfg->wrapMode = CLAY_TEXT_WRAP_WORDS;
    cfg->textAlignment = CLAY_TEXT_ALIGN_LEFT;
    cfg->letterSpacing = 0;
    cfg->lineHeight = 0;
	
    if (lua_istable(L, 2)) {
        lua_getfield(L, 2, "fontId");
        if (lua_isnumber(L, -1)) cfg->fontId = (uint16_t)lua_tointeger(L, -1);
        lua_pop(L, 1);

        lua_getfield(L, 2, "fontSize");
        if (lua_isnumber(L, -1)) cfg->fontSize = (uint16_t)lua_tointeger(L, -1);
        lua_pop(L, 1);

        lua_getfield(L, 2, "textAlignment");
        if (lua_isnumber(L, -1)) cfg->textAlignment = (Clay_TextAlignment)lua_tointeger(L, -1);
        lua_pop(L, 1);
        
		lua_getfield(L, 2, "textColor");
		if (lua_istable(L, -1)) {
			lua_getfield(L, -1, "r"); cfg->textColor.r = (float)luaL_optnumber(L, -1, 255); lua_pop(L, 1);
			lua_getfield(L, -1, "g"); cfg->textColor.g = (float)luaL_optnumber(L, -1, 255); lua_pop(L, 1);
			lua_getfield(L, -1, "b"); cfg->textColor.b = (float)luaL_optnumber(L, -1, 255); lua_pop(L, 1);
			lua_getfield(L, -1, "a"); cfg->textColor.a = (float)luaL_optnumber(L, -1, 255); lua_pop(L, 1);
		}
		lua_pop(L, 1);

        lua_getfield(L, 2, "letterSpacing");
        if (lua_isnumber(L, -1)) cfg->letterSpacing = (uint16_t)lua_tointeger(L, -1);
        lua_pop(L, 1);

        lua_getfield(L, 2, "lineHeight");
        if (lua_isnumber(L, -1)) cfg->lineHeight = (uint16_t)lua_tointeger(L, -1);
        lua_pop(L, 1);

        lua_getfield(L, 2, "wrapMode");
        if (lua_isnumber(L, -1)) cfg->wrapMode = (Clay_TextElementConfigWrapMode)lua_tointeger(L, -1);
        lua_pop(L, 1);
    }
    
	CLAY_TEXT(s, cfg);

    lua_pushboolean(L, 1);
    return 1;
}

static int l_Clay_Id(lua_State *L) {
	Clay_String s = Clay_CopyLuaString(L, 1);
    uint32_t index = (uint32_t)luaL_optinteger(L, 2, 0);
    bool isLocal = lua_toboolean(L, 3);

    Clay_ElementId eid;
    if (index > 0) {
        // CLAY_AUTO_ID or CLAY_AUTO_ID_LOCAL
        eid = Clay__HashStringWithOffset(
            s, index,
            isLocal ? Clay__GetParentElementId() : 0
        );
    } else {
        // CLAY_ID or CLAY_ID_LOCAL
        eid = Clay__HashString(
            s,
            isLocal ? Clay__GetParentElementId() : 0
        );
    }
    
    clay__last_id = eid;  // cache for next element

    lua_newtable(L);
    lua_pushinteger(L, eid.id); lua_rawseti(L, -2, 1);
    lua_pushinteger(L, eid.offset); lua_rawseti(L, -2, 2);
    lua_pushinteger(L, eid.baseId); lua_rawseti(L, -2, 3);
	lua_pushlstring(L, s.chars, s.length); lua_rawseti(L, -2, 4);

    return 1;
}

static int l_Clay_GetLastElementId(lua_State *L) {
	Clay_ElementId eid = clay__last_id;
    lua_newtable(L);
    lua_pushinteger(L, eid.id); lua_rawseti(L, -2, 1);
    lua_pushinteger(L, eid.offset); lua_rawseti(L, -2, 2);
    lua_pushinteger(L, eid.baseId); lua_rawseti(L, -2, 3);
	lua_pushlstring(L, eid.stringId.chars, eid.stringId.length); lua_rawseti(L, -2, 4);
    return 1;
}

static int l_Clay_GetElementId(lua_State *L) {
	Clay_String s = Clay_CopyLuaString(L, 1);

    Clay_ElementId eid = Clay_GetElementId(s);
    lua_newtable(L);
    lua_pushinteger(L, eid.id); lua_rawseti(L, -2, 1);
    lua_pushinteger(L, eid.offset); lua_rawseti(L, -2, 2);
    lua_pushinteger(L, eid.baseId); lua_rawseti(L, -2, 3);
	lua_pushlstring(L, eid.stringId.chars, eid.stringId.length); lua_rawseti(L, -2, 4);
    return 1;
}

static int l_Clay_GetElementIdWithIndex(lua_State *L) {
	Clay_String s = Clay_CopyLuaString(L, 1);
    uint32_t index = (uint32_t)luaL_checkinteger(L, 2);
	
    Clay_ElementId eid = Clay_GetElementIdWithIndex(s, index);
    lua_newtable(L);
    lua_pushinteger(L, eid.id); lua_rawseti(L, -2, 1);
    lua_pushinteger(L, eid.offset); lua_rawseti(L, -2, 2);
    lua_pushinteger(L, eid.baseId); lua_rawseti(L, -2, 3);
	lua_pushlstring(L, eid.stringId.chars, eid.stringId.length); lua_rawseti(L, -2, 4);
    return 1;
}

static int l_Clay_BeginLayout(lua_State *L) {
    Clay_BeginLayout();
    return 0;
}

typedef struct {
    Clay_RenderCommandArray array;
    int index;
} Clay_IteratorState;

static int clay_iter_next(lua_State *L) {
    Clay_IteratorState* it = (Clay_IteratorState*)lua_touserdata(L, lua_upvalueindex(1));
    if (!it || it->index >= it->array.length)
        return 0;

    Clay_RenderCommand* cmd = &it->array.internalArray[it->index++];

    // Wrap pointer as userdata (not lightuserdata so metatable can attach)
    Clay_RenderCommand** udata = (Clay_RenderCommand**)lua_newuserdata(L, sizeof(Clay_RenderCommand*));
    *udata = cmd;

    luaL_setmetatable(L, "ClayCommand");
    return 1;
}

static int l_Clay_EndLayoutIter(lua_State *L) {
    Clay_IteratorState* it = (Clay_IteratorState*)lua_newuserdata(L, sizeof(Clay_IteratorState));
    *it = (Clay_IteratorState){0};
    it->array = Clay_EndLayout();
    it->index = 0;

    lua_pushcclosure(L, clay_iter_next, 1);
    return 1;
}

static Clay_RenderCommand* checkcmd(lua_State *L) {
    return *(Clay_RenderCommand**)luaL_checkudata(L, 1, "ClayCommand");
}

static int l_ClayCmd_Type(lua_State *L) {
    Clay_RenderCommand* cmd = checkcmd(L);
    lua_pushinteger(L, cmd->commandType);
    return 1;
}

static int l_ClayCmd_Id(lua_State *L) {
    Clay_RenderCommand* cmd = checkcmd(L);
    lua_pushinteger(L, cmd->id);
    return 1;
}

static int l_ClayCmd_zIndex(lua_State *L) {
    Clay_RenderCommand* cmd = checkcmd(L);
    lua_pushinteger(L, cmd->zIndex);
    return 1;
}

static int l_ClayCmd_Bounds(lua_State *L) {
    Clay_RenderCommand* cmd = checkcmd(L);
    lua_pushnumber(L, cmd->boundingBox.x);
    lua_pushnumber(L, cmd->boundingBox.y);
    lua_pushnumber(L, cmd->boundingBox.width);
    lua_pushnumber(L, cmd->boundingBox.height);
    return 4;
}

static int l_ClayCmd_Color(lua_State *L) {
    Clay_RenderCommand* cmd = checkcmd(L);
    switch (cmd->commandType) {
        case CLAY_RENDER_COMMAND_TYPE_RECTANGLE:
            lua_pushnumber(L, cmd->renderData.rectangle.backgroundColor.r);
            lua_pushnumber(L, cmd->renderData.rectangle.backgroundColor.g);
            lua_pushnumber(L, cmd->renderData.rectangle.backgroundColor.b);
            lua_pushnumber(L, cmd->renderData.rectangle.backgroundColor.a);
            return 4;

        case CLAY_RENDER_COMMAND_TYPE_TEXT:
            lua_pushnumber(L, cmd->renderData.text.textColor.r);
            lua_pushnumber(L, cmd->renderData.text.textColor.g);
            lua_pushnumber(L, cmd->renderData.text.textColor.b);
            lua_pushnumber(L, cmd->renderData.text.textColor.a);
            return 4;

        case CLAY_RENDER_COMMAND_TYPE_IMAGE:
            lua_pushnumber(L, cmd->renderData.image.backgroundColor.r);
            lua_pushnumber(L, cmd->renderData.image.backgroundColor.g);
            lua_pushnumber(L, cmd->renderData.image.backgroundColor.b);
            lua_pushnumber(L, cmd->renderData.image.backgroundColor.a);
            return 4;

        case CLAY_RENDER_COMMAND_TYPE_BORDER:
            lua_pushnumber(L, cmd->renderData.border.color.r);
            lua_pushnumber(L, cmd->renderData.border.color.g);
            lua_pushnumber(L, cmd->renderData.border.color.b);
            lua_pushnumber(L, cmd->renderData.border.color.a);
            return 4;

        default:
            return 0;
    }
}

static int l_ClayCmd_Text(lua_State *L) {
    Clay_RenderCommand* cmd = checkcmd(L);
    if (cmd->commandType != CLAY_RENDER_COMMAND_TYPE_TEXT) return 0;
    lua_pushlstring(L,
        cmd->renderData.text.stringContents.chars,
        cmd->renderData.text.stringContents.length);
    lua_pushinteger(L, cmd->renderData.text.fontId);
    lua_pushinteger(L, cmd->renderData.text.fontSize);
    lua_pushinteger(L, cmd->renderData.text.letterSpacing);
    lua_pushinteger(L, cmd->renderData.text.lineHeight);
    
    return 5;
}

static int l_ClayCmd_CornerRadius(lua_State *L) {
    Clay_RenderCommand* cmd = checkcmd(L);
    if (cmd->commandType == CLAY_RENDER_COMMAND_TYPE_RECTANGLE) {
        lua_pushnumber(L, cmd->renderData.rectangle.cornerRadius.topLeft);
        lua_pushnumber(L, cmd->renderData.rectangle.cornerRadius.topRight);
        lua_pushnumber(L, cmd->renderData.rectangle.cornerRadius.bottomLeft);
        lua_pushnumber(L, cmd->renderData.rectangle.cornerRadius.bottomRight);
        return 4;
    } else if (cmd->commandType == CLAY_RENDER_COMMAND_TYPE_BORDER) {
        lua_pushnumber(L, cmd->renderData.border.cornerRadius.topLeft);
        lua_pushnumber(L, cmd->renderData.border.cornerRadius.topRight);
        lua_pushnumber(L, cmd->renderData.border.cornerRadius.bottomLeft);
        lua_pushnumber(L, cmd->renderData.border.cornerRadius.bottomRight);
        return 4;
    }
    return 0;
}

static int l_ClayCmd_BorderWidth(lua_State *L) {
    Clay_RenderCommand* cmd = checkcmd(L);
    if (cmd->commandType != CLAY_RENDER_COMMAND_TYPE_BORDER)
        return 0;
    lua_pushnumber(L, cmd->renderData.border.width.left);
    lua_pushnumber(L, cmd->renderData.border.width.right);
    lua_pushnumber(L, cmd->renderData.border.width.top);
    lua_pushnumber(L, cmd->renderData.border.width.bottom);
    return 4;
}

static int l_ClayCmd_ImageData(lua_State *L) {
    Clay_RenderCommand* cmd = checkcmd(L);
    if (cmd->commandType != CLAY_RENDER_COMMAND_TYPE_IMAGE)
        return 0;
    lua_pushlightuserdata(L, cmd->renderData.image.imageData);
    return 1;
}

static int l_ClayCmd_Clip(lua_State *L) {
    Clay_RenderCommand* cmd = checkcmd(L);
    if (cmd->commandType != CLAY_RENDER_COMMAND_TYPE_SCISSOR_START &&
        cmd->commandType != CLAY_RENDER_COMMAND_TYPE_SCISSOR_END)
        return 0;
    lua_pushboolean(L, cmd->renderData.clip.horizontal);
    lua_pushboolean(L, cmd->renderData.clip.vertical);
    return 2;
}

static void Clay_CreateCommandMetatable(lua_State *L) {
    if (luaL_newmetatable(L, "ClayCommand")) {
        lua_pushcfunction(L, l_ClayCmd_Type);
        lua_setfield(L, -2, "type");

        lua_pushcfunction(L, l_ClayCmd_Id);
        lua_setfield(L, -2, "id");

        lua_pushcfunction(L, l_ClayCmd_Bounds);
        lua_setfield(L, -2, "bounds");

        lua_pushcfunction(L, l_ClayCmd_Color);
        lua_setfield(L, -2, "color");

        lua_pushcfunction(L, l_ClayCmd_Text);
        lua_setfield(L, -2, "text");
        
        lua_pushcfunction(L, l_ClayCmd_CornerRadius);
		lua_setfield(L, -2, "cornerRadius");

		lua_pushcfunction(L, l_ClayCmd_BorderWidth);
		lua_setfield(L, -2, "borderWidth");

		lua_pushcfunction(L, l_ClayCmd_ImageData);
		lua_setfield(L, -2, "imageData");

		lua_pushcfunction(L, l_ClayCmd_Clip);
		lua_setfield(L, -2, "clip");

		lua_pushcfunction(L, l_ClayCmd_zIndex);
		lua_setfield(L, -2, "zIndex");

        // allow method syntax cmd:method()
        lua_pushvalue(L, -1);
        lua_setfield(L, -2, "__index");
    }
    lua_pop(L, 1);
}

static int l_Clay_SetLayoutDimensions(lua_State *L) {
    double w = luaL_checknumber(L, 1);
    double h = luaL_checknumber(L, 2);
    Clay_SetLayoutDimensions((Clay_Dimensions){ (float)w, (float)h });
    return 0;
}

static int l_Clay_SetPointerState(lua_State *L) {
    double x = luaL_checknumber(L, 1);
    double y = luaL_checknumber(L, 2);
    bool down = lua_toboolean(L, 3);
    Clay_SetPointerState((Clay_Vector2){ (float)x, (float)y }, down);
    return 0;
}

static int l_Clay_UpdateScrollContainers(lua_State *L) {
    bool enable = lua_toboolean(L, 1);
    double dx = luaL_checknumber(L, 2);
    double dy = luaL_checknumber(L, 3);
    double dt = luaL_checknumber(L, 4);
    Clay_UpdateScrollContainers(enable, (Clay_Vector2){(float)dx, (float)dy}, (float)dt);
    return 0;
}

static int l_Clay_GetScrollOffset(lua_State *L) {
    Clay_Vector2 off = Clay_GetScrollOffset();
    lua_newtable(L);
    lua_pushnumber(L, off.x); lua_setfield(L, -2, "x");
    lua_pushnumber(L, off.y); lua_setfield(L, -2, "y");
    return 1;
}

static int l_Clay_GetElementData(lua_State *L) {
    uint32_t id = (uint32_t)luaL_checkinteger(L, 1);
    Clay_ElementData d = Clay_GetElementData((Clay_ElementId){ .id = id });
    lua_newtable(L);
    lua_pushnumber(L, d.boundingBox.x); lua_setfield(L, -2, "x");
    lua_pushnumber(L, d.boundingBox.y); lua_setfield(L, -2, "y");
    lua_pushnumber(L, d.boundingBox.width); lua_setfield(L, -2, "width");
    lua_pushnumber(L, d.boundingBox.height); lua_setfield(L, -2, "height");
    lua_pushboolean(L, d.found); lua_setfield(L, -2, "found");
    return 1;
}

static void ClayErrorPrinter(Clay_ErrorData err) {
    fprintf(stderr, "[Clay Error] %.*s\n", (int)err.errorText.length, err.errorText.chars);
    switch(err.errorType) {
        // etc.
    }
}

static int l_Clay_Initialize(lua_State *L) {
    size_t capacity = (size_t)luaL_checkinteger(L, 1);
    float width  = (float)luaL_checknumber(L, 2);
    float height = (float)luaL_checknumber(L, 3);

    void *mem = malloc(capacity);
    if (!mem)
        return luaL_error(L, "malloc failed");

    Clay_Arena arena = Clay_CreateArenaWithCapacityAndMemory(capacity, mem);
    Clay_Context *ctx = Clay_Initialize(
        arena,
        (Clay_Dimensions){ width, height },
        (Clay_ErrorHandler){ ClayErrorPrinter, NULL }
    );

    if (!ctx) {
        free(mem);
        lua_pushnil(L);
        lua_pushnil(L);
        return 2;
    }

    Clay_SetMeasureTextFunction(Bridge_MeasureTextFunction, NULL);

    g_ClayArenaMem = mem;
    g_ClayArenaCap = capacity;

    lua_pushlightuserdata(L, mem);
    lua_pushlightuserdata(L, ctx);
    return 2;
}


static int l_Clay_Shutdown(lua_State* L) {
    (void)L;
    if (g_ClayArenaMem) {
        free(g_ClayArenaMem);
        g_ClayArenaMem = NULL;
        g_ClayArenaCap = 0;
    }
    return 0;
}

static int l_Clay_Hovered(lua_State *L) {
    bool hovered = Clay_Hovered();
    lua_pushboolean(L, hovered);
    return 1;
}

static int l_Clay_PointerOver(lua_State *L) {
    uint32_t id = (uint32_t)luaL_checkinteger(L, 1);
    bool over = Clay_PointerOver((Clay_ElementId){ .id = id });
    lua_pushboolean(L, over);
    return 1;
}

static int l_Clay_GetScrollContainerData(lua_State *L) {
	Clay_ElementId elid;

    // param 1: element id table {id, offset, baseId, label}
    if (!lua_istable(L, 1))
        return luaL_error(L, "GetScrollContainerData requires an id table from clay.id()");

    lua_rawgeti(L, 1, 1);
    elid.id = (uint32_t)luaL_checkinteger(L, -1);
    lua_pop(L, 1);

    lua_rawgeti(L, 1, 2);
    elid.offset = (uint32_t)luaL_optinteger(L, -1, 0);
    lua_pop(L, 1);

    lua_rawgeti(L, 1, 3);
    elid.baseId = (uint32_t)luaL_optinteger(L, -1, 0);
    lua_pop(L, 1);
    
    Clay_ScrollContainerData data = Clay_GetScrollContainerData(elid);

    if (!data.found) {
        lua_pushnil(L);
        return 1;
    }

    lua_newtable(L);  // main return table

    // scrollPosition {x, y}
    lua_newtable(L);
    lua_pushnumber(L, data.scrollPosition ? data.scrollPosition->x : 0.0);
    lua_setfield(L, -2, "x");
    lua_pushnumber(L, data.scrollPosition ? data.scrollPosition->y : 0.0);
    lua_setfield(L, -2, "y");
    lua_setfield(L, -2, "scrollPosition");

    // scrollContainerDimensions {width, height}
    lua_newtable(L);
    lua_pushnumber(L, data.scrollContainerDimensions.width);
    lua_setfield(L, -2, "width");
    lua_pushnumber(L, data.scrollContainerDimensions.height);
    lua_setfield(L, -2, "height");
    lua_setfield(L, -2, "scrollContainerDimensions");

    // contentDimensions {width, height}
    lua_newtable(L);
    lua_pushnumber(L, data.contentDimensions.width);
    lua_setfield(L, -2, "width");
    lua_pushnumber(L, data.contentDimensions.height);
    lua_setfield(L, -2, "height");
    lua_setfield(L, -2, "contentDimensions");

    // config {horizontal, vertical, childOffset={x,y}}
    lua_newtable(L);
    lua_pushboolean(L, data.config.horizontal);
    lua_setfield(L, -2, "horizontal");
    lua_pushboolean(L, data.config.vertical);
    lua_setfield(L, -2, "vertical");

    lua_newtable(L);
    lua_pushnumber(L, data.config.childOffset.x);
    lua_setfield(L, -2, "x");
    lua_pushnumber(L, data.config.childOffset.y);
    lua_setfield(L, -2, "y");
    lua_setfield(L, -2, "childOffset");

    lua_setfield(L, -2, "config");

    lua_pushboolean(L, data.found);
    lua_setfield(L, -2, "found");

    return 1;
}

// Set absolute scroll position for a specific scroll container
static int l_Clay_SetScrollContainerPosition(lua_State *L) {
    Clay_ElementId elid;

    // param 1: element id table {id, offset, baseId, label}
    if (!lua_istable(L, 1))
        return luaL_error(L, "SetScrollContainerPosition requires an id table from clay.id()");

    lua_rawgeti(L, 1, 1);
    elid.id = (uint32_t)luaL_checkinteger(L, -1);
    lua_pop(L, 1);

    lua_rawgeti(L, 1, 2);
    elid.offset = (uint32_t)luaL_optinteger(L, -1, 0);
    lua_pop(L, 1);

    lua_rawgeti(L, 1, 3);
    elid.baseId = (uint32_t)luaL_optinteger(L, -1, 0);
    lua_pop(L, 1);

    float x = (float)luaL_optnumber(L, 2, 0.0);
    float y = (float)luaL_optnumber(L, 3, 0.0);

    Clay_ScrollContainerData data = Clay_GetScrollContainerData(elid);
    if (!data.found || data.scrollPosition == NULL) {
        return 0; // silently ignore if it's not a scroll container this frame
    }

    // Write directly to Clay's internal scroll position
    data.scrollPosition->x = x;
    data.scrollPosition->y = y;
    return 0;
}


// clay.setScrollOffset(id, x, y)
static int l_Clay_SetScrollOffset(lua_State *L) {
    Clay_ElementId elid;

    // param 1: element id table {id, offset, baseId, label}
    if (!lua_istable(L, 1))
        return luaL_error(L, "setScrollOffset(idTable, x, y) requires an id table from clay.id()");

    lua_rawgeti(L, 1, 1);
    elid.id = (uint32_t)luaL_checkinteger(L, -1);
    lua_pop(L, 1);

    lua_rawgeti(L, 1, 2);
    elid.offset = (uint32_t)luaL_optinteger(L, -1, 0);
    lua_pop(L, 1);

    lua_rawgeti(L, 1, 3);
    elid.baseId = (uint32_t)luaL_optinteger(L, -1, 0);
    lua_pop(L, 1);

    // params 2 and 3: scroll x, y
    float x = (float)luaL_optnumber(L, 2, 0.0);
    float y = (float)luaL_optnumber(L, 3, 0.0);

    Clay_Context* ctx = Clay_GetCurrentContext();
    for (int32_t i = 0; i < ctx->scrollContainerDatas.length; ++i) {
        Clay__ScrollContainerDataInternal *mapping =
            Clay__ScrollContainerDataInternalArray_Get(&ctx->scrollContainerDatas, i);
        if (mapping->elementId == elid.id) {
            mapping->scrollPosition.x = x;
            mapping->scrollPosition.y = y;
            mapping->openThisFrame = true;
            break;
        }
    }

    return 0;
}

static int l_Clay_SetScrollPosition(lua_State* L) {
    Clay_ElementId id;
    id.id = (uint32_t)luaL_checkinteger(L, 1);

    Clay_ScrollContainerData data = Clay_GetScrollContainerData(id);
    if (!data.found || !data.scrollPosition) {
        // Optionally print a warning or just silently return
        // printf("Scroll container not found for id %u\n", id.id);
        return 0;
    }

    // Expect a table {x=..., y=...}
    luaL_checktype(L, 2, LUA_TTABLE);

    lua_getfield(L, 2, "x");
    float x = (float)luaL_optnumber(L, -1, data.scrollPosition->x);
    lua_pop(L, 1);

    lua_getfield(L, 2, "y");
    float y = (float)luaL_optnumber(L, -1, data.scrollPosition->y);
    lua_pop(L, 1);

    // Directly modify Clay's internal scroll position
    data.scrollPosition->x = x;
    data.scrollPosition->y = y;

    return 0;
}

static int l_Clay_SetDebugModeEnabled(lua_State *L) {
    bool enabled = lua_toboolean(L, 1);
    Clay_SetDebugModeEnabled(enabled);
    return 0;
}

static int l_Clay_IsDebugModeEnabled(lua_State *L) {
    bool enabled = Clay_IsDebugModeEnabled();
    lua_pushboolean(L, enabled);
    return 1;
}

static int l_Clay_SetCullingEnabled(lua_State *L) {
    bool enabled = lua_toboolean(L, 1);
    Clay_SetCullingEnabled(enabled);
    return 0;
}

static int l_Clay_GetMaxElementCount(lua_State *L) {
    int32_t count = Clay_GetMaxElementCount();
    lua_pushinteger(L, count);
    return 1;
}

static int l_Clay_SetMaxElementCount(lua_State *L) {
    int32_t count = (int32_t)luaL_checkinteger(L, 1);
    Clay_SetMaxElementCount(count);
    return 0;
}

static int l_Clay_SetExternalScrollHandlingEnabled(lua_State *L) {
	bool enabled = lua_toboolean(L, 1);
	Clay_SetExternalScrollHandlingEnabled(enabled);
	return 0;
}

static int l_Clay_GetMaxMeasureTextCacheWordCount(lua_State *L) {
    int32_t count = Clay_GetMaxMeasureTextCacheWordCount();
    lua_pushinteger(L, count);
    return 1;
}

static int l_Clay_SetMaxMeasureTextCacheWordCount(lua_State *L) {
    int32_t count = (int32_t)luaL_checkinteger(L, 1);
    Clay_SetMaxMeasureTextCacheWordCount(count);
    return 0;
}

static int l_Clay_ResetMeasureTextCache(lua_State *L) {
    Clay_ResetMeasureTextCache();
    return 0;
}

// Helper functions for sizing
static int l_Clay_SizingFixed(lua_State *L) {
    // Match CLAY_SIZING_FIXED exactly: min = max = size by default
    float size = (float)luaL_checknumber(L, 1);
    float min = size;
    float max = size;

    // Optional second arg to override max
    if (lua_gettop(L) >= 2 && lua_isnumber(L, 2)) {
        max = (float)lua_tonumber(L, 2);
    }

    lua_newtable(L);                      // main table
    lua_pushinteger(L, CLAY__SIZING_TYPE_FIXED);
    lua_setfield(L, -2, "type");

    lua_newtable(L);                      // minMax subtable
    lua_pushnumber(L, min); lua_setfield(L, -2, "min");
    lua_pushnumber(L, max); lua_setfield(L, -2, "max");
    lua_setfield(L, -2, "minMax");

    return 1;
}

static int l_Clay_SizingGrow(lua_State *L) {
    float min = 0.0f, max = 0.0f;  // Clay sets max = CLAY__MAX_FLOAT internally if <= 0

    if (lua_gettop(L) >= 1 && lua_isnumber(L, 1))
        min = (float)lua_tonumber(L, 1);
    if (lua_gettop(L) >= 2 && lua_isnumber(L, 2))
        max = (float)lua_tonumber(L, 2);

    lua_newtable(L);
    lua_pushinteger(L, CLAY__SIZING_TYPE_GROW);
    lua_setfield(L, -2, "type");

    lua_newtable(L);
    lua_pushnumber(L, min); lua_setfield(L, -2, "min");
    lua_pushnumber(L, max); lua_setfield(L, -2, "max");
    lua_setfield(L, -2, "minMax");

    return 1;
}

static int l_Clay_SizingFit(lua_State *L) {
    float min = 0.0f, max = 0.0f; // Clay sets max = CLAY__MAX_FLOAT internally if <= 0

    if (lua_gettop(L) >= 1 && lua_isnumber(L, 1))
        min = (float)lua_tonumber(L, 1);
    if (lua_gettop(L) >= 2 && lua_isnumber(L, 2))
        max = (float)lua_tonumber(L, 2);

    lua_newtable(L);
    lua_pushinteger(L, CLAY__SIZING_TYPE_FIT);
    lua_setfield(L, -2, "type");

    lua_newtable(L);
    lua_pushnumber(L, min); lua_setfield(L, -2, "min");
    lua_pushnumber(L, max); lua_setfield(L, -2, "max");
    lua_setfield(L, -2, "minMax");

    return 1;
}

static int l_Clay_SizingPercent(lua_State *L) {
    float percent = (float)luaL_checknumber(L, 1);

    lua_newtable(L);
    lua_pushinteger(L, CLAY__SIZING_TYPE_PERCENT);
    lua_setfield(L, -2, "type");

    lua_pushnumber(L, percent);
    lua_setfield(L, -2, "percent");

    return 1;
}

// -----------------------------------------------------------------------------
// Padding helpers
// -----------------------------------------------------------------------------
static int l_Clay_PaddingAll(lua_State *L) {
    float all = (float)luaL_checknumber(L, 1);
    lua_newtable(L);
    lua_pushnumber(L, all); lua_setfield(L, -2, "left");
    lua_pushnumber(L, all); lua_setfield(L, -2, "right");
    lua_pushnumber(L, all); lua_setfield(L, -2, "top");
    lua_pushnumber(L, all); lua_setfield(L, -2, "bottom");
    return 1;
}

static int l_Clay_PaddingXY(lua_State *L) {
    float x = (float)luaL_checknumber(L, 1);
    float y = (float)luaL_checknumber(L, 2);
    lua_newtable(L);
    lua_pushnumber(L, x); lua_setfield(L, -2, "left");
    lua_pushnumber(L, x); lua_setfield(L, -2, "right");
    lua_pushnumber(L, y); lua_setfield(L, -2, "top");
    lua_pushnumber(L, y); lua_setfield(L, -2, "bottom");
    return 1;
}

static int l_Clay_PaddingLTRB(lua_State *L) {
    float left = (float)luaL_checknumber(L, 1);
    float top = (float)luaL_checknumber(L, 2);
    float right = (float)luaL_checknumber(L, 3);
    float bottom = (float)luaL_checknumber(L, 4);
    lua_newtable(L);
    lua_pushnumber(L, left);   lua_setfield(L, -2, "left");
    lua_pushnumber(L, right);  lua_setfield(L, -2, "right");
    lua_pushnumber(L, top);    lua_setfield(L, -2, "top");
    lua_pushnumber(L, bottom); lua_setfield(L, -2, "bottom");
    return 1;
}

// -----------------------------------------------------------------------------
// Floating helper
// -----------------------------------------------------------------------------
static int l_Clay_Floating(lua_State *L) {
    lua_newtable(L);
    lua_getfield(L, 1, "attachPoint"); // optional int
    if (lua_isnumber(L, -1))
        lua_setfield(L, -2, "attachPoint");
    lua_pop(L, 1);

    lua_getfield(L, 1, "offset");
    if (lua_istable(L, -1)) {
        lua_newtable(L);
        lua_getfield(L, -2, "x");
        lua_setfield(L, -3, "x");
        lua_getfield(L, -2, "y");
        lua_setfield(L, -3, "y");
        lua_setfield(L, -2, "offset");
    }
    lua_pop(L, 1);
    return 1;
}

// -----------------------------------------------------------------------------
// Module registration
// -----------------------------------------------------------------------------
int luaopen_clay(lua_State *L) {
    lua_newtable(L);

    // Core layout
    lua_pushcfunction(L, l_Clay_BeginLayout); lua_setfield(L, -2, "beginLayout");
    lua_pushcfunction(L, l_Clay_EndLayoutIter); lua_setfield(L, -2, "endLayoutIter");
    lua_pushcfunction(L, l_Clay_CreateElement); lua_setfield(L, -2, "createElement");
    lua_pushcfunction(L, l_Clay_CreateTextElement); lua_setfield(L, -2, "createTextElement");
    lua_pushcfunction(L, l_Clay_Id); lua_setfield(L, -2, "id");
    lua_pushcfunction(L, l_Clay_GetLastElementId); lua_setfield(L, -2, "getLastElementId");
    
    lua_pushcfunction(L, l_Clay_GetElementId); lua_setfield(L, -2, "getElementId");
    lua_pushcfunction(L, l_Clay_GetElementIdWithIndex); lua_setfield(L, -2, "getElementIdWithIndex");

    // Layout config / runtime
    lua_pushcfunction(L, l_Clay_SetLayoutDimensions); lua_setfield(L, -2, "setLayoutDimensions");
    lua_pushcfunction(L, l_Clay_SetPointerState); lua_setfield(L, -2, "setPointerState");
    lua_pushcfunction(L, l_Clay_UpdateScrollContainers); lua_setfield(L, -2, "updateScrollContainers");
    lua_pushcfunction(L, l_Clay_GetScrollOffset); lua_setfield(L, -2, "getScrollOffset");
    lua_pushcfunction(L, l_Clay_GetElementData); lua_setfield(L, -2, "getElementData");

    // Core API
    lua_pushcfunction(L, l_Clay_GetCurrentContext); lua_setfield(L, -2, "getCurrentContext");
    lua_pushcfunction(L, l_Clay_Initialize); lua_setfield(L, -2, "initialize");
    lua_pushcfunction(L, l_Clay_Shutdown); lua_setfield(L, -2, "shutdown");
    lua_pushcfunction(L, l_Clay_MinMemorySize); lua_setfield(L, -2, "minMemorySize");
    lua_pushcfunction(L, l_Clay_CreateArenaWithCapacityAndMemory); lua_setfield(L, -2, "createArenaWithCapacityAndMemory");
    lua_pushcfunction(L, l_Clay_Hovered); lua_setfield(L, -2, "hovered");
    lua_pushcfunction(L, l_Clay_PointerOver); lua_setfield(L, -2, "pointerOver");
    lua_pushcfunction(L, l_Clay_GetScrollContainerData); lua_setfield(L, -2, "getScrollContainerData");
    lua_pushcfunction(L, l_Clay_SetScrollContainerPosition); lua_setfield(L, -2, "setScrollContainerPosition");
    lua_pushcfunction(L, l_Clay_SetScrollOffset); lua_setfield(L, -2, "setScrollOffset");
    lua_pushcfunction(L, l_Clay_SetDebugModeEnabled); lua_setfield(L, -2, "setDebugModeEnabled");
    lua_pushcfunction(L, l_Clay_IsDebugModeEnabled); lua_setfield(L, -2, "isDebugModeEnabled");
    lua_pushcfunction(L, l_Clay_SetCullingEnabled); lua_setfield(L, -2, "setCullingEnabled");
    lua_pushcfunction(L, l_Clay_GetMaxElementCount); lua_setfield(L, -2, "getMaxElementCount");
    lua_pushcfunction(L, l_Clay_SetMaxElementCount); lua_setfield(L, -2, "setMaxElementCount");
    lua_pushcfunction(L, l_Clay_SetExternalScrollHandlingEnabled); lua_setfield(L, -2, "setExternalScrollHandlingEnabled");
    lua_pushcfunction(L, l_Clay_GetMaxMeasureTextCacheWordCount); lua_setfield(L, -2, "getMaxMeasureTextCacheWordCount");
    lua_pushcfunction(L, l_Clay_SetMaxMeasureTextCacheWordCount); lua_setfield(L, -2, "setMaxMeasureTextCacheWordCount");
    lua_pushcfunction(L, l_Clay_ResetMeasureTextCache); lua_setfield(L, -2, "resetMeasureTextCache");

    // Custom hooks
    lua_pushcfunction(L, l_Clay_SetMeasureTextFunction); lua_setfield(L, -2, "setMeasureTextFunction");

    // Helper functions for creating configs
    lua_pushcfunction(L, l_Clay_SizingFixed); lua_setfield(L, -2, "sizingFixed");
    lua_pushcfunction(L, l_Clay_SizingGrow); lua_setfield(L, -2, "sizingGrow");
    lua_pushcfunction(L, l_Clay_SizingFit); lua_setfield(L, -2, "sizingFit");
    lua_pushcfunction(L, l_Clay_SizingPercent); lua_setfield(L, -2, "sizingPercent");
	lua_pushcfunction(L, l_Clay_PaddingAll); lua_setfield(L, -2, "paddingAll");
	lua_pushcfunction(L, l_Clay_PaddingXY); lua_setfield(L, -2, "paddingXY");
	lua_pushcfunction(L, l_Clay_PaddingLTRB); lua_setfield(L, -2, "paddingLTRB");
	lua_pushcfunction(L, l_Clay_Floating); lua_setfield(L, -2, "floating");

    // Export command type constants
    lua_pushinteger(L, CLAY_RENDER_COMMAND_TYPE_NONE); lua_setfield(L, -2, "RENDER_NONE");
    lua_pushinteger(L, CLAY_RENDER_COMMAND_TYPE_RECTANGLE); lua_setfield(L, -2, "RENDER_RECTANGLE");
    lua_pushinteger(L, CLAY_RENDER_COMMAND_TYPE_BORDER); lua_setfield(L, -2, "RENDER_BORDER");
    lua_pushinteger(L, CLAY_RENDER_COMMAND_TYPE_TEXT); lua_setfield(L, -2, "RENDER_TEXT");
    lua_pushinteger(L, CLAY_RENDER_COMMAND_TYPE_IMAGE); lua_setfield(L, -2, "RENDER_IMAGE");
    lua_pushinteger(L, CLAY_RENDER_COMMAND_TYPE_SCISSOR_START); lua_setfield(L, -2, "RENDER_SCISSOR_START");
    lua_pushinteger(L, CLAY_RENDER_COMMAND_TYPE_SCISSOR_END); lua_setfield(L, -2, "RENDER_SCISSOR_END");
    lua_pushinteger(L, CLAY_RENDER_COMMAND_TYPE_CUSTOM); lua_setfield(L, -2, "RENDER_CUSTOM");

    // Export sizing type constants
    lua_pushinteger(L, CLAY__SIZING_TYPE_FIT); lua_setfield(L, -2, "SIZING_FIT");
    lua_pushinteger(L, CLAY__SIZING_TYPE_GROW); lua_setfield(L, -2, "SIZING_GROW");
    lua_pushinteger(L, CLAY__SIZING_TYPE_FIXED); lua_setfield(L, -2, "SIZING_FIXED");
    lua_pushinteger(L, CLAY__SIZING_TYPE_PERCENT); lua_setfield(L, -2, "SIZING_PERCENT");

    // Export layout align constants
    lua_pushinteger(L, CLAY_ALIGN_X_LEFT); lua_setfield(L, -2, "ALIGN_X_LEFT");
    lua_pushinteger(L, CLAY_ALIGN_X_CENTER); lua_setfield(L, -2, "ALIGN_X_CENTER");
    lua_pushinteger(L, CLAY_ALIGN_X_RIGHT); lua_setfield(L, -2, "ALIGN_X_RIGHT");
    lua_pushinteger(L, CLAY_ALIGN_Y_TOP); lua_setfield(L, -2, "ALIGN_Y_TOP");
    lua_pushinteger(L, CLAY_ALIGN_Y_CENTER); lua_setfield(L, -2, "ALIGN_Y_CENTER");
    lua_pushinteger(L, CLAY_ALIGN_Y_BOTTOM); lua_setfield(L, -2, "ALIGN_Y_BOTTOM");

    lua_pushinteger(L, CLAY_TEXT_ALIGN_LEFT); lua_setfield(L, -2, "TEXT_ALIGN_LEFT");
    lua_pushinteger(L, CLAY_TEXT_ALIGN_CENTER); lua_setfield(L, -2, "TEXT_ALIGN_CENTER");
    lua_pushinteger(L, CLAY_TEXT_ALIGN_RIGHT); lua_setfield(L, -2, "TEXT_ALIGN_RIGHT");
    
    // Export text wrap mode constants
    lua_pushinteger(L, CLAY_TEXT_WRAP_NONE); lua_setfield(L, -2, "TEXT_WRAP_NONE");
    lua_pushinteger(L, CLAY_TEXT_WRAP_WORDS); lua_setfield(L, -2, "TEXT_WRAP_WORDS");
    lua_pushinteger(L, CLAY_TEXT_WRAP_NEWLINES); lua_setfield(L, -2, "TEXT_WRAP_NEWLINES");

    // Export layout direction constants
    lua_pushinteger(L, CLAY_LEFT_TO_RIGHT); lua_setfield(L, -2, "LEFT_TO_RIGHT");
    lua_pushinteger(L, CLAY_TOP_TO_BOTTOM); lua_setfield(L, -2, "TOP_TO_BOTTOM");

    // Floating attach point constants
    lua_pushinteger(L, CLAY_ATTACH_POINT_LEFT_TOP);       lua_setfield(L, -2, "ATTACH_POINT_LEFT_TOP");
    lua_pushinteger(L, CLAY_ATTACH_POINT_LEFT_CENTER);    lua_setfield(L, -2, "ATTACH_POINT_LEFT_CENTER");
    lua_pushinteger(L, CLAY_ATTACH_POINT_LEFT_BOTTOM);    lua_setfield(L, -2, "ATTACH_POINT_LEFT_BOTTOM");
    lua_pushinteger(L, CLAY_ATTACH_POINT_CENTER_TOP);     lua_setfield(L, -2, "ATTACH_POINT_CENTER_TOP");
    lua_pushinteger(L, CLAY_ATTACH_POINT_CENTER_CENTER);  lua_setfield(L, -2, "ATTACH_POINT_CENTER_CENTER");
    lua_pushinteger(L, CLAY_ATTACH_POINT_CENTER_BOTTOM);  lua_setfield(L, -2, "ATTACH_POINT_CENTER_BOTTOM");
    lua_pushinteger(L, CLAY_ATTACH_POINT_RIGHT_TOP);      lua_setfield(L, -2, "ATTACH_POINT_RIGHT_TOP");
    lua_pushinteger(L, CLAY_ATTACH_POINT_RIGHT_CENTER);   lua_setfield(L, -2, "ATTACH_POINT_RIGHT_CENTER");
    lua_pushinteger(L, CLAY_ATTACH_POINT_RIGHT_BOTTOM);   lua_setfield(L, -2, "ATTACH_POINT_RIGHT_BOTTOM");

    // Floating 'attachTo' constants
    lua_pushinteger(L, CLAY_ATTACH_TO_NONE);               lua_setfield(L, -2, "ATTACH_TO_NONE");
    lua_pushinteger(L, CLAY_ATTACH_TO_PARENT);             lua_setfield(L, -2, "ATTACH_TO_PARENT");
    lua_pushinteger(L, CLAY_ATTACH_TO_ELEMENT_WITH_ID);    lua_setfield(L, -2, "ATTACH_TO_ELEMENT_WITH_ID");
    lua_pushinteger(L, CLAY_ATTACH_TO_ROOT);               lua_setfield(L, -2, "ATTACH_TO_ROOT");

    // Floating pointer capture mode
    lua_pushinteger(L, CLAY_POINTER_CAPTURE_MODE_CAPTURE);     lua_setfield(L, -2, "POINTER_CAPTURE_MODE_CAPTURE");
    lua_pushinteger(L, CLAY_POINTER_CAPTURE_MODE_PASSTHROUGH); lua_setfield(L, -2, "POINTER_CAPTURE_MODE_PASSTHROUGH");

    // Floating clipTo constants
    lua_pushinteger(L, CLAY_CLIP_TO_NONE);               lua_setfield(L, -2, "CLIP_TO_NONE");
    lua_pushinteger(L, CLAY_CLIP_TO_ATTACHED_PARENT);    lua_setfield(L, -2, "CLIP_TO_ATTACHED_PARENT");

	// Creates the metatables for render command parsing
	Clay_CreateCommandMetatable(L);

    return 1;
}

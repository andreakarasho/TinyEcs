local cache = {}
local clay = require("clay")
local glfw = require("ffi.ffi_glfw")

local page_selected = 1
local pages = { "Home", "Corner Radius", "Long List", "Text Alignment", "Floating Inventory", "UI Controls"}
local page_paths = {
	"demo/page_home.lua", 
	"demo/page_corner_radius.lua", 
	"demo/page_longlist.lua", 
	"demo/page_text_alignment.lua", 
	"demo/page_floating_inventory.lua",
	"demo/page_ui_controls.lua"
}

-- Mouse click edge detection (so clicks only fire once per press)
local prevMouseDown = false
local function mouseClicked(nowDown)
    local clicked = (not prevMouseDown) and nowDown
    prevMouseDown = nowDown
    return clicked
end

local function SidebarItemComponent(label, i, mouse_down)
    local sidebar_id = clay.id("SidebarItem", i)

    local active  = page_selected == i
    local hovered = clay.pointerOver(sidebar_id[1])

    local col = hovered and COLOR_SIDEBAR_BG_HOV or COLOR_SIDEBAR_BG_BASE
    if active then col = COLOR_SIDEBAR_BG_ACT end

    clay.createElement(sidebar_id, {
        layout = {
            sizing = { width = clay.sizingGrow(), height = clay.sizingFixed(44) },
            childAlignment = { y = clay.ALIGN_Y_CENTER },
            padding = clay.paddingAll(12)
        },
        backgroundColor = col,
        border = {
            color = COLOR_WHITE_24,
            width = { left=0, right=0, top=1, bottom=1 }
        }
    }, function()
        if clay.hovered() and mouseClicked(mouse_down) then
            page_selected = i
        end

        clay.createTextElement(label, { fontId=1, fontSize=16, textColor = COLOR_WHITE })
    end)
end

function layout(dt)
    glfw.GetCursorPos(device.win, mouse_x, mouse_y)
    local mx, my = mouse_x[0], mouse_y[0]
    local down = glfw.GetMouseButton(device.win, glfw.MOUSE_BUTTON_LEFT) == glfw.PRESS

    clay.setLayoutDimensions(device.width, device.height)
    clay.setPointerState(mx, my, down)
	
	-- Only feed the global scroll integrator when NOT driving via a scrollbar
	if device.scrolling_override ~= true then
		local dx, dy = -device.scroll.x, -device.scroll.y
		clay.updateScrollContainers(true, dx, dy, dt)
	end
				
    clay.beginLayout()

    clay.createElement(clay.id("Root"), {
        layout = {
            sizing = { width = clay.sizingFixed(device.width), height = clay.sizingFixed(device.height) },
            layoutDirection = clay.TOP_TO_BOTTOM,
        },
        backgroundColor = COLOR_ROOT_BG
    }, function()

        clay.createElement(clay.id("Header"), {
            layout = {
                sizing = { width = clay.sizingGrow(), height = clay.sizingFixed(48) },
                padding = clay.paddingAll(12)
            },
            backgroundColor = COLOR_HEADER_BG,
            border = {
                color = COLOR_WHITE_45,
                width = { left=0, right=0, top=0, bottom=1 }
            }
        }, function()
            clay.createTextElement(
                "Clay Demo:\t[color=#ffA0A0]LuaJIT 2.1[/color] + [color=#A0A0ff]BGFX[/color] + [color=#00ff00]GLFW[/color]",
                { fontId=1, fontSize=16, textColor=COLOR_WHITE }
            )
        end)

        clay.createElement(clay.id("MainRow"), {
            layout = {
                layoutDirection = clay.LEFT_TO_RIGHT,
                sizing = { width = clay.sizingGrow(), height = clay.sizingGrow() },
                childGap = 0
            }
        }, function()

            clay.createElement(clay.id("Sidebar"), {
                layout = {
                    layoutDirection = clay.TOP_TO_BOTTOM,
                    sizing = { width = clay.sizingFixed(220), height = clay.sizingGrow() },
                    padding = clay.paddingAll(12),
                    childGap = 8,
                },
                backgroundColor = COLOR_SIDEBAR_BG_BASE,
                border = {
                    color = COLOR_WHITE_32,
                    width = { left=0, right=1, top=0, bottom=0 }
                }
            }, function()
                for i, label in ipairs(pages) do
                    SidebarItemComponent(label, i, down)
                end
            end)

            clay.createElement(clay.id("Content"), {
                layout = {
                    sizing = { width = clay.sizingGrow(), height = clay.sizingGrow() },
                    padding = clay.paddingAll(8),
                    layoutDirection = clay.TOP_TO_BOTTOM
                },
                backgroundColor = COLOR_CONTENT_BG
            }, function()
                clay.createTextElement("Page: " .. pages[page_selected], {
                    fontId=1, fontSize=16, textColor = { r=240, g=240, b=255, a=255 }
                })

                clay.createElement(clay.id("Spacer"), {
                    layout = { sizing = { width = clay.sizingGrow(), height = clay.sizingFixed(2) } }
                })
                
                clay.createElement(clay.id("ScrollViewport"), {
                    layout = {
                        sizing = { width = clay.sizingFit(), height = clay.sizingFit() },
                        padding = clay.paddingLTRB(16, 16, 16, 0)
                    },
                    border = {
                        color = COLOR_WHITE_24,
                        width = { left=0, right=0, top=1, bottom=0 }
                    },
                    clip = { vertical = true, horizontal = false }
                }, function()
                    clay.createElement(clay.id("ScrollContent"), {
                        layout = {
                            sizing = { width = clay.sizingFit(), height = clay.sizingFit() },
                            layoutDirection = clay.TOP_TO_BOTTOM,
                            padding = clay.paddingAll(0),
                            childGap = 12
                        }
                    }, function()
                        local page_env = loadScript(page_paths[page_selected])
                        if page_env and page_env.layout then
							page_env.layout(dt)
						end
                    end)

					local viewport_id = clay.id("ScrollViewport")
					local content_id  = clay.id("ScrollContent")
					clay.scrollbar(viewport_id, content_id, device.scroll.y, nil)
                end)
            end)
        end)
    end)
end

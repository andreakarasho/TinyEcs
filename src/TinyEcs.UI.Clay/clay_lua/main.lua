local ffi = require("ffi")
local clay = require("clay")
local bgfx = require("ffi.ffi_bgfx")
local glfw = require("ffi.ffi_glfw")

local stbrt = require("ffi.ffi_stb_rect_pack")
local stbtt = require("ffi.ffi_stb_truetype")
local stbi = require("ffi.ffi_stb_image")

local window = require("window")
local font_manager = require("font_manager")

require("ffi.ffi_math")

ffi.cdef [[
	typedef struct {
		float x, y;
		float u, v;
		uint8_t r, g, b, a;
	} Vertex;
]]

demo = nil -- main layout

-- Device setup
device = {}
device.win = nil
device.width = 800
device.height = 600
device.view_id = 0 -- BGFX view ID
device.font = nil -- For stb_truetype font
device.shader = nil -- BGFX shader program
device.bgfx_init_s = ffi.new("bgfx_init_t[1]")
device.clay_memory = nil
device.clay_arena = nil
device.scroll = {x = 0, y = 0}

-- Color palette
COLOR_WHITE = {r = 255, g = 255, b = 255, a = 255}
COLOR_WHITE_24 = {r = 255, g = 255, b = 255, a = 24}
COLOR_WHITE_32 = {r = 255, g = 255, b = 255, a = 32}
COLOR_WHITE_45 = {r = 255, g = 255, b = 255, a = 45}

COLOR_HEADER_BG = {r = 35, g = 56, b = 90, a = 255}
COLOR_ROOT_BG = {r = 50, g = 80, b = 120, a = 255}
COLOR_CONTENT_BG = {r = 30, g = 46, b = 75, a = 255}

COLOR_SIDEBAR_BG_BASE = {r = 60, g = 78, b = 110, a = 255}
COLOR_SIDEBAR_BG_HOV = {r = 85, g = 105, b = 150, a = 255}
COLOR_SIDEBAR_BG_ACT = {r = 40, g = 60, b = 95, a = 255}

-- Shader uniforms
u_transform2D = nil
u_rcParams = nil
u_rcCorner = nil
u_rcBorder = nil
u_borderColor = nil
u_mode = nil

mouse_x = ffi.new("double[1]")
mouse_y = ffi.new("double[1]")

cache = {}
function loadScript(filename)
    if cache[filename] then
        return cache[filename]
    end

    local file = assert(io.open(filename, "rb"), "[WARNING] File not found: " .. filename)
    local str = file:read("*a")
    file:close()

    local chunk, err = assert(loadstring(str))
    if (chunk ~= nil) then
        local env = {}
        setmetatable(env, {__index = _G})
        setfenv(chunk, env)

        local status, result = pcall(chunk)

        if (status) then
            print("Loaded script: " .. filename)
        else
            print(result, debug.traceback(1))
        end

        cache[filename] = env

        return env
    else
        print(err, debug.traceback())
    end
end

local function create_shader_from_file(filename)
    local file = io.open(filename, "rb")

    assert(file, filename .. "not found")

    local source = file:read("*a")
    file:close()

    local mem = bgfx.bgfx_copy(ffi.cast("char *", source), #source)
    local shader = bgfx.bgfx_create_shader(mem)
    if (shader == nil) then
        print("failed to create shader " .. filename)
        return
    end

    return shader
end

-- Load BGFX shader
local function load_shader(vs_path, fs_path)
    local vs = create_shader_from_file(vs_path)
    local fs = create_shader_from_file(fs_path)
    if vs == nil or fs == nil then
        error("Shader load failed")
    end
    local program = bgfx.bgfx_create_program(vs, fs, true)
    return program
end

-- Helper to generate vertices for a rectangle
local function generateRectangleVertices(x, y, width, height, r, g, b, a)
    local r = math.max(0, math.min(255, r))
    local g = math.max(0, math.min(255, g))
    local b = math.max(0, math.min(255, b))
    local a = math.max(0, math.min(255, a))

    local vertices = ffi.new("Vertex[4]")
    vertices[0] = {x, y, 0, 0, r, g, b, a}
    vertices[1] = {x + width, y, 1, 0, r, g, b, a}
    vertices[2] = {x + width, y + height, 1, 1, r, g, b, a}
    vertices[3] = {x, y + height, 0, 1, r, g, b, a}
    local indices = ffi.new("uint16_t[6]", {0, 1, 2, 0, 2, 3})
    return vertices, indices
end

local function init_bgfx()
    local bgfx_init_s = device.bgfx_init_s
    bgfx.bgfx_init_ctor(bgfx_init_s)
    bgfx_init_s[0].type = bgfx.BGFX_RENDERER_TYPE_COUNT
    bgfx_init_s[0].vendorId = bgfx.BGFX_PCI_ID_NONE
    bgfx_init_s[0].deviceId = 0
    bgfx_init_s[0].debug = false
    bgfx_init_s[0].profile = false
    bgfx_init_s[0].resolution.width = device.width
    bgfx_init_s[0].resolution.height = device.height
    bgfx_init_s[0].resolution.reset = bgfx.BGFX_RESET_VSYNC
    bgfx_init_s[0].resolution.format = bgfx.BGFX_TEXTURE_FORMAT_RGBA8

    -- Retrieve platform data
    if (ffi.os == "Windows") then
        local nwh = ffi.cast("void*", glfw.GetWin32Window(device.win))
        bgfx_init_s[0].platformData.nwh = nwh
    elseif (ffi.os == "OSX") then
        local nwh = ffi.cast("void*", glfw.GetCocoaWindow(device.win))
        bgfx_init_s[0].platformData.nwh = nwh
    elseif (ffi.os == "Linux") then
        local nwh = ffi.cast("void *", glfw.GetX11Window(device.win))
        local ndt = ffi.cast("void *", glfw.GetX11Display())
        bgfx_init_s[0].platformData.nwh = nwh
        bgfx_init_s[0].platformData.ndt = ndt
    else
        error("Unsupported platform: " .. ffi.os)
    end

    -- Initialize bgfx
    bgfx.bgfx_init(bgfx_init_s[0])

    --bgfx.bgfx_set_debug(bgfx.BGFX_DEBUG_TEXT)

    local renderer = bgfx.bgfx_get_renderer_name(bgfx.bgfx_get_renderer_type())
    print("renderer:", renderer ~= nil and ffi.string(renderer) or "unknown")

    local video_flags = 0
    bgfx.bgfx_reset(device.width, device.height, video_flags, bgfx_init_s[0].resolution.format)
    bgfx.bgfx_set_view_rect(device.view_id, 0, 0, device.width, device.height)
    bgfx.bgfx_set_view_clear(device.view_id, bgfx.BGFX_CLEAR_COLOR + bgfx.BGFX_CLEAR_DEPTH, 0x336666ff, 1.0, 0)
end

-- Initialization
local function initialize()
    device.win = window.create({0, 0, device.width, device.height, false})
    device.width, device.height = window.framebufferSize()

    init_bgfx()

    -- Initialize Clay with default memory
    local minMemory = clay.minMemorySize()
    local ctx, mem = clay.initialize(minMemory, device.width, device.height)
    if ctx == nil then
        error("Failed to initialize Clay")
    end

    clay.setMeasureTextFunction(font_manager.measureText)

    device.shader = load_shader("shader/clay.vs.bin", "shader/clay.fs.bin")

    device.vdecl = ffi.new("bgfx_vertex_layout_t[1]")
    bgfx.bgfx_vertex_layout_begin(device.vdecl, bgfx.bgfx_get_renderer_type())
    bgfx.bgfx_vertex_layout_add(device.vdecl, bgfx.BGFX_ATTRIB_POSITION, 2, bgfx.BGFX_ATTRIB_TYPE_FLOAT, false, false)
    bgfx.bgfx_vertex_layout_add(device.vdecl, bgfx.BGFX_ATTRIB_TEXCOORD0, 2, bgfx.BGFX_ATTRIB_TYPE_FLOAT, false, false)
    bgfx.bgfx_vertex_layout_add(device.vdecl, bgfx.BGFX_ATTRIB_COLOR0, 4, bgfx.BGFX_ATTRIB_TYPE_UINT8, true, false)
    bgfx.bgfx_vertex_layout_end(device.vdecl)
    device.vdecl_h = bgfx.bgfx_create_vertex_layout(device.vdecl)

    device.view = ffi.new("mat4_t"):identity()
    device.projection =
        ffi.new("mat4_t"):from_ortho(
        0,
        device.width,
        device.height,
        0,
        0,
        100,
        0,
        bgfx.bgfx_get_caps().homogeneousDepth,
        false
    )

    -- Create buffers
    local stride = ffi.sizeof("Vertex")
    device.maxVertexCount = 65536
    device.maxVertexBufferSize = stride * device.maxVertexCount
    device.maxElementCount = device.maxVertexCount * 2
    device.maxElementBufferSize = device.maxElementCount * ffi.sizeof("uint16_t")

    if not device.white_tex then
        local white = ffi.new("uint32_t[1]", 0xffffffff)
        local mem = bgfx.bgfx_copy(white, 4)
        device.white_tex = bgfx.bgfx_create_texture_2d(1, 1, false, 1, bgfx.BGFX_TEXTURE_FORMAT_RGBA8, 0, mem)
        device.s_texColor = bgfx.bgfx_create_uniform("s_texColor", bgfx.BGFX_UNIFORM_TYPE_SAMPLER, 1)
    end

    -- Create shader uniform handles
    u_transform2D = bgfx.bgfx_create_uniform("u_transform2D", bgfx.BGFX_UNIFORM_TYPE_VEC4, 1)
    u_rcParams = bgfx.bgfx_create_uniform("u_rcParams", bgfx.BGFX_UNIFORM_TYPE_VEC4, 1)
    u_rcCorner = bgfx.bgfx_create_uniform("u_rcCorner", bgfx.BGFX_UNIFORM_TYPE_VEC4, 1)
    u_rcBorder = bgfx.bgfx_create_uniform("u_rcBorder", bgfx.BGFX_UNIFORM_TYPE_VEC4, 1)
    u_borderColor = bgfx.bgfx_create_uniform("u_borderColor", bgfx.BGFX_UNIFORM_TYPE_VEC4, 1)
    u_mode = bgfx.bgfx_create_uniform("u_mode", bgfx.BGFX_UNIFORM_TYPE_VEC4, 1)

    -- Set clay debug
    --clay.setDebugModeEnabled(true)

    -- Set callbacks
    window.callback_register(
        "mouse_scroll",
        function(delta_x, delta_y, x, y)
            device.scroll.x = y
            device.scroll.y = y
        end
    )

    window.callback_register(
        "window_framebuffer_size",
        function(w, h)
            if w == 0 or h == 0 then
                return
            end -- ignore minimized window
            device.width = w
            device.height = h

            -- Reset BGFX to new size
            local format = bgfx.BGFX_TEXTURE_FORMAT_RGBA8
            bgfx.bgfx_reset(w, h, bgfx.BGFX_RESET_VSYNC, format)
            bgfx.bgfx_set_view_rect(device.view_id, 0, 0, w, h)

            -- Update Clay and projection matrix
            clay.setLayoutDimensions(w, h)
            device.projection =
                ffi.new("mat4_t"):from_ortho(0, w, h, 0, 0, 100, 0, bgfx.bgfx_get_caps().homogeneousDepth, false)
        end
    )

    demo = loadScript("demo/body.lua")

    -- UI component extensions
    clay.scrollbar = loadScript("component/scrollbar.lua").scrollbar
    clay.checkbox = loadScript("component/checkbox.lua").checkbox
    clay.radio = loadScript("component/radio.lua").radio
    clay.edit = loadScript("component/edit.lua").edit
    clay.slider = loadScript("component/slider.lua").slider
    clay.property = loadScript("component/property.lua").property
end

local clipStack = {}

local function pushScissor(x, y, w, h)
    -- TODO: Clamp to view size if needed.
    x = math.max(0, math.floor(x or 0))
    y = math.max(0, math.floor(y or 0))
    w = math.max(0, math.floor(w or 0))
    h = math.max(0, math.floor(h or 0))
    table.insert(clipStack, {x = x, y = y, w = w, h = h})
    bgfx.bgfx_set_scissor(x, y, w, h)
end

local function popScissor()
    clipStack[#clipStack] = nil
    local top = clipStack[#clipStack]
    if top then
        bgfx.bgfx_set_scissor(top.x, top.y, top.w, top.h)
    else
        -- disable scissor
        bgfx.bgfx_set_scissor(0, 0, 0, 0)
    end
end

local function applyTopScissor()
    local top = clipStack[#clipStack]
    if top then
        bgfx.bgfx_set_scissor(top.x, top.y, top.w, top.h)
    end
end

-- Main loop
local function mainLoop()
    local lastTime = window.time()
    while not window.shouldClose() do
        local currentTime = glfw.GetTime()
        local dt = currentTime - lastTime
        lastTime = currentTime

        window.update(dt)

        demo.layout(dt)
        device.scroll.x = 0
        device.scroll.y = 0

        -- Sets view and projection matrix for view_id
        bgfx.bgfx_set_view_transform(device.view_id, device.view, device.projection)

        bgfx.bgfx_touch(device.view_id)

        local vertexStride = ffi.sizeof("Vertex")

        for cmd in clay.endLayoutIter() do
            local t = cmd:type()

            -- Safe defaults before any bgfx_submit()
            bgfx.bgfx_set_uniform(u_rcBorder, ffi.new("float[4]", 0, 0, 0, 0), 1)
            bgfx.bgfx_set_uniform(u_borderColor, ffi.new("float[4]", 1, 1, 1, 1), 1)
            bgfx.bgfx_set_uniform(u_rcCorner, ffi.new("float[4]", 0, 0, 0, 0), 1)
            bgfx.bgfx_set_uniform(u_rcParams, ffi.new("float[4]", 0, 0, 0, 0), 1)

            if t == clay.RENDER_RECTANGLE then
                local x, y, w, h = cmd:bounds()
                local r, g, b, a = cmd:color()
                local tl, tr, bl, br = cmd:cornerRadius()

                local vertices, indices = generateRectangleVertices(x, y, w, h, r, g, b, a)
                local vertCount, idxCount = 4, 6

                local tvb = ffi.new("bgfx_transient_vertex_buffer_t[1]")
                local tib = ffi.new("bgfx_transient_index_buffer_t[1]")
                tib[0].isIndex16 = true

                if bgfx.bgfx_get_avail_transient_vertex_buffer(vertCount, device.vdecl) < vertCount then
                    print("Warning: Not enough transient buffer space")
                else
                    bgfx.bgfx_alloc_transient_vertex_buffer(tvb, vertCount, device.vdecl)
                    bgfx.bgfx_alloc_transient_index_buffer(tib, idxCount, false)

                    ffi.copy(tvb[0].data, vertices, vertexStride * vertCount)
                    ffi.copy(tib[0].data, indices, ffi.sizeof("uint16_t") * idxCount)

                    bgfx.bgfx_set_transient_vertex_buffer(0, tvb, 0, vertCount)
                    bgfx.bgfx_set_transient_index_buffer(tib, 0, idxCount)
                    bgfx.bgfx_set_texture(0, device.s_texColor, device.white_tex, 0xffffffff)
                    bgfx.bgfx_set_uniform(u_transform2D, ffi.new("float[4]", 1, 1, 0, 0), 1)

                    local feather = 1.25
                    local invW = (w > 0) and (1.0 / w) or 0.0
                    local invH = (h > 0) and (1.0 / h) or 0.0

                    bgfx.bgfx_set_uniform(u_rcBorder, ffi.new("float[4]", 0, 0, 0, 0), 1)
                    bgfx.bgfx_set_uniform(u_borderColor, ffi.new("float[4]", r / 255, g / 255, b / 255, a / 255), 1)
                    bgfx.bgfx_set_uniform(u_rcCorner, ffi.new("float[4]", tl, tr, br, bl), 1)
                    bgfx.bgfx_set_uniform(u_rcParams, ffi.new("float[4]", feather, invW, invH, 0.0), 1)

                    local state =
                        bit.bor(
                        bgfx.BGFX_STATE_WRITE_RGB,
                        bgfx.BGFX_STATE_WRITE_A,
                        bgfx.BGFX_STATE_BLEND_ALPHA,
                        bgfx.BGFX_STATE_MSAA
                    )
                    bgfx.bgfx_set_state(state, 0)
                    applyTopScissor()
                    bgfx.bgfx_submit(device.view_id, device.shader, 0, bgfx.BGFX_DISCARD_ALL)
                end
            elseif t == clay.RENDER_BORDER then
                local x, y, w, h = cmd:bounds()
                local r, g, b, a = cmd:color()

                local left, right, top, bottom = cmd:borderWidth()
                local tl, tr, bl, br = cmd:cornerRadius()

                local hasRounded = (tl > 0) or (tr > 0) or (bl > 0) or (br > 0)
                local feather = 1.25
                local invW = (w > 0) and (1.0 / w) or 0.0
                local invH = (h > 0) and (1.0 / h) or 0.0

                local vertices, indices = generateRectangleVertices(x, y, w, h, r, g, b, a)
                local vertCount, idxCount = 4, 6

                local tvb = ffi.new("bgfx_transient_vertex_buffer_t[1]")
                local tib = ffi.new("bgfx_transient_index_buffer_t[1]")
                tib[0].isIndex16 = true

                if bgfx.bgfx_get_avail_transient_vertex_buffer(vertCount, device.vdecl) < vertCount then
                    print("Warning: Not enough transient buffer space")
                else
                    bgfx.bgfx_alloc_transient_vertex_buffer(tvb, vertCount, device.vdecl)
                    bgfx.bgfx_alloc_transient_index_buffer(tib, idxCount, false)

                    ffi.copy(tvb[0].data, vertices, vertexStride * vertCount)
                    ffi.copy(tib[0].data, indices, ffi.sizeof("uint16_t") * idxCount)

                    bgfx.bgfx_set_transient_vertex_buffer(0, tvb, 0, vertCount)
                    bgfx.bgfx_set_transient_index_buffer(tib, 0, idxCount)
                    bgfx.bgfx_set_texture(0, device.s_texColor, device.white_tex, 0xffffffff)

                    bgfx.bgfx_set_uniform(u_rcCorner, ffi.new("float[4]", tl, tr, br, bl), 1)
                    bgfx.bgfx_set_uniform(u_rcBorder, ffi.new("float[4]", left, right, top, bottom), 1)
                    bgfx.bgfx_set_uniform(u_borderColor, ffi.new("float[4]", r / 255, g / 255, b / 255, a / 255), 1)

                    local modeVal = hasRounded and 1.0 or 2.0
                    bgfx.bgfx_set_uniform(u_rcParams, ffi.new("float[4]", feather, invW, invH, modeVal), 1)

                    local state =
                        bit.bor(
                        bgfx.BGFX_STATE_WRITE_RGB,
                        bgfx.BGFX_STATE_WRITE_A,
                        bgfx.BGFX_STATE_BLEND_ALPHA,
                        bgfx.BGFX_STATE_MSAA
                    )
                    bgfx.bgfx_set_state(state, 0)
                    applyTopScissor()
                    bgfx.bgfx_submit(device.view_id, device.shader, 0, bgfx.BGFX_DISCARD_ALL)
                end
            elseif t == clay.RENDER_TEXT then
                local text, fontId, fontSize, letterSpacing, lineHeight = cmd:text()
                local r, g, b, a = cmd:color()
                local x, y = cmd:bounds()

                local font = font_manager.load(fontId, fontSize)
                if text and #text > 0 and font then
                    local vertices, indices, vertCount, idxCount =
                        font_manager.generateTextVertices(font, text, x, y, r, g, b, a)

                    local tvb = ffi.new("bgfx_transient_vertex_buffer_t[1]")
                    local tib = ffi.new("bgfx_transient_index_buffer_t[1]")
                    tib[0].isIndex16 = true

                    if bgfx.bgfx_get_avail_transient_vertex_buffer(vertCount, device.vdecl) < vertCount then
                        print("Warning: Not enough transient buffer space")
                    else
                        bgfx.bgfx_alloc_transient_vertex_buffer(tvb, vertCount, device.vdecl)
                        bgfx.bgfx_alloc_transient_index_buffer(tib, idxCount, false)

                        ffi.copy(tvb[0].data, vertices, vertexStride * vertCount)
                        ffi.copy(tib[0].data, indices, ffi.sizeof("uint16_t") * idxCount)

                        bgfx.bgfx_set_transient_vertex_buffer(0, tvb, 0, vertCount)
                        bgfx.bgfx_set_transient_index_buffer(tib, 0, idxCount)
                        bgfx.bgfx_set_texture(0, device.s_texColor, font.texture, 0xffffffff)
                        bgfx.bgfx_set_uniform(u_transform2D, ffi.new("float[4]", 1, 1, 0, 0), 1)

                        local state =
                            bit.bor(
                            bgfx.BGFX_STATE_WRITE_RGB,
                            bgfx.BGFX_STATE_WRITE_A,
                            bgfx.BGFX_STATE_MSAA,
                            bgfx.BGFX_STATE_BLEND_FUNC_SEPARATE(
                                bgfx.BGFX_STATE_BLEND_ONE,
                                bgfx.BGFX_STATE_BLEND_INV_SRC_ALPHA,
                                bgfx.BGFX_STATE_BLEND_ONE,
                                bgfx.BGFX_STATE_BLEND_INV_SRC_ALPHA
                            )
                        )
                        bgfx.bgfx_set_state(state, 0)
                        applyTopScissor()
                        bgfx.bgfx_submit(device.view_id, device.shader, 0, bgfx.BGFX_DISCARD_ALL)
                    end
                end
            elseif t == clay.RENDER_IMAGE then
                local r, g, b, a = cmd:color()
                local texPtr = cmd:imageData()
                local tex = ffi.cast("bgfx_texture_handle_t*", texPtr)
                bgfx.bgfx_set_texture(0, device.s_texColor, tex[0], 0xffffffff)

                local x, y, w, h = cmd:bounds()
                local vertices, indices = generateRectangleVertices(x, y, w, h, r, g, b, a)
                local vertCount = 4
                local idxCount = 6

                local tvb = ffi.new("bgfx_transient_vertex_buffer_t[1]")
                local tib = ffi.new("bgfx_transient_index_buffer_t[1]")
                tib[0].isIndex16 = true

                if bgfx.bgfx_get_avail_transient_vertex_buffer(vertCount, device.vdecl) < vertCount then
                    print("Warning: Not enough transient buffer space")
                else
                    bgfx.bgfx_alloc_transient_vertex_buffer(tvb, vertCount, device.vdecl)
                    bgfx.bgfx_alloc_transient_index_buffer(tib, idxCount, false)

                    ffi.copy(tvb[0].data, vertices, vertexStride * vertCount)
                    ffi.copy(tib[0].data, indices, ffi.sizeof("uint16_t") * idxCount)

                    bgfx.bgfx_set_transient_vertex_buffer(0, tvb, 0, vertCount)
                    bgfx.bgfx_set_transient_index_buffer(tib, 0, idxCount)
                    bgfx.bgfx_set_state(bgfx.BGFX_STATE_WRITE_RGB + bgfx.BGFX_STATE_BLEND_ALPHA, 0)
                    applyTopScissor()
                    bgfx.bgfx_submit(device.view_id, device.shader, 0, bgfx.BGFX_DISCARD_ALL)
                end
            elseif t == clay.RENDER_SCISSOR_START then
                local x, y, w, h = cmd:bounds()
                local clipH, clipV = cmd:clip()
                if not clipH then
                    x, w = 0, device.width
                end
                if not clipV then
                    y, h = 0, device.height
                end
                pushScissor(x, y, w, h)
            elseif t == clay.RENDER_SCISSOR_END then
                popScissor()
            elseif t == clay.RENDER_CUSTOM then
            end
        end

        bgfx.bgfx_frame(false)
    end
end

-- Cleanup
local function cleanup()
    window.destroy()
    bgfx.bgfx_shutdown()
    clay.shutdown()
end

initialize()
mainLoop()
cleanup()

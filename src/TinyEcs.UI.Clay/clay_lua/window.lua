local ffi = require("ffi")
local glfw = require("ffi.ffi_glfw")
local _window = nil

local scroll_delta_x, scroll_delta_y = 0, 0 -- frame offset deltas

local M = {}

function M.create(config)
    glfw.SetErrorCallback(
        function(err, desc)
            print(ffi.string(desc))
        end
    )

    if (glfw.Init() == 0) then
        error("glfw.Init() failed")
    end

    -- Configuration
    local x = config and tonumber(config.x) or 0
    local y = config and tonumber(config.y) or 0
    local width = config and tonumber(config.width) or 800
    local height = config and tonumber(config.height) or 600
    local fullscreen = config and config.fullscreen == true or false

    local monitor = glfw.GetPrimaryMonitor()
    local mode = glfw.GetVideoMode(monitor)

    glfw.WindowHint(glfw.CLIENT_API, glfw.NO_API)
    glfw.WindowHint(glfw.DOUBLEBUFFER, 1)
    glfw.WindowHint(glfw.RED_BITS, mode.redBits)
    glfw.WindowHint(glfw.GREEN_BITS, mode.greenBits)
    glfw.WindowHint(glfw.BLUE_BITS, mode.blueBits)
    glfw.WindowHint(glfw.ALPHA_BITS, glfw.DONT_CARE)
    glfw.WindowHint(glfw.DEPTH_BITS, glfw.DONT_CARE)
    glfw.WindowHint(glfw.STENCIL_BITS, 8)
    glfw.WindowHint(glfw.REFRESH_RATE, mode.refreshRate)

    if (ffi.os == "OSX") then
        glfw.WindowHint(glfw.CONTEXT_VERSION_MAJOR, 3)
        glfw.WindowHint(glfw.CONTEXT_VERSION_MINOR, 2)
        glfw.WindowHint(glfw.OPENGL_FORWARD_COMPAT, 1)
        glfw.WindowHint(glfw.OPENGL_PROFILE, glfw.OPENGL_CORE_PROFILE)
    end

    local wnd = nil
    if (fullscreen) then
        wnd = glfw.CreateWindow(width, height, "", monitor, nil)
    else
        wnd = glfw.CreateWindow(width, height, "", nil, nil)
        glfw.SetWindowPos(wnd, (mode.width - width) / 2, (mode.height - height) / 2)
    end

    if (wnd == 0) then
        glfw.Terminate()
        error("Glfw window creation failed")
    end

    _window = wnd

    M.callback_send("window_creation", wnd, width, height) -- If using OpenGL remember to call glfw.MakeContextCurrent(wnd) here

    glfw.PollEvents() -- crash fix for OSX

    -- helper for creating and registering callbacks safely
    M.__callbacks = {}
    local function make_callback(ctype, func)
        local cb = ffi.cast(ctype, func)
        table.insert(M.__callbacks, cb) -- prevent GC
        return cb
    end

    glfw.SetWindowPosCallback(
        wnd,
        make_callback(
            "GLFWwindowposfun",
            function(_, x, y)
                M.callback_send("window_position", x, y)
            end
        )
    )

    glfw.SetWindowSizeCallback(
        wnd,
        make_callback(
            "GLFWwindowsizefun",
            function(_, w, h)
                M.callback_send("window_size", w, h)
            end
        )
    )

    glfw.SetWindowCloseCallback(
        wnd,
        make_callback(
            "GLFWwindowclosefun",
            function(_)
                M.callback_send("window_close")
            end
        )
    )

    glfw.SetWindowRefreshCallback(
        wnd,
        make_callback(
            "GLFWwindowrefreshfun",
            function(_)
                M.callback_send("window_refresh")
            end
        )
    )

    glfw.SetWindowFocusCallback(
        wnd,
        make_callback(
            "GLFWwindowfocusfun",
            function(_, focused)
                M.callback_send("window_focus", focused)
            end
        )
    )

    glfw.SetWindowIconifyCallback(
        wnd,
        make_callback(
            "GLFWwindowiconifyfun",
            function(_, iconified)
                M.callback_send("window_iconify", iconified)
            end
        )
    )

    glfw.SetWindowMaximizeCallback(
        wnd,
        make_callback(
            "GLFWwindowmaximizefun",
            function(_, maximized)
                M.callback_send("window_maximized", maximized)
            end
        )
    )

    glfw.SetFramebufferSizeCallback(
        wnd,
        make_callback(
            "GLFWframebuffersizefun",
            function(_, w, h)
                M.callback_send("window_framebuffer_size", w, h)
            end
        )
    )

    glfw.SetWindowContentScaleCallback(
        wnd,
        make_callback(
            "GLFWwindowcontentscalefun",
            function(_, sx, sy)
                M.callback_send("window_content_scale", sx, sy)
            end
        )
    )

    glfw.SetKeyCallback(
        wnd,
        make_callback(
            "GLFWkeyfun",
            function(_, key, sc, action, mods)
                M.callback_send("window_set_key", key, sc, action, mods) -- https://www.glfw.org/docs/latest/group__keys.html
            end
        )
    )

    glfw.SetCharCallback(
        wnd,
        make_callback(
            "GLFWcharfun",
            function(_, codepoint)
                M.callback_send("window_set_char", codepoint)
            end
        )
    )

    glfw.SetCharModsCallback(
        wnd,
        make_callback(
            "GLFWcharmodsfun",
            function(_, codepoint, mods)
                M.callback_send("window_set_char_mods", codepoint, mods)
            end
        )
    )

    glfw.SetMouseButtonCallback(
        wnd,
        make_callback(
            "GLFWmousebuttonfun",
            function(_, button, action, mods)
                M.callback_send("mouse_button", button, action, mods)
            end
        )
    )

    glfw.SetCursorPosCallback(
        wnd,
        make_callback(
            "GLFWcursorposfun",
            function(_, x, y)
                M.callback_send("mouse_position", x, y)
            end
        )
    )

    glfw.SetCursorEnterCallback(
        wnd,
        make_callback(
            "GLFWcursorenterfun",
            function(_, entered)
                M.callback_send("mouse_enter", entered)
            end
        )
    )

    glfw.SetScrollCallback(
        wnd,
        make_callback(
            "GLFWscrollfun",
            function(_, xoff, yoff)
                scroll_delta_x = scroll_delta_x + xoff
                scroll_delta_y = scroll_delta_y + yoff
                M.callback_send("mouse_scroll", scroll_delta_x, scroll_delta_y, xoff, yoff)
            end
        )
    )

    glfw.SetDropCallback(
        wnd,
        make_callback(
            "GLFWdropfun",
            function(_, count, paths)
                local t = {}
                for i = 0, count - 1 do
                    table.insert(t, ffi.string(paths[i]))
                end
                M.callback_send("file_drop", t, count)
            end
        )
    )

    return wnd
end

function M.update(dt)
    glfw.PollEvents()
    M.callback_send("window_update", _window, dt)

    scroll_delta_x = 0
    scroll_delta_y = 0
end

-- Get or Set window title
function M.title(str)
    if (str) then
        glfw.SetWindowTitle(_window, str)
        return str
    end
    return glfw.GetWindowTitle(_window)
end

-- Get or Set window width and height
function M.size(w, h)
    local _w, _h = ffi.new("int[1]"), ffi.new("int[1]")
    glfw.GetWindowSize(_window, _w, _h)
    if (w or h) then
        w = w or _w[0]
        h = h or _h[0]
        glfw.SetWindowSize(_window, w, h)
        return w, h
    end
    return _w[0], _h[0]
end

-- Get or Set window position
function M.position(x, y)
    local _x, _y = glfw.GetWindowPos(_window)
    if (x or y) then
        x = x or _x
        y = y or _y
        glfw.SetWindowPos(_window, x, y)
        return x, y
    end
    return _x, _y
end

-- Get framebuffer width and height
function M.framebufferSize()
    local width = ffi.new("int[1]")
    local height = ffi.new("int[1]")
    glfw.GetFramebufferSize(_window, width, height)
    return width[0], height[0]
end

function M.show()
    glfw.ShowWindow(_window)
    M.callback_send("window_show", _window)
end

function M.hide()
    M.callback_send("window_hide", _window)
    glfw.HideWindow(_window)
end

function M.time()
    return glfw.GetTime()
end

function M.shouldClose()
    return glfw.WindowShouldClose(_window) == glfw.TRUE
end

function M.destroy()
    if (_window ~= nil) then
        M.callback_send("window_destroy", _window)
        glfw.DestroyWindow(_window)
        glfw.Terminate()
        _window = nil
    end
end

M.callbacks = {}
-- Create a callback that will execute lowest priority order first
function M.callback_register(name, func_or_userdata, prior)
    if not (M.callbacks) then
        return -- intentionally removed all callbacks
    end
    assert(func_or_userdata ~= nil)
    if not (M.callbacks[name]) then
        M.callbacks[name] = {}
    end

    if (type(func_or_userdata) == "function") then
        M.callbacks[name][func_or_userdata] = {["functor"] = function(...)
                func_or_userdata(...)
            end, ["prior"] = prior or 0}
    elseif (func_or_userdata[name]) then
        M.callbacks[name][func_or_userdata] = {["functor"] = function(...)
                func_or_userdata[name](func_or_userdata, ...)
            end, ["prior"] = prior or 0}
    end
end

function M.callback_remove(name, func_or_userdata)
    if (M.callbacks and M.callbacks[name]) then
        M.callbacks[name][func_or_userdata] = nil
    end
end

local function order(t, a, b)
    return t[a].prior > t[b].prior
end

local function spairs(t, order)
    -- collect the keys
    local keys = {}
    for k in pairs(t) do
        keys[#keys + 1] = k
    end

    -- if order function given, sort by it by passing the table and keys a, b,
    -- otherwise just sort the keys
    if order then
        table.sort(
            keys,
            function(a, b)
                return order(t, a, b)
            end
        )
    else
        table.sort(keys)
    end

    -- return the iterator function
    local i = 0
    return function()
        i = i + 1
        if keys[i] then
            return keys[i], t[keys[i]]
        end
    end
end

-- Execute all registered functors for a specific event name
function M.callback_send(name, ...)
    if (M.callbacks and M.callbacks[name]) then
        for func_or_userdata, v in spairs(M.callbacks[name], order) do
            if (v.functor) then
                v.functor(...)
            end
        end
    end
end

return M

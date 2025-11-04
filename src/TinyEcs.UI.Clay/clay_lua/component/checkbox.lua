local clay = require("clay")
local glfw = require("ffi.ffi_glfw")

_cb_state = _cb_state or {}

-- checkbox(id, label, defaultChecked, opts)
function checkbox(id, label, defaultChecked, opts)
    -- input (read once here)
    glfw.GetCursorPos(device.win, mouse_x, mouse_y)
    local mx, my = mouse_x[0], mouse_y[0]
    local down = glfw.GetMouseButton(device.win, glfw.MOUSE_BUTTON_LEFT) == glfw.PRESS

    -- config
    local size     = (opts and opts.size)     or 18
    local gap      = (opts and opts.gap)      or 8
    local fontId   = (opts and opts.fontId)   or 1
    local fontSize = (opts and opts.fontSize) or 16
    local disabled = (opts and opts.disabled) or false

    -- colors
    local c = (opts and opts.colors) or {}
    local c_box_base = (c.box and c.box.base) or { r=40,  g=40,  b=40,  a=160 }
    local c_box_hov  = (c.box and c.box.hov ) or { r=60,  g=60,  b=60,  a=200 }
    local c_box_act  = (c.box and c.box.act ) or { r=80,  g=80,  b=80,  a=220 }
    local c_box_dis  = (c.box and c.box.dis ) or { r=30,  g=30,  b=30,  a=100 }
    local c_border   = c.border               or { r=255, g=255, b=255, a=64  }
    local c_fill     = c.fill                 or { r=120, g=190, b=255, a=220 }
    local c_label    = c.label                or { r=230, g=230, b=240, a=255 }

    -- per-id state (value persists across frames)
    local key = id[1]
    local st = _cb_state[key]
    if not st then
        st = { downLast=false, pressed=false, value = not not defaultChecked }
        _cb_state[key] = st
    end

    -- hover (pointerOver is last-frame; that's fine)
    local hovered = (not disabled) and clay.pointerOver(id[1]) or false

    -- press / release
    local toggled = false
    if (not st.downLast) and down and hovered and (not disabled) then
        st.pressed = true
    elseif st.pressed and (not down) and st.downLast then
        -- toggle on release (standard checkbox UX; doesn't require still-hovering)
        st.value = not st.value
        toggled = true
        st.pressed = false
    elseif not down then
        st.pressed = false
    end
    st.downLast = down

    -- visuals
    local boxCol = disabled and c_box_dis or (st.pressed and c_box_act or (hovered and c_box_hov or c_box_base))

    clay.createElement(id, {
        layout = {
            layoutDirection = clay.LEFT_TO_RIGHT,
            childAlignment = { x = clay.ALIGN_X_LEFT, y = clay.ALIGN_Y_CENTER },
            sizing = { width = clay.sizingFit(), height = clay.sizingFixed(size) },
            childGap = gap,
        },
    }, function()
        local box_id = clay.id("cb-box", key)
        clay.createElement(box_id, {
            layout = { 
				sizing = { width = clay.sizingFixed(size), height = clay.sizingFixed(size) },  
				childAlignment = { x = clay.ALIGN_X_CENTER, y = clay.ALIGN_Y_CENTER } 
            },
            backgroundColor = boxCol,
            border = { color = c_border, width = { left=1, right=1, top=1, bottom=1 } },
        }, function()
            if st.value then
                local pad = math.max(3, math.floor(size * 0.2))
                clay.createElement(clay.id("cb-fill", key), {
                    layout = {
                        sizing = {
                            width  = clay.sizingFixed(size - pad*2),
                            height = clay.sizingFixed(size - pad*2)
                        },
                        margin = { left = pad, right = 0, top = pad, bottom = 0 },
                    },
                    backgroundColor = c_fill,
                    border = { color = { r=255, g=255, b=255, a=90 }, width = { left=1, right=1, top=1, bottom=1 } },
                })
            end
        end)

        if label ~= nil then
            clay.createTextElement(label, { fontId=fontId, fontSize=fontSize, textColor=c_label })
        end
    end)

    return st.value, toggled
end

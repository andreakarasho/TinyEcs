local clay = require("clay")
local glfw = require("ffi.ffi_glfw")

_radio_group = _radio_group or {} -- per-group selected value
_radio_btn = _radio_btn or {} -- per-button press state

-- radio(id, label, groupKey, myValue, defaultSelected, opts)
-- opts (all optional; read-only):
--   opts.size (default 18), opts.gap (8), opts.fontId (1), opts.fontSize (16), opts.disabled (false)
--   opts.colors: { ringBase, ringHov, ringAct, ringDis, border, dot, label }
--   opts.allowNone: true to allow deselecting all by clicking selected (off by default)
function radio(id, label, groupKey, myValue, defaultSelected, opts)
    -- input
    glfw.GetCursorPos(device.win, mouse_x, mouse_y)
    local mx, my = mouse_x[0], mouse_y[0]
    local down = glfw.GetMouseButton(device.win, glfw.MOUSE_BUTTON_LEFT) == glfw.PRESS

    -- config
    local size = (opts and opts.size) or 20
    local gap = (opts and opts.gap) or 8
    local fontId = (opts and opts.fontId) or 1
    local fontSize = (opts and opts.fontSize) or 16
    local disabled = (opts and opts.disabled) or false
    local allowNone = (opts and opts.allowNone) or false

    -- colors
    local col = opts and opts.colors
    local ringBase = (col and col.ringBase) or {r = 40, g = 40, b = 40, a = 160}
    local ringHov = (col and col.ringHov) or {r = 60, g = 60, b = 60, a = 200}
    local ringAct = (col and col.ringAct) or {r = 80, g = 80, b = 80, a = 220}
    local ringDis = (col and col.ringDis) or {r = 30, g = 30, b = 30, a = 100}
    local border = (col and col.border) or {r = 255, g = 255, b = 255, a = 64}
    local dotCol = (col and col.dot) or {r = 120, g = 190, b = 255, a = 220}
    local labelCol = (col and col.label) or {r = 230, g = 230, b = 240, a = 255}

    -- group state init
    local gsel = _radio_group[groupKey]
    if gsel == nil and defaultSelected then
        gsel = myValue
        _radio_group[groupKey] = gsel
    end
    local selected = (gsel ~= nil and gsel == myValue)

    -- per-button press state
    local key = id[1]
    local st = _radio_btn[key]
    if not st then
        st = {downLast = false, pressed = false}
        _radio_btn[key] = st
    end

    local hovered = (not disabled) and clay.pointerOver(id[1]) or false

    -- press/release
    local changed = false
    if (not st.downLast) and down and hovered and (not disabled) then
        st.pressed = true
    elseif st.pressed and (not down) and st.downLast then
        st.pressed = false
        -- clicking sets this as selected; clicking again only clears if allowNone=true
        local before = _radio_group[groupKey]
        if selected then
            if allowNone then
                _radio_group[groupKey] = nil
            end
        else
            _radio_group[groupKey] = myValue
        end
        changed = (_radio_group[groupKey] ~= before)
        selected = (_radio_group[groupKey] == myValue)
    elseif not down then
        st.pressed = false
    end
    st.downLast = down

    -- visuals: ring color
    local ringCol = disabled and ringDis or (st.pressed and ringAct or (hovered and ringHov or ringBase))

    -- layout: [◉][gap][label]
    clay.createElement(
        id,
        {
            layout = {
                layoutDirection = clay.LEFT_TO_RIGHT,
                childAlignment = {x = clay.ALIGN_X_LEFT, y = clay.ALIGN_Y_CENTER},
                sizing = {width = clay.sizingFit(), height = clay.sizingFixed(size)},
                childGap = gap
            }
        },
        function()
            local ring_id = clay.id("radio-ring", key)
            clay.createElement(
                ring_id,
                {
                    layout = {
                        sizing = {width = clay.sizingFixed(size), height = clay.sizingFixed(size)},
                        childAlignment = {x = clay.ALIGN_X_CENTER, y = clay.ALIGN_Y_CENTER}
                    },
                    backgroundColor = ringCol,
                    border = {color = border, width = {left = 1, right = 1, top = 1, bottom = 1}},
                    cornerRadius = {
                        topLeft = 18,
                        topRight = 18,
                        bottomLeft = 18,
                        bottomRight = 18
                    }
                },
                function()
                    if selected then
                        -- inner dot
                        local dot = math.max(6, math.floor(size * 0.55))
                        clay.createElement(
                            clay.id("radio-dot", key),
                            {
                                layout = {sizing = {width = clay.sizingFixed(dot), height = clay.sizingFixed(dot)}},
                                backgroundColor = dotCol,
                                border = {
                                    color = {r = 255, g = 255, b = 255, a = 90},
                                    width = {left = 1, right = 1, top = 1, bottom = 1}
                                },
                                cornerRadius = {
                                    topLeft = 18,
                                    topRight = 18,
                                    bottomLeft = 18,
                                    bottomRight = 18
                                }
                            }
                        )
                    end
                end
            )

            if label ~= nil then
                clay.createTextElement(label, {fontId = fontId, fontSize = fontSize, textColor = labelCol})
            end
        end
    )

    return selected, changed
end

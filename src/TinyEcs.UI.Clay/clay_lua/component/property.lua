-- component/property.lua
local clay = require("clay")
local glfw = require("ffi.ffi_glfw")

local _prop_state = _prop_state or {}   -- [id] -> { active=false, focused=false, lastDown=false, repeatT=0 }
local _prop_keys  = _prop_keys  or {}   -- key edge table

local function clamp(x,a,b) if x<a then return a elseif x>b then return b else return x end end
local function lerp(a,b,t) return a + (b-a)*t end
local function unlerp(a,b,x) return (b==a) and 0 or (x-a)/(b-a) end
local function snap_step(x, step)
    if not step or step == 0 then return x end
    return math.floor(x/step + 0.5) * step
end

local function key_edge(win, key, keyTable)
    local cur  = (glfw.GetKey(win, key) == glfw.PRESS)
    local last = keyTable[key] or false
    keyTable[key] = cur
    return (cur and not last), cur
end

-- property(id, label, value, opts) -> newValue, changed, focused
-- opts:
-- { min=0, max=1, step=0, pageStep=nil, width=220, height=28, pad=6,
--   buttonW=28, format=nil (function(v)->string or string.format pattern like \"%.2f\"),
--   colors={ bg,bgHov,bgAct,border,text,btn,btnHov,btnAct,fill,disable },
--   cornerRadius=8 }
function property(id, label, value, opts)
    local minv      = (opts and opts.min) or 0
    local maxv      = (opts and opts.max) or 1
    if maxv == minv then maxv = minv + 1 end

    local step      = (opts and opts.step) or 0
    local pageStep  = (opts and opts.pageStep) or (step > 0 and step * 10 or (maxv - minv) * 0.1)
    local width     = (opts and opts.width) or 220
    local height    = (opts and opts.height) or 28
    local pad       = (opts and opts.pad) or 6
    local buttonW   = (opts and opts.buttonW) or 28
    local radius    = (opts and opts.cornerRadius) or 8

    local col          = opts and opts.colors
    local c_bg         = (col and col.bg)      or {r=30,g=30,b=35,a=140}
    local c_bgHov      = (col and col.bgHov)   or {r=40,g=40,b=48,a=160}
    local c_bgAct      = (col and col.bgAct)   or {r=55,g=55,b=65,a=190}
    local c_border     = (col and col.border)  or {r=255,g=255,b=255,a=45}
    local c_text       = (col and col.text)    or {r=235,g=235,b=245,a=255}
    local c_btn        = (col and col.btn)     or {r=70,g=70,b=85,a=170}
    local c_btnHov     = (col and col.btnHov)  or {r=85,g=85,b=110,a=200}
    local c_btnAct     = (col and col.btnAct)  or {r=110,g=110,b=150,a=220}
    local c_disable    = (col and col.disable) or {r=120,g=120,b=120,a=90}

    -- input
    glfw.GetCursorPos(device.win, mouse_x, mouse_y)
    local mx, my = mouse_x[0], mouse_y[0]
    local down   = (glfw.GetMouseButton(device.win, glfw.MOUSE_BUTTON_LEFT) == glfw.PRESS)
    local nowT   = glfw.GetTime and glfw.GetTime() or 0

    -- state
    local key = id[1]
    local st = _prop_state[key]
    if not st then
        st = { active=false, focused=false, lastDown=false, repeatT=0, dragStartX=0, dragStartV=value }
        _prop_state[key] = st
    end

    -- normalize & snap current
    local n = clamp(unlerp(minv, maxv, value), 0, 1)
    if step > 0 then
        value = snap_step(lerp(minv, maxv, n), step)
        n = clamp(unlerp(minv, maxv, value), 0, 1)
    end

    local changed = false

    -- SUB-IDS
    local leftId   = clay.id("prop-left", key)
    local centerId = clay.id("prop-center", key)
    local rightId  = clay.id("prop-right", key)

    -- Root (rounded)
    clay.createElement(id, {
        layout = {
            layoutDirection = clay.LEFT_TO_RIGHT,
            sizing = { width = clay.sizingFixed(width), height = clay.sizingFixed(height) },
            childAlignment = { x = clay.ALIGN_X_CENTER, y = clay.ALIGN_Y_CENTER },
            childGap = 0
        },
        backgroundColor = c_bg,
        --border = { color = c_border, width = { left=1,right=1,top=1,bottom=1 } },
        cornerRadius = { topLeft = radius, topRight = radius, bottomLeft = radius, bottomRight = radius }
    }, function()
        -- LEFT (decrement)
        local hovL = clay.pointerOver(leftId[1])
        local colL = (down and hovL) and c_btnAct or (hovL and c_btnHov or c_btn)
        clay.createElement(leftId, {
            layout = {
                sizing = { width = clay.sizingFixed(buttonW), height = clay.sizingGrow() },
                childAlignment = { x = clay.ALIGN_X_CENTER, y = clay.ALIGN_Y_CENTER }
            },
            backgroundColor = colL,
            cornerRadius = { topLeft = radius, topRight = 0, bottomLeft = radius, bottomRight = 0 }
        }, function()
            clay.createTextElement("-", { fontId=1, fontSize=18, textColor=c_text })
        end)

        -- CENTER (label + slider bar behind text)
        clay.createElement(centerId, {
            layout = {
                layoutDirection = clay.TOP_TO_BOTTOM,
                sizing = { width = clay.sizingGrow(), height = clay.sizingGrow() },
                childAlignment = { x = clay.ALIGN_X_CENTER, y = clay.ALIGN_Y_CENTER },
                padding = clay.paddingXY(pad, 0)
            },
            backgroundColor = (st.active and c_bgAct or (clay.pointerOver(centerId[1]) and c_bgHov or c_bg)),
        }, function()
			-- Text on top
			local text
			if type(opts and opts.format) == "function" then
				text = (opts.format)(value)
			elseif type(opts and opts.format) == "string" then
				local ok, s = pcall(string.format, opts.format, value)
				text = ok and s or tostring(value)
			else
				text = string.format("%s: %s", label or "Property", tostring(value))
			end

			clay.createTextElement(text, { fontId=1, fontSize=16, textColor=c_text })
        end)

        -- RIGHT (increment)
        local hovR = clay.pointerOver(rightId[1])
        local colR = (down and hovR) and c_btnAct or (hovR and c_btnHov or c_btn)
        clay.createElement(rightId, {
            layout = {
                sizing = { width = clay.sizingFixed(buttonW), height = clay.sizingGrow() },
                childAlignment = { x = clay.ALIGN_X_CENTER, y = clay.ALIGN_Y_CENTER }
            },
            backgroundColor = colR,
            cornerRadius = { topLeft = 0, topRight = radius, bottomLeft = 0, bottomRight = radius }
        }, function()
            clay.createTextElement("+", { fontId=1, fontSize=18, textColor=c_text })
        end)
    end)

    -- Interaction: mouse clicks (edge detect per-sub-area)
    local hoveredRoot = clay.pointerOver(id[1])
    local hovL = clay.pointerOver(leftId[1])
    local hovC = clay.pointerOver(centerId[1])
    local hovR = clay.pointerOver(rightId[1])

    -- Repeating click helper (for holding on +/-)
    local function handle_repeat(startedNow)
        local firstDelay, repeatRate = 0.40, 0.06
        if startedNow then
            st.repeatT = nowT + firstDelay
            return true
        elseif down and nowT >= st.repeatT then
            st.repeatT = nowT + repeatRate
            return true
        end
        return false
    end

    -- left / right buttons
    if down and not st.lastDown then
        if hovL then
            st.focused = true
            if handle_repeat(true) then
                value = clamp(value - (step > 0 and step or 1), minv, maxv)
                if step > 0 then value = snap_step(value, step) end
                changed = true
            end
        elseif hovR then
            st.focused = true
            if handle_repeat(true) then
                value = clamp(value + (step > 0 and step or 1), minv, maxv)
                if step > 0 then value = snap_step(value, step) end
                changed = true
            end
        elseif hovC then
            -- start slider drag (absolute position along center)
            st.active, st.focused = true, true
            st.dragStartX = mx
            st.dragStartV = value
            -- also set immediately based on cursor position
            local cb = clay.getElementData(centerId[1])
            if cb.found and cb.width > 0 then
                local pos = clamp((mx - cb.x - pad) / math.max(1, (cb.width - pad*2)), 0, 1)
                local v = lerp(minv, maxv, pos)
                if step > 0 then v = snap_step(v, step) end
                if v ~= value then value, changed = v, true end
            end
        else
            st.focused = hoveredRoot
        end
    elseif down and st.lastDown then
        -- hold-to-repeat on +/- zones
        if hovL and handle_repeat(false) then
            value = clamp(value - (step > 0 and step or 1), minv, maxv)
            if step > 0 then value = snap_step(value, step) end
            changed = true
        elseif hovR and handle_repeat(false) then
            value = clamp(value + (step > 0 and step or 1), minv, maxv)
            if step > 0 then value = snap_step(value, step) end
            changed = true
        end
        -- drag center
        if st.active then
            local cb = clay.getElementData(centerId[1])
            if cb.found and cb.width > 0 then
                local pos = clamp((mx - cb.x - pad) / math.max(1, (cb.width - pad*2)), 0, 1)
                local v = lerp(minv, maxv, pos)
                if step > 0 then v = snap_step(v, step) end
                if v ~= value then value = v; changed = true end
            end
        end
    elseif (not down) and st.lastDown then
        st.active = false
    end

    -- keyboard (when focused)
    if st.focused then
        local shift = (glfw.GetKey(device.win, glfw.KEY_LEFT_SHIFT)  == glfw.PRESS) or
                      (glfw.GetKey(device.win, glfw.KEY_RIGHT_SHIFT) == glfw.PRESS)
        local deltaSmall = (step > 0 and step or (maxv - minv) * 0.01)
        local deltaBig   = pageStep

        if select(1, key_edge(device.win, glfw.KEY_LEFT,  _prop_keys)) then
            value = clamp(value - (shift and deltaBig or deltaSmall), minv, maxv)
            if step > 0 then value = snap_step(value, step) end
            changed = true
        end
        if select(1, key_edge(device.win, glfw.KEY_RIGHT, _prop_keys)) then
            value = clamp(value + (shift and deltaBig or deltaSmall), minv, maxv)
            if step > 0 then value = snap_step(value, step) end
            changed = true
        end
        if select(1, key_edge(device.win, glfw.KEY_HOME, _prop_keys)) then
            value, changed = minv, true
        end
        if select(1, key_edge(device.win, glfw.KEY_END, _prop_keys)) then
            value, changed = maxv, true
        end
        if select(1, key_edge(device.win, glfw.KEY_PAGE_DOWN, _prop_keys)) then
            value = clamp(value - deltaBig, minv, maxv); if step > 0 then value = snap_step(value, step) end; changed = true
        end
        if select(1, key_edge(device.win, glfw.KEY_PAGE_UP, _prop_keys)) then
            value = clamp(value + deltaBig, minv, maxv); if step > 0 then value = snap_step(value, step) end; changed = true
        end
    end

    st.lastDown = down

    -- Final clamp & snap + normalized cache
    value = clamp(value, minv, maxv)
    if step > 0 then value = snap_step(value, step) end

    return value, changed, st.focused
end


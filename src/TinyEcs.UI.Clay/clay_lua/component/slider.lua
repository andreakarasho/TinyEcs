local clay = require("clay")
local glfw = require("ffi.ffi_glfw")

local _slider_state = _slider_state or {} -- [id] -> { active, focused, lastDown }
local _slider_keys  = _slider_keys  or {} -- reuse your key_edge table

local function clamp(x,a,b) if x<a then return a elseif x>b then return b else return x end end
local function lerp(a,b,t) return a + (b-a)*t end
local function unlerp(a,b,x) return (b==a) and 0 or (x-a)/(b-a) end
local function snap_step(x, step)
    if not step or step == 0 then return x end
    local s = math.floor(x/step + 0.5)*step
    if s == 0 then s = 0 end
    return s
end

local function key_edge(win, key, keyTable)
    local cur  = (glfw.GetKey(win, key) == glfw.PRESS)
    local last = keyTable[key] or false
    keyTable[key] = cur
    return (cur and not last), cur
end

function slider(id, value, opts)
    -- opts
    local minv            = (opts and opts.min) or 0
    local maxv            = (opts and opts.max) or 1
    if maxv == minv then maxv = minv + 1 end
    local step            = (opts and opts.step) or 0
    local pageStep        = (opts and opts.pageStep) or (step > 0 and step * 10 or (maxv - minv) * 0.1)
    local width           = (opts and opts.width) or 200
    local height          = (opts and opts.height) or 24
    local pad             = (opts and opts.pad) or 0
    local disabled        = (opts and opts.disabled) or false
    local trackThickness  = (opts and opts.trackThickness) or math.max(6, math.floor(height * 0.30))
    local thumbThickness  = (opts and opts.thumbThickness) or math.max(6, math.floor(height * 0.60))

    local col       = (opts and opts.colors)
    local c_bg      = (col and col.bg)      or {r=30,g=30,b=35,a=0}
    local c_bgHov   = (col and col.bgHov)   or {r=40,g=40,b=48,a=0}
    local c_bgAct   = (col and col.bgAct)   or {r=55,g=55,b=65,a=0}
    local c_bgborder  = (col and col.bgborder)  or {r=255,g=255,b=255,a=0}
    local c_border  = (col and col.border)  or {r=255,g=255,b=255,a=50}
    local c_track   = (col and col.track)   or {r=90,g=90,b=100,a=140}
    local c_thumb   = (col and col.thumb)   or {r=120,g=170,b=255,a=255}
    local c_fill    = (col and col.fill)    or {r=120,g=170,b=255,a=180}
    local c_disable = (col and col.disable) or {r=120,g=120,b=120,a=90}

    -- state
    local key = id[1]
    local st = _slider_state[key]
    if not st then
        st = { active=false, focused=false, lastDown=false }
        _slider_state[key] = st
    end

    local railId = clay.id("rail", key)
    local trkId  = clay.id("trk",  key)

    -- normalize value -> n in [0,1]
    local n = clamp(unlerp(minv, maxv, value), 0, 1)
    if step and step > 0 then
        local snapped = snap_step(lerp(minv, maxv, n), step)
        n = clamp(unlerp(minv, maxv, snapped), 0, 1)
        value = lerp(minv, maxv, n)
    end

    -- input
    glfw.GetCursorPos(device.win, mouse_x, mouse_y)
    local mx, my = mouse_x[0], mouse_y[0]
    local down   = (glfw.GetMouseButton(device.win, glfw.MOUSE_BUTTON_LEFT) == glfw.PRESS)
    local hovered= clay.pointerOver(id[1])

    local changed = false

    -- mouse interactions (use track box; vertical respects verticalFromTop)
    if not disabled then
        if (not st.lastDown) and down and hovered then
            st.active, st.focused = true, true
            local tb = clay.getElementData(trkId[1])
            local lx, ly = mx - tb.x, my - tb.y
            local pos = (lx / tb.width)
            n = clamp(pos, 0, 1)
            value = lerp(minv, maxv, n)
            if step > 0 then 
				value = snap_step(value, step)
				n = clamp(unlerp(minv, maxv, value), 0, 1) 
            end
            changed = true
        elseif st.active and down then
            local tb = clay.getElementData(trkId[1])
            local lx, ly = mx - tb.x, my - tb.y
            local pos = (lx / tb.width)
            pos = clamp(pos, 0, 1)
            if pos ~= n then
                n = pos
                value = lerp(minv, maxv, n)
                if step > 0 then 
					value = snap_step(value, step)
					n = clamp(unlerp(minv, maxv, value), 0, 1) 
                end
                changed = true
            end
        elseif st.active and (not down) then
            st.active = false
        end

        -- keyboard nudges
        if st.focused then
            local press
            local shift = (glfw.GetKey(device.win, glfw.KEY_LEFT_SHIFT)  == glfw.PRESS) or (glfw.GetKey(device.win, glfw.KEY_RIGHT_SHIFT) == glfw.PRESS)
            local deltaSmall = (step > 0 and step or (maxv - minv) * 0.01)
            local deltaBig = pageStep

			press = select(1, key_edge(device.win, glfw.KEY_RIGHT,_slider_keys))
			if press then 
				value = clamp(value + (shift and deltaBig or deltaSmall), minv, maxv)
				changed = true 
			end
			
			press = select(1, key_edge(device.win, glfw.KEY_LEFT, _slider_keys))
			if press then 
				value = clamp(value - (shift and deltaBig or deltaSmall), minv, maxv)
				changed = true
			end

            if step > 0 and changed then 
				value = snap_step(value, step) 
			end
            if changed then 
				n = clamp(unlerp(minv, maxv, value), 0, 1) 
			end
        end
    end

    st.lastDown = down
    local active = st.active

    -- visuals (root -> rail -> trk -> fill)
    local innerW = width  - pad * 2
    local innerH = height - pad * 2
    local bgCol  = disabled and c_bg or (active and c_bgAct or (hovered and c_bgHov or c_bg))
    local trackCol = disabled and c_disable or c_track
    local fillCol  = disabled and c_disable or c_fill

    clay.createElement(
        id,
        {
            layout = {
                layoutDirection = clay.LEFT_TO_RIGHT,
                childAlignment  = { x = clay.ALIGN_X_LEFT, y = clay.ALIGN_Y_CENTER },
                sizing          = { width = clay.sizingFixed(width), height = clay.sizingFixed(height) },
                padding         = clay.paddingAll(pad),
                childGap        = 0
            },
            backgroundColor = bgCol,
            border          = { color = c_bgborder, width = {left=1,right=1,top=1,bottom=1} }
        },
        function()
            clay.createElement(
                railId,
                {
                    layout = {
                        layoutDirection = clay.LEFT_TO_RIGHT,
                        childAlignment  = { x = clay.ALIGN_X_LEFT, y = clay.ALIGN_Y_CENTER },
                        sizing          = { width = clay.sizingFixed(innerW), height = clay.sizingFixed(innerH) },
                        childGap        = 0
                    }
                },
                function()
					-- track (container)
					clay.createElement(
						trkId,
						{
							layout = {
								sizing = { width = clay.sizingFixed(innerW), height = clay.sizingFixed(trackThickness) },
							},
							backgroundColor = trackCol,
							--border          = { color = c_border, width = {left=1,right=1,top=1,bottom=1} }
						},
						function()
							local fillW = math.floor(innerW * n)
							local padThumb = fillW - thumbThickness/2
							clay.createElement(
								clay.id("fill", key),
								{
									layout = {
										sizing = { width = clay.sizingFixed(fillW), height = clay.sizingFixed(trackThickness) },
										childAlignment  = { x = clay.ALIGN_X_LEFT, y = clay.ALIGN_Y_CENTER }
									},
									backgroundColor = fillCol
								}, function()
									-- invisible spacer
									clay.createElement(clay.id("spacer-left", key), {
										layout = { sizing = { width = clay.sizingFixed(padThumb), height = clay.sizingFixed(trackThickness) } },
									})
									clay.createElement(clay.id("thumb", key),
									{
										layout = {
											sizing = { width = clay.sizingFixed(thumbThickness), height = clay.sizingFixed(thumbThickness) }
										},
										backgroundColor = c_thumb,
										border          = { color = c_border, width = {left=1,right=1,top=1,bottom=1} },
										cornerRadius = { topLeft = 8, topRight = 8, bottomLeft = 8, bottomRight = 8}
									})
								end
							)
						end
					)
				end
            )
        end
    )
    return value, changed, active
end

local clay = require("clay")
local glfw = require("ffi.ffi_glfw")

_state = {}

-- scrollbar(viewport_id, content_id, mx, my, down, wheel, opts)
-- opts (all optional, read-only; no default {}):
--   opts.horizontal  -> true for horizontal, false/nil for vertical
--   opts.trackW      -> pixels (default 10 if nil)
--   opts.minThumb    -> pixels (default 24 if nil)
--   opts.wheelStep   -> pixels per wheel notch (default 40 if nil)
--   opts.zIndex      -> default 1000 if nil
--   opts.colorTrack, opts.colorThumb -> clay color tables (if provided)
--   opts.wheelX      -> horizontal wheel delta (falls back to `wheel` if nil)
function scrollbar(viewport_id, content_id, wheel, opts)
    glfw.GetCursorPos(device.win, mouse_x, mouse_y)
    local mx, my = mouse_x[0], mouse_y[0]
    local down = glfw.GetMouseButton(device.win, glfw.MOUSE_BUTTON_LEFT) == glfw.PRESS

    local H = (opts and opts.horizontal) == true
    local trackW = (opts and opts.trackW) or 10
    local minThumb = (opts and opts.minThumb) or 24
    local wheelStep = (opts and opts.wheelStep) or 40
    local zIndex = (opts and opts.zIndex) or 1000
    local colorTrack = opts and opts.colorTrack
    local colorThumb = opts and opts.colorThumb
    local wheelUse = H and ((opts and opts.wheelX) or wheel) or wheel

    local viewBox = clay.getElementData(viewport_id[1])
    local contentBox = clay.getElementData(content_id[1])
    if not (viewBox.found and contentBox.found) then
        return
    end

    -- stable sizes from element boxes
    local viewX, viewY, viewW, viewH = viewBox.x, viewBox.y, viewBox.width, viewBox.height
    local contentW, contentH = contentBox.width, contentBox.height

    -- axis selection
    local viewLen = H and viewW or viewH
    local contentLen = H and contentW or contentH
    local maxScroll = math.max(0, contentLen - viewLen)
    if maxScroll <= 0.5 then
        return
    end -- no overflow → don’t draw

    -- absolute scroll from element offset (0 .. max)
    local abs =
        H and math.max(0, math.min(maxScroll, -(contentBox.x or 0))) or
        math.max(0, math.min(maxScroll, -(contentBox.y or 0)))

    -- thumb geometry
    local ratio = math.min(1, viewLen / contentLen)
    local thumbLen = math.max(minThumb, viewLen * ratio)
    local travel = math.max(0, viewLen - thumbLen)
    local norm = (maxScroll > 0) and (abs / maxScroll) or 0
    local thumbPos = travel * norm

    -- track rect (window space)
    local trackX, trackY, trackWpx, trackHpx
    if H then
        trackX, trackY = viewX, (viewY + viewH - trackW - 4)
        trackWpx, trackHpx = viewW, trackW
    else
        trackX, trackY = (viewX + viewW - trackW - 4), viewY
        trackWpx, trackHpx = trackW, viewH
    end
    local overTrack = (mx >= trackX and mx <= trackX + trackWpx and my >= trackY and my <= trackY + trackHpx)

    -- per-viewport+axis state (no string concat; small int key)
    local key = viewport_id[1] * 2 + (H and 1 or 0)
    local st = _state[key] or {dragging = false, anchor = 0, downLast = false}
    _state[key] = st

    -- start/stop drag (anchored)
    if (not st.downLast) and down and overTrack then
        st.dragging = true
        local cursorNorm0
        if H then
            cursorNorm0 = (viewLen > 0) and ((mx - trackX) / viewLen) or 0
        else
            cursorNorm0 = (viewLen > 0) and ((my - trackY) / viewLen) or 0
        end
        cursorNorm0 = math.max(0, math.min(1, cursorNorm0))
        local currentNorm = (maxScroll > 0) and (abs / maxScroll) or 0
        st.anchor = currentNorm - cursorNorm0
    elseif st.dragging and (not down) and st.downLast then
        st.dragging = false
    end
    st.downLast = down

    -- dragging: clamp cursor to track, map to target, set with negative sign
    if st.dragging then
        local cursorNorm
        if H then
            local cx = math.min(trackX + viewLen, math.max(trackX, mx))
            cursorNorm = (viewLen > 0) and ((cx - trackX) / viewLen) or 0
        else
            local cy = math.min(trackY + viewLen, math.max(trackY, my))
            cursorNorm = (viewLen > 0) and ((cy - trackY) / viewLen) or 0
        end
        local targetNorm = math.max(0, math.min(1, cursorNorm + st.anchor))
        local targetAbs = targetNorm * maxScroll

        if math.abs(targetAbs - abs) > 0.001 then
            if H then
                local scd = clay.getScrollContainerData(viewport_id)
                local curAbsY =
                    (scd and scd.found and scd.scrollPosition and scd.scrollPosition.y) and
                    math.max(0, -scd.scrollPosition.y) or
                    math.max(0, -(contentBox.y or 0))
                clay.setScrollContainerPosition(viewport_id, -targetAbs, -curAbsY)
            else
                local scd = clay.getScrollContainerData(viewport_id)
                local curAbsX =
                    (scd and scd.found and scd.scrollPosition and scd.scrollPosition.x) and
                    math.max(0, -scd.scrollPosition.x) or
                    math.max(0, -(contentBox.x or 0))
                clay.setScrollContainerPosition(viewport_id, -curAbsX, -targetAbs)
            end
            abs = targetAbs
            norm = (maxScroll > 0) and (abs / maxScroll) or 0
            thumbPos = travel * norm
        end
        device.scrolling_override = true
    else
        device.scrolling_override = false
    end

    -- wheel on track (vertical by default; horizontal uses wheelX/wheel)
    if wheelUse ~= 0 and overTrack then
        local targetAbs = math.max(0, math.min(maxScroll, abs - wheelUse * wheelStep))
        if math.abs(targetAbs - abs) > 0.001 then
            if H then
                local scd = clay.getScrollContainerData(viewport_id)
                local curAbsY =
                    (scd and scd.found and scd.scrollPosition and scd.scrollPosition.y) and
                    math.max(0, -scd.scrollPosition.y) or
                    math.max(0, -(contentBox.y or 0))
                clay.setScrollContainerPosition(viewport_id, -targetAbs, -curAbsY)
            else
                local scd = clay.getScrollContainerData(viewport_id)
                local curAbsX =
                    (scd and scd.found and scd.scrollPosition and scd.scrollPosition.x) and
                    math.max(0, -scd.scrollPosition.x) or
                    math.max(0, -(contentBox.x or 0))
                clay.setScrollContainerPosition(viewport_id, -curAbsX, -targetAbs)
            end
            abs = targetAbs
            norm = (maxScroll > 0) and (abs / maxScroll) or 0
            thumbPos = travel * norm
        end
    end

    -- ids per axis so they don't clash
    local scroll_id = clay.id("ScrollBarTrack", key)
    local thumb_id = clay.id("ScrollBarThumb", key)

    -- draw floating track with spacer-thumb (no temp tables beyond Clay colors)
    clay.createElement(
        scroll_id,
        {
            floating = {
                attachTo = clay.ATTACH_TO_ROOT,
                attachPoints = {element = clay.ATTACH_POINT_LEFT_TOP, parent = clay.ATTACH_POINT_LEFT_TOP},
                offset = {x = trackX, y = trackY},
                zIndex = zIndex,
                pointerCaptureMode = clay.POINTER_CAPTURE_MODE_CAPTURE,
                clipTo = clay.CLIP_TO_NONE
            },
            layout = H and
                {
                    layoutDirection = clay.LEFT_TO_RIGHT,
                    sizing = {width = clay.sizingFixed(viewW), height = clay.sizingFixed(trackW)},
                    childGap = 0
                } or
                {
                    layoutDirection = clay.TOP_TO_BOTTOM,
                    sizing = {width = clay.sizingFixed(trackW), height = clay.sizingFixed(viewH)},
                    childGap = 0
                },
            backgroundColor = colorTrack or
                {r = 80, g = 80, b = 80, a = (overTrack or _state[key].dragging) and 80 or 64}
        },
        function()
            if H then
                clay.createElement(
                    clay.id("SBLeftSpacer", key),
                    {
                        layout = {sizing = {width = clay.sizingFixed(math.floor(thumbPos)), height = clay.sizingGrow()}}
                    }
                )
                clay.createElement(
                    thumb_id,
                    {
                        layout = {sizing = {width = clay.sizingFixed(math.floor(thumbLen)), height = clay.sizingGrow()}},
                        backgroundColor = colorThumb or
                            (_state[key].dragging and {r = 200, g = 200, b = 200, a = 200} or
                                {r = 180, g = 180, b = 180, a = 140}),
                        border = {
                            color = {r = 255, g = 255, b = 255, a = 64},
                            width = {left = 1, right = 1, top = 1, bottom = 1}
                        }
                    }
                )
                clay.createElement(
                    clay.id("SBRightSpacer", key),
                    {
                        layout = {sizing = {width = clay.sizingGrow(), height = clay.sizingGrow()}}
                    }
                )
            else
                clay.createElement(
                    clay.id("SBTopSpacer", key),
                    {
                        layout = {sizing = {width = clay.sizingGrow(), height = clay.sizingFixed(math.floor(thumbPos))}}
                    }
                )
                clay.createElement(
                    thumb_id,
                    {
                        layout = {sizing = {width = clay.sizingGrow(), height = clay.sizingFixed(math.floor(thumbLen))}},
                        backgroundColor = colorThumb or
                            (_state[key].dragging and {r = 200, g = 200, b = 200, a = 200} or
                                {r = 180, g = 180, b = 180, a = 140}),
                        border = {
                            color = {r = 255, g = 255, b = 255, a = 64},
                            width = {left = 1, right = 1, top = 1, bottom = 1}
                        }
                    }
                )
                clay.createElement(
                    clay.id("SBBotSpacer", key),
                    {
                        layout = {sizing = {width = clay.sizingGrow(), height = clay.sizingGrow()}}
                    }
                )
            end
        end
    )
end

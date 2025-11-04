-- edit.lua
local clay = require("clay")
local window = require("window")
local font_manager = require("font_manager")
local glfw = require("ffi.ffi_glfw")
local ffi = require("ffi")
local bit = require("bit")
local bor, band, rsh = bit.bor, bit.band, bit.rshift

_edit_state = _edit_state or {} -- [id] -> { focused, caret, sel_a, sel_b, downLast, lastClickT, clickCount, blinkT, scrollX }
_edit_charbuf = _edit_charbuf or {}
_edit_keys = _edit_keys or {}
_edit_measure = font_manager.measureText -- function(text, {fontId, fontSize}, monospaceOpt) -> width in px
_edit_repeat = _edit_repeat or {} -- [key] -> {down=false, next=0}

local function key_repeat(win, key, initialDelay, repeatInterval)
    local now  = (glfw.GetTime and glfw.GetTime()) or 0
    local down = (glfw.GetKey(win, key) == glfw.PRESS)
    local st   = _edit_repeat[key]
    if not st then
        st = { down = false, next = 0 }
        _edit_repeat[key] = st
    end

    if down then
        if not st.down then
            -- first press: fire immediately, then wait initialDelay
            st.down = true
            st.next = now + initialDelay
            return true
        end
        if now >= st.next then
            -- repeat tick
            st.next = now + repeatInterval
            return true
        end
    else
        -- key released: reset
        st.down = false
        st.next = 0
    end
    return false
end

-- UTF-32 -> UTF-8 (LuaJIT bit ops)
local function cp_to_utf8(cp)
    cp = tonumber(cp) or 0
    if cp < 0x80 then
        return string.char(cp)
    elseif cp < 0x800 then
        return string.char(bor(0xC0, rsh(cp, 6)), bor(0x80, band(cp, 0x3F)))
    elseif cp < 0x10000 then
        if cp >= 0xD800 and cp <= 0xDFFF then
            cp = 0xFFFD
        end
        return string.char(bor(0xE0, rsh(cp, 12)), bor(0x80, band(rsh(cp, 6), 0x3F)), bor(0x80, band(cp, 0x3F)))
    elseif cp < 0x110000 then
        return string.char(
            bor(0xF0, rsh(cp, 18)),
            bor(0x80, band(rsh(cp, 12), 0x3F)),
            bor(0x80, band(rsh(cp, 6), 0x3F)),
            bor(0x80, band(cp, 0x3F))
        )
    end
    return ""
end

-- Count UTF-8 codepoints (not bytes)
local function utf8_len(s)
    local n, i, l = 0, 1, #s
    while i <= l do
        n = n + 1
        local b = s:byte(i)
        if b < 0x80 then i = i + 1
        elseif b < 0xE0 then i = i + 2
        elseif b < 0xF0 then i = i + 3
        else i = i + 4 end
    end
    return n
end

-- Take first N codepoints of a UTF-8 string
local function utf8_take(s, n)
    if n <= 0 then return "" end
    local i, l, left = 1, #s, n
    while i <= l and left > 0 do
        left = left - 1
        local b = s:byte(i)
        if b < 0x80 then i = i + 1
        elseif b < 0xE0 then i = i + 2
        elseif b < 0xF0 then i = i + 3
        else i = i + 4 end
    end
    return s:sub(1, i - 1)
end

-- UTF-8 helpers
local function utf8_iscont(b)
    return (b and band(b, 0xC0) == 0x80) or false
end

local function utf8_next_index(s, i)
    local n = #s
    if i > n then
        return n + 1
    end
    i = i + 1
    while i <= n and utf8_iscont(s:byte(i)) do
        i = i + 1
    end
    return i
end

local function utf8_prev_index(s, i)
    if i <= 1 then
        return 1
    end
    i = i - 1
    while i > 1 and utf8_iscont(s:byte(i)) do
        i = i - 1
    end
    return i
end

-- word boundaries (simple: spaces/punct split)
local function is_word_delim(byte)
    if not byte then
        return true
    end
    local ch = string.char(byte)
    return ch:match("[%s%p]") ~= nil
end

local function word_left_byte(s, bi)
    local i = utf8_prev_index(s, bi)
    while i > 1 and not is_word_delim(s:byte(i)) do
        i = utf8_prev_index(s, i)
    end
    return i
end

local function word_right_byte(s, bi)
    local i = bi
    local n = #s + 1
    while i <= #s and not is_word_delim(s:byte(i)) do
        i = utf8_next_index(s, i)
    end
    if i < n then
        i = utf8_next_index(s, i)
    end
    return i
end

-- simple key edge
local function key_edge(win, key)
    local cur = glfw.GetKey(win, key) == glfw.PRESS
    local last = _edit_keys[key] or false
    _edit_keys[key] = cur
    return (cur and not last), cur
end

-- caret from x (px) using your measureText callback (binary search over bytes)
local function caret_from_x(text, xTarget, fontId, fontSize)
    if not _edit_measure then
        return #text + 1
    end
    local cfg = {fontId = fontId, fontSize = fontSize}
    local lo, hi = 1, #text + 1
    while lo < hi do
        local mid = math.floor((lo + hi) * 0.5)
        local w = _edit_measure(text:sub(1, mid - 1), cfg, true) or 0
        if math.abs(w - xTarget) < 0.5 then
            lo = mid
            break
        end
        if w <= xTarget then
            lo = mid + 1
        else
            hi = mid
        end
    end
    local bi = math.min(lo, #text + 1)
    while bi > 1 and utf8_iscont(text:byte(bi)) do
        bi = bi - 1
    end
    return bi
end

-- measure prefix width in px
local function measure_prefix(text, bytesEnd, fontId, fontSize)
    if bytesEnd <= 1 then
        return 0
    end
    return _edit_measure(text:sub(1, bytesEnd - 1), {fontId = fontId, fontSize = fontSize}, true) or 0
end

-- byte index for a target x (px) (binary search, UTF-8 safe)
local function byte_from_x(text, targetX, fontId, fontSize)
    local lo, hi = 1, #text + 1
    while lo < hi do
        local mid = rsh(lo + hi, 1)
        local w = _edit_measure(text:sub(1, mid - 1), {fontId = fontId, fontSize = fontSize}, true) or 0
        if math.abs(w - targetX) < 0.5 then
            lo = mid
            break
        end
        if w <= targetX then
            lo = mid + 1
        else
            hi = mid
        end
    end
    local bi = math.min(lo, #text + 1)
    while bi > 1 and utf8_iscont(text:byte(bi)) do
        bi = bi - 1
    end
    return bi
end

-- keep caret visible horizontally by adjusting st.scrollX (px)
local function ensure_caret_visible(text, caretBi, boxW, pad, st, fontId, fontSize)
    local avail = math.max(0, boxW - pad * 2)
    local caretX = measure_prefix(text, caretBi, fontId, fontSize)
    if caretX - st.scrollX > avail then
        st.scrollX = caretX - avail + 1
    elseif caretX - st.scrollX < 0 then
        st.scrollX = caretX
    end
    if st.scrollX < 0 then
        st.scrollX = 0
    end
end

-- visible byte range [b0, b1) for current scroll window
local function visible_byte_range(text, st, boxW, pad, fontId, fontSize)
    local avail = math.max(0, boxW - pad * 2)
    local leftX = st.scrollX
    local rightX = st.scrollX + avail
    local b0 = byte_from_x(text, leftX, fontId, fontSize)
    local b1 = byte_from_x(text, rightX, fontId, fontSize)
    if b1 < b0 then
        b1 = b0
    end
    return b0, b1
end

-- Simple built-in filters: return filtered string
local BUILTIN_FILTERS = {
    -- digits only
    digits = function(s) return (s:gsub("%D", "")) end,

    -- signed integer (allows leading + or - anywhere you paste; not a full validator)
    integer = function(s) return (s:gsub("[^%-%+%d]", "")) end,

    -- float-ish: digits, one dot, optional sign, and exponent chars (loose by design)
    float = function(s) return (s:gsub("[^%-%+%.eE%d]", "")) end,

    -- hex (no 0x added)
    hex = function(s) return (s:gsub("[^%x]", "")) end,

    -- alpha-numeric + underscore
    alnum = function(s) return (s:gsub("[^%w]", "")) end,

    -- printable ASCII (remove control chars)
    ascii = function(s) return (s:gsub("[^%c%g%s]", "")):gsub("[%z\1-\31\127]", "") end,

    -- no newlines / tabs
    no_newlines = function(s) return (s:gsub("[\r\n]", "")) end,
    no_tabs     = function(s) return (s:gsub("\t", "")) end,

    -- example: a filename-safe-ish set (very conservative)
    filename = function(s) return (s:gsub('[<>:"/\\|%?%*%z]', "")) end,
}

local function apply_filter_chunk(s, filter)
    if not filter then return s end
    if type(filter) == "function" then
        return filter(s) or ""
    end
    local f = BUILTIN_FILTERS[filter]
    if f then return f(s) or "" end
    return s -- unknown filter name => accept as-is
end

-- Collect typed characters via GLFW char callback
local function set_char_callback(codepoint)
    if codepoint == 8 or codepoint == 127 then return end -- ignore BS/Del
    local s = cp_to_utf8(tonumber(codepoint))
    if #s > 0 then
        s = apply_filter_chunk(s, filterOpt)  -- early filter (optional)
        if #s > 0 then
            _edit_charbuf[#_edit_charbuf + 1] = s
        end
    end
end

window.callback_register("window_set_char", set_char_callback)

-- PUBLIC: edit(id, text, opts) -> newText, changed, focused
-- opts: { width=200, height=24, pad=8, fontId=1, fontSize=16, colors={bg,bgHov,bgAct,border,text,caret,selBg,selText}, maxChars=nil, filter=nil (can be built-in by string name or custom function }
function edit(id, text, opts)
    glfw.GetCursorPos(device.win, mouse_x, mouse_y)
    local mx, my = mouse_x[0], mouse_y[0]
    local down = glfw.GetMouseButton(device.win, glfw.MOUSE_BUTTON_LEFT) == glfw.PRESS
    local timeNow = glfw.GetTime and glfw.GetTime() or 0

    text = text or ""
    local width = (opts and opts.width) or 200
    local height = (opts and opts.height) or 24
    local pad = (opts and opts.pad) or 8
    local fontId = (opts and opts.fontId) or 1
    local fontSize = (opts and opts.fontSize) or 16
    local maxChars = (opts and opts.maxChars) or nil  -- e.g. 32; nil = unlimited
	local filterOpt = (opts and opts.filter) or nil   -- string key or function(s) -> filtered s


    local col = opts and opts.colors
    local c_bg = (col and col.bg) or {r = 30, g = 30, b = 35, a = 160}
    local c_bgHov = (col and col.bgHov) or {r = 40, g = 40, b = 48, a = 190}
    local c_bgAct = (col and col.bgAct) or {r = 50, g = 50, b = 60, a = 220}
    local c_border = (col and col.border) or {r = 255, g = 255, b = 255, a = 64}
    local c_text = (col and col.text) or {r = 235, g = 235, b = 245, a = 255}
    local c_caret = (col and col.caret) or {r = 180, g = 200, b = 255, a = 255}
    local c_selBg = (col and col.selBg) or {r = 90, g = 120, b = 180, a = 120}
    local c_selTxt = (col and col.selTxt) or c_text

    -- state per id
    local key = id[1]
    local st = _edit_state[key]
    if not st then
        st = {
            focused = false,
            caret = #text + 1,
            sel_a = 0,
            sel_b = 0,
            downLast = false,
            lastClickT = 0,
            clickCount = 0,
            blinkT = 0,
            scrollX = 0.0
        }
        _edit_state[key] = st
    end
    local changed = false

    -- helper: clamp caret & scroll after any mutation
    local function clamp_after_change()
        if st.caret < 1 then
            st.caret = 1
        end
        local maxpos = #text + 1
        if st.caret > maxpos then
            st.caret = maxpos
        end
        if #text == 0 then
            st.scrollX = 0
        end
        local box = clay.getElementData(id[1])
        if box.found then
            ensure_caret_visible(text, st.caret, box.width, pad, st, fontId, fontSize)
        end
    end

	-- Insert 'ins' into [a,b) (byte indices), applying filter and maxChars (codepoints).
	-- Returns new text and new caret (byte index).
	local function insert_filtered(text0, a, b, ins)
		-- 1) filter chunk
		ins = apply_filter_chunk(ins, filterOpt)

		-- early out if nothing to insert and nothing to delete
		if (not ins or #ins == 0) and a == b then
			return text0, st.caret
		end

		-- 2) enforce maxChars in codepoints (if provided)
		if maxChars then
			-- current count minus what will be removed
			local removing = text0:sub(a, b - 1)
			local cur_cp = utf8_len(text0) - utf8_len(removing)
			local allowed = maxChars - cur_cp
			if allowed <= 0 then
				-- cannot add; just delete selection (if any)
				if a ~= b then
					local t = text0:sub(1, a - 1) .. text0:sub(b)
					return t, a
				else
					return text0, st.caret -- nothing changes
				end
			end
			if utf8_len(ins) > allowed then
				ins = utf8_take(ins, allowed) -- clip to remaining space
			end
		end

		-- 3) splice
		local out = text0:sub(1, a - 1) .. ins .. text0:sub(b)
		local newCaret = a + #ins
		return out, newCaret
	end

    -- hover
    local hovered = clay.pointerOver(id[1])

    -- mouse press / focus & selection begin
    if (not st.downLast) and down then
        if hovered then
            st.focused = true
            local box = clay.getElementData(id[1])
            local localX = (box.found and (mx - box.x - pad) or 0)
            if localX < 0 then
                localX = 0
            end
            local bi = caret_from_x(text, st.scrollX + localX, fontId, fontSize)

            -- click count (single/double/triple)
            if timeNow - st.lastClickT < 0.35 then
                st.clickCount = st.clickCount + 1
            else
                st.clickCount = 1
            end
            st.lastClickT = timeNow

            if st.clickCount == 1 then
                st.caret = bi
                st.sel_a, st.sel_b = 0, 0
            elseif st.clickCount == 2 then
                local wl = word_left_byte(text, bi)
                local wr = word_right_byte(text, bi)
                st.sel_a, st.sel_b = wl, wr
                st.caret = wr
            else
                st.sel_a, st.sel_b = 1, #text + 1
                st.caret = #text + 1
            end
            clamp_after_change()
            st.blinkT = timeNow
        else
            st.focused = false
            st.sel_a, st.sel_b = 0, 0
        end
    end

    -- drag selection
    if st.focused and down then
        local box = clay.getElementData(id[1])
        if box.found then
            local localX = mx - box.x - pad
            localX = math.max(0, math.min(localX, box.width - 2 * pad))
            local bi = caret_from_x(text, st.scrollX + localX, fontId, fontSize)
            if st.sel_a == 0 and st.sel_b == 0 then
                st.sel_a = st.caret
            end
            st.sel_b = bi
            st.caret = bi
            clamp_after_change()
            st.blinkT = timeNow
        end
    end

    st.downLast = down

    -- typed characters (single concat)
	if st.focused and #_edit_charbuf > 0 then
		local typed = table.concat(_edit_charbuf)
		_edit_charbuf = {}

		-- normalize line breaks (optional; good for single-line)
		typed = typed:gsub("\r\n", "\n"):gsub("\r", "\n")

		local a, b
		if st.sel_a ~= 0 and st.sel_b ~= 0 and st.sel_a ~= st.sel_b then
			a, b = st.sel_a, st.sel_b
			if a > b then a, b = b, a end
		else
			a, b = st.caret, st.caret
		end

		text, st.caret = insert_filtered(text, a, b, typed)
		st.sel_a, st.sel_b = 0, 0
		clamp_after_change()
		changed = true
		st.blinkT = timeNow
	elseif not st.focused and #_edit_charbuf > 0 then
		_edit_charbuf = {}
	end

    -- keyboard controls (UTF-8 aware)
    if st.focused then
        local function has_sel()
            return (st.sel_a ~= 0 or st.sel_b ~= 0) and (st.sel_a ~= st.sel_b)
        end
        local function del_sel()
            local a, b = st.sel_a, st.sel_b
            if a > b then
                a, b = b, a
            end
            text = text:sub(1, a - 1) .. text:sub(b)
            st.caret = a
            st.sel_a, st.sel_b = 0, 0
            clamp_after_change()
            changed = true
        end

        local press

		-- Backspace with repeat (e.g. 0.45s delay, then ~25 Hz)
		local backspaceTick = key_repeat(device.win, glfw.KEY_BACKSPACE, 0.45, 0.04)
		if backspaceTick then
			if has_sel() then
				del_sel()
			elseif st.caret > 1 then
				local bi0 = utf8_prev_index(text, st.caret)
				text = text:sub(1, bi0 - 1) .. text:sub(st.caret)
				st.caret = bi0
				clamp_after_change()
				changed = true
			end
			st.blinkT = timeNow
		end


        -- Delete
        press = key_edge(device.win, glfw.KEY_DELETE)
        if press then
            if has_sel() then
                del_sel()
            elseif st.caret <= #text then
                local bi1 = utf8_next_index(text, st.caret)
                text = text:sub(1, st.caret - 1) .. text:sub(bi1)
                clamp_after_change()
                changed = true
            end
            st.blinkT = timeNow
        end

        local ctrl =
            (glfw.GetKey(device.win, glfw.KEY_LEFT_CONTROL) == glfw.PRESS) or
            (glfw.GetKey(device.win, glfw.KEY_RIGHT_CONTROL) == glfw.PRESS)
        local shift =
            (glfw.GetKey(device.win, glfw.KEY_LEFT_SHIFT) == glfw.PRESS) or
            (glfw.GetKey(device.win, glfw.KEY_RIGHT_SHIFT) == glfw.PRESS)

        -- Left / Right (Ctrl=word, Shift=extend)
        press = key_edge(device.win, glfw.KEY_LEFT)
        if press then
            local newBi = ctrl and word_left_byte(text, st.caret) or utf8_prev_index(text, st.caret)
            if shift then
                if st.sel_a == 0 and st.sel_b == 0 then
                    st.sel_a = st.caret
                end
                st.sel_b = newBi
            else
                st.sel_a, st.sel_b = 0, 0
            end
            st.caret = newBi
            clamp_after_change()
            st.blinkT = timeNow
        end

        press = key_edge(device.win, glfw.KEY_RIGHT)
        if press then
            local newBi = ctrl and word_right_byte(text, st.caret) or utf8_next_index(text, st.caret)
            if shift then
                if st.sel_a == 0 and st.sel_b == 0 then
                    st.sel_a = st.caret
                end
                st.sel_b = newBi
            else
                st.sel_a, st.sel_b = 0, 0
            end
            st.caret = newBi
            clamp_after_change()
            st.blinkT = timeNow
        end

        -- Home / End
        press = key_edge(device.win, glfw.KEY_HOME)
        if press then
            if shift then
                if st.sel_a == 0 and st.sel_b == 0 then
                    st.sel_a = st.caret
                end
                st.sel_b = 1
            else
                st.sel_a, st.sel_b = 0, 0
            end
            st.caret = 1
            clamp_after_change()
            st.blinkT = timeNow
        end

        press = key_edge(device.win, glfw.KEY_END)
        if press then
            local endBi = #text + 1
            if shift then
                if st.sel_a == 0 and st.sel_b == 0 then
                    st.sel_a = st.caret
                end
                st.sel_b = endBi
            else
                st.sel_a, st.sel_b = 0, 0
            end
            st.caret = endBi
            clamp_after_change()
            st.blinkT = timeNow
        end

        -- Ctrl+Backspace/Delete (delete word)
        if ctrl then
            if backspaceTick and not has_sel() then
                local bi0 = word_left_byte(text, st.caret)
                text = text:sub(1, bi0 - 1) .. text:sub(st.caret)
                st.caret = bi0
                clamp_after_change()
                changed = true
                st.blinkT = timeNow
            end
            press = key_edge(device.win, glfw.KEY_DELETE)
            if press and not has_sel() then
                local bi1 = word_right_byte(text, st.caret)
                text = text:sub(1, st.caret - 1) .. text:sub(bi1)
                clamp_after_change()
                changed = true
                st.blinkT = timeNow
            end
            -- Ctrl+A (select all)
            press = key_edge(device.win, glfw.KEY_A)
            if press then
                st.sel_a, st.sel_b = 1, #text + 1
                st.caret = st.sel_b
                clamp_after_change()
                st.blinkT = timeNow
            end
        end
    end

    -- Clipboard (Ctrl+C/X/V)
    if st.focused then
        local ctrlHeld =
            (glfw.GetKey(device.win, glfw.KEY_LEFT_CONTROL) == glfw.PRESS) or
            (glfw.GetKey(device.win, glfw.KEY_RIGHT_CONTROL) == glfw.PRESS)

        local function get_clip()
            local p = glfw.GetClipboardString(device.win) or ""
            if p then
                -- TODO: strip newline on single line edit?
                return ffi.string(p)
            end
        end
        local function set_clip(s)
            glfw.SetClipboardString(device.win, s)
        end

        local pressC = key_edge(device.win, glfw.KEY_C)
        local pressX = key_edge(device.win, glfw.KEY_X)
        local pressV = key_edge(device.win, glfw.KEY_V)

        if ctrlHeld and (pressC or pressX) then
            local a, b = st.sel_a, st.sel_b
            if a > b then
                a, b = b, a
            end
            if a ~= 0 and b ~= 0 and a ~= b then
                set_clip(text:sub(a, b - 1))
                if pressX then
                    text = text:sub(1, a - 1) .. text:sub(b)
                    st.caret = a
                    st.sel_a, st.sel_b = 0, 0
                    clamp_after_change()
                    changed = true
                end
            end
        end

		if ctrlHeld and pressV then
			local clip = get_clip() or ""
			-- normalize line breaks
			clip = clip:gsub("\r\n", "\n"):gsub("\r", "\n")
			-- (optionally strip newlines for single-line edits)
			-- clip = clip:gsub("[\n\t]", " ")

			local a, b
			if st.sel_a ~= 0 and st.sel_b ~= 0 and st.sel_a ~= st.sel_b then
				a, b = st.sel_a, st.sel_b
				if a > b then a, b = b, a end
			else
				a, b = st.caret, st.caret
			end

			local newText, newCaret = insert_filtered(text, a, b, clip)
			if newText ~= text then
				text, st.caret = newText, newCaret
				st.sel_a, st.sel_b = 0, 0
				clamp_after_change()
				changed = true
				st.blinkT = timeNow
			end
		end
    end

    -- caret blink
    local showCaret = false
    if st.focused then
        local t = timeNow - st.blinkT
        showCaret = ((t * 2.0) % 2.0) < 1.0
    end

    -- draw: background & border (VISIBLE WINDOW ONLY)
    local box_for_draw = clay.getElementData(id[1])
    local boxW = (box_for_draw.found and box_for_draw.width or 300)

    -- compute visible byte window from scrollX
    local b0, b1 = visible_byte_range(text, st, boxW, pad, fontId, fontSize)

    -- selection (absolute bytes)
    local selA, selB = st.sel_a, st.sel_b
    if selA > selB then
        selA, selB = selB, selA
    end
    local hasSel = (selA ~= 0 or selB ~= 0) and (selA ~= selB)

    -- caret (byte)
    local caretBi = st.caret

    -- visible selection intersection
    local visSelA, visSelB = b0, b0
    if hasSel then
        visSelA = math.max(b0, math.min(selA, b1))
        visSelB = math.max(b0, math.min(selB, b1))
        if visSelB < visSelA then
            visSelB = visSelA
        end
    end

    -- split visible text
    local leftVis, midVis, rightVis
    if hasSel then
        leftVis = (visSelA > b0) and text:sub(b0, visSelA - 1) or ""
        midVis = (visSelB > visSelA) and text:sub(visSelA, visSelB - 1) or ""
        rightVis = (b1 > visSelB) and text:sub(visSelB, b1 - 1) or ""
    else
        local caretInside = (caretBi >= b0 and caretBi <= b1)
        leftVis = (caretInside and caretBi > b0) and text:sub(b0, caretBi - 1) or text:sub(b0, b1 - 1)
        midVis = ""
        rightVis = (caretInside and caretBi < b1) and text:sub(caretBi, b1 - 1) or ""
    end

    clay.createElement(
        id,
        {
            layout = {
                layoutDirection = clay.LEFT_TO_RIGHT,
                childAlignment = {x = clay.ALIGN_X_LEFT, y = clay.ALIGN_Y_CENTER},
                sizing = {width = clay.sizingFixed(width), height = clay.sizingFixed(height)},
                padding = {left = pad, right = pad, top = 0, bottom = 0},
                childGap = 0
            },
            clip = {horizontal = true, vertical = false}, -- clip horizontally
            backgroundColor = st.focused and c_bgAct or (hovered and c_bgHov or c_bg),
            border = {color = c_border, width = {left = 1, right = 1, top = 1, bottom = 1}}
        },
        function()
            -- left (unselected)
            if #leftVis > 0 then
                clay.createTextElement(leftVis, {fontId = fontId, fontSize = fontSize, textColor = c_text})
            end

            -- caret (only when no selection)
            if not hasSel and st.focused then
                local caretInside = (caretBi >= b0 and caretBi <= b1)
                if caretInside and showCaret then
                    clay.createElement(
                        clay.id("caret", key),
                        {
                            layout = {
                                sizing = {
                                    width = clay.sizingFixed(1),
                                    height = clay.sizingFixed(math.floor(height * 0.65))
                                },
                                padding = {left = 1, right = 1, top = 0, bottom = 0}
                            },
                            backgroundColor = c_caret
                        }
                    )
                end
            end

            -- mid (selected)
            if hasSel and #midVis > 0 then
                clay.createElement(
                    clay.id("sel", key),
                    {
                        layout = {layoutDirection = clay.LEFT_TO_RIGHT, childGap = 0},
                        backgroundColor = c_selBg
                    },
                    function()
                        clay.createTextElement(midVis, {fontId = fontId, fontSize = fontSize, textColor = c_selTxt})
                    end
                )
            end

            -- right (unselected)
            if #rightVis > 0 then
                clay.createTextElement(rightVis, {fontId = fontId, fontSize = fontSize, textColor = c_text})
            end
        end
    )

    return text, changed, st.focused
end

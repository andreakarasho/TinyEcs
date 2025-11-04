-- TODO: Kerning?
-- TODO: FIX ME: UTF-8 support

local ffi = require("ffi")
local bit = require("bit")
local band, bor, lshift = bit.band, bit.bor, bit.lshift

local clay = require("clay")
local bgfx = require("ffi.ffi_bgfx")
local glfw = require("ffi.ffi_glfw")
local window = require("window")
local stbrt = require("ffi.ffi_stb_rect_pack")
local stbtt = require("ffi.ffi_stb_truetype")
local stbi = require("ffi.ffi_stb_image")
require("ffi.ffi_math")

local font_cache = {}
local fonts = {
    [0] = "font/DejaVuSansMono.ttf",
    [1] = "font/georgia.ttf"
}

local function getGlyphAdvance(font, codepoint)
    local advanceWidth = ffi.new("int[1]")
    local lsb = ffi.new("int[1]")
    stbtt.GetCodepointHMetrics(font.info, codepoint, advanceWidth, lsb)
    return advanceWidth[0] * font.scale
end

-- Unicode blocks we want to lazily load when a codepoint appears in them
local unicode_ranges = {
    -- Basic
    {first = 0x0020, count = 96}, -- Basic Latin (ASCII)
    {first = 0x00A0, count = 96}, -- Latin-1 Supplement
    {first = 0x0100, count = 256}, -- Latin Extended-A/B (Central/Eastern European, accents)
    {first = 0x0180, count = 208}, -- Latin Extended Additional (Vietnamese, etc.)
    -- Western Europe + punctuation
    {first = 0x2000, count = 96}, -- General Punctuation
    {first = 0x2100, count = 128}, -- Letterlike Symbols, Currency, Misc Technical
    -- Greek
    {first = 0x0370, count = 128}, -- Greek and Coptic
    -- Cyrillic (Russian, Ukrainian, Bulgarian, etc.)
    {first = 0x0400, count = 256}, -- Cyrillic + Cyrillic Supplement
    -- Hebrew, Arabic
    {first = 0x0590, count = 128}, -- Hebrew
    {first = 0x0600, count = 256}, -- Arabic
    {first = 0x0750, count = 96}, -- Arabic Supplement
    {first = 0x08A0, count = 128}, -- Arabic Extended
    -- Devanagari / South Asian (Hindi, etc.)
    {first = 0x0900, count = 128}, -- Devanagari
    {first = 0x0980, count = 128}, -- Bengali
    {first = 0x0A00, count = 128}, -- Gurmukhi / Gujarati (Punjabi)
    -- CJK (Chinese, Japanese, Korean)
    {first = 0x3000, count = 256}, -- CJK Symbols and Punctuation
    {first = 0x3040, count = 96}, -- Hiragana
    {first = 0x30A0, count = 96}, -- Katakana
    {first = 0x3130, count = 96}, -- Hangul Compatibility Jamo
    {first = 0x4E00, count = 20992}, -- CJK Unified Ideographs (common Han characters subset)
    {first = 0xAC00, count = 11184}, -- Hangul Syllables (Korean full set)
    -- Thai, Lao
    {first = 0x0E00, count = 128}, -- Thai
    {first = 0x0E80, count = 64}, -- Lao
    -- Miscellaneous
    {first = 0x1F00, count = 256}, -- Greek Extended / Misc Symbols
    {first = 0x1F300, count = 768}, -- Misc Symbols & Pictographs (Emoji)
    {first = 0x1F600, count = 512}, -- Emoticons (Emoji Faces)
    {first = 0x1F900, count = 512} -- Supplemental Symbols & Pictographs
}

local function findPackedGlyph(font, codepoint)
    for i = 1, #font.glyphs do
        local first = font.glyphs_first[i]
        local count = font.glyphs_count[i]
        if codepoint >= first and codepoint < first + count then
            return font.glyphs[i], first
        end
    end
    return nil
end

local function flushAtlas(font)
    if not font.atlas_dirty then
        return
    end
    local count = font.width * font.height

    -- grayscale -> RGBA
    for i = 0, count - 1 do
        local v = font.atlas_pixels[i]
        font.rgba[i * 4 + 0] = v
        font.rgba[i * 4 + 1] = v
        font.rgba[i * 4 + 2] = v
        font.rgba[i * 4 + 3] = v
    end

    local mem = bgfx.bgfx_copy(font.rgba, count * 4)
    bgfx.bgfx_update_texture_2d(font.texture, 0, 0, 0, 0, font.width, font.height, mem, font.width * 4)
    font.atlas_dirty = false
end

local function packGlyph(font, codepoint)
    -- already exists?
    local pack, base = findPackedGlyph(font, codepoint)
    if pack then
        return true
    end

    -- find the Unicode block for this codepoint
    for _, range in ipairs(unicode_ranges) do
        local first, count = range.first, range.count
        if codepoint >= first and codepoint < first + count then
            local buf = ffi.new("stbtt_packedchar[?]", count)
            local ok = stbtt.PackFontRange(font.pack_context, font.data, 0, font.size, first, count, buf)
            if ok ~= 0 then
                table.insert(font.glyphs, buf)
                table.insert(font.glyphs_first, first)
                table.insert(font.glyphs_count, count)
                font.atlas_dirty = true
                -- Update atlas texture
                flushAtlas(font)
                return true
            else
                print(string.format("[warn] pack failed for U+%04X..U+%04X", first, first + count - 1))
                return false
            end
        end
    end
    return false
end

local function utf8_iter_fast(s)
    local p = ffi.cast("const uint8_t*", s) -- pointer to string bytes
    local i, n = 0, #s -- byte index, length

    return function()
        if i >= n then
            return nil
        end

        local b0 = p[i]
        i = i + 1
        -- 1-byte ASCII
        if b0 < 0x80 then
            return b0
        end

        -- 2-byte sequence: C2..DF 80..BF
        if b0 >= 0xC2 and b0 < 0xE0 then
            -- 3-byte sequence: E0..EF with edge checks for E0/ED
            if i < n then
                local b1 = p[i]
                if band(b1, 0xC0) == 0x80 then
                    i = i + 1
                    return bor(lshift(b0 - 0xC0, 6), band(b1, 0x3F))
                end
            end
        elseif b0 < 0xF0 then
            -- 4-byte sequence: F0..F4 with edge checks for F0/F4
            if i + 1 < n then
                local b1 = p[i]
                local b2 = p[i + 1]
                -- disallow overlongs/surrogates
                if b0 == 0xE0 then
                    if b1 >= 0xA0 and b1 <= 0xBF and band(b2, 0xC0) == 0x80 then
                        i = i + 2
                        return bor(lshift(b0 - 0xE0, 12), lshift(band(b1, 0x3F), 6), band(b2, 0x3F))
                    end
                elseif b0 == 0xED then
                    if b1 >= 0x80 and b1 <= 0x9F and band(b2, 0xC0) == 0x80 then
                        i = i + 2
                        return bor(lshift(b0 - 0xE0, 12), lshift(band(b1, 0x3F), 6), band(b2, 0x3F))
                    end
                else
                    if band(b1, 0xC0) == 0x80 and band(b2, 0xC0) == 0x80 then
                        i = i + 2
                        return bor(lshift(b0 - 0xE0, 12), lshift(band(b1, 0x3F), 6), band(b2, 0x3F))
                    end
                end
            end
        elseif b0 < 0xF5 then
            if i + 2 < n then
                local b1 = p[i]
                local b2 = p[i + 1]
                local b3 = p[i + 2]
                if b0 == 0xF0 then
                    if b1 >= 0x90 and b1 <= 0xBF and band(b2, 0xC0) == 0x80 and band(b3, 0xC0) == 0x80 then
                        i = i + 3
                        return bor(
                            lshift(b0 - 0xF0, 18),
                            lshift(band(b1, 0x3F), 12),
                            lshift(band(b2, 0x3F), 6),
                            band(b3, 0x3F)
                        )
                    end
                elseif b0 == 0xF4 then
                    if b1 >= 0x80 and b1 <= 0x8F and band(b2, 0xC0) == 0x80 and band(b3, 0xC0) == 0x80 then
                        i = i + 3
                        return bor(
                            lshift(b0 - 0xF0, 18),
                            lshift(band(b1, 0x3F), 12),
                            lshift(band(b2, 0x3F), 6),
                            band(b3, 0x3F)
                        )
                    end
                else
                    if band(b1, 0xC0) == 0x80 and band(b2, 0xC0) == 0x80 and band(b3, 0xC0) == 0x80 then
                        i = i + 3
                        return bor(
                            lshift(b0 - 0xF0, 18),
                            lshift(band(b1, 0x3F), 12),
                            lshift(band(b2, 0x3F), 6),
                            band(b3, 0x3F)
                        )
                    end
                end
            end
        end

        -- Invalid sequence: advance over any continuation bytes, return U+FFFD
        while i < n and band(p[i], 0xC0) == 0x80 do
            i = i + 1
        end
        return 0xFFFD
    end
end

local function packLanguageRange(font, first, num)
    local ok = stbtt.PackFontRange(font.pack_context, font.data, 0, font.size, first, num, font.glyphs + first)
    if ok == 0 then
        print(string.format("[warn] pack failed for U+%04X..U+%04X", first, first + num - 1))
    end
end

local M = {}

function M.load(font_id, font_size)
    local filename = fonts[font_id]
    assert(filename, "Invalid font id " .. tostring(font_id))

    if not font_cache[font_id] then
        font_cache[font_id] = {}
    elseif font_cache[font_id][font_size] then
        return font_cache[font_id][font_size]
    end

    -- Load font file
    local file = assert(io.open(filename, "rb"), "Font file not found: " .. filename)
    local fontDataStr = file:read("*a")
    file:close()

    local fontData = ffi.new("uint8_t[?]", #fontDataStr)
    ffi.copy(fontData, fontDataStr, #fontDataStr)

    local atlasWidth, atlasHeight = 2048, 2048
    local atlasPixels = ffi.new("uint8_t[?]", atlasWidth * atlasHeight)
    local packContext = ffi.new("stbtt_pack_context[1]")

    assert(stbtt.PackBegin(packContext, atlasPixels, atlasWidth, atlasHeight, 0, 1, nil) ~= 0, "PackBegin failed")

    -- Bake ASCII range (32–126)
    local firstChar, numChars = 32, 95
    local glyphs = ffi.new("stbtt_packedchar[?]", numChars)

    assert(
        stbtt.PackFontRange(packContext, fontData, 0, font_size, firstChar, numChars, glyphs) ~= 0,
        "PackFontRange failed"
    )
    --stbtt.PackEnd(packContext)

    -- Convert grayscale -> RGBA
    local rgba = ffi.new("uint8_t[?]", atlasWidth * atlasHeight * 4)
    for i = 0, atlasWidth * atlasHeight - 1 do
        local v = atlasPixels[i]
        rgba[i * 4 + 0] = v
        rgba[i * 4 + 1] = v
        rgba[i * 4 + 2] = v
        rgba[i * 4 + 3] = v
    end

    local mem = bgfx.bgfx_copy(rgba, atlasWidth * atlasHeight * 4)
    local tex = bgfx.bgfx_create_texture_2d(atlasWidth, atlasHeight, false, 1, bgfx.BGFX_TEXTURE_FORMAT_RGBA8, 0, mem)

    local fontInfo = ffi.new("stbtt_fontinfo[1]")
    assert(stbtt.InitFont(fontInfo, fontData, 0) ~= 0, "InitFont failed")
    local scale = stbtt.ScaleForPixelHeight(fontInfo, font_size)

    -- Create the final font object
    local font = {
        id = font_id,
        size = font_size,
        width = atlasWidth,
        height = atlasHeight,
        texture = tex,
        data = fontData,
        info = fontInfo,
        scale = scale,
        glyphs = {glyphs},
        glyphs_first = {firstChar},
        glyphs_count = {numChars},
        -- UTF-8 Dynamic glph atlas
        pack_context = packContext,
        atlas_pixels = atlasPixels,
        atlas_dirty = false,
        next_x = 0,
        next_y = 0,
        row_height = 0,
        rgba = rgba
    }

    font.space_advance = getGlyphAdvance(font, 32)
    font.tab_advance = font.space_advance * 4

    font_cache[font_id][font_size] = font
    return font
end

--[[
	Persistent cache table for measureText
	For unbounded dynamic text, like a live chat feed, terminal emulator, or anything that constantly generates new strings
	caching could grow indefinitely, so you’d just clear it periodically.
--]]
local measure_cache = {}

function M.measureText(text, config, skip_tags)
    local font_id = config.fontId
    local font_size = config.fontSize
    local key = string.format("%d:%d:%s", font_id, font_size, text)

    -- Cache lookup
    local cached = measure_cache[key]
    if cached then
        return cached[1], cached[2]
    end

    -- Strip markup tags (Clay’s internal text format)
    local clean = nil
    if skip_tags then
        clean = text
    else
        clean =
            text:gsub("%[color%s*=%s*#?%x%x%x%x%x%x%]", ""):gsub("%[/color%]", ""):gsub("%[/?[bi]%]", ""):gsub(
            "%[/?strike%]",
            ""
        )
    end

    local font = M.load(font_id, font_size)
    if not font then
        return 0, 0
    end

    local xpos = ffi.new("float[1]", 0)
    local ypos = ffi.new("float[1]", 0)
    local quad = ffi.new("stbtt_aligned_quad[1]")

    local width = 0
    local minY, maxY = math.huge, -math.huge
    local first = font.first_char

    for c in utf8_iter_fast(clean) do
        if c == 9 then
            -- Tab
            local rel = xpos[0]
            xpos[0] = math.floor(rel / font.tab_advance + 1) * font.tab_advance
        elseif c == 32 then
            -- Space
            xpos[0] = xpos[0] + font.space_advance
        else
            -- Lookup existing or lazily pack
            local pack, base = findPackedGlyph(font, c)
            if not pack then
                if packGlyph(font, c) then
                    pack, base = findPackedGlyph(font, c)
                end
            end

            if pack then
                stbtt.GetPackedQuad(pack, font.width, font.height, c - base, xpos, ypos, quad, 0)

                local q = quad[0]
                if q.x1 > width then
                    width = q.x1
                end
                if q.y0 < minY then
                    minY = q.y0
                end
                if q.y1 > maxY then
                    maxY = q.y1
                end
            else
                -- Fallback: invisible glyph, advance by width
                xpos[0] = xpos[0] + getGlyphAdvance(font, c)
            end
        end
    end

    local height
    if maxY > minY and maxY < math.huge and minY > -math.huge then
        height = maxY - minY
    else
        height = font.size
    end

    -- Clamp width to pen width
    if xpos[0] > width then
        width = xpos[0]
    end

    measure_cache[key] = {width, height}
    return width, height
end

-- TODO: Add support for other tags, only [color=#hex][/color] implemented
local function parseRichText(text, r, g, b, a)
    local segments = {}
    local colorStack = {{r = r, g = g, b = b, a = a}}
    local bold, italic, strike = false, false, false

    local function currentColor()
        return colorStack[#colorStack]
    end

    -- Split input into tags ([...]) and plain text
    -- Keeps the delimiters separate, preserves all whitespace.
    for token in text:gmatch("(%b[])") do
    end -- warm-up (ensures balanced matching works)
    local parts = {}
    local lastEnd = 1
    for startPos, tag, endPos in text:gmatch("()%[([^%]]-)%]()") do
        if startPos > lastEnd then
            table.insert(parts, {type = "text", value = text:sub(lastEnd, startPos - 1)})
        end
        table.insert(parts, {type = "tag", value = tag})
        lastEnd = endPos
    end
    if lastEnd <= #text then
        table.insert(parts, {type = "text", value = text:sub(lastEnd)})
    end

    -- Walk the tokens
    for _, part in ipairs(parts) do
        if part.type == "text" then
            -- Always preserve whitespace exactly as is
            if #part.value > 0 then
                table.insert(
                    segments,
                    {
                        text = part.value,
                        color = {r = currentColor().r, g = currentColor().g, b = currentColor().b, a = currentColor().a},
                        bold = bold,
                        italic = italic,
                        strike = strike
                    }
                )
            end
        elseif part.type == "tag" then
            local cmd, value = part.value:match("([^=]+)=?(.*)")
            cmd = cmd and cmd:lower() or ""

            if cmd == "color" then
                local hex = value:gsub("#", "")
                local rr = tonumber(hex:sub(1, 2), 16) or currentColor().r
                local gg = tonumber(hex:sub(3, 4), 16) or currentColor().g
                local bb = tonumber(hex:sub(5, 6), 16) or currentColor().b
                table.insert(colorStack, {r = rr, g = gg, b = bb, a = 255})
            elseif cmd == "/color" then
                if #colorStack > 1 then
                    table.remove(colorStack)
                end
            elseif cmd == "b" then
                bold = true
            elseif cmd == "/b" then
                bold = false
            elseif cmd == "i" then
                italic = true
            elseif cmd == "/i" then
                italic = false
            elseif cmd == "strike" then
                strike = true
            elseif cmd == "/strike" then
                strike = false
            end
        end
    end

    return segments
end

-- Helper for text vertices using stbtt packed atlas
-- 4 verts / 6 indices per glyph
local function generateTextVerticesRaw(font, text, x, y, r, g, b, a)
    local n = #text
    local vertices = ffi.new("Vertex[?]", n * 4)
    local indices = ffi.new("uint16_t[?]", n * 6)

    r = math.floor(math.min(math.max(r or 255, 0), 255))
    g = math.floor(math.min(math.max(g or 255, 0), 255))
    b = math.floor(math.min(math.max(b or 255, 0), 255))
    a = math.floor(math.min(math.max(a or 255, 0), 255))

    -- baseline setup
    local ascent = ffi.new("int[1]")
    local descent = ffi.new("int[1]")
    local lineGap = ffi.new("int[1]")
    stbtt.GetFontVMetrics(font.info, ascent, descent, lineGap)
    local baseline = y + ascent[0] * font.scale

    local xpos = ffi.new("float[1]", x)
    local ypos = ffi.new("float[1]", baseline)
    local quad = ffi.new("stbtt_aligned_quad[1]")

    local first_char = font.first_char
    local vcount, icount = 0, 0

    for c in utf8_iter_fast(text) do
        if c == 9 then
            -- tab
            local rel = xpos[0] - x
            xpos[0] = math.floor(rel / font.tab_advance + 1) * font.tab_advance + x
        elseif c == 32 then
            -- space
            xpos[0] = xpos[0] + font.space_advance
        else
            -- find existing pack or lazily load a new one
            local pack, base = findPackedGlyph(font, c)
            if not pack then
                if packGlyph(font, c) then
                    pack, base = findPackedGlyph(font, c)
                end
            end

            -- if we still don’t have a pack, just advance and skip
            if not pack then
                xpos[0] = xpos[0] + getGlyphAdvance(font, c)
            else
                -- valid packedchar, safe to render
                stbtt.GetPackedQuad(pack, font.width, font.height, c - base, xpos, ypos, quad, 0)

                local q = quad[0]
                local vbase = vcount

                vertices[vbase + 0] = {q.x0, q.y0, q.s0, q.t0, r, g, b, a}
                vertices[vbase + 1] = {q.x1, q.y0, q.s1, q.t0, r, g, b, a}
                vertices[vbase + 2] = {q.x1, q.y1, q.s1, q.t1, r, g, b, a}
                vertices[vbase + 3] = {q.x0, q.y1, q.s0, q.t1, r, g, b, a}

                indices[icount + 0] = vbase + 0
                indices[icount + 1] = vbase + 1
                indices[icount + 2] = vbase + 2
                indices[icount + 3] = vbase + 0
                indices[icount + 4] = vbase + 2
                indices[icount + 5] = vbase + 3

                vcount = vcount + 4
                icount = icount + 6
            end
        end
    end

    return vertices, indices, vcount, icount, xpos[0]
end

function M.generateTextVertices(font, text, x, y, r, g, b, a)
    local segments = parseRichText(text, r, g, b, a)
    local verts, inds = {}, {}
    local cursorX = x

    local totalVcount = 0
    local totalIcount = 0

    for _, seg in ipairs(segments) do
        -- Generate raw vertices and indices for this segment
        local segVerts, segInds, vcount, icount, xEnd =
            generateTextVerticesRaw(font, seg.text, cursorX, y, seg.color.r, seg.color.g, seg.color.b, seg.color.a)

        -- Append vertices
        for i = 0, vcount - 1 do
            table.insert(verts, segVerts[i])
        end

        -- Append indices with offset
        for i = 0, icount - 1 do
            table.insert(inds, segInds[i] + totalVcount)
        end

        -- Advance cursor based on actual pen position delta
        cursorX = xEnd

        -- Update totals
        totalVcount = totalVcount + vcount
        totalIcount = totalIcount + icount
    end

    -- Convert to FFI arrays
    local vbuffer = ffi.new("Vertex[?]", totalVcount)
    local ibuffer = ffi.new("uint16_t[?]", totalIcount)

    for i = 1, totalVcount do
        vbuffer[i - 1] = verts[i]
    end
    for i = 1, totalIcount do
        ibuffer[i - 1] = inds[i]
    end

    return vbuffer, ibuffer, totalVcount, totalIcount
end

return M

local ffi = require 'ffi'

-- Define C structures and functions from stb_truetype.h
ffi.cdef[[
typedef struct stbtt__buf stbtt__buf;
typedef struct stbtt_pack_context stbtt_pack_context;
typedef struct stbrp_rect stbrp_rect;

struct stbtt_pack_context {
   void *user_allocator_context;
   void *pack_info;
   int   width;
   int   height;
   int   stride_in_bytes;
   int   padding;
   int   skip_missing;
   unsigned int   h_oversample, v_oversample;
   unsigned char *pixels;
   void  *nodes;
} stbtt_pack_context;

typedef struct {
    unsigned char *data;
    int fontstart;
    int numGlyphs;
    int loca, head, glyf, hhea, hmtx, kern, gpos, svg;
    int index_map;
    int indexToLocFormat;
    void *userdata;      // For custom allocation
    stbtt__buf *cff;     // pointers instead of structs
    stbtt__buf *charstrings;
    stbtt__buf *gsubrs;
    stbtt__buf *subrs;
    stbtt__buf *fontdicts;
    stbtt__buf *fdselect;
} stbtt_fontinfo;

typedef struct {
    short x, y, cx, cy, cx1, cy1;
    unsigned char type;
    unsigned char padding;
} stbtt_vertex;

typedef struct {
    unsigned short x0, y0, x1, y1;
    float xoff, yoff, xadvance;
} stbtt_bakedchar;

typedef struct {
    float x0, y0, s0, t0;
    float x1, y1, s1, t1;
} stbtt_aligned_quad;

typedef struct {
    unsigned short x0, y0, x1, y1;
    float xoff, yoff, xadvance;
    float xoff2, yoff2;
} stbtt_packedchar;

typedef struct stbtt_pack_context stbtt_pack_context;  // Opaque
typedef struct stbtt_pack_range {
    float font_size;
    int first_unicode_codepoint_in_range;
    int *array_of_unicode_codepoints;
    int num_chars;
    stbtt_packedchar *chardata_for_range;
    unsigned char h_oversample, v_oversample;
} stbtt_pack_range;

// Functions
int stbtt_GetNumberOfFonts(const unsigned char *data);
int stbtt_GetFontOffsetForIndex(const unsigned char *data, int index);
int stbtt_InitFont(stbtt_fontinfo *info, const unsigned char *data, int offset);
int stbtt_FindGlyphIndex(const stbtt_fontinfo *info, int unicode_codepoint);
float stbtt_ScaleForPixelHeight(const stbtt_fontinfo *info, float pixels);
float stbtt_ScaleForMappingEmToPixels(const stbtt_fontinfo *info, float pixels);
void stbtt_GetFontVMetrics(const stbtt_fontinfo *info, int *ascent, int *descent, int *lineGap);
int stbtt_GetFontVMetricsOS2(const stbtt_fontinfo *info, int *typoAscent, int *typoDescent, int *typoLineGap);
void stbtt_GetFontBoundingBox(const stbtt_fontinfo *info, int *x0, int *y0, int *x1, int *y1);
void stbtt_GetCodepointHMetrics(const stbtt_fontinfo *info, int codepoint, int *advanceWidth, int *leftSideBearing);
int stbtt_GetCodepointKernAdvance(const stbtt_fontinfo *info, int ch1, int ch2);
int stbtt_GetCodepointBox(const stbtt_fontinfo *info, int codepoint, int *x0, int *y0, int *x1, int *y1);
void stbtt_GetGlyphHMetrics(const stbtt_fontinfo *info, int glyph_index, int *advanceWidth, int *leftSideBearing);
int stbtt_GetGlyphKernAdvance(const stbtt_fontinfo *info, int glyph1, int glyph2);
int stbtt_GetGlyphBox(const stbtt_fontinfo *info, int glyph_index, int *x0, int *y0, int *x1, int *y1);
int stbtt_IsGlyphEmpty(const stbtt_fontinfo *info, int glyph_index);
int stbtt_GetCodepointShape(const stbtt_fontinfo *info, int unicode_codepoint, stbtt_vertex **vertices);
int stbtt_GetGlyphShape(const stbtt_fontinfo *info, int glyph_index, stbtt_vertex **vertices);
void stbtt_FreeShape(const stbtt_fontinfo *info, stbtt_vertex *vertices);
unsigned char *stbtt_FindSVGDoc(const stbtt_fontinfo *info, int gl);
int stbtt_GetCodepointSVG(const stbtt_fontinfo *info, int unicode_codepoint, const char **svg);
int stbtt_GetGlyphSVG(const stbtt_fontinfo *info, int gl, const char **svg);
void stbtt_FreeBitmap(unsigned char *bitmap, void *userdata);
unsigned char *stbtt_GetCodepointBitmap(const stbtt_fontinfo *info, float scale_x, float scale_y, int codepoint, int *width, int *height, int *xoff, int *yoff);
unsigned char *stbtt_GetCodepointBitmapSubpixel(const stbtt_fontinfo *info, float scale_x, float scale_y, float shift_x, float shift_y, int codepoint, int *width, int *height, int *xoff, int *yoff);
void stbtt_MakeCodepointBitmap(const stbtt_fontinfo *info, unsigned char *output, int out_w, int out_h, int out_stride, float scale_x, float scale_y, int codepoint);
void stbtt_MakeCodepointBitmapSubpixel(const stbtt_fontinfo *info, unsigned char *output, int out_w, int out_h, int out_stride, float scale_x, float scale_y, float shift_x, float shift_y, int codepoint);
void stbtt_GetCodepointBitmapBox(const stbtt_fontinfo *info, int codepoint, float scale_x, float scale_y, int *ix0, int *iy0, int *ix1, int *iy1);
void stbtt_GetCodepointBitmapBoxSubpixel(const stbtt_fontinfo *info, int codepoint, float scale_x, float scale_y, float shift_x, float shift_y, int *ix0, int *iy0, int *ix1, int *iy1);
unsigned char *stbtt_GetGlyphBitmap(const stbtt_fontinfo *info, float scale_x, float scale_y, int glyph, int *width, int *height, int *xoff, int *yoff);
unsigned char *stbtt_GetGlyphBitmapSubpixel(const stbtt_fontinfo *info, float scale_x, float scale_y, float shift_x, float shift_y, int glyph, int *width, int *height, int *xoff, int *yoff);
void stbtt_MakeGlyphBitmap(const stbtt_fontinfo *info, unsigned char *output, int out_w, int out_h, int out_stride, float scale_x, float scale_y, int glyph);
void stbtt_MakeGlyphBitmapSubpixel(const stbtt_fontinfo *info, unsigned char *output, int out_w, int out_h, int out_stride, float scale_x, float scale_y, float shift_x, float shift_y, int glyph);
void stbtt_GetGlyphBitmapBox(const stbtt_fontinfo *info, int glyph, float scale_x, float scale_y, int *ix0, int *iy0, int *ix1, int *iy1);
void stbtt_GetGlyphBitmapBoxSubpixel(const stbtt_fontinfo *info, int glyph, float scale_x, float scale_y, float shift_x, float shift_y, int *ix0, int *iy0, int *ix1, int *iy1);
unsigned char *stbtt_GetCodepointSDF(const stbtt_fontinfo *info, float scale, int codepoint, int padding, unsigned char onedge_value, float pixel_dist_scale, int *width, int *height, int *xoff, int *yoff);
unsigned char *stbtt_GetGlyphSDF(const stbtt_fontinfo *info, float scale, int glyph, int padding, unsigned char onedge_value, float pixel_dist_scale, int *width, int *height, int *xoff, int *yoff);
void stbtt_FreeSDF(unsigned char *bitmap, void *userdata);
int stbtt_PackBegin(stbtt_pack_context *spc, unsigned char *pixels, int width, int height, int stride_in_bytes, int padding, void *alloc_context);
void stbtt_PackEnd(stbtt_pack_context *spc);
void stbtt_PackSetOversampling(stbtt_pack_context *spc, unsigned int h_oversample, unsigned int v_oversample);
void stbtt_PackSetSkipMissingCodepoints(stbtt_pack_context *spc, int skip);
void stbtt_GetPackedQuad(const stbtt_packedchar *chardata, int pw, int ph, int char_index, float *xpos, float *ypos, stbtt_aligned_quad *q, int align_to_integer);
int stbtt_PackFontRangesGatherRects(stbtt_pack_context *spc, const stbtt_fontinfo *info, stbtt_pack_range *ranges, int num_ranges, stbrp_rect *rects);
void stbtt_PackFontRangesPackRects(stbtt_pack_context *spc, stbrp_rect *rects, int num_rects);
int stbtt_PackFontRangesRenderIntoRects(stbtt_pack_context *spc, const stbtt_fontinfo *info, stbtt_pack_range *ranges, int num_ranges, stbrp_rect *rects);
int stbtt_PackFontRanges(stbtt_pack_context *spc, const unsigned char *fontdata, int font_index, stbtt_pack_range *ranges, int num_ranges);
int stbtt_PackFontRange(stbtt_pack_context *spc, const unsigned char *fontdata, int font_index, float font_size, int first_unicode_char_in_range, int num_chars_in_range, stbtt_packedchar *chardata_for_range);
void stbtt_GetScaledFontVMetrics(const unsigned char *fontdata, int index, float size, float *ascent, float *descent, float *lineGap);
const char *stbtt_GetFontNameString(const stbtt_fontinfo *font, int *length, int platformID, int encodingID, int languageID, int nameID);
]]

local function ffi_tryload(name)
    local ok, lib = pcall(ffi.load, name)
    if ok then return lib end

    local os = ffi.os
    local ext = (os == "Windows") and ".dll" or (os == "OSX" and ".dylib" or ".so")
    local path = "./bin/" .. name .. ext
    return ffi.load(path)
end

local stbi = {}
local lib = ffi_tryload("libstb_truetype")  -- Adjust the library name if needed
stbi = setmetatable({}, {
    __index = function(t, k)
        return lib['stbtt_' .. k]
    end,
})

return stbi

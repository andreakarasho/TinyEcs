local ffi = require 'ffi'

ffi.cdef[[
typedef struct {
    int id;
    int w, h;
    int x, y;
    int was_packed;
} stbrp_rect;

typedef struct {
    int x, y;
    struct stbrp_node* next;
} stbrp_node;

typedef struct {
    int width, height;
    int align;
    int init_mode;
    int heuristic;
    int num_nodes;
    stbrp_node* active_head;
    stbrp_node* free_head;
    stbrp_node extra[2];
} stbrp_context;

void stbrp_init_target(stbrp_context* context, int width, int height, stbrp_node* nodes, int num_nodes);
void stbrp_pack_rects(stbrp_context* context, stbrp_rect* rects, int num_rects);
void stbrp_setup_allow_out_of_mem(stbrp_context* context, int allow_out_of_mem);
void stbrp_setup_heuristic(stbrp_context* context, int heuristic);
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
local lib = ffi_tryload("libstb_rect_pack")
stbi = setmetatable({}, {
    __index = function(t, k)
        return lib['stbrp_'..k]
    end,
})

function stbi.pack_rectangles(rect_sizes, max_w, max_h)
    local num = #rect_sizes
    local rects = ffi.new("stbrp_rect[?]", num)
    local nodes = ffi.new("stbrp_node[?]", max_w)
    local ctx = ffi.new("stbrp_context")

    for i, r in ipairs(rect_sizes) do
        rects[i - 1].id = i - 1
        rects[i - 1].w = r[1]
        rects[i - 1].h = r[2]
    end

    stbi.init_target(ctx, max_w, max_h, nodes, max_w)
    stbi.pack_rects(ctx, rects, num)

    local results = {}
    for i = 0, num - 1 do
        results[i + 1] = {
            id = tonumber(rects[i].id),
            x = tonumber(rects[i].x),
            y = tonumber(rects[i].y),
            w = tonumber(rects[i].w),
            h = tonumber(rects[i].h),
            was_packed = rects[i].was_packed ~= 0
        }
    end

    return results
end

return stbi

local ffi = require 'ffi'

ffi.cdef[[
typedef unsigned char stbi_uc;

// ======= Image Reading =======
stbi_uc *stbi_load(const char *filename, int *x, int *y, int *comp, int req_comp);
stbi_uc *stbi_load_from_memory(const stbi_uc *buffer, int len, int *x, int *y, int *comp, int req_comp);
const char *stbi_failure_reason(void);
void stbi_image_free(void *retval_from_stbi_load);
int stbi_info_from_memory(const stbi_uc *buffer, int len, int *x, int *y, int *comp);
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
local lib = ffi_tryload("libstb_image")
stbi = setmetatable({}, {
    __index = function(t, k)
        return lib['stbi_'..k]
    end,
})

return stbi

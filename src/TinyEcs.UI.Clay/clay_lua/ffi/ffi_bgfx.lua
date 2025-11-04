local ffi = require("ffi")
local bit = require("bit")
ffi.cdef([[
  typedef enum bgfx_fatal {
    BGFX_FATAL_DEBUG_CHECK,
    BGFX_FATAL_INVALID_SHADER,
    BGFX_FATAL_UNABLE_TO_INITIALIZE,
    BGFX_FATAL_UNABLE_TO_CREATE_TEXTURE,
    BGFX_FATAL_DEVICE_LOST,
    BGFX_FATAL_COUNT
  } bgfx_fatal_t;
  
  typedef enum bgfx_renderer_type {
    BGFX_RENDERER_TYPE_NOOP,
    BGFX_RENDERER_TYPE_AGC,
    BGFX_RENDERER_TYPE_DIRECT3D11,
    BGFX_RENDERER_TYPE_DIRECT3D12,
    BGFX_RENDERER_TYPE_GNM,
    BGFX_RENDERER_TYPE_METAL,
    BGFX_RENDERER_TYPE_NVN,
    BGFX_RENDERER_TYPE_OPENGLES,
    BGFX_RENDERER_TYPE_OPENGL,
    BGFX_RENDERER_TYPE_VULKAN,
    BGFX_RENDERER_TYPE_COUNT
  } bgfx_renderer_type_t;
  
  typedef enum bgfx_access {
    BGFX_ACCESS_READ,
    BGFX_ACCESS_WRITE,
    BGFX_ACCESS_READWRITE,
    BGFX_ACCESS_COUNT
  } bgfx_access_t;
  
  typedef enum bgfx_attrib {
    BGFX_ATTRIB_POSITION,
    BGFX_ATTRIB_NORMAL,
    BGFX_ATTRIB_TANGENT,
    BGFX_ATTRIB_BITANGENT,
    BGFX_ATTRIB_COLOR0,
    BGFX_ATTRIB_COLOR1,
    BGFX_ATTRIB_COLOR2,
    BGFX_ATTRIB_COLOR3,
    BGFX_ATTRIB_INDICES,
    BGFX_ATTRIB_WEIGHT,
    BGFX_ATTRIB_TEXCOORD0,
    BGFX_ATTRIB_TEXCOORD1,
    BGFX_ATTRIB_TEXCOORD2,
    BGFX_ATTRIB_TEXCOORD3,
    BGFX_ATTRIB_TEXCOORD4,
    BGFX_ATTRIB_TEXCOORD5,
    BGFX_ATTRIB_TEXCOORD6,
    BGFX_ATTRIB_TEXCOORD7,
    BGFX_ATTRIB_COUNT
  } bgfx_attrib_t;
  
  typedef enum bgfx_attrib_type {
    BGFX_ATTRIB_TYPE_UINT8,
    BGFX_ATTRIB_TYPE_UINT10,
    BGFX_ATTRIB_TYPE_INT16,
    BGFX_ATTRIB_TYPE_HALF,
    BGFX_ATTRIB_TYPE_FLOAT,
    BGFX_ATTRIB_TYPE_COUNT
  } bgfx_attrib_type_t;
  
  typedef enum bgfx_texture_format {
    BGFX_TEXTURE_FORMAT_BC1,
    BGFX_TEXTURE_FORMAT_BC2,
    BGFX_TEXTURE_FORMAT_BC3,
    BGFX_TEXTURE_FORMAT_BC4,
    BGFX_TEXTURE_FORMAT_BC5,
    BGFX_TEXTURE_FORMAT_BC6H,
    BGFX_TEXTURE_FORMAT_BC7,
    BGFX_TEXTURE_FORMAT_ETC1,
    BGFX_TEXTURE_FORMAT_ETC2,
    BGFX_TEXTURE_FORMAT_ETC2A,
    BGFX_TEXTURE_FORMAT_ETC2A1,
    BGFX_TEXTURE_FORMAT_PTC12,
    BGFX_TEXTURE_FORMAT_PTC14,
    BGFX_TEXTURE_FORMAT_PTC12A,
    BGFX_TEXTURE_FORMAT_PTC14A,
    BGFX_TEXTURE_FORMAT_PTC22,
    BGFX_TEXTURE_FORMAT_PTC24,
    BGFX_TEXTURE_FORMAT_ATC,
    BGFX_TEXTURE_FORMAT_ATCE,
    BGFX_TEXTURE_FORMAT_ATCI,
    BGFX_TEXTURE_FORMAT_ASTC4X4,
    BGFX_TEXTURE_FORMAT_ASTC5X4,
    BGFX_TEXTURE_FORMAT_ASTC5X5,
    BGFX_TEXTURE_FORMAT_ASTC6X5,
    BGFX_TEXTURE_FORMAT_ASTC6X6,
    BGFX_TEXTURE_FORMAT_ASTC8X5,
    BGFX_TEXTURE_FORMAT_ASTC8X6,
    BGFX_TEXTURE_FORMAT_ASTC8X8,
    BGFX_TEXTURE_FORMAT_ASTC10X5,
    BGFX_TEXTURE_FORMAT_ASTC10X6,
    BGFX_TEXTURE_FORMAT_ASTC10X8,
    BGFX_TEXTURE_FORMAT_ASTC10X10,
    BGFX_TEXTURE_FORMAT_ASTC12X10,
    BGFX_TEXTURE_FORMAT_ASTC12X12,
    BGFX_TEXTURE_FORMAT_UNKNOWN,
    BGFX_TEXTURE_FORMAT_R1,
    BGFX_TEXTURE_FORMAT_A8,
    BGFX_TEXTURE_FORMAT_R8,
    BGFX_TEXTURE_FORMAT_R8I,
    BGFX_TEXTURE_FORMAT_R8U,
    BGFX_TEXTURE_FORMAT_R8S,
    BGFX_TEXTURE_FORMAT_R16,
    BGFX_TEXTURE_FORMAT_R16I,
    BGFX_TEXTURE_FORMAT_R16U,
    BGFX_TEXTURE_FORMAT_R16F,
    BGFX_TEXTURE_FORMAT_R16S,
    BGFX_TEXTURE_FORMAT_R32I,
    BGFX_TEXTURE_FORMAT_R32U,
    BGFX_TEXTURE_FORMAT_R32F,
    BGFX_TEXTURE_FORMAT_RG8,
    BGFX_TEXTURE_FORMAT_RG8I,
    BGFX_TEXTURE_FORMAT_RG8U,
    BGFX_TEXTURE_FORMAT_RG8S,
    BGFX_TEXTURE_FORMAT_RG16,
    BGFX_TEXTURE_FORMAT_RG16I,
    BGFX_TEXTURE_FORMAT_RG16U,
    BGFX_TEXTURE_FORMAT_RG16F,
    BGFX_TEXTURE_FORMAT_RG16S,
    BGFX_TEXTURE_FORMAT_RG32I,
    BGFX_TEXTURE_FORMAT_RG32U,
    BGFX_TEXTURE_FORMAT_RG32F,
    BGFX_TEXTURE_FORMAT_RGB8,
    BGFX_TEXTURE_FORMAT_RGB8I,
    BGFX_TEXTURE_FORMAT_RGB8U,
    BGFX_TEXTURE_FORMAT_RGB8S,
    BGFX_TEXTURE_FORMAT_RGB9E5F,
    BGFX_TEXTURE_FORMAT_BGRA8,
    BGFX_TEXTURE_FORMAT_RGBA8,
    BGFX_TEXTURE_FORMAT_RGBA8I,
    BGFX_TEXTURE_FORMAT_RGBA8U,
    BGFX_TEXTURE_FORMAT_RGBA8S,
    BGFX_TEXTURE_FORMAT_RGBA16,
    BGFX_TEXTURE_FORMAT_RGBA16I,
    BGFX_TEXTURE_FORMAT_RGBA16U,
    BGFX_TEXTURE_FORMAT_RGBA16F,
    BGFX_TEXTURE_FORMAT_RGBA16S,
    BGFX_TEXTURE_FORMAT_RGBA32I,
    BGFX_TEXTURE_FORMAT_RGBA32U,
    BGFX_TEXTURE_FORMAT_RGBA32F,
    BGFX_TEXTURE_FORMAT_B5G6R5,
    BGFX_TEXTURE_FORMAT_R5G6B5,
    BGFX_TEXTURE_FORMAT_BGRA4,
    BGFX_TEXTURE_FORMAT_RGBA4,
    BGFX_TEXTURE_FORMAT_BGR5A1,
    BGFX_TEXTURE_FORMAT_RGB5A1,
    BGFX_TEXTURE_FORMAT_RGB10A2,
    BGFX_TEXTURE_FORMAT_RG11B10F,
    BGFX_TEXTURE_FORMAT_UNKNOWNDEPTH,
    BGFX_TEXTURE_FORMAT_D16,
    BGFX_TEXTURE_FORMAT_D24,
    BGFX_TEXTURE_FORMAT_D24S8,
    BGFX_TEXTURE_FORMAT_D32,
    BGFX_TEXTURE_FORMAT_D16F,
    BGFX_TEXTURE_FORMAT_D24F,
    BGFX_TEXTURE_FORMAT_D32F,
    BGFX_TEXTURE_FORMAT_D0S8,
    BGFX_TEXTURE_FORMAT_COUNT
  } bgfx_texture_format_t;
  
  typedef enum bgfx_uniform_type {
    BGFX_UNIFORM_TYPE_SAMPLER,
    BGFX_UNIFORM_TYPE_END,
    BGFX_UNIFORM_TYPE_VEC4,
    BGFX_UNIFORM_TYPE_MAT3,
    BGFX_UNIFORM_TYPE_MAT4,
    BGFX_UNIFORM_TYPE_COUNT
  } bgfx_uniform_type_t;
  
  typedef enum bgfx_backbuffer_ratio {
    BGFX_BACKBUFFER_RATIO_EQUAL,
    BGFX_BACKBUFFER_RATIO_HALF,
    BGFX_BACKBUFFER_RATIO_QUARTER,
    BGFX_BACKBUFFER_RATIO_EIGHTH,
    BGFX_BACKBUFFER_RATIO_SIXTEENTH,
    BGFX_BACKBUFFER_RATIO_DOUBLE,
    BGFX_BACKBUFFER_RATIO_COUNT
  } bgfx_backbuffer_ratio_t;
  
  typedef enum bgfx_occlusion_query_result {
    BGFX_OCCLUSION_QUERY_RESULT_INVISIBLE,
    BGFX_OCCLUSION_QUERY_RESULT_VISIBLE,
    BGFX_OCCLUSION_QUERY_RESULT_NORESULT,
    BGFX_OCCLUSION_QUERY_RESULT_COUNT
  } bgfx_occlusion_query_result_t;
  
  typedef enum bgfx_topology {
    BGFX_TOPOLOGY_TRI_LIST,
    BGFX_TOPOLOGY_TRI_STRIP,
    BGFX_TOPOLOGY_LINE_LIST,
    BGFX_TOPOLOGY_LINE_STRIP,
    BGFX_TOPOLOGY_POINT_LIST,
    BGFX_TOPOLOGY_COUNT
  } bgfx_topology_t;
  
  typedef enum bgfx_topology_convert {
    BGFX_TOPOLOGY_CONVERT_TRI_LIST_FLIP_WINDING,
    BGFX_TOPOLOGY_CONVERT_TRI_STRIP_FLIP_WINDING,
    BGFX_TOPOLOGY_CONVERT_TRI_LIST_TO_LINE_LIST,
    BGFX_TOPOLOGY_CONVERT_TRI_STRIP_TO_TRI_LIST,
    BGFX_TOPOLOGY_CONVERT_LINE_STRIP_TO_LINE_LIST,
    BGFX_TOPOLOGY_CONVERT_COUNT
  } bgfx_topology_convert_t;
  
  typedef enum bgfx_topology_sort {
    BGFX_TOPOLOGY_SORT_DIRECTION_FRONT_TO_BACK_MIN,
    BGFX_TOPOLOGY_SORT_DIRECTION_FRONT_TO_BACK_AVG,
    BGFX_TOPOLOGY_SORT_DIRECTION_FRONT_TO_BACK_MAX,
    BGFX_TOPOLOGY_SORT_DIRECTION_BACK_TO_FRONT_MIN,
    BGFX_TOPOLOGY_SORT_DIRECTION_BACK_TO_FRONT_AVG,
    BGFX_TOPOLOGY_SORT_DIRECTION_BACK_TO_FRONT_MAX,
    BGFX_TOPOLOGY_SORT_DISTANCE_FRONT_TO_BACK_MIN,
    BGFX_TOPOLOGY_SORT_DISTANCE_FRONT_TO_BACK_AVG,
    BGFX_TOPOLOGY_SORT_DISTANCE_FRONT_TO_BACK_MAX,
    BGFX_TOPOLOGY_SORT_DISTANCE_BACK_TO_FRONT_MIN,
    BGFX_TOPOLOGY_SORT_DISTANCE_BACK_TO_FRONT_AVG,
    BGFX_TOPOLOGY_SORT_DISTANCE_BACK_TO_FRONT_MAX,
    BGFX_TOPOLOGY_SORT_COUNT
  } bgfx_topology_sort_t;
  
  typedef enum bgfx_view_mode {
    BGFX_VIEW_MODE_DEFAULT,
    BGFX_VIEW_MODE_SEQUENTIAL,
    BGFX_VIEW_MODE_DEPTH_ASCENDING,
    BGFX_VIEW_MODE_DEPTH_DESCENDING,
    BGFX_VIEW_MODE_COUNT
  } bgfx_view_mode_t;
  
  typedef enum bgfx_native_window_handle_type {
    BGFX_NATIVE_WINDOW_HANDLE_TYPE_DEFAULT,
    BGFX_NATIVE_WINDOW_HANDLE_TYPE_WAYLAND,
    BGFX_NATIVE_WINDOW_HANDLE_TYPE_COUNT
  } bgfx_native_window_handle_type_t;
  
  typedef enum bgfx_render_frame {
    BGFX_RENDER_FRAME_NO_CONTEXT,
    BGFX_RENDER_FRAME_RENDER,
    BGFX_RENDER_FRAME_TIMEOUT,
    BGFX_RENDER_FRAME_EXITING,
    BGFX_RENDER_FRAME_COUNT
  } bgfx_render_frame_t;
  
  typedef uint16_t bgfx_view_id_t;
  typedef struct bgfx_allocator_interface_s {
    const struct bgfx_allocator_vtbl_s* vtbl;
  } bgfx_allocator_interface_t;
  
  typedef struct bgfx_allocator_vtbl_s {
    void* (*realloc)(bgfx_allocator_interface_t* _this, void* _ptr, size_t _size, size_t _align, const char* _file, uint32_t _line);
  } bgfx_allocator_vtbl_t;
  
  typedef struct bgfx_interface_vtbl bgfx_interface_vtbl_t;
  typedef struct bgfx_callback_interface_s {
    const struct bgfx_callback_vtbl_s* vtbl;
  } bgfx_callback_interface_t;
  
  typedef struct bgfx_callback_vtbl_s {
    void (*fatal)(bgfx_callback_interface_t* _this, const char* _filePath, uint16_t _line, bgfx_fatal_t _code, const char* _str);
    void (*trace_vargs)(bgfx_callback_interface_t* _this, const char* _filePath, uint16_t _line, const char* _format, va_list _argList);
    void (*profiler_begin)(bgfx_callback_interface_t* _this, const char* _name, uint32_t _abgr, const char* _filePath, uint16_t _line);
    void (*profiler_begin_literal)(bgfx_callback_interface_t* _this, const char* _name, uint32_t _abgr, const char* _filePath, uint16_t _line);
    void (*profiler_end)(bgfx_callback_interface_t* _this);
    uint32_t (*cache_read_size)(bgfx_callback_interface_t* _this, uint64_t _id);
    _Bool (*cache_read)(bgfx_callback_interface_t* _this, uint64_t _id, void* _data, uint32_t _size);
    void (*cache_write)(bgfx_callback_interface_t* _this, uint64_t _id, const void* _data, uint32_t _size);
    void (*screen_shot)(bgfx_callback_interface_t* _this, const char* _filePath, uint32_t _width, uint32_t _height, uint32_t _pitch, const void* _data, uint32_t _size, _Bool _yflip);
    void (*capture_begin)(bgfx_callback_interface_t* _this, uint32_t _width, uint32_t _height, uint32_t _pitch, bgfx_texture_format_t _format, _Bool _yflip);
    void (*capture_end)(bgfx_callback_interface_t* _this);
    void (*capture_frame)(bgfx_callback_interface_t* _this, const void* _data, uint32_t _size);
  } bgfx_callback_vtbl_t;
  
  typedef struct bgfx_dynamic_index_buffer_handle_s { uint16_t idx; } bgfx_dynamic_index_buffer_handle_t;
  typedef struct bgfx_dynamic_vertex_buffer_handle_s { uint16_t idx; } bgfx_dynamic_vertex_buffer_handle_t;
  typedef struct bgfx_frame_buffer_handle_s { uint16_t idx; } bgfx_frame_buffer_handle_t;
  typedef struct bgfx_index_buffer_handle_s { uint16_t idx; } bgfx_index_buffer_handle_t;
  typedef struct bgfx_indirect_buffer_handle_s { uint16_t idx; } bgfx_indirect_buffer_handle_t;
  typedef struct bgfx_occlusion_query_handle_s { uint16_t idx; } bgfx_occlusion_query_handle_t;
  typedef struct bgfx_program_handle_s { uint16_t idx; } bgfx_program_handle_t;
  typedef struct bgfx_shader_handle_s { uint16_t idx; } bgfx_shader_handle_t;
  typedef struct bgfx_texture_handle_s { uint16_t idx; } bgfx_texture_handle_t;
  typedef struct bgfx_uniform_handle_s { uint16_t idx; } bgfx_uniform_handle_t;
  typedef struct bgfx_vertex_buffer_handle_s { uint16_t idx; } bgfx_vertex_buffer_handle_t;
  typedef struct bgfx_vertex_layout_handle_s { uint16_t idx; } bgfx_vertex_layout_handle_t;
  
  typedef void (*bgfx_release_fn_t)(void* _ptr, void* _userData);
  
  typedef struct bgfx_caps_gpu_s {
    uint16_t vendorId;
    uint16_t deviceId;
  } bgfx_caps_gpu_t;
  
  typedef struct bgfx_caps_limits_s {
    uint32_t maxDrawCalls;
    uint32_t maxBlits;
    uint32_t maxTextureSize;
    uint32_t maxTextureLayers;
    uint32_t maxViews;
    uint32_t maxFrameBuffers;
    uint32_t maxFBAttachments;
    uint32_t maxPrograms;
    uint32_t maxShaders;
    uint32_t maxTextures;
    uint32_t maxTextureSamplers;
    uint32_t maxComputeBindings;
    uint32_t maxVertexLayouts;
    uint32_t maxVertexStreams;
    uint32_t maxIndexBuffers;
    uint32_t maxVertexBuffers;
    uint32_t maxDynamicIndexBuffers;
    uint32_t maxDynamicVertexBuffers;
    uint32_t maxUniforms;
    uint32_t maxOcclusionQueries;
    uint32_t maxEncoders;
    uint32_t minResourceCbSize;
    uint32_t transientVbSize;
    uint32_t transientIbSize;
  } bgfx_caps_limits_t;
  
  typedef struct bgfx_caps_s {
    bgfx_renderer_type_t rendererType;
    uint64_t supported;
    uint16_t vendorId;
    uint16_t deviceId;
    _Bool homogeneousDepth;
    _Bool originBottomLeft;
    uint8_t numGPUs;
    bgfx_caps_gpu_t gpu[4];
    bgfx_caps_limits_t limits;
    uint16_t formats[BGFX_TEXTURE_FORMAT_COUNT];
  } bgfx_caps_t;
  
  typedef struct bgfx_internal_data_s {
    const bgfx_caps_t* caps;
    void* context;
  } bgfx_internal_data_t;
  
  typedef struct bgfx_platform_data_s {
    void* ndt;
    void* nwh;
    void* context;
    void* backBuffer;
    void* backBufferDS;
    bgfx_native_window_handle_type_t type;
  } bgfx_platform_data_t;
  
  typedef struct bgfx_resolution_s {
    bgfx_texture_format_t format;
    uint32_t width;
    uint32_t height;
    uint32_t reset;
    uint8_t numBackBuffers;
    uint8_t maxFrameLatency;
    uint8_t debugTextScale;
  } bgfx_resolution_t;
  
  typedef struct bgfx_init_limits_s {
    uint16_t maxEncoders;
    uint32_t minResourceCbSize;
    uint32_t transientVbSize;
    uint32_t transientIbSize;
  } bgfx_init_limits_t;
  
  typedef struct bgfx_init_s {
    bgfx_renderer_type_t type;
    uint16_t vendorId;
    uint16_t deviceId;
    uint64_t capabilities;
    _Bool debug;
    _Bool profile;
    bgfx_platform_data_t platformData;
    bgfx_resolution_t resolution;
    bgfx_init_limits_t limits;
    bgfx_callback_interface_t* callback;
    bgfx_allocator_interface_t* allocator;
  } bgfx_init_t;
  
  typedef struct bgfx_memory_s {
    uint8_t* data;
    uint32_t size;
  } bgfx_memory_t;
  
  typedef struct bgfx_transient_index_buffer_s {
    uint8_t* data;
    uint32_t size;
    uint32_t startIndex;
    bgfx_index_buffer_handle_t handle;
    _Bool isIndex16;
  } bgfx_transient_index_buffer_t;
  
  typedef struct bgfx_transient_vertex_buffer_s {
    uint8_t* data;
    uint32_t size;
    uint32_t startVertex;
    uint16_t stride;
    bgfx_vertex_buffer_handle_t handle;
    bgfx_vertex_layout_handle_t layoutHandle;
  } bgfx_transient_vertex_buffer_t;
  
  typedef struct bgfx_instance_data_buffer_s {
    uint8_t* data;
    uint32_t size;
    uint32_t offset;
    uint32_t num;
    uint16_t stride;
    bgfx_vertex_buffer_handle_t handle;
  } bgfx_instance_data_buffer_t;
  
  typedef struct bgfx_texture_info_s {
    bgfx_texture_format_t format;
    uint32_t storageSize;
    uint16_t width;
    uint16_t height;
    uint16_t depth;
    uint16_t numLayers;
    uint8_t numMips;
    uint8_t bitsPerPixel;
    _Bool cubeMap;
  } bgfx_texture_info_t;
  
  typedef struct bgfx_uniform_info_s {
    char name[256];
    bgfx_uniform_type_t type;
    uint16_t num;
  } bgfx_uniform_info_t;
  
  typedef struct bgfx_attachment_s {
    bgfx_access_t access;
    bgfx_texture_handle_t handle;
    uint16_t mip;
    uint16_t layer;
    uint16_t numLayers;
    uint8_t resolve;
  } bgfx_attachment_t;
  
  typedef struct bgfx_transform_s {
    float* data;
    uint16_t num;
  } bgfx_transform_t;
  
  typedef struct bgfx_view_stats_s {
    char name[256];
    bgfx_view_id_t view;
    int64_t cpuTimeBegin;
    int64_t cpuTimeEnd;
    int64_t gpuTimeBegin;
    int64_t gpuTimeEnd;
    uint32_t gpuFrameNum;
  } bgfx_view_stats_t;
  
  typedef struct bgfx_encoder_stats_s {
    int64_t cpuTimeBegin;
    int64_t cpuTimeEnd;
  } bgfx_encoder_stats_t;
  
  typedef struct bgfx_stats_s {
    int64_t cpuTimeFrame;
    int64_t cpuTimeBegin;
    int64_t cpuTimeEnd;
    int64_t cpuTimerFreq;
    int64_t gpuTimeBegin;
    int64_t gpuTimeEnd;
    int64_t gpuTimerFreq;
    int64_t waitRender;
    int64_t waitSubmit;
    uint32_t numDraw;
    uint32_t numCompute;
    uint32_t numBlit;
    uint32_t maxGpuLatency;
    uint32_t gpuFrameNum;
    uint16_t numDynamicIndexBuffers;
    uint16_t numDynamicVertexBuffers;
    uint16_t numFrameBuffers;
    uint16_t numIndexBuffers;
    uint16_t numOcclusionQueries;
    uint16_t numPrograms;
    uint16_t numShaders;
    uint16_t numTextures;
    uint16_t numUniforms;
    uint16_t numVertexBuffers;
    uint16_t numVertexLayouts;
    int64_t textureMemoryUsed;
    int64_t rtMemoryUsed;
    int32_t transientVbUsed;
    int32_t transientIbUsed;
    uint32_t numPrims[BGFX_TOPOLOGY_COUNT];
    int64_t gpuMemoryMax;
    int64_t gpuMemoryUsed;
    uint16_t width;
    uint16_t height;
    uint16_t textWidth;
    uint16_t textHeight;
    uint16_t numViews;
    bgfx_view_stats_t* viewStats;
    uint8_t numEncoders;
    bgfx_encoder_stats_t* encoderStats;
  } bgfx_stats_t;
  
  typedef struct bgfx_vertex_layout_s {
    uint32_t hash;
    uint16_t stride;
    uint16_t offset[BGFX_ATTRIB_COUNT];
    uint16_t attributes[BGFX_ATTRIB_COUNT];
  } bgfx_vertex_layout_t;
  
  struct bgfx_encoder_s;
  typedef struct bgfx_encoder_s bgfx_encoder_t;
  void bgfx_attachment_init(bgfx_attachment_t* _this, bgfx_texture_handle_t _handle, bgfx_access_t _access, uint16_t _layer, uint16_t _numLayers, uint16_t _mip, uint8_t _resolve);
  bgfx_vertex_layout_t* bgfx_vertex_layout_begin(bgfx_vertex_layout_t* _this, bgfx_renderer_type_t _rendererType);
  bgfx_vertex_layout_t* bgfx_vertex_layout_add(bgfx_vertex_layout_t* _this, bgfx_attrib_t _attrib, uint8_t _num, bgfx_attrib_type_t _type, _Bool _normalized, _Bool _asInt);
  void bgfx_vertex_layout_decode(const bgfx_vertex_layout_t* _this, bgfx_attrib_t _attrib, uint8_t * _num, bgfx_attrib_type_t * _type, _Bool * _normalized, _Bool * _asInt);
  _Bool bgfx_vertex_layout_has(const bgfx_vertex_layout_t* _this, bgfx_attrib_t _attrib);
  bgfx_vertex_layout_t* bgfx_vertex_layout_skip(bgfx_vertex_layout_t* _this, uint8_t _num);
  void bgfx_vertex_layout_end(bgfx_vertex_layout_t* _this);
  uint16_t bgfx_vertex_layout_get_offset(const bgfx_vertex_layout_t* _this, bgfx_attrib_t _attrib);
  uint16_t bgfx_vertex_layout_get_stride(const bgfx_vertex_layout_t* _this);
  uint32_t bgfx_vertex_layout_get_size(const bgfx_vertex_layout_t* _this, uint32_t _num);
  void bgfx_vertex_pack(const float _input[4], _Bool _inputNormalized, bgfx_attrib_t _attr, const bgfx_vertex_layout_t * _layout, void* _data, uint32_t _index);
  void bgfx_vertex_unpack(float _output[4], bgfx_attrib_t _attr, const bgfx_vertex_layout_t * _layout, const void* _data, uint32_t _index);
  void bgfx_vertex_convert(const bgfx_vertex_layout_t * _dstLayout, void* _dstData, const bgfx_vertex_layout_t * _srcLayout, const void* _srcData, uint32_t _num);
  uint32_t bgfx_weld_vertices(void* _output, const bgfx_vertex_layout_t * _layout, const void* _data, uint32_t _num, _Bool _index32, float _epsilon);
  uint32_t bgfx_topology_convert(bgfx_topology_convert_t _conversion, void* _dst, uint32_t _dstSize, const void* _indices, uint32_t _numIndices, _Bool _index32);
  void bgfx_topology_sort_tri_list(bgfx_topology_sort_t _sort, void* _dst, uint32_t _dstSize, const float _dir[3], const float _pos[3], const void* _vertices, uint32_t _stride, const void* _indices, uint32_t _numIndices, _Bool _index32);
  uint8_t bgfx_get_supported_renderers(uint8_t _max, bgfx_renderer_type_t* _enum);
  const char* bgfx_get_renderer_name(bgfx_renderer_type_t _type);
  void bgfx_init_ctor(bgfx_init_t* _init);
  _Bool bgfx_init(const bgfx_init_t * _init);
  void bgfx_shutdown(void);
  void bgfx_reset(uint32_t _width, uint32_t _height, uint32_t _flags, bgfx_texture_format_t _format);
  uint32_t bgfx_frame( _Bool _capture);
  bgfx_renderer_type_t bgfx_get_renderer_type(void);
  const bgfx_caps_t* bgfx_get_caps(void);
  const bgfx_stats_t* bgfx_get_stats(void);
  const bgfx_memory_t* bgfx_alloc(uint32_t _size);
  const bgfx_memory_t* bgfx_copy(const void* _data, uint32_t _size);
  const bgfx_memory_t* bgfx_make_ref(const void* _data, uint32_t _size);
  const bgfx_memory_t* bgfx_make_ref_release(const void* _data, uint32_t _size, bgfx_release_fn_t _releaseFn, void* _userData);
  void bgfx_set_debug(uint32_t _debug);
  void bgfx_dbg_text_clear(uint8_t _attr, _Bool _small);
  void bgfx_dbg_text_printf(uint16_t _x, uint16_t _y, uint8_t _attr, const char* _format, ... );
  void bgfx_dbg_text_vprintf(uint16_t _x, uint16_t _y, uint8_t _attr, const char* _format, va_list _argList);
  void bgfx_dbg_text_image(uint16_t _x, uint16_t _y, uint16_t _width, uint16_t _height, const void* _data, uint16_t _pitch);
  bgfx_index_buffer_handle_t bgfx_create_index_buffer(const bgfx_memory_t* _mem, uint16_t _flags);
  void bgfx_set_index_buffer_name(bgfx_index_buffer_handle_t _handle, const char* _name, int32_t _len);
  void bgfx_destroy_index_buffer(bgfx_index_buffer_handle_t _handle);
  bgfx_vertex_layout_handle_t bgfx_create_vertex_layout(const bgfx_vertex_layout_t * _layout);
  void bgfx_destroy_vertex_layout(bgfx_vertex_layout_handle_t _layoutHandle);
  bgfx_vertex_buffer_handle_t bgfx_create_vertex_buffer(const bgfx_memory_t* _mem, const bgfx_vertex_layout_t * _layout, uint16_t _flags);
  void bgfx_set_vertex_buffer_name(bgfx_vertex_buffer_handle_t _handle, const char* _name, int32_t _len);
  void bgfx_destroy_vertex_buffer(bgfx_vertex_buffer_handle_t _handle);
  bgfx_dynamic_index_buffer_handle_t bgfx_create_dynamic_index_buffer(uint32_t _num, uint16_t _flags);
  bgfx_dynamic_index_buffer_handle_t bgfx_create_dynamic_index_buffer_mem(const bgfx_memory_t* _mem, uint16_t _flags);
  void bgfx_update_dynamic_index_buffer(bgfx_dynamic_index_buffer_handle_t _handle, uint32_t _startIndex, const bgfx_memory_t* _mem);
  void bgfx_destroy_dynamic_index_buffer(bgfx_dynamic_index_buffer_handle_t _handle);
  bgfx_dynamic_vertex_buffer_handle_t bgfx_create_dynamic_vertex_buffer(uint32_t _num, const bgfx_vertex_layout_t* _layout, uint16_t _flags);
  bgfx_dynamic_vertex_buffer_handle_t bgfx_create_dynamic_vertex_buffer_mem(const bgfx_memory_t* _mem, const bgfx_vertex_layout_t* _layout, uint16_t _flags);
  void bgfx_update_dynamic_vertex_buffer(bgfx_dynamic_vertex_buffer_handle_t _handle, uint32_t _startVertex, const bgfx_memory_t* _mem);
  void bgfx_destroy_dynamic_vertex_buffer(bgfx_dynamic_vertex_buffer_handle_t _handle);
  uint32_t bgfx_get_avail_transient_index_buffer(uint32_t _num, _Bool _index32);
  uint32_t bgfx_get_avail_transient_vertex_buffer(uint32_t _num, const bgfx_vertex_layout_t * _layout);
  uint32_t bgfx_get_avail_instance_data_buffer(uint32_t _num, uint16_t _stride);
  void bgfx_alloc_transient_index_buffer(bgfx_transient_index_buffer_t* _tib, uint32_t _num, _Bool _index32);
  void bgfx_alloc_transient_vertex_buffer(bgfx_transient_vertex_buffer_t* _tvb, uint32_t _num, const bgfx_vertex_layout_t * _layout);
  _Bool bgfx_alloc_transient_buffers(bgfx_transient_vertex_buffer_t* _tvb, const bgfx_vertex_layout_t * _layout, uint32_t _numVertices, bgfx_transient_index_buffer_t* _tib, uint32_t _numIndices, _Bool _index32);
  void bgfx_alloc_instance_data_buffer(bgfx_instance_data_buffer_t* _idb, uint32_t _num, uint16_t _stride);
  bgfx_indirect_buffer_handle_t bgfx_create_indirect_buffer(uint32_t _num);
  void bgfx_destroy_indirect_buffer(bgfx_indirect_buffer_handle_t _handle);
  bgfx_shader_handle_t bgfx_create_shader(const bgfx_memory_t* _mem);
  uint16_t bgfx_get_shader_uniforms(bgfx_shader_handle_t _handle, bgfx_uniform_handle_t* _uniforms, uint16_t _max);
  void bgfx_set_shader_name(bgfx_shader_handle_t _handle, const char* _name, int32_t _len);
  void bgfx_destroy_shader(bgfx_shader_handle_t _handle);
  bgfx_program_handle_t bgfx_create_program(bgfx_shader_handle_t _vsh, bgfx_shader_handle_t _fsh, _Bool _destroyShaders);
  bgfx_program_handle_t bgfx_create_compute_program(bgfx_shader_handle_t _csh, _Bool _destroyShaders);
  void bgfx_destroy_program(bgfx_program_handle_t _handle);
  _Bool bgfx_is_texture_valid(uint16_t _depth, _Bool _cubeMap, uint16_t _numLayers, bgfx_texture_format_t _format, uint64_t _flags);
  _Bool bgfx_is_frame_buffer_valid(uint8_t _num, const bgfx_attachment_t* _attachment);
  void bgfx_calc_texture_size(bgfx_texture_info_t * _info, uint16_t _width, uint16_t _height, uint16_t _depth, _Bool _cubeMap, _Bool _hasMips, uint16_t _numLayers, bgfx_texture_format_t _format);
  bgfx_texture_handle_t bgfx_create_texture(const bgfx_memory_t* _mem, uint64_t _flags, uint8_t _skip, bgfx_texture_info_t* _info);
  bgfx_texture_handle_t bgfx_create_texture_2d(uint16_t _width, uint16_t _height, _Bool _hasMips, uint16_t _numLayers, bgfx_texture_format_t _format, uint64_t _flags, const bgfx_memory_t* _mem);
  bgfx_texture_handle_t bgfx_create_texture_2d_scaled(bgfx_backbuffer_ratio_t _ratio, _Bool _hasMips, uint16_t _numLayers, bgfx_texture_format_t _format, uint64_t _flags);
  bgfx_texture_handle_t bgfx_create_texture_3d(uint16_t _width, uint16_t _height, uint16_t _depth, _Bool _hasMips, bgfx_texture_format_t _format, uint64_t _flags, const bgfx_memory_t* _mem);
  bgfx_texture_handle_t bgfx_create_texture_cube(uint16_t _size, _Bool _hasMips, uint16_t _numLayers, bgfx_texture_format_t _format, uint64_t _flags, const bgfx_memory_t* _mem);
  void bgfx_update_texture_2d(bgfx_texture_handle_t _handle, uint16_t _layer, uint8_t _mip, uint16_t _x, uint16_t _y, uint16_t _width, uint16_t _height, const bgfx_memory_t* _mem, uint16_t _pitch);
  void bgfx_update_texture_3d(bgfx_texture_handle_t _handle, uint8_t _mip, uint16_t _x, uint16_t _y, uint16_t _z, uint16_t _width, uint16_t _height, uint16_t _depth, const bgfx_memory_t* _mem);
  void bgfx_update_texture_cube(bgfx_texture_handle_t _handle, uint16_t _layer, uint8_t _side, uint8_t _mip, uint16_t _x, uint16_t _y, uint16_t _width, uint16_t _height, const bgfx_memory_t* _mem, uint16_t _pitch);
  uint32_t bgfx_read_texture(bgfx_texture_handle_t _handle, void* _data, uint8_t _mip);
  void bgfx_set_texture_name(bgfx_texture_handle_t _handle, const char* _name, int32_t _len);
  void* bgfx_get_direct_access_ptr(bgfx_texture_handle_t _handle);
  void bgfx_destroy_texture(bgfx_texture_handle_t _handle);
  bgfx_frame_buffer_handle_t bgfx_create_frame_buffer(uint16_t _width, uint16_t _height, bgfx_texture_format_t _format, uint64_t _textureFlags);
  bgfx_frame_buffer_handle_t bgfx_create_frame_buffer_scaled(bgfx_backbuffer_ratio_t _ratio, bgfx_texture_format_t _format, uint64_t _textureFlags);
  bgfx_frame_buffer_handle_t bgfx_create_frame_buffer_from_handles(uint8_t _num, const bgfx_texture_handle_t* _handles, _Bool _destroyTexture);
  bgfx_frame_buffer_handle_t bgfx_create_frame_buffer_from_attachment(uint8_t _num, const bgfx_attachment_t* _attachment, _Bool _destroyTexture);
  bgfx_frame_buffer_handle_t bgfx_create_frame_buffer_from_nwh(void* _nwh, uint16_t _width, uint16_t _height, bgfx_texture_format_t _format, bgfx_texture_format_t _depthFormat);
  void bgfx_set_frame_buffer_name(bgfx_frame_buffer_handle_t _handle, const char* _name, int32_t _len);
  bgfx_texture_handle_t bgfx_get_texture(bgfx_frame_buffer_handle_t _handle, uint8_t _attachment);
  void bgfx_destroy_frame_buffer(bgfx_frame_buffer_handle_t _handle);
  bgfx_uniform_handle_t bgfx_create_uniform(const char* _name, bgfx_uniform_type_t _type, uint16_t _num);
  void bgfx_get_uniform_info(bgfx_uniform_handle_t _handle, bgfx_uniform_info_t * _info);
  void bgfx_destroy_uniform(bgfx_uniform_handle_t _handle);
  bgfx_occlusion_query_handle_t bgfx_create_occlusion_query(void);
  bgfx_occlusion_query_result_t bgfx_get_result(bgfx_occlusion_query_handle_t _handle, int32_t* _result);
  void bgfx_destroy_occlusion_query(bgfx_occlusion_query_handle_t _handle);
  void bgfx_set_palette_color(uint8_t _index, const float _rgba[4]);
  void bgfx_set_palette_color_rgba32f(uint8_t _index, float _r, float _g, float _b, float _a);
  void bgfx_set_palette_color_rgba8(uint8_t _index, uint32_t _rgba);
  void bgfx_set_view_name(bgfx_view_id_t _id, const char* _name, int32_t _len);
  void bgfx_set_view_rect(bgfx_view_id_t _id, uint16_t _x, uint16_t _y, uint16_t _width, uint16_t _height);
  void bgfx_set_view_rect_ratio(bgfx_view_id_t _id, uint16_t _x, uint16_t _y, bgfx_backbuffer_ratio_t _ratio);
  void bgfx_set_view_scissor(bgfx_view_id_t _id, uint16_t _x, uint16_t _y, uint16_t _width, uint16_t _height);
  void bgfx_set_view_clear(bgfx_view_id_t _id, uint16_t _flags, uint32_t _rgba, float _depth, uint8_t _stencil);
  void bgfx_set_view_clear_mrt(bgfx_view_id_t _id, uint16_t _flags, float _depth, uint8_t _stencil, uint8_t _c0, uint8_t _c1, uint8_t _c2, uint8_t _c3, uint8_t _c4, uint8_t _c5, uint8_t _c6, uint8_t _c7);
  void bgfx_set_view_mode(bgfx_view_id_t _id, bgfx_view_mode_t _mode);
  void bgfx_set_view_frame_buffer(bgfx_view_id_t _id, bgfx_frame_buffer_handle_t _handle);
  void bgfx_set_view_transform(bgfx_view_id_t _id, const void* _view, const void* _proj);
  void bgfx_set_view_order(bgfx_view_id_t _id, uint16_t _num, const bgfx_view_id_t* _order);
  void bgfx_reset_view(bgfx_view_id_t _id);
  bgfx_encoder_t* bgfx_encoder_begin( _Bool _forThread);
  void bgfx_encoder_end(bgfx_encoder_t* _encoder);
  void bgfx_encoder_set_marker(bgfx_encoder_t* _this, const char* _name, int32_t _len);
  void bgfx_encoder_set_state(bgfx_encoder_t* _this, uint64_t _state, uint32_t _rgba);
  void bgfx_encoder_set_condition(bgfx_encoder_t* _this, bgfx_occlusion_query_handle_t _handle, _Bool _visible);
  void bgfx_encoder_set_stencil(bgfx_encoder_t* _this, uint32_t _fstencil, uint32_t _bstencil);
  uint16_t bgfx_encoder_set_scissor(bgfx_encoder_t* _this, uint16_t _x, uint16_t _y, uint16_t _width, uint16_t _height);
  void bgfx_encoder_set_scissor_cached(bgfx_encoder_t* _this, uint16_t _cache);
  uint32_t bgfx_encoder_set_transform(bgfx_encoder_t* _this, const void* _mtx, uint16_t _num);
  void bgfx_encoder_set_transform_cached(bgfx_encoder_t* _this, uint32_t _cache, uint16_t _num);
  uint32_t bgfx_encoder_alloc_transform(bgfx_encoder_t* _this, bgfx_transform_t* _transform, uint16_t _num);
  void bgfx_encoder_set_uniform(bgfx_encoder_t* _this, bgfx_uniform_handle_t _handle, const void* _value, uint16_t _num);
  void bgfx_encoder_set_index_buffer(bgfx_encoder_t* _this, bgfx_index_buffer_handle_t _handle, uint32_t _firstIndex, uint32_t _numIndices);
  void bgfx_encoder_set_dynamic_index_buffer(bgfx_encoder_t* _this, bgfx_dynamic_index_buffer_handle_t _handle, uint32_t _firstIndex, uint32_t _numIndices);
  void bgfx_encoder_set_transient_index_buffer(bgfx_encoder_t* _this, const bgfx_transient_index_buffer_t* _tib, uint32_t _firstIndex, uint32_t _numIndices);
  void bgfx_encoder_set_vertex_buffer(bgfx_encoder_t* _this, uint8_t _stream, bgfx_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices);
  void bgfx_encoder_set_vertex_buffer_with_layout(bgfx_encoder_t* _this, uint8_t _stream, bgfx_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices, bgfx_vertex_layout_handle_t _layoutHandle);
  void bgfx_encoder_set_dynamic_vertex_buffer(bgfx_encoder_t* _this, uint8_t _stream, bgfx_dynamic_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices);
  void bgfx_encoder_set_dynamic_vertex_buffer_with_layout(bgfx_encoder_t* _this, uint8_t _stream, bgfx_dynamic_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices, bgfx_vertex_layout_handle_t _layoutHandle);
  void bgfx_encoder_set_transient_vertex_buffer(bgfx_encoder_t* _this, uint8_t _stream, const bgfx_transient_vertex_buffer_t* _tvb, uint32_t _startVertex, uint32_t _numVertices);
  void bgfx_encoder_set_transient_vertex_buffer_with_layout(bgfx_encoder_t* _this, uint8_t _stream, const bgfx_transient_vertex_buffer_t* _tvb, uint32_t _startVertex, uint32_t _numVertices, bgfx_vertex_layout_handle_t _layoutHandle);
  void bgfx_encoder_set_vertex_count(bgfx_encoder_t* _this, uint32_t _numVertices);
  void bgfx_encoder_set_instance_data_buffer(bgfx_encoder_t* _this, const bgfx_instance_data_buffer_t* _idb, uint32_t _start, uint32_t _num);
  void bgfx_encoder_set_instance_data_from_vertex_buffer(bgfx_encoder_t* _this, bgfx_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _num);
  void bgfx_encoder_set_instance_data_from_dynamic_vertex_buffer(bgfx_encoder_t* _this, bgfx_dynamic_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _num);
  void bgfx_encoder_set_instance_count(bgfx_encoder_t* _this, uint32_t _numInstances);
  void bgfx_encoder_set_texture(bgfx_encoder_t* _this, uint8_t _stage, bgfx_uniform_handle_t _sampler, bgfx_texture_handle_t _handle, uint32_t _flags);
  void bgfx_encoder_touch(bgfx_encoder_t* _this, bgfx_view_id_t _id);
  void bgfx_encoder_submit(bgfx_encoder_t* _this, bgfx_view_id_t _id, bgfx_program_handle_t _program, uint32_t _depth, uint8_t _flags);
  void bgfx_encoder_submit_occlusion_query(bgfx_encoder_t* _this, bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_occlusion_query_handle_t _occlusionQuery, uint32_t _depth, uint8_t _flags);
  void bgfx_encoder_submit_indirect(bgfx_encoder_t* _this, bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_indirect_buffer_handle_t _indirectHandle, uint32_t _start, uint32_t _num, uint32_t _depth, uint8_t _flags);
  void bgfx_encoder_submit_indirect_count(bgfx_encoder_t* _this, bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_indirect_buffer_handle_t _indirectHandle, uint32_t _start, bgfx_index_buffer_handle_t _numHandle, uint32_t _numIndex, uint32_t _numMax, uint32_t _depth, uint8_t _flags);
  void bgfx_encoder_set_compute_index_buffer(bgfx_encoder_t* _this, uint8_t _stage, bgfx_index_buffer_handle_t _handle, bgfx_access_t _access);
  void bgfx_encoder_set_compute_vertex_buffer(bgfx_encoder_t* _this, uint8_t _stage, bgfx_vertex_buffer_handle_t _handle, bgfx_access_t _access);
  void bgfx_encoder_set_compute_dynamic_index_buffer(bgfx_encoder_t* _this, uint8_t _stage, bgfx_dynamic_index_buffer_handle_t _handle, bgfx_access_t _access);
  void bgfx_encoder_set_compute_dynamic_vertex_buffer(bgfx_encoder_t* _this, uint8_t _stage, bgfx_dynamic_vertex_buffer_handle_t _handle, bgfx_access_t _access);
  void bgfx_encoder_set_compute_indirect_buffer(bgfx_encoder_t* _this, uint8_t _stage, bgfx_indirect_buffer_handle_t _handle, bgfx_access_t _access);
  void bgfx_encoder_set_image(bgfx_encoder_t* _this, uint8_t _stage, bgfx_texture_handle_t _handle, uint8_t _mip, bgfx_access_t _access, bgfx_texture_format_t _format);
  void bgfx_encoder_dispatch(bgfx_encoder_t* _this, bgfx_view_id_t _id, bgfx_program_handle_t _program, uint32_t _numX, uint32_t _numY, uint32_t _numZ, uint8_t _flags);
  void bgfx_encoder_dispatch_indirect(bgfx_encoder_t* _this, bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_indirect_buffer_handle_t _indirectHandle, uint32_t _start, uint32_t _num, uint8_t _flags);
  void bgfx_encoder_discard(bgfx_encoder_t* _this, uint8_t _flags);
  void bgfx_encoder_blit(bgfx_encoder_t* _this, bgfx_view_id_t _id, bgfx_texture_handle_t _dst, uint8_t _dstMip, uint16_t _dstX, uint16_t _dstY, uint16_t _dstZ, bgfx_texture_handle_t _src, uint8_t _srcMip, uint16_t _srcX, uint16_t _srcY, uint16_t _srcZ, uint16_t _width, uint16_t _height, uint16_t _depth);
  void bgfx_request_screen_shot(bgfx_frame_buffer_handle_t _handle, const char* _filePath);
  bgfx_render_frame_t bgfx_render_frame(int32_t _msecs);
  void bgfx_set_platform_data(const bgfx_platform_data_t * _data);
  const bgfx_internal_data_t* bgfx_get_internal_data(void);
  uintptr_t bgfx_override_internal_texture_ptr(bgfx_texture_handle_t _handle, uintptr_t _ptr);
  uintptr_t bgfx_override_internal_texture(bgfx_texture_handle_t _handle, uint16_t _width, uint16_t _height, uint8_t _numMips, bgfx_texture_format_t _format, uint64_t _flags);
  void bgfx_set_marker(const char* _name, int32_t _len);
  void bgfx_set_state(uint64_t _state, uint32_t _rgba);
  void bgfx_set_condition(bgfx_occlusion_query_handle_t _handle, _Bool _visible);
  void bgfx_set_stencil(uint32_t _fstencil, uint32_t _bstencil);
  uint16_t bgfx_set_scissor(uint16_t _x, uint16_t _y, uint16_t _width, uint16_t _height);
  void bgfx_set_scissor_cached(uint16_t _cache);
  uint32_t bgfx_set_transform(const void* _mtx, uint16_t _num);
  void bgfx_set_transform_cached(uint32_t _cache, uint16_t _num);
  uint32_t bgfx_alloc_transform(bgfx_transform_t* _transform, uint16_t _num);
  void bgfx_set_uniform(bgfx_uniform_handle_t _handle, const void* _value, uint16_t _num);
  void bgfx_set_index_buffer(bgfx_index_buffer_handle_t _handle, uint32_t _firstIndex, uint32_t _numIndices);
  void bgfx_set_dynamic_index_buffer(bgfx_dynamic_index_buffer_handle_t _handle, uint32_t _firstIndex, uint32_t _numIndices);
  void bgfx_set_transient_index_buffer(const bgfx_transient_index_buffer_t* _tib, uint32_t _firstIndex, uint32_t _numIndices);
  void bgfx_set_vertex_buffer(uint8_t _stream, bgfx_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices);
  void bgfx_set_vertex_buffer_with_layout(uint8_t _stream, bgfx_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices, bgfx_vertex_layout_handle_t _layoutHandle);
  void bgfx_set_dynamic_vertex_buffer(uint8_t _stream, bgfx_dynamic_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices);
  void bgfx_set_dynamic_vertex_buffer_with_layout(uint8_t _stream, bgfx_dynamic_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices, bgfx_vertex_layout_handle_t _layoutHandle);
  void bgfx_set_transient_vertex_buffer(uint8_t _stream, const bgfx_transient_vertex_buffer_t* _tvb, uint32_t _startVertex, uint32_t _numVertices);
  void bgfx_set_transient_vertex_buffer_with_layout(uint8_t _stream, const bgfx_transient_vertex_buffer_t* _tvb, uint32_t _startVertex, uint32_t _numVertices, bgfx_vertex_layout_handle_t _layoutHandle);
  void bgfx_set_vertex_count(uint32_t _numVertices);
  void bgfx_set_instance_data_buffer(const bgfx_instance_data_buffer_t* _idb, uint32_t _start, uint32_t _num);
  void bgfx_set_instance_data_from_vertex_buffer(bgfx_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _num);
  void bgfx_set_instance_data_from_dynamic_vertex_buffer(bgfx_dynamic_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _num);
  void bgfx_set_instance_count(uint32_t _numInstances);
  void bgfx_set_texture(uint8_t _stage, bgfx_uniform_handle_t _sampler, bgfx_texture_handle_t _handle, uint32_t _flags);
  void bgfx_touch(bgfx_view_id_t _id);
  void bgfx_submit(bgfx_view_id_t _id, bgfx_program_handle_t _program, uint32_t _depth, uint8_t _flags);
  void bgfx_submit_occlusion_query(bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_occlusion_query_handle_t _occlusionQuery, uint32_t _depth, uint8_t _flags);
  void bgfx_submit_indirect(bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_indirect_buffer_handle_t _indirectHandle, uint32_t _start, uint32_t _num, uint32_t _depth, uint8_t _flags);
  void bgfx_submit_indirect_count(bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_indirect_buffer_handle_t _indirectHandle, uint32_t _start, bgfx_index_buffer_handle_t _numHandle, uint32_t _numIndex, uint32_t _numMax, uint32_t _depth, uint8_t _flags);
  void bgfx_set_compute_index_buffer(uint8_t _stage, bgfx_index_buffer_handle_t _handle, bgfx_access_t _access);
  void bgfx_set_compute_vertex_buffer(uint8_t _stage, bgfx_vertex_buffer_handle_t _handle, bgfx_access_t _access);
  void bgfx_set_compute_dynamic_index_buffer(uint8_t _stage, bgfx_dynamic_index_buffer_handle_t _handle, bgfx_access_t _access);
  void bgfx_set_compute_dynamic_vertex_buffer(uint8_t _stage, bgfx_dynamic_vertex_buffer_handle_t _handle, bgfx_access_t _access);
  void bgfx_set_compute_indirect_buffer(uint8_t _stage, bgfx_indirect_buffer_handle_t _handle, bgfx_access_t _access);
  void bgfx_set_image(uint8_t _stage, bgfx_texture_handle_t _handle, uint8_t _mip, bgfx_access_t _access, bgfx_texture_format_t _format);
  void bgfx_dispatch(bgfx_view_id_t _id, bgfx_program_handle_t _program, uint32_t _numX, uint32_t _numY, uint32_t _numZ, uint8_t _flags);
  void bgfx_dispatch_indirect(bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_indirect_buffer_handle_t _indirectHandle, uint32_t _start, uint32_t _num, uint8_t _flags);
  void bgfx_discard(uint8_t _flags);
  void bgfx_blit(bgfx_view_id_t _id, bgfx_texture_handle_t _dst, uint8_t _dstMip, uint16_t _dstX, uint16_t _dstY, uint16_t _dstZ, bgfx_texture_handle_t _src, uint8_t _srcMip, uint16_t _srcX, uint16_t _srcY, uint16_t _srcZ, uint16_t _width, uint16_t _height, uint16_t _depth);
  typedef enum bgfx_function_id {
    BGFX_FUNCTION_ID_ATTACHMENT_INIT,
    BGFX_FUNCTION_ID_VERTEX_LAYOUT_BEGIN,
    BGFX_FUNCTION_ID_VERTEX_LAYOUT_ADD,
    BGFX_FUNCTION_ID_VERTEX_LAYOUT_DECODE,
    BGFX_FUNCTION_ID_VERTEX_LAYOUT_HAS,
    BGFX_FUNCTION_ID_VERTEX_LAYOUT_SKIP,
    BGFX_FUNCTION_ID_VERTEX_LAYOUT_END,
    BGFX_FUNCTION_ID_VERTEX_LAYOUT_GET_OFFSET,
    BGFX_FUNCTION_ID_VERTEX_LAYOUT_GET_STRIDE,
    BGFX_FUNCTION_ID_VERTEX_LAYOUT_GET_SIZE,
    BGFX_FUNCTION_ID_VERTEX_PACK,
    BGFX_FUNCTION_ID_VERTEX_UNPACK,
    BGFX_FUNCTION_ID_VERTEX_CONVERT,
    BGFX_FUNCTION_ID_WELD_VERTICES,
    BGFX_FUNCTION_ID_TOPOLOGY_CONVERT,
    BGFX_FUNCTION_ID_TOPOLOGY_SORT_TRI_LIST,
    BGFX_FUNCTION_ID_GET_SUPPORTED_RENDERERS,
    BGFX_FUNCTION_ID_GET_RENDERER_NAME,
    BGFX_FUNCTION_ID_INIT_CTOR,
    BGFX_FUNCTION_ID_INIT,
    BGFX_FUNCTION_ID_SHUTDOWN,
    BGFX_FUNCTION_ID_RESET,
    BGFX_FUNCTION_ID_FRAME,
    BGFX_FUNCTION_ID_GET_RENDERER_TYPE,
    BGFX_FUNCTION_ID_GET_CAPS,
    BGFX_FUNCTION_ID_GET_STATS,
    BGFX_FUNCTION_ID_ALLOC,
    BGFX_FUNCTION_ID_COPY,
    BGFX_FUNCTION_ID_MAKE_REF,
    BGFX_FUNCTION_ID_MAKE_REF_RELEASE,
    BGFX_FUNCTION_ID_SET_DEBUG,
    BGFX_FUNCTION_ID_DBG_TEXT_CLEAR,
    BGFX_FUNCTION_ID_DBG_TEXT_PRINTF,
    BGFX_FUNCTION_ID_DBG_TEXT_VPRINTF,
    BGFX_FUNCTION_ID_DBG_TEXT_IMAGE,
    BGFX_FUNCTION_ID_CREATE_INDEX_BUFFER,
    BGFX_FUNCTION_ID_SET_INDEX_BUFFER_NAME,
    BGFX_FUNCTION_ID_DESTROY_INDEX_BUFFER,
    BGFX_FUNCTION_ID_CREATE_VERTEX_LAYOUT,
    BGFX_FUNCTION_ID_DESTROY_VERTEX_LAYOUT,
    BGFX_FUNCTION_ID_CREATE_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_SET_VERTEX_BUFFER_NAME,
    BGFX_FUNCTION_ID_DESTROY_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_CREATE_DYNAMIC_INDEX_BUFFER,
    BGFX_FUNCTION_ID_CREATE_DYNAMIC_INDEX_BUFFER_MEM,
    BGFX_FUNCTION_ID_UPDATE_DYNAMIC_INDEX_BUFFER,
    BGFX_FUNCTION_ID_DESTROY_DYNAMIC_INDEX_BUFFER,
    BGFX_FUNCTION_ID_CREATE_DYNAMIC_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_CREATE_DYNAMIC_VERTEX_BUFFER_MEM,
    BGFX_FUNCTION_ID_UPDATE_DYNAMIC_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_DESTROY_DYNAMIC_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_GET_AVAIL_TRANSIENT_INDEX_BUFFER,
    BGFX_FUNCTION_ID_GET_AVAIL_TRANSIENT_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_GET_AVAIL_INSTANCE_DATA_BUFFER,
    BGFX_FUNCTION_ID_ALLOC_TRANSIENT_INDEX_BUFFER,
    BGFX_FUNCTION_ID_ALLOC_TRANSIENT_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_ALLOC_TRANSIENT_BUFFERS,
    BGFX_FUNCTION_ID_ALLOC_INSTANCE_DATA_BUFFER,
    BGFX_FUNCTION_ID_CREATE_INDIRECT_BUFFER,
    BGFX_FUNCTION_ID_DESTROY_INDIRECT_BUFFER,
    BGFX_FUNCTION_ID_CREATE_SHADER,
    BGFX_FUNCTION_ID_GET_SHADER_UNIFORMS,
    BGFX_FUNCTION_ID_SET_SHADER_NAME,
    BGFX_FUNCTION_ID_DESTROY_SHADER,
    BGFX_FUNCTION_ID_CREATE_PROGRAM,
    BGFX_FUNCTION_ID_CREATE_COMPUTE_PROGRAM,
    BGFX_FUNCTION_ID_DESTROY_PROGRAM,
    BGFX_FUNCTION_ID_IS_TEXTURE_VALID,
    BGFX_FUNCTION_ID_IS_FRAME_BUFFER_VALID,
    BGFX_FUNCTION_ID_CALC_TEXTURE_SIZE,
    BGFX_FUNCTION_ID_CREATE_TEXTURE,
    BGFX_FUNCTION_ID_CREATE_TEXTURE_2D,
    BGFX_FUNCTION_ID_CREATE_TEXTURE_2D_SCALED,
    BGFX_FUNCTION_ID_CREATE_TEXTURE_3D,
    BGFX_FUNCTION_ID_CREATE_TEXTURE_CUBE,
    BGFX_FUNCTION_ID_UPDATE_TEXTURE_2D,
    BGFX_FUNCTION_ID_UPDATE_TEXTURE_3D,
    BGFX_FUNCTION_ID_UPDATE_TEXTURE_CUBE,
    BGFX_FUNCTION_ID_READ_TEXTURE,
    BGFX_FUNCTION_ID_SET_TEXTURE_NAME,
    BGFX_FUNCTION_ID_GET_DIRECT_ACCESS_PTR,
    BGFX_FUNCTION_ID_DESTROY_TEXTURE,
    BGFX_FUNCTION_ID_CREATE_FRAME_BUFFER,
    BGFX_FUNCTION_ID_CREATE_FRAME_BUFFER_SCALED,
    BGFX_FUNCTION_ID_CREATE_FRAME_BUFFER_FROM_HANDLES,
    BGFX_FUNCTION_ID_CREATE_FRAME_BUFFER_FROM_ATTACHMENT,
    BGFX_FUNCTION_ID_CREATE_FRAME_BUFFER_FROM_NWH,
    BGFX_FUNCTION_ID_SET_FRAME_BUFFER_NAME,
    BGFX_FUNCTION_ID_GET_TEXTURE,
    BGFX_FUNCTION_ID_DESTROY_FRAME_BUFFER,
    BGFX_FUNCTION_ID_CREATE_UNIFORM,
    BGFX_FUNCTION_ID_GET_UNIFORM_INFO,
    BGFX_FUNCTION_ID_DESTROY_UNIFORM,
    BGFX_FUNCTION_ID_CREATE_OCCLUSION_QUERY,
    BGFX_FUNCTION_ID_GET_RESULT,
    BGFX_FUNCTION_ID_DESTROY_OCCLUSION_QUERY,
    BGFX_FUNCTION_ID_SET_PALETTE_COLOR,
    BGFX_FUNCTION_ID_SET_PALETTE_COLOR_RGBA32F,
    BGFX_FUNCTION_ID_SET_PALETTE_COLOR_RGBA8,
    BGFX_FUNCTION_ID_SET_VIEW_NAME,
    BGFX_FUNCTION_ID_SET_VIEW_RECT,
    BGFX_FUNCTION_ID_SET_VIEW_RECT_RATIO,
    BGFX_FUNCTION_ID_SET_VIEW_SCISSOR,
    BGFX_FUNCTION_ID_SET_VIEW_CLEAR,
    BGFX_FUNCTION_ID_SET_VIEW_CLEAR_MRT,
    BGFX_FUNCTION_ID_SET_VIEW_MODE,
    BGFX_FUNCTION_ID_SET_VIEW_FRAME_BUFFER,
    BGFX_FUNCTION_ID_SET_VIEW_TRANSFORM,
    BGFX_FUNCTION_ID_SET_VIEW_ORDER,
    BGFX_FUNCTION_ID_RESET_VIEW,
    BGFX_FUNCTION_ID_ENCODER_BEGIN,
    BGFX_FUNCTION_ID_ENCODER_END,
    BGFX_FUNCTION_ID_ENCODER_SET_MARKER,
    BGFX_FUNCTION_ID_ENCODER_SET_STATE,
    BGFX_FUNCTION_ID_ENCODER_SET_CONDITION,
    BGFX_FUNCTION_ID_ENCODER_SET_STENCIL,
    BGFX_FUNCTION_ID_ENCODER_SET_SCISSOR,
    BGFX_FUNCTION_ID_ENCODER_SET_SCISSOR_CACHED,
    BGFX_FUNCTION_ID_ENCODER_SET_TRANSFORM,
    BGFX_FUNCTION_ID_ENCODER_SET_TRANSFORM_CACHED,
    BGFX_FUNCTION_ID_ENCODER_ALLOC_TRANSFORM,
    BGFX_FUNCTION_ID_ENCODER_SET_UNIFORM,
    BGFX_FUNCTION_ID_ENCODER_SET_INDEX_BUFFER,
    BGFX_FUNCTION_ID_ENCODER_SET_DYNAMIC_INDEX_BUFFER,
    BGFX_FUNCTION_ID_ENCODER_SET_TRANSIENT_INDEX_BUFFER,
    BGFX_FUNCTION_ID_ENCODER_SET_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_ENCODER_SET_VERTEX_BUFFER_WITH_LAYOUT,
    BGFX_FUNCTION_ID_ENCODER_SET_DYNAMIC_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_ENCODER_SET_DYNAMIC_VERTEX_BUFFER_WITH_LAYOUT,
    BGFX_FUNCTION_ID_ENCODER_SET_TRANSIENT_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_ENCODER_SET_TRANSIENT_VERTEX_BUFFER_WITH_LAYOUT,
    BGFX_FUNCTION_ID_ENCODER_SET_VERTEX_COUNT,
    BGFX_FUNCTION_ID_ENCODER_SET_INSTANCE_DATA_BUFFER,
    BGFX_FUNCTION_ID_ENCODER_SET_INSTANCE_DATA_FROM_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_ENCODER_SET_INSTANCE_DATA_FROM_DYNAMIC_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_ENCODER_SET_INSTANCE_COUNT,
    BGFX_FUNCTION_ID_ENCODER_SET_TEXTURE,
    BGFX_FUNCTION_ID_ENCODER_TOUCH,
    BGFX_FUNCTION_ID_ENCODER_SUBMIT,
    BGFX_FUNCTION_ID_ENCODER_SUBMIT_OCCLUSION_QUERY,
    BGFX_FUNCTION_ID_ENCODER_SUBMIT_INDIRECT,
    BGFX_FUNCTION_ID_ENCODER_SUBMIT_INDIRECT_COUNT,
    BGFX_FUNCTION_ID_ENCODER_SET_COMPUTE_INDEX_BUFFER,
    BGFX_FUNCTION_ID_ENCODER_SET_COMPUTE_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_ENCODER_SET_COMPUTE_DYNAMIC_INDEX_BUFFER,
    BGFX_FUNCTION_ID_ENCODER_SET_COMPUTE_DYNAMIC_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_ENCODER_SET_COMPUTE_INDIRECT_BUFFER,
    BGFX_FUNCTION_ID_ENCODER_SET_IMAGE,
    BGFX_FUNCTION_ID_ENCODER_DISPATCH,
    BGFX_FUNCTION_ID_ENCODER_DISPATCH_INDIRECT,
    BGFX_FUNCTION_ID_ENCODER_DISCARD,
    BGFX_FUNCTION_ID_ENCODER_BLIT,
    BGFX_FUNCTION_ID_REQUEST_SCREEN_SHOT,
    BGFX_FUNCTION_ID_RENDER_FRAME,
    BGFX_FUNCTION_ID_SET_PLATFORM_DATA,
    BGFX_FUNCTION_ID_GET_INTERNAL_DATA,
    BGFX_FUNCTION_ID_OVERRIDE_INTERNAL_TEXTURE_PTR,
    BGFX_FUNCTION_ID_OVERRIDE_INTERNAL_TEXTURE,
    BGFX_FUNCTION_ID_SET_MARKER,
    BGFX_FUNCTION_ID_SET_STATE,
    BGFX_FUNCTION_ID_SET_CONDITION,
    BGFX_FUNCTION_ID_SET_STENCIL,
    BGFX_FUNCTION_ID_SET_SCISSOR,
    BGFX_FUNCTION_ID_SET_SCISSOR_CACHED,
    BGFX_FUNCTION_ID_SET_TRANSFORM,
    BGFX_FUNCTION_ID_SET_TRANSFORM_CACHED,
    BGFX_FUNCTION_ID_ALLOC_TRANSFORM,
    BGFX_FUNCTION_ID_SET_UNIFORM,
    BGFX_FUNCTION_ID_SET_INDEX_BUFFER,
    BGFX_FUNCTION_ID_SET_DYNAMIC_INDEX_BUFFER,
    BGFX_FUNCTION_ID_SET_TRANSIENT_INDEX_BUFFER,
    BGFX_FUNCTION_ID_SET_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_SET_VERTEX_BUFFER_WITH_LAYOUT,
    BGFX_FUNCTION_ID_SET_DYNAMIC_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_SET_DYNAMIC_VERTEX_BUFFER_WITH_LAYOUT,
    BGFX_FUNCTION_ID_SET_TRANSIENT_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_SET_TRANSIENT_VERTEX_BUFFER_WITH_LAYOUT,
    BGFX_FUNCTION_ID_SET_VERTEX_COUNT,
    BGFX_FUNCTION_ID_SET_INSTANCE_DATA_BUFFER,
    BGFX_FUNCTION_ID_SET_INSTANCE_DATA_FROM_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_SET_INSTANCE_DATA_FROM_DYNAMIC_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_SET_INSTANCE_COUNT,
    BGFX_FUNCTION_ID_SET_TEXTURE,
    BGFX_FUNCTION_ID_TOUCH,
    BGFX_FUNCTION_ID_SUBMIT,
    BGFX_FUNCTION_ID_SUBMIT_OCCLUSION_QUERY,
    BGFX_FUNCTION_ID_SUBMIT_INDIRECT,
    BGFX_FUNCTION_ID_SUBMIT_INDIRECT_COUNT,
    BGFX_FUNCTION_ID_SET_COMPUTE_INDEX_BUFFER,
    BGFX_FUNCTION_ID_SET_COMPUTE_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_SET_COMPUTE_DYNAMIC_INDEX_BUFFER,
    BGFX_FUNCTION_ID_SET_COMPUTE_DYNAMIC_VERTEX_BUFFER,
    BGFX_FUNCTION_ID_SET_COMPUTE_INDIRECT_BUFFER,
    BGFX_FUNCTION_ID_SET_IMAGE,
    BGFX_FUNCTION_ID_DISPATCH,
    BGFX_FUNCTION_ID_DISPATCH_INDIRECT,
    BGFX_FUNCTION_ID_DISCARD,
    BGFX_FUNCTION_ID_BLIT,
    BGFX_FUNCTION_ID_COUNT
  } bgfx_function_id_t;
  
  struct bgfx_interface_vtbl {
    void (*attachment_init)(bgfx_attachment_t* _this, bgfx_texture_handle_t _handle, bgfx_access_t _access, uint16_t _layer, uint16_t _numLayers, uint16_t _mip, uint8_t _resolve);
    bgfx_vertex_layout_t* (*vertex_layout_begin)(bgfx_vertex_layout_t* _this, bgfx_renderer_type_t _rendererType);
    bgfx_vertex_layout_t* (*vertex_layout_add)(bgfx_vertex_layout_t* _this, bgfx_attrib_t _attrib, uint8_t _num, bgfx_attrib_type_t _type, _Bool _normalized, _Bool _asInt);
    void (*vertex_layout_decode)(const bgfx_vertex_layout_t* _this, bgfx_attrib_t _attrib, uint8_t * _num, bgfx_attrib_type_t * _type, _Bool * _normalized, _Bool * _asInt);
    _Bool (*vertex_layout_has)(const bgfx_vertex_layout_t* _this, bgfx_attrib_t _attrib);
    bgfx_vertex_layout_t* (*vertex_layout_skip)(bgfx_vertex_layout_t* _this, uint8_t _num);
    void (*vertex_layout_end)(bgfx_vertex_layout_t* _this);
    uint16_t (*vertex_layout_get_offset)(const bgfx_vertex_layout_t* _this, bgfx_attrib_t _attrib);
    uint16_t (*vertex_layout_get_stride)(const bgfx_vertex_layout_t* _this);
    uint32_t (*vertex_layout_get_size)(const bgfx_vertex_layout_t* _this, uint32_t _num);
    void (*vertex_pack)(const float _input[4], _Bool _inputNormalized, bgfx_attrib_t _attr, const bgfx_vertex_layout_t * _layout, void* _data, uint32_t _index);
    void (*vertex_unpack)(float _output[4], bgfx_attrib_t _attr, const bgfx_vertex_layout_t * _layout, const void* _data, uint32_t _index);
    void (*vertex_convert)(const bgfx_vertex_layout_t * _dstLayout, void* _dstData, const bgfx_vertex_layout_t * _srcLayout, const void* _srcData, uint32_t _num);
    uint32_t (*weld_vertices)(void* _output, const bgfx_vertex_layout_t * _layout, const void* _data, uint32_t _num, _Bool _index32, float _epsilon);
    uint32_t (*topology_convert)(bgfx_topology_convert_t _conversion, void* _dst, uint32_t _dstSize, const void* _indices, uint32_t _numIndices, _Bool _index32);
    void (*topology_sort_tri_list)(bgfx_topology_sort_t _sort, void* _dst, uint32_t _dstSize, const float _dir[3], const float _pos[3], const void* _vertices, uint32_t _stride, const void* _indices, uint32_t _numIndices, _Bool _index32);
    uint8_t (*get_supported_renderers)(uint8_t _max, bgfx_renderer_type_t* _enum);
    const char* (*get_renderer_name)(bgfx_renderer_type_t _type);
    void (*init_ctor)(bgfx_init_t* _init);
    _Bool (*init)(const bgfx_init_t * _init);
    void (*shutdown)(void);
    void (*reset)(uint32_t _width, uint32_t _height, uint32_t _flags, bgfx_texture_format_t _format);
    uint32_t (*frame)( _Bool _capture);
    bgfx_renderer_type_t (*get_renderer_type)(void);
    const bgfx_caps_t* (*get_caps)(void);
    const bgfx_stats_t* (*get_stats)(void);
    const bgfx_memory_t* (*alloc)(uint32_t _size);
    const bgfx_memory_t* (*copy)(const void* _data, uint32_t _size);
    const bgfx_memory_t* (*make_ref)(const void* _data, uint32_t _size);
    const bgfx_memory_t* (*make_ref_release)(const void* _data, uint32_t _size, bgfx_release_fn_t _releaseFn, void* _userData);
    void (*set_debug)(uint32_t _debug);
    void (*dbg_text_clear)(uint8_t _attr, _Bool _small);
    void (*dbg_text_printf)(uint16_t _x, uint16_t _y, uint8_t _attr, const char* _format, ... );
    void (*dbg_text_vprintf)(uint16_t _x, uint16_t _y, uint8_t _attr, const char* _format, va_list _argList);
    void (*dbg_text_image)(uint16_t _x, uint16_t _y, uint16_t _width, uint16_t _height, const void* _data, uint16_t _pitch);
    bgfx_index_buffer_handle_t (*create_index_buffer)(const bgfx_memory_t* _mem, uint16_t _flags);
    void (*set_index_buffer_name)(bgfx_index_buffer_handle_t _handle, const char* _name, int32_t _len);
    void (*destroy_index_buffer)(bgfx_index_buffer_handle_t _handle);
    bgfx_vertex_layout_handle_t (*create_vertex_layout)(const bgfx_vertex_layout_t * _layout);
    void (*destroy_vertex_layout)(bgfx_vertex_layout_handle_t _layoutHandle);
    bgfx_vertex_buffer_handle_t (*create_vertex_buffer)(const bgfx_memory_t* _mem, const bgfx_vertex_layout_t * _layout, uint16_t _flags);
    void (*set_vertex_buffer_name)(bgfx_vertex_buffer_handle_t _handle, const char* _name, int32_t _len);
    void (*destroy_vertex_buffer)(bgfx_vertex_buffer_handle_t _handle);
    bgfx_dynamic_index_buffer_handle_t (*create_dynamic_index_buffer)(uint32_t _num, uint16_t _flags);
    bgfx_dynamic_index_buffer_handle_t (*create_dynamic_index_buffer_mem)(const bgfx_memory_t* _mem, uint16_t _flags);
    void (*update_dynamic_index_buffer)(bgfx_dynamic_index_buffer_handle_t _handle, uint32_t _startIndex, const bgfx_memory_t* _mem);
    void (*destroy_dynamic_index_buffer)(bgfx_dynamic_index_buffer_handle_t _handle);
    bgfx_dynamic_vertex_buffer_handle_t (*create_dynamic_vertex_buffer)(uint32_t _num, const bgfx_vertex_layout_t* _layout, uint16_t _flags);
    bgfx_dynamic_vertex_buffer_handle_t (*create_dynamic_vertex_buffer_mem)(const bgfx_memory_t* _mem, const bgfx_vertex_layout_t* _layout, uint16_t _flags);
    void (*update_dynamic_vertex_buffer)(bgfx_dynamic_vertex_buffer_handle_t _handle, uint32_t _startVertex, const bgfx_memory_t* _mem);
    void (*destroy_dynamic_vertex_buffer)(bgfx_dynamic_vertex_buffer_handle_t _handle);
    uint32_t (*get_avail_transient_index_buffer)(uint32_t _num, _Bool _index32);
    uint32_t (*get_avail_transient_vertex_buffer)(uint32_t _num, const bgfx_vertex_layout_t * _layout);
    uint32_t (*get_avail_instance_data_buffer)(uint32_t _num, uint16_t _stride);
    void (*alloc_transient_index_buffer)(bgfx_transient_index_buffer_t* _tib, uint32_t _num, _Bool _index32);
    void (*alloc_transient_vertex_buffer)(bgfx_transient_vertex_buffer_t* _tvb, uint32_t _num, const bgfx_vertex_layout_t * _layout);
    _Bool (*alloc_transient_buffers)(bgfx_transient_vertex_buffer_t* _tvb, const bgfx_vertex_layout_t * _layout, uint32_t _numVertices, bgfx_transient_index_buffer_t* _tib, uint32_t _numIndices, _Bool _index32);
    void (*alloc_instance_data_buffer)(bgfx_instance_data_buffer_t* _idb, uint32_t _num, uint16_t _stride);
    bgfx_indirect_buffer_handle_t (*create_indirect_buffer)(uint32_t _num);
    void (*destroy_indirect_buffer)(bgfx_indirect_buffer_handle_t _handle);
    bgfx_shader_handle_t (*create_shader)(const bgfx_memory_t* _mem);
    uint16_t (*get_shader_uniforms)(bgfx_shader_handle_t _handle, bgfx_uniform_handle_t* _uniforms, uint16_t _max);
    void (*set_shader_name)(bgfx_shader_handle_t _handle, const char* _name, int32_t _len);
    void (*destroy_shader)(bgfx_shader_handle_t _handle);
    bgfx_program_handle_t (*create_program)(bgfx_shader_handle_t _vsh, bgfx_shader_handle_t _fsh, _Bool _destroyShaders);
    bgfx_program_handle_t (*create_compute_program)(bgfx_shader_handle_t _csh, _Bool _destroyShaders);
    void (*destroy_program)(bgfx_program_handle_t _handle);
    _Bool (*is_texture_valid)(uint16_t _depth, _Bool _cubeMap, uint16_t _numLayers, bgfx_texture_format_t _format, uint64_t _flags);
    _Bool (*is_frame_buffer_valid)(uint8_t _num, const bgfx_attachment_t* _attachment);
    void (*calc_texture_size)(bgfx_texture_info_t * _info, uint16_t _width, uint16_t _height, uint16_t _depth, _Bool _cubeMap, _Bool _hasMips, uint16_t _numLayers, bgfx_texture_format_t _format);
    bgfx_texture_handle_t (*create_texture)(const bgfx_memory_t* _mem, uint64_t _flags, uint8_t _skip, bgfx_texture_info_t* _info);
    bgfx_texture_handle_t (*create_texture_2d)(uint16_t _width, uint16_t _height, _Bool _hasMips, uint16_t _numLayers, bgfx_texture_format_t _format, uint64_t _flags, const bgfx_memory_t* _mem);
    bgfx_texture_handle_t (*create_texture_2d_scaled)(bgfx_backbuffer_ratio_t _ratio, _Bool _hasMips, uint16_t _numLayers, bgfx_texture_format_t _format, uint64_t _flags);
    bgfx_texture_handle_t (*create_texture_3d)(uint16_t _width, uint16_t _height, uint16_t _depth, _Bool _hasMips, bgfx_texture_format_t _format, uint64_t _flags, const bgfx_memory_t* _mem);
    bgfx_texture_handle_t (*create_texture_cube)(uint16_t _size, _Bool _hasMips, uint16_t _numLayers, bgfx_texture_format_t _format, uint64_t _flags, const bgfx_memory_t* _mem);
    void (*update_texture_2d)(bgfx_texture_handle_t _handle, uint16_t _layer, uint8_t _mip, uint16_t _x, uint16_t _y, uint16_t _width, uint16_t _height, const bgfx_memory_t* _mem, uint16_t _pitch);
    void (*update_texture_3d)(bgfx_texture_handle_t _handle, uint8_t _mip, uint16_t _x, uint16_t _y, uint16_t _z, uint16_t _width, uint16_t _height, uint16_t _depth, const bgfx_memory_t* _mem);
    void (*update_texture_cube)(bgfx_texture_handle_t _handle, uint16_t _layer, uint8_t _side, uint8_t _mip, uint16_t _x, uint16_t _y, uint16_t _width, uint16_t _height, const bgfx_memory_t* _mem, uint16_t _pitch);
    uint32_t (*read_texture)(bgfx_texture_handle_t _handle, void* _data, uint8_t _mip);
    void (*set_texture_name)(bgfx_texture_handle_t _handle, const char* _name, int32_t _len);
    void* (*get_direct_access_ptr)(bgfx_texture_handle_t _handle);
    void (*destroy_texture)(bgfx_texture_handle_t _handle);
    bgfx_frame_buffer_handle_t (*create_frame_buffer)(uint16_t _width, uint16_t _height, bgfx_texture_format_t _format, uint64_t _textureFlags);
    bgfx_frame_buffer_handle_t (*create_frame_buffer_scaled)(bgfx_backbuffer_ratio_t _ratio, bgfx_texture_format_t _format, uint64_t _textureFlags);
    bgfx_frame_buffer_handle_t (*create_frame_buffer_from_handles)(uint8_t _num, const bgfx_texture_handle_t* _handles, _Bool _destroyTexture);
    bgfx_frame_buffer_handle_t (*create_frame_buffer_from_attachment)(uint8_t _num, const bgfx_attachment_t* _attachment, _Bool _destroyTexture);
    bgfx_frame_buffer_handle_t (*create_frame_buffer_from_nwh)(void* _nwh, uint16_t _width, uint16_t _height, bgfx_texture_format_t _format, bgfx_texture_format_t _depthFormat);
    void (*set_frame_buffer_name)(bgfx_frame_buffer_handle_t _handle, const char* _name, int32_t _len);
    bgfx_texture_handle_t (*get_texture)(bgfx_frame_buffer_handle_t _handle, uint8_t _attachment);
    void (*destroy_frame_buffer)(bgfx_frame_buffer_handle_t _handle);
    bgfx_uniform_handle_t (*create_uniform)(const char* _name, bgfx_uniform_type_t _type, uint16_t _num);
    void (*get_uniform_info)(bgfx_uniform_handle_t _handle, bgfx_uniform_info_t * _info);
    void (*destroy_uniform)(bgfx_uniform_handle_t _handle);
    bgfx_occlusion_query_handle_t (*create_occlusion_query)(void);
    bgfx_occlusion_query_result_t (*get_result)(bgfx_occlusion_query_handle_t _handle, int32_t* _result);
    void (*destroy_occlusion_query)(bgfx_occlusion_query_handle_t _handle);
    void (*set_palette_color)(uint8_t _index, const float _rgba[4]);
    void (*set_palette_color_rgba32f)(uint8_t _index, float _r, float _g, float _b, float _a);
    void (*set_palette_color_rgba8)(uint8_t _index, uint32_t _rgba);
    void (*set_view_name)(bgfx_view_id_t _id, const char* _name, int32_t _len);
    void (*set_view_rect)(bgfx_view_id_t _id, uint16_t _x, uint16_t _y, uint16_t _width, uint16_t _height);
    void (*set_view_rect_ratio)(bgfx_view_id_t _id, uint16_t _x, uint16_t _y, bgfx_backbuffer_ratio_t _ratio);
    void (*set_view_scissor)(bgfx_view_id_t _id, uint16_t _x, uint16_t _y, uint16_t _width, uint16_t _height);
    void (*set_view_clear)(bgfx_view_id_t _id, uint16_t _flags, uint32_t _rgba, float _depth, uint8_t _stencil);
    void (*set_view_clear_mrt)(bgfx_view_id_t _id, uint16_t _flags, float _depth, uint8_t _stencil, uint8_t _c0, uint8_t _c1, uint8_t _c2, uint8_t _c3, uint8_t _c4, uint8_t _c5, uint8_t _c6, uint8_t _c7);
    void (*set_view_mode)(bgfx_view_id_t _id, bgfx_view_mode_t _mode);
    void (*set_view_frame_buffer)(bgfx_view_id_t _id, bgfx_frame_buffer_handle_t _handle);
    void (*set_view_transform)(bgfx_view_id_t _id, const void* _view, const void* _proj);
    void (*set_view_order)(bgfx_view_id_t _id, uint16_t _num, const bgfx_view_id_t* _order);
    void (*reset_view)(bgfx_view_id_t _id);
    bgfx_encoder_t* (*encoder_begin)( _Bool _forThread);
    void (*encoder_end)(bgfx_encoder_t* _encoder);
    void (*encoder_set_marker)(bgfx_encoder_t* _this, const char* _name, int32_t _len);
    void (*encoder_set_state)(bgfx_encoder_t* _this, uint64_t _state, uint32_t _rgba);
    void (*encoder_set_condition)(bgfx_encoder_t* _this, bgfx_occlusion_query_handle_t _handle, _Bool _visible);
    void (*encoder_set_stencil)(bgfx_encoder_t* _this, uint32_t _fstencil, uint32_t _bstencil);
    uint16_t (*encoder_set_scissor)(bgfx_encoder_t* _this, uint16_t _x, uint16_t _y, uint16_t _width, uint16_t _height);
    void (*encoder_set_scissor_cached)(bgfx_encoder_t* _this, uint16_t _cache);
    uint32_t (*encoder_set_transform)(bgfx_encoder_t* _this, const void* _mtx, uint16_t _num);
    void (*encoder_set_transform_cached)(bgfx_encoder_t* _this, uint32_t _cache, uint16_t _num);
    uint32_t (*encoder_alloc_transform)(bgfx_encoder_t* _this, bgfx_transform_t* _transform, uint16_t _num);
    void (*encoder_set_uniform)(bgfx_encoder_t* _this, bgfx_uniform_handle_t _handle, const void* _value, uint16_t _num);
    void (*encoder_set_index_buffer)(bgfx_encoder_t* _this, bgfx_index_buffer_handle_t _handle, uint32_t _firstIndex, uint32_t _numIndices);
    void (*encoder_set_dynamic_index_buffer)(bgfx_encoder_t* _this, bgfx_dynamic_index_buffer_handle_t _handle, uint32_t _firstIndex, uint32_t _numIndices);
    void (*encoder_set_transient_index_buffer)(bgfx_encoder_t* _this, const bgfx_transient_index_buffer_t* _tib, uint32_t _firstIndex, uint32_t _numIndices);
    void (*encoder_set_vertex_buffer)(bgfx_encoder_t* _this, uint8_t _stream, bgfx_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices);
    void (*encoder_set_vertex_buffer_with_layout)(bgfx_encoder_t* _this, uint8_t _stream, bgfx_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices, bgfx_vertex_layout_handle_t _layoutHandle);
    void (*encoder_set_dynamic_vertex_buffer)(bgfx_encoder_t* _this, uint8_t _stream, bgfx_dynamic_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices);
    void (*encoder_set_dynamic_vertex_buffer_with_layout)(bgfx_encoder_t* _this, uint8_t _stream, bgfx_dynamic_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices, bgfx_vertex_layout_handle_t _layoutHandle);
    void (*encoder_set_transient_vertex_buffer)(bgfx_encoder_t* _this, uint8_t _stream, const bgfx_transient_vertex_buffer_t* _tvb, uint32_t _startVertex, uint32_t _numVertices);
    void (*encoder_set_transient_vertex_buffer_with_layout)(bgfx_encoder_t* _this, uint8_t _stream, const bgfx_transient_vertex_buffer_t* _tvb, uint32_t _startVertex, uint32_t _numVertices, bgfx_vertex_layout_handle_t _layoutHandle);
    void (*encoder_set_vertex_count)(bgfx_encoder_t* _this, uint32_t _numVertices);
    void (*encoder_set_instance_data_buffer)(bgfx_encoder_t* _this, const bgfx_instance_data_buffer_t* _idb, uint32_t _start, uint32_t _num);
    void (*encoder_set_instance_data_from_vertex_buffer)(bgfx_encoder_t* _this, bgfx_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _num);
    void (*encoder_set_instance_data_from_dynamic_vertex_buffer)(bgfx_encoder_t* _this, bgfx_dynamic_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _num);
    void (*encoder_set_instance_count)(bgfx_encoder_t* _this, uint32_t _numInstances);
    void (*encoder_set_texture)(bgfx_encoder_t* _this, uint8_t _stage, bgfx_uniform_handle_t _sampler, bgfx_texture_handle_t _handle, uint32_t _flags);
    void (*encoder_touch)(bgfx_encoder_t* _this, bgfx_view_id_t _id);
    void (*encoder_submit)(bgfx_encoder_t* _this, bgfx_view_id_t _id, bgfx_program_handle_t _program, uint32_t _depth, uint8_t _flags);
    void (*encoder_submit_occlusion_query)(bgfx_encoder_t* _this, bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_occlusion_query_handle_t _occlusionQuery, uint32_t _depth, uint8_t _flags);
    void (*encoder_submit_indirect)(bgfx_encoder_t* _this, bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_indirect_buffer_handle_t _indirectHandle, uint32_t _start, uint32_t _num, uint32_t _depth, uint8_t _flags);
    void (*encoder_submit_indirect_count)(bgfx_encoder_t* _this, bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_indirect_buffer_handle_t _indirectHandle, uint32_t _start, bgfx_index_buffer_handle_t _numHandle, uint32_t _numIndex, uint32_t _numMax, uint32_t _depth, uint8_t _flags);
    void (*encoder_set_compute_index_buffer)(bgfx_encoder_t* _this, uint8_t _stage, bgfx_index_buffer_handle_t _handle, bgfx_access_t _access);
    void (*encoder_set_compute_vertex_buffer)(bgfx_encoder_t* _this, uint8_t _stage, bgfx_vertex_buffer_handle_t _handle, bgfx_access_t _access);
    void (*encoder_set_compute_dynamic_index_buffer)(bgfx_encoder_t* _this, uint8_t _stage, bgfx_dynamic_index_buffer_handle_t _handle, bgfx_access_t _access);
    void (*encoder_set_compute_dynamic_vertex_buffer)(bgfx_encoder_t* _this, uint8_t _stage, bgfx_dynamic_vertex_buffer_handle_t _handle, bgfx_access_t _access);
    void (*encoder_set_compute_indirect_buffer)(bgfx_encoder_t* _this, uint8_t _stage, bgfx_indirect_buffer_handle_t _handle, bgfx_access_t _access);
    void (*encoder_set_image)(bgfx_encoder_t* _this, uint8_t _stage, bgfx_texture_handle_t _handle, uint8_t _mip, bgfx_access_t _access, bgfx_texture_format_t _format);
    void (*encoder_dispatch)(bgfx_encoder_t* _this, bgfx_view_id_t _id, bgfx_program_handle_t _program, uint32_t _numX, uint32_t _numY, uint32_t _numZ, uint8_t _flags);
    void (*encoder_dispatch_indirect)(bgfx_encoder_t* _this, bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_indirect_buffer_handle_t _indirectHandle, uint32_t _start, uint32_t _num, uint8_t _flags);
    void (*encoder_discard)(bgfx_encoder_t* _this, uint8_t _flags);
    void (*encoder_blit)(bgfx_encoder_t* _this, bgfx_view_id_t _id, bgfx_texture_handle_t _dst, uint8_t _dstMip, uint16_t _dstX, uint16_t _dstY, uint16_t _dstZ, bgfx_texture_handle_t _src, uint8_t _srcMip, uint16_t _srcX, uint16_t _srcY, uint16_t _srcZ, uint16_t _width, uint16_t _height, uint16_t _depth);
    void (*request_screen_shot)(bgfx_frame_buffer_handle_t _handle, const char* _filePath);
    bgfx_render_frame_t (*render_frame)(int32_t _msecs);
    void (*set_platform_data)(const bgfx_platform_data_t * _data);
    const bgfx_internal_data_t* (*get_internal_data)(void);
    uintptr_t (*override_internal_texture_ptr)(bgfx_texture_handle_t _handle, uintptr_t _ptr);
    uintptr_t (*override_internal_texture)(bgfx_texture_handle_t _handle, uint16_t _width, uint16_t _height, uint8_t _numMips, bgfx_texture_format_t _format, uint64_t _flags);
    void (*set_marker)(const char* _name, int32_t _len);
    void (*set_state)(uint64_t _state, uint32_t _rgba);
    void (*set_condition)(bgfx_occlusion_query_handle_t _handle, _Bool _visible);
    void (*set_stencil)(uint32_t _fstencil, uint32_t _bstencil);
    uint16_t (*set_scissor)(uint16_t _x, uint16_t _y, uint16_t _width, uint16_t _height);
    void (*set_scissor_cached)(uint16_t _cache);
    uint32_t (*set_transform)(const void* _mtx, uint16_t _num);
    void (*set_transform_cached)(uint32_t _cache, uint16_t _num);
    uint32_t (*alloc_transform)(bgfx_transform_t* _transform, uint16_t _num);
    void (*set_uniform)(bgfx_uniform_handle_t _handle, const void* _value, uint16_t _num);
    void (*set_index_buffer)(bgfx_index_buffer_handle_t _handle, uint32_t _firstIndex, uint32_t _numIndices);
    void (*set_dynamic_index_buffer)(bgfx_dynamic_index_buffer_handle_t _handle, uint32_t _firstIndex, uint32_t _numIndices);
    void (*set_transient_index_buffer)(const bgfx_transient_index_buffer_t* _tib, uint32_t _firstIndex, uint32_t _numIndices);
    void (*set_vertex_buffer)(uint8_t _stream, bgfx_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices);
    void (*set_vertex_buffer_with_layout)(uint8_t _stream, bgfx_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices, bgfx_vertex_layout_handle_t _layoutHandle);
    void (*set_dynamic_vertex_buffer)(uint8_t _stream, bgfx_dynamic_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices);
    void (*set_dynamic_vertex_buffer_with_layout)(uint8_t _stream, bgfx_dynamic_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _numVertices, bgfx_vertex_layout_handle_t _layoutHandle);
    void (*set_transient_vertex_buffer)(uint8_t _stream, const bgfx_transient_vertex_buffer_t* _tvb, uint32_t _startVertex, uint32_t _numVertices);
    void (*set_transient_vertex_buffer_with_layout)(uint8_t _stream, const bgfx_transient_vertex_buffer_t* _tvb, uint32_t _startVertex, uint32_t _numVertices, bgfx_vertex_layout_handle_t _layoutHandle);
    void (*set_vertex_count)(uint32_t _numVertices);
    void (*set_instance_data_buffer)(const bgfx_instance_data_buffer_t* _idb, uint32_t _start, uint32_t _num);
    void (*set_instance_data_from_vertex_buffer)(bgfx_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _num);
    void (*set_instance_data_from_dynamic_vertex_buffer)(bgfx_dynamic_vertex_buffer_handle_t _handle, uint32_t _startVertex, uint32_t _num);
    void (*set_instance_count)(uint32_t _numInstances);
    void (*set_texture)(uint8_t _stage, bgfx_uniform_handle_t _sampler, bgfx_texture_handle_t _handle, uint32_t _flags);
    void (*touch)(bgfx_view_id_t _id);
    void (*submit)(bgfx_view_id_t _id, bgfx_program_handle_t _program, uint32_t _depth, uint8_t _flags);
    void (*submit_occlusion_query)(bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_occlusion_query_handle_t _occlusionQuery, uint32_t _depth, uint8_t _flags);
    void (*submit_indirect)(bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_indirect_buffer_handle_t _indirectHandle, uint32_t _start, uint32_t _num, uint32_t _depth, uint8_t _flags);
    void (*submit_indirect_count)(bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_indirect_buffer_handle_t _indirectHandle, uint32_t _start, bgfx_index_buffer_handle_t _numHandle, uint32_t _numIndex, uint32_t _numMax, uint32_t _depth, uint8_t _flags);
    void (*set_compute_index_buffer)(uint8_t _stage, bgfx_index_buffer_handle_t _handle, bgfx_access_t _access);
    void (*set_compute_vertex_buffer)(uint8_t _stage, bgfx_vertex_buffer_handle_t _handle, bgfx_access_t _access);
    void (*set_compute_dynamic_index_buffer)(uint8_t _stage, bgfx_dynamic_index_buffer_handle_t _handle, bgfx_access_t _access);
    void (*set_compute_dynamic_vertex_buffer)(uint8_t _stage, bgfx_dynamic_vertex_buffer_handle_t _handle, bgfx_access_t _access);
    void (*set_compute_indirect_buffer)(uint8_t _stage, bgfx_indirect_buffer_handle_t _handle, bgfx_access_t _access);
    void (*set_image)(uint8_t _stage, bgfx_texture_handle_t _handle, uint8_t _mip, bgfx_access_t _access, bgfx_texture_format_t _format);
    void (*dispatch)(bgfx_view_id_t _id, bgfx_program_handle_t _program, uint32_t _numX, uint32_t _numY, uint32_t _numZ, uint8_t _flags);
    void (*dispatch_indirect)(bgfx_view_id_t _id, bgfx_program_handle_t _program, bgfx_indirect_buffer_handle_t _indirectHandle, uint32_t _start, uint32_t _num, uint8_t _flags);
    void (*discard)(uint8_t _flags);
    void (*blit)(bgfx_view_id_t _id, bgfx_texture_handle_t _dst, uint8_t _dstMip, uint16_t _dstX, uint16_t _dstY, uint16_t _dstZ, bgfx_texture_handle_t _src, uint8_t _srcMip, uint16_t _srcX, uint16_t _srcY, uint16_t _srcZ, uint16_t _width, uint16_t _height, uint16_t _depth);
  };
  
  typedef bgfx_interface_vtbl_t* (*PFN_BGFX_GET_INTERFACE)(uint32_t _version);
  bgfx_interface_vtbl_t* bgfx_get_interface(uint32_t _version);
]])

local bgfx_api = {}
bgfx_api.BGFX_API_VERSION = 129
bgfx_api.BGFX_STATE_WRITE_R = 0x0000000000000001ULL
bgfx_api.BGFX_STATE_WRITE_G = 0x0000000000000002ULL
bgfx_api.BGFX_STATE_WRITE_B = 0x0000000000000004ULL
bgfx_api.BGFX_STATE_WRITE_A = 0x0000000000000008ULL
bgfx_api.BGFX_STATE_WRITE_Z = 0x0000004000000000ULL
bgfx_api.BGFX_STATE_DEPTH_TEST_LESS = 0x0000000000000010ULL
bgfx_api.BGFX_STATE_DEPTH_TEST_LEQUAL = 0x0000000000000020ULL
bgfx_api.BGFX_STATE_DEPTH_TEST_EQUAL = 0x0000000000000030ULL
bgfx_api.BGFX_STATE_DEPTH_TEST_GEQUAL = 0x0000000000000040ULL
bgfx_api.BGFX_STATE_DEPTH_TEST_GREATER = 0x0000000000000050ULL
bgfx_api.BGFX_STATE_DEPTH_TEST_NOTEQUAL = 0x0000000000000060ULL
bgfx_api.BGFX_STATE_DEPTH_TEST_NEVER = 0x0000000000000070ULL
bgfx_api.BGFX_STATE_DEPTH_TEST_ALWAYS = 0x0000000000000080ULL
bgfx_api.BGFX_STATE_DEPTH_TEST_SHIFT = 4
bgfx_api.BGFX_STATE_DEPTH_TEST_MASK = 0x00000000000000f0ULL
bgfx_api.BGFX_STATE_BLEND_ZERO = 0x0000000000001000ULL
bgfx_api.BGFX_STATE_BLEND_ONE = 0x0000000000002000ULL
bgfx_api.BGFX_STATE_BLEND_SRC_COLOR = 0x0000000000003000ULL
bgfx_api.BGFX_STATE_BLEND_INV_SRC_COLOR = 0x0000000000004000ULL
bgfx_api.BGFX_STATE_BLEND_SRC_ALPHA = 0x0000000000005000ULL
bgfx_api.BGFX_STATE_BLEND_INV_SRC_ALPHA = 0x0000000000006000ULL
bgfx_api.BGFX_STATE_BLEND_DST_ALPHA = 0x0000000000007000ULL
bgfx_api.BGFX_STATE_BLEND_INV_DST_ALPHA = 0x0000000000008000ULL
bgfx_api.BGFX_STATE_BLEND_DST_COLOR = 0x0000000000009000ULL
bgfx_api.BGFX_STATE_BLEND_INV_DST_COLOR = 0x000000000000a000ULL
bgfx_api.BGFX_STATE_BLEND_SRC_ALPHA_SAT = 0x000000000000b000ULL
bgfx_api.BGFX_STATE_BLEND_FACTOR = 0x000000000000c000ULL
bgfx_api.BGFX_STATE_BLEND_INV_FACTOR = 0x000000000000d000ULL
bgfx_api.BGFX_STATE_BLEND_SHIFT = 12
bgfx_api.BGFX_STATE_BLEND_MASK = 0x000000000ffff000ULL
bgfx_api.BGFX_STATE_BLEND_EQUATION_ADD = 0x0000000000000000ULL
bgfx_api.BGFX_STATE_BLEND_EQUATION_SUB = 0x0000000010000000ULL
bgfx_api.BGFX_STATE_BLEND_EQUATION_REVSUB = 0x0000000020000000ULL
bgfx_api.BGFX_STATE_BLEND_EQUATION_MIN = 0x0000000030000000ULL
bgfx_api.BGFX_STATE_BLEND_EQUATION_MAX = 0x0000000040000000ULL
bgfx_api.BGFX_STATE_BLEND_EQUATION_SHIFT = 28
bgfx_api.BGFX_STATE_BLEND_EQUATION_MASK = 0x00000003f0000000ULL
bgfx_api.BGFX_STATE_CULL_CW = 0x0000001000000000ULL
bgfx_api.BGFX_STATE_CULL_CCW = 0x0000002000000000ULL
bgfx_api.BGFX_STATE_CULL_SHIFT = 36
bgfx_api.BGFX_STATE_CULL_MASK = 0x0000003000000000ULL
bgfx_api.BGFX_STATE_ALPHA_REF_SHIFT = 40
bgfx_api.BGFX_STATE_ALPHA_REF_MASK = 0x0000ff0000000000ULL
bgfx_api.BGFX_STATE_PT_TRISTRIP = 0x0001000000000000ULL
bgfx_api.BGFX_STATE_PT_LINES = 0x0002000000000000ULL
bgfx_api.BGFX_STATE_PT_LINESTRIP = 0x0003000000000000ULL
bgfx_api.BGFX_STATE_PT_POINTS = 0x0004000000000000ULL
bgfx_api.BGFX_STATE_PT_SHIFT = 48
bgfx_api.BGFX_STATE_PT_MASK = 0x0007000000000000ULL
bgfx_api.BGFX_STATE_POINT_SIZE_SHIFT = 52
bgfx_api.BGFX_STATE_POINT_SIZE_MASK = 0x00f0000000000000ULL
bgfx_api.BGFX_STATE_MSAA = 0x0100000000000000ULL
bgfx_api.BGFX_STATE_LINEAA = 0x0200000000000000ULL
bgfx_api.BGFX_STATE_CONSERVATIVE_RASTER = 0x0400000000000000ULL
bgfx_api.BGFX_STATE_NONE = 0x0000000000000000ULL
bgfx_api.BGFX_STATE_FRONT_CCW = 0x0000008000000000ULL
bgfx_api.BGFX_STATE_BLEND_INDEPENDENT = 0x0000000400000000ULL
bgfx_api.BGFX_STATE_BLEND_ALPHA_TO_COVERAGE = 0x0000000800000000ULL
bgfx_api.BGFX_STATE_MASK = 0xffffffffffffffffULL
bgfx_api.BGFX_STATE_RESERVED_SHIFT = 61
bgfx_api.BGFX_STATE_RESERVED_MASK = 0xe000000000000000ULL
bgfx_api.BGFX_STENCIL_FUNC_REF_SHIFT = 0
bgfx_api.BGFX_STENCIL_FUNC_REF_MASK = 0x000000ff
bgfx_api.BGFX_STENCIL_FUNC_RMASK_SHIFT = 8
bgfx_api.BGFX_STENCIL_FUNC_RMASK_MASK = 0x0000ff00
bgfx_api.BGFX_STENCIL_NONE = 0x00000000
bgfx_api.BGFX_STENCIL_MASK = 0xffffffff
bgfx_api.BGFX_STENCIL_DEFAULT = 0x00000000
bgfx_api.BGFX_STENCIL_TEST_LESS = 0x00010000
bgfx_api.BGFX_STENCIL_TEST_LEQUAL = 0x00020000
bgfx_api.BGFX_STENCIL_TEST_EQUAL = 0x00030000
bgfx_api.BGFX_STENCIL_TEST_GEQUAL = 0x00040000
bgfx_api.BGFX_STENCIL_TEST_GREATER = 0x00050000
bgfx_api.BGFX_STENCIL_TEST_NOTEQUAL = 0x00060000
bgfx_api.BGFX_STENCIL_TEST_NEVER = 0x00070000
bgfx_api.BGFX_STENCIL_TEST_ALWAYS = 0x00080000
bgfx_api.BGFX_STENCIL_TEST_SHIFT = 16
bgfx_api.BGFX_STENCIL_TEST_MASK = 0x000f0000
bgfx_api.BGFX_STENCIL_OP_FAIL_S_ZERO = 0x00000000
bgfx_api.BGFX_STENCIL_OP_FAIL_S_KEEP = 0x00100000
bgfx_api.BGFX_STENCIL_OP_FAIL_S_REPLACE = 0x00200000
bgfx_api.BGFX_STENCIL_OP_FAIL_S_INCR = 0x00300000
bgfx_api.BGFX_STENCIL_OP_FAIL_S_INCRSAT = 0x00400000
bgfx_api.BGFX_STENCIL_OP_FAIL_S_DECR = 0x00500000
bgfx_api.BGFX_STENCIL_OP_FAIL_S_DECRSAT = 0x00600000
bgfx_api.BGFX_STENCIL_OP_FAIL_S_INVERT = 0x00700000
bgfx_api.BGFX_STENCIL_OP_FAIL_S_SHIFT = 20
bgfx_api.BGFX_STENCIL_OP_FAIL_S_MASK = 0x00f00000
bgfx_api.BGFX_STENCIL_OP_FAIL_Z_ZERO = 0x00000000
bgfx_api.BGFX_STENCIL_OP_FAIL_Z_KEEP = 0x01000000
bgfx_api.BGFX_STENCIL_OP_FAIL_Z_REPLACE = 0x02000000
bgfx_api.BGFX_STENCIL_OP_FAIL_Z_INCR = 0x03000000
bgfx_api.BGFX_STENCIL_OP_FAIL_Z_INCRSAT = 0x04000000
bgfx_api.BGFX_STENCIL_OP_FAIL_Z_DECR = 0x05000000
bgfx_api.BGFX_STENCIL_OP_FAIL_Z_DECRSAT = 0x06000000
bgfx_api.BGFX_STENCIL_OP_FAIL_Z_INVERT = 0x07000000
bgfx_api.BGFX_STENCIL_OP_FAIL_Z_SHIFT = 24
bgfx_api.BGFX_STENCIL_OP_FAIL_Z_MASK = 0x0f000000
bgfx_api.BGFX_STENCIL_OP_PASS_Z_ZERO = 0x00000000
bgfx_api.BGFX_STENCIL_OP_PASS_Z_KEEP = 0x10000000
bgfx_api.BGFX_STENCIL_OP_PASS_Z_REPLACE = 0x20000000
bgfx_api.BGFX_STENCIL_OP_PASS_Z_INCR = 0x30000000
bgfx_api.BGFX_STENCIL_OP_PASS_Z_INCRSAT = 0x40000000
bgfx_api.BGFX_STENCIL_OP_PASS_Z_DECR = 0x50000000
bgfx_api.BGFX_STENCIL_OP_PASS_Z_DECRSAT = 0x60000000
bgfx_api.BGFX_STENCIL_OP_PASS_Z_INVERT = 0x70000000
bgfx_api.BGFX_STENCIL_OP_PASS_Z_SHIFT = 28
bgfx_api.BGFX_STENCIL_OP_PASS_Z_MASK = 0xf0000000
bgfx_api.BGFX_CLEAR_NONE = 0x0000
bgfx_api.BGFX_CLEAR_COLOR = 0x0001
bgfx_api.BGFX_CLEAR_DEPTH = 0x0002
bgfx_api.BGFX_CLEAR_STENCIL = 0x0004
bgfx_api.BGFX_CLEAR_DISCARD_COLOR_0 = 0x0008
bgfx_api.BGFX_CLEAR_DISCARD_COLOR_1 = 0x0010
bgfx_api.BGFX_CLEAR_DISCARD_COLOR_2 = 0x0020
bgfx_api.BGFX_CLEAR_DISCARD_COLOR_3 = 0x0040
bgfx_api.BGFX_CLEAR_DISCARD_COLOR_4 = 0x0080
bgfx_api.BGFX_CLEAR_DISCARD_COLOR_5 = 0x0100
bgfx_api.BGFX_CLEAR_DISCARD_COLOR_6 = 0x0200
bgfx_api.BGFX_CLEAR_DISCARD_COLOR_7 = 0x0400
bgfx_api.BGFX_CLEAR_DISCARD_DEPTH = 0x0800
bgfx_api.BGFX_CLEAR_DISCARD_STENCIL = 0x1000
bgfx_api.BGFX_DISCARD_NONE = 0x00
bgfx_api.BGFX_DISCARD_BINDINGS = 0x01
bgfx_api.BGFX_DISCARD_INDEX_BUFFER = 0x02
bgfx_api.BGFX_DISCARD_INSTANCE_DATA = 0x04
bgfx_api.BGFX_DISCARD_STATE = 0x08
bgfx_api.BGFX_DISCARD_TRANSFORM = 0x10
bgfx_api.BGFX_DISCARD_VERTEX_STREAMS = 0x20
bgfx_api.BGFX_DISCARD_ALL = 0xff
bgfx_api.BGFX_DEBUG_NONE = 0x00000000
bgfx_api.BGFX_DEBUG_WIREFRAME = 0x00000001
bgfx_api.BGFX_DEBUG_IFH = 0x00000002
bgfx_api.BGFX_DEBUG_STATS = 0x00000004
bgfx_api.BGFX_DEBUG_TEXT = 0x00000008
bgfx_api.BGFX_DEBUG_PROFILER = 0x00000010
bgfx_api.BGFX_BUFFER_COMPUTE_FORMAT_8X1 = 0x0001
bgfx_api.BGFX_BUFFER_COMPUTE_FORMAT_8X2 = 0x0002
bgfx_api.BGFX_BUFFER_COMPUTE_FORMAT_8X4 = 0x0003
bgfx_api.BGFX_BUFFER_COMPUTE_FORMAT_16X1 = 0x0004
bgfx_api.BGFX_BUFFER_COMPUTE_FORMAT_16X2 = 0x0005
bgfx_api.BGFX_BUFFER_COMPUTE_FORMAT_16X4 = 0x0006
bgfx_api.BGFX_BUFFER_COMPUTE_FORMAT_32X1 = 0x0007
bgfx_api.BGFX_BUFFER_COMPUTE_FORMAT_32X2 = 0x0008
bgfx_api.BGFX_BUFFER_COMPUTE_FORMAT_32X4 = 0x0009
bgfx_api.BGFX_BUFFER_COMPUTE_FORMAT_SHIFT = 0
bgfx_api.BGFX_BUFFER_COMPUTE_FORMAT_MASK = 0x000f
bgfx_api.BGFX_BUFFER_COMPUTE_TYPE_INT = 0x0010
bgfx_api.BGFX_BUFFER_COMPUTE_TYPE_UINT = 0x0020
bgfx_api.BGFX_BUFFER_COMPUTE_TYPE_FLOAT = 0x0030
bgfx_api.BGFX_BUFFER_COMPUTE_TYPE_SHIFT = 4
bgfx_api.BGFX_BUFFER_COMPUTE_TYPE_MASK = 0x0030
bgfx_api.BGFX_BUFFER_NONE = 0x0000
bgfx_api.BGFX_BUFFER_COMPUTE_READ = 0x0100
bgfx_api.BGFX_BUFFER_COMPUTE_WRITE = 0x0200
bgfx_api.BGFX_BUFFER_DRAW_INDIRECT = 0x0400
bgfx_api.BGFX_BUFFER_ALLOW_RESIZE = 0x0800
bgfx_api.BGFX_BUFFER_INDEX32 = 0x1000
bgfx_api.BGFX_TEXTURE_NONE = 0x0000000000000000ULL
bgfx_api.BGFX_TEXTURE_MSAA_SAMPLE = 0x0000000800000000ULL
bgfx_api.BGFX_TEXTURE_RT = 0x0000001000000000ULL
bgfx_api.BGFX_TEXTURE_COMPUTE_WRITE = 0x0000100000000000ULL
bgfx_api.BGFX_TEXTURE_SRGB = 0x0000200000000000ULL
bgfx_api.BGFX_TEXTURE_BLIT_DST = 0x0000400000000000ULL
bgfx_api.BGFX_TEXTURE_READ_BACK = 0x0000800000000000ULL
bgfx_api.BGFX_TEXTURE_RT_MSAA_X2 = 0x0000002000000000ULL
bgfx_api.BGFX_TEXTURE_RT_MSAA_X4 = 0x0000003000000000ULL
bgfx_api.BGFX_TEXTURE_RT_MSAA_X8 = 0x0000004000000000ULL
bgfx_api.BGFX_TEXTURE_RT_MSAA_X16 = 0x0000005000000000ULL
bgfx_api.BGFX_TEXTURE_RT_MSAA_SHIFT = 36
bgfx_api.BGFX_TEXTURE_RT_MSAA_MASK = 0x0000007000000000ULL
bgfx_api.BGFX_TEXTURE_RT_WRITE_ONLY = 0x0000008000000000ULL
bgfx_api.BGFX_TEXTURE_RT_SHIFT = 36
bgfx_api.BGFX_TEXTURE_RT_MASK = 0x000000f000000000ULL
bgfx_api.BGFX_SAMPLER_U_MIRROR = 0x00000001
bgfx_api.BGFX_SAMPLER_U_CLAMP = 0x00000002
bgfx_api.BGFX_SAMPLER_U_BORDER = 0x00000003
bgfx_api.BGFX_SAMPLER_U_SHIFT = 0
bgfx_api.BGFX_SAMPLER_U_MASK = 0x00000003
bgfx_api.BGFX_SAMPLER_V_MIRROR = 0x00000004
bgfx_api.BGFX_SAMPLER_V_CLAMP = 0x00000008
bgfx_api.BGFX_SAMPLER_V_BORDER = 0x0000000c
bgfx_api.BGFX_SAMPLER_V_SHIFT = 2
bgfx_api.BGFX_SAMPLER_V_MASK = 0x0000000c
bgfx_api.BGFX_SAMPLER_W_MIRROR = 0x00000010
bgfx_api.BGFX_SAMPLER_W_CLAMP = 0x00000020
bgfx_api.BGFX_SAMPLER_W_BORDER = 0x00000030
bgfx_api.BGFX_SAMPLER_W_SHIFT = 4
bgfx_api.BGFX_SAMPLER_W_MASK = 0x00000030
bgfx_api.BGFX_SAMPLER_MIN_POINT = 0x00000040
bgfx_api.BGFX_SAMPLER_MIN_ANISOTROPIC = 0x00000080
bgfx_api.BGFX_SAMPLER_MIN_SHIFT = 6
bgfx_api.BGFX_SAMPLER_MIN_MASK = 0x000000c0
bgfx_api.BGFX_SAMPLER_MAG_POINT = 0x00000100
bgfx_api.BGFX_SAMPLER_MAG_ANISOTROPIC = 0x00000200
bgfx_api.BGFX_SAMPLER_MAG_SHIFT = 8
bgfx_api.BGFX_SAMPLER_MAG_MASK = 0x00000300
bgfx_api.BGFX_SAMPLER_MIP_POINT = 0x00000400
bgfx_api.BGFX_SAMPLER_MIP_SHIFT = 10
bgfx_api.BGFX_SAMPLER_MIP_MASK = 0x00000400
bgfx_api.BGFX_SAMPLER_COMPARE_LESS = 0x00010000
bgfx_api.BGFX_SAMPLER_COMPARE_LEQUAL = 0x00020000
bgfx_api.BGFX_SAMPLER_COMPARE_EQUAL = 0x00030000
bgfx_api.BGFX_SAMPLER_COMPARE_GEQUAL = 0x00040000
bgfx_api.BGFX_SAMPLER_COMPARE_GREATER = 0x00050000
bgfx_api.BGFX_SAMPLER_COMPARE_NOTEQUAL = 0x00060000
bgfx_api.BGFX_SAMPLER_COMPARE_NEVER = 0x00070000
bgfx_api.BGFX_SAMPLER_COMPARE_ALWAYS = 0x00080000
bgfx_api.BGFX_SAMPLER_COMPARE_SHIFT = 16
bgfx_api.BGFX_SAMPLER_COMPARE_MASK = 0x000f0000
bgfx_api.BGFX_SAMPLER_BORDER_COLOR_SHIFT = 24
bgfx_api.BGFX_SAMPLER_BORDER_COLOR_MASK = 0x0f000000
bgfx_api.BGFX_SAMPLER_RESERVED_SHIFT = 28
bgfx_api.BGFX_SAMPLER_RESERVED_MASK = 0xf0000000
bgfx_api.BGFX_SAMPLER_NONE = 0x00000000
bgfx_api.BGFX_SAMPLER_SAMPLE_STENCIL = 0x00100000
bgfx_api.BGFX_RESET_MSAA_X2 = 0x00000010
bgfx_api.BGFX_RESET_MSAA_X4 = 0x00000020
bgfx_api.BGFX_RESET_MSAA_X8 = 0x00000030
bgfx_api.BGFX_RESET_MSAA_X16 = 0x00000040
bgfx_api.BGFX_RESET_MSAA_SHIFT = 4
bgfx_api.BGFX_RESET_MSAA_MASK = 0x00000070
bgfx_api.BGFX_RESET_NONE = 0x00000000
bgfx_api.BGFX_RESET_FULLSCREEN = 0x00000001
bgfx_api.BGFX_RESET_VSYNC = 0x00000080
bgfx_api.BGFX_RESET_MAXANISOTROPY = 0x00000100
bgfx_api.BGFX_RESET_CAPTURE = 0x00000200
bgfx_api.BGFX_RESET_FLUSH_AFTER_RENDER = 0x00002000
bgfx_api.BGFX_RESET_FLIP_AFTER_RENDER = 0x00004000
bgfx_api.BGFX_RESET_SRGB_BACKBUFFER = 0x00008000
bgfx_api.BGFX_RESET_HDR10 = 0x00010000
bgfx_api.BGFX_RESET_HIDPI = 0x00020000
bgfx_api.BGFX_RESET_DEPTH_CLAMP = 0x00040000
bgfx_api.BGFX_RESET_SUSPEND = 0x00080000
bgfx_api.BGFX_RESET_TRANSPARENT_BACKBUFFER = 0x00100000
bgfx_api.BGFX_RESET_FULLSCREEN_SHIFT = 0
bgfx_api.BGFX_RESET_FULLSCREEN_MASK = 0x00000001
bgfx_api.BGFX_RESET_RESERVED_SHIFT = 31
bgfx_api.BGFX_RESET_RESERVED_MASK = 0x80000000
bgfx_api.BGFX_CAPS_ALPHA_TO_COVERAGE = 0x0000000000000001ULL
bgfx_api.BGFX_CAPS_BLEND_INDEPENDENT = 0x0000000000000002ULL
bgfx_api.BGFX_CAPS_COMPUTE = 0x0000000000000004ULL
bgfx_api.BGFX_CAPS_CONSERVATIVE_RASTER = 0x0000000000000008ULL
bgfx_api.BGFX_CAPS_DRAW_INDIRECT = 0x0000000000000010ULL
bgfx_api.BGFX_CAPS_DRAW_INDIRECT_COUNT = 0x0000000000000020ULL
bgfx_api.BGFX_CAPS_FRAGMENT_DEPTH = 0x0000000000000040ULL
bgfx_api.BGFX_CAPS_FRAGMENT_ORDERING = 0x0000000000000080ULL
bgfx_api.BGFX_CAPS_GRAPHICS_DEBUGGER = 0x0000000000000100ULL
bgfx_api.BGFX_CAPS_HDR10 = 0x0000000000000200ULL
bgfx_api.BGFX_CAPS_HIDPI = 0x0000000000000400ULL
bgfx_api.BGFX_CAPS_IMAGE_RW = 0x0000000000000800ULL
bgfx_api.BGFX_CAPS_INDEX32 = 0x0000000000001000ULL
bgfx_api.BGFX_CAPS_INSTANCING = 0x0000000000002000ULL
bgfx_api.BGFX_CAPS_OCCLUSION_QUERY = 0x0000000000004000ULL
bgfx_api.BGFX_CAPS_PRIMITIVE_ID = 0x0000000000008000ULL
bgfx_api.BGFX_CAPS_RENDERER_MULTITHREADED = 0x0000000000010000ULL
bgfx_api.BGFX_CAPS_SWAP_CHAIN = 0x0000000000020000ULL
bgfx_api.BGFX_CAPS_TEXTURE_BLIT = 0x0000000000040000ULL
bgfx_api.BGFX_CAPS_TEXTURE_COMPARE_LEQUAL = 0x0000000000080000ULL
bgfx_api.BGFX_CAPS_TEXTURE_COMPARE_RESERVED = 0x0000000000100000ULL
bgfx_api.BGFX_CAPS_TEXTURE_CUBE_ARRAY = 0x0000000000200000ULL
bgfx_api.BGFX_CAPS_TEXTURE_DIRECT_ACCESS = 0x0000000000400000ULL
bgfx_api.BGFX_CAPS_TEXTURE_READ_BACK = 0x0000000000800000ULL
bgfx_api.BGFX_CAPS_TEXTURE_2D_ARRAY = 0x0000000001000000ULL
bgfx_api.BGFX_CAPS_TEXTURE_3D = 0x0000000002000000ULL
bgfx_api.BGFX_CAPS_TRANSPARENT_BACKBUFFER = 0x0000000004000000ULL
bgfx_api.BGFX_CAPS_VERTEX_ATTRIB_HALF = 0x0000000008000000ULL
bgfx_api.BGFX_CAPS_VERTEX_ATTRIB_UINT10 = 0x0000000010000000ULL
bgfx_api.BGFX_CAPS_VERTEX_ID = 0x0000000020000000ULL
bgfx_api.BGFX_CAPS_VIEWPORT_LAYER_ARRAY = 0x0000000040000000ULL
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_NONE = 0x00000000
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_2D = 0x00000001
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_2D_SRGB = 0x00000002
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_2D_EMULATED = 0x00000004
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_3D = 0x00000008
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_3D_SRGB = 0x00000010
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_3D_EMULATED = 0x00000020
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_CUBE = 0x00000040
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_CUBE_SRGB = 0x00000080
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_CUBE_EMULATED = 0x00000100
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_VERTEX = 0x00000200
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_IMAGE_READ = 0x00000400
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_IMAGE_WRITE = 0x00000800
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_FRAMEBUFFER = 0x00001000
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_FRAMEBUFFER_MSAA = 0x00002000
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_MSAA = 0x00004000
bgfx_api.BGFX_CAPS_FORMAT_TEXTURE_MIP_AUTOGEN = 0x00008000
bgfx_api.BGFX_RESOLVE_NONE = 0x00
bgfx_api.BGFX_RESOLVE_AUTO_GEN_MIPS = 0x01
bgfx_api.BGFX_PCI_ID_NONE = 0x0000
bgfx_api.BGFX_PCI_ID_SOFTWARE_RASTERIZER = 0x0001
bgfx_api.BGFX_PCI_ID_AMD = 0x1002
bgfx_api.BGFX_PCI_ID_APPLE = 0x106b
bgfx_api.BGFX_PCI_ID_INTEL = 0x8086
bgfx_api.BGFX_PCI_ID_NVIDIA = 0x10de
bgfx_api.BGFX_PCI_ID_MICROSOFT = 0x1414
bgfx_api.BGFX_PCI_ID_ARM = 0x13b5
bgfx_api.BGFX_CUBE_MAP_POSITIVE_X = 0x00
bgfx_api.BGFX_CUBE_MAP_NEGATIVE_X = 0x01
bgfx_api.BGFX_CUBE_MAP_POSITIVE_Y = 0x02
bgfx_api.BGFX_CUBE_MAP_NEGATIVE_Y = 0x03
bgfx_api.BGFX_CUBE_MAP_POSITIVE_Z = 0x04
bgfx_api.BGFX_CUBE_MAP_NEGATIVE_Z = 0x05

-- Enable RGB write.
bgfx_api.BGFX_STATE_WRITE_RGB 					= bgfx_api.BGFX_STATE_WRITE_R+bgfx_api.BGFX_STATE_WRITE_G+bgfx_api.BGFX_STATE_WRITE_B
bgfx_api.BGFX_STATE_WRITE_RGBA 					= bgfx_api.BGFX_STATE_WRITE_R+bgfx_api.BGFX_STATE_WRITE_G+bgfx_api.BGFX_STATE_WRITE_B+bgfx_api.BGFX_STATE_WRITE_A

-- Write all channels mask.
bgfx_api.BGFX_STATE_WRITE_MASK 					= bgfx_api.BGFX_STATE_WRITE_RGB+bgfx_api.BGFX_STATE_WRITE_A+bgfx_api.BGFX_STATE_WRITE_Z

bgfx_api.BGFX_STATE_ALPHA_REF 					= function(v) bit.band( bit.lshift(v,bgfx_api.BGFX_STATE_ALPHA_REF_SHIFT), bgfx_api.BGFX_STATE_ALPHA_REF_MASK ) end
bgfx_api.BGFX_STATE_POINT_SIZE 					= function(v) bit.band( bit.lshift(v,bgfx_api.BGFX_STATE_POINT_SIZE_SHIFT), bgfx_api.BGFX_STATE_POINT_SIZE_MASK ) end
bgfx_api.BGFX_STATE_DEFAULT  					= bgfx_api.BGFX_STATE_WRITE_RGB+bgfx_api.BGFX_STATE_WRITE_A+bgfx_api.BGFX_STATE_WRITE_Z+bgfx_api.BGFX_STATE_DEPTH_TEST_LESS+bgfx_api.BGFX_STATE_CULL_CW+bgfx_api.BGFX_STATE_MSAA
bgfx_api.BGFX_STATE_ALPHA_REF 					= function(v) bit.band( bit.lshift(v,bgfx_api.BGFX_STENCIL_FUNC_REF_SHIFT), bgfx_api.BGFX_STENCIL_FUNC_REF_MASK ) end
bgfx_api.BGFX_STENCIL_FUNC_RMASK 				= function(v) bit.band( bit.lshift(v,bgfx_api.BGFX_STENCIL_FUNC_RMASK_SHIFT), bgfx_api.BGFX_STENCIL_FUNC_RMASK_MASK ) end
bgfx_api.BGFX_CLEAR_DISCARD_COLOR_MASK 		 	= (
												bgfx_api.BGFX_CLEAR_DISCARD_COLOR_0+
												bgfx_api.BGFX_CLEAR_DISCARD_COLOR_1+
												bgfx_api.BGFX_CLEAR_DISCARD_COLOR_2+
												bgfx_api.BGFX_CLEAR_DISCARD_COLOR_3+
												bgfx_api.BGFX_CLEAR_DISCARD_COLOR_4+
												bgfx_api.BGFX_CLEAR_DISCARD_COLOR_5+
												bgfx_api.BGFX_CLEAR_DISCARD_COLOR_6+
												bgfx_api.BGFX_CLEAR_DISCARD_COLOR_7
												)

bgfx_api.BGFX_CLEAR_DISCARD_MASK 					= bgfx_api.BGFX_CLEAR_DISCARD_COLOR_MASK+bgfx_api.BGFX_CLEAR_DISCARD_DEPTH+bgfx_api.BGFX_CLEAR_DISCARD_STENCIL
bgfx_api.BGFX_BUFFER_COMPUTE_READ_WRITE		 		= bgfx_api.BGFX_BUFFER_COMPUTE_READ+bgfx_api.BGFX_BUFFER_COMPUTE_WRITE
bgfx_api.BGFX_SAMPLER_BORDER_COLOR 					= function(v) bit.band( bit.lshift(v,bgfx_api.BGFX_SAMPLER_BORDER_COLOR_SHIFT), bgfx_api.BGFX_SAMPLER_BORDER_COLOR_MASK ) end
bgfx_api.BGFX_SAMPLER_POINT 						= bgfx_api.BGFX_SAMPLER_MIN_POINT+bgfx_api.BGFX_SAMPLER_MAG_POINT+bgfx_api.BGFX_SAMPLER_MIP_POINT
bgfx_api.BGFX_SAMPLER_UVW_MIRROR 			 		= bgfx_api.BGFX_SAMPLER_U_MIRROR+bgfx_api.BGFX_SAMPLER_V_MIRROR+bgfx_api.BGFX_SAMPLER_W_MIRROR
bgfx_api.BGFX_SAMPLER_UVW_CLAMP				 		= bgfx_api.BGFX_SAMPLER_U_CLAMP+bgfx_api.BGFX_SAMPLER_V_CLAMP+bgfx_api.BGFX_SAMPLER_W_CLAMP
bgfx_api.BGFX_SAMPLER_UVW_BORDER 			 		= bgfx_api.BGFX_SAMPLER_U_BORDER+bgfx_api.BGFX_SAMPLER_V_BORDER+bgfx_api.BGFX_SAMPLER_W_BORDER
bgfx_api.BGFX_SAMPLER_BITS_MASK				 		= bgfx_api.BGFX_SAMPLER_U_MASK+bgfx_api.BGFX_SAMPLER_V_MASK+bgfx_api.BGFX_SAMPLER_W_MASK+bgfx_api.BGFX_SAMPLER_MIN_MASK+bgfx_api.BGFX_SAMPLER_MAG_MASK+bgfx_api.BGFX_SAMPLER_MIP_MASK+bgfx_api.BGFX_SAMPLER_COMPARE_MASK
bgfx_api.BGFX_CAPS_TEXTURE_COMPARE_ALL 		 		= bgfx_api.BGFX_CAPS_TEXTURE_COMPARE_RESERVED+bgfx_api.BGFX_CAPS_TEXTURE_COMPARE_LEQUAL

-- Blend function separate.
bgfx_api.BGFX_STATE_BLEND_FUNC_SEPARATE = function(srcRGB,dstRGB,srcA,dstA)
	return 0 + (srcRGB + bit.lshift(dstRGB,4)) + bit.lshift((srcA + bit.lshift(dstA,4)),8)
end

-- Blend equation separate.
bgfx_api.BGFX_STATE_BLEND_EQUATION_SEPARATE = function(_equationRGB,_equationA)
	return _equationRGB + bit.lshift(_equationA,3)
end

-- Blend function.
bgfx_api.BGFX_STATE_BLEND_FUNC = function(src,dst)
	return bgfx_api.BGFX_STATE_BLEND_FUNC_SEPARATE(src,dst,src,dst)
end

-- Blend equation.
bgfx_api.BGFX_STATE_BLEND_EQUATION = function(_equation) 
	return bgfx_api.BGFX_STATE_BLEND_EQUATION_SEPARATE(_equation, _equation) 
end


-- Utility predefined blend modes.

-- Additive blending.
bgfx_api.BGFX_STATE_BLEND_ADD = bgfx_api.BGFX_STATE_BLEND_FUNC(bgfx_api.BGFX_STATE_BLEND_ONE,bgfx_api.BGFX_STATE_BLEND_ONE)

-- Alpha blend.
bgfx_api.BGFX_STATE_BLEND_ALPHA = bgfx_api.BGFX_STATE_BLEND_FUNC(bgfx_api.BGFX_STATE_BLEND_SRC_ALPHA,bgfx_api.BGFX_STATE_BLEND_INV_SRC_ALPHA)

-- Selects darker color of blend.
bgfx_api.BGFX_STATE_BLEND_DARKEN = bgfx_api.BGFX_STATE_BLEND_FUNC(bgfx_api.BGFX_STATE_BLEND_ONE,bgfx_api.BGFX_STATE_BLEND_ONE)+bgfx_api.BGFX_STATE_BLEND_EQUATION(bgfx_api.BGFX_STATE_BLEND_EQUATION_MIN)

-- Selects lighter color of blend.
bgfx_api.BGFX_STATE_BLEND_LIGHTEN = bgfx_api.BGFX_STATE_BLEND_FUNC(bgfx_api.BGFX_STATE_BLEND_ONE,bgfx_api.BGFX_STATE_BLEND_ONE)+bgfx_api.BGFX_STATE_BLEND_EQUATION(bgfx_api.BGFX_STATE_BLEND_EQUATION_MAX)

-- Multiplies colors.
bgfx_api.BGFX_STATE_BLEND_MULTIPLY = bgfx_api.BGFX_STATE_BLEND_FUNC(bgfx_api.BGFX_STATE_BLEND_DST_COLOR,bgfx_api.BGFX_STATE_BLEND_ZERO)

-- Opaque pixels will cover the pixels directly below them without any math or algorithm applied to them.
bgfx_api.BGFX_STATE_BLEND_NORMAL = bgfx_api.BGFX_STATE_BLEND_FUNC(bgfx_api.BGFX_STATE_BLEND_ONE,bgfx_api.BGFX_STATE_BLEND_INV_SRC_ALPHA)

-- Multiplies the inverse of the blend and base colors.
bgfx_api.BGFX_STATE_BLEND_SCREEN = bgfx_api.BGFX_STATE_BLEND_FUNC(bgfx_api.BGFX_STATE_BLEND_ONE,bgfx_api.BGFX_STATE_BLEND_INV_SRC_COLOR)

-- Decreases the brightness of the base color based on the value of the blend color.
bgfx_api.BGFX_STATE_BLEND_LINEAR_BURN = bgfx_api.BGFX_STATE_BLEND_FUNC(bgfx_api.BGFX_STATE_BLEND_DST_COLOR,bgfx_api.BGFX_STATE_BLEND_INV_DST_COLOR)+bgfx_api.BGFX_STATE_BLEND_EQUATION(bgfx_api.BGFX_STATE_BLEND_EQUATION_SUB)

--
bgfx_api.BGFX_STATE_BLEND_FUNC_RT_x = function(_src, _dst)
	return bit.rshift(_src,bgfx_api.BGFX_STATE_BLEND_SHIFT) + bit.lshift(bit.rshift(_src,bgfx_api.BGFX_STATE_BLEND_SHIFT),4)
end

--
bgfx_api.BGFX_STATE_BLEND_FUNC_RT_xE = function(_src, _dst, _equation)
	return bgfx_api.BGFX_STATE_BLEND_FUNC_RT_x(_src,_dst) + bit.lshift(bit.rshift(_equation,bgfx_api.BGFX_STATE_BLEND_EQUATION_SHIFT),8)
end

bgfx_api.BGFX_STATE_BLEND_FUNC_RT_1 = function(_src, _dst)  return bit.lshift(bgfx_api.BGFX_STATE_BLEND_FUNC_RT_x(_src, _dst),0) end
bgfx_api.BGFX_STATE_BLEND_FUNC_RT_2 = function(_src, _dst)  return bit.lshift(bgfx_api.BGFX_STATE_BLEND_FUNC_RT_x(_src, _dst),11) end
bgfx_api.BGFX_STATE_BLEND_FUNC_RT_3 = function(_src, _dst)  return bit.lshift(bgfx_api.BGFX_STATE_BLEND_FUNC_RT_x(_src, _dst),22) end

bgfx_api.BGFX_STATE_BLEND_FUNC_RT_1E = function(_src, _dst, _equation)  return bit.lshift(bgfx_api.BGFX_STATE_BLEND_FUNC_RT_xE(_src, _dst, _equation),0) end
bgfx_api.BGFX_STATE_BLEND_FUNC_RT_2E = function(_src, _dst, _equation)  return bit.lshift(bgfx_api.BGFX_STATE_BLEND_FUNC_RT_xE(_src, _dst, _equation),11) end
bgfx_api.BGFX_STATE_BLEND_FUNC_RT_3E = function(_src, _dst, _equation)  return bit.lshift(bgfx_api.BGFX_STATE_BLEND_FUNC_RT_xE(_src, _dst, _equation),22) end
-- from typedef enum bgfx_attrib_type (bgfx_attrib_type_t)
bgfx_api.BGFX_ATTRIB_TYPE_UINT8 = 0
bgfx_api.BGFX_ATTRIB_TYPE_UINT10 = 1
bgfx_api.BGFX_ATTRIB_TYPE_INT16 = 2
bgfx_api.BGFX_ATTRIB_TYPE_HALF = 3
bgfx_api.BGFX_ATTRIB_TYPE_FLOAT = 4
bgfx_api.BGFX_ATTRIB_TYPE_COUNT = 5

-- from typedef enum bgfx_texture_format (bgfx_texture_format_t)
bgfx_api.BGFX_TEXTURE_FORMAT_BC1 = 0
bgfx_api.BGFX_TEXTURE_FORMAT_BC2 = 1
bgfx_api.BGFX_TEXTURE_FORMAT_BC3 = 2
bgfx_api.BGFX_TEXTURE_FORMAT_BC4 = 3
bgfx_api.BGFX_TEXTURE_FORMAT_BC5 = 4
bgfx_api.BGFX_TEXTURE_FORMAT_BC6H = 5
bgfx_api.BGFX_TEXTURE_FORMAT_BC7 = 6
bgfx_api.BGFX_TEXTURE_FORMAT_ETC1 = 7
bgfx_api.BGFX_TEXTURE_FORMAT_ETC2 = 8
bgfx_api.BGFX_TEXTURE_FORMAT_ETC2A = 9
bgfx_api.BGFX_TEXTURE_FORMAT_ETC2A1 = 10
bgfx_api.BGFX_TEXTURE_FORMAT_PTC12 = 11
bgfx_api.BGFX_TEXTURE_FORMAT_PTC14 = 12
bgfx_api.BGFX_TEXTURE_FORMAT_PTC12A = 13
bgfx_api.BGFX_TEXTURE_FORMAT_PTC14A = 14
bgfx_api.BGFX_TEXTURE_FORMAT_PTC22 = 15
bgfx_api.BGFX_TEXTURE_FORMAT_PTC24 = 16
bgfx_api.BGFX_TEXTURE_FORMAT_ATC = 17
bgfx_api.BGFX_TEXTURE_FORMAT_ATCE = 18
bgfx_api.BGFX_TEXTURE_FORMAT_ATCI = 19
bgfx_api.BGFX_TEXTURE_FORMAT_ASTC4X4 = 20
bgfx_api.BGFX_TEXTURE_FORMAT_ASTC5X4 = 21
bgfx_api.BGFX_TEXTURE_FORMAT_ASTC5X5 = 22
bgfx_api.BGFX_TEXTURE_FORMAT_ASTC6X5 = 23
bgfx_api.BGFX_TEXTURE_FORMAT_ASTC6X6 = 24
bgfx_api.BGFX_TEXTURE_FORMAT_ASTC8X5 = 25
bgfx_api.BGFX_TEXTURE_FORMAT_ASTC8X6 = 26
bgfx_api.BGFX_TEXTURE_FORMAT_ASTC8X8 = 27
bgfx_api.BGFX_TEXTURE_FORMAT_ASTC10X5 = 28
bgfx_api.BGFX_TEXTURE_FORMAT_ASTC10X6 = 29
bgfx_api.BGFX_TEXTURE_FORMAT_ASTC10X8 = 30
bgfx_api.BGFX_TEXTURE_FORMAT_ASTC10X10 = 31
bgfx_api.BGFX_TEXTURE_FORMAT_ASTC12X10 = 32
bgfx_api.BGFX_TEXTURE_FORMAT_ASTC12X12 = 33
bgfx_api.BGFX_TEXTURE_FORMAT_UNKNOWN = 34
bgfx_api.BGFX_TEXTURE_FORMAT_R1 = 35
bgfx_api.BGFX_TEXTURE_FORMAT_A8 = 36
bgfx_api.BGFX_TEXTURE_FORMAT_R8 = 37
bgfx_api.BGFX_TEXTURE_FORMAT_R8I = 38
bgfx_api.BGFX_TEXTURE_FORMAT_R8U = 39
bgfx_api.BGFX_TEXTURE_FORMAT_R8S = 40
bgfx_api.BGFX_TEXTURE_FORMAT_R16 = 41
bgfx_api.BGFX_TEXTURE_FORMAT_R16I = 42
bgfx_api.BGFX_TEXTURE_FORMAT_R16U = 43
bgfx_api.BGFX_TEXTURE_FORMAT_R16F = 44
bgfx_api.BGFX_TEXTURE_FORMAT_R16S = 45
bgfx_api.BGFX_TEXTURE_FORMAT_R32I = 46
bgfx_api.BGFX_TEXTURE_FORMAT_R32U = 47
bgfx_api.BGFX_TEXTURE_FORMAT_R32F = 48
bgfx_api.BGFX_TEXTURE_FORMAT_RG8 = 49
bgfx_api.BGFX_TEXTURE_FORMAT_RG8I = 50
bgfx_api.BGFX_TEXTURE_FORMAT_RG8U = 51
bgfx_api.BGFX_TEXTURE_FORMAT_RG8S = 52
bgfx_api.BGFX_TEXTURE_FORMAT_RG16 = 53
bgfx_api.BGFX_TEXTURE_FORMAT_RG16I = 54
bgfx_api.BGFX_TEXTURE_FORMAT_RG16U = 55
bgfx_api.BGFX_TEXTURE_FORMAT_RG16F = 56
bgfx_api.BGFX_TEXTURE_FORMAT_RG16S = 57
bgfx_api.BGFX_TEXTURE_FORMAT_RG32I = 58
bgfx_api.BGFX_TEXTURE_FORMAT_RG32U = 59
bgfx_api.BGFX_TEXTURE_FORMAT_RG32F = 60
bgfx_api.BGFX_TEXTURE_FORMAT_RGB8 = 61
bgfx_api.BGFX_TEXTURE_FORMAT_RGB8I = 62
bgfx_api.BGFX_TEXTURE_FORMAT_RGB8U = 63
bgfx_api.BGFX_TEXTURE_FORMAT_RGB8S = 64
bgfx_api.BGFX_TEXTURE_FORMAT_RGB9E5F = 65
bgfx_api.BGFX_TEXTURE_FORMAT_BGRA8 = 66
bgfx_api.BGFX_TEXTURE_FORMAT_RGBA8 = 67
bgfx_api.BGFX_TEXTURE_FORMAT_RGBA8I = 68
bgfx_api.BGFX_TEXTURE_FORMAT_RGBA8U = 69
bgfx_api.BGFX_TEXTURE_FORMAT_RGBA8S = 70
bgfx_api.BGFX_TEXTURE_FORMAT_RGBA16 = 71
bgfx_api.BGFX_TEXTURE_FORMAT_RGBA16I = 72
bgfx_api.BGFX_TEXTURE_FORMAT_RGBA16U = 73
bgfx_api.BGFX_TEXTURE_FORMAT_RGBA16F = 74
bgfx_api.BGFX_TEXTURE_FORMAT_RGBA16S = 75
bgfx_api.BGFX_TEXTURE_FORMAT_RGBA32I = 76
bgfx_api.BGFX_TEXTURE_FORMAT_RGBA32U = 77
bgfx_api.BGFX_TEXTURE_FORMAT_RGBA32F = 78
bgfx_api.BGFX_TEXTURE_FORMAT_B5G6R5 = 79
bgfx_api.BGFX_TEXTURE_FORMAT_R5G6B5 = 80
bgfx_api.BGFX_TEXTURE_FORMAT_BGRA4 = 81
bgfx_api.BGFX_TEXTURE_FORMAT_RGBA4 = 82
bgfx_api.BGFX_TEXTURE_FORMAT_BGR5A1 = 83
bgfx_api.BGFX_TEXTURE_FORMAT_RGB5A1 = 84
bgfx_api.BGFX_TEXTURE_FORMAT_RGB10A2 = 85
bgfx_api.BGFX_TEXTURE_FORMAT_RG11B10F = 86
bgfx_api.BGFX_TEXTURE_FORMAT_UNKNOWNDEPTH = 87
bgfx_api.BGFX_TEXTURE_FORMAT_D16 = 88
bgfx_api.BGFX_TEXTURE_FORMAT_D24 = 89
bgfx_api.BGFX_TEXTURE_FORMAT_D24S8 = 90
bgfx_api.BGFX_TEXTURE_FORMAT_D32 = 91
bgfx_api.BGFX_TEXTURE_FORMAT_D16F = 92
bgfx_api.BGFX_TEXTURE_FORMAT_D24F = 93
bgfx_api.BGFX_TEXTURE_FORMAT_D32F = 94
bgfx_api.BGFX_TEXTURE_FORMAT_D0S8 = 95
bgfx_api.BGFX_TEXTURE_FORMAT_COUNT = 96

-- from typedef enum bgfx_access (bgfx_access_t)
bgfx_api.BGFX_ACCESS_READ = 0
bgfx_api.BGFX_ACCESS_WRITE = 1
bgfx_api.BGFX_ACCESS_READWRITE = 2
bgfx_api.BGFX_ACCESS_COUNT = 3

-- from typedef enum bgfx_render_frame (bgfx_render_frame_t)
bgfx_api.BGFX_RENDER_FRAME_NO_CONTEXT = 0
bgfx_api.BGFX_RENDER_FRAME_RENDER = 1
bgfx_api.BGFX_RENDER_FRAME_TIMEOUT = 2
bgfx_api.BGFX_RENDER_FRAME_EXITING = 3
bgfx_api.BGFX_RENDER_FRAME_COUNT = 4

-- from typedef enum bgfx_topology_sort (bgfx_topology_sort_t)
bgfx_api.BGFX_TOPOLOGY_SORT_DIRECTION_FRONT_TO_BACK_MIN = 0
bgfx_api.BGFX_TOPOLOGY_SORT_DIRECTION_FRONT_TO_BACK_AVG = 1
bgfx_api.BGFX_TOPOLOGY_SORT_DIRECTION_FRONT_TO_BACK_MAX = 2
bgfx_api.BGFX_TOPOLOGY_SORT_DIRECTION_BACK_TO_FRONT_MIN = 3
bgfx_api.BGFX_TOPOLOGY_SORT_DIRECTION_BACK_TO_FRONT_AVG = 4
bgfx_api.BGFX_TOPOLOGY_SORT_DIRECTION_BACK_TO_FRONT_MAX = 5
bgfx_api.BGFX_TOPOLOGY_SORT_DISTANCE_FRONT_TO_BACK_MIN = 6
bgfx_api.BGFX_TOPOLOGY_SORT_DISTANCE_FRONT_TO_BACK_AVG = 7
bgfx_api.BGFX_TOPOLOGY_SORT_DISTANCE_FRONT_TO_BACK_MAX = 8
bgfx_api.BGFX_TOPOLOGY_SORT_DISTANCE_BACK_TO_FRONT_MIN = 9
bgfx_api.BGFX_TOPOLOGY_SORT_DISTANCE_BACK_TO_FRONT_AVG = 10
bgfx_api.BGFX_TOPOLOGY_SORT_DISTANCE_BACK_TO_FRONT_MAX = 11
bgfx_api.BGFX_TOPOLOGY_SORT_COUNT = 12

-- from typedef enum bgfx_native_window_handle_type (bgfx_native_window_handle_type_t)
bgfx_api.BGFX_NATIVE_WINDOW_HANDLE_TYPE_DEFAULT = 0
bgfx_api.BGFX_NATIVE_WINDOW_HANDLE_TYPE_WAYLAND = 1
bgfx_api.BGFX_NATIVE_WINDOW_HANDLE_TYPE_COUNT = 2

-- from typedef enum bgfx_uniform_type (bgfx_uniform_type_t)
bgfx_api.BGFX_UNIFORM_TYPE_SAMPLER = 0
bgfx_api.BGFX_UNIFORM_TYPE_END = 1
bgfx_api.BGFX_UNIFORM_TYPE_VEC4 = 2
bgfx_api.BGFX_UNIFORM_TYPE_MAT3 = 3
bgfx_api.BGFX_UNIFORM_TYPE_MAT4 = 4
bgfx_api.BGFX_UNIFORM_TYPE_COUNT = 5

-- from typedef enum bgfx_occlusion_query_result (bgfx_occlusion_query_result_t)
bgfx_api.BGFX_OCCLUSION_QUERY_RESULT_INVISIBLE = 0
bgfx_api.BGFX_OCCLUSION_QUERY_RESULT_VISIBLE = 1
bgfx_api.BGFX_OCCLUSION_QUERY_RESULT_NORESULT = 2
bgfx_api.BGFX_OCCLUSION_QUERY_RESULT_COUNT = 3

-- from typedef enum bgfx_topology (bgfx_topology_t)
bgfx_api.BGFX_TOPOLOGY_TRI_LIST = 0
bgfx_api.BGFX_TOPOLOGY_TRI_STRIP = 1
bgfx_api.BGFX_TOPOLOGY_LINE_LIST = 2
bgfx_api.BGFX_TOPOLOGY_LINE_STRIP = 3
bgfx_api.BGFX_TOPOLOGY_POINT_LIST = 4
bgfx_api.BGFX_TOPOLOGY_COUNT = 5

-- from typedef enum bgfx_fatal (bgfx_fatal_t)
bgfx_api.BGFX_FATAL_DEBUG_CHECK = 0
bgfx_api.BGFX_FATAL_INVALID_SHADER = 1
bgfx_api.BGFX_FATAL_UNABLE_TO_INITIALIZE = 2
bgfx_api.BGFX_FATAL_UNABLE_TO_CREATE_TEXTURE = 3
bgfx_api.BGFX_FATAL_DEVICE_LOST = 4
bgfx_api.BGFX_FATAL_COUNT = 5

-- from typedef enum bgfx_topology_convert (bgfx_topology_convert_t)
bgfx_api.BGFX_TOPOLOGY_CONVERT_TRI_LIST_FLIP_WINDING = 0
bgfx_api.BGFX_TOPOLOGY_CONVERT_TRI_STRIP_FLIP_WINDING = 1
bgfx_api.BGFX_TOPOLOGY_CONVERT_TRI_LIST_TO_LINE_LIST = 2
bgfx_api.BGFX_TOPOLOGY_CONVERT_TRI_STRIP_TO_TRI_LIST = 3
bgfx_api.BGFX_TOPOLOGY_CONVERT_LINE_STRIP_TO_LINE_LIST = 4
bgfx_api.BGFX_TOPOLOGY_CONVERT_COUNT = 5

-- from typedef enum bgfx_view_mode (bgfx_view_mode_t)
bgfx_api.BGFX_VIEW_MODE_DEFAULT = 0
bgfx_api.BGFX_VIEW_MODE_SEQUENTIAL = 1
bgfx_api.BGFX_VIEW_MODE_DEPTH_ASCENDING = 2
bgfx_api.BGFX_VIEW_MODE_DEPTH_DESCENDING = 3
bgfx_api.BGFX_VIEW_MODE_COUNT = 4

-- from typedef enum bgfx_renderer_type (bgfx_renderer_type_t)
bgfx_api.BGFX_RENDERER_TYPE_NOOP = 0
bgfx_api.BGFX_RENDERER_TYPE_AGC = 1
bgfx_api.BGFX_RENDERER_TYPE_DIRECT3D11 = 2
bgfx_api.BGFX_RENDERER_TYPE_DIRECT3D12 = 3
bgfx_api.BGFX_RENDERER_TYPE_GNM = 4
bgfx_api.BGFX_RENDERER_TYPE_METAL = 5
bgfx_api.BGFX_RENDERER_TYPE_NVN = 6
bgfx_api.BGFX_RENDERER_TYPE_OPENGLES = 7
bgfx_api.BGFX_RENDERER_TYPE_OPENGL = 8
bgfx_api.BGFX_RENDERER_TYPE_VULKAN = 9
bgfx_api.BGFX_RENDERER_TYPE_COUNT = 10

-- from typedef enum bgfx_attrib (bgfx_attrib_t)
bgfx_api.BGFX_ATTRIB_POSITION = 0
bgfx_api.BGFX_ATTRIB_NORMAL = 1
bgfx_api.BGFX_ATTRIB_TANGENT = 2
bgfx_api.BGFX_ATTRIB_BITANGENT = 3
bgfx_api.BGFX_ATTRIB_COLOR0 = 4
bgfx_api.BGFX_ATTRIB_COLOR1 = 5
bgfx_api.BGFX_ATTRIB_COLOR2 = 6
bgfx_api.BGFX_ATTRIB_COLOR3 = 7
bgfx_api.BGFX_ATTRIB_INDICES = 8
bgfx_api.BGFX_ATTRIB_WEIGHT = 9
bgfx_api.BGFX_ATTRIB_TEXCOORD0 = 10
bgfx_api.BGFX_ATTRIB_TEXCOORD1 = 11
bgfx_api.BGFX_ATTRIB_TEXCOORD2 = 12
bgfx_api.BGFX_ATTRIB_TEXCOORD3 = 13
bgfx_api.BGFX_ATTRIB_TEXCOORD4 = 14
bgfx_api.BGFX_ATTRIB_TEXCOORD5 = 15
bgfx_api.BGFX_ATTRIB_TEXCOORD6 = 16
bgfx_api.BGFX_ATTRIB_TEXCOORD7 = 17
bgfx_api.BGFX_ATTRIB_COUNT = 18

-- from typedef enum bgfx_function_id (bgfx_function_id_t)
bgfx_api.BGFX_FUNCTION_ID_ATTACHMENT_INIT = 0
bgfx_api.BGFX_FUNCTION_ID_VERTEX_LAYOUT_BEGIN = 1
bgfx_api.BGFX_FUNCTION_ID_VERTEX_LAYOUT_ADD = 2
bgfx_api.BGFX_FUNCTION_ID_VERTEX_LAYOUT_DECODE = 3
bgfx_api.BGFX_FUNCTION_ID_VERTEX_LAYOUT_HAS = 4
bgfx_api.BGFX_FUNCTION_ID_VERTEX_LAYOUT_SKIP = 5
bgfx_api.BGFX_FUNCTION_ID_VERTEX_LAYOUT_END = 6
bgfx_api.BGFX_FUNCTION_ID_VERTEX_LAYOUT_GET_OFFSET = 7
bgfx_api.BGFX_FUNCTION_ID_VERTEX_LAYOUT_GET_STRIDE = 8
bgfx_api.BGFX_FUNCTION_ID_VERTEX_LAYOUT_GET_SIZE = 9
bgfx_api.BGFX_FUNCTION_ID_VERTEX_PACK = 10
bgfx_api.BGFX_FUNCTION_ID_VERTEX_UNPACK = 11
bgfx_api.BGFX_FUNCTION_ID_VERTEX_CONVERT = 12
bgfx_api.BGFX_FUNCTION_ID_WELD_VERTICES = 13
bgfx_api.BGFX_FUNCTION_ID_TOPOLOGY_CONVERT = 14
bgfx_api.BGFX_FUNCTION_ID_TOPOLOGY_SORT_TRI_LIST = 15
bgfx_api.BGFX_FUNCTION_ID_GET_SUPPORTED_RENDERERS = 16
bgfx_api.BGFX_FUNCTION_ID_GET_RENDERER_NAME = 17
bgfx_api.BGFX_FUNCTION_ID_INIT_CTOR = 18
bgfx_api.BGFX_FUNCTION_ID_INIT = 19
bgfx_api.BGFX_FUNCTION_ID_SHUTDOWN = 20
bgfx_api.BGFX_FUNCTION_ID_RESET = 21
bgfx_api.BGFX_FUNCTION_ID_FRAME = 22
bgfx_api.BGFX_FUNCTION_ID_GET_RENDERER_TYPE = 23
bgfx_api.BGFX_FUNCTION_ID_GET_CAPS = 24
bgfx_api.BGFX_FUNCTION_ID_GET_STATS = 25
bgfx_api.BGFX_FUNCTION_ID_ALLOC = 26
bgfx_api.BGFX_FUNCTION_ID_COPY = 27
bgfx_api.BGFX_FUNCTION_ID_MAKE_REF = 28
bgfx_api.BGFX_FUNCTION_ID_MAKE_REF_RELEASE = 29
bgfx_api.BGFX_FUNCTION_ID_SET_DEBUG = 30
bgfx_api.BGFX_FUNCTION_ID_DBG_TEXT_CLEAR = 31
bgfx_api.BGFX_FUNCTION_ID_DBG_TEXT_PRINTF = 32
bgfx_api.BGFX_FUNCTION_ID_DBG_TEXT_VPRINTF = 33
bgfx_api.BGFX_FUNCTION_ID_DBG_TEXT_IMAGE = 34
bgfx_api.BGFX_FUNCTION_ID_CREATE_INDEX_BUFFER = 35
bgfx_api.BGFX_FUNCTION_ID_SET_INDEX_BUFFER_NAME = 36
bgfx_api.BGFX_FUNCTION_ID_DESTROY_INDEX_BUFFER = 37
bgfx_api.BGFX_FUNCTION_ID_CREATE_VERTEX_LAYOUT = 38
bgfx_api.BGFX_FUNCTION_ID_DESTROY_VERTEX_LAYOUT = 39
bgfx_api.BGFX_FUNCTION_ID_CREATE_VERTEX_BUFFER = 40
bgfx_api.BGFX_FUNCTION_ID_SET_VERTEX_BUFFER_NAME = 41
bgfx_api.BGFX_FUNCTION_ID_DESTROY_VERTEX_BUFFER = 42
bgfx_api.BGFX_FUNCTION_ID_CREATE_DYNAMIC_INDEX_BUFFER = 43
bgfx_api.BGFX_FUNCTION_ID_CREATE_DYNAMIC_INDEX_BUFFER_MEM = 44
bgfx_api.BGFX_FUNCTION_ID_UPDATE_DYNAMIC_INDEX_BUFFER = 45
bgfx_api.BGFX_FUNCTION_ID_DESTROY_DYNAMIC_INDEX_BUFFER = 46
bgfx_api.BGFX_FUNCTION_ID_CREATE_DYNAMIC_VERTEX_BUFFER = 47
bgfx_api.BGFX_FUNCTION_ID_CREATE_DYNAMIC_VERTEX_BUFFER_MEM = 48
bgfx_api.BGFX_FUNCTION_ID_UPDATE_DYNAMIC_VERTEX_BUFFER = 49
bgfx_api.BGFX_FUNCTION_ID_DESTROY_DYNAMIC_VERTEX_BUFFER = 50
bgfx_api.BGFX_FUNCTION_ID_GET_AVAIL_TRANSIENT_INDEX_BUFFER = 51
bgfx_api.BGFX_FUNCTION_ID_GET_AVAIL_TRANSIENT_VERTEX_BUFFER = 52
bgfx_api.BGFX_FUNCTION_ID_GET_AVAIL_INSTANCE_DATA_BUFFER = 53
bgfx_api.BGFX_FUNCTION_ID_ALLOC_TRANSIENT_INDEX_BUFFER = 54
bgfx_api.BGFX_FUNCTION_ID_ALLOC_TRANSIENT_VERTEX_BUFFER = 55
bgfx_api.BGFX_FUNCTION_ID_ALLOC_TRANSIENT_BUFFERS = 56
bgfx_api.BGFX_FUNCTION_ID_ALLOC_INSTANCE_DATA_BUFFER = 57
bgfx_api.BGFX_FUNCTION_ID_CREATE_INDIRECT_BUFFER = 58
bgfx_api.BGFX_FUNCTION_ID_DESTROY_INDIRECT_BUFFER = 59
bgfx_api.BGFX_FUNCTION_ID_CREATE_SHADER = 60
bgfx_api.BGFX_FUNCTION_ID_GET_SHADER_UNIFORMS = 61
bgfx_api.BGFX_FUNCTION_ID_SET_SHADER_NAME = 62
bgfx_api.BGFX_FUNCTION_ID_DESTROY_SHADER = 63
bgfx_api.BGFX_FUNCTION_ID_CREATE_PROGRAM = 64
bgfx_api.BGFX_FUNCTION_ID_CREATE_COMPUTE_PROGRAM = 65
bgfx_api.BGFX_FUNCTION_ID_DESTROY_PROGRAM = 66
bgfx_api.BGFX_FUNCTION_ID_IS_TEXTURE_VALID = 67
bgfx_api.BGFX_FUNCTION_ID_IS_FRAME_BUFFER_VALID = 68
bgfx_api.BGFX_FUNCTION_ID_CALC_TEXTURE_SIZE = 69
bgfx_api.BGFX_FUNCTION_ID_CREATE_TEXTURE = 70
bgfx_api.BGFX_FUNCTION_ID_CREATE_TEXTURE_2D = 71
bgfx_api.BGFX_FUNCTION_ID_CREATE_TEXTURE_2D_SCALED = 72
bgfx_api.BGFX_FUNCTION_ID_CREATE_TEXTURE_3D = 73
bgfx_api.BGFX_FUNCTION_ID_CREATE_TEXTURE_CUBE = 74
bgfx_api.BGFX_FUNCTION_ID_UPDATE_TEXTURE_2D = 75
bgfx_api.BGFX_FUNCTION_ID_UPDATE_TEXTURE_3D = 76
bgfx_api.BGFX_FUNCTION_ID_UPDATE_TEXTURE_CUBE = 77
bgfx_api.BGFX_FUNCTION_ID_READ_TEXTURE = 78
bgfx_api.BGFX_FUNCTION_ID_SET_TEXTURE_NAME = 79
bgfx_api.BGFX_FUNCTION_ID_GET_DIRECT_ACCESS_PTR = 80
bgfx_api.BGFX_FUNCTION_ID_DESTROY_TEXTURE = 81
bgfx_api.BGFX_FUNCTION_ID_CREATE_FRAME_BUFFER = 82
bgfx_api.BGFX_FUNCTION_ID_CREATE_FRAME_BUFFER_SCALED = 83
bgfx_api.BGFX_FUNCTION_ID_CREATE_FRAME_BUFFER_FROM_HANDLES = 84
bgfx_api.BGFX_FUNCTION_ID_CREATE_FRAME_BUFFER_FROM_ATTACHMENT = 85
bgfx_api.BGFX_FUNCTION_ID_CREATE_FRAME_BUFFER_FROM_NWH = 86
bgfx_api.BGFX_FUNCTION_ID_SET_FRAME_BUFFER_NAME = 87
bgfx_api.BGFX_FUNCTION_ID_GET_TEXTURE = 88
bgfx_api.BGFX_FUNCTION_ID_DESTROY_FRAME_BUFFER = 89
bgfx_api.BGFX_FUNCTION_ID_CREATE_UNIFORM = 90
bgfx_api.BGFX_FUNCTION_ID_GET_UNIFORM_INFO = 91
bgfx_api.BGFX_FUNCTION_ID_DESTROY_UNIFORM = 92
bgfx_api.BGFX_FUNCTION_ID_CREATE_OCCLUSION_QUERY = 93
bgfx_api.BGFX_FUNCTION_ID_GET_RESULT = 94
bgfx_api.BGFX_FUNCTION_ID_DESTROY_OCCLUSION_QUERY = 95
bgfx_api.BGFX_FUNCTION_ID_SET_PALETTE_COLOR = 96
bgfx_api.BGFX_FUNCTION_ID_SET_PALETTE_COLOR_RGBA32F = 97
bgfx_api.BGFX_FUNCTION_ID_SET_PALETTE_COLOR_RGBA8 = 98
bgfx_api.BGFX_FUNCTION_ID_SET_VIEW_NAME = 99
bgfx_api.BGFX_FUNCTION_ID_SET_VIEW_RECT = 100
bgfx_api.BGFX_FUNCTION_ID_SET_VIEW_RECT_RATIO = 101
bgfx_api.BGFX_FUNCTION_ID_SET_VIEW_SCISSOR = 102
bgfx_api.BGFX_FUNCTION_ID_SET_VIEW_CLEAR = 103
bgfx_api.BGFX_FUNCTION_ID_SET_VIEW_CLEAR_MRT = 104
bgfx_api.BGFX_FUNCTION_ID_SET_VIEW_MODE = 105
bgfx_api.BGFX_FUNCTION_ID_SET_VIEW_FRAME_BUFFER = 106
bgfx_api.BGFX_FUNCTION_ID_SET_VIEW_TRANSFORM = 107
bgfx_api.BGFX_FUNCTION_ID_SET_VIEW_ORDER = 108
bgfx_api.BGFX_FUNCTION_ID_RESET_VIEW = 109
bgfx_api.BGFX_FUNCTION_ID_ENCODER_BEGIN = 110
bgfx_api.BGFX_FUNCTION_ID_ENCODER_END = 111
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_MARKER = 112
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_STATE = 113
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_CONDITION = 114
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_STENCIL = 115
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_SCISSOR = 116
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_SCISSOR_CACHED = 117
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_TRANSFORM = 118
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_TRANSFORM_CACHED = 119
bgfx_api.BGFX_FUNCTION_ID_ENCODER_ALLOC_TRANSFORM = 120
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_UNIFORM = 121
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_INDEX_BUFFER = 122
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_DYNAMIC_INDEX_BUFFER = 123
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_TRANSIENT_INDEX_BUFFER = 124
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_VERTEX_BUFFER = 125
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_VERTEX_BUFFER_WITH_LAYOUT = 126
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_DYNAMIC_VERTEX_BUFFER = 127
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_DYNAMIC_VERTEX_BUFFER_WITH_LAYOUT = 128
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_TRANSIENT_VERTEX_BUFFER = 129
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_TRANSIENT_VERTEX_BUFFER_WITH_LAYOUT = 130
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_VERTEX_COUNT = 131
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_INSTANCE_DATA_BUFFER = 132
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_INSTANCE_DATA_FROM_VERTEX_BUFFER = 133
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_INSTANCE_DATA_FROM_DYNAMIC_VERTEX_BUFFER = 134
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_INSTANCE_COUNT = 135
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_TEXTURE = 136
bgfx_api.BGFX_FUNCTION_ID_ENCODER_TOUCH = 137
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SUBMIT = 138
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SUBMIT_OCCLUSION_QUERY = 139
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SUBMIT_INDIRECT = 140
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SUBMIT_INDIRECT_COUNT = 141
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_COMPUTE_INDEX_BUFFER = 142
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_COMPUTE_VERTEX_BUFFER = 143
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_COMPUTE_DYNAMIC_INDEX_BUFFER = 144
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_COMPUTE_DYNAMIC_VERTEX_BUFFER = 145
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_COMPUTE_INDIRECT_BUFFER = 146
bgfx_api.BGFX_FUNCTION_ID_ENCODER_SET_IMAGE = 147
bgfx_api.BGFX_FUNCTION_ID_ENCODER_DISPATCH = 148
bgfx_api.BGFX_FUNCTION_ID_ENCODER_DISPATCH_INDIRECT = 149
bgfx_api.BGFX_FUNCTION_ID_ENCODER_DISCARD = 150
bgfx_api.BGFX_FUNCTION_ID_ENCODER_BLIT = 151
bgfx_api.BGFX_FUNCTION_ID_REQUEST_SCREEN_SHOT = 152
bgfx_api.BGFX_FUNCTION_ID_RENDER_FRAME = 153
bgfx_api.BGFX_FUNCTION_ID_SET_PLATFORM_DATA = 154
bgfx_api.BGFX_FUNCTION_ID_GET_INTERNAL_DATA = 155
bgfx_api.BGFX_FUNCTION_ID_OVERRIDE_INTERNAL_TEXTURE_PTR = 156
bgfx_api.BGFX_FUNCTION_ID_OVERRIDE_INTERNAL_TEXTURE = 157
bgfx_api.BGFX_FUNCTION_ID_SET_MARKER = 158
bgfx_api.BGFX_FUNCTION_ID_SET_STATE = 159
bgfx_api.BGFX_FUNCTION_ID_SET_CONDITION = 160
bgfx_api.BGFX_FUNCTION_ID_SET_STENCIL = 161
bgfx_api.BGFX_FUNCTION_ID_SET_SCISSOR = 162
bgfx_api.BGFX_FUNCTION_ID_SET_SCISSOR_CACHED = 163
bgfx_api.BGFX_FUNCTION_ID_SET_TRANSFORM = 164
bgfx_api.BGFX_FUNCTION_ID_SET_TRANSFORM_CACHED = 165
bgfx_api.BGFX_FUNCTION_ID_ALLOC_TRANSFORM = 166
bgfx_api.BGFX_FUNCTION_ID_SET_UNIFORM = 167
bgfx_api.BGFX_FUNCTION_ID_SET_INDEX_BUFFER = 168
bgfx_api.BGFX_FUNCTION_ID_SET_DYNAMIC_INDEX_BUFFER = 169
bgfx_api.BGFX_FUNCTION_ID_SET_TRANSIENT_INDEX_BUFFER = 170
bgfx_api.BGFX_FUNCTION_ID_SET_VERTEX_BUFFER = 171
bgfx_api.BGFX_FUNCTION_ID_SET_VERTEX_BUFFER_WITH_LAYOUT = 172
bgfx_api.BGFX_FUNCTION_ID_SET_DYNAMIC_VERTEX_BUFFER = 173
bgfx_api.BGFX_FUNCTION_ID_SET_DYNAMIC_VERTEX_BUFFER_WITH_LAYOUT = 174
bgfx_api.BGFX_FUNCTION_ID_SET_TRANSIENT_VERTEX_BUFFER = 175
bgfx_api.BGFX_FUNCTION_ID_SET_TRANSIENT_VERTEX_BUFFER_WITH_LAYOUT = 176
bgfx_api.BGFX_FUNCTION_ID_SET_VERTEX_COUNT = 177
bgfx_api.BGFX_FUNCTION_ID_SET_INSTANCE_DATA_BUFFER = 178
bgfx_api.BGFX_FUNCTION_ID_SET_INSTANCE_DATA_FROM_VERTEX_BUFFER = 179
bgfx_api.BGFX_FUNCTION_ID_SET_INSTANCE_DATA_FROM_DYNAMIC_VERTEX_BUFFER = 180
bgfx_api.BGFX_FUNCTION_ID_SET_INSTANCE_COUNT = 181
bgfx_api.BGFX_FUNCTION_ID_SET_TEXTURE = 182
bgfx_api.BGFX_FUNCTION_ID_TOUCH = 183
bgfx_api.BGFX_FUNCTION_ID_SUBMIT = 184
bgfx_api.BGFX_FUNCTION_ID_SUBMIT_OCCLUSION_QUERY = 185
bgfx_api.BGFX_FUNCTION_ID_SUBMIT_INDIRECT = 186
bgfx_api.BGFX_FUNCTION_ID_SUBMIT_INDIRECT_COUNT = 187
bgfx_api.BGFX_FUNCTION_ID_SET_COMPUTE_INDEX_BUFFER = 188
bgfx_api.BGFX_FUNCTION_ID_SET_COMPUTE_VERTEX_BUFFER = 189
bgfx_api.BGFX_FUNCTION_ID_SET_COMPUTE_DYNAMIC_INDEX_BUFFER = 190
bgfx_api.BGFX_FUNCTION_ID_SET_COMPUTE_DYNAMIC_VERTEX_BUFFER = 191
bgfx_api.BGFX_FUNCTION_ID_SET_COMPUTE_INDIRECT_BUFFER = 192
bgfx_api.BGFX_FUNCTION_ID_SET_IMAGE = 193
bgfx_api.BGFX_FUNCTION_ID_DISPATCH = 194
bgfx_api.BGFX_FUNCTION_ID_DISPATCH_INDIRECT = 195
bgfx_api.BGFX_FUNCTION_ID_DISCARD = 196
bgfx_api.BGFX_FUNCTION_ID_BLIT = 197
bgfx_api.BGFX_FUNCTION_ID_COUNT = 198

-- from typedef enum bgfx_backbuffer_ratio (bgfx_backbuffer_ratio_t)
bgfx_api.BGFX_BACKBUFFER_RATIO_EQUAL = 0
bgfx_api.BGFX_BACKBUFFER_RATIO_HALF = 1
bgfx_api.BGFX_BACKBUFFER_RATIO_QUARTER = 2
bgfx_api.BGFX_BACKBUFFER_RATIO_EIGHTH = 3
bgfx_api.BGFX_BACKBUFFER_RATIO_SIXTEENTH = 4
bgfx_api.BGFX_BACKBUFFER_RATIO_DOUBLE = 5
bgfx_api.BGFX_BACKBUFFER_RATIO_COUNT = 6
local function ffi_tryload(name)
    local ok, lib = pcall(ffi.load, name)
    if ok then return lib end

    local os = ffi.os
    local ext = (os == "Windows") and ".dll" or (os == "OSX" and ".dylib" or ".so")
    local path = "./bin/" .. name .. ext
    return ffi.load(path)
end
local C = ffi_tryload("libbgfx-shared-libRelease")
return setmetatable( bgfx_api, {
	__index = function( table, key )
		return C[ key ]
	end
} )

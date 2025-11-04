local ffi = require("ffi")
local bit = require("bit")
ffi.cdef([[
  
  typedef void (*GLFWglproc)(void);
  
  
  typedef void (*GLFWvkproc)(void);
  
  typedef struct GLFWmonitor GLFWmonitor;
  typedef struct GLFWwindow GLFWwindow;
  typedef struct GLFWcursor GLFWcursor;
  
  typedef void (* GLFWerrorfun)(int,const char*);
  
  
  typedef void (* GLFWwindowposfun)(GLFWwindow*,int,int);
  
  
  typedef void (* GLFWwindowsizefun)(GLFWwindow*,int,int);
  
  
  typedef void (* GLFWwindowclosefun)(GLFWwindow*);
  
  
  typedef void (* GLFWwindowrefreshfun)(GLFWwindow*);
  
  
  typedef void (* GLFWwindowfocusfun)(GLFWwindow*,int);
  
  
  typedef void (* GLFWwindowiconifyfun)(GLFWwindow*,int);
  
  
  typedef void (* GLFWwindowmaximizefun)(GLFWwindow*,int);
  
  
  typedef void (* GLFWframebuffersizefun)(GLFWwindow*,int,int);
  
  
  typedef void (* GLFWwindowcontentscalefun)(GLFWwindow*,float,float);
  
  
  typedef void (* GLFWmousebuttonfun)(GLFWwindow*,int,int,int);
  
  
  typedef void (* GLFWcursorposfun)(GLFWwindow*,double,double);
  
  
  typedef void (* GLFWcursorenterfun)(GLFWwindow*,int);
  
  
  typedef void (* GLFWscrollfun)(GLFWwindow*,double,double);
  
  
  typedef void (* GLFWkeyfun)(GLFWwindow*,int,int,int,int);
  
  
  typedef void (* GLFWcharfun)(GLFWwindow*,unsigned int);
  
  
  typedef void (* GLFWcharmodsfun)(GLFWwindow*,unsigned int,int);
  
  
  typedef void (* GLFWdropfun)(GLFWwindow*,int,const char*[]);
  
  
  typedef void (* GLFWmonitorfun)(GLFWmonitor*,int);
  
  
  typedef void (* GLFWjoystickfun)(int,int);
  
  typedef struct GLFWvidmode {
    int width;
    int height;
    int redBits;
    int greenBits;
    int blueBits;
    int refreshRate;
  } GLFWvidmode;
  
  typedef struct GLFWgammaramp {
    unsigned short* red;
    unsigned short* green;
    unsigned short* blue;
    unsigned int size;
  } GLFWgammaramp;
  
  typedef struct GLFWimage {
    int width;
    int height;
    unsigned char* pixels;
  } GLFWimage;
  
  typedef struct GLFWgamepadstate {
    unsigned char buttons[15];
    float axes[6];
  } GLFWgamepadstate;
  
  int glfwInit(void);
  void glfwTerminate(void);
  void glfwInitHint(int hint, int value);
  void glfwGetVersion(int* major, int* minor, int* rev);
  const char* glfwGetVersionString(void);
  int glfwGetError(const char** description);
  GLFWerrorfun glfwSetErrorCallback(GLFWerrorfun callback);
  GLFWmonitor** glfwGetMonitors(int* count);
  GLFWmonitor* glfwGetPrimaryMonitor(void);
  void glfwGetMonitorPos(GLFWmonitor* monitor, int* xpos, int* ypos);
  void glfwGetMonitorWorkarea(GLFWmonitor* monitor, int* xpos, int* ypos, int* width, int* height);
  void glfwGetMonitorPhysicalSize(GLFWmonitor* monitor, int* widthMM, int* heightMM);
  void glfwGetMonitorContentScale(GLFWmonitor* monitor, float* xscale, float* yscale);
  const char* glfwGetMonitorName(GLFWmonitor* monitor);
  void glfwSetMonitorUserPointer(GLFWmonitor* monitor, void* pointer);
  void* glfwGetMonitorUserPointer(GLFWmonitor* monitor);
  GLFWmonitorfun glfwSetMonitorCallback(GLFWmonitorfun callback);
  const GLFWvidmode* glfwGetVideoModes(GLFWmonitor* monitor, int* count);
  const GLFWvidmode* glfwGetVideoMode(GLFWmonitor* monitor);
  void glfwSetGamma(GLFWmonitor* monitor, float gamma);
  const GLFWgammaramp* glfwGetGammaRamp(GLFWmonitor* monitor);
  void glfwSetGammaRamp(GLFWmonitor* monitor, const GLFWgammaramp* ramp);
  void glfwDefaultWindowHints(void);
  void glfwWindowHint(int hint, int value);
  void glfwWindowHintString(int hint, const char* value);
  GLFWwindow* glfwCreateWindow(int width, int height, const char* title, GLFWmonitor* monitor, GLFWwindow* share);
  void glfwDestroyWindow(GLFWwindow* window);
  int glfwWindowShouldClose(GLFWwindow* window);
  void glfwSetWindowShouldClose(GLFWwindow* window, int value);
  void glfwSetWindowTitle(GLFWwindow* window, const char* title);
  void glfwSetWindowIcon(GLFWwindow* window, int count, const GLFWimage* images);
  void glfwGetWindowPos(GLFWwindow* window, int* xpos, int* ypos);
  void glfwSetWindowPos(GLFWwindow* window, int xpos, int ypos);
  void glfwGetWindowSize(GLFWwindow* window, int* width, int* height);
  void glfwSetWindowSizeLimits(GLFWwindow* window, int minwidth, int minheight, int maxwidth, int maxheight);
  void glfwSetWindowAspectRatio(GLFWwindow* window, int numer, int denom);
  void glfwSetWindowSize(GLFWwindow* window, int width, int height);
  void glfwGetFramebufferSize(GLFWwindow* window, int* width, int* height);
  void glfwGetWindowFrameSize(GLFWwindow* window, int* left, int* top, int* right, int* bottom);
  void glfwGetWindowContentScale(GLFWwindow* window, float* xscale, float* yscale);
  float glfwGetWindowOpacity(GLFWwindow* window);
  void glfwSetWindowOpacity(GLFWwindow* window, float opacity);
  void glfwIconifyWindow(GLFWwindow* window);
  void glfwRestoreWindow(GLFWwindow* window);
  void glfwMaximizeWindow(GLFWwindow* window);
  void glfwShowWindow(GLFWwindow* window);
  void glfwHideWindow(GLFWwindow* window);
  void glfwFocusWindow(GLFWwindow* window);
  void glfwRequestWindowAttention(GLFWwindow* window);
  GLFWmonitor* glfwGetWindowMonitor(GLFWwindow* window);
  void glfwSetWindowMonitor(GLFWwindow* window, GLFWmonitor* monitor, int xpos, int ypos, int width, int height, int refreshRate);
  int glfwGetWindowAttrib(GLFWwindow* window, int attrib);
  void glfwSetWindowAttrib(GLFWwindow* window, int attrib, int value);
  void glfwSetWindowUserPointer(GLFWwindow* window, void* pointer);
  void* glfwGetWindowUserPointer(GLFWwindow* window);
  GLFWwindowposfun glfwSetWindowPosCallback(GLFWwindow* window, GLFWwindowposfun callback);
  GLFWwindowsizefun glfwSetWindowSizeCallback(GLFWwindow* window, GLFWwindowsizefun callback);
  GLFWwindowclosefun glfwSetWindowCloseCallback(GLFWwindow* window, GLFWwindowclosefun callback);
  GLFWwindowrefreshfun glfwSetWindowRefreshCallback(GLFWwindow* window, GLFWwindowrefreshfun callback);
  GLFWwindowfocusfun glfwSetWindowFocusCallback(GLFWwindow* window, GLFWwindowfocusfun callback);
  GLFWwindowiconifyfun glfwSetWindowIconifyCallback(GLFWwindow* window, GLFWwindowiconifyfun callback);
  GLFWwindowmaximizefun glfwSetWindowMaximizeCallback(GLFWwindow* window, GLFWwindowmaximizefun callback);
  GLFWframebuffersizefun glfwSetFramebufferSizeCallback(GLFWwindow* window, GLFWframebuffersizefun callback);
  GLFWwindowcontentscalefun glfwSetWindowContentScaleCallback(GLFWwindow* window, GLFWwindowcontentscalefun callback);
  void glfwPollEvents(void);
  void glfwWaitEvents(void);
  void glfwWaitEventsTimeout(double timeout);
  void glfwPostEmptyEvent(void);
  int glfwGetInputMode(GLFWwindow* window, int mode);
  void glfwSetInputMode(GLFWwindow* window, int mode, int value);
  int glfwRawMouseMotionSupported(void);
  const char* glfwGetKeyName(int key, int scancode);
  int glfwGetKeyScancode(int key);
  int glfwGetKey(GLFWwindow* window, int key);
  int glfwGetMouseButton(GLFWwindow* window, int button);
  void glfwGetCursorPos(GLFWwindow* window, double* xpos, double* ypos);
  void glfwSetCursorPos(GLFWwindow* window, double xpos, double ypos);
  GLFWcursor* glfwCreateCursor(const GLFWimage* image, int xhot, int yhot);
  GLFWcursor* glfwCreateStandardCursor(int shape);
  void glfwDestroyCursor(GLFWcursor* cursor);
  void glfwSetCursor(GLFWwindow* window, GLFWcursor* cursor);
  GLFWkeyfun glfwSetKeyCallback(GLFWwindow* window, GLFWkeyfun callback);
  GLFWcharfun glfwSetCharCallback(GLFWwindow* window, GLFWcharfun callback);
  GLFWcharmodsfun glfwSetCharModsCallback(GLFWwindow* window, GLFWcharmodsfun callback);
  GLFWmousebuttonfun glfwSetMouseButtonCallback(GLFWwindow* window, GLFWmousebuttonfun callback);
  GLFWcursorposfun glfwSetCursorPosCallback(GLFWwindow* window, GLFWcursorposfun callback);
  GLFWcursorenterfun glfwSetCursorEnterCallback(GLFWwindow* window, GLFWcursorenterfun callback);
  GLFWscrollfun glfwSetScrollCallback(GLFWwindow* window, GLFWscrollfun callback);
  GLFWdropfun glfwSetDropCallback(GLFWwindow* window, GLFWdropfun callback);
  int glfwJoystickPresent(int jid);
  const float* glfwGetJoystickAxes(int jid, int* count);
  const unsigned char* glfwGetJoystickButtons(int jid, int* count);
  const unsigned char* glfwGetJoystickHats(int jid, int* count);
  const char* glfwGetJoystickName(int jid);
  const char* glfwGetJoystickGUID(int jid);
  void glfwSetJoystickUserPointer(int jid, void* pointer);
  void* glfwGetJoystickUserPointer(int jid);
  int glfwJoystickIsGamepad(int jid);
  GLFWjoystickfun glfwSetJoystickCallback(GLFWjoystickfun callback);
  int glfwUpdateGamepadMappings(const char* string);
  const char* glfwGetGamepadName(int jid);
  int glfwGetGamepadState(int jid, GLFWgamepadstate* state);
  void glfwSetClipboardString(GLFWwindow* window, const char* string);
  const char* glfwGetClipboardString(GLFWwindow* window);
  double glfwGetTime(void);
  void glfwSetTime(double time);
  uint64_t glfwGetTimerValue(void);
  uint64_t glfwGetTimerFrequency(void);
  void glfwMakeContextCurrent(GLFWwindow* window);
  GLFWwindow* glfwGetCurrentContext(void);
  void glfwSwapBuffers(GLFWwindow* window);
  void glfwSwapInterval(int interval);
  int glfwExtensionSupported(const char* extension);
  GLFWglproc glfwGetProcAddress(const char* procname);
  int glfwVulkanSupported(void);
  const char** glfwGetRequiredInstanceExtensions(uint32_t* count);
  const char* glfwGetWin32Adapter(GLFWmonitor* monitor);
  const char* glfwGetWin32Monitor(GLFWmonitor* monitor);
  uint32_t glfwGetWin32Window(GLFWwindow* window);
  uint32_t glfwGetCocoaMonitor(GLFWmonitor* monitor);
  uint32_t glfwGetCocoaWindow(GLFWwindow* window);
  const char* glfwGetX11Display(void);
  uint32_t glfwGetX11Adapter(GLFWmonitor* monitor);
  uint32_t glfwGetX11Monitor(GLFWmonitor* monitor);
  uint32_t glfwGetX11Window(GLFWwindow* window);
  void glfwSetX11SelectionString(const char* string);
  const char* glfwGetX11SelectionString(void);
]])

local glfw_api = {}
glfw_api.VERSION_MAJOR = 3
glfw_api.VERSION_MINOR = 4
glfw_api.VERSION_REVISION = 0
glfw_api.TRUE = 1
glfw_api.FALSE = 0
glfw_api.RELEASE = 0
glfw_api.PRESS = 1
glfw_api.REPEAT = 2
glfw_api.HAT_CENTERED = 0
glfw_api.HAT_UP = 1
glfw_api.HAT_RIGHT = 2
glfw_api.HAT_DOWN = 4
glfw_api.HAT_LEFT = 8
-- glfw_api.HAT_RIGHT_UP = (HAT_RIGHT|HAT_UP)
-- glfw_api.HAT_RIGHT_DOWN = (HAT_RIGHT|HAT_DOWN)
-- glfw_api.HAT_LEFT_UP = (HAT_LEFT|HAT_UP)
-- glfw_api.HAT_LEFT_DOWN = (HAT_LEFT|HAT_DOWN)
glfw_api.KEY_UNKNOWN = -1
glfw_api.KEY_SPACE = 32
glfw_api.KEY_APOSTROPHE = 39
glfw_api.KEY_COMMA = 44
glfw_api.KEY_MINUS = 45
glfw_api.KEY_PERIOD = 46
glfw_api.KEY_SLASH = 47
glfw_api.KEY_0 = 48
glfw_api.KEY_1 = 49
glfw_api.KEY_2 = 50
glfw_api.KEY_3 = 51
glfw_api.KEY_4 = 52
glfw_api.KEY_5 = 53
glfw_api.KEY_6 = 54
glfw_api.KEY_7 = 55
glfw_api.KEY_8 = 56
glfw_api.KEY_9 = 57
glfw_api.KEY_SEMICOLON = 59
glfw_api.KEY_EQUAL = 61
glfw_api.KEY_A = 65
glfw_api.KEY_B = 66
glfw_api.KEY_C = 67
glfw_api.KEY_D = 68
glfw_api.KEY_E = 69
glfw_api.KEY_F = 70
glfw_api.KEY_G = 71
glfw_api.KEY_H = 72
glfw_api.KEY_I = 73
glfw_api.KEY_J = 74
glfw_api.KEY_K = 75
glfw_api.KEY_L = 76
glfw_api.KEY_M = 77
glfw_api.KEY_N = 78
glfw_api.KEY_O = 79
glfw_api.KEY_P = 80
glfw_api.KEY_Q = 81
glfw_api.KEY_R = 82
glfw_api.KEY_S = 83
glfw_api.KEY_T = 84
glfw_api.KEY_U = 85
glfw_api.KEY_V = 86
glfw_api.KEY_W = 87
glfw_api.KEY_X = 88
glfw_api.KEY_Y = 89
glfw_api.KEY_Z = 90
glfw_api.KEY_LEFT_BRACKET = 91
glfw_api.KEY_BACKSLASH = 92
glfw_api.KEY_RIGHT_BRACKET = 93
glfw_api.KEY_GRAVE_ACCENT = 96
glfw_api.KEY_WORLD_1 = 161
glfw_api.KEY_WORLD_2 = 162
glfw_api.KEY_ESCAPE = 256
glfw_api.KEY_ENTER = 257
glfw_api.KEY_TAB = 258
glfw_api.KEY_BACKSPACE = 259
glfw_api.KEY_INSERT = 260
glfw_api.KEY_DELETE = 261
glfw_api.KEY_RIGHT = 262
glfw_api.KEY_LEFT = 263
glfw_api.KEY_DOWN = 264
glfw_api.KEY_UP = 265
glfw_api.KEY_PAGE_UP = 266
glfw_api.KEY_PAGE_DOWN = 267
glfw_api.KEY_HOME = 268
glfw_api.KEY_END = 269
glfw_api.KEY_CAPS_LOCK = 280
glfw_api.KEY_SCROLL_LOCK = 281
glfw_api.KEY_NUM_LOCK = 282
glfw_api.KEY_PRINT_SCREEN = 283
glfw_api.KEY_PAUSE = 284
glfw_api.KEY_F1 = 290
glfw_api.KEY_F2 = 291
glfw_api.KEY_F3 = 292
glfw_api.KEY_F4 = 293
glfw_api.KEY_F5 = 294
glfw_api.KEY_F6 = 295
glfw_api.KEY_F7 = 296
glfw_api.KEY_F8 = 297
glfw_api.KEY_F9 = 298
glfw_api.KEY_F10 = 299
glfw_api.KEY_F11 = 300
glfw_api.KEY_F12 = 301
glfw_api.KEY_F13 = 302
glfw_api.KEY_F14 = 303
glfw_api.KEY_F15 = 304
glfw_api.KEY_F16 = 305
glfw_api.KEY_F17 = 306
glfw_api.KEY_F18 = 307
glfw_api.KEY_F19 = 308
glfw_api.KEY_F20 = 309
glfw_api.KEY_F21 = 310
glfw_api.KEY_F22 = 311
glfw_api.KEY_F23 = 312
glfw_api.KEY_F24 = 313
glfw_api.KEY_F25 = 314
glfw_api.KEY_KP_0 = 320
glfw_api.KEY_KP_1 = 321
glfw_api.KEY_KP_2 = 322
glfw_api.KEY_KP_3 = 323
glfw_api.KEY_KP_4 = 324
glfw_api.KEY_KP_5 = 325
glfw_api.KEY_KP_6 = 326
glfw_api.KEY_KP_7 = 327
glfw_api.KEY_KP_8 = 328
glfw_api.KEY_KP_9 = 329
glfw_api.KEY_KP_DECIMAL = 330
glfw_api.KEY_KP_DIVIDE = 331
glfw_api.KEY_KP_MULTIPLY = 332
glfw_api.KEY_KP_SUBTRACT = 333
glfw_api.KEY_KP_ADD = 334
glfw_api.KEY_KP_ENTER = 335
glfw_api.KEY_KP_EQUAL = 336
glfw_api.KEY_LEFT_SHIFT = 340
glfw_api.KEY_LEFT_CONTROL = 341
glfw_api.KEY_LEFT_ALT = 342
glfw_api.KEY_LEFT_SUPER = 343
glfw_api.KEY_RIGHT_SHIFT = 344
glfw_api.KEY_RIGHT_CONTROL = 345
glfw_api.KEY_RIGHT_ALT = 346
glfw_api.KEY_RIGHT_SUPER = 347
glfw_api.KEY_MENU = 348
glfw_api.KEY_LAST = glfw_api.KEY_MENU
glfw_api.MOD_SHIFT = 0x0001
glfw_api.MOD_CONTROL = 0x0002
glfw_api.MOD_ALT = 0x0004
glfw_api.MOD_SUPER = 0x0008
glfw_api.MOD_CAPS_LOCK = 0x0010
glfw_api.MOD_NUM_LOCK = 0x0020
glfw_api.MOUSE_BUTTON_1 = 0
glfw_api.MOUSE_BUTTON_2 = 1
glfw_api.MOUSE_BUTTON_3 = 2
glfw_api.MOUSE_BUTTON_4 = 3
glfw_api.MOUSE_BUTTON_5 = 4
glfw_api.MOUSE_BUTTON_6 = 5
glfw_api.MOUSE_BUTTON_7 = 6
glfw_api.MOUSE_BUTTON_8 = 7
glfw_api.MOUSE_BUTTON_LAST = glfw_api.MOUSE_BUTTON_8
glfw_api.MOUSE_BUTTON_LEFT = glfw_api.MOUSE_BUTTON_1
glfw_api.MOUSE_BUTTON_RIGHT = glfw_api.MOUSE_BUTTON_2
glfw_api.MOUSE_BUTTON_MIDDLE = glfw_api.MOUSE_BUTTON_3
glfw_api.JOYSTICK_1 = 0
glfw_api.JOYSTICK_2 = 1
glfw_api.JOYSTICK_3 = 2
glfw_api.JOYSTICK_4 = 3
glfw_api.JOYSTICK_5 = 4
glfw_api.JOYSTICK_6 = 5
glfw_api.JOYSTICK_7 = 6
glfw_api.JOYSTICK_8 = 7
glfw_api.JOYSTICK_9 = 8
glfw_api.JOYSTICK_10 = 9
glfw_api.JOYSTICK_11 = 10
glfw_api.JOYSTICK_12 = 11
glfw_api.JOYSTICK_13 = 12
glfw_api.JOYSTICK_14 = 13
glfw_api.JOYSTICK_15 = 14
glfw_api.JOYSTICK_16 = 15
glfw_api.JOYSTICK_LAST = glfw_api.JOYSTICK_16
glfw_api.GAMEPAD_BUTTON_A = 0
glfw_api.GAMEPAD_BUTTON_B = 1
glfw_api.GAMEPAD_BUTTON_X = 2
glfw_api.GAMEPAD_BUTTON_Y = 3
glfw_api.GAMEPAD_BUTTON_LEFT_BUMPER = 4
glfw_api.GAMEPAD_BUTTON_RIGHT_BUMPER = 5
glfw_api.GAMEPAD_BUTTON_BACK = 6
glfw_api.GAMEPAD_BUTTON_START = 7
glfw_api.GAMEPAD_BUTTON_GUIDE = 8
glfw_api.GAMEPAD_BUTTON_LEFT_THUMB = 9
glfw_api.GAMEPAD_BUTTON_RIGHT_THUMB = 10
glfw_api.GAMEPAD_BUTTON_DPAD_UP = 11
glfw_api.GAMEPAD_BUTTON_DPAD_RIGHT = 12
glfw_api.GAMEPAD_BUTTON_DPAD_DOWN = 13
glfw_api.GAMEPAD_BUTTON_DPAD_LEFT = 14
glfw_api.GAMEPAD_BUTTON_LAST = glfw_api.GAMEPAD_BUTTON_DPAD_LEFT
glfw_api.GAMEPAD_BUTTON_CROSS = glfw_api.GAMEPAD_BUTTON_A
glfw_api.GAMEPAD_BUTTON_CIRCLE = glfw_api.GAMEPAD_BUTTON_B
glfw_api.GAMEPAD_BUTTON_SQUARE = glfw_api.GAMEPAD_BUTTON_X
glfw_api.GAMEPAD_BUTTON_TRIANGLE = glfw_api.GAMEPAD_BUTTON_Y
glfw_api.GAMEPAD_AXIS_LEFT_X = 0
glfw_api.GAMEPAD_AXIS_LEFT_Y = 1
glfw_api.GAMEPAD_AXIS_RIGHT_X = 2
glfw_api.GAMEPAD_AXIS_RIGHT_Y = 3
glfw_api.GAMEPAD_AXIS_LEFT_TRIGGER = 4
glfw_api.GAMEPAD_AXIS_RIGHT_TRIGGER = 5
glfw_api.GAMEPAD_AXIS_LAST = glfw_api.GAMEPAD_AXIS_RIGHT_TRIGGER
glfw_api.NO_ERROR = 0
glfw_api.NOT_INITIALIZED = 0x00010001
glfw_api.NO_CURRENT_CONTEXT = 0x00010002
glfw_api.INVALID_ENUM = 0x00010003
glfw_api.INVALID_VALUE = 0x00010004
glfw_api.OUT_OF_MEMORY = 0x00010005
glfw_api.API_UNAVAILABLE = 0x00010006
glfw_api.VERSION_UNAVAILABLE = 0x00010007
glfw_api.PLATFORM_ERROR = 0x00010008
glfw_api.FORMAT_UNAVAILABLE = 0x00010009
glfw_api.NO_WINDOW_CONTEXT = 0x0001000A
glfw_api.FOCUSED = 0x00020001
glfw_api.ICONIFIED = 0x00020002
glfw_api.RESIZABLE = 0x00020003
glfw_api.VISIBLE = 0x00020004
glfw_api.DECORATED = 0x00020005
glfw_api.AUTO_ICONIFY = 0x00020006
glfw_api.FLOATING = 0x00020007
glfw_api.MAXIMIZED = 0x00020008
glfw_api.CENTER_CURSOR = 0x00020009
glfw_api.TRANSPARENT_FRAMEBUFFER = 0x0002000A
glfw_api.HOVERED = 0x0002000B
glfw_api.FOCUS_ON_SHOW = 0x0002000C
glfw_api.RED_BITS = 0x00021001
glfw_api.GREEN_BITS = 0x00021002
glfw_api.BLUE_BITS = 0x00021003
glfw_api.ALPHA_BITS = 0x00021004
glfw_api.DEPTH_BITS = 0x00021005
glfw_api.STENCIL_BITS = 0x00021006
glfw_api.ACCUM_RED_BITS = 0x00021007
glfw_api.ACCUM_GREEN_BITS = 0x00021008
glfw_api.ACCUM_BLUE_BITS = 0x00021009
glfw_api.ACCUM_ALPHA_BITS = 0x0002100A
glfw_api.AUX_BUFFERS = 0x0002100B
glfw_api.STEREO = 0x0002100C
glfw_api.SAMPLES = 0x0002100D
glfw_api.SRGB_CAPABLE = 0x0002100E
glfw_api.REFRESH_RATE = 0x0002100F
glfw_api.DOUBLEBUFFER = 0x00021010
glfw_api.CLIENT_API = 0x00022001
glfw_api.CONTEXT_VERSION_MAJOR = 0x00022002
glfw_api.CONTEXT_VERSION_MINOR = 0x00022003
glfw_api.CONTEXT_REVISION = 0x00022004
glfw_api.CONTEXT_ROBUSTNESS = 0x00022005
glfw_api.OPENGL_FORWARD_COMPAT = 0x00022006
glfw_api.OPENGL_DEBUG_CONTEXT = 0x00022007
glfw_api.OPENGL_PROFILE = 0x00022008
glfw_api.CONTEXT_RELEASE_BEHAVIOR = 0x00022009
glfw_api.CONTEXT_NO_ERROR = 0x0002200A
glfw_api.CONTEXT_CREATION_API = 0x0002200B
glfw_api.SCALE_TO_MONITOR = 0x0002200C
glfw_api.COCOA_RETINA_FRAMEBUFFER = 0x00023001
glfw_api.COCOA_FRAME_NAME = 0x00023002
glfw_api.COCOA_GRAPHICS_SWITCHING = 0x00023003
glfw_api.X11_CLASS_NAME = 0x00024001
glfw_api.X11_INSTANCE_NAME = 0x00024002
glfw_api.NO_API = 0
glfw_api.OPENGL_API = 0x00030001
glfw_api.OPENGL_ES_API = 0x00030002
glfw_api.NO_ROBUSTNESS = 0
glfw_api.NO_RESET_NOTIFICATION = 0x00031001
glfw_api.LOSE_CONTEXT_ON_RESET = 0x00031002
glfw_api.OPENGL_ANY_PROFILE = 0
glfw_api.OPENGL_CORE_PROFILE = 0x00032001
glfw_api.OPENGL_COMPAT_PROFILE = 0x00032002
glfw_api.CURSOR = 0x00033001
glfw_api.STICKY_KEYS = 0x00033002
glfw_api.STICKY_MOUSE_BUTTONS = 0x00033003
glfw_api.LOCK_KEY_MODS = 0x00033004
glfw_api.RAW_MOUSE_MOTION = 0x00033005
glfw_api.CURSOR_NORMAL = 0x00034001
glfw_api.CURSOR_HIDDEN = 0x00034002
glfw_api.CURSOR_DISABLED = 0x00034003
glfw_api.ANY_RELEASE_BEHAVIOR = 0
glfw_api.RELEASE_BEHAVIOR_FLUSH = 0x00035001
glfw_api.RELEASE_BEHAVIOR_NONE = 0x00035002
glfw_api.NATIVE_CONTEXT_API = 0x00036001
glfw_api.EGL_CONTEXT_API = 0x00036002
glfw_api.OSMESA_CONTEXT_API = 0x00036003
glfw_api.ARROW_CURSOR = 0x00036001
glfw_api.IBEAM_CURSOR = 0x00036002
glfw_api.CROSSHAIR_CURSOR = 0x00036003
glfw_api.HAND_CURSOR = 0x00036004
glfw_api.HRESIZE_CURSOR = 0x00036005
glfw_api.VRESIZE_CURSOR = 0x00036006
glfw_api.CONNECTED = 0x00040001
glfw_api.DISCONNECTED = 0x00040002
glfw_api.JOYSTICK_HAT_BUTTONS = 0x00050001
glfw_api.COCOA_CHDIR_RESOURCES = 0x00051001
glfw_api.COCOA_MENUBAR = 0x00051002
glfw_api.DONT_CARE = -1
local function ffi_tryload(name)
    local ok, lib = pcall(ffi.load, name)
    if ok then return lib end

    local os = ffi.os
    local ext = (os == "Windows") and ".dll" or (os == "OSX" and ".dylib" or ".so")
    local path = "./bin/" .. name .. ext
    return ffi.load(path)
end
local C = ffi_tryload("libglfw3")
return setmetatable( glfw_api, {
	__index = function( table, key )
		return C[ 'glfw'..key ]
	end
} )

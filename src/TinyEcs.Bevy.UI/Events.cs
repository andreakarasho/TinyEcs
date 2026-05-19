using System.Numerics;

namespace TinyEcs.Bevy.UI;

// All UI pointer events fire as entity-targeted triggers (On<T>). The trigger's
// EntityId is the element under the pointer; observers can opt into bubble
// propagation up the parent chain (`trigger.Propagate(true)`).

public struct UiClick       { public Vector2 Position; }
public struct UiOver        { }
public struct UiOut         { }
public struct UiPointerDown { public Vector2 Position; }
public struct UiPointerUp   { public Vector2 Position; }

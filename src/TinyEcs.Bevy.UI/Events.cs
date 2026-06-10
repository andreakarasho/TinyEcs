using System.Numerics;

namespace TinyEcs.Bevy.UI;

// All UI pointer events fire as entity-targeted triggers (On<T>). The trigger's
// EntityId is the element under the pointer; observers can opt into bubble
// propagation up the parent chain (`trigger.Propagate(true)`).

public struct UiClick       { public Vector2 Position; }
public struct UiDoubleClick { public Vector2 Position; }
public struct UiOver        { }
public struct UiOut         { }
public struct UiPointerDown { public Vector2 Position; }
public struct UiPointerUp   { public Vector2 Position; }
// Pointer moved while over the target entity. Delta is the cursor displacement
// since the previous frame (logical pixels).
public struct UiMove        { public Vector2 Position; public Vector2 Delta; }
// Scroll wheel input dispatched to the entity under the pointer. Delta follows
// Bevy's convention: positive Y = scrolled up, positive X = scrolled right.
public struct UiScroll      { public Vector2 Position; public Vector2 Delta; }

// Hover INTENT (vs UiOver's immediate enter): the pointer rested over a
// UiHoverIntent-tagged entity for its delay. Fires once per rest; UiHoverEnd
// follows when the pointer leaves (only after a UiHoverStart fired). Tooltip
// timing, in one place.
public struct UiHoverStart  { public Vector2 Position; }
public struct UiHoverEnd    { }

/// <summary>Opt-in marker for UiHoverStart/UiHoverEnd. DelayMs &lt;= 0 uses the
/// 250 ms default.</summary>
public struct UiHoverIntent { public float DelayMs; }

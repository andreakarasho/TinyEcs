namespace TinyEcs.Bevy.Modding;

// 1-byte (not zero-size): TinyEcs `Data<T>` queries mis-cast columns for
// zero-size tags when the entity has other components, so anything iterated via
// Data<...> needs a real column. (Marker enumeration uses QueryBuilder, which is
// size-agnostic.)

/// Marks an entity spawned by a mod (for teardown on mod unload, and so the host
/// can scope mod-owned entities — e.g. the click bridge below).
public struct ModEntity { public byte Slot; }

/// Event fired on a mod-owned entity when it is clicked (Bevy.UI On&lt;UiClick&gt;:
/// press and release over the same entity).
public struct ModClick { }

/// Event fired on a mod-owned entity when it is right-clicked (On&lt;UiRightClick&gt;).
/// X/Y is the right-PRESS position in UI-layout space — where a context menu opens.
public struct ModRightClick { public float X; public float Y; }

/// Event fired on a mod-owned entity when the pointer enters (Over = true) or leaves
/// (Over = false) it — Bevy.UI's UiOver / UiOut on the topmost hovered entity.
public struct ModHover { public bool Over; }

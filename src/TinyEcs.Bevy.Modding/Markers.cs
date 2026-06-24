namespace TinyEcs.Bevy.Modding;

// 1-byte (not zero-size): TinyEcs `Data<T>` queries mis-cast columns for
// zero-size tags when the entity has other components, so anything iterated via
// Data<...> needs a real column. (Marker enumeration uses QueryBuilder, which is
// size-agnostic.)

/// Marks an entity spawned by a mod (for teardown on mod unload, and so the host
/// can scope mod-owned entities — e.g. the click bridge below).
public struct ModEntity { public byte Slot; }

/// One-frame tag set by the click bridge when a mod-owned interactive entity is
/// clicked (On&lt;UiClick&gt;). Mods poll it via a registered query; the plugin clears
/// it each frame (Stage.Update, after the Update runner). Bridges Bevy.UI's
/// observer-based clicks to the mod's poll-a-component model.
public struct ModClicked { public byte Tick; }

/// Stateful tag mirroring Bevy.UI's single HoveredEntity: the hover bridge inserts
/// it on UiOver and removes it on UiOut, so at most ONE mod entity carries it.
/// Mods poll a sparse `with ModHovered` query (the currently-hovered element) and
/// walk ancestors themselves — instead of scanning every interactive element's
/// Interaction byte each frame. NOT one-frame: it persists until the pointer
/// leaves, so there is no clear system (unlike ModClicked).
public struct ModHovered { public byte Tick; }

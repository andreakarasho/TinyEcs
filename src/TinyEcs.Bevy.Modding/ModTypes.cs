// Runtime-neutral schedule / observer-event / query-term enums the modding surface
// speaks. GuestBridge.cs, the scheduler in ModdingPlugin.cs, and both backends
// (CoreWasmModBackend translates the FlatBuffers ModAbi enums onto these; JcoModBackend
// consumes them directly) talk ONLY these neutral types — no generated bindings, so the
// lib compiles unchanged under WasmGuest.
//
// A case that carries a payload (Insert(type-path), Custom(string), ...) splits into
// the enum case here + a separate payload field on the owning type (ModSystemSpec.
// CustomStage, ModObserverSpec.TypePath, ModQueryTerm.TypePath) — a plain enum can't
// carry per-case data.

namespace TinyEcs.Bevy.Modding;

/// Which Bevy Stage a mod system runs in (plus the once-on-load ModStartup).
internal enum ModSchedule : byte
{
    /// Runs once when a mod is first loaded (not Stage.Startup).
    ModStartup,
    First,
    PreUpdate,
    Update,
    PostUpdate,
    Last,
    /// Payload (the custom stage's name) travels separately — see ModSystemSpec.CustomStage.
    Custom,
}

/// Observer event kind. Insert/Remove/Custom carry a type-path / event-name payload
/// separately — see ModObserverSpec.TypePath.
internal enum ModObserverKind : byte
{
    Spawn,
    Despawn,
    Insert,
    Remove,
    Custom,
}

/// Query term kind. Every case names a type-path — see ModQueryTerm.TypePath.
internal enum ModQueryTermKind : byte
{
    Ref,
    Mut,
    With,
    Without,
}

/// One query term: a ModQueryTermKind plus the type-path it names. SystemImpl.AddQuery
/// takes a span of these.
internal readonly struct ModQueryTerm(ModQueryTermKind kind, string typePath)
{
    public readonly ModQueryTermKind Kind = kind;
    public readonly string TypePath = typePath;
}

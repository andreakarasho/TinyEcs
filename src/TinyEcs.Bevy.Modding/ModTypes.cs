// Runtime-neutral mirrors of the tinyecs.wit `schedule` / `observer-event` /
// `query-for` variants. The fork's source generator emits these as
// Wit.Tinyecs.Modding.App.Schedule/ObserverEvent/QueryFor (aliased WitApp.* at call
// sites) from the WIT AdditionalFile — a wasmtime-only concept. GuestBridge.cs, the
// scheduler in ModdingPlugin.cs, and JcoModBackend.cs talk ONLY these neutral types;
// WasmtimeModBackend.cs (and its adapter file, WasmtimeGuestAdapters.cs) is the sole
// place that maps WitApp.* onto them. Keeps the runtime-agnostic half of the lib
// compilable with zero wasmtime/Wit.* references under WasmGuest.
//
// Each WIT variant case with a payload (Insert(type-path), Custom(string), ...)
// splits into the enum case here + a separate payload field on the type that holds
// it (ModSystemSpec.CustomStage, ModObserverSpec.TypePath, ModQueryTerm.TypePath) —
// a plain enum can't carry per-case data the way a WIT variant / C# discriminated
// union can.

namespace TinyEcs.Bevy.Modding;

/// Mirrors tinyecs.wit `schedule` (cuo Stage selection).
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

/// Mirrors tinyecs.wit `observer-event`. Insert/Remove/Custom carry a type-path /
/// event-name payload separately — see ModObserverSpec.TypePath.
internal enum ModObserverKind : byte
{
    Spawn,
    Despawn,
    Insert,
    Remove,
    Custom,
}

/// Mirrors tinyecs.wit `query-for`. Every case carries a type-path payload — see
/// ModQueryTerm.TypePath.
internal enum ModQueryTermKind : byte
{
    Ref,
    Mut,
    With,
    Without,
}

/// One query term: a ModQueryTermKind plus the type-path it names. Neutral
/// replacement for WitApp.QueryFor — SystemImpl.AddQuery takes a span of these.
internal readonly struct ModQueryTerm(ModQueryTermKind kind, string typePath)
{
    public readonly ModQueryTermKind Kind = kind;
    public readonly string TypePath = typePath;
}

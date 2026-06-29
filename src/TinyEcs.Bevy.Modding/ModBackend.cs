// Runtime-abstraction seam for the modding host. The scheduler (ModdingPlugin),
// the loaded-mod bookkeeping (ModRuntimes/ModControl) and the World-side query
// snapshotting are runtime-agnostic; everything that touches a concrete wasm
// runtime (engine/store/linker/component instance + canonical-ABI value
// marshalling + the generated WIT bindings) lives behind IModBackend /
// IModInstance. Select the backend with ModdingConfig.Backend.
//
// The WIT contracts and the mods themselves are unaffected by the choice — the
// seam is purely internal to this library.

namespace TinyEcs.Bevy.Modding;

/// Which wasm component runtime hosts the mods.
public enum WasmBackend : byte
{
    /// Native wasmtime via the wasmtime-dotnet fork (desktop default).
    Wasmtime,
}

/// One per process. Owns the runtime engine and instantiates mod components.
internal interface IModBackend : IDisposable
{
    /// Compile + instantiate a component, defining the host imports (the generic
    /// `app` bridge plus the game-specific per-mod hooks). Does NOT run the guest
    /// `setup` — the caller invokes IModInstance.Setup once the runtime is recorded.
    IModInstance Load(byte[] wasm, ModHostContext ctx);
}

/// One loaded mod, as the runtime-agnostic scheduler sees it. Implementations wrap
/// their runtime's component instance + generated bindings. Mechanics only: errors
/// throw and are logged by the caller (which knows the manifest name).
internal interface IModInstance : IDisposable
{
    /// Call the guest `setup(app)` — the guest registers its systems/observers
    /// into the shared ModHostContext.
    void Setup();

    /// Marshal a system's params (commands / query snapshots) and call its export.
    void RunSystem(ModSystemSpec sys);

    /// Call a guest observer callback `export(entity: u64, json: string)`.
    void CallObserver(string export, ulong entity, string json);

    /// Invoke a guest export shaped `export(arg: u8, data: list&lt;u8&gt;) -> bool` if the
    /// guest exports it; returns false (no call) when it is absent. Meaning-free — the lib
    /// assigns no semantics; a host names the export and uses it for an inline predicate
    /// hook it must consult synchronously (outside the per-frame scheduler).
    bool TryInvokeBoolExport(string export, byte arg, ReadOnlySpan<byte> data);

    /// Tear down + re-instantiate from fresh bytes, reusing the host imports, then
    /// re-run setup. The caller resets the shared ModHostContext first.
    void Reload(byte[] wasm);
}

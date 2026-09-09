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

/// Which wasm runtime hosts the mods.
public enum WasmBackend : byte
{
    /// Browser: host runs as a wasm component (NativeAOT-LLVM), mods are jco-transpiled
    /// and brokered by the JS glue. Requires ModdingConfig.JsChannel. See JcoModBackend.cs.
    Jco,
    /// Native core-wasm module mods (desktop default) via the upstream Wasmtime NuGet +
    /// the FlatSharp ModAbi wire contract (abi/mod-abi.fbs). See CoreWasmModBackend.cs.
    /// The component-model mod path (wasmtime-dotnet fork) was removed — a component
    /// binary is now rejected by ModdingPlugin's sniff guard.
    Core,
}

/// A mod component to load, backend-shaped: wasmtime instantiates from raw bytes
/// (Bytes required); Jco instantiates a pre-compiled, pre-transpiled component by
/// name (Bytes is null — sync instantiate-from-bytes is impossible in the browser).
/// Name is always the manifest name, so logging/errors can name the mod either way.
internal readonly struct ModSource(string name, byte[]? bytes)
{
    public readonly string Name = name;
    public readonly byte[]? Bytes = bytes;
}

/// One per process. Owns the runtime engine and instantiates mod components.
internal interface IModBackend : IDisposable
{
    /// Compile + instantiate a component, defining the host imports (the generic
    /// `app` bridge plus the game-specific per-mod hooks). Does NOT run the guest
    /// `setup` — the caller invokes IModInstance.Setup once the runtime is recorded.
    IModInstance Load(in ModSource source, ModHostContext ctx);
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

    /// Whether this mod actually asked for the inline bool export (the core ABI's
    /// wants_filter handshake bit), so a host can skip installing its hook entirely.
    /// Defaults to true for a backend that only learns at call time whether the guest
    /// exports it (Jco probes the export list per call).
    bool WantsFilter => true;

    /// The SECOND, independent inline bool export (the ABI's mod_filter_out, gated by
    /// wants_filter_out) — a host that needs two synchronous predicate hooks (e.g. one
    /// per traffic direction) drives them through the two slots. Defaults to "absent"
    /// so an implementation that predates the slot needs no edit.
    bool WantsFilterOut => false;

    /// Slot-2 twin of TryInvokeBoolExport. Same contract; false (no call) when absent.
    bool TryInvokeBoolExportOut(string export, byte arg, ReadOnlySpan<byte> data) => false;

    /// Tear down + re-instantiate, reusing the host imports, then re-run setup. The
    /// caller resets the shared ModHostContext first. Wasmtime instantiates fresh
    /// from source.Bytes; Jco is deferred-capable — it may kick an async recompile
    /// and swap the instance on a LATER call (fire-and-forget from this call's POV).
    void Reload(in ModSource source);
}

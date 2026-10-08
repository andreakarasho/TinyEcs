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
    /// Native (default). Desktop: wasm32-wasip2 component mods on the embedded
    /// wasmtime (Component/ComponentModBackend.cs); a core-wasm module is rejected at
    /// load. A guest that sets ModdingConfig.WasmExecutor instead relays every mod to
    /// its native host over the FlatSharp wire (abi/mod-abi.fbs, CoreWasmModBackend.cs)
    /// — internal to that guest/host pair, mods never see it.
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

    /// UTF8 overload (what FlushObservers calls): `json` is valid for the call only.
    /// Default bounces off the string one.
    void CallObserver(string export, ulong entity, ReadOnlySpan<byte> json)
        => CallObserver(export, entity, System.Text.Encoding.UTF8.GetString(json));

    /// Run the packet observer `export` (a Packet ModObserverSpec.Name) on one message
    /// (ModPacketChain) and return its verdict; its params are evaluated and its
    /// commands applied like any observer's. For Replace, `replacement` is valid until
    /// the next call on this instance. Defaults to Pass for a backend without packet
    /// observers.
    ModPacketVerdict CallPacketObserver(string export, ModPacketDirection dir, ReadOnlySpan<byte> packet, out ReadOnlySpan<byte> replacement)
    {
        replacement = default;
        return ModPacketVerdict.Pass;
    }

    /// Tear down + re-instantiate, reusing the host imports, then re-run setup. The
    /// caller resets the shared ModHostContext first. The component backend instantiates fresh
    /// from source.Bytes; Jco is deferred-capable — it may kick an async recompile
    /// and swap the instance on a LATER call (fire-and-forget from this call's POV).
    void Reload(in ModSource source);
}

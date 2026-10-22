// Runtime-abstraction seam for the modding host. The scheduler (ModdingPlugin),
// the loaded-mod bookkeeping (ModRuntimes/ModControl) and the World-side query
// snapshotting are runtime-agnostic; everything that touches a concrete wasm
// runtime (engine/store/linker/component instance + canonical-ABI value
// marshalling + the generated WIT bindings) lives behind IModBackend /
// IModInstance. Two backends: the embedded component backend
// (Component/ComponentModBackend.cs, wasm32-wasip2 component mods on wasmtime) and,
// when ModdingConfig.WasmExecutor is set, the relay (ModRelayBackend.cs) a wasm guest
// uses to hand its mods to its native host over abi/mod-abi.fbs.
//
// The WIT contracts and the mods themselves are unaffected by the choice — the
// seam is purely internal to this library.

namespace TinyEcs.Bevy.Modding;

/// A host call a mod made that can't be served (unknown function, malformed args): the
/// mod traps with this message.
public sealed class ModCallException(string message, Exception? inner = null) : Exception(message, inner);

/// A mod component to load: the component backend instantiates from raw bytes
/// (Bytes required); the relay is name/slot-keyed (Bytes is null — the native host
/// holds the component). Name is always the manifest name, so logging/errors can
/// name the mod either way. Dir = the mod's own folder, its WASI filesystem root
/// (null = no filesystem).
internal readonly struct ModSource(string name, byte[]? bytes, string? dir = null)
{
    public readonly string Name = name;
    public readonly byte[]? Bytes = bytes;
    public readonly string? Dir = dir;
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

    /// Call a guest observer: `value` is the trigger's value (Encoding.Typed; empty for a
    /// tag or a despawn), valid for the call only.
    void CallObserver(string export, ulong entity, ReadOnlySpan<byte> value);

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
    /// from source.Bytes; the relay asks the native host to re-instantiate by slot.
    void Reload(in ModSource source);
}

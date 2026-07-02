// JCO backend: hosts mods in the BROWSER, where the C# host itself runs as a wasm
// component (NativeAOT-LLVM) and therefore cannot embed a wasm runtime in-process.
// Instead the mod components are jco-transpiled (https://github.com/bytecodealliance/jco)
// and instantiated by the JS glue; the host and the mods are SIBLING wasm modules and
// the JS layer is the linker between them.
//
// Two directions cross the JS boundary:
//
//   host -> guest  (this file): Setup / RunSystem / observer / packet / reload. The
//     host marshals the call and forwards it through IJsModChannel, which calls the
//     jco-instantiated mod's matching export. This half is pure C# and is what this
//     file implements — it slots into IModInstance like the wasmtime backend.
//
//   guest -> host  (NOT here): the tinyecs:modding/app bridge a mod calls back into
//     (spawn / component get-set / query iter / emit-event). With jco those land in
//     JS, which calls the host component's EXPORTS, which delegate to ModHostContext.
//     The dispatch logic maps the same calls onto ModHostContext as the wasmtime bridge
//     (GuestBridge.cs — AppImpl / CommandsImpl / QueryImpl); reuse that, don't rebuild it.
//     Exposing it as host exports needs the WASM_GUEST host world ([UnmanagedCallersOnly]
//     + a host WIT), a separate build that does not exist yet. So IJsModChannel's
//     implementation lives in the host under WASM_GUEST; the lib only declares the contract.
//
// UNPROVEN (cannot be exercised on desktop — no JS, no jco): reentrancy. A host->guest
// call that triggers a guest->host callback re-enters the host component while it is
// mid-dispatch (host -> JS -> mod -> JS -> host). The Component Model + jco reentrancy
// rules decide whether that traps. This must be spiked with one trivial mod before any
// of this is trusted. The seam is shaped so that spike only has to implement IJsModChannel.

using System.Buffers;

namespace TinyEcs.Bevy.Modding;

/// One prepared system parameter, handed across the JS boundary for a RunSystem call.
/// Public because the channel is implemented by the host (a different assembly). The
/// query snapshot is host-owned ArrayPool memory, valid ONLY for the duration of the
/// synchronous RunSystem call — the channel impl must consume it before returning (the
/// guest iterates it inline, exactly as the wasmtime backend relies on).
public readonly struct ModRunParam
{
    /// true = a `commands` resource; false = a `query` resource (Snapshot/Components set).
    public readonly bool IsCommands;
    /// Matching-entity ids (Query only). Length is a pooled capacity, NOT the count.
    public readonly ulong[]? Snapshot;
    /// Valid prefix length of Snapshot.
    public readonly int Matched;
    /// ref/mut columns in declared order — defines the query-result component index.
    public readonly IReadOnlyList<(string TypePath, bool Mut)>? Components;

    private ModRunParam(bool isCommands, ulong[]? snapshot, int matched, IReadOnlyList<(string, bool)>? components)
    {
        IsCommands = isCommands;
        Snapshot = snapshot;
        Matched = matched;
        Components = components;
    }

    public static ModRunParam Commands() => new(true, null, 0, null);
    public static ModRunParam Query(ulong[] snapshot, int matched, IReadOnlyList<(string, bool)> components)
        => new(false, snapshot, matched, components);
}

/// The host->guest transport for the JCO backend. Implemented by the WASM_GUEST host
/// (it owns the component-model imports the JS glue satisfies); supplied to the lib via
/// ModdingConfig.JsChannel. One channel per process, shared by every loaded mod (each
/// addressed by the handle Load returns). Every method drives one guest call to
/// completion SYNCHRONOUSLY — the ECS schedule is synchronous, so the channel must not
/// yield (use Component Model sync lowering, not async).
///
/// Mods are pre-compiled and name-keyed, NOT loaded from bytes: sync instantiate-from-
/// bytes is impossible in the browser (only `new WebAssembly.Instance(precompiledModule,
/// imports)` is sync-legal), so discovery + load both go by name. See ModdingPlugin.
/// SetupEcsMods, which uses ListMods() instead of a filesystem scan when JsChannel is set.
public interface IJsModChannel : IDisposable
{
    /// The mods JS has pre-transpiled and can instantiate, as a JSON array of
    /// manifests (name/version/ruleset — the same shape as mod.json, parsed via
    /// ModManifestJsonContext). Replaces the filesystem scan the wasmtime backend
    /// uses. Called once at startup; a mod not listed here cannot be Load'ed.
    string ListMods();

    /// Sync-instantiate the named mod's pre-compiled component (JS is the linker)
    /// and record ctx for the guest->host bridge calls this mod will make. `name`
    /// must be one a prior ListMods() call returned. Returns an opaque handle.
    int Load(int slot, string name, ModHostContext ctx);

    /// Call the guest `setup(app)`.
    void Setup(int handle);

    /// Call a guest system export with the prepared params (the channel wraps each as a
    /// JS-side resource proxy whose callbacks route to the host bridge exports).
    void RunSystem(int handle, string name, ReadOnlySpan<ModRunParam> @params);

    /// Call a guest observer callback `export(entity: u64, json: string)`.
    void CallObserver(int handle, string export, ulong entity, string json);

    /// Invoke `export(arg: u8, data: list<u8>) -> bool` if present; false when absent
    /// (the JS side probes the mod's exports). Meaning-free — see IModInstance.TryInvokeBoolExport.
    bool TryInvokeBoolExport(int handle, string export, byte arg, ReadOnlySpan<byte> data);

    /// Deferred-capable: kicks a re-instantiate of the mod named at Load time. JS may
    /// recompile asynchronously and swap the live instance on a LATER call (this call
    /// itself does not block on it) — the guest treats reload as fire-and-forget, unlike
    /// the wasmtime backend's synchronous Reload. No bytes: JS re-fetches/recompiles by
    /// the name it already has.
    void Reload(int handle);
}

internal sealed class JcoModBackend : IModBackend
{
    private readonly IJsModChannel _channel;

    public JcoModBackend(IJsModChannel channel) => _channel = channel;

    public IModInstance Load(in ModSource source, ModHostContext ctx)
        => new JcoModInstance(_channel, ctx, _channel.Load(ctx.Slot, source.Name, ctx));

    // The channel owns the JS-side instances; one channel per process, disposed once.
    public void Dispose() => _channel.Dispose();
}

internal sealed class JcoModInstance : IModInstance
{
    private readonly IJsModChannel _channel;
    private readonly ModHostContext _ctx;
    private readonly int _handle;

    // Pooled snapshots in flight for one RunSystem call, returned after the channel call
    // (the guest iterates them synchronously within it). Same discipline as the other backends.
    private readonly List<ulong[]> _snapshotScratch = new();
    // Reused param buffer (grows on demand). ModRunParam holds managed refs so it can't
    // be stackalloc'd; one channel call consumes [0,count) before returning, so reuse is safe.
    private ModRunParam[] _paramScratch = System.Array.Empty<ModRunParam>();

    public JcoModInstance(IJsModChannel channel, ModHostContext ctx, int handle)
    {
        _channel = channel;
        _ctx = ctx;
        _handle = handle;
    }

    public void Setup() => _channel.Setup(_handle);

    public void CallObserver(string export, ulong entity, string json)
        => _channel.CallObserver(_handle, export, entity, json);

    public bool TryInvokeBoolExport(string export, byte arg, ReadOnlySpan<byte> data)
        => _channel.TryInvokeBoolExport(_handle, export, arg, data);

    public void RunSystem(ModSystemSpec sys)
    {
        var count = sys.Params.Count;
        if (_paramScratch.Length < count)
            _paramScratch = new ModRunParam[count];
        var @params = _paramScratch;
        _snapshotScratch.Clear();

        for (var i = 0; i < count; i++)
        {
            var p = sys.Params[i];
            if (p.Kind == ModParamKind.Commands)
            {
                @params[i] = ModRunParam.Commands();
            }
            else
            {
                var snapshot = ModdingPlugin.BuildSnapshot(_ctx, p.Query!, out var matched);
                _snapshotScratch.Add(snapshot);
                @params[i] = ModRunParam.Query(snapshot, matched, p.Query!.Components);
            }
        }

        try
        {
            _channel.RunSystem(_handle, sys.Name, @params.AsSpan(0, count));
        }
        finally
        {
            foreach (var arr in _snapshotScratch)
                ArrayPool<ulong>.Shared.Return(arr);
            _snapshotScratch.Clear();
        }
    }

    public void Reload(in ModSource source) => _channel.Reload(_handle);

    // The channel owns JS-side instance lifetime; nothing local to drop.
    public void Dispose() { }
}

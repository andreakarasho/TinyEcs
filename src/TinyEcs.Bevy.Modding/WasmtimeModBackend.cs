// Wasmtime backend: hosts mods on native wasmtime via the wasmtime-dotnet fork.
// All wasmtime types + the fork's generated WIT bindings (Wit.Tinyecs.Modding.*)
// are confined to this file — the rest of the library talks IModBackend/IModInstance.
//
// Behaviour is byte-for-byte the prior inline implementation that lived in
// ModdingPlugin/ModRuntimes; it was lifted here unchanged. Diagnostics (manifest
// name in log lines) stay with the caller, so these methods just throw on error.

using System.Buffers;
using Wasmtime;
using G = Wit.Tinyecs.Modding.GuestImports;

namespace TinyEcs.Bevy.Modding;

internal sealed class WasmtimeModBackend : IModBackend
{
    private readonly Engine _engine = new();
    // Game-specific per-mod linker hooks (e.g. cuo:modding/net + ui), defined once
    // per mod after the generic `app` bridge and before instantiation. Reused as-is
    // on reload (the host imports never change between loads).
    private readonly IReadOnlyList<Action<Linker, ModHostContext>> _perModLinker;

    public WasmtimeModBackend(IReadOnlyList<Action<Linker, ModHostContext>> perModLinker)
        => _perModLinker = perModLinker;

    public IModInstance Load(in ModSource source, ModHostContext ctx)
    {
        var linker = new Linker(_engine);
        linker.AddWasiP2();
        var imports = new GuestBridge(ctx);
        linker.Define(imports);
        foreach (var hook in _perModLinker)
            hook(linker, ctx);

        var store = new Store(_engine);
        store.AddWasiP2(inheritStdout: true, inheritStderr: true);

        var component = Component.Compile(_engine, source.Bytes!);
        var instance = store.GetComponentInstance(component, linker);
        var exports = new Wit.Tinyecs.Modding.GuestExports(instance, store);

        return new WasmtimeModInstance(_engine, linker, imports, store, instance, exports, ctx);
    }

    public void Dispose() => _engine.Dispose();
}

internal sealed class WasmtimeModInstance : IModInstance
{
    private readonly Engine _engine;
    private readonly Linker _linker;
    private readonly GuestBridge _imports;
    private readonly ModHostContext _ctx;
    private Store _store;
    private ComponentInstance _instance;
    private Wit.Tinyecs.Modding.GuestExports _exports;

    // Reused per RunSystem: the ArrayPool-rented query snapshots in flight for one
    // system call, returned after the wasm Call. Safe to share (one per instance):
    // the stage runners are SingleThreaded and a call is never re-entered.
    private readonly List<ulong[]> _snapshotScratch = new();
    // Presence of a host-called guest export, by name. GetFunction throws on a
    // missing export and does NOT cache the miss, so we cache it here to keep the
    // packet hot path exception-free. Cleared on reload (fresh instance).
    private readonly Dictionary<string, bool> _exportPresence = new();

    public WasmtimeModInstance(
        Engine engine, Linker linker, GuestBridge imports, Store store,
        ComponentInstance instance, Wit.Tinyecs.Modding.GuestExports exports, ModHostContext ctx)
    {
        _engine = engine;
        _linker = linker;
        _imports = imports;
        _store = store;
        _instance = instance;
        _exports = exports;
        _ctx = ctx;
    }

    public void Setup()
    {
        var appHandle = _imports.RegisterApp(new AppAdapter(new AppImpl(_ctx)));
        _exports.Setup(appHandle);
    }

    private bool HasExport(string name)
    {
        if (_exportPresence.TryGetValue(name, out var has))
            return has;
        try { _instance.GetFunction(name); has = true; }
        catch { has = false; }
        _exportPresence[name] = has;
        return has;
    }

    public bool TryInvokeBoolExport(string export, byte arg, ReadOnlySpan<byte> data)
    {
        if (!HasExport(export))
            return false;

        Span<ComponentValue> args = stackalloc ComponentValue[2];
        var idVal = ComponentValue.CreateByte(arg);
        var lb = new ListBuilder(data.Length);
        for (var i = 0; i < data.Length; i++)
            lb[i] = ComponentValue.CreateByte(data[i]);
        var listVal = ComponentValue.CreateList(lb, externallyOwned: false);

        args[0] = idVal;
        args[1] = listVal;
        try
        {
            using var res = _instance.Call(export, 1, args);
            return res.Length > 0 && res[0].ToBoolean();
        }
        finally
        {
            idVal.Dispose(_store);
            listVal.Dispose(_store);
        }
    }

    public void CallObserver(string export, ulong entity, string json)
    {
        Span<ComponentValue> args = stackalloc ComponentValue[2];
        args[0] = ComponentValue.CreateUInt64(entity);
        args[1] = ComponentValue.CreateString(json, true);
        try
        {
            using var _ = _instance.Call(export, 0, args);
        }
        finally
        {
            args[0].Dispose(_store);
            args[1].Dispose(_store);
        }
    }

    public void RunSystem(ModSystemSpec sys)
    {
        var count = sys.Params.Count;
        // Param counts are tiny; stack the common case, heap-fall-back for the rare large one.
        Span<ComponentValue> vals = count <= 16 ? stackalloc ComponentValue[count] : new ComponentValue[count];
        _snapshotScratch.Clear();

        for (var i = 0; i < count; i++)
        {
            var p = sys.Params[i];
            if (p.Kind == ModParamKind.Commands)
            {
                var h = _imports.RegisterCommands(new CommandsAdapter(new CommandsImpl(_ctx)));
                vals[i] = ComponentValue.CreateOwnResource(_store, h, G.CommandsTypeId);
            }
            else
            {
                var snapshot = ModdingPlugin.BuildSnapshot(_ctx, p.Query!, out var matched);
                _snapshotScratch.Add(snapshot);
                var h = _imports.RegisterQuery(new QueryAdapter(new QueryImpl(_ctx, snapshot, matched, p.Query!.Components)));
                vals[i] = ComponentValue.CreateOwnResource(_store, h, G.QueryTypeId);
            }
        }

        try
        {
            using var _ = _instance.Call(sys.Name, 0, vals);
        }
        finally
        {
            for (var i = 0; i < count; i++)
                vals[i].Dispose(_store);
            // Return query snapshots only AFTER the Call: the guest iterates them
            // synchronously within Call and drops the resource at fn end.
            foreach (var arr in _snapshotScratch)
                ArrayPool<ulong>.Shared.Return(arr);
            _snapshotScratch.Clear();
        }
    }

    // Tear the mod down and re-instantiate from fresh bytes. REUSE the existing
    // Linker + bridge: the host import functions never change between loads, and the
    // fork registers every linker.Define'd function in a STATIC, process-global,
    // never-freed table capped at 1024 — re-Define'ing on reload leaks a full set of
    // slots each time. So we only recompile + instantiate on a fresh Store.
    public void Reload(in ModSource source)
    {
        try { _store.Dispose(); } catch { /* already torn down */ }
        _exportPresence.Clear();

        _store = new Store(_engine);
        _store.AddWasiP2(inheritStdout: true, inheritStderr: true);

        var component = Component.Compile(_engine, source.Bytes!);
        _instance = _store.GetComponentInstance(component, _linker);
        _exports = new Wit.Tinyecs.Modding.GuestExports(_instance, _store);

        Setup();
    }

    public void Dispose()
    {
        try { _store.Dispose(); } catch { /* already torn down */ }
    }
}

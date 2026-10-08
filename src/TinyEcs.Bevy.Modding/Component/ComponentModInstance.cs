// One loaded component mod: its store + instance, the systems/observers it declared in
// `setup`, the host-resource handle table backing every `tinyecs:modding/ecs` handle it
// holds, and the command buffer its `commands` params fill during a call.
//
// Semantics mirror the core ABI runner (ModAbiRunner) term for term:
//  - queries are the same ModQuerySpec + ModdingPlugin.BuildSnapshot, evaluated
//    BEFORE the call with the system's own (lastRun, thisRun] change window, the same
//    idle-skip, despawned rows skipped;
//  - `mut` rows write back through the registry on `row.set` (the snapshot is a copy
//    of ids, so in-call writes can't disturb iteration);
//  - commands apply in order AFTER the export returns (spawn hands out the id at once);
//  - observers ride the same host global observers (ModdingPlugin.RegisterModObservers),
//    buffered and flushed at the same points; packet observers are called
//    synchronously by ModPacketChain through `observe-packet`.
// Two term kinds have no core-ABI twin and are handled here: `added` (a read term
// post-filtered on IModComponent.AddedSince) and the res / events parameters.

extern alias WasmtimeCm;

using System.Buffers;
using System.Runtime.InteropServices;
using Cm = WasmtimeCm::Wasmtime;

namespace TinyEcs.Bevy.Modding;

internal enum HandleKind : byte { Free, System, App, Commands, Query, Row, Res, Events }

internal struct HandleSlot
{
    public HandleKind Kind;
    public object? Obj;
    public ulong Entity;
    public int NextFree;
}

/// rep = slot index + 1. Freed by the resource destructor when the guest drops the
/// handle, so per-call handles (params, rows) recycle their slots every call.
internal sealed class HandleTable
{
    private HandleSlot[] _slots = new HandleSlot[32];
    private int _count;
    private int _free = -1;

    public uint Alloc(HandleKind kind, object? obj, ulong entity = 0)
    {
        int index;
        if (_free >= 0)
        {
            index = _free;
            _free = _slots[index].NextFree;
        }
        else
        {
            if (_count == _slots.Length)
                Array.Resize(ref _slots, _slots.Length * 2);
            index = _count++;
        }
        _slots[index] = new HandleSlot { Kind = kind, Obj = obj, Entity = entity };
        return (uint)index + 1;
    }

    public ref HandleSlot Get(uint rep, HandleKind kind)
    {
        var index = (int)rep - 1;
        if ((uint)index >= (uint)_count || _slots[index].Kind != kind)
            ThrowBad(rep, kind);
        return ref _slots[index];
    }

    public void Free(uint rep)
    {
        var index = (int)rep - 1;
        if ((uint)index >= (uint)_count || _slots[index].Kind == HandleKind.Free)
            return;
        _slots[index] = new HandleSlot { Kind = HandleKind.Free, NextFree = _free };
        _free = index;
    }

    public void Clear()
    {
        Array.Clear(_slots, 0, _count);
        _count = 0;
        _free = -1;
    }

    private static void ThrowBad(uint rep, HandleKind kind)
        => throw new InvalidOperationException($"invalid {kind} handle {rep}");
}

internal enum ComponentParamKind : byte { Commands, Query, Res, Events }

/// Events of one type sent since the owning system's last run. Filled by a host global
/// observer registered when the system declares the parameter.
internal sealed class ComponentEventBuffer
{
    // A guest that never runs (disabled, throttled out) must not grow this forever.
    private const int Cap = 1024;

    public readonly List<string> Pending = new();
    public readonly List<string> Current = new();
    // Set when the mod reloads: the old host observer can't be unregistered, so it
    // keeps firing into a buffer nobody reads anymore.
    public bool Dead;

    public void Push(string json)
    {
        if (Dead)
            return;
        if (Pending.Count == Cap)
            Pending.RemoveAt(0);
        Pending.Add(json);
    }

    public void Swap()
    {
        Current.Clear();
        Current.AddRange(Pending);
        Pending.Clear();
    }
}

internal sealed class ComponentParam
{
    public ComponentParamKind Kind;
    // Query
    public ModQuerySpec? Query;
    public IModComponent[]? Added;
    public ulong[]? Snapshot;
    public int Count;
    public int Cursor;
    // Res / Events
    public string Path = "";
    public bool Mutable;
    public ComponentEventBuffer? Events;
}

internal sealed class ComponentSystem
{
    public readonly SystemImpl Impl;
    public readonly byte[] NameUtf8;
    public readonly List<ComponentParam> Params = new();

    public ComponentSystem(string name)
    {
        Impl = new SystemImpl(name);
        NameUtf8 = System.Text.Encoding.UTF8.GetBytes(name);
    }

    public ModSystemSpec Spec => Impl.Spec;

    public void AddCommands()
    {
        Impl.AddCommands();
        Params.Add(new ComponentParam { Kind = ComponentParamKind.Commands });
    }

    // `added` reads like `ref` (a row payload) and is post-filtered on the added tick, so
    // the shared spec / snapshot / row-index logic stays untouched.
    public void AddQuery(ModHostContext ctx, ReadOnlySpan<(string Case, string Path)> terms)
    {
        var neutral = new ModQueryTerm[terms.Length];
        List<IModComponent>? added = null;
        for (var i = 0; i < terms.Length; i++)
        {
            var (kind, path) = terms[i];
            neutral[i] = new ModQueryTerm(kind switch
            {
                "ref" or "added" => ModQueryTermKind.Ref,
                "mut" => ModQueryTermKind.Mut,
                "with" => ModQueryTermKind.With,
                "without" => ModQueryTermKind.Without,
                "changed" => ModQueryTermKind.Changed,
                _ => throw new InvalidOperationException($"system '{Spec.Name}': unknown query term '{kind}'"),
            }, path);
            if (kind == "mut" && ctx.Registry.TryGet(path, out var mutComp))
                ModQueryTerm.RejectReadOnlyMut(ctx, $"system '{Spec.Name}'", mutComp, path);
            if (kind == "added")
            {
                if (!ctx.Registry.TryGet(path, out var comp))
                    throw new InvalidOperationException($"system '{Spec.Name}': unregistered component '{path}'");
                (added ??= new()).Add(comp);
            }
        }
        Impl.AddQuery(neutral);
        Params.Add(new ComponentParam
        {
            Kind = ComponentParamKind.Query,
            Query = Spec.Params[^1].Query,
            Added = added?.ToArray(),
        });
    }

    public void AddRes(string path, bool mutable)
        => Params.Add(new ComponentParam { Kind = ComponentParamKind.Res, Path = path, Mutable = mutable });

    public void AddEvents(ModHostContext ctx, string path)
    {
        var buffer = new ComponentEventBuffer();
        if (ctx.App != null && ctx.Registry.TryGetEvent(path, out var ev))
            ev.RegisterObserver(ctx.App, (_, json) => buffer.Push(json));
        Params.Add(new ComponentParam { Kind = ComponentParamKind.Events, Path = path, Events = buffer });
    }
}

/// Commands recorded during one guest call, applied in order once it returns.
internal sealed class ComponentCommandBuffer
{
    private enum Op : byte { Insert, Remove, Despawn, Send }

    private struct Entry
    {
        public Op Op;
        public ulong Entity;
        public int First;
        public int Count;
    }

    private readonly ComponentModInstance _owner;
    private readonly List<Entry> _entries = new();
    // Bundle items / removed paths / the event name; Len = -1 for a path-only item.
    private readonly List<(string Path, int Off, int Len)> _items = new();
    private byte[] _payload = new byte[1024];
    private int _payloadLen;

    private readonly List<(string, ReadOnlyMemory<byte>)> _bundleScratch = new();
    private readonly List<string> _pathScratch = new();

    public ComponentCommandBuffer(ComponentModInstance owner) => _owner = owner;

    public ulong Spawn(in Cm.ComponentValue bundle)
    {
        var id = new CommandsImpl(_owner.Ctx).SpawnEmpty().Id().EcsId;
        Insert(id, bundle);
        return id;
    }

    public void Insert(ulong entity, in Cm.ComponentValue bundle)
    {
        var list = bundle.ToListBuilder();
        var first = _items.Count;
        for (var i = 0; i < list.Length; i++)
        {
            var pair = list[i].ToTuple();
            var json = pair[1].ToUtf8Span();
            _items.Add((_owner.Backend.Path(pair[0]), Copy(json), json.Length));
        }
        _entries.Add(new Entry { Op = Op.Insert, Entity = entity, First = first, Count = list.Length });
    }

    public void Remove(ulong entity, in Cm.ComponentValue paths)
    {
        var list = paths.ToListBuilder();
        var first = _items.Count;
        for (var i = 0; i < list.Length; i++)
            _items.Add((_owner.Backend.Path(list[i]), 0, -1));
        _entries.Add(new Entry { Op = Op.Remove, Entity = entity, First = first, Count = list.Length });
    }

    public void Despawn(ulong entity)
        => _entries.Add(new Entry { Op = Op.Despawn, Entity = entity });

    public void Send(string eventPath, ReadOnlySpan<byte> json)
    {
        _items.Add((eventPath, Copy(json), json.Length));
        _entries.Add(new Entry { Op = Op.Send, First = _items.Count - 1, Count = 1 });
    }

    private int Copy(ReadOnlySpan<byte> bytes)
    {
        if (_payloadLen + bytes.Length > _payload.Length)
            Array.Resize(ref _payload, Math.Max(_payload.Length * 2, _payloadLen + bytes.Length));
        var off = _payloadLen;
        bytes.CopyTo(_payload.AsSpan(off));
        _payloadLen += bytes.Length;
        return off;
    }

    public void Discard()
    {
        _entries.Clear();
        _items.Clear();
        _payloadLen = 0;
    }

    // Per-entry guard like ModAbiRunner.ApplyCommandBuffer: one bad payload (a JSON
    // body the registry refuses) must not drop every command after it.
    public void Apply()
    {
        if (_entries.Count == 0)
            return;
        var ctx = _owner.Ctx;
        var commands = new CommandsImpl(ctx);
        for (var i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            try
            {
                switch (e.Op)
                {
                    case Op.Insert:
                        _bundleScratch.Clear();
                        for (var k = 0; k < e.Count; k++)
                        {
                            var (path, off, len) = _items[e.First + k];
                            _bundleScratch.Add((path, len == 0 ? EmptyObject : _payload.AsMemory(off, len)));
                        }
                        commands.EntityById(e.Entity).Insert(CollectionsMarshal.AsSpan(_bundleScratch));
                        break;
                    case Op.Remove:
                        _pathScratch.Clear();
                        for (var k = 0; k < e.Count; k++)
                            _pathScratch.Add(_items[e.First + k].Path);
                        commands.EntityById(e.Entity).Remove(CollectionsMarshal.AsSpan(_pathScratch));
                        break;
                    case Op.Despawn:
                        commands.EntityById(e.Entity).Despawn();
                        break;
                    case Op.Send:
                    {
                        var (path, off, len) = _items[e.First];
                        commands.EmitEvent(path, 0, _payload.AsSpan(off, len));
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ecs-mod] {0} command #{1} ({2}) failed: {3}", ctx.Name, i, e.Op, ex.Message);
            }
        }
        Discard();
    }

    // An empty payload IS "{}" (a tag carries no data), as on the core ABI.
    private static readonly ReadOnlyMemory<byte> EmptyObject = "{}"u8.ToArray();
}

internal sealed unsafe class ComponentModInstance : IModInstance
{
    internal readonly ComponentModBackend Backend;
    internal readonly ModHostContext Ctx;
    internal readonly HandleTable Handles = new();
    internal readonly ComponentCommandBuffer Commands;

    private readonly Cm.Engine _engine;
    private Cm.Store _store = null!;
    private Cm.Component _component = null!;
    private Cm.ComponentInstance _instance = null!;
    private Cm.ComponentInstanceFunction _setup;
    private Cm.ComponentInstanceFunction _run;
    private Cm.ComponentInstanceFunction _observe;
    private Cm.ComponentInstanceFunction _observePacket;

    private readonly Dictionary<ModSystemSpec, ComponentSystem> _systems = new();
    private readonly Dictionary<string, ComponentSystem> _observers = new();
    // Host-called exports (TryCallExport), looked up once per instance; null = not exported.
    private readonly Dictionary<string, Cm.ComponentInstanceFunction?> _exports = new();
    private readonly List<ComponentEventBuffer> _eventBuffers = new();
    private readonly ModJsonBuffer _json = new();
    // The last packet observer's replacement (valid until the next call).
    private byte[] _packetReplacement = new byte[512];
    private int _packetReplacementLength;

    public ComponentModInstance(ComponentModBackend backend, Cm.Engine engine, byte[] bytes, ModHostContext ctx)
    {
        Backend = backend;
        Ctx = ctx;
        _engine = engine;
        Commands = new ComponentCommandBuffer(this);
        Instantiate(bytes);
    }

    private void Instantiate(byte[] bytes)
    {
        _store = new Cm.Store(_engine);
        _store.AddWasiP2(inheritStdout: true, inheritStderr: true);
        _component = Cm.Component.Compile(_engine, bytes);
        _instance = _store.GetComponentInstance(_component, Backend.Linker);
        _exports.Clear();
        _setup = _instance.GetFunction("setup");
        _run = _instance.GetFunction("run");
        _observe = _instance.GetFunction("observe");
        _observePacket = _instance.GetFunction("observe-packet");
    }

    // ── setup-time callbacks (from ComponentModBackend's host functions) ──────

    internal uint NewSystem(string name) => Handles.Alloc(HandleKind.System, new ComponentSystem(name));

    internal ComponentSystem SystemOf(uint rep) => (ComponentSystem)Handles.Get(rep, HandleKind.System).Obj!;

    internal void AddSystem(ModSchedule schedule, ComponentSystem sys)
    {
        ReadOnlySpan<SystemImpl> one = [sys.Impl];
        new AppImpl(Ctx).AddSystems(schedule, null, one);
        _systems[sys.Spec] = sys;
        TrackEvents(sys);
    }

    internal void AddObserver(ModObserverKind kind, string path, ComponentSystem sys)
    {
        var token = "c" + _observers.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _observers[token] = sys;
        new AppImpl(Ctx).AddObserver(token, kind, path);
        TrackEvents(sys);
    }

    internal void AddPacketObserver(ModPacketDirection dir, ReadOnlySpan<byte> ids, ComponentSystem sys)
    {
        var token = "c" + _observers.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _observers[token] = sys;
        new AppImpl(Ctx).AddPacketObserver(token, dir, ids);
        TrackEvents(sys);
    }

    private void TrackEvents(ComponentSystem sys)
    {
        foreach (var p in sys.Params)
            if (p.Events != null && !_eventBuffers.Contains(p.Events))
                _eventBuffers.Add(p.Events);
    }

    // ── call-time callbacks ───────────────────────────────────────────────────

    internal bool NextRow(ComponentParam q, out uint rep)
    {
        var world = Ctx.World;
        while (q.Cursor < q.Count)
        {
            var id = q.Snapshot![q.Cursor++];
            if (world.Exists(id))
            {
                rep = Handles.Alloc(HandleKind.Row, q, id);
                return true;
            }
        }
        rep = 0;
        return false;
    }

    internal ReadOnlySpan<byte> RowGet(ComponentParam q, ulong entity, byte index)
    {
        var comps = q.Query!.Components;
        if (index >= comps.Count)
            throw new InvalidOperationException($"row.get({index}): the query reads {comps.Count} components");
        _json.Reset();
        if (Ctx.Registry.TryGet(comps[index].typePath, out var comp) && Ctx.World.Exists(entity) && comp.Has(Ctx.World, entity))
            comp.GetJsonUtf8(Ctx.World, entity, _json);
        else
            IModComponent.WriteUtf8(_json, "null");
        return _json.WrittenSpan;
    }

    internal void RowSet(ComponentParam q, ulong entity, byte index, ReadOnlySpan<byte> json)
    {
        var comps = q.Query!.Components;
        if (index >= comps.Count)
            throw new InvalidOperationException($"row.set({index}): the query reads {comps.Count} components");
        var (path, mutable) = comps[index];
        if (!mutable)
            throw new InvalidOperationException($"row.set({index}): {path} is not a `mut` term");
        if (Ctx.World.Exists(entity) && Ctx.Registry.TryGet(path, out var comp))
            comp.SetJsonUtf8(Ctx.World, entity, json);
    }

    // The value's UTF8 JSON (valid until the next RowGet/ResGet), or false when absent.
    internal bool ResGet(ComponentParam p, out ReadOnlySpan<byte> json)
    {
        json = default;
        if (Ctx.App == null || !Ctx.Registry.TryGetResource(p.Path, out var r))
            return false;
        _json.Reset();
        r.GetJsonUtf8For(Ctx.App, Ctx.Name, _json);
        json = _json.WrittenSpan;
        return !json.SequenceEqual("null"u8);
    }

    internal void ResSet(ComponentParam p, ReadOnlySpan<byte> json)
    {
        if (!p.Mutable)
            throw new InvalidOperationException($"res.set: {p.Path} was declared with add-res, not add-res-mut");
        if (Ctx.App != null && Ctx.Registry.TryGetResource(p.Path, out var r))
            r.SetJsonUtf8From(Ctx.App, json, Ctx.Name);
    }

    // ── IModInstance ──────────────────────────────────────────────────────────

    public void Setup()
    {
        _systems.Clear();
        _observers.Clear();
        foreach (var b in _eventBuffers)
            b.Dead = true;
        _eventBuffers.Clear();

        var prev = Enter();
        var args = stackalloc Cm.ComponentValue[1];
        args[0] = Cm.ComponentValue.CreateOwnResource(Cm.StoreContext.FromStore(_store), Handles.Alloc(HandleKind.App, null), ComponentModBackend.TypeApp);
        try
        {
            using var _ = _instance.Call(_setup, 0, args, 1);
        }
        finally
        {
            args[0].Dispose(_store);
            Exit(prev);
        }
    }

    public void RunSystem(ModSystemSpec spec)
    {
        if (!_systems.TryGetValue(spec, out var sys))
            return;
        var hasQuery = false;
        var anyRows = Evaluate(sys, ref hasQuery);
        try
        {
            if (ModdingPlugin.ShouldSkipIdle(spec, hasQuery, anyRows))
                return;
            Call(_run, sys, entity: null, json: default);
        }
        finally
        {
            Release(sys);
        }
    }

    public void CallObserver(string export, ulong entity, string json)
        => CallObserver(export, entity, string.IsNullOrEmpty(json) ? default : System.Text.Encoding.UTF8.GetBytes(json));

    public void CallObserver(string export, ulong entity, ReadOnlySpan<byte> json)
    {
        if (!_observers.TryGetValue(export, out var sys))
            return;
        var hasQuery = false;
        Evaluate(sys, ref hasQuery);
        try
        {
            Call(_observe, sys, entity, json);
        }
        finally
        {
            Release(sys);
        }
    }

    // observe-packet(system, direction, packet, params) -> verdict.
    public ModPacketVerdict CallPacketObserver(string export, ModPacketDirection dir, ReadOnlySpan<byte> packet, out ReadOnlySpan<byte> replacement)
    {
        replacement = default;
        if (!_observers.TryGetValue(export, out var sys))
            return ModPacketVerdict.Pass;
        var hasQuery = false;
        Evaluate(sys, ref hasQuery);
        var verdict = ModPacketVerdict.Pass;
        var prev = Enter();
        var cx = Cm.StoreContext.FromStore(_store);
        var args = stackalloc Cm.ComponentValue[4];
        args[0] = ComponentModBackend.Utf8String(sys.NameUtf8);
        args[1] = Cm.ComponentValue.CreateEnum(dir, &ComponentModBackend.PacketDirectionName);
        var bytes = new Cm.ListBuilder(packet.Length);
        for (var i = 0; i < packet.Length; i++)
            bytes[i] = Cm.ComponentValue.CreateByte(packet[i]);
        args[2] = new Cm.ComponentValue(bytes, externallyOwned: false);
        args[3] = BuildParams(sys, cx);

        var ok = false;
        try
        {
            using (var results = _instance.Call(_observePacket, 1, args, 4))
                verdict = ReadVerdict(results[0]);
            ok = true;
        }
        finally
        {
            for (var i = 0; i < 4; i++)
                args[i].Dispose(_store);
            Exit(prev);
            if (ok)
                Commands.Apply();
            else
                Commands.Discard();
            Release(sys);
        }
        if (verdict == ModPacketVerdict.Replace)
            replacement = _packetReplacement.AsSpan(0, _packetReplacementLength);
        return verdict;
    }

    // Copied out while the results are alive (the commands applied next may re-enter).
    private ModPacketVerdict ReadVerdict(in Cm.ComponentValue value)
    {
        var (disc, payload) = value.ToVariantRaw();
        var name = disc.Span;
        if (name.SequenceEqual("pass"u8))
            return ModPacketVerdict.Pass;
        if (name.SequenceEqual("block"u8))
            return ModPacketVerdict.Block;
        if (!name.SequenceEqual("replace"u8))
            throw new InvalidOperationException($"observe-packet: unknown verdict '{System.Text.Encoding.UTF8.GetString(name)}'");
        var list = payload!.Value.ToListBuilder();
        if (list.Length == 0)
            return ModPacketVerdict.Pass;
        if (_packetReplacement.Length < list.Length)
            _packetReplacement = new byte[Math.Max(list.Length, _packetReplacement.Length * 2)];
        Cm.ComponentValue.ReadListOfPrimitives(list, _packetReplacement.AsSpan(0, list.Length));
        _packetReplacementLength = list.Length;
        return ModPacketVerdict.Replace;
    }

    // Snapshot every query, swap event buffers. Returns whether anything has rows/events.
    private bool Evaluate(ComponentSystem sys, ref bool hasQuery)
    {
        var any = false;
        var spec = sys.Spec;
        foreach (var p in sys.Params)
        {
            switch (p.Kind)
            {
                case ComponentParamKind.Query:
                {
                    hasQuery = true;
                    var snap = ModdingPlugin.BuildSnapshot(Ctx, p.Query!, spec.LastRunWorldTick, out var matched);
                    if (p.Added != null)
                        matched = FilterAdded(snap, matched, p.Added, spec.LastRunWorldTick);
                    p.Snapshot = snap;
                    p.Count = matched;
                    p.Cursor = 0;
                    any |= matched > 0;
                    break;
                }
                case ComponentParamKind.Events:
                    p.Events!.Swap();
                    any |= p.Events.Current.Count > 0;
                    break;
            }
        }
        // The Changed/Added window closes here, as on the core ABI — even if idle-skipped.
        spec.LastRunWorldTick = TinyEcs.Bevy.SystemTicks.Current;
        return any;
    }

    private int FilterAdded(ulong[] snap, int matched, IModComponent[] added, uint since)
    {
        var world = Ctx.World;
        var kept = 0;
        for (var i = 0; i < matched; i++)
        {
            var id = snap[i];
            var ok = true;
            foreach (var comp in added)
                if (!comp.AddedSince(world, id, since)) { ok = false; break; }
            if (ok)
                snap[kept++] = id;
        }
        return kept;
    }

    private static void Release(ComponentSystem sys)
    {
        foreach (var p in sys.Params)
            if (p.Snapshot != null)
            {
                ArrayPool<ulong>.Shared.Return(p.Snapshot);
                p.Snapshot = null;
                p.Count = p.Cursor = 0;
            }
    }

    // run(system, params) / observe(system, trigger-data, params).
    private void Call(Cm.ComponentInstanceFunction fn, ComponentSystem sys, ulong? entity, scoped ReadOnlySpan<byte> json)
    {
        var prev = Enter();
        var cx = Cm.StoreContext.FromStore(_store);
        var argc = entity.HasValue ? 3 : 2;
        var args = stackalloc Cm.ComponentValue[3];
        args[0] = ComponentModBackend.Utf8String(sys.NameUtf8);
        if (entity.HasValue)
        {
            var rec = new Cm.RecordBuilder(2, disposeNames: false);
            rec.Set(0, Backend.FieldEntity, Cm.ComponentValue.CreateUInt64(entity.Value));
            rec.Set(1, Backend.FieldValue, ComponentModBackend.Utf8String(json.IsEmpty ? "{}"u8 : json));
            args[1] = new Cm.ComponentValue(rec, externallyOwned: false);
        }
        args[argc - 1] = BuildParams(sys, cx);

        var ok = false;
        try
        {
            using (_instance.Call(fn, 0, args, argc)) { }
            ok = true;
        }
        finally
        {
            for (var i = 0; i < argc; i++)
                args[i].Dispose(_store);
            Exit(prev);
            if (ok)
                Commands.Apply();
            else
                Commands.Discard();
        }
    }

    private Cm.ComponentValue BuildParams(ComponentSystem sys, Cm.StoreContext cx)
    {
        var list = new Cm.ListBuilder(sys.Params.Count);
        for (var i = 0; i < sys.Params.Count; i++)
        {
            var p = sys.Params[i];
            var (kind, type, name) = p.Kind switch
            {
                ComponentParamKind.Commands => (HandleKind.Commands, ComponentModBackend.TypeCommands, Backend.CaseCommands),
                ComponentParamKind.Query => (HandleKind.Query, ComponentModBackend.TypeQuery, Backend.CaseQuery),
                ComponentParamKind.Res => (HandleKind.Res, ComponentModBackend.TypeRes, Backend.CaseRes),
                _ => (HandleKind.Events, ComponentModBackend.TypeEvents, Backend.CaseEvents),
            };
            var handle = Cm.ComponentValue.CreateOwnResource(cx, Handles.Alloc(kind, p), type);
            list[i] = Cm.ComponentValue.CreateVariant(name, handle, copyDiscriminant: true);
        }
        return new Cm.ComponentValue(list, externallyOwned: false);
    }

    internal bool TryCallExport(string name, ReadOnlySpan<Cm.ComponentValue> args, int resultCount, ComponentResultsReader? onResults)
    {
        if (!_exports.TryGetValue(name, out var found))
        {
            try
            {
                found = _instance.GetFunction(name);
            }
            catch (Cm.WasmtimeException)
            {
                found = null;
            }
            _exports[name] = found;
        }
        if (found is not { } fn)
        {
            foreach (var a in args)
                a.Dispose(_store);
            return false;
        }
        var prev = Enter();
        try
        {
            using var results = _instance.Call(fn, resultCount, args);
            onResults?.Invoke(results);
        }
        finally
        {
            foreach (var a in args)
                a.Dispose(_store);
            Exit(prev);
        }
        Commands.Apply();
        return true;
    }

    private ComponentModInstance? Enter()
    {
        var prev = Backend.Current;
        Backend.Current = this;
        return prev;
    }

    private void Exit(ComponentModInstance? prev) => Backend.Current = prev;

    public bool TryInvokeBoolExport(string export, byte arg, ReadOnlySpan<byte> data) => false;

    public bool WantsFilter => false;

    public void Reload(in ModSource source)
    {
        TearDown();
        Instantiate(source.Bytes ?? throw new InvalidOperationException($"component mod '{source.Name}' needs its bytes"));
        Setup();
    }

    private void TearDown()
    {
        // Handles the old guest still held die with its store; their destructors may
        // fire during disposal, so keep Current pointed here until it is done.
        var prev = Enter();
        try
        {
            _store.Dispose();
            _component.Dispose();
        }
        finally
        {
            Exit(prev);
        }
        Handles.Clear();
        Commands.Discard();
    }

    public void Dispose()
    {
        TearDown();
        foreach (var b in _eventBuffers)
            b.Dead = true;
        Backend.Forget(Ctx);
    }
}

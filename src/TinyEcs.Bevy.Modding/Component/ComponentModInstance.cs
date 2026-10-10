// One loaded component mod: its store + instance, the systems/observers it declared in
// `setup`, the host-resource handle table backing every `tinyecs:modding/ecs` handle it
// holds, and the command buffer its `commands` params fill during a call.
//
// Semantics mirror the relay ABI runner (ModAbiRunner) term for term:
//  - queries are the same ModQuerySpec + ModdingPlugin.BuildSnapshot, evaluated
//    BEFORE the call with the system's own (lastRun, thisRun] change window, the same
//    idle-skip, despawned rows skipped;
//  - `query.entities` hands the guest the live rows; the host app's typed interface
//    (ComponentModImports) reads their columns and queues write-backs;
//  - commands apply in order AFTER the export returns (spawn hands out the id at once);
//  - observers ride the same host global observers (ModdingPlugin.RegisterModObservers),
//    buffered and flushed at the same points; packet observers are called
//    synchronously by ModPacketChain.
// Every system is called through the mod's own export named like it (wasvy-style),
// its params as typed arguments: <name>(params...), observers <name>(entity, [value,]
// params...), packet observers <name>(direction, packet, params...) -> verdict.
// Two term kinds have no core-ABI twin and are handled here: `added` (a read term
// post-filtered on IModComponent.AddedSince) and the res / events parameters.

extern alias WasmtimeCm;

using System.Buffers;
using System.Runtime.InteropServices;
using Cm = WasmtimeCm::Wasmtime;

namespace TinyEcs.Bevy.Modding;

internal enum HandleKind : byte { Free, System, App, Commands, Query, Res, Events }

internal struct HandleSlot
{
    public HandleKind Kind;
    public object? Obj;
    public int NextFree;
}

/// rep = slot index + 1. Freed by the resource destructor when the guest drops the
/// handle, so per-call handles (params) recycle their slots every call.
internal sealed class HandleTable
{
    private HandleSlot[] _slots = new HandleSlot[32];
    private int _count;
    private int _free = -1;

    public uint Alloc(HandleKind kind, object? obj)
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
        _slots[index] = new HandleSlot { Kind = kind, Obj = obj };
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

    // Each event's value (Encoding.Typed).
    public readonly List<byte[]> Pending = new();
    public readonly List<byte[]> Current = new();
    // Set when the mod reloads: the old host observer can't be unregistered, so it
    // keeps firing into a buffer nobody reads anymore.
    public bool Dead;

    public void Push(ReadOnlySpan<byte> value)
    {
        if (Dead)
            return;
        if (Pending.Count == Cap)
            Pending.RemoveAt(0);
        Pending.Add(value.ToArray());
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
    // The reading terms' components (declaration order), resolved on first typed read;
    // null entries are unregistered paths.
    public IModComponent?[]? Reads;
    public ulong[]? Snapshot;
    public int Count;
    // Res / Events
    public string Path = "";
    public bool Mutable;
    public ComponentEventBuffer? Events;
    // Res: the value across runs, and whether this run already encoded it (Same = that
    // run's "equal to the previous observation").
    public ModResValue? Res;
    public bool Fetched;
    public bool Same;
}

internal sealed class ComponentSystem
{
    public readonly SystemImpl Impl;
    public readonly string Name;
    public readonly List<ComponentParam> Params = new();
    // The mod's export named like the system (bound after `setup`).
    public Cm.ComponentInstanceFunction Export;
    // An observer: its trigger, and whether its export takes a value record after the
    // entity (TriggerValue) or nothing more (a tag, spawn / despawn).
    public ModObserverSpec? Observer;
    public bool TriggerValue;

    public ComponentSystem(string name)
    {
        Impl = new SystemImpl(name);
        Name = name;
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
        => Params.Add(new ComponentParam { Kind = ComponentParamKind.Res, Path = path, Mutable = mutable, Res = new ModResValue() });

    public void AddEvents(ModHostContext ctx, string path)
    {
        var buffer = new ComponentEventBuffer();
        if (ctx.App != null)
            ctx.Registry.BinaryEvent(path).Observe(ctx.App, static () => true, (_, value) => buffer.Push(value));
        Params.Add(new ComponentParam { Kind = ComponentParamKind.Events, Path = path, Events = buffer });
    }
}

/// Commands recorded during one guest call, applied in order once it returns.
internal sealed class ComponentCommandBuffer
{
    private enum Op : byte { Remove, Despawn, Typed, Insert, SetResource, Send }

    private struct Entry
    {
        public Op Op;
        public ulong Entity;
        public int First;
        public int Count;
        // Op.Typed: the slot holding the value (index First).
        public TypedSlot? Slot;
        // Insert / SetResource / Send: index of the path in _paths.
        public int Path;
    }

    // Value writes (a game's typed imports, ComponentModImports): the struct is kept as
    // is, one list per payload type reused across calls, so an insert neither encodes nor
    // allocates in the steady state.
    private abstract class TypedSlot
    {
        public abstract void Apply(int index, ModHostContext ctx, ulong entity);
        public abstract void Clear();
    }

    private sealed class ComponentSlot<T> : TypedSlot where T : struct
    {
        public readonly List<(IModComponentOf<T> Target, T Value)> Items = new();

        public override void Apply(int index, ModHostContext ctx, ulong entity)
        {
            if (ctx.World.Exists(entity))
            {
                var (target, value) = Items[index];
                target.Set(ctx.World, entity, value);
            }
        }

        public override void Clear() => Items.Clear();
    }

    private sealed class ResourceSlot<T> : TypedSlot
    {
        public readonly List<(IModResourceOf<T> Target, T Value)> Items = new();

        public override void Apply(int index, ModHostContext ctx, ulong entity)
        {
            if (ctx.App != null)
            {
                var (target, value) = Items[index];
                target.Set(ctx.App, value, ctx.Name);
            }
        }

        public override void Clear() => Items.Clear();
    }

    private sealed class EventSlot<T> : TypedSlot where T : struct
    {
        public readonly List<(IModEventOf<T> Target, T Value)> Items = new();

        public override void Apply(int index, ModHostContext ctx, ulong entity)
        {
            var (target, value) = Items[index];
            target.Emit(ctx.World, entity, value);
        }

        public override void Clear() => Items.Clear();
    }

    private readonly Dictionary<Type, TypedSlot> _typed = new();

    private readonly ComponentModInstance _owner;
    private readonly List<Entry> _entries = new();
    // Removed paths; for Insert / SetResource / Send the one path of the value.
    private readonly List<string> _paths = new();
    private readonly List<string> _pathScratch = new();
    // Encoding.Typed values of Insert / SetResource / Send (Entry.First = offset, Count =
    // length), applied through the path's binary view.
    private byte[] _values = new byte[256];
    private int _valuesLen;
    private readonly List<ModPayload> _payloadScratch = new();

    public ComponentCommandBuffer(ComponentModInstance owner) => _owner = owner;

    public void Remove(ulong entity, in Cm.ComponentValue paths)
    {
        var list = paths.ToListBuilder();
        var first = _paths.Count;
        for (var i = 0; i < list.Length; i++)
            _paths.Add(_owner.Backend.Path(list[i]));
        _entries.Add(new Entry { Op = Op.Remove, Entity = entity, First = first, Count = list.Length });
    }

    public void Despawn(ulong entity)
        => _entries.Add(new Entry { Op = Op.Despawn, Entity = entity });

    // An entity with no components yet, its id valid at once; typed inserts follow.
    public ulong SpawnEmpty() => new CommandsImpl(_owner.Ctx).SpawnEmpty().Id;

    /// An insert / resource write / send carrying its value as Encoding.Typed bytes.
    public void InsertBytes(ulong entity, string path, ReadOnlySpan<byte> value) => AddValue(Op.Insert, entity, path, value);

    public void SetResourceBytes(string path, ReadOnlySpan<byte> value) => AddValue(Op.SetResource, 0, path, value);

    public void SendBytes(string path, ReadOnlySpan<byte> value) => AddValue(Op.Send, 0, path, value);

    private void AddValue(Op op, ulong entity, string path, ReadOnlySpan<byte> value)
    {
        if (_valuesLen + value.Length > _values.Length)
            Array.Resize(ref _values, Math.Max(_values.Length * 2, _valuesLen + value.Length));
        value.CopyTo(_values.AsSpan(_valuesLen));
        _paths.Add(path);
        _entries.Add(new Entry { Op = op, Entity = entity, First = _valuesLen, Count = value.Length, Slot = null, Path = _paths.Count - 1 });
        _valuesLen += value.Length;
    }

    public void InsertTyped<T>(ulong entity, IModComponentOf<T> target, in T value) where T : struct
    {
        var slot = Slot<ComponentSlot<T>>();
        slot.Items.Add((target, value));
        _entries.Add(new Entry { Op = Op.Typed, Entity = entity, First = slot.Items.Count - 1, Slot = slot });
    }

    public void SetResourceTyped<T>(IModResourceOf<T> target, in T value)
    {
        var slot = Slot<ResourceSlot<T>>();
        slot.Items.Add((target, value));
        _entries.Add(new Entry { Op = Op.Typed, First = slot.Items.Count - 1, Slot = slot });
    }

    public void SendTyped<T>(IModEventOf<T> target, in T value) where T : struct
    {
        var slot = Slot<EventSlot<T>>();
        slot.Items.Add((target, value));
        _entries.Add(new Entry { Op = Op.Typed, First = slot.Items.Count - 1, Slot = slot });
    }

    private TSlot Slot<TSlot>() where TSlot : TypedSlot, new()
    {
        if (!_typed.TryGetValue(typeof(TSlot), out var slot))
            _typed[typeof(TSlot)] = slot = new TSlot();
        return (TSlot)slot;
    }

    public void Discard()
    {
        _entries.Clear();
        _paths.Clear();
        _valuesLen = 0;
        foreach (var slot in _typed.Values)
            slot.Clear();
    }

    // Per-entry guard like ModAbiRunner.ApplyCommandBuffer: one bad command (a mapper
    // that refuses its value) must not drop every command after it.
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
                    case Op.Remove:
                        _pathScratch.Clear();
                        for (var k = 0; k < e.Count; k++)
                            _pathScratch.Add(_paths[e.First + k]);
                        commands.EntityById(e.Entity).Remove(CollectionsMarshal.AsSpan(_pathScratch));
                        break;
                    case Op.Despawn:
                        commands.EntityById(e.Entity).Despawn();
                        break;
                    case Op.Typed:
                        e.Slot!.Apply(e.First, ctx, e.Entity);
                        break;
                    case Op.Insert:
                    {
                        var path = _paths[e.Path];
                        _payloadScratch.Clear();
                        _payloadScratch.Add(new ModPayload(path, _values.AsMemory(e.First, e.Count), ctx.Registry.Binary(path)));
                        commands.EntityById(e.Entity).Insert(CollectionsMarshal.AsSpan(_payloadScratch));
                        break;
                    }
                    case Op.SetResource:
                        commands.ResourceSet(ctx.Registry.BinaryResource(_paths[e.Path]), _values.AsSpan(e.First, e.Count));
                        break;
                    case Op.Send:
                        commands.EmitEvent(ctx.Registry.BinaryEvent(_paths[e.Path]), 0, _values.AsSpan(e.First, e.Count));
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ecs-mod] {0} command #{1} ({2}) failed: {3}", ctx.Name, i, e.Op, ex.Message);
            }
        }
        Discard();
    }
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

    private readonly Dictionary<ModSystemSpec, ComponentSystem> _systems = new();
    private readonly Dictionary<string, ComponentSystem> _observers = new();
    // Host-called exports (TryCallExport), looked up once per instance; null = not exported.
    private readonly Dictionary<string, Cm.ComponentInstanceFunction?> _exports = new();
    private readonly List<ComponentEventBuffer> _eventBuffers = new();
    private readonly ModByteBuffer _res = new();
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
        _component = ComponentModBackend.Compile(bytes);
        _instance = _store.GetComponentInstance(_component, Backend.Linker);
        _exports.Clear();
        _setup = _instance.GetFunction("setup");
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
        var spec = new ModObserverSpec { Name = token, Kind = kind, TypePath = path };
        sys.Observer = spec;
        Ctx.AddObserver(spec);
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

    // The snapshot's rows still alive. A row can only die between the snapshot and here
    // through an earlier observer's commands; those are compacted out, so `entities` and
    // every typed column agree on the order.
    internal ReadOnlySpan<ulong> Live(ComponentParam q)
    {
        var world = Ctx.World;
        var snap = q.Snapshot;
        var live = 0;
        for (var i = 0; i < q.Count; i++)
            if (world.Exists(snap![i]))
                snap[live++] = snap[i];
        q.Count = live;
        return snap.AsSpan(0, live);
    }

    // query.entities: list<entity>, the order of every typed column.
    internal Cm.ComponentValue Entities(ComponentParam q)
    {
        var live = Live(q);
        var list = new Cm.ListBuilder(live.Length);
        for (var i = 0; i < live.Length; i++)
            list[i] = Cm.ComponentValue.CreateUInt64(live[i]);
        return new Cm.ComponentValue(list, externallyOwned: true);
    }

    internal IModComponent?[] ReadsOf(ComponentParam q) => q.Reads ??= ResolveReads(q.Query!);

    // A typed `get` (a game import) hands the guest the current value. When this run
    // already encoded it (run-on-change, or an `unchanged` call), that is the value taken;
    // otherwise encode it now, so `unchanged` next run compares against what was read.
    internal void MarkResGot(ComponentParam p)
    {
        FetchRes(p);
        p.Res!.MarkGot();
    }

    // A typed `put`: a later get / unchanged re-reads the new value.
    internal void ResWritten(ComponentParam p) => p.Fetched = false;

    private IModComponent?[] ResolveReads(ModQuerySpec query)
    {
        var comps = query.Components;
        var reads = new IModComponent?[comps.Count];
        for (var i = 0; i < reads.Length; i++)
            reads[i] = Ctx.Registry.TryGet(comps[i].typePath, out var c) ? c : null;
        return reads;
    }

    // Encodes the param's value once per run (lazily for a system that reads it only
    // through `get` / `unchanged`, up front for a run-on-change one) and records it, so
    // "unchanged since the previous run" is a byte compare.
    private bool FetchRes(ComponentParam p)
    {
        if (p.Fetched)
            return p.Same;
        _res.Reset();
        var present = Ctx.App != null && Ctx.Registry.BinaryResource(p.Path).TryWrite(Ctx.App, Ctx.Name, _res);
        p.Same = p.Res!.Observe(_res.WrittenSpan, present);
        p.Fetched = true;
        return p.Same;
    }

    internal bool ResUnchanged(ComponentParam p)
    {
        FetchRes(p);
        return p.Res!.UnchangedSinceGot;
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
        foreach (var sys in _systems.Values)
            BindExport(sys);
        foreach (var sys in _observers.Values)
        {
            BindExport(sys);
            BindTrigger(sys);
        }
    }

    // An observer export leads with `entity: entity` (u64); a record next is the value.
    private void BindTrigger(ComponentSystem sys)
    {
        if (sys.Observer is not { Kind: not ModObserverKind.Packet } obs)
            return;
        var kinds = _instance.GetParamKinds(sys.Export);
        if (kinds.Length == 0 || kinds[0] != Cm.ComponentTypeKind.U64)
            throw new InvalidOperationException($"observer '{sys.Name}' must take `entity: entity` first");
        sys.TriggerValue = kinds.Length > 1 && kinds[1] != Cm.ComponentTypeKind.Own;
        if (sys.TriggerValue && Backend.TypedTrigger == null)
            throw new InvalidOperationException($"observer '{sys.Name}' takes a trigger value, but the host defines no trigger lowering");
        if (sys.TriggerValue && obs.TypePath == null)
            throw new InvalidOperationException($"observer '{sys.Name}': a value for a trigger without one");
    }

    // Every declared system is an export of the mod's world named like the system.
    private void BindExport(ComponentSystem sys)
    {
        try
        {
            sys.Export = _instance.GetFunction(sys.Name);
        }
        catch (Cm.WasmtimeException)
        {
            throw new InvalidOperationException(
                $"system '{sys.Name}' is declared in setup but the mod exports no '{sys.Name}' function");
        }
    }

    public void RunSystem(ModSystemSpec spec)
    {
        if (!_systems.TryGetValue(spec, out var sys))
            return;
        var hasQuery = false;
        var anyRows = Evaluate(sys, ref hasQuery, out var hasInputs, out var resChanged);
        try
        {
            if (ModdingPlugin.ShouldSkipRun(spec, hasQuery, hasInputs, anyRows, resChanged))
                return;
            Call(sys, entity: null, value: default);
        }
        finally
        {
            Release(sys);
        }
    }

    public void CallObserver(string export, ulong entity, ReadOnlySpan<byte> value)
    {
        if (!_observers.TryGetValue(export, out var sys))
            return;
        var hasQuery = false;
        Evaluate(sys, ref hasQuery, out _, out _);
        try
        {
            Call(sys, entity, value);
        }
        finally
        {
            Release(sys);
        }
    }

    // <export>(direction, packet, params...) -> verdict.
    public ModPacketVerdict CallPacketObserver(string export, ModPacketDirection dir, ReadOnlySpan<byte> packet, out ReadOnlySpan<byte> replacement)
    {
        replacement = default;
        if (!_observers.TryGetValue(export, out var sys))
            return ModPacketVerdict.Pass;
        var hasQuery = false;
        Evaluate(sys, ref hasQuery, out _, out _);
        var verdict = ModPacketVerdict.Pass;
        var prev = Enter();
        var cx = Cm.StoreContext.FromStore(_store);
        var argc = 2 + sys.Params.Count;
        var args = stackalloc Cm.ComponentValue[argc];
        args[0] = Cm.ComponentValue.CreateEnum(dir, &ComponentModBackend.PacketDirectionName);
        var bytes = new Cm.ListBuilder(packet.Length);
        for (var i = 0; i < packet.Length; i++)
            bytes[i] = Cm.ComponentValue.CreateByte(packet[i]);
        args[1] = new Cm.ComponentValue(bytes, externallyOwned: false);
        BuildParams(sys, cx, new Span<Cm.ComponentValue>(args + 2, sys.Params.Count));

        var ok = false;
        try
        {
            using (var results = _instance.Call(sys.Export, 1, args, argc))
                verdict = ReadVerdict(results[0]);
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
            {
                Commands.Discard();
                Forget(sys);
            }
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
            throw new InvalidOperationException($"packet observer: unknown verdict '{System.Text.Encoding.UTF8.GetString(name)}'");
        var list = payload!.Value.ToListBuilder();
        if (list.Length == 0)
            return ModPacketVerdict.Pass;
        if (_packetReplacement.Length < list.Length)
            _packetReplacement = new byte[Math.Max(list.Length, _packetReplacement.Length * 2)];
        Cm.ComponentValue.ReadListOfPrimitives(list, _packetReplacement.AsSpan(0, list.Length));
        _packetReplacementLength = list.Length;
        return ModPacketVerdict.Replace;
    }

    // Snapshot every query, swap event buffers, and — for a run-on-change system —
    // serialize every resource. Returns whether anything has rows/events; hasInputs =
    // some query / res / events param, resChanged = some resource differs from its
    // previous run's (only computed for run-on-change: the others fetch lazily).
    private bool Evaluate(ComponentSystem sys, ref bool hasQuery, out bool hasInputs, out bool resChanged)
    {
        var any = false;
        hasInputs = false;
        resChanged = false;
        var spec = sys.Spec;
        var since = spec.Window.Since;
        foreach (var p in sys.Params)
        {
            switch (p.Kind)
            {
                case ComponentParamKind.Res:
                    hasInputs = true;
                    p.Fetched = false;
                    if (spec.RunOnChange)
                        resChanged |= !FetchRes(p);
                    break;
                case ComponentParamKind.Query:
                {
                    hasQuery = true;
                    hasInputs = true;
                    var snap = ModdingPlugin.BuildSnapshot(Ctx, p.Query!, since, out var matched);
                    if (p.Added != null)
                        matched = FilterAdded(snap, matched, p.Added, since);
                    p.Snapshot = snap;
                    p.Count = matched;
                    any |= matched > 0;
                    break;
                }
                case ComponentParamKind.Events:
                    hasInputs = true;
                    p.Events!.Swap();
                    any |= p.Events.Current.Count > 0;
                    break;
            }
        }
        // The Changed/Added window closes here, as on the relay ABI — even if idle-skipped.
        spec.Window.Close();
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

    // After a failed guest call: it may not have taken this run's values in, so the next
    // run neither skips nor reports a resource unchanged.
    private static void Forget(ComponentSystem sys)
    {
        sys.Spec.HasRun = false;
        foreach (var p in sys.Params)
            p.Res?.Forget();
    }

    private static void Release(ComponentSystem sys)
    {
        foreach (var p in sys.Params)
            if (p.Snapshot != null)
            {
                ArrayPool<ulong>.Shared.Return(p.Snapshot);
                p.Snapshot = null;
                p.Count = 0;
            }
    }

    // A system's export: <export>(params...); an observer's: <export>(entity, [value,] params...).
    private void Call(ComponentSystem sys, ulong? entity, scoped ReadOnlySpan<byte> value)
    {
        var lead = !entity.HasValue ? 0 : sys.TriggerValue ? 2 : 1;
        var argc = lead + sys.Params.Count;
        var args = stackalloc Cm.ComponentValue[argc];
        if (entity.HasValue)
        {
            args[0] = Cm.ComponentValue.CreateUInt64(entity.Value);
            // Lowered before Enter: the hook runs host code only.
            if (sys.TriggerValue)
                args[1] = Backend.TypedTrigger!(sys.Observer!.TypePath!, value);
        }
        var prev = Enter();
        var cx = Cm.StoreContext.FromStore(_store);
        BuildParams(sys, cx, new Span<Cm.ComponentValue>(args + lead, sys.Params.Count));

        var ok = false;
        try
        {
            using (_instance.Call(sys.Export, 0, args, argc)) { }
            ok = true;
        }
        finally
        {
            // A typed trigger value is built like a result (names owned): freed whole.
            var owned = entity.HasValue && sys.TriggerValue ? 1 : -1;
            for (var i = 0; i < argc; i++)
            {
                if (i == owned)
                    args[i].DisposeOwned();
                else
                    args[i].Dispose(_store);
            }
            Exit(prev);
            if (ok)
                Commands.Apply();
            else
            {
                Commands.Discard();
                Forget(sys);
            }
        }
    }

    // Each parameter is its own typed argument: an owned commands / query / res / events.
    private void BuildParams(ComponentSystem sys, Cm.StoreContext cx, Span<Cm.ComponentValue> into)
    {
        for (var i = 0; i < sys.Params.Count; i++)
        {
            var p = sys.Params[i];
            var (kind, type) = p.Kind switch
            {
                ComponentParamKind.Commands => (HandleKind.Commands, ComponentModBackend.TypeCommands),
                ComponentParamKind.Query => (HandleKind.Query, ComponentModBackend.TypeQuery),
                ComponentParamKind.Res => (HandleKind.Res, ComponentModBackend.TypeRes),
                _ => (HandleKind.Events, ComponentModBackend.TypeEvents),
            };
            into[i] = Cm.ComponentValue.CreateOwnResource(cx, Handles.Alloc(kind, p), type);
        }
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

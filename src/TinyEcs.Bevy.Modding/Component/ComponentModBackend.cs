// Component-model mod backend: hosts wasm32-wasip2 COMPONENTS built against
// abi/tinyecs-mod.wit (world `guest`) on the wasmtime-dotnet fork (aliased
// WasmtimeCm — see the csproj). ModdingPlugin routes a mod here when its bytes carry
// the component layer (preamble bytes 6-7 = 01 00); core modules keep going to
// CoreWasmModBackend. Same scheduler, registry, snapshot (ModdingPlugin.BuildSnapshot)
// and command semantics as the core ABI — only the wire differs: instead of a
// FlatBuffers SystemInput/CommandBuffer pair per call, the guest pulls rows and pushes
// commands through the `tinyecs:modding/ecs` host resources, defined here.
//
// One engine + one linker per backend. The ecs imports are defined once and find the
// mod they serve through `Current` (the instance whose export is on the stack; every
// guest call goes through ComponentModInstance.Enter/Exit). A host app adds its own
// interfaces to the same linker via ModdingConfig.ComponentImports (ComponentModImports).

extern alias WasmtimeCm;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Cm = WasmtimeCm::Wasmtime;

namespace TinyEcs.Bevy.Modding;

internal sealed unsafe class ComponentModBackend : IModBackend
{
    /// The generic interface every component mod imports.
    public const string EcsInterface = "tinyecs:modding/ecs@0.1.0";

    // Host resource type ids (wasmtime keys host resource types by a u32 the embedder
    // picks). Kept in a high range so a host app's own resources (a generator numbers
    // from 0) can't collide — see ComponentModImports.ReservedTypeIdEnd.
    public const uint TypeIdBase = 0x7E5C_0000;
    public const uint TypeSystem = TypeIdBase + 1;
    public const uint TypeApp = TypeIdBase + 2;
    public const uint TypeCommands = TypeIdBase + 3;
    public const uint TypeQuery = TypeIdBase + 4;
    public const uint TypeRes = TypeIdBase + 6;
    public const uint TypeEvents = TypeIdBase + 7;

    // One engine per process, and every compiled component cached by content: compiling
    // is the dominant load cost (seconds for a large component), and the same bytes are
    // loaded again by every App a process builds (tests) and by a reload that didn't
    // change the file. A changed file compiles once more; the old entry stays (a reload
    // is a dev action, the cost is bounded by how often you rebuild).
    private static readonly Cm.Engine Engine = new();
    private static readonly ConcurrentDictionary<string, Lazy<Cm.Component>> Compiled = new();

    internal readonly Cm.Linker Linker;
    private readonly Dictionary<ModHostContext, ComponentModInstance> _byCtx = new();

    // Interned type paths, looked up straight from the guest's UTF8 (no string per call).
    private readonly Dictionary<string, string> _paths = new();
    private readonly Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> _pathLookup;

    // Variant case names / record field names, encoded once. Variant discriminants are
    // COPIED into each value (the value frees its copy); record names are never freed by
    // a value, so these must outlive every call — they live as long as the backend.
    internal readonly Cm.ByteVector FieldEntity = Cm.ByteVector.Constant("entity");
    internal readonly Cm.ByteVector FieldValue = Cm.ByteVector.Constant("value");

    /// The instance whose guest export is executing (null between calls).
    internal ComponentModInstance? Current;

    // ComponentModImports.TypedTrigger.
    internal ComponentTriggerLowering? TypedTrigger;

    public ComponentModBackend(IReadOnlyList<Action<ComponentModImports>> hostImports)
    {
        _pathLookup = _paths.GetAlternateLookup<ReadOnlySpan<char>>();
        Linker = new Cm.Linker(Engine);
        Linker.AddWasiP2();
        DefineEcs();

        if (hostImports.Count > 0)
        {
            var scope = new ComponentModImports(this);
            foreach (var define in hostImports)
                define(scope);
        }
    }

    public IModInstance Load(in ModSource source, ModHostContext ctx)
    {
        if (source.Bytes == null)
            throw new InvalidOperationException($"component mod '{source.Name}' needs its bytes");
        var inst = new ComponentModInstance(this, Engine, source.Bytes, ctx);
        _byCtx[ctx] = inst;
        return inst;
    }

    internal void Forget(ModHostContext ctx) => _byCtx.Remove(ctx);

    internal static Cm.Component Compile(byte[] bytes)
        => Compiled.GetOrAdd(Convert.ToHexString(SHA256.HashData(bytes)),
            _ => new Lazy<Cm.Component>(() => Cm.Component.Compile(Engine, bytes))).Value;

    internal bool TryCallExport(ModHostContext ctx, string name, ReadOnlySpan<Cm.ComponentValue> args,
        int resultCount, ComponentResultsReader? onResults)
    {
        if (_byCtx.TryGetValue(ctx, out var inst))
            return inst.TryCallExport(name, args, resultCount, onResults);
        foreach (var a in args)
            a.Dispose();
        return false;
    }

    public void Dispose()
    {
        Linker.Dispose();
    }

    // ── type paths ────────────────────────────────────────────────────────────

    internal string Path(in Cm.ComponentValue value) => Intern(value.ToUtf8Span());

    internal string Intern(ReadOnlySpan<byte> utf8)
    {
        Span<char> chars = utf8.Length <= 256 ? stackalloc char[utf8.Length] : new char[utf8.Length];
        var n = Encoding.UTF8.GetChars(utf8, chars);
        if (_pathLookup.TryGetValue(chars[..n], out var interned))
            return interned;
        var s = new string(chars[..n]);
        _paths[s] = s;
        return s;
    }

    // ── values ────────────────────────────────────────────────────────────────

    /// A WIT `string` value straight from UTF8 (one native copy, no managed string).
    /// The fork only builds strings from System.String, so this writes the native
    /// wasmtime_component_val { u8 kind; union of @ 8 } layout itself: kind 12, a
    /// wasm_byte_vec_t {size, data} — exactly what ByteVector wraps — in the union.
    internal static Cm.ComponentValue Utf8String(ReadOnlySpan<byte> utf8)
    {
        Cm.ComponentValue value = default;
        var p = (byte*)Unsafe.AsPointer(ref value);
        p[0] = 12;
        *(Cm.ByteVector*)(p + 8) = new Cm.ByteVector(utf8);
        return value;
    }

    // ── tinyecs:modding/ecs host side ─────────────────────────────────────────────

    private void DefineEcs()
    {
        using var ecs = Linker.DefineInstance(EcsInterface);

        // The destructor runs when the guest drops its handle (inside a call), so the
        // slot belongs to Current. Null only while a store is being torn down.
        Action<uint> drop = rep => Current?.Handles.Free(rep);
        ecs.DefineResource("system", TypeSystem, drop);
        ecs.DefineResource("app", TypeApp, drop);
        ecs.DefineResource("commands", TypeCommands, drop);
        ecs.DefineResource("query", TypeQuery, drop);
        ecs.DefineResource("res", TypeRes, drop);
        ecs.DefineResource("events", TypeEvents, drop);

        ecs.DefineFunction("[constructor]system", SystemNew, this);
        ecs.DefineFunction("[method]system.add-commands", SystemAddCommands, this);
        ecs.DefineFunction("[method]system.add-query", SystemAddQuery, this);
        ecs.DefineFunction("[method]system.add-res", SystemAddRes, this);
        ecs.DefineFunction("[method]system.add-res-mut", SystemAddResMut, this);
        ecs.DefineFunction("[method]system.add-events", SystemAddEvents, this);
        ecs.DefineFunction("[method]system.after", SystemAfter, this);
        ecs.DefineFunction("[method]system.before", SystemBefore, this);
        ecs.DefineFunction("[method]system.run-on-change", SystemRunOnChange, this);

        ecs.DefineFunction("[method]app.add-systems", AppAddSystems, this);
        ecs.DefineFunction("[method]app.add-observer", AppAddObserver, this);

        ecs.DefineFunction("[method]commands.spawn", CommandsSpawn, this);
        ecs.DefineFunction("[method]commands.insert", CommandsInsert, this);
        ecs.DefineFunction("[method]commands.remove", CommandsRemove, this);
        ecs.DefineFunction("[method]commands.despawn", CommandsDespawn, this);
        ecs.DefineFunction("[method]commands.send", CommandsSend, this);
        ecs.DefineFunction("[method]commands.set-resource", CommandsSetResource, this);

        ecs.DefineFunction("[method]query.rows", QueryRows, this);
        ecs.DefineFunction("[method]query.entities", QueryEntities, this);
        ecs.DefineFunction("[method]query.set", QuerySet, this);

        ecs.DefineFunction("[method]res.get", ResGet, this);
        ecs.DefineFunction("[method]res.set", ResSet, this);
        ecs.DefineFunction("[method]res.unchanged", ResUnchanged, this);

        ecs.DefineFunction("[method]events.read", EventsRead, this);
    }

    private static ComponentModInstance Mod(object? state)
        => ((ComponentModBackend)state!).Current
           ?? throw new InvalidOperationException("tinyecs:modding/ecs called outside a guest call");

    // Each handler has the fork's ComponentFunctionDelegate shape; `state` is this backend.

    private static void SystemNew(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        var rep = m.NewSystem(args[0].ToStringValue());
        results[0] = Cm.ComponentValue.CreateOwnResource(cx, rep, TypeSystem);
    }

    private static void SystemAddCommands(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        m.SystemOf(args[0].ToResourceRep(cx)).AddCommands();
    }

    private static void SystemAddQuery(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        var sys = m.SystemOf(args[0].ToResourceRep(cx));
        var list = args[1].ToListBuilder();
        var terms = new (string Case, string Path)[list.Length];
        for (var i = 0; i < terms.Length; i++)
        {
            var (kind, payload) = list[i].ToVariant();
            terms[i] = (kind, payload is { } p ? m.Backend.Path(p) : "");
        }
        sys.AddQuery(m.Ctx, terms);
    }

    private static void SystemAddRes(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        m.SystemOf(args[0].ToResourceRep(cx)).AddRes(m.Backend.Path(args[1]), mutable: false);
    }

    private static void SystemAddResMut(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        m.SystemOf(args[0].ToResourceRep(cx)).AddRes(m.Backend.Path(args[1]), mutable: true);
    }

    private static void SystemAddEvents(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        m.SystemOf(args[0].ToResourceRep(cx)).AddEvents(m.Ctx, m.Backend.Path(args[1]));
    }

    private static void SystemAfter(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        m.SystemOf(args[0].ToResourceRep(cx)).Impl.After(m.SystemOf(args[1].ToResourceRep(cx)).Impl);
    }

    private static void SystemBefore(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        m.SystemOf(args[0].ToResourceRep(cx)).Impl.Before(m.SystemOf(args[1].ToResourceRep(cx)).Impl);
    }

    private static void SystemRunOnChange(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        m.SystemOf(args[0].ToResourceRep(cx)).Spec.RunOnChange = true;
    }

    private static void AppAddSystems(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        m.Handles.Get(args[0].ToResourceRep(cx), HandleKind.App);
        var schedule = args[1].ToEnum<ModSchedule>(&ScheduleOf);
        var list = args[2].ToListBuilder();
        for (var i = 0; i < list.Length; i++)
            m.AddSystem(schedule, m.SystemOf(list[i].ToResourceRep(cx)));
    }

    private static void AppAddObserver(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        m.Handles.Get(args[0].ToResourceRep(cx), HandleKind.App);
        var (kind, payload) = args[1].ToVariant();
        if (kind == "on-packet")
        {
            // packet-filter { direction, ids }
            var filter = payload!.Value.ToRecordBuilder();
            var dir = filter.Get(0).ToEnum<ModPacketDirection>(&PacketDirectionOf);
            var idList = filter.Get(1).ToListBuilder();
            Span<byte> ids = idList.Length <= 256 ? stackalloc byte[idList.Length] : new byte[idList.Length];
            Cm.ComponentValue.ReadListOfPrimitives(idList, ids);
            m.AddPacketObserver(dir, ids, m.SystemOf(args[2].ToResourceRep(cx)));
            return;
        }
        var path = payload is { } p ? m.Backend.Path(p) : "";
        var obsKind = kind switch
        {
            "on-add" => ModObserverKind.Insert,
            "on-remove" => ModObserverKind.Remove,
            "on-event" => ModObserverKind.Custom,
            _ => throw new InvalidOperationException($"unknown trigger '{kind}'"),
        };
        m.AddObserver(obsKind, path, m.SystemOf(args[2].ToResourceRep(cx)));
    }

    private static void CommandsSpawn(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        m.Handles.Get(args[0].ToResourceRep(cx), HandleKind.Commands);
        results[0] = Cm.ComponentValue.CreateUInt64(m.Commands.Spawn(args[1]));
    }

    private static void CommandsInsert(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        m.Handles.Get(args[0].ToResourceRep(cx), HandleKind.Commands);
        m.Commands.Insert(args[1].ToUInt64(), args[2]);
    }

    private static void CommandsRemove(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        m.Handles.Get(args[0].ToResourceRep(cx), HandleKind.Commands);
        m.Commands.Remove(args[1].ToUInt64(), args[2]);
    }

    private static void CommandsDespawn(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        m.Handles.Get(args[0].ToResourceRep(cx), HandleKind.Commands);
        m.Commands.Despawn(args[1].ToUInt64());
    }

    private static void CommandsSend(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        m.Handles.Get(args[0].ToResourceRep(cx), HandleKind.Commands);
        m.Commands.Send(m.Backend.Path(args[1]), args[2].ToUtf8Span());
    }

    private static void CommandsSetResource(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        m.Handles.Get(args[0].ToResourceRep(cx), HandleKind.Commands);
        m.Commands.SetResource(m.Backend.Path(args[1]), args[2].ToUtf8Span());
    }

    private static void QueryRows(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        var param = (ComponentParam)m.Handles.Get(args[0].ToResourceRep(cx), HandleKind.Query).Obj!;
        results[0] = m.Rows(param);
    }

    private static void QueryEntities(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        var param = (ComponentParam)m.Handles.Get(args[0].ToResourceRep(cx), HandleKind.Query).Obj!;
        results[0] = m.Entities(param);
    }

    private static void QuerySet(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        var param = (ComponentParam)m.Handles.Get(args[0].ToResourceRep(cx), HandleKind.Query).Obj!;
        m.QuerySet(param, args[1].ToUInt64(), args[2].ToByte(), args[3].ToUtf8Span());
    }

    private static void ResGet(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        var param = (ComponentParam)m.Handles.Get(args[0].ToResourceRep(cx), HandleKind.Res).Obj!;
        results[0] = m.ResGet(param, out var json)
            ? Cm.ComponentValue.CreateOption(Utf8String(json))
            : Cm.ComponentValue.CreateOption(null);
    }

    private static void ResSet(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        var param = (ComponentParam)m.Handles.Get(args[0].ToResourceRep(cx), HandleKind.Res).Obj!;
        m.ResSet(param, args[1].ToUtf8Span());
    }

    private static void ResUnchanged(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        var param = (ComponentParam)m.Handles.Get(args[0].ToResourceRep(cx), HandleKind.Res).Obj!;
        results[0] = Cm.ComponentValue.CreateBoolean(m.ResUnchanged(param));
    }

    private static void EventsRead(object? state, Cm.ComponentCallResults args, Cm.ComponentValue* results, Cm.StoreContext cx)
    {
        var m = Mod(state);
        var param = (ComponentParam)m.Handles.Get(args[0].ToResourceRep(cx), HandleKind.Events).Obj!;
        var current = param.Events!.Current;
        var list = new Cm.ListBuilder(current.Count);
        for (var i = 0; i < current.Count; i++)
            list[i] = Cm.ComponentValue.CreateString(current[i], externallyOwned: true);
        results[0] = new Cm.ComponentValue(list, externallyOwned: true);
    }

    // packet-direction enum names, as WIT spells them. Constants: the fork never frees
    // an enum value's name.
    private static readonly Cm.ByteVector DirIncoming = Cm.ByteVector.Constant("incoming");
    private static readonly Cm.ByteVector DirOutgoing = Cm.ByteVector.Constant("outgoing");

    internal static Cm.ByteVector PacketDirectionName(ModPacketDirection dir)
        => dir == ModPacketDirection.Incoming ? DirIncoming : DirOutgoing;

    private static ModPacketDirection PacketDirectionOf(Cm.ByteVector name) => name.GetString() switch
    {
        "incoming" => ModPacketDirection.Incoming,
        "outgoing" => ModPacketDirection.Outgoing,
        var other => throw new InvalidOperationException($"unknown packet direction '{other}'"),
    };

    private static ModSchedule ScheduleOf(Cm.ByteVector name) => name.GetString() switch
    {
        "startup" => ModSchedule.ModStartup,
        "first" => ModSchedule.First,
        "pre-update" => ModSchedule.PreUpdate,
        "update" => ModSchedule.Update,
        "post-update" => ModSchedule.PostUpdate,
        "last" => ModSchedule.Last,
        var other => throw new InvalidOperationException($"unknown schedule '{other}'"),
    };
}

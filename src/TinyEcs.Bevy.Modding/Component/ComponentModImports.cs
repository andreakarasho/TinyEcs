// The seam a HOST APP uses to give component-model mods its own imports. The lib
// defines only the generic `tinyecs:modding/ecs` interface (ComponentModBackend); a game's
// world (its WIT `include`s tinyecs:modding/guest and adds `import <game>:modding/...`) is
// satisfied by the host defining those interfaces on the SAME component linker, from a
// ModdingConfig.ComponentImports callback. Either way works:
//   - dynamic:  var i = imports.DefineInstance("game:mod/host@0.1.0");
//               i.DefineFunction("log", (state, args, results, cx) => ..., state);
//   - generated: the fork's WIT source generator emits an `IComponentImports` whose
//               Register(Linker) does the same — call it with imports.Linker.
// Handlers run synchronously inside a guest call; `Current` names the mod making it,
// so one linker serves every loaded mod.
//
// Typed game imports: the generic interface carries no values, so a game interface takes
// the call's own `commands` / `query` / `res` / `events` handles (`borrow<commands>` etc.
// of tinyecs:modding/ecs) and reads / writes values on them as structs — Commands /
// Query / Res / Events below resolve a borrowed handle's rep to the current call's
// parameter, read a query's snapshot (entities + reading terms, IModComponentOf<T>
// columns) and queue typed inserts / resource sets in order with the call's other
// commands.

extern alias WasmtimeCm;

using Cm = WasmtimeCm::Wasmtime;

namespace TinyEcs.Bevy.Modding;

/// Handed to every ModdingConfig.ComponentImports callback once, when the component
/// backend is created (before the first component mod is instantiated).
public sealed class ComponentModImports
{
    private readonly ComponentModBackend _backend;

    internal ComponentModImports(ComponentModBackend backend) => _backend = backend;

    /// The component linker every component mod is instantiated with (WASI p2 and the
    /// `tinyecs:modding/ecs` interface are already defined on it).
    public Cm.Linker Linker => _backend.Linker;

    /// The mod whose guest call is on the stack; null outside a guest call.
    public ModHostContext? Current => _backend.Current?.Ctx;

    /// Defines (or opens) an interface instance, e.g. "game:mod/assets@0.1.0".
    public Cm.LinkerInstance DefineInstance(string interfaceName) => _backend.Linker.DefineInstance(interfaceName);

    /// First resource type id this lib does NOT use. A host defining its own resources
    /// must keep its ids out of [ComponentModBackend.TypeIdBase, ReservedTypeIdEnd).
    public const uint ReservedTypeIdEnd = ComponentModBackend.TypeIdBase + 0x100;

    /// Calls a game-specific export (one the generic world doesn't know) on the
    /// component mod behind `ctx`. Returns false when that mod isn't a
    /// component or doesn't export `name`. `args` are disposed either way;
    /// `onResults` (optional) reads the results before they are released.
    public bool TryCallExport(ModHostContext ctx, string name, ReadOnlySpan<Cm.ComponentValue> args,
        int resultCount = 0, ComponentResultsReader? onResults = null)
        => _backend.TryCallExport(ctx, name, args, resultCount, onResults);

    /// Observer triggers: an observer export takes its trigger as `(entity: entity, value:
    /// <record>, params...)`, or `(entity: entity, params...)` for a trigger that carries
    /// no value; the backend tells which by the export's second parameter when it binds
    /// it. For a valued one it hands the trigger's value (Encoding.Typed) to this hook,
    /// which lowers it to the record the export takes. Unset = a valued observer fails
    /// the mod's load.
    public ComponentTriggerLowering? TypedTrigger
    {
        get => _backend.TypedTrigger;
        set => _backend.TypedTrigger = value;
    }

    /// The current call's `commands` parameter behind a (borrowed) handle rep.
    public ComponentModCommands Commands(uint rep)
    {
        var m = Mod();
        m.Handles.Get(rep, HandleKind.Commands);
        return new ComponentModCommands(m);
    }

    /// The current call's `query` parameter behind a (borrowed) handle rep.
    public ComponentModQuery Query(uint rep)
    {
        var m = Mod();
        return new ComponentModQuery(m, (ComponentParam)m.Handles.Get(rep, HandleKind.Query).Obj!);
    }

    /// The current call's `res` parameter behind a (borrowed) handle rep.
    public ComponentModRes Res(uint rep)
    {
        var m = Mod();
        return new ComponentModRes(m, (ComponentParam)m.Handles.Get(rep, HandleKind.Res).Obj!);
    }

    /// The current call's `events` parameter behind a (borrowed) handle rep.
    public ComponentModEvents Events(uint rep)
    {
        var m = Mod();
        return new ComponentModEvents((ComponentParam)m.Handles.Get(rep, HandleKind.Events).Obj!);
    }

    private ComponentModInstance Mod()
        => _backend.Current ?? throw new InvalidOperationException("a typed import was called outside a guest call");
}

/// A guest call's `commands` parameter, for typed game imports. Every write is deferred
/// like the generic ones: applied after the export returns, in call order.
public readonly struct ComponentModCommands
{
    private readonly ComponentModInstance _mod;

    internal ComponentModCommands(ComponentModInstance mod) => _mod = mod;

    /// Spawns an entity with no components; its id is valid at once.
    public ulong Spawn() => _mod.Commands.SpawnEmpty();

    /// Queues an insert of the component registered as `path` (its mapper must implement
    /// IModComponentOf&lt;T&gt;). Traps on a read-only one.
    public void Insert<T>(ulong entity, string path, in T value) where T : struct
        => _mod.Commands.InsertTyped(entity, Writable<T>(_mod.Ctx, path, "insert"), value);

    /// Insert with the mapper already resolved by Writable (a caller inserting one path
    /// many times resolves it once).
    public void Insert<T>(ulong entity, IModComponentOf<T> component, in T value) where T : struct
        => _mod.Commands.InsertTyped(entity, component, value);

    /// Queues a write of the resource registered as `path` (IModResourceOf&lt;T&gt;); traps
    /// on a read-only one, like `commands.set-resource`.
    public void SetResource<T>(string path, in T value)
    {
        if (!_mod.Ctx.Registry.TryGetResource(path, out var r))
            throw new InvalidOperationException($"set-resource: '{path}' is not a registered resource");
        if (r.ReadOnly)
            throw new InvalidOperationException($"set-resource: '{path}' is read-only");
        if (r is not IModResourceOf<T> typed)
            throw new InvalidOperationException($"set-resource: '{path}' has no typed {typeof(T).Name} mapper");
        _mod.Commands.SetResourceTyped(typed, value);
    }

    /// Queues a send of the event registered as `path` (its mapper must implement
    /// IModEventOf&lt;T&gt;), like `commands.send`.
    public void Send<T>(string path, in T value) where T : struct
    {
        if (!_mod.Ctx.Registry.TryGetEvent(path, out var ev))
            throw new InvalidOperationException($"send: '{path}' is not a registered event");
        if (ev is not IModEventOf<T> typed)
            throw new InvalidOperationException($"send: '{path}' has no typed {typeof(T).Name} mapper");
        _mod.Commands.SendTyped(typed, value);
    }

    /// Queues an insert of the component registered as `path` from its value as
    /// Encoding.Typed bytes (the path's binary codec decodes it at apply). Traps on a
    /// read-only or unregistered one.
    public void InsertBytes(ulong entity, string path, ReadOnlySpan<byte> value)
    {
        if (_mod.Ctx.Registry.Binary(path).Component.ReadOnly)
            throw new InvalidOperationException($"insert: '{path}' is read-only");
        _mod.Commands.InsertBytes(entity, path, value);
    }

    /// SetResource from Encoding.Typed bytes.
    public void SetResourceBytes(string path, ReadOnlySpan<byte> value)
    {
        if (_mod.Ctx.Registry.BinaryResource(path).ReadOnly)
            throw new InvalidOperationException($"set-resource: '{path}' is read-only");
        _mod.Commands.SetResourceBytes(path, value);
    }

    /// Send from Encoding.Typed bytes.
    public void SendBytes(string path, ReadOnlySpan<byte> value)
    {
        _mod.Ctx.Registry.BinaryEvent(path);
        _mod.Commands.SendBytes(path, value);
    }

    /// The writable typed mapper of the component registered as `path`; traps on a
    /// read-only or untyped one. `what` names the operation in the trap.
    public static IModComponentOf<T> Writable<T>(ModHostContext ctx, string path, string what) where T : struct
    {
        if (!ctx.Registry.TryGet(path, out var comp))
            throw new InvalidOperationException($"{what}: '{path}' is not a registered component");
        if (comp.ReadOnly)
            throw new InvalidOperationException($"{what}: '{path}' is read-only");
        return comp as IModComponentOf<T>
            ?? throw new InvalidOperationException($"{what}: '{path}' has no typed {typeof(T).Name} mapper");
    }
}

/// A guest call's `query` parameter, for typed game imports: the snapshot, as entities +
/// typed columns.
public readonly struct ComponentModQuery
{
    private readonly ComponentModInstance _mod;
    private readonly ComponentParam _param;

    internal ComponentModQuery(ComponentModInstance mod, ComponentParam param)
    {
        _mod = mod;
        _param = param;
    }

    public World World => _mod.Ctx.World;

    /// The live matched entities, in `entities` order.
    public ReadOnlySpan<ulong> Entities => _mod.Live(_param);

    /// The mapper of reading term `term` (ref / mut / changed / added, in declaration
    /// order) — Get it per entity of Entities. Traps unless that term reads `path`.
    public IModComponentOf<T> Column<T>(int term, string path) where T : struct
    {
        Term(term, path, "column");
        return _mod.ReadsOf(_param)[term] as IModComponentOf<T>
            ?? throw new InvalidOperationException($"column: '{path}' has no typed {typeof(T).Name} mapper");
    }

    /// Queues a write-back of reading term `term` (deferred, in order with the call's
    /// commands). Traps unless that term is a `mut` term reading `path`.
    public void Set<T>(ulong entity, int term, string path, in T value) where T : struct
    {
        if (!Term(term, path, "set"))
            throw new InvalidOperationException($"set({term}): {path} is not a `mut` term");
        _mod.Commands.InsertTyped(entity, ComponentModCommands.Writable<T>(_mod.Ctx, path, "set"), value);
    }

    /// Set with the mapper already resolved by ComponentModCommands.Writable.
    public void Set<T>(ulong entity, int term, string path, IModComponentOf<T> component, in T value) where T : struct
    {
        if (!Term(term, path, "set"))
            throw new InvalidOperationException($"set({term}): {path} is not a `mut` term");
        _mod.Commands.InsertTyped(entity, component, value);
    }

    /// Reading term `term`'s value of each live entity, as Encoding.Typed bytes appended to
    /// `into` one after another; `ends[i]` = the end offset of entity i's value. Traps
    /// unless that term reads `path`.
    public void ColumnBytes(int term, string path, System.Buffers.IBufferWriter<byte> into, Span<int> ends)
    {
        Term(term, path, "column");
        var codec = _mod.Ctx.Registry.Binary(path);
        var world = World;
        var ids = Entities;
        if (ends.Length < ids.Length)
            throw new ArgumentException("ends is shorter than Entities", nameof(ends));
        var counting = new CountingWriter(into);
        for (var i = 0; i < ids.Length; i++)
        {
            codec.TryWrite(world, ids[i], counting);
            ends[i] = counting.Written;
        }
    }

    /// A write-back of reading term `term` from Encoding.Typed bytes (see Set).
    public void SetBytes(ulong entity, int term, string path, ReadOnlySpan<byte> value)
    {
        if (!Term(term, path, "set"))
            throw new InvalidOperationException($"set({term}): {path} is not a `mut` term");
        _mod.Commands.InsertBytes(entity, path, value);
    }

    private sealed class CountingWriter(System.Buffers.IBufferWriter<byte> inner) : System.Buffers.IBufferWriter<byte>
    {
        public int Written;
        public void Advance(int count) { inner.Advance(count); Written += count; }
        public Memory<byte> GetMemory(int sizeHint = 0) => inner.GetMemory(sizeHint);
        public Span<byte> GetSpan(int sizeHint = 0) => inner.GetSpan(sizeHint);
    }

    // Validates term `term` reads `path`; returns whether it is `mut`.
    private bool Term(int term, string path, string what)
    {
        var comps = _param.Query!.Components;
        if ((uint)term >= (uint)comps.Count)
            throw new InvalidOperationException($"{what}({term}): the query reads {comps.Count} components");
        var (read, mutable) = comps[term];
        if (read != path)
            throw new InvalidOperationException($"{what}({term}): that term reads {read}, not {path}");
        return mutable;
    }
}

/// A guest call's `res` parameter, for typed game imports.
public readonly struct ComponentModRes
{
    private readonly ComponentModInstance _mod;
    private readonly ComponentParam _param;

    internal ComponentModRes(ComponentModInstance mod, ComponentParam param)
    {
        _mod = mod;
        _param = param;
    }

    public string Path => _param.Path;

    /// The resource's current value; false when the host has none. Traps unless the
    /// parameter names `path`.
    public bool TryGet<T>(string path, out T value)
    {
        if (_param.Path != path)
            throw new InvalidOperationException($"get: the parameter is {_param.Path}, not {path}");
        var ctx = _mod.Ctx;
        if (!ctx.Registry.TryGetResource(path, out var r))
            throw new InvalidOperationException($"get: '{path}' is not a registered resource");
        if (r is not IModResourceOf<T> typed)
            throw new InvalidOperationException($"get: '{path}' has no typed {typeof(T).Name} mapper");
        _mod.MarkResGot(_param);
        if (ctx.App != null && typed.TryGet(ctx.App, ctx.Name, out value))
            return true;
        value = default!;
        return false;
    }

    /// The resource's current value as Encoding.Typed bytes written to `into`; false (nothing
    /// written) when the host has none. Traps unless the parameter names `path`.
    public bool TryGetBytes(string path, System.Buffers.IBufferWriter<byte> into)
    {
        if (_param.Path != path)
            throw new InvalidOperationException($"get: the parameter is {_param.Path}, not {path}");
        var ctx = _mod.Ctx;
        var codec = ctx.Registry.BinaryResource(path);
        _mod.MarkResGot(_param);
        return ctx.App != null && codec.TryWrite(ctx.App, ctx.Name, into);
    }

    /// Put from Encoding.Typed bytes.
    public void PutBytes(string path, ReadOnlySpan<byte> value)
    {
        if (_param.Path != path)
            throw new InvalidOperationException($"put: the parameter is {_param.Path}, not {path}");
        if (!_param.Mutable)
            throw new InvalidOperationException($"put: {path} was declared with add-res, not add-res-mut");
        var ctx = _mod.Ctx;
        var codec = ctx.Registry.BinaryResource(path);
        if (codec.ReadOnly)
            throw new InvalidOperationException($"put: '{path}' is read-only");
        if (ctx.App != null)
            codec.Set(ctx.App, value, ctx.Name);
        _mod.ResWritten(_param);
    }

    /// Writes the resource now (an `add-res-mut` parameter; a `TryGet` after it reads the
    /// new value). Traps unless the parameter names `path` and is writable.
    public void Put<T>(string path, in T value)
    {
        if (_param.Path != path)
            throw new InvalidOperationException($"put: the parameter is {_param.Path}, not {path}");
        if (!_param.Mutable)
            throw new InvalidOperationException($"put: {path} was declared with add-res, not add-res-mut");
        var ctx = _mod.Ctx;
        if (!ctx.Registry.TryGetResource(path, out var r))
            throw new InvalidOperationException($"put: '{path}' is not a registered resource");
        if (r.ReadOnly)
            throw new InvalidOperationException($"put: '{path}' is read-only");
        if (r is not IModResourceOf<T> typed)
            throw new InvalidOperationException($"put: '{path}' has no typed {typeof(T).Name} mapper");
        if (ctx.App != null)
            typed.Set(ctx.App, value, ctx.Name);
        _mod.ResWritten(_param);
    }
}

/// Lowers an observer's trigger value (ComponentModImports.TypedTrigger): the payload
/// registered as `path`, Encoding.Typed, to the record the export takes.
public delegate Cm.ComponentValue ComponentTriggerLowering(string path, ReadOnlySpan<byte> value);

/// Reads the results of ComponentModImports.TryCallExport (valid only inside the call).
public delegate void ComponentResultsReader(Cm.ComponentCallResults results);

/// A guest call's `events` parameter, for typed game imports: the events of its type sent
/// since the system's last run, each as its value (Encoding.Typed).
public readonly struct ComponentModEvents
{
    private readonly ComponentParam _param;

    internal ComponentModEvents(ComponentParam param) => _param = param;

    public string Path => _param.Path;

    /// The events, in send order. Traps unless the parameter names `path`.
    public IReadOnlyList<byte[]> Read(string path)
    {
        if (_param.Path != path)
            throw new InvalidOperationException($"read: the parameter is {_param.Path}, not {path}");
        return _param.Events!.Current;
    }
}

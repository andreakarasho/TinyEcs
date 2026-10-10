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
// Typed game imports: a game interface can take the call's own `commands` / `query` /
// `res` handles (`borrow<commands>` etc. of tinyecs:modding/ecs) and work on them with
// structs instead of JSON — Commands / Query / Res below resolve a borrowed handle's rep
// to the current call's parameter, read a query's snapshot (entities + reading terms,
// IModComponentOf<T> columns) and queue typed inserts / resource sets in order with the
// call's other commands.

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

    /// Typed observer triggers: an observer export may take its trigger value typed —
    /// `(entity: entity, value: <record>, params...)`, or `(entity: entity, params...)` for
    /// a trigger that carries no value — instead of `(trigger: trigger-data, ...)`; the
    /// backend tells which by the export's first parameter when it binds it. For a typed
    /// export it hands the trigger's JSON (`json` = "{}" for a payload-less one) to this
    /// hook, which lowers it to the record the export takes. Unset = no typed triggers (a
    /// typed export fails the mod's load).
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
    /// IModComponentOf&lt;T&gt;).
    public void Insert<T>(ulong entity, string path, in T value) where T : struct
        => _mod.Commands.InsertTyped(entity, Typed<T>(_mod.Ctx, path), value);

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

    internal static IModComponentOf<T> Typed<T>(ModHostContext ctx, string path) where T : struct
    {
        if (!ctx.Registry.TryGet(path, out var comp))
            throw new InvalidOperationException($"'{path}' is not a registered component");
        return comp as IModComponentOf<T>
            ?? throw new InvalidOperationException($"'{path}' has no typed {typeof(T).Name} mapper");
    }
}

/// A guest call's `query` parameter, for typed game imports: the snapshot `rows` reads,
/// as entities + typed columns.
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

    /// The live matched entities, in `rows` / `entities` order.
    public ReadOnlySpan<ulong> Entities => _mod.Live(_param);

    /// The mapper of reading term `term` (ref / mut / changed / added, in declaration
    /// order) — Get it per entity of Entities. Traps unless that term reads `path`.
    public IModComponentOf<T> Column<T>(int term, string path) where T : struct
    {
        Term(term, path, "column");
        return _mod.ReadsOf(_param)[term] as IModComponentOf<T>
            ?? throw new InvalidOperationException($"column: '{path}' has no typed {typeof(T).Name} mapper");
    }

    /// Queues a write-back of reading term `term` (`query.set`, typed). Traps unless that
    /// term is a `mut` term reading `path`.
    public void Set<T>(ulong entity, int term, string path, in T value) where T : struct
    {
        if (!Term(term, path, "set"))
            throw new InvalidOperationException($"set({term}): {path} is not a `mut` term");
        _mod.Commands.InsertTyped(entity, ComponentModCommands.Typed<T>(_mod.Ctx, path), value);
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
}

/// Lowers a typed observer's trigger value (ComponentModImports.TypedTrigger): the payload
/// registered as `path`, as its JSON, to the record the export takes.
public delegate Cm.ComponentValue ComponentTriggerLowering(string path, ReadOnlySpan<byte> json);

/// Reads the results of ComponentModImports.TryCallExport (valid only inside the call).
public delegate void ComponentResultsReader(Cm.ComponentCallResults results);

/// A guest call's `events` parameter, for typed game imports: the events of its type sent
/// since the system's last run, as their JSON (what `events.read` hands out).
public readonly struct ComponentModEvents
{
    private readonly ComponentParam _param;

    internal ComponentModEvents(ComponentParam param) => _param = param;

    public string Path => _param.Path;

    /// The events, in send order. Traps unless the parameter names `path`.
    public IReadOnlyList<string> Read(string path)
    {
        if (_param.Path != path)
            throw new InvalidOperationException($"read: the parameter is {_param.Path}, not {path}");
        return _param.Events!.Current;
    }
}

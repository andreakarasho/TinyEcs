// The `app` bridge a WASM guest/mod calls back into (spawn / component get-set /
// query iter / emit-event / resource get-set). Every type here is neutral — no
// wasmtime types, no generated bindings — so it compiles unchanged under both the
// desktop core-wasm backend (CoreWasmModBackend applies a mod's CommandBuffer through
// these Impl structs) and the browser Jco backend, and under WasmGuest. Each Impl
// struct exposes the guest-facing operations as concrete methods over neutral/BCL
// types (ModSchedule/ModObserverKind/ModQueryTerm, plain strings/spans).
//
// They share a per-mod ModHostContext (World + component registry + the list of
// systems the mod registered during setup). These methods run synchronously
// inside a guest call mid-system, so they touch the World directly (the documented
// carve-out); the runner systems are SingleThreaded to keep that safe.

using TinyEcs;
using TinyEcs.Bevy;

namespace TinyEcs.Bevy.Modding;

internal enum ModParamKind { Commands, Query }

internal sealed class ModQuerySpec
{
    // All declared terms (used to compute the matching-entity snapshot).
    public readonly List<(string typePath, bool mut, ModQueryTermKind kind)> Terms = new();
    // ref/mut terms only, in declared order — defines query-result component index.
    public readonly List<(string typePath, bool mut)> Components = new();

    // Registry mappers resolved ONCE per spec. Both hot loops (BuildSnapshot filters
    // every term per candidate entity; the row builder serializes every read term per
    // row) used to hash the type-path string on each visit — O(entities x terms)
    // dictionary lookups per tick. A registry is built once per App and the wire id
    // space is interned in the mod's Handshake, so both are stable for the spec's life.
    private (IModComponent? Comp, ModQueryTermKind Kind)[]? _termMappers;
    private (IModComponent Comp, ushort TypeId)[]? _rowMappers;

    // Parallel to Terms; null Comp = the path names nothing registered (BuildSnapshot
    // treats that as "matches only a Without term", same as the lookup it replaces).
    public (IModComponent? Comp, ModQueryTermKind Kind)[] TermMappers(ModComponentRegistry registry)
    {
        if (_termMappers != null)
            return _termMappers;
        var mappers = new (IModComponent?, ModQueryTermKind)[Terms.Count];
        for (var i = 0; i < Terms.Count; i++)
            mappers[i] = (registry.TryGet(Terms[i].typePath, out var comp) ? comp : null, Terms[i].kind);
        return _termMappers = mappers;
    }

    // Parallel to Components. Throws (not skips) on an unregistered read term: the row
    // builder emits one CompValue per read term in declaration order, so a hole would
    // shift every later positional accessor in the guest for the whole row.
    public (IModComponent Comp, ushort TypeId)[] RowMappers(
        ModComponentRegistry registry, Dictionary<string, ushort> pathToId, string systemName)
    {
        if (_rowMappers != null)
            return _rowMappers;
        var mappers = new (IModComponent, ushort)[Components.Count];
        for (var i = 0; i < Components.Count; i++)
        {
            var path = Components[i].typePath;
            if (!registry.TryGet(path, out var comp))
                throw new InvalidOperationException($"system '{systemName}': unregistered component '{path}'");
            mappers[i] = (comp, pathToId.TryGetValue(path, out var id) ? id : (ushort)0xFFFF);
        }
        return _rowMappers = mappers;
    }
}

internal sealed class ModParam
{
    public ModParamKind Kind;
    public ModQuerySpec? Query; // set when Kind == Query
}

internal sealed class ModSystemSpec
{
    public string Name = "";
    public ModSchedule Stage;
    public string? CustomStage;
    public readonly List<ModParam> Params = new();
    public readonly List<string> After = new();
    public readonly List<string> Before = new();
    // Consecutive scheduler ticks every query param matched zero entities.
    // Drives the idle-skip in ModdingPlugin.RunSystemsForStage — crossing the
    // component boundary for a no-row tick costs ~0.3ms/system on the jco
    // (JS-engine-in-wasm) backend.
    public int EmptyStreak;
    // Minimum host-milliseconds between runs (SystemDecl.interval_ms); 0 = every tick.
    // Checked BEFORE any query is evaluated — a throttled system costs nothing.
    public uint IntervalMs;
    // Time.Total (ms) at the last run; the interval gate's reference point. Starts at
    // -inf so the FIRST tick always runs (a 0 default would gate the system out at
    // boot, when Time.Total is still ~0).
    public float LastRunTime = float.NegativeInfinity;
    // World.CurrentTick at the last EVALUATION of this system (set even when the run
    // was idle-skipped, since the queries were still evaluated). Changed query terms
    // filter on "changed-tick newer than this".
    public uint LastRunWorldTick;
}

internal sealed class ModObserverSpec
{
    public string Name = "";                       // guest export to call on fire
    public ModObserverKind Kind;
    public string? TypePath;                        // component/event path for Insert/Remove/Custom
}

/// Shared glue for one mod instance. Public so a host can configure per-mod
/// behaviour (extra linker imports, input wiring) from a ModdingConfig hook.
public sealed class ModHostContext
{
    public World World = null!;
    public ModComponentRegistry Registry = null!;
    // Manifest name of the mod this context belongs to — diagnostics raised deep in
    // the guest-call path (a rejected command, a bad payload) name the mod.
    public string Name = "";
    // Index of this mod among the loaded runtimes — stamped onto every ModEntity
    // this mod spawns (ModEntity.Slot) so the host can scope a mod's entities for
    // disable/reload teardown without touching other mods' entities.
    public int Slot;
    // The host App — used to resolve singleton resources lazily at call time.
    // Absent in bare unit-test apps, so every use is guarded by HasResource.
    public App? App;
    // Optional host hook to consume a mouse button (the input-override capability).
    // The generic lib has no input model of its own; the host wires this.
    public Action<byte>? ConsumeMouse;
    // Keyboard half of the input-override capability (consume a key this frame).
    public Action<uint>? ConsumeKeyboard;
    // Game-specific wasm host imports, described by the HOST (a ModdingConfig.
    // PerModContext hook) as meaning-free shape descriptors — see ModHostImports.cs.
    // The generic lib defines only the ECS imports (log / entity_parent /
    // entity_children / component_get / resource_get) and supplies no game imports
    // of its own; it never learns their names or semantics. HostImportModule is the
    // wasm import module BOTH sets are defined under (fixed by the mod wire ABI the
    // host ships, so the host owns the string).
    public string HostImportModule = "host";
    public readonly List<ModHostImport> HostImports = new();
    internal readonly List<ModSystemSpec> Systems = new();
    // Systems bucketed by stage so the per-frame runner does a dict lookup
    // instead of scanning every system and filtering. Populated in AddSystems;
    // per-stage insertion order preserved (declaration order within a stage).
    internal readonly Dictionary<ModSchedule, List<ModSystemSpec>> SystemsByStage = new();
    // Observers the mod registered during setup; wired to host global observers
    // after setup (see ModdingPlugin.RegisterModObservers).
    internal readonly List<ModObserverSpec> Observers = new();
}

internal struct AppImpl(ModHostContext ctx)
{
    public void AddSystems(ModSchedule schedule, string? customStage, ReadOnlySpan<SystemImpl> systems)
    {
        foreach (var s in systems)
        {
            var spec = s.Spec;
            spec.Stage = schedule;
            spec.CustomStage = customStage;
            ctx.Systems.Add(spec);
            if (!ctx.SystemsByStage.TryGetValue(spec.Stage, out var bucket))
                ctx.SystemsByStage[spec.Stage] = bucket = new List<ModSystemSpec>();
            bucket.Add(spec);
        }
    }

    public void AddObserver(string name, ModObserverKind kind, string? typePath)
    {
        ctx.Observers.Add(new ModObserverSpec { Name = name, Kind = kind, TypePath = typePath });
    }
}

internal struct SystemImpl
{
    public readonly ModSystemSpec Spec;

    public SystemImpl(string name) => Spec = new ModSystemSpec { Name = name };

    public void AddCommands() => Spec.Params.Add(new ModParam { Kind = ModParamKind.Commands });

    public void AddQuery(ReadOnlySpan<ModQueryTerm> query)
    {
        var q = new ModQuerySpec();
        foreach (var qf in query)
        {
            switch (qf.Kind)
            {
                case ModQueryTermKind.Ref:
                    q.Terms.Add((qf.TypePath, false, qf.Kind));
                    q.Components.Add((qf.TypePath, false));
                    break;
                case ModQueryTermKind.Mut:
                    q.Terms.Add((qf.TypePath, true, qf.Kind));
                    q.Components.Add((qf.TypePath, true));
                    break;
                // Changed is BOTH a filter term (Terms, so BuildSnapshot applies the
                // change gate) and a read term (Components, so the row carries the
                // payload) — the guest sees it interleaved with Ref/Mut in declaration
                // order, exactly like a Ref.
                case ModQueryTermKind.Changed:
                    q.Terms.Add((qf.TypePath, false, qf.Kind));
                    q.Components.Add((qf.TypePath, false));
                    break;
                case ModQueryTermKind.With:
                    q.Terms.Add((qf.TypePath, false, qf.Kind));
                    break;
                case ModQueryTermKind.Without:
                    q.Terms.Add((qf.TypePath, false, qf.Kind));
                    break;
            }
        }
        Spec.Params.Add(new ModParam { Kind = ModParamKind.Query, Query = q });
    }

    public void After(SystemImpl other) => Spec.After.Add(other.Spec.Name);
    public void Before(SystemImpl other) => Spec.Before.Add(other.Spec.Name);
}

// Internal: the lib tests drive it directly via InternalsVisibleTo (see the csproj).
internal struct CommandsImpl(ModHostContext ctx)
{
    public EntityCommandsImpl SpawnEmpty()
    {
        var ent = ctx.World.Entity();
        ent.Set(new ModEntity { Slot = (byte)ctx.Slot });
        return new EntityCommandsImpl(ctx, ent.ID);
    }

    public EntityCommandsImpl Spawn(ReadOnlySpan<(string, string)> bundle)
    {
        var ent = ctx.World.Entity();
        ent.Set(new ModEntity { Slot = (byte)ctx.Slot });
        var id = ent.ID;
        foreach (var (typePath, json) in bundle)
            if (ctx.Registry.TryGet(typePath, out var comp))
                comp.SetJson(ctx.World, id, json);
        return new EntityCommandsImpl(ctx, id);
    }

    // UTF8 overload for the core-ABI apply path: payloads are slices of the guest's
    // reply buffer and go straight into the registry, so no string per component.
    public EntityCommandsImpl Spawn(ReadOnlySpan<(string, ReadOnlyMemory<byte>)> bundle)
    {
        var ent = ctx.World.Entity();
        ent.Set(new ModEntity { Slot = (byte)ctx.Slot });
        var id = ent.ID;
        foreach (var (typePath, json) in bundle)
            if (ctx.Registry.TryGet(typePath, out var comp))
                comp.SetJsonUtf8(ctx.World, id, json.Span);
        return new EntityCommandsImpl(ctx, id);
    }

    public EntityCommandsImpl Entity(EntityImpl entity)
        => new EntityCommandsImpl(ctx, entity.EcsId);

    public EntityCommandsImpl EntityById(ulong id) => new EntityCommandsImpl(ctx, id);

    // Singleton-resource access by type-path (the "change resource" capability).
    // The calling mod's name rides along: a resource holding per-mod state serves
    // this mod's slice (see IModResource.GetJsonFor / SetJsonFrom); every other
    // mapper's default ignores it and keeps whole-resource semantics.
    public string ResourceGet(string resource)
        => ctx.App != null && ctx.Registry.TryGetResource(resource, out var r)
            ? r.GetJsonFor(ctx.App, ctx.Name)
            : "null";

    public void ResourceSet(string resource, string value)
    {
        if (ctx.App != null && ctx.Registry.TryGetResource(resource, out var r))
            r.SetJsonFrom(ctx.App, value, ctx.Name);
    }

    public void ResourceSet(string resource, ReadOnlySpan<byte> value)
    {
        if (ctx.App != null && ctx.Registry.TryGetResource(resource, out var r))
            r.SetJsonUtf8From(ctx.App, value, ctx.Name);
    }

    // Input override — consume a mouse button this frame. The lib owns no input
    // model; route to the host's hook (no-op if the host didn't wire one).
    public void InputConsumeMouse(byte button) => ctx.ConsumeMouse?.Invoke(button);

    // Keyboard half of the input override — same routing (no-op if unwired).
    public void InputConsumeKeyboard(uint key) => ctx.ConsumeKeyboard?.Invoke(key);

    // Emit a host-registered custom event by name. Fires as a typed host trigger
    // (On<T>), so host systems and any mod observing `custom(name)` both receive
    // it. No-op if the name isn't registered.
    public void EmitEvent(string name, ulong entity, string json)
    {
        if (ctx.Registry.TryGetEvent(name, out var ev))
            ev.Emit(ctx.World, entity, json);
    }
}

internal struct EntityCommandsImpl(ModHostContext ctx, ulong entity)
{
    public EntityImpl Id() => new EntityImpl(ctx, entity);

    // Exists-guarded like Remove/AddChild/Despawn: a guest's InsertCmd routinely
    // names an entity that died between the pushed query snapshot and this apply
    // (or an unresolvable temp ref, which ModAbiRunner.Resolve maps to 0). World.Set
    // panics on a dead id, and that throw escapes the whole CommandBuffer — every
    // later command in the buffer would be silently dropped.
    public void Insert(ReadOnlySpan<(string, string)> bundle)
    {
        if (!ctx.World.Exists(entity))
            return;
        foreach (var (typePath, json) in bundle)
            if (ctx.Registry.TryGet(typePath, out var comp))
                comp.SetJson(ctx.World, entity, json);
    }

    // UTF8 overload — see CommandsImpl.Spawn(ReadOnlySpan<(string, ReadOnlyMemory<byte>)>).
    public void Insert(ReadOnlySpan<(string, ReadOnlyMemory<byte>)> bundle)
    {
        if (!ctx.World.Exists(entity))
            return;
        foreach (var (typePath, json) in bundle)
            if (ctx.Registry.TryGet(typePath, out var comp))
                comp.SetJsonUtf8(ctx.World, entity, json.Span);
    }

    public void Remove(ReadOnlySpan<string> bundle)
    {
        foreach (var typePath in bundle)
            if (ctx.Registry.TryGet(typePath, out var comp) && ctx.World.Exists(entity))
                comp.Remove(ctx.World, entity);
    }

    // `entity` is the parent (this entity-commands' entity); add `child` under it.
    public void AddChild(EntityImpl child, uint index)
    {
        var childId = child.EcsId;
        if (ctx.World.Exists(entity) && ctx.World.Exists(childId))
            ctx.World.AddChild(entity, childId, index >= int.MaxValue ? -1 : (int)index);
    }

    public void Despawn() { if (ctx.World.Exists(entity)) ctx.World.Delete(entity); }
}

internal struct EntityImpl(ModHostContext ctx, ulong ecsId)
{
    public ulong EcsId => ecsId;

    public ulong Id() => ecsId;

    public EntityImpl? Parent()
    {
        if (!ctx.World.Exists(ecsId))
            return null;

        var p = (ulong)ctx.World.GetParent(ecsId);
        return p != 0 && ctx.World.Exists(p) ? new EntityImpl(ctx, p) : null;
    }

    // Exists() first: a guest holds ids from a PUSHED snapshot, so they routinely
    // outlive the entity, and World.Has panics on a dead one — that throw would trap
    // the guest mid-system. "No component" is the honest answer at this boundary.
    public string Get(string component)
        => ctx.World.Exists(ecsId) && ctx.Registry.TryGet(component, out var c) && c.Has(ctx.World, ecsId)
            ? c.GetJson(ctx.World, ecsId)
            : "null";

    public EntityImpl[] Children()
    {
        // TinyEcs's relationship mapper keeps the ordered child list on the parent as
        // a `Children` component — read it instead of scanning every entity with a
        // Parent (which was O(all parented entities) per call).
        if (!ctx.World.Exists(ecsId) || !ctx.World.Has<TinyEcs.Children>(ecsId))
            return Array.Empty<EntityImpl>();
        var children = ctx.World.Get<TinyEcs.Children>(ecsId);
        var result = new EntityImpl[children.Count];
        var i = 0;
        foreach (var child in children)
            result[i++] = new EntityImpl(ctx, (ulong)child);
        return result;
    }
}

// snapshot is an ArrayPool-rented buffer owned by CallSystem (returned in its
// finally after the wasm Call); only [0, count) is valid. We never return it here
// — the buffer outlives this resource's Dispose and is reclaimed post-Call. The
// wasmtime adapter boxes this struct once on RegisterQuery, so _cursor persists
// across Iter().
internal struct QueryImpl(ModHostContext ctx, ulong[] snapshot, int count, List<(string typePath, bool mut)> components)
{
    private int _cursor;

    public QueryResultImpl? Iter()
    {
        // Skip entities despawned mid-iteration (snapshot is taken up front).
        while (_cursor < count)
        {
            var id = snapshot[_cursor++];
            if (ctx.World.Exists(id))
                return new QueryResultImpl(ctx, id, components);
        }
        return null;
    }
}

internal struct QueryResultImpl(ModHostContext ctx, ulong entity, List<(string typePath, bool mut)> components)
{
    public EntityImpl Entity() => new EntityImpl(ctx, entity);

    public ComponentImpl Component(byte index)
    {
        var (typePath, mut) = components[index];
        return new ComponentImpl(ctx, entity, typePath, mut);
    }
}

internal struct ComponentImpl(ModHostContext ctx, ulong entity, string typePath, bool mutable)
{
    public string Get()
        => ctx.World.Exists(entity) && ctx.Registry.TryGet(typePath, out var comp)
            ? comp.GetJson(ctx.World, entity)
            : "null";

    public void Set(string value)
    {
        if (!mutable)
            ThrowNotMutable(typePath);
        if (ctx.World.Exists(entity) && ctx.Registry.TryGet(typePath, out var comp))
            comp.SetJson(ctx.World, entity, value);
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void ThrowNotMutable(string typePath)
        => throw new InvalidOperationException($"component {typePath} was not declared mutable");
}

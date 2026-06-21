// Host implementation of the tinyecs:modding `app` interface (the imports the WASM
// guest/mod consumes). The wasmtime-dotnet fork emits each WIT resource (app/
// system/commands/entity-commands/entity/query/query-result/component) as an
// INTERFACE; the host implements them. We use structs (the fork boxes each once
// when it's stored in the handle table — same single alloc as a class, with the
// boxed instance persisting mutable state like QueryImpl._cursor across calls).
// The top-level imports (GuestBridge) is still an abstract class with overrides.
//
// They share a per-mod ModHostContext (World + component registry + the list of
// systems the mod registered during setup). These methods run synchronously
// inside a guest call mid-system, so they touch the World directly (the documented
// carve-out); the runner systems are SingleThreaded to keep that safe.

using System.Diagnostics.CodeAnalysis;
using TinyEcs;
using TinyEcs.Bevy;
using G = Wit.Tinyecs.Modding.GuestImports;
using WitApp = Wit.Tinyecs.Modding.App;

namespace TinyEcs.Bevy.Modding;

internal enum ModParamKind { Commands, Query }

internal sealed class ModQuerySpec
{
    // All declared terms (used to compute the matching-entity snapshot).
    public readonly List<(string typePath, bool mut, WitApp.QueryFor.Case kind)> Terms = new();
    // ref/mut terms only, in declared order — defines query-result component index.
    public readonly List<(string typePath, bool mut)> Components = new();
}

internal sealed class ModParam
{
    public ModParamKind Kind;
    public ModQuerySpec? Query; // set when Kind == Query
}

internal sealed class ModSystemSpec
{
    public string Name = "";
    public WitApp.Schedule.Case Stage;
    public string? CustomStage;
    public readonly List<ModParam> Params = new();
    public readonly List<string> After = new();
    public readonly List<string> Before = new();
}

/// Shared glue for one mod instance. Public so a host can configure per-mod
/// behaviour (extra linker imports, input wiring) from a ModdingConfig hook.
public sealed class ModHostContext
{
    public World World = null!;
    public ModComponentRegistry Registry = null!;
    // The host App — used to resolve singleton resources lazily at call time.
    // Absent in bare unit-test apps, so every use is guarded by HasResource.
    public App? App;
    // Optional host hook to consume a mouse button (the input-override capability).
    // The generic lib has no input model of its own; the host wires this.
    public Action<byte>? ConsumeMouse;
    internal readonly List<ModSystemSpec> Systems = new();
    // Systems bucketed by stage so the per-frame runner does a dict lookup
    // instead of scanning every system and filtering. Populated in AddSystems;
    // per-stage insertion order preserved (declaration order within a stage).
    internal readonly Dictionary<WitApp.Schedule.Case, List<ModSystemSpec>> SystemsByStage = new();
    // Cached `With<Parent>` query for EntityImpl.Children — EntityImpl is a struct
    // recreated per call, so the cache lives here (one ctx per mod/world).
    internal Query? ChildrenQuery;
}

internal sealed class GuestBridge(ModHostContext ctx) : G
{
    public override G.ISystem NewSystem(string name) => new SystemImpl(name);
}

internal struct AppImpl(ModHostContext ctx) : G.IApp
{
    public void AddSystems(WitApp.Schedule schedule, ReadOnlySpan<G.ISystem> systems)
    {
        foreach (var s in systems)
        {
            var spec = ((SystemImpl)s).Spec;
            spec.Stage = schedule.Discriminant;
            spec.CustomStage = schedule.CustomPayload;
            ctx.Systems.Add(spec);
            if (!ctx.SystemsByStage.TryGetValue(spec.Stage, out var bucket))
                ctx.SystemsByStage[spec.Stage] = bucket = new List<ModSystemSpec>();
            bucket.Add(spec);
        }
    }

    public void Dispose() { }
}

internal struct SystemImpl : G.ISystem
{
    public readonly ModSystemSpec Spec;

    public SystemImpl(string name) => Spec = new ModSystemSpec { Name = name };

    public void AddCommands() => Spec.Params.Add(new ModParam { Kind = ModParamKind.Commands });

    public void AddQuery(ReadOnlySpan<WitApp.QueryFor> query)
    {
        var q = new ModQuerySpec();
        foreach (var qf in query)
        {
            switch (qf.Discriminant)
            {
                case WitApp.QueryFor.Case.Ref:
                    q.Terms.Add((qf.RefPayload, false, qf.Discriminant));
                    q.Components.Add((qf.RefPayload, false));
                    break;
                case WitApp.QueryFor.Case.Mut:
                    q.Terms.Add((qf.MutPayload, true, qf.Discriminant));
                    q.Components.Add((qf.MutPayload, true));
                    break;
                case WitApp.QueryFor.Case.With:
                    q.Terms.Add((qf.WithPayload, false, qf.Discriminant));
                    break;
                case WitApp.QueryFor.Case.Without:
                    q.Terms.Add((qf.WithoutPayload, false, qf.Discriminant));
                    break;
            }
        }
        Spec.Params.Add(new ModParam { Kind = ModParamKind.Query, Query = q });
    }

    public void After(G.ISystem other) => Spec.After.Add(((SystemImpl)other).Spec.Name);
    public void Before(G.ISystem other) => Spec.Before.Add(((SystemImpl)other).Spec.Name);
    public void Dispose() { }
}

// Internal: it implements the (internal) generated G.ICommands and references
// internal generated types. Tests that drive it directly reach it via
// InternalsVisibleTo (see the csproj).
internal struct CommandsImpl(ModHostContext ctx) : G.ICommands
{
    public G.IEntityCommands SpawnEmpty()
    {
        var ent = ctx.World.Entity();
        ent.Set(new ModEntity());
        return new EntityCommandsImpl(ctx, ent.ID);
    }

    public G.IEntityCommands Spawn(ReadOnlySpan<(string, string)> bundle)
    {
        var ent = ctx.World.Entity();
        ent.Set(new ModEntity());
        var id = ent.ID;
        foreach (var (typePath, json) in bundle)
            if (ctx.Registry.TryGet(typePath, out var comp))
                comp.SetJson(ctx.World, id, json);
        return new EntityCommandsImpl(ctx, id);
    }

    public G.IEntityCommands Entity(G.IEntity entity)
        => new EntityCommandsImpl(ctx, ((EntityImpl)entity).EcsId);

    // Typed spawn — components cross as native records (no JSON registry).
    public G.IEntityCommands SpawnTyped(ReadOnlySpan<WitApp.ComponentValue> bundle)
    {
        var ent = ctx.World.Entity();
        ent.Set(new ModEntity());
        var id = ent.ID;
        foreach (var cv in bundle)
            ModTypedComponents.Apply(ctx.World, id, cv);
        return new EntityCommandsImpl(ctx, id);
    }

    // Singleton-resource access by type-path (the "change resource" capability).
    public string ResourceGet(string resource)
        => ctx.App != null && ctx.Registry.TryGetResource(resource, out var r)
            ? r.GetJson(ctx.App)
            : "null";

    public void ResourceSet(string resource, string value)
    {
        if (ctx.App != null && ctx.Registry.TryGetResource(resource, out var r))
            r.SetJson(ctx.App, value);
    }

    // Input override — consume a mouse button this frame. The lib owns no input
    // model; route to the host's hook (no-op if the host didn't wire one).
    public void InputConsumeMouse(byte button) => ctx.ConsumeMouse?.Invoke(button);

    public void Dispose() { }
}

internal struct EntityCommandsImpl(ModHostContext ctx, ulong entity) : G.IEntityCommands
{
    public G.IEntity Id() => new EntityImpl(ctx, entity);

    public void Insert(ReadOnlySpan<(string, string)> bundle)
    {
        foreach (var (typePath, json) in bundle)
            if (ctx.Registry.TryGet(typePath, out var comp))
                comp.SetJson(ctx.World, entity, json);
    }

    public void InsertTyped(ReadOnlySpan<WitApp.ComponentValue> bundle)
    {
        foreach (var cv in bundle)
            ModTypedComponents.Apply(ctx.World, entity, cv);
    }

    public void Remove(ReadOnlySpan<string> bundle)
    {
        foreach (var typePath in bundle)
            if (ctx.Registry.TryGet(typePath, out var comp) && ctx.World.Exists(entity))
                comp.Remove(ctx.World, entity);
    }

    // `entity` is the parent (this entity-commands' entity); add `child` under it.
    public void AddChild(G.IEntity child, uint index)
    {
        var childId = ((EntityImpl)child).EcsId;
        if (ctx.World.Exists(entity) && ctx.World.Exists(childId))
            ctx.World.AddChild(entity, childId, index >= int.MaxValue ? -1 : (int)index);
    }

    public void Despawn() { if (ctx.World.Exists(entity)) ctx.World.Delete(entity); }
    public void Dispose() { }
}

internal struct EntityImpl(ModHostContext ctx, ulong ecsId) : G.IEntity
{
    public ulong EcsId => ecsId;

    public G.IEntity? Parent()
    {
        var p = (ulong)ctx.World.GetParent(ecsId);
        return p != 0 && ctx.World.Exists(p) ? new EntityImpl(ctx, p) : null;
    }

    public string Get(string component)
        => ctx.Registry.TryGet(component, out var c) && c.Has(ctx.World, ecsId)
            ? c.GetJson(ctx.World, ecsId)
            : "null";

    public ReadOnlySpan<G.IEntity> Children()
    {
        // Enumerate by the Parent relationship (no host-internal access needed).
        var result = new List<G.IEntity>();
        var q = ctx.ChildrenQuery ??= ctx.World.QueryBuilder().With<TinyEcs.Parent>().Build();
        var it = q.Iter();
        while (it.Next())
            foreach (var ev in it.Entities())
                if ((ulong)ctx.World.Get<TinyEcs.Parent>(ev.ID).Id == ecsId)
                    result.Add(new EntityImpl(ctx, ev.ID));
        return result.ToArray();
    }

    public void Dispose() { }
}

// snapshot is an ArrayPool-rented buffer owned by CallSystem (returned in its
// finally after the wasm Call); only [0, count) is valid. We never return it here
// — the buffer outlives this resource's Dispose and is reclaimed post-Call. The
// fork boxes this struct once on RegisterQuery, so _cursor persists across Iter().
internal struct QueryImpl(ModHostContext ctx, ulong[] snapshot, int count, List<(string typePath, bool mut)> components) : G.IQuery
{
    private int _cursor;

    public G.IQueryResult? Iter()
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

    public void Dispose() { }
}

internal struct QueryResultImpl(ModHostContext ctx, ulong entity, List<(string typePath, bool mut)> components) : G.IQueryResult
{
    public G.IEntity Entity() => new EntityImpl(ctx, entity);

    public G.IComponent Component(byte index)
    {
        var (typePath, mut) = components[index];
        return new ComponentImpl(ctx, entity, typePath, mut);
    }

    public void Dispose() { }
}

internal struct ComponentImpl(ModHostContext ctx, ulong entity, string typePath, bool mutable) : G.IComponent
{
    public string Get()
        => ctx.Registry.TryGet(typePath, out var comp) ? comp.GetJson(ctx.World, entity) : "null";

    public void Set(string value)
    {
        if (!mutable)
            ThrowNotMutable(typePath);
        if (ctx.Registry.TryGet(typePath, out var comp))
            comp.SetJson(ctx.World, entity, value);
    }

    // Typed write — native record via component-value, no JSON. (Typed READ is
    // not exposed: the fork runtime mishandles variant-return ownership.)
    public void SetTyped(WitApp.ComponentValue value)
    {
        if (!mutable)
            ThrowNotMutable(typePath);
        ModTypedComponents.Apply(ctx.World, entity, value);
    }

    [DoesNotReturn]
    private static void ThrowNotMutable(string typePath)
        => throw new InvalidOperationException($"component {typePath} was not declared mutable");

    public void Dispose() { }
}

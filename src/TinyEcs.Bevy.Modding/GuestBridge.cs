// Runtime-agnostic half of the tinyecs:modding `app` bridge (the imports a WASM
// guest/mod consumes). Every type in this file is neutral — no wasmtime types, no
// Wit.* generated bindings — so it compiles under WasmGuest with zero wasmtime
// references. Each Impl struct exposes the guest-facing operations as concrete
// methods over neutral/BCL types (ModSchedule/ModObserverKind/ModQueryTerm, plain
// strings/spans, concrete struct returns instead of interfaces).
//
// The wasmtime backend needs each of these to satisfy a fork-generated G.I*
// interface (G.IApp, G.ICommands, ...) with WitApp-typed parameters. Those thin
// adapter shims — plus the GuestBridge(ModHostContext) : G.GuestImports class
// itself, plus the typed (component-value) methods (SpawnTyped/InsertTyped/
// SetTyped), which only the wasmtime channel ever calls — live in
// WasmtimeGuestAdapters.cs, a wasmtime-only file excluded under WasmGuest. Several
// structs here are declared `partial` so that file can add those typed methods
// without this file needing to know about WitApp.ComponentValue.
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
    internal readonly List<ModSystemSpec> Systems = new();
    // Systems bucketed by stage so the per-frame runner does a dict lookup
    // instead of scanning every system and filtering. Populated in AddSystems;
    // per-stage insertion order preserved (declaration order within a stage).
    internal readonly Dictionary<ModSchedule, List<ModSystemSpec>> SystemsByStage = new();
    // Cached `With<Parent>` query for EntityImpl.Children — EntityImpl is a struct
    // recreated per call, so the cache lives here (one ctx per mod/world).
    internal Query? ChildrenQuery;
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

// Internal: references internal generated types. Tests that drive it directly
// reach it via InternalsVisibleTo (see the csproj). `partial`: the typed
// (component-value) SpawnTyped overload is wasmtime-only — see WasmtimeGuestAdapters.cs.
internal partial struct CommandsImpl(ModHostContext ctx)
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

    public EntityCommandsImpl Entity(EntityImpl entity)
        => new EntityCommandsImpl(ctx, entity.EcsId);

    public EntityCommandsImpl EntityById(ulong id) => new EntityCommandsImpl(ctx, id);

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

// `partial`: InsertTyped (component-value) is wasmtime-only — see WasmtimeGuestAdapters.cs.
internal partial struct EntityCommandsImpl(ModHostContext ctx, ulong entity)
{
    public EntityImpl Id() => new EntityImpl(ctx, entity);

    public void Insert(ReadOnlySpan<(string, string)> bundle)
    {
        foreach (var (typePath, json) in bundle)
            if (ctx.Registry.TryGet(typePath, out var comp))
                comp.SetJson(ctx.World, entity, json);
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
        var p = (ulong)ctx.World.GetParent(ecsId);
        return p != 0 && ctx.World.Exists(p) ? new EntityImpl(ctx, p) : null;
    }

    public string Get(string component)
        => ctx.Registry.TryGet(component, out var c) && c.Has(ctx.World, ecsId)
            ? c.GetJson(ctx.World, ecsId)
            : "null";

    public EntityImpl[] Children()
    {
        // Enumerate by the Parent relationship (no host-internal access needed).
        var result = new List<EntityImpl>();
        var q = ctx.ChildrenQuery ??= ctx.World.QueryBuilder().With<TinyEcs.Parent>().Build();
        var it = q.Iter();
        while (it.Next())
            foreach (var ev in it.Entities())
                if ((ulong)ctx.World.Get<TinyEcs.Parent>(ev.ID).Id == ecsId)
                    result.Add(new EntityImpl(ctx, ev.ID));
        return result.ToArray();
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

// `partial`: SetTyped (component-value) is wasmtime-only — see WasmtimeGuestAdapters.cs.
internal partial struct ComponentImpl(ModHostContext ctx, ulong entity, string typePath, bool mutable)
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

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void ThrowNotMutable(string typePath)
        => throw new InvalidOperationException($"component {typePath} was not declared mutable");
}

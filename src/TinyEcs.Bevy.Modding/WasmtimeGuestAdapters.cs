// Wasmtime-only half of the tinyecs:modding `app` bridge. Excluded from the build
// under WasmGuest (see the csproj) — everything here references the wasmtime-dotnet
// fork's generated WIT bindings (Wit.Tinyecs.Modding.*, aliased G./WitApp.).
//
// GuestBridge.cs defines the neutral Impl structs (AppImpl, SystemImpl, CommandsImpl,
// EntityCommandsImpl, EntityImpl, QueryImpl, QueryResultImpl, ComponentImpl) with
// concrete methods over neutral/BCL types. This file:
//   - maps the fork-generated WitApp.Schedule/ObserverEvent/QueryFor onto the
//     neutral ModSchedule/ModObserverKind/ModQueryTerm at the boundary,
//   - adds thin *Adapter structs implementing the fork's G.I* resource interfaces
//     by delegating to the matching neutral Impl struct (the fork boxes a resource
//     once when it's stored in the handle table, so mutable state like
//     QueryImpl._cursor still persists across calls through the adapter),
//   - adds the typed (component-value, no-JSON) SpawnTyped/InsertTyped/SetTyped
//     overloads as partial-struct extensions of CommandsImpl/EntityCommandsImpl/
//     ComponentImpl — the Jco path never calls these (typed values are converted to
//     JSON JS-side), so they stay confined to this wasmtime-only file.
//   - defines GuestBridge itself, the abstract-class-derived guest import root the
//     fork's Linker.Define needs.

using TinyEcs;
using G = Wit.Tinyecs.Modding.GuestImports;
using WitApp = Wit.Tinyecs.Modding.App;

namespace TinyEcs.Bevy.Modding;

internal sealed class GuestBridge(ModHostContext ctx) : G
{
    public override G.ISystem NewSystem(string name) => new SystemAdapter(new SystemImpl(name));
}

// ── WitApp.* <-> neutral mapping ──────────────────────────────────────────────

internal static class WitAppMapping
{
    public static ModSchedule ToModSchedule(WitApp.Schedule.Case c) => c switch
    {
        WitApp.Schedule.Case.ModStartup => ModSchedule.ModStartup,
        WitApp.Schedule.Case.First => ModSchedule.First,
        WitApp.Schedule.Case.PreUpdate => ModSchedule.PreUpdate,
        WitApp.Schedule.Case.Update => ModSchedule.Update,
        WitApp.Schedule.Case.PostUpdate => ModSchedule.PostUpdate,
        WitApp.Schedule.Case.Last => ModSchedule.Last,
        WitApp.Schedule.Case.Custom => ModSchedule.Custom,
        _ => throw new NotSupportedException($"unmapped schedule case {c}"),
    };

    public static ModQueryTerm ToModQueryTerm(WitApp.QueryFor qf) => qf.Discriminant switch
    {
        WitApp.QueryFor.Case.Ref => new ModQueryTerm(ModQueryTermKind.Ref, qf.RefPayload),
        WitApp.QueryFor.Case.Mut => new ModQueryTerm(ModQueryTermKind.Mut, qf.MutPayload),
        WitApp.QueryFor.Case.With => new ModQueryTerm(ModQueryTermKind.With, qf.WithPayload),
        WitApp.QueryFor.Case.Without => new ModQueryTerm(ModQueryTermKind.Without, qf.WithoutPayload),
        _ => throw new NotSupportedException($"unmapped query-for case {qf.Discriminant}"),
    };
}

// ── Adapter shims: G.I* -> neutral Impl ───────────────────────────────────────

internal struct AppAdapter(AppImpl impl) : G.IApp
{
    public void AddSystems(WitApp.Schedule schedule, ReadOnlySpan<G.ISystem> systems)
    {
        var neutral = new SystemImpl[systems.Length];
        for (var i = 0; i < systems.Length; i++)
            neutral[i] = ((SystemAdapter)systems[i]).Impl;
        impl.AddSystems(WitAppMapping.ToModSchedule(schedule.Discriminant), schedule.CustomPayload, neutral);
    }

    public void AddObserver(string name, WitApp.ObserverEvent evt)
    {
        switch (evt.Discriminant)
        {
            case WitApp.ObserverEvent.Case.Spawn: impl.AddObserver(name, ModObserverKind.Spawn, null); break;
            case WitApp.ObserverEvent.Case.Despawn: impl.AddObserver(name, ModObserverKind.Despawn, null); break;
            case WitApp.ObserverEvent.Case.Insert: impl.AddObserver(name, ModObserverKind.Insert, evt.InsertPayload); break;
            case WitApp.ObserverEvent.Case.Remove: impl.AddObserver(name, ModObserverKind.Remove, evt.RemovePayload); break;
            case WitApp.ObserverEvent.Case.Custom: impl.AddObserver(name, ModObserverKind.Custom, evt.CustomPayload); break;
            default: throw new NotSupportedException($"unmapped observer-event case {evt.Discriminant}");
        }
    }

    public void Dispose() { }
}

internal struct SystemAdapter(SystemImpl impl) : G.ISystem
{
    public SystemImpl Impl => impl;

    public void AddCommands() => impl.AddCommands();

    public void AddQuery(ReadOnlySpan<WitApp.QueryFor> query)
    {
        var terms = new ModQueryTerm[query.Length];
        for (var i = 0; i < query.Length; i++)
            terms[i] = WitAppMapping.ToModQueryTerm(query[i]);
        impl.AddQuery(terms);
    }

    public void After(G.ISystem other) => impl.After(((SystemAdapter)other).Impl);
    public void Before(G.ISystem other) => impl.Before(((SystemAdapter)other).Impl);
    public void Dispose() { }
}

internal struct CommandsAdapter(CommandsImpl impl) : G.ICommands
{
    public G.IEntityCommands SpawnEmpty() => new EntityCommandsAdapter(impl.SpawnEmpty());
    public G.IEntityCommands Spawn(ReadOnlySpan<(string, string)> bundle) => new EntityCommandsAdapter(impl.Spawn(bundle));
    public G.IEntityCommands Entity(G.IEntity entity) => new EntityCommandsAdapter(impl.Entity(((EntityAdapter)entity).Impl));
    public G.IEntityCommands EntityById(ulong id) => new EntityCommandsAdapter(impl.EntityById(id));
    public G.IEntityCommands SpawnTyped(ReadOnlySpan<WitApp.ComponentValue> bundle) => new EntityCommandsAdapter(impl.SpawnTyped(bundle));
    public string ResourceGet(string resource) => impl.ResourceGet(resource);
    public void ResourceSet(string resource, string value) => impl.ResourceSet(resource, value);
    public void InputConsumeMouse(byte button) => impl.InputConsumeMouse(button);
    public void InputConsumeKeyboard(uint key) => impl.InputConsumeKeyboard(key);
    public void EmitEvent(string name, ulong entity, string json) => impl.EmitEvent(name, entity, json);
    public void Dispose() { }
}

internal struct EntityCommandsAdapter(EntityCommandsImpl impl) : G.IEntityCommands
{
    public G.IEntity Id() => new EntityAdapter(impl.Id());
    public void Insert(ReadOnlySpan<(string, string)> bundle) => impl.Insert(bundle);
    public void InsertTyped(ReadOnlySpan<WitApp.ComponentValue> bundle) => impl.InsertTyped(bundle);
    public void Remove(ReadOnlySpan<string> bundle) => impl.Remove(bundle);
    public void AddChild(G.IEntity child, uint index) => impl.AddChild(((EntityAdapter)child).Impl, index);
    public void Despawn() => impl.Despawn();
    public void Dispose() { }
}

internal struct EntityAdapter(EntityImpl impl) : G.IEntity
{
    public EntityImpl Impl => impl;

    public ulong Id() => impl.Id();

    public G.IEntity? Parent()
    {
        var p = impl.Parent();
        return p.HasValue ? new EntityAdapter(p.Value) : null;
    }

    public string Get(string component) => impl.Get(component);

    public ReadOnlySpan<G.IEntity> Children()
    {
        var kids = impl.Children();
        var result = new G.IEntity[kids.Length];
        for (var i = 0; i < kids.Length; i++)
            result[i] = new EntityAdapter(kids[i]);
        return result;
    }

    public void Dispose() { }
}

internal struct QueryAdapter(QueryImpl impl) : G.IQuery
{
    public G.IQueryResult? Iter()
    {
        var r = impl.Iter();
        return r.HasValue ? new QueryResultAdapter(r.Value) : null;
    }

    public void Dispose() { }
}

internal readonly struct QueryResultAdapter(QueryResultImpl impl) : G.IQueryResult
{
    public G.IEntity Entity() => new EntityAdapter(impl.Entity());
    public G.IComponent Component(byte index) => new ComponentAdapter(impl.Component(index));
    public void Dispose() { }
}

internal readonly struct ComponentAdapter(ComponentImpl impl) : G.IComponent
{
    public string Get() => impl.Get();
    public void Set(string value) => impl.Set(value);
    public void SetTyped(WitApp.ComponentValue value) => impl.SetTyped(value);
    public void Dispose() { }
}

// ── Typed (component-value, no-JSON) methods — wasmtime-only ─────────────────
// The Jco path converts typed component-value to JSON JS-side and never calls
// these; keeping them here (rather than in GuestBridge.cs) means the neutral
// scheduler never needs to know WitApp.ComponentValue exists.

internal partial struct CommandsImpl
{
    // Typed spawn — components cross as native records (no JSON registry).
    public EntityCommandsImpl SpawnTyped(ReadOnlySpan<WitApp.ComponentValue> bundle)
    {
        var ent = ctx.World.Entity();
        ent.Set(new ModEntity { Slot = (byte)ctx.Slot });
        var id = ent.ID;
        foreach (var cv in bundle)
            ModTypedComponents.Apply(ctx.World, id, cv);
        return new EntityCommandsImpl(ctx, id);
    }
}

internal partial struct EntityCommandsImpl
{
    public void InsertTyped(ReadOnlySpan<WitApp.ComponentValue> bundle)
    {
        foreach (var cv in bundle)
            ModTypedComponents.Apply(ctx.World, entity, cv);
    }
}

internal partial struct ComponentImpl
{
    // Typed write — native record via component-value, no JSON. (Typed READ is
    // not exposed: the fork runtime mishandles variant-return ownership.)
    public void SetTyped(WitApp.ComponentValue value)
    {
        if (!mutable)
            ThrowNotMutableTyped(typePath);
        ModTypedComponents.Apply(ctx.World, entity, value);
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void ThrowNotMutableTyped(string typePath)
        => throw new InvalidOperationException($"component {typePath} was not declared mutable");
}

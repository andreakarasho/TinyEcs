// The string/JSON component+resource registry for the tinyecs:modding modding API.
// Maps a WIT `type-path` string to a concrete TinyEcs component / App resource +
// JSON (de)serialization, so a WASM mod can spawn/query/mutate host state by name.
// AOT-safe: every registered entry is a closed generic (no reflection); JSON goes
// through a host-supplied System.Text.Json source-gen JsonTypeInfo<T>.
//
// The host (e.g. the game) populates a ModComponentRegistry with the components
// and resources it chooses to expose, then hands it to the plugin via
// ModdingConfig. This file is the mechanism only — it knows no concrete game type.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Collections;

namespace TinyEcs.Bevy.Modding;

/// One registered component type, keyed by WIT type-path. All ECS access is
/// through the closed generic so there is no runtime reflection.
public interface IModComponent
{
    bool Has(World world, ulong entity);
    void CollectEntities(World world, ref PooledList<ulong> into);
    string GetJson(World world, ulong entity);
    void SetJson(World world, ulong entity, string json);
    void Remove(World world, ulong entity);
}

public sealed class ModComponent<T>(JsonTypeInfo<T> typeInfo) : IModComponent where T : struct
{
    // ponytail: cached on first use, not in the ctor — the ctor has no World.
    // Built once and reused; Query.Match() re-resolves archetypes lazily.
    // Single-world assumption (one registry per App/World).
    private Query? _query;

    public bool Has(World world, ulong entity) => world.Has<T>(entity);

    public void CollectEntities(World world, ref PooledList<ulong> into)
    {
        // QueryBuilder (by component id) instead of Query<Data<T>>: works for
        // zero-size marker components too, which Data<T> mis-casts.
        var q = _query ??= world.QueryBuilder().With<T>().Build();
        var it = q.Iter();
        while (it.Next())
            foreach (var ev in it.Entities())
                into.Add(ev.ID);
    }

    public string GetJson(World world, ulong entity)
        => JsonSerializer.Serialize(world.Get<T>(entity), typeInfo);

    public void SetJson(World world, ulong entity, string json)
        => world.Set(entity, JsonSerializer.Deserialize(json, typeInfo)!);

    public void Remove(World world, ulong entity) => world.Entity(entity).Unset<T>();
}

/// One registered singleton resource, keyed by WIT type-path. Mirrors
/// IModComponent but operates on App-owned Res<T> instead of per-entity columns.
public interface IModResource
{
    string GetJson(App app);
    void SetJson(App app, string json);
}

/// Plain struct/class resource (de)serialized whole via STJ. AOT-safe (closed
/// generic, source-gen JSON). Used for resources with a clean serializable shape.
public sealed class ModResource<T>(JsonTypeInfo<T> typeInfo) : IModResource where T : notnull
{
    public string GetJson(App app)
        => app.HasResource<T>() ? JsonSerializer.Serialize(app.GetResource<T>(), typeInfo) : "null";

    public void SetJson(App app, string json)
    {
        if (app.HasResource<T>())
            app.GetResourceRef<T>() = JsonSerializer.Deserialize(json, typeInfo)!;
    }
}

public sealed class ModComponentRegistry
{
    private readonly Dictionary<string, IModComponent> _byPath = new();
    private readonly Dictionary<string, IModResource> _resByPath = new();

    public void Register(string typePath, IModComponent component) => _byPath[typePath] = component;

    public bool TryGet(string typePath, [MaybeNullWhen(false)] out IModComponent component) => _byPath.TryGetValue(typePath, out component);

    public void RegisterResource(string typePath, IModResource resource) => _resByPath[typePath] = resource;

    public bool TryGetResource(string typePath, [MaybeNullWhen(false)] out IModResource resource) => _resByPath.TryGetValue(typePath, out resource);
}

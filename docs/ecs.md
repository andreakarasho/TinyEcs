# Pure ECS (`TinyEcs`)

The core `TinyEcs` assembly is a reflection-free, archetype-based entity
component system. It owns entities, components, archetype storage, queries,
and entity-tied observers — and nothing else. No stages, no scheduler, no
resources, no events; pick those up in `TinyEcs.Bevy` if you need them.

## Project requirements
- Targets: .NET 8.0 / 9.0 / 10.0
- No reflection in hot paths (no `GetType()`, no `GetProperty()`)
- Components are `struct`s; tags are zero-sized structs

## World

```csharp
var world = new World();

var entity = world.Entity();           // fresh entity
var named  = world.Entity("Player");   // create-or-fetch by name
var withId = world.Entity(1234);       // pin to a specific id

bool alive = entity.Exists();
entity.Delete();
```

Entities are 64-bit IDs with generation-based recycling.

## Components and tags

Components hold data. Tags are zero-sized structs used as markers — they
add no per-entity payload.

```csharp
struct Position { public float X, Y, Z; }
struct IsAlly { }

world.Entity()
    .Set(new Position { X = 0, Y = 1, Z = -1 })
    .Set<IsAlly>();   // tag — no allocation
```

| Operation         | API                                     |
|-------------------|------------------------------------------|
| Add / update      | `entity.Set(value)` / `Set<TTag>()`     |
| Remove            | `entity.Unset<T>()`                     |
| Read by ref       | `ref var p = ref entity.Get<Position>();` |
| Probe presence    | `entity.Has<T>()`                       |

`Get<T>` works only on data components, not tags.

## Archetypes

Entities that share the same component set live in the same archetype.
Adding or removing a component moves the entity to a new archetype. Storage
is columnar, so iteration over a query touches contiguous memory. You never
interact with archetypes directly.

## Queries

`TinyEcs` ships a runtime `QueryBuilder` that picks archetypes by term:

```csharp
var query = world.QueryBuilder()
    .With<Position>()
    .With<Velocity>()
    .Without<Frozen>()
    .Optional<Health>()
    .Build();

int total = query.Count();           // matched entity count

foreach (var it in query.Iter())     // chunk-at-a-time iteration
{
    // `it` is a QueryIterator: gives the current Archetype, chunk Count,
    // and per-term column access. Suitable for hot loops.
    // See src/TinyEcs/Query.cs for the full surface.
}
```

Available builder terms:
- `With<T>()` / `Without<T>()` — component or tag presence/absence.
- `With<TAction, TTarget>()` / `With<TAction>(EcsID target)` — relationship terms.
- `Optional<T>()` — include matching entities even if `T` is missing.
- `Term(IQueryTerm term)` — raw escape hatch.

This is a low-level API designed for performance. If you want a more
ergonomic `foreach (var (pos, vel) in q)` loop with filter combinators,
that's what the Bevy layer's `Data<...>` / `Filter<...>` query form is for.

## Change detection

Every component carries a "changed tick" that bumps each time the component
is written. Ticks advance per `World.Update()` call, so a system can
distinguish "modified since last frame" from "stale". The pure-ECS surface
exposes the tick via the iterator:

```csharp
foreach (var it in query.Iter())
{
    var ticks = it.GetChangedTicks(termIndex);   // Span<uint>
    // compare against your "last seen" tick
}
```

The Bevy layer wraps this into `Changed<T>` / `Added<T>` query filters.

## Relationships

`AddChild` / `RemoveChild` maintain `Parent` and `Children` components.
A child has one parent; a parent has many children.

```csharp
var root  = world.Entity();
var child = world.Entity();

root.AddChild(child);

ref var children = ref root.Get<Children>();
foreach (var c in children) { /* ... */ }

root.RemoveChild(child);
root.Delete();   // cascades to descendants
```

### Typed relationships

For multiple relation kinds, use `Parent<TKind>` / `Children<TKind>`. `TKind`
is any tag struct.

```csharp
struct Owns;
struct LikesEntity;

world.AddChild<Owns>(player, sword);
world.AddChild<LikesEntity>(player, npc);

EcsID owner   = world.GetParent<Owns>(sword);
var inventory = world.GetChildren<Owns>(player);
world.RemoveChild<Owns>(sword);
```

Default cleanup: typed `AddChild<TKind>` uses `UnlinkDescendants` on delete;
the non-generic `AddChild` uses `DeleteDescendants`.

## Entity-tied observers

`World` stores observers attached to a specific entity as a component on
that entity. They are reflection-free, survive across frames, and clean up
automatically when the entity despawns. Useful for per-entity reactive
logic — e.g. respond when *this* button changes.

The Bevy layer surfaces this through `EntityCommands.Observe<TTrigger>(...)`
on top of standard triggers (`OnSpawn`, `OnInsert<T>`, etc); pure-ECS users
who want this functionality usually depend on `TinyEcs.Bevy`.

## Scope

`TinyEcs` is intentionally minimal. The following live one layer up in
`TinyEcs.Bevy`:

- Stages, system ordering, threading modes
- `Commands`, `Res<T>` / `ResMut<T>`, `EventReader<T>` / `EventWriter<T>`, `Local<T>`
- States, plugins, bundles
- Global / app-wide observer registration

If a feature isn't in this document, it's a Bevy-layer feature.

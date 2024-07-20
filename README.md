# TinyEcs

[![NuGet Version](https://img.shields.io/nuget/v/TinyEcs.Main?label=TinyEcs)](https://www.nuget.org/packages/TinyEcs.Main)

TinyEcs: a reflection-free dotnet ECS library, born to meet your needs.

## Key Features

-   Fast
-   Reflection-free design
-   NativeAOT & bflat support
-   Zero runtime allocations
-   Compatible with major game engines/frameworks: Unity, Godot, Monogame, FNA, Raylib-cs, etc.
-   Relationships support
-   `Bevy systems` concept

## Requirements

-   `netstandard2.1`
-   `net8.0`

## Status

🚧 Early development stage: Expect breaking changes! 🚧

## Run the pepe game!

```bash
cd samples/TinyEcsGame
dotnet run -c Release
```

## Basic Samples

```csharp
using var ecs = new World();

// Generate entities
var player = ecs.Entity()
    .Set<Position>(new Position { X = 2 })
    .Set<Label>(new Label { Value = "Tom" })
    .Add<Player>();

var npc = ecs.Entity()
    .Set<Position>(new Position { X = 75 })
    .Set<Label>(new Label { Value = "Dan" })
    .Add<Npc>();

// Query entities with Position + Label components
ecs.Query<(Position, Label)>()
    .Each((EntityView entity, ref Position pos, ref Label label) => {
        Console.WriteLine(label.Value);
    });

// Multi-threaded query for entities with Position + Label + Player, without Npc.
ecs.Query<(Position, Label), (With<Player>, Without<Npc>)>()
    .EachJob((ref Position pos, ref Label label) => {
        Console.WriteLine(label.Value);
    });

// Component structs
struct Position { public float X, Y, Z; }
struct Label { public string Value; }
struct Player { }
struct Npc { }
```

## Bevy Systems

Organize your application using the "Bevy systems" concept.

```csharp
using var ecs = new World();
var scheduler = new Scheduler(ecs);

scheduler.AddSystem((World world) => {
    // Spawn entities
}, SystemStages.Startup);

scheduler.AddSystem((Query<(Position, Velocity), Without<Npc>> query) => {
    foreach ((var entities, var posSpan, var velSpan) in query.Iter<Position, Velocity>() {
        // parse all spans
    }
});

scheduler.AddPlugin<MyPlugin>();

scheduler.AddSystem((Res<string> myText) => Console.WriteLine(myText.Value))
    .RunIf((SchedulerState schedState) => schedState.ResourceExists<string>());
scheduler.AddResource("My text");

// Run all systems once
scheduler.Run();


struct MyPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddSystem((World world, Local<int> i32) => {
            // Do something
        });

        scheduler.AddSystem((EventWriter<MyEvent> writer) => {
            // Write events
        });

        scheduler.AddSystem((EventReader<MyEvent> reader) => {
            // Read events
        });

        scheduler.AddEvent<MyEvent>();
    }

    struct MyEvent { }
}
```

## More Functionalities

Access entity data, deferred operations, raw queries, and multithreading.

```csharp
// Entity data access
ref var pos = ref entity.Get<Position>();
bool hasPos = entity.Has<Position>();
entity.Unset<Position>();

// Deferred operations
world.Deferred(w => {
    // Operations
});
world.BeginDeferred();
// Operations
world.EndDeferred();

// Raw queries
var query = world.Query<(Position, Velocity), Without<Npc>>();
foreach (var archetype in query) {
    foreach (ref var chunk in archetype) {
        // Operations
    }
}
foreach ((var entities, var posSpan, var velSpan) in query.Iter<Position, Velocity>() {
    // Operations
}
query.Each((ref Position pos, ref Velocity vel) => {
    // Operations
});
query.EachJob((ref Position pos, ref Velocity vel) => {
    // Operations
});
```

## Unique/Named entities

```csharp
// Get or create an entity named 'Khun' 🐶
var dog = ecs.Entity("Khun")
    .Add<Bau>();
```

```csharp
// Retrive already-registered components using their names
// [Might not work on NativeAOT!]
var entity = ecs.Entity<Apples>();
var applesComponent = ecs.Entity("Apples");

struct Apples { public int Amount; }
```

## Relationships

```csharp
var woodenChest = ecs.Entity()
    .Set<Container>();

var sword = ecs.Entity()
    .Add<Weapon>()
    .Set<Damage>(new Damage { Min = 5, Max = 15 })
    .Set<Amount>(new Amount { Value = 1 });

// This will create a relationship like (Contents, sword)
woodenChest.Set<Contents>(sword);

// Grab all relationships of type (Contents, *)
ecs.Query<ValueTuple, With<(Contents, Wildcard)>>().Each((EntityView entity) =>
    Console.WriteLine($"I'm {entity.ID} and I'm a child of the wooden chest!"));
```

### Add same component multiple times

Relations open the scenario to assign the same component more than a single time

```csharp
var player = ecs.Entity();
player.Set<BeginPoint, Position>(new Position() { Value = Vector3.Zero; });
player.Set<EndPoint, Position>(new Position() { Value = { X = 10, Y = 35, Z = 0 }; });

// Will retrive the begin position {0, 0, 0}
ref var beginPos = ref player.Get<BeginPoint, Poisition>();

// Will retrive the end position {10, 35, 0}
ref var endPos = ref player.Get<EndPoint, Poisition>();

// Queries for begin & end positions!
// Notice that relationships are rappresented by a Tuple(A, B)
ecs.Query<(Relation<BeginPoint, Position>, Relation<EndPoint, Position>)>()
    .Each((ref Relation<BeginPoint, Position> begin, ref Relation<EndPoint, Position> end) => {
    // ...
});

struct Position { public Vector3 Value; }
struct BeginPoint { }
struct EndPoint { }
```

### Assign entities to entities

```csharp
var bob = ecs.Entity("Bob");
var likes = ecs.Entity("Likes");
var pasta = ecs.Entity("Pasta");

bob.Add(likes, pasta);
```

### `ChildOf`

Use the pre-build `ChildOf` relationship.
This tag is marked as 'Unique' which means cannot exists more than one `(ChildOf, *)` relation attached to the child entity.
The `parent.AddChild(child)` function it's a shortcut of `child.Set<ChildOf>(parent)`;

```csharp
var parent0 = ecs.Entity();
var parent1 = ecs.Entity();
var child = ecs.Entity();

// Attach (ChildOf, parent0) to child
parent0.AddChild(child);

// Detach any (ChildOf, *) from child
// and attach (ChildOf, parent1) to child
parent1.AddChild(child);
```

### `Symmetric`

`Symmetric` is a pre-build relationship which assign the relation to the target too:
`A.Set(Rel, B) <=> B.Set(Rel, A)`

```csharp
// Set the tag as symmetric
ecs.Entity<TradingWith>().Set<Symmetric>();

var playerA = ecs.Entity("Player A");
var playerB = ecs.Entity("Player B");

playerA.Add<TradingWith>(playerB);

// Both returns 'True'
var resultA = playerA.Has<TradingWith>(playerB);
var resultB = playerB.Has<TradingWith>(playerA);

struct TradingWith { }
```

## Bechmarks:
-   [friflo - ECS.CSharp.Benchmark-common-use-cases](https://github.com/friflo/ECS.CSharp.Benchmark-common-use-cases/tree/main?tab=readme-ov-file#feature-matrix)

## Credits

Inspired by:

-   [entity-component-system](https://github.com/jasonliang-dev/entity-component-system)
-   [flecs](https://github.com/SanderMertens/flecs)
-   [bevy](https://github.com/bevyengine/bevy)

## Cool Design Reference

-   [flecs Manual](https://github.com/SanderMertens/flecs/blob/master/docs/Manual.md)

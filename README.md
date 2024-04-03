# TinyEcs

[![NuGet Version](https://img.shields.io/nuget/v/TinyEcs.Main?label=TinyEcs)](https://www.nuget.org/packages/TinyEcs.Main)
[![NuGet Version](https://img.shields.io/nuget/v/TinyEcs.Plugins?label=TinyEcs.Plugins)](https://www.nuget.org/packages/TinyEcs.Plugins)

TinyEcs: a reflection-free dotnet ECS library, born to meet your needs.

## Key Features

-   Reflection-free design
-   NativeAOT & bflat compatibility
-   Compatible with major game engines/frameworks: Unity, Godot, Monogame, FNA, Raylib-cs, etc.

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
    .Set<Name>(new Name { Value = "Tom" })
    .Set<Player>();

var npc = ecs.Entity()
    .Set<Position>(new Position { X = 75 })
    .Set<Name>(new Name { Value = "Dan" })
    .Set<Npc>();

// Query entities with Position + Name components
ecs.Each((EntityView entity, ref Position pos, ref Name name) => {
    Console.WriteLine(name.Value);
});

// Multi-threaded query for entities with Position + Name + Player, without Npc.
ecs.EachJob<(Position, Name), (With<Player>, Not<Npc>)>((ref Position pos, ref Name name) => {
    Console.WriteLine(name.Value);
});

// Component structs
struct Position { public float X, Y, Z; }
struct Name { public string Value; }
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

scheduler.AddSystem((Query<(Position, Velocity), Not<Npc>> query) => {
    // Query execution
});

scheduler.AddSystem((Res<string> myText) => Console.WriteLine(myText.Value))
    .RunIf((SchedulerState schedState) => schedState.ResourceExists<string>());
scheduler.AddResource("My text");

// Run all systems once
scheduler.Run();
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
foreach (var archetype in world.Query<(Position, Velocity), Not<Npc>>()) {
    // Operations
}

// Multithreading
world.EachJob<(A, B), (With<C>, Not<D>)>(...);
```

## Plugins

Enhance functionality with plugins like relationships, unique entities, and entity enable/disable.

```csharp
// Relationships
var woodenChest = ecs.Entity()
    .Set<Container>();

var sword = ecs.Entity()
    .Set<Weapon>()
    .Set<Damage>(new Damage { Min = 5, Max = 15 })
    .Set<Amount>(new Amount { Value = 1 });

woodenChest.AddChild<ChestContainer>(sword);

ecs.Each<With<Child<ChestContainer>>>((EntityView entity) =>
    Console.WriteLine($"I'm {entity.ID} and I'm a child of the wooden chest!"));

// Unique entities
var dog = ecs.Entity("Khun").Set<Bau>();

// Enable/Disable entities
var ent = ecs.Entity();
ent.Disable();
ent.Enable();
bool isEnabled = ent.IsEnabled();

ecs.Each<Not<Disabled>>((EntityView entity) => Console.WriteLine("entity {0}", entity.ID));
```

## Credits

Inspired by:

-   [entity-component-system](https://github.com/jasonliang-dev/entity-component-system)
-   [flecs](https://github.com/SanderMertens/flecs)
-   [bevy](https://github.com/bevyengine/bevy)

## Cool Design Reference

-   [flecs Manual](https://github.com/SanderMertens/flecs/blob/master/docs/Manual.md)

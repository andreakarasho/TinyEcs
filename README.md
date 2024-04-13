# TinyEcs

[![NuGet Version](https://img.shields.io/nuget/v/TinyEcs.Main?label=TinyEcs)](https://www.nuget.org/packages/TinyEcs.Main)

TinyEcs: a reflection-free dotnet ECS library, born to meet your needs.

## Key Features

-   Fast
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
    .Set<Label>(new Label { Value = "Tom" })
    .Set<Player>();

var npc = ecs.Entity()
    .Set<Position>(new Position { X = 75 })
    .Set<Label>(new Label { Value = "Dan" })
    .Set<Npc>();

// Query entities with Position + Label components
ecs.Each((EntityView entity, ref Position pos, ref Label label) => {
    Console.WriteLine(label.Value);
});

// Multi-threaded query for entities with Position + Label + Player, without Npc.
ecs.EachJob<(Position, Label), (With<Player>, Not<Npc>)>((ref Position pos, ref Label label) => {
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

scheduler.AddSystem((Query<(Position, Velocity), Not<Npc>> query) => {
    // Query execution
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
foreach (var archetype in world.Query<(Position, Velocity), Not<Npc>>()) {
    // Operations
}

// Multithreading
world.EachJob<(A, B), (With<C>, Not<D>)>(...);
```

## Relationships

```csharp
var woodenChest = ecs.Entity()
    .Set<Container>();

var sword = ecs.Entity()
    .Set<Weapon>()
    .Set<Damage>(new Damage { Min = 5, Max = 15 })
    .Set<Amount>(new Amount { Value = 1 });

// This will create a relationship like (Contents, sword)
woodenChest.Set<Contents>(sword);

// Grab all relationships of type (Contents, *)
ecs.Each<With<(Contents, Wildcard)>>((EntityView entity) =>
    Console.WriteLine($"I'm {entity.ID} and I'm a child of the wooden chest!"));
```

Assign entities to entities

```csharp
var bob = ecs.Entity("Bob");
var likes = ecs.Entity("Likes");
var pasta = ecs.Entity("Pasta");

bob.Set(likes, pasta);
```

Use the pre-build `ChildOf` relationship.
This tag is marked as 'Unique' which means cannot exists more than one `(ChildOf, *)` relation attached to the entity.

```csharp
var root0 = ecs.Entity();
var root1 = ecs.Entity();
var child = ecs.Entity();

// Attach (ChildOf, root0) to child
root0.AddChild(child);

// Detach any (ChildOf, *) from child
// and attach (Child=f, root1) to child
root1.AddChild(child)
```

## Unique/Named entities

```csharp
// Get or create an entity named 'Khun' 🐶
var dog = ecs.Entity("Khun")
	.Set<Bau>();
```

```csharp
// Retrive already-registered components using their names
// [Might not work on NativeAOT!]
var entity = ecs.Entity<Apples>();
var applesComponent = ecs.Entity("Apples");

struct Apples { public int Amount; }
```

## Credits

Inspired by:

-   [entity-component-system](https://github.com/jasonliang-dev/entity-component-system)
-   [flecs](https://github.com/SanderMertens/flecs)
-   [bevy](https://github.com/bevyengine/bevy)

## Cool Design Reference

-   [flecs Manual](https://github.com/SanderMertens/flecs/blob/master/docs/Manual.md)

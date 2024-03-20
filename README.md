# TinyEcs

[![NuGet Version](https://img.shields.io/nuget/v/TinyEcs.Main?label=TinyEcs)](https://www.nuget.org/packages/TinyEcs.Main)
[![NuGet Version](https://img.shields.io/nuget/v/TinyEcs.Plugins?label=TinyEcs.Plugins)](https://www.nuget.org/packages/TinyEcs.Plugins)

Small project born by the need of a reflection-free dotnet ECS library.<br>
NativeAOT & bflat compatible.
Compatible with all major game engines/frameworks Unity, Godot, Monogame, FNA, Raylib-cs, etc...

# Requirements

`netstandard2.1` `net8.0`

# Status

<i>This project is in an early development stage. Breaking changes are real :D</i> `¯\_(ツ)_/¯`

# Run the pepe game!

```
cd samples/TinyEcsGame
dotnet run -c Release
```

# Basic samples

```csharp
using var ecs = new World();

// Generate a player entity
var player = ecs.Entity()
   .Set(new Position() { X = 2 })
   .Set(new Name() { Name = "Tom" })
   .Set<Player>();

// Generate a npc entity
var npc = ecs.Entity()
   .Set(new Position() { X = 75 })
   .Set(new Name() { Name = "Dan" })
   .Set<Npc>();

// Query for all entities with [Position + Name] and the entity
ecs.Query((EntityView entity, ref Position pos, ref Name name) => {
    Console.WriteLine(name.Vaue);
});

// Query for all entities with [Position + Name + Player], without [Npc]
ecs.Filter<(Player, Not<Npc>)>()
   .Query((ref Position pos, ref Name name) => {
        Console.WriteLine(name.Vaue);
});


struct Position { public float X, Y, Z; }
struct Name { public string Value; }
struct Player { }
struct Npc { }
```

Bevy systems :D

```csharp
using var ecs = new World();
var scheduler = new Scheduler(ecs);

scheduler
    // Arguments oderd doesn't matter!
    .AddSystem((Commands commands, Query<(Position, Velocity), Not<Npc>> query) => {
        // query execution
        query.Each((ref Position pos, ref Velocity vel) => {

        });

        // spawn a deferred entity
        commands.Entity()
            .Set<Position>(default)
            .Set<Velocity>(default);
    })

    // Merge the deferred commands to the world
    .AddSystem((Commands commands) => commands.Merge())

    // Res<> is a special type which stores any value you want
    .AddSystem((Res<string> myText) => Console.WriteLine(myText.Value))

    // Add an unique resource type which can be invoked as system argument
	// In this case it's Res<string>
    .AddResource("My text");

// Run all systems once
scheduler.Run();
```

# More functionalities

Access to the entity data

```csharp
ref var pos = ref entity.Get<Position>();
ref var pos = ref world.Get<Position>(entity);

bool hasPos = entity.Has<Position>();
bool hasPos = world.Has<Position>(entity);

entity.Unset<Position>();
world.Unset<Position>(entity);
```

Advanced queries

```csharp
foreach (var archetype in world.Filter<(With<Position>, With<Velocity>, Not<Npc>)>())
{
	var posIndex = archetype.GetComponentIndex<Position>();
	var velIndex = archetype.GetComponentIndex<Velocity>();

	foreach (ref readonly var chunk in archetype)
	{
		var posSpan = chunk.GetSpan<Position>(posIndex);
		var velSpan = chunk.GetSpan<Velocity>(velIndex);

		for (var i = 0; i < chunk.Count; ++i)
		{
			// ...
		}
	}
}
```

# Plugins

Relationships

```csharp
// We can define a custom hierarchy
//  so we can handle multiple parent-child
//  relationships at the same time
struct ChestContainer {}

// Spawn the container
var woodenChest = ecs.Entity()
   .Set<Container>();

// Spawn the chest content
var sword = ecs.Entity()
    .Set<Weapon>()
    .Set<Damage>(new () { Min = 5, Max = 15 });
    .Set<Amount>(new () { Value = 1 });

var goldCoins = ecs.Entity()
    .Set<Gold>()
    .Set<Amount>(new () { Value = 245 });

var silverCoins = ecs.Entity()
    .Set<Silver>()
    .Set<Amount>(new () { Value = 874 });

// Add items to the woodenChest
woodenChest.AddChild<ChestContainer>(sword);
woodenChest.AddChild<ChestContainer>(goldCoins);
woodenChest.AddChild<ChestContainer>(silverCoins);

// Query for all children that have a 'ChestContainer' relationship
ecs.Filter<With<Child<ChestContainer>>>()
   .Query((EntityView entity) => {
       Console.WriteLine($"I'm {entity.ID} and I'm a child of the wooden chest!");
   });
```

Unique entities

```csharp
// create or get an entity named 'Khun'
var dog = ecs.Entity("Khun");
dog.Set<Bau>(); 🐶
```

Enable/Disable entities

```csharp
var ent = ecs.Entity();
ent.Disable();
ent.Enable();
bool isEnabled = ent.IsEnabled();

// `Disabled` is a simple built-in component!
ecs.Filter<Not<Disabled>>()
	.Query((EntityView entity) => { });
```

# Credits

Base code idea inspired by:

-   https://github.com/jasonliang-dev/entity-component-system
-   https://github.com/SanderMertens/flecs
-   https://github.com/bevyengine/bevy

# Cool design reference

-   https://github.com/SanderMertens/flecs/blob/master/docs/Manual.md

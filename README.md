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

// Query for all entities with [Position + Name] and access the entity associated
ecs.Each((EntityView entity, ref Position pos, ref Name name) => {
    Console.WriteLine(name.Vaue);
});

// Query, using a multithread strategy, for all entities with [Position + Name + Player], without [Npc].
// The first tuple (Position, Name) is the accessing data,
//   the 2nd tuple (With<Player>, Not<Npc>) is the filter.
ecs.EachJob<(Position, Name), (With<Player>, Not<Npc>)>((ref Position pos, ref Name name) => {
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


// Create a one-shot system
scheduler.AddSystem((World world) => {
   // spawn some deferred entities
    world.BeginDeferred();
    for (var i = 0; i < 1000; ++i)
        world.Entity()
            .Set<Position>(default)
            .Set<Velocity>(default);
    world.EndDefered();
}, SystemStages.Startup);


// Arguments order doesn't matter!
scheduler.AddSystem((Query<(Position, Velocity), Not<Npc>> query) => {
	// query execution
	query.Each((ref Position pos, ref Velocity vel) => {
		// do something here
	});

	// Same query, but using more threads!
	query.EachJob((ref Position pos, ref Velocity vel) => {
		// do something here
	});
});

// Res<> is a special type which stores any value you want
scheduler.AddSystem((Res<string> myText) => Console.WriteLine(myText.Value))
    // Run the system only if the 'string' resource exists
    .RunIf((SchedulerState schedState) => schedState.ResourceExists<string>());

// Add an unique resource type which can be invoked as system argument
// In this case it's Res<string>
scheduler.AddResource("My text");

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
foreach (var archetype in world.Query<(Position, Velocity), Not<Npc>>())
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
ecs.Each<With<Child<ChestContainer>>>((EntityView entity) =>
    Console.WriteLine($"I'm {entity.ID} and I'm a child of the wooden chest!"));
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
ecs.Each<Not<Disabled>>((EntityView entity) => Console.WriteLine("entity {0}", entity.ID));
```

# Credits

Base code idea inspired by:

-   https://github.com/jasonliang-dev/entity-component-system
-   https://github.com/SanderMertens/flecs
-   https://github.com/bevyengine/bevy

# Cool design reference

-   https://github.com/SanderMertens/flecs/blob/master/docs/Manual.md

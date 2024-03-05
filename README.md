# TinyEcs

Small project born by the need of a reflection-free dotnet ECS library.<br>
NativeAOT & bflat compatible.

# Requirements

`netstandard2.1` `net8.0`

# Status

<i>This project is in an early development stage. Breaking changes are real :D</i> `¯\_(ツ)_/¯`

# Run the pepe game!

```
cd samples/TinyEcsGame
dotnet run -c Release
```

# Basic sample

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
// Spawn the container
var woodenChest = ecs.Entity()
   .Set<Container>();

var sword = ecs.Entity()
    .Set<Weapon>()
    .Set<Damage>(new () { Min = 5, Max = 15 });
    .Set<Amount>(new () { Value = 1 });
var goldCoins = ecs.Entity()
    .Set<Gold>()
    .Set<Amount>(new () { Value = 245 })
var silverCoins = ecs.Entity()
    .Set<Silver>()
    .Set<Amount>(new () { Value = 874 })

// Add contents to the woodenChest
woodenChest.AddChild(sword);
woodenChest.AddChild(goldCoins);
woodenChest.AddChild(silverCoins);
```

Unique entities
```csharp
// create or get an entity named 'Khun'
var dog = ecs.Entity("Khun");
dog.Set<Bau>(); 🐶
```


# Credits

Base code idea inspired by:

-   https://github.com/jasonliang-dev/entity-component-system
-   https://github.com/SanderMertens/flecs
-   https://github.com/bevyengine/bevy

# Cool design reference

-   https://github.com/SanderMertens/flecs/blob/master/docs/Manual.md

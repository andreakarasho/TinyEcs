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
ecs.Entity()
	.Set(new Position() { X = 2 })
	.Set(new Name() { Name = "Tom" })
	.Set<Player>();

// Generate a npc entity
ecs.Entity()
	.Set(new Position() { X = 75 })
	.Set(new Name() { Name = "Dan" })
	.Set<Npc>();

// Query for all entities with [Position + Name]
ecs.Query()
	.Each((ref Position pos, ref Name name) => {
		Console.WriteLine(name.Vaue);
	});

// Query for all entities with [Position + Name + Player]
ecs.Query()
	.With<Player>()
	.Each((ref Position pos, ref Name name) => {
		Console.WriteLine(name.Vaue);
	});


struct Position { public float X, Y, Z; }
struct Name { public string Value; }
struct Player { }
struct Npc { }
```

# Credits

Base code idea inspired by:

-   https://github.com/jasonliang-dev/entity-component-system
-   https://github.com/SanderMertens/flecs
-   https://github.com/bevyengine/bevy

# Cool design reference

-   https://github.com/SanderMertens/flecs/blob/master/docs/Manual.md

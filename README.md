# TinyEcs

Small project born by the need of a reflection-free dotnet ECS library.<br>
NativeAOT & bflat compatible.

# Requirements

`net7.0+` atm

# Status

<i>This project is in an early development stage. Breaking changes are real :D</i> `¯\_(ツ)_/¯`

# Run the pepe game!

```
cd src/TinyEcs.MonogameSample
dotnet run -c Release
```

# Basic sample

```csharp
using var ecs = new World();

// Create the components & tags
var posID = ecs.Entity<Position>();
var playerTag = ecs.Entity<Player>();
var npcTag = ecs.Entity<Npc>();

// Create a run-once system which will run the first time
// the app calls "ecs.Step(deltaTime)" method
ecs.Entity()
		.System(&Setup)
		.Set<EcsPhase, EcsSystemPhaseOnUpdate>();

// Create a frame which will run every frame
// if the query condition is satisfied
ecs.Entity()
	.System(&MovePlayer, posID, playerTag)
	.Set<EcsPhase, EcsSystemPhaseOnUpdate>();

// Create a system that runs every second
ecs.Entity()
	.System(&MessageEverySecond, 1f)
	.Set<EcsPhase, EcsSystemPhaseOnUpdate>();


// Run the loop!
var sw = Stopwatch.StartNew();
while (true)
    ecs.Step((float) sw.ElapsedMilliseconds);



static void Setup(ref Iterator it)
{
    // Create an empty entity into the main world.
    // All changes to the entity are done without modifing the world.
    // Commands will get merged at the end of the frame.
    it.Commands.Entity()
        .Set(new Position() { X = -12f, Y = 0f, Z = 75f })
        .Set<Player>();

	it.Commands.Entity()
		.Set(new Position() { X = 0f, Y = 0f, Z = 20f })
		.Set<Npc>();
}

static void MovePlayer(ref Iterator it)
{
    var posSpan = it.Field<Position>(0);

    for (int i = 0; i < it.Count; ++i)
    {
        ref var pos = ref posSpan[i];
        pos.X += 0.5f * it.DeltaTime;
    }
}

static void MessageEverySecond(ref Iterator it)
{
	Console.WriteLine("message!");
}


struct Position { public float X, Y, Z; }
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

# TinyEcs
Small project born by the need of a reflection-free dotnet ECS library.<br>
NativeAOT compatible.

# Requirements
`net7.0+` atm

# Status
<i>Semi-production ready.</i>  `¯\_(ツ)_/¯`

# Run the pepe game!
```
cd src/TinyEcs.MonogameSample
dotnet run -c Release
```

# Basic sample
```csharp
var ecs = new World();

// Create a run-once system which will run the first time
// the app calls "ecs.Step(deltaTime)" method
ecs.StartupSystem(&Setup);

// Create a frame which will run every frame 
// if the query condition is satisfied
ecs.System
(
	&MovePlayer, 
	ecs.Query()
		.With<Position>()
		.With<Player>()
);

// Create a system that runs every second
ecs.System(&MessageEverySecond, 1f);


while (true)
    ecs.Step(deltaTime);


static void Setup(Iterator it)
{
    // Create an empty entity into the main world.
    // All changes to the entity are done without modifing the world.
    // Commands will get merged at the end of the frame.
    var ent = it.Commands.Entity()
        .Set(new Position() { X = -12f, Y = 0f, Z = 75f })
        .Set<Player>();
}

static void MovePlayer(Iterator it)
{
    var posSpan = it.Field<Position>();
    
    for (int i = 0; i < it.Count; ++i)
    {
        ref var pos = ref posSpan[i];
        pos.X += 0.5f;     
    }
}

static void MessageEverySecond(Iterator it)
{
	Console.WriteLine("message!");
}


struct Position : IComponent { public float X, Y, Z; }
struct Player : ITag { }
```

# Credits
Base code idea inspired by:
- https://github.com/jasonliang-dev/entity-component-system
- https://github.com/SanderMertens/flecs
- https://github.com/bevyengine/bevy

# Cool design reference
- https://github.com/SanderMertens/flecs/blob/master/docs/Manual.md

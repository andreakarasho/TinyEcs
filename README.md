# TinyEcs
Small project borns by the need of a reflection-free dotnet ECS library.<br>
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
var ecs = new Ecs();

// Create a run-once system which will run the first time
// the app calls "ecs.Step(deltaTime)" method
ecs.AddStartupSystem(&Setup);

// Create a frame which will run every frame 
// if the query condition is satisfied
ecs.AddSystem(&MovePlayer)
    .SetQuery(
        ecs.Query()
            .With<Position>()
            .With<Player>()
            .ID
    );


while (true)
    ecs.Step(deltaTime);


static void Setup(Commands commands, ref EntityIterator it)
{
    // Create an empty entity into the main world.
    // All changes to the entity are done in a secondary world 
    // which will get merged at the end of the frame
    var ent = commands.Spawn()
        .Set(new Position() { X = -12f, Y = 0f, Z = 75f })
        .Set<Player>();
}

static void MovePlayer(Commands commands, ref EntityIterator it)
{
    var posSpan = it.Field<Position>();
    
    for (int i = 0; i < it.Count; ++i)
    {
        ref var pos = ref posSpan[i];
        pos.X += 0.5f;     
    }
}

struct Position { float X, Y, Z; }
struct Player { }
```

# Credits
Base code idea inspired by:
- https://github.com/jasonliang-dev/entity-component-system
- https://github.com/SanderMertens/flecs
- https://github.com/bevyengine/bevy

# Cool design reference
- https://github.com/SanderMertens/flecs/blob/master/docs/Manual.md

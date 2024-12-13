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

-   `net9.0`

## Status

🚧 Early development stage: Expect breaking changes! 🚧

## Run the pepe game!

```bash
cd samples/TinyEcsGame
dotnet run -c Release
```

## Sample code

This is a very basic example and doen't show the whole features set of this library.

```csharp
using var world = new World();
var scheduler = new Scheduler(world);

// create the Time variable accessible globlly by any system which stay fixed at 60fps
scheduler.AddResource(new Time() { FrameTime = 1000.0f / 60.0f });
scheduler.AddResource(new AssetManager());

var setupSysFn = Setup;
scheduler.AddSystem(setupSysFn);

var moveSysFn = MoveEntities;
scheduler.AddSystem(moveSysFn);

var countSomethingSysFn = CountSomething;
scheduler.AddSystem(countSomethingSysFn);


while (true)
    scheduler.Run();


// the variable 'assets' is globally accessible by any system
void Setup(World world, Res<AssetManager> assets)
{
    // spawn an entity and attach some components to it
    world.Entity()
        .Set(new Position() { X = 20f, Y = 9f  })
        .Set(new Velocity() { X = 1f, Y = 1.3f });

    var texture = new Texture(0, 2, 2);
    texture.SetData(new byte[] { 0, 0, 0, 0 });
    assets.Register("image.png", texture);
}

// query for [Position + Velocity] components
// the variable 'time' is globally accessible by any system
void MoveEntities(Query<Data<Position, Velocity>> query, Res<Time> time)
{
    foreach ((Ptr<Position> pos, Ptr<Velocity> vel) in query)
    {
        pos.Ref.X += vel.Ref.X * time.Value.FrameTime;
        pos.Ref.X += vel.Ref.Y * time.Value.FrameTime;
    }
}

// the variable 'localCounter' will exist only in this system context,
// which means declaring a `Local<int>` into another system will create a new variable.
void CountSomething(Local<int> localCounter, Res<Time> time)
{
    localCounter.Value += 1;
}


struct Position { public float X, Y; }
struct Velocity { public float X, Y; }

class Time
{
    public float FrameTime;
}

class Texture
{
    public Texture(int id, int width, int height)
    {
        Id = id;
        Width = width;
        Height = height;
    }

    public int Id { get; }
    public int Width { get; }
    public int Height { get; }

    public void SetData(byte[] data)
    {
        // ...
    }
}

class AssetManager
{
    private readonly Dictionary<string, Texture> _assets = new ();

    public void Register(string name, Texture texture)
    {
        _assets[name] = texture;
    }

    public Texture? Get(string name)
    {
        _assets.TryGetValue(name, out var texture);
        return texture;
    }
}

```

## Bechmarks

-   [friflo - ECS.CSharp.Benchmark-common-use-cases](https://github.com/friflo/ECS.CSharp.Benchmark-common-use-cases/tree/main?tab=readme-ov-file#feature-matrix)

## Credits

Inspired by:

-   [entity-component-system](https://github.com/jasonliang-dev/entity-component-system)
-   [flecs](https://github.com/SanderMertens/flecs)
-   [bevy](https://github.com/bevyengine/bevy)

## Cool Design Reference

-   [flecs Manual](https://github.com/SanderMertens/flecs/blob/master/docs/Manual.md)

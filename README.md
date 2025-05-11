# TinyEcs

[![NuGet Version](https://img.shields.io/nuget/v/TinyEcs.Main?label=TinyEcs)](https://www.nuget.org/packages/TinyEcs.Main)

TinyEcs: a reflection-free dotnet ECS library, born to meet your needs.

## Key Features

-   Fast
-   Reflection-free design
-   NativeAOT & bflat support
-   Zero runtime allocations
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

## Documentation

### Create an entity

```csharp
var world = new World();
EntityView entity = world.Entity();
```

### Delete an entity

```csharp
var world = new World();
EntityView entity = world.Entity();
entity.Delete(); // or world.Delete(entity);
```

### Entity exists

```csharp
bool exists = entity.Exists(); // or world.Exists(entity);
```

### Set component
Components are the real data that an entity contains. An array will be allocated per component. So you can grab the data using the `world.Get<T>()` api.
Requirements:
- must be a `struct`
- must contains one field at least
```csharp
EntityView entity = world.Entity()
    .Set(new Position() { X = 0, Y = 1, Z = -1 });

struct Position { public float X, Y, Z; }
```

### Add tag
Tags are used to describe an entity. No data will get allocated when adding a tag. Tags are not accessible from the `world.Get<T>()` api.
Requirements:
- must be a `struct`
- must be empty
```csharp
EntityView entity = world.Entity()
    .Add<IsFruit>();

struct IsFruit;
```

### Unset component/tag

```csharp
entity.Unset<IsFruit>()
      .Unset<Position>();
```

### Has component/tag

```csharp
bool isFruit = entity.Has<IsFruit>();
bool hasPosition = entity.Has<Position>();
```

### Get component
Attention: you can query for a non empty component only!
```csharp
ref Position pos = ref entity.Get<Position>(); // or world.Get<Position>(entity);
```

## Scheduler
The scheduler class is highly ispired by the [bevy scheduler concept](https://bevy-cheatbook.github.io/programming/schedules.html).
This is the real deal for modern game engines which want to implement their game beahviour fast and easy.

### Create a scheduler
A scheduler can handle one world only.
```csharp
var world = new World();
var scheduler = new Scheduler(world);
```
### Run a scheduler
Control each tick using
```csharp
while (!exit) {
    scheduler.RunOnce();
}
```
or just run until a certain condition is met
```csharp
var exitCalledFn = ExitCalled;
scheduler.Run(exitCalledFn);

bool ExitCalled() {
    // handle your logic here
}
```

### Systems

```csharp
var printSomethingFn = PrintSomething;
scheduler.OnUpdate(printSomethingFn);

// The scheduler will run all systems registered before one time
scheduler.RunOnce();

void PrintSomething() => Console.WriteLine("Hello from TinyEcs!");
```

The systems declaraction order matters.
```csharp
scheduler.OnUpdate(() => Console.WriteLine("Foo"));
scheduler.OnUpdate(() => Console.WriteLine("Bar"));
scheduler.OnUpdate(() => Console.WriteLine("Baz"));

// This will print:
// Foo
// Bar
// Baz
scheduler.RunOnce();
```

Systems are organized in stages:
```csharp
scheduler.OnStartup(() => Console.WriteLine("1"));
scheduler.OnFrameStart(() => Console.WriteLine("2"));
scheduler.OnBeforeUpdate(() => Console.WriteLine("3"));
scheduler.OnUpdate(() => Console.WriteLine("4"));
scheduler.OnAfterUpdate(() => Console.WriteLine("5"));
scheduler.OnFrameEnd(() => Console.WriteLine("6"));
scheduler.OnStartup(() => Console.WriteLine("7"));

// This will print:
// 1 to 7 in order
scheduler.RunOnce();

// This will print:
// 2 to 7 in order. "1" get excluded because the OnStartup are one-shot systems.
scheduler.RunOnce();
```

### Queries
Queries are one of the most type used in systems. They allow you to pick entities and manipulate the data associated with them.
```csharp
scheduler.OnUpdate((
    Query<Data<Position, Velocity>> query
) => {
    // access to the entity data
    foreach ((Ptr<Position> pos, Ptr<Velocity> vel) in query) {
        pos.Ref.X += vel.Ref.X;
        pos.Ref.Y += vel.Ref.Y
    }

     // Access to the entity too
     foreach ((PtrRO<EntityView> entity, Ptr<Position> pos, Ptr<Velocity> vel) in query) {
        pos.Ref.X += vel.Ref.X;
        pos.Ref.Y += vel.Ref.Y
    }
});
```
### Queries filters
Most of the time you need to pick a specific set of entities under certain conditions.

#### With<T>
`With<T>` will tell to the query to grab all entities that contains the type `T`. `T` can be a component or a tag.
```csharp
Query<
    Data<Position, Velocity>,
    With<Mass>
> query
```

#### Without<T>
`Without<T>` will tell to the query to exclude from the query all entities that contains the type `T`. `T` can be a component or a tag.
```csharp
Query<
    Data<Position, Velocity>,
    Without<Mass>
> query
```

#### Optional<T>
`Optional<T>` will tell to the query to try to get all entities that contains the type `T`. `T` can be a component.
Which means the query will returns entities which might not contains that `T`. 
Check if `T` is valid using `Ptr<T>::IsValid()` method.
```csharp
Query<
    Data<Position, Velocity>,
    Optional<Position>
> query

foreach ((Ptr<Position> maybePos, Ptr<Velocity> vel) in query) {
    if (maybePos.IsValid()) {
        maybePos.Ref.X += 1;
    }
}
```

#### Filter<T0...TN>
You can mix all the filters above using the `Filter<...>` struct to create complex queries.
```csharp
Query<
    Data<Position, Velocity>,
    Filter<Optional<Position>, With<Mass>, Without<Moon>>
> query
```

## Sample code

This is a very basic example which doens't show the whole features set of this library.

```csharp
using var world = new World();
var scheduler = new Scheduler(world);

// create the Time variable accessible globally by any system which stays fixed at 60fps
scheduler.AddResource(new Time() { FrameTime = 1000.0f / 60.0f });
scheduler.AddResource(new AssetManager());

var setupSysFn = Setup;
scheduler.OnStartup(setupSysFn);

var moveSysFn = MoveEntities;
scheduler.OnUpdate(moveSysFn);

var countSomethingSysFn = CountSomething;
scheduler.OnUpdate(countSomethingSysFn);


while (true)
    scheduler.RunOnce();

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

void MoveEntities(Query<Data<Position, Velocity>> query, Res<Time> time)
{
    foreach ((Ptr<Position> pos, Ptr<Velocity> vel) in query)
    {
        pos.Ref.X += vel.Ref.X * time.Value.FrameTime;
        pos.Ref.Y += vel.Ref.Y * time.Value.FrameTime;
    }
}

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

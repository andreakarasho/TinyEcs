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


# Documentation

## World
### Create an entity

```csharp
var world = new World();
EntityView entity = world.Entity();
```
---
### Delete an entity

```csharp
var world = new World();
EntityView entity = world.Entity();
entity.Delete(); // or world.Delete(entity);
```
---
### Entity exists

```csharp
bool exists = entity.Exists(); // or world.Exists(entity);
```
---
### Set component
Components are the real data that an entity contains. An array will be allocated per component. You can access to the data using the `world.Get<T>()` api.

Requirements:
- must be a `struct`
- must contains one field at least
```csharp
EntityView entity = world.Entity()
    .Set(new Position() { X = 0, Y = 1, Z = -1 });

struct Position { public float X, Y, Z; }
```
---
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
---
### Unset component/tag

```csharp
entity.Unset<IsFruit>()
      .Unset<Position>();
```
---
### Has component/tag

```csharp
bool isFruit = entity.Has<IsFruit>();
bool hasPosition = entity.Has<Position>();
```
---
### Get component
Attention: you can query for a non empty component only!
```csharp
ref Position pos = ref entity.Get<Position>(); // or world.Get<Position>(entity);
```
---
### AddChild/RemoveChild
`AddChild` will add a component called `Children` to the parent entity and `Parent` to each child.
`Children` contains a list of all entities associated to the parent.
A child can have an unique parent only.

```csharp
var root = world.Entity();
var child = world.Entity();
var anotherChild = world.Entity();

root.AddChild(child);
root.AddChild(anotherChild);

ref var children = ref root.Get<Children>();
foreach (var child in children) {
}

// Remove the child from the parent
root.RemoveChild(anotherchild);

// This will delete all children too
root.Delete();
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
---
### Run a scheduler
Control each tick using
```csharp
while (!exit) {
    scheduler.RunOnce();
}
```
or just run until a certain condition is met.
```csharp
var exitCalledFn = ExitCalled;
scheduler.Run(exitCalledFn);

bool ExitCalled() {
    // handle your logic here
}
```
---
### Systems
Systems are where "things" happen. 
You should wrap your game logic using systems!

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
---
### Stages
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
---
### System parameters
You can set 0 to 16 parameters in any order of any type per system.
```csharp
scheduler.OnUpdate((
    World world,
    Query<Data<Position>> query1,
    Query<Data<Position>, Without<Velocity>> query2,
    Res<TileMap> tileMap
) => {
});
```

#### World
Access to the `World` instance.
```csharp
// Spawn an entity during the startup phase
scheduler.OnStartup((World world) => world.Entity());
```
---
#### Commands
Access to the `World` instance, but in deferred mode.
```csharp
// Spawn an entity during the startup phase in deferred mode
scheduler.OnStartup((Commands commands) => commands.Entity());
```
---
#### `Query<TData>`
`TData` constraint is a `Data<T0...TN>` type which is used to express the set of components that contains data (no tags).
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

     // Access to the entity using the same query
     foreach ((PtrRO<EntityView> entity, Ptr<Position> pos, Ptr<Velocity> vel) in query) {
        pos.Ref.X += vel.Ref.X;
        pos.Ref.Y += vel.Ref.Y
    }
});
```


#### `Query<TData, TFilter>`
Filters help you to express a more granular search.

##### `With<T>` 
This will tell to the query to grab all entities that contains the type `T`. `T` can be a component or a tag.
```csharp
Query<
    Data<Position, Velocity>,
    With<Mass>
> query
```

##### `Without<T>` 
This will tell to the query to exclude from the query all entities that contains the type `T`. `T` can be a component or a tag.
```csharp
Query<
    Data<Position, Velocity>,
    Without<Mass>
> query
```

##### `Changed<T>`
The query will check if `T` is changed from last execution.
```csharp
Query<
    Data<Position, Velocity>,
    Changed<Position>
> query
```

##### `Added<T>`
The query will check if `T` has been added from last execution.
```csharp
Query<
    Data<Position, Velocity>,
    Added<Position>
> query
```

##### `Optional<T>` 
This will tell to the query to try to get all entities that contains the type `T`.
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

##### `Empty`
Sometime you need to find entities without specifing any `Data<T0...TN>`.
```csharp
Query<
    Empty,
    Filter<With<Position>, With<Mass>, Without<Moon>>
> query
```

##### `Filter<T0...TN>` 
This is to mix all the filters above to create more complex queries.
```csharp
Query<
    Data<Position, Velocity>,
    Filter<Optional<Position>, With<Mass>, Without<Moon>>
> query
```
---
#### Resources

##### `Res<T>`
`Res<T>` is a special system parameter which allow you to inject singleton classes/structs of any type globally.
Here is where you gonna put your `GameNetworkSocket` implementation, your super `TileMap` code, the `GraphicDevice`, etc.
Now guess what? Yeah you did it. They can get called in systems sign.
```csharp
// Declare the resource
scheduler.AddResource(new GameNetworkSocket());

scheduler.OnUpdate((Res<GameNetworkSocket> socket) => {
    socket.Value.SendAttackPacket();
});
```
##### `Local<T>`
`Local<T>` are the same of `Res<T>` but it exists in the declared system only.
```csharp
scheduler.OnUpdate((Local<int> counter) => {
    counter.Value++;
    Console.WriteLine("counter system A: {0}, counter.Value);
});

scheduler.OnUpdate((Local<int> counter) => {
    counter.Value++;
    Console.WriteLine("counter system B: {0}, counter.Value);
});

// This will print
// counter system A: 1
// counter system B: 1
scheduler.RunOnce();
```
---
#### Events
Events are used to trigger behaviours between systems. Multiple system can read the same data using `EventReader<T>`. Events lives for 1 frame only.
```csharp
// Register the event
scheduler.AddEvent<OnClicked>();

// Read the events
scheduler.OnUpdate((EventReader<OnClicked> reader) => {
    foreach (var clickedEvent in reader) {
        
    }
});

// Create the events
scheduler.OnUpdate((EventWriter<OnClicked> writer, Res<MouseContext> mouseCtx) => {
    if (mouseCtx.Value.IsLeftClicked()) {
        writer.Enqueue(new OnClicked() { MouseLeft = true });
    }
});

struct OnClicked { public bool MouseLeft; }
```
---
#### SchedulerState
`SchedulerState` is a system parameter which expose few Scheduler behaviour into the systems.
```csharp
scheduler.OnUpdate((SchedulerState sched) => {
    sched.AddResource(new TileMap());
});
```
---
#### State
State are simply enums useful to run certain systems in certain conditions.

##### `State<T>`
This is a special system parameter which keeps the current state of `T`.

```csharp
// Register the state. No systems get triggered yet
scheduler.AddState(GameState.Loading);

// OnEnter/OnExit runs only when the state changes
scheduler.OnEnter(GameState.Loading, () => Console.WriteLine("enter Loading"));
scheduler.OnExit(GameState.Loading, () => Console.WriteLine("exit Loading"));

scheduler.OnEnter(GameState.GamePlay, () => Console.WriteLine("enter GamePlay"));
scheduler.OnExit(GameState.GamePlay, () => Console.WriteLine("exit GamePlay"));

scheduler.OnUpdate((State<GameState> state, Local<int> currentStateIndex) => {
    var states = Enum.GetValues<GameState>();

    // Switch to the next state
    state.Set(states[currentStateIndex.Value % states.Length]);

    currentStateIndex.Value += 1;
});

// This will run:
// exit Loading
// enter GamePlay
schduler.RunOnce();

// This will run:
// exit GamePlay
// enter Loading
schduler.RunOnce();

enum GameState
{
    Loading,
    Gameplay
}
```
---
#### System conditions
Often you need to run a system only when a condition is met.
```csharp
scheduler.OnUpdate((Res<int> val) => val.Value++);
scheduler.OnUpdate((Res<int> val) => Console.WriteLine("val: {0}", val.Value))
         // Run the system only when `val` is even...
         .RunIf((Res<int> val) => val.Value % 2 == 0)
         // and when exist entities with [Position + Velocity]
         .RunIf((Query<Data<Position, Velocity>> query) => query.Count() > 0)
         // and when the scheduler is in a specific state
         .RunIf((SchedulerState sched) => sched.InState(GameState.Gameplay));
```
---
### Plugin
Plugins are a way to organize your code better.

```csharp
scheduler.AddPlugin<UIPlugin>();
scheduler.AddPlugin<GamePlayPlugin>();

struct GameplayPlugin : IPlugin {
    public void Build(Scheduler scheduler) {
        // declare your logic
    }
}

struct UIPlugin : IPlugin {
    public void Build(Scheduler scheduler) {
        // declare your logic
    }
}
```
---


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

## Run the pepe game!

```bash
cd samples/TinyEcsGame
dotnet run -c Release
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

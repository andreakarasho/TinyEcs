# TinyEcs
TinyEcs: a high-performance, reflection-free entity component system (ECS) framework for .NET with Bevy-inspired scheduling.

## Key Features

-   **Reflection-free**: No `GetType()` or runtime reflection - perfect for NativeAOT/bflat
-   **Zero allocations**: Designed for minimal GC pressure in hot paths
-   **Cache-friendly**: Archetype-based storage for optimal memory layout
-   **Thread-safe**: Deferred command system with parallel execution support
-   **Modern scheduling**: Bevy-inspired App, stages, system parameters, and plugins
-   **Change detection**: Built-in tick tracking with `Changed<T>` and `Added<T>` filters
-   **Component bundles**: Group related components for cleaner entity spawning
-   **Observer system**: React to entity lifecycle events (spawn, despawn, component changes)
-   **State management**: Enum-based state transitions with OnEnter/OnExit systems

## Requirements
-   .NET 8.0+ for core ECS
-   .NET 9.0+ recommended for full Bevy layer support

## Status

Active development - API stable for core features. Production-ready for single and multi-threaded scenarios.


# Documentation

## World
### Create an entity

```csharp
var world = new World();

// Get or create a new entity
EntityView entity = world.Entity();

// Get or create an entity with a specific name
EntityView entity = world.Entity("Player");

// Get or create an entity with a specific id
EntityView entity = world.Entity(1234);
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
### Set component/tag
Components are the real data that an entity contains. An array will be allocated per component. You can access to the data using the `world.Get<T>()` api.
Tags are used to describe an entity. No data will get allocated when adding a tag. Tags are not accessible from the `world.Get<T>()` api.

Requirements:
- must be a `struct`

```csharp
EntityView entity = world.Entity()
    // This is a component
    .Set(new Position() { X = 0, Y = 1, Z = -1 })
    // This is a tag
    .Set<IsAlly>(); 

struct Position { public float X, Y, Z; }
struct IsAlly;
```
---
### Unset component/tag

```csharp
entity.Unset<IsAlly>()
      .Unset<Position>();
```
---
### Has component/tag

```csharp
bool isAlly = entity.Has<IsAlly>();
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

## Bevy-Inspired App & Scheduling

TinyEcs includes a powerful scheduling layer inspired by [Bevy](https://bevyengine.org/), bringing modern ECS patterns to .NET.

### Quick Start with App

```csharp
using TinyEcs;
using TinyEcs.Bevy;

var world = new World();
var app = new App(world, ThreadingMode.Auto); // Auto, Single, or Multi

// Add systems to stages
app.AddSystem((Query<Data<Position, Velocity>> query, Res<Time> time) =>
{
    foreach (var (pos, vel) in query)
        pos.Ref.Value += vel.Ref.Value * time.Value.Delta;
})
.InStage(Stage.Update)
.Build();

// Game loop (startup runs automatically on first call)
while (running)
    app.Run();
```

### Stages

Systems execute in predefined stages (in order):
- `Stage.Startup` - Runs once on first frame (always single-threaded)
- `Stage.First` - First regular update stage
- `Stage.PreUpdate` - Before main update
- `Stage.Update` - Main gameplay logic
- `Stage.PostUpdate` - After main update
- `Stage.Last` - Final stage (rendering, cleanup)

Custom stages supported:
```csharp
var stage = Stage.Custom("Physics");
app.AddStage(stage).After(Stage.Update).Before(Stage.PostUpdate);
```

### System Ordering

```csharp
app.AddSystem(ProcessInput)
   .InStage(Stage.Update)
   .Label("input")
   .Build();

app.AddSystem(MovePlayer)
   .InStage(Stage.Update)
   .After("input")  // Runs after ProcessInput
   .Build();
```

### Threading

```csharp
// Parallel execution (default with ThreadingMode.Auto)
app.AddSystem(ParallelSystem).InStage(Stage.Update).Build();

// Force single-threaded (e.g., for UI or graphics)
app.AddSystem(RenderSystem)
   .InStage(Stage.Last)
   .SingleThreaded()
   .Build();
```
---
### System parameters
You can set 0 to 16 parameters in any order of any type per system.
```csharp
app.AddSystem((
    World world,
    Query<Data<Position>> query1,
    Query<Data<Position>, Without<Velocity>> query2,
    Res<TileMap> tileMap
) => {
    // system logic
})
.InStage(Stage.Update)
.Build();
```

#### World
Access to the `World` instance.
```csharp
// Spawn an entity during the startup phase
app.AddSystem((World world) => world.Entity())
   .InStage(Stage.Startup)
   .Build();
```
---
#### Commands
Deferred command buffer for thread-safe entity operations.
```csharp
// Spawn an entity during the startup phase
app.AddSystem((Commands commands) => commands.Spawn())
   .InStage(Stage.Startup)
   .Build();
```
---
#### `Query<TData>`
`TData` constraint is a `Data<T0...TN>` type which is used to express the set of components that contains data (no tags).
Queries are one of the most used types in systems. They allow you to pick entities and manipulate the data associated with them.
```csharp
app.AddSystem((Query<Data<Position, Velocity>> query) => {
    // Access component data
    foreach ((Ptr<Position> pos, Ptr<Velocity> vel) in query) {
        pos.Ref.X += vel.Ref.X;
        pos.Ref.Y += vel.Ref.Y;
    }

    // Access entity ID along with components
    foreach ((PtrRO<ulong> entityId, Ptr<Position> pos, Ptr<Velocity> vel) in query) {
        Console.WriteLine($"Entity {entityId.Ref}: pos=({pos.Ref.X}, {pos.Ref.Y})");
    }
})
.InStage(Stage.Update)
.Build();
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

##### `Res<T>` and `ResMut<T>`
Resources are global singletons accessible across systems with proper borrowing semantics:

```csharp
// Add resource
app.AddResource(new Time { Delta = 0.016f });

// Read-only access
app.AddSystem((Res<Time> time) => {
    Console.WriteLine($"Delta: {time.Value.Delta}");
}).InStage(Stage.Update).Build();

// Mutable access
app.AddSystem((ResMut<Score> score) => {
    score.Value.Points += 10;
}).InStage(Stage.Update).Build();
```

- `Res<T>.Value` returns `ref readonly T` (read-only borrowing)
- `ResMut<T>.Value` returns `ref T` (exclusive write access)

##### `Local<T>`
Per-system persistent state:
```csharp
app.AddSystem((Local<int> counter) => {
    counter.Value++;
    Console.WriteLine($"System A: {counter.Value}");
}).InStage(Stage.Update).Build();

app.AddSystem((Local<int> counter) => {
    counter.Value++;
    Console.WriteLine($"System B: {counter.Value}");
}).InStage(Stage.Update).Build();

// Prints: System A: 1, System B: 1 (separate counters)
```
---
#### Events
Events enable communication between systems. They persist across stages but clear between frames.

```csharp
struct ScoreEvent { public int Points; }

// Write events
app.AddSystem((EventWriter<ScoreEvent> writer) => {
    writer.Send(new ScoreEvent { Points = 100 });
}).InStage(Stage.Update).Build();

// Read events (available in same frame, after writer runs)
app.AddSystem((EventReader<ScoreEvent> reader) => {
    foreach (var evt in reader.Read())
        Console.WriteLine($"Score: {evt.Points}");
})
.InStage(Stage.PostUpdate)
.Build();
```
---
#### State Management
Enum-based state machines with transition systems:

```csharp
enum GameState { Menu, Playing, Paused }

app.AddState(GameState.Menu);

// Run on state entry
app.AddSystem((Commands cmd) => {
    cmd.Spawn().Insert(new Player());
})
.OnEnter(GameState.Playing)
.Build();

// Run on state exit
app.AddSystem((Query<Data<Player>> query, Commands cmd) => {
    foreach (var player in query)
        cmd.Entity(player.EntityId).Delete();
})
.OnExit(GameState.Playing)
.Build();

// Trigger transition
app.AddSystem((ResMut<NextState<GameState>> next) => {
    next.Value.Set(GameState.Playing);
})
.InStage(Stage.Update)
.Build();
```
---
#### System conditions
Often you need to run a system only when a condition is met.
```csharp
app.AddSystem((ResMut<int> val) => val.Value++)
   .InStage(Stage.Update)
   .Build();

app.AddSystem((Res<int> val) => Console.WriteLine($"val: {val.Value}"))
   .InStage(Stage.Update)
   // Run only when val is even
   .RunIf((Res<int> val) => val.Value % 2 == 0)
   // And when entities with [Position + Velocity] exist
   .RunIf((Query<Data<Position, Velocity>> query) => query.Count() > 0)
   .Build();
```
---
### Component Bundles

Group related components for cleaner entity spawning:

```csharp
struct PlayerBundle : IBundle
{
    public Position Position;
    public Health Health;
    public Sprite Sprite;

    public readonly void Insert(EntityView entity)
    {
        entity.Set(Position);
        entity.Set(Health);
        entity.Set(Sprite);
    }

    public readonly void Insert(EntityCommands entity)
    {
        entity.Insert(Position);
        entity.Insert(Health);
        entity.Insert(Sprite);
    }
}

// Use bundle
commands.SpawnBundle(new PlayerBundle
{
    Position = new Position { X = 0, Y = 0 },
    Health = new Health { Value = 100 },
    Sprite = new Sprite { Color = Color.Red }
});
```

### Observers

React to entity lifecycle events:

```csharp
// OnSpawn - fired when entity is created
app.AddObserver<OnSpawn>((world, trigger) => {
    Console.WriteLine($"Entity {trigger.EntityId} spawned");
});

// OnDespawn - fired when entity is deleted
app.AddObserver<OnDespawn>((world, trigger) => {
    Console.WriteLine($"Entity {trigger.EntityId} despawned");
});

// OnAdd<T> - fired when component is added for the FIRST time
app.AddObserver<OnAdd<Health>>((world, trigger) => {
    Console.WriteLine($"Health added first time: {trigger.Component.Value}");
});

// OnInsert<T> - fired when component is added OR updated
app.AddObserver<OnInsert<Health>>((world, trigger) => {
    Console.WriteLine($"Health set to: {trigger.Component.Value}");
});

// OnRemove<T> - fired when component is removed
app.AddObserver<OnRemove<Health>>((world, trigger) => {
    Console.WriteLine($"Health removed: {trigger.Component.Value}");
});

// Entity-specific observers (fire only for specific entity)
commands.Spawn()
    .Observe<OnInsert<Health>>((w, trigger) =>
        Console.WriteLine($"My health: {trigger.Component.Value}"))
    .Insert(new Health { Value = 100 });

// Custom events with On<T>
app.AddObserver<On<CustomEvent>>((world, trigger) => {
    Console.WriteLine($"Custom event: {trigger.Event.Message}");
});
// Trigger custom event
world.EmitTrigger(new On<CustomEvent>(new CustomEvent { Message = "Hello" }));
```

**Available Observer Events:**
- `OnSpawn` - Entity created
- `OnDespawn` - Entity deleted
- `OnAdd<T>` - Component added (first time only)
- `OnInsert<T>` - Component added or updated
- `OnRemove<T>` - Component removed
- `On<TEvent>` - Custom user-defined events

### Plugins

Organize code into reusable modules:

```csharp
struct PhysicsPlugin : IPlugin
{
    public float Gravity;

    public void Build(App app)
    {
        app.AddResource(new PhysicsSettings { Gravity = Gravity });
        app.AddSystem(ApplyGravity).InStage(Stage.Update).Build();
    }
}

app.AddPlugin(new PhysicsPlugin { Gravity = 9.81f });
```
---


## Complete Example

A simple but complete example using the modern App API:

```csharp
using TinyEcs;
using TinyEcs.Bevy;

var world = new World();
var app = new App(world, ThreadingMode.Auto);

// Add resources
app.AddResource(new Time { Delta = 1.0f / 60.0f });

// Spawn entities in startup
app.AddSystem((Commands commands) =>
{
    for (int i = 0; i < 1000; i++)
    {
        commands.Spawn()
            .Insert(new Position { X = i * 10, Y = 0 })
            .Insert(new Velocity { X = 1, Y = 1 });
    }
})
.InStage(Stage.Startup)
.Build();

// Move entities
app.AddSystem((Query<Data<Position, Velocity>> query, Res<Time> time) =>
{
    foreach (var (pos, vel) in query)
    {
        pos.Ref.X += vel.Ref.X * time.Value.Delta;
        pos.Ref.Y += vel.Ref.Y * time.Value.Delta;
    }
})
.InStage(Stage.Update)
.Label("movement")
.Build();

// Check bounds (runs after movement)
app.AddSystem((Query<Data<Position, Velocity>> query) =>
{
    foreach (var (pos, vel) in query)
    {
        if (pos.Ref.X > 800) vel.Ref.X = -Math.Abs(vel.Ref.X);
        if (pos.Ref.X < 0) vel.Ref.X = Math.Abs(vel.Ref.X);
        if (pos.Ref.Y > 600) vel.Ref.Y = -Math.Abs(vel.Ref.Y);
        if (pos.Ref.Y < 0) vel.Ref.Y = Math.Abs(vel.Ref.Y);
    }
})
.InStage(Stage.Update)
.After("movement")
.Build();

// Game loop (Run() automatically runs startup systems on first call)
while (running)
    app.Run();


struct Position { public float X, Y; }
struct Velocity { public float X, Y; }
class Time { public float Delta; }
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

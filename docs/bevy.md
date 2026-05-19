# Bevy Layer (`TinyEcs.Bevy`)

Bevy-inspired scheduling on top of the pure ECS. Brings `App`, stages,
system-parameter injection, plugins, observers, states, events, bundles,
and threaded execution.

## Project requirements
- Targets: .NET 9.0 / 10.0
- Depends on `TinyEcs` core
- No reflection in hot paths

## Quick start

```csharp
using TinyEcs;
using TinyEcs.Bevy;

var world = new World();
var app   = new App(world, ThreadingMode.Auto); // Auto | Single | Multi

app.AddResource(new Time { Delta = 1f / 60f });

app.AddSystem((Query<Data<Position, Velocity>> q, Res<Time> time) =>
{
    foreach (var (pos, vel) in q)
    {
        pos.Ref.X += vel.Ref.X * time.Value.Delta;
        pos.Ref.Y += vel.Ref.Y * time.Value.Delta;
    }
})
.InStage(Stage.Update)
.Build();

while (running) app.Update();   // Startup runs once on the first call
```

## App

`App` owns resources, events, states, the schedule, and global observers.
`World` stays pure ECS.

```csharp
app.AddResource(new GameSettings());
var settings = app.GetResource<GameSettings>();
bool has     = app.HasResource<GameSettings>();

app.SendEvent(new ScoreEvent(100));    // outside a system
app.AddState(GameState.Menu);
app.SetState(GameState.Playing);       // immediate, outside systems
```

Inside systems, prefer the system parameters below.

## Stages

Default order:
- `Stage.Startup` — runs once on the first frame; always single-threaded.
- `Stage.First`
- `Stage.PreUpdate`
- `Stage.Update`
- `Stage.PostUpdate`
- `Stage.Last`

Custom stages slot in via `After` / `Before`:

```csharp
var physics = Stage.Custom("Physics");
app.AddStage(physics).After(Stage.Update).Before(Stage.PostUpdate);
```

## Systems

A system is any delegate whose parameters are system parameters. Register with
`AddSystem`, configure with the fluent builder, finalize with `Build()`.

```csharp
app.AddSystem(ProcessInput)
   .InStage(Stage.Update)
   .Label("input")
   .Build();

app.AddSystem(MovePlayer)
   .InStage(Stage.Update)
   .After("input")
   .Build();
```

### Ordering
- Declaration order is preserved when no constraints are given.
- `.After(label)` / `.Before(label)` enforce explicit ordering.
- `.Chain()` runs after the previously added system.
- Topological sort resolves the graph. Missing labels throw.

### Threading
- `ThreadingMode.Auto` — parallel if `ProcessorCount > 1`.
- `ThreadingMode.Single` — sequential.
- `ThreadingMode.Multi` — parallel.
- Per-system overrides: `.SingleThreaded()`, `.WithThreadingMode(mode)`.
- Systems without conflicting access run in parallel batches.
- Batches preserve declaration order.

## System parameters

A system can take 0–16 parameters, any order, any supported type.

### `World`
```csharp
app.AddSystem((World w) => w.Entity())
   .InStage(Stage.Startup).Build();
```

### `Commands`
Deferred entity / component / resource operations. Applied after the system
(or batch) finishes. Each system gets its own buffer — thread-safe.

```csharp
app.AddSystem((Commands cmd, Query<Data<Health>> wounded) =>
{
    // Spawn a brand-new entity with components.
    var spawned = cmd.Spawn()
        .Insert(new Position { X = 0, Y = 0 })
        .Insert(new Health { Value = 100 });

    // Patch an existing entity discovered via a query.
    foreach (var (entity, _) in wounded)
        cmd.Entity(entity.Ref).Insert(new Position { X = 0, Y = 0 });

    // Drop a resource onto the App (applies after the frame).
    cmd.InsertResource(new GameSettings());
})
.InStage(Stage.Update).Build();
```

### Queries

`Query<Data<T0..Tn>>` exposes components for iteration. `Data` lists the
components a system reads or writes (tags don't appear here).

```csharp
app.AddSystem((Query<Data<Position, Velocity>> q) =>
{
    foreach (var (pos, vel) in q)
    {
        pos.Ref.X += vel.Ref.X;
        pos.Ref.Y += vel.Ref.Y;
    }

    // Entity ID + components
    foreach (var (entity, pos, vel) in q)
        Console.WriteLine($"#{entity.Ref}: {pos.Ref.X}, {pos.Ref.Y}");
})
.InStage(Stage.Update).Build();
```

Add a `Filter<...>` for matching constraints:

| Filter        | Effect                                                         |
|---------------|----------------------------------------------------------------|
| `With<T>`     | Entity must have `T` (component or tag).                       |
| `Without<T>`  | Entity must NOT have `T`.                                      |
| `Changed<T>`  | `T` modified since the system last ran.                        |
| `Added<T>`    | `T` was added since the system last ran.                       |
| `Optional<T>` | Include matching entities; `Ptr<T>.IsValid()` indicates state. |
| `MarkChanged<T>` | Manually flag `T` as changed.                                |

```csharp
Query<Data<Position, Velocity>, Without<Mass>> q1;
Query<Data<Position>, Changed<Position>>       q2;
Query<Data<Sprite>,    Filter<With<Mass>, Without<Moon>>> q3;
Query<Empty,           Filter<With<Position>, With<Mass>>>  q4;
```

`Optional<T>` example — the query type is shown so the `Ptr<T>` shape of the
loop variables is obvious:
```csharp
app.AddSystem((Query<Data<Position, Velocity>, Filter<Optional<Position>>> q) =>
{
    foreach ((Ptr<Position> maybe, Ptr<Velocity> vel) in q)
        if (maybe.IsValid())
            maybe.Ref.X += 1;
})
.InStage(Stage.Update).Build();
```

### Resources — `Res<T>` and `ResMut<T>`

Resources are App-owned singletons.

```csharp
app.AddResource(new Time { Delta = 0.016f });

app.AddSystem((Res<Time> t)    => Console.WriteLine(t.Value.Delta));   // ref readonly
app.AddSystem((ResMut<Score> s) => s.Value.Points += 10);              // ref T
```

### `Local<T>`
Per-system persistent state. Each system gets its own copy.

```csharp
app.AddSystem((Local<int> n) => Console.WriteLine(++n.Value))
   .InStage(Stage.Update).Build();
```

### Events — `EventReader<T>` / `EventWriter<T>`
Queue lives on `App`. Events clear between frames.

```csharp
struct ScoreEvent { public int Points; }

app.AddSystem((EventWriter<ScoreEvent> w) => w.Send(new ScoreEvent { Points = 100 }))
   .InStage(Stage.Update).Build();

app.AddSystem((EventReader<ScoreEvent> r) =>
{
    foreach (var e in r.Read())
        Console.WriteLine(e.Points);
})
.InStage(Stage.PostUpdate).Build();
```

### States — `Res<State<T>>` / `ResMut<NextState<T>>`

```csharp
enum GameState { Menu, Playing, Paused }
app.AddState(GameState.Menu);

app.AddSystem(StartGame).OnEnter(GameState.Playing).Build();
app.AddSystem(StopMusic).OnExit(GameState.Playing).Build();

app.AddSystem((ResMut<NextState<GameState>> next) =>
    next.Value.Set(GameState.Playing))
.InStage(Stage.Update).Build();

app.AddSystem((Res<State<GameState>> s) =>
    Console.WriteLine($"{s.Value.Previous} -> {s.Value.Current}"))
.InStage(Stage.Update).Build();
```

Transitions queue via `NextState` and apply after the frame.

### `RunIf`
Gate a system on any system-parameter-driven condition. Multiple conditions
must all hold for the system to run.

```csharp
app.AddSystem(DoWork)
   .InStage(Stage.Update)
   .RunIf((Res<Counter> c) => c.Value.Value % 2 == 0)
   .RunIf((Query<Data<Position, Velocity>> q) => q.Count() > 0)
   .Build();
```

### Composite system parameters

Group related params into a single parameter object. Inherit
`CompositeSystemParam` and register inner params in the constructor — the
base class forwards `Initialize` / `Fetch` / access merging automatically.

```csharp
public sealed class PhysicsParams : CompositeSystemParam
{
    public readonly Query<Data<Position, Velocity>> Moving;
    public readonly Res<TimeResource>               Time;
    public readonly Commands                        Commands;

    public PhysicsParams()
    {
        Moving   = Add(new Query<Data<Position, Velocity>>());
        Time     = Add(new Res<TimeResource>());
        Commands = Add(new Commands());
    }
}

app.AddSystem((PhysicsParams p) =>
{
    foreach (var (pos, vel) in p.Moving)
        pos.Ref.X += vel.Ref.X * p.Time.Value.Delta;
})
.InStage(Stage.Update).Build();
```

Implement `ISystemParam` directly (`Initialize(App)`, `Fetch(App)`,
`GetAccess()`) only if you need behavior the composite base can't express.

See `samples/MyBattleground/CompositeParamExample.cs` and the widgets in
`src/TinyEcs.Bevy.UI/Widgets/` for full examples.

## Component bundles

Group related components for ergonomic spawning. Implement `IBundle`:

```csharp
struct PlayerBundle : IBundle
{
    public Position Position;
    public Health   Health;
    public Sprite   Sprite;

    public readonly void Insert(EntityCommands e)
    {
        e.Insert(Position);
        e.Insert(Health);
        e.Insert(Sprite);
    }
}

app.AddSystem((Commands cmd, Query<Data<Player>> players) =>
{
    // Spawn a new entity with the full bundle.
    cmd.SpawnBundle(new PlayerBundle { /* ... */ });

    // Re-equip every existing player from a different bundle.
    foreach (var (entity, _) in players)
        cmd.Entity(entity.Ref).InsertBundle(new RespawnBundle { /* ... */ });
})
.InStage(Stage.Update).Build();
```

## Observers

Global observers are registered on `App` and fire for any entity.

```csharp
app.AddObserver<OnSpawn>((world, t) =>
    Console.WriteLine($"#{t.EntityId} spawned"));

app.AddObserver<OnAdd<Health>>((world, t) =>
    Console.WriteLine($"first-time health = {t.Component.Value}"));

app.AddObserver<OnInsert<Health>>((world, t) =>
    Console.WriteLine($"health now {t.Component.Value}"));

app.AddObserver<OnRemove<Health>>((world, t) =>
    Console.WriteLine($"health removed: {t.Component.Value}"));

app.AddObserver<OnDespawn>((world, t) =>
    Console.WriteLine($"#{t.EntityId} gone"));
```

Observers can take system parameters:
```csharp
app.AddObserver<OnDespawn, Res<EntityTracker>, ResMut<Stats>>((t, tracker, stats) =>
{
    tracker.Value.LogDespawn(t.EntityId);
    stats.Value.EntityCount--;
});
```

Entity-tied observers attach during spawn or to an existing entity. Stored as
a component on the entity; auto-cleaned on despawn. The base form takes the
trigger only; extra system parameters can follow.

```csharp
app.AddSystem((Commands cmd) =>
{
    // Attach an observer to a brand-new entity.
    cmd.Spawn()
        .Observe<OnInsert<Health>>(t => Console.WriteLine(t.Component.Value))
        .Insert(new Health { Value = 100 });

    // Attach an observer that itself takes system parameters.
    cmd.Spawn()
        .Observe<OnInsert<Health>, Commands>((t, inner) =>
            inner.Spawn().Insert(new Particle()))
        .Insert(new Health { Value = 99 });
})
.InStage(Stage.Startup).Build();
```

Custom events via `commands.EmitTrigger`:
```csharp
struct CustomEvent { public string Message; }

app.AddObserver<On<CustomEvent>>((w, t) =>
    Console.WriteLine(t.Event.Message));

app.AddSystem((Commands cmd) =>
    cmd.EmitTrigger(new CustomEvent { Message = "Hello" }))
.InStage(Stage.Update).Build();
```

Available trigger types:
- `OnSpawn`, `OnDespawn`
- `OnAdd<T>`, `OnInsert<T>`, `OnRemove<T>`
- `On<TEvent>`

All triggers implement `IEntityTrigger` with reflection-free `EntityId`
access.

### Trigger propagation

`On<TEvent>` implements `IPropagatingTrigger`. When fired with
`propagate: true`, after the target entity's observers run, the trigger
bubbles up to the entity's `Parent`, then *its* parent, all the way to the
root. Any observer in the chain can call `trigger.Propagate(false)` to stop
the bubble — observers receive the trigger by reference precisely for this.

```csharp
app.AddSystem((Commands commands, Query<Data<TinyEcs.Children>> withKids) =>
{
    foreach (var (entity, _) in withKids)
    {
        // Entity-targeted emit with bubbling enabled. Each ancestor that
        // observes On<DamageEvent> will be invoked in turn.
        commands.Entity(entity.Ref)
            .EmitTrigger(new DamageEvent { Amount = 5 }, propagate: true);
    }
})
.InStage(Stage.Update).Build();

app.AddObserver<On<DamageEvent>>((world, trigger) =>
{
    Console.WriteLine($"#{trigger.EntityId} took {trigger.Event.Amount}");
    // Stop bubbling if this entity is a shielded boss.
    if (world.Entity(trigger.EntityId).Has<Shielded>())
        trigger.Propagate(false);
});
```

`Commands.EmitTrigger(evt)` (no `propagate` argument) always emits both
globally and — if the event implements `IEntityTrigger` with `EntityId != 0` —
on the entity with `propagate: true`. The `EntityCommands.EmitTrigger`
overload defaults `propagate` to `false`, so you opt in.

## Plugins

```csharp
struct PhysicsPlugin : IPlugin
{
    public float Gravity;

    public void Build(App app)
    {
        app.AddResource(new PhysicsSettings { Gravity = Gravity });
        app.AddSystem(ApplyGravity).InStage(Stage.Update).Build();
        app.AddSystem(ResolveCollisions).InStage(Stage.PostUpdate).Build();
    }
}

app.AddPlugin(new PhysicsPlugin { Gravity = 9.81f });
```

## Troubleshooting

| Symptom                                  | Fix                                                  |
|------------------------------------------|------------------------------------------------------|
| `System must be assigned to a stage`     | Call `.InStage(...)` (or `.OnEnter/.OnExit`) before `.Build()`. |
| `Circular dependency detected`           | Check `.After/.Before` chains for cycles.            |
| `No system with label X found`           | Add the labeled system before its dependents.        |
| Components don't update                  | Use `pos.Ref.X = ...`, not `pos.Value`.              |
| Commands don't appear to apply           | They're deferred; effects land after the system (or batch). |

# CLAUDE Project Brief

## Overview
TinyEcs is a high-performance, reflection-free entity component system (ECS) framework for .NET. It targets zero-runtime-allocation workflows, supports NativeAOT/bflat, and ships with an optional Bevy-inspired scheduling layer that brings modern stage orchestration, observers, system-parameter injection, and component bundles to C# game and simulation projects.

## Core Philosophy
- **Reflection-free**: All component registration and lookups avoid runtime reflection, enabling AOT and high determinism. **This is a strict requirement - no `GetType()`, `GetProperty()`, or other reflection APIs in hot paths.**
- **Zero-allocation**: Designed for minimal GC pressure in hot paths
- **Cache-friendly**: Archetype-based storage for optimal memory layout
- **Compile-time safety**: Strong typing with ref structs and source generation
- **Thread-safe**: Deferred command system for safe multi-threaded execution

## Repository Layout
- `src/TinyEcs/` — Core ECS runtime (world, entity views, archetype storage, queries)
- `src/TinyEcs.Bevy/` — Bevy-inspired extensions (App, stages, plugins, observers, system parameters, bundles)
- `samples/` — Example programs (TinyEcsGame with Raylib, MyBattleground)
- `tests/` — xUnit test suites (115+ tests covering all features)
- `benchmarks/` — Performance evaluation scenarios

## Core ECS (src/TinyEcs/)

### World & Entities
- `World` - Main ECS container, manages entities and components
- `EntityView` - Lightweight handle to an entity with `.Set()`, `.Get()`, `.Has()`, `.Unset()`, `.Delete()`
- Entities are 64-bit IDs with recycling support

### Components
- Must be `struct` types (value types)
- Zero-sized tags supported (e.g., `struct Player {}`)
- Change detection built-in with tick tracking
- Access via `Ptr<T>` (ref struct) for zero-copy reads/writes

### Queries
- `Query<Data<T1, T2, ...>>` - Multi-component iteration
- `Filter<T1, T2, ...>` - Combine multiple filters
- Built-in filters:
  - `With<T>` / `Without<T>` - Component presence
  - `Changed<T>` / `Added<T>` - Change detection
  - `Optional<T>` - Nullable component access
  - `MarkChanged<T>` - Manually mark components as changed
- Queries use `foreach` pattern with ref access via `.Ref`

### Archetypes
- Entities grouped by component signature
- Cache-friendly columnar storage
- Automatic archetype transitions on component add/remove

## Bevy Layer (src/TinyEcs.Bevy/)

### App & Stages
```csharp
var app = new App(ThreadingMode.Auto); // or Single, Multi
app.AddPlugin(new MyPlugin());
app.RunStartup(); // Runs once
while (running) app.Update(); // Run all stages
```

**Default Stages** (in execution order):
- `Stage.Startup` - Runs once on first frame (always single-threaded)
- `Stage.First` - First regular update stage
- `Stage.PreUpdate` - Before main update
- `Stage.Update` - Main gameplay logic
- `Stage.PostUpdate` - After main update
- `Stage.Last` - Final stage (rendering, cleanup)

**Custom Stages**:
```csharp
var stage = Stage.Custom("MyStage");
app.AddStage(stage).After(Stage.Update).Before(Stage.PostUpdate);
```

### System Registration
```csharp
// Fluent API with system parameters
app.AddSystem((Query<Data<Position, Velocity>> query, Res<Time> time) =>
{
    foreach (var (pos, vel) in query)
        pos.Ref.Value += vel.Ref.Value * time.Value.Delta;
})
.InStage(Stage.Update)
.Label("movement")
.After("input")
.SingleThreaded() // Force single-threaded execution
.RunIf(world => !world.GetResource<GameState>().Paused)
.Build();
```

### System Ordering
- **Declaration order preserved** - Systems run in the order they're added when no dependencies exist
- **Explicit ordering**:
  - `.After("label")` - Run after labeled system
  - `.Before("label")` - Run before labeled system
  - `.Chain()` - Run after the previously added system
- **Topological sort** - Automatically resolves dependency graph
- **Error handling** - Throws exception if label doesn't exist

### Threading Modes
- `ThreadingMode.Auto` - Use parallel execution if `ProcessorCount > 1`
- `ThreadingMode.Single` - Force all systems to run sequentially
- `ThreadingMode.Multi` - Enable parallel execution
- **Per-system override**: `.SingleThreaded()` or `.WithThreadingMode(mode)`
- **Batching** - Systems without conflicts run in parallel batches
- **Declaration order in batches** - Batches preserve system declaration order
- **Startup stage**: `Stage.Startup` always runs in single-threaded mode regardless of app threading mode. This ensures deterministic initialization and proper resource setup.

### System Parameters

**Query Parameters**:
```csharp
Query<Data<Position, Velocity>> query
Query<Data<Sprite>, Filter<Changed<Position>>> filtered
```

**Resource Access**:
```csharp
Res<TimeResource> time        // Immutable resource
ResMut<ScoreTracker> score    // Mutable resource
```
- `Res<T>.Value` returns `ref readonly T`, ensuring read-only borrowing
- `ResMut<T>.Value` returns `ref T` for exclusive write access

**Deferred Commands**:
```csharp
Commands commands
commands.Spawn().Insert(new Position { X = 0 });
commands.Entity(id).Insert(new Health { Value = 100 });
commands.InsertResource(new GameSettings());
```

**Events**:
```csharp
EventWriter<ScoreEvent> writer
writer.Send(new ScoreEvent(100));

EventReader<ScoreEvent> reader
foreach (var evt in reader.Read()) { }
```

**Local State**:
```csharp
Local<int> counter  // Per-system persistent state
counter.Value++;
```

**Composite System Parameters**:

You can create custom system parameters that group multiple parameters together, reducing clutter and improving code organization. This mirrors Bevy's `SystemParam` derive macro.

```csharp
// Define a composite parameter
public class PhysicsParams : ISystemParam
{
    public Query<Data<Position, Velocity>> MovingEntities = new();
    public Res<TimeResource> Time = new();
    public Commands Commands = new();

    public void Initialize(World world)
    {
        MovingEntities.Initialize(world);
        Time.Initialize(world);
        Commands.Initialize(world);
    }

    public void Fetch(World world)
    {
        MovingEntities.Fetch(world);
        Time.Fetch(world);
        Commands.Fetch(world);
    }

    public SystemParamAccess GetAccess()
    {
        var access = new SystemParamAccess();
        // Combine access from all sub-parameters
        foreach (var read in MovingEntities.GetAccess().ReadResources)
            access.ReadResources.Add(read);
        foreach (var write in MovingEntities.GetAccess().WriteResources)
            access.WriteResources.Add(write);
        // ... repeat for Time and Commands
        return access;
    }
}

// Use in a system (replaces 3 parameters with 1)
app.AddSystem((PhysicsParams physics) =>
{
    foreach (var (pos, vel) in physics.MovingEntities)
    {
        pos.Ref.X += vel.Ref.X * physics.Time.Value.Delta;
    }
}).InStage(Stage.Update).Build();
```

**Benefits**:
- Reduces parameter count (1 composite param vs 3+ individual params)
- Groups related functionality together
- Makes systems more readable and maintainable
- Reusable across multiple systems
- Still maintains proper access tracking for parallel execution

See `samples/MyBattleground/CompositeParamExample.cs` for a complete working example.

### Component Bundles
```csharp
// Define a bundle
struct SpriteBundle : IBundle
{
    public Position Position;
    public Sprite Sprite;
    public Velocity Velocity;

    public readonly void Insert(EntityView entity)
    {
        entity.Set(Position);
        entity.Set(Sprite);
        entity.Set(Velocity);
    }

    public readonly void Insert(EntityCommands entity)
    {
        entity.Insert(Position);
        entity.Insert(Sprite);
        entity.Insert(Velocity);
    }
}

// Use bundles
commands.SpawnBundle(new SpriteBundle { /* ... */ });
entity.InsertBundle(bundle);
```

### Observers
```csharp
// Global observers - react to events on any entity
app.AddObserver<OnSpawn>((world, trigger) =>
    Console.WriteLine($"Entity {trigger.EntityId} spawned"));

app.AddObserver<OnInsert<Health>>((world, trigger) =>
    Console.WriteLine($"Health added: {trigger.Component.Value}"));

app.AddObserver<OnRemove<Player>>((world, trigger) =>
    Console.WriteLine($"Player removed from {trigger.EntityId}"));

// Observers with system parameters
app.AddObserver<OnDespawn, Res<EntityTracker>, ResMut<Stats>>((trigger, tracker, stats) =>
{
    tracker.Value.LogDespawn(trigger.EntityId);
    stats.Value.EntityCount--;
});

// Filter by entity ID in the observer callback if needed
app.AddObserver<OnInsert<Health>>((world, trigger) =>
{
    if (trigger.EntityId == mySpecificEntityId)
    {
        Console.WriteLine("My specific entity's health changed!");
    }
});
```

**Observer Events**:
- `OnSpawn` - Entity created
- `OnDespawn` - Entity deleted
- `OnInsert<T>` - Component added/updated (includes the component value)
- `OnRemove<T>` - Component removed (includes the component value)
- `OnAdd<T>` - Component added for the first time
- `On<TEvent>` - Custom event triggered via `commands.EmitTrigger()`

All triggers implement `IEntityTrigger` interface providing reflection-free access to `EntityId`.

**Entity-Specific Observers** (`commands.Spawn().Observe()`):
```csharp
// Attach observer during entity spawn (WORKS)
commands.Spawn()
    .Observe<OnInsert<Health>>((w, trigger) =>
        Console.WriteLine($"Health: {trigger.Component.Value}"))
    .Insert(new Health { Value = 100 });

// Attach to existing entity (WORKS for initial events)
commands.Entity(entityId)
    .Observe<OnInsert<Health>>((w, trigger) => { ... })
    .Insert(new Health { Value = 99 });
```

**Implementation**:
- Observers stored as `EntityObservers` component on entities
- Automatically cleaned up when entity despawns
- Completely reflection-free using `IEntityTrigger` interface
- Observers inserted before Insert/Remove commands in queue

**Known Limitations** (edge cases, 3/125 tests fail):
- Observers may not fire on `world.Set()` calls made directly after app.Run() completes
- OnDespawn observers may not fire (timing issue with component removal)
- For these cases, use global observers with entity ID filtering instead

**Status**: Production-ready for primary use case (observers during entity spawn). Edge cases can use global observers as workaround.

### State Management
```csharp
enum GameState { Menu, Playing, Paused }

app.AddState(GameState.Menu);

app.AddSystem(StartGame)
   .OnEnter(GameState.Playing)
   .Build();

app.AddSystem(StopMusic)
   .OnExit(GameState.Playing)
   .Build();

// In a system
world.SetState(GameState.Paused);
var current = world.GetState<GameState>();
```
- `Res<State<GameState>>` exposes the current/previous values (`state.Value.Current`)
- `ResMut<NextState<GameState>>` queues transitions (`next.Value.Set(GameState.Playing)`) that apply after the frame

### Plugins
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

## Important Implementation Details

### Ptr<T> and Component Access
- Components accessed via `Ptr<T>` ref struct
- `Ptr<T>.Ref` gives direct reference to component data
- Example: `pos.Ref.Value += velocity.Ref.Value * deltaTime`

### Query Iteration Patterns
```csharp
// Multi-component query
var query = world.Query<Data<Position, Velocity>>();
foreach (var (pos, vel) in query)
{
    pos.Ref.Value += vel.Ref.Value; // Direct ref access
}

// With filters
var query = world.Query<Data<Health>, Filter<Changed<Health>>>();
foreach (var health in query)
{
    if (health.Ref.Value <= 0)
        Console.WriteLine("Entity died!");
}
```

### Change Detection
- Each component has a "changed tick"
- `Changed<T>` filter checks if component modified since last system run
- `Added<T>` filter checks if component just added
- `MarkChanged<T>` manually marks component as changed
- Tick management automatic per `World.Update()` call

### Deferred Commands Pattern
- Commands queued during system execution
- Applied after system completes (or after batch if parallel)
- Thread-safe - each system gets its own command buffer
- Prevents race conditions in parallel execution

## Code Generation

### T4 Templates
- `src/TinyEcs.Bevy/TinyEcs.Bevy.Data.tt` - Generates `Data<T1, ..., T16>` types
- `src/TinyEcs.Bevy/TinyEcs.Bevy.Filter.tt` - Generates `Filter<T1, ..., T16>` types
- Generated files: `.g.cs` suffix
- Run T4 transformation: `dotnet t4 <template>.tt`

### Namespace Organization
- Core types in `TinyEcs` namespace
- Bevy extensions in `TinyEcs.Bevy` namespace
- Filters, Data, and system parameters in `TinyEcs.Bevy`

## Testing Strategy

### Test Organization (tests/)
- `BevyApp.cs` - App, stages, system ordering, observers, events, bundles (18 tests)
- `ChangeDetection.cs` - Changed/Added filters, tick management
- `Query.cs` - Query iteration, filters, multi-component access
- `Entity.cs` - Entity creation, component add/remove, deletion
- `World.cs` - World lifecycle, entity recycling
- `Components.cs` - Component registration and access patterns

### Key Test Patterns
```csharp
[Fact]
public void SystemsRunInDeclarationOrder()
{
    var app = new App();
    var executed = new List<string>();

    app.AddSystem(w => executed.Add("First")).InStage(Stage.Update).Build();
    app.AddSystem(w => executed.Add("Second")).InStage(Stage.Update).Build();

    app.Run();
    Assert.Equal(new[] { "First", "Second" }, executed);
}
```

### Running Tests
```bash
dotnet test tests/TinyEcs.Tests.csproj                    # All tests (115+)
dotnet test --filter "FullyQualifiedName~BevyApp"        # BevyApp tests only
dotnet test --filter "FullyQualifiedName~SystemsRun"     # Specific test name
```

## Sample: TinyEcsGame

Located at `samples/TinyEcsGame/Program.cs` - Full game with Raylib integration.

**Key Patterns Demonstrated**:
- Plugin architecture (RaylibPlugin, GameplayPlugin, RenderingPlugin)
- Bundles (SpriteBundle)
- System ordering with labels and `.After()`
- Single-threaded systems (all Raylib calls)
- Resources (TimeResource, WindowSize, AssetsManager)
- Deferred commands for entity spawning
- 100,000 entity stress test

**Component Bundle Example**:
```csharp
commands.SpawnBundle(new SpriteBundle
{
    Position = new Position { Value = new Vector2(x, y) },
    Velocity = new Velocity { Value = new Vector2(vx, vy) },
    Sprite = new Sprite { Color = color, Scale = 1.0f },
    Rotation = new Rotation { Value = 0f }
});
```

## Common Patterns & Best Practices

### Thread Safety
- Mark Raylib/UI systems as `.SingleThreaded()`
- Use `ResMut<T>` for exclusive resource access
- Use `Res<T>` for shared read-only access
- Commands automatically thread-safe

### Performance
- Queries are cached and reused
- Batch operations when possible
- Use `Changed<T>` to skip unchanged entities
- Avoid allocations in hot paths (use structs, stackalloc, spans)

### System Organization
```csharp
// Group related systems in plugins
struct MovementPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddSystem(ApplyVelocity).InStage(Stage.Update).Label("move").Build();
        app.AddSystem(CheckBounds).InStage(Stage.Update).After("move").Build();
    }
}
```

### Error Handling
- Invalid label references throw `InvalidOperationException` with clear message
- Circular dependencies throw on topological sort
- Missing stage assignment throws when building system

## Building & Development

### Build Commands
```bash
dotnet build TinyEcs.slnx                              # Build everything
dotnet build src/TinyEcs.Bevy/TinyEcs.Bevy.csproj     # Just Bevy layer
dotnet build samples/TinyEcsGame/TinyEcsGame.csproj   # Sample game
```

### Project Structure
- `TinyEcs.slnx` - Solution file
- Target: .NET 9.0 (preview) or .NET 8.0
- NativeAOT compatible
- No reflection usage

## Key Files Reference

**Core ECS**:
- `src/TinyEcs/World.cs` - Main world container
- `src/TinyEcs/Query.cs` - Query iteration, filters
- `src/TinyEcs/Ptr.cs` - Component pointer abstraction
- `src/TinyEcs/Archetype.cs` - Archetype storage

**Bevy Layer**:
- `src/TinyEcs.Bevy/BevyApp.cs` - App, stages, system scheduling (1300+ lines)
- `src/TinyEcs.Bevy/BevySystemParams.cs` - System parameters (Commands, Res, Local, etc.)
- `src/TinyEcs.Bevy/BevyFilters.cs` - Query filters (With, Without, Changed, Added, etc.)
- `src/TinyEcs.Bevy/BevyBundle.cs` - Component bundle system

**Generated Code**:
- `src/TinyEcs.Bevy/TinyEcs.Bevy.Data.g.cs` - Data<T1..T16> tuples
- `src/TinyEcs.Bevy/TinyEcs.Bevy.Filter.g.cs` - Filter<T1..T16> combinators

## UI Layer (src/TinyEcs.UI/)

TinyEcs.UI integrates the Clay immediate-mode layout engine with the TinyEcs/Bevy runtime while remaining reflection-free and AOT friendly.

### ClayUiPlugin & State
- `ClayUiPlugin` registers `ClayUiState`, optional `ClayPointerState`, and default systems for hierarchy sync, pointer ingestion, layout invalidation, and the Stage.Update layout pass.
- `ClayUiState` owns the Clay arena/context, installs a custom measure-text callback, tracks element-to-entity mappings, hover/capture state, and exposes the latest `ReadOnlySpan<Clay_RenderCommand>` for renderer integration.
- `ClayUiEntityLayout` can materialize entity-driven UI (`UiNode`, `UiText`, `UiNodeParent`) into Clay layout trees when enabled.

### Pointer Flow
- `ClayPointerState` captures pointer position/button/scroll deltas each frame.
- `UiPointerEvent` is published via the global event channel.
- `UiPointerTrigger` implements `ITrigger`, `IEntityTrigger`, and `IPropagatingTrigger`, enabling `entity.Observe<UiPointerTrigger>()` with bubbling up the TinyEcs `Parent` chain.

### UI Components
- `UiNode`: wraps a `Clay_ElementDeclaration` for layout/styling.
- `UiText`: stores Clay strings/config for labels.
- `UiNodeParent`: declarative hierarchy instruction mirrored into TinyEcs `Parent`/`Children`.

### Widgets (src/TinyEcs.UI/Widgets/)
- `PanelWidget`: padded containers with configurable sizing/background/gaps.
- `ButtonWidget`: styled buttons with hover/pressed colors and text configuration, ready to consume pointer events.

### Sample Reference
- `samples/MyBattleground/UiClayExample.cs` demonstrates plugin setup, widget composition, simulated pointer input, pointer logging (`EventReader<UiPointerEvent>` + `Observe<UiPointerTrigger>`), and render command inspection for renderer integration.

## Recent Improvements (2025)

### System Ordering Fixes
- Fixed topological sort to preserve declaration order
- Fixed batch building to process systems forward (not backward)
- Added better error messages for invalid label dependencies
- All systems now execute in predictable order

### Threading Enhancements
- Added `ThreadingMode` enum (Auto, Single, Multi)
- Per-system `.SingleThreaded()` and `.WithThreadingMode()` overrides
- Fixed parallel batching to respect single-threaded systems
- Proper ordering within batches maintained

### Resource Access Refinements
- Resources now live inside reusable boxes so systems can borrow by `ref`
- `Res<T>` exposes `ref readonly T` while `ResMut<T>` exposes `ref T` to enforce read-only vs. mutable access
- Added `State<T>` / `NextState<T>` resources mirroring Bevy’s queuing semantics for state transitions

### Bundle System
- Implemented `IBundle` interface for component grouping
- `SpawnBundle()` and `InsertBundle()` extension methods
- Support for both immediate and deferred operations
- Cleaner entity spawning code

### Change Detection
- Moved filters to `TinyEcs.Bevy` namespace
- Made `Ptr<T>.Ref` public (previously `.Value`)
- `Changed<T>`, `Added<T>`, `MarkChanged<T>` filters
- Tick-based tracking per component

### Composite System Parameters (NEW - 2025-10-10)
- Added support for custom `ISystemParam` implementations that group multiple parameters
- Mirrors Bevy's `SystemParam` derive macro pattern
- Reduces parameter count and improves code organization
- Example: Combine `Query`, `Res`, and `Commands` into a single `PhysicsParams` type
- See [BevySystemParams.cs:1186-1249](src/TinyEcs.Bevy/BevySystemParams.cs#L1186-L1249) for documentation
- See [CompositeParamExample.cs](samples/MyBattleground/CompositeParamExample.cs) for working example

### State Transition Bug Fix (2025-10-10)
- **Fixed critical bug**: `OnEnter` and `OnExit` systems were firing every frame instead of once per transition
- Root cause: `PreviousStates` dictionary was not being updated after state transitions processed
- Fix: Update `PreviousStates` to match `States` after running OnEnter/OnExit systems in `ProcessStateTransitions()`
- All existing tests pass (31/31)
- See [StateTransitionExample.cs](samples/MyBattleground/StateTransitionExample.cs) for verification test

## Migration Notes

If updating from older TinyEcs versions:
1. `Ptr<T>.Value` renamed to `Ptr<T>.Ref`
2. Filters moved from `TinyEcs` to `TinyEcs.Bevy` namespace
3. `Data<T>` and `Filter<T>` now in `TinyEcs.Bevy` namespace
4. System ordering now preserves declaration order (breaking if you relied on undefined behavior)
5. `.After(label)` and `.Before(label)` now throw if label doesn't exist

## Contributing Guidelines

When adding features:
1. **Add tests first** - Minimum 3 tests per feature
2. **Preserve reflection-free design** - No `typeof()` in hot paths
3. **Maintain thread safety** - Use Commands for deferred operations
4. **Document with XML comments** - All public APIs
5. **Follow naming conventions** - PascalCase for public, _camelCase for private
6. **Keep zero-allocation** - Use structs, ref structs, spans
7. **Update CLAUDE.md** - Document new patterns and APIs

## Troubleshooting

**"System must be assigned to a stage"**
→ Call `.InStage(Stage.X)` or `.OnEnter()/.OnExit()` before `.Build()`

**"Circular dependency detected"**
→ Check `.After()` and `.Before()` chains for cycles

**"No system with label X found"**
→ Ensure labeled system is added before systems that reference it

**Access violation (0xC0000005) with Raylib**
→ Mark all Raylib systems as `.SingleThreaded()`

**Components not updating**
→ Check you're using `ref` access: `pos.Ref.Value` not `pos.Value`

**Commands not applying**
→ Commands are deferred - check after system/frame completes

## Status
Active development - API stable for core features. Bevy layer under refinement. Production-ready for single-threaded and deterministic multi-threaded scenarios.

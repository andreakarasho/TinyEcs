# TinyEcs

<i>Non production ready.</i>

# Sample
```csharp

const int ENTITIES_COUNT = 1_000_000;

using var world = new World();

for (int i = 0; i < ENTITIES_COUNT; ++i)
{
   var entity = world.CreateEntity();
   world.Attach<Position>(entity);
   world.Attach<Velocity>(entity);
}

var query = world.Query()
     .With<Position>()
     .With<Velocity>();

foreach (var view in query)
{
    ref readonly var entity = ref view.Entity;
    ref var pos = ref view.Get<Position>();
    ref var vel = ref view.Get<Velocity>();
    
    // do things
}


struct Position { public float X, Y; }
struct Velocity { public float X, Y; }
```

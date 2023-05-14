# TinyEcs
This small drop-in single-file project borns by the need of a reflection-free dotnet ECS library.<br>
NativeAOT compatible.

# Status
<i>Semi-production ready.</i>  `¯\_(ツ)_/¯`

# Sample
```csharp

const int ENTITIES_COUNT = 1_000_000;

using var world = new World();

for (int i = 0; i < ENTITIES_COUNT; ++i)
{
   var entity = world.Entity()
	.Set<Position>(new Position() { X = 1f, Y = -1f })
	.Set<Velocity>();
}

var query = world.Query()
     .With<Position>()
     .With<Velocity>();

foreach (var it in query)
{
	var p = it.Field<Position>();
	var v = it.Field<Velocity>();

	foreach (var row in it)
	{
		ref var pos = ref p.Get();
		ref var vel = ref v.Get();

		pos.X *= vel.X;
		pos.Y *= vel.Y;
	}
}


struct Position { public float X, Y; }
struct Velocity { public float X, Y; }
```
# Parent-Child relation
```csharp
using var world = new World();

var inventory = world.Entity();
var sword = world.Entity();
var gold = world.Entity();

sword.AttachTo(inventory);
gold.AttachTo(inventory);

// detach
sword.Detach();

// cleanup
inventory.RemoveAllChildren();
```

# Credits
Base code idea inspired by:
- https://github.com/jasonliang-dev/entity-component-system
- https://github.com/SanderMertens/flecs

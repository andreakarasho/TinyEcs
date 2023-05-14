# TinyEcs
This small drop-in single-file project borns by the need of a reflection-free dotnet ECS library.<br>
NativeAOT compatible.

# Status
<i>Semi-production ready.</i>  `¯\_(ツ)_/¯`

# Sample
```csharp
// Initialize the world
using var world = new World();

// Spawn some entity
var entity = world.Entity()
	.Set(new Position() { X = 1f, Y = -1f })
	.Set<Velocity>();


// Search the entities
var query = world.Query()
	.With<Position>()
	.With<Velocity>();
     
unsafe
{
	// Note: you can also parse the query 
	//	"foreach (var it in query) { }"
	
	world.RegisterSystem(query, &MoveSystem);
}


void MoveSystem(in Iterator it)
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
# EntityView
```csharp
using var world = new World();

EntityView e = world.Entity();

// set the transform component
e.Set<Transform>();

// return the Transform component 
ref Transform transform = ref e.Get<Transform>(); 

// return true
bool has = e.Has<Transform>(); 

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

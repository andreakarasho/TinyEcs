# TinyEcs

<i>Non production ready.</i>

# Sample
```csharp

const int ENTITIES_COUNT = 1_000_000;

using var world = new World();

for (int i = 0; i < ENTITIES_COUNT; ++i)
{
   var entity = world.CreateEntity()
	.Set<Position>(new Position() { X = 1f, Y = -1f })
	.Set<Velocity>();
}

var query = world.Query()
     .With<Position>()
     .With<Velocity>();

foreach (var it in query)
{
	ref var p = ref it.Field<Position>();
	ref var v = ref it.Field<Velocity>();

	for (var row = 0; row < it.Count; ++row)
	{
		ref var pos = ref it.Get(ref p, row);
		ref var vel = ref it.Get(ref v, row);

		pos.X *= vel.X;
		pos.Y *= vel.Y;
	}
}


struct Position { public float X, Y; }
struct Velocity { public float X, Y; }
```

# Credits
Base code idea inspired by https://github.com/jasonliang-dev/entity-component-system

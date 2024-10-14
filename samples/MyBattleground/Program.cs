// https://github.com/jasonliang-dev/entity-component-system
using System.Diagnostics;
using TinyEcs;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

const int ENTITIES_COUNT = (524_288 * 2 * 1);

using var ecs = new World();
var scheduler = new Scheduler(ecs);

for (int i = 0; i < ENTITIES_COUNT; i++)
	ecs.Entity()
		.Set<Position>(new Position())
		.Set<Velocity>(new Velocity())
		 ;

scheduler.AddSystem((Query<Data<Position, Velocity>> q) =>
{
	foreach ((var entities, var posA, var velA) in q.Iter())
	{
		for (var i = 0; i < entities.Length; ++i)
		{
			ref var pos = ref posA[i];
			ref var vel = ref velA[i];

			pos.X *= vel.X;
			pos.Y *= vel.Y;
		}
	}
});

var sw = Stopwatch.StartNew();
var start = 0f;
var last = 0f;

while (true)
{
	for (int i = 0; i < 3600; ++i)
	{
		scheduler.Run();
	}

	last = start;
	start = sw.ElapsedMilliseconds;

	Console.WriteLine("query done in {0} ms", start - last);
}

struct Position : IComponent
{
	public float X, Y, Z;
}

struct Velocity : IComponent
{
	public float X, Y;
}

struct Mass : IComponent { public float Value; }


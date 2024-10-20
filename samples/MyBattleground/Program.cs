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
		.Set<Velocity>(new Velocity());


var parent = ecs.Entity("parent");
var child0 = ecs.Entity();
var child1 = ecs.Entity();
var child2 = ecs.Entity();

parent.AddChild2(child0);

//var mapper = new EntityMapper<Parent, Children>(ecs);
//mapper.Add(parent, child0);
//mapper.Add(parent, child1);
//mapper.Add(parent, child2);
//mapper.Add(child0, child1);

ref var children = ref parent.Get<Children>();
ref var name = ref parent.Get<Name>();

var v = ecs.Entity("parent");
var v2 = ecs.Entity("lulz");

//parent.Delete();


//mapper.RemoveChild(child0);
//mapper.RemoveChild(child1);
//mapper.RemoveChild(child2);

if (parent.Has<Parent>())
{

}

if (parent.Has<Children>())
{

}

scheduler.AddSystem((Query<Data<Position, Velocity>> q) =>
{
	var sw = Stopwatch.StartNew();
	var start = 0f;
	var last = 0f;

	while (true)
	{
		for (int i = 0; i < 3600; ++i)
		{
			foreach ((var entities, var posA, var velA) in q)
			{
				ref var pos = ref MemoryMarshal.GetReference(posA);
				ref var vel = ref MemoryMarshal.GetReference(velA);

				ref var lastPos = ref Unsafe.Add(ref pos, entities.Length);
				while (Unsafe.IsAddressLessThan(ref pos, ref lastPos))
				{
					pos.X *= vel.X;
					pos.Y *= vel.Y;

					pos = ref Unsafe.Add(ref pos, 1);
					vel = ref Unsafe.Add(ref vel, 1);
				}
			}
		}

		last = start;
		start = sw.ElapsedMilliseconds;

		Console.WriteLine("query done in {0} ms", start - last);
	}
});


scheduler.Run();

struct Position : IComponent
{
	public float X, Y, Z;
}

struct Velocity : IComponent
{
	public float X, Y;
}

struct Mass : IComponent { public float Value; }

struct Tag : IComponent { }

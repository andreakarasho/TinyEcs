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

scheduler.AddSystem((Query<Data<Position, Velocity>> q)=>
{
	foreach ((var en, var pos, var vel) in q)
	{
		pos.Ref.X *= vel.Ref.X;
		pos.Ref.Y *= vel.Ref.Y;
	}

	// foreach ((var entities, var posA, var velA) in q)
	// {
	// 	ref var pos = ref MemoryMarshal.GetReference(posA);
	// 	ref var vel = ref MemoryMarshal.GetReference(velA);

	// 	ref var lastPos = ref Unsafe.Add(ref pos, entities.Length);
	// 	while (Unsafe.IsAddressLessThan(ref pos, ref lastPos))
	// 	{
	// 		pos.X *= vel.X;
	// 		pos.Y *= vel.Y;

	// 		pos = ref Unsafe.Add(ref pos, 1);
	// 		vel = ref Unsafe.Add(ref vel, 1);
	// 	}
	// }
});


var query = ecs.QueryBuilder()
	.Data<Position>()
	.Data<Velocity>()
	.Build();

var sw = Stopwatch.StartNew();
var start = 0f;
var last = 0f;

while (true)
{
	for (int i = 0; i < 3600; ++i)
	{
		scheduler.Run();
		// unsafe
		// {
		// 	foreach ((var pos, var vel) in query.Each<Position, Velocity>())
		// 	{
		// 		pos.Pointer->X *= vel.Pointer->X;
		// 		pos.Pointer->Y *= vel.Pointer->Y;
		// 	}
		// }


		// var it = query.Iter();
		// while (it.Next())
		// {
		// 	var count = it.Count;

		// 	var posA = it.Data<Position>(0);
		// 	var velA = it.Data<Velocity>(1);

		// 	for (var j = 0; j < count; ++j)
		// 	{
		// 		ref var pos = ref posA[j];
		// 		ref var vel = ref velA[j];
		// 		pos.X *= vel.X;
		// 		pos.Y *= vel.Y;
		// 	}
		// }
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

struct Tag : IComponent { }

struct Mobiles;
struct Items;

struct Trading : IComponent, IRule
{
	public readonly void Apply()
	{

	}
}

interface IRule
{
	void Apply();
}

abstract class EntityRule
{
	private readonly World _world;

	protected EntityRule(World world)
	{
		_world = world;

		world.OnComponentSet += (ecs, id, c) => OnComponentSet(ecs.Entity(id), in c);
	}

	public virtual void OnComponentSet(EntityView entity, ref readonly ComponentInfo component)
	{

	}

	public virtual void OnComponentUnset(EntityView entity)
	{

	}

	public virtual void OnEntityCreated(EntityView entity)
	{

	}

	public virtual void OnEntityRemoved(EntityView entity)
	{

	}
}

class SymmetricRule : EntityRule
{
	public SymmetricRule(World world) : base(world)
	{
	}

	public override void OnComponentSet(EntityView entity, ref readonly ComponentInfo component)
	{

	}
}


struct FakeTuple : ITuple
{
	public object? this[int index] => throw new NotImplementedException();

	public int Length => throw new NotImplementedException();
}

class Foo<T> where T : allows ref struct
{
}

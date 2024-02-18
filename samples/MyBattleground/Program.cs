// https://github.com/jasonliang-dev/entity-component-system
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TinyEcs;

const int ENTITIES_COUNT = (524_288 * 2 * 1);

using var ecs = new World();


var e = ecs.Entity()
	.Set<Position>(new Position())
	.Set<Velocity>(new Velocity());

var child = ecs.Entity();
var child2 = ecs.Entity();
var child3 = ecs.Entity();

ecs.Relation<Root>(e, child);
ecs.Relation<Root>(e, child2);
ecs.Relation<Root>(child2, child3);

ecs.Filter<With<(Root, EcsID)>>()
	.Query((EntityView entity, ref (Root, EcsID) relation) =>
	{
		Console.WriteLine("parent {0}", entity.ID);
		Console.WriteLine("....child: {0}", relation.Item2);
	});

ecs.Query((EntityView entity, ref (EcsID, Root) relation) =>
	{
		// filter by the parent id
		if (relation.Item1 != e)
		{
			return;
		}

		Console.WriteLine("child {0}", entity.ID);
		Console.WriteLine("....parent: {0}", relation.Item1);
	});


e.AddChild(child);
e.AddChild(child2);

var ff = ecs.Pair<ChildOf>(e);

var qry = ecs.QueryBuilder().With(ff).Build();

foreach (var arch in qry)
{

}

foreach (var arch in ecs.Filter([ff]))
{
	foreach (ref readonly var chunk in arch)
	{
		foreach (ref var entity in chunk.Entities.AsSpan(0, chunk.Count))
		{
			Console.WriteLine("child {0}", entity.ID);
		}
	}
}



ecs.Entity()
	.Set<Position>(new Position())
	.Set<Velocity>(new Velocity());

ecs.Entity()
	.Set<Position>(new Position())
	.Set<Velocity>(new Velocity())
	.Set<Likes>()
	.Set<Dogs>();

for (int i = 0; i < ENTITIES_COUNT; i++)
	ecs.Entity()
		 .Set<Position>(new Position())
		 .Set<Velocity>(new Velocity())
		 .Set<PlayerTag>();


var sw = Stopwatch.StartNew();
var start = 0f;
var last = 0f;


while (true)
{
	//var cur = (start - last) / 1000f;

	for (int i = 0; i < 3600; ++i)
	{
		ecs.Filter<(With<PlayerTag>, Not<Likes>, Not<Dogs>)>()
			.Query((ref Position pos, ref Velocity vel) =>
			{
				pos.X *= vel.X;
				pos.Y *= vel.Y;
			});

		// foreach (var archetype in ecs.Filter<(Position, Velocity)>())
		// {
		// 	var column0 = archetype.GetComponentIndex<Position>();
		// 	var column1 = archetype.GetComponentIndex<Velocity>();
		//
		// 	foreach (ref readonly var chunk in archetype)
		// 	{
		// 		ref var pos = ref chunk.GetReference<Position>(column0);
		// 		ref var vel = ref chunk.GetReference<Velocity>(column1);
		//
		// 		ref var last2 = ref Unsafe.Add(ref pos, chunk.Count);
		//
		// 		while (Unsafe.IsAddressLessThan(ref pos, ref last2))
		// 		{
		// 			pos.X *= vel.X;
		// 			pos.Y *= vel.Y;
		//
		// 			pos = ref Unsafe.Add(ref pos, 1);
		// 			vel = ref Unsafe.Add(ref vel, 1);
		// 		}
		// 	}
		// }
	}

	last = start;
	start = sw.ElapsedMilliseconds;

	Console.WriteLine("query done in {0} ms", start - last);
}

enum TileType
{
	Land,
	Static
}


struct Serial { public uint Value; }
struct Position { public float X, Y, Z; }
struct Velocity { public float X, Y; }
struct PlayerTag { }

struct CustomEvent { }

struct Likes;
struct Dogs { }
struct Apples { }

struct TestStr { public byte v; }

struct ManagedData { public string Text; public int Integer; }

struct Context1 {}
struct Context2 {}

struct Root { }
struct Child { }

struct Relation<TAction, TTarget> where TAction : struct where TTarget : struct
{
	public TAction Action;
	public TTarget Target;
}

// https://github.com/jasonliang-dev/entity-component-system
using System.Diagnostics;
using System;
using TinyEcs;
using System.Runtime.CompilerServices;

const int ENTITIES_COUNT = (524_288 * 2 * 1);

using var ecs = new World();

var e = ecs.Entity("Main")
	.Set<Position>(new Position() {X = 2})
	.Set<Velocity>(new Velocity());


ecs.Filter<(Position, Velocity)>()
	.Query((EntityView entity) => {
		Console.WriteLine(entity);
	});



var e2 = ecs.Entity("Main");
ref var pp = ref e2.Get<Position>();

var child = ecs.Entity("child 0");
var child2 = ecs.Entity("child 1");
var child3 = ecs.Entity("child 2");


e.AddChild(child);
e.AddChild(child2);
child2.AddChild(child3);

foreach (var childId in e.Children())
{

}

ecs.Filter<(With<Child>, With<Parent>)>().Query((EntityView entity, ref Relationship relation) => {
	Console.WriteLine("im [{0}] a child of {1}, but also having {2} children", entity, ecs.Entity(relation.Parent), relation.Count);
});

Console.WriteLine();

ecs.Filter<With<Parent>>().Query((EntityView entity, ref Relationship relation) => {
	Console.WriteLine("parent {0} has {1} children", entity, relation.Count);

	foreach (var id in entity.Children())
	{
		Console.WriteLine("\tChild: {0}", ecs.Entity(id));
	}
});

Console.WriteLine();

e.RemoveChild(child);
ecs.Filter<With<Parent>>().Query((EntityView entity, ref Relationship relation) => {
	Console.WriteLine("parent {0} has {1} children", entity, relation.Count);
});

Console.WriteLine();

child2.RemoveChild(child3);
ecs.Filter<With<Parent>>().Query((EntityView entity, ref Relationship relation) => {
	Console.WriteLine("parent {0} has {1} children", entity, relation.Count);
});

Console.WriteLine();

e.ClearChildren();
ecs.Filter<With<Parent>>().Query((EntityView entity, ref Relationship relation) => {
	Console.WriteLine("parent {0} has {1} children", entity.ID, relation.Count);
});

e.Delete();

	ecs.Entity()
		 .Set<Position>(new Position())
		 .Set<Velocity>(new Velocity());

for (int i = 0; i < ENTITIES_COUNT / 1000; i++)
	ecs.Entity()
		 .Set<Position>(new Position())
		 .Set<Velocity>(new Velocity())
		 .Set<PlayerTag>()
		 .Set<Dogs>()
		 .Set<Likes>()
		 ;

var sw = Stopwatch.StartNew();
var start = 0f;
var last = 0f;

while (true)
{
	//var cur = (start - last) / 1000f;
	for (int i = 0; i < 3600; ++i)
	{
		// ecs.Filter<With<PlayerTag>>()
		// 	.Query((ref Position pos, ref Velocity vel) =>
		// 	{
		// 		pos.X *= vel.X;
		// 		pos.Y *= vel.Y;
		// 	});

		ecs.System<Not<PlayerTag>, Position, Velocity>((ref Position pos , ref Velocity vel) => {
			pos.X *= vel.X;
			pos.Y *= vel.Y;
		});

		// foreach (var archetype in ecs.Query2<(Position, Velocity)>())
		// {
		// 	var column0 = archetype.GetComponentIndex<Position>();
		// 	var column1 = archetype.GetComponentIndex<Velocity>();

		// 	foreach (ref readonly var chunk in archetype)
		// 	{
		// 		ref var pos = ref chunk.GetReference<Position>(column0);
		// 		ref var vel = ref chunk.GetReference<Velocity>(column1);

		// 		ref var last2 = ref Unsafe.Add(ref pos, chunk.Count);

		// 		while (Unsafe.IsAddressLessThan(ref pos, ref last2))
		// 		{
		// 			pos.X *= vel.X;
		// 			pos.Y *= vel.Y;

		// 			pos = ref Unsafe.Add(ref pos, 1);
		// 			vel = ref Unsafe.Add(ref vel, 1);
		// 		}
		// 	}
		// }

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

struct Chunk;
struct ChunkTile;

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

e.Disable();
var enabled = e.IsEnabled();
e.Disable();
enabled = e.IsEnabled();
e.Enable();
enabled = e.IsEnabled();

ecs.Filter<(Position, Velocity)>()
	.Query((EntityView entity) => {
		Console.WriteLine(entity.Name());
	});



// var e2 = ecs.Entity("Main");
// ref var pp = ref e2.Get<Position>();

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
	Console.WriteLine("im [{0}] a child of {1}, but also having {2} children", entity.Name(), ecs.Entity(relation.Parent).Name(), relation.Count);
});

Console.WriteLine();

ecs.Filter<With<Parent>>().Query((EntityView entity, ref Relationship relation) => {
	Console.WriteLine("parent {0} has {1} children", entity.Name(), relation.Count);

	foreach (var id in entity.Children())
	{
		Console.WriteLine("\tChild: {0}", ecs.Entity(id).Name());
	}
});

Console.WriteLine();

e.RemoveChild(child);
ecs.Filter<With<Parent>>().Query((EntityView entity, ref Relationship relation) => {
	Console.WriteLine("parent {0} has {1} children", entity.Name(), relation.Count);
});

Console.WriteLine();

child2.RemoveChild(child3);
ecs.Filter<With<Parent>>().Query((EntityView entity, ref Relationship relation) => {
	Console.WriteLine("parent {0} has {1} children", entity.Name(), relation.Count);
});

Console.WriteLine();

// e.ClearChildren();
// ecs.Filter<With<Parent>>().Query((EntityView entity, ref Relationship relation) => {
// 	Console.WriteLine("parent {0} has {1} children", entity.Name(), relation.Count);
// });

for (var i = 0; i < 5; ++i)
{
	var c = ecs.Entity();
	Console.WriteLine("Add {0}", c.ID);
	e.AddChild(c);
}

foreach (var childId in e.Children())
{
	Console.WriteLine("child {0}", childId);
}

e.Delete();
var exists = e.Exists();

ecs.Entity()
	.Set<Position>(new Position())
	.Set<Velocity>(new Velocity());

for (int i = 0; i < ENTITIES_COUNT / 1; i++)
	ecs.Entity()
		.Set<Position>(new Position() {X = i})
		.Set<Velocity>(new Velocity(){X = i})
		//  .Set<PlayerTag>()
		//  .Set<Dogs>()
		//  .Set<Likes>()
		 ;

for (var i = 7000; i < 8000 * 2; ++i)
	ecs.Entity((ulong)i).Delete();

var sw = Stopwatch.StartNew();
var start = 0f;
var last = 0f;


while (true)
{
	//var cur = (start - last) / 1000f;
	for (int i = 0; i < 3600; ++i)
	{
		ecs
		//.Filter<With<PlayerTag>>()
			.Query((ref Position pos, ref Velocity vel) =>
			{
				pos.X *= vel.X;
				pos.Y *= vel.Y;
			});
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

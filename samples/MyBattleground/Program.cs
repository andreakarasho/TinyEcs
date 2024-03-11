// https://github.com/jasonliang-dev/entity-component-system
using System.Diagnostics;
using System;
using TinyEcs;
using System.Runtime.CompilerServices;

const int ENTITIES_COUNT = (524_288 * 2 * 1);

using var ecs = new World();
var systems = new SystemManager(ecs);
var ff = systems.Add<MoveSystem>();

Console.WriteLine("system name : {0}", ff.Name);

ecs.Entity<PlayerTag>();
ecs.Entity<Likes>();

var e = ecs.Entity("Main")
	.Set<Position>(new Position() {X = 2})
	.Set<Velocity>(new Velocity());



var query = ecs.Query<(Position, Velocity), (Not<PlayerTag>, Not<Likes>)>();
query.Each((ref Position pos, ref Velocity vel) => {

});



systems.Update();

var rabbit = ecs.Entity();
var eats = ecs.Entity();
var carrots = ecs.Entity();
var grass = ecs.Entity();


e.Disable();
var enabled = e.IsEnabled();
e.Disable();
enabled = e.IsEnabled();
e.Enable();
enabled = e.IsEnabled();

// ecs.Filter<(Position, Velocity, Not<Disabled>)>()
// 	.Query((EntityView entity) => {
// 		Console.WriteLine(entity.Name());
// 	});

// ecs.Query((EntityView entity, ref ComponentInfo cmp) => {
// 		Console.WriteLine("cmp {0} size {1}", cmp.ID, cmp.Size);
// 	});

// var e2 = ecs.Entity("Main");
// ref var pp = ref e2.Get<Position>();

var child = ecs.Entity("child 0");
var child2 = ecs.Entity("child 1");
var child3 = ecs.Entity("child 2");


e.AddChild<Hierarchy>(child);
e.AddChild<Hierarchy>(child2);
child2.AddChild<Hierarchy>(child3);


e.AddChild<Chunk>(child2);
e.AddChild<Chunk>(child3);

//child2.Delete();


// e.ClearChildren();
// ecs.Filter<With<Parent>>().Query((EntityView entity, ref Relationship relation) => {
// 	Console.WriteLine("parent {0} has {1} children", entity.Name(), relation.Count);
// });

for (var i = 0; i < 5; ++i)
{
	var c = ecs.Entity();
	Console.WriteLine("Add {0}", c.ID);
	e.AddChild<Hierarchy>(c);
}

foreach (var childId in e.Children<Hierarchy>())
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
		// ecs.Query<(Position, Velocity)>()
		//    .Each((ref Position pos, ref Velocity vel) => {
		// 	pos.X *= vel.X;
		// 	pos.Y *= vel.Y;
		// });

		ecs.Each((ref Position pos, ref Velocity vel) => {
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


sealed class MoveSystem : EcsSystem
{
	private Query? _query;

	public override void OnCreate(World ecs)
	{
		_query = ecs.Query<(Position, Velocity)>();
		Console.WriteLine("system {0} created", Name);
	}

	public override void OnStart(World ecs)
	{
		Console.WriteLine("system {0} started", Name);
	}

	public override void OnUpdate(World ecs)
	{
		_query!.Each((ref Position pos, ref Velocity vel) => {
			Console.WriteLine("aa");
		});
	}
}

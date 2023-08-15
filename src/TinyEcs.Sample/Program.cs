// https://github.com/jasonliang-dev/entity-component-system
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TinyEcs;

const int ENTITIES_COUNT = 524_288 * 2 * 1;

using var world = new World();

var main = world.Spawn();
var secondMain = world.Spawn();
for (int i = 0; i < 10; ++i)
	world.Spawn().ChildOf(main).ChildOf(secondMain);

for (int i = 0; i < 10; ++i)
	world.Spawn().ChildOf(main);

main.Children(s => {
	var p = s.Parent();
	Console.WriteLine("child id {0}", s.ID);
});

secondMain.Children(s => {
	var p = s.Parent();
	Console.WriteLine("secondMain child id {0}", s.ID);
});


//main.Despawn();
//main.ClearChildren();

world.Query().With<EcsChildOf>(main.ID).Iterate(static (ref Iterator it) => {
	Console.WriteLine("found children");
});

world.Query().With<EcsChildOf, EcsAny>().Iterate(static (ref Iterator it) => {
	Console.WriteLine("found children for any");
});

var ecs = new World();


var pos = ecs.Spawn();
var vel = ecs.Spawn();
var likes = ecs.Spawn();
var cats = ecs.Spawn();
var flowers = ecs.Spawn();
var childOf = ecs.Spawn();
var root = ecs.Spawn().ID;

var id = ecs.Spawn()
	.Set(new Position() { X = 10, Y = 29 })
	.SetPair<Likes, Dogs>()
	.SetPair<Likes, Apples>()
	.SetTag(pos.ID)
	.SetPair(likes.ID, cats.ID)
	.SetPair(likes.ID, flowers.ID)
	.SetPair(childOf.ID, root)
	.ID;

ref var posID = ref ecs.Component<Position>();
var pairLikesDogID = ecs.Pair<Likes, Dogs>();

var ent = ecs.Spawn().Set(new Position() { X = 20, Y = 9 });

ref var p = ref ent.Get<Position>();
p.X = 9999;
p.Y = 12;
p.Z = 0.2f;

//ecs.Step(0f);

//ref var posp = ref ecs.Entity(ent.ID).Get<Position>();

var ok = ecs.Entity(id)
	.Has<Likes, Dogs>();

var qry1 = ecs.Query().With(childOf.ID, root);

// foreach (var it in qry1)
// {

// }

ecs.Entity(id).Each(static s =>
{
	if (s.IsPair())
	{
		Console.WriteLine("pair: ({0}, {1})", s.First(), s.Second());
	}
	else if (s.IsEntity())
	{
		Console.WriteLine("entity: {0}", s.ID);
	}
	else
	{
		Console.WriteLine("unknown: {0}", s.ID);
	}
});

unsafe
{
	ecs.Query()
		.With<EcsComponent>()
		.Iterate(static (ref Iterator it) => {
			var cmpA = it.Field<EcsComponent>();

			for (int i = 0; i < it.Count; ++i)
			{
				ref var cmp = ref cmpA[i];
				var entity = it.Entity(i);

				Console.WriteLine("{0} --> ID: {1} - SIZE: {2} - CMP ID: {3}", cmp.Size <= 0 ? "tag      " : "component", entity.ID, cmp.Size, cmp.ID);
			}
		});


	ecs.StartupSystem(&Setup);
	//ecs.System(&PrintSystem, 1f);
	//ecs.System(&PrintComponents, ecs.Query().With<EcsComponent>(), 1f);
	ecs.System(&ParseQuery, ecs.Query()
		.With<Position>()
		.With<Velocity>()
		.Without<float>()
		.With<Likes, Dogs>());
}

var sw = Stopwatch.StartNew();
var start = 0f;
var last = 0f;

while (true)
{
	var cur = (start - last) / 1000f;

	for (int i = 0; i < 3600; ++i)
	{
		ecs.Step(cur);

		//ecs.Print();
	}

	last = start;
	start = sw.ElapsedMilliseconds;

	Console.WriteLine("query done in {0} ms", start - last);
}


static void Setup(ref Iterator it)
{
	var sw = Stopwatch.StartNew();

	for (int i = 0; i < ENTITIES_COUNT; i++)
		it.Commands.Spawn()
			.Set<Position>()
			.Set<Velocity>()
			.SetTag<int>()
			.SetTag<short>()
			.SetTag<byte>()
			.SetTag<decimal>()
			.SetTag<uint>()
			.SetTag<ushort>()
			.SetPair<Likes, Dogs>()
			;

	var character = it.Commands!.Spawn()
		.Set(new Serial() { Value = 0xDEAD_BEEF });

	Console.WriteLine("Setup done in {0} ms", sw.ElapsedMilliseconds);
}

static void PrintComponents(ref Iterator it)
{
	var cmpA = it.Field<EcsComponent>();

	for (int i = 0; i < it.Count; ++i)
	{
		ref var cmp = ref cmpA[i];
		Console.WriteLine("{0} --> ID: {1} - SIZE: {2} - CMP ID: {3}", cmp.Size <= 0 ? "tag      " : "component", cmp.ID, cmp.Size, cmp.ID);
	}
}

static void ParseQuery(ref Iterator it)
{
	// it.World.PrintGraph();
	// it.Archetype.Print();

	var posA = it.Field<Position>();
	var velA = it.Field<Velocity>();

	for (int i = 0, count = it.Count; i < count; ++i)
	{
		ref var pos = ref posA[i];
		ref var vel = ref velA[i];

		pos.X *= vel.X;
		pos.Y *= vel.Y;
	}
}

static void PrintSystem(ref Iterator it)
{
	Console.WriteLine("1");
}

static void PrintWarnSystem(ref Iterator it)
{
	//Console.WriteLine("3");
}


enum TileType
{
	Land,
	Static
}


struct Serial { public uint Value; }
struct Position { public float X, Y, Z; }
struct Velocity { public float X, Y; }
struct PlayerTag { public ulong ID; }


struct Likes;
struct Dogs { }
struct Apples { }

struct TestStr { public byte v; }

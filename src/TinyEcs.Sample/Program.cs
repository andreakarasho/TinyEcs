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


var world = new World();

for (int i = 0; i < 100; ++i)
	world.Spawn();
var cmds = new Commands(world);
var e = cmds.Spawn().Set<float>(22.0f).Set<byte>();
//ref var floatt = ref e.Get<float>();
cmds.Merge();

e.Set<int>(55);
e.Set<double>(2.0);

cmds.Merge();

ref var floatt3 = ref e.Get<float>();

world.Spawn().Set<float>().Set<byte>().Set<int>();
world.Spawn().Set<float>().Set<float>().Set<int>();
world.Spawn().Set<int>().Set<double>().Set<int>();

world.PrintGraph();

world.Query().With<float>().With<int>().Iterate(static (ref Iterator a) => {
	var floatA = a.Field<float>();
	var intA = a.Field<int>();

	for (int i = 0; i < a.Count; ++i)
	{
		ref var ff = ref floatA[i];

	}
});


var ecs = new Ecs();


var pos = ecs.Spawn();
var vel = ecs.Spawn();
var likes = ecs.Spawn();
var cats = ecs.Spawn();
var flowers = ecs.Spawn();
var childOf = ecs.Spawn();
var root = ecs.Spawn().ID;

var id = ecs.Spawn()
	.Set(new Position() { X = 10, Y = 29 })
	.Add<Likes, Dogs>()
	.Add<Likes, Apples>()
	.Add(pos.ID)
	.Add(likes.ID, cats.ID)
	.Add(likes.ID, flowers.ID)
	.Add(childOf.ID, root)
	.ID;

var posID = ecs.Component<Position>();
var pairLikesDogID = ecs.Component<Likes, Dogs>();

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
	var query = ecs.Query().With<Position>().With<Velocity>().Without<float>();

	ecs.Query()
		.With<EcsComponent>()
		.Iterate(static (ref Iterator it) => {
			var cmpA = it.Field<EcsComponent>();

			for (int i = 0; i < it.Count; ++i)
			{
				ref var cmp = ref cmpA[i];
				var entity = it.Entity(i);

				Console.WriteLine("component --> ID: {0} - SIZE: {1} - CMP ID: {2}", entity.ID, cmp.Size, cmp.ID);
			}
		});


	ecs.StartupSystem(&Setup);
	//ecs.System(&PrintSystem, 1f);
	ecs.System(&ParseQuery, ecs.Query()
		.With<Position>()
		.With<Velocity>()
		.Without<float>()
		.Without<Likes, Dogs>());
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
	}

	last = start;
	start = sw.ElapsedMilliseconds;

	Console.WriteLine("query done in {0} ms", start - last);
}


static void Setup(ref Iterator it)
{
	var sw = Stopwatch.StartNew();

	for (int i = 0; i < ENTITIES_COUNT; i++)
		it.Commands!.Spawn()
			.Set<Position>()
			.Set<Velocity>()
			.Set<int>()
			.Set<short>()
			.Set<byte>()
			.Set<decimal>()
			.Set<uint>()
			.Set<ushort>()
			.Add<Likes, Dogs>()
			;

	var character = it.Commands!.Spawn()
		.Set(new Serial() { Value = 0xDEAD_BEEF });

	Console.WriteLine("Setup done in {0} ms", sw.ElapsedMilliseconds);
}

static void ParseQuery(ref Iterator it)
{
	var posA = it.Field<Position>();
	var velA = it.Field<Velocity>();

	// while (posA.IsValid() && velA.IsValid())
	// {
	// 	posA.Value.X *= velA.Value.X;
	// 	posA.Value.Y *= velA.Value.Y;

	// 	posA.Next();
	// 	velA.Next();
	// }

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

struct Likes { }
struct Dogs { }
struct Apples { }

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


//var world = new World();


//var e0 = world.Spawn().Set<Position>();
//var e1 = world.Spawn();
//var likes = world.Spawn();
//var apple = world.Spawn();

//e0.SetID(e1);

//e0.Pair(likes, apple);

//e0.Each(s =>
//{
//	//ref var cmp = ref s.Get<EcsComponent>();
//	if (IDOp.IsPair(s.ID))
//	{
//		Console.WriteLine("pair: {0} {1}", IDOp.GetPairFirst(s.ID), IDOp.GetPairSecond(s.ID));
//	}
//	else
//	{
//		Console.WriteLine("entity {0}", s.ID);
//	}
//});

//var qry = world.Query().With<EcsComponent>();

//foreach (var it in qry)
//{
//	var cmpA = it.Field<EcsComponent>();
//	var entityA = it.Field<EntityView>();

//	for (int i = 0; i < it.Count; ++i)
//	{
//		ref var cmp = ref cmpA[i];
//		ref var ent = ref entityA[i];

//		var xx = it.World.Spawn();

//		Console.WriteLine("component --> ID: {0} - SIZE: {1}", ent.ID, cmp.Size);
//	}
//}

var world = new World();
var w0 = world.SpawnEmpty();
w0.Despawn();

var w1 = world.SpawnEmpty();
var oo = w1.IsAlive();


Console.WriteLine();

var ecs = new Ecs();

var pos = ecs.Spawn();
var vel = ecs.Spawn();

var likes = ecs.Spawn();
var cats = ecs.Spawn();

var childOf = ecs.Spawn();

var root = ecs.Spawn().ID;

var id = ecs.Spawn()
	.Set(new Position() { X = 10, Y = 29 })
	.Set<Likes, Dogs>()
	.Set(pos.ID)
	.Set(likes.ID, cats.ID)
	.Set(childOf.ID, root)
	.ID;

//ecs.Step(0f);

var ok = ecs.Entity(id)
	.Has<Likes, Dogs>();

var qry1 = ecs.Query().With(childOf.ID, root);

foreach (var it in qry1)
{
	
}

ecs.Entity(id).Each(s =>
{
	if (IDOp.IsPair(s.ID))
	{
		var first = IDOp.GetPairFirst(s.ID);
		var second = IDOp.GetPairSecond(s.ID);

		Console.WriteLine("pair: {0} {1}", first, second);
	}
	else
	{
		if (s.Has<EcsComponent>())
		{
			Console.WriteLine("entity {0} [component]", s.ID);
		}
		else
		{
			Console.WriteLine("entity {0} [entity]", s.ID);
		}
	}
});

unsafe
{
	var query = ecs.Query().With<Position>().With<Velocity>().Without<float>();
	var queryCmp = ecs.Query()
			.With<EcsComponent>();

	foreach (var it in queryCmp)
	{
		var cmpA = it.Field<EcsComponent>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var cmp = ref cmpA[i];
			var entity = it.Entity(i);

			Console.WriteLine("component --> ID: {0} - SIZE: {1} - CMP ID: {2}", entity, cmp.Size, cmp.ID);
		}
	}


	//ecs.SetSingleton<PlayerTag>(new PlayerTag() { ID = 123 });
	//ref var single = ref ecs.GetSingleton<PlayerTag>();

	ecs.AddStartupSystem(&Setup);

	//ecs.AddSystem(&PrintSystem)
	//	.SetTick(1f); // update every 50ms
	ecs.AddSystem(&ParseQuery)
		.SetQuery(query.ID);
	//ecs.AddSystem(&PrintWarnSystem);
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


static void Setup(Commands cmds, ref EntityIterator it)
{
	var sw = Stopwatch.StartNew();

	for (int i = 0; i < ENTITIES_COUNT; i++)
		cmds.Spawn()
			.Set<Position>()
			.Set<Velocity>();

	var character = cmds.Spawn()
		.Set(new Serial() { Value = 0xDEAD_BEEF });

	Console.WriteLine("Setup done in {0} ms", sw.ElapsedMilliseconds);
}

static void ParseQuery(Commands cmds, ref EntityIterator it)
{
	var posF = it.Field<Position>();
	var velF = it.Field<Velocity>();

	for (int i = 0; i < it.Count; ++i)
	{
		ref var pos = ref posF[i];
		ref var vel = ref velF[i];

		pos.X *= vel.X;
		pos.Y *= vel.Y;

		//cmds.Entity(it.Entity(i))
		//	.Set(1f);

		
	}
}

static void PrintSystem(Commands cmds, ref EntityIterator it)
{
	Console.WriteLine("1");
}

static void PrintWarnSystem(Commands cmds, ref EntityIterator it)
{
	//Console.WriteLine("3");
}


enum TileType
{
    Land,
    Static
}


struct Serial { public uint Value;  }
struct Position { public float X, Y, Z; }
struct Velocity { public float X, Y; }
struct PlayerTag { public ulong ID; }

struct Likes { }
struct Dogs { }
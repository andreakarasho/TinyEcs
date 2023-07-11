// https://github.com/jasonliang-dev/entity-component-system
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TinyEcs;

const int ENTITIES_COUNT = 524_288 * 2 * 1;


var ecs = new Ecs();

unsafe
{
	var query = ecs.Query().With<Position>().With<Velocity>().Without<float>();
	var queryCmp = ecs.Query()
			.With<EcsComponent>();

	foreach (var it in queryCmp)
	{
		var cmpA = it.Field<EcsComponent>();
		var entityA = it.Field<EntityView>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var cmp = ref cmpA[i];
			ref var ent = ref entityA[i];

			Console.WriteLine("component --> ID: {0} - SIZE: {1} - INDEX: {2}", ent.ID, cmp.Size, cmp.GlobalIndex);
		}
	}


	//ecs.SetSingleton<PlayerTag>(new PlayerTag() { ID = 123 });
	//ref var single = ref ecs.GetSingleton<PlayerTag>();

	ecs.AddStartupSystem(&Setup);

	//ecs.AddSystem(&PrintSystem)
	//	.SetTick(1f); // update every 50ms
	ecs.AddSystem(&ParseQuery)
		.SetQuery(in query);
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

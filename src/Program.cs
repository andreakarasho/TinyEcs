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

for (int i = 0; i < ENTITIES_COUNT; i++)
	ecs.Spawn()
	   .Set<Position>()
	   .Set<Velocity>();

var builder = ecs.Query()
	.With<Position>()
	.With<Velocity>()
	.Without<float>()
	;

unsafe
{
	ecs.AddStartupSystem(ecs.Query(), &Setup);
	ecs.AddSystem(in builder, &ParseQuery);
}

var sw = Stopwatch.StartNew();

while (true)
{
    sw.Restart();

    for (int i = 0; i < 3600; ++i)
	    ecs.Step(0f);

	Console.WriteLine("query done in {0} ms", sw.ElapsedMilliseconds);
}


static void Setup(Commands cmds, ref EntityIterator it)
{
	var sw = Stopwatch.StartNew();

	for (int i = 0; i < 3; i++)
		cmds.Spawn()
			.Set<Position>()
			.Set<Velocity>()
			.Set<int>(123);

	var character = cmds.Spawn()
		.Set(new Serial() { Value = 0xDEAD_BEEF });

	Console.WriteLine("Setup done in {0} ms", sw.ElapsedMilliseconds);
}

static void ParseQuery(Commands cmds, ref EntityIterator it)
{

#if true
	var entA = it.Span<EntityView>();
	var posF = it.Span<Position>();
	var velF = it.Span<Velocity>();

	for (int i = 0; i < it.Count; ++i)
	{
		ref var pos = ref posF[i];
		ref var vel = ref velF[i];
		//ref var ent = ref entA[i];

		pos.X *= vel.X;
		pos.Y *= vel.Y;
	}

#else
	var posF = it.Field<Position>();
	var velF = it.Field<Velocity>();
	foreach (ref readonly var e in it)
	{
		ref var pos = ref posF.Get();
		ref var vel = ref velF.Get();

		pos.X *= vel.X;
		pos.Y *= vel.Y;
	}
#endif
}


enum TileType
{
    Land,
    Static
}


struct Serial { public uint Value;  }
struct Position { public float X, Y, Z; }
struct Velocity { public float X, Y; }
struct PlayerTag { }

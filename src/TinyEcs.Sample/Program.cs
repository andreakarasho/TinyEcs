// https://github.com/jasonliang-dev/entity-component-system
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TinyEcs;

const int ENTITIES_COUNT = 524_288 * 2 * 1;

using var ecs = new World();

unsafe
{
	var text0 = Generate.CreateQueries(false);
	var text1 = Generate.CreateQueries(true);

	File.WriteAllText("a.txt", text0);
	File.WriteAllText("b.txt", text1);

	for (int i = 0; i < ENTITIES_COUNT; i++)
		ecs.Entity()
			.Set<Position>(new Position())
			.Set<Velocity>(new Velocity());

	//ecs.Query()
	//	.System<EcsSystemPhaseOnUpdate, Position, Velocity>(static (ref Position pos, ref Velocity vel) =>
	//	{
	//		pos.X *= vel.X;
	//		pos.Y *= vel.Y;
	//	});

	//ecs.Query()
	//	//.Without<PlayerTag>()
	//	.System<EcsSystemPhaseOnUpdate, Position, Velocity>((ref readonly EntityView entity, ref Position pos, ref Velocity vel) =>
	//	{
	//		pos.X *= vel.X;
	//		pos.Y *= vel.Y;
	//	});
}

var query = ecs.Query()
	.With<Position>()
	.With<Velocity>();

IteratorDelegate del = Iter;

var sw = Stopwatch.StartNew();
var start = 0f;
var last = 0f;

while (true)
{
	var cur = (start - last) / 1000f;

	for (int i = 0; i < 3600; ++i)
	{
		query.Iterate(del);
		//ecs.Step(cur);

		// foreach (var archetype in query)
		// {
		// 	var count = archetype.Count;
		// 	ref var pos = ref MemoryMarshal.GetReference(archetype.ComponentData<Position>());
		// 	ref var vel = ref MemoryMarshal.GetReference(archetype.ComponentData<Velocity>());
		//
		// 	ref var last2 = ref Unsafe.Add(ref pos, count);
		//
		// 	while (Unsafe.IsAddressLessThan(ref pos, ref last2))
		// 	{
		// 		pos.X *= vel.X;
		// 		pos.Y *= vel.Y;
		//
		// 		pos = ref Unsafe.Add(ref pos, 1);
		// 		vel = ref Unsafe.Add(ref vel, 1);
		// 	}
		// }
	}

	last = start;
	start = sw.ElapsedMilliseconds;

	Console.WriteLine("query done in {0} ms", start - last);
}

static void Iter(ref Iterator it)
{
	ref var pos = ref it.FieldRef<Position>(0);
	ref var vel = ref it.FieldRef<Velocity>(1);
	ref var last = ref Unsafe.Add(ref pos, it.Count);

	while (Unsafe.IsAddressLessThan(ref pos, ref last))
	{
		pos.X *= vel.X;
		pos.Y *= vel.Y;

		pos = ref Unsafe.Add(ref pos, 1);
		vel = ref Unsafe.Add(ref vel, 1);
	}
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

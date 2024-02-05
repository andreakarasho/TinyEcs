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
			.Set<Velocity>(new Velocity())
			//.Set<ManagedData>(new ManagedData() { Integer = i, Text = i.ToString() })
			;

	ecs.Entity()
			.Set<PlayerTag>()
			.Set<Position>(new Position() { X = 1 })
			.Set<Velocity>(new Velocity() { Y = 1 })
			.Set<ManagedData>(new ManagedData() { Integer = 1, Text = "PALLE" });

	ecs.Query()
		.With<EcsComponent>()
		.Iterate(static (ref Iterator it) => {
			var cmpA = it.Field<EcsComponent>(0);

			for (int i = 0; i < it.Count; ++i)
			{
				ref var cmp = ref cmpA[i];
				var entity = it.Entity(i);

				Console.WriteLine("{0} --> ID: {1} - SIZE: {2}", cmp.Size <= 0 ? "tag      " : "component", entity.ID, cmp.Size);
			}
		});

	//ecs.Query()
	//	.With<Position>()
	//	.With<Velocity>()
	//	//.With<ManagedData>()
	//	//.Without<PlayerTag>()
	//	.System(static (ref Iterator it) =>
	//	{
	//		var posA = it.Field<Position>(0);
	//		var velA = it.Field<Velocity>(1);
	//		//var manA = it.Field<ManagedData>(2);

	//		for (int i = 0, count = it.Count; i < count; ++i)
	//		{
	//			ref var pos = ref posA[i];
	//			ref var vel = ref velA[i];
	//			//ref var man = ref manA[i];

	//			pos.X *= vel.X;
	//			pos.Y *= vel.Y;
	//		}
	//	});

	//ecs.Query()
	//	.With<Position>()
	//	.With<Velocity>()
	//	.With<ManagedData>()
	//	.With<PlayerTag>()
	//	.System(static (ref Iterator it) =>
	//	{
	//		var posA = it.Field<Position>(0);
	//		var velA = it.Field<Velocity>(1);
	//		var manA = it.Field<ManagedData>(2);

	//		for (int i = 0, count = it.Count; i < count; ++i)
	//		{
	//			ref var pos = ref posA[i];
	//			ref var vel = ref velA[i];
	//			ref var man = ref manA[i];

	//			pos.X *= vel.X;
	//			pos.Y *= vel.Y;
	//		}
	//	});

	ecs.Query()
		.System<EcsSystemPhaseOnUpdate, Position, Velocity>(static (ref Position pos, ref Velocity vel) =>
		{
			pos.X *= vel.X;
			pos.Y *= vel.Y;
		});

	//ecs.Query()
	//	//.Without<PlayerTag>()
	//	.System<EcsSystemPhaseOnUpdate, Position, Velocity>((ref readonly EntityView entity, ref Position pos, ref Velocity vel) =>
	//	{
	//		pos.X *= vel.X;
	//		pos.Y *= vel.Y;
	//	});
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

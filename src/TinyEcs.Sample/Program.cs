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
var cmds = new Commands2(world);
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

unsafe
{
	// world.Query(stackalloc EntityID[] {
	// 		world.Component<float>(),
	// 		world.Component<byte>()
	// 	},
	// 	Span<EntityID>.Empty,
	// 	static arch => {
	// 		Console.WriteLine("arch: [{0}]", string.Join(", ", arch.Components));
	// 	}
	// );
}

world.Query().With<float>().With<int>().Iterate(static a => {
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
	.Set<Likes, Dogs>()
	.Set<Likes, Apples>()
	.Set(pos.ID)
	.Set(likes.ID, cats.ID)
	.Set(likes.ID, flowers.ID)
	.Set(childOf.ID, root)
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
		.Iterate(static it => {
			var cmpA = it.Field<EcsComponent>();

			for (int i = 0; i < it.Count; ++i)
			{
				ref var cmp = ref cmpA[i];
				var entity = it.Entity(i);

				Console.WriteLine("component --> ID: {0} - SIZE: {1} - CMP ID: {2}", entity.ID, cmp.Size, cmp.ID);
			}
		});



	//ecs.SetSingleton<PlayerTag>(new PlayerTag() { ID = 123 });
	//ref var single = ref ecs.GetSingleton<PlayerTag>();

	ecs.AddStartupSystem(&Setup);

	//ecs.AddSystem(&PrintSystem)
	//	.SetTick(1f); // update every 50ms
	ecs.AddSystem(&ParseQuery)
		.SetQuery(ecs.Query().With<Position>().With<Velocity>().Without<float>());

		//.SetQuery(query.ID);
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


static void Setup(Iterator it)
{
	var sw = Stopwatch.StartNew();

	for (int i = 0; i < ENTITIES_COUNT; i++)
		it.Commands!.Spawn()
			.Set<Position>()
			.Set<Velocity>();

	var character = it.Commands!.Spawn()
		.Set(new Serial() { Value = 0xDEAD_BEEF });

	Console.WriteLine("Setup done in {0} ms", sw.ElapsedMilliseconds);
}

static void ParseQuery(Iterator it)
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

static void PrintSystem(Iterator it)
{
	Console.WriteLine("1");
}

static void PrintWarnSystem(Iterator it)
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

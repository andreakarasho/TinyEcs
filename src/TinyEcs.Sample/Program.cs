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

using var world = new World<Context1>();

var positionID = world.Spawn()
	.Component<Position>();

var velocityID = world.Spawn()
	.Component<Velocity>();


var main = world.Spawn();
var secondMain = world.Spawn();

unsafe
{
	world.Spawn()
		.System
		(
			&SystemCtx1,
			positionID,
			velocityID
		)
		.Set<EcsPhase, EcsSystemPhaseOnUpdate>();

	// world.System
	// (
	// 	&SystemCtx1,
	// 	positionID,
	// 	velocityID
	// );

	// world.Event
	// (
	// 	&ObserveThings,
	// 	stackalloc Term[] {
	// 		Term.With(world.Component<Position>().ID),
	// 		//Term.With(world.Component<Velocity>().ID),
	// 		//Term.With(world.Pair<EcsChildOf>(main.ID))
	// 	},
	// 	stackalloc EntityID[] {
	// 		world.Component<CustomEvent>().ID
	// 		//world.Component<EcsObserverOnSet>().ID,
	// 		//world.Component<EcsObserverOnUnset>().ID
	// 	}
	// );

	//world.Event(&ObserveThings, [ Position ], [ CustomEvent ]);

	//world.Despawn(world.Component<Position>().ID);
	main.Set<Velocity>();
	main.Set<Position>(new Position() { X = -123, Y = 456, Z = 0.123388f });
	//world.EmitEvent<CustomEvent, Position>(main);

		// .With<Position>()
		// .With<Velocity>()
		// .With<EcsChildOf>(main.ID)
		// .OnEvent<EcsObserverOnSet>()
		// .OnEvent<EcsObserverOnUnset>();
}


for (int i = 0; i < 10; ++i)
	world.Spawn().Set<Position>().Set<Velocity>().ChildOf(main);

// main.Set<Position>(new Position() { X = 12, Y = -2, Z = 0.8f });
// main.Set<Dogs>();
// main.Set<Likes>();
// main.Set<Velocity>(new Velocity() { X = 345f, Y = 0.23f});
// main.Unset<Velocity>();
// main.Unset<Velocity>();

for (int i = 0; i < 10; ++i)
	world.Spawn().ChildOf(main);

main.Children(static s => {
	var p = s.Parent();
	Console.WriteLine("child id {0}", s.ID);
});

secondMain.Children(static s => {
	var p = s.Parent();
	Console.WriteLine("secondMain child id {0}", s.ID);
});

// while (true)
// 	world.Step();

main.Despawn();
//main.ClearChildren();

world.Query().With<EcsChildOf>(main.ID).Iterate(static (ref Iterator<Context1> it) => {
	Console.WriteLine("found children");
});

world.Query().With<EcsChildOf, EcsAny>().Iterate(static (ref Iterator<Context1> it) => {
	Console.WriteLine("found children for any");
});

var ecs = new World<Context2>();


// var pos = ecs.Spawn();
// var vel = ecs.Spawn();
// var likes = ecs.Spawn();
// var cats = ecs.Spawn();
// var flowers = ecs.Spawn();
// var childOf = ecs.Spawn();
// var root = ecs.Spawn().ID;

// var id = ecs.Spawn()
// 	.Set(new Position() { X = 10, Y = 29 })
// 	.Set<Likes, Dogs>()
// 	.Set<Likes, Apples>()
// 	.Set(pos.ID)
// 	.Set(likes.ID, cats.ID)
// 	.Set(likes.ID, flowers.ID)
// 	.Set(childOf.ID, root)
// 	.ID;

// var pairLikesDogID = ecs.Pair<Likes, Dogs>();

// var ent = ecs.Spawn().Set(new Position() { X = 20, Y = 9 });

// ref var p = ref ent.Get<Position>();
// p.X = 9999;
// p.Y = 12;
// p.Z = 0.2f;

// //ecs.Step(0f);

// //ref var posp = ref ecs.Entity(ent.ID).Get<Position>();

// var ok = ecs.Entity(id)
// 	.Has<Likes, Dogs>();

// var qry1 = ecs.Query().With(childOf.ID, root);

// // foreach (var it in qry1)
// // {

// // }

// ecs.Entity(id).Each(static s =>
// {
// 	if (s.IsPair())
// 	{
// 		Console.WriteLine("pair: ({0}, {1})", s.First(), s.Second());
// 	}
// 	else if (s.IsEntity())
// 	{
// 		Console.WriteLine("entity: {0}", s.ID);
// 	}
// 	else
// 	{
// 		Console.WriteLine("unknown: {0}", s.ID);
// 	}
// });

unsafe
{
	ecs.Query()
		.With<EcsComponent>()
		.Iterate(static (ref Iterator<Context2> it) => {
			var cmpA = it.Field<EcsComponent>();

			for (int i = 0; i < it.Count; ++i)
			{
				ref var cmp = ref cmpA[i];
				var entity = it.Entity(i);

				Console.WriteLine("{0} --> ID: {1} - SIZE: {2}", cmp.Size <= 0 ? "tag      " : "component", entity.ID, cmp.Size);
			}
		});


	ecs.StartupSystem(&Setup);
	//ecs.System(&PrintSystem, 1f);
	//ecs.System(&PrintComponents, ecs.Query().With<EcsComponent>(), 1f);
	ecs.System(&ParseQuery, ecs.Query()
		.With<Position>()
		.With<Velocity>()
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


static void Setup(ref Iterator<Context2> it)
{
	var sw = Stopwatch.StartNew();

	for (int i = 0; i < ENTITIES_COUNT; i++)
		it.World.Spawn()
			.Set<Position>()
			.Set<Velocity>()
			// .SetTag<int>()
			// .SetTag<short>()
			// .SetTag<byte>()
			// .SetTag<decimal>()
			// .SetTag<uint>()
			// .SetTag<ushort>()
			.Set<Likes, Dogs>()
			;

	var character = it.Commands!.Spawn()
		.Set(new Serial() { Value = 0xDEAD_BEEF });

	Console.WriteLine("Setup done in {0} ms", sw.ElapsedMilliseconds);
}

static void PrintComponents(ref Iterator<Context2> it)
{
	var cmpA = it.Field<EcsComponent>();

	for (int i = 0; i < it.Count; ++i)
	{
		ref var cmp = ref cmpA[i];
		Console.WriteLine("{0} --> ID: {1} - SIZE: {2}", cmp.Size <= 0 ? "tag      " : "component", cmp.ID, cmp.Size);
	}
}

static void ParseQuery(ref Iterator<Context2> it)
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

static void PrintSystem(ref Iterator<Context2> it)
{
	Console.WriteLine("1");
}

static void PrintWarnSystem(ref Iterator<Context2> it)
{
	//Console.WriteLine("3");
}


static void ObserveThings(ref Iterator<Context1> it)
{
	var posA = it.Field<Position>();
	var velA = it.Field<Velocity>();

	var isAdded = it.EventID == it.World.Component<EcsEventOnSet>().ID;
	var first = isAdded ? "added" : "removed";
	var sec = isAdded ? "to" : "from";

	for (int i = 0; i < it.Count; ++i)
	{
		ref var pos = ref posA[i];
		ref var vel = ref velA[i];

		Console.WriteLine(
			"{0} position {1} {2} {3}",
			first,
			$"pos [{pos.X}, {pos.Y}, {pos.Z}], vel [{vel.X}, {vel.Y}]",
			sec,
			it.Entity(i).ID
		 );
	}
}

static void SystemCtx1(ref Iterator<Context1> it)
{
	Console.WriteLine("ok");
}

enum TileType
{
	Land,
	Static
}


struct Serial : IComponent { public uint Value; }
struct Position : IComponent { public float X, Y, Z; }
struct Velocity : IComponent { public float X, Y; }
struct PlayerTag : IComponent { public ulong ID; }

struct CustomEvent : IEvent { }

struct Likes : ITag;
struct Dogs : ITag { }
struct Apples : ITag { }

struct TestStr { public byte v; }



struct Context1 {}
struct Context2 {}

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

// using var world = new World();
// var tttt = world.Entity(1).Type();
// world.Entity(1).Set(2);

// world.PrintGraph();


// var positionID = world.Entity<Position>();
// var velocityID = world.Entity<Velocity>();
// var pairID = world.Pair<Likes, Dogs>();

// var main = world.Entity();
// var secondMain = world.Entity();

// ref var rec = ref world.GetRecord(secondMain);

//  var ooo = world.Entity();
//  ooo.ChildOf(main);
//  ooo.ChildOf(secondMain);

// unsafe
// {
// 	Term t = Term.With(positionID.ID);
// 	t.Not();
// 	t = !t;


// 	world.Entity()
// 		.System(&SystemCtx1, positionID, +velocityID, -pairID)
// 		.Set<EcsPhase, EcsSystemPhaseOnUpdate>();

// 	// world.New()
// 	// 	.System
// 	// 	(
// 	// 		&SystemCtx1,
// 	// 		[positionID, velocityID],
// 	// 		[pairID],
// 	// 		float.NaN
// 	// 	)
// 	// 	.Set<EcsPhase, EcsSystemPhaseOnUpdate>();

// 	// world.New()
// 	// 	.System
// 	// 	(
// 	// 		&SystemCtx1,
// 	// 		positionID,
// 	// 		velocityID
// 	// 	)
// 	// 	.Set<EcsPhase, EcsSystemPhaseOnUpdate>();

// 	// world.System
// 	// (
// 	// 	&SystemCtx1,
// 	// 	positionID,
// 	// 	velocityID
// 	// );

// 	// world.Event
// 	// (
// 	// 	&ObserveThings,
// 	// 	stackalloc Term[] {
// 	// 		Term.With(world.Component<Position>().ID),
// 	// 		//Term.With(world.Component<Velocity>().ID),
// 	// 		//Term.With(world.Pair<EcsChildOf>(main.ID))
// 	// 	},
// 	// 	stackalloc EcsID[] {
// 	// 		world.Component<CustomEvent>().ID
// 	// 		//world.Component<EcsObserverOnSet>().ID,
// 	// 		//world.Component<EcsObserverOnUnset>().ID
// 	// 	}
// 	// );

// 	//world.Event(&ObserveThings, [ Position ], [ CustomEvent ]);

// 	//world.Despawn(world.Component<Position>().ID);
// 	main.Set<Velocity>();
// 	main.Set<Position>(new Position() { X = -123, Y = 456, Z = 0.123388f });
// 	//main.Set<Likes,Dogs>();
// 	//world.EmitEvent<CustomEvent, Position>(main);

// 		// .With<Position>()
// 		// .With<Velocity>()
// 		// .With<EcsChildOf>(main.ID)
// 		// .OnEvent<EcsObserverOnSet>()
// 		// .OnEvent<EcsObserverOnUnset>();
// }


// for (int i = 0; i < 10; ++i)
// 	world.Entity().Set<Position>().Set<Velocity>().ChildOf(main).ChildOf(secondMain);

// // main.Set<Position>(new Position() { X = 12, Y = -2, Z = 0.8f });
// // main.Set<Dogs>();
// // main.Set<Likes>();
// // main.Set<Velocity>(new Velocity() { X = 345f, Y = 0.23f});
// // main.Unset<Velocity>();
// // main.Unset<Velocity>();

// // for (int i = 0; i < 10; ++i)
// // 	world.New().ChildOf(main);

// main.Children(static s => {
// 	var p = s.Parent();
// 	Console.WriteLine("child id {0}", s.ID);
// });

// secondMain.Children(static s => {
// 	var p = s.Parent();
// 	Console.WriteLine("secondMain child id {0}", s.ID);
// });

// // while (true)
// // 	world.Step();
// world.Step();

// secondMain.Delete();
// //main.ClearChildren();

// world.Query().With<EcsChildOf>(main.ID).Iterate(static (ref Iterator it) => {
// 	Console.WriteLine("found children");
// });

// world.Query().With<EcsChildOf, EcsAny>().Iterate(static (ref Iterator it) => {
// 	Console.WriteLine("found children for any");
// });
using var ecs = new World();

unsafe
{
	var posID = ecs.Entity<Position>();
	var velID = ecs.Entity<Velocity>();
	var serialID = ecs.Entity<Serial>();

	ecs.SetSingleton(new Serial(){Value = 1});

	for (int i = 0; i < 100; i++)
		ecs.Entity()
			.Set<Position>(new Position())
			.Set<Velocity>(new Velocity())
			;

	// ecs.Entity()
	// 	.System(&Setup)
	// 	.Set<EcsPhase, EcsSystemPhaseOnStartup>();

	ecs.Entity()
		.System(&ParseQuery, posID, velID, Term.Singleton(serialID))
		.Set<EcsPhase, EcsSystemPhaseOnUpdate>();

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



	Console.WriteLine("Setup done in {0} ms", sw.ElapsedMilliseconds);
}

static void PrintComponents(ref Iterator it)
{
	var cmpA = it.Field<EcsComponent>(0);

	for (int i = 0; i < it.Count; ++i)
	{
		ref var cmp = ref cmpA[i];
		Console.WriteLine("{0} --> ID: {1} - SIZE: {2}", cmp.Size <= 0 ? "tag      " : "component", cmp.ID, cmp.Size);
	}
}

static void ParseQuery(ref Iterator it)
{
	var posA = it.Field<Position>(0);
	var velA = it.Field<Velocity>(1);
	ref var singleton = ref it.Single<Serial>(2);

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


static void ObserveThings(ref Iterator it)
{
	var posA = it.Field<Position>(0);
	var velA = it.Field<Velocity>(1);

	var isAdded = it.EventID == it.World.Entity<EcsEventOnSet>();
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

static void SystemCtx1(ref Iterator it)
{
	Console.WriteLine("ok");
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

struct CustomEvent { }

struct Likes;
struct Dogs { }
struct Apples { }

struct TestStr { public byte v; }



struct Context1 {}
struct Context2 {}

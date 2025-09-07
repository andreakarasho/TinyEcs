// https://github.com/jasonliang-dev/entity-component-system
using System.Diagnostics;
using TinyEcs;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

const int ENTITIES_COUNT = (524_288 * 2 * 1);

using var ecs = new World();
var scheduler = new Scheduler(ecs);

scheduler.AddResource(new CustomResource());
scheduler.AddPlugin<TestPlugin>();
scheduler.AddPlugin<ANamespace.AAA>();

// scheduler.AddPlugin<TestSys>();
// scheduler.AddPlugin<TestSys2>();
scheduler.RunOnce();

return;
// ecs.Entity();
// ecs.Entity().Delete();
// var xx = ecs.Entity().Add<Tag>();
//
// xx.Get<Tag>();
// Console.WriteLine("");

// scheduler.OnUpdate((Query<Data<Position>, Changed<Position>> query) =>
// {
// 	foreach ((var ent, var pos) in query)
// 	{

// 	}
// }, ThreadingMode.Single);


// var ab = ecs.Entity()
// 	.Set(new Position()).Set(new Velocity());
// ecs.Entity()
// 	.Set(new Position() { X = -1 });
// // var q = ecs.QueryBuilder()
// // 	.With<Position>()
// // 	.Changed<Position>()
// // 	.Build();

// // var a = new QueryIter<Data<Position>, Changed<Position>>(q.Iter());

// // foreach ((var ent, var pos) in a)
// // {
// // 	pos.Ref.X *= 2;
// // 	pos.Ref.Y *= 2;
// // }


// // foreach ((var ent, var pos) in a)
// // {
// // 	pos.Ref.X *= 2;
// // 	pos.Ref.Y *= 2;
// // }


// scheduler.RunOnce();
// scheduler.RunOnce();

// ab.Set(new Position() { X = 2 });
// scheduler.RunOnce();

// scheduler.AddState(GameState.Loading);
// scheduler.AddState(AnotherState.C);

// scheduler.OnUpdate(() =>
// {
// 	Console.WriteLine("im in loading state");
// }, ThreadingMode.Single)
// .RunIf((SchedulerState state) => state.InState(GameState.Loading))
// .RunIf((SchedulerState state) => state.InState(AnotherState.A));


// scheduler.OnEnter(GameState.Loading, () => Console.WriteLine("on enter loading"), ThreadingMode.Single);
// scheduler.OnEnter(GameState.Loading, () => Console.WriteLine("on enter loading 2"), ThreadingMode.Single);
// scheduler.OnExit(GameState.Loading, () => Console.WriteLine("on exit loading"), ThreadingMode.Single);

// scheduler.OnEnter(GameState.Playing, () => Console.WriteLine("on enter playing"), ThreadingMode.Single);
// scheduler.OnExit(GameState.Playing, () => Console.WriteLine("on exit playing"), ThreadingMode.Single);

// scheduler.OnEnter(GameState.Menu, () => Console.WriteLine("on enter Menu"), ThreadingMode.Single);
// scheduler.OnExit(GameState.Menu, () => Console.WriteLine("on exit Menu"), ThreadingMode.Single);

// scheduler.OnUpdate((State<GameState> state, State<AnotherState> anotherState, Local<float> loading, Local<GameState[]> states, Local<int> index) =>
// {
// 	states.Value ??= Enum.GetValues<GameState>();

// 	loading.Value += 0.1f;
// 	// Console.WriteLine("next {0:P}", loading.Value);

// 	Console.WriteLine("current state: {0}", state.Current);

// 	if (loading.Value >= 1f)
// 	{
// 		loading.Value = 0f;
// 		// Console.WriteLine("on swapping state");
// 		state.Set(states.Value[(++index.Value) % states.Value.Length]);
// 		anotherState.Set(AnotherState.A);
// 	}

// }, threadingType: ThreadingMode.Single);


// while (true)
// 	scheduler.RunOnce();

for (var i = 0; i < ENTITIES_COUNT; i++)
    ecs.Entity()
        .Set(new Position())
        .Set(new Velocity());

// ecs.Entity().Set(new Position()).Set(new Velocity()).Set(new Mass());

// ecs.Entity()
// 	.Set(new Velocity());
// ecs.Entity()
// 	.Set(new Position());

// ecs.Entity().Set(new Velocity()).Set(new Position());
scheduler.AddSystem([TinySystem] (
    Query<Data<Position, Velocity>> q,
    Query<Data<Position, Velocity>, Added<Position>> added
) =>
{

    // foreach ((var pos, var vel) in or)
    // {
    // 	if (pos.IsValid())
    // 	{
    // 		Console.Write("pos is valid ");
    // 	}
    // 	else
    // 	{

    // 	}

    // 	if (vel.IsValid())
    // 	{
    // 		Console.Write("vel is valid");
    // 	}
    // 	else
    // 	{

    // 	}

    // 	Console.WriteLine();
    // }

    foreach (var (pos, vel) in q)
    {
        pos.Ref.X *= vel.Ref.X;
        pos.Ref.Y *= vel.Ref.Y;

        // pos.Ref.X *= vel.Ref.X;
        // pos.Ref.Y *= vel.Ref.Y;

        // if (pos.IsChanged)
        // 	pos.ClearState();
    }

    // foreach ((var pos, var vel) in added)
    // {
    // 	pos.Ref.X *= vel.Ref.X;
    // 	pos.Ref.Y *= vel.Ref.Y;

    // 	// if (pos.IsAdded)
    // 	// 	pos.ClearState();
    // }
}, threadingType: ThreadingMode.Single);


var query = ecs.QueryBuilder()
    .With<Position>()
    .With<Velocity>()
    .Build();

var sw = Stopwatch.StartNew();
var start = 0f;
var last = 0f;

while (true)
{
    for (int i = 0; i < 3600; ++i)
    {
        // scheduler.RunOnce();

        Execute(query);
        // ExecuteIterator(query);

        // var it = query.Iter();
        // while (it.Next())
        // {
        // 	var count = it.Count;

        // 	ref var pos = ref it.DataRef<Position>(0);
        // 	ref var vel = ref it.DataRef<Velocity>(1);
        // 	ref var lastPos = ref Unsafe.Add(ref pos, count);

        // 	while (Unsafe.IsAddressLessThan(ref pos, ref lastPos))
        // 	{
        // 		pos.X *= vel.X;
        // 		pos.Y *= vel.Y;

        // 		pos = ref Unsafe.Add(ref pos, 1);
        // 		vel = ref Unsafe.Add(ref vel, 1);
        // 	}
        // }
    }

    last = start;
    start = sw.ElapsedMilliseconds;

    Console.WriteLine("query done in {0} ms", start - last);
}


static void Execute(Query query)
{
    foreach (var (pos, vel) in Data<Position, Velocity>.CreateIterator(query.Iter()))
    {
        pos.Ref.X *= vel.Ref.X;
        pos.Ref.Y *= vel.Ref.Y;
    }
}

[TinySystem]
static void ExecuteIterator(Query query)
{
    var it = query.Iter();

    while (it.Next())
    {
        var span0 = it.Data<Position>(0);
        var span1 = it.Data<Velocity>(1);
        var count = it.Count;

        for (var i = 0; i < count; ++i)
        {
            ref var pos = ref span0[i];
            ref var vel = ref span1[i];

            pos.X *= vel.X;
            pos.Y *= vel.Y;
        }
    }
}

public struct Position
{
    public float X, Y, Z;
}

public struct Velocity
{
    public float X, Y;
}

struct Mass { public float Value; }

struct Tag { }


enum GameState
{
    Loading,
    Playing,
    Menu,
    Menu2
}

enum AnotherState
{
    A, B, C
}



// [TinyPlugin]
// partial struct BBB
// {
// 	[TinySystem(Stages.Update, ThreadingMode.Single)]
// 	void Execute(Query<Data<Position, Velocity>> query)
// 	{
// 		foreach (var (pos, vel) in query)
// 		{
// 			pos.Ref.X *= vel.Ref.X;
// 			pos.Ref.Y *= vel.Ref.Y;
// 		}
// 	}
//
// 	public void Build(Scheduler scheduler)
// 	{
//
// 	}
// }

// class TestSys : IPlugin
// {
//
// 	public void Build(Scheduler scheduler)
// 	{
// 		scheduler.OnUpdate2(TestMethod);
// 		var xx = TestMethod2;
// 		scheduler.OnUpdate2(xx);
// 		scheduler.OnUpdate2(TestMethod2);
// 		scheduler.OnUpdate2(TestMethod2);
// 		scheduler.OnUpdate2(TestMethod2);
// 		scheduler.OnUpdate2(Palle)
// 			.RunIf((World w) =>
// 			{
// 				return true;
// 			});
//
// 		scheduler.OnUpdate2((Local<int> w) =>
// 		{
//
// 		});
//
//
// 		scheduler.OnStartup2((World w) => { });
// 	}
//
// 	void TestMethod(World world, Query<Data<Position, Velocity>> query)
// 	{
//
// 	}
//
// 	void TestMethod2(World world, Query<Data<Position, Velocity>> query)
// 	{
//
// 	}
//
// 	void Palle()
// 	{
//
// 	}
// }
//
// class TestSys2 : IPlugin
// {
//
// 	public void Build(Scheduler scheduler)
// 	{
// 		scheduler.OnUpdate2(TestMethod);
// 		var xx = TestMethod2;
// 		scheduler.OnUpdate2(xx);
// 		scheduler.OnUpdate2(TestMethod2, ThreadingMode.Single);
// 		scheduler.OnUpdate2(TestMethod2);
// 		scheduler.OnUpdate2(TestMethod2);
// 		scheduler.OnUpdate2(Palle)
// 			.RunIf((World w) =>
// 			{
// 				return true;
// 			});
//
// 		scheduler.OnUpdate2((Local<int> w) =>
// 		{
//
// 		});
// 	}
//
// 	void TestMethod(World world, Query<Data<Position, Velocity>> query)
// 	{
//
// 	}
//
// 	void TestMethod2(World world, Query<Data<Position, Velocity>> query)
// 	{
//
// 	}
//
// 	void Palle()
// 	{
//
// 	}
// }


// namespace TinyEcs
// {
// 	public delegate void SystemFn(World param1, SchedulerState param2);
// 	public partial class Scheduler {
// 		public void OnUpdate2(SystemFn fn)
// 		{
//
// 		}
// 	}
// }

// partial class GenSystem : TinySystem2
// {
// 	private Query<Data<Position, Velocity>> _query;
//
// 	public override void Setup(World world)
// 	{
// 		_query = (Query<Data<Position, Velocity>>)Query<Data<Position, Velocity>>.Generate(world);
// 	}
//
// 	public override void Execute(World world)
// 	{
// 		Execute(_query);
// 	}
// }


[TinySystem]
partial class GenSystem
{
	void Execute(World world, Query<Data<Position, Velocity>> query, Res<CustomResource> customRes)
	{

	}
}


[TinySystem]
partial class HelloSystem
{
	void Execute(Query<Data<Position, Velocity>> query, Local<CustomResource> customRes)
	{

	}
}

namespace ANamespace
{
	[TinyPlugin]
	public partial class AAA
	{
		[TinySystem]
		[RunIf(nameof(TestRun))]
		[RunIf(nameof(TestRun2))]
		void Execute(Query<Data<Position, Velocity>> query)
		{
			foreach (var (pos, vel) in query)
			{
				pos.Ref.X *= vel.Ref.X;
				pos.Ref.Y *= vel.Ref.Y;
			}
		}


		[TinySystem(Stages.OnEnter, ThreadingMode.Single)]
		static void CheckState(World world)
		{

		}

		[TinySystem]
		[RunIf(nameof(TestRun2))]
		[RunIf(nameof(TestRun2))]
		[RunIf(nameof(TestRun2))]
		[RunIf(nameof(TestRun2))]
		static void DoThat(Query<Data<Position, Velocity>> query, EventWriter<CustomEvent> writer)
		{
			foreach (var (pos, vel) in query)
			{
				pos.Ref.X *= vel.Ref.X;
				pos.Ref.Y *= vel.Ref.Y;
			}
		}


		[TinySystem(threadingMode: ThreadingMode.Single), AfterOf(nameof(Second))]
		void First()
		{
			Console.WriteLine("1");
		}

		[TinySystem(threadingMode: ThreadingMode.Single), AfterOf(nameof(Third))]
		void Second(EventReader<CustomEvent> reader)
		{
			Console.WriteLine("2");
		}

		[TinySystem(threadingMode: ThreadingMode.Single), BeforeOf("HELLO")]
		void Third()
		{
			Console.WriteLine("3");
		}

		private bool TestRun(SchedulerState state, World world, Local<int> index)
		{
			return true;
		}

		private bool TestRun2()
		{
			return true;
		}

		public void Build(Scheduler scheduler)
		{
			scheduler.AddEvent<CustomEvent>();
		}

		struct CustomEvent
		{
			public int Value;
		}
	}

}

[TinyPlugin]
public partial class TestPlugin
{
	public void Build(Scheduler scheduler)
	{

	}
}


public class CustomResource
{

}

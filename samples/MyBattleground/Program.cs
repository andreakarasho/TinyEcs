// https://github.com/jasonliang-dev/entity-component-system
using System.Diagnostics;
using TinyEcs;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;

const int ENTITIES_COUNT = (524_288 * 2 * 1);

using var ecs = new World();
var scheduler = new Scheduler(ecs);



// scheduler.OnUpdate((Query<Data<Position>, Changed<Position>> query) =>
// {
// 	foreach ((var ent, var pos) in query)
// 	{

// 	}
// }, ThreadingMode.Single);


var ab = ecs.Entity()
	.Set(new Position()).Set(new Velocity());
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

ab.Set(new Position() { X = 2 });
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

for (int i = 0; i < ENTITIES_COUNT; i++)
	ecs.Entity()
		.Set<Position>(new Position())
		.Set<Velocity>(new Velocity());

// ecs.Entity().Set(new Position()).Set(new Velocity()).Set(new Mass());



scheduler.AddSystem((
	Query<Data<Position, Velocity>, With<Position>> q,
	Query<Data<Position, Velocity>, Added<Position>> added
) =>
{
	foreach ((var pos, var vel) in q)
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
		scheduler.RunOnce();

		// Execute(query);
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
	foreach ((var pos, var vel) in Data<Position, Velocity>.CreateIterator(query.Iter()))
	{
		pos.Ref.X *= vel.Ref.X;
		pos.Ref.Y *= vel.Ref.Y;
	}
}

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

struct Position
{
	public float X, Y, Z;
}

struct Velocity
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

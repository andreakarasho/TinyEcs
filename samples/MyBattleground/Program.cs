// https://github.com/jasonliang-dev/entity-component-system
using System.Diagnostics;
using TinyEcs;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;

const int ENTITIES_COUNT = (524_288 * 2 * 1);

using var ecs = new World();
var scheduler = new Scheduler(ecs);


scheduler.AddState(GameState.Loading);


// scheduler.AddSystem(() => Console.WriteLine("on enter"), Stages.OnEnter, ThreadingMode.Single)
// 		 .RunIf((State<GameState> state) => state.Changed && state.Current == GameState.Loading);

// scheduler.AddSystem(() => Console.WriteLine("on exit"), Stages.OnExit, ThreadingMode.Single)
// 		 .RunIf((State<GameState> state) => state.Changed && state.Previous == GameState.Loading);

scheduler.OnEnter(GameState.Loading, () => Console.WriteLine("on enter loading"), ThreadingMode.Single);
scheduler.OnExit(GameState.Loading, () => Console.WriteLine("on exit loading"), ThreadingMode.Single);

scheduler.OnEnter(GameState.Playing, () => Console.WriteLine("on enter playing"), ThreadingMode.Single);
scheduler.OnExit(GameState.Playing, () => Console.WriteLine("on exit playing"), ThreadingMode.Single);

scheduler.AddSystem((State<GameState> state, Local<float> loading, Local<GameState[]> states, Local<int> index) =>
{
	states.Value ??= Enum.GetValues<GameState>();

	loading.Value += 0.1f;
	Console.WriteLine("next {0:P}", loading.Value);

	if (loading.Value >= 1f)
	{
		loading.Value = 0f;
		Console.WriteLine("on swapping state");
		state.Set(states.Value[(++index.Value) % states.Value.Length]);
	}

}, threadingType: ThreadingMode.Single);


while (true)
	scheduler.RunOnce();

for (int i = 0; i < ENTITIES_COUNT; i++)
	ecs.Entity()
		.Set<Position>(new Position())
		.Set<Velocity>(new Velocity());

ecs.Entity().Set(new Position()).Set(new Velocity()).Set(new Mass());



scheduler.AddSystem((Query<Data<Position, Velocity>> q) =>
{
	foreach ((var ent, var pos, var vel) in q)
	{
		pos.Ref.X *= vel.Ref.X;
		pos.Ref.Y *= vel.Ref.Y;
	}
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
	foreach ((var ent, var pos, var vel) in Data<Position, Velocity>.CreateIterator(query.Iter()))
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
	Playing
}

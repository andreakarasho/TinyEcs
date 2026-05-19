using System.Diagnostics;
using System.Runtime.CompilerServices;
using TinyEcs;
using TinyEcs.Bevy;

public static class PerformanceTinyEcsExample
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void RunQuery(Query<Data<Position, Velocity>> query)
	{
		foreach (var (pos, vel) in query)
		{
			pos.Ref.X *= vel.Ref.X;
			pos.Ref.Y *= vel.Ref.Y;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void RunQueryChunked(Query<Data<Position, Velocity>> query)
	{
		var iter = query.Inner.Iter();
		while (iter.Next())
		{
			var posIdx = iter.GetColumnIndexOf<Position>();
			var velIdx = iter.GetColumnIndexOf<Velocity>();
			var posSpan = iter.Data<Position>(posIdx);
			var velSpan = iter.Data<Velocity>(velIdx);
			for (int i = 0; i < posSpan.Length; i++)
			{
				posSpan[i].X *= velSpan[i].X;
				posSpan[i].Y *= velSpan[i].Y;
			}
		}
	}

	public static void Run()
	{
		RunBench("chunked Span<T>", RunQueryChunked);
		RunBench("foreach (per-row)", RunQuery);
		RunBenchRaw("raw loop (no app.Update)");
	}

	static void RunBenchRaw(string label)
	{
		using var world = new World();
		var app = new App(world, ThreadingMode.Single);

		app.AddSystem(Stage.Startup, (Commands cmds) =>
		{
			const int ENTITIES_COUNT = (524_288 * 2 * 1);
			for (int i = 0; i < ENTITIES_COUNT; i++)
			{
				cmds.Spawn()
					.Insert(new Position { X = 0, Y = 0 })
					.Insert(new Velocity { X = 0, Y = 0 });
			}
		});
		app.RunStartup();

		var query = world.QueryBuilder().With<Position>().With<Velocity>().Build();

		const int RUNS = 3;
		const int FRAMES_PER_RUN = 3600;
		var samples = new long[RUNS];

		for (int i = 0; i < FRAMES_PER_RUN; ++i)
			DoChunked(query);

		for (int r = 0; r < RUNS; r++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			var sw = Stopwatch.StartNew();
			for (int i = 0; i < FRAMES_PER_RUN; ++i)
				DoChunked(query);
			sw.Stop();
			samples[r] = sw.ElapsedMilliseconds;
		}

		Array.Sort(samples);
		var median = samples[RUNS / 2];
		Console.WriteLine($"[{label}] runs: [{string.Join(", ", samples)}] ms, median: {median} ms ({(double)median / FRAMES_PER_RUN:F4} ms/frame)");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void DoChunked(TinyEcs.Query query)
	{
		var iter = query.Iter();
		while (iter.Next())
		{
			var posIdx = iter.GetColumnIndexOf<Position>();
			var velIdx = iter.GetColumnIndexOf<Velocity>();
			var posSpan = iter.Data<Position>(posIdx);
			var velSpan = iter.Data<Velocity>(velIdx);
			for (int i = 0; i < posSpan.Length; i++)
			{
				posSpan[i].X *= velSpan[i].X;
				posSpan[i].Y *= velSpan[i].Y;
			}
		}
	}

	static void RunBench(string label, Action<Query<Data<Position, Velocity>>> system)
	{
		using var world = new World();
		var app = new App(world, ThreadingMode.Single);

		app
			.AddSystem(Stage.Startup, (Commands cmds) =>
			{
				const int ENTITIES_COUNT = (524_288 * 2 * 1);

				for (int i = 0; i < ENTITIES_COUNT; i++)
				{
					cmds.Spawn()
						.Insert(new Position { X = 0, Y = 0 })
						.Insert(new Velocity { X = 0, Y = 0 });
				}
			})
			.AddSystem(Stage.Update, system);

		const int RUNS = 3;
		const int FRAMES_PER_RUN = 3600;
		var samples = new long[RUNS];

		for (int i = 0; i < FRAMES_PER_RUN; ++i)
			app.Update();

		for (int r = 0; r < RUNS; r++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			var sw = Stopwatch.StartNew();
			for (int i = 0; i < FRAMES_PER_RUN; ++i)
				app.Update();
			sw.Stop();
			samples[r] = sw.ElapsedMilliseconds;
		}

		Array.Sort(samples);
		var median = samples[RUNS / 2];
		Console.WriteLine($"[{label}] runs: [{string.Join(", ", samples)}] ms, median: {median} ms ({(double)median / FRAMES_PER_RUN:F4} ms/frame)");
	}

	struct Position
	{
		public float X;
		public float Y;
	}
	struct Velocity
	{
		public float X;
		public float Y;
	}
}

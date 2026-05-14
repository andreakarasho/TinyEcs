using System.Diagnostics;
using TinyEcs;
using TinyEcs.Bevy;

public static class PerformanceTinyEcsExample
{
	public static void Run()
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
			.AddSystem(Stage.Update, (Query<Data<Position, Velocity>> query) =>
			{
				foreach (var (pos, vel) in query)
				{
					pos.Ref.X *= vel.Ref.X;
					pos.Ref.Y *= vel.Ref.Y;
				}
			});

		const int RUNS = 3;
		const int FRAMES_PER_RUN = 3600;
		var samples = new long[RUNS];

		// Warmup pass to let JIT tier up + PGO collect data
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
		Console.WriteLine($"runs: [{string.Join(", ", samples)}] ms");
		Console.WriteLine($"median: {median} ms over {FRAMES_PER_RUN} frames ({(double)median / FRAMES_PER_RUN:F4} ms/frame)");
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

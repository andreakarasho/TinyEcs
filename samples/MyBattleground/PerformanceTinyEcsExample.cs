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


		var sw = Stopwatch.StartNew();
		var start = 0f;
		var last = 0f;

		while (true)
		{
			for (int i = 0; i < 3600; ++i)
			{
				app.Update();
			}

			last = start;
			start = sw.ElapsedMilliseconds;

			Console.WriteLine("query done in {0} ms", start - last);
		}
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

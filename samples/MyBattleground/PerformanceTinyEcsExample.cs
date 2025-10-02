using System.Diagnostics;
using MyBattleground.Bevy;
using TinyEcs;

namespace MyBattleground;


public sealed class PerfSystem : Bevy.SystemBase<Time>
{
	protected override void Execute(TinyEcs.World world, Time _)
	{
		foreach (var (pos, vel) in world.Query<Data<Position, Velocity>>())
		{
			pos.Ref.X *= vel.Ref.X;
			pos.Ref.Y *= vel.Ref.Y;
		}
	}
}


public static class PerformanceTinyEcsExample
{
	public static void Run()
	{
		using var world = new TinyEcs.World();
		var app = new Bevy.App(world);

		app
			.AddResource(new Time { DeltaTime = 1f / 60f, TotalTime = 0f })
			.AddSystem(w =>
			{
				for (var i = 0; i < 1_000_000; i++)
				{
					w.Entity()
						.Set(new Position { X = 0, Y = 0 })
						.Set(new Velocity { X = 0, Y = 0 });
				}
			})
				.InStage(Bevy.Stage.Startup)
				.Build()

			.AddSystem(new PerfSystem())
				.InStage(Bevy.Stage.Update)
				.Build();


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
}

using System;
using TinyEcs.Bevy;
using TinyEcs;

namespace MyBattleground;

/// <summary>
/// Example demonstrating the Commands system parameter for deferred world operations
/// </summary>
public static class CommandsExample
{
	// Components
	public struct Player { }
	public struct Enemy { }
	public struct Health { public int Value; }

	public static void Run()
	{
		Console.WriteLine("=== Commands Example ===\n");

		var world = new TinyEcs.World();
		var app = new App(world);

		// System 1: Spawn entities using Commands
		app.AddSystem((Commands commands) =>
		{
			Console.WriteLine("System 1: Spawning entities with Commands...");

			// Spawn player with fluent API
			var playerId = commands.Spawn()
				.Insert<Player>()
				.Insert(new Health { Value = 100 })
				.Id;

			Console.WriteLine($"  Spawned player entity {playerId}");

			// Spawn enemies
			for (int i = 0; i < 3; i++)
			{
				var enemyId = commands.Spawn()
					.Insert<Enemy>()
					.Insert(new Health { Value = 50 + i * 10 })
					.Id;

				Console.WriteLine($"  Spawned enemy entity {enemyId} with {50 + i * 10} HP");
			}

			Console.WriteLine("  (Commands queued - will be applied at end of system)");

		}).InStage(TinyEcs.Bevy.Stage.Startup);

		// System 2: Modify entities using Commands
		app.AddSystem((Commands commands) =>
		{
			Console.WriteLine("\nSystem 2: Modifying entities with Commands...");

			// In a real game, you'd query for entities here
			// For this example, we'll just operate on known IDs

			// Damage player (entity 257)
			commands.Entity(257)
				.Insert(new Health { Value = 80 });
			Console.WriteLine("  Updated player health to 80");

			// Despawn enemy (entity 258)
			commands.Entity(258).Despawn();
			Console.WriteLine("  Despawned first enemy");

			Console.WriteLine("  (Commands queued - will be applied at end of system)");

		}).InStage(TinyEcs.Bevy.Stage.Update);

		// System 3: Insert a resource
		app.AddSystem((Commands commands) =>
		{
			Console.WriteLine("\nSystem 3: Adding resource with Commands...");
			commands.InsertResource(new GameStats { TotalKills = 1 });
			Console.WriteLine("  Inserted GameStats resource");

		}).InStage(TinyEcs.Bevy.Stage.Update);

		// System 4: Read the resource
		app.AddSystem((Res<GameStats> stats) =>
		{
			Console.WriteLine($"\nSystem 4: Reading resource - Total kills: {stats.Value.TotalKills}");

		}).InStage(TinyEcs.Bevy.Stage.PostUpdate);

		Console.WriteLine("\n--- Running Startup Stage ---");
		app.Update();

		Console.WriteLine("\n--- Running Update/PostUpdate Stages ---");
		app.Update();

		Console.WriteLine("\n=== Example Complete ===");
		Console.WriteLine("\nKey Points:");
		Console.WriteLine("- Commands queue operations during system execution");
		Console.WriteLine("- Operations are applied at the end of each system (EndDeferred)");
		Console.WriteLine("- This prevents structural changes during iteration");
		Console.WriteLine("- Observers are flushed after each stage completes");
	}

	class GameStats
	{
		public int TotalKills { get; set; }
	}
}
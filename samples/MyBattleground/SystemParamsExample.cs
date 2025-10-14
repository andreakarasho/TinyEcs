using System;
using TinyEcs.Bevy;
using TinyEcs;

namespace MyBattleground;

// ============================================================================
// System Parameters Example
// ============================================================================

// Components
public struct Health
{
	public int Current;
	public int Max;
}

public struct Damage
{
	public int Amount;
}

public struct EnemyTag { }

// Resources
public class GameConfig
{
	public int StartingHealth { get; set; } = 100;
	public float DamageMultiplier { get; set; } = 1.0f;
}

public class GameStats
{
	public int EnemiesKilled { get; set; }
	public int TotalDamageDealt { get; set; }
}

// Events
public record DamageDealtEvent(int Amount, string Target);
public record EnemyKilledEvent(string EnemyName);

// Example Plugin using System Parameters
public class CombatPlugin : IPlugin
{
	public void Build(App app)
	{
		Console.WriteLine("📦 Installing CombatPlugin with System Parameters");

		// System with Res<T> - read-only resource access
		app.AddSystem((Res<GameConfig> config) =>
		{
			Console.WriteLine($"⚙️  [Config] Starting Health: {config.Value.StartingHealth}, Damage Multiplier: {config.Value.DamageMultiplier}x");
		})
		.InStage(TinyEcs.Bevy.Stage.Startup)
		.Build();

		// System with ResMut<T> - mutable resource access
		app.AddSystem((ResMut<GameStats> stats) =>
		{
			stats.Value.TotalDamageDealt += 15;
			Console.WriteLine($"📊 [Stats] Total Damage: {stats.Value.TotalDamageDealt}, Enemies Killed: {stats.Value.EnemiesKilled}");
		})
		.InStage(TinyEcs.Bevy.Stage.Update)
		.Label("update_stats")
		.Build();

		// System with Local<T> - per-system local state
		app.AddSystem((Local<FrameCounter> counter) =>
		{
			counter.Value.Count++;
			if (counter.Value.Count % 2 == 0)
			{
				Console.WriteLine($"⏱️  [Timer] Frame #{counter.Value.Count} (even frame)");
			}
		})
		.InStage(TinyEcs.Bevy.Stage.Update)
		.Build();

		// System with EventWriter<T> - send events
		app.AddSystem((EventWriter<DamageDealtEvent> damageEvents, Res<GameConfig> config) =>
		{
			// Simulate dealing damage
			var damage = (int)(10 * config.Value.DamageMultiplier);
			damageEvents.Send(new DamageDealtEvent(damage, "Goblin"));
			Console.WriteLine($"⚔️  [Combat] Dealt {damage} damage to Goblin");
		})
		.InStage(TinyEcs.Bevy.Stage.Update)
		.After("update_stats")
		.Build();

		// System with EventReader<T> - read events
		app.AddSystem((EventReader<DamageDealtEvent> damageReader, ResMut<GameStats> stats) =>
		{
			foreach (var evt in damageReader.Read())
			{
				stats.Value.TotalDamageDealt += evt.Amount;
				Console.WriteLine($"📬 [EventHandler] Processed damage event: {evt.Amount} to {evt.Target}");
			}
		})
		.InStage(TinyEcs.Bevy.Stage.Last)
		.Build();

		// System with EventReader for enemy killed events
		app.AddSystem((EventReader<EnemyKilledEvent> killedReader, ResMut<GameStats> stats) =>
		{
			if (killedReader.HasEvents)
			{
				foreach (var evt in killedReader.Read())
				{
					stats.Value.EnemiesKilled++;
					Console.WriteLine($"💀 [EventHandler] Enemy killed: {evt.EnemyName} (Total: {stats.Value.EnemiesKilled})");
				}
			}
		})
		.InStage(TinyEcs.Bevy.Stage.Last)
		.Build();

		// Complex system with multiple parameters
		app.AddSystem((
			Query<Data<Health, Damage>, With<EnemyTag>> enemyQuery,
			Res<GameConfig> config,
			EventWriter<EnemyKilledEvent> killedEvents,
			Local<EnemySpawnCounter> spawnCounter
		) =>
		{
			// Count enemies
			int enemyCount = 0;
			foreach (var (health, damage) in enemyQuery)
			{
				enemyCount++;
				Console.WriteLine($"🧟 [Enemy #{enemyCount}] Health: {health.Ref.Current}/{health.Ref.Max}, Damage: {damage.Ref.Amount}");

				// Simulate enemy death
				if (health.Ref.Current <= 0)
				{
					killedEvents.Send(new EnemyKilledEvent($"Enemy #{enemyCount}"));
				}
			}

			// Spawn new enemy every 2 frames
			spawnCounter.Value.FramesSinceLastSpawn++;
			if (spawnCounter.Value.FramesSinceLastSpawn >= 2 && spawnCounter.Value.TotalSpawned < 3)
			{
				Console.WriteLine($"🌟 [Spawner] Spawning new enemy...");
				spawnCounter.Value.FramesSinceLastSpawn = 0;
				spawnCounter.Value.TotalSpawned++;
			}
		})
		.InStage(TinyEcs.Bevy.Stage.Update)
		.RunIf(HasEnemies)
		.Build();
	}

	private static bool HasEnemies(World world)
	{
		foreach (var _ in world.Query<Data<Health>, With<EnemyTag>>())
		{
			return true;
		}
		return false;
	}
}

// Local state types
public class FrameCounter
{
	public int Count { get; set; }
}

public class EnemySpawnCounter
{
	public int FramesSinceLastSpawn { get; set; }
	public int TotalSpawned { get; set; }
}

public static class SystemParamsExample
{
	public static void Run()
	{
		Console.WriteLine("=== System Parameters Example ===\n");
		Console.WriteLine("Demonstrating Bevy-style system parameters:\n");
		Console.WriteLine("- Res<T> / ResMut<T> - Resource access");
		Console.WriteLine("- Local<T> - Per-system local state");
		Console.WriteLine("- EventReader<T> / EventWriter<T> - Event handling");
		Console.WriteLine("- QueryParam<T> - Type-safe ECS queries\n");

		// Create world and app
		using var world = new TinyEcs.World();
		var app = new App(world);

		// Add resources
		app.AddResource(new GameConfig { StartingHealth = 100, DamageMultiplier = 1.5f });
		app.AddResource(new GameStats());

		// Spawn some enemies for testing
		for (int i = 0; i < 2; i++)
		{
			world.Entity()
				.Set(new Health { Current = 50 - (i * 25), Max = 100 })
				.Set(new Damage { Amount = 10 + (i * 5) })
				.Set(new EnemyTag());
		}

		// Install combat plugin
		app.AddPlugin<CombatPlugin>();

		// Run frames
		Console.WriteLine("\n===== Frame 1 =====");
		app.Update();

		Console.WriteLine("\n===== Frame 2 =====");
		app.Update();

		Console.WriteLine("\n===== Frame 3 =====");
		app.Update();

		Console.WriteLine("\n===== Frame 4 =====");
		app.Update();

		// Final stats
		var finalStats = world.GetResource<GameStats>();
		Console.WriteLine($"\n📊 Final Stats:");
		Console.WriteLine($"   Enemies Killed: {finalStats.EnemiesKilled}");
		Console.WriteLine($"   Total Damage Dealt: {finalStats.TotalDamageDealt}");

		Console.WriteLine("\n✅ System Parameters example completed!");
	}
}
using System;
using TinyEcs.Bevy;
using TinyEcs;

namespace MyBattleground;

public static class ObserverExample
{
	// Components
	public struct Health
	{
		public int Value;
		public override string ToString() => $"Health({Value})";
	}
	public struct Position
	{
		public float X;
		public float Y;
		public override string ToString() => $"Position({X}, {Y})";
	}
	public struct Enemy { }
	public struct Player { }

	// Resources to store entity references
	public class PlayerEntity { public ulong Id; }
	public class EnemyEntity { public ulong Id; }

	public static void Run()
	{
		Console.WriteLine("=== Bevy Observer Example ===\n");

		var world = new World();
		var app = new App(world);

		// Observers automatically enable component tracking when registered!

		// Observer: React when Position is updated
		app.AddObserver<OnInsert<Position>>(trigger =>
		{
			Console.WriteLine($"📍 Entity {trigger.EntityId} moved to ({trigger.Component.X}, {trigger.Component.Y})");
		});

		// Observer: React when Health is removed
		app.AddObserver<OnRemove<Health>>(trigger =>
		{
			Console.WriteLine($"💀 Entity {trigger.EntityId} lost Health component");
		});

		// Observer: React when entity spawns
		app.AddObserver<OnSpawn>(trigger =>
		{
			Console.WriteLine($"✨ Entity {trigger.EntityId} spawned");
		});

		// Observer: React when entity despawns
		app.AddObserver<OnDespawn>(trigger =>
		{
			Console.WriteLine($"💥 Entity {trigger.EntityId} despawned");
		});

		// Observer with system parameters - check if it's an enemy when health is added
		app.AddObserver<OnInsert<Health>, TinyEcs.Bevy.Query<Data<Enemy>>>((trigger, query) =>
		{
			if (query.Contains(trigger.EntityId))
			{
				Console.WriteLine($"⚔️  Enemy {trigger.EntityId} has {trigger.Component.Value} HP");
			}
		});

		// Add a startup system to create entities. Use Commands so resources
		// land on the owning App instead of the World.
		app.AddSystem((Commands commands) =>
		{
			Console.WriteLine("--- Creating entities ---\n");

			// Spawn player - OnSpawn automatically triggered
			var player = commands.Spawn()
				.Insert<Player>()
				.Insert(new Health { Value = 100 })
				.Insert(new Position { X = 0, Y = 0 });

			Console.WriteLine();

			// Spawn enemy - OnSpawn automatically triggered
			var enemy = commands.Spawn()
				.Insert<Enemy>()
				.Insert(new Health { Value = 50 })
				.Insert(new Position { X = 10, Y = 5 });

			// Store entities as resources for later systems.
			commands.InsertResource(new PlayerEntity { Id = player.Id });
			commands.InsertResource(new EnemyEntity { Id = enemy.Id });
		}).InStage(TinyEcs.Bevy.Stage.Startup);

		// Add an update system to move player
		app.AddSystem((Commands commands, Res<PlayerEntity> playerRes) =>
		{
			var player = playerRes.Value.Id;

			Console.WriteLine("\n--- Moving player ---\n");
			commands.Entity(player).Insert(new Position { X = 5, Y = 3 });
		}).InStage(TinyEcs.Bevy.Stage.Update);

		// Add another system to damage enemy
		app.AddSystem((Commands commands, Res<EnemyEntity> enemyRes) =>
		{
			var enemy = enemyRes.Value.Id;

			Console.WriteLine("\n--- Enemy takes damage ---\n");
			commands.Entity(enemy).Remove<Health>();
		}).InStage(TinyEcs.Bevy.Stage.PostUpdate);

		// Add a final system to despawn enemy
		app.AddSystem((Commands commands, Res<EnemyEntity> enemyRes) =>
		{
			var enemy = enemyRes.Value.Id;

			Console.WriteLine("\n--- Despawning enemy ---\n");
			commands.Entity(enemy).Despawn();
		}).InStage(TinyEcs.Bevy.Stage.Last);

		// Run the app - observers will auto-flush at end of each stage
		app.Update();

		Console.WriteLine("\n=== Example Complete ===");
	}
}

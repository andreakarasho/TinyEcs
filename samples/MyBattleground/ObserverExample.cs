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
	public class PlayerEntity { public TinyEcs.EntityView Id; }
	public class EnemyEntity { public TinyEcs.EntityView Id; }

	public static void Run()
	{
		Console.WriteLine("=== Bevy Observer Example ===\n");

		var world = new World();
		var app = new App(world);

		// Observers automatically enable component tracking when registered!

		// Observer: React when Position is updated
		app.AddObserver<OnInsert<Position>>((world, trigger) =>
		{
			Console.WriteLine($"📍 Entity {trigger.EntityId} moved to ({trigger.Component.X}, {trigger.Component.Y})");
		});

		// Observer: React when Health is removed
		app.AddObserver<OnRemove<Health>>((world, trigger) =>
		{
			Console.WriteLine($"💀 Entity {trigger.EntityId} lost Health component");
		});

		// Observer: React when entity spawns
		app.AddObserver<OnSpawn>((world, trigger) =>
		{
			Console.WriteLine($"✨ Entity {trigger.EntityId} spawned");
		});

		// Observer: React when entity despawns
		app.AddObserver<OnDespawn>((world, trigger) =>
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

		// Add a startup system to create entities
		app.AddSystem((TinyEcs.World world) =>
		{
			Console.WriteLine("--- Creating entities ---\n");

			// Spawn player - OnSpawn automatically triggered
			var player = world.Entity();

			// Add components to player - OnInsert automatically triggered
			player.Set<Player>();
			world.Set(player, new Health { Value = 100 });
			world.Set(player, new Position { X = 0, Y = 0 });

			Console.WriteLine();

			// Spawn enemy - OnSpawn automatically triggered
			var enemy = world.Entity();

			// Add components to enemy - OnInsert automatically triggered
			world.Set<Enemy>(enemy);
			world.Set(enemy, new Health { Value = 50 });
			world.Set(enemy, new Position { X = 10, Y = 5 });

			// Store entities as resources for later systems
			world.AddResource(new PlayerEntity { Id = player });
			world.AddResource(new EnemyEntity { Id = enemy });
		}).InStage(TinyEcs.Bevy.Stage.Startup);

		// Add an update system to move player
		app.AddSystem((TinyEcs.World world) =>
		{
			var player = world.GetResource<PlayerEntity>().Id;

			Console.WriteLine("\n--- Moving player ---\n");
			world.Set(player, new Position { X = 5, Y = 3 });
		}).InStage(TinyEcs.Bevy.Stage.Update);

		// Add another system to damage enemy
		app.AddSystem((TinyEcs.World world) =>
		{
			var enemy = world.GetResource<EnemyEntity>().Id;

			Console.WriteLine("\n--- Enemy takes damage ---\n");
			world.Unset<Health>(enemy);
		}).InStage(TinyEcs.Bevy.Stage.PostUpdate);

		// Add a final system to despawn enemy
		app.AddSystem((TinyEcs.World world) =>
		{
			var enemy = world.GetResource<EnemyEntity>().Id;

			Console.WriteLine("\n--- Despawning enemy ---\n");
			world.Delete(enemy);
		}).InStage(TinyEcs.Bevy.Stage.Last);

		// Run the app - observers will auto-flush at end of each stage
		app.Update();

		Console.WriteLine("\n=== Example Complete ===");
	}
}

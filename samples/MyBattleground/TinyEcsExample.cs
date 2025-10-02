using System;
using TinyEcs;
using MyBattleground.Bevy;

// ============================================================================
// TinyEcs + Bevy App Integration Example
// ============================================================================
// This example demonstrates the new App class that wraps TinyEcs.World
// combining Bevy-style API with real ECS functionality

namespace MyBattleground;

// Components (for TinyEcs entities)
public struct Position
{
	public float X, Y, Z;
}

public struct Velocity
{
	public float X, Y;
}

public struct PlayerTag { }

// Resources (global singletons)
public class Time
{
	public float DeltaTime { get; set; }
	public float TotalTime { get; set; }
}

public class PlayerData
{
	public int Score { get; set; }
	public bool IsPaused { get; set; }
}

// States
public enum AppState { MainMenu, Loading, Playing, Paused, GameOver }

// Plugin example
public class GamePlugin : Bevy.IPlugin
{
	public void Build(Bevy.App app)
	{
		Console.WriteLine("📦 Installing GamePlugin");

		// Startup systems (run once)
		app.AddSystem(world =>
		{
			Console.WriteLine("🚀 [Startup] Initializing game...");

			// Create some entities with TinyEcs
			for (int i = 0; i < 3; i++)
			{
				world.Entity()
					.Set(new Position { X = i, Y = i, Z = 0 })
					.Set(new Velocity { X = 1, Y = 1 });
			}

			// Create player entity
			world.Entity()
				.Set(new PlayerTag())
				.Set(new Position { X = 0, Y = 0, Z = 0 })
				.Set(new Velocity { X = 2, Y = 2 });
		})
		.InStage(Bevy.Stage.Startup)
		.Build();

		app.AddSystem(world =>
		{
			Console.WriteLine("📦 [Startup] Loading initial assets...");
		})
		.InStage(Bevy.Stage.Startup)
		.Build();

		// Frame systems
		app.AddSystem(world =>
		{
			Console.WriteLine("▶️  [First] Frame Start");
		})
		.InStage(Bevy.Stage.First)
		.Build();

		// Time system
		app.AddSystem(world =>
		{
			var time = world.GetResource<Time>();
			time.TotalTime += time.DeltaTime;
			Console.WriteLine($"⏰ [Update] Time: {time.TotalTime:F2}s");
		})
		.InStage(Bevy.Stage.Update)
		.Label("time")
		.Build();

		// Move entities using TinyEcs Query<TData>
		app.AddSystem(world =>
		{
			var time = world.GetResource<Time>();

			// Use type-safe Query<Data<Position, Velocity>>
			var query = world.Query<Data<Position, Velocity>>();

			// Iterate using foreach with deconstruction
			foreach (var (pos, vel) in query)
			{
				pos.Ref.X += vel.Ref.X * time.DeltaTime;
				pos.Ref.Y += vel.Ref.Y * time.DeltaTime;
			}
		})
		.InStage(Bevy.Stage.Update)
		.After("time")
		.Build();

		// Player-specific system using Query<TData, TFilter>
		app.AddSystem(world =>
		{
			var playerData = world.GetResource<PlayerData>();

			// Use Query with With<PlayerTag> filter
			var query = world.Query<Data<Position, Velocity>, With<PlayerTag>>();

			// Iterate only entities that have PlayerTag
			foreach (var (pos, vel) in query)
			{
				playerData.Score += 10;
				Console.WriteLine($"🎮 [Update] Player - Score: {playerData.Score}, Pos: ({pos.Ref.X:F1}, {pos.Ref.Y:F1})");
			}
		})
		.InStage(Bevy.Stage.Update)
		.After("time")
		.RunIfState(AppState.Playing)
		.Build();

		app.AddSystem(world =>
		{
			Console.WriteLine("⏹️  [Last] Frame End\n");
		})
		.InStage(Bevy.Stage.Last)
		.Build();
	}
}

// State transition plugin
public class StatePlugin : Bevy.IPlugin
{
	public void Build(Bevy.App app)
	{
		Console.WriteLine("📦 Installing StatePlugin");

		// State transition systems
		app.AddSystem(world =>
		{
			Console.WriteLine($"🎮 Entered Playing State!");
		})
		.OnEnter(AppState.Playing)
		.Build();

		app.AddSystem(world =>
		{
			Console.WriteLine($"⏸️  Exited Playing State!");
		})
		.OnExit(AppState.Playing)
		.Build();

		app.AddSystem(world =>
		{
			var playerData = world.GetResource<PlayerData>();
			Console.WriteLine($"💀 Game Over! Final score: {playerData.Score}");
		})
		.OnEnter(AppState.GameOver)
		.Build();

		// Game logic system that changes state based on score
		app.AddSystem(world =>
		{
			var playerData = world.GetResource<PlayerData>();
			var state = world.GetState<AppState>();

			// Transition to GameOver when score reaches 50
			if (playerData.Score >= 50 && state == AppState.Playing)
			{
				Console.WriteLine("💀 Player reached 50 points - triggering Game Over!");
				world.SetState(AppState.GameOver);
			}
		})
		.InStage(Bevy.Stage.Update)
		.RunIfState(AppState.Playing)
		.Build();

		// Menu system
		app.AddSystem(world =>
		{
			Console.WriteLine("📋 [Update] Main Menu - Auto-starting game!");
			world.SetState(AppState.Loading);
		})
		.InStage(Bevy.Stage.Update)
		.RunIfState(AppState.MainMenu)
		.Build();

		// Loading system
		app.AddSystem(world =>
		{
			Console.WriteLine("⏳ [Update] Loading assets...");
			Console.WriteLine("✅ Loading complete!");
			world.SetState(AppState.Playing);
		})
		.InStage(Bevy.Stage.Update)
		.RunIfState(AppState.Loading)
		.Build();
	}
}

public static class TinyEcsExample
{
	public static void Run()
	{
		Console.WriteLine("=== TinyEcs + Bevy App Integration Example ===\n");
		Console.WriteLine("Combining Bevy-style API with real ECS from TinyEcs\n");
		Console.WriteLine("Using Query<TData> and Query<TData, TFilter> for type-safe iteration\n");

		// Create TinyEcs World
		using var world = new TinyEcs.World();

		// Create App that wraps the world
		var app = new Bevy.App(world);

		// Add state management
		app.AddState(AppState.MainMenu);

		// Add resources
		app.AddResource(new Time { DeltaTime = 0.016f });
		app.AddResource(new PlayerData());

		// Install plugins
		app.AddPlugin<GamePlugin>();
		app.AddPlugin<StatePlugin>();

		// Run frames
		Console.WriteLine("===== Frame 1: Startup + MainMenu (auto-transitions to Loading) =====");
		app.Update();

		Console.WriteLine("\n===== Frame 2: Loading (auto-transitions to Playing) =====");
		app.Update();

		Console.WriteLine("\n===== Frame 3: Playing =====");
		app.Update();

		Console.WriteLine("\n===== Frame 4: Playing =====");
		app.Update();

		Console.WriteLine("\n===== Frame 5: Playing (score reaches 50, transitions to GameOver) =====");
		app.Update();

		Console.WriteLine("\n===== Frame 6: GameOver =====");
		app.Update();

		Console.WriteLine("\n✅ Example completed!");
	}
}

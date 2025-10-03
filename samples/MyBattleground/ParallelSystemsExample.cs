using System;
using System.Diagnostics;
using System.Threading;
using MyBattleground.Bevy;

namespace MyBattleground;

/// <summary>
/// Example demonstrating parallel system execution based on resource access patterns
/// </summary>
public static class ParallelSystemsExample
{
	// Resources
	public class ResourceA { public int Value; }
	public class ResourceB { public int Value; }
	public class ResourceC { public int Value; }
	public class SharedCounter { public int Count; }

	public static void Run()
	{
		Console.WriteLine("=== Parallel Systems Example ===\n");

		var world = new TinyEcs.World();
		var app = new App(world);

		// Add resources
		app.AddResource(new ResourceA { Value = 0 });
		app.AddResource(new ResourceB { Value = 0 });
		app.AddResource(new ResourceC { Value = 0 });
		app.AddResource(new SharedCounter { Count = 0 });

		// Systems that CAN run in parallel (no conflicts)
		// These all read different resources
		app.AddSystem((Res<ResourceA> a) =>
		{
			Console.WriteLine($"[Thread {Environment.CurrentManagedThreadId}] System1: Reading ResourceA = {a.Value.Value}");
			Thread.Sleep(10); // Simulate work
		})
		.InStage(Stage.Update)
		.Build();

		app.AddSystem((Res<ResourceB> b) =>
		{
			Console.WriteLine($"[Thread {Environment.CurrentManagedThreadId}] System2: Reading ResourceB = {b.Value.Value}");
			Thread.Sleep(10); // Simulate work
		})
		.InStage(Stage.Update)
		.Build();

		app.AddSystem((Res<ResourceC> c) =>
		{
			Console.WriteLine($"[Thread {Environment.CurrentManagedThreadId}] System3: Reading ResourceC = {c.Value.Value}");
			Thread.Sleep(10); // Simulate work
		})
		.InStage(Stage.Update)
		.Build();

		// Systems that CANNOT run in parallel with each other (write conflict)
		app.AddSystem((ResMut<SharedCounter> counter) =>
		{
			Console.WriteLine($"[Thread {Environment.CurrentManagedThreadId}] System4: Writing SharedCounter");
			counter.Value.Count++;
			Thread.Sleep(10);
		})
		.InStage(Stage.PostUpdate)
		.Build();

		app.AddSystem((ResMut<SharedCounter> counter) =>
		{
			Console.WriteLine($"[Thread {Environment.CurrentManagedThreadId}] System5: Writing SharedCounter");
			counter.Value.Count++;
			Thread.Sleep(10);
		})
		.InStage(Stage.PostUpdate)
		.Build();

		// System that reads SharedCounter (can't run parallel with writers)
		app.AddSystem((Res<SharedCounter> counter) =>
		{
			Console.WriteLine($"[Thread {Environment.CurrentManagedThreadId}] System6: Reading SharedCounter = {counter.Value.Count}");
		})
		.InStage(Stage.Last)
		.Build();

		// System with RunIf condition using system parameters
		app.AddSystem((Res<SharedCounter> counter) =>
		{
			Console.WriteLine($"[Thread {Environment.CurrentManagedThreadId}] System7: Conditional system running! Counter = {counter.Value.Count}");
		})
		.InStage(Stage.Last)
		.RunIf((Res<SharedCounter> counter) => counter.Value.Count >= 2)
		.Build();

		// System that should NOT run (condition false)
		app.AddSystem((Res<SharedCounter> counter) =>
		{
			Console.WriteLine($"[Thread {Environment.CurrentManagedThreadId}] System8: This should NOT run!");
		})
		.InStage(Stage.Last)
		.RunIf((Res<SharedCounter> counter) => counter.Value.Count > 100)
		.Build();

		Console.WriteLine("Systems registered. Starting execution...\n");

		var sw = Stopwatch.StartNew();
		app.Run();
		sw.Stop();

		Console.WriteLine($"\nExecution completed in {sw.ElapsedMilliseconds}ms");
		Console.WriteLine("\nExpected behavior:");
		Console.WriteLine("- System1, System2, System3 should run in parallel (different thread IDs, ~10ms total)");
		Console.WriteLine("- System4 and System5 should run sequentially (same/different threads, ~20ms total)");
		Console.WriteLine("- System6 should run after System4 and System5");
		Console.WriteLine("- System7 should run (counter >= 2)");
		Console.WriteLine("- System8 should NOT run (counter > 100 is false)");
	}
}

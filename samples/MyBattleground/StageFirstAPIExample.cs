using System;
using MyBattleground.Bevy;

namespace MyBattleground.Examples;

public static class StageFirstAPIExample
{
	public static void Run()
	{
		Console.WriteLine("=== Stage-First API Example ===\n");

		var app = new App(new TinyEcs.World());

		// Old fluent API - must call .InStage() and then .Build()
		Console.WriteLine("Using fluent API (.InStage):");
		app.AddSystem((world) =>
		{
			Console.WriteLine("  System 1 (fluent API with .InStage)");
		}).InStage(Stage.Startup).Build();

		// New stage-first API - cleaner and more explicit
		Console.WriteLine("Using stage-first API (stage, system):");
		app.AddSystem(Stage.Startup, (world) =>
		{
			Console.WriteLine("  System 2 (stage-first API)");
		});

		// Works with system parameters too!
		app.AddSystem(Stage.Startup, (Res<ExampleConfig> config) =>
		{
			Console.WriteLine($"  System 3 with params (stage-first): speed = {config.Value.Speed}");
		});

		// For labels, ordering, or RunIf - use the fluent API
		Console.WriteLine("\nAdvanced: Labels, ordering, and conditions (use fluent API):");
		app.AddSystem((Commands commands) =>
		{
			Console.WriteLine("  System 4 (labeled and ordered)");
		})
		.InStage(Stage.Startup)
		.Label("my_labeled_system")
		.RunIf(world => true)
		.Build();

		// Add a resource
		app.AddResource(new ExampleConfig { Speed = 100 });

		Console.WriteLine("\n--- Running Startup Stage ---\n");
		app.RunStartup();

		Console.WriteLine("\n=== Complete ===\n");
		Console.WriteLine("Summary:");
		Console.WriteLine("  • Use stage-first API for simple systems:");
		Console.WriteLine("    app.AddSystem(Stage.X, system)");
		Console.WriteLine("  • Use fluent API for advanced features:");
		Console.WriteLine("    app.AddSystem(system).InStage(Stage.X).Label(...).After(...).Build()");
	}
}

public class ExampleConfig
{
	public int Speed { get; set; }
}

using Xunit;
using TinyEcs.Bevy;

namespace TinyEcs.Tests;

// Test events
public struct TestTriggerEvent { public int Value; }
public struct HelloEvent { public int Value; }

public class ObserverCommandsTests
{
	[Fact]
	public void ObserveWithCommandsExtensionMethodAutoAppliesCommands()
	{
		// This test verifies the fix for the user's issue:
		// commands.Spawn().Observe<On<Test>, Commands>((trigger, commands) => {
		//   commands.EmitTrigger(new Hello());
		// });
		//
		// Before the fix, the EmitTrigger was queued but never applied, so the
		// observer listening to Hello never fired.
		//
		// After the fix, Commands parameters are automatically applied after the
		// observer executes, so the trigger fires immediately.

		var app = new App();
		var testEventFired = false;
		var helloEventFired = false;
		ulong? spawnedEntityId = null;

		// Global observer listening for HelloEvent
		app.AddObserver<On<HelloEvent>>((w, trigger) =>
		{
			helloEventFired = true;
			Assert.Equal(99, trigger.Event.Value);
		});

		// System that spawns entity with observer using Commands parameter
		app.AddSystem((Commands commands) =>
		{
			var entityCmd = commands.Spawn()
				.Observe<On<TestTriggerEvent>, Commands>((trigger, cmd) =>
				{
					testEventFired = true;
					// Commands parameter should auto-apply after this callback
					// This is the key fix - EmitTrigger is queued and then auto-applied
					cmd.EmitTrigger(new HelloEvent { Value = 99 });
				});

			spawnedEntityId = entityCmd.Id;
		})
		.InStage(Stage.Startup)
		.Label("spawn")
		.Build();

		// System to emit the trigger after entity is created
		app.AddSystem((Commands commands) =>
		{
			if (spawnedEntityId.HasValue)
			{
				// Emit trigger on the spawned entity
				commands.Entity(spawnedEntityId.Value)
					.EmitTrigger(new TestTriggerEvent { Value = 1 });
			}
		})
		.InStage(Stage.Startup)
		.After("spawn")
		.Build();

		app.Run();

		Assert.True(testEventFired, "Entity observer should have fired");
		Assert.True(helloEventFired, "Global observer should have fired - Commands.EmitTrigger was auto-applied");
	}
}

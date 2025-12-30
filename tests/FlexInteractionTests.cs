using System.Numerics;
using TinyEcs.Bevy;
using TinyEcs.UI.Bevy;
using Flexbox;

namespace TinyEcs.Tests;

/// <summary>
/// Tests for the InteractionState system that tracks current interaction state (None, Hovered, Pressed).
/// Uses Bevy's simple approach - just tracks current state via Changed&lt;InteractionState&gt;.
/// </summary>
public class InteractionTests
{
	/// <summary>
	/// Helper method to set up a basic app with UI plugins.
	/// </summary>
	private static App CreateApp()
	{
		var app = new App(ThreadingMode.Single);
		app.AddPlugin(new TinyEcsUiPlugin());
		return app;
	}

	/// <summary>
	/// Helper to set delta time for interaction timing.
	/// </summary>
	private static void SetDeltaTime(App app, float seconds = 0.016f)
	{
		var world = app.GetWorld();
		var deltaTime = world.GetResource<DeltaTime>();
		deltaTime.Seconds = seconds;
	}

	[Fact]
	public void InteractionPlugin_AddsInteractionStateToInteractiveEntities()
	{
		var app = CreateApp();
		ulong entityId = 0;

		// Create an entity with Interactive component
		app.AddSystem((Commands commands) =>
		{
			entityId = commands.Spawn()
				.Insert(new UiNode())
				.Insert(new Interactive())
				.Id;
		})
		.InStage(Stage.Startup)
		.Build();

		app.RunStartup();
		var world = app.GetWorld();
		SetDeltaTime(app);

		// Run update to let InteractionPlugin add components
		app.Update();

		// Verify InteractionState was added
		Assert.True(world.Has<InteractionState>(entityId), "Should have InteractionState component");
	}

	[Fact]
	public void InteractionState_InitialStateIsNone()
	{
		var app = CreateApp();
		ulong entityId = 0;

		app.AddSystem((Commands commands) =>
		{
			entityId = commands.Spawn()
				.Insert(new UiNode())
				.Insert(new Interactive())
				.Id;
		})
		.InStage(Stage.Startup)
		.Build();

		app.RunStartup();
		var world = app.GetWorld();
		SetDeltaTime(app);

		// Run update to add InteractionState
		app.Update();

		var state = world.Get<InteractionState>(entityId);
		Assert.Equal(Interaction.None, state.State);
	}

	[Fact]
	public void Interaction_EnumHasExpectedValues()
	{
		// Verify the Interaction enum has all expected values
		Assert.Contains(Interaction.None, Enum.GetValues<Interaction>());
		Assert.Contains(Interaction.Hovered, Enum.GetValues<Interaction>());
		Assert.Contains(Interaction.Pressed, Enum.GetValues<Interaction>());

		// Verify count is exactly 3 (simplified model)
		Assert.Equal(3, Enum.GetValues<Interaction>().Length);
	}

	[Fact]
	public void InteractionState_CanBeSetDirectly()
	{
		var app = CreateApp();
		ulong entityId = 0;

		app.AddSystem((Commands commands) =>
		{
			entityId = commands.Spawn()
				.Insert(new UiNode())
				.Insert(new Interactive())
				.Insert(new InteractionState { State = Interaction.Hovered })
				.Id;
		})
		.InStage(Stage.Startup)
		.Build();

		app.RunStartup();
		var world = app.GetWorld();

		var state = world.Get<InteractionState>(entityId);
		Assert.Equal(Interaction.Hovered, state.State);
	}

	[Fact]
	public void InteractionState_TransitionToPressed()
	{
		var app = CreateApp();
		ulong entityId = 0;

		app.AddSystem((Commands commands) =>
		{
			entityId = commands.Spawn()
				.Insert(new UiNode())
				.Insert(new Interactive())
				.Insert(new InteractionState { State = Interaction.None })
				.Id;
		})
		.InStage(Stage.Startup)
		.Build();

		app.RunStartup();
		var world = app.GetWorld();
		SetDeltaTime(app);

		// Set state to Pressed
		world.Set(entityId, new InteractionState { State = Interaction.Pressed });

		var state = world.Get<InteractionState>(entityId);
		Assert.Equal(Interaction.Pressed, state.State);
	}

	[Fact]
	public void InteractionState_TransitionToHovered()
	{
		var app = CreateApp();
		ulong entityId = 0;

		app.AddSystem((Commands commands) =>
		{
			entityId = commands.Spawn()
				.Insert(new UiNode())
				.Insert(new Interactive())
				.Insert(new InteractionState { State = Interaction.None })
				.Id;
		})
		.InStage(Stage.Startup)
		.Build();

		app.RunStartup();
		var world = app.GetWorld();
		SetDeltaTime(app);

		// Set state to Hovered
		world.Set(entityId, new InteractionState { State = Interaction.Hovered });

		var state = world.Get<InteractionState>(entityId);
		Assert.Equal(Interaction.Hovered, state.State);
	}

	[Fact]
	public void InteractionState_CanTransitionFromPressedToNone()
	{
		var app = CreateApp();
		ulong entityId = 0;

		app.AddSystem((Commands commands) =>
		{
			entityId = commands.Spawn()
				.Insert(new UiNode())
				.Insert(new Interactive())
				.Insert(new InteractionState { State = Interaction.Pressed })
				.Id;
		})
		.InStage(Stage.Startup)
		.Build();

		app.RunStartup();
		var world = app.GetWorld();
		SetDeltaTime(app);

		// Transition from Pressed to None
		world.Set(entityId, new InteractionState { State = Interaction.None });

		var state = world.Get<InteractionState>(entityId);
		Assert.Equal(Interaction.None, state.State);
	}

	[Fact]
	public void DeltaTime_ResourceIsRegistered()
	{
		var app = CreateApp();
		app.RunStartup();

		var world = app.GetWorld();
		var deltaTime = world.GetResource<DeltaTime>();

		Assert.NotNull(deltaTime);
		Assert.Equal(0f, deltaTime.Seconds);
	}

	[Fact]
	public void DeltaTime_CanBeUpdated()
	{
		var app = CreateApp();
		app.RunStartup();

		var world = app.GetWorld();
		var deltaTime = world.GetResource<DeltaTime>();

		deltaTime.Seconds = 0.016f;

		Assert.Equal(0.016f, deltaTime.Seconds);
	}

	[Fact]
	public void Interactive_ComponentHasFocusableProperty()
	{
		// Verify Interactive component has Focusable property
		var interactive = new Interactive(focusable: true);
		Assert.True(interactive.Focusable);

		var nonFocusable = new Interactive(focusable: false);
		Assert.False(nonFocusable.Focusable);
	}

	[Fact]
	public void InteractionState_ChangedFilterTriggersOnUpdate()
	{
		var app = CreateApp();
		ulong entityId = 0;
		bool changedDetected = false;

		app.AddSystem((Commands commands) =>
		{
			entityId = commands.Spawn()
				.Insert(new UiNode())
				.Insert(new Interactive())
				.Insert(new InteractionState { State = Interaction.None })
				.Id;
		})
		.InStage(Stage.Startup)
		.Build();

		// System that detects interaction state changes
		app.AddSystem((Query<Data<InteractionState>, Filter<Changed<InteractionState>>> changedQuery) =>
		{
			foreach (var (_, state) in changedQuery)
			{
				if (state.Ref.State == Interaction.Pressed)
				{
					changedDetected = true;
				}
			}
		})
		.InStage(Stage.Update)
		.Build();

		app.RunStartup();
		var world = app.GetWorld();
		SetDeltaTime(app);

		// Initial update
		app.Update();

		// Change state
		world.Set(entityId, new InteractionState { State = Interaction.Pressed });

		// Run update to detect change
		app.Update();

		Assert.True(changedDetected, "Changed<InteractionState> should detect state changes");
	}
}

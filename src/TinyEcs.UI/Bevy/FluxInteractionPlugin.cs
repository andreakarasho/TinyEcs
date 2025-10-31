using System;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Resource that provides delta time (time since last frame) for UI systems.
/// Must be updated each frame by the application (typically in Stage.First).
/// </summary>
public class DeltaTime
{
	/// <summary>Time since last frame in seconds</summary>
	public float Seconds { get; set; }

	public DeltaTime()
	{
		Seconds = 0f;
	}
}

/// <summary>
/// Plugin that provides a state machine for tracking UI interaction states.
/// Converts pointer events (Enter, Exit, Down, Up) into higher-level interaction states
/// (PointerEnter, PointerLeave, Pressed, Released, PressCanceled).
///
/// This system is the foundation for animated interactions and widget state management.
/// Ported from sickle_ui's flux_interaction.rs.
/// </summary>
public struct FluxInteractionPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// Register resources
		app.AddResource(new FluxInteractionConfig());
		app.AddResource(new DeltaTime());

		// System execution chain:
		// 1. Tick stopwatches and remove expired ones
		// 2. Update FluxInteraction based on pointer events
		// 3. Reset stopwatches when interaction changes
		// 4. Update PrevInteraction to track state transitions
		app.AddSystem((
			Res<FluxInteractionConfig> config,
			Res<DeltaTime> deltaTime,
			Commands commands,
			Query<Data<FluxInteractionStopwatch>> stopwatchQuery) =>
		{
			TickFluxInteractionStopwatch(config, deltaTime, commands, stopwatchQuery);
		})
		.InStage(Stage.PreUpdate)
		.Label("flux:tick-stopwatch")
		.Build();

		app.AddSystem((
			Commands commands,
			Query<Data<PrevInteraction, InteractionState, FluxInteraction>, Filter<Changed<InteractionState>>> changedQuery) =>
		{
			UpdateFluxInteraction(commands, changedQuery);
		})
		.InStage(Stage.PreUpdate)
		.Label("flux:update-interaction")
		.After("flux:tick-stopwatch")
		.Build();

		// Reset stopwatch when FluxInteraction changes
		app.AddSystem((
			Commands commands,
			Query<Data<FluxInteractionStopwatch>, Filter<Changed<FluxInteraction>>> changedFluxQuery) =>
		{
			ResetStopwatchOnChange(commands, changedFluxQuery);
		})
		.InStage(Stage.PreUpdate)
		.Label("flux:reset-stopwatch")
		.After("flux:update-interaction")
		.Build();

		// Add stopwatch to new FluxInteraction entities (using Added filter)
		// Also re-add stopwatches that were removed by the tick system
		app.AddSystem((
			Commands commands,
			Query<Data<FluxInteraction>, Filter<Without<FluxInteractionStopwatch>>> fluxWithoutStopwatch) =>
		{
			// Ensure ALL FluxInteraction entities have a stopwatch
			// This handles both newly added entities and entities whose stopwatch was removed
			foreach (var (entityId, _) in fluxWithoutStopwatch)
			{
				commands.Entity(entityId.Ref).Insert(new FluxInteractionStopwatch { ElapsedSeconds = 0f });
			}
		})
		.InStage(Stage.PreUpdate)
		.Label("flux:add-stopwatch")
		.After("flux:reset-stopwatch")
		.Build();

		app.AddSystem((
			Query<Data<PrevInteraction, InteractionState>, Filter<Changed<InteractionState>>> changedQuery) =>
		{
			UpdatePrevInteraction(changedQuery);
		})
		.InStage(Stage.PreUpdate)
		.Label("flux:update-prev")
		.After("flux:reset-stopwatch")
		.Build();
	}

	/// <summary>
	/// Tick all stopwatches and remove ones that exceed max duration.
	/// </summary>
	private static void TickFluxInteractionStopwatch(
		Res<FluxInteractionConfig> config,
		Res<DeltaTime> deltaTime,
		Commands commands,
		Query<Data<FluxInteractionStopwatch>> stopwatchQuery)
	{
		foreach (var (entityId, stopwatch) in stopwatchQuery)
		{
			ref var sw = ref stopwatch.Ref;

			if (sw.ElapsedSeconds > config.Value.MaxInteractionDuration)
			{
				// Remove stopwatch if it's been running too long
				commands.Entity(entityId.Ref).Remove<FluxInteractionStopwatch>();
			}
			else
			{
				// Tick the stopwatch
				sw.ElapsedSeconds += deltaTime.Value.Seconds;
			}
		}
	}

	/// <summary>
	/// Update FluxInteraction state based on changes to InteractionState.
	/// This system implements the state transition logic.
	/// </summary>
	private static void UpdateFluxInteraction(
		Commands commands,
		Query<Data<PrevInteraction, InteractionState, FluxInteraction>, Filter<Changed<InteractionState>>> changedQuery)
	{
		foreach (var (entityId, prev, curr, flux) in changedQuery)
		{
			ref var fluxRef = ref flux.Ref;

			// Don't update disabled interactions
			if (fluxRef.State == FluxInteractionState.Disabled)
				continue;

			ref readonly var prevState = ref prev.Ref;
			ref readonly var currState = ref curr.Ref;

			var oldState = fluxRef.State;

			// State transition logic (ported from sickle_ui)
			// None -> Hovered = PointerEnter
			if (prevState.State == PrevInteractionState.None && currState.State == InteractionStateEnum.Hovered)
			{
				fluxRef.State = FluxInteractionState.PointerEnter;
			}
			// None -> Pressed OR Hovered -> Pressed = Pressed
			else if ((prevState.State == PrevInteractionState.None && currState.State == InteractionStateEnum.Pressed) ||
			         (prevState.State == PrevInteractionState.Hovered && currState.State == InteractionStateEnum.Pressed))
			{
				fluxRef.State = FluxInteractionState.Pressed;
			}
			// Hovered -> None = PointerLeave
			else if (prevState.State == PrevInteractionState.Hovered && currState.State == InteractionStateEnum.None)
			{
				fluxRef.State = FluxInteractionState.PointerLeave;
			}
			// Pressed -> None = PressCanceled
			else if (prevState.State == PrevInteractionState.Pressed && currState.State == InteractionStateEnum.None)
			{
				fluxRef.State = FluxInteractionState.PressCanceled;
			}
			// Pressed -> Hovered = Released
			else if (prevState.State == PrevInteractionState.Pressed && currState.State == InteractionStateEnum.Hovered)
			{
				fluxRef.State = FluxInteractionState.Released;
			}

			// Only insert if state actually changed (triggers change detection)
			if (fluxRef.State != oldState)
			{
				commands.Entity(entityId.Ref).Insert(fluxRef);
			}
		}
	}

	/// <summary>
	/// Reset stopwatches when FluxInteraction changes.
	/// </summary>
	private static void ResetStopwatchOnChange(
		Commands commands,
		Query<Data<FluxInteractionStopwatch>, Filter<Changed<FluxInteraction>>> changedFluxQuery)
	{
		// Reset stopwatches for entities whose FluxInteraction just changed
		foreach (var (entityId, _) in changedFluxQuery)
		{
			commands.Entity(entityId.Ref).Insert(new FluxInteractionStopwatch { ElapsedSeconds = 0f });
		}
	}

	/// <summary>
	/// Update PrevInteraction to match current InteractionState.
	/// This must run after UpdateFluxInteraction to track transitions correctly.
	/// </summary>
	private static void UpdatePrevInteraction(
		Query<Data<PrevInteraction, InteractionState>, Filter<Changed<InteractionState>>> changedQuery)
	{
		foreach (var (prev, curr) in changedQuery)
		{
			ref var prevRef = ref prev.Ref;
			ref readonly var currState = ref curr.Ref;

			prevRef.State = currState.State switch
			{
				InteractionStateEnum.Pressed => PrevInteractionState.Pressed,
				InteractionStateEnum.Hovered => PrevInteractionState.Hovered,
				InteractionStateEnum.None => PrevInteractionState.None,
				_ => PrevInteractionState.None
			};
		}
	}
}

/// <summary>
/// Configuration for FluxInteraction behavior.
/// </summary>
public class FluxInteractionConfig
{
	/// <summary>
	/// Maximum duration (in seconds) for an interaction to be tracked.
	/// After this duration, the stopwatch is removed to prevent memory leaks.
	/// Default: 1.0 second.
	/// </summary>
	public float MaxInteractionDuration { get; set; } = 1.0f;
}

/// <summary>
/// Bundle for easily adding tracked interaction to an entity.
/// Includes FluxInteraction, PrevInteraction, and FluxInteractionStopwatch.
/// </summary>
public struct TrackedInteraction : IBundle
{
	public FluxInteraction Interaction;
	public PrevInteraction PrevInteraction;
	public FluxInteractionStopwatch Stopwatch;

	public TrackedInteraction()
	{
		Interaction = new FluxInteraction();
		PrevInteraction = new PrevInteraction();
		Stopwatch = new FluxInteractionStopwatch();
	}

	public readonly void Insert(EntityView entity)
	{
		entity.Set(Interaction);
		entity.Set(PrevInteraction);
		entity.Set(Stopwatch);
	}

	public readonly void Insert(EntityCommands entity)
	{
		entity.Insert(Interaction);
		entity.Insert(PrevInteraction);
		entity.Insert(Stopwatch);
	}
}

/// <summary>
/// High-level interaction state that represents the lifecycle of a pointer interaction.
/// This is the main state machine for UI interactions.
/// </summary>
public enum FluxInteractionState
{
	None,
	PointerEnter,
	PointerLeave,
	/// <summary>Pressing started, but not completed or cancelled</summary>
	Pressed,
	/// <summary>Pressing completed over the node</summary>
	Released,
	/// <summary>Pressing cancelled by releasing outside of node</summary>
	PressCanceled,
	Disabled
}

/// <summary>
/// Component that tracks the current FluxInteraction state.
/// </summary>
public struct FluxInteraction
{
	public FluxInteractionState State;

	public FluxInteraction()
	{
		State = FluxInteractionState.None;
	}
}

/// <summary>
/// Simplified previous state for tracking state transitions.
/// This is used to determine which FluxInteraction state to transition to.
/// </summary>
public enum PrevInteractionState
{
	None,
	Pressed,
	Hovered
}

/// <summary>
/// Component that tracks the previous interaction state.
/// Used for determining state transitions in the flux interaction state machine.
/// </summary>
public struct PrevInteraction
{
	public PrevInteractionState State;

	public PrevInteraction()
	{
		State = PrevInteractionState.None;
	}
}

/// <summary>
/// Stopwatch for tracking the duration of the current interaction.
/// Automatically reset when FluxInteraction changes.
/// Removed after MaxInteractionDuration to prevent memory leaks.
/// </summary>
public struct FluxInteractionStopwatch
{
	public float ElapsedSeconds;

	public FluxInteractionStopwatch()
	{
		ElapsedSeconds = 0f;
	}
}

/// <summary>
/// Low-level interaction state that directly reflects pointer events.
/// This is analogous to Bevy's built-in Interaction enum.
/// </summary>
public enum InteractionStateEnum
{
	None,
	Hovered,
	Pressed
}

/// <summary>
/// Component that tracks the current low-level interaction state.
/// Updated by pointer event handlers.
/// </summary>
public struct InteractionState
{
	public InteractionStateEnum State;

	public InteractionState()
	{
		State = InteractionStateEnum.None;
	}
}

using System.Numerics;
using TinyEcs.Bevy;
using TinyEcs.UI.Easing;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Plugin that provides animated interaction states for UI elements.
/// Ported from sickle_ui's animated_interaction module.
/// Works with FluxInteraction to provide smooth transitions between interaction states.
/// </summary>
public struct AnimatedInteractionPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// System to update animation progress based on FluxInteraction state and elapsed time
		// Runs EVERY FRAME to allow progress to increment over time
		app.AddSystem((
			Commands commands,
			Res<DeltaTime> deltaTime,
			Query<Data<AnimatedInteractionState, FluxInteraction, FluxInteractionStopwatch>> animatedQuery) =>
		{
			UpdateAnimationProgress(commands, deltaTime, animatedQuery);
		})
		.InStage(Stage.Update)
		.Label("animated-interaction:update-progress")
		.Build();
	}

	/// <summary>
	/// Updates animation progress for all animated interactions based on current state and elapsed time.
	/// </summary>
	private static void UpdateAnimationProgress(
		Commands commands,
		Res<DeltaTime> deltaTime,
		Query<Data<AnimatedInteractionState, FluxInteraction, FluxInteractionStopwatch>> animatedQuery)
	{
		foreach (var (entityId, animState, flux, stopwatch) in animatedQuery)
		{
			ref var state = ref animState.Ref;
			ref readonly var interaction = ref flux.Ref;
			ref readonly var timer = ref stopwatch.Ref;

			// Get animation config for current state
			var config = state.GetConfigForState(interaction.State);

			// Calculate progress based on elapsed time
			var elapsed = timer.ElapsedSeconds;
			var progress = CalculateProgress(interaction.State, config, elapsed);

			// Update state using Commands to trigger change detection
			state.Progress = progress;
			state.CurrentState = interaction.State;
			commands.Entity(entityId.Ref).Insert(state);
		}
	}

	/// <summary>
	/// Calculates animation progress (0-1) based on state, config, and elapsed time.
	/// </summary>
	private static AnimationProgress CalculateProgress(
		FluxInteractionState state,
		AnimationConfig config,
		float elapsed)
	{
		// Determine if this is an "in" or "out" transition
		var isOut = state == FluxInteractionState.PointerLeave ||
		            state == FluxInteractionState.Released ||
		            state == FluxInteractionState.PressCanceled;

		var duration = isOut
			? (config.OutDuration ?? config.Duration)
			: config.Duration;

		var easing = isOut
			? (config.OutEasing ?? config.Easing)
			: config.Easing;

		// Handle instant transitions (duration == 0)
		if (duration <= 0f)
		{
			return AnimationProgress.End;
		}

		// Calculate normalized time (0-1)
		var t = (elapsed / duration).Clamp(0f, 1f);

		// Apply easing function
		var easedT = t.Apply(easing);

		// Determine progress state
		if (easedT >= 1f)
		{
			return AnimationProgress.End;
		}
		else if (easedT <= 0f)
		{
			return AnimationProgress.Start;
		}
		else
		{
			return AnimationProgress.Inbetween(easedT);
		}
	}
}

/// <summary>
/// Animation configuration for a specific interaction state transition.
/// </summary>
public struct AnimationConfig
{
	/// <summary>
	/// Duration of the "in" animation (entering the state) in seconds.
	/// </summary>
	public float Duration;

	/// <summary>
	/// Easing function to apply to the "in" animation.
	/// </summary>
	public Ease Easing;

	/// <summary>
	/// Optional duration for the "out" animation (exiting the state).
	/// If null, uses Duration.
	/// </summary>
	public float? OutDuration;

	/// <summary>
	/// Optional easing function for the "out" animation.
	/// If null, uses Easing.
	/// </summary>
	public Ease? OutEasing;

	public AnimationConfig()
	{
		Duration = 0.1f; // Default 100ms
		Easing = Ease.Linear;
		OutDuration = null;
		OutEasing = null;
	}

	public AnimationConfig(float duration, Ease easing = Ease.Linear)
	{
		Duration = duration;
		Easing = easing;
		OutDuration = null;
		OutEasing = null;
	}

	public AnimationConfig(float duration, Ease easing, float outDuration, Ease outEasing)
	{
		Duration = duration;
		Easing = easing;
		OutDuration = outDuration;
		OutEasing = outEasing;
	}
}

/// <summary>
/// Represents the progress of an animation.
/// </summary>
public struct AnimationProgress
{
	private readonly ProgressType _type;
	private readonly float _value;

	private enum ProgressType
	{
		Start,
		Inbetween,
		End
	}

	private AnimationProgress(ProgressType type, float value = 0f)
	{
		_type = type;
		_value = value;
	}

	/// <summary>
	/// Animation at the start (progress = 0).
	/// </summary>
	public static AnimationProgress Start => new AnimationProgress(ProgressType.Start);

	/// <summary>
	/// Animation in progress (0 < progress < 1).
	/// </summary>
	public static AnimationProgress Inbetween(float progress) => new AnimationProgress(ProgressType.Inbetween, progress);

	/// <summary>
	/// Animation at the end (progress = 1).
	/// </summary>
	public static AnimationProgress End => new AnimationProgress(ProgressType.End, 1f);

	/// <summary>
	/// Returns true if animation is at the start.
	/// </summary>
	public bool IsStart => _type == ProgressType.Start;

	/// <summary>
	/// Returns true if animation is in progress.
	/// </summary>
	public bool IsInbetween => _type == ProgressType.Inbetween;

	/// <summary>
	/// Returns true if animation is at the end.
	/// </summary>
	public bool IsEnd => _type == ProgressType.End;

	/// <summary>
	/// Gets the normalized progress value (0-1).
	/// </summary>
	public float Value => _type switch
	{
		ProgressType.Start => 0f,
		ProgressType.End => 1f,
		ProgressType.Inbetween => _value,
		_ => 0f
	};
}

/// <summary>
/// Component that tracks animated interaction state for a UI element.
/// Stores animation configurations for different interaction states and current progress.
/// </summary>
public struct AnimatedInteractionState
{
	/// <summary>
	/// Current interaction state being animated.
	/// </summary>
	public FluxInteractionState CurrentState;

	/// <summary>
	/// Current animation progress.
	/// </summary>
	public AnimationProgress Progress;

	/// <summary>
	/// Base animation config (used for all states if specific configs not set).
	/// </summary>
	public AnimationConfig BaseConfig;

	/// <summary>
	/// Optional animation config for hover state (PointerEnter/PointerLeave).
	/// </summary>
	public AnimationConfig? HoverConfig;

	/// <summary>
	/// Optional animation config for press state (Pressed/Released).
	/// </summary>
	public AnimationConfig? PressConfig;

	/// <summary>
	/// Optional animation config for cancel state (PressCanceled).
	/// </summary>
	public AnimationConfig? CancelConfig;

	public AnimatedInteractionState()
	{
		CurrentState = FluxInteractionState.None;
		Progress = AnimationProgress.End;
		BaseConfig = new AnimationConfig(); // 100ms linear
		HoverConfig = null;
		PressConfig = null;
		CancelConfig = null;
	}

	/// <summary>
	/// Gets the appropriate animation config for a given interaction state.
	/// </summary>
	public readonly AnimationConfig GetConfigForState(FluxInteractionState state)
	{
		return state switch
		{
			FluxInteractionState.PointerEnter => HoverConfig ?? BaseConfig,
			FluxInteractionState.PointerLeave => HoverConfig ?? BaseConfig,
			FluxInteractionState.Pressed => PressConfig ?? BaseConfig,
			FluxInteractionState.Released => PressConfig ?? BaseConfig,
			FluxInteractionState.PressCanceled => CancelConfig ?? BaseConfig,
			_ => BaseConfig
		};
	}

	/// <summary>
	/// Builder pattern: Sets base animation config.
	/// </summary>
	public AnimatedInteractionState WithBase(float duration, Ease easing = Ease.Linear)
	{
		BaseConfig = new AnimationConfig(duration, easing);
		return this;
	}

	/// <summary>
	/// Builder pattern: Sets hover animation config.
	/// </summary>
	public AnimatedInteractionState WithHover(float duration, Ease easing = Ease.Linear)
	{
		HoverConfig = new AnimationConfig(duration, easing);
		return this;
	}

	/// <summary>
	/// Builder pattern: Sets press animation config.
	/// </summary>
	public AnimatedInteractionState WithPress(float duration, Ease easing = Ease.Linear)
	{
		PressConfig = new AnimationConfig(duration, easing);
		return this;
	}

	/// <summary>
	/// Builder pattern: Sets press animation config with separate out duration/easing.
	/// </summary>
	public AnimatedInteractionState WithPress(float duration, Ease easing, float outDuration, Ease outEasing)
	{
		PressConfig = new AnimationConfig(duration, easing, outDuration, outEasing);
		return this;
	}

	/// <summary>
	/// Builder pattern: Sets cancel animation config.
	/// </summary>
	public AnimatedInteractionState WithCancel(float duration, Ease easing = Ease.Linear)
	{
		CancelConfig = new AnimationConfig(duration, easing);
		return this;
	}
}

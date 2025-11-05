using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Direction of the slider (horizontal or vertical).
/// </summary>
public enum SliderDirection
{
	Horizontal,
	Vertical
}

/// <summary>
/// Marker component for slider thumb elements.
/// Used to identify slider thumbs for interaction handling.
/// </summary>
public struct SliderThumb { }

/// <summary>
/// Component used to manage the state of a slider during dragging.
/// Automatically added/removed when dragging starts/stops.
/// </summary>
public struct SliderDragState
{
	/// <summary>
	/// Whether the slider is currently being dragged.
	/// </summary>
	public bool IsDragging;
}

/// <summary>
/// Component that represents a slider widget with a draggable thumb.
/// The slider allows selecting a value between min and max by dragging the thumb or clicking the track.
/// </summary>
public struct Slider
{
	/// <summary>Current value (clamped between Min and Max)</summary>
	public float Value;

	/// <summary>Minimum value</summary>
	public float Min;

	/// <summary>Maximum value</summary>
	public float Max;

	/// <summary>Direction of the slider</summary>
	public SliderDirection Direction;

	/// <summary>Entity ID of the thumb element</summary>
	public ulong ThumbEntity;

	/// <summary>Entity ID of the track/rail element</summary>
	public ulong TrackEntity;

	/// <summary>Entity ID of the fill element (optional, shows filled portion)</summary>
	public ulong FillEntity;

	public Slider(float min, float max, float initialValue, SliderDirection direction = SliderDirection.Horizontal)
	{
		Min = min;
		Max = max;
		Value = Math.Clamp(initialValue, min, max);
		Direction = direction;
		ThumbEntity = 0;
		TrackEntity = 0;
		FillEntity = 0;
	}

	/// <summary>
	/// Gets the normalized value (0.0 to 1.0)
	/// </summary>
	public readonly float GetNormalizedValue()
	{
		if (Max <= Min)
			return 0f;
		return (Value - Min) / (Max - Min);
	}

	/// <summary>
	/// Sets the value from a normalized value (0.0 to 1.0)
	/// </summary>
	public void SetFromNormalized(float normalized)
	{
		normalized = Math.Clamp(normalized, 0f, 1f);
		Value = Min + normalized * (Max - Min);
	}
}

/// <summary>
/// Event triggered when a slider value changes.
/// Use with On&lt;SliderChanged&gt; in observers.
/// </summary>
public readonly struct SliderChanged
{
	public readonly float Value;
	public readonly float NormalizedValue;

	public SliderChanged(float value, float normalizedValue)
	{
		Value = value;
		NormalizedValue = normalizedValue;
	}
}

/// <summary>
/// Plugin that adds slider widget functionality.
/// Handles clicking/dragging the track or thumb to update slider value and updating visual state.
///
/// Usage:
/// <code>
/// app.AddPlugin(new SliderPlugin());
/// </code>
/// </summary>
public struct SliderPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// Observer to start dragging when pointer down on track or thumb
		app.AddObserver<On<UiPointerTrigger>, Query<Data<Slider>>, Query<Data<Parent>, Filter<With<SliderThumb>>>, Commands>(StartSliderDrag);

		// Observer to stop dragging when pointer up
		app.AddObserver<On<UiPointerTrigger>, Query<Data<SliderDragState>>, Commands>(StopSliderDrag);

		// System to handle track/thumb interaction and value updates
		// Runs every frame to handle continuous dragging
		// Only processes sliders that have SliderDragState (actively being dragged)
		app.AddSystem((
			Commands commands,
			Res<PointerInputState> pointerInput,
			Query<Data<Slider, SliderDragState>> activeDrags,
			Query<Data<ComputedLayout>> layouts) =>
		{
			UpdateSliderValue(commands, pointerInput, activeDrags, layouts);
		})
		.InStage(Stage.PreUpdate)
		.Label("slider:update-value")
		.After("flux:update-interaction")
		.Build();

		// System to update thumb and fill positions when slider value changes
		// Runs every frame to handle layout changes
		app.AddSystem((
			Commands commands,
			Query<Data<Slider>> allSliders,
			Query<Data<UiNode>> allNodes,
			Query<Data<ComputedLayout>> layouts) =>
		{
			UpdateSliderVisuals(commands, allSliders, allNodes, layouts);
		})
		.InStage(Stage.PostUpdate)
		.Label("slider:update-visuals")
		.After("flexbox:read_layout")
		.Build();
	}

	/// <summary>
	/// Starts slider dragging when pointer down event occurs on track or thumb.
	/// </summary>
	private static void StartSliderDrag(
		On<UiPointerTrigger> trigger,
		Query<Data<Slider>> sliderTracks,
		Query<Data<Parent>, Filter<With<SliderThumb>>> thumbs,
		Commands commands)
	{
		var evt = trigger.Event.Event;
		if (evt.Type != UiPointerEventType.PointerDown)
			return;

		// Check if clicked on a slider track
		if (sliderTracks.Contains(trigger.EntityId))
		{
			commands.Entity(trigger.EntityId).Insert(new SliderDragState { IsDragging = true });
			return;
		}

		// Check if clicked on a slider thumb - add drag state to parent track
		if (thumbs.Contains(trigger.EntityId))
		{
			var (_, parent) = thumbs.Get(trigger.EntityId);
			var trackEntityId = parent.Ref.Id;

			if (sliderTracks.Contains(trackEntityId))
			{
				commands.Entity(trackEntityId).Insert(new SliderDragState { IsDragging = true });
			}
		}
	}

	/// <summary>
	/// Stops slider dragging when pointer up event occurs.
	/// Stops ALL active slider drags, not just the one receiving the event.
	/// </summary>
	private static void StopSliderDrag(
		On<UiPointerTrigger> trigger,
		Query<Data<SliderDragState>> dragStates,
		Commands commands)
	{
		var evt = trigger.Event.Event;
		if (evt.Type != UiPointerEventType.PointerUp)
			return;

		// Remove drag state from ALL sliders that are being dragged
		// This ensures sliders stop even if pointer was released outside their bounds
		foreach (var (entityId, _) in dragStates)
		{
			commands.Entity(entityId.Ref).Remove<SliderDragState>();
		}
	}

	/// <summary>
	/// Updates slider value based on current pointer position while dragging.
	/// Only runs for sliders that have SliderDragState (actively being dragged).
	/// </summary>
	private static void UpdateSliderValue(
		Commands commands,
		Res<PointerInputState> pointerInput,
		Query<Data<Slider, SliderDragState>> activeDrags,
		Query<Data<ComputedLayout>> layouts)
	{
		var mousePos = pointerInput.Value.Position;
		var isPointerDown = pointerInput.Value.IsPrimaryButtonDown;

		// Process all sliders that are actively being dragged
		foreach (var (trackEntityId, slider, dragState) in activeDrags)
		{
			ref var s = ref slider.Ref;

			// Stop dragging if pointer button was released
			if (!isPointerDown)
			{
				commands.Entity(trackEntityId.Ref).Remove<SliderDragState>();
				continue;
			}

			// Get track layout
			if (s.TrackEntity == 0 || !layouts.Contains(s.TrackEntity))
				continue;

			var (_, trackLayout) = layouts.Get(s.TrackEntity);
			ref readonly var track = ref trackLayout.Ref;

			// Calculate normalized position based on direction
			float normalized;
			if (s.Direction == SliderDirection.Horizontal)
			{
				var relativeX = mousePos.X - track.X;
				normalized = relativeX / track.Width;
			}
			else
			{
				var relativeY = mousePos.Y - track.Y;
				normalized = relativeY / track.Height;
			}

			// Clamp and set value
			normalized = Math.Clamp(normalized, 0f, 1f);
			var oldValue = s.Value;
			s.SetFromNormalized(normalized);

			// Only update if value changed
			if (Math.Abs(s.Value - oldValue) > 0.0001f)
			{
				// Re-insert to trigger change detection
				commands.Entity(trackEntityId.Ref).Insert(s);

				// Emit SliderChanged event both globally and per-entity
				var changeEvent = new SliderChanged(s.Value, normalized);
				commands.Entity(trackEntityId.Ref).EmitTrigger(changeEvent);  // Per-entity (BevyObservers)
				commands.EmitTrigger(changeEvent);  // Global (EventChannel)
			}
		}
	}

	/// <summary>
	/// Updates the thumb and fill positions based on slider value.
	/// </summary>
	private static void UpdateSliderVisuals(
		Commands commands,
		Query<Data<Slider>> allSliders,
		Query<Data<UiNode>> allNodes,
		Query<Data<ComputedLayout>> layouts)
	{
		foreach (var (entityId, slider) in allSliders)
		{
			ref readonly var s = ref slider.Ref;
			var normalized = s.GetNormalizedValue();

			// Get track layout
			if (s.TrackEntity == 0 || !layouts.Contains(s.TrackEntity))
				continue;

			var (_, trackLayout) = layouts.Get(s.TrackEntity);
			ref readonly var track = ref trackLayout.Ref;

			// Skip if layout not calculated yet (width/height would be 0)
			if (s.Direction == SliderDirection.Horizontal && track.Width <= 0.01f)
				continue;
			if (s.Direction == SliderDirection.Vertical && track.Height <= 0.01f)
				continue;

			// Update thumb position
			if (s.ThumbEntity != 0 && allNodes.Contains(s.ThumbEntity))
			{
				var (_, thumbNode) = allNodes.Get(s.ThumbEntity);
				ref var thumb = ref thumbNode.Ref;

				// Get thumb size for centering
				float thumbSize = 20f; // Default thumb size
				if (layouts.Contains(s.ThumbEntity))
				{
					var (_, thumbLayout) = layouts.Get(s.ThumbEntity);
					thumbSize = s.Direction == SliderDirection.Horizontal
						? thumbLayout.Ref.Width
						: thumbLayout.Ref.Height;
				}

				if (s.Direction == SliderDirection.Horizontal)
				{
					// Horizontal: position thumb along x-axis
					var thumbX = normalized * track.Width - (thumbSize / 2);
					thumb.Left = FlexValue.Points(thumbX);
					thumb.Top = FlexValue.Points(-5f); // Offset to center vertically on track
				}
				else
				{
					// Vertical: position thumb along y-axis
					var thumbY = normalized * track.Height - (thumbSize / 2);
					thumb.Top = FlexValue.Points(thumbY);
					thumb.Left = FlexValue.Points(-5f); // Offset to center horizontally on track
				}

				thumb.PositionType = Flexbox.PositionType.Absolute;
				commands.Entity(s.ThumbEntity).Insert(thumb);
			}

			// Update fill size
			if (s.FillEntity != 0 && allNodes.Contains(s.FillEntity))
			{
				var (_, fillNode) = allNodes.Get(s.FillEntity);
				ref var fill = ref fillNode.Ref;

				if (s.Direction == SliderDirection.Horizontal)
				{
					// Horizontal: fill from left to thumb position
					fill.Width = FlexValue.Percent(normalized * 100f);
				}
				else
				{
					// Vertical: fill from top to thumb position
					fill.Height = FlexValue.Percent(normalized * 100f);
				}

				commands.Entity(s.FillEntity).Insert(fill);
			}
		}
	}
}

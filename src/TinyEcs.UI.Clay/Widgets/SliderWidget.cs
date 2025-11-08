using System;
using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Component to track slider state.
/// </summary>
public struct SliderState
{
	public ulong LabelEntityId;
	public ulong FillEntityId;
	public ulong TrackEntityId;
	public ulong ThumbEntityId;
	public float Value;
	public float Min;
	public float Max;
	public float Step;
	public bool IsDragging;
	public string Label;
}

/// <summary>
/// Marker component to update slider label text.
/// </summary>
public struct SliderLabelUpdate
{
	public string Text;
}

/// <summary>
/// Marker component to update slider fill width.
/// </summary>
public struct SliderFillUpdate
{
	public float NormalizedValue;
}

/// <summary>
/// Event fired when a slider value changes.
/// </summary>
public struct SliderValueChanged
{
	public float Value;
}

/// <summary>
/// Extension methods for creating slider widgets.
/// </summary>
public static class SliderWidget
{
	/// <summary>
	/// Creates a slider widget with label, track, fill, and draggable thumb using theme colors.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the slider to</param>
	/// <param name="theme">Theme resource for styling</param>
	/// <param name="label">Label text for the slider</param>
	/// <param name="initialValue">Initial slider value</param>
	/// <param name="min">Minimum value</param>
	/// <param name="max">Maximum value</param>
	/// <param name="step">Step increment (0 for continuous)</param>
	/// <returns>The slider container entity ID</returns>
	/// <remarks>
	/// Listen to SliderValueChanged event to be notified when value changes:
	/// app.AddObserver&lt;On&lt;SliderValueChanged&gt;&gt;((world, trigger) => { ... });
	/// </remarks>
	public static ulong CreateSlider(
		this Commands commands,
		EntityCommands parent,
		ClayTheme theme,
		string label,
		float initialValue,
		float min = 0f,
		float max = 1f,
		float step = 0f)
	{
		var sliderTheme = theme.Slider;

		// Container for the slider (label + slider)
		var containerNode = ClayNode.Configure()
			.Size(300, 80)
			.Column()
			.Padding(8)
			.Gap(8)
			.Background(50, 55, 60, 255)
			.CornerRadius(4)
			.Build();

		var container = commands.SpawnClayElement(containerNode);
		parent.AddChild(container);

		// Label
		var labelNode = ClayNode.Configure()
			.WidthGrow()
			.Height(20)
			.Text($"{label}: {initialValue:F2}", theme.Typography.DefaultFontSize, sliderTheme.LabelColor)
			.Build();

		var labelEntity = commands.SpawnClayElement(labelNode);
		container.AddChild(labelEntity);

		// Slider track container - this is the interactive area
		var sliderContainerNode = ClayNode.Configure()
			.WidthGrow()
			.Height(24)
			.Row()
			.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
			.Build();

		var sliderContainer = commands.SpawnClayElement(sliderContainerNode);
		container.AddChild(sliderContainer);

		// Rail container - wraps the track to center it vertically
		var railNode = ClayNode.Configure()
			.WidthGrow()
			.HeightGrow()
			.Row()
			.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
			.Build();

		var rail = commands.SpawnClayElement(railNode);
		sliderContainer.AddChild(rail);

		// Track (background rail) - centered within the rail container
		var trackHeight = sliderTheme.TrackHeight;
		var trackNode = ClayNode.Configure()
			.WidthGrow()
			.Height(trackHeight)
			.Row()
			.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
			.Background(sliderTheme.TrackColor)
			.CornerRadius(sliderTheme.CornerRadius)
			.Build();

		var track = commands.SpawnClayElement(trackNode);
		rail.AddChild(track);

		// Fill (the colored part showing the value)
		// Note: Using ClayNode.Default with for percentage sizing (not supported in fluent API yet)
		float normalizedValue = (initialValue - min) / (max - min);
		var fillNode = ClayNode.Default with
		{
			Layout = new Clay_LayoutConfig
			{
				sizing = new Clay_Sizing(
					Clay_SizingAxis.Percent(normalizedValue),
					Clay_SizingAxis.Fixed(trackHeight)
				),
				layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
				childAlignment = new Clay_ChildAlignment(
					Clay_LayoutAlignmentX.CLAY_ALIGN_X_RIGHT,
					Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
				)
			},
			Rectangle = new Clay_RectangleRenderData
			{
				backgroundColor = sliderTheme.FillColor
			},
			CornerRadius = Clay_CornerRadius.All(sliderTheme.CornerRadius)
		};

		var fill = commands.SpawnClayElement(fillNode);
		track.AddChild(fill);

		// Thumb (the draggable circle) - slightly larger than the track and centered on it
		var thumbSize = sliderTheme.ThumbSize;
		var thumbNode = ClayNode.Configure()
			.Size(thumbSize, thumbSize)
			.Background(sliderTheme.ThumbColor)
			.CornerRadius((ushort)(thumbSize / 2))
			.Border(new Clay_Color(255, 255, 255, 50), 1)
			.Build();

		var thumb = commands.SpawnClayElement(thumbNode);
		fill.AddChild(thumb);

		// Add slider interaction state component
		commands.Entity(sliderContainer.Id).Insert(new SliderState
		{
			LabelEntityId = labelEntity.Id,
			FillEntityId = fill.Id,
			TrackEntityId = track.Id,
			ThumbEntityId = thumb.Id,
			Value = initialValue,
			Min = min,
			Max = max,
			Step = step,
			IsDragging = false,
			Label = label
		});

		// Capture slider container ID for use in observer closure
		var sliderContainerId = sliderContainer.Id;

		// Add pointer observers for interaction
		sliderContainer.Observe<On<ClayPointerEvent>, Commands, Query<Data<SliderState>>, Query<Data<ClayComputedLayout>>>((trigger, cmd, stateQuery, layoutQuery) =>
		{
			var evt = trigger.Event;

			// Use the slider container ID (where SliderState is stored), not the event's entity ID
			if (!stateQuery.Contains(sliderContainerId))
			{
				return;
			}

			// Stop propagation - we're handling this event
			trigger.Propagate(false);

			var (_, statePtr) = stateQuery.Get(sliderContainerId);
			var state = statePtr.Ref;

			// Get the actual computed dimensions from the track and thumb
			var (_, trackLayoutPtr) = layoutQuery.Get(state.TrackEntityId);
			var (_, thumbLayoutPtr) = layoutQuery.Get(state.ThumbEntityId);
			var trackLayout = trackLayoutPtr.Ref;
			var thumbLayout = thumbLayoutPtr.Ref;

			// Get the slider container layout to calculate relative positions
			var (_, containerLayoutPtr) = layoutQuery.Get(sliderContainerId);
			var containerLayout = containerLayoutPtr.Ref;

			if (evt.EventType == ClayPointerEventType.Pressed)
			{
				state.IsDragging = true;

				// Update value based on click position using track-relative coordinates
				var trackWidth = trackLayout.Width;
				var thumbWidth = thumbLayout.Width;
				var trackOffsetX = trackLayout.X - containerLayout.X;

				// Convert mouse position to track-relative coordinates
				// Match Lua: simple pos = lx / tb.width
				var trackLocalX = evt.LocalPosition.X - trackOffsetX;
				var normalized = Math.Clamp(trackLocalX / trackWidth, 0f, 1f);
				var newValue = min + normalized * (max - min);

				if (step > 0)
				{
					newValue = MathF.Round(newValue / step) * step;
				}

				state.Value = Math.Clamp(newValue, min, max);

				// Update state component
				cmd.Entity(sliderContainerId).Insert(state);
				UpdateSliderVisuals(cmd, state, sliderContainerId);
			}
			else if (evt.EventType == ClayPointerEventType.Move && state.IsDragging)
			{
				// Update value based on mouse position using track-relative coordinates
				var trackWidth = trackLayout.Width;
				var thumbWidth = thumbLayout.Width;
				var trackOffsetX = trackLayout.X - containerLayout.X;

				// Convert mouse position to track-relative coordinates
				// Match Lua: simple pos = lx / tb.width
				var trackLocalX = evt.LocalPosition.X - trackOffsetX;
				var normalized = Math.Clamp(trackLocalX / trackWidth, 0f, 1f);
				var newValue = min + normalized * (max - min);

				if (step > 0)
				{
					newValue = MathF.Round(newValue / step) * step;
				}

				state.Value = Math.Clamp(newValue, min, max);

				// Update state component
				cmd.Entity(sliderContainerId).Insert(state);
				UpdateSliderVisuals(cmd, state, sliderContainerId);
			}
			else if (evt.EventType == ClayPointerEventType.Released)
			{
				state.IsDragging = false;
				cmd.Entity(sliderContainerId).Insert(state);
			}
		});

		return sliderContainer.Id;
	}

	private static void UpdateSliderVisuals(Commands commands, SliderState state, ulong containerId)
	{
		// Create a local copy to capture in closures
		var label = state.Label;
		var value = state.Value;
		var min = state.Min;
		var max = state.Max;

		// Update label text
		commands.Entity(state.LabelEntityId).Insert(new SliderLabelUpdate { Text = $"{label}: {value:F2}" });

		// Update fill width
		float normalized = (value - min) / (max - min);
		commands.Entity(state.FillEntityId).Insert(new SliderFillUpdate { NormalizedValue = normalized });

		// Emit event for the value change
		commands.Entity(containerId).EmitTrigger(new SliderValueChanged
		{
			Value = value
		});
	}
}

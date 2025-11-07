using System;
using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Component to track radio button state.
/// </summary>
public struct RadioState
{
	public ulong RingEntityId;
	public ulong DotEntityId;
	public ulong LabelEntityId;
	public string GroupKey;
	public string Value;
	public bool IsPressed;
	public bool Disabled;
}

/// <summary>
/// Resource to track radio group selections.
/// </summary>
public class RadioGroupState
{
	public Dictionary<string, string?> SelectedValues { get; } = new();
}

/// <summary>
/// Event fired when a radio button value changes.
/// </summary>
public struct RadioValueChanged
{
	public string GroupKey;
	public string? Value;
}

/// <summary>
/// Marker component to update radio button dot alpha.
/// </summary>
public struct RadioDotUpdate
{
	public byte Alpha;
}

/// <summary>
/// Extension methods for creating radio button widgets.
/// </summary>
public static class RadioWidget
{
	/// <summary>
	/// Creates a radio button widget with a ring and optional label.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the radio button to</param>
	/// <param name="groupKey">Key identifying the radio group</param>
	/// <param name="value">Value this radio button represents</param>
	/// <param name="label">Label text for the radio button (optional)</param>
	/// <param name="defaultSelected">Whether this button should be selected by default</param>
	/// <param name="disabled">Whether the radio button is disabled</param>
	/// <param name="size">Size of the radio button in pixels</param>
	/// <returns>The radio button container entity ID</returns>
	/// <remarks>
	/// Listen to RadioValueChanged event to be notified when selection changes:
	/// entity.Observe&lt;On&lt;RadioValueChanged&gt;&gt;((trigger) => { ... });
	/// </remarks>
	public static ulong CreateRadioButton(
		this Commands commands,
		EntityCommands parent,
		string groupKey,
		string value,
		string? label = null,
		bool defaultSelected = false,
		bool disabled = false,
		float size = 20f)
	{
		// Container for radio button (ring + label)
		var containerNode = ClayNode.Configure()
			.WidthFit(0, float.MaxValue)
			.Height(size)
			.Row()
			.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
			.Gap(8)
			.Build();

		var container = commands.SpawnClayElement(containerNode);
		parent.AddChild(container);

		// Radio ring (outer circle)
		var ringColor = disabled
			? new Clay_Color(30, 30, 30, 100)
			: new Clay_Color(40, 40, 40, 160);

		var ringNode = ClayNode.Configure()
			.Size(size, size)
			.AlignCenter()
			.Background(ringColor)
			.Border(new Clay_Color(255, 255, 255, 64), 1)
			.CornerRadius(18) // Circular
			.Build();

		var ring = commands.SpawnClayElement(ringNode);
		container.AddChild(ring);

		// Dot (inner circle) - always present but transparent when not selected
		var dotSize = Math.Max(6, (int)(size * 0.55f));
		var dotAlpha = defaultSelected ? (byte)220 : (byte)0;

		var dotNode = ClayNode.Configure()
			.Size(dotSize, dotSize)
			.Background(120, 190, 255, dotAlpha)
			.Border(new Clay_Color(255, 255, 255, dotAlpha > 0 ? (byte)90 : (byte)0), 1)
			.CornerRadius(18) // Circular
			.Build();

		var dot = commands.SpawnClayElement(dotNode);
		ring.AddChild(dot); // Always add dot to ring

		// Label (if provided)
		ulong labelEntityId = 0;
		if (!string.IsNullOrEmpty(label))
		{
			var labelNode = ClayNode.Configure()
				.Text(label, 16, new Clay_Color(230, 230, 240, 255))
				.Build();

			var labelEntity = commands.SpawnClayElement(labelNode);
			container.AddChild(labelEntity);
			labelEntityId = labelEntity.Id;
		}

		// Add radio button state component
		commands.Entity(container.Id).Insert(new RadioState
		{
			RingEntityId = ring.Id,
			DotEntityId = dot.Id,
			LabelEntityId = labelEntityId,
			GroupKey = groupKey,
			Value = value,
			IsPressed = false,
			Disabled = disabled
		});

		// Capture container ID for use in observer closure
		var containerId = container.Id;

		// Add pointer observers for interaction
		container.Observe<On<ClayPointerEvent>, Commands, Query<Data<RadioState>>, ResMut<RadioGroupState>>((trigger, cmd, stateQuery, groupState) =>
		{
			var evt = trigger.Event;

			// Use the container ID (where RadioState is stored)
			if (!stateQuery.Contains(containerId))
			{
				return;
			}

			var (_, statePtr) = stateQuery.Get(containerId);
			var state = statePtr.Ref;

			// Ignore if disabled
			if (state.Disabled)
			{
				return;
			}

			// Stop propagation - we're handling this event
			trigger.Propagate(false);

			if (evt.EventType == ClayPointerEventType.Pressed)
			{
				state.IsPressed = true;
				cmd.Entity(containerId).Insert(state);
			}
			else if (evt.EventType == ClayPointerEventType.Released && state.IsPressed)
			{
				state.IsPressed = false;
				cmd.Entity(containerId).Insert(state);

				// Get current selection
				var currentSelection = groupState.Value.SelectedValues.GetValueOrDefault(state.GroupKey);
				var wasSelected = currentSelection == state.Value;

				// Update selection
				groupState.Value.SelectedValues[state.GroupKey] = state.Value;

				// If this button wasn't selected before, update dot visibility
				if (!wasSelected)
				{
					// Update all radio buttons in this group
					foreach (var (otherEntityId, otherStatePtr) in stateQuery)
					{
						var otherState = otherStatePtr.Ref;

						// Only process buttons in the same group
						if (otherState.GroupKey != state.GroupKey)
							continue;

						// Determine if this button should be selected
						var shouldBeSelected = otherState.Value == state.Value;
						var newAlpha = shouldBeSelected ? (byte)220 : (byte)0;

						// Update the dot's color by fetching its ClayNode and modifying it
						cmd.Entity(otherState.DotEntityId).Insert(new RadioDotUpdate
						{
							Alpha = newAlpha
						});
					}

					// Emit event for the value change
					cmd.Entity(containerId).EmitTrigger(new RadioValueChanged
					{
						GroupKey = state.GroupKey,
						Value = state.Value
					});
				}
			}
		});

		return container.Id;
	}
}

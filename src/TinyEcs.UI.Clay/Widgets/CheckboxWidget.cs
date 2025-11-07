using System;
using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Component to track checkbox state.
/// </summary>
public struct CheckboxState
{
	public ulong BoxEntityId;
	public ulong FillEntityId;
	public ulong LabelEntityId;
	public bool Checked;
	public bool IsPressed;
	public string Label;
	public bool Disabled;
}

/// <summary>
/// Marker component to update checkbox box color based on state.
/// </summary>
public struct CheckboxBoxUpdate
{
	public Clay_Color Color;
}

/// <summary>
/// Marker component to update checkbox fill alpha.
/// </summary>
public struct CheckboxFillUpdate
{
	public byte Alpha;
}

/// <summary>
/// Event fired when a checkbox value changes.
/// </summary>
public struct CheckboxValueChanged
{
	public bool Checked;
}

/// <summary>
/// Extension methods for creating checkbox widgets.
/// </summary>
public static class CheckboxWidget
{
	/// <summary>
	/// Creates a checkbox widget with a box and optional label.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the checkbox to</param>
	/// <param name="label">Label text for the checkbox (optional)</param>
	/// <param name="defaultChecked">Initial checked state</param>
	/// <param name="disabled">Whether the checkbox is disabled</param>
	/// <param name="size">Size of the checkbox box in pixels</param>
	/// <returns>The checkbox container entity ID</returns>
	/// <remarks>
	/// Listen to CheckboxValueChanged event to be notified when value changes:
	/// app.AddObserver&lt;On&lt;CheckboxValueChanged&gt;&gt;((world, trigger) => { ... });
	/// </remarks>
	public static ulong CreateCheckbox(
		this Commands commands,
		EntityCommands parent,
		string? label = null,
		bool defaultChecked = false,
		bool disabled = false,
		float size = 18f)
	{
		// Container for checkbox (box + label)
		var containerNode = ClayNode.Configure()
			.WidthFit(0, float.MaxValue)
			.Height(size)
			.Row()
			.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
			.Gap(8)
			.Build();

		var container = commands.SpawnClayElement(containerNode);
		parent.AddChild(container);

		// Checkbox box
		var boxColor = disabled
			? new Clay_Color(30, 30, 30, 100)
			: new Clay_Color(40, 40, 40, 160);

		var boxNode = ClayNode.Configure()
			.Size(size, size)
			.AlignCenter()
			.Background(boxColor)
			.Border(new Clay_Color(255, 255, 255, 64), 1)
			.Build();

		var box = commands.SpawnClayElement(boxNode);
		container.AddChild(box);

		// Fill (checkmark) - always present but transparent when not checked
		var padding = Math.Max(3, (int)(size * 0.2f));
		var fillSize = size - padding * 2;
		var fillAlpha = defaultChecked ? (byte)220 : (byte)0;

		var fillNode = ClayNode.Configure()
			.Size(fillSize, fillSize)
			.Background(120, 190, 255, fillAlpha)
			.Border(new Clay_Color(255, 255, 255, fillAlpha > 0 ? (byte)90 : (byte)0), 1)
			.Build();

		var fill = commands.SpawnClayElement(fillNode);
		box.AddChild(fill); // Always add fill to box

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

		// Add checkbox state component
		commands.Entity(container.Id).Insert(new CheckboxState
		{
			BoxEntityId = box.Id,
			FillEntityId = fill.Id,
			LabelEntityId = labelEntityId,
			Checked = defaultChecked,
			IsPressed = false,
			Label = label ?? string.Empty,
			Disabled = disabled
		});

		// Capture container ID for use in observer closure
		var containerId = container.Id;

		// Add pointer observers for interaction
		container.Observe<On<ClayPointerEvent>, Commands, Query<Data<CheckboxState>>>((trigger, cmd, stateQuery) =>
		{
			var evt = trigger.Event;

			// Use the container ID (where CheckboxState is stored)
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
				// Toggle on release
				state.Checked = !state.Checked;
				state.IsPressed = false;
				cmd.Entity(containerId).Insert(state);

				// Update fill alpha based on checked state
				var newAlpha = state.Checked ? (byte)220 : (byte)0;
				cmd.Entity(state.FillEntityId).Insert(new CheckboxFillUpdate
				{
					Alpha = newAlpha
				});

				// Emit event for the value change
				cmd.Entity(containerId).EmitTrigger(new CheckboxValueChanged
				{
					Checked = state.Checked
				});
			}
		});

		return container.Id;
	}
}

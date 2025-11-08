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
	public Clay_Color FillColor;       // Theme fill color for the checkmark
	public Clay_Color FillBorderColor; // Theme border color for the checkmark
}

/// <summary>
/// Marker component to update checkbox box color based on state.
/// </summary>
public struct CheckboxBoxUpdate
{
	public Clay_Color Color;
}

/// <summary>
/// Marker component to update checkbox fill color and alpha.
/// </summary>
public struct CheckboxFillUpdate
{
	public Clay_Color FillColor;
	public Clay_Color BorderColor;
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
	/// Creates a checkbox widget with a box and optional label using theme colors.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the checkbox to</param>
	/// <param name="theme">Theme resource for styling</param>
	/// <param name="label">Label text for the checkbox (optional)</param>
	/// <param name="defaultChecked">Initial checked state</param>
	/// <param name="disabled">Whether the checkbox is disabled</param>
	/// <returns>The checkbox container entity ID</returns>
	/// <remarks>
	/// Listen to CheckboxValueChanged event to be notified when value changes:
	/// app.AddObserver&lt;On&lt;CheckboxValueChanged&gt;&gt;((world, trigger) => { ... });
	/// </remarks>
	public static ulong CreateCheckbox(
		this Commands commands,
		EntityCommands parent,
		ClayTheme theme,
		string? label = null,
		bool defaultChecked = false,
		bool disabled = false)
	{
		var checkboxTheme = theme.Checkbox;
		var size = checkboxTheme.Size;

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

		// Checkbox box - use theme colors
		var boxColor = disabled ? checkboxTheme.DisabledColor :
					   defaultChecked ? checkboxTheme.CheckedColor : checkboxTheme.BoxColor;

		var boxNode = ClayNode.Configure()
			.Size(size, size)
			.AlignCenter()
			.Background(boxColor)
			.Border(checkboxTheme.BorderColor, checkboxTheme.BorderWidth)
			.CornerRadius(checkboxTheme.CornerRadius)
			.Build();

		var box = commands.SpawnClayElement(boxNode);
		container.AddChild(box);

		// Fill (checkmark) - always present but transparent when not checked
		var padding = Math.Max(3, (int)(size * 0.2f));
		var fillSize = size - padding * 2;
		var fillColor = checkboxTheme.FillColor;
		var fillAlpha = defaultChecked ? (byte)fillColor.a : (byte)0;
		var fillNode = ClayNode.Configure()
			.Size(fillSize, fillSize)
			.Background(new Clay_Color(fillColor.r, fillColor.g, fillColor.b, fillAlpha))
			.CornerRadius((ushort)(checkboxTheme.CornerRadius > 0 ? checkboxTheme.CornerRadius - 1 : 0))
			.Build();

		var fill = commands.SpawnClayElement(fillNode);
		box.AddChild(fill); // Always add fill to box

		// Label (if provided)
		ulong labelEntityId = 0;
		if (!string.IsNullOrEmpty(label))
		{
			var labelNode = ClayNode.Configure()
				.Text(label, theme.Typography.DefaultFontSize, checkboxTheme.LabelColor)
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
			Disabled = disabled,
			FillColor = checkboxTheme.FillColor,
			FillBorderColor = new Clay_Color(255, 255, 255, 90) // Default border color for fill
		});

		// Capture container ID for use in observer closure
		var containerId = container.Id;

		// Add pointer observers for interaction
		container.Observe<On<ClayPointerEvent>, Commands, Query<Data<CheckboxState>>, Res<ClayTheme>>((trigger, cmd, stateQuery, theme) =>
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
				var newAlpha = state.Checked ? (byte)state.FillColor.a : (byte)0;
				cmd.Entity(state.FillEntityId).Insert(new CheckboxFillUpdate
				{
					FillColor = state.FillColor,
					BorderColor = state.FillBorderColor,
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

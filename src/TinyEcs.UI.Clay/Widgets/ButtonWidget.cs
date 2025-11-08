using System;
using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Component to track button state.
/// </summary>
public struct ButtonState
{
	public ulong TextEntityId;
	public bool IsPressed;
	public bool Disabled;
}

/// <summary>
/// Event fired when a button is clicked.
/// </summary>
public struct ButtonClicked
{
	// Empty event - just signals the click
}

/// <summary>
/// Extension methods for creating button widgets.
/// </summary>
public static class ButtonWidget
{
	/// <summary>
	/// Creates a button widget with text label using theme colors.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the button to</param>
	/// <param name="theme">Theme resource for styling</param>
	/// <param name="text">Button text label</param>
	/// <param name="width">Button width in pixels (0 = use theme default)</param>
	/// <param name="disabled">Whether the button is disabled</param>
	/// <param name="backgroundColor">Optional background color override</param>
	/// <param name="textColor">Optional text color override</param>
	/// <param name="fontSize">Optional font size override (0 = use theme default)</param>
	/// <returns>The button container entity ID</returns>
	/// <remarks>
	/// Listen to ButtonClicked event to be notified when button is clicked:
	/// app.AddObserver&lt;On&lt;ButtonClicked&gt;&gt;((world, trigger) => { ... });
	/// </remarks>
	public static ulong CreateButton(
		this Commands commands,
		EntityCommands parent,
		ClayTheme theme,
		string text,
		float width = 0f,
		bool disabled = false,
		Clay_Color? backgroundColor = null,
		Clay_Color? textColor = null,
		ushort fontSize = 0)
	{
		var buttonTheme = theme.Button;

		// Use theme defaults if not overridden
		var actualWidth = width > 0 ? width : 200f;
		var actualHeight = buttonTheme.Height;
		var actualFontSize = fontSize > 0 ? fontSize : (ushort)16;

		// Apply theme colors based on state
		var bgColor = backgroundColor ?? (disabled ? buttonTheme.DisabledBackgroundColor : buttonTheme.BackgroundColor);
		var txtColor = textColor ?? (disabled ? buttonTheme.DisabledTextColor : buttonTheme.TextColor);

		// Button container (the colored background)
		var buttonNode = ClayNode.Configure()
			.Size(actualWidth, actualHeight)
			.Padding((ushort)buttonTheme.Padding)
			.AlignCenter()
			.Background(bgColor)
			.CornerRadius(buttonTheme.CornerRadius)
			.Build();

		var button = commands.SpawnClayElement(buttonNode);
		parent.AddChild(button);

		// Text label as a separate child element
		var textNode = ClayNode.Configure()
			.WidthFit()
			.HeightFit()
			.Text(text, actualFontSize, txtColor)
			.Build();

		var textElement = commands.SpawnClayElement(textNode);
		button.AddChild(textElement);

		// Add button state component
		commands.Entity(button.Id).Insert(new ButtonState
		{
			TextEntityId = textElement.Id,
			IsPressed = false,
			Disabled = disabled
		});

		// Capture button ID for use in observer closure
		var buttonId = button.Id;

		// Add pointer observers for interaction
		button.Observe<On<ClayPointerEvent>, Commands, Query<Data<ButtonState>>>((trigger, cmd, stateQuery) =>
		{
			var evt = trigger.Event;

			// Use the button ID (where ButtonState is stored)
			if (!stateQuery.Contains(buttonId))
			{
				return;
			}

			var (_, statePtr) = stateQuery.Get(buttonId);
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
				cmd.Entity(buttonId).Insert(state);
			}
			else if (evt.EventType == ClayPointerEventType.Released && state.IsPressed)
			{
				state.IsPressed = false;
				cmd.Entity(buttonId).Insert(state);

				// Emit click event
				cmd.Entity(buttonId).EmitTrigger(new ButtonClicked());
			}
		});

		return button.Id;
	}
}

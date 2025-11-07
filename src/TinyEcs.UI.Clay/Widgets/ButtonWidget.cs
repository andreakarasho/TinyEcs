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
	/// Creates a button widget with text label.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the button to</param>
	/// <param name="text">Button text label</param>
	/// <param name="width">Button width in pixels</param>
	/// <param name="height">Button height in pixels</param>
	/// <param name="backgroundColor">Button background color</param>
	/// <param name="textColor">Button text color</param>
	/// <param name="fontSize">Font size for the text</param>
	/// <param name="disabled">Whether the button is disabled</param>
	/// <param name="cornerRadius">Corner radius for rounded corners</param>
	/// <returns>The button container entity ID</returns>
	/// <remarks>
	/// Listen to ButtonClicked event to be notified when button is clicked:
	/// app.AddObserver&lt;On&lt;ButtonClicked&gt;&gt;((world, trigger) => { ... });
	/// </remarks>
	public static ulong CreateButton(
		this Commands commands,
		EntityCommands parent,
		string text,
		float width = 200f,
		float height = 60f,
		Clay_Color? backgroundColor = null,
		Clay_Color? textColor = null,
		ushort fontSize = 20,
		bool disabled = false,
		ushort cornerRadius = 8)
	{
		// Use default colors if not provided
		var bgColor = backgroundColor ?? new Clay_Color(70, 130, 180, 255);
		var txtColor = textColor ?? new Clay_Color(255, 255, 255, 255);

		if (disabled)
		{
			bgColor = new Clay_Color(60, 60, 60, 255);
			txtColor = new Clay_Color(150, 150, 150, 255);
		}

		// Button container (the colored background)
		var buttonNode = ClayNode.Configure()
			.Size(width, height)
			.Padding(8)
			.AlignCenter()
			.Background(bgColor)
			.CornerRadius(cornerRadius)
			.Build();

		var button = commands.SpawnClayElement(buttonNode);
		parent.AddChild(button);

		// Text label as a separate child element
		var textNode = ClayNode.Configure()
			.WidthFit()
			.HeightFit()
			.Text(text, fontSize, txtColor)
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

using System;
using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Component to track text input state.
/// </summary>
public struct TextInputState
{
	public ulong TextEntityId;
	public string Text;
	public string Placeholder;
	public bool IsFocused;
	public bool IsPressed;
	public bool Disabled;
	public int MaxLength;
}

/// <summary>
/// Marker component to update text input text display.
/// </summary>
public struct TextInputTextUpdate
{
	public string Text;
}

/// <summary>
/// Event fired when text input value changes.
/// </summary>
public struct TextInputValueChanged
{
	public string Text;
}

/// <summary>
/// Event fired when text input gains focus.
/// </summary>
public struct TextInputFocused
{
	// Empty event marker
}

/// <summary>
/// Event fired when text input loses focus.
/// </summary>
public struct TextInputBlurred
{
	// Empty event marker
}

/// <summary>
/// Extension methods for creating text input widgets.
/// </summary>
public static class TextInputWidget
{
	/// <summary>
	/// Creates a text input widget.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the text input to</param>
	/// <param name="placeholder">Placeholder text when empty</param>
	/// <param name="initialText">Initial text value</param>
	/// <param name="width">Input width in pixels</param>
	/// <param name="height">Input height in pixels</param>
	/// <param name="maxLength">Maximum text length (0 for unlimited)</param>
	/// <param name="disabled">Whether the input is disabled</param>
	/// <returns>The text input container entity ID</returns>
	/// <remarks>
	/// Listen to TextInputValueChanged event to be notified when text changes:
	/// app.AddObserver&lt;On&lt;TextInputValueChanged&gt;&gt;((world, trigger) => { ... });
	/// </remarks>
	public static ulong CreateTextInput(
		this Commands commands,
		EntityCommands parent,
		string placeholder = "Enter text...",
		string initialText = "",
		float width = 200f,
		float height = 40f,
		int maxLength = 0,
		bool disabled = false)
	{
		var bgColor = disabled
			? new Clay_Color(30, 30, 30, 255)
			: new Clay_Color(40, 40, 40, 255);

		var textColor = disabled
			? new Clay_Color(100, 100, 100, 255)
			: new Clay_Color(220, 220, 220, 255);

		// Input container
		var inputNode = ClayNode.Configure()
			.Size(width, height)
			.Padding(8)
			.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
			.Background(bgColor)
			.Border(new Clay_Color(80, 80, 90, 255), 1)
			.CornerRadius(4)
			.Build();

		var input = commands.SpawnClayElement(inputNode);
		parent.AddChild(input);

		// Text display
		var displayText = string.IsNullOrEmpty(initialText) ? placeholder : initialText;
		var displayColor = string.IsNullOrEmpty(initialText)
			? new Clay_Color(120, 120, 130, 255) // Placeholder color
			: textColor;

		var textNode = ClayNode.Configure()
			.WidthFit(0, 0)
			.HeightFit(0, 0)
			.Text(displayText, 16, displayColor)
			.Build();

		var textElement = commands.SpawnClayElement(textNode);
		input.AddChild(textElement);

		// Add text input state component
		commands.Entity(input.Id).Insert(new TextInputState
		{
			TextEntityId = textElement.Id,
			Text = initialText,
			Placeholder = placeholder,
			IsFocused = false,
			IsPressed = false,
			Disabled = disabled,
			MaxLength = maxLength
		});

		var inputId = input.Id;

		// Add pointer observers for interaction
		input.Observe<On<ClayPointerEvent>, Commands, Query<Data<TextInputState>>>((trigger, cmd, stateQuery) =>
		{
			var evt = trigger.Event;

			if (!stateQuery.Contains(inputId))
			{
				return;
			}

			var (_, statePtr) = stateQuery.Get(inputId);
			var state = statePtr.Ref;

			if (state.Disabled)
			{
				return;
			}

			trigger.Propagate(false);

			if (evt.EventType == ClayPointerEventType.Pressed)
			{
				state.IsPressed = true;
				cmd.Entity(inputId).Insert(state);
			}
			else if (evt.EventType == ClayPointerEventType.Released && state.IsPressed)
			{
				state.IsPressed = false;
				state.IsFocused = true;
				cmd.Entity(inputId).Insert(state);

				// Update border to show focus
				cmd.Entity(inputId).EmitTrigger(new TextInputFocused());
			}
		});

		return input.Id;
	}
}

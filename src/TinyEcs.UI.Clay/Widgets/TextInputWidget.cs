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
	public Clay_Color BorderColor;       // Theme border color when not focused
	public Clay_Color FocusBorderColor;  // Theme border color when focused
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
	/// Creates a text input widget using theme colors.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the text input to</param>
	/// <param name="theme">Theme resource for styling</param>
	/// <param name="placeholder">Placeholder text when empty</param>
	/// <param name="initialText">Initial text value</param>
	/// <param name="width">Input width in pixels (0 = use default 200)</param>
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
		ClayTheme theme,
		string placeholder = "Enter text...",
		string initialText = "",
		float width = 0f,
		int maxLength = 0,
		bool disabled = false)
	{
		var textInputTheme = theme.TextInput;
		var actualWidth = width > 0 ? width : 200f;
		var height = textInputTheme.Height;

		var bgColor = disabled ? textInputTheme.DisabledBackgroundColor : textInputTheme.BackgroundColor;
		var textColor = textInputTheme.TextColor;

		// Input container
		var inputNode = ClayNode.Configure()
			.Size(actualWidth, height)
			.Padding((ushort)textInputTheme.Padding)
			.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
			.Background(bgColor)
			.Border(textInputTheme.BorderColor, textInputTheme.BorderWidth)
			.CornerRadius(textInputTheme.CornerRadius)
			.Build();

		var input = commands.SpawnClayElement(inputNode);
		parent.AddChild(input);

		// Text display
		var displayText = string.IsNullOrEmpty(initialText) ? placeholder : initialText;
		var displayColor = string.IsNullOrEmpty(initialText)
			? textInputTheme.PlaceholderColor
			: textColor;

		var textNode = ClayNode.Configure()
			.WidthFit(0, 0)
			.HeightFit(0, 0)
			.Text(displayText, theme.Typography.DefaultFontSize, displayColor)
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
			MaxLength = maxLength,
			BorderColor = textInputTheme.BorderColor,
			FocusBorderColor = textInputTheme.FocusBorderColor
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

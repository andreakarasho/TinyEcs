using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Component that represents a text input field.
/// Stores the current text value and cursor position.
/// Note: This is a basic implementation without keyboard input handling.
/// Full keyboard support requires a KeyboardInputPlugin.
/// Child elements are identified by marker components:
/// - TextInputText: the text display element
/// - TextInputPlaceholder: the placeholder text element
/// - TextInputCursor: the cursor/caret element
/// </summary>
public struct TextInput
{
	/// <summary>Current text value</summary>
	public string Text;

	/// <summary>Placeholder text shown when empty</summary>
	public string Placeholder;

	/// <summary>Maximum character length (0 = unlimited)</summary>
	public int MaxLength;

	/// <summary>Whether the input is currently focused</summary>
	public bool IsFocused;

	public TextInput(string placeholder = "", int maxLength = 0)
	{
		Text = string.Empty;
		Placeholder = placeholder;
		MaxLength = maxLength;
		IsFocused = false;
	}
}

/// <summary>
/// Marker component for the text display element inside a text input.
/// </summary>
public struct TextInputText { }

/// <summary>
/// Marker component for the placeholder text element inside a text input.
/// </summary>
public struct TextInputPlaceholder { }

/// <summary>
/// Marker component for the cursor/caret element inside a text input.
/// </summary>
public struct TextInputCursor { }

/// <summary>
/// Event triggered when text input value changes.
/// Use with On&lt;TextInputChanged&gt; in observers.
/// </summary>
public readonly struct TextInputChanged
{
	public readonly string Text;

	public TextInputChanged(string text)
	{
		Text = text;
	}
}

/// <summary>
/// Event triggered when text input gains focus.
/// Use with On&lt;TextInputFocused&gt; in observers.
/// </summary>
public readonly struct TextInputFocused
{
	// Empty event struct
}

/// <summary>
/// Event triggered when text input loses focus.
/// Use with On&lt;TextInputBlurred&gt; in observers.
/// </summary>
public readonly struct TextInputBlurred
{
	// Empty event struct
}

/// <summary>
/// Plugin that adds text input widget functionality.
/// Handles focus management and visual state updates.
///
/// Note: This is a basic implementation. Keyboard input handling
/// would require integration with a KeyboardInputPlugin.
///
/// Usage:
/// <code>
/// app.AddPlugin(new TextInputPlugin());
/// </code>
/// </summary>
public struct TextInputPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// System to handle focus changes on press
		app.AddSystem((
			Commands commands,
			Query<Data<TextInput, InteractionState>, Filter<Changed<InteractionState>>> textInputs,
			Query<Data<TextInput>> allTextInputs) =>
		{
			UpdateFocus(commands, textInputs, allTextInputs);
		})
		.InStage(Stage.PreUpdate)
		.Label("textinput:update-focus")
		.After("interaction:add-to-interactive")
		.Build();

		// System to update visual state when input changes
		app.AddSystem((
			Commands commands,
			Query<Data<TextInput>, Filter<Changed<TextInput>>> changedInputs,
			Query<Data<Parent, UiText>, Filter<With<TextInputText>>> textElements,
			Query<Data<Parent, UiNode>, Filter<With<TextInputPlaceholder>>> placeholders,
			Query<Data<Parent, UiNode>, Filter<With<TextInputCursor>>> cursors) =>
		{
			UpdateTextInputVisuals(commands, changedInputs, textElements, placeholders, cursors);
		})
		.InStage(Stage.PreUpdate)
		.Label("textinput:update-visuals")
		.After("textinput:update-focus")
		.Build();
	}

	/// <summary>
	/// Updates focus state when pressing on text inputs.
	/// Unfocuses other text inputs when one gains focus.
	/// </summary>
	private static void UpdateFocus(
		Commands commands,
		Query<Data<TextInput, InteractionState>, Filter<Changed<InteractionState>>> textInputs,
		Query<Data<TextInput>> allTextInputs)
	{
		foreach (var (entityId, textInput, interaction) in textInputs)
		{
			ref var input = ref textInput.Ref;
			ref readonly var state = ref interaction.Ref;

			// Gain focus on press
			if (state.State == Interaction.Pressed && !input.IsFocused)
			{
				// Unfocus all other text inputs
				foreach (var (otherEntityId, otherInput) in allTextInputs)
				{
					if (otherEntityId.Ref != entityId.Ref && otherInput.Ref.IsFocused)
					{
						ref var other = ref otherInput.Ref;
						other.IsFocused = false;
						commands.Entity(otherEntityId.Ref).Insert(other);
						commands.Entity(otherEntityId.Ref).EmitTrigger(new TextInputBlurred());
					}
				}

				// Set focus
				input.IsFocused = true;
				commands.Entity(entityId.Ref).Insert(input);
				commands.Entity(entityId.Ref).EmitTrigger(new TextInputFocused());
			}
		}
	}

	/// <summary>
	/// Updates the visual state of text inputs (text, placeholder, cursor visibility).
	/// Finds child elements by looking for entities with marker components.
	/// </summary>
	private static void UpdateTextInputVisuals(
		Commands commands,
		Query<Data<TextInput>, Filter<Changed<TextInput>>> changedInputs,
		Query<Data<Parent, UiText>, Filter<With<TextInputText>>> textElements,
		Query<Data<Parent, UiNode>, Filter<With<TextInputPlaceholder>>> placeholders,
		Query<Data<Parent, UiNode>, Filter<With<TextInputCursor>>> cursors)
	{
		foreach (var (inputEntityId, textInput) in changedInputs)
		{
			ref readonly var input = ref textInput.Ref;
			var inputId = inputEntityId.Ref;

			// Find and update text display
			foreach (var (textEntityId, parent, textElement) in textElements)
			{
				if (parent.Ref.Id != inputId)
					continue;

				ref var text = ref textElement.Ref;
				text.Value = input.Text;
				commands.Entity(textEntityId.Ref).Insert(text);
				break;
			}

			// Find and update placeholder visibility
			foreach (var (placeholderEntityId, parent, placeholderNode) in placeholders)
			{
				if (parent.Ref.Id != inputId)
					continue;

				ref var node = ref placeholderNode.Ref;

				// Show placeholder only when text is empty
				node.Display = string.IsNullOrEmpty(input.Text)
					? Flexbox.Display.Flex
					: Flexbox.Display.None;

				commands.Entity(placeholderEntityId.Ref).Insert(node);
				break;
			}

			// Find and update cursor visibility
			foreach (var (cursorEntityId, parent, cursorNode) in cursors)
			{
				if (parent.Ref.Id != inputId)
					continue;

				ref var node = ref cursorNode.Ref;

				// Show cursor only when focused
				node.Display = input.IsFocused
					? Flexbox.Display.Flex
					: Flexbox.Display.None;

				commands.Entity(cursorEntityId.Ref).Insert(node);
				break;
			}
		}
	}
}

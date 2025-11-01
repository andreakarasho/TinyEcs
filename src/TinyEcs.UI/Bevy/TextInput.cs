using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Component that represents a text input field.
/// Stores the current text value and cursor position.
/// Note: This is a basic implementation without keyboard input handling.
/// Full keyboard support requires a KeyboardInputPlugin.
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

	/// <summary>Entity ID of the text display element</summary>
	public ulong TextEntity;

	/// <summary>Entity ID of the placeholder text element</summary>
	public ulong PlaceholderEntity;

	/// <summary>Entity ID of the cursor/caret element</summary>
	public ulong CursorEntity;

	public TextInput(string placeholder = "", int maxLength = 0)
	{
		Text = string.Empty;
		Placeholder = placeholder;
		MaxLength = maxLength;
		IsFocused = false;
		TextEntity = 0;
		PlaceholderEntity = 0;
		CursorEntity = 0;
	}
}

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
		// System to handle focus changes on click
		app.AddSystem((
			Commands commands,
			Query<Data<TextInput, FluxInteraction>, Filter<Changed<FluxInteraction>>> textInputs,
			Query<Data<TextInput>> allTextInputs) =>
		{
			UpdateFocus(commands, textInputs, allTextInputs);
		})
		.InStage(Stage.PreUpdate)
		.Label("textinput:update-focus")
		.After("flux:update-interaction")
		.Build();

		// System to update visual state when input changes
		app.AddSystem((
			Commands commands,
			Query<Data<TextInput>, Filter<Changed<TextInput>>> changedInputs,
			Query<Data<UiText>> textElements,
			Query<Data<UiNode>> allNodes) =>
		{
			UpdateTextInputVisuals(commands, changedInputs, textElements, allNodes);
		})
		.InStage(Stage.PreUpdate)
		.Label("textinput:update-visuals")
		.After("textinput:update-focus")
		.Build();
	}

	/// <summary>
	/// Updates focus state when clicking on text inputs.
	/// Unfocuses other text inputs when one gains focus.
	/// </summary>
	private static void UpdateFocus(
		Commands commands,
		Query<Data<TextInput, FluxInteraction>, Filter<Changed<FluxInteraction>>> textInputs,
		Query<Data<TextInput>> allTextInputs)
	{
		foreach (var (entityId, textInput, flux) in textInputs)
		{
			ref var input = ref textInput.Ref;
			ref readonly var interaction = ref flux.Ref;

			// Gain focus on press
			if (interaction.State == FluxInteractionState.Pressed && !input.IsFocused)
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
	/// </summary>
	private static void UpdateTextInputVisuals(
		Commands commands,
		Query<Data<TextInput>, Filter<Changed<TextInput>>> changedInputs,
		Query<Data<UiText>> textElements,
		Query<Data<UiNode>> allNodes)
	{
		foreach (var (entityId, textInput) in changedInputs)
		{
			ref readonly var input = ref textInput.Ref;

			// Update text display
			if (input.TextEntity != 0 && textElements.Contains(input.TextEntity))
			{
				var (_, textElement) = textElements.Get(input.TextEntity);
				ref var text = ref textElement.Ref;
				text.Value = input.Text;
				commands.Entity(input.TextEntity).Insert(text);
			}

			// Update placeholder visibility
			if (input.PlaceholderEntity != 0 && allNodes.Contains(input.PlaceholderEntity))
			{
				var (_, placeholderNode) = allNodes.Get(input.PlaceholderEntity);
				ref var node = ref placeholderNode.Ref;

				// Show placeholder only when text is empty
				node.Display = string.IsNullOrEmpty(input.Text)
					? Flexbox.Display.Flex
					: Flexbox.Display.None;

				commands.Entity(input.PlaceholderEntity).Insert(node);
			}

			// Update cursor visibility
			if (input.CursorEntity != 0 && allNodes.Contains(input.CursorEntity))
			{
				var (_, cursorNode) = allNodes.Get(input.CursorEntity);
				ref var node = ref cursorNode.Ref;

				// Show cursor only when focused
				node.Display = input.IsFocused
					? Flexbox.Display.Flex
					: Flexbox.Display.None;

				commands.Entity(input.CursorEntity).Insert(node);
			}
		}
	}
}

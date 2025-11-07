using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Plugin that adds text input widget systems to the application.
/// </summary>
public struct TextInputPlugin : IPlugin
{
	public void Build(App app)
	{
		// Observer to enforce single-focus behavior
		// When a text input gains focus, blur all other text inputs
		app.AddObserver<On<TextInputFocused>, Commands, Query<Data<TextInputState>>>((trigger, commands, inputs) =>
		{
			var focusedEntityId = trigger.EntityId;

			// Blur all other text inputs
			foreach (var (entityId, statePtr) in inputs)
			{
				if (entityId.Ref != focusedEntityId)
				{
					var state = statePtr.Ref;
					if (state.IsFocused && !state.Disabled)
					{
						state.IsFocused = false;
						commands.Entity(entityId.Ref).Insert(state);
						commands.Entity(entityId.Ref).EmitTrigger(new TextInputBlurred());
					}
				}
			}
		});

		// System to process text input from ClayTextInputState resource
		app.AddSystem((Commands commands, Res<ClayTextInputState> textInputState, Query<Data<TextInputState>> inputs) =>
		{
			foreach (var (entityId, statePtr) in inputs)
			{
				var state = statePtr.Ref;

				// Only process input for focused text inputs
				if (!state.IsFocused || state.Disabled)
				{
					continue;
				}

				bool textChanged = false;
				var newText = state.Text;

				// Process typed characters
				foreach (var c in textInputState.Value.GetChars())
				{
					// Filter out control characters
					if (char.IsControl(c))
					{
						continue;
					}

					// Check max length
					if (state.MaxLength > 0 && newText.Length >= state.MaxLength)
					{
						continue;
					}

					newText += c;
					textChanged = true;
				}

				// Handle backspace
				if (textInputState.Value.BackspacePressed && newText.Length > 0)
				{
					newText = newText.Substring(0, newText.Length - 1);
					textChanged = true;
				}

				// Handle escape (blur the input)
				if (textInputState.Value.EscapePressed)
				{
					state.IsFocused = false;
					commands.Entity(entityId.Ref).Insert(state);
					commands.Entity(entityId.Ref).EmitTrigger(new TextInputBlurred());
					continue;
				}

				// Handle enter (blur and emit event)
				if (textInputState.Value.EnterPressed)
				{
					state.IsFocused = false;
					commands.Entity(entityId.Ref).Insert(state);
					commands.Entity(entityId.Ref).EmitTrigger(new TextInputBlurred());
					continue;
				}

				// Update text if changed
				if (textChanged)
				{
					state.Text = newText;
					commands.Entity(entityId.Ref).Insert(state);

					// Queue visual update
					commands.Entity(state.TextEntityId).Insert(new TextInputTextUpdate
					{
						Text = string.IsNullOrEmpty(newText) ? state.Placeholder : newText
					});

					// Emit value changed event
					commands.Entity(entityId.Ref).EmitTrigger(new TextInputValueChanged
					{
						Text = newText
					});
				}
			}
		})
		.InStage(Stage.Update)
		.Label("textinput:process-input")
		.Build();

		// System to update text input text display
		app.AddSystem((Commands commands, Query<Data<TextInputTextUpdate, ClayNode>> updates) =>
		{
			foreach (var (entityId, updatePtr, nodePtr) in updates)
			{
				var update = updatePtr.Ref;
				ref var node = ref nodePtr.Ref;

				if (node.Text.HasValue)
				{
					var text = node.Text.Value;
					text.Text = update.Text;
					node.Text = text;

					// Re-insert to trigger change detection
					commands.Entity(entityId.Ref).Insert(node);
				}
			}
		})
		.InStage(Stage.First)
		.Label("textinput:update-text")
		.Build();

		// System to update text color based on focus state
		app.AddSystem((Commands commands, Query<Data<TextInputState, ClayNode>> inputs) =>
		{
			foreach (var (entityId, statePtr, nodePtr) in inputs)
			{
				var state = statePtr.Ref;
				ref var node = ref nodePtr.Ref;

				// Update border color based on focus
				if (node.Border.HasValue)
				{
					var border = node.Border.Value;
					border.color = state.IsFocused
						? new Clay_Color(120, 170, 255, 255) // Blue when focused
						: new Clay_Color(80, 80, 90, 255);     // Gray when not focused

					node.Border = border;
					commands.Entity(entityId.Ref).Insert(node);
				}
			}
		})
		.InStage(Stage.Update)
		.Label("textinput:update-visual-state")
		.Build();
	}
}

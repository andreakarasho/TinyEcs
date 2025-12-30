using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Component that represents a checkbox widget with checked state.
/// The checkbox toggles its state when pressed (Interaction.Pressed).
/// Ported from sickle_ui.
/// </summary>
public struct Checkbox
{
	/// <summary>Whether the checkbox is currently checked</summary>
	public bool Checked;

	public Checkbox(bool initialValue = false)
	{
		Checked = initialValue;
	}
}

/// <summary>
/// Marker component for the checkmark visual element inside a checkbox.
/// Used by CheckboxPlugin to find and update the checkmark's visibility.
/// </summary>
public struct Checkmark { }

/// <summary>
/// Event triggered when a checkbox state changes.
/// Use with On&lt;CheckboxChanged&gt; in observers.
/// </summary>
public readonly struct CheckboxChanged
{
	public readonly bool Checked;

	public CheckboxChanged(bool isChecked)
	{
		Checked = isChecked;
	}
}

/// <summary>
/// Plugin that adds checkbox widget functionality.
/// Handles toggling checkbox state on click and updating visual state.
///
/// Usage:
/// <code>
/// app.AddPlugin(new CheckboxPlugin());
/// </code>
/// </summary>
public struct CheckboxPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// System to toggle checkbox when pressed
		app.AddSystem((
			Commands commands,
			Query<Data<Checkbox, InteractionState>, Filter<Changed<InteractionState>>> checkboxes) =>
		{
			ToggleCheckbox(commands, checkboxes);
		})
		.InStage(Stage.PreUpdate)
		.Label("checkbox:toggle")
		.After("interaction:add-to-interactive")
		.Build();

		// System to update checkmark visibility when checkbox state changes
		app.AddSystem((
			Commands commands,
			Query<Data<Checkbox>, Filter<Changed<Checkbox>>> changedCheckboxes,
			Query<Data<Parent, UiNode>, Filter<With<Checkmark>>> checkmarks) =>
		{
			UpdateCheckboxVisuals(commands, changedCheckboxes, checkmarks);
		})
		.InStage(Stage.PreUpdate)
		.Label("checkbox:update-visuals")
		.After("checkbox:toggle")
		.Build();
	}

	/// <summary>
	/// Toggles checkbox state when Interaction.Pressed is detected.
	/// </summary>
	private static void ToggleCheckbox(
		Commands commands,
		Query<Data<Checkbox, InteractionState>, Filter<Changed<InteractionState>>> checkboxes)
	{
		foreach (var (entityId, checkbox, interaction) in checkboxes)
		{
			ref var cb = ref checkbox.Ref;
			ref readonly var state = ref interaction.Ref;

			// Toggle on press
			if (state.State == Interaction.Pressed)
			{
				cb.Checked = !cb.Checked;

				// Re-insert to trigger change detection
				commands.Entity(entityId.Ref).Insert(cb);

				// Emit CheckboxChanged event on the entity
				var changeEvent = new CheckboxChanged(cb.Checked);
				commands.Entity(entityId.Ref).EmitTrigger(changeEvent);
			}
		}
	}

	/// <summary>
	/// Updates the checkmark visual visibility based on checkbox state.
	/// Shows checkmark when checked, hides when unchecked.
	/// Finds checkmark by looking for child entities with Checkmark marker.
	/// </summary>
	private static void UpdateCheckboxVisuals(
		Commands commands,
		Query<Data<Checkbox>, Filter<Changed<Checkbox>>> changedCheckboxes,
		Query<Data<Parent, UiNode>, Filter<With<Checkmark>>> checkmarks)
	{
		foreach (var (entityId, checkbox) in changedCheckboxes)
		{
			ref readonly var cb = ref checkbox.Ref;
			var checkboxId = entityId.Ref;

			// Find the checkmark entity that is a child of this checkbox
			foreach (var (checkmarkId, parent, node) in checkmarks)
			{
				if (parent.Ref.Id != checkboxId)
					continue;

				// Found the checkmark for this checkbox
				ref var n = ref node.Ref;
				n.Display = cb.Checked ? Flexbox.Display.Flex : Flexbox.Display.None;

				// Re-insert to trigger change detection and layout update
				commands.Entity(checkmarkId.Ref).Insert(n);
				break;
			}
		}
	}
}

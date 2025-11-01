using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Component that represents a checkbox widget with checked state.
/// The checkbox toggles its state when clicked (FluxInteraction.Released).
/// Ported from sickle_ui.
/// </summary>
public struct Checkbox
{
	/// <summary>Whether the checkbox is currently checked</summary>
	public bool Checked;

	/// <summary>Entity ID of the checkmark visual element</summary>
	public ulong CheckmarkEntity;

	public Checkbox(bool initialValue = false)
	{
		Checked = initialValue;
		CheckmarkEntity = 0;
	}
}

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
		// System to toggle checkbox when clicked
		app.AddSystem((
			Commands commands,
			Query<Data<Checkbox, FluxInteraction>, Filter<Changed<FluxInteraction>>> checkboxes) =>
		{
			ToggleCheckbox(commands, checkboxes);
		})
		.InStage(Stage.PreUpdate)
		.Label("checkbox:toggle")
		.After("flux:update-interaction")
		.Build();

		// System to update checkmark visibility when checkbox state changes
		app.AddSystem((
			Commands commands,
			Query<Data<Checkbox>, Filter<Changed<Checkbox>>> changedCheckboxes,
			Query<Data<UiNode>> allNodes) =>
		{
			UpdateCheckboxVisuals(commands, changedCheckboxes, allNodes);
		})
		.InStage(Stage.PreUpdate)
		.Label("checkbox:update-visuals")
		.After("checkbox:toggle")
		.Build();
	}

	/// <summary>
	/// Toggles checkbox state when FluxInteraction.Released is detected.
	/// </summary>
	private static void ToggleCheckbox(
		Commands commands,
		Query<Data<Checkbox, FluxInteraction>, Filter<Changed<FluxInteraction>>> checkboxes)
	{
		foreach (var (entityId, checkbox, flux) in checkboxes)
		{
			ref var cb = ref checkbox.Ref;
			ref readonly var interaction = ref flux.Ref;

			// Toggle on release (click)
			if (interaction.State == FluxInteractionState.Released)
			{
				cb.Checked = !cb.Checked;

				// Re-insert to trigger change detection
				commands.Entity(entityId.Ref).Insert(cb);

				// Emit CheckboxChanged event
				commands.Entity(entityId.Ref).EmitTrigger(new CheckboxChanged(cb.Checked));
			}
		}
	}

	/// <summary>
	/// Updates the checkmark visual visibility based on checkbox state.
	/// Shows checkmark when checked, hides when unchecked.
	/// </summary>
	private static void UpdateCheckboxVisuals(
		Commands commands,
		Query<Data<Checkbox>, Filter<Changed<Checkbox>>> changedCheckboxes,
		Query<Data<UiNode>> allNodes)
	{
		foreach (var (entityId, checkbox) in changedCheckboxes)
		{
			ref readonly var cb = ref checkbox.Ref;

			if (cb.CheckmarkEntity != 0 && allNodes.Contains(cb.CheckmarkEntity))
			{
				// Read existing node, update only Display property
				var (_, existingNode) = allNodes.Get(cb.CheckmarkEntity);
				ref var node = ref existingNode.Ref;

				// Update display property
				node.Display = cb.Checked ? Flexbox.Display.Flex : Flexbox.Display.None;

				// Re-insert to trigger change detection and layout update
				commands.Entity(cb.CheckmarkEntity).Insert(node);
			}
		}
	}
}

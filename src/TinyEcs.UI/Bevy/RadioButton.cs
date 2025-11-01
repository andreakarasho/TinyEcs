using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Component that represents a radio button group.
/// All radio buttons with the same GroupId are mutually exclusive.
/// </summary>
public struct RadioGroup
{
	/// <summary>Unique identifier for this radio button group</summary>
	public int GroupId;

	public RadioGroup(int groupId)
	{
		GroupId = groupId;
	}
}

/// <summary>
/// Component that represents a radio button widget.
/// Radio buttons within the same RadioGroup are mutually exclusive.
/// Only one radio button in a group can be selected at a time.
/// </summary>
public struct RadioButton
{
	/// <summary>Whether this radio button is currently selected</summary>
	public bool Selected;

	/// <summary>Value associated with this radio button</summary>
	public int Value;

	/// <summary>Entity ID of the indicator visual element</summary>
	public ulong IndicatorEntity;

	public RadioButton(int value, bool initiallySelected = false)
	{
		Selected = initiallySelected;
		Value = value;
		IndicatorEntity = 0;
	}
}

/// <summary>
/// Event triggered when a radio button is selected.
/// Use with On&lt;RadioButtonSelected&gt; in observers.
/// </summary>
public readonly struct RadioButtonSelected
{
	public readonly int Value;
	public readonly int GroupId;

	public RadioButtonSelected(int value, int groupId)
	{
		Value = value;
		GroupId = groupId;
	}
}

/// <summary>
/// Plugin that adds radio button widget functionality.
/// Handles mutual exclusion within radio button groups and visual state updates.
///
/// Usage:
/// <code>
/// app.AddPlugin(new RadioButtonPlugin());
/// </code>
/// </summary>
public struct RadioButtonPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// System to handle radio button selection
		app.AddSystem((
			Commands commands,
			Query<Data<RadioButton, RadioGroup, FluxInteraction>, Filter<Changed<FluxInteraction>>> radioButtons,
			Query<Data<RadioButton, RadioGroup>> allRadioButtons) =>
		{
			HandleRadioButtonClick(commands, radioButtons, allRadioButtons);
		})
		.InStage(Stage.PreUpdate)
		.Label("radio:handle-click")
		.After("flux:update-interaction")
		.Build();

		// System to update visual state when radio button selection changes
		app.AddSystem((
			Commands commands,
			Query<Data<RadioButton>, Filter<Changed<RadioButton>>> changedRadioButtons,
			Query<Data<UiNode>> allNodes) =>
		{
			UpdateRadioButtonVisuals(commands, changedRadioButtons, allNodes);
		})
		.InStage(Stage.PreUpdate)
		.Label("radio:update-visuals")
		.After("radio:handle-click")
		.Build();
	}

	/// <summary>
	/// Handles radio button clicks and enforces mutual exclusion within groups.
	/// </summary>
	private static void HandleRadioButtonClick(
		Commands commands,
		Query<Data<RadioButton, RadioGroup, FluxInteraction>, Filter<Changed<FluxInteraction>>> radioButtons,
		Query<Data<RadioButton, RadioGroup>> allRadioButtons)
	{
		foreach (var (entityId, radioButton, radioGroup, flux) in radioButtons)
		{
			ref var rb = ref radioButton.Ref;
			ref readonly var group = ref radioGroup.Ref;
			ref readonly var interaction = ref flux.Ref;

			// Select on release (click)
			if (interaction.State == FluxInteractionState.Released && !rb.Selected)
			{
				// Deselect all other radio buttons in the same group
				foreach (var (otherEntityId, otherRadio, otherGroup) in allRadioButtons)
				{
					if (otherEntityId.Ref != entityId.Ref && otherGroup.Ref.GroupId == group.GroupId)
					{
						ref var other = ref otherRadio.Ref;
						if (other.Selected)
						{
							other.Selected = false;
							commands.Entity(otherEntityId.Ref).Insert(other);
						}
					}
				}

				// Select this radio button
				rb.Selected = true;
				commands.Entity(entityId.Ref).Insert(rb);

			// Emit RadioButtonSelected event both globally and per-entity
			var selectEvent = new RadioButtonSelected(rb.Value, group.GroupId);
			commands.Entity(entityId.Ref).EmitTrigger(selectEvent);  // Per-entity (BevyObservers)
			commands.EmitTrigger(new On<RadioButtonSelected>(entityId.Ref, selectEvent));  // Global (EventChannel)
			}
		}
	}

	/// <summary>
	/// Updates the indicator visibility based on radio button selection state.
	/// Shows indicator when selected, hides when unselected.
	/// </summary>
	private static void UpdateRadioButtonVisuals(
		Commands commands,
		Query<Data<RadioButton>, Filter<Changed<RadioButton>>> changedRadioButtons,
		Query<Data<UiNode>> allNodes)
	{
		foreach (var (entityId, radioButton) in changedRadioButtons)
		{
			ref readonly var rb = ref radioButton.Ref;

			if (rb.IndicatorEntity != 0 && allNodes.Contains(rb.IndicatorEntity))
			{
				// Read existing node, update only Display property
				var (_, existingNode) = allNodes.Get(rb.IndicatorEntity);
				ref var node = ref existingNode.Ref;

				// Update display property
				node.Display = rb.Selected ? Flexbox.Display.Flex : Flexbox.Display.None;

				// Re-insert to trigger change detection and layout update
				commands.Entity(rb.IndicatorEntity).Insert(node);
			}
		}
	}
}

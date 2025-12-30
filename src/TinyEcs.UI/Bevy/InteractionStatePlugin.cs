using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Plugin that updates InteractionState based on UiPointerEvent triggers.
/// This bridges the pointer event system with widget interaction handling.
///
/// InteractionState tracks current state (None, Hovered, Pressed) directly.
/// Widgets react to changes via Changed&lt;InteractionState&gt;.
/// </summary>
public struct InteractionStatePlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// Add observer to update InteractionState based on pointer events
		// This runs whenever a UiPointerTrigger is emitted on an entity
		app.AddObserver<On<UiPointerTrigger>, Commands, Query<Data<InteractionState>>>(
			(trigger, commands, interactionQuery) =>
		{
			var entityId = trigger.EntityId;
			var pointerEvent = trigger.Event.Event;

			// Only process entities that already have InteractionState
			// (InteractionPlugin adds InteractionState to Interactive entities)
			if (!interactionQuery.Contains(entityId))
				return;

			var (_, existingState) = interactionQuery.Get(entityId);
			var state = existingState.Ref;

			// Update state based on pointer event type
			var newState = pointerEvent.Type switch
			{
				UiPointerEventType.PointerEnter =>
					// Only transition to Hovered if not already pressed
					state.State != Interaction.Pressed
						? Interaction.Hovered
						: state.State,

				UiPointerEventType.PointerExit =>
					// Only transition to None if not pressed
					// If pressed and we exit, we stay pressed (drag outside scenario)
					state.State != Interaction.Pressed
						? Interaction.None
						: state.State,

				UiPointerEventType.PointerDown =>
					pointerEvent.IsPrimaryButton
						? Interaction.Pressed
						: state.State,

				UiPointerEventType.PointerUp =>
					// Released over the element = transition to Hovered
					Interaction.Hovered,

				// No state change on move or scroll
				_ => state.State
			};

			// Only insert if state actually changed (triggers change detection)
			if (newState != state.State)
			{
				commands.Entity(entityId).Insert(new InteractionState { State = newState });
			}
		});

		// System to handle pointer release outside of elements
		// When primary button is released and entity is still Pressed,
		// it means the release happened outside - transition to None
		app.AddSystem((
			Res<PointerInputState> pointerInput,
			Commands commands,
			Query<Data<InteractionState>> interactionQuery) =>
		{
			if (pointerInput.Value.IsPrimaryButtonReleased)
			{
				foreach (var (entityId, state) in interactionQuery)
				{
					if (state.Ref.State == Interaction.Pressed)
					{
						// Still pressed after release event = released outside
						// Transition to None
						commands.Entity(entityId.Ref).Insert(new InteractionState { State = Interaction.None });
					}
				}
			}
		})
		.InStage(Stage.PreUpdate)
		.Label("interaction:handle-release-outside")
		.Build();
	}
}

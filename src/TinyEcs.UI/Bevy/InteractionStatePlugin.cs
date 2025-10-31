using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Plugin that updates InteractionState based on UiPointerEvent triggers.
/// This bridges the pointer event system with the FluxInteraction state machine.
///
/// InteractionState is the low-level state (None, Hovered, Pressed) that directly
/// reflects pointer events. FluxInteractionPlugin then converts these into higher-level
/// interaction states (PointerEnter, Released, etc.).
/// </summary>
public struct InteractionStatePlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// Add observer to update InteractionState based on pointer events
		// This runs whenever a UiPointerTrigger is emitted on an entity
		// Uses Commands and Query to properly trigger change detection
		app.AddObserver<On<UiPointerTrigger>, Commands, Query<Data<InteractionState>, Optional<InteractionState>>>(
			(trigger, commands, interactionQuery) =>
		{
			var entityId = trigger.EntityId;
			var pointerEvent = trigger.Event.Event;

			// Get or create InteractionState using Optional
			InteractionState state;
			if (interactionQuery.Contains(entityId))
			{
				var (_, maybeState) = interactionQuery.Get(entityId);
				state = maybeState.IsValid() ? maybeState.Ref : new InteractionState();
			}
			else
			{
				state = new InteractionState();
			}

			// Update state based on pointer event type
			var newState = pointerEvent.Type switch
			{
				UiPointerEventType.PointerEnter =>
					// Only transition to Hovered if not already pressed
					state.State != InteractionStateEnum.Pressed
						? InteractionStateEnum.Hovered
						: state.State,

				UiPointerEventType.PointerExit =>
					// Only transition to None if not pressed
					// If pressed and we exit, we stay pressed (drag outside scenario)
					state.State != InteractionStateEnum.Pressed
						? InteractionStateEnum.None
						: state.State,

				UiPointerEventType.PointerDown =>
					pointerEvent.IsPrimaryButton
						? InteractionStateEnum.Pressed
						: state.State,

				UiPointerEventType.PointerUp =>
					// Released over the element = transition to Hovered
					// (FluxInteraction will detect this as "Released")
					InteractionStateEnum.Hovered,

				// No state change on move or scroll
				_ => state.State
			};

			// Only insert if state actually changed (triggers change detection)
			if (newState != state.State)
			{
				commands.Entity(entityId).Insert(new InteractionState { State = newState });
			}
		});

		// Add a system to detect when pointer is released outside of any element
		// This handles the "press canceled" scenario
		app.AddSystem((
			Res<PointerInputState> pointerInput,
			Query<Data<InteractionState>> interactionQuery) =>
		{
			// If primary button was just released and no entity is hovered,
			// reset all pressed entities to None
			if (pointerInput.Value.IsPrimaryButtonReleased)
			{
				foreach (var (entityId, state) in interactionQuery)
				{
					ref var stateRef = ref state.Ref;

					if (stateRef.State == InteractionStateEnum.Pressed)
					{
						// Check if pointer is still over this element
						// If not, transition to None (canceled)
						// Note: This is a simplified approach - ideally we'd track which
						// entity is currently hovered, but for now we rely on PointerExit
						// events to handle this.

						// For now, we don't automatically cancel here - we let PointerExit
						// handle the transition. If PointerUp fires on an element, it will
						// transition to Hovered. If the pointer exits first, it stays Pressed
						// until release, then FluxInteraction will detect PressCanceled.
					}
				}
			}
		})
		.InStage(Stage.PreUpdate)
		.Label("interaction:update-pressed-state")
		.Before("flux:tick-stopwatch")
		.Build();
	}
}

using System.Numerics;
using Clay_cs;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Clay;

/// <summary>
/// Tracks interaction state between frames for Enter/Exit/Move event detection.
/// </summary>
public class InteractionState
{
	/// <summary>
	/// Element IDs that were under the pointer in the previous frame.
	/// Stores full Clay_ElementId to allow direct entity ID composition.
	/// </summary>
	public HashSet<Clay_ElementId> PreviousHoveredIds = new();

	/// <summary>
	/// Previous pointer position for Move event detection.
	/// </summary>
	public Vector2 PreviousPointerPosition = Vector2.Zero;

	/// <summary>
	/// Tracks which element received a Pressed event for each button.
	/// Used to emit Released events to the same element even if pointer moves outside.
	/// Key: ClayMouseButton flag, Value: Clay_ElementId of the element that was pressed.
	/// </summary>
	public Dictionary<ClayMouseButton, Clay_ElementId> PressedElements = new();
}

/// <summary>
/// Plugin responsible for Clay pointer interaction and event emission.
/// Runs in PostUpdate stage after layout calculation.
/// </summary>
public struct ClayInteractionPlugin : IPlugin
{
	public void Build(App app)
	{
		// System ordering:
		// 1. Process pointer interactions
		// 2. Emit pointer events

		app.AddSystem((
			Query<Data<ClayElementId>, Filter<With<ClayNode>>> nodes,
			Res<ClayPointerState> pointer,
			Commands commands,
			EventWriter<ClayPointerEvent> events,
			Local<InteractionState> state,
			Local<HashSet<Clay_ElementId>> currentHoveredIds,
			Query<Data<ClayInteraction>> interactions
		) => ProcessPointerInteraction(nodes, pointer, commands, events, state, currentHoveredIds, interactions))
			.InStage(Stage.PostUpdate)
			.Label("clay:interaction")
			.Build();
	}

	/// <summary>
	/// Emit a pointer event to the event stream and as a trigger.
	/// </summary>
	private static void EmitEvent(
		ClayPointerEvent evt,
		ulong entityId,
		Commands commands,
		EventWriter<ClayPointerEvent> events)
	{
		// Send to event stream
		events.Send(evt);

		// Emit trigger - EntityCommands.EmitTrigger automatically wraps with On<ClayPointerEvent>
		// and injects the entity ID.
		commands.Entity(entityId).EmitTrigger(evt);
	}

	/// <summary>
	/// Update ClayInteraction component state based on pointer event.
	/// </summary>
	private static void UpdateInteractionState(
		ulong entityId,
		ClayPointerEventType eventType,
		ClayMouseButton button,
		bool isCurrentlyHovered,
		Commands commands,
		Query<Data<ClayInteraction>> interactions)
	{
		// Only update if the entity has ClayInteraction component
		if (!interactions.Contains(entityId))
			return;

		var (_, interaction) = interactions.Get(entityId);
		var state = interaction.Ref;

		switch (eventType)
		{
			case ClayPointerEventType.Enter:
				// Entering element - set to Hovered if not pressed
				if (state.PressedButtons == ClayMouseButton.None)
					state.State = ClayInteractionState.Hovered;
				break;

			case ClayPointerEventType.Exit:
				// Exiting element - set to None if not pressed, stay Pressed if buttons are down
				if (state.PressedButtons == ClayMouseButton.None)
					state.State = ClayInteractionState.None;
				break;

			case ClayPointerEventType.Pressed:
				// Button pressed on element - set to Pressed and track buttons
				state.State = ClayInteractionState.Pressed;
				state.PressedButtons |= button;
				break;

			case ClayPointerEventType.Released:
				// Button released - remove from pressed buttons
				state.PressedButtons &= ~button;
				// Update state based on remaining pressed buttons
				if (state.PressedButtons == ClayMouseButton.None)
				{
					// No more buttons pressed - set to Hovered if pointer is still over element, otherwise None
					state.State = isCurrentlyHovered ? ClayInteractionState.Hovered : ClayInteractionState.None;
				}
				break;
		}

		// Re-insert the modified component to trigger change detection
		if (interaction.Ref.State != state.State || interaction.Ref.PressedButtons != state.PressedButtons)
			commands.Entity(entityId).Insert(state);
	}

	/// <summary>
	/// Process pointer interactions and emit events.
	/// </summary>
	private static unsafe void ProcessPointerInteraction(
		Query<Data<ClayElementId>, Filter<With<ClayNode>>> nodes,
		Res<ClayPointerState> pointer,
		Commands commands,
		EventWriter<ClayPointerEvent> events,
		Local<InteractionState> state,
		Local<HashSet<Clay_ElementId>> currentHoveredIds,
		Query<Data<ClayInteraction>> interactions)
	{
		// Get elements under pointer
		var pointerOverIds = Clay_cs.Clay.GetPointerOverIds();
		if (pointerOverIds.IsEmpty)
			return;

		// Build current frame's hovered element IDs set (reuse Local to avoid allocations)
		currentHoveredIds.Value!.Clear();
		foreach (ref readonly var id in pointerOverIds)
		{
			currentHoveredIds.Value!.Add(id);
		}

		// Track if pointer position changed (Local<T> is never null, compiler doesn't know this)
		var pointerMoved = state.Value!.PreviousPointerPosition != pointer.Value.Position;

		var found = false;

		for (var i = pointerOverIds.Length - 1; i >= 0 && !found; i--)
		{
			var id = pointerOverIds[i];
			var entId = IDOp.Compose(id.id, id.offset);

			if (!nodes.Contains(entId))
			{
				continue;
			}

			var (entity, nodeId) = nodes.Get(entId);
			ref readonly var entityId = ref entity.Ref;
			var elementId = nodeId.Ref.Id;
			var isCurrentlyHovered = currentHoveredIds.Value!.Contains(elementId);
			var wasPreviouslyHovered = state.Value!.PreviousHoveredIds.Contains(elementId);

			// Detect Enter event (element is now hovered but wasn't before)
			if (isCurrentlyHovered && !wasPreviouslyHovered)
			{
				var elementData = Clay_cs.Clay.GetElementData(elementId);
				if (elementData.found)
				{
					var localPos = new Vector2(
						pointer.Value.Position.X - elementData.boundingBox.x,
						pointer.Value.Position.Y - elementData.boundingBox.y
					);

					var enterEvent = new ClayPointerEvent
					{
						EventType = ClayPointerEventType.Enter,
						Position = pointer.Value.Position,
						LocalPosition = localPos,
						Button = pointer.Value.ButtonsDown,
						ScrollDelta = Vector2.Zero,
					};

					EmitEvent(enterEvent, entityId, commands, events);
					UpdateInteractionState(entityId, ClayPointerEventType.Enter, ClayMouseButton.None, true, commands, interactions);
				}
			}

			// Detect Move event (element is hovered and pointer moved)
			if (isCurrentlyHovered && wasPreviouslyHovered && pointerMoved)
			{
				var elementData = Clay_cs.Clay.GetElementData(elementId);
				if (elementData.found)
				{
					var localPos = new Vector2(
						pointer.Value.Position.X - elementData.boundingBox.x,
						pointer.Value.Position.Y - elementData.boundingBox.y
					);

					var moveEvent = new ClayPointerEvent
					{
						EventType = ClayPointerEventType.Move,
						Position = pointer.Value.Position,
						LocalPosition = localPos,
						Button = pointer.Value.ButtonsDown,
						ScrollDelta = Vector2.Zero,
					};

					EmitEvent(moveEvent, entityId, commands, events);
				}
			}

			// Only process button/scroll events for currently hovered elements
			if (!isCurrentlyHovered)
			{
				continue;
			}

			// Get element data for local position calculation
			var elementDataForEvents = Clay_cs.Clay.GetElementData(elementId);

			if (!elementDataForEvents.found)
			{
				// Element data not found
				continue;
			}

			var localPosForEvents = new Vector2(
				pointer.Value.Position.X - elementDataForEvents.boundingBox.x,
				pointer.Value.Position.Y - elementDataForEvents.boundingBox.y
			);

			// Emit pointer events for pressed buttons (these bubble by default like in web)
			if (pointer.Value.ButtonsPressed != ClayMouseButton.None)
			{
				var downEvent = new ClayPointerEvent
				{
					EventType = ClayPointerEventType.Pressed,
					Position = pointer.Value.Position,
					LocalPosition = localPosForEvents,
					Button = pointer.Value.ButtonsPressed,
					ScrollDelta = Vector2.Zero,
				};

				EmitEvent(downEvent, entityId, commands, events);
				UpdateInteractionState(entityId, ClayPointerEventType.Pressed, pointer.Value.ButtonsPressed, true, commands, interactions);

				// Track each pressed button separately for this element
				foreach (ClayMouseButton button in Enum.GetValues<ClayMouseButton>())
				{
					if (button != ClayMouseButton.None && pointer.Value.ButtonsPressed.HasFlag(button))
					{
						state.Value!.PressedElements[button] = elementId;
					}
				}
				found = true;
			}

			// Emit pointer events for released buttons
			if (pointer.Value.ButtonsReleased != ClayMouseButton.None)
			{
				// Check which buttons were actually pressed on THIS element
				var buttonsReleasedOnThisElement = ClayMouseButton.None;
				foreach (ClayMouseButton button in Enum.GetValues<ClayMouseButton>())
				{
					if (button != ClayMouseButton.None && pointer.Value.ButtonsReleased.HasFlag(button))
					{
						// Only emit Released/Click if this element is where the button was originally pressed
						if (state.Value!.PressedElements.TryGetValue(button, out var pressedElementId) &&
							pressedElementId.id == elementId.id && pressedElementId.offset == elementId.offset)
						{
							buttonsReleasedOnThisElement |= button;
							state.Value!.PressedElements.Remove(button);
						}
					}
				}

				// Only emit Released/Click events if at least one button was pressed on this element
				if (buttonsReleasedOnThisElement != ClayMouseButton.None)
				{
					var upEvent = new ClayPointerEvent
					{
						EventType = ClayPointerEventType.Released,
						Position = pointer.Value.Position,
						LocalPosition = localPosForEvents,
						Button = buttonsReleasedOnThisElement,
						ScrollDelta = Vector2.Zero,
					};

					EmitEvent(upEvent, entityId, commands, events);
					UpdateInteractionState(entityId, ClayPointerEventType.Released, buttonsReleasedOnThisElement, true, commands, interactions);

					// Also emit click event if released over same element where it was pressed
					var clickEvent = new ClayPointerEvent
					{
						EventType = ClayPointerEventType.Click,
						Position = pointer.Value.Position,
						LocalPosition = localPosForEvents,
						Button = buttonsReleasedOnThisElement,
						ScrollDelta = Vector2.Zero,
					};

					EmitEvent(clickEvent, entityId, commands, events);

					found = true;
				}
			}

			// Emit scroll events
			var scrollDelta = pointer.Value.ScrollDelta;
			if (scrollDelta != Vector2.Zero)
			{
				var scrollEvent = new ClayPointerEvent
				{
					EventType = ClayPointerEventType.Scroll,
					Position = pointer.Value.Position,
					LocalPosition = localPosForEvents,
					Button = pointer.Value.ButtonsDown,
					ScrollDelta = scrollDelta,
				};

				EmitEvent(scrollEvent, entityId, commands, events);

				found = true;
			}
		}

		// Detect Exit events for elements that were previously hovered but are no longer hovered
		// Use IDOp.Compose to directly build entity IDs instead of searching through all nodes
		foreach (var prevElementId in state.Value!.PreviousHoveredIds)
		{
			// Skip if still hovered
			if (currentHoveredIds.Value!.Contains(prevElementId))
				continue;

			// Compose entity ID directly from Clay element ID
			var entityId = IDOp.Compose(prevElementId.id, prevElementId.offset);

			// Verify the entity still exists and has a ClayNode
			if (!nodes.Contains(entityId))
				continue;

			// This element was hovered but isn't now - emit Exit event
			var elementData = Clay_cs.Clay.GetElementData(prevElementId);
			if (elementData.found)
			{
				var localPos = new Vector2(
					state.Value!.PreviousPointerPosition.X - elementData.boundingBox.x,
					state.Value!.PreviousPointerPosition.Y - elementData.boundingBox.y
				);

				var exitEvent = new ClayPointerEvent
				{
					EventType = ClayPointerEventType.Exit,
					Position = state.Value!.PreviousPointerPosition,
					LocalPosition = localPos,
					Button = pointer.Value.ButtonsDown,
					ScrollDelta = Vector2.Zero,
				};

				EmitEvent(exitEvent, entityId, commands, events);
				UpdateInteractionState(entityId, ClayPointerEventType.Exit, ClayMouseButton.None, false, commands, interactions);
			}
		}

		// Emit Released events for elements that were pressed but are no longer under the pointer
		// This handles drag scenarios where the pointer moves outside the element bounds
		if (pointer.Value.ButtonsReleased != ClayMouseButton.None)
		{
			foreach (ClayMouseButton button in Enum.GetValues<ClayMouseButton>())
			{
				if (button != ClayMouseButton.None && pointer.Value.ButtonsReleased.HasFlag(button))
				{
					// Check if we tracked a pressed element for this button
					if (state.Value!.PressedElements.TryGetValue(button, out var pressedElementId))
					{
						// Compose entity ID
						var entityId = IDOp.Compose(pressedElementId.id, pressedElementId.offset);

						// Verify the entity still exists
						if (nodes.Contains(entityId))
						{
							// Get element data for local position calculation
							var elementData = Clay_cs.Clay.GetElementData(pressedElementId);
							if (elementData.found)
							{
								var localPos = new Vector2(
									pointer.Value.Position.X - elementData.boundingBox.x,
									pointer.Value.Position.Y - elementData.boundingBox.y
								);

								var releaseEvent = new ClayPointerEvent
								{
									EventType = ClayPointerEventType.Released,
									Position = pointer.Value.Position,
									LocalPosition = localPos,
									Button = button,
									ScrollDelta = Vector2.Zero,
								};

								EmitEvent(releaseEvent, entityId, commands, events);
								UpdateInteractionState(entityId, ClayPointerEventType.Released, button, false, commands, interactions);
							}
						}

						// Remove from tracking
						state.Value!.PressedElements.Remove(button);
					}
				}
			}
		}

		// Update state for next frame - swap the HashSets to avoid allocating a new one
		// Clear previous and populate with current values
		state.Value!.PreviousHoveredIds.Clear();
		foreach (var id in currentHoveredIds.Value!)
		{
			state.Value!.PreviousHoveredIds.Add(id);
		}
		state.Value!.PreviousPointerPosition = pointer.Value.Position;
	}
}

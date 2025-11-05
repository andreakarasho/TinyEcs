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
	/// </summary>
	public HashSet<uint> PreviousHoveredIds = new();

	/// <summary>
	/// Previous pointer position for Move event detection.
	/// </summary>
	public Vector2 PreviousPointerPosition = Vector2.Zero;
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
			Local<HashSet<uint>> currentHoveredIds
		) => ProcessPointerInteraction(nodes, pointer, commands, events, state, currentHoveredIds))
			.InStage(Stage.PostUpdate)
			.Label("clay:interaction")
			.Build();
	}

	/// <summary>
	/// Emit a pointer event to the event stream and as a trigger.
	/// If evt.Bubbles is true, TinyEcs will automatically propagate the trigger up the parent hierarchy.
	/// </summary>
	private static void EmitEvent(
		ClayPointerEvent evt,
		Commands commands,
		EventWriter<ClayPointerEvent> events)
	{
		// Send to event stream
		events.Send(evt);

		// Emit trigger - TinyEcs will handle bubbling automatically if evt.Bubbles is true
		// via the IPropagatingTrigger interface implemented by ClayPointerTrigger
		commands.EmitTrigger(new ClayPointerTrigger
		{
			EntityId = evt.CurrentTarget,
			Event = evt
		});
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
		Local<HashSet<uint>> currentHoveredIds)
	{
		// Get elements under pointer
		var pointerOverIds = Clay_cs.Clay.GetPointerOverIds();
		if (pointerOverIds.IsEmpty)
			return;

		// Build current frame's hovered element IDs set (reuse Local to avoid allocations)
		currentHoveredIds.Value!.Clear();
		foreach (ref readonly var id in pointerOverIds)
		{
			currentHoveredIds.Value!.Add(id.id);
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
			var isCurrentlyHovered = currentHoveredIds.Value!.Contains(elementId.id);
			var wasPreviouslyHovered = state.Value!.PreviousHoveredIds.Contains(elementId.id);

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
						EntityId = entityId,
						CurrentTarget = entityId,
						EventType = ClayPointerEventType.Enter,
						Position = pointer.Value.Position,
						LocalPosition = localPos,
						IsPrimaryButton = pointer.Value.PrimaryDown,
						ScrollDelta = Vector2.Zero,
						Bubbles = false // Enter/Exit events don't bubble by default (like in web)
					};

					EmitEvent(enterEvent, commands, events);
				}
			}

			// Detect Exit event (element was hovered but isn't now)
			if (!isCurrentlyHovered && wasPreviouslyHovered)
			{
				var elementData = Clay_cs.Clay.GetElementData(elementId);
				if (elementData.found)
				{
					var localPos = new Vector2(
						state.Value!.PreviousPointerPosition.X - elementData.boundingBox.x,
						state.Value!.PreviousPointerPosition.Y - elementData.boundingBox.y
					);

					var exitEvent = new ClayPointerEvent
					{
						EntityId = entityId,
						CurrentTarget = entityId,
						EventType = ClayPointerEventType.Exit,
						Position = state.Value!.PreviousPointerPosition,
						LocalPosition = localPos,
						IsPrimaryButton = pointer.Value.PrimaryDown,
						ScrollDelta = Vector2.Zero,
						Bubbles = false // Enter/Exit events don't bubble by default (like in web)
					};

					EmitEvent(exitEvent, commands, events);
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
						EntityId = entityId,
						CurrentTarget = entityId,
						EventType = ClayPointerEventType.Move,
						Position = pointer.Value.Position,
						LocalPosition = localPos,
						IsPrimaryButton = pointer.Value.PrimaryDown,
						ScrollDelta = Vector2.Zero,
						Bubbles = false // Move events don't bubble by default
					};

					EmitEvent(moveEvent, commands, events);
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

			// Emit pointer events (these bubble by default like in web)
			if (pointer.Value.PrimaryPressed)
			{
				var downEvent = new ClayPointerEvent
				{
					EntityId = entityId,
					CurrentTarget = entityId,
					EventType = ClayPointerEventType.Pressed,
					Position = pointer.Value.Position,
					LocalPosition = localPosForEvents,
					IsPrimaryButton = true,
					ScrollDelta = Vector2.Zero,
					Bubbles = true // Mouse events bubble
				};

				EmitEvent(downEvent, commands, events);
				found = true;
			}

			if (pointer.Value.PrimaryReleased)
			{
				var upEvent = new ClayPointerEvent
				{
					EntityId = entityId,
					CurrentTarget = entityId,
					EventType = ClayPointerEventType.Released,
					Position = pointer.Value.Position,
					LocalPosition = localPosForEvents,
					IsPrimaryButton = true,
					ScrollDelta = Vector2.Zero,
					Bubbles = true // Mouse events bubble
				};

				EmitEvent(upEvent, commands, events);

				// Also emit click event if released over same element
				var clickEvent = new ClayPointerEvent
				{
					EntityId = entityId,
					CurrentTarget = entityId,
					EventType = ClayPointerEventType.Click,
					Position = pointer.Value.Position,
					LocalPosition = localPosForEvents,
					IsPrimaryButton = true,
					ScrollDelta = Vector2.Zero,
					Bubbles = true // Click events bubble
				};

				EmitEvent(clickEvent, commands, events);

				found = true;
			}

			// Emit scroll events
			var scrollDelta = pointer.Value.ScrollDelta;
			if (scrollDelta != Vector2.Zero)
			{
				var scrollEvent = new ClayPointerEvent
				{
					EntityId = entityId,
					CurrentTarget = entityId,
					EventType = ClayPointerEventType.Scroll,
					Position = pointer.Value.Position,
					LocalPosition = localPosForEvents,
					IsPrimaryButton = pointer.Value.PrimaryDown,
					ScrollDelta = scrollDelta,
					Bubbles = true // Scroll events bubble
				};

				EmitEvent(scrollEvent, commands, events);

				found = true;
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

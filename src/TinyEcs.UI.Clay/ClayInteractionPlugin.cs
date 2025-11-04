using System.Numerics;
using Clay_cs;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Clay;

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
			Query<Data<ClayNode, ClayElementId>> nodes,
			Res<ClayPointerState> pointer,
			Commands commands,
			EventWriter<ClayPointerEvent> events
		) => ProcessPointerInteraction(nodes, pointer, commands, events))
			.InStage(Stage.PostUpdate)
			.Label("clay:interaction")
			.Build();
	}

	/// <summary>
	/// Process pointer interactions and emit events.
	/// </summary>
	private static unsafe void ProcessPointerInteraction(
		Query<Data<ClayNode, ClayElementId>> nodes,
		Res<ClayPointerState> pointer,
		Commands commands,
		EventWriter<ClayPointerEvent> events)
	{
		// Get elements under pointer
		var pointerOverIds = Clay_cs.Clay.GetPointerOverIds();

		if (pointerOverIds.Length == 0)
		{
			// Pointer not over any element
			return;
		}

		static bool contains(uint id, ReadOnlySpan<Clay_ElementId> ids)
		{
			for (int i = 0; i < ids.Length; i++)
			{
				if (ids[i].id == id)
				{
					return true;
				}
			}
			return false;
		}


		foreach (var (entity, node, nodeId) in nodes)
		{
			if (!contains(nodeId.Ref.Id, pointerOverIds))
			{
				// This node is not under the pointer
				continue;
			}

			ref readonly var entityId = ref entity.Ref;

			// Get element data for local position calculation
			var elementData = Clay_cs.Clay.GetElementData(new Clay_ElementId() { id = nodeId.Ref.Id });

			if (!elementData.found)
			{
				// Element data not found
				continue;
			}

			var localPos = new Vector2(
				pointer.Value.Position.X - elementData.boundingBox.x,
				pointer.Value.Position.Y - elementData.boundingBox.y
			);

			// Emit pointer events
			if (pointer.Value.PrimaryPressed)
			{
				var downEvent = new ClayPointerEvent
				{
					EntityId = entityId,
					EventType = ClayPointerEventType.Down,
					Position = pointer.Value.Position,
					LocalPosition = localPos,
					IsPrimaryButton = true,
					ScrollDelta = Vector2.Zero
				};

				events.Send(downEvent);

				// Emit trigger for observers
				commands.EmitTrigger(new ClayPointerTrigger
				{
					EntityId = entityId,
					Event = downEvent
				});
			}

			if (pointer.Value.PrimaryReleased)
			{
				var upEvent = new ClayPointerEvent
				{
					EntityId = entityId,
					EventType = ClayPointerEventType.Up,
					Position = pointer.Value.Position,
					LocalPosition = localPos,
					IsPrimaryButton = true,
					ScrollDelta = Vector2.Zero
				};

				events.Send(upEvent);

				// Emit trigger for observers
				commands.EmitTrigger(new ClayPointerTrigger
				{
					EntityId = entityId,
					Event = upEvent
				});

				// Also emit click event if released over same element
				var clickEvent = new ClayPointerEvent
				{
					EntityId = entityId,
					EventType = ClayPointerEventType.Click,
					Position = pointer.Value.Position,
					LocalPosition = localPos,
					IsPrimaryButton = true,
					ScrollDelta = Vector2.Zero
				};

				events.Send(clickEvent);

				// Emit trigger for observers
				commands.EmitTrigger(new ClayPointerTrigger
				{
					EntityId = entityId,
					Event = clickEvent
				});
			}

			// Emit scroll events
			var scrollDelta = pointer.Value.GetAccumulatedScroll();
			if (scrollDelta != Vector2.Zero)
			{
				var scrollEvent = new ClayPointerEvent
				{
					EntityId = entityId,
					EventType = ClayPointerEventType.Scroll,
					Position = pointer.Value.Position,
					LocalPosition = localPos,
					IsPrimaryButton = pointer.Value.PrimaryDown,
					ScrollDelta = scrollDelta
				};

				events.Send(scrollEvent);

				// Emit trigger for observers
				commands.EmitTrigger(new ClayPointerTrigger
				{
					EntityId = entityId,
					Event = scrollEvent
				});
			}
		}
	}
}

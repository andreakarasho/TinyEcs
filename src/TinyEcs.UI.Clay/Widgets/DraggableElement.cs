using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Component that makes a Clay element draggable.
/// Attach this to any floating element to enable drag-to-move behavior.
/// </summary>
public struct DraggableElement
{
	public bool IsDragging;
	public float DragStartX;
	public float DragStartY;
	public float InitialX;
	public float InitialY;
}

/// <summary>
/// Plugin that adds draggable element systems to the application.
/// </summary>
public struct DraggableElementPlugin : IPlugin
{
	public void Build(App app)
	{
		// System to handle dragging with global pointer tracking
		app.AddSystem((Commands commands, Res<ClayPointerState> pointer, Query<Data<DraggableElement, ClayNode>> draggables) =>
		{
			foreach (var (entityId, statePtr, nodePtr) in draggables)
			{
				var elementId = entityId.Ref;
				ref var state = ref statePtr.Ref;

				// Only process if dragging
				if (!state.IsDragging)
				{
					continue;
				}

				// Check if button is still pressed
				if (!pointer.Value.IsLeftDown)
				{
					// Stop dragging
					state.IsDragging = false;
					commands.Entity(elementId).Insert(state);
					continue;
				}

				// Update element position based on current pointer position
				var deltaX = pointer.Value.Position.X - state.DragStartX;
				var deltaY = pointer.Value.Position.Y - state.DragStartY;

				ref var node = ref nodePtr.Ref;

				if (node.Floating.HasValue)
				{
					var floating = node.Floating.Value;
					floating.offset.x = state.InitialX + deltaX;
					floating.offset.y = state.InitialY + deltaY;
					node.Floating = floating;

					// Re-insert to trigger change detection
					commands.Entity(elementId).Insert(node);
				}
			}
		})
		.InStage(Stage.Update)
		.Label("draggable:drag")
		.Build();
	}
}

/// <summary>
/// Extension methods for creating draggable elements.
/// </summary>
public static class DraggableElementExtensions
{
	/// <summary>
	/// Makes a floating element draggable.
	/// Automatically sets up the DraggableElement component and pointer observer.
	/// </summary>
	/// <param name="element">The element to make draggable</param>
	/// <param name="commands">Commands for component insertion</param>
	/// <param name="initialX">Initial X position (should match the floating offset)</param>
	/// <param name="initialY">Initial Y position (should match the floating offset)</param>
	public static EntityCommands MakeDraggable(
		this EntityCommands element,
		Commands commands,
		float initialX = 0,
		float initialY = 0)
	{
		var elementId = element.Id;

		// Add DraggableElement component
		element.Insert(new DraggableElement
		{
			IsDragging = false,
			DragStartX = 0,
			DragStartY = 0,
			InitialX = initialX,
			InitialY = initialY
		});

		// Add pointer observer to start dragging on press
		element.Observe<On<ClayPointerEvent>, Commands, Query<Data<DraggableElement, ClayNode>>>((trigger, cmd, query) =>
		{
			var evt = trigger.Event;

			// Only handle press events
			if (evt.EventType != ClayPointerEventType.Pressed)
			{
				return;
			}

			// Stop propagation - we're handling this event
			trigger.Propagate(false);

			if (!query.Contains(elementId))
			{
				return;
			}

			var (_, statePtr, nodePtr) = query.Get(elementId);
			var state = statePtr.Ref;
			ref var node = ref nodePtr.Ref;

			// Start dragging
			state.IsDragging = true;
			state.DragStartX = evt.Position.X;
			state.DragStartY = evt.Position.Y;

			// Get current element position from node
			if (node.Floating.HasValue)
			{
				var floating = node.Floating.Value;
				state.InitialX = floating.offset.x;
				state.InitialY = floating.offset.y;
			}

			cmd.Entity(elementId).Insert(state);
		});

		return element;
	}
}

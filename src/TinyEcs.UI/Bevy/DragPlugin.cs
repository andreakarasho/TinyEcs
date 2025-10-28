using System.Numerics;
using TinyEcs.Bevy;
using Flexbox;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Plugin that handles dragging for UI elements with the Draggable component.
/// Processes mouse input to enable click-and-drag movement.
/// </summary>
public struct DragPlugin : IPlugin
{
	public void Build(App app)
	{
		// System to handle drag input
		app.AddSystem((
			Res<PointerInputState> pointerInput,
			Query<Data<Draggable, ComputedLayout, Interactive>> draggables) =>
		{
			ProcessDragInput(pointerInput, draggables);
		})
		.InStage(Stage.PostUpdate)
		.Label("ui:drag:process-input")
		.After("ui:pointer:hit-test")
		.Build();

		// System to apply drag positions (override Flexbox layout)
		// Runs AFTER ui:stack:update but BEFORE ui:build-render-commands
		app.AddSystem((
			Commands commands,
			Query<Data<Draggable, UiNode, ComputedLayout>> draggables) =>
		{
			ApplyDragPositions(commands, draggables);
		})
		.InStage(Stage.PostUpdate)
		.Label("ui:drag:apply-positions")
		.After("ui:stack:update")
		.Build();
	}

	/// <summary>
	/// Processes drag input and updates draggable elements.
	/// </summary>
	private static void ProcessDragInput(
		Res<PointerInputState> pointerInput,
		Query<Data<Draggable, ComputedLayout, Interactive>> draggables)
	{
		var input = pointerInput.Value;

		// Handle drag start
		if (input.IsPrimaryButtonPressed)
		{
			// Find draggable under mouse
			foreach (var (entityId, draggable, layout, interactive) in draggables)
			{
				ref var drag = ref draggable.Ref;
				ref var l = ref layout.Ref;

				// Check if mouse is over this draggable
				if (input.Position.X >= l.X && input.Position.X <= l.X + l.Width &&
					input.Position.Y >= l.Y && input.Position.Y <= l.Y + l.Height)
				{
					// Start dragging
					drag.IsDragging = true;
					drag.DragOffset = input.Position - new Vector2(l.X, l.Y);
					drag.Position = new Vector2(l.X, l.Y);
					drag.HasCustomPosition = true;
					Console.WriteLine($"[Drag] Started dragging entity {entityId.Ref} at ({l.X:F0}, {l.Y:F0})");
					break; // Only drag one element at a time
				}
			}
		}

		// Handle drag end
		if (input.IsPrimaryButtonReleased)
		{
			foreach (var (entityId, draggable, layout, interactive) in draggables)
			{
				ref var drag = ref draggable.Ref;
				if (drag.IsDragging)
				{
					drag.IsDragging = false;
				}
			}
		}

		// Handle dragging
		if (input.IsPrimaryButtonDown)
		{
			foreach (var (entityId, draggable, layout, interactive) in draggables)
			{
				ref var drag = ref draggable.Ref;
				if (drag.IsDragging)
				{
					// Update position based on mouse position
					var newPos = input.Position - drag.DragOffset;
					drag.Position = newPos;
					Console.WriteLine($"[Drag] Moving entity {entityId.Ref} to ({newPos.X:F0}, {newPos.Y:F0})");
				}
			}
		}
	}

	/// <summary>
	/// Applies drag positions by directly updating ComputedLayout.
	/// Runs after Flexbox layout to override positions for dragged elements.
	/// </summary>
	private static void ApplyDragPositions(
		Commands commands,
		Query<Data<Draggable, UiNode, ComputedLayout>> draggables)
	{
		foreach (var (entityId, draggable, uiNode, layout) in draggables)
		{
			ref var drag = ref draggable.Ref;
			ref var node = ref uiNode.Ref;
			ref var l = ref layout.Ref;

			if (drag.HasCustomPosition)
			{
				// Override Flexbox positioning with absolute positioning
				node.PositionType = PositionType.Absolute;
				node.Left = FlexValue.Points(drag.Position.X);
				node.Top = FlexValue.Points(drag.Position.Y);

				commands.Entity(entityId.Ref).Insert(node);
			}
		}
	}
}

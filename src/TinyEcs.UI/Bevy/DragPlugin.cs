using System.Numerics;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Plugin that handles dragging for UI elements with the Draggable component.
/// Processes mouse input to enable click-and-drag movement.
/// </summary>
public struct DragPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// System to handle drag input
		app.AddSystem((
			Res<PointerInputState> pointerInput,
			Res<UiStack> uiStack,
			Query<Data<Draggable, ComputedLayout, Interactive>> draggables,
			Query<Data<ComputedLayout, Interactive>, Without<Draggable>> notDraggables) =>
		{
			ProcessDragInput(pointerInput, uiStack, draggables, notDraggables);
		})
		.InStage(Stage.PostUpdate)
		.Label("ui:drag:process-input")
		.After("flexbox:read_layout") // Must run after layout is read to get correct positions
		.Build();

		// System to apply drag positions (override Flexbox layout)
		// Runs AFTER flexbox:read_layout to override positions
		app.AddSystem((
			Commands commands,
			Query<Data<Draggable, UiNode>> draggables) =>
		{
			ApplyDragPositions(commands, draggables);
		})
		.InStage(Stage.PostUpdate)
		.Label("ui:drag:apply-positions")
		.After("flexbox:read_layout") // Override positions after Flexbox calculates them
		.Build();
	}

	/// <summary>
	/// Processes drag input and updates draggable elements.
	/// Modifies Draggable component in-place (no Commands needed since we check IsDragging directly).
	/// </summary>
	private static void ProcessDragInput(
		Res<PointerInputState> pointerInput,
		Res<UiStack> uiStack,
		Query<Data<Draggable, ComputedLayout, Interactive>> draggables,
		Query<Data<ComputedLayout, Interactive>, Without<Draggable>> notDraggables)
	{
		var input = pointerInput.Value;

		// Handle drag start
		if (input.IsPrimaryButtonPressed)
		{
			// Find draggable under mouse
			for (var i = uiStack.Value.Count - 1; i >= 0; i--)
			{
				var entry = uiStack.Value.Entries[i];

				if (!draggables.Contains(entry.EntityId))
				{
					// Do not start drag if over non-draggable interactive element
					if (notDraggables.Contains(entry.EntityId))
					{
						var (_, layout2, _) = notDraggables.Get(entry.EntityId);
						ref var l2 = ref layout2.Ref;

						if (l2.Contains(input.Position))
							break;
					}

					continue;
				}

				var (_, draggable, layout, _) = draggables.Get(entry.EntityId);
				ref var drag = ref draggable.Ref;
				ref var l = ref layout.Ref;

				// Check if mouse is over this draggable
				if (l.Contains(input.Position))
				{
					// Start dragging
					drag.IsDragging = true;
					drag.DragOffset = input.Position - new Vector2(l.X, l.Y);
					drag.Position = new Vector2(l.X, l.Y);
					// Don't set HasCustomPosition until mouse actually moves
					// This prevents the element from shifting on click

					break; // Only drag one element at a time
				}
			}
		}
		// Handle drag end
		else if (input.IsPrimaryButtonReleased)
		{
			foreach (var (_, draggable, _, _) in draggables)
			{
				ref var drag = ref draggable.Ref;
				if (drag.IsDragging)
				{
					drag.IsDragging = false;
					drag.HasCustomPosition = false; // Reset when drag ends
				}
			}
		}
		// Handle dragging (only if not just pressed/released)
		else if (input.IsPrimaryButtonDown)
		{
			foreach (var (_, draggable, _, _) in draggables)
			{
				ref var drag = ref draggable.Ref;
				if (drag.IsDragging)
				{
					// Update position based on mouse position
					var newPos = input.Position - drag.DragOffset;

					// Only set HasCustomPosition if position actually changed
					// This prevents applying absolute positioning when just holding the mouse down
					if (newPos != drag.Position)
					{
						drag.Position = newPos;
						drag.HasCustomPosition = true;
					}
				}
			}
		}
	}

	/// <summary>
	/// Applies drag positions by modifying UiNode to use absolute positioning.
	/// Uses screen-absolute coordinates directly.
	/// Clears margins to prevent double-offset (margins still apply to absolute positioning in Flexbox).
	/// </summary>
	private static void ApplyDragPositions(
		Commands commands,
		Query<Data<Draggable, UiNode>> draggables)
	{
		foreach (var (entityId, draggable, uiNode) in draggables)
		{
			ref var drag = ref draggable.Ref;
			ref var node = ref uiNode.Ref;

			// Only apply if has custom position (set when actually moving during drag)
			if (drag.HasCustomPosition)
			{
				// Update UiNode to use absolute positioning with screen-absolute coordinates
				node.PositionType = Flexbox.PositionType.Absolute;
				node.Left = FlexValue.Points(drag.Position.X);
				node.Top = FlexValue.Points(drag.Position.Y);
				node.Right = FlexValue.Auto();
				node.Bottom = FlexValue.Auto();

				// Clear margins - they still apply to absolutely positioned elements in Flexbox
				// This prevents the margin from being added on top of our absolute position
				node.MarginLeft = FlexValue.Points(0);
				node.MarginTop = FlexValue.Points(0);
				node.MarginRight = FlexValue.Points(0);
				node.MarginBottom = FlexValue.Points(0);

				// Re-insert UiNode to trigger Flexbox sync
				commands.Entity(entityId.Ref).Insert(node);
			}
		}
	}
}

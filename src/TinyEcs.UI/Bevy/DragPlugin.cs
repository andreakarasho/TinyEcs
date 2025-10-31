using System;
using System.Numerics;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Plugin that handles dragging for UI elements with the Draggable component.
/// Enhanced version with state machine from sickle_ui.
/// Integrates with FluxInteraction for proper interaction lifecycle tracking.
/// </summary>
public struct DragPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// System 1: Initialize drag origin when drag starts
		app.AddSystem((
			Res<PointerInputState> pointerInput,
			Query<Data<Draggable>> draggables) =>
		{
			InitializeDragOrigin(pointerInput, draggables);
		})
		.InStage(Stage.PreUpdate)
		.Label("drag:init-origin")
		.After("flux:update-prev")
		.Build();

		// System 2: Update drag progress (tracks position changes during drag)
		// Runs after FluxInteraction update to get latest interaction state
		app.AddSystem((
			Res<PointerInputState> pointerInput,
			Query<Data<Draggable, FluxInteraction>> draggables) =>
		{
			UpdateDragProgress(pointerInput, draggables);
		})
		.InStage(Stage.PreUpdate)
		.Label("drag:update-progress")
		.After("drag:init-origin")
		.Build();

		// System 3: Update drag state based on FluxInteraction changes
		app.AddSystem((
			Query<Data<Draggable, FluxInteraction>, Filter<Changed<FluxInteraction>>> changedFlux) =>
		{
			UpdateDragState(changedFlux);
		})
		.InStage(Stage.PreUpdate)
		.Label("drag:update-state")
		.After("drag:update-progress")
		.Build();

		// System 3: Apply drag positions (override Flexbox layout)
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
	/// Updates drag progress by tracking position changes during active drags.
	/// Handles the MaybeDragged → DragStart → Dragging state transitions.
	/// Ported from sickle_ui's update_drag_progress.
	/// </summary>
	private static void UpdateDragProgress(
		Res<PointerInputState> pointerInput,
		Query<Data<Draggable, FluxInteraction>> draggables)
	{
		var mousePos = pointerInput.Value.Position;

		foreach (var (entityId, draggable, fluxInteraction) in draggables)
		{
			ref var drag = ref draggable.Ref;
			ref readonly var flux = ref fluxInteraction.Ref;

			// Transition from DragEnd to Inactive (cleanup)
			if (drag.State == DragState.DragEnd)
			{
				drag.State = DragState.Inactive;
				drag.Clear();
				continue;
			}

			// Transition from DragCanceled to Inactive
			if (drag.State == DragState.DragCanceled)
			{
				drag.State = DragState.Inactive;
				continue;
			}

			// Only update if FluxInteraction is Pressed and we're in a drag state
			if (flux.State == FluxInteractionState.Pressed &&
			    (drag.State == DragState.MaybeDragged ||
			     drag.State == DragState.DragStart ||
			     drag.State == DragState.Dragging))
			{
				// TODO: ESC key cancellation (requires keyboard input system)
				// if (drag.State is DragStart or Dragging && escKeyPressed)
				// {
				//     drag.State = DragCanceled;
				//     drag.Clear();
				//     continue;
				// }

				// DragStart only lasts one frame, then transitions to Dragging
				if (drag.State == DragState.DragStart)
				{
					drag.State = DragState.Dragging;
				}

				// Get current mouse position
				var currentPosition = mousePos;

				// If we have both previous and current position, calculate movement
				if (drag.Position.HasValue)
				{
					var prevPos = drag.Position.Value;
					var delta = currentPosition - prevPos;

					// Check if actually moved (no tolerance threshold in sickle_ui)
					if (delta.LengthSquared() > 0f)
					{
						// Transition from MaybeDragged to DragStart on first movement
						if (drag.State == DragState.MaybeDragged)
						{
							drag.State = DragState.DragStart;
						}

						// Update position and delta
						drag.Position = currentPosition;
						drag.Diff = delta;
					}
				}
			}
		}
	}

	/// <summary>
	/// Updates drag state based on FluxInteraction changes.
	/// Handles starting drag (Pressed) and ending drag (Released/PressCanceled).
	/// Ported from sickle_ui's update_drag_state.
	/// </summary>
	private static void UpdateDragState(
		Query<Data<Draggable, FluxInteraction>, Filter<Changed<FluxInteraction>>> changedFlux)
	{
		foreach (var (entityId, draggable, fluxInteraction) in changedFlux)
		{
			ref var drag = ref draggable.Ref;
			ref readonly var flux = ref fluxInteraction.Ref;

			// Start drag on Pressed
			if (flux.State == FluxInteractionState.Pressed &&
			    drag.State != DragState.MaybeDragged)
			{
				// Note: We don't have access to PointerInputState here to get the position
				// So we'll set origin/position in the next UpdateDragProgress call
				drag.State = DragState.MaybeDragged;
				drag.Source = DragSource.Mouse;
				drag.Origin = null; // Will be set when we get pointer position
				drag.Position = null;
				drag.Diff = Vector2.Zero;
			}
			// End drag on Released or PressCanceled
			else if (flux.State == FluxInteractionState.Released ||
			         flux.State == FluxInteractionState.PressCanceled)
			{
				if (drag.State == DragState.DragStart || drag.State == DragState.Dragging)
				{
					drag.State = DragState.DragEnd;
				}
				else if (drag.State == DragState.MaybeDragged)
				{
					// Clicked but didn't drag - just go back to inactive
					drag.State = DragState.Inactive;
					drag.Clear();
				}
			}
		}
	}

	/// <summary>
	/// Initializes drag origin and position when FluxInteraction.Pressed is detected.
	/// This is a helper system that runs early to capture the initial pointer position.
	/// </summary>
	private static void InitializeDragOrigin(
		Res<PointerInputState> pointerInput,
		Query<Data<Draggable>> draggables)
	{
		var mousePos = pointerInput.Value.Position;

		foreach (var (_, draggable) in draggables)
		{
			ref var drag = ref draggable.Ref;

			// Set origin and initial position when MaybeDragged and no position set yet
			if (drag.State == DragState.MaybeDragged && !drag.Position.HasValue)
			{
				drag.Origin = mousePos;
				drag.Position = mousePos;
			}
		}
	}

	/// <summary>
	/// Applies drag positions by modifying UiNode to use absolute positioning.
	/// Uses screen-absolute coordinates directly.
	/// Updated to work with DragState state machine.
	/// </summary>
	private static void ApplyDragPositions(
		Commands commands,
		Query<Data<Draggable, UiNode>> draggables)
	{
		foreach (var (entityId, draggable, uiNode) in draggables)
		{
			ref var drag = ref draggable.Ref;
			ref var node = ref uiNode.Ref;

			// Only apply positions during active drag states
			if ((drag.State == DragState.DragStart || drag.State == DragState.Dragging) &&
			    drag.Position.HasValue)
			{
				var position = drag.Position.Value;

				// Update UiNode to use absolute positioning with screen-absolute coordinates
				node.PositionType = Flexbox.PositionType.Absolute;
				node.Left = FlexValue.Points(position.X);
				node.Top = FlexValue.Points(position.Y);
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
			// Reset to relative positioning when drag ends
			else if (drag.State == DragState.Inactive && node.PositionType == Flexbox.PositionType.Absolute)
			{
				node.PositionType = Flexbox.PositionType.Relative;
				node.Left = FlexValue.Auto();
				node.Top = FlexValue.Auto();

				// Re-insert UiNode to trigger Flexbox sync
				commands.Entity(entityId.Ref).Insert(node);
			}
		}
	}
}

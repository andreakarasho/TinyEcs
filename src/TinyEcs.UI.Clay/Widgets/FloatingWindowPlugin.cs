using System.Collections.Generic;
using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Resource to track the window stack (z-ordering).
/// Windows at the end of the list are on top.
/// </summary>
public class UiStack
{
	public List<ulong> Windows = new();
}

/// <summary>
/// Plugin that adds floating window widget systems to the application.
/// </summary>
public struct FloatingWindowPlugin : IPlugin
{
	public void Build(App app)
	{
		// Add UiStack resource
		app.AddResource(new UiStack());

		// Observer to handle window clicks (bring to front)
		app.AddObserver<On<WindowClicked>, Commands, ResMut<UiStack>, Query<Data<ClayNode>>>((trigger, commands, stack, nodeQuery) =>
		{
			var windowId = trigger.EntityId;

			// Remove from current position in stack
			stack.Value.Windows.Remove(windowId);

			// Add to end (topmost)
			stack.Value.Windows.Add(windowId);

			// Update z-indices for all windows in stack
			for (int i = 0; i < stack.Value.Windows.Count; i++)
			{
				var winId = stack.Value.Windows[i];

				if (!nodeQuery.Contains(winId))
				{
					continue;
				}

				var (_, nodePtr) = nodeQuery.Get(winId);
				ref var node = ref nodePtr.Ref;

				if (node.Floating.HasValue)
				{
					var floating = node.Floating.Value;
					floating.zIndex = (short)(100 + i); // Base z-index of 100, increment per window
					node.Floating = floating;

					// Re-insert to trigger change detection
					commands.Entity(winId).Insert(node);
				}
			}
		});

		// Observer to handle window close requests
		app.AddObserver<On<WindowCloseRequested>, Commands, ResMut<UiStack>>((trigger, commands, stack) =>
		{
			var windowId = trigger.EntityId;

			// Remove from stack
			stack.Value.Windows.Remove(windowId);

			// Despawn the window entity
			commands.Entity(windowId).Despawn();
		});

		// System to register new windows to the stack
		// When a FloatingWindowState is added, add the window to the stack
		app.AddSystem((Commands commands, ResMut<UiStack> stack, Query<Data<FloatingWindowState>, Filter<Added<FloatingWindowState>>> newWindows) =>
		{
			foreach (var (entityId, _) in newWindows)
			{
				var windowId = entityId.Ref;

				// Add to stack if not already present
				if (!stack.Value.Windows.Contains(windowId))
				{
					stack.Value.Windows.Add(windowId);
				}
			}
		})
		.InStage(Stage.Update)
		.Label("floatingwindow:register")
		.Build();

		// System to handle window dragging with global pointer tracking
		app.AddSystem((Commands commands, Res<ClayPointerState> pointer, Query<Data<FloatingWindowState>> windowStates, Query<Data<ClayNode>> nodeQuery) =>
		{
			foreach (var (entityId, statePtr) in windowStates)
			{
				var windowId = entityId.Ref;
				var state = statePtr.Ref;

				// Only process if dragging
				if (!state.IsDragging)
				{
					continue;
				}

				// Check if button is still pressed
				if (!pointer.Value.PrimaryDown)
				{
					// Stop dragging
					state.IsDragging = false;
					commands.Entity(windowId).Insert(state);
					continue;
				}

				// Update window position based on current pointer position
				var deltaX = pointer.Value.Position.X - state.DragStartX;
				var deltaY = pointer.Value.Position.Y - state.DragStartY;

				if (nodeQuery.Contains(windowId))
				{
					var (_, windowNodePtr) = nodeQuery.Get(windowId);
					ref var windowNode = ref windowNodePtr.Ref;

					if (windowNode.Floating.HasValue)
					{
						var floating = windowNode.Floating.Value;
						floating.offset.x = state.InitialX + deltaX;
						floating.offset.y = state.InitialY + deltaY;
						windowNode.Floating = floating;

						// Re-insert to trigger change detection
						commands.Entity(windowId).Insert(windowNode);
					}
				}
			}
		})
		.InStage(Stage.Update)
		.Label("floatingwindow:drag")
		.Build();

		// System to handle window resizing with global pointer tracking
		app.AddSystem((Commands commands, Res<ClayPointerState> pointer, Query<Data<FloatingWindowState>> windowStates, Query<Data<ClayNode>> nodeQuery) =>
		{
			foreach (var (entityId, statePtr) in windowStates)
			{
				var windowId = entityId.Ref;
				var state = statePtr.Ref;

				// Only process if resizing
				if (!state.IsResizing)
				{
					continue;
				}

				// Check if button is still pressed
				if (!pointer.Value.PrimaryDown)
				{
					// Stop resizing
					state.IsResizing = false;
					state.ResizingEdge = ResizeEdge.None;
					commands.Entity(windowId).Insert(state);
					continue;
				}

				// Update window size based on current pointer position and resize edge
				var deltaX = pointer.Value.Position.X - state.DragStartX;
				var deltaY = pointer.Value.Position.Y - state.DragStartY;

				var newWidth = state.InitialWidth;
				var newHeight = state.InitialHeight;

				// Apply delta based on which edge is being resized
				switch (state.ResizingEdge)
				{
					case ResizeEdge.Right:
						newWidth = System.Math.Max(state.MinWidth, state.InitialWidth + deltaX);
						newHeight = state.InitialHeight; // Height doesn't change
						break;
					case ResizeEdge.Bottom:
						newWidth = state.InitialWidth; // Width doesn't change
						newHeight = System.Math.Max(state.MinHeight, state.InitialHeight + deltaY);
						break;
					case ResizeEdge.BottomRight:
						newWidth = System.Math.Max(state.MinWidth, state.InitialWidth + deltaX);
						newHeight = System.Math.Max(state.MinHeight, state.InitialHeight + deltaY);
						break;
				}

				if (nodeQuery.Contains(windowId))
				{
					var (_, windowNodePtr) = nodeQuery.Get(windowId);
					ref var windowNode = ref windowNodePtr.Ref;

					var layout = windowNode.Layout;
					layout.sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(newWidth),
						Clay_SizingAxis.Fixed(newHeight)
					);
					windowNode.Layout = layout;

					// Re-insert to trigger change detection
					commands.Entity(windowId).Insert(windowNode);
				}

				// Note: All resize borders (right, bottom, corner) now auto-resize via layout system
				// No manual position/size updates needed anymore
			}
		})
		.InStage(Stage.Update)
		.Label("floatingwindow:resize")
		.Build();
	}
}

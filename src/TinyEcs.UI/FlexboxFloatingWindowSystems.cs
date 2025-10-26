using System;
using System.Numerics;
using TinyEcs.Bevy;
using TinyEcs.UI.Flexbox;

namespace TinyEcs.UI;

/// <summary>
/// Systems for handling Flexbox floating window interactions (dragging, resizing, etc).
/// </summary>
public static class FlexboxFloatingWindowSystems
{
	/// <summary>
	/// System to handle floating window dragging and button clicks.
	/// Processes UiPointerEvents and updates window positions.
	/// </summary>
	public static void HandleWindowDrag(
		EventReader<UiPointerEvent> events,
		Query<Data<FlexboxFloatingWindowState, FlexboxFloatingWindowLinks, FlexboxNode>> windows,
		Query<Data<Parent>> parents,
		Commands commands,
		ResMut<FlexboxUiState> uiState)
	{
		foreach (var evt in events.Read())
		{
			// Process each window to see if it should handle this event
			foreach (var (entityId, winState, winLinks, winNode) in windows)
			{
				var windowId = entityId.Ref;
				ref var state = ref winState.Ref;
				var links = winLinks.Ref;
				ref var node = ref winNode.Ref;

				// Check if this event is related to this window (target is window or descendant)
				bool isWindowEvent = evt.Target == windowId ||
									  evt.Target == links.TitleBarId ||
									  evt.Target == links.ContentAreaId ||
									  evt.Target == links.CloseButtonId ||
									  evt.Target == links.MinimizeButtonId ||
									  evt.Target == links.MaximizeButtonId;

				if (!isWindowEvent)
				{
					// Check if target is a descendant of this window by walking up parent chain
					var current = evt.Target;
					int safety = 0;
					while (current != 0 && safety++ < 256)
					{
						if (current == windowId)
						{
							isWindowEvent = true;
							break;
						}
						if (!parents.Contains(current)) break;
						var parentData = parents.Get(current);
						parentData.Deconstruct(out _, out var parentPtr);
						var parentId = parentPtr.Ref.Id;
						if (parentId == 0 || parentId == current) break;
						current = parentId;
					}
				}

				if (!isWindowEvent)
					continue; // This event is not for this window

				// Handle button clicks (only on direct target, not bubbled)
				if (evt.Type == UiPointerEventType.PointerDown && evt.IsPrimaryButton && evt.Target == evt.CurrentTarget)
				{
					if (evt.Target == links.CloseButtonId)
					{
						commands.Entity(windowId).Despawn();
						continue;
					}

					if (evt.Target == links.MinimizeButtonId)
					{
						state.IsMinimized = !state.IsMinimized;
						continue;
					}

					if (evt.Target == links.MaximizeButtonId)
					{
						state.IsMaximized = !state.IsMaximized;
						continue;
					}
				}

				// Handle window dragging from title bar
				if (evt.Type == UiPointerEventType.PointerDown && evt.IsPrimaryButton)
				{
					// Check if clicking on title bar or its children (but not buttons)
					bool onTitleBar = evt.Target == links.TitleBarId;

					if (!onTitleBar && links.TitleBarId != 0)
					{
						// Walk up parent chain to see if we're a child of title bar
						var current = evt.Target;
						int safety = 0;
						while (current != 0 && safety++ < 256)
						{
							if (current == links.TitleBarId)
							{
								onTitleBar = true;
								break;
							}

							if (!parents.Contains(current)) break;
							var parentData = parents.Get(current);
							parentData.Deconstruct(out _, out var parentPtr);
							var parentId = parentPtr.Ref.Id;
							if (parentId == 0 || parentId == current) break;
							current = parentId;
						}
					}

					// Don't drag when clicking buttons
					bool onButton = evt.Target == links.CloseButtonId ||
									evt.Target == links.MinimizeButtonId ||
									evt.Target == links.MaximizeButtonId;

					if (onTitleBar && !onButton && state.CanDrag)
					{
						state.IsDragging = true;
						state.DragOffset = evt.Position - state.Position;
					}
				}
				else if (evt.Type == UiPointerEventType.PointerUp)
				{
					if (state.IsDragging)
					{
						state.IsDragging = false;

						// Sync FlexboxNode component with final position
						node.Left = FlexValue.Points(state.Position.X);
						node.Top = FlexValue.Points(state.Position.Y);
					}
				}
				else if (evt.Type == UiPointerEventType.PointerMove)
				{
					if (state.IsDragging)
					{
						// Update position (absolute positioning in Flexbox)
						state.Position = evt.Position - state.DragOffset;

						// Update the FlexboxNode component so it persists
						// The sync system will pick up these changes next frame
						node.Left = FlexValue.Points(state.Position.X);
						node.Top = FlexValue.Points(state.Position.Y);
					}
				}
			}
		}
	}
}

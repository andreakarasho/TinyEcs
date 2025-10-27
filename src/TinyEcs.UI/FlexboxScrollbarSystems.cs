using System;
using System.Numerics;
using TinyEcs.Bevy;
using TinyEcs.UI.Flexbox;

namespace TinyEcs.UI;

/// <summary>
/// Systems for handling scrollbar interaction (dragging the handle).
/// </summary>
public static class FlexboxScrollbarSystems
{
	/// <summary>
	/// Handles scrollbar drag interaction.
	/// Must run in Stage.PreUpdate after pointer processing.
	/// </summary>
	public static void HandleScrollbarDrag(
		EventReader<UiPointerEvent> events,
		Query<Data<FlexboxScrollbarState, FlexboxScrollbarLinks, FlexboxScrollbarStyle, FlexboxNode>> scrollbars,
		Query<Data<FlexboxNode>> nodes,
		Query<Data<Parent>> parents,
		ResMut<FlexboxUiState> uiState)
	{
		foreach (var evt in events.Read())
		{
			foreach (var (entityId, statePtr, linksPtr, stylePtr, scrollbarNodePtr) in scrollbars)
			{
				var scrollbarId = entityId.Ref;
				ref var state = ref statePtr.Ref;
				var links = linksPtr.Ref;
				var style = stylePtr.Ref;
				ref var scrollbarNode = ref scrollbarNodePtr.Ref;

				// Check if event targets this scrollbar's track or handle
				bool isTargetingScrollbar = evt.Target == links.TrackId ||
											evt.Target == links.HandleId ||
											evt.CurrentTarget == links.TrackId ||
											evt.CurrentTarget == links.HandleId;

				// Accept events when targeting scrollbar OR when already dragging
				bool acceptEvent = isTargetingScrollbar || state.IsDragging;
				if (!acceptEvent)
					continue;

				// Only process if this is the INNERMOST scrollbar being targeted
				// Check if there's another scrollbar between the target and this scrollbar
				if (isTargetingScrollbar && !state.IsDragging)
				{
					// Walk up from target to find if there's another scrollbar in between
					var current = evt.Target;
					bool foundIntermediateScrollbar = false;

					while (current != 0 && current != scrollbarId)
					{
						// Check if this entity is part of another scrollbar
						foreach (var (otherId, _, otherLinks, _, _) in scrollbars)
						{
							var otherScrollbarId = otherId.Ref;
							if (otherScrollbarId == scrollbarId) continue; // Skip self

							var otherLinksRef = otherLinks.Ref;
							if (current == otherLinksRef.TrackId || current == otherLinksRef.HandleId)
							{
								foundIntermediateScrollbar = true;
								break;
							}
						}

						if (foundIntermediateScrollbar) break;

						// Move to parent
						if (!parents.Contains(current))
							break;
						var parentData = parents.Get(current);
						parentData.Deconstruct(out _, out var parentPtr);
						current = parentPtr.Ref.Id;
					}

					if (foundIntermediateScrollbar)
						continue; // Skip - let the inner scrollbar handle it
				}

				switch (evt.Type)
				{
					case UiPointerEventType.PointerDown:
						if (!isTargetingScrollbar) break; // Only start drag when pressing this scrollbar

						if (evt.IsPrimaryButton)
						{
							state.IsDragging = true;

							// Get handle position from Flexbox node layout
							if (uiState.Value.TryGetNode(links.HandleId, out var handleNode) && handleNode != null)
							{
								if (state.Orientation == ScrollbarOrientation.Vertical)
								{
									// Store offset from handle top to pointer Y
									state.DragOffset = evt.Position.Y - handleNode.layout.top;
								}
								else
								{
									// Store offset from handle left to pointer X
									state.DragOffset = evt.Position.X - handleNode.layout.left;
								}
							}
						}
						break;

					case UiPointerEventType.PointerUp:
						state.IsDragging = false;
						break;

					case UiPointerEventType.PointerMove:
						if (!state.IsDragging) break;

						// Update handle position based on pointer movement
						if (uiState.Value.TryGetNode(links.TrackId, out var trackNode) && trackNode != null &&
							nodes.Contains(links.HandleId))
						{
							var handleData = nodes.Get(links.HandleId);
							handleData.Deconstruct(out var handleNodePtr);
							ref var handleNode = ref handleNodePtr.Ref;

							if (state.Orientation == ScrollbarOrientation.Vertical)
							{
								// Calculate new handle position
								var trackHeight = trackNode.layout.height;
								var handleHeight = handleNode.Height.Unit == global::Flexbox.Unit.Point
									? handleNode.Height.Value
									: style.MinHandleSize;
								var availableHeight = trackHeight - handleHeight - (style.HandlePadding * 2f);

								// Calculate new top position (relative to track)
								var newTop = evt.Position.Y - state.DragOffset - trackNode.layout.top;
								newTop = Math.Clamp(newTop, style.HandlePadding, style.HandlePadding + availableHeight);

								// Update scroll position (0.0 to 1.0)
								if (availableHeight > 0)
								{
									state.ScrollPosition = (newTop - style.HandlePadding) / availableHeight;
									state.ScrollPosition = Math.Clamp(state.ScrollPosition, 0f, 1f);
								}

								// Update handle node position
								handleNode.Top = FlexValue.Points(newTop);
							}
							else // Horizontal
							{
								// Calculate new handle position
								var trackWidth = trackNode.layout.width;
								var handleWidth = handleNode.Width.Unit == global::Flexbox.Unit.Point
									? handleNode.Width.Value
									: style.MinHandleSize;
								var availableWidth = trackWidth - handleWidth - (style.HandlePadding * 2f);

								// Calculate new left position (relative to track)
								var newLeft = evt.Position.X - state.DragOffset - trackNode.layout.left;
								newLeft = Math.Clamp(newLeft, style.HandlePadding, style.HandlePadding + availableWidth);

								// Update scroll position (0.0 to 1.0)
								if (availableWidth > 0)
								{
									state.ScrollPosition = (newLeft - style.HandlePadding) / availableWidth;
									state.ScrollPosition = Math.Clamp(state.ScrollPosition, 0f, 1f);
								}

								// Update handle node position
								handleNode.Left = FlexValue.Points(newLeft);
							}
						}
						break;
				}
			}
		}
	}

	/// <summary>
	/// Updates scrollbar visual state (handle color on hover/active).
	/// Must run in Stage.PreUpdate before layout.
	/// </summary>
	public static void UpdateScrollbarVisuals(
		Query<Data<FlexboxScrollbarState, FlexboxScrollbarLinks, FlexboxScrollbarStyle>> scrollbars,
		Query<Data<FlexboxNode>> nodes,
		Res<FlexboxPointerState> pointerState)
	{
		foreach (var (statePtr, linksPtr, stylePtr) in scrollbars)
		{
			ref readonly var state = ref statePtr.Ref;
			var links = linksPtr.Ref;
			var style = stylePtr.Ref;

			if (!nodes.Contains(links.HandleId))
				continue;

			var handleData = nodes.Get(links.HandleId);
			handleData.Deconstruct(out var handleNodePtr);
			ref var handleNode = ref handleNodePtr.Ref;

			// Update handle color based on interaction state
			if (state.IsDragging)
			{
				handleNode.BackgroundColor = style.HandleActiveColor;
			}
			else
			{
				// TODO: Check if pointer is hovering over handle
				// For now, just use default color
				handleNode.BackgroundColor = style.HandleColor;
			}
		}
	}

	/// <summary>
	/// Syncs standalone scroll container scrollbars (not part of windows).
	/// Updates scrollbar position when scroll container is scrolled via wheel.
	/// Updates scroll container offset when scrollbar is dragged.
	/// </summary>
	public static void SyncScrollContainerScrollbars(
		Query<Data<FlexboxScrollContainerLinks>> scrollContainerWrappers,
		Query<Data<FlexboxScrollContainer>> scrollContainers,
		Query<Data<FlexboxScrollbarState, FlexboxScrollbarLinks, FlexboxScrollbarStyle, FlexboxNode>> scrollbars,
		Query<Data<FlexboxNode>> nodes,
		Commands commands,
		ResMut<FlexboxUiState> uiState)
	{
		foreach (var (entityId, linksPtr) in scrollContainerWrappers)
		{
			var links = linksPtr.Ref;

			// Skip if doesn't have both parts
			if (links.ContainerId == 0 || links.ScrollbarId == 0)
				continue;

			// Get scroll container state
			if (!scrollContainers.Contains(links.ContainerId))
				continue;

			var scrollData = scrollContainers.Get(links.ContainerId);
			scrollData.Deconstruct(out var scrollPtr);
			ref var scroll = ref scrollPtr.Ref;

			// Get scrollbar state
			if (!scrollbars.Contains(links.ScrollbarId))
				continue;

			var scrollbarData = scrollbars.Get(links.ScrollbarId);
			scrollbarData.Deconstruct(out var statePtr, out var barLinksPtr, out var stylePtr, out var barNodePtr);
			ref var scrollbarState = ref statePtr.Ref;
			var scrollbarLinks = barLinksPtr.Ref;
			var scrollbarStyle = stylePtr.Ref;
			ref var scrollbarNode = ref barNodePtr.Ref;

			// Get viewport node and content area node
			if (!uiState.Value.TryGetNode(links.ContainerId, out var viewportNode) || viewportNode == null)
				continue;

			// Get content area node for measuring total content height
			// Content area has Height=Auto and grows to fit all children
			global::Flexbox.Node? contentNode = links.ContentAreaId != 0
				? uiState.Value.TryGetNode(links.ContentAreaId, out var cn) ? cn : null
				: null;

			// Calculate content height and viewport height
			float contentHeight;
			float viewportHeight = viewportNode.layout.content.height;

			if (contentNode != null)
			{
				// Content area has Height=Auto, so its layout.height is the total content height
				contentHeight = contentNode.layout.height;
			}
			else
			{
				// Fallback: no separate content area, measure viewport's children
				contentHeight = viewportHeight; // No scrolling needed
			}

			var maxScrollY = MathF.Max(0f, contentHeight - viewportHeight);

			// If scrollbar was just dragged, update scroll container offset
			if (scrollbarState.IsDragging)
			{
				var newOffset = scrollbarState.ScrollPosition * maxScrollY;
				scroll.Offset = new Vector2(scroll.Offset.X, newOffset);
				commands.Entity(links.ContainerId).Insert(scroll);
			}
			else
			{
				// Otherwise, sync scrollbar to match scroll container offset
				var visibleRatio = contentHeight > 0 ? Math.Clamp(viewportHeight / contentHeight, 0.01f, 1.0f) : 1.0f;
				var scrollPosition = maxScrollY > 0 ? Math.Clamp(scroll.Offset.Y / maxScrollY, 0f, 1f) : 0f;

				scrollbarState.VisibleRatio = visibleRatio;
				scrollbarState.ScrollPosition = scrollPosition;

				// Update handle size and position
				if (nodes.Contains(scrollbarLinks.HandleId))
				{
					var handleData = nodes.Get(scrollbarLinks.HandleId);
					handleData.Deconstruct(out var handleNodePtr);
					ref var handleNode = ref handleNodePtr.Ref;

					var trackHeight = scrollbarNode.Height.Unit == global::Flexbox.Unit.Point
						? scrollbarNode.Height.Value
						: 100f;
					var availableHeight = trackHeight - (scrollbarStyle.HandlePadding * 2f);
					var handleHeight = Math.Max(scrollbarStyle.MinHandleSize, availableHeight * visibleRatio);

					handleNode.Height = FlexValue.Points(handleHeight);

					var maxHandleTravel = availableHeight - handleHeight;
					var newTop = scrollbarStyle.HandlePadding + (maxHandleTravel * scrollPosition);
					handleNode.Top = FlexValue.Points(newTop);
				}
			}
		}
	}
}

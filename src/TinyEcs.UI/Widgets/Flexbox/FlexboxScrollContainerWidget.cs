using System;
using System.Numerics;
using Flexbox;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Flexbox;

/// <summary>
/// Viewport configuration for scroll containers.
/// Stores the visual clipping height (used during rendering, not layout).
/// </summary>
public struct FlexboxScrollContainerViewport
{
	public float Height;
}

/// <summary>
/// Links to parts of a scroll container widget with scrollbar.
/// </summary>
public struct FlexboxScrollContainerLinks
{
	public ulong ContainerId;    // The viewport scroll container entity (has FlexboxScrollContainer component)
	public ulong ContentAreaId;  // The inner content area (holds all children)
	public ulong ScrollbarId;    // The vertical scrollbar entity
}

/// <summary>
/// Return value from FlexboxScrollContainerWidget.CreateVertical.
/// Provides IDs for accessing the correct container for adding children.
/// </summary>
public struct FlexboxScrollContainerHandle
{
	public ulong WrapperOrContainerId;  // Wrapper (if scrollbar) or container (if no scrollbar)
	public ulong ContentAreaId;          // Where to parent child widgets
	public ulong ScrollbarId;            // Scrollbar entity (0 if no scrollbar)
}

/// <summary>
/// Scrollable container widget for the Flexbox UI system.
/// - Clips its content to the content rect
/// - Responds to PointerScroll events to adjust scroll offset
/// - Optionally includes a draggable scrollbar
/// </summary>
public static class FlexboxScrollContainerWidget
{
	/// <summary>
	/// Create a vertical scroll container of a fixed size with optional scrollbar.
	/// Returns a handle with the content area ID for parenting child widgets.
	/// </summary>
	public static FlexboxScrollContainerHandle CreateVertical(
		Commands commands,
		Vector2 size,
		ulong parent = 0,
		float scrollSpeed = 24f,
		bool includeScrollbar = true)
	{
		ulong wrapperId = 0;
		ulong viewportId = 0;  // The scroll container viewport (when scrollbar enabled)
		ulong contentAreaId;
		ulong scrollbarId = 0;

		if (includeScrollbar)
		{
			// Create horizontal wrapper for content + scrollbar
			wrapperId = commands.Spawn()
				.Insert(new FlexboxNode
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Row,
					PositionType = PositionType.Relative,
					Width = FlexValue.Points(size.X),
					Height = FlexValue.Points(size.Y)
				})
				.Id;

			if (parent != 0)
				commands.Entity(wrapperId).Insert(new FlexboxNodeParent(parent));

			// Create two-layer structure: viewport (fixed height) + content area (auto height)
			// This ensures children can layout beyond the visible area

			// Viewport - fixed height, takes up space in parent layout, clips overflow
			viewportId = commands.Spawn()
				.Insert(new FlexboxNode
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Column,
					FlexGrow = 1f,                       // Take remaining width after scrollbar
					Width = FlexValue.Auto(),            // Let flexbox calculate width
					Height = FlexValue.Points(size.Y),   // Fixed height for layout positioning
					Overflow = Overflow.Scroll,          // Allow scrolling - layouts all children but clips rendering
					BackgroundColor = Vector4.Zero
				})
				.Insert(FlexboxScrollContainer.VerticalOnly(scrollSpeed))
				.Insert(new FlexboxScrollContainerViewport { Height = size.Y }) // Store viewport height for rendering
				.Insert(new FlexboxNodeParent(wrapperId, index: 0))
				.Id;

			// Content area - auto height, can grow beyond viewport
			contentAreaId = commands.Spawn()
				.Insert(new FlexboxNode
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Column,
					JustifyContent = Justify.FlexStart,
					AlignItems = Align.Stretch,
					Width = FlexValue.Percent(100f),     // Match parent width
					Height = FlexValue.Auto(),           // Auto height - grows to fit all children
					BackgroundColor = Vector4.Zero
				})
				.Insert(new FlexboxNodeParent(viewportId, index: 0))  // Child of viewport
				.Id;

			// Create scrollbar
			var scrollbarHandle = FlexboxScrollbarWidget.CreateVertical(
				commands,
				size.Y,
				visibleRatio: 0.5f, // Will be updated by sync system
				style: null,
				parent: 0); // No parent yet
			scrollbarId = scrollbarHandle.ScrollbarId;

			// Position scrollbar as second child
			commands.Entity(scrollbarId)
				.Insert(new FlexboxNodeParent(wrapperId, index: 1));

			// Store links on wrapper for sync system
			commands.Entity(wrapperId).Insert(new FlexboxScrollContainerLinks
			{
				ContainerId = viewportId,      // The viewport scroll container entity
				ContentAreaId = contentAreaId, // The separate content area that can grow
				ScrollbarId = scrollbarId
			});
		}
		else
		{
			// Create two-layer structure: viewport (fixed height) + content area (auto height)
			// This ensures children can layout beyond the visible area

			// Viewport - fixed height, takes up space in parent layout
			viewportId = commands.Spawn()
				.Insert(new FlexboxNode
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Column,
					Width = FlexValue.Points(size.X),
					Height = FlexValue.Points(size.Y),   // Fixed height for layout positioning
					Overflow = Overflow.Hidden,          // Clip children
					BackgroundColor = Vector4.Zero
				})
				.Insert(new FlexboxScrollContainerViewport { Height = size.Y }) // Store viewport height for clipping
				.Id;

			if (parent != 0)
				commands.Entity(viewportId).Insert(new FlexboxNodeParent(parent));

			// Content area - auto height, can grow beyond viewport
			contentAreaId = commands.Spawn()
				.Insert(new FlexboxNode
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Column,
					JustifyContent = Justify.FlexStart,
					AlignItems = Align.Stretch,
					Width = FlexValue.Points(size.X),
					Height = FlexValue.Auto(),           // Auto height - grows to fit all children
					BackgroundColor = Vector4.Zero
				})
				.Insert(FlexboxScrollContainer.VerticalOnly(scrollSpeed))
				.Insert(new FlexboxNodeParent(viewportId, index: 0))  // Child of viewport
				.Id;
		}

		// Handle wheel scroll by adjusting component Offset (per-entity observer)
		// Attach to viewport (or content area for non-scrollbar mode)
		var scrollableEntityId = includeScrollbar ? viewportId : contentAreaId;
		var contentIdForMeasurement = contentAreaId;  // Always use content area for measuring children
		commands.Entity(scrollableEntityId)
			.Observe<On<UiPointerTrigger>, Query<Data<FlexboxScrollContainer>>, Query<Data<Parent>>, Commands, Res<FlexboxUiState>>((trigger, scrollers, parents, cmd, state) =>
			{
				var e = trigger.Event.Event;
				if (e.Type != UiPointerEventType.PointerScroll)
					return;

				if (!scrollers.Contains(trigger.EntityId))
					return;

				// Only process if this is the INNERMOST scroll container
				// Check if there's another scroll container between the target and this entity
				// If the target entity is this entity, process it (direct scroll on viewport)
				if (e.Target != trigger.EntityId)
				{
					// Walk up from target to this entity, checking for intermediate scroll containers
					var current = e.Target;
					while (current != 0 && current != trigger.EntityId)
					{
						// If we find another scroll container on the way up, this is not the innermost one
						if (scrollers.Contains(current))
						{
							return; // Skip - let the inner scroll container handle it
						}

						// Move to parent
						if (!parents.Contains(current))
							break;
						var parentData = parents.Get(current);
						parentData.Deconstruct(out _, out var parentPtr);
						current = parentPtr.Ref.Id;
					}
				}

				// Fetch current scroll container component
				var data = scrollers.Get(trigger.EntityId);
				data.Deconstruct(out var sPtr);
				var sc = sPtr.Ref;

				// Apply scroll (positive wheel up -> negative offset change to move content down)
				var delta = e.ScrollDelta * sc.ScrollSpeed;
				if (!sc.Vertical) delta.Y = 0;
				if (!sc.Horizontal) delta.X = 0;

				var newOffset = sc.Offset - delta; // invert so wheel up scrolls content down

				// Clamp using Flexbox node layouts if available
				// Use content area node's height directly (it has Height=Auto and grows to fit children)
				if (state.Value.TryGetNode(contentIdForMeasurement, out var contentNode) && contentNode != null)
				{
					// Also get viewport node for viewport height
					state.Value.TryGetNode(trigger.EntityId, out var viewportNode);

					// Content area has Height=Auto, so its layout.height is the total content height
					var contentLayout = contentNode.layout;
					float contentHeight = contentLayout.height;
					float viewportH = viewportNode != null ? viewportNode.layout.content.height : contentLayout.content.height;

					var maxScrollY = MathF.Max(0f, contentHeight - viewportH);
					if (maxScrollY <= 0f) newOffset.Y = 0f; else newOffset.Y = System.Math.Clamp(newOffset.Y, 0f, maxScrollY);
				}

				sc.Offset = newOffset;
				cmd.Entity(trigger.EntityId).Insert(sc);
			});

		// Return handle with proper IDs
		return new FlexboxScrollContainerHandle
		{
			WrapperOrContainerId = includeScrollbar ? wrapperId : contentAreaId,
			ContentAreaId = contentAreaId,
			ScrollbarId = scrollbarId
		};
	}
}



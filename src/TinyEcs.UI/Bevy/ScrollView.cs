using System.Numerics;
using TinyEcs.Bevy;
using Flexbox;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// A complete scroll view widget with integrated scrollbars, viewport clipping, and automatic content sizing.
/// Inspired by sickle_ui's ScrollView implementation.
///
/// Structure:
/// - ScrollView (root container)
///   - Viewport (clips content, handles scroll wheel input)
///     - Content (holds user children, positioned via scroll offset)
///   - Scrollbar Container (absolute positioned overlay)
///     - Vertical Scrollbar
///       - Vertical Scrollbar Handle (draggable thumb)
///     - Horizontal Scrollbar
///       - Horizontal Scrollbar Handle (draggable thumb)
/// </summary>
public struct ScrollView
{
	/// <summary>Viewport entity that contains and clips the content</summary>
	public ulong Viewport;

	/// <summary>Content container entity that holds user children</summary>
	public ulong ContentContainer;

	/// <summary>Vertical scrollbar entity (if enabled)</summary>
	public ulong? VerticalScrollBar;

	/// <summary>Vertical scrollbar handle entity (if enabled)</summary>
	public ulong? VerticalScrollBarHandle;

	/// <summary>Horizontal scrollbar entity (if enabled)</summary>
	public ulong? HorizontalScrollBar;

	/// <summary>Horizontal scrollbar handle entity (if enabled)</summary>
	public ulong? HorizontalScrollBarHandle;

	public ScrollView(ulong viewport, ulong contentContainer)
	{
		Viewport = viewport;
		ContentContainer = contentContainer;
		VerticalScrollBar = null;
		VerticalScrollBarHandle = null;
		HorizontalScrollBar = null;
		HorizontalScrollBarHandle = null;
	}
}

/// <summary>
/// Marker component to identify the viewport of a scroll view.
/// The viewport is the visible area that clips the content.
/// </summary>
public struct ScrollViewViewport
{
	public ulong ScrollViewEntity;

	public ScrollViewViewport(ulong scrollViewEntity)
	{
		ScrollViewEntity = scrollViewEntity;
	}
}

/// <summary>
/// Marker component to identify the content container of a scroll view.
/// The content container holds all user children and is positioned via scroll offset.
/// </summary>
public struct ScrollViewContent
{
	public ulong ScrollViewEntity;

	public ScrollViewContent(ulong scrollViewEntity)
	{
		ScrollViewEntity = scrollViewEntity;
	}
}

/// <summary>
/// Component for scrollbar handle/thumb that tracks which scroll view it controls.
/// Used to identify which ScrollView to update when dragging.
/// </summary>
public struct ScrollBarHandle
{
	public ControlOrientation Axis;
	public ulong ScrollViewEntity;

	public ScrollBarHandle(ControlOrientation axis, ulong scrollViewEntity)
	{
		Axis = axis;
		ScrollViewEntity = scrollViewEntity;
	}
}

/// <summary>
/// Plugin that adds scroll view functionality including viewport layout and scrollbar visibility management.
/// </summary>
public struct ScrollViewPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// System 1: Handle scrollbar thumb dragging using Delta from Draggable
		// This mirrors sickle_ui's approach: use drag delta * ratio to update scroll offset
		app.AddSystem((
			Query<Data<Draggable, ScrollBarHandle, ComputedLayout>, Filter<Changed<Draggable>>> draggables,
			Query<Data<ScrollView, ComputedLayout>> scrollViews,
			Query<Data<Scrollable, ComputedLayout>> scrollables,
			Query<Data<ComputedLayout>> contentLayouts,
			Commands commands) =>
		{
			UpdateScrollViewOnDrag(draggables, scrollViews, scrollables, contentLayouts, commands);
		})
			.InStage(Stage.PostUpdate)
			.Label("ui:scrollview:on-drag")
			.After("ui:drag:apply-positions")  // Updated to use new DragPlugin label
			.Build();

		// System 2: Update scrollbar visibility and handle positioning based on content size
		app.AddSystem((
			Query<Data<ScrollView, ComputedLayout>> scrollViews,
			Query<Data<Scrollable, ComputedLayout>> scrollables,
			Query<Data<UiNode, ComputedLayout>> layouts,
			Commands commands) =>
		{
			UpdateScrollViewLayout(scrollViews, scrollables, layouts, commands);
		})
			.InStage(Stage.PostUpdate)
			.Label("ui:scrollview:update-layout")
			.After("ui:scrollview:on-drag")
			.Build();
	}

	/// <summary>
	/// Handles scrollbar thumb dragging using the Delta from Draggable.
	/// Mirrors sickle_ui's approach: uses drag delta multiplied by overflow ratio to update scroll offset.
	/// This way we only update the scroll offset, and let UpdateScrollViewLayout position the thumb.
	/// </summary>
	private static void UpdateScrollViewOnDrag(
		Query<Data<Draggable, ScrollBarHandle, ComputedLayout>, Filter<Changed<Draggable>>> draggables,
		Query<Data<ScrollView, ComputedLayout>> scrollViews,
		Query<Data<Scrollable, ComputedLayout>> scrollables,
		Query<Data<ComputedLayout>> contentLayouts,
		Commands commands)
	{
		foreach (var (_, draggable, barHandle, barLayout) in draggables)
		{
			ref var drag = ref draggable.Ref;
			ref var handle = ref barHandle.Ref;

			// Only process if dragging and has delta
			if (!drag.IsDragging || !drag.Diff.HasValue || drag.Diff.Value == Vector2.Zero)
				continue;

			var scrollViewId = handle.ScrollViewEntity;

			// Get scroll view and its layout
			if (!scrollViews.Contains(scrollViewId))
				continue;

			var (_, scrollView, containerLayout) = scrollViews.Get(scrollViewId);
			ref var view = ref scrollView.Ref;

			// Get the viewport's scrollable + layout
			if (!scrollables.Contains(view.Viewport))
				continue;

			var (_, viewportScrollable, viewportLayout) = scrollables.Get(view.Viewport);
			ref var scroll = ref viewportScrollable.Ref;
			ref var containerLayoutRef = ref viewportLayout.Ref;

			// Get content layout
			if (!contentLayouts.Contains(view.ContentContainer))
				continue;

			var (_, contentLayout) = contentLayouts.Get(view.ContentContainer);
			ref var content = ref contentLayout.Ref;

			// Calculate overflow
			var containerSize = handle.Axis == ControlOrientation.Vertical
				? containerLayoutRef.Height
				: containerLayoutRef.Width;
			var contentSize = handle.Axis == ControlOrientation.Vertical
				? content.Height
				: content.Width;
			var overflow = contentSize - containerSize;

			if (overflow <= 0f)
				continue;

			// Calculate ratio: overflow / remaining_space
			// The thumb moves in remainingSpace, content scrolls by overflow
			var barSize = handle.Axis == ControlOrientation.Vertical
				? barLayout.Ref.Height
				: barLayout.Ref.Width;
			var remainingSpace = containerSize - barSize;

			if (remainingSpace <= 0f)
				continue;

			var ratio = overflow / remainingSpace;

			// Apply delta * ratio to scroll offset (this is the key insight from sickle!)
			var deltaComponent = handle.Axis == ControlOrientation.Vertical
				? drag.Diff.Value.Y
				: drag.Diff.Value.X;
			var scrollDelta = deltaComponent * ratio;


			// Update scroll offset
			if (handle.Axis == ControlOrientation.Vertical)
			{
				scroll.ScrollOffset = new Vector2(scroll.ScrollOffset.X, scroll.ScrollOffset.Y + scrollDelta);
			}
			else
			{
				scroll.ScrollOffset = new Vector2(scroll.ScrollOffset.X + scrollDelta, scroll.ScrollOffset.Y);
			}

			// Re-insert to trigger change detection
			// Update the viewport's Scrollable component to reflect new offset
			commands.Entity(view.Viewport).Insert(scroll);
		}
	}

	/// <summary>
	/// Updates scrollbar visibility and positions scrollbar handles based on content size and scroll offset.
	/// Hides scrollbars when content fits within the viewport.
	/// </summary>
	private static void UpdateScrollViewLayout(
		Query<Data<ScrollView, ComputedLayout>> scrollViews,
		Query<Data<Scrollable, ComputedLayout>> scrollables,
		Query<Data<UiNode, ComputedLayout>> layouts,
		Commands commands)
	{
		foreach (var (entityId, scrollView, scrollViewLayout) in scrollViews)
		{
			ref var view = ref scrollView.Ref;
			ref var viewLayout = ref scrollViewLayout.Ref;

			// Use viewport's scrollable and layout for sizing/offsets
			if (!scrollables.Contains(view.Viewport))
				continue;

			var (_, viewportScrollable, viewportLayout) = scrollables.Get(view.Viewport);
			ref var scroll = ref viewportScrollable.Ref;

			var containerWidth = viewportLayout.Ref.Width;
			var containerHeight = viewportLayout.Ref.Height;


			if (containerWidth <= 0f || containerHeight <= 0f)
				continue;

			var contentWidth = scroll.ContentSize.X;
			var contentHeight = scroll.ContentSize.Y;

			// Update vertical scrollbar
			if (view.VerticalScrollBar.HasValue && view.VerticalScrollBarHandle.HasValue)
			{
				var verticalBarId = view.VerticalScrollBar.Value;
				var verticalHandleId = view.VerticalScrollBarHandle.Value;

				// Check if vertical scrolling is needed
				if (containerHeight >= contentHeight || containerHeight <= 5f)
				{
					// Hide vertical scrollbar by setting display to None
					if (layouts.Contains(verticalBarId))
					{
						var (_, barNode, _) = layouts.Get(verticalBarId);
						ref var node = ref barNode.Ref;
						node.Display = Display.None;
					}
				}
				else
				{
					// Show vertical scrollbar
					if (layouts.Contains(verticalBarId))
					{
						var (_, barNode, barLayout) = layouts.Get(verticalBarId);
						ref var node = ref barNode.Ref;
						node.Display = Display.Flex;
					}

					// Update handle size and position
					if (layouts.Contains(verticalHandleId))
					{
						var (_, handleNode, handleLayout) = layouts.Get(verticalHandleId);
						var (_, _, railLayout) = layouts.Get(verticalBarId);

						var overflow = contentHeight - containerHeight;
						var visibleRatio = Math.Clamp(containerHeight / contentHeight, 0f, 1f);
						var handleHeight = Math.Clamp(visibleRatio * containerHeight, 20f, containerHeight);
						var remainingSpace = containerHeight - handleHeight;
						var scrollRatio = overflow > 0f ? Math.Clamp(scroll.ScrollOffset.Y / overflow, 0f, 1f) : 0f;
						var handleOffset = scrollRatio * remainingSpace;

						ref var node = ref handleNode.Ref;
						node.Height = FlexValue.Points(handleHeight);
						node.Width = FlexValue.Points(railLayout.Ref.Width);
						node.Left = FlexValue.Points(0f);
						node.Top = FlexValue.Points(handleOffset);

						// Ensure change is written back for sync
						commands.Entity(verticalHandleId).Insert(node);
					}
				}
			}

			// Update horizontal scrollbar
			if (view.HorizontalScrollBar.HasValue && view.HorizontalScrollBarHandle.HasValue)
			{
				var horizontalBarId = view.HorizontalScrollBar.Value;
				var horizontalHandleId = view.HorizontalScrollBarHandle.Value;

				// Check if horizontal scrolling is needed
				if (containerWidth >= contentWidth || containerWidth <= 5f)
				{
					// Hide horizontal scrollbar
					if (layouts.Contains(horizontalBarId))
					{
						var (_, barNode, _) = layouts.Get(horizontalBarId);
						ref var node = ref barNode.Ref;
						node.Display = Display.None;
					}
				}
				else
				{
					// Show horizontal scrollbar
					if (layouts.Contains(horizontalBarId))
					{
						var (_, barNode, barLayout) = layouts.Get(horizontalBarId);
						ref var node = ref barNode.Ref;
						node.Display = Display.Flex;
					}

					// Update handle size and position
					if (layouts.Contains(horizontalHandleId))
					{
						var (_, handleNode, handleLayout) = layouts.Get(horizontalHandleId);
						var (_, _, railLayout) = layouts.Get(horizontalBarId);

						var overflow = contentWidth - containerWidth;
						var visibleRatio = Math.Clamp(containerWidth / contentWidth, 0f, 1f);
						var handleWidth = Math.Clamp(visibleRatio * containerWidth, 20f, containerWidth);
						var remainingSpace = containerWidth - handleWidth;
						var scrollRatio = overflow > 0f ? Math.Clamp(scroll.ScrollOffset.X / overflow, 0f, 1f) : 0f;
						var handleOffset = scrollRatio * remainingSpace;

						ref var node = ref handleNode.Ref;
						node.Width = FlexValue.Points(handleWidth);
						node.Height = FlexValue.Points(railLayout.Ref.Height);
						node.Top = FlexValue.Points(0f);
						node.Left = FlexValue.Points(handleOffset);

						// Ensure change is written back for sync
						commands.Entity(horizontalHandleId).Insert(node);
					}
				}
			}
		}
	}
}

/// <summary>
/// Helper methods for creating scroll views.
/// </summary>
public static class ScrollViewHelpers
{
	/// <summary>
	/// Creates a complete scroll view with optional horizontal and vertical scrollbars.
	/// </summary>
	/// <param name="commands">Entity commands for spawning entities</param>
	/// <param name="enableVertical">Enable vertical scrolling and scrollbar</param>
	/// <param name="enableHorizontal">Enable horizontal scrolling and scrollbar</param>
	/// <param name="width">Width of the scroll view</param>
	/// <param name="height">Height of the scroll view</param>
	/// <param name="scrollbarWidth">Width of scrollbars in pixels (default: 12)</param>
	/// <returns>Tuple of (scrollViewId, contentId) - use contentId as parent for user content</returns>
	public static (ulong scrollViewId, ulong contentId) CreateScrollView(
		Commands commands,
		bool enableVertical = true,
		bool enableHorizontal = false,
		FlexValue? width = null,
		FlexValue? height = null,
		float scrollbarWidth = 12f)
	{
		width ??= FlexValue.Percent(100f);
		height ??= FlexValue.Percent(100f);

		// Create root scroll view container
		var scrollViewId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = width.Value,
				Height = height.Value,
				Display = Display.Flex,
				FlexDirection = FlexDirection.Column
			})
			.Id;

		// Create viewport (clips content)
		var viewportId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Percent(100f),
				Height = FlexValue.Percent(100f),
				PositionType = PositionType.Absolute,
				Overflow = Overflow.Hidden,
				AlignItems = Align.FlexStart, // Left-align content container (don't center it)
				JustifyContent = Justify.FlexStart // Top-align content container
			})
			.Insert(new Scrollable
			{
				EnableVertical = enableVertical,
				EnableHorizontal = enableHorizontal,
				ScrollSpeed = 20f
			})
			.Insert(new ScrollViewViewport(scrollViewId))
			.Id;

		// Create content container (holds user children)
		var contentId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Auto(),
				Height = FlexValue.Auto(),
				MinWidth = FlexValue.Percent(100f),
				MinHeight = FlexValue.Percent(100f),
				Display = Display.Flex,
				FlexDirection = FlexDirection.Column,
				AlignSelf = Align.FlexStart, // Ensure content starts at top-left, not centered
				PaddingRight = enableVertical ? FlexValue.Points(scrollbarWidth) : FlexValue.Points(0f),
				PaddingBottom = enableHorizontal ? FlexValue.Points(scrollbarWidth) : FlexValue.Points(0f)
			})
			.Insert(new ScrollViewContent(scrollViewId))
			.Id;

		// Create scrollbar container (absolute positioned overlay)
		var scrollbarContainerId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Percent(100f),
				Height = FlexValue.Percent(100f),
				PositionType = PositionType.Absolute,
				Display = Display.Flex,
				JustifyContent = Justify.FlexEnd
			})
			.Id;

		ulong? verticalBarId = null;
		ulong? verticalHandleId = null;
		ulong? horizontalBarId = null;
		ulong? horizontalHandleId = null;

		// Create vertical scrollbar
		if (enableVertical)
		{
			verticalBarId = commands.Spawn()
				.Insert(new UiNode
				{
					Width = FlexValue.Points(scrollbarWidth),
					Height = FlexValue.Percent(100f),
					PositionType = PositionType.Absolute,
					Right = FlexValue.Points(0f),
					Display = Display.Flex,
					FlexDirection = FlexDirection.Column
				})
				.Insert(BackgroundColor.FromRgba(100, 100, 100, 180))
				.Id;

			verticalHandleId = commands.Spawn()
				.Insert(new UiNode
				{
					Width = FlexValue.Percent(100f),
					Height = FlexValue.Points(40f),
					PositionType = PositionType.Absolute
				})
				.Insert(BackgroundColor.FromRgba(180, 180, 180, 220))
				.Insert(new ScrollbarThumb())
				.Insert(new ScrollBarHandle(ControlOrientation.Vertical, scrollViewId))
				.Insert(new Draggable())
				.Insert(new Interactive())
				.Id;

			// Set up scrollbar component
			commands.Entity(verticalBarId.Value).Insert(new Scrollbar(viewportId, ControlOrientation.Vertical, 20f));

			// Set up hierarchy
			commands.Entity(verticalBarId.Value).AddChild(verticalHandleId.Value);
			commands.Entity(scrollbarContainerId).AddChild(verticalBarId.Value);
		}

		// Create horizontal scrollbar
		if (enableHorizontal)
		{
			horizontalBarId = commands.Spawn()
				.Insert(new UiNode
				{
					Width = FlexValue.Percent(100f),
					Height = FlexValue.Points(scrollbarWidth),
					PositionType = PositionType.Absolute,
					Bottom = FlexValue.Points(0f),
					Display = Display.Flex,
					FlexDirection = FlexDirection.Row
				})
				.Insert(BackgroundColor.FromRgba(100, 100, 100, 180))
				.Id;

			horizontalHandleId = commands.Spawn()
				.Insert(new UiNode
				{
					Width = FlexValue.Points(40f),
					Height = FlexValue.Percent(100f),
					PositionType = PositionType.Absolute
				})
				.Insert(BackgroundColor.FromRgba(180, 180, 180, 220))
				.Insert(new ScrollbarThumb())
				.Insert(new ScrollBarHandle(ControlOrientation.Horizontal, scrollViewId))
				.Insert(new Draggable())
				.Insert(new Interactive())
				.Id;

			// Set up scrollbar component
			commands.Entity(horizontalBarId.Value).Insert(new Scrollbar(viewportId, ControlOrientation.Horizontal, 20f));

			// Set up hierarchy
			commands.Entity(horizontalBarId.Value).AddChild(horizontalHandleId.Value);
			commands.Entity(scrollbarContainerId).AddChild(horizontalBarId.Value);
		}

		// Set up ScrollView component
		commands.Entity(scrollViewId).Insert(new ScrollView(viewportId, contentId)
		{
			VerticalScrollBar = verticalBarId,
			VerticalScrollBarHandle = verticalHandleId,
			HorizontalScrollBar = horizontalBarId,
			HorizontalScrollBarHandle = horizontalHandleId
		});

		// Set up main hierarchy
		commands.Entity(scrollViewId).AddChild(viewportId);
		commands.Entity(scrollViewId).AddChild(scrollbarContainerId);
		commands.Entity(viewportId).AddChild(contentId);

		return (scrollViewId, contentId);
	}
}

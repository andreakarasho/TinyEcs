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
/// Plugin that adds scroll view functionality including viewport layout and scrollbar visibility management.
/// </summary>
public struct ScrollViewPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// System to update scrollbar visibility and handle positioning based on content size
		app.AddSystem((
			Query<Data<ScrollView, Scrollable, ComputedLayout>> scrollViews,
			Query<Data<UiNode, ComputedLayout>> layouts,
			Commands commands) =>
		{
			UpdateScrollViewLayout(scrollViews, layouts, commands);
		})
		.InStage(Stage.PostUpdate)
		.Label("ui:scrollview:update-layout")
		.After("ui:scrollbar:update-thumbs")
		.Build();
	}

	/// <summary>
	/// Updates scrollbar visibility and positions scrollbar handles based on content size and scroll offset.
	/// Hides scrollbars when content fits within the viewport.
	/// </summary>
	private static void UpdateScrollViewLayout(
		Query<Data<ScrollView, Scrollable, ComputedLayout>> scrollViews,
		Query<Data<UiNode, ComputedLayout>> layouts,
		Commands commands)
	{
		foreach (var (entityId, scrollView, scrollable, scrollViewLayout) in scrollViews)
		{
			ref var view = ref scrollView.Ref;
			ref var scroll = ref scrollable.Ref;
			ref var viewLayout = ref scrollViewLayout.Ref;

			var containerWidth = viewLayout.Width;
			var containerHeight = viewLayout.Height;

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
						var (_, barNode, _) = layouts.Get(verticalBarId);
						ref var node = ref barNode.Ref;
						node.Display = Display.Flex;
					}

					// Update handle size and position
					if (layouts.Contains(verticalHandleId))
					{
						var (_, handleNode, handleLayout) = layouts.Get(verticalHandleId);

						var overflow = contentHeight - containerHeight;
						var visibleRatio = Math.Clamp(containerHeight / contentHeight, 0f, 1f);
						var handleHeight = Math.Clamp(visibleRatio * containerHeight, 20f, containerHeight);
						var remainingSpace = containerHeight - handleHeight;
						var scrollRatio = overflow > 0f ? Math.Clamp(scroll.ScrollOffset.Y / overflow, 0f, 1f) : 0f;
						var handleOffset = scrollRatio * remainingSpace;

						ref var node = ref handleNode.Ref;
						node.Height = FlexValue.Points(handleHeight);
						node.Top = FlexValue.Points(handleOffset);
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
						var (_, barNode, _) = layouts.Get(horizontalBarId);
						ref var node = ref barNode.Ref;
						node.Display = Display.Flex;
					}

					// Update handle size and position
					if (layouts.Contains(horizontalHandleId))
					{
						var (_, handleNode, handleLayout) = layouts.Get(horizontalHandleId);

						var overflow = contentWidth - containerWidth;
						var visibleRatio = Math.Clamp(containerWidth / contentWidth, 0f, 1f);
						var handleWidth = Math.Clamp(visibleRatio * containerWidth, 20f, containerWidth);
						var remainingSpace = containerWidth - handleWidth;
						var scrollRatio = overflow > 0f ? Math.Clamp(scroll.ScrollOffset.X / overflow, 0f, 1f) : 0f;
						var handleOffset = scrollRatio * remainingSpace;

						ref var node = ref handleNode.Ref;
						node.Width = FlexValue.Points(handleWidth);
						node.Left = FlexValue.Points(handleOffset);
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
			.Insert(new Scrollable
			{
				EnableVertical = enableVertical,
				EnableHorizontal = enableHorizontal,
				ScrollSpeed = 20f
			})
			.Id;

		// Create viewport (clips content)
		var viewportId = commands.Spawn()
			.Insert(new UiNode
			{
				Width = FlexValue.Percent(100f),
				Height = FlexValue.Percent(100f),
				PositionType = PositionType.Absolute,
				Overflow = Overflow.Hidden
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
			.Insert(new ZIndex(1))
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
				.Id;

			// Set up scrollbar component
			commands.Entity(verticalBarId.Value).Insert(new Scrollbar(scrollViewId, ControlOrientation.Vertical, 20f));

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
				.Id;

			// Set up scrollbar component
			commands.Entity(horizontalBarId.Value).Insert(new Scrollbar(scrollViewId, ControlOrientation.Horizontal, 20f));

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

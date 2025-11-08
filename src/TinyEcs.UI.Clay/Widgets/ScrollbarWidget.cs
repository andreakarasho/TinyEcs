using System;
using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Component to track scrollbar state.
/// </summary>
public struct ScrollbarState
{
	public ulong ContainerEntityId;   // Container element (for visibility)
	public ulong ContentAreaEntityId; // Content area to scroll
	public ulong ViewportEntityId;    // Viewport (visible area) for mouse wheel detection
	public ulong TrackEntityId;
	public ulong SpacerEntityId;      // Spacer used to position the thumb
	public ulong ThumbEntityId;
	public float ScrollPosition;      // Current scroll position (0-1)
	public float ViewportSize;        // Size of viewport relative to content (0-1)
	public float ContentSize;         // Total content size in pixels
	public float VisibleSize;         // Visible viewport size in pixels
	public bool IsDragging;
	public float DragStartY;          // Mouse Y position when drag started
	public float DragStartScroll;     // Scroll position when drag started
	public bool IsHorizontal;         // true for horizontal, false for vertical
}

/// <summary>
/// Marker component to update scrollbar thumb size.
/// </summary>
public struct ScrollbarThumbUpdate
{
	public float Size;         // 0-1
}

/// <summary>
/// Marker component to update scrollbar spacer (thumb position).
/// </summary>
public struct ScrollbarSpacerUpdate
{
	public float Position;     // 0-1
}

/// <summary>
/// Marker component to update scrollbar visibility.
/// </summary>
public struct ScrollbarVisibilityUpdate
{
	public bool IsVisible;
}

/// <summary>
/// Event fired when scrollbar position changes.
/// </summary>
public struct ScrollbarScrolled
{
	public float ScrollPosition;  // 0-1
	public float ScrollPixels;    // Actual pixel offset
}

/// <summary>
/// Extension methods for creating scrollbar widgets.
/// </summary>
public static class ScrollbarWidget
{
	/// <summary>
	/// Creates a vertical scrollbar widget with track and draggable thumb using theme colors.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the scrollbar to</param>
	/// <param name="theme">Theme resource for styling</param>
	/// <param name="viewportEntityId">Entity ID of the viewport (visible area) for mouse wheel detection</param>
	/// <param name="contentAreaEntityId">Entity ID of the content area to scroll</param>
	/// <param name="contentSize">Total size of scrollable content in pixels</param>
	/// <param name="visibleSize">Size of visible viewport in pixels</param>
	/// <param name="initialScroll">Initial scroll position (0-1, default 0)</param>
	/// <returns>The scrollbar container entity ID</returns>
	/// <remarks>
	/// The content area entity must have a ClayScrollContainer component for scrolling to work.
	/// ScrollbarScrolled events are automatically emitted and handled by the ScrollbarPlugin.
	///
	/// Call UpdateScrollbar() to update thumb size when content/viewport changes.
	/// </remarks>
	public static ulong CreateVerticalScrollbar(
		this Commands commands,
		EntityCommands parent,
		ClayTheme theme,
		ulong viewportEntityId,
		ulong contentAreaEntityId,
		float contentSize,
		float visibleSize,
		float initialScroll = 0f)
	{
		return CreateScrollbar(commands, parent, theme, viewportEntityId, contentAreaEntityId, contentSize, visibleSize, initialScroll, isHorizontal: false);
	}

	/// <summary>
	/// Creates a horizontal scrollbar widget with track and draggable thumb using theme colors.
	/// </summary>
	public static ulong CreateHorizontalScrollbar(
		this Commands commands,
		EntityCommands parent,
		ClayTheme theme,
		ulong viewportEntityId,
		ulong contentAreaEntityId,
		float contentSize,
		float visibleSize,
		float initialScroll = 0f)
	{
		return CreateScrollbar(commands, parent, theme, viewportEntityId, contentAreaEntityId, contentSize, visibleSize, initialScroll, isHorizontal: true);
	}

	private static ulong CreateScrollbar(
		Commands commands,
		EntityCommands parent,
		ClayTheme theme,
		ulong viewportEntityId,
		ulong contentAreaEntityId,
		float contentSize,
		float visibleSize,
		float initialScroll,
		bool isHorizontal)
	{
		var scrollbarTheme = theme.Scrollbar;

		// Calculate viewport size as percentage of content
		float viewportSize = Math.Clamp(visibleSize / Math.Max(contentSize, 1f), 0.01f, 1f);
		float scrollPosition = Math.Clamp(initialScroll, 0f, 1f);

		// Check if scrollbar should be visible (content larger than viewport)
		bool isVisible = contentSize > visibleSize;
		float scrollbarSize = isVisible ? scrollbarTheme.Size : 0f;

		// Container for the scrollbar (track + thumb)
		// Uses normal layout flow as a sibling to the content
		var containerBuilder = ClayNode.Configure();

		if (isHorizontal)
		{
			containerBuilder = containerBuilder.WidthGrow().Height(scrollbarSize).Row();
		}
		else
		{
			containerBuilder = containerBuilder.Width(scrollbarSize).HeightGrow().Column();
		}

		containerBuilder = containerBuilder
			.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP);

		var containerNode = containerBuilder.Build();

		var container = commands.SpawnClayElement(containerNode);
		parent.AddChild(container);

		// Track (background)
		var trackBuilder = ClayNode.Configure()
			.WidthGrow()
			.HeightGrow()
			.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP)
			.Background(scrollbarTheme.TrackColor)
			.CornerRadius(scrollbarTheme.CornerRadius);

		if (isHorizontal)
		{
			trackBuilder.Row();
		}
		else
		{
			trackBuilder.Column();
		}

		var trackNode = trackBuilder.Build();

		var track = commands.SpawnClayElement(trackNode);
		container.AddChild(track);

		// Spacer before thumb (for positioning)
		var spacerBuilder = ClayNode.Configure();
		var spacerSize = scrollPosition * (1f - viewportSize);

		if (isHorizontal)
		{
			spacerBuilder = spacerBuilder.WidthPercent(spacerSize).HeightGrow();
		}
		else
		{
			spacerBuilder = spacerBuilder.WidthGrow().HeightPercent(spacerSize);
		}

		var spacerNode = spacerBuilder.Build();

		var spacer = commands.SpawnClayElement(spacerNode);
		track.AddChild(spacer);

		// Thumb (the draggable part)
		var thumbBuilder = ClayNode.Configure();

		if (isHorizontal)
		{
			thumbBuilder = thumbBuilder.WidthPercent(viewportSize).HeightGrow();
		}
		else
		{
			thumbBuilder = thumbBuilder.WidthGrow().HeightPercent(viewportSize);
		}

		thumbBuilder = thumbBuilder
			.Background(scrollbarTheme.ThumbColor)
			.CornerRadius((ushort)(scrollbarTheme.CornerRadius > 2 ? scrollbarTheme.CornerRadius - 2 : 0));

		var thumbNode = thumbBuilder.Build();

		var thumb = commands.SpawnClayElement(thumbNode);
		track.AddChild(thumb);

		// Add scrollbar state component
		commands.Entity(container.Id).Insert(new ScrollbarState
		{
			ContainerEntityId = container.Id,
			ContentAreaEntityId = contentAreaEntityId,
			ViewportEntityId = viewportEntityId,
			TrackEntityId = track.Id,
			SpacerEntityId = spacer.Id,
			ThumbEntityId = thumb.Id,
			ScrollPosition = scrollPosition,
			ViewportSize = viewportSize,
			ContentSize = contentSize,
			VisibleSize = visibleSize,
			IsDragging = false,
			DragStartY = 0f,
			DragStartScroll = 0f,
			IsHorizontal = isHorizontal
		});

		// Capture container ID for use in observer closure
		var containerId = container.Id;

		// Add pointer observers for interaction
		container.Observe<On<ClayPointerEvent>, Commands, Query<Data<ScrollbarState>>, Query<Data<ClayComputedLayout>>>((trigger, cmd, stateQuery, layoutQuery) =>
		{
			var evt = trigger.Event;

			// Use the container ID (where ScrollbarState is stored)
			if (!stateQuery.Contains(containerId))
			{
				return;
			}

			// Stop propagation - we're handling this event
			trigger.Propagate(false);

			var (_, statePtr) = stateQuery.Get(containerId);
			var state = statePtr.Ref;

			// Get the container layout to convert coordinates
			var (_, containerLayoutPtr) = layoutQuery.Get(containerId);
			var containerLayout = containerLayoutPtr.Ref;

			// Get the track layout to calculate positions
			var (_, trackLayoutPtr) = layoutQuery.Get(state.TrackEntityId);
			var trackLayout = trackLayoutPtr.Ref;

			// Get the thumb layout for size calculations
			var (_, thumbLayoutPtr) = layoutQuery.Get(state.ThumbEntityId);
			var thumbLayout = thumbLayoutPtr.Ref;

			if (evt.EventType == ClayPointerEventType.Pressed)
			{
				// Check if clicking on thumb to start drag
				var thumbPos = state.IsHorizontal ? thumbLayout.X : thumbLayout.Y;
				var thumbSize = state.IsHorizontal ? thumbLayout.Width : thumbLayout.Height;
				var mousePos = state.IsHorizontal ? evt.LocalPosition.X : evt.LocalPosition.Y;
				var trackStart = state.IsHorizontal ? trackLayout.X : trackLayout.Y;
				var trackLocalMouse = mousePos + (state.IsHorizontal ? trackLayout.X : trackLayout.Y);

				// If clicking on thumb, start drag
				if (trackLocalMouse >= thumbPos && trackLocalMouse <= thumbPos + thumbSize)
				{
					state.IsDragging = true;
					state.DragStartY = mousePos;
					state.DragStartScroll = state.ScrollPosition;
					cmd.Entity(containerId).Insert(state);
				}
				else
				{
					// Clicking on track - jump to that position and enter dragging mode
					var trackSize = state.IsHorizontal ? trackLayout.Width : trackLayout.Height;
					var trackLocalPos = mousePos;

					// Calculate the scroll position based on click
					// Account for thumb size - click should center thumb at click position
					var maxScrollableArea = trackSize * (1f - state.ViewportSize);
					var targetThumbPos = trackLocalPos - (thumbSize / 2f);
					var normalized = Math.Clamp(targetThumbPos / maxScrollableArea, 0f, 1f);

					state.ScrollPosition = normalized;
					state.IsDragging = true;  // Enter dragging mode so user can continue dragging
					cmd.Entity(containerId).Insert(state);
					UpdateScrollbarVisuals(cmd, state, containerId);
				}
			}
			else if (evt.EventType == ClayPointerEventType.Move && state.IsDragging)
			{
				// Update scroll position based on current mouse position (not delta)
				// This allows continuous dragging even when mouse leaves thumb bounds
				var trackSize = state.IsHorizontal ? trackLayout.Width : trackLayout.Height;
				var thumbSize = state.IsHorizontal ? thumbLayout.Width : thumbLayout.Height;

				// Convert mouse position to track-relative coordinates
				var containerPos = state.IsHorizontal ? containerLayout.X : containerLayout.Y;
				var trackPos = state.IsHorizontal ? trackLayout.X : trackLayout.Y;
				var trackOffset = trackPos - containerPos;
				var mousePos = state.IsHorizontal ? evt.LocalPosition.X : evt.LocalPosition.Y;
				var trackLocalPos = mousePos - trackOffset;

				// Calculate scroll position directly from track-relative mouse position
				// Account for thumb size - the scrollable area is reduced by thumb size
				var maxScrollableArea = trackSize * (1f - state.ViewportSize);

				if (maxScrollableArea > 0)
				{
					// Center the thumb at mouse position (subtract half thumb size)
					var targetThumbPos = trackLocalPos - (thumbSize / 2f);
					state.ScrollPosition = Math.Clamp(targetThumbPos / maxScrollableArea, 0f, 1f);
				}

				cmd.Entity(containerId).Insert(state);
				UpdateScrollbarVisuals(cmd, state, containerId);
			}
			else if (evt.EventType == ClayPointerEventType.Released)
			{
				state.IsDragging = false;
				cmd.Entity(containerId).Insert(state);
			}
		});

		return container.Id;
	}

	/// <summary>
	/// Updates scrollbar thumb size and position based on content/viewport changes.
	/// </summary>
	/// <param name="commands">Commands</param>
	/// <param name="scrollbarId">The scrollbar entity ID</param>
	/// <param name="contentSize">New content size in pixels</param>
	/// <param name="visibleSize">New visible viewport size in pixels</param>
	public static void UpdateScrollbar(this Commands commands, ulong scrollbarId, float contentSize, float visibleSize)
	{
		// This will be processed by a system that queries ScrollbarState
		// For now, we'll emit an update event that the plugin system will handle
		commands.Entity(scrollbarId).Insert(new ScrollbarContentUpdate
		{
			ContentSize = contentSize,
			VisibleSize = visibleSize
		});
	}

	private static void UpdateScrollbarVisuals(Commands commands, ScrollbarState state, ulong containerId)
	{
		// Update spacer size to position the thumb
		// Spacer size = scrollPosition * (1 - viewportSize)
		var spacerSize = state.ScrollPosition * (1f - state.ViewportSize);
		commands.Entity(state.SpacerEntityId).Insert(new ScrollbarSpacerUpdate
		{
			Position = spacerSize
		});

		// Update thumb size (in case viewport changed)
		commands.Entity(state.ThumbEntityId).Insert(new ScrollbarThumbUpdate
		{
			Size = state.ViewportSize
		});

		// Calculate actual pixel offset
		var maxScrollPixels = Math.Max(0f, state.ContentSize - state.VisibleSize);
		var scrollPixels = state.ScrollPosition * maxScrollPixels;

		// Emit event for the scroll change
		commands.Entity(containerId).EmitTrigger(new ScrollbarScrolled
		{
			ScrollPosition = state.ScrollPosition,
			ScrollPixels = scrollPixels
		});
	}
}

/// <summary>
/// Marker component to update scrollbar content size.
/// </summary>
public struct ScrollbarContentUpdate
{
	public float ContentSize;
	public float VisibleSize;
}

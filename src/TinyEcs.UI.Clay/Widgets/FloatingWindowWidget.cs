using System;
using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Resize edge for floating window.
/// </summary>
public enum ResizeEdge
{
	None,
	Right,
	Bottom,
	BottomRight
}

/// <summary>
/// Component to track floating window state for dragging, resizing, and z-ordering.
/// </summary>
public struct FloatingWindowState
{
	public ulong TitleBarEntityId;
	public ulong CloseButtonEntityId;
	public ulong ContentWrapperEntityId;  // Viewport for scrolling
	public ulong ContentAreaEntityId;     // Actual scrollable content
	public ulong VerticalScrollbarId;
	public ulong ResizeRightEntityId;
	public ulong ResizeBottomEntityId;
	public ulong ResizeCornerEntityId;
	public string Title;
	public bool IsDragging;
	public bool IsResizing;
	public ResizeEdge ResizingEdge;
	public float DragStartX;
	public float DragStartY;
	public float InitialX;
	public float InitialY;
	public float InitialWidth;
	public float InitialHeight;
	public float MinWidth;
	public float MinHeight;
	public float ContentWidth;   // Track content size for scrollbar updates
	public float ContentHeight;
	public Clay_Color TitleBarColor;
	public Clay_Color TitleTextColor;
	public Clay_Color BackgroundColor;
	public Clay_Color BorderColor;
	public Clay_Color CloseButtonColor;
	public Clay_Color CloseButtonHoverColor;
	public Clay_Color ResizeHandleColor;
}

/// <summary>
/// Event fired when a window is clicked (for bringing to front).
/// </summary>
public struct WindowClicked
{
}

/// <summary>
/// Event fired when close button is clicked.
/// </summary>
public struct WindowCloseRequested
{
}

/// <summary>
/// Extension methods for creating floating window widgets.
/// </summary>
public static class FloatingWindowWidget
{
	/// <summary>
	/// Creates a floating window widget with title bar, close button, and content area using theme colors.
	/// The window is draggable via the title bar, resizable via the corner handle, and can be closed via the close button.
	/// </summary>
	/// <param name="commands">Commands for entity creation</param>
	/// <param name="parent">Parent entity to attach the window to</param>
	/// <param name="theme">Theme resource for styling</param>
	/// <param name="title">Window title</param>
	/// <param name="x">Initial X position</param>
	/// <param name="y">Initial Y position</param>
	/// <param name="width">Window width</param>
	/// <param name="height">Window height</param>
	/// <param name="minWidth">Minimum window width (default 150)</param>
	/// <param name="minHeight">Minimum window height (default 100)</param>
	/// <returns>The content area entity commands for adding children</returns>
	public static EntityCommands CreateFloatingWindow(
		this Commands commands,
		EntityCommands parent,
		ClayTheme theme,
		string title,
		float x,
		float y,
		float width,
		float height,
		float minWidth = 150f,
		float minHeight = 100f)
	{
		var windowTheme = theme.FloatingWindow;

		// Window container - floating positioned
		var windowNode = ClayNode.Configure()
			.Size(width, height)
			.Column()
			.Gap(0)
			.Background(windowTheme.BackgroundColor)
			.CornerRadius(windowTheme.CornerRadius)
			.Border(windowTheme.BorderColor, windowTheme.BorderWidth)
			.Floating(100)
			.FloatingOffset(x, y)
			.Build();

		var window = commands.SpawnClayElement(windowNode);
		parent.AddChild(window);

		// Title bar (draggable area)
		var titleBarNode = ClayNode.Configure()
			.WidthGrow()
			.Height(windowTheme.TitleBarHeight)
			.Row()
			.Padding((ushort)windowTheme.TitleBarPadding)
			.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
			.Background(windowTheme.TitleBarColor)
			.CornerRadius(windowTheme.CornerRadius, windowTheme.CornerRadius, 0, 0)
			.Build();

		var titleBar = commands.SpawnClayElement(titleBarNode);
		window.AddChild(titleBar);

		// Title text
		var titleTextNode = ClayNode.Configure()
			.WidthGrow()
			.HeightGrow()
			.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
			.Text(title, theme.Typography.DefaultFontSize, windowTheme.TitleTextColor)
			.Build();

		var titleText = commands.SpawnClayElement(titleTextNode);
		titleBar.AddChild(titleText);

		// Close button
		var closeButtonNode = ClayNode.Configure()
			.Width(20)
			.Height(20)
			.Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
			.Background(windowTheme.CloseButtonColor)
			.CornerRadius(4)
			.Text("x", 18, windowTheme.TitleTextColor)
			.Build();

		var closeButton = commands.SpawnClayElement(closeButtonNode);
		titleBar.AddChild(closeButton);

		// Content + resize borders layout structure:
		// Root (column): title bar, middle row, bottom border container
		//   Middle row (row): content area, right border
		//     Content area: user content
		//     Right border: vertical resize handle
		//   Bottom border container (row): bottom border, corner
		//     Bottom border: horizontal resize handle
		//     Corner: bottom-right diagonal resize handle

		var resizeBorderWidth = 8f;
		var resizeBorderColor = windowTheme.BackgroundColor;

		// Middle row container (content + right border)
		var middleRowNode = ClayNode.Configure()
			.WidthGrow()
			.HeightGrow()
			.Row()
			.Gap(0)
			.Padding((ushort)resizeBorderWidth, 0, 0, (ushort)resizeBorderWidth) // Left and top margins
			.Build();

		var middleRow = commands.SpawnClayElement(middleRowNode);
		window.AddChild(middleRow);

		// Content area - scrollable panel structure
		// Create content wrapper (viewport - visible area)
		var contentWrapperNode = ClayNode.Default with
		{
			Layout = new Clay_LayoutConfig
			{
				sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
				layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM
			},
			Clip = new Clay_ClipElementConfig
			{
				horizontal = false,
				vertical = true,
				childOffset = new Clay_Vector2 { x = 0, y = 0 }
			}
		};

		var contentWrapper = commands.SpawnClayElement(contentWrapperNode);
		middleRow.AddChild(contentWrapper);

		// Create the actual scrollable content area
		var contentNode2 = ClayNode.Configure()
			.WidthFit()
			.HeightFit()
			.Column()
			.Padding((ushort)windowTheme.ContentPadding)
			.Gap(8)
			.Build();

		var contentArea = commands.SpawnClayElement(contentNode2);
		contentWrapper.AddChild(contentArea);

		// Add scroll container component
		commands.Entity(contentArea.Id).Insert(new ClayScrollContainer
		{
			ScrollOffset = System.Numerics.Vector2.Zero
		});

		// Add vertical scrollbar
		var verticalScrollbarId = commands.CreateVerticalScrollbar(
			middleRow,
			theme,
			contentWrapper.Id,  // Viewport for mouse wheel detection
			contentArea.Id,     // Content area to scroll
			contentSize: 1000,  // Large initial value to ensure visibility
			visibleSize: 100,   // Small viewport to ensure scrollbar shows
			initialScroll: 0f
		);

		// Right border (vertical resize)
		var resizeRightNode = ClayNode.Configure()
			.Width(resizeBorderWidth)
			.HeightGrow()
			.Background(resizeBorderColor)
			.Build();

		var resizeRight = commands.SpawnClayElement(resizeRightNode);
		middleRow.AddChild(resizeRight);

		// Bottom border container (row layout: bottom border + corner)
		var bottomBorderContainerNode = ClayNode.Configure()
			.WidthGrow()
			.Height(resizeBorderWidth)
			.Row()
			.Gap(0)
			.Padding((ushort)resizeBorderWidth, 0, 0, 0) // Left margin
			.Build();

		var bottomBorderContainer = commands.SpawnClayElement(bottomBorderContainerNode);
		window.AddChild(bottomBorderContainer);

		// Bottom border left part (horizontal resize)
		var resizeBottomNode = ClayNode.Configure()
			.WidthGrow()
			.Height(resizeBorderWidth)
			.Background(resizeBorderColor)
			.Build();

		var resizeBottom = commands.SpawnClayElement(resizeBottomNode);
		bottomBorderContainer.AddChild(resizeBottom);

		// Bottom-right corner (diagonal resize)
		var resizeCornerNode = ClayNode.Configure()
			.Size(resizeBorderWidth, resizeBorderWidth)
			.Background(resizeBorderColor)
			.Build();

		var resizeCorner = commands.SpawnClayElement(resizeCornerNode);
		bottomBorderContainer.AddChild(resizeCorner);

		// Add window state component
		commands.Entity(window.Id).Insert(new FloatingWindowState
		{
			TitleBarEntityId = titleBar.Id,
			CloseButtonEntityId = closeButton.Id,
			ContentWrapperEntityId = contentWrapper.Id,
			ContentAreaEntityId = contentArea.Id,
			VerticalScrollbarId = verticalScrollbarId,
			ResizeRightEntityId = resizeRight.Id,
			ResizeBottomEntityId = resizeBottom.Id,
			ResizeCornerEntityId = resizeCorner.Id,
			Title = title,
			IsDragging = false,
			IsResizing = false,
			ResizingEdge = ResizeEdge.None,
			DragStartX = 0,
			DragStartY = 0,
			InitialX = x,
			InitialY = y,
			InitialWidth = width,
			InitialHeight = height,
			MinWidth = minWidth,
			MinHeight = minHeight,
			ContentWidth = 0,
			ContentHeight = 0,
			TitleBarColor = windowTheme.TitleBarColor,
			TitleTextColor = windowTheme.TitleTextColor,
			BackgroundColor = windowTheme.BackgroundColor,
			BorderColor = windowTheme.BorderColor,
			CloseButtonColor = windowTheme.CloseButtonColor,
			CloseButtonHoverColor = windowTheme.CloseButtonHoverColor,
			ResizeHandleColor = windowTheme.ResizeHandleColor
		});

		// Capture IDs for use in observer closures
		var windowId = window.Id;
		var titleBarId = titleBar.Id;
		var closeButtonId = closeButton.Id;
		var resizeRightId = resizeRight.Id;
		var resizeBottomId = resizeBottom.Id;
		var resizeCornerId = resizeCorner.Id;

		// Add pointer observer for title bar (dragging)
		// Only handles initial press - continuous dragging handled by FloatingWindowPlugin system
		titleBar.Observe<On<ClayPointerEvent>, Commands, Query<Data<FloatingWindowState>>, Query<Data<ClayNode>>>((trigger, cmd, stateQuery, nodeQuery) =>
		{
			var evt = trigger.Event;

			// Only handle press events
			if (evt.EventType != ClayPointerEventType.Pressed)
			{
				return;
			}

			// Stop propagation - we're handling this event
			trigger.Propagate(false);

			if (!stateQuery.Contains(windowId))
			{
				return;
			}

			var (_, statePtr) = stateQuery.Get(windowId);
			var state = statePtr.Ref;

			// Start dragging
			state.IsDragging = true;
			state.DragStartX = evt.Position.X;
			state.DragStartY = evt.Position.Y;

			// Get current window position from node
			if (nodeQuery.Contains(windowId))
			{
				var (_, windowNodePtr) = nodeQuery.Get(windowId);
				ref var windowNode = ref windowNodePtr.Ref;

				if (windowNode.Floating.HasValue)
				{
					var floating = windowNode.Floating.Value;
					state.InitialX = floating.offset.x;
					state.InitialY = floating.offset.y;
				}
			}

			cmd.Entity(windowId).Insert(state);

			// Emit click event to bring window to front
			cmd.Entity(windowId).EmitTrigger(new WindowClicked());
		});

		// Add pointer observer for close button
		closeButton.Observe<On<ClayPointerEvent>, Commands>((trigger, cmd) =>
		{
			var evt = trigger.Event;

			if (evt.EventType == ClayPointerEventType.Pressed)
			{
				// Stop propagation
				trigger.Propagate(false);

				// Emit close event
				cmd.Entity(windowId).EmitTrigger(new WindowCloseRequested());
			}
		});

		// Add pointer observer for window body (for click-to-focus)
		window.Observe<On<ClayPointerEvent>, Commands>((trigger, cmd) =>
		{
			var evt = trigger.Event;

			if (evt.EventType == ClayPointerEventType.Pressed)
			{
				// Don't stop propagation - let child elements handle first
				// But emit click event to bring window to front
				cmd.Entity(windowId).EmitTrigger(new WindowClicked());
			}
		});

		// Helper lambda to create resize observer
		void AddResizeObserver(EntityCommands resizeArea, ResizeEdge edge)
		{
			resizeArea.Observe<On<ClayPointerEvent>, Commands, Query<Data<FloatingWindowState>>, Query<Data<ClayComputedLayout>>>((trigger, cmd, stateQuery, layoutQuery) =>
			{
				var evt = trigger.Event;

				// Only handle press events
				if (evt.EventType != ClayPointerEventType.Pressed)
				{
					return;
				}

				// Stop propagation - we're handling this event
				trigger.Propagate(false);

				if (!stateQuery.Contains(windowId))
				{
					return;
				}

				var (_, statePtr) = stateQuery.Get(windowId);
				var state = statePtr.Ref;

				// Start resizing
				state.IsResizing = true;
				state.ResizingEdge = edge;
				state.DragStartX = evt.Position.X;
				state.DragStartY = evt.Position.Y;

				// Get current window size from computed layout
				if (layoutQuery.Contains(windowId))
				{
					var (_, layoutPtr) = layoutQuery.Get(windowId);
					var layout = layoutPtr.Ref;

					state.InitialWidth = layout.Width;
					state.InitialHeight = layout.Height;
				}

				cmd.Entity(windowId).Insert(state);

				// Emit click event to bring window to front
				cmd.Entity(windowId).EmitTrigger(new WindowClicked());
			});
		}

		// Add observers for all three resize areas
		AddResizeObserver(resizeRight, ResizeEdge.Right);
		AddResizeObserver(resizeBottom, ResizeEdge.Bottom);
		AddResizeObserver(resizeCorner, ResizeEdge.BottomRight);

		return contentArea;
	}
}

using System;
using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Component to track floating window state for dragging, resizing, and z-ordering.
/// </summary>
public struct FloatingWindowState
{
	public ulong TitleBarEntityId;
	public ulong CloseButtonEntityId;
	public ulong ContentAreaEntityId;
	public ulong ResizeHandleEntityId;
	public string Title;
	public bool IsDragging;
	public bool IsResizing;
	public float DragStartX;
	public float DragStartY;
	public float InitialX;
	public float InitialY;
	public float InitialWidth;
	public float InitialHeight;
	public float MinWidth;
	public float MinHeight;
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

		// Content area
		var contentNode = ClayNode.Configure()
			.WidthGrow()
			.HeightGrow()
			.Column()
			.Padding((ushort)windowTheme.ContentPadding)
			.Gap(8)
			.Build();

		var contentArea = commands.SpawnClayElement(contentNode);
		window.AddChild(contentArea);

		// Resize handle (bottom-right corner)
		// Using a floating element positioned at the bottom-right corner of the window
		var resizeHandleSize = 16f;
		var resizeHandleNode = ClayNode.Configure()
			.Size(resizeHandleSize, resizeHandleSize)
			.Background(windowTheme.ResizeHandleColor)
			.CornerRadius(0, 0, windowTheme.CornerRadius, 0)
			.Floating(101) // z-index 1 relative to parent window (stays above window content)
			.FloatingOffset(width - resizeHandleSize, height - resizeHandleSize)
			.Build();

		var resizeHandle = commands.SpawnClayElement(resizeHandleNode);
		window.AddChild(resizeHandle);

		// Add window state component
		commands.Entity(window.Id).Insert(new FloatingWindowState
		{
			TitleBarEntityId = titleBar.Id,
			CloseButtonEntityId = closeButton.Id,
			ContentAreaEntityId = contentArea.Id,
			ResizeHandleEntityId = resizeHandle.Id,
			Title = title,
			IsDragging = false,
			IsResizing = false,
			DragStartX = 0,
			DragStartY = 0,
			InitialX = x,
			InitialY = y,
			InitialWidth = width,
			InitialHeight = height,
			MinWidth = minWidth,
			MinHeight = minHeight,
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
		var resizeHandleId = resizeHandle.Id;

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

		// Add pointer observer for resize handle
		// Only handles initial press - continuous resizing handled by FloatingWindowPlugin system
		resizeHandle.Observe<On<ClayPointerEvent>, Commands, Query<Data<FloatingWindowState>>, Query<Data<ClayComputedLayout>>>((trigger, cmd, stateQuery, layoutQuery) =>
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

		return contentArea;
	}
}

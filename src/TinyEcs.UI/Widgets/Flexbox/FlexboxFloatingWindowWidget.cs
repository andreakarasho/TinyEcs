using System;
using System.Numerics;
using Flexbox;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Flexbox;

/// <summary>
/// Component to track Flexbox floating window state.
/// Stores position, drag state, and window controls.
/// </summary>
public struct FlexboxFloatingWindowState
{
	public Vector2 Position;
	public Vector2 Size;
	public bool IsDragging;
	public Vector2 DragOffset;
	public bool IsMinimized;
	public bool IsMaximized;
	public Vector2 RestorePosition;
	public Vector2 RestoreSize;

	public readonly bool CanDrag => !IsMinimized && !IsMaximized;
}

/// <summary>
/// Links to key parts of a Flexbox floating window.
/// Stored on the window entity for quick access in systems.
/// </summary>
public struct FlexboxFloatingWindowLinks
{
	public ulong TitleBarId;
	public ulong ContentAreaId;
	public ulong CloseButtonId;
	public ulong MinimizeButtonId;
	public ulong MaximizeButtonId;
	public ulong ScrollbarId;  // Vertical scrollbar for content area
}

/// <summary>
/// Style configuration for Flexbox floating windows.
/// </summary>
public struct FlexboxFloatingWindowStyle
{
	public Vector2 InitialSize;
	public Vector2 MinSize;
	public Vector2 MaxSize;
	public float TitleBarHeight;
	public Vector4 TitleBarColor;
	public Vector4 WindowBackgroundColor;
	public Vector4 BorderColor;
	public float BorderWidth;
	public float CornerRadius;
	public float TitleFontSize;
	public Vector4 TitleTextColor;
	public float ContentPadding;
	public float ContentGap;
	public bool ShowCloseButton;
	public bool ShowMinimizeButton;
	public bool ShowMaximizeButton;
	public bool Draggable;

	public static FlexboxFloatingWindowStyle Default()
	{
		return new FlexboxFloatingWindowStyle
		{
			InitialSize = new Vector2(400f, 300f),
			MinSize = new Vector2(200f, 150f),
			MaxSize = new Vector2(1200f, 900f),
			TitleBarHeight = 32f,
			TitleBarColor = new Vector4(0.22f, 0.25f, 0.32f, 1f), // Dark gray-blue
			WindowBackgroundColor = new Vector4(0.12f, 0.16f, 0.22f, 1f), // Darker background
			BorderColor = new Vector4(0.42f, 0.45f, 0.50f, 1f), // Light gray border
			BorderWidth = 1f,
			CornerRadius = 8f,
			TitleFontSize = 16f,
			TitleTextColor = new Vector4(0.95f, 0.96f, 0.97f, 1f), // Off-white
			ContentPadding = 16f,
			ContentGap = 12f,
			ShowCloseButton = true,
			ShowMinimizeButton = true,
			ShowMaximizeButton = true,
			Draggable = true
		};
	}

	public static FlexboxFloatingWindowStyle Dialog()
	{
		var style = Default();
		style.InitialSize = new Vector2(300f, 200f);
		style.ShowMinimizeButton = false;
		style.ShowMaximizeButton = false;
		return style;
	}

	public static FlexboxFloatingWindowStyle Tool()
	{
		var style = Default();
		style.InitialSize = new Vector2(250f, 400f);
		style.TitleBarHeight = 24f;
		style.TitleFontSize = 14f;
		style.ContentPadding = 12f;
		return style;
	}
}

/// <summary>
/// Creates floating window widgets using Flexbox layout.
/// Windows can be dragged, minimized, maximized, and closed.
///
/// Unlike Clay floating windows which use native floating config,
/// Flexbox windows use absolute positioning that must be manually updated.
///
/// Usage:
/// <code>
/// var window = FlexboxFloatingWindowWidget.Create(
///     commands,
///     "My Window",
///     new Vector2(100f, 100f),
///     FlexboxFloatingWindowStyle.Default()
/// );
///
/// // Add content as children of window.ContentAreaId
/// FlexboxLabelWidget.Create(commands, "Hello, World!")
///     .Insert(new FlexboxNodeParent(window.ContentAreaId));
/// </code>
/// </summary>
public static class FlexboxFloatingWindowWidget
{
	/// <summary>
	/// Creates a floating window entity with title bar and content area.
	/// Returns a structure with window ID and content area ID for adding children.
	/// </summary>
	public static FlexboxWindowHandle Create(
		Commands commands,
		string title,
		Vector2 initialPosition,
		FlexboxFloatingWindowStyle? style = null)
	{
		var windowStyle = style ?? FlexboxFloatingWindowStyle.Default();

		// Create main window container with absolute positioning
		var windowId = commands.Spawn()
			.Insert(new FlexboxNode
			{
				Display = Display.Flex,
				FlexDirection = FlexDirection.Column,
				PositionType = PositionType.Absolute,
				Left = FlexValue.Points(initialPosition.X),
				Top = FlexValue.Points(initialPosition.Y),
				Width = FlexValue.Points(windowStyle.InitialSize.X),
				Height = FlexValue.Points(windowStyle.InitialSize.Y),
				BackgroundColor = windowStyle.WindowBackgroundColor,
				BorderColor = windowStyle.BorderColor,
				BorderTop = FlexValue.Points(windowStyle.BorderWidth),
				BorderRight = FlexValue.Points(windowStyle.BorderWidth),
				BorderBottom = FlexValue.Points(windowStyle.BorderWidth),
				BorderLeft = FlexValue.Points(windowStyle.BorderWidth),
				BorderRadius = windowStyle.CornerRadius
			})
			.Insert(new FlexboxInteractive()) // Make window interactive for dragging
			.Insert(new FlexboxFloatingWindowState
			{
				Position = initialPosition,
				Size = windowStyle.InitialSize,
				IsDragging = false,
				DragOffset = Vector2.Zero,
				IsMinimized = false,
				IsMaximized = false,
				RestorePosition = initialPosition,
				RestoreSize = windowStyle.InitialSize
			})
			.Id;

		// Create title bar
		var titleBarInfo = CreateTitleBar(commands, windowId, title, windowStyle);

		// Create horizontal container for content area + scrollbar
		var contentContainerId = commands.Spawn()
			.Insert(new FlexboxNode
			{
				Display = Display.Flex,
				FlexDirection = FlexDirection.Row,
				PositionType = PositionType.Relative,
				Width = FlexValue.Percent(100f),
				FlexGrow = 1f, // Take all remaining space after title bar
				FlexShrink = 1f,
				FlexBasis = FlexBasis.Auto()
			})
			.Insert(new FlexboxNodeParent(windowId, index: 1)) // After title bar
			.Id;

		// Create content area with clipping (takes most of horizontal space)
		var contentAreaId = commands.Spawn()
			.Insert(new FlexboxNode
			{
				Display = Display.Flex,
				FlexDirection = FlexDirection.Column,
				PositionType = PositionType.Relative,
				FlexGrow = 1f, // Take remaining width after scrollbar
				FlexShrink = 1f,
				FlexBasis = FlexBasis.Auto(),
				Width = FlexValue.Auto(), // Let flexbox calculate width
				Height = FlexValue.Percent(100f), // Take full height of parent
				PaddingTop = windowStyle.ContentPadding,
				PaddingRight = windowStyle.ContentPadding,
				PaddingBottom = windowStyle.ContentPadding,
				PaddingLeft = windowStyle.ContentPadding
			})
			.Insert(new FlexboxScrollContainer()) // Enable clipping/scissor mode
			.Insert(new FlexboxNodeParent(contentContainerId, index: 0)) // First child of container
			.Id;

		// Create vertical scrollbar (on the right side)
		// Calculate available height for scrollbar (window height - title bar height)
		var scrollbarHeight = windowStyle.InitialSize.Y - windowStyle.TitleBarHeight;
		var scrollbarHandle = FlexboxScrollbarWidget.CreateVertical(
			commands,
			scrollbarHeight,
			visibleRatio: 0.5f, // Default - will be updated by sync system
			style: null,
			parent: 0); // No parent yet

		// Position scrollbar as second child (after content area)
		commands.Entity(scrollbarHandle.ScrollbarId)
			.Insert(new FlexboxNodeParent(contentContainerId, index: 1));

		// Store links for systems to access window parts
		commands.Entity(windowId).Insert(new FlexboxFloatingWindowLinks
		{
			TitleBarId = titleBarInfo.TitleBarId,
			ContentAreaId = contentAreaId,
			CloseButtonId = titleBarInfo.CloseButtonId,
			MinimizeButtonId = titleBarInfo.MinimizeButtonId,
			MaximizeButtonId = titleBarInfo.MaximizeButtonId,
			ScrollbarId = scrollbarHandle.ScrollbarId
		});

		// Note: Window dragging is handled by the global FlexboxFloatingWindowSystem
		// registered in FlexboxUiPlugin, not entity-specific observers

		return new FlexboxWindowHandle
		{
			WindowId = windowId,
			ContentAreaId = contentAreaId
		};
	}

	/// <summary>
	/// Creates a simple dialog window (no minimize/maximize buttons).
	/// </summary>
	public static FlexboxWindowHandle CreateDialog(
		Commands commands,
		string title,
		Vector2 position)
	{
		return Create(commands, title, position, FlexboxFloatingWindowStyle.Dialog());
	}

	/// <summary>
	/// Creates a tool window (smaller, utility-style).
	/// </summary>
	public static FlexboxWindowHandle CreateTool(
		Commands commands,
		string title,
		Vector2 position)
	{
		return Create(commands, title, position, FlexboxFloatingWindowStyle.Tool());
	}

	private struct TitleBarInfo
	{
		public ulong TitleBarId;
		public ulong CloseButtonId;
		public ulong MinimizeButtonId;
		public ulong MaximizeButtonId;
	}

	private static TitleBarInfo CreateTitleBar(
		Commands commands,
		ulong windowId,
		string title,
		FlexboxFloatingWindowStyle style)
	{
		// Create title bar container
		var titleBarId = commands.Spawn()
			.Insert(new FlexboxNode
			{
				Display = Display.Flex,
				FlexDirection = FlexDirection.Row,
				JustifyContent = Justify.SpaceBetween,
				AlignItems = Align.Center,
				PositionType = PositionType.Relative, // Position relative to parent window
				Width = FlexValue.Percent(100f),
				Height = FlexValue.Points(style.TitleBarHeight),
				FlexShrink = 0f, // Don't shrink the title bar
				BackgroundColor = style.TitleBarColor,
				PaddingLeft = 12f,
				PaddingRight = 8f
			})
			.Insert(new FlexboxInteractive()) // Make title bar interactive for dragging
			.Insert(new FlexboxNodeParent(windowId, index: 0)) // First child of window
			.Id;

		// Add title text
		commands.Spawn()
			.Insert(new FlexboxNode
			{
				Display = Display.Flex,
				FlexDirection = FlexDirection.Row,
				AlignItems = Align.Center,
				FlexGrow = 1f // Take up remaining space
			})
			.Insert(new FlexboxText(title, style.TitleFontSize, style.TitleTextColor))
			.Insert(new FlexboxNodeParent(titleBarId));

		// Create button container
		var buttonContainerId = commands.Spawn()
			.Insert(new FlexboxNode
			{
				Display = Display.Flex,
				FlexDirection = FlexDirection.Row,
				AlignItems = Align.Center
				// Note: Gap is handled via individual button margins
			})
			.Insert(new FlexboxNodeParent(titleBarId))
			.Id;

		// Add window control buttons
		var buttonSize = style.TitleBarHeight - 8f;

		ulong minimizeButtonId = 0;
		ulong maximizeButtonId = 0;
		ulong closeButtonId = 0;

		if (style.ShowMinimizeButton)
		{
			minimizeButtonId = CreateTitleBarButton(
				commands,
				buttonContainerId,
				buttonSize,
				"_",
				new Vector4(0.23f, 0.51f, 0.96f, 1f)); // Blue
		}

		if (style.ShowMaximizeButton)
		{
			maximizeButtonId = CreateTitleBarButton(
				commands,
				buttonContainerId,
				buttonSize,
				"□",
				new Vector4(0.13f, 0.77f, 0.37f, 1f)); // Green
		}

		if (style.ShowCloseButton)
		{
			closeButtonId = CreateTitleBarButton(
				commands,
				buttonContainerId,
				buttonSize,
				"×",
				new Vector4(0.94f, 0.27f, 0.27f, 1f)); // Red
		}

		return new TitleBarInfo
		{
			TitleBarId = titleBarId,
			CloseButtonId = closeButtonId,
			MinimizeButtonId = minimizeButtonId,
			MaximizeButtonId = maximizeButtonId
		};
	}

	private static ulong CreateTitleBarButton(
		Commands commands,
		ulong parentId,
		float size,
		string symbol,
		Vector4 color)
	{
		return commands.Spawn()
			.Insert(new FlexboxNode
			{
				Display = Display.Flex,
				FlexDirection = FlexDirection.Row,
				JustifyContent = Justify.Center,
				AlignItems = Align.Center,
				Width = FlexValue.Points(size),
				Height = FlexValue.Points(size),
				BackgroundColor = color,
				BorderRadius = 4f,
				MarginLeft = FlexValue.Points(4f) // Add spacing between buttons
			})
			.Insert(new FlexboxText(symbol, size * 0.7f, new Vector4(1f, 1f, 1f, 1f))) // White text
			.Insert(new FlexboxInteractive())
			.Insert(new FlexboxNodeParent(parentId))
			.Id;
	}

	/// <summary>
	/// Observer callback for handling window interactions (dragging and button clicks).
	/// This is attached to each window entity.
	/// </summary>
	private static void HandleWindowInteraction(
		On<UiPointerTrigger> trigger,
		Query<Data<FlexboxFloatingWindowState, FlexboxFloatingWindowLinks, FlexboxNode>> windows,
		Query<Data<Parent>> parents,
		Commands commands,
		ResMut<FlexboxUiState> uiState)
	{
		var evt = trigger.Event.Event;
		var windowId = trigger.EntityId;

		// Get window state and links
		if (!windows.Contains(windowId)) return;

		var winData = windows.Get(windowId);
		winData.Deconstruct(out var stateParam, out var linksParam, out var nodeParam);

		ref var state = ref stateParam.Ref;
		var links = linksParam.Ref;
		ref var node = ref nodeParam.Ref;

		// Handle button clicks (only on direct target, not bubbled)
		if (evt.Type == UiPointerEventType.PointerDown && evt.IsPrimaryButton && evt.Target == evt.CurrentTarget)
		{
			if (evt.Target == links.CloseButtonId)
			{
				// Close window by despawning it
				commands.Entity(windowId).Despawn();
				return;
			}

			if (evt.Target == links.MinimizeButtonId)
			{
				state.IsMinimized = !state.IsMinimized;
				// TODO: Update window visibility or size
				return;
			}

			if (evt.Target == links.MaximizeButtonId)
			{
				state.IsMaximized = !state.IsMaximized;
				// TODO: Update window size to fullscreen or restore
				return;
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
				Console.WriteLine($"[Drag] START - onTitleBar={onTitleBar}, onButton={onButton}, CanDrag={state.CanDrag}");
			}
		}
		else if (evt.Type == UiPointerEventType.PointerUp)
		{
			state.IsDragging = false;
		}
		else if (evt.Type == UiPointerEventType.PointerMove)
		{
			if (state.IsDragging)
			{
				// Update position (absolute positioning in Flexbox)
				state.Position = evt.Position - state.DragOffset;
				node.Left = FlexValue.Points(state.Position.X);
				node.Top = FlexValue.Points(state.Position.Y);

				// Update the internal Flexbox node directly for immediate layout effect
				if (uiState.Value.EntityToFlexboxNode.TryGetValue(windowId, out var flexboxNode))
				{
					Console.WriteLine($"[Drag] Before: Pos[0]={flexboxNode.nodeStyle.Position[0].value}, Pos[1]={flexboxNode.nodeStyle.Position[1].value}");
					flexboxNode.nodeStyle.Position[0] = new global::Flexbox.Value(state.Position.X, global::Flexbox.Unit.Point); // Left (Edge.Left = 0)
					flexboxNode.nodeStyle.Position[1] = new global::Flexbox.Value(state.Position.Y, global::Flexbox.Unit.Point); // Top (Edge.Top = 1)
					Console.WriteLine($"[Drag] After: Pos[0]={flexboxNode.nodeStyle.Position[0].value}, Pos[1]={flexboxNode.nodeStyle.Position[1].value}, NewPos={state.Position}");
				}

				// No need to mark dirty - layout syncs every frame now
			}
		}
	}
}

/// <summary>
/// Return value from FlexboxFloatingWindowWidget.Create.
/// Provides IDs for the window and its content area.
/// </summary>
public struct FlexboxWindowHandle
{
	public ulong WindowId;
	public ulong ContentAreaId;
}

using System;
using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using EcsID = ulong;

namespace TinyEcs.UI.Widgets;

/// <summary>
/// Component to track floating window state.
/// </summary>
public struct FloatingWindowState
{
	public Vector2 Position;
	public Vector2 Size;
	public bool IsDragging;
	public bool IsResizing;
	public bool IsMinimized;
	public bool IsMaximized;
	public Vector2 DragOffset;
	public Vector2 RestorePosition;
	public Vector2 RestoreSize;

	public readonly bool CanDrag => !IsMinimized && !IsMaximized;
	public readonly bool CanResize => !IsMinimized && !IsMaximized;
}

/// <summary>
/// Links to key parts of a floating window for observer-driven behavior.
/// Stored on the window entity.
/// </summary>
public struct FloatingWindowLinks
{
	public EcsID TitleBarId;
	public EcsID ResizeHandleId;
	public EcsID ScrollContainerId;
	public EcsID ContentAreaId;
	public EcsID CloseButtonId;
	public EcsID MinimizeButtonId;
	public EcsID MaximizeButtonId;
}

/// <summary>
/// Style configuration for floating window widgets.
/// </summary>
public readonly record struct ClayFloatingWindowStyle(
	Vector2 InitialSize,
	Vector2 MinSize,
	Vector2 MaxSize,
	float TitleBarHeight,
	Clay_Color TitleBarColor,
	Clay_Color WindowBackgroundColor,
	Clay_Color BorderColor,
	Clay_BorderWidth BorderWidth,
	Clay_CornerRadius CornerRadius,
	ushort TitleFontSize,
	Clay_Color TitleTextColor,
	Clay_Padding ContentPadding,
	ushort ContentGap,
	bool ShowCloseButton,
	bool ShowMinimizeButton,
	bool ShowMaximizeButton,
	bool Resizable,
	bool Draggable)
{
	public static ClayFloatingWindowStyle Default => new(
		new Vector2(400f, 300f),
		new Vector2(200f, 150f),
		new Vector2(1200f, 900f),
		32f,
		new Clay_Color(55, 65, 81, 255),
		new Clay_Color(31, 41, 55, 255),
		new Clay_Color(107, 114, 128, 255),
		new Clay_BorderWidth { left = 1, right = 1, top = 1, bottom = 1 },
		Clay_CornerRadius.All(8),
		16,
		new Clay_Color(243, 244, 246, 255),
		Clay_Padding.All(16),
		12,
		true,
		true,
		true,
		true,
		true);

	public static ClayFloatingWindowStyle Dialog => Default with
	{
		InitialSize = new Vector2(300f, 200f),
		ShowMinimizeButton = false,
		ShowMaximizeButton = false,
		Resizable = false,
		// z-order handled by UiWindowOrder
	};

	public static ClayFloatingWindowStyle Tool => Default with
	{
		InitialSize = new Vector2(250f, 400f),
		TitleBarHeight = 24f,
		TitleFontSize = 14,
		ContentPadding = Clay_Padding.All(12),
		// z-order handled by UiWindowOrder
	};

	public static ClayFloatingWindowStyle Panel => Default with
	{
		InitialSize = new Vector2(500f, 600f),
		ShowCloseButton = false,
		ShowMinimizeButton = false,
		ShowMaximizeButton = false,
		Draggable = true,
		Resizable = true
	};
}

/// <summary>
/// Creates floating window widgets with title bars, dragging, and optional controls.
/// </summary>
public static class FloatingWindowWidget
{
	/// <summary>
	/// Creates a floating window entity with title bar and content area.
	/// </summary>
	/// <param name="commands">Command buffer for entity creation.</param>
	/// <param name="style">Visual style configuration.</param>
	/// <param name="title">Window title text.</param>
	/// <param name="initialPosition">Starting position on screen.</param>
	/// <param name="parent">Optional parent entity ID.</param>
	/// <param name="includeContentArea">When true (default), creates a padded content container (body) under the title bar. Set to false for title-bar-only windows.</param>
	/// <returns>EntityCommands for the window container (use .Id to parent content to it).</returns>
	public static EntityCommands Create(
		Commands commands,
		ClayFloatingWindowStyle style,
		ReadOnlySpan<char> title,
		Vector2 initialPosition,
		EcsID? parent = default,
		bool includeContentArea = true)
	{
		// Create main window container with floating behavior
		var window = commands.Spawn();

		window.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(style.InitialSize.X),
						Clay_SizingAxis.Fixed(style.InitialSize.Y)),
					layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM
				},
				backgroundColor = style.WindowBackgroundColor,
				cornerRadius = style.CornerRadius,
				border = new Clay_BorderElementConfig
				{
					color = style.BorderColor,
					width = style.BorderWidth
				},
				floating = new Clay_FloatingElementConfig
				{
					attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT,
					offset = new Clay_Vector2 { x = initialPosition.X, y = initialPosition.Y },
					// zIndex neutral; global stacking controlled by UiWindowOrder + render pass
					zIndex = 0,
					parentId = 0, // Float relative to root
					attachPoints = new Clay_FloatingAttachPoints
					{
						element = Clay_FloatingAttachPointType.CLAY_ATTACH_POINT_LEFT_TOP,
						parent = Clay_FloatingAttachPointType.CLAY_ATTACH_POINT_LEFT_TOP
					},
					pointerCaptureMode = Clay_PointerCaptureMode.CLAY_POINTER_CAPTURE_MODE_CAPTURE
				}
			}
		});

		if (parent.HasValue && parent.Value != 0)
		{
			window.Insert(UiNodeParent.For(parent.Value));
		}

		// Add window state
		window.Insert(new FloatingWindowState
		{
			Position = initialPosition,
			Size = style.InitialSize,
			IsDragging = false,
			IsResizing = false,
			IsMinimized = false,
			IsMaximized = false,
			DragOffset = Vector2.Zero,
			RestorePosition = initialPosition,
			RestoreSize = style.InitialSize
		});


		// Create title bar and capture button IDs
		var titleBarInfo = CreateTitleBar(commands, window.Id, style, title);

		// Optionally create a content/body area below the title bar
		EcsID scrollContainerId = 0;
		EcsID contentAreaId = 0;
		if (includeContentArea)
		{
			// Create scroll container wrapper (fixed size with clipping)
			var scrollContainer = commands.Spawn();
			scrollContainer.Insert(new UiNode
			{
				Declaration = new Clay_ElementDeclaration
				{
					layout = new Clay_LayoutConfig
					{
						sizing = new Clay_Sizing(
							Clay_SizingAxis.Grow(),
							Clay_SizingAxis.Grow()),
						layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM
					},
					clip = new Clay_ClipElementConfig
					{
						// Enable vertical scrolling with clipping
						vertical = true,
						horizontal = false,
						childOffset = new Clay_Vector2 { x = 0, y = 0 }
					}
				}
			});
			// Scroll container appears after title bar
			scrollContainer.Insert(UiNodeParent.For(window.Id, 1));
			scrollContainerId = scrollContainer.Id;

			// Create scrollable content area inside the scroll container
			var contentArea = commands.Spawn();
			contentArea.Insert(new UiNode
			{
				Declaration = new Clay_ElementDeclaration
				{
					layout = new Clay_LayoutConfig
					{
						sizing = new Clay_Sizing(
							Clay_SizingAxis.Grow(),
							Clay_SizingAxis.Fit(0, float.MaxValue)),
						padding = style.ContentPadding,
						childGap = style.ContentGap,
						layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM
					}
				}
			});
			// Content area is child of scroll container
			contentArea.Insert(UiNodeParent.For(scrollContainer.Id));
			contentAreaId = contentArea.Id;
		}

		// Link parts for observers and external access
		window.Insert(new FloatingWindowLinks
		{
			TitleBarId = titleBarInfo.TitleBarId,
			ResizeHandleId = 0,
			ScrollContainerId = scrollContainerId,
			ContentAreaId = contentAreaId,
			CloseButtonId = titleBarInfo.CloseButtonId,
			MinimizeButtonId = titleBarInfo.MinimizeButtonId,
			MaximizeButtonId = titleBarInfo.MaximizeButtonId
		});

		// Add resize handle if resizable
		if (style.Resizable)
		{
			CreateResizeHandle(commands, window.Id, style);
		}

		// Note: Window registration in UiWindowOrder happens via global observer system in UiWidgetsPlugin
		// (Entity observers don't fire for components added in the same command batch)

		// Handle dragging via entity-specific observer (works in tests and runtime)
		window.Observe<UiPointerTrigger,
			Query<Data<FloatingWindowState, UiNode, FloatingWindowLinks>>,
			Query<Data<Parent>>,
			ResMut<UiWindowOrder>>((trigger, windows, parents, windowOrder) =>
		{
			var evt = trigger.Event;
			var id = evt.CurrentTarget;
			if (!windows.Contains(id)) return;
			var winData = windows.Get(id);
			winData.Deconstruct(out var stateParam, out var nodeParam, out var linksParam);

			ref var st = ref stateParam.Ref;
			ref var node = ref nodeParam.Ref;
			var links = linksParam.Ref;

			if (evt.Type == UiPointerEventType.PointerDown && evt.IsPrimaryButton)
			{
				// Check if clicking on a window control button - if so, don't start dragging
				bool onButton = evt.Target == links.CloseButtonId ||
								evt.Target == links.MinimizeButtonId ||
								evt.Target == links.MaximizeButtonId;

				if (onButton)
				{
					// Don't drag when clicking buttons, but still bring to front
					windowOrder.Value.MoveToTop(id);
					return;
				}

				// Bring window to front on any click within the window hierarchy
				windowOrder.Value.MoveToTop(id);

				// Start drag when clicking on title bar or any of its descendants (but not buttons)
				bool onTitleBar = false;
				if (links.TitleBarId != 0)
				{
					onTitleBar = evt.Target == links.TitleBarId;
					if (!onTitleBar)
					{
						// climb parents from target to see if we hit the title bar
						var current = evt.Target;
						int safety = 0;
						while (current != 0 && safety++ < 256)
						{
							if (current == links.TitleBarId) { onTitleBar = true; break; }
							if (!parents.Contains(current)) break;
							var parentData = parents.Get(current);
							parentData.Deconstruct(out _, out var parentPtr);
							var parentId = parentPtr.Ref.Id;
							if (parentId == 0 || parentId == current) break;
							current = parentId;
						}
					}
				}

				if (onTitleBar && st.CanDrag)
				{
					st.IsDragging = true;
					st.DragOffset = evt.Position - st.Position;
				}
			}
			else if (evt.Type == UiPointerEventType.PointerUp)
			{
				st.IsDragging = false;
			}
			else if (evt.Type == UiPointerEventType.PointerMove)
			{
				if (st.IsDragging)
				{
					// Prefer absolute pointer when available; fall back to delta (used by tests)
					if (evt.MoveDelta != Vector2.Zero)
						st.Position += evt.MoveDelta;
					else
						st.Position = evt.Position - st.DragOffset;
					node.Declaration.floating.offset = new Clay_Vector2
					{
						x = st.Position.X,
						y = st.Position.Y
					};
				}
			}

			// handled
		});

		// Handle window control button clicks via entity-specific observer
		window.Observe<UiPointerTrigger,
			Query<Data<FloatingWindowLinks>>,
			Commands>((trigger, windows, commands) =>
		{
			var evt = trigger.Event;
			var windowId = evt.CurrentTarget;

			// Only handle pointer down events (immediate button response)
			if (evt.Type != UiPointerEventType.PointerDown || !evt.IsPrimaryButton)
				return;

			// Check if the window still exists and get its links
			if (!windows.Contains(windowId)) return;
			var winData = windows.Get(windowId);
			winData.Deconstruct(out _, out var linksPtr);
			var links = linksPtr.Ref;

			// Check if the close button was clicked
			if (links.CloseButtonId != 0 && evt.Target == links.CloseButtonId)
			{
				// Despawn the window entity (which will trigger OnRemove<FloatingWindowState>
				// and clean up the UiWindowOrder via the global observer in UiWidgetsPlugin)
				commands.Entity(windowId).Despawn();
			}

			// Handle minimize button (if implemented later)
			// if (links.MinimizeButtonId != 0 && evt.Target == links.MinimizeButtonId)
			// {
			//     // Toggle minimize state
			// }

			// Handle maximize button (if implemented later)
			// if (links.MaximizeButtonId != 0 && evt.Target == links.MaximizeButtonId)
			// {
			//     // Toggle maximize state
			// }
		});

		return window;
	}

	/// <summary>
	/// Creates a simple dialog window.
	/// </summary>
	public static EntityCommands CreateDialog(
		Commands commands,
		ReadOnlySpan<char> title,
		Vector2 position,
		EcsID? parent = default)
	{
		return Create(commands, ClayFloatingWindowStyle.Dialog, title, position, parent);
	}

	/// <summary>
	/// Creates a tool window (smaller, utility-style).
	/// </summary>
	public static EntityCommands CreateTool(
		Commands commands,
		ReadOnlySpan<char> title,
		Vector2 position,
		EcsID? parent = default)
	{
		return Create(commands, ClayFloatingWindowStyle.Tool, title, position, parent);
	}

	/// <summary>
	/// Creates a panel window (no window controls, just draggable/resizable).
	/// </summary>
	public static EntityCommands CreatePanel(
		Commands commands,
		ReadOnlySpan<char> title,
		Vector2 position,
		EcsID? parent = default)
	{
		return Create(commands, ClayFloatingWindowStyle.Panel, title, position, parent);
	}

	private struct TitleBarInfo
	{
		public EcsID TitleBarId;
		public EcsID CloseButtonId;
		public EcsID MinimizeButtonId;
		public EcsID MaximizeButtonId;
	}

	private static TitleBarInfo CreateTitleBar(
		Commands commands,
		EcsID windowId,
		ClayFloatingWindowStyle style,
		ReadOnlySpan<char> title)
	{
		var titleBar = commands.Spawn();

		// Calculate button space on the right
		var buttonCount = 0;
		if (style.ShowCloseButton) buttonCount++;
		if (style.ShowMinimizeButton) buttonCount++;
		if (style.ShowMaximizeButton) buttonCount++;
		var buttonSpace = buttonCount * (style.TitleBarHeight + 4);

		titleBar.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Grow(),
						Clay_SizingAxis.Fixed(style.TitleBarHeight)),
					layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
					childAlignment = new Clay_ChildAlignment(
						Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT,
						Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER),
					padding = Clay_Padding.HorVer(12, 0),
					childGap = 8
				},
				backgroundColor = style.TitleBarColor
			}
		});
		// Ensure title bar is first child (index 0)
		titleBar.Insert(UiNodeParent.For(windowId, 0));

		// Add title text
		var titleText = commands.Spawn();
		titleText.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Grow(),
						Clay_SizingAxis.Fixed(style.TitleBarHeight)),
					childAlignment = new Clay_ChildAlignment(
						Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT,
						Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
				}
			}
		});
		titleText.Insert(UiText.From(title, new Clay_TextElementConfig
		{
			textColor = style.TitleTextColor,
			fontSize = style.TitleFontSize,
			textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_LEFT,
			wrapMode = Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE
		}));
		titleText.Insert(UiNodeParent.For(titleBar.Id));

		// Create button container on the right
		var buttonContainer = commands.Spawn();
		buttonContainer.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fit(0, buttonSpace),
						Clay_SizingAxis.Fixed(style.TitleBarHeight)),
					layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
					childAlignment = new Clay_ChildAlignment(
						Clay_LayoutAlignmentX.CLAY_ALIGN_X_RIGHT,
						Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER),
					childGap = 4
				}
			}
		});
		buttonContainer.Insert(UiNodeParent.For(titleBar.Id));

		// Add window control buttons
		var buttonSize = style.TitleBarHeight - 8;

		EcsID minimizeButtonId = 0;
		EcsID maximizeButtonId = 0;
		EcsID closeButtonId = 0;

		if (style.ShowMinimizeButton)
		{
			minimizeButtonId = CreateTitleBarButton(commands, buttonContainer.Id, buttonSize, "_",
				new Clay_Color(59, 130, 246, 255));
		}

		if (style.ShowMaximizeButton)
		{
			maximizeButtonId = CreateTitleBarButton(commands, buttonContainer.Id, buttonSize, "□",
				new Clay_Color(34, 197, 94, 255));
		}

		if (style.ShowCloseButton)
		{
			closeButtonId = CreateTitleBarButton(commands, buttonContainer.Id, buttonSize, "×",
				new Clay_Color(239, 68, 68, 255));
		}

		return new TitleBarInfo
		{
			TitleBarId = titleBar.Id,
			CloseButtonId = closeButtonId,
			MinimizeButtonId = minimizeButtonId,
			MaximizeButtonId = maximizeButtonId
		};
	}

	private static EcsID CreateTitleBarButton(
		Commands commands,
		EcsID parentId,
		float size,
		ReadOnlySpan<char> symbol,
		Clay_Color color)
	{
		var button = commands.Spawn();
		button.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(size),
						Clay_SizingAxis.Fixed(size)),
					childAlignment = new Clay_ChildAlignment(
						Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
						Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
				},
				backgroundColor = color,
				cornerRadius = Clay_CornerRadius.All(4)
			}
		});

		button.Insert(UiText.From(symbol, new Clay_TextElementConfig
		{
			textColor = new Clay_Color(255, 255, 255, 255),
			fontSize = (ushort)(size * 0.7f),
			textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
		}));

		button.Insert(UiNodeParent.For(parentId));

		return button.Id;
	}

	private static void CreateResizeHandle(
		Commands commands,
		EcsID windowId,
		ClayFloatingWindowStyle style)
	{
		var handle = commands.Spawn();
		var handleSize = 16f;

		handle.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(handleSize),
						Clay_SizingAxis.Fixed(handleSize))
				},
				backgroundColor = new Clay_Color(156, 163, 175, 200),
				cornerRadius = new Clay_CornerRadius
				{
					topLeft = 0,
					topRight = 0,
					bottomLeft = 0,
					bottomRight = (float)style.CornerRadius.bottomRight
				},
				floating = new Clay_FloatingElementConfig
				{
					offset = new Clay_Vector2
					{
						x = style.InitialSize.X - handleSize,
						y = style.InitialSize.Y - handleSize
					},
					zIndex = 0,
					parentId = windowId.GetHashCode() > 0 ? (uint)windowId.GetHashCode() : 0,
					attachPoints = new Clay_FloatingAttachPoints
					{
						element = Clay_FloatingAttachPointType.CLAY_ATTACH_POINT_LEFT_TOP,
						parent = Clay_FloatingAttachPointType.CLAY_ATTACH_POINT_LEFT_TOP
					},
					pointerCaptureMode = Clay_PointerCaptureMode.CLAY_POINTER_CAPTURE_MODE_CAPTURE
				}
			}
		});

		handle.Insert(UiText.From("⋰", new Clay_TextElementConfig
		{
			textColor = new Clay_Color(255, 255, 255, 255),
			fontSize = 12,
			textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
		}));

		handle.Insert(UiNodeParent.For(windowId));
	}

	/// <summary>
	/// System to handle window dragging from title bar.
	/// Use this as a reference for implementing window interactions.
	/// </summary>
	public static void HandleWindowDrag(
		EventReader<UiPointerEvent> events,
		Query<Data<FloatingWindowState, UiNode>> windows,
		Res<ClayPointerState> pointer)
	{
		// This is a simplified reference implementation
		// In a real implementation, you would:
		// 1. Match evt.Target with specific window entities
		// 2. Check if the click is on the title bar (not buttons)
		// 3. Update window position based on pointer delta
		// 4. Update the floating offset in the UiNode declaration

		foreach (var evt in events.Read())
		{
			if (evt.Type == UiPointerEventType.PointerDown && evt.IsPrimaryButton)
			{
				foreach (var (state, node) in windows)
				{
					ref var stateRef = ref state.Ref;
					if (stateRef.CanDrag)
					{
						stateRef.IsDragging = true;
						stateRef.DragOffset = pointer.Value.Position - stateRef.Position;
					}
				}
			}
			else if (evt.Type == UiPointerEventType.PointerUp)
			{
				foreach (var (state, node) in windows)
				{
					ref var stateRef = ref state.Ref;
					stateRef.IsDragging = false;
				}
			}
			else if (evt.Type == UiPointerEventType.PointerMove)
			{
				foreach (var (state, node) in windows)
				{
					ref var stateRef = ref state.Ref;
					if (stateRef.IsDragging)
					{
						stateRef.Position = pointer.Value.Position - stateRef.DragOffset;

						// Update the floating offset in the UI node
						ref var nodeRef = ref node.Ref;
						nodeRef.Declaration.floating.offset = new Clay_Vector2
						{
							x = stateRef.Position.X,
							y = stateRef.Position.Y
						};
					}
				}
			}
		}
	}

	/// <summary>
	/// System to handle window resizing from resize handle.
	/// Use this as a reference for implementing resize interactions.
	/// </summary>
	public static void HandleWindowResize(
		EventReader<UiPointerEvent> events,
		Query<Data<FloatingWindowState, UiNode>> windows,
		Res<ClayPointerState> pointer)
	{
		// Similar to HandleWindowDrag, but:
		// 1. Detect resize handle clicks
		// 2. Update window size based on pointer position
		// 3. Clamp to min/max size
		// 4. Update UiNode sizing configuration
	}
}

using System;
using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using EcsID = ulong;

namespace TinyEcs.UI.Widgets;

/// <summary>
/// Component to track scroll state.
/// </summary>
public struct ScrollState
{
	public Vector2 ScrollOffset;
	public Vector2 ContentSize;
	public Vector2 ViewportSize;
	public bool IsScrolling;

	// Scrollbar drag state
	public bool IsScrollbarDragging;
	public float ScrollbarDragStartY;

	// Scroll metrics (similar to FloatingWindowState)
	public float ScrollOffsetY;
	public float MaxScrollY;
	public float ViewportHeight;
	public float ContentHeight;

	public readonly Vector2 MaxScroll => new(
		Math.Max(0, ContentSize.X - ViewportSize.X),
		Math.Max(0, ContentSize.Y - ViewportSize.Y));

	public void ClampScroll()
	{
		var max = MaxScroll;
		ScrollOffset.X = Math.Clamp(ScrollOffset.X, 0, max.X);
		ScrollOffset.Y = Math.Clamp(ScrollOffset.Y, 0, max.Y);
	}
}

/// <summary>
/// Links to key parts of a scroll container for system access.
/// </summary>
public struct ScrollContainerLinks
{
	public EcsID ContentWrapperId;  // The clip container
	public EcsID ContentAreaId;     // The actual content
	public EcsID ScrollbarTrackId;
	public EcsID ScrollbarThumbLayerId;
	public EcsID ScrollbarThumbId;
}

/// <summary>
/// Style configuration for scroll container widgets.
/// </summary>
public readonly record struct ClayScrollContainerStyle(
	Vector2 Size,
	Clay_Color BackgroundColor,
	Clay_Color ScrollbarColor,
	Clay_Color ScrollbarHoverColor,
	float ScrollbarWidth,
	Clay_Padding Padding,
	ushort ChildGap,
	bool ShowScrollbarX,
	bool ShowScrollbarY,
	Clay_CornerRadius CornerRadius)
{
	public static ClayScrollContainerStyle Default => new(
		new Vector2(300f, 400f),
		new Clay_Color(31, 41, 55, 255),
		new Clay_Color(75, 85, 99, 255),
		new Clay_Color(107, 114, 128, 255),
		8f,
		Clay_Padding.All(12),
		8,
		false,
		true,
		Clay_CornerRadius.All(4));

	public static ClayScrollContainerStyle Compact => Default with
	{
		Size = new Vector2(200f, 250f),
		Padding = Clay_Padding.All(8),
		ScrollbarWidth = 6f
	};

	public static ClayScrollContainerStyle Wide => Default with
	{
		Size = new Vector2(600f, 400f),
		ShowScrollbarX = true
	};
}

/// <summary>
/// Creates scrollable container widgets that can contain overflow content.
/// </summary>
public static class ScrollContainerWidget
{
	/// <summary>
	/// Creates a scrollable container entity.
	/// </summary>
	public static EntityCommands Create(
		Commands commands,
		ClayScrollContainerStyle style,
		EcsID? parent = default)
	{
		// Create main container with horizontal layout (content + scrollbar side-by-side)
		var container = commands.Spawn();

		// Use Fit sizing to respect max dimensions
		var sizing = new Clay_Sizing(
			Clay_SizingAxis.Fit(0, style.Size.X),
			Clay_SizingAxis.Fit(0, style.Size.Y));

		container.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = sizing,
					layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT
				},
				backgroundColor = style.BackgroundColor,
				cornerRadius = style.CornerRadius
			}
		});

		if (parent.HasValue && parent.Value != 0)
		{
			container.Insert(UiNodeParent.For(parent.Value));
		}

		// Create content wrapper (first child - left side)
		// This is the scroll container with clipping
		var contentWrapper = commands.Spawn();
		contentWrapper.Insert(new UiNode
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
					vertical = style.ShowScrollbarY,
					horizontal = style.ShowScrollbarX,
					childOffset = new Clay_Vector2 { x = 0, y = 0 }
				}
			}
		});
		// Mark as scroll container for Clay scroll handling
		contentWrapper.Insert(new UiScrollContainer());
		contentWrapper.Insert(UiNodeParent.For(container.Id));

		// Create scrollable content area (child of wrapper)
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
					padding = style.Padding,
					childGap = style.ChildGap,
					layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM
				}
			}
		});
		contentArea.Insert(UiNodeParent.For(contentWrapper.Id));

		// Track scrollbar entity IDs
		EcsID scrollbarTrackId = 0;
		EcsID scrollbarThumbLayerId = 0;
		EcsID scrollbarThumbId = 0;

		// Create vertical scrollbar (second child - right side)
		if (style.ShowScrollbarY)
		{
			var scrollbarIds = CreateScrollbar(
				commands,
				container.Id,
				style.ScrollbarWidth,
				style.Size.Y,
				style.ScrollbarColor,
				isVertical: true);
			scrollbarTrackId = scrollbarIds.trackId;
			scrollbarThumbLayerId = scrollbarIds.thumbLayerId;
			scrollbarThumbId = scrollbarIds.thumbId;
		}

		// Create horizontal scrollbar if enabled
		if (style.ShowScrollbarX)
		{
			// TODO: Track horizontal scrollbar IDs if needed
			CreateScrollbar(
				commands,
				container.Id,
				style.Size.X,
				style.ScrollbarWidth,
				style.ScrollbarColor,
				isVertical: false);
		}

		// Add scroll state
		container.Insert(new ScrollState
		{
			ScrollOffset = Vector2.Zero,
			ContentSize = Vector2.Zero,
			ViewportSize = style.Size,
			IsScrolling = false,
			IsScrollbarDragging = false,
			ScrollbarDragStartY = 0,
			ScrollOffsetY = 0,
			MaxScrollY = 0,
			ViewportHeight = style.Size.Y,
			ContentHeight = 0
		});

		// Add links to scroll container parts
		container.Insert(new ScrollContainerLinks
		{
			ContentWrapperId = contentWrapper.Id,
			ContentAreaId = contentArea.Id,
			ScrollbarTrackId = scrollbarTrackId,
			ScrollbarThumbLayerId = scrollbarThumbLayerId,
			ScrollbarThumbId = scrollbarThumbId
		});

		return container;
	}

	/// <summary>
	/// Creates a vertical-only scrollable container.
	/// </summary>
	public static EntityCommands CreateVertical(
		Commands commands,
		Vector2 size,
		EcsID? parent = default)
	{
		return Create(commands, ClayScrollContainerStyle.Default with
		{
			Size = size,
			ShowScrollbarX = false,
			ShowScrollbarY = true,
			Padding = Clay_Padding.All(0)
		}, parent);
	}

	/// <summary>
	/// Creates a horizontal-only scrollable container.
	/// </summary>
	public static EntityCommands CreateHorizontal(
		Commands commands,
		Vector2 size,
		EcsID? parent = default)
	{
		return Create(commands, ClayScrollContainerStyle.Default with
		{
			Size = size,
			ShowScrollbarX = true,
			ShowScrollbarY = false
		}, parent);
	}

	private static (EcsID trackId, EcsID thumbLayerId, EcsID thumbId) CreateScrollbar(
		Commands commands,
		EcsID parent,
		float width,
		float height,
		Clay_Color color,
		bool isVertical)
	{
		// Create scrollbar track (background bar) - uses layout positioning, no floating
		var scrollbar = commands.Spawn();
		scrollbar.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(isVertical ? 8f : width),
						isVertical ? Clay_SizingAxis.Grow() : Clay_SizingAxis.Fixed(8f))
				},
				backgroundColor = color,
				cornerRadius = Clay_CornerRadius.All(4)
			}
		});
		scrollbar.Insert(UiNodeParent.For(parent));

		// Create thumb layer - uses padding to position thumb based on scroll offset
		var thumbLayer = commands.Spawn();
		thumbLayer.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(isVertical ? 8f : width),
						isVertical ? Clay_SizingAxis.Grow() : Clay_SizingAxis.Fixed(8f)),
					layoutDirection = isVertical
						? Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM
						: Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
					padding = new Clay_Padding
					{
						left = 0,
						right = 0,
						top = 0,
						bottom = 0
					}
				}
			}
		});
		thumbLayer.Insert(UiNodeParent.For(scrollbar.Id));

		// Create scrollbar thumb (draggable handle) - layout-based positioning, respects clipping
		var thumb = commands.Spawn();
		thumb.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(isVertical ? 6f : 40f),
						Clay_SizingAxis.Fixed(isVertical ? 40f : 6f))
				},
				backgroundColor = new Clay_Color(156, 163, 175, 255),
				cornerRadius = Clay_CornerRadius.All(3)
			}
		});
		thumb.Insert(UiNodeParent.For(thumbLayer.Id));

		return (scrollbar.Id, thumbLayer.Id, thumb.Id);
	}

	/// <summary>
	/// System to handle scroll wheel input.
	/// Use this as a reference for implementing scroll interactions.
	/// </summary>
	public static void HandleScrollInput(
		EventReader<UiPointerEvent> events,
		Query<Data<ScrollState, UiNode>> scrollContainers,
		Res<ClayPointerState> pointer)
	{
		// Check for scroll wheel events
		ref readonly var pointerState = ref pointer.Value;

		if (pointerState.ScrollDelta != Vector2.Zero)
		{
			foreach (var (state, node) in scrollContainers)
			{
				// This is simplified - you would need to check if the pointer
				// is over this specific scroll container

				ref var stateRef = ref state.Ref;

				// Apply scroll delta (typically Y axis for vertical scrolling)
				stateRef.ScrollOffset.Y += pointerState.ScrollDelta.Y * 20f; // Scroll speed multiplier
				stateRef.ScrollOffset.X += pointerState.ScrollDelta.X * 20f;

				// Clamp to valid range
				stateRef.ClampScroll();

				// Update content position based on scroll offset
				// This would require updating child entity positions
			}
		}
	}
}

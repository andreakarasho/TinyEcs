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
		// Create main container
		var container = commands.Spawn();
		container.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(style.Size.X),
						Clay_SizingAxis.Fixed(style.Size.Y)),
					layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM
				},
				backgroundColor = style.BackgroundColor,
				cornerRadius = style.CornerRadius,
				clip = new Clay_ClipElementConfig()
				{
					// Enable clipping to hide overflow content
				}
			}
		});

		if (parent.HasValue && parent.Value != 0)
		{
			container.Insert(UiNodeParent.For(parent.Value));
		}

		// Create scrollable content area
		var contentArea = commands.Spawn();
		var scrollbarSpace = (style.ShowScrollbarY ? style.ScrollbarWidth : 0f);
		var contentWidth = style.Size.X - scrollbarSpace;

		contentArea.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(contentWidth),
						Clay_SizingAxis.Fit(0, float.MaxValue)),
					padding = style.Padding,
					childGap = style.ChildGap,
					layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM
				}
			}
		});
		contentArea.Insert(UiNodeParent.For(container.Id));

		// Create vertical scrollbar if enabled
		if (style.ShowScrollbarY)
		{
			CreateScrollbar(
				commands,
				container.Id,
				style.ScrollbarWidth,
				style.Size.Y,
				style.ScrollbarColor,
				isVertical: true);
		}

		// Create horizontal scrollbar if enabled
		if (style.ShowScrollbarX)
		{
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
			IsScrolling = false
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
			ShowScrollbarY = true
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

	private static void CreateScrollbar(
		Commands commands,
		EcsID parent,
		float width,
		float height,
		Clay_Color color,
		bool isVertical)
	{
		var scrollbar = commands.Spawn();

		var offsetX = isVertical ? width - 8f : 0f;
		var offsetY = isVertical ? 0f : height - 8f;

		scrollbar.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(isVertical ? 8f : width),
						Clay_SizingAxis.Fixed(isVertical ? height : 8f))
				},
				backgroundColor = color,
				cornerRadius = Clay_CornerRadius.All(4),
				floating = new Clay_FloatingElementConfig
				{
					offset = new Clay_Vector2 { x = offsetX, y = offsetY },
					zIndex = 10,
					parentId = parent.GetHashCode() > 0 ? (uint)parent.GetHashCode() : 0,
					attachPoints = new Clay_FloatingAttachPoints
					{
						element = Clay_FloatingAttachPointType.CLAY_ATTACH_POINT_LEFT_TOP,
						parent = Clay_FloatingAttachPointType.CLAY_ATTACH_POINT_LEFT_TOP
					}
				}
			}
		});
		scrollbar.Insert(UiNodeParent.For(parent));

		// Create scrollbar thumb
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
		thumb.Insert(UiNodeParent.For(scrollbar.Id));
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

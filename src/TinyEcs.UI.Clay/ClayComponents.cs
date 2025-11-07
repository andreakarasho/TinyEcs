using System.Numerics;
using Clay_cs;

namespace TinyEcs.UI.Clay;

/// <summary>
/// Core Clay layout and style component.
/// Contains all configuration for Clay element layout, styling, and behavior.
/// </summary>
public struct ClayNode
{
	// Layout configuration
	public Clay_LayoutConfig Layout;

	// Visual styling
	public Clay_RectangleRenderData? Rectangle;
	public Clay_BorderElementConfig? Border;
	public Clay_CornerRadius? CornerRadius;

	// Text configuration (optional)
	public ClayText? Text;

	// Image configuration (optional)
	public Clay_ImageElementConfig? Image;

	// Floating element configuration (optional)
	public Clay_FloatingElementConfig? Floating;

	// Scroll container configuration (optional)
	public Clay_ClipElementConfig? Clip;

	// Custom element data (optional)
	public Clay_CustomElementConfig? Custom;

	public static ClayNode Default => new ClayNode
	{
		Layout = new Clay_LayoutConfig
		{
			sizing = new Clay_Sizing(
				Clay_SizingAxis.Grow(),
				Clay_SizingAxis.Grow()
			),
			layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
			childAlignment = new Clay_ChildAlignment(
				Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT,
				Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP
			)
		}
	};

	public static ClayNodeBuilder Configure() => new ClayNodeBuilder();
}

/// <summary>
/// Links an ECS entity to a Clay element ID.
/// Used for retained mode rendering and interaction tracking.
/// </summary>
public readonly struct ClayElementId(Clay_ElementId id)
{
	public readonly Clay_ElementId Id = id;

	public static ClayElementId From(ulong entityId)
	{
		return new ClayElementId(new Clay_ElementId()
		{
			id = (uint)IDOp.RealID(entityId),
			offset = (uint)IDOp.GetGeneration(entityId)
		});
		// Use entity ID directly as Clay element ID
		// return new(Clay_cs.Clay.Id(IDOp.RealID(entityId).ToString()));
	}
}

/// <summary>
/// Stores the computed layout bounds from Clay layout calculation.
/// Updated each frame by the layout system.
/// </summary>
public struct ClayComputedLayout
{
	public float X;
	public float Y;
	public float Width;
	public float Height;

	public Vector2 Position => new Vector2(X, Y);
	public Vector2 Size => new Vector2(Width, Height);
}

/// <summary>
/// Optional text content for Clay text elements.
/// </summary>
public struct ClayText
{
	public string Text;
	public Clay_TextElementConfig Config;

	public static ClayText From(string text) => new ClayText
	{
		Text = text,
		Config = new Clay_TextElementConfig()
		{
			fontSize = 16,
			textColor = new Clay_Color(255, 255, 255, 255)
		}
	};
}

/// <summary>
/// Marks this element as a Clay scroll container.
/// Stores scroll offset state that persists across frames.
/// </summary>
public struct ClayScrollContainer
{
	public Vector2 ScrollOffset;
	public Vector2 ScrollVelocity;

	public static ClayScrollContainer Default => new ClayScrollContainer
	{
		ScrollOffset = Vector2.Zero,
		ScrollVelocity = Vector2.Zero
	};
}

/// <summary>
/// Marks this entity as needing Clay layout recalculation.
/// Automatically added when ClayNode changes are detected.
/// </summary>
public struct ClayDirty { }

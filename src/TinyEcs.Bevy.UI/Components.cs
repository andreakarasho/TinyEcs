using System.Numerics;
using ClayColor = Clay.Color;

namespace TinyEcs.Bevy.UI;

public struct Node
{
	public Display Display;
	public PositionType PositionType;
	public Overflow Overflow;
	public FlexDirection FlexDirection;
	public JustifyContent JustifyContent;
	public AlignItems AlignItems;
	public Val Width, Height;
	public Val MinWidth, MinHeight;
	public Val MaxWidth, MaxHeight;
	public Val Left, Top, Right, Bottom;
	public UiRect Padding;
	public UiRect Border;
	public Val Gap;
	public float AspectRatio;

	public static readonly Node Default = new()
	{
		Display = Display.Flex,
		PositionType = PositionType.Relative,
		Overflow = Overflow.Visible,
		FlexDirection = FlexDirection.Row,
		JustifyContent = JustifyContent.Start,
		AlignItems = AlignItems.Start,
		Width = Val.Auto, Height = Val.Auto,
		MinWidth = Val.Auto, MinHeight = Val.Auto,
		MaxWidth = Val.Auto, MaxHeight = Val.Auto,
		Left = Val.Auto, Top = Val.Auto, Right = Val.Auto, Bottom = Val.Auto,
		Padding = UiRect.Zero,
		Border = UiRect.Zero,
		Gap = Val.Px(0),
		AspectRatio = 0,
	};
}

public struct BackgroundColor { public ClayColor Value; public BackgroundColor(ClayColor c) => Value = c; }
public struct BorderColor     { public ClayColor Value; public BorderColor(ClayColor c)     => Value = c; }

public struct BorderRadius
{
	public float TopLeft, TopRight, BottomLeft, BottomRight;
	public static BorderRadius All(float r) => new() { TopLeft = r, TopRight = r, BottomLeft = r, BottomRight = r };
}

public struct Outline
{
	public ClayColor Color;
	public float Width;
	public float Offset;
}

public struct BoxShadow
{
	public ClayColor Color;
	public float OffsetX, OffsetY;
	public float BlurRadius, SpreadRadius;
}

public struct UiImage
{
	public object? ImageData;
	public Vector2 SourceSize;
	public ClayColor Tint;
}

/// Local layering order. Higher values render on top of siblings.
/// Currently only respected on `PositionType.Absolute` elements (Clay z-sorts
/// floating elements only). Non-absolute siblings stack in tree order.
public struct ZIndex       { public int Value; public ZIndex(int v)       => Value = v; }

/// Global layering override. When present, wins over `ZIndex` and is treated as
/// a global z order. Same Absolute-only caveat as `ZIndex`.
public struct GlobalZIndex { public int Value; public GlobalZIndex(int v) => Value = v; }

public struct Text         { public string Value; public Text(string v) => Value = v; }
public struct TextFont     { public ushort FontId; public ushort Size; }
public struct TextColor    { public ClayColor Value; public TextColor(ClayColor c) => Value = c; }

/// <summary>How a Text node's content wraps at the container width. Mirrors
/// Clay's TextWrapMode; absent = Words (Clay's default).</summary>
public enum TextWrapKind : byte { Words = 0, Newlines = 1, None = 2 }
public struct TextWrap     { public TextWrapKind Kind; public TextWrap(TextWrapKind k) => Kind = k; }

public enum Interaction : byte
{
	None = 0,
	Hovered = 1,
	Pressed = 2,
}

public struct FocusPolicy { public bool Block; public static readonly FocusPolicy BlockAll = new() { Block = true }; }

/// <summary>Hit-test opt-out: this element's whole bounding box is the hit
/// target — InteractionSystem skips the host's PixelHitTest hook for it. For
/// elements whose transparent regions must still capture the pointer (a
/// see-through frame, a bar with cutouts).</summary>
public struct UiContainsByBounds;

public struct Button { }

public struct RelativeCursorPosition
{
	public Vector2 Normalized;
	public bool InBounds;
}

/// Current scroll offset of an `Overflow.Scroll` container. Written by the layout
/// pass after EndLayout so user code can read scroll position. Use
/// `UiClayContext.SetScrollPosition` to scroll programmatically.
public struct ScrollPosition
{
	public float OffsetX;
	public float OffsetY;
}

public struct ComputedNode
{
	public Vector2 Size;
	public Vector2 Position;
	public uint ClayId;
	// Index of this element's render command in the frame's command list =
	// paint order. Higher means painted later (drawn on top). Hit-tests that
	// need topmost-first selection among overlapping elements should tiebreak
	// on this rather than ClayId (which is an entity-id hash, unrelated to
	// draw order and unstable across entity recycling).
	public int PaintOrder;
}

/// Marker that makes the layout pass emit a Clay Custom render command for this
/// entity. Renderers can resolve the originating entity via
/// `UiClayContext.ClayToEntity` and pull whatever per-renderer data they need.
/// `Data` is passed through to `Clay.RenderCommand.Custom.CustomData`.
public struct UiCustom
{
	public object? Data;
}

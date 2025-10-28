using System.Numerics;
using Flexbox;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// The core UI layout component that defines the position, size, and layout properties of a UI element.
/// Maps directly to Flexbox layout properties.
/// Equivalent to Bevy's Node component.
/// Every UI entity should have this component.
/// </summary>
public struct UiNode
{
	// === Flexbox Container Properties ===

	/// <summary>Direction children are laid out (row or column)</summary>
	public FlexDirection FlexDirection;

	/// <summary>How children are aligned along the main axis</summary>
	public Justify JustifyContent;

	/// <summary>How children are aligned along the cross axis</summary>
	public Align AlignItems;

	/// <summary>How this element aligns itself within its parent</summary>
	public Align AlignSelf;

	/// <summary>How multiple lines of children are aligned</summary>
	public Align AlignContent;

	/// <summary>Whether children wrap to next line</summary>
	public Wrap FlexWrap;

	// === Display & Positioning ===

	/// <summary>Display type (Flex or None)</summary>
	public Display Display;

	/// <summary>Positioning type (Relative or Absolute)</summary>
	public PositionType PositionType;

	/// <summary>How overflow content is handled</summary>
	public Overflow Overflow;

	// === Flex Item Properties ===

	/// <summary>How much this element grows to fill available space</summary>
	public float FlexGrow;

	/// <summary>How much this element shrinks when space is limited</summary>
	public float FlexShrink;

	/// <summary>Base size before flex grow/shrink is applied</summary>
	public FlexBasis FlexBasis;

	// === Size ===

	/// <summary>Width of the element (points, percent, or auto)</summary>
	public FlexValue Width;

	/// <summary>Height of the element (points, percent, or auto)</summary>
	public FlexValue Height;

	/// <summary>Minimum width constraint</summary>
	public FlexValue MinWidth;

	/// <summary>Minimum height constraint</summary>
	public FlexValue MinHeight;

	/// <summary>Maximum width constraint</summary>
	public FlexValue MaxWidth;

	/// <summary>Maximum height constraint</summary>
	public FlexValue MaxHeight;

	// === Margin (space outside the element) ===

	public FlexValue MarginTop;
	public FlexValue MarginRight;
	public FlexValue MarginBottom;
	public FlexValue MarginLeft;

	// === Padding (space inside the element) ===

	public FlexValue PaddingTop;
	public FlexValue PaddingRight;
	public FlexValue PaddingBottom;
	public FlexValue PaddingLeft;

	// === Border ===

	public FlexValue BorderTop;
	public FlexValue BorderRight;
	public FlexValue BorderBottom;
	public FlexValue BorderLeft;

	// === Position (for absolute positioning) ===

	public FlexValue Top;
	public FlexValue Right;
	public FlexValue Bottom;
	public FlexValue Left;

	// === Factory Methods ===

	/// <summary>Creates a UiNode with default Flexbox values</summary>
	public static UiNode Default()
	{
		return new UiNode
		{
			FlexDirection = FlexDirection.Column,
			JustifyContent = Justify.FlexStart,
			AlignItems = Align.Stretch,
			AlignSelf = Align.Auto,
			AlignContent = Align.Stretch,
			FlexWrap = Wrap.NoWrap,
			Display = Display.Flex,
			PositionType = PositionType.Relative,
			Overflow = Overflow.Visible,
			FlexGrow = 0f,
			FlexShrink = 1f,
			FlexBasis = FlexBasis.Auto(),
			Width = FlexValue.Auto(),
			Height = FlexValue.Auto(),
			MinWidth = FlexValue.Undefined(),
			MinHeight = FlexValue.Undefined(),
			MaxWidth = FlexValue.Undefined(),
			MaxHeight = FlexValue.Undefined(),
			MarginTop = FlexValue.Undefined(),
			MarginRight = FlexValue.Undefined(),
			MarginBottom = FlexValue.Undefined(),
			MarginLeft = FlexValue.Undefined(),
			PaddingTop = FlexValue.Undefined(),
			PaddingRight = FlexValue.Undefined(),
			PaddingBottom = FlexValue.Undefined(),
			PaddingLeft = FlexValue.Undefined(),
			BorderTop = FlexValue.Undefined(),
			BorderRight = FlexValue.Undefined(),
			BorderBottom = FlexValue.Undefined(),
			BorderLeft = FlexValue.Undefined(),
			Top = FlexValue.Undefined(),
			Right = FlexValue.Undefined(),
			Bottom = FlexValue.Undefined(),
			Left = FlexValue.Undefined()
		};
	}
}

/// <summary>
/// Component that defines the background color of a UI element.
/// Similar to Bevy's BackgroundColor component.
/// </summary>
public struct BackgroundColor
{
	/// <summary>RGBA color (0-1 range for each component)</summary>
	public Vector4 Color;

	public BackgroundColor(Vector4 color)
	{
		Color = color;
	}

	public BackgroundColor(float r, float g, float b, float a = 1f)
	{
		Color = new Vector4(r, g, b, a);
	}

	public static BackgroundColor FromRgba(byte r, byte g, byte b, byte a = 255)
	{
		return new BackgroundColor(r / 255f, g / 255f, b / 255f, a / 255f);
	}

	public static BackgroundColor White => new(1f, 1f, 1f, 1f);
	public static BackgroundColor Black => new(0f, 0f, 0f, 1f);
	public static BackgroundColor Red => new(1f, 0f, 0f, 1f);
	public static BackgroundColor Green => new(0f, 1f, 0f, 1f);
	public static BackgroundColor Blue => new(0f, 0f, 1f, 1f);
	public static BackgroundColor Transparent => new(0f, 0f, 0f, 0f);
}

/// <summary>
/// Component that defines the border color of a UI element.
/// </summary>
public struct BorderColor
{
	/// <summary>RGBA color (0-1 range for each component)</summary>
	public Vector4 Color;

	public BorderColor(Vector4 color)
	{
		Color = color;
	}

	public BorderColor(float r, float g, float b, float a = 1f)
	{
		Color = new Vector4(r, g, b, a);
	}

	public static BorderColor FromRgba(byte r, byte g, byte b, byte a = 255)
	{
		return new BorderColor(r / 255f, g / 255f, b / 255f, a / 255f);
	}
}

/// <summary>
/// Component that defines border radius for rounded corners.
/// </summary>
public struct BorderRadius
{
	public float Radius;

	public BorderRadius(float radius)
	{
		Radius = radius;
	}

	public static implicit operator BorderRadius(float radius) => new(radius);
}

/// <summary>
/// Component that stores text content for UI elements.
/// Similar to Bevy's Text component.
/// </summary>
public struct UiText
{
	public string Value;

	public UiText(string value)
	{
		Value = value ?? string.Empty;
	}

	public static implicit operator UiText(string value) => new(value);
}

/// <summary>
/// Component that defines text styling properties.
/// </summary>
public struct TextStyle
{
	public float FontSize;
	public Vector4 Color;

	public TextStyle(float fontSize = 16f, Vector4 color = default)
	{
		FontSize = fontSize;
		Color = color == default ? new Vector4(1, 1, 1, 1) : color;
	}

	public static TextStyle Default() => new(16f, new Vector4(1, 1, 1, 1));
}

/// <summary>
/// Component that marks a UI element as interactive (can receive pointer events).
/// Similar to Bevy's Interaction component.
/// </summary>
public struct Interactive
{
	/// <summary>Whether this element can receive keyboard focus</summary>
	public bool Focusable;

	public Interactive(bool focusable = true)
	{
		Focusable = focusable;
	}
}

/// <summary>
/// Component that stores the computed layout result from Flexbox.
/// This is set by the layout system and read by renderers.
/// Internal use - not typically set by users.
/// </summary>
public struct ComputedLayout
{
	public float X;
	public float Y;
	public float Width;
	public float Height;

	public ComputedLayout(float x, float y, float width, float height)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	/// <summary>
	/// Checks if a point is within the bounds of this layout.
	/// </summary>
	/// <param name="point">The point to check</param>
	/// <returns>True if the point is within bounds, false otherwise</returns>
	public readonly bool Contains(Vector2 point)
	{
		return point.X >= X && point.X <= X + Width &&
		       point.Y >= Y && point.Y <= Y + Height;
	}
}

/// <summary>
/// Component that links a UI entity to its Flexbox node.
/// Internal use - managed by the layout system.
/// </summary>
public struct FlexboxNodeRef
{
	/// <summary>Reference to the Flexbox Node object in the layout tree</summary>
	public global::Flexbox.Node? Node;

	/// <summary>Unique element ID for event targeting</summary>
	public uint ElementId;
}

/// <summary>
/// Component that makes a UI container scrollable.
/// Allows content to overflow the container bounds with scroll offset.
/// </summary>
public struct Scrollable
{
	/// <summary>Current scroll offset (positive = scrolled down/right)</summary>
	public Vector2 ScrollOffset;

	/// <summary>Total content size (can be larger than container)</summary>
	public Vector2 ContentSize;

	/// <summary>Enable horizontal scrolling</summary>
	public bool EnableHorizontal;

	/// <summary>Enable vertical scrolling</summary>
	public bool EnableVertical;

	/// <summary>Scroll speed multiplier</summary>
	public float ScrollSpeed;

	public Scrollable(bool vertical = true, bool horizontal = false, float scrollSpeed = 20f)
	{
		ScrollOffset = Vector2.Zero;
		ContentSize = Vector2.Zero;
		EnableVertical = vertical;
		EnableHorizontal = horizontal;
		ScrollSpeed = scrollSpeed;
	}
}

/// <summary>
/// Component that makes a UI element draggable.
/// Allows the element to be moved by clicking and dragging.
/// </summary>
public struct Draggable
{
	/// <summary>Whether this element is currently being dragged</summary>
	public bool IsDragging;

	/// <summary>Offset from element's top-left corner to the mouse position when drag started</summary>
	public Vector2 DragOffset;

	/// <summary>The position where the element should be placed (overrides Flexbox layout)</summary>
	public Vector2 Position;

	/// <summary>Whether position has been set (used to override Flexbox)</summary>
	public bool HasCustomPosition;

	public Draggable()
	{
		IsDragging = false;
		DragOffset = Vector2.Zero;
		Position = Vector2.Zero;
		HasCustomPosition = false;
	}
}

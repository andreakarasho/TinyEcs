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
/// Supports individual corner radii (like CSS border-radius).
/// </summary>
public struct BorderRadius
{
	/// <summary>Top-left corner radius</summary>
	public float TopLeft;

	/// <summary>Top-right corner radius</summary>
	public float TopRight;

	/// <summary>Bottom-right corner radius</summary>
	public float BottomRight;

	/// <summary>Bottom-left corner radius</summary>
	public float BottomLeft;

	/// <summary>Creates a BorderRadius with the same radius for all corners</summary>
	public BorderRadius(float radius)
	{
		TopLeft = radius;
		TopRight = radius;
		BottomRight = radius;
		BottomLeft = radius;
	}

	/// <summary>Creates a BorderRadius with individual corner radii</summary>
	public BorderRadius(float topLeft, float topRight, float bottomRight, float bottomLeft)
	{
		TopLeft = topLeft;
		TopRight = topRight;
		BottomRight = bottomRight;
		BottomLeft = bottomLeft;
	}

	/// <summary>Creates a BorderRadius with separate top and bottom radii (top-left/top-right, bottom-right/bottom-left)</summary>
	public static BorderRadius FromTopBottom(float top, float bottom)
	{
		return new BorderRadius(top, top, bottom, bottom);
	}

	/// <summary>Creates a BorderRadius with separate left and right radii (top-left/bottom-left, top-right/bottom-right)</summary>
	public static BorderRadius FromLeftRight(float left, float right)
	{
		return new BorderRadius(left, right, right, left);
	}

	/// <summary>Gets the uniform radius value (returns TopLeft if all corners are equal)</summary>
	public readonly float Radius => TopLeft;

	/// <summary>Checks if all corners have the same radius</summary>
	public readonly bool IsUniform => TopLeft == TopRight && TopRight == BottomRight && BottomRight == BottomLeft;

	public static implicit operator BorderRadius(float radius) => new(radius);
}

/// <summary>
/// Component that defines border thickness for each edge.
/// Supports individual edge thickness (like CSS border-width).
/// Similar to Bevy's UiRect for borders.
/// </summary>
public struct BorderThickness
{
	/// <summary>Top edge thickness</summary>
	public float Top;

	/// <summary>Right edge thickness</summary>
	public float Right;

	/// <summary>Bottom edge thickness</summary>
	public float Bottom;

	/// <summary>Left edge thickness</summary>
	public float Left;

	/// <summary>Creates a BorderThickness with the same thickness for all edges</summary>
	public BorderThickness(float thickness)
	{
		Top = thickness;
		Right = thickness;
		Bottom = thickness;
		Left = thickness;
	}

	/// <summary>Creates a BorderThickness with individual edge thickness</summary>
	public BorderThickness(float top, float right, float bottom, float left)
	{
		Top = top;
		Right = right;
		Bottom = bottom;
		Left = left;
	}

	/// <summary>Creates a BorderThickness with horizontal (left/right) and vertical (top/bottom) values</summary>
	public static BorderThickness FromHorizontalVertical(float horizontal, float vertical)
	{
		return new BorderThickness(vertical, horizontal, vertical, horizontal);
	}

	/// <summary>Creates a BorderThickness with only horizontal edges (left/right)</summary>
	public static BorderThickness Horizontal(float thickness)
	{
		return new BorderThickness(0f, thickness, 0f, thickness);
	}

	/// <summary>Creates a BorderThickness with only vertical edges (top/bottom)</summary>
	public static BorderThickness Vertical(float thickness)
	{
		return new BorderThickness(thickness, 0f, thickness, 0f);
	}

	/// <summary>Gets the uniform thickness value (returns Top if all edges are equal)</summary>
	public readonly float Thickness => Top;

	/// <summary>Checks if all edges have the same thickness</summary>
	public readonly bool IsUniform => Top == Right && Right == Bottom && Bottom == Left;

	/// <summary>Gets the maximum thickness value across all edges</summary>
	public readonly float Max => Math.Max(Math.Max(Top, Right), Math.Max(Bottom, Left));

	public static implicit operator BorderThickness(float thickness) => new(thickness);
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
/// Text alignment options for horizontal positioning within the layout area.
/// </summary>
public enum TextAlign
{
	/// <summary>Align text to the left (default)</summary>
	Left,
	/// <summary>Center text horizontally</summary>
	Center,
	/// <summary>Align text to the right</summary>
	Right
}

/// <summary>
/// Text alignment options for vertical positioning within the layout area.
/// </summary>
public enum TextVerticalAlign
{
	/// <summary>Align text to the top (default)</summary>
	Top,
	/// <summary>Center text vertically</summary>
	Middle,
	/// <summary>Align text to the bottom</summary>
	Bottom
}

/// <summary>
/// Component that defines text styling properties.
/// </summary>
public struct TextStyle
{
	public float FontSize;
	public Vector4 Color;
	public TextAlign HorizontalAlign;
	public TextVerticalAlign VerticalAlign;

	public TextStyle(
		float fontSize = 16f,
		Vector4 color = default,
		TextAlign horizontalAlign = TextAlign.Left,
		TextVerticalAlign verticalAlign = TextVerticalAlign.Top)
	{
		FontSize = fontSize;
		Color = color == default ? new Vector4(1, 1, 1, 1) : color;
		HorizontalAlign = horizontalAlign;
		VerticalAlign = verticalAlign;
	}

	public static TextStyle Default() => new(16f, new Vector4(1, 1, 1, 1), TextAlign.Left, TextVerticalAlign.Top);
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
/// Enhanced version that tracks both cumulative scroll offset and last scroll event.
/// </summary>
public struct Scrollable
{
	/// <summary>Current scroll offset (positive = scrolled down/right)</summary>
	public Vector2 ScrollOffset;

    /// <summary>Total content size (can be larger than container)</summary>
    public Vector2 ContentSize;

    /// <summary>
    /// Local origin of the content within the container, in container space.
    /// Typically equals the minimum X/Y of child bounds relative to the container's top-left.
    /// Used to normalize content so that initial view shows the first elements at offset 0.
    /// </summary>
    public Vector2 ContentOrigin;

	/// <summary>Enable horizontal scrolling</summary>
	public bool EnableHorizontal;

	/// <summary>Enable vertical scrolling</summary>
	public bool EnableVertical;

	/// <summary>Scroll speed multiplier for wheel events</summary>
	public float ScrollSpeed;

	// === Last Scroll Event Tracking (from sickle_ui) ===

	/// <summary>Axis of the last scroll event (null if no event this frame)</summary>
	public ScrollAxis? LastScrollAxis;

	/// <summary>Delta of the last scroll event</summary>
	public float LastScrollDelta;

	/// <summary>Unit of the last scroll event (Line or Pixel)</summary>
	public ScrollUnit LastScrollUnit;

    public Scrollable(bool vertical = true, bool horizontal = false, float scrollSpeed = 20f)
    {
        ScrollOffset = Vector2.Zero;
        ContentSize = Vector2.Zero;
        ContentOrigin = Vector2.Zero;
        EnableVertical = vertical;
        EnableHorizontal = horizontal;
        ScrollSpeed = scrollSpeed;
        LastScrollAxis = null;
        LastScrollDelta = 0f;
		LastScrollUnit = ScrollUnit.Pixel;
	}

	/// <summary>
	/// Gets the last scroll change if any occurred this frame.
	/// Returns null if no scroll event happened.
	/// </summary>
	public readonly (ScrollAxis axis, float delta, ScrollUnit unit)? GetLastChange()
	{
		if (LastScrollAxis.HasValue)
		{
			return (LastScrollAxis.Value, LastScrollDelta, LastScrollUnit);
		}
		return null;
	}
}

/// <summary>
/// Scroll axis direction.
/// </summary>
public enum ScrollAxis
{
	Horizontal,
	Vertical
}

/// <summary>
/// Scroll unit type (from Bevy's MouseScrollUnit).
/// </summary>
public enum ScrollUnit
{
	/// <summary>Scroll by lines (typically 20px per line)</summary>
	Line,
	/// <summary>Scroll by exact pixels</summary>
	Pixel
}

/// <summary>
/// Component that makes a UI element draggable.
/// Enhanced version with state machine from sickle_ui.
/// Tracks drag lifecycle: Inactive → MaybeDragged → DragStart → Dragging → DragEnd/DragCanceled.
/// </summary>
public struct Draggable
{
	/// <summary>Current drag state</summary>
	public DragState State;

	/// <summary>Position where the drag originated (where the mouse was pressed)</summary>
	public Vector2? Origin;

	/// <summary>Current position during drag</summary>
	public Vector2? Position;

	/// <summary>Delta/difference in position since last frame</summary>
	public Vector2? Diff;

	/// <summary>Source of the drag (Mouse or Touch)</summary>
	public DragSource Source;

	// Legacy fields for backwards compatibility
	/// <summary>Whether this element is currently being dragged (computed from State)</summary>
	public readonly bool IsDragging => State == DragState.DragStart || State == DragState.Dragging;

	/// <summary>Offset from element's top-left corner to the mouse position when drag started</summary>
	public Vector2 DragOffset;

	/// <summary>Whether position has been set (used to override Flexbox)</summary>
	public bool HasCustomPosition;

	public Draggable()
	{
		State = DragState.Inactive;
		Origin = null;
		Position = null;
		Diff = null;
		Source = DragSource.Mouse;
		DragOffset = Vector2.Zero;
		HasCustomPosition = false;
	}

	/// <summary>Clears drag tracking state</summary>
	public void Clear()
	{
		Origin = null;
		Position = null;
		Diff = Vector2.Zero;
	}
}

/// <summary>
/// Drag state lifecycle (from sickle_ui).
/// </summary>
public enum DragState
{
	/// <summary>Not dragging</summary>
	Inactive,
	/// <summary>Pointer pressed but not moved yet - waiting to see if this is a drag</summary>
	MaybeDragged,
	/// <summary>First frame of drag (movement detected)</summary>
	DragStart,
	/// <summary>Actively dragging</summary>
	Dragging,
	/// <summary>Drag completed successfully</summary>
	DragEnd,
	/// <summary>Drag cancelled (ESC key or pointer released outside)</summary>
	DragCanceled
}

/// <summary>
/// Source of drag input (from sickle_ui).
/// </summary>
public enum DragSource
{
	Mouse,
	Touch // Note: Touch ID tracking not implemented yet
}

/// <summary>
/// Resource that provides text measurement functionality.
/// Renderers must register their measurement implementation here.
/// The callback receives the text content and style, and returns intrinsic dimensions.
/// </summary>
public class TextMeasureContext
{
	/// <summary>
	/// Callback to measure text dimensions.
	/// Parameters: (text, textStyle) => (width, height)
	/// The callback should measure the text with the given style (fontSize, etc.)
	/// and return the intrinsic dimensions in pixels.
	/// Note: Alignment properties don't affect intrinsic measurement, only rendering.
	/// </summary>
	public Func<string, TextStyle, (float width, float height)>? MeasureText { get; set; }

	/// <summary>
	/// Internal: Callback to get component data for an entity during measurement.
	/// Parameters: (entityId) => (text, textStyle)
	/// Set by the FlexboxUiPlugin.
	/// </summary>
	internal Func<ulong, (string text, TextStyle style)>? GetTextData { get; set; }
}

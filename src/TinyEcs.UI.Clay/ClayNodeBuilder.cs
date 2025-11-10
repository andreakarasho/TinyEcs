using Clay_cs;

namespace TinyEcs.UI.Clay;

/// <summary>
/// Fluent builder for ClayNode configuration.
///
/// Example usage:
/// <code>
/// // Before (verbose):
/// var node = ClayNode.Default with
/// {
///     Layout = new Clay_LayoutConfig
///     {
///         sizing = new Clay_Sizing(
///             Clay_SizingAxis.Fixed(200),
///             Clay_SizingAxis.Fixed(50)
///         ),
///         padding = Clay_Padding.All(8),
///         layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
///         childAlignment = new Clay_ChildAlignment(
///             Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
///             Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
///         )
///     },
///     Rectangle = new Clay_RectangleRenderData
///     {
///         backgroundColor = new Clay_Color(100, 120, 200, 255)
///     },
///     CornerRadius = Clay_CornerRadius.All(4),
///     Text = new ClayText
///     {
///         Text = "Click Me",
///         Config = new Clay_TextElementConfig
///         {
///             fontSize = 16,
///             textColor = new Clay_Color(255, 255, 255, 255)
///         }
///     }
/// };
///
/// // After (fluent):
/// var node = ClayNode.Configure()
///     .Size(200, 50)
///     .Padding(8)
///     .Row()
///     .AlignCenter()
///     .Background(100, 120, 200)
///     .CornerRadius(4)
///     .Text("Click Me", 16, new Clay_Color(255, 255, 255, 255))
///     .Build();
/// </code>
/// </summary>
public ref struct ClayNodeBuilder
{
	private ClayNode _node;

	public ClayNodeBuilder()
	{
		_node = ClayNode.Default;
	}

	public ClayNodeBuilder(ClayNode node)
	{
		_node = node;
	}

	// Layout methods

	/// <summary>
	/// Sets a fixed width in pixels.
	/// </summary>
	public ClayNodeBuilder Width(float width)
	{
		_node.Layout.sizing.width = Clay_SizingAxis.Fixed(width);
		return this;
	}

	/// <summary>
	/// Sets a fixed height in pixels.
	/// </summary>
	public ClayNodeBuilder Height(float height)
	{
		_node.Layout.sizing.height = Clay_SizingAxis.Fixed(height);
		return this;
	}

	/// <summary>
	/// Sets width to grow and fill available space in the parent container.
	/// </summary>
	public ClayNodeBuilder WidthGrow()
	{
		_node.Layout.sizing.width = Clay_SizingAxis.Grow();
		return this;
	}

	/// <summary>
	/// Sets height to grow and fill available space in the parent container.
	/// </summary>
	public ClayNodeBuilder HeightGrow()
	{
		_node.Layout.sizing.height = Clay_SizingAxis.Grow();
		return this;
	}

	/// <summary>
	/// Sets width to fit content/children, with optional min/max constraints.
	/// </summary>
	public ClayNodeBuilder WidthFit(float min = 0, float max = 0)
	{
		_node.Layout.sizing.width = Clay_SizingAxis.Fit(min, max);
		return this;
	}

	/// <summary>
	/// Sets height to fit content/children, with optional min/max constraints.
	/// </summary>
	public ClayNodeBuilder HeightFit(float min = 0, float max = 0)
	{
		_node.Layout.sizing.height = Clay_SizingAxis.Fit(min, max);
		return this;
	}

	/// <summary>
	/// Sets width as a percentage of parent (0-1 range).
	/// </summary>
	public ClayNodeBuilder WidthPercent(float percent)
	{
		_node.Layout.sizing.width = Clay_SizingAxis.Percent(percent);
		return this;
	}

	/// <summary>
	/// Sets height as a percentage of parent (0-1 range).
	/// </summary>
	public ClayNodeBuilder HeightPercent(float percent)
	{
		_node.Layout.sizing.height = Clay_SizingAxis.Percent(percent);
		return this;
	}

	/// <summary>
	/// Sets fixed width and height in pixels.
	/// </summary>
	public ClayNodeBuilder Size(float width, float height)
	{
		_node.Layout.sizing = new Clay_Sizing(
			Clay_SizingAxis.Fixed(width),
			Clay_SizingAxis.Fixed(height)
		);
		return this;
	}

	/// <summary>
	/// Sets padding on all sides.
	/// </summary>
	public ClayNodeBuilder Padding(ushort all)
	{
		_node.Layout.padding = Clay_Padding.All(all);
		return this;
	}

	/// <summary>
	/// Sets padding with separate horizontal (left/right) and vertical (top/bottom) values.
	/// </summary>
	public ClayNodeBuilder Padding(ushort x, ushort y)
	{
		_node.Layout.padding = new Clay_Padding
		{
			left = x,
			right = x,
			top = y,
			bottom = y
		};
		return this;
	}

	/// <summary>
	/// Sets padding for each side individually.
	/// </summary>
	public ClayNodeBuilder Padding(ushort left, ushort right, ushort top, ushort bottom)
	{
		_node.Layout.padding = new Clay_Padding
		{
			left = left,
			right = right,
			top = top,
			bottom = bottom
		};
		return this;
	}

	/// <summary>
	/// Sets the gap between child elements.
	/// </summary>
	public ClayNodeBuilder Gap(ushort gap)
	{
		_node.Layout.childGap = gap;
		return this;
	}

	/// <summary>
	/// Sets layout direction to horizontal (left-to-right).
	/// </summary>
	public ClayNodeBuilder Row()
	{
		_node.Layout.layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT;
		return this;
	}

	/// <summary>
	/// Sets layout direction to vertical (top-to-bottom).
	/// </summary>
	public ClayNodeBuilder Column()
	{
		_node.Layout.layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM;
		return this;
	}

	/// <summary>
	/// Centers child elements both horizontally and vertically.
	/// </summary>
	public ClayNodeBuilder AlignCenter()
	{
		_node.Layout.childAlignment = new Clay_ChildAlignment(
			Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
			Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
		);
		return this;
	}

	/// <summary>
	/// Aligns child elements to the left (preserves existing vertical alignment).
	/// </summary>
	public ClayNodeBuilder AlignLeft()
	{
		_node.Layout.childAlignment = new Clay_ChildAlignment(
			Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT,
			_node.Layout.childAlignment.y
		);
		return this;
	}

	/// <summary>
	/// Aligns child elements to the right (preserves existing vertical alignment).
	/// </summary>
	public ClayNodeBuilder AlignRight()
	{
		_node.Layout.childAlignment = new Clay_ChildAlignment(
			Clay_LayoutAlignmentX.CLAY_ALIGN_X_RIGHT,
			_node.Layout.childAlignment.y
		);
		return this;
	}

	/// <summary>
	/// Aligns child elements to the top (preserves existing horizontal alignment).
	/// </summary>
	public ClayNodeBuilder AlignTop()
	{
		_node.Layout.childAlignment = new Clay_ChildAlignment(
			_node.Layout.childAlignment.x,
			Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP
		);
		return this;
	}

	/// <summary>
	/// Aligns child elements to the bottom (preserves existing horizontal alignment).
	/// </summary>
	public ClayNodeBuilder AlignBottom()
	{
		_node.Layout.childAlignment = new Clay_ChildAlignment(
			_node.Layout.childAlignment.x,
			Clay_LayoutAlignmentY.CLAY_ALIGN_Y_BOTTOM
		);
		return this;
	}

	/// <summary>
	/// Sets both horizontal and vertical alignment of child elements.
	/// </summary>
	public ClayNodeBuilder Align(Clay_LayoutAlignmentX x, Clay_LayoutAlignmentY y)
	{
		_node.Layout.childAlignment = new Clay_ChildAlignment(x, y);
		return this;
	}

	// Visual methods

	/// <summary>
	/// Sets the background color using RGBA values (0-255).
	/// </summary>
	public ClayNodeBuilder Background(byte r, byte g, byte b, byte a = 255)
	{
		_node.Rectangle = new Clay_RectangleRenderData
		{
			backgroundColor = new Clay_Color(r, g, b, a)
		};
		return this;
	}

	/// <summary>
	/// Sets the background color using a Clay_Color.
	/// </summary>
	public ClayNodeBuilder Background(Clay_Color color)
	{
		_node.Rectangle = new Clay_RectangleRenderData
		{
			backgroundColor = color
		};
		return this;
	}

	/// <summary>
	/// Sets the same corner radius for all corners.
	/// </summary>
	public ClayNodeBuilder CornerRadius(float radius)
	{
		_node.CornerRadius = Clay_CornerRadius.All((ushort)radius);
		return this;
	}

	/// <summary>
	/// Sets individual corner radii for each corner.
	/// </summary>
	public ClayNodeBuilder CornerRadius(float topLeft, float topRight, float bottomLeft, float bottomRight)
	{
		_node.CornerRadius = new Clay_CornerRadius
		{
			topLeft = topLeft,
			topRight = topRight,
			bottomLeft = bottomLeft,
			bottomRight = bottomRight
		};
		return this;
	}

	/// <summary>
	/// Sets a border with RGBA color and uniform width on all sides.
	/// </summary>
	public ClayNodeBuilder Border(byte r, byte g, byte b, byte a = 255, ushort width = 1)
	{
		_node.Border = new Clay_BorderElementConfig
		{
			color = new Clay_Color(r, g, b, a),
			width = new Clay_BorderWidth
			{
				left = width,
				right = width,
				top = width,
				bottom = width
			}
		};
		return this;
	}

	/// <summary>
	/// Sets a border with Clay_Color and uniform width on all sides.
	/// </summary>
	public ClayNodeBuilder Border(Clay_Color color, ushort width = 1)
	{
		_node.Border = new Clay_BorderElementConfig
		{
			color = color,
			width = new Clay_BorderWidth
			{
				left = width,
				right = width,
				top = width,
				bottom = width
			}
		};
		return this;
	}

	/// <summary>
	/// Sets a border with Clay_Color and individual widths for each side.
	/// </summary>
	public ClayNodeBuilder Border(Clay_Color color, ushort left, ushort right, ushort top, ushort bottom)
	{
		_node.Border = new Clay_BorderElementConfig
		{
			color = color,
			width = new Clay_BorderWidth
			{
				left = left,
				right = right,
				top = top,
				bottom = bottom
			}
		};
		return this;
	}

	// Text methods

	/// <summary>
	/// Sets text content with optional font size, color, and font ID.
	/// </summary>
	public ClayNodeBuilder Text(string text, ushort fontSize = 16, Clay_Color? color = null, ushort fontId = 0)
	{
		_node.Text = new ClayText
		{
			Text = text,
			Config = new Clay_TextElementConfig
			{
				fontSize = fontSize,
				textColor = color ?? new Clay_Color(255, 255, 255, 255),
				fontId = fontId
			}
		};
		return this;
	}

	/// <summary>
	/// Sets the text color using RGBA values (only applies if text was already set).
	/// </summary>
	public ClayNodeBuilder TextColor(byte r, byte g, byte b, byte a = 255)
	{
		if (_node.Text.HasValue)
		{
			var text = _node.Text.Value;
			text.Config.textColor = new Clay_Color(r, g, b, a);
			_node.Text = text;
		}
		return this;
	}

	/// <summary>
	/// Sets the font size (only applies if text was already set).
	/// </summary>
	public ClayNodeBuilder FontSize(ushort size)
	{
		if (_node.Text.HasValue)
		{
			var text = _node.Text.Value;
			text.Config.fontSize = size;
			_node.Text = text;
		}
		return this;
	}

	// Floating methods

	/// <summary>
	/// Makes this element a floating element (positioned above normal flow) with specified z-index.
	/// Defaults to attaching to parent and capturing pointer events.
	/// </summary>
	public ClayNodeBuilder Floating(
		short zIndex = 100,
		Clay_FloatingAttachToElement attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT,
		Clay_PointerCaptureMode pointerCaptureMode = Clay_PointerCaptureMode.CLAY_POINTER_CAPTURE_MODE_CAPTURE)
	{
		_node.Floating = new Clay_FloatingElementConfig
		{
			zIndex = zIndex,
			attachTo = attachTo,
			pointerCaptureMode = pointerCaptureMode
		};
		return this;
	}

	/// <summary>
	/// Sets the offset position for a floating element (only applies if Floating was already set).
	/// </summary>
	public ClayNodeBuilder FloatingOffset(float x, float y)
	{
		if (_node.Floating.HasValue)
		{
			var floating = _node.Floating.Value;
			floating.offset = new Clay_Vector2 { x = x, y = y };
			_node.Floating = floating;
		}
		return this;
	}

	/// <summary>
	/// Sets the attachment points for a floating element (only applies if Floating was already set).
	/// Defines which point on this element attaches to which point on the parent.
	/// </summary>
	public ClayNodeBuilder FloatingAttachPoints(
		Clay_FloatingAttachPointType element,
		Clay_FloatingAttachPointType parent)
	{
		if (_node.Floating.HasValue)
		{
			var floating = _node.Floating.Value;
			floating.attachPoints = new Clay_FloatingAttachPoints
			{
				element = element,
				parent = parent
			};
			_node.Floating = floating;
		}
		return this;
	}

	/// <summary>
	/// Sets which element this floating element attaches to (only applies if Floating was already set).
	/// </summary>
	public ClayNodeBuilder FloatingAttachTo(Clay_FloatingAttachToElement attachTo)
	{
		if (_node.Floating.HasValue)
		{
			var floating = _node.Floating.Value;
			floating.attachTo = attachTo;
			_node.Floating = floating;
		}
		return this;
	}

	/// <summary>
	/// Sets the pointer capture mode for a floating element (only applies if Floating was already set).
	/// </summary>
	public ClayNodeBuilder FloatingCapture(Clay_PointerCaptureMode mode)
	{
		if (_node.Floating.HasValue)
		{
			var floating = _node.Floating.Value;
			floating.pointerCaptureMode = mode;
			_node.Floating = floating;
		}
		return this;
	}

	// Custom element methods

	/// <summary>
	/// Sets custom element configuration with custom data pointer.
	/// Used for elements that need custom rendering logic.
	/// </summary>
	public unsafe ClayNodeBuilder Custom(nint customData)
	{
		_node.Custom = new Clay_CustomElementConfig
		{
			customData = (void*)customData
		};
		return this;
	}

	// Build

	/// <summary>
	/// Builds and returns the final ClayNode configuration.
	/// </summary>
	public readonly ClayNode Build() => _node;

	/// <summary>
	/// Implicit conversion from ClayNodeBuilder to ClayNode.
	/// </summary>
	public static implicit operator ClayNode(ClayNodeBuilder builder) => builder._node;
}

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
	public ClayNodeBuilder Width(float width)
	{
		_node.Layout.sizing.width = Clay_SizingAxis.Fixed(width);
		return this;
	}

	public ClayNodeBuilder Height(float height)
	{
		_node.Layout.sizing.height = Clay_SizingAxis.Fixed(height);
		return this;
	}

	public ClayNodeBuilder WidthGrow()
	{
		_node.Layout.sizing.width = Clay_SizingAxis.Grow();
		return this;
	}

	public ClayNodeBuilder HeightGrow()
	{
		_node.Layout.sizing.height = Clay_SizingAxis.Grow();
		return this;
	}

	public ClayNodeBuilder WidthFit(float min = 0, float max = 0)
	{
		_node.Layout.sizing.width = Clay_SizingAxis.Fit(min, max);
		return this;
	}

	public ClayNodeBuilder HeightFit(float min = 0, float max = 0)
	{
		_node.Layout.sizing.height = Clay_SizingAxis.Fit(min, max);
		return this;
	}

	public ClayNodeBuilder Size(float width, float height)
	{
		_node.Layout.sizing = new Clay_Sizing(
			Clay_SizingAxis.Fixed(width),
			Clay_SizingAxis.Fixed(height)
		);
		return this;
	}

	public ClayNodeBuilder Padding(ushort all)
	{
		_node.Layout.padding = Clay_Padding.All(all);
		return this;
	}

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

	public ClayNodeBuilder Gap(ushort gap)
	{
		_node.Layout.childGap = gap;
		return this;
	}

	public ClayNodeBuilder Row()
	{
		_node.Layout.layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT;
		return this;
	}

	public ClayNodeBuilder Column()
	{
		_node.Layout.layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM;
		return this;
	}

	public ClayNodeBuilder AlignCenter()
	{
		_node.Layout.childAlignment = new Clay_ChildAlignment(
			Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
			Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
		);
		return this;
	}

	public ClayNodeBuilder AlignLeft()
	{
		_node.Layout.childAlignment = new Clay_ChildAlignment(
			Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT,
			_node.Layout.childAlignment.y
		);
		return this;
	}

	public ClayNodeBuilder AlignRight()
	{
		_node.Layout.childAlignment = new Clay_ChildAlignment(
			Clay_LayoutAlignmentX.CLAY_ALIGN_X_RIGHT,
			_node.Layout.childAlignment.y
		);
		return this;
	}

	public ClayNodeBuilder AlignTop()
	{
		_node.Layout.childAlignment = new Clay_ChildAlignment(
			_node.Layout.childAlignment.x,
			Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP
		);
		return this;
	}

	public ClayNodeBuilder AlignBottom()
	{
		_node.Layout.childAlignment = new Clay_ChildAlignment(
			_node.Layout.childAlignment.x,
			Clay_LayoutAlignmentY.CLAY_ALIGN_Y_BOTTOM
		);
		return this;
	}

	public ClayNodeBuilder Align(Clay_LayoutAlignmentX x, Clay_LayoutAlignmentY y)
	{
		_node.Layout.childAlignment = new Clay_ChildAlignment(x, y);
		return this;
	}

	// Visual methods
	public ClayNodeBuilder Background(byte r, byte g, byte b, byte a = 255)
	{
		_node.Rectangle = new Clay_RectangleRenderData
		{
			backgroundColor = new Clay_Color(r, g, b, a)
		};
		return this;
	}

	public ClayNodeBuilder Background(Clay_Color color)
	{
		_node.Rectangle = new Clay_RectangleRenderData
		{
			backgroundColor = color
		};
		return this;
	}

	public ClayNodeBuilder CornerRadius(float radius)
	{
		_node.CornerRadius = Clay_CornerRadius.All((ushort)radius);
		return this;
	}

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
	public ClayNodeBuilder Floating(short zIndex = 100)
	{
		_node.Floating = new Clay_FloatingElementConfig
		{
			zIndex = zIndex,
			attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT,
			pointerCaptureMode = Clay_PointerCaptureMode.CLAY_POINTER_CAPTURE_MODE_CAPTURE
		};
		return this;
	}

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

	// Build
	public readonly ClayNode Build() => _node;

	public static implicit operator ClayNode(ClayNodeBuilder builder) => builder._node;
}

using System;
using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using EcsID = ulong;

namespace TinyEcs.UI.Widgets;

/// <summary>
/// Style configuration for separator widgets.
/// </summary>
public readonly record struct ClaySeparatorStyle(
	float Thickness,
	Clay_Color Color,
	Clay_Padding Margin,
	Clay_SizingAxis SizingAxis)
{
	public static ClaySeparatorStyle Horizontal => new(
		1f,
		new Clay_Color(75, 85, 99, 255),
		Clay_Padding.Ver(8),
		Clay_SizingAxis.Grow());

	public static ClaySeparatorStyle Vertical => new(
		1f,
		new Clay_Color(75, 85, 99, 255),
		Clay_Padding.Hor(8),
		Clay_SizingAxis.Grow());

	public static ClaySeparatorStyle HorizontalThick => Horizontal with
	{
		Thickness = 2f
	};

	public static ClaySeparatorStyle VerticalThick => Vertical with
	{
		Thickness = 2f
	};

	public static ClaySeparatorStyle HorizontalDotted => Horizontal with
	{
		Color = new Clay_Color(107, 114, 128, 128)
	};

	public static ClaySeparatorStyle VerticalDotted => Vertical with
	{
		Color = new Clay_Color(107, 114, 128, 128)
	};
}

/// <summary>
/// Creates separator/divider widgets for visually separating UI sections.
/// </summary>
public static class SeparatorWidget
{
	/// <summary>
	/// Creates a horizontal separator line.
	/// </summary>
	public static EntityCommands CreateHorizontal(
		Commands commands,
		EcsID? parent = default,
		ClaySeparatorStyle? style = default)
	{
		var actualStyle = style ?? ClaySeparatorStyle.Horizontal;

		var separator = commands.Spawn();
		separator.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						actualStyle.SizingAxis,
						Clay_SizingAxis.Fixed(actualStyle.Thickness)),
					padding = actualStyle.Margin
				},
				backgroundColor = actualStyle.Color
			}
		});

		if (parent.HasValue && parent.Value != 0)
		{
			separator.Insert(UiNodeParent.For(parent.Value));
		}

		return separator;
	}

	/// <summary>
	/// Creates a vertical separator line.
	/// </summary>
	public static EntityCommands CreateVertical(
		Commands commands,
		EcsID? parent = default,
		ClaySeparatorStyle? style = default)
	{
		var actualStyle = style ?? ClaySeparatorStyle.Vertical;

		var separator = commands.Spawn();
		separator.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(actualStyle.Thickness),
						actualStyle.SizingAxis),
					padding = actualStyle.Margin
				},
				backgroundColor = actualStyle.Color
			}
		});

		if (parent.HasValue && parent.Value != 0)
		{
			separator.Insert(UiNodeParent.For(parent.Value));
		}

		return separator;
	}

	/// <summary>
	/// Creates a horizontal separator with custom width.
	/// </summary>
	public static EntityCommands CreateHorizontalFixed(
		Commands commands,
		float width,
		float thickness = 1f,
		Clay_Color? color = default,
		EcsID? parent = default)
	{
		var separator = commands.Spawn();
		separator.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(width),
						Clay_SizingAxis.Fixed(thickness)),
					padding = Clay_Padding.Ver(8)
				},
				backgroundColor = color ?? new Clay_Color(75, 85, 99, 255)
			}
		});

		if (parent.HasValue && parent.Value != 0)
		{
			separator.Insert(UiNodeParent.For(parent.Value));
		}

		return separator;
	}

	/// <summary>
	/// Creates a vertical separator with custom height.
	/// </summary>
	public static EntityCommands CreateVerticalFixed(
		Commands commands,
		float height,
		float thickness = 1f,
		Clay_Color? color = default,
		EcsID? parent = default)
	{
		var separator = commands.Spawn();
		separator.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(thickness),
						Clay_SizingAxis.Fixed(height)),
					padding = Clay_Padding.Hor(8)
				},
				backgroundColor = color ?? new Clay_Color(75, 85, 99, 255)
			}
		});

		if (parent.HasValue && parent.Value != 0)
		{
			separator.Insert(UiNodeParent.For(parent.Value));
		}

		return separator;
	}

	/// <summary>
	/// Creates a spacer (invisible separator) for adding vertical space.
	/// </summary>
	public static EntityCommands CreateSpacer(
		Commands commands,
		float height,
		EcsID? parent = default)
	{
		var spacer = commands.Spawn();
		spacer.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Grow(),
						Clay_SizingAxis.Fixed(height))
				}
			}
		});

		if (parent.HasValue && parent.Value != 0)
		{
			spacer.Insert(UiNodeParent.For(parent.Value));
		}

		return spacer;
	}

	/// <summary>
	/// Creates a horizontal spacer for adding space between elements.
	/// </summary>
	public static EntityCommands CreateHorizontalSpacer(
		Commands commands,
		float width,
		EcsID? parent = default)
	{
		var spacer = commands.Spawn();
		spacer.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(width),
						Clay_SizingAxis.Grow())
				}
			}
		});

		if (parent.HasValue && parent.Value != 0)
		{
			spacer.Insert(UiNodeParent.For(parent.Value));
		}

		return spacer;
	}
}

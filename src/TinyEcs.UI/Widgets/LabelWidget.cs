using System;
using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using EcsID = ulong;

namespace TinyEcs.UI.Widgets;

/// <summary>
/// Style configuration for label widgets.
/// </summary>
public readonly record struct ClayLabelStyle(
	Clay_Color TextColor,
	ushort FontSize,
	Clay_TextAlignment Alignment,
	Clay_TextElementConfigWrapMode WrapMode,
	Clay_Sizing Sizing,
	Clay_Padding Padding)
{
	public static ClayLabelStyle Default => new(
		new Clay_Color(255, 255, 255, 255),
		16,
		Clay_TextAlignment.CLAY_TEXT_ALIGN_LEFT,
		Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_WORDS,
		new Clay_Sizing(
			Clay_SizingAxis.Fit(0, float.MaxValue),
			Clay_SizingAxis.Fit(0, float.MaxValue)),
		Clay_Padding.All(0));

	public static ClayLabelStyle Heading1 => Default with
	{
		FontSize = 32,
		Padding = Clay_Padding.Ver(8)
	};

	public static ClayLabelStyle Heading2 => Default with
	{
		FontSize = 24,
		Padding = Clay_Padding.Ver(6)
	};

	public static ClayLabelStyle Heading3 => Default with
	{
		FontSize = 20,
		Padding = Clay_Padding.Ver(4)
	};

	public static ClayLabelStyle Body => Default;

	public static ClayLabelStyle Caption => Default with
	{
		FontSize = 12,
		TextColor = new Clay_Color(180, 180, 180, 255)
	};
}

/// <summary>
/// Creates simple text label widgets.
/// </summary>
public static class LabelWidget
{
	/// <summary>
	/// Creates a text label entity with the specified style and content.
	/// </summary>
	public static EntityCommands Create(
		Commands commands,
		ClayLabelStyle style,
		ReadOnlySpan<char> text,
		EcsID? parent = default)
	{
		var label = commands.Spawn();

		label.Insert(new UiNode
		{
			Declaration = new Clay_ElementDeclaration
			{
				layout = new Clay_LayoutConfig
				{
					sizing = style.Sizing,
					padding = style.Padding
				}
			}
		});

		label.Insert(UiText.From(text, new Clay_TextElementConfig
		{
			textColor = style.TextColor,
			fontSize = style.FontSize,
			textAlignment = style.Alignment,
			wrapMode = style.WrapMode
		}));

		if (parent.HasValue && parent.Value != 0)
		{
			label.Insert(UiNodeParent.For(parent.Value));
		}

		return label;
	}

	/// <summary>
	/// Creates a heading-style label.
	/// </summary>
	public static EntityCommands CreateHeading(
		Commands commands,
		ReadOnlySpan<char> text,
		int level = 1,
		EcsID? parent = default)
	{
		var style = level switch
		{
			1 => ClayLabelStyle.Heading1,
			2 => ClayLabelStyle.Heading2,
			3 => ClayLabelStyle.Heading3,
			_ => ClayLabelStyle.Body
		};

		return Create(commands, style, text, parent);
	}

	/// <summary>
	/// Creates a caption-style label (smaller, dimmed).
	/// </summary>
	public static EntityCommands CreateCaption(
		Commands commands,
		ReadOnlySpan<char> text,
		EcsID? parent = default)
	{
		return Create(commands, ClayLabelStyle.Caption, text, parent);
	}
}

using System;
using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using EcsID = ulong;

namespace TinyEcs.UI.Widgets;

public readonly record struct ClayButtonStyle(
	Vector2 Size,
	Clay_Color Background,
	Clay_CornerRadius CornerRadius,
	Clay_Color HoverBackground,
	Clay_Color PressedBackground,
	Clay_TextElementConfig Text)
{
	public static ClayButtonStyle Default => new(
		new Vector2(160f, 48f),
		new Clay_Color(98, 166, 229, 255),
		Clay_CornerRadius.All(8),
		new Clay_Color(120, 188, 252, 255),
		new Clay_Color(79, 147, 210, 255),
		new Clay_TextElementConfig
		{
			textColor = new Clay_Color(255, 255, 255, 255),
			fontSize = 20,
			wrapMode = Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE,
			textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
		});
}

public struct ButtonState
{
    public bool IsHovered;
    public bool IsPressed;
}

public static class ButtonWidget
{
    public static EntityCommands Create(
        Commands commands,
        ClayButtonStyle style,
        ReadOnlySpan<char> label,
        EcsID? parent = default)
    {
        var button = commands.Spawn();
        button.Insert(new UiNode
        {
            Declaration = new Clay_ElementDeclaration
            {
                layout = new Clay_LayoutConfig
                {
                    sizing = new Clay_Sizing(
                        Clay_SizingAxis.Fixed(style.Size.X),
                        Clay_SizingAxis.Fixed(style.Size.Y)),
                    childAlignment = new Clay_ChildAlignment(
                        Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
                        Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER),
                    layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT
                },
                backgroundColor = style.Background,
                cornerRadius = style.CornerRadius
            }
        });

        // Store style and interaction state for observer-driven behavior
        button.Insert(style);
        button.Insert(new ButtonState { IsHovered = false, IsPressed = false });

        button.Insert(UiText.From(label, style.Text));

        if (parent.HasValue && parent.Value != 0)
        {
            button.Insert(UiNodeParent.For(parent.Value));
        }

        return button;
    }
}

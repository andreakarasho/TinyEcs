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

/// <summary>
/// Creates button widgets using the reactive Interaction-based pattern.
/// Buttons automatically update their visuals via the UiWidgetObservers system
/// when their Interaction component changes (None/Hovered/Pressed).
///
/// To react to button clicks, add an observer:
/// app.AddObserver&lt;OnClick&lt;Button&gt;&gt;((trigger) => Console.WriteLine($"Button {trigger.EntityId} clicked!"));
/// </summary>
public static class ButtonWidget
{
    /// <summary>
    /// Creates a button entity with reactive interaction handling.
    /// The button's visual state updates automatically via observers when hovered/pressed.
    /// </summary>
    /// <param name="commands">Command buffer for entity creation.</param>
    /// <param name="style">Visual style configuration.</param>
    /// <param name="label">Button text label.</param>
    /// <param name="parent">Optional parent entity ID.</param>
    /// <returns>EntityCommands for the button (use .Id to reference it).</returns>
    public static EntityCommands Create(
        Commands commands,
        ClayButtonStyle style,
        ReadOnlySpan<char> label,
        EcsID? parent = default)
    {
        var button = commands.Spawn();

        // Create the visual node with a Clay ID for pointer events
        var buttonNode = new UiNode
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
        };

        // Assign Clay ID so the button can receive pointer events
        buttonNode.SetId(ClayId.Global($"button-{button.Id}"));
        button.Insert(buttonNode);

        // Store style for reactive visual updates
        button.Insert(style);

        // Add text label
        button.Insert(UiText.From(label, style.Text));

        // Mark as interactive (enables Interaction component updates)
        button.Insert(Interactive.Default);
        button.Insert(Interaction.None);

        // Add marker for button-specific observers
        button.Insert(new UiWidgetObservers.Button());

        if (parent.HasValue && parent.Value != 0)
        {
            button.Insert(UiNodeParent.For(parent.Value));
        }

        return button;
    }
}

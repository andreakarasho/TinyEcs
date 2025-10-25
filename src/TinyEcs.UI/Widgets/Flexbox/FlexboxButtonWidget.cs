using System;
using System.Numerics;
using Flexbox;
using TinyEcs.Bevy;
using TinyEcs.UI;

namespace TinyEcs.UI.Flexbox;

/// <summary>
/// Button widget for Flexbox UI system.
/// Creates a clickable button with text, background, and hover/pressed states.
///
/// Usage:
/// <code>
/// var buttonId = FlexboxButtonWidget.Create(
///     commands,
///     text: "Click Me",
///     onClick: (world, trigger) => Console.WriteLine("Clicked!"),
///     style: FlexboxButtonStyle.Default()
/// ).Id;
/// </code>
/// </summary>
public static class FlexboxButtonWidget
{
    /// <summary>
    /// Creates a button widget.
    /// Returns EntityCommands for further configuration.
    /// </summary>
    public static EntityCommands Create(
        Commands commands,
        string text,
        Action<On<UiPointerTrigger>> onClick,
        FlexboxButtonStyle? style = null)
    {
        var buttonStyle = style ?? FlexboxButtonStyle.Default();

        var buttonId = commands.Spawn()
            .Insert(FlexboxNode.Row())
            .Insert(new FlexboxInteractive())
            .Insert(Interaction.None)
            .Id;

        // Configure layout
        commands.Entity(buttonId).Insert(new FlexboxNode
        {
            FlexDirection = FlexDirection.Row,
            JustifyContent = Justify.Center,
            AlignItems = Align.Center,
            Width = buttonStyle.Width,
            Height = buttonStyle.Height,
            PaddingTop = buttonStyle.PaddingVertical,
            PaddingBottom = buttonStyle.PaddingVertical,
            PaddingLeft = buttonStyle.PaddingHorizontal,
            PaddingRight = buttonStyle.PaddingHorizontal,
            BackgroundColor = buttonStyle.BackgroundColor,
            BorderRadius = buttonStyle.BorderRadius
        });

        // Create text child
        var textId = commands.Spawn()
            .Insert(new FlexboxNode
            {
                Display = Display.Flex
            })
            .Insert(new FlexboxText(text, buttonStyle.FontSize, buttonStyle.TextColor))
            .Insert(new FlexboxNodeParent(buttonId))
            .Id;

        // Attach click observer
        if (onClick != null)
        {
            commands.Entity(buttonId)
                .Observe<On<UiPointerTrigger>>((trigger) =>
                {
                    var e = trigger.Event.Event;
                    if (e.Type == UiPointerEventType.PointerDown &&
                        e.IsPrimaryButton)
                    {
                        onClick(trigger);
                    }
                });
        }

        return commands.Entity(buttonId);
    }

    /// <summary>
    /// Creates a simple text button with default styling.
    /// </summary>
    public static EntityCommands CreateSimple(
        Commands commands,
        string text,
        Action<On<UiPointerTrigger>> onClick)
    {
        return Create(commands, text, onClick, FlexboxButtonStyle.Default());
    }
}

/// <summary>
/// Styling configuration for FlexboxButtonWidget.
/// </summary>
public struct FlexboxButtonStyle
{
    public FlexValue Width;
    public FlexValue Height;
    public float PaddingHorizontal;
    public float PaddingVertical;
    public Vector4 BackgroundColor;
    public Vector4 HoverColor;
    public Vector4 PressedColor;
    public Vector4 TextColor;
    public float FontSize;
    public float BorderRadius;

    public static FlexboxButtonStyle Default()
    {
        return new FlexboxButtonStyle
        {
            Width = FlexValue.Auto(),
            Height = FlexValue.Points(40f),
            PaddingHorizontal = 16f,
            PaddingVertical = 8f,
            BackgroundColor = new Vector4(0.2f, 0.4f, 0.8f, 1f),
            HoverColor = new Vector4(0.3f, 0.5f, 0.9f, 1f),
            PressedColor = new Vector4(0.1f, 0.3f, 0.7f, 1f),
            TextColor = new Vector4(1f, 1f, 1f, 1f),
            FontSize = 16f,
            BorderRadius = 4f
        };
    }
}

/// <summary>
/// System that updates button visuals based on Interaction state.
/// Should run in PreUpdate stage.
/// </summary>
public static class FlexboxButtonSystems
{
    public static void UpdateButtonVisuals(
        Query<Data<Interaction, FlexboxNode>, Filter<Changed<Interaction>>> buttons)
    {
        var style = FlexboxButtonStyle.Default();

        foreach (var (interaction, node) in buttons)
        {
            ref var interactionRef = ref interaction.Ref;
            ref var nodeRef = ref node.Ref;

            nodeRef.BackgroundColor = interactionRef switch
            {
                Interaction.None => style.BackgroundColor,
                Interaction.Hovered => style.HoverColor,
                Interaction.Pressed => style.PressedColor,
                _ => style.BackgroundColor
            };
        }
    }
}

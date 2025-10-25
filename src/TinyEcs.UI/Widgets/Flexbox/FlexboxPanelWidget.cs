using System.Numerics;
using Flexbox;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Flexbox;

/// <summary>
/// Panel widget for Flexbox UI system.
/// Creates a container with background, padding, and configurable layout direction.
///
/// Usage:
/// <code>
/// var panelId = FlexboxPanelWidget.Create(
///     commands,
///     style: FlexboxPanelStyle.Default(),
///     direction: FlexDirection.Column
/// ).Id;
///
/// // Add children
/// FlexboxLabelWidget.Create(commands, "Child 1")
///     .Insert(new FlexboxNodeParent(panelId));
/// </code>
/// </summary>
public static class FlexboxPanelWidget
{
    /// <summary>
    /// Creates a panel widget.
    /// Returns EntityCommands for further configuration.
    /// </summary>
    public static EntityCommands Create(
        Commands commands,
        FlexboxPanelStyle? style = null,
        FlexDirection direction = FlexDirection.Column)
    {
        var panelStyle = style ?? FlexboxPanelStyle.Default();

        var panelId = commands.Spawn()
            .Insert(new FlexboxNode
            {
                FlexDirection = direction,
                JustifyContent = Justify.FlexStart,
                AlignItems = Align.Stretch,
                Width = panelStyle.Width,
                Height = panelStyle.Height,
                PaddingTop = panelStyle.Padding,
                PaddingRight = panelStyle.Padding,
                PaddingBottom = panelStyle.Padding,
                PaddingLeft = panelStyle.Padding,
                BackgroundColor = panelStyle.BackgroundColor,
                BorderRadius = panelStyle.BorderRadius
            })
            .Id;

        return commands.Entity(panelId);
    }

    /// <summary>
    /// Creates a column panel (vertical layout).
    /// </summary>
    public static EntityCommands CreateColumn(Commands commands, FlexboxPanelStyle? style = null)
    {
        return Create(commands, style, FlexDirection.Column);
    }

    /// <summary>
    /// Creates a row panel (horizontal layout).
    /// </summary>
    public static EntityCommands CreateRow(Commands commands, FlexboxPanelStyle? style = null)
    {
        return Create(commands, style, FlexDirection.Row);
    }
}

/// <summary>
/// Styling configuration for FlexboxPanelWidget.
/// </summary>
public struct FlexboxPanelStyle
{
    public FlexValue Width;
    public FlexValue Height;
    public float Padding;
    public Vector4 BackgroundColor;
    public float BorderRadius;

    public static FlexboxPanelStyle Default()
    {
        return new FlexboxPanelStyle
        {
            Width = FlexValue.Auto(),
            Height = FlexValue.Auto(),
            Padding = 16f,
            BackgroundColor = new Vector4(0.15f, 0.15f, 0.15f, 1f),
            BorderRadius = 8f
        };
    }

    public static FlexboxPanelStyle Transparent()
    {
        return new FlexboxPanelStyle
        {
            Width = FlexValue.Auto(),
            Height = FlexValue.Auto(),
            Padding = 0f,
            BackgroundColor = Vector4.Zero,
            BorderRadius = 0f
        };
    }

    public static FlexboxPanelStyle Card()
    {
        return new FlexboxPanelStyle
        {
            Width = FlexValue.Auto(),
            Height = FlexValue.Auto(),
            Padding = 20f,
            BackgroundColor = new Vector4(0.2f, 0.2f, 0.2f, 1f),
            BorderRadius = 12f
        };
    }
}

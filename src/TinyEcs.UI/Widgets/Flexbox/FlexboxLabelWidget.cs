using System.Numerics;
using Flexbox;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Flexbox;

/// <summary>
/// Label widget for Flexbox UI system.
/// Displays static or dynamic text with configurable styling.
///
/// Usage:
/// <code>
/// var labelId = FlexboxLabelWidget.Create(
///     commands,
///     text: "Hello World",
///     style: FlexboxLabelStyle.Heading1()
/// ).Id;
/// </code>
/// </summary>
public static class FlexboxLabelWidget
{
    /// <summary>
    /// Creates a label widget.
    /// Returns EntityCommands for further configuration.
    /// </summary>
    public static EntityCommands Create(
        Commands commands,
        string text,
        FlexboxLabelStyle? style = null)
    {
        var labelStyle = style ?? FlexboxLabelStyle.Body();

        var labelId = commands.Spawn()
            .Insert(new FlexboxNode
            {
                Display = Display.Flex,
                Width = FlexValue.Auto(),
                Height = FlexValue.Auto()
            })
            .Insert(new FlexboxText(text, labelStyle.FontSize, labelStyle.Color))
            .Id;

        return commands.Entity(labelId);
    }

    /// <summary>
    /// Creates a heading label (H1).
    /// </summary>
    public static EntityCommands CreateHeading1(Commands commands, string text)
    {
        return Create(commands, text, FlexboxLabelStyle.Heading1());
    }

    /// <summary>
    /// Creates a heading label (H2).
    /// </summary>
    public static EntityCommands CreateHeading2(Commands commands, string text)
    {
        return Create(commands, text, FlexboxLabelStyle.Heading2());
    }

    /// <summary>
    /// Creates a heading label (H3).
    /// </summary>
    public static EntityCommands CreateHeading3(Commands commands, string text)
    {
        return Create(commands, text, FlexboxLabelStyle.Heading3());
    }

    /// <summary>
    /// Creates a body text label.
    /// </summary>
    public static EntityCommands CreateBody(Commands commands, string text)
    {
        return Create(commands, text, FlexboxLabelStyle.Body());
    }

    /// <summary>
    /// Creates a caption/small text label.
    /// </summary>
    public static EntityCommands CreateCaption(Commands commands, string text)
    {
        return Create(commands, text, FlexboxLabelStyle.Caption());
    }
}

/// <summary>
/// Styling configuration for FlexboxLabelWidget.
/// </summary>
public struct FlexboxLabelStyle
{
    public float FontSize;
    public Vector4 Color;
    public FontWeight Weight;

    public static FlexboxLabelStyle Heading1()
    {
        return new FlexboxLabelStyle
        {
            FontSize = 32f,
            Color = new Vector4(1f, 1f, 1f, 1f),
            Weight = FontWeight.Bold
        };
    }

    public static FlexboxLabelStyle Heading2()
    {
        return new FlexboxLabelStyle
        {
            FontSize = 24f,
            Color = new Vector4(1f, 1f, 1f, 1f),
            Weight = FontWeight.Bold
        };
    }

    public static FlexboxLabelStyle Heading3()
    {
        return new FlexboxLabelStyle
        {
            FontSize = 20f,
            Color = new Vector4(1f, 1f, 1f, 1f),
            Weight = FontWeight.SemiBold
        };
    }

    public static FlexboxLabelStyle Body()
    {
        return new FlexboxLabelStyle
        {
            FontSize = 16f,
            Color = new Vector4(0.9f, 0.9f, 0.9f, 1f),
            Weight = FontWeight.Regular
        };
    }

    public static FlexboxLabelStyle Caption()
    {
        return new FlexboxLabelStyle
        {
            FontSize = 12f,
            Color = new Vector4(0.7f, 0.7f, 0.7f, 1f),
            Weight = FontWeight.Regular
        };
    }
}

/// <summary>
/// Font weight enum (for future renderer integration).
/// </summary>
public enum FontWeight
{
    Regular,
    SemiBold,
    Bold
}

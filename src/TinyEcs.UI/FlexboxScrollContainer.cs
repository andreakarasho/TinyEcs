using System.Numerics;

namespace TinyEcs.UI;

/// <summary>
/// Component that marks an element as a scrollable container in Flexbox UI.
/// Rendering applies clipping to the content rect and offsets children by -Offset.
/// </summary>
public struct FlexboxScrollContainer
{
    /// <summary>Current scroll offset in pixels. Positive Y scrolls content up.</summary>
    public Vector2 Offset;

    /// <summary>Enable vertical scrolling.</summary>
    public bool Vertical;

    /// <summary>Enable horizontal scrolling.</summary>
    public bool Horizontal;

    /// <summary>Scroll speed multiplier per wheel unit.</summary>
    public float ScrollSpeed;

    public static FlexboxScrollContainer VerticalOnly(float speed = 20f) => new()
    {
        Offset = Vector2.Zero,
        Vertical = true,
        Horizontal = false,
        ScrollSpeed = speed
    };
}


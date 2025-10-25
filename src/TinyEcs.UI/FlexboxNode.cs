using System;
using System.Numerics;
using Flexbox;

namespace TinyEcs.UI;

/// <summary>
/// Component that describes Flexbox layout properties for a UI entity.
/// Parallel to UiNode (Clay) but for Flexbox layout engine.
///
/// Usage:
/// <code>
/// entity.Set(new FlexboxNode
/// {
///     FlexDirection = FlexDirection.Row,
///     JustifyContent = Justify.Center,
///     AlignItems = Align.Center,
///     Width = 100f,
///     Height = 50f,
///     BackgroundColor = new Vector4(1, 0, 0, 1)
/// });
/// </code>
/// </summary>
public struct FlexboxNode
{
    // === Layout Properties (Flexbox) ===

    public FlexDirection FlexDirection;
    public Justify JustifyContent;
    public Align AlignItems;
    public Align AlignSelf;
    public Align AlignContent;
    public Wrap FlexWrap;
    public PositionType PositionType;
    public Display Display;
    public Overflow Overflow;

    public float FlexGrow;
    public float FlexShrink;
    public FlexBasis FlexBasis;

    // === Dimensions ===

    public FlexValue Width;
    public FlexValue Height;
    public FlexValue MinWidth;
    public FlexValue MinHeight;
    public FlexValue MaxWidth;
    public FlexValue MaxHeight;

    // === Spacing ===

    public FlexValue MarginTop;
    public FlexValue MarginRight;
    public FlexValue MarginBottom;
    public FlexValue MarginLeft;

    public FlexValue PaddingTop;
    public FlexValue PaddingRight;
    public FlexValue PaddingBottom;
    public FlexValue PaddingLeft;

    public FlexValue BorderTop;
    public FlexValue BorderRight;
    public FlexValue BorderBottom;
    public FlexValue BorderLeft;

    // === Position ===

    public FlexValue Top;
    public FlexValue Right;
    public FlexValue Bottom;
    public FlexValue Left;

    // === Rendering Properties (not layout, used by renderer) ===

    public Vector4 BackgroundColor;
    public Vector4 BorderColor;
    public float BorderRadius;

    // === Element ID (assigned during layout pass) ===

    public uint ElementId;

    // === Factory Methods ===

    /// <summary>
    /// Creates a default FlexboxNode with common defaults.
    /// </summary>
    public static FlexboxNode Default()
    {
        return new FlexboxNode
        {
            FlexDirection = FlexDirection.Column,
            JustifyContent = Justify.FlexStart,
            AlignItems = Align.Stretch,
            AlignSelf = Align.Auto,
            AlignContent = Align.Stretch,
            FlexWrap = Wrap.NoWrap,
            PositionType = PositionType.Relative,
            Display = Display.Flex,
            Overflow = Overflow.Visible,
            FlexGrow = 0f,
            FlexShrink = 1f,
            FlexBasis = FlexBasis.Auto(),
            Width = FlexValue.Auto(),
            Height = FlexValue.Auto(),
            MinWidth = FlexValue.Undefined(),
            MinHeight = FlexValue.Undefined(),
            MaxWidth = FlexValue.Undefined(),
            MaxHeight = FlexValue.Undefined(),
            BackgroundColor = Vector4.Zero,
            BorderColor = Vector4.Zero,
            BorderRadius = 0f,
            ElementId = 0
        };
    }

    /// <summary>
    /// Creates a container with row layout.
    /// </summary>
    public static FlexboxNode Row()
    {
        var node = Default();
        node.FlexDirection = FlexDirection.Row;
        return node;
    }

    /// <summary>
    /// Creates a container with column layout.
    /// </summary>
    public static FlexboxNode Column()
    {
        var node = Default();
        node.FlexDirection = FlexDirection.Column;
        return node;
    }
}

/// <summary>
/// Flexbox value that can be points, percent, or auto.
/// </summary>
public struct FlexValue
{
    public float Value;
    public Unit Unit;

    public FlexValue(float value, Unit unit)
    {
        Value = value;
        Unit = unit;
    }

    public static FlexValue Points(float value) => new FlexValue(value, Unit.Point);
    public static FlexValue Percent(float value) => new FlexValue(value, Unit.Percent);
    public static FlexValue Auto() => new FlexValue(float.NaN, Unit.Auto);
    public static FlexValue Undefined() => new FlexValue(float.NaN, Unit.Undefined);

    public bool IsUndefined => Unit == Unit.Undefined;
    public bool IsAuto => Unit == Unit.Auto;
    public bool IsDefined => Unit != Unit.Undefined && Unit != Unit.Auto;

    public static implicit operator FlexValue(float value) => Points(value);
}

/// <summary>
/// Flexbox flex-basis value (auto, content, or length).
/// </summary>
public struct FlexBasis
{
    public FlexValue Value;

    public FlexBasis(FlexValue value)
    {
        Value = value;
    }

    public static FlexBasis Auto() => new FlexBasis(FlexValue.Auto());
    public static FlexBasis Points(float value) => new FlexBasis(FlexValue.Points(value));
    public static FlexBasis Percent(float value) => new FlexBasis(FlexValue.Percent(value));

    public static implicit operator FlexBasis(float value) => Points(value);
}

/// <summary>
/// Component that marks an entity as interactive for Flexbox pointer events.
/// Reuses existing Interactive component from Clay system.
/// </summary>
public struct FlexboxInteractive
{
    public bool IsFocusable;

    public FlexboxInteractive(bool isFocusable = false)
    {
        IsFocusable = isFocusable;
    }
}

/// <summary>
/// Component for text content in Flexbox UI.
/// Stores text string and styling.
/// </summary>
public struct FlexboxText
{
    public string Text;
    public float FontSize;
    public Vector4 Color;

    public FlexboxText(string text, float fontSize = 16f, Vector4 color = default)
    {
        Text = text ?? string.Empty;
        FontSize = fontSize;
        Color = color == default ? new Vector4(1, 1, 1, 1) : color;
    }
}

/// <summary>
/// Component that specifies parent relationship for Flexbox UI hierarchy.
/// Reuses existing UiNodeParent from Clay system.
/// </summary>
public struct FlexboxNodeParent
{
    public ulong Parent;
    public int Index;

    public FlexboxNodeParent(ulong parent, int index = -1)
    {
        Parent = parent;
        Index = index;
    }
}

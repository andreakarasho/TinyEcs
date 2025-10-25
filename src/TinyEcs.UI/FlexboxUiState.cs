using System;
using System.Collections.Generic;
using System.Numerics;
using Flexbox;

namespace TinyEcs.UI;

/// <summary>
/// Resource that holds the Flexbox layout state and computed results.
/// Parallel to ClayUiState but for Flexbox layout engine.
/// </summary>
public sealed class FlexboxUiState
{
    /// <summary>
    /// Maps entity IDs to their Flexbox nodes for layout computation.
    /// </summary>
    internal readonly Dictionary<ulong, Node> EntityToFlexboxNode = new();

    /// <summary>
    /// Maps entity IDs to their computed layout results after layout pass.
    /// </summary>
    internal readonly Dictionary<ulong, ComputedLayout> EntityToLayout = new();

    /// <summary>
    /// Maps element IDs (for pointer hit testing) to entity IDs.
    /// Element IDs are assigned sequentially during layout pass.
    /// </summary>
    internal readonly Dictionary<uint, ulong> ElementToEntityMap = new();

    /// <summary>
    /// Root Flexbox nodes (entities without parents or with explicit root marking).
    /// </summary>
    internal readonly List<ulong> RootEntities = new();

    /// <summary>
    /// Currently hovered element IDs from previous frame (for delta detection).
    /// </summary>
    internal readonly HashSet<uint> HoveredElementIds = new();

    /// <summary>
    /// Next element ID to assign during layout pass.
    /// </summary>
    internal uint NextElementId = 1;

    /// <summary>
    /// Container dimensions for root layout calculation.
    /// </summary>
    public float ContainerWidth { get; set; } = 1920f;
    public float ContainerHeight { get; set; } = 1080f;

    /// <summary>
    /// Whether the layout needs recomputation this frame.
    /// </summary>
    public bool IsDirty { get; internal set; } = true;

    /// <summary>
    /// Marks the entire layout as dirty, requiring recomputation next frame.
    /// </summary>
    public void MarkDirty()
    {
        IsDirty = true;
    }

    /// <summary>
    /// Clears all state (useful for complete rebuild).
    /// </summary>
    public void Clear()
    {
        EntityToFlexboxNode.Clear();
        EntityToLayout.Clear();
        ElementToEntityMap.Clear();
        RootEntities.Clear();
        HoveredElementIds.Clear();
        NextElementId = 1;
        IsDirty = true;
    }

    /// <summary>
    /// Public accessor to retrieve a computed layout for an entity.
    /// </summary>
    public bool TryGetLayout(ulong entityId, out ComputedLayout layout)
        => EntityToLayout.TryGetValue(entityId, out layout);
}

/// <summary>
/// Computed layout result for an entity after Flexbox calculation.
/// </summary>
public struct ComputedLayout
{
    /// <summary>
    /// Element ID assigned for pointer hit testing.
    /// </summary>
    public uint ElementId;

    /// <summary>
    /// Absolute position (including parent offsets).
    /// </summary>
    public Vector2 Position;

    /// <summary>
    /// Computed dimensions.
    /// </summary>
    public Vector2 Size;

    /// <summary>
    /// Relative position within parent.
    /// </summary>
    public Vector2 LocalPosition;

    /// <summary>
    /// Margin, padding, border computed values.
    /// </summary>
    public EdgeInsets Margin;
    public EdgeInsets Padding;
    public EdgeInsets Border;

    /// <summary>
    /// Content area (position + size after padding/border).
    /// </summary>
    public Vector2 ContentPosition;
    public Vector2 ContentSize;

    /// <summary>
    /// Layout direction (LTR/RTL).
    /// </summary>
    public Direction Direction;

    /// <summary>
    /// Whether content overflowed the container.
    /// </summary>
    public bool HadOverflow;
}

/// <summary>
/// Edge insets (top, right, bottom, left).
/// </summary>
public struct EdgeInsets
{
    public float Top;
    public float Right;
    public float Bottom;
    public float Left;

    public EdgeInsets(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public static EdgeInsets Zero => new EdgeInsets(0, 0, 0, 0);
}

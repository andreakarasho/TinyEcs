using System;
using System.Collections.Generic;
using System.Numerics;
using Flexbox;

namespace TinyEcs.UI;

/// <summary>
/// Resource that holds the Flexbox layout state.
/// NEW ARCHITECTURE: No more ComputedLayout - read directly from Node.layout.
/// </summary>
public sealed class FlexboxUiState
{
	/// <summary>
	/// Maps entity IDs to their Flexbox nodes.
	/// Synced every frame from FlexboxNode components.
	/// </summary>
	internal readonly Dictionary<ulong, Node> EntityToFlexboxNode = new();

	/// <summary>
	/// Maps element IDs (for pointer hit testing) to entity IDs.
	/// Element IDs are assigned during layout pass.
	/// </summary>
	internal readonly Dictionary<uint, ulong> ElementToEntityMap = new();

	/// <summary>
	/// Root Flexbox nodes (entities without Parent component).
	/// </summary>
	public readonly List<ulong> RootEntities = new();

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
	/// Clears all state (useful for complete rebuild).
	/// </summary>
	public void Clear()
	{
		EntityToFlexboxNode.Clear();
		ElementToEntityMap.Clear();
		RootEntities.Clear();
		HoveredElementIds.Clear();
		NextElementId = 1;
	}

	/// <summary>
	/// Gets the Flexbox node for an entity (for direct layout access).
	/// </summary>
	public bool TryGetNode(ulong entityId, out Node? node)
		=> EntityToFlexboxNode.TryGetValue(entityId, out node);

	/// <summary>
	/// Gets the computed absolute position for an entity by walking up the parent chain.
	/// Flexbox positions are relative to parent, so we must accumulate.
	/// </summary>
	public Vector2 GetAbsolutePosition(ulong entityId)
	{
		if (!EntityToFlexboxNode.TryGetValue(entityId, out var node))
			return Vector2.Zero;

		// Walk up parent chain accumulating positions
		var position = new Vector2(node.layout.left, node.layout.top);
		var parent = node.Parent;

		while (parent != null)
		{
			position.X += parent.layout.left;
			position.Y += parent.layout.top;
			parent = parent.Parent;
		}

		return position;
	}

	/// <summary>
	/// Gets the size from a Flexbox node's computed layout.
	/// </summary>
	public Vector2 GetSize(ulong entityId)
	{
		if (!EntityToFlexboxNode.TryGetValue(entityId, out var node))
			return Vector2.Zero;

		return new Vector2(node.layout.width, node.layout.height);
	}
}

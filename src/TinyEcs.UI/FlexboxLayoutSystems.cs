using System;
using System.Collections.Generic;
using System.Numerics;
using Flexbox;
using TinyEcs.Bevy;

namespace TinyEcs.UI;

/// <summary>
/// Systems for Flexbox layout computation.
/// NEW ARCHITECTURE: Syncs ECS components to Flexbox nodes every frame.
/// No more ComputedLayout - read directly from Flexbox node.layout.
/// </summary>
public static class FlexboxLayoutSystems
{
	/// <summary>
	/// Synchronizes FlexboxNodeParent components with ECS Parent/Children hierarchy.
	/// Runs in PreUpdate stage.
	/// </summary>
	public static void SyncHierarchy(
		Commands commands,
		Query<Data<FlexboxNodeParent>, Filter<Changed<FlexboxNodeParent>>> desiredParents,
		Query<Data<Parent>> currentParents,
		Query<Data<Children>> childrenLists)
	{
		foreach (var (entityId, desiredParent) in desiredParents)
		{
			var childId = entityId.Ref;
			ref var desired = ref desiredParent.Ref;

			// Read current parent from query
			ulong currentParent = 0;
			if (currentParents.Contains(childId))
			{
				var pdata = currentParents.Get(childId);
				pdata.Deconstruct(out _, out var pPtr);
				currentParent = pPtr.Ref.Id;
			}

			var targetParent = desired.Parent;

			// If parent unchanged, only adjust index if needed
			if (targetParent == currentParent)
			{
				if (targetParent != 0 && desired.Index >= 0 && childrenLists.Contains(targetParent))
				{
					var listData = childrenLists.Get(targetParent);
					listData.Deconstruct(out _, out var listPtr);
					ref var list = ref listPtr.Ref;

					// find current index
					int currentIndex = -1;
					int i = 0;
					foreach (var id in list)
					{
						if (id == childId) { currentIndex = i; break; }
						i++;
					}

					if (currentIndex != desired.Index)
					{
						commands.RemoveChild(childId);
						var clamped = desired.Index;
						if (clamped < 0) clamped = 0;
						if (clamped > list.Count) clamped = list.Count;
						commands.AddChild(targetParent, childId, clamped);
					}
				}
				continue;
			}

			// Remove from current parent if moving to root
			if (targetParent == 0 && currentParent != 0)
			{
				commands.RemoveChild(childId);
				continue;
			}

			// Move under a new parent
			if (targetParent != 0)
			{
				if (desired.Index >= 0)
					commands.AddChild(targetParent, childId, desired.Index);
				else
					commands.AddChild(targetParent, childId);
			}
		}
	}

	/// <summary>
	/// NEW ARCHITECTURE: Syncs ALL FlexboxNode components to Flexbox.Node objects every frame.
	/// This allows direct editing of components to automatically update the layout.
	/// Runs in Update stage.
	/// </summary>
	public static void SyncComponentsToFlexboxNodes(
		ResMut<FlexboxUiState> state,
		Query<Data<FlexboxNode>> nodes,
		Query<Data<FlexboxText>> texts,
		Query<Data<FlexboxNode>, Filter<Without<Parent>>> rootNodes,
		Query<Data<Children>> childrenQuery)
	{
		ref var stateRef = ref state.Value;

		// Step 1: Sync all FlexboxNode components to their Flexbox.Node objects
		foreach (var (entityId, flexNodeData) in nodes)
		{
			var id = entityId.Ref;
			ref var flexNode = ref flexNodeData.Ref;

			// Get or create Flexbox node
			if (!stateRef.EntityToFlexboxNode.TryGetValue(id, out var flexboxNode))
			{
				flexboxNode = new Node();
				stateRef.EntityToFlexboxNode[id] = flexboxNode;
			}

			// Apply all properties from FlexboxNode component to Flexbox.Node
			ApplyFlexboxNodeToNode(ref flexNode, flexboxNode);
			flexboxNode.Context = id;

			// Configure text measurement if entity has FlexboxText
			if (texts.Contains(id))
			{
				var textData = texts.Get(id);
				textData.Deconstruct(out var textPtr);
				ref var text = ref textPtr.Ref;
				ConfigureTextMeasurement(flexboxNode, ref text);
			}
		}

		// Step 2: Rebuild parent-child relationships
		// Clear all existing relationships
		foreach (var (_, node) in stateRef.EntityToFlexboxNode)
		{
			foreach (var child in node.Children)
				child.Parent = null!;
			node.Children.Clear();
			node.Parent = null!;
		}

		// Rebuild hierarchy from Children components
		stateRef.RootEntities.Clear();
		stateRef.ElementToEntityMap.Clear();
		stateRef.NextElementId = 1;

		// Identify roots
		foreach (var (entityId, _) in rootNodes)
		{
			stateRef.RootEntities.Add(entityId.Ref);
		}

		// Attach children recursively starting from roots
		foreach (var rootId in stateRef.RootEntities)
		{
			AttachChildrenRecursive(rootId, stateRef.EntityToFlexboxNode, childrenQuery);
		}

		// Step 3: Calculate layout for each root
		foreach (var rootEntityId in stateRef.RootEntities)
		{
			if (!stateRef.EntityToFlexboxNode.TryGetValue(rootEntityId, out var rootNode))
				continue;

			rootNode.CalculateLayout(
				stateRef.ContainerWidth,
				stateRef.ContainerHeight,
				Direction.LTR);

			// Assign element IDs for pointer hit testing
			AssignElementIds(ref stateRef, rootEntityId, rootNode);
		}
	}

	private static void AttachChildrenRecursive(
		ulong parentId,
		Dictionary<ulong, Node> entityToNode,
		Query<Data<Children>> childrenQuery)
	{
		if (!childrenQuery.Contains(parentId))
			return;

		var childrenData = childrenQuery.Get(parentId);
		childrenData.Deconstruct(out var childrenPtr);

		if (!entityToNode.TryGetValue(parentId, out var parentNode))
			return;

		foreach (var childId in childrenPtr.Ref)
		{
			if (entityToNode.TryGetValue(childId, out var childNode))
			{
				parentNode.AddChild(childNode);
				AttachChildrenRecursive(childId, entityToNode, childrenQuery);
			}
		}
	}

	private static void AssignElementIds(
		ref FlexboxUiState state,
		ulong entityId,
		Node node)
	{
		// Assign element ID for pointer hit testing
		var elementId = state.NextElementId++;
		state.ElementToEntityMap[elementId] = entityId;

		// Store element ID in the node's context (we'll use a tuple)
		// Since Context is object, we can store (entityId, elementId)
		node.Context = (entityId, elementId);

		// Recursively assign for children
		for (int i = 0; i < node.ChildrenCount; i++)
		{
			var child = node.GetChild(i);
			if (child != null && child.Context is ulong childEntityId)
			{
				AssignElementIds(ref state, childEntityId, child);
			}
			else if (child != null && child.Context is ValueTuple<ulong, uint> childContext)
			{
				// Already has tuple context, just reassign
				AssignElementIds(ref state, childContext.Item1, child);
			}
		}
	}

	private static void ApplyFlexboxNodeToNode(ref FlexboxNode flexNode, Node node)
	{
		var style = node.nodeStyle;

		// Layout properties
		style.FlexDirection = flexNode.FlexDirection;
		style.JustifyContent = flexNode.JustifyContent;
		style.AlignItems = flexNode.AlignItems;
		style.AlignSelf = flexNode.AlignSelf;
		style.AlignContent = flexNode.AlignContent;
		style.FlexWrap = flexNode.FlexWrap;
		style.PositionType = flexNode.PositionType;
		style.Display = flexNode.Display;
		style.Overflow = flexNode.Overflow;

		style.FlexGrow = flexNode.FlexGrow;
		style.FlexShrink = flexNode.FlexShrink;
		style.FlexBasis = ConvertFlexBasis(flexNode.FlexBasis);

		// Dimensions
		style.Dimensions[(int)Dimension.Width] = ConvertFlexValue(flexNode.Width);
		style.Dimensions[(int)Dimension.Height] = ConvertFlexValue(flexNode.Height);
		style.MinDimensions[(int)Dimension.Width] = ConvertFlexValue(flexNode.MinWidth);
		style.MinDimensions[(int)Dimension.Height] = ConvertFlexValue(flexNode.MinHeight);
		style.MaxDimensions[(int)Dimension.Width] = ConvertFlexValue(flexNode.MaxWidth);
		style.MaxDimensions[(int)Dimension.Height] = ConvertFlexValue(flexNode.MaxHeight);

		// Margin
		style.Margin[(int)Edge.Top] = ConvertFlexValue(flexNode.MarginTop);
		style.Margin[(int)Edge.Right] = ConvertFlexValue(flexNode.MarginRight);
		style.Margin[(int)Edge.Bottom] = ConvertFlexValue(flexNode.MarginBottom);
		style.Margin[(int)Edge.Left] = ConvertFlexValue(flexNode.MarginLeft);

		// Padding
		style.Padding[(int)Edge.Top] = ConvertFlexValue(flexNode.PaddingTop);
		style.Padding[(int)Edge.Right] = ConvertFlexValue(flexNode.PaddingRight);
		style.Padding[(int)Edge.Bottom] = ConvertFlexValue(flexNode.PaddingBottom);
		style.Padding[(int)Edge.Left] = ConvertFlexValue(flexNode.PaddingLeft);

		// Border
		style.Border[(int)Edge.Top] = ConvertFlexValue(flexNode.BorderTop);
		style.Border[(int)Edge.Right] = ConvertFlexValue(flexNode.BorderRight);
		style.Border[(int)Edge.Bottom] = ConvertFlexValue(flexNode.BorderBottom);
		style.Border[(int)Edge.Left] = ConvertFlexValue(flexNode.BorderLeft);

		// Position
		style.Position[(int)Edge.Top] = ConvertFlexValue(flexNode.Top);
		style.Position[(int)Edge.Right] = ConvertFlexValue(flexNode.Right);
		style.Position[(int)Edge.Bottom] = ConvertFlexValue(flexNode.Bottom);
		style.Position[(int)Edge.Left] = ConvertFlexValue(flexNode.Left);
	}

	private static Value ConvertFlexValue(FlexValue flexValue)
	{
		if (flexValue.Unit == Unit.Undefined)
			return Value.UndefinedValue;
		if (flexValue.Unit == Unit.Auto)
			return new Value(float.NaN, Unit.Auto);
		return new Value(flexValue.Value, flexValue.Unit);
	}

	private static Value ConvertFlexBasis(FlexBasis flexBasis)
	{
		return ConvertFlexValue(flexBasis.Value);
	}

	private static void ConfigureTextMeasurement(Node node, ref FlexboxText textData)
	{
		var capturedText = textData.Text;
		var capturedFontSize = textData.FontSize;
		node.SetMeasureFunc((n, width, widthMode, height, heightMode) =>
		{
			var textLength = capturedText?.Length ?? 0;
			var fontSize = capturedFontSize;
			var estimatedWidth = textLength * fontSize * 0.6f;
			var estimatedHeight = fontSize;

			var measuredWidth = widthMode == MeasureMode.Exactly ? width : estimatedWidth;
			var measuredHeight = heightMode == MeasureMode.Exactly ? height : estimatedHeight;

			return new Size(measuredWidth, measuredHeight);
		});
	}
}

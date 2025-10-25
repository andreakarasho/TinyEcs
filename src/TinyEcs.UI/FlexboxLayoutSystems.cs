using System;
using System.Collections.Generic;
using System.Numerics;
using Flexbox;
using TinyEcs.Bevy;

namespace TinyEcs.UI;

/// <summary>
/// Systems for Flexbox layout computation.
/// Parallel to ClayLayoutSystems but for Flexbox layout engine.
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
	/// Computes Flexbox layout for all FlexboxNode entities.
	/// Runs in Update stage.
	/// </summary>
	public static void ComputeLayout(
		ResMut<FlexboxUiState> state,
		Query<Data<FlexboxNode>> nodes,
		Query<Data<Parent>> parents,
		Query<Data<Children>> childrenQuery,
		Query<Data<FlexboxText>> texts)
	{
		ref var stateRef = ref state.Value;

		// Clear previous frame data
		stateRef.EntityToFlexboxNode.Clear();
		stateRef.EntityToLayout.Clear();
		stateRef.ElementToEntityMap.Clear();
		stateRef.RootEntities.Clear();
		stateRef.NextElementId = 1;

		// Build Flexbox node tree from entity hierarchy
		BuildFlexboxTree(ref stateRef, nodes, parents, childrenQuery, texts);

		// Calculate layout for each root
		foreach (var rootEntityId in stateRef.RootEntities)
		{
			if (!stateRef.EntityToFlexboxNode.TryGetValue(rootEntityId, out var rootNode))
				continue;

			// Compute layout with container dimensions
			rootNode.CalculateLayout(
				stateRef.ContainerWidth,
				stateRef.ContainerHeight,
				Direction.LTR);

			// Extract computed layouts recursively
			ExtractLayout(ref stateRef, rootEntityId, rootNode, Vector2.Zero);
		}

		stateRef.IsDirty = false;
	}

	private static void BuildFlexboxTree(
		ref FlexboxUiState state,
		Query<Data<FlexboxNode>> nodes,
		Query<Data<Parent>> parents,
		Query<Data<Children>> childrenQuery,
		Query<Data<FlexboxText>> texts)
	{
		// Create Flexbox nodes for all entities
		foreach (var (entityId, flexNode) in nodes)
		{
			ref var node = ref flexNode.Ref;
			var flexboxNode = new Node();

			// Apply FlexboxNode properties to Flexbox.Node
			ApplyFlexboxNodeToNode(ref node, flexboxNode);

			// Track originating entity for extraction and hit-testing
			flexboxNode.Context = entityId.Ref;

			// If entity has FlexboxText, configure as text node with measure function
			if (texts.Contains(entityId.Ref))
			{
				var textDataTuple = texts.Get(entityId.Ref);
				textDataTuple.Deconstruct(out var textPtr);
				ref var textData = ref textPtr.Ref;
				ConfigureTextNode(flexboxNode, ref textData);
			}

			state.EntityToFlexboxNode[entityId.Ref] = flexboxNode;
		}

        // Identify roots (entities without Parent)
        foreach (var (entityId, _) in nodes)
        {
            if (!parents.Contains(entityId.Ref))
                state.RootEntities.Add(entityId.Ref);
        }

        // Recursively attach children in declared order using Children lists
        foreach (var rootId in state.RootEntities)
        {
            if (!state.EntityToFlexboxNode.TryGetValue(rootId, out var rootNode))
                continue;
            AttachChildrenRecursive(ref state, rootId, rootNode, childrenQuery);
        }
    }

    private static void AttachChildrenRecursive(
        ref FlexboxUiState state,
        ulong parentId,
        Node parentNode,
        Query<Data<Children>> childrenQuery)
    {
        if (!childrenQuery.Contains(parentId))
            return;
        var chData = childrenQuery.Get(parentId);
        chData.Deconstruct(out var listPtr);
        ref var list = ref listPtr.Ref;
        foreach (var childId in list)
        {
            if (state.EntityToFlexboxNode.TryGetValue(childId, out var childNode))
            {
                parentNode.AddChild(childNode);
                AttachChildrenRecursive(ref state, childId, childNode, childrenQuery);
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

	private static void ConfigureTextNode(Node node, ref FlexboxText textData)
	{
		// Set measure function for text nodes
		// This would need actual text measurement (e.g., via Raylib.MeasureTextEx)
		// For now, we'll use a simple approximation
		// Copy text properties into locals to avoid capturing a ref in the lambda
		var capturedText = textData.Text;
		var capturedFontSize = textData.FontSize;
		node.SetMeasureFunc((n, width, widthMode, height, heightMode) =>
		{
			// Simple text measurement approximation
			var textLength = capturedText?.Length ?? 0;
			var fontSize = capturedFontSize;
			var estimatedWidth = textLength * fontSize * 0.6f; // rough estimate
			var estimatedHeight = fontSize;

			var measuredWidth = widthMode == MeasureMode.Exactly ? width : estimatedWidth;
			var measuredHeight = heightMode == MeasureMode.Exactly ? height : estimatedHeight;

			return new Size(measuredWidth, measuredHeight);
		});
	}

	private static void ExtractLayout(
		ref FlexboxUiState state,
		ulong entityId,
		Node node,
		Vector2 parentOffset)
	{
		var layout = node.layout;

		// Assign element ID for pointer hit testing
		var elementId = state.NextElementId++;
		state.ElementToEntityMap[elementId] = entityId;

		// Compute absolute position
		var localPos = new Vector2(layout.left, layout.top);
		var absolutePos = parentOffset + localPos;

		// Extract computed layout
		var computedLayout = new ComputedLayout
		{
			ElementId = elementId,
			Position = absolutePos,
			Size = new Vector2(layout.width, layout.height),
			LocalPosition = localPos,
			Margin = new EdgeInsets(
				layout.margin.top,
				layout.margin.right,
				layout.margin.bottom,
				layout.margin.left),
			Padding = new EdgeInsets(
				layout.padding.top,
				layout.padding.right,
				layout.padding.bottom,
				layout.padding.left),
			Border = new EdgeInsets(
				layout.border.top,
				layout.border.right,
				layout.border.bottom,
				layout.border.left),
			ContentPosition = new Vector2(layout.content.x, layout.content.y),
			ContentSize = new Vector2(layout.content.width, layout.content.height),
			Direction = layout.direction,
			HadOverflow = layout.hadOverflow
		};

		state.EntityToLayout[entityId] = computedLayout;

		// Recursively extract layouts for children
		foreach (var child in node.Children)
		{
			if (child.Context is ulong childEntityId)
			{
				ExtractLayout(ref state, childEntityId, child, absolutePos);
			}
		}
	}
}

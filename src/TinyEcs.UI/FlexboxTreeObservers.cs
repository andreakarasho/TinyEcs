using System.Numerics;
using Flexbox;
using TinyEcs.Bevy;

namespace TinyEcs.UI;

/// <summary>
/// Observer-based Flexbox tree management.
/// Instead of rebuilding the entire tree every frame, observers reactively update
/// the tree only when components change.
/// </summary>
public static class FlexboxTreeObservers
{
	/// <summary>
	/// Registers all observers for reactive Flexbox tree management.
	/// Call this from FlexboxUiPlugin.Build().
	/// </summary>
	public static void RegisterObservers(App app)
	{
		// Observer 1: Create/update Flexbox node when FlexboxNode is added or changed
		app.AddObserver<OnInsert<FlexboxNode>, ResMut<FlexboxUiState>>((trigger, state) =>
		{
			var component = trigger.Component;
			CreateOrUpdateNode(trigger.EntityId, ref component, ref state.Value);
		});

		// Observer 2: Clean up when FlexboxNode is removed
		app.AddObserver<OnRemove<FlexboxNode>, ResMut<FlexboxUiState>>((trigger, state) =>
		{
			state.Value.EntityToFlexboxNode.Remove(trigger.EntityId);
		});

		// Observer 3: Update text measurement when FlexboxText is added/changed
		app.AddObserver<OnInsert<FlexboxText>, ResMut<FlexboxUiState>>((trigger, state) =>
		{
			if (state.Value.EntityToFlexboxNode.TryGetValue(trigger.EntityId, out var node))
			{
				var textData = trigger.Component;
				ConfigureTextNode(node, ref textData);
			}
		});
	}

	/// <summary>
	/// Creates or updates a Flexbox node for an entity.
	/// Called by OnInsert<FlexboxNode> observer.
	/// </summary>
	private static void CreateOrUpdateNode(
		ulong entityId,
		ref FlexboxNode flexNodeComponent,
		ref FlexboxUiState state)
	{
		// Reuse existing node if available, otherwise create new
		if (!state.EntityToFlexboxNode.TryGetValue(entityId, out var flexboxNode))
		{
			flexboxNode = new Node();
			state.EntityToFlexboxNode[entityId] = flexboxNode;
		}

		// Apply FlexboxNode properties to Flexbox.Node
		ApplyFlexboxNodeToNode(ref flexNodeComponent, flexboxNode);

		// Track originating entity
		flexboxNode.Context = entityId;

		// No need to mark dirty - layout syncs every frame now
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
		// Copy text properties into locals to avoid capturing a ref in the lambda
		var capturedText = textData.Text;
		var capturedFontSize = textData.FontSize;
		node.SetMeasureFunc((n, width, widthMode, height, heightMode) =>
		{
			// Simple text measurement approximation
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

/// <summary>
/// System to rebuild Flexbox parent-child hierarchy when it changes.
/// Runs reactively using Changed<FlexboxNodeParent> filter.
/// </summary>
public static class FlexboxHierarchySystem
{
	/// <summary>
	/// Rebuilds Flexbox hierarchy for nodes whose parent relationships changed.
	/// Much more efficient than rebuilding the entire tree every frame.
	/// </summary>
	public static void RebuildChangedHierarchy(
		ResMut<FlexboxUiState> state,
		Query<Data<FlexboxNodeParent>, Filter<Changed<FlexboxNodeParent>>> changedParents,
		Query<Data<FlexboxNode>, Filter<Without<Parent>>> rootNodes,
		Query<Data<Children>> childrenQuery)
	{
		ref var stateRef = ref state.Value;

		// Check if there were any parent changes
		bool hasChanges = false;
		foreach (var _ in changedParents)
		{
			hasChanges = true;
			break;
		}

		// Only rebuild if there were parent changes
		if (!hasChanges)
			return;

		// Clear ALL nodes' parent/child relationships before rebuilding
		// (we need to clear all because we'll be re-attaching the entire hierarchy)
		foreach (var (_, node) in stateRef.EntityToFlexboxNode)
		{
			// Clear parent reference on each child before clearing Children list
			foreach (var child in node.Children)
			{
				child.Parent = null!;
			}
			node.Children.Clear();
			node.Parent = null!;
		}

		// Rebuild roots list (only when hierarchy changes)
		stateRef.RootEntities.Clear();
		foreach (var (entityId, _) in rootNodes)
		{
			stateRef.RootEntities.Add(entityId.Ref);
		}

		// Re-attach children for all roots
		foreach (var rootId in stateRef.RootEntities)
		{
			if (stateRef.EntityToFlexboxNode.TryGetValue(rootId, out var rootNode))
			{
				AttachChildrenRecursive(ref stateRef, rootId, rootNode, childrenQuery);
			}
		}

		// No need to mark dirty - layout syncs every frame now
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
}

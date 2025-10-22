using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI.Widgets;

namespace TinyEcs.UI;

internal static class ClayUiEntityLayout
{
    public static void Build(
        ClayUiLayoutContext context,
        Query<Data<UiNode>, Filter<Without<Parent>>> roots,
        Query<Data<UiNode>> allNodes,
        Query<Data<UiText>> uiTexts,
        Query<Data<Children>> childLists)
    {
        var state = context.State;
        var world = state.World;

        // If we have a window order resource, render non-windows first, then windows in ordered stack
        UiWindowOrder? windowOrder = null;
        if (world.HasResource<UiWindowOrder>())
            windowOrder = world.GetResource<UiWindowOrder>();

        if (windowOrder is null)
        {
            foreach ((PtrRO<ulong> entityPtr, Ptr<UiNode> nodePtr) in roots)
            {
                var entityId = entityPtr.Ref;
                ref var node = ref nodePtr.Ref;
                AssignElementId(ref node, entityId);
                state.RegisterElement(entityId, node.Declaration.id);
                RenderNode(state, entityId, ref node, allNodes, uiTexts, childLists);
            }
            return;
        }

        // Collect roots into windows vs others
        var windows = new System.Collections.Generic.List<ulong>();
        var others = new System.Collections.Generic.List<(ulong id, UiNode node)>();

        foreach ((PtrRO<ulong> entityPtr, Ptr<UiNode> nodePtr) in roots)
        {
            var entityId = entityPtr.Ref;
            ref var node = ref nodePtr.Ref;
            if (world.Has<FloatingWindowState>(entityId))
            {
                windows.Add(entityId);
            }
            else
            {
                // Render others in discovery order after assigning ids
                AssignElementId(ref node, entityId);
                state.RegisterElement(entityId, node.Declaration.id);
                others.Add((entityId, node));
            }
        }

        // Render non-window roots first
        foreach (var (id, node) in others)
        {
            var n = node; // copy to ref-compatible variable
            RenderNode(state, id, ref n, allNodes, uiTexts, childLists);
        }

        // Determine which windows are tracked in order
        var ordered = new System.Collections.Generic.HashSet<ulong>();

        // First render any windows not tracked yet (background)
        foreach (var id in windows)
        {
            // We'll fill 'ordered' next; for now, consider any id not present in the order list
            bool isTracked = false;
            foreach (var orderedId in windowOrder.Enumerate())
            {
                if (orderedId == id) { isTracked = true; break; }
            }
            if (isTracked) continue;

            var data = allNodes.Get(id);
            data.Deconstruct(out _, out var nodePtr);
            ref var node = ref nodePtr.Ref;
            AssignElementId(ref node, id);
            state.RegisterElement(id, node.Declaration.id);
            RenderNode(state, id, ref node, allNodes, uiTexts, childLists);
        }

        // Then render windows using linked list order (last = topmost)
        foreach (var id in windowOrder.Enumerate())
        {
            if (!windows.Contains(id)) continue;
            var data = allNodes.Get(id);
            data.Deconstruct(out _, out var nodePtr);
            ref var node = ref nodePtr.Ref;
            AssignElementId(ref node, id);
            state.RegisterElement(id, node.Declaration.id);
            RenderNode(state, id, ref node, allNodes, uiTexts, childLists);
            ordered.Add(id);
        }
    }

	private static void RenderNode(
		ClayUiState state,
		ulong entityId,
		ref UiNode node,
		Query<Data<UiNode>> allNodes,
		Query<Data<UiText>> uiTexts,
		Query<Data<Children>> childLists)
	{
		Clay.OpenElement();

		// Update scroll offset for clipped containers before configuring
		if (node.Declaration.clip.vertical || node.Declaration.clip.horizontal)
		{
			var scrollOffset = Clay.GetScrollOffset();
			node.Declaration.clip.childOffset = scrollOffset;
		}

		Clay.ConfigureOpenElement(node.Declaration);

		if (uiTexts.Contains(entityId))
		{
			var textData = uiTexts.Get(entityId);
			textData.Deconstruct(out _, out var textPtr);
			ref var text = ref textPtr.Ref;
			if (text.HasContent)
			{
				Clay.OpenTextElement(text.Value, text.Config);
			}
		}

		if (childLists.Contains(entityId))
		{
			var childData = childLists.Get(entityId);
			childData.Deconstruct(out _, out var childrenPtr);
			ref var children = ref childrenPtr.Ref;

			foreach (var childId in children)
			{
				if (!allNodes.Contains(childId))
					continue;

				var childNodeData = allNodes.Get(childId);
				childNodeData.Deconstruct(out _, out var childNodePtr);

				ref var childNode = ref childNodePtr.Ref;
				AssignElementId(ref childNode, childId);
				state.RegisterElement(childId, childNode.Declaration.id);
				RenderNode(state, childId, ref childNode, allNodes, uiTexts, childLists);
			}
		}

		Clay.CloseElement();
	}

	private static void AssignElementId(ref UiNode node, ulong entityId)
	{
		if (node.Declaration.id.id != 0)
			return;

		var text = entityId.RealId().ToString();
		node.Declaration.id = Clay.Id(text);
	}
}

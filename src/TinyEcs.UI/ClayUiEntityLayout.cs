using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI.Widgets;

namespace TinyEcs.UI;

internal static class ClayUiEntityLayout
{
	public static void Build(
		Query<Data<UiNode>, Filter<Without<Parent>>> roots,
		Query<Data<UiNode>> allNodes,
		Query<Data<UiText>> uiTexts,
		Query<Data<Children>> childLists,
		Query<Data<FloatingWindowState>> floatingWindows,
		ResMut<UiWindowOrder> windowOrder,
		Local<HashSet<ulong>> windows)
	{
		windows.Value!.Clear();


		foreach ((PtrRO<ulong> entityPtr, Ptr<UiNode> nodePtr) in roots)
		{
			var entityId = entityPtr.Ref;
			ref var node = ref nodePtr.Ref;
			if (floatingWindows.Contains(entityId))
			{
				windows.Value!.Add(entityId);
			}
			else
			{
				// Render others in discovery order after assigning ids
				AssignElementId(ref node, entityId);
				BuildNode(entityId, ref node, allNodes, uiTexts, childLists);
			}
		}

		// Then render windows using linked list order (last = topmost)
		foreach (var id in windowOrder.Value.Enumerate())
		{
			if (!windows.Value!.Contains(id))
				continue;
			var data = allNodes.Get(id);
			data.Deconstruct(out _, out var nodePtr);
			ref var node = ref nodePtr.Ref;
			AssignElementId(ref node, id);
			BuildNode(id, ref node, allNodes, uiTexts, childLists);
		}
	}

	private static void BuildNode(
		ulong entityId,
		ref UiNode node,
		Query<Data<UiNode>> allNodes,
		Query<Data<UiText>> uiTexts,
		Query<Data<Children>> childLists)
	{
		// update scrolls before the opening element, otherwise the scorlling get lost
		if (node.Declaration.clip.vertical || node.Declaration.clip.horizontal)
		{
			// Save the childOffset that Clay just updated
			var scroll = Clay.GetScrollContainerData(node.Declaration.id);
			unsafe
			{
				if (scroll.found && scroll.scrollPosition != null)
				{
					node.Declaration.clip.childOffset = *scroll.scrollPosition;
				}
			}
		}

		Clay.OpenElement();

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
				BuildNode(childId, ref childNode, allNodes, uiTexts, childLists);
			}
		}

		Clay.CloseElement();
	}

	private static void AssignElementId(ref UiNode node, ulong entityId)
	{
		// Only compute element ID once - cache it in the node declaration
		if (node.Declaration.id.id != 0)
			return;

		// Use the entity ID directly as the hash to avoid string allocation
		// This is safe because entity IDs are unique and stable
		node.Declaration.id = new Clay_ElementId
		{
			id = (uint)(entityId ^ (entityId >> 32)), // Fold 64-bit ID into 32-bit hash
			stringId = default
		};
	}
}

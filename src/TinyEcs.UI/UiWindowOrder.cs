using System.Collections.Generic;

namespace TinyEcs.UI;

public sealed class UiWindowOrder
{
	private readonly LinkedList<ulong> _order = new();
	private readonly Dictionary<ulong, LinkedListNode<ulong>> _nodes = new();

	public void MoveToTop(ulong windowId)
	{
		if (windowId == 0) return;
		if (_nodes.TryGetValue(windowId, out var node))
		{
			if (node.List != null) _order.Remove(node);
			_order.AddLast(node);
		}
		else
		{
			var newNode = new LinkedListNode<ulong>(windowId);
			_order.AddLast(newNode);
			_nodes[windowId] = newNode;
		}
	}

	/// <summary>
	/// Removes a window from the z-order stack.
	/// </summary>
	/// <param name="windowId">The window entity ID to remove.</param>
	/// <returns>True if the window was found and removed, false otherwise.</returns>
	public bool Remove(ulong windowId)
	{
		if (windowId == 0) return false;
		if (_nodes.TryGetValue(windowId, out var node))
		{
			if (node.List != null) _order.Remove(node);
			_nodes.Remove(windowId);
			return true;
		}
		return false;
	}

	public IEnumerable<ulong> Enumerate() => _order;
}


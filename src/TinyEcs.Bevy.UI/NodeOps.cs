using System.Numerics;

namespace TinyEcs.Bevy.UI;

/// <summary>
/// Compare-before-write <see cref="Node"/> edits through a query ref (Bevy's
/// set_if_neq): each helper writes and marks <c>SetChanged&lt;Node&gt;</c> only on a
/// real change, so calling them every frame keeps the relayout gate closed.
/// </summary>
public static class UiNodeOps
{
	public static void SetDisplay(Query<Data<Node>> nodeQ, ulong entity, Display d)
	{
		if (!nodeQ.TryGet(entity, out var row)) return;
		var (_, n) = row;
		if (n.Ref.Display == d) return;
		n.Ref.Display = d;
		nodeQ.SetChanged<Node>(entity);
	}

	public static void SetXY(Query<Data<Node>> nodeQ, ulong entity, float x, float y)
	{
		if (!nodeQ.TryGet(entity, out var row)) return;
		var (_, n) = row;
		if (SamePx(n.Ref.Left, x) && SamePx(n.Ref.Top, y)) return;
		n.Ref.Left = Val.Px(x);
		n.Ref.Top = Val.Px(y);
		nodeQ.SetChanged<Node>(entity);
	}

	public static void SetSize(Query<Data<Node>> nodeQ, ulong entity, float w, float h)
	{
		if (!nodeQ.TryGet(entity, out var row)) return;
		var (_, n) = row;
		if (SamePx(n.Ref.Width, w) && SamePx(n.Ref.Height, h)) return;
		n.Ref.Width = Val.Px(w);
		n.Ref.Height = Val.Px(h);
		nodeQ.SetChanged<Node>(entity);
	}

	/// <summary>Hide every direct child of <paramref name="root"/> except
	/// <paramref name="keep"/> (0 = none), which is shown.</summary>
	public static void HideChildren(Query<Data<TinyEcs.Children>> childrenQ, Query<Data<Node>> nodeQ, ulong root, ulong keep)
	{
		if (!childrenQ.TryGet(root, out var kidsRow)) return;
		var (_, kids) = kidsRow;
		foreach (var cid in kids.Ref)
			SetDisplay(nodeQ, cid, cid == keep ? Display.Flex : Display.None);
	}

	/// <summary>Show every direct child of <paramref name="root"/>. Call on a restore
	/// edge only: per frame it would unhide children hidden on purpose.</summary>
	public static void ShowChildren(Query<Data<TinyEcs.Children>> childrenQ, Query<Data<Node>> nodeQ, ulong root)
	{
		if (!childrenQ.TryGet(root, out var kidsRow)) return;
		var (_, kids) = kidsRow;
		foreach (var cid in kids.Ref)
			SetDisplay(nodeQ, cid, Display.Flex);
	}

	/// <summary>True when <paramref name="entity"/> or an ancestor is Display.None. A
	/// hidden subtree keeps its last ComputedNode frozen, so bounds polls over
	/// ComputedNode must check this.</summary>
	public static bool Hidden(ulong entity, Query<Data<TinyEcs.Parent>> parents, Query<Data<Node>> nodes)
	{
		ulong cur = entity;
		for (int i = 0; i < 32 && cur != 0; i++)
		{
			if (nodes.TryGet(cur, out var nodeRow))
			{
				var (_, n) = nodeRow;
				if (n.Ref.Display == Display.None) return true;
			}
			if (!parents.TryGet(cur, out var parentRow)) return false;
			var (_, p) = parentRow;
			cur = (ulong)p.Ref.Id;
		}
		return false;
	}

	private static bool SamePx(in Val v, float px) => v.Type == ValType.Px && v.Value == px;
}

/// <summary>Opt-in tag on a window ROOT: its position is remembered under Key while it
/// lives and restored when a window with the same Key is next inserted.</summary>
public struct UiPositionKey { public ulong Key; }

/// <summary>Session position store (not persisted).</summary>
public sealed class UiPositionCache { public readonly Dictionary<ulong, Vector2> Saved = new(); }

/// <summary>
/// In-session window position memory. Records every keyed window's position each
/// frame — so every close path (gesture, host teardown, despawn of a whole scene)
/// is a "save on close" with no per-close hook — and restores it on the key's
/// insert, whatever spawned the window. A later system may still override it.
/// </summary>
public sealed class UiPositionCachePlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddResource(new UiPositionCache());
		var recordFn = Record;
		app.AddSystem(recordFn).InStage(Stage.PostUpdate).Build();
		app.AddObserver<OnInsert<UiPositionKey>, Res<UiPositionCache>, Query<Data<Node>>>(RestoreOnInsert);
	}

	private static void Record(Query<Data<Node, UiPositionKey>> q, ResMut<UiPositionCache> cache)
	{
		foreach (var (_, node, key) in q)
			cache.Value.Saved[key.Ref.Key] = new Vector2((int)node.Ref.Left.Value, (int)node.Ref.Top.Value);
	}

	private static void RestoreOnInsert(
		OnInsert<UiPositionKey> trigger,
		Res<UiPositionCache> cache,
		Query<Data<Node>> nodeQ)
	{
		if (!cache.Value.Saved.TryGetValue(trigger.Component.Key, out var pt)) return;
		var id = trigger.EntityId;
		if (!nodeQ.Contains(id)) return;
		var (_, node) = nodeQ.Get(id);
		node.Ref.Left = Val.Px(pt.X);
		node.Ref.Top = Val.Px(pt.Y);
		nodeQ.SetChanged<Node>(id);
	}
}

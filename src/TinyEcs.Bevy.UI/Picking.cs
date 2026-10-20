using System.Numerics;

namespace TinyEcs.Bevy.UI;

// The one hit-test. Everything that asks "what is under the cursor?" — the
// library's own Interaction / UiClick pipeline, host gestures (window drag,
// right-click close, pickup), widget lookups, tooltips — goes through UiPick,
// so every consumer agrees on the answer: the topmost PAINTED element, ranked by
// ComputedNode.PaintOrder (render-command index = z-then-tree order), passing the
// optional pixel-perfect hook, not hidden by a Display.None ancestor and not
// clipped away by an Overflow.Scroll/Clip ancestor.

public readonly struct UiHit
{
	public readonly ulong Entity;
	public readonly int PaintOrder;

	public UiHit(ulong entity, int paintOrder)
	{
		Entity = entity;
		PaintOrder = paintOrder;
	}

	public static readonly UiHit None = new(0, int.MinValue);
	public bool Found => Entity != 0;
}

/// <summary>
/// Host hook for pixel-perfect picking (see <see cref="UiClayContext.PixelHitTest"/>).
/// Called for every paint candidate that is NOT tagged <see cref="UiContainsByBounds"/>,
/// with the element's <see cref="UiCustom.Data"/> (null when it has none) and its
/// box. The hook owns the WHOLE test, bounds reject included — the library does
/// not pre-reject by the box, so a host may widen it (e.g. a sprite that draws
/// past its node). Return false to let the pick fall through to what is behind.
/// </summary>
public delegate bool UiPixelHitTest(ulong entity, object? customData, Vector2 pos, in ComputedNode box);

public static class UiPick
{
	/// <summary>Topmost painted element at <paramref name="pos"/>, without overflow-clip /
	/// hidden-ancestor pruning. Prefer the full overload (or <see cref="UiGesturePick"/>).</summary>
	public static UiHit Topmost(
		Vector2 pos,
		Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
		UiPixelHitTest? pixelHit = null)
		=> Topmost(pos, rendered, null, null, null, pixelHit);

	// An element is a pick SURFACE when it paints: a UiCustom (even with null
	// Data — an intentional invisible hit surface), a BackgroundColor or Text. A
	// bare layout node also gets a ComputedNode (from a ScissorStart) but paints
	// nothing, so it must not capture the pointer over its transparent area.
	// Image-only / border-only elements are hit only with UiContainsByBounds.
	//
	// `parents` enables overflow clip + hidden pruning (see HiddenOrClippedOut);
	// `nodes` extends the hidden walk to bare layout ancestors (they never appear
	// in `rendered`); `clipToParent` makes the clip walk honour floats: an
	// Absolute element WITHOUT ClipToParent escapes its ancestors' clip, exactly
	// like the renderer draws it. Without that query every ancestor clips.
	// `bounds`: UiContainsByBounds elements hit anywhere in their box (no hook).
	public static UiHit Topmost(
		Vector2 pos,
		Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
		Query<Data<TinyEcs.Parent>>? parents,
		Query<Data<UiContainsByBounds>>? bounds = null,
		Query<Data<Node>>? nodes = null,
		UiPixelHitTest? pixelHit = null,
		Query<Data<ClipToParent>>? clipToParent = null)
	{
		var hit = UiHit.None;
		foreach (var (ent, computed, node, custom, bg, text) in rendered)
		{
			if (node.Ref.Display == Display.None) continue;
			ref readonly var bb = ref computed.Ref;
			if (bb.PaintOrder < hit.PaintOrder) continue;
			bool boundsOnly = bounds != null && bounds.Contains(ent.Ref);
			if (!boundsOnly && !custom.IsValid() && !bg.IsValid() && !text.IsValid()) continue;
			if (boundsOnly || pixelHit == null)
			{
				if (!bb.Contains(pos)) continue;
			}
			else if (!pixelHit(ent.Ref, custom.IsValid() ? custom.Ref.Data : null, pos, in bb))
				continue;
			if (parents != null && HiddenOrClippedOut(ent.Ref, pos, rendered, parents, nodes, clipToParent)) continue;
			hit = new UiHit(ent.Ref, bb.PaintOrder);
		}
		return hit;
	}

	/// <summary>
	/// True when <paramref name="entity"/> is hidden by a Display.None ancestor (its
	/// frozen ComputedNode must not answer picks: nothing removes a hidden
	/// subtree's ComputedNode), or when <paramref name="pos"/> falls outside the box
	/// of an Overflow.Scroll/Clip ancestor that clips it.
	/// </summary>
	public static bool HiddenOrClippedOut(
		ulong entity,
		Vector2 pos,
		Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
		Query<Data<TinyEcs.Parent>> parents,
		Query<Data<Node>>? nodes,
		Query<Data<ClipToParent>>? clipToParent = null)
	{
		ulong cur = entity;
		bool clipped = true;
		for (int i = 0; i < 32; i++)
		{
			// A float that does not opt into ClipToParent is drawn unclipped by
			// every ancestor scissor — so it is hittable outside them too.
			if (clipped && clipToParent != null && !clipToParent.Contains(cur)
			    && TryNode(cur, rendered, nodes, out var curNode)
			    && curNode.PositionType == PositionType.Absolute)
				clipped = false;

			if (!parents.TryGet(cur, out var parentRow)) break;
			var (_, p) = parentRow;
			cur = (ulong)p.Ref.Id;
			if (cur == 0) continue;
			if (nodes != null && nodes.TryGet(cur, out var ancestorNodeRow))
			{
				var (_, an) = ancestorNodeRow;
				if (an.Ref.Display == Display.None) return true;
			}
			if (!clipped || !rendered.TryGet(cur, out var renderedRow)) continue;
			var (_, comp, node, _, _, _) = renderedRow;
			if (node.Ref.Overflow == Overflow.Visible) continue;
			if (!comp.Ref.Contains(pos))
				return true;
		}
		return false;
	}

	private static bool TryNode(
		ulong entity,
		Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
		Query<Data<Node>>? nodes,
		out Node node)
	{
		if (nodes != null && nodes.TryGet(entity, out var nodeRow))
		{
			var (_, n) = nodeRow;
			node = n.Ref;
			return true;
		}
		if (rendered.TryGet(entity, out var renderedRow))
		{
			var (_, _, n, _, _, _) = renderedRow;
			node = n.Ref;
			return true;
		}
		node = default;
		return false;
	}

	/// <summary>Walk the Parent chain from <paramref name="entity"/> (inclusive) to the
	/// nearest ancestor matching <paramref name="markerQ"/>; 0 if none. Depth-capped
	/// against a malformed parent link.</summary>
	public static ulong AncestorWith<T>(
		ulong entity,
		Query<Data<T>> markerQ,
		Query<Data<TinyEcs.Parent>> parents,
		int maxDepth = 64) where T : struct
	{
		ulong cur = entity;
		for (int i = 0; i < maxDepth && cur != 0; i++)
		{
			if (markerQ.Contains(cur)) return cur;
			if (!parents.TryGet(cur, out var parentRow)) return 0;
			var (_, p) = parentRow;
			cur = (ulong)p.Ref.Id;
		}
		return 0;
	}

	/// <summary>The <see cref="UiMovable"/> window root owning <paramref name="entity"/>
	/// (inclusive), 0 if none. The hit is usually a child of the window a gesture targets.</summary>
	public static ulong MovableRoot(
		ulong entity,
		Query<Data<Node, GlobalZIndex>, Filter<With<UiMovable>>> movables,
		Query<Data<TinyEcs.Parent>> parents)
	{
		ulong cur = entity;
		for (int i = 0; i < 32 && cur != 0 && !movables.Contains(cur); i++)
		{
			if (!parents.TryGet(cur, out var parentRow)) return 0;
			var (_, parent) = parentRow;
			cur = (ulong)parent.Ref.Id;
		}
		return cur != 0 && movables.Contains(cur) ? cur : 0;
	}
}

/// <summary>
/// System param bundling every query the pick needs, plus the per-layout
/// <see cref="UiPickCache"/>. Gestures ask it instead of re-declaring the queries;
/// answers for the cached cursor position cost nothing.
/// </summary>
public sealed class UiGesturePick : CompositeSystemParam
{
	public readonly Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> Rendered;
	public readonly Query<Data<Node, GlobalZIndex>, Filter<With<UiMovable>>> Movables;
	public readonly Query<Data<TinyEcs.Parent>> Parents;
	public readonly Query<Data<UiContainsByBounds>> Bounds;
	public readonly Query<Data<Node>> Nodes;
	public readonly Query<Data<ClipToParent>> ClipToParents;
	public readonly Res<UiPickCache> Cache;

	public UiGesturePick()
	{
		Rendered = Add(new Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>>());
		Movables = Add(new Query<Data<Node, GlobalZIndex>, Filter<With<UiMovable>>>());
		Parents = Add(new Query<Data<TinyEcs.Parent>>());
		Bounds = Add(new Query<Data<UiContainsByBounds>>());
		Nodes = Add(new Query<Data<Node>>());
		ClipToParents = Add(new Query<Data<ClipToParent>>());
		Cache = Add(new Res<UiPickCache>());
	}

	// Bare test apps compose gesture systems without UiPlugin: an empty cache
	// just means every call computes live.
	public override void Initialize(App app)
	{
		if (!app.HasResource<UiPickCache>()) app.AddResource(new UiPickCache());
		base.Initialize(app);
	}

	/// <summary>The pixel hook the cache last mirrored from <see cref="UiClayContext.PixelHitTest"/>.</summary>
	public UiPixelHitTest? PixelHitTest => Cache.Value.PixelHitTest;

	/// <summary>Live pick (no cache) with every filter applied.</summary>
	public UiHit Compute(Vector2 pos)
		=> UiPick.Topmost(pos, Rendered, Parents, Bounds, Nodes, Cache.Value.PixelHitTest, ClipToParents);

	/// <summary>Topmost painted, visible element under <paramref name="pos"/>. Served
	/// from the cache when it was computed at this position for the current layout.</summary>
	public UiHit Topmost(Vector2 pos)
	{
		var c = Cache.Value;
		return c.Valid && c.Pos == pos ? c.Hit : Compute(pos);
	}

	public ulong MovableRoot(ulong entity)
		=> UiPick.MovableRoot(entity, Movables, Parents);

	/// <summary>The movable window root under the cursor (0 if none).</summary>
	public ulong TopmostMovable(Vector2 pos)
	{
		var c = Cache.Value;
		return c.Valid && c.Pos == pos ? c.Window : MovableRoot(Topmost(pos).Entity);
	}

	/// <summary>Does a press at <paramref name="pos"/> land on the window owning
	/// <paramref name="entity"/>, not one stacked over it? For gesture areas found by
	/// a bounds test that are not themselves pickable.</summary>
	public bool IsTopmostWindow(Vector2 pos, ulong entity)
		=> MovableRoot(entity) == TopmostMovable(pos);

	/// <summary>The entity matching <paramref name="widgets"/> at or above the topmost
	/// hit (0 if none): how a widget gesture finds its widget. The widget must be a
	/// pick surface (paints, or UiContainsByBounds).</summary>
	public ulong TopmostWith<TData, TFilter>(Vector2 pos, Query<TData, TFilter> widgets)
		where TData : struct, IData<TData>, IQueryComponentAccess, allows ref struct
		where TFilter : struct, IFilter<TFilter>, IQueryFilterAccess, allows ref struct
	{
		ulong cur = Topmost(pos).Entity;
		for (int i = 0; i < 64 && cur != 0; i++)
		{
			if (widgets.Contains(cur)) return cur;
			if (!Parents.TryGet(cur, out var row)) return 0;
			var (_, p) = row;
			cur = (ulong)p.Ref.Id;
		}
		return 0;
	}

	/// <summary>Is the topmost hit <paramref name="entity"/> itself or inside it?</summary>
	public bool Hits(Vector2 pos, ulong entity)
	{
		if (entity == 0) return false;
		ulong cur = Topmost(pos).Entity;
		for (int i = 0; i < 64 && cur != 0; i++)
		{
			if (cur == entity) return true;
			if (!Parents.TryGet(cur, out var row)) return false;
			var (_, p) = row;
			cur = (ulong)p.Ref.Id;
		}
		return false;
	}
}

/// <summary>
/// Once-per-layout answer to "what is topmost under the pointer?". UiPlugin
/// refreshes it at <see cref="UiPointer.Position"/> before PreUpdate (for the
/// frame's gestures) and right after layout (for InteractionSystem and late
/// gestures). Readers use it only for the exact position it was computed at.
/// </summary>
public sealed class UiPickCache
{
	public bool Valid;
	public Vector2 Pos;
	public UiHit Hit;
	public ulong Window;
	// Mirrored from UiClayContext.PixelHitTest on every refresh so a pick param
	// needs no access to the Clay context.
	public UiPixelHitTest? PixelHitTest;

	// The cache is a class: the pick param's Res<UiPickCache> hands out the
	// instance, so the refresh writes through it.
	internal static void Refresh(
		Res<UiPointer> pointer,
		Res<UiClayContext> ctx,
		UiGesturePick pick)
	{
		var c = pick.Cache.Value;
		c.PixelHitTest = ctx.Value.PixelHitTest;
		var pos = pointer.Value.Position;
		c.Hit = pick.Compute(pos);
		c.Window = pick.MovableRoot(c.Hit.Entity);
		c.Pos = pos;
		c.Valid = true;
	}
}

/// <summary>
/// Owns <see cref="UiPickCache"/> and its two refresh points. Added by
/// <see cref="UiPlugin"/>; usable alone (with <see cref="UiPointer"/> and
/// <see cref="UiClayContext"/> resources) by headless apps that hand-build
/// ComputedNodes.
/// </summary>
public sealed class UiPickCachePlugin : IPlugin
{
	public void Build(App app)
	{
		if (!app.HasResource<UiPickCache>()) app.AddResource(new UiPickCache());
		app.AddStage(UiPlugin.UiPickPreGestureStage).After(Stage.First).Before(Stage.PreUpdate);
		app.AddStage(UiPlugin.UiPickPostLayoutStage).After(UiPlugin.UiLayoutStage).Before(UiPlugin.UiPostLayoutStage);
		app.AddSystem((Res<UiPointer> p, Res<UiClayContext> c, UiGesturePick pick) =>
			UiPickCache.Refresh(p, c, pick))
			.InStage(UiPlugin.UiPickPreGestureStage).SingleThreaded().Build();
		app.AddSystem((Res<UiPointer> p, Res<UiClayContext> c, UiGesturePick pick) =>
			UiPickCache.Refresh(p, c, pick))
			.InStage(UiPlugin.UiPickPostLayoutStage).SingleThreaded().Build();
	}
}

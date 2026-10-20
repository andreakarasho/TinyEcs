using System.Numerics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using Xunit;
using ClayColor = Clay.Color;
using UiNode = TinyEcs.Bevy.UI.Node;
using UiText = TinyEcs.Bevy.UI.Text;
using BevyStage = TinyEcs.Bevy.Stage;

namespace TinyEcs.Tests;

// The one hit-test (UiPick): topmost painted element by PaintOrder, overlap /
// walk-to-window-root / clip / hidden-ancestor rules. UiCustom.Data == null and
// no pixel hook make every surface a solid box, so the ranking logic is
// exercised in pure ECS; the layout-driven cases run the real Clay pass.
[Collection("ClayUi")]
public class UiPickTests
{
	private static ulong Element(World w, float x, float y, float width, float height, int paintOrder)
		=> w.Entity()
			.Set(new UiNode())
			.Set(new UiCustom { Data = null })
			.Set(new ComputedNode
			{
				Position = new Vector2(x, y),
				Size = new Vector2(width, height),
				PaintOrder = paintOrder,
			}).ID;

	private static void MakeMovable(World w, ulong id)
	{
		w.Set<UiMovable>(id);
		w.Set(id, new GlobalZIndex(0));
	}

	private static ulong TextElement(World w, float x, float y, float width, float height, int paintOrder)
		=> w.Entity()
			.Set(new UiNode())
			.Set(new UiText("x"))
			.Set(new ComputedNode
			{
				Position = new Vector2(x, y),
				Size = new Vector2(width, height),
				PaintOrder = paintOrder,
			}).ID;

	private static ulong BareLayoutNode(World w, float x, float y, float width, float height, int paintOrder)
		=> w.Entity()
			.Set(new UiNode())
			.Set(new ComputedNode
			{
				Position = new Vector2(x, y),
				Size = new Vector2(width, height),
				PaintOrder = paintOrder,
			}).ID;

	private static Query<Data<ComputedNode, UiNode, UiCustom, BackgroundColor, UiText>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<UiText>>> Rendered(App app)
		=> Fetch(new Query<Data<ComputedNode, UiNode, UiCustom, BackgroundColor, UiText>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<UiText>>>(), app);

	private static Query<Data<UiNode, GlobalZIndex>, Filter<With<UiMovable>>> Movables(App app)
		=> Fetch(new Query<Data<UiNode, GlobalZIndex>, Filter<With<UiMovable>>>(), app);

	private static Query<Data<TinyEcs.Parent>> Parents(App app) => Fetch(new Query<Data<TinyEcs.Parent>>(), app);
	private static Query<Data<UiNode>> Nodes(App app) => Fetch(new Query<Data<UiNode>>(), app);
	private static Query<Data<ClipToParent>> ClipToParents(App app) => Fetch(new Query<Data<ClipToParent>>(), app);

	private static T Fetch<T>(T q, App app) where T : ISystemParam
	{
		q.Initialize(app);
		q.Fetch(app);
		return q;
	}

	[Fact]
	public void Topmost_returns_none_when_nothing_under_pointer()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		Element(w, 0, 0, 10, 10, 1);
		var hit = UiPick.Topmost(new Vector2(500, 500), Rendered(app));
		Assert.False(hit.Found);
	}

	[Fact]
	public void Topmost_picks_the_only_element_under_pointer()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var e = Element(w, 0, 0, 100, 100, 3);
		var hit = UiPick.Topmost(new Vector2(50, 50), Rendered(app));
		Assert.True(hit.Found);
		Assert.Equal(e, hit.Entity);
	}

	[Fact]
	public void Topmost_picks_highest_paint_order_among_overlaps()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		// Spawn the FRONT one first so entity-id order does NOT match paint
		// order — guards against the ClayId/entity-id tiebreak bug.
		var front = Element(w, 0, 0, 100, 100, 9);
		Element(w, 0, 0, 100, 100, 4);
		var hit = UiPick.Topmost(new Vector2(50, 50), Rendered(app));
		Assert.Equal(front, hit.Entity);
	}

	[Fact]
	public void Topmost_ignores_elements_the_pointer_misses()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		Element(w, 0, 0, 40, 40, 9);     // high paint order, but pointer is outside it
		var inRange = Element(w, 100, 100, 40, 40, 2);
		var hit = UiPick.Topmost(new Vector2(120, 120), Rendered(app));
		Assert.Equal(inRange, hit.Entity);
	}

	[Fact]
	public void Topmost_skips_hidden_elements()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var visible = Element(w, 0, 0, 100, 100, 2);
		w.Entity()
			.Set(new UiNode { Display = Display.None })
			.Set(new UiCustom { Data = null })
			.Set(new ComputedNode
			{
				Position = new Vector2(0, 0),
				Size = new Vector2(100, 100),
				PaintOrder = 99, // would win if not hidden (stale ComputedNode case)
			});
		var hit = UiPick.Topmost(new Vector2(50, 50), Rendered(app));
		Assert.Equal(visible, hit.Entity);
	}

	[Fact]
	public void MovableRoot_returns_self_when_the_hit_is_the_movable()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var root = Element(w, 0, 0, 100, 100, 1);
		MakeMovable(w, root);
		Assert.Equal(root, UiPick.MovableRoot(root, Movables(app), Parents(app)));
	}

	[Fact]
	public void MovableRoot_walks_child_up_to_its_window_root()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var root = Element(w, 0, 0, 100, 100, 1);
		MakeMovable(w, root);
		var content = Element(w, 0, 0, 100, 100, 2);
		var item = Element(w, 10, 10, 20, 20, 3);
		w.AddChild(root, content);
		w.AddChild(content, item);
		// A click resolves to the topmost child, which must walk up to the root.
		Assert.Equal(root, UiPick.MovableRoot(item, Movables(app), Parents(app)));
	}

	[Fact]
	public void MovableRoot_returns_zero_when_no_movable_ancestor()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var loose = Element(w, 0, 0, 100, 100, 1); // no UiMovable anywhere in chain
		Assert.Equal(0UL, UiPick.MovableRoot(loose, Movables(app), Parents(app)));
	}

	[Fact]
	public void Topmost_hits_text_element_without_uicustom()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var label = TextElement(w, 0, 0, 100, 20, 3);
		var hit = UiPick.Topmost(new Vector2(50, 10), Rendered(app));
		Assert.Equal(label, hit.Entity);
	}

	// Resize grips are found by a bounds test on the grip itself; the press must
	// still belong to the grip's window, not to one stacked over it.
	[Fact]
	public void Covered_resize_grip_does_not_belong_to_the_window_behind()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var back = Element(w, 0, 0, 300, 300, 1);
		MakeMovable(w, back);
		var grip = Element(w, 280, 280, 20, 20, 2);
		w.AddChild(back, grip);
		var front = Element(w, 250, 250, 200, 200, 5);
		MakeMovable(w, front);

		bool backOwns = true, frontOwns = false, backOwnsUncovered = false;
		app.AddSystem((UiGesturePick pick) =>
		{
			backOwns = pick.IsTopmostWindow(new Vector2(290, 290), back);
			frontOwns = pick.IsTopmostWindow(new Vector2(290, 290), front);
			backOwnsUncovered = pick.IsTopmostWindow(new Vector2(10, 10), back);
		}).InStage(BevyStage.Update).Build();
		app.Update();

		Assert.False(backOwns);
		Assert.True(frontOwns);
		Assert.True(backOwnsUncovered);
	}

	// Same question for a scroll container outside any movable window (a modal's
	// list): it owns the press unless a movable window is stacked over it.
	[Fact]
	public void Unowned_scroll_container_owns_the_press_only_when_uncovered()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var list = Element(w, 0, 0, 200, 200, 1);
		var window = Element(w, 150, 150, 200, 200, 5);
		MakeMovable(w, window);

		bool uncovered = false, covered = true;
		app.AddSystem((UiGesturePick pick) =>
		{
			uncovered = pick.IsTopmostWindow(new Vector2(190, 10), list);
			covered = pick.IsTopmostWindow(new Vector2(190, 190), list);
		}).InStage(BevyStage.Update).Build();
		app.Update();

		Assert.True(uncovered);
		Assert.False(covered);
	}

	// A window's title TEXT (no UiCustom) sits over a window behind it, in a spot
	// the front window's own background doesn't cover. Clicking the text must
	// resolve to the FRONT window (the text's owner), not the window behind.
	[Fact]
	public void Title_text_resolves_to_its_own_window_not_the_one_behind()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var backRoot = Element(w, 0, 0, 300, 300, 1); // opaque window behind
		MakeMovable(w, backRoot);

		var frontRoot = Element(w, 50, 0, 10, 10, 5); // front window root (small, elsewhere)
		MakeMovable(w, frontRoot);
		var title = TextElement(w, 100, 5, 120, 20, 9); // title text over the back window
		w.AddChild(frontRoot, title);

		var hit = UiPick.Topmost(new Vector2(110, 12), Rendered(app));
		Assert.Equal(title, hit.Entity);
		Assert.Equal(frontRoot, UiPick.MovableRoot(hit.Entity, Movables(app), Parents(app)));
	}

	// A scrollable text run keeps its full-content bbox (taller than the scroll
	// viewport). UiPick must clip it to the Overflow.Scroll ancestor's box, or
	// the overflow is grab-able past the window edge.
	[Fact]
	public void Topmost_clips_overflowing_child_to_scroll_ancestor()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var root = Element(w, 0, 0, 100, 200, 1);
		MakeMovable(w, root);
		// scroll viewport: 100x50
		var outer = w.Entity()
			.Set(new UiNode { Overflow = Overflow.Scroll })
			.Set(new UiCustom { Data = null })
			.Set(new ComputedNode { Position = new Vector2(0, 0), Size = new Vector2(100, 50), PaintOrder = 2 }).ID;
		// content overflows to 200 tall
		var inner = Element(w, 0, 0, 100, 200, 3);
		w.AddChild(root, outer);
		w.AddChild(outer, inner);

		// Inside inner's full bbox but BELOW the viewport -> clipped, not the inner.
		var below = UiPick.Topmost(new Vector2(50, 120), Rendered(app), Parents(app));
		Assert.NotEqual(inner, below.Entity);
		// Inside the viewport -> inner is the topmost hit.
		var inside = UiPick.Topmost(new Vector2(50, 30), Rendered(app), Parents(app));
		Assert.Equal(inner, inside.Entity);
	}

	// Integration test through the REAL Clay layout: a window with a bare
	// Overflow.Scroll container holding tall content. A click well below the
	// window must NOT grab it via the content that overflows the viewport.
	// (Unit test above hand-builds ComputedNode; this one exercises the actual
	// layout + ComputedNode write, which is where a bare scroll container with
	// no render command of its own would otherwise have no clip box.)
	[Fact]
	public void Scroll_overflow_not_grabbable_below_window_through_layout()
	{
		var app = new App(ThreadingMode.Single);
		app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600) });
		var w = app.GetWorld();

		var root = w.Entity()
			.Set(new UiNode { PositionType = PositionType.Absolute, Left = Val.Px(50), Top = Val.Px(50), Width = Val.Px(200), Height = Val.Px(100) })
			.Set<UiMovable>()
			.Set(new GlobalZIndex(0))
			.Set(new BackgroundColor(new ClayColor(40, 40, 40, 255))).ID;
		var outer = w.Entity()
			.Set(new UiNode { PositionType = PositionType.Absolute, Left = Val.Px(10), Top = Val.Px(10), Width = Val.Px(180), Height = Val.Px(50), Overflow = Overflow.Scroll })
			.Set(new ScrollPosition()).ID;                 // bare scroll viewport (no own paint)
		var inner = w.Entity()
			.Set(new UiNode { Width = Val.Px(180), Height = Val.Px(300) })  // overflows the 50px viewport
			.Set(new BackgroundColor(new ClayColor(200, 0, 0, 255))).ID;
		w.AddChild(root, outer);
		w.AddChild(outer, inner);

		app.Update();   // Clay layout + ComputedNode write

		// Below the window (root bottom = 150) but within the inner's overflow.
		var below = UiPick.Topmost(new Vector2(120, 250), Rendered(app), Parents(app));
		var owner = UiPick.MovableRoot(below.Entity, Movables(app), Parents(app));
		Assert.NotEqual(root, owner);   // overflow must not be a grab handle outside the window

		// Inside the viewport: still resolves to the window.
		var inside = UiPick.Topmost(new Vector2(120, 75), Rendered(app), Parents(app));
		Assert.Equal(root, UiPick.MovableRoot(inside.Entity, Movables(app), Parents(app)));
	}

	// A UiMovable root that carries NO render surface of its own must be
	// hittable ONLY through its painting children — a click on a child
	// walks up to the root (so drag / click-capture claim it), but a click on the
	// root's empty area is NOT a hit (it passes through to the world / window
	// behind instead of the whole rect trapping it).
	[Fact]
	public void Bare_movable_root_only_hit_via_its_painting_children()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var root = w.Entity()
			.Set(new UiNode())          // bare: no UiCustom/Text/Bg, no ComputedNode
			.Set<UiMovable>()
			.Set(new GlobalZIndex(0)).ID;
		var child = Element(w, 0, 0, 50, 50, 2);   // paints (solid hit), covers only the top-left
		w.AddChild(root, child);

		// Over the painting child -> resolves up to the root (claim/drag target).
		var onChild = UiPick.Topmost(new Vector2(25, 25), Rendered(app), Parents(app));
		Assert.Equal(child, onChild.Entity);
		Assert.Equal(root, UiPick.MovableRoot(onChild.Entity, Movables(app), Parents(app)));

		// Over the root's empty area (no child) -> NO hit: nothing of the window
		// traps the click, so it passes through (the trespass-vs-no-trespass line).
		var onGap = UiPick.Topmost(new Vector2(150, 150), Rendered(app), Parents(app));
		Assert.False(onGap.Found);
	}

	// A bare layout node paints nothing — it must not be a hit, or a window's
	// invisible full-size wrapper would capture clicks over its whole box.
	[Fact]
	public void Topmost_skips_bare_layout_node()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		BareLayoutNode(w, 0, 0, 100, 100, 5);
		var hit = UiPick.Topmost(new Vector2(50, 50), Rendered(app));
		Assert.False(hit.Found);
	}

	// A window root that DOES paint (an invisible UiCustom hit surface, modelled
	// as a solid box) sits under a bare full-size content wrapper. A click
	// resolves THROUGH the invisible wrapper to the root, so the window stays
	// draggable from anywhere over its frame.
	[Fact]
	public void Bare_wrapper_falls_through_to_the_painting_window_root()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var root = Element(w, 0, 0, 200, 80, 1); // UiCustom-bearing => a hit surface
		MakeMovable(w, root);
		var wrapper = BareLayoutNode(w, 0, 0, 200, 80, 7); // invisible, on top
		w.AddChild(root, wrapper);

		var hit = UiPick.Topmost(new Vector2(100, 40), Rendered(app));
		Assert.Equal(root, hit.Entity);
		Assert.Equal(root, UiPick.MovableRoot(hit.Entity, Movables(app), Parents(app)));
	}

	[Fact]
	public void Topmost_then_MovableRoot_resolves_front_window_over_back()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		// Back window (root + child) painted first; front window painted on top.
		var backRoot = Element(w, 0, 0, 100, 100, 1);
		MakeMovable(w, backRoot);
		var backChild = Element(w, 0, 0, 100, 100, 2);
		w.AddChild(backRoot, backChild);

		var frontRoot = Element(w, 40, 40, 100, 100, 5);
		MakeMovable(w, frontRoot);
		var frontChild = Element(w, 40, 40, 100, 100, 6);
		w.AddChild(frontRoot, frontChild);

		// Pointer in the overlap region: must resolve to the FRONT window.
		var hit = UiPick.Topmost(new Vector2(60, 60), Rendered(app));
		var owner = UiPick.MovableRoot(hit.Entity, Movables(app), Parents(app));
		Assert.Equal(frontRoot, owner);
	}

	private struct Slot;

	// A Display.None wrapper prunes its subtree from layout, but the
	// descendants keep their last ComputedNode (stale box, stale high paint
	// order). They must not swallow presses aimed at a live element below.
	[Fact]
	public void Descendant_of_hidden_wrapper_is_not_a_pick_candidate()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var liveRoot = Element(w, 0, 0, 100, 100, 1);
		MakeMovable(w, liveRoot);
		var live = Element(w, 10, 10, 20, 20, 3);
		w.AddChild(liveRoot, live);

		var otherRoot = Element(w, 300, 300, 20, 20, 4);
		MakeMovable(w, otherRoot);
		var hidden = w.Entity().Set(new UiNode { Display = Display.None }).ID;
		w.AddChild(otherRoot, hidden);
		var ghost = Element(w, 0, 0, 100, 100, 9);
		w.AddChild(hidden, ghost);

		var hit = UiPick.Topmost(new Vector2(15, 15), Rendered(app), Parents(app), null, Nodes(app));
		Assert.Equal(live, hit.Entity);
	}

	[Fact]
	public void Pixel_hook_rejection_falls_through_to_the_element_behind()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var back = Element(w, 0, 0, 100, 100, 1);
		var front = Element(w, 0, 0, 100, 100, 5);
		var marked = new object();
		w.Set(front, new UiCustom { Data = marked });

		UiPixelHitTest hook = (ulong _, object? data, Vector2 pos, in ComputedNode bb)
			=> data != marked && bb.Contains(pos);
		var hit = UiPick.Topmost(new Vector2(50, 50), Rendered(app), hook);
		Assert.Equal(back, hit.Entity);
	}

	[Fact]
	public void Pixel_hook_owns_the_bounds_and_may_widen_them()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var pile = Element(w, 0, 0, 20, 20, 1);
		UiPixelHitTest widen = (ulong _, object? _, Vector2 pos, in ComputedNode bb)
			=> pos.X >= bb.Position.X && pos.Y >= bb.Position.Y
			&& pos.X < bb.Position.X + bb.Size.X + 5 && pos.Y < bb.Position.Y + bb.Size.Y + 5;
		Assert.Equal(pile, UiPick.Topmost(new Vector2(22, 22), Rendered(app), widen).Entity);
		Assert.False(UiPick.Topmost(new Vector2(22, 22), Rendered(app)).Found);
	}

	[Fact]
	public void ContainsByBounds_surface_skips_the_hook_and_needs_no_paint()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var frame = BareLayoutNode(w, 0, 0, 100, 100, 2);
		w.Set(frame, new UiContainsByBounds());
		var bounds = Fetch(new Query<Data<UiContainsByBounds>>(), app);
		UiPixelHitTest rejectAll = (ulong _, object? _, Vector2 _, in ComputedNode _) => false;
		var hit = UiPick.Topmost(new Vector2(50, 50), Rendered(app), Parents(app), bounds, null, rejectAll);
		Assert.Equal(frame, hit.Entity);
	}

	// A float (Absolute) inside a scroll viewport is drawn unclipped unless it
	// opts into ClipToParent: the pick must agree with the renderer.
	[Fact]
	public void Absolute_float_escapes_ancestor_clip_unless_ClipToParent()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var viewport = w.Entity()
			.Set(new UiNode { Overflow = Overflow.Scroll })
			.Set(new UiCustom { Data = null })
			.Set(new ComputedNode { Position = new Vector2(0, 0), Size = new Vector2(100, 50), PaintOrder = 1 }).ID;
		var popup = w.Entity()
			.Set(new UiNode { PositionType = PositionType.Absolute })
			.Set(new UiCustom { Data = null })
			.Set(new ComputedNode { Position = new Vector2(0, 0), Size = new Vector2(100, 200), PaintOrder = 2 }).ID;
		w.AddChild(viewport, popup);

		var below = new Vector2(50, 120);
		Assert.Equal(popup, UiPick.Topmost(below, Rendered(app), Parents(app), null, Nodes(app), null, ClipToParents(app)).Entity);
		// Without the ClipToParent query every ancestor clips (conservative).
		Assert.NotEqual(popup, UiPick.Topmost(below, Rendered(app), Parents(app), null, Nodes(app)).Entity);

		w.Set(popup, new ClipToParent());
		Assert.NotEqual(popup, UiPick.Topmost(below, Rendered(app), Parents(app), null, Nodes(app), null, ClipToParents(app)).Entity);
	}

	[Fact]
	public void TopmostWith_and_Hits_resolve_through_the_pick()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var widget = Element(w, 0, 0, 100, 100, 1);
		w.Set(widget, new Slot());
		var child = Element(w, 10, 10, 20, 20, 2);
		w.AddChild(widget, child);
		Element(w, 50, 50, 100, 100, 3); // cover over the widget's lower-right

		ulong onChild = 1, covered = 1;
		bool hitsChild = false, hitsCovered = true;
		app.AddSystem((UiGesturePick pick, Query<Data<Slot>> slots) =>
		{
			onChild = pick.TopmostWith(new Vector2(15, 15), slots);
			covered = pick.TopmostWith(new Vector2(60, 60), slots);
			hitsChild = pick.Hits(new Vector2(15, 15), widget);
			hitsCovered = pick.Hits(new Vector2(60, 60), widget);
		}).InStage(BevyStage.Update).Build();
		app.Update();

		Assert.Equal(widget, onChild);
		Assert.Equal(0ul, covered);
		Assert.True(hitsChild);
		Assert.False(hitsCovered);
	}

}

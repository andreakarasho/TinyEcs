using System.Collections.Generic;
using System.Numerics;
using Clay;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using Xunit;
using ClayColor = Clay.Color;
using UiNode = TinyEcs.Bevy.UI.Node;
using BevyStage = TinyEcs.Bevy.Stage;

namespace TinyEcs.Tests;

// The relayout gate: LayoutSystem re-solves the Clay tree only when a layout
// INPUT changed since its previous run. Change TICKS are the signal, so an
// in-place `.Ref` write is invisible unless the producer marks it — that
// contract is what these tests pin down, together with the structural
// (component/entity removal, reparenting) escape hatches.
[Collection("ClayUi")]
public class UiRelayoutGateTests
{
	private static App MakeApp(Vector2? size = null)
	{
		var world = new World();
		var app = new App(world, ThreadingMode.Single);
		app.AddPlugin(new UiPlugin { LogicalSize = size ?? new Vector2(800, 600) });
		return app;
	}

	// Runs frames until the gate closes (the ComputedNode feedback loop needs a
	// couple of passes to reach its fixpoint) and returns the settled context.
	private static UiClayContext Settle(App app, int frames = 3)
	{
		for (var i = 0; i < frames; i++)
			app.Update();

		var ctx = app.GetResource<UiClayContext>();
		var gen = ctx.LayoutGeneration;
		app.Update();
		Assert.Equal(gen, ctx.LayoutGeneration); // gate must be closed on a static tree
		return ctx;
	}

	private static ulong SpawnRoot(App app)
	{
		ulong id = 0;
		app.AddSystem((Commands c) =>
		{
			id = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(100), Height = Val.Px(50) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();
		return id;
	}

	[Fact]
	public void InPlace_node_write_without_SetChanged_is_skipped()
	{
		var app = MakeApp();
		SpawnRoot(app);
		var ctx = Settle(app);

		var mutate = true;
		app.AddSystem((Query<Data<UiNode>> q) =>
		{
			if (!mutate) return;
			mutate = false;
			foreach (var (_, n) in q)
				n.Ref.Width = Val.Px(180);
		})
		.InStage(BevyStage.Update).SingleThreaded().Build();

		var gen = ctx.LayoutGeneration;
		app.Update();

		// Documents the contract: a bare `.Ref` write bumps no change tick, so
		// the gate cannot see it. Producers must call SetChanged.
		Assert.Equal(gen, ctx.LayoutGeneration);
	}

	[Fact]
	public void InPlace_node_write_with_SetChanged_relayouts()
	{
		var app = MakeApp();
		SpawnRoot(app);
		var ctx = Settle(app);

		var mutate = true;
		app.AddSystem((Query<Data<UiNode>> q) =>
		{
			if (!mutate) return;
			mutate = false;
			foreach (var (e, n) in q)
			{
				n.Ref.Width = Val.Px(180);
				q.SetChanged<UiNode>(e.Ref);
			}
		})
		.InStage(BevyStage.Update).SingleThreaded().Build();

		var gen = ctx.LayoutGeneration;
		app.Update();

		Assert.NotEqual(gen, ctx.LayoutGeneration);

		// ...and the new width reached the render commands the same frame the
		// write happened (no one-frame lag).
		var cmds = app.GetResource<UiRenderCommands>().Span;
		var found = false;
		for (var i = 0; i < cmds.Length; i++)
			if (cmds[i].CommandType == RenderCommandType.Rectangle && cmds[i].BoundingBox.Width == 180f)
				found = true;
		Assert.True(found, "relayout ran but the 180px width never reached the commands");
	}

	[Fact]
	public void Despawn_of_ui_entity_relayouts()
	{
		var app = MakeApp();
		ulong childId = 0;
		app.AddSystem((Commands c) =>
		{
			var root = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.White));
			var child = c.Spawn()
				.Insert(new UiNode { Width = Val.Px(40), Height = Val.Px(20) })
				.Insert(new BackgroundColor(ClayColor.Red));
			c.AddChild(root, child);
			childId = child.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		var ctx = Settle(app);

		var despawn = true;
		app.AddSystem((Commands c) =>
		{
			if (!despawn) return;
			despawn = false;
			c.Entity(childId).Despawn();
		})
		.InStage(BevyStage.Update).SingleThreaded().Build();

		var gen = ctx.LayoutGeneration;
		app.Update();

		// No tick survives a removed component — OnRemove<Node> forces the pass.
		Assert.NotEqual(gen, ctx.LayoutGeneration);
		var rects = 0;
		var cmds = app.GetResource<UiRenderCommands>().Span;
		for (var i = 0; i < cmds.Length; i++)
			if (cmds[i].CommandType == RenderCommandType.Rectangle)
				rects++;
		Assert.Equal(1, rects);
	}

	[Fact]
	public void AddChild_to_existing_parent_relayouts()
	{
		var app = MakeApp();
		ulong rootId = 0, orphanId = 0;
		app.AddSystem((Commands c) =>
		{
			rootId = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Id;
			orphanId = c.Spawn()
				.Insert(new UiNode { Width = Val.Px(40), Height = Val.Px(20) })
				.Insert(new BackgroundColor(ClayColor.Red))
				.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		var ctx = Settle(app);

		var attach = true;
		app.AddSystem((Commands c) =>
		{
			if (!attach) return;
			attach = false;
			c.AddChild(rootId, orphanId);
		})
		.InStage(BevyStage.Update).SingleThreaded().Build();

		var gen = ctx.LayoutGeneration;
		app.Update();

		// Reparenting moves an existing Node without touching a single layout
		// value: OnInsert<Parent> is the only signal.
		Assert.NotEqual(gen, ctx.LayoutGeneration);
	}

	[Fact]
	public void ComputedNode_is_written_in_place_after_the_first_insert()
	{
		var app = MakeApp();
		var inserts = new List<ulong>();
		app.AddObserver<OnInsert<ComputedNode>>(t => inserts.Add(t.EntityId));

		app.AddSystem((Commands c) =>
		{
			var root = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.White));
			var child = c.Spawn()
				.Insert(new UiNode { Width = Val.Px(40), Height = Val.Px(20) })
				.Insert(new BackgroundColor(ClayColor.Red));
			c.AddChild(root, child);
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		for (var i = 0; i < 4; i++)
			app.Update();

		// Two painted elements → exactly two inserts, no matter how many frames
		// ran. Re-Inserting an unchanged ComputedNode every frame is what the
		// in-place writeback removes (and what used to fan out an observer
		// storm + a deferred command per element per frame).
		Assert.Equal(2, inserts.Count);
	}

	[Fact]
	public void Surface_resize_relayouts()
	{
		var app = MakeApp();
		SpawnRoot(app);
		var ctx = Settle(app);

		var gen = ctx.LayoutGeneration;
		app.GetResource<UiSurface>().LogicalSize = new Vector2(1024, 768);
		app.Update();

		// Surface size / UiScale are resources, not components: stored-last compare.
		Assert.NotEqual(gen, ctx.LayoutGeneration);
	}

	[Fact]
	public void Static_tree_keeps_skipping_and_reports_a_zero_dirty_mask()
	{
		var app = MakeApp();
		SpawnRoot(app);
		var profiler = app.GetResource<SystemProfiler>();
		profiler.Enabled = true;

		Settle(app);

		var skipped = profiler.LayoutSkipped;
		for (var i = 0; i < 5; i++)
			app.Update();

		Assert.Equal(skipped + 5, profiler.LayoutSkipped);

		// A marked Node write reopens the gate and shows up as probe bit 0.
		var mutate = true;
		app.AddSystem((Query<Data<UiNode>> q) =>
		{
			if (!mutate) return;
			mutate = false;
			foreach (var (e, n) in q)
			{
				n.Ref.Height = Val.Px(70);
				q.SetChanged<UiNode>(e.Ref);
			}
		})
		.InStage(BevyStage.Update).SingleThreaded().Build();

		app.Update();
		Assert.Equal(skipped + 5, profiler.LayoutSkipped);   // it ran
		Assert.Equal(1, profiler.LayoutDirtyMask & 1);        // bit 0 = Node
	}

	[Fact]
	public void One_shot_SetChanged_relayouts_exactly_once()
	{
		var app = MakeApp();
		SpawnRoot(app);
		var profiler = app.GetResource<SystemProfiler>();
		profiler.Enabled = true;
		var ctx = Settle(app);

		var mutate = true;
		app.AddSystem((Query<Data<UiNode>> q) =>
		{
			if (!mutate) return;
			mutate = false;
			foreach (var (e, n) in q)
			{
				n.Ref.Width = Val.Px(180);
				q.SetChanged<UiNode>(e.Ref);
			}
		})
		.InStage(BevyStage.Update).SingleThreaded().Build();

		var gen = ctx.LayoutGeneration;

		// Frame 1: the marked write opens the gate through probe bit 0 (Node),
		// on the SAME frame the write happened.
		app.Update();
		var afterWrite = ctx.LayoutGeneration;
		Assert.NotEqual(gen, afterWrite);
		Assert.Equal(1, profiler.LayoutDirtyMask & 1);

		// Frame 2 still relayouts, but ONLY through the ForceRelayout escape
		// hatch: the geometry moved, so the ComputedNode writeback demands one
		// more pass to settle the Right/Bottom anchoring feedback loop. The
		// payoff is bit 0 being CLEAR — the Node change is not reported a second
		// time. Under the old DetectCurrentTickChanges hack it was, so a marked
		// write cost a third pass on top of this one.
		app.Update();
		Assert.NotEqual(afterWrite, ctx.LayoutGeneration);
		Assert.Equal(0, profiler.LayoutDirtyMask & 1);
		Assert.NotEqual(0, profiler.LayoutDirtyMask & (1 << UiLayoutChanged.ForceBit));

		// ...and then it is done: no third pass, no trickle.
		var settled = ctx.LayoutGeneration;
		var skipped = profiler.LayoutSkipped;
		for (var i = 0; i < 3; i++)
			app.Update();

		Assert.Equal(settled, ctx.LayoutGeneration);
		Assert.Equal(skipped + 3, profiler.LayoutSkipped);
	}

	[Fact]
	public void Interaction_reset_does_not_reopen_the_gate()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(100), Height = Val.Px(50) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction());
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.GetResource<UiPointer>().Position = new Vector2(20, 20);
		var ctx = Settle(app);

		// Hovering, then leaving, rewrites Interaction on the element — a
		// non-layout component, and the write is neq-guarded either way.
		var gen = ctx.LayoutGeneration;
		app.GetResource<UiPointer>().Position = new Vector2(500, 500);
		app.Update();
		app.Update();

		Assert.Equal(gen, ctx.LayoutGeneration);
		var hovered = 0;
		foreach (var (_, it) in app.GetWorld().Query<Data<Interaction>>())
			if (it.Ref != Interaction.None)
				hovered++;
		Assert.Equal(0, hovered);
	}
}

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

// Per-root layout cache: a relayout re-walks only the roots that hold a changed
// entity and replays the others' recorded declarations. The output must equal a
// full re-walk, and a node that leaves a root (despawn, reparent) must leave its
// recorded stream too.
[Collection("ClayUi")]
public class UiLayoutCacheTests
{
	private sealed class Tree
	{
		public ulong A, AChild, B, BChild;
	}

	private static (App App, Tree T) Build()
	{
		var app = new App(new World(), ThreadingMode.Single);
		app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600) });
		app.GetResource<SystemProfiler>().Enabled = true;
		var t = new Tree();
		app.AddSystem((Commands c) =>
		{
			EntityCommands Root(float left) => c.Spawn()
				.Insert(new UiNode { PositionType = PositionType.Absolute, Left = Val.Px(left), Top = Val.Px(10), Width = Val.Px(200), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.White));
			EntityCommands Child(float w) => c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(w), Height = Val.Px(20) })
				.Insert(new BackgroundColor(ClayColor.White));
			var a = Root(10);
			var ac = Child(50);
			var b = Root(300);
			var bc = Child(60);
			a.AddChild(ac);
			b.AddChild(bc);
			(t.A, t.AChild, t.B, t.BChild) = (a.Id, ac.Id, b.Id, bc.Id);
		}).InStage(BevyStage.Startup).SingleThreaded().Build();
		for (var i = 0; i < 8; i++)
			app.Update();
		return (app, t);
	}

	private static Dictionary<uint, BoundingBox> Snapshot(App app)
	{
		var map = new Dictionary<uint, BoundingBox>();
		foreach (var cmd in app.GetResource<UiClayContext>().LastCommands)
			map[cmd.Id] = cmd.BoundingBox;
		return map;
	}

	private static void Run(App app, System.Action<Commands, Query<Data<UiNode>>> once)
	{
		var done = false;
		app.AddSystem((Commands c, Query<Data<UiNode>> q) =>
		{
			if (done) return;
			done = true;
			once(c, q);
		}).InStage(BevyStage.Update).SingleThreaded().Build();
		app.Update();
	}

	[Fact]
	public void Change_in_one_root_rewalks_it_and_replays_the_other()
	{
		var (app, t) = Build();
		var prof = app.GetResource<SystemProfiler>();

		Run(app, (_, q) =>
		{
			var (_, n) = q.Get(t.AChild);
			n.Ref.Width = Val.Px(120);
			q.SetChanged<UiNode>(t.AChild);
		});

		Assert.Equal(2, prof.LayoutRoots);
		Assert.Equal(1, prof.LayoutReplayed);
		var ctx = app.GetResource<UiClayContext>();
		Assert.True(ctx.TryGetElementBoundingBox(t.AChild, out var box));
		Assert.Equal(120f, box.Width);

		// The replayed output equals a full re-walk.
		var cached = Snapshot(app);
		ctx.MarkLayoutDirty();
		app.Update();
		Assert.Equal(0, prof.LayoutReplayed);
		Assert.Equal(cached, Snapshot(app));
	}

	[Fact]
	public void Despawned_child_leaves_its_root_stream()
	{
		var (app, t) = Build();
		var prof = app.GetResource<SystemProfiler>();

		Run(app, (c, _) => c.Entity(t.AChild).Despawn());

		Assert.Equal(1, prof.LayoutReplayed);
		var emitted = Snapshot(app);
		Assert.False(emitted.ContainsKey(ElementId.HashNumber((uint)t.AChild).Id));
		Assert.True(emitted.ContainsKey(ElementId.HashNumber((uint)t.BChild).Id));
	}

	[Fact]
	public void Child_moved_to_another_root_is_declared_once()
	{
		var (app, t) = Build();

		// A stale replay of B would declare BChild a second time under A: Clay
		// rejects a duplicate element id.
		Run(app, (c, _) => c.AddChild(t.A, t.BChild));
		app.Update();

		var ctx = app.GetResource<UiClayContext>();
		Assert.True(ctx.TryGetElementBoundingBox(t.BChild, out var box));
		Assert.True(ctx.TryGetElementBoundingBox(t.A, out var a));
		Assert.InRange(box.X, a.X, a.X + a.Width);
	}
}

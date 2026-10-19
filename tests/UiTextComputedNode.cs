using System.Numerics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using Xunit;
using ClayColor = Clay.Color;
using UiNode = TinyEcs.Bevy.UI.Node;
using BevyStage = TinyEcs.Bevy.Stage;

namespace TinyEcs.Tests;

// A node that only paints text emits a Text command under Clay's own child id.
// It must still get a ComputedNode (the element's box, ranked by the text's
// paint order), or hit tests can't see a plain label.
[Collection("ClayUi")]
public class UiTextComputedNodeTests
{
	private static (App App, ulong Root, ulong Label, ulong Other) Build()
	{
		var app = new App(new World(), ThreadingMode.Single);
		app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600) });
		ulong root = 0, label = 0, other = 0;
		app.AddSystem((Commands c) =>
		{
			var r = c.Spawn()
				.Insert(new UiNode { PositionType = PositionType.Absolute, Left = Val.Px(10), Top = Val.Px(10), Width = Val.Px(200), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.White));
			var l = c.Spawn()
				.Insert(new UiNode { Width = Val.Px(120), Height = Val.Px(20) })
				.Insert(new Text("label"));
			r.AddChild(l);
			var o = c.Spawn()
				.Insert(new UiNode { PositionType = PositionType.Absolute, Left = Val.Px(400), Top = Val.Px(10), Width = Val.Px(50), Height = Val.Px(50) })
				.Insert(new BackgroundColor(ClayColor.White));
			(root, label, other) = (r.Id, l.Id, o.Id);
		}).InStage(BevyStage.Startup).SingleThreaded().Build();
		for (var i = 0; i < 8; i++)
			app.Update();
		return (app, root, label, other);
	}

	[Fact]
	public void Text_only_node_gets_its_element_box_and_paints_above_its_parent()
	{
		var (app, root, label, _) = Build();
		var w = app.GetWorld();

		Assert.True(w.Has<ComputedNode>(label));
		var cn = w.Get<ComputedNode>(label);
		Assert.Equal(new Vector2(120, 20), cn.Size);
		Assert.Equal(new Vector2(10, 10), cn.Position);
		Assert.True(cn.PaintOrder > w.Get<ComputedNode>(root).PaintOrder);
	}

	// Another root's change replays this root from the layout cache; the text
	// must still be credited to its node (the box follows a later move).
	[Fact]
	public void Text_only_node_tracks_moves_after_a_cached_replay()
	{
		var (app, root, label, other) = Build();
		var w = app.GetWorld();

		w.Get<BackgroundColor>(other) = new BackgroundColor(ClayColor.Black);
		w.SetChanged<BackgroundColor>(other);
		app.Update();
		app.Update();

		w.Get<UiNode>(root).Left = Val.Px(30);
		w.SetChanged<UiNode>(root);
		for (var i = 0; i < 4; i++)
			app.Update();

		Assert.Equal(new Vector2(30, 10), w.Get<ComputedNode>(label).Position);
	}

	// A ClipToParent float emits a ScissorStart whose rect is the parent's clip
	// area. Its ComputedNode must be its own box, not that whole viewport.
	[Fact]
	public void Clip_to_parent_float_gets_its_own_box_not_the_clip_rect()
	{
		var app = new App(new World(), ThreadingMode.Single);
		app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600) });
		ulong floatId = 0;
		app.AddSystem((Commands c) =>
		{
			var viewport = c.Spawn()
				.Insert(new UiNode { PositionType = PositionType.Absolute, Left = Val.Px(10), Top = Val.Px(10), Width = Val.Px(300), Height = Val.Px(200), Overflow = Overflow.Scroll })
				.Insert(new ScrollPosition())
				.Insert(new BackgroundColor(ClayColor.White));
			var f = c.Spawn()
				.Insert(new UiNode { PositionType = PositionType.Absolute, Left = Val.Px(20), Top = Val.Px(30), Width = Val.Px(50), Height = Val.Px(16) })
				.Insert<ClipToParent>();
			var label = c.Spawn()
				.Insert(new UiNode { Width = Val.Px(30), Height = Val.Px(10) })
				.Insert(new Text("x"));
			f.AddChild(label);
			viewport.AddChild(f);
			floatId = f.Id;
		}).InStage(BevyStage.Startup).SingleThreaded().Build();
		for (var i = 0; i < 8; i++)
			app.Update();

		var cn = app.GetWorld().Get<ComputedNode>(floatId);
		Assert.Equal(new Vector2(50, 16), cn.Size);
	}
}

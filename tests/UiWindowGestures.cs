using System.Numerics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Input;
using TinyEcs.Bevy.UI;
using Xunit;
using UiNode = TinyEcs.Bevy.UI.Node;

namespace TinyEcs.Tests;

// Headless fixture for the pick-driven window gestures: windows are hand-built
// ComputedNodes with a null UiCustom (a solid pick surface, no pixel hook), the
// raw MouseInput is fed one snapshot per frame, and the real schedule runs.
internal sealed class GestureRig
{
	public readonly App App;
	public readonly MouseInput Mouse;
	private float _time;

	public GestureRig(params IPlugin[] plugins)
	{
		App = new App(ThreadingMode.Single);
		Mouse = new MouseInput();
		App.AddResource(Mouse);
		App.AddResource(new UiClayContext());
		App.AddPlugin(new UiWindowPlugin());
		foreach (var p in plugins) App.AddPlugin(p);
	}

	public World World => App.GetWorld();

	public void Step(float x, float y, MouseButtons down = MouseButtons.None)
	{
		Mouse.SetSnapshot(new Vector2(x, y), down);
		Mouse.Update(_time += 16f);
		App.Update();
	}

	public void Press(float x, float y, MouseButtons button = MouseButtons.Left)
	{
		Step(x, y);
		Step(x, y, button);
	}

	public ulong Window(float left, float top, float width, float height, int paintOrder = 1, int z = 0)
	{
		var id = Element(left, top, width, height, paintOrder);
		World.Set<UiMovable>(id);
		World.Set(id, new GlobalZIndex(z));
		return id;
	}

	public ulong Element(float left, float top, float width, float height, int paintOrder = 1)
		=> World.Entity()
			.Set(new UiNode { PositionType = PositionType.Absolute, Left = Val.Px(left), Top = Val.Px(top), Width = Val.Px(width), Height = Val.Px(height) })
			.Set(new UiCustom { Data = null })
			.Set(new ComputedNode
			{
				Position = new Vector2(left, top),
				Size = new Vector2(width, height),
				PaintOrder = paintOrder,
			}).ID;

	public bool Alive(ulong id) => World.Exists(id);
}

public class UiWindowGestureTests
{
	[Fact]
	public void Held_left_drag_moves_the_window_by_the_cursor_delta()
	{
		var rig = new GestureRig();
		var win = rig.Window(100, 50, 80, 80);

		rig.Press(140, 90);
		rig.Step(160, 110, MouseButtons.Left);

		var node = rig.World.Get<UiNode>(win);
		Assert.Equal(120f, node.Left.Value);
		Assert.Equal(70f, node.Top.Value);

		rig.Step(160, 110);
		rig.Step(300, 300);
		Assert.Equal(120f, rig.World.Get<UiNode>(win).Left.Value);
	}

	[Fact]
	public void Drag_does_not_start_on_a_press_outside_the_window()
	{
		var rig = new GestureRig();
		var win = rig.Window(100, 50, 80, 80);

		rig.Press(10, 10);
		rig.Step(140, 90, MouseButtons.Left);
		rig.Step(160, 110, MouseButtons.Left);

		Assert.Equal(100f, rig.World.Get<UiNode>(win).Left.Value);
	}

	[Fact]
	public void A_press_on_a_child_drags_its_window_root()
	{
		var rig = new GestureRig();
		var win = rig.Window(100, 50, 80, 80, paintOrder: 1);
		var child = rig.Element(110, 60, 20, 20, paintOrder: 2);
		rig.World.AddChild(win, child);

		rig.Press(115, 65);
		rig.Step(135, 75, MouseButtons.Left);

		Assert.Equal(120f, rig.World.Get<UiNode>(win).Left.Value);
	}

	[Fact]
	public void Focusing_a_window_bumps_its_z_to_the_front()
	{
		var rig = new GestureRig();
		var win = rig.Window(100, 50, 80, 80);

		rig.Press(140, 90);

		Assert.True(rig.World.Get<GlobalZIndex>(win).Value > 0);
	}

	[Fact]
	public void A_drag_yield_hit_focuses_but_does_not_drag()
	{
		var rig = new GestureRig();
		var win = rig.Window(100, 50, 80, 80, paintOrder: 1);
		var item = rig.Element(110, 60, 20, 20, paintOrder: 2);
		rig.World.AddChild(win, item);
		rig.World.Set<UiDragYield>(item);

		rig.Press(115, 65);
		rig.Step(135, 85, MouseButtons.Left);

		Assert.Equal(100f, rig.World.Get<UiNode>(win).Left.Value);
		Assert.True(rig.World.Get<GlobalZIndex>(win).Value > 0);
		Assert.True(rig.App.GetResource<UiGestureGate>().IsFree);
	}

	[Fact]
	public void No_drag_window_focuses_but_stays_put()
	{
		var rig = new GestureRig();
		var win = rig.Window(100, 50, 80, 80);
		rig.World.Set<UiMovableNoDrag>(win);

		rig.Press(140, 90);
		rig.Step(200, 150, MouseButtons.Left);

		Assert.Equal(100f, rig.World.Get<UiNode>(win).Left.Value);
	}

	[Fact]
	public void Drag_modifier_gates_the_latch()
	{
		var rig = new GestureRig();
		var win = rig.Window(100, 50, 80, 80);
		var mods = rig.App.GetResource<UiWindowModifiers>();
		mods.DragRequiresModifier = true;

		rig.Press(140, 90);
		rig.Step(160, 110, MouseButtons.Left);
		Assert.Equal(100f, rig.World.Get<UiNode>(win).Left.Value);
		rig.Step(160, 110);

		mods.DragModifierHeld = true;
		rig.Press(160, 110);
		rig.Step(180, 130, MouseButtons.Left);
		Assert.Equal(120f, rig.World.Get<UiNode>(win).Left.Value);
	}

	[Fact]
	public void A_suppressed_drag_never_latches()
	{
		var rig = new GestureRig();
		var win = rig.Window(100, 50, 80, 80);
		rig.App.GetResource<UiWindowModifiers>().DragSuppressed = true;

		rig.Press(140, 90);
		rig.Step(160, 110, MouseButtons.Left);

		Assert.Equal(100f, rig.World.Get<UiNode>(win).Left.Value);
	}

	[Fact]
	public void Window_drag_claims_the_gesture_gate_and_yields_when_taken()
	{
		var rig = new GestureRig();
		var win = rig.Window(100, 100, 80, 80);
		var gate = rig.App.GetResource<UiGestureGate>();

		rig.Press(110, 110);
		Assert.Equal(UiGestureMode.Window, gate.Mode);
		rig.Step(130, 110, MouseButtons.Left);
		Assert.Equal(120f, rig.World.Get<UiNode>(win).Left.Value);
		rig.Step(130, 110);
		Assert.True(gate.IsFree);

		// Another gesture owns the press: the window must not latch.
		Assert.True(gate.TryClaim(UiGestureMode.Resize));
		rig.Step(130, 110, MouseButtons.Left);
		rig.Step(180, 110, MouseButtons.Left);
		Assert.Equal(120f, rig.World.Get<UiNode>(win).Left.Value);
		rig.Step(180, 110);
		Assert.Equal(UiGestureMode.Resize, gate.Mode);
	}

	[Fact]
	public void Active_drag_publishes_the_whole_pixel_frame_delta_and_the_release()
	{
		var rig = new GestureRig();
		var win = rig.Window(100, 50, 80, 80);
		var active = rig.App.GetResource<ActiveWindowDrag>();

		rig.Press(140, 90);
		rig.Step(150.4f, 95.6f, MouseButtons.Left);
		Assert.Equal(win, active.Owner);
		Assert.Equal(new Vector2(10, 6), active.FrameDelta);

		rig.Step(150, 96);
		Assert.Equal(0UL, active.Owner);
		Assert.Equal(win, active.JustReleased);
	}

	[Fact]
	public void Clicking_a_window_declared_above_the_counter_lifts_it_to_the_front()
	{
		var rig = new GestureRig();
		var main = rig.Window(100, 50, 80, 80, z: 60);
		var aux = rig.Window(300, 50, 80, 80, z: 61);

		rig.Press(120, 70);
		var mainZ = rig.World.Get<GlobalZIndex>(main).Value;
		Assert.True(mainZ > 61, $"main window z {mainZ} should be above the 61 floor");

		rig.Step(120, 70);
		rig.Press(320, 70);
		var auxZ = rig.World.Get<GlobalZIndex>(aux).Value;
		Assert.True(auxZ > mainZ, $"aux z {auxZ} should be above main z {mainZ}");
		Assert.True(rig.World.Get<GlobalZIndex>(main).Value >= 60);
	}

	[Fact]
	public void Forced_drag_recenters_under_the_cursor_and_does_not_demote()
	{
		var rig = new GestureRig();
		var win = rig.Window(100, 100, 200, 100, z: 60);
		rig.Press(300, 300);

		rig.App.GetResource<ForcedWindowDrag>().Owner = win;
		rig.Step(300, 300, MouseButtons.Left);
		var node = rig.World.Get<UiNode>(win);
		Assert.Equal(200f, node.Left.Value);  // 300 - 200/2
		Assert.Equal(250f, node.Top.Value);   // 300 - 100/2
		Assert.True(rig.World.Get<GlobalZIndex>(win).Value >= 60);

		rig.Step(320, 310, MouseButtons.Left);
		Assert.Equal(220f, rig.World.Get<UiNode>(win).Left.Value);
	}

	[Fact]
	public void Right_click_press_then_release_over_a_window_closes_it()
	{
		var rig = new GestureRig();
		var win = rig.Window(100, 50, 80, 80);

		rig.Press(140, 90, MouseButtons.Right);
		Assert.True(rig.Alive(win));

		rig.Step(140, 90);
		Assert.False(rig.Alive(win));
	}

	[Fact]
	public void Right_click_released_after_dragging_off_does_not_close()
	{
		var rig = new GestureRig();
		var win = rig.Window(100, 50, 80, 80);

		rig.Press(140, 90, MouseButtons.Right);
		rig.Step(10, 10);

		Assert.True(rig.Alive(win));
	}

	[Fact]
	public void The_right_press_over_a_window_is_consumed_for_later_systems()
	{
		var rig = new GestureRig();
		rig.Window(100, 50, 80, 80);
		var seen = 0;
		rig.App.AddSystem((Res<MouseInput> m) => { if (m.Value.IsPressedOnce(MouseButton.Right) || m.Value.IsPressed(MouseButton.Right)) seen++; })
			.InStage(Stage.Update).Build();

		rig.Press(140, 90, MouseButtons.Right);
		rig.Step(140, 90, MouseButtons.Right);
		Assert.Equal(0, seen);

		rig.Step(10, 10);
		rig.Press(10, 10, MouseButtons.Right);
		Assert.Equal(1, seen); // not over a window: the world gets it
	}

	[Fact]
	public void No_right_click_close_keeps_the_window_and_leaves_the_edge_unconsumed()
	{
		var rig = new GestureRig();
		var win = rig.Window(100, 50, 80, 80);
		rig.World.Set<UiNoRightClickClose>(win);
		var pressEdges = 0;
		rig.App.AddSystem((Res<MouseInput> m) => { if (m.Value.IsPressedOnce(MouseButton.Right)) pressEdges++; })
			.InStage(Stage.Update).Build();

		rig.Press(140, 90, MouseButtons.Right);
		rig.Step(140, 90);

		Assert.True(rig.Alive(win));
		Assert.Equal(1, pressEdges);
	}

	[Fact]
	public void Manual_close_windows_get_the_request_but_are_not_despawned()
	{
		var rig = new GestureRig();
		var win = rig.Window(100, 50, 80, 80);
		rig.World.Set<UiManualClose>(win);
		var requests = 0;
		rig.App.AddObserver<On<UiCloseRequested>>(t => { if (t.EntityId == win) requests++; });

		rig.Press(140, 90, MouseButtons.Right);
		rig.Step(140, 90);

		Assert.Equal(1, requests);
		Assert.True(rig.Alive(win));
	}

	[Fact]
	public void Anchored_close_follows_the_modifier_and_group_settings()
	{
		var rig = new GestureRig();
		var a = rig.Window(100, 50, 80, 80);
		var b = rig.Window(180, 50, 80, 80);
		var other = rig.Window(400, 50, 80, 80);
		rig.World.Set(a, new UiAnchorMember { GroupId = 7 });
		rig.World.Set(b, new UiAnchorMember { GroupId = 7 });
		var mods = rig.App.GetResource<UiWindowModifiers>();
		mods.CloseAnchoredRequiresModifier = true;
		mods.CloseAnchoredGroup = true;

		rig.Press(140, 90, MouseButtons.Right);
		rig.Step(140, 90);
		Assert.True(rig.Alive(a)); // modifier not held

		mods.CloseAnchoredModifierHeld = true;
		rig.Press(140, 90, MouseButtons.Right);
		rig.Step(140, 90);
		Assert.False(rig.Alive(a));
		Assert.False(rig.Alive(b));
		Assert.True(rig.Alive(other));
	}

	[Fact]
	public void A_press_outside_a_popup_dismisses_it_and_a_press_inside_does_not()
	{
		var rig = new GestureRig();
		var popup = rig.Element(100, 100, 50, 50, paintOrder: 5);
		rig.World.Set<UiPopup>(popup);

		rig.Press(120, 120);
		Assert.True(rig.Alive(popup));

		rig.Step(10, 10);
		rig.Press(10, 10);
		Assert.False(rig.Alive(popup));
	}

	[Fact]
	public void Any_right_press_dismisses_a_popup()
	{
		var rig = new GestureRig();
		var popup = rig.Element(100, 100, 50, 50, paintOrder: 5);
		rig.World.Set<UiPopup>(popup);

		rig.Press(120, 120, MouseButtons.Right);
		Assert.False(rig.Alive(popup));
	}
}

public class UiResizableTests
{
	[Fact]
	public void Dragging_the_corner_grip_resizes_within_the_bounds()
	{
		var rig = new GestureRig(new UiResizablePlugin());
		var win = rig.Window(100, 100, 200, 150);
		rig.World.Set(win, new UiResizable { MinW = 120, MinH = 100, MaxW = 260 });

		// Grip = 10px square at the bottom-right corner (295,245).
		rig.Press(295, 245);
		Assert.Equal(UiGestureMode.Window, rig.App.GetResource<UiGestureGate>().Mode);
		rig.Step(395, 225, MouseButtons.Left);

		var node = rig.World.Get<UiNode>(win);
		Assert.Equal(260f, node.Width.Value);  // clamped to MaxW
		Assert.Equal(130f, node.Height.Value);
		Assert.Equal(100f, node.Left.Value);   // the resize, not a window drag

		rig.Step(395, 225);
		Assert.True(rig.App.GetResource<UiGestureGate>().IsFree);
	}

	[Fact]
	public void A_press_away_from_the_grip_drags_instead()
	{
		var rig = new GestureRig(new UiResizablePlugin());
		var win = rig.Window(100, 100, 200, 150);
		rig.World.Set(win, new UiResizable());

		rig.Press(150, 150);
		rig.Step(170, 150, MouseButtons.Left);

		var node = rig.World.Get<UiNode>(win);
		Assert.Equal(200f, node.Width.Value);
		Assert.Equal(120f, node.Left.Value);
	}
}

public class UiNodeOpsTests
{
	[Fact]
	public void Writes_mark_only_on_a_real_change()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var e = w.Entity().Set(new UiNode { Left = Val.Px(5), Top = Val.Px(6) }).ID;
		var changes = new List<int>();
		var frame = 0;
		app.AddSystem((Query<Data<UiNode>> q) =>
		{
			UiNodeOps.SetXY(q, e, frame >= 2 ? 7 : 5, 6);
		}).InStage(Stage.Update).Build();
		app.AddSystem((Query<Data<UiNode>, Filter<Changed<UiNode>>> q) => { foreach (var _ in q) changes.Add(frame); })
			.InStage(Stage.PostUpdate).Build();

		for (frame = 0; frame < 4; frame++) app.Update();

		Assert.Contains(2, changes);
		Assert.DoesNotContain(1, changes); // a no-op write never marks
		Assert.DoesNotContain(3, changes);
		Assert.Equal(7f, w.Get<UiNode>(e).Left.Value);
	}

	[Fact]
	public void Hidden_walks_display_none_ancestors()
	{
		var app = new App(ThreadingMode.Single);
		var w = app.GetWorld();
		var root = w.Entity().Set(new UiNode { Display = Display.None }).ID;
		var child = w.Entity().Set(new UiNode()).ID;
		w.AddChild(root, child);
		var loose = w.Entity().Set(new UiNode()).ID;
		bool childHidden = false, looseHidden = true;
		app.AddSystem((Query<Data<TinyEcs.Parent>> parents, Query<Data<UiNode>> nodes) =>
		{
			childHidden = UiNodeOps.Hidden(child, parents, nodes);
			looseHidden = UiNodeOps.Hidden(loose, parents, nodes);
		}).InStage(Stage.Update).Build();
		app.Update();

		Assert.True(childHidden);
		Assert.False(looseHidden);
	}
}

public class UiPositionCacheTests
{
	[Fact]
	public void A_reinserted_key_restores_the_last_recorded_position()
	{
		var app = new App(ThreadingMode.Single);
		app.AddPlugin(new UiPositionCachePlugin());
		var w = app.GetWorld();
		var first = w.Entity().Set(new UiNode { Left = Val.Px(10), Top = Val.Px(20) }).Set(new UiPositionKey { Key = 42 }).ID;
		app.Update();
		ref var n = ref w.Get<UiNode>(first);
		n.Left = Val.Px(300);
		n.Top = Val.Px(200);
		app.Update();
		w.Delete(first);

		ulong second = 0;
		app.AddSystem((Commands c) =>
		{
			if (second != 0) return;
			second = c.Spawn().Insert(new UiNode { Left = Val.Px(0), Top = Val.Px(0) }).Insert(new UiPositionKey { Key = 42 }).Id;
		}).InStage(Stage.Update).Build();
		app.Update();

		var node = w.Get<UiNode>(second);
		Assert.Equal(300f, node.Left.Value);
		Assert.Equal(200f, node.Top.Value);
	}
}

public class UiAnchorTests
{
	private static (GestureRig rig, ulong a, ulong b) TwoAnchorables(int kindB = 1)
	{
		var rig = new GestureRig(new UiAnchorPlugin());
		var a = rig.Window(100, 100, 50, 50);
		var b = rig.Window(300, 100, 50, 50);
		rig.World.Set(a, new UiAnchorable { Kind = 1 });
		rig.World.Set(b, new UiAnchorable { Kind = kindB });
		return (rig, a, b);
	}

	// The headless rig has no layout pass, so the ComputedNode the snap reads is
	// moved alongside the Node by hand.
	private static void SyncBox(GestureRig rig, ulong id)
	{
		var n = rig.World.Get<UiNode>(id);
		ref var cn = ref rig.World.Get<ComputedNode>(id);
		cn.Position = new Vector2(n.Left.Value, n.Top.Value);
	}

	[Fact]
	public void Dropping_onto_a_same_kind_window_snaps_flush_and_groups_them()
	{
		var (rig, a, b) = TwoAnchorables();

		rig.Press(320, 120);              // grab b
		rig.Step(150, 120, MouseButtons.Left); // b now overlaps a's right half
		SyncBox(rig, b);
		rig.Step(150, 120);              // release: snap

		var node = rig.World.Get<UiNode>(b);
		Assert.Equal(150f, node.Left.Value); // flush to a's right edge
		Assert.Equal(100f, node.Top.Value);
		rig.Step(150, 120);
		Assert.True(rig.World.Has<UiAnchorMember>(a));
		Assert.Equal(rig.World.Get<UiAnchorMember>(a).GroupId, rig.World.Get<UiAnchorMember>(b).GroupId);
	}

	[Fact]
	public void Different_kinds_do_not_anchor()
	{
		var (rig, _, b) = TwoAnchorables(kindB: 2);

		rig.Press(320, 120);
		rig.Step(150, 120, MouseButtons.Left);
		SyncBox(rig, b);
		rig.Step(150, 120);
		rig.Step(150, 120);

		Assert.Equal(130f, rig.World.Get<UiNode>(b).Left.Value);
		Assert.False(rig.World.Has<UiAnchorMember>(b));
	}

	[Fact]
	public void Dragging_a_grouped_window_carries_the_group_unless_detaching()
	{
		var (rig, a, b) = TwoAnchorables();
		rig.World.Set(a, new UiAnchorMember { GroupId = 3 });
		rig.World.Set(b, new UiAnchorMember { GroupId = 3 });

		rig.Press(120, 120);
		rig.Step(130, 140, MouseButtons.Left);
		Assert.Equal(310f, rig.World.Get<UiNode>(b).Left.Value);
		Assert.Equal(120f, rig.World.Get<UiNode>(b).Top.Value);
		rig.Step(130, 140);

		rig.App.GetResource<UiWindowModifiers>().AnchorDetachHeld = true;
		rig.Press(130, 140);
		rig.Step(150, 140, MouseButtons.Left);
		rig.Step(150, 140, MouseButtons.Left);
		Assert.False(rig.World.Has<UiAnchorMember>(a));
		Assert.False(rig.World.Has<UiAnchorMember>(b)); // a 2-member group dissolves
		Assert.Equal(310f, rig.World.Get<UiNode>(b).Left.Value);
	}
}

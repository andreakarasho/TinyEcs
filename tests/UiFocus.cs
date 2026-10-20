using System.Numerics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Input;
using TinyEcs.Bevy.UI;
using Xunit;
using UiNode = TinyEcs.Bevy.UI.Node;
using BevyStage = TinyEcs.Bevy.Stage;

namespace TinyEcs.Tests;

[Collection("ClayUi")]
public class UiFocusTests
{
	private static App MakeApp()
	{
		var app = new App(new World(), ThreadingMode.Single);
		app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600) });
		app.AddPlugin(new UiFocusPlugin());
		return app;
	}

	private static ulong[] Spawn(App app, Action<Commands, List<ulong>> build)
	{
		var ids = new List<ulong>();
		app.AddSystem((Commands c) => build(c, ids)).InStage(BevyStage.Startup).SingleThreaded().Build();
		app.RunStartup();
		app.Update();
		return ids.ToArray();
	}

	private static void Press(App app, params KeyCode[] keys)
	{
		var kb = app.GetResource<KeyboardInput>();
		kb.SetSnapshot(keys); kb.Update(0f); app.Update();
		kb.SetSnapshot([]); kb.Update(0f); app.Update();
	}

	private static void PressShifted(App app, KeyCode key)
	{
		var kb = app.GetResource<KeyboardInput>();
		kb.SetSnapshot([KeyCode.LeftShift]); kb.Update(0f); app.Update();
		kb.SetSnapshot([KeyCode.LeftShift, key]); kb.Update(0f); app.Update();
		kb.SetSnapshot([]); kb.Update(0f); app.Update();
	}

	[Fact]
	public void Tab_walks_tab_index_order_and_wraps_with_shift_tab_back()
	{
		var app = MakeApp();
		var ids = Spawn(app, (c, ids) =>
		{
			var group = c.Spawn().Insert(new UiNode()).Insert(new TabGroup());
			var b = c.Spawn().Insert(new UiNode()).Insert(new TabIndex(2));
			var a = c.Spawn().Insert(new UiNode()).Insert(new TabIndex(1));
			group.AddChild(a); group.AddChild(b);
			ids.Add(a.Id); ids.Add(b.Id);
		});
		var focus = app.GetResource<InputFocus>();

		Press(app, KeyCode.Tab);
		Assert.Equal(ids[0], focus.Entity);
		Press(app, KeyCode.Tab);
		Assert.Equal(ids[1], focus.Entity);
		Press(app, KeyCode.Tab);
		Assert.Equal(ids[0], focus.Entity);
		PressShifted(app, KeyCode.Tab);
		Assert.Equal(ids[1], focus.Entity);
	}

	[Fact]
	public void Modal_group_keeps_tab_inside()
	{
		var app = MakeApp();
		var ids = Spawn(app, (c, ids) =>
		{
			var outer = c.Spawn().Insert(new UiNode()).Insert(new TabGroup { Order = 0 });
			var o1 = c.Spawn().Insert(new UiNode()).Insert(new TabIndex(0));
			outer.AddChild(o1);
			var modal = c.Spawn().Insert(new UiNode()).Insert(new TabGroup { Order = 1, Modal = true });
			var m1 = c.Spawn().Insert(new UiNode()).Insert(new TabIndex(0));
			var m2 = c.Spawn().Insert(new UiNode()).Insert(new TabIndex(1));
			modal.AddChild(m1); modal.AddChild(m2);
			ids.Add(o1.Id); ids.Add(m1.Id); ids.Add(m2.Id);
		});
		var focus = app.GetResource<InputFocus>();
		focus.Entity = ids[1];

		Press(app, KeyCode.Tab);
		Assert.Equal(ids[2], focus.Entity);
		Press(app, KeyCode.Tab);
		Assert.Equal(ids[1], focus.Entity);
	}

	[Fact]
	public void Hidden_and_negative_index_entities_are_skipped()
	{
		var app = MakeApp();
		var ids = Spawn(app, (c, ids) =>
		{
			var a = c.Spawn().Insert(new UiNode()).Insert(new TabIndex(0));
			var skip = c.Spawn().Insert(new UiNode()).Insert(new TabIndex(-1));
			var hidden = c.Spawn().Insert(new UiNode { Display = Display.None }).Insert(new TabIndex(1));
			var b = c.Spawn().Insert(new UiNode()).Insert(new TabIndex(2));
			ids.Add(a.Id); ids.Add(b.Id);
		});
		var focus = app.GetResource<InputFocus>();
		focus.Entity = ids[0];

		Press(app, KeyCode.Tab);
		Assert.Equal(ids[1], focus.Entity);
	}

	[Fact]
	public void AutoFocus_takes_focus_and_gained_lost_fire()
	{
		var app = MakeApp();
		var gained = new List<ulong>();
		var lost = new List<ulong>();
		app.AddObserver<On<FocusGained>>(t => gained.Add(t.EntityId));
		app.AddObserver<On<FocusLost>>(t => lost.Add(t.EntityId));
		var ids = Spawn(app, (c, ids) =>
		{
			ids.Add(c.Spawn().Insert(new UiNode()).Insert<AutoFocus>().Id);
			ids.Add(c.Spawn().Insert(new UiNode()).Id);
		});

		Assert.Equal(ids[0], app.GetResource<InputFocus>().Entity);
		Assert.Equal(new[] { ids[0] }, gained);

		app.GetResource<InputFocus>().Set(ids[1]);
		app.Update();
		Assert.Equal(new[] { ids[0] }, lost);
		Assert.Equal(new[] { ids[0], ids[1] }, gained);
	}

	[Fact]
	public void Focused_key_and_char_bubble_to_ancestors()
	{
		var app = MakeApp();
		var keys = new List<KeyCode>();
		var chars = new List<char>();
		var ids = Spawn(app, (c, ids) =>
		{
			var root = c.Spawn().Insert(new UiNode())
				.Observe((On<FocusedKey> t) => keys.Add(t.Event.Key))
				.Observe((On<FocusedChar> t) => chars.Add(t.Event.Value));
			var leaf = c.Spawn().Insert(new UiNode());
			root.AddChild(leaf);
			ids.Add(root.Id); ids.Add(leaf.Id);
		});
		app.GetResource<InputFocus>().Entity = ids[1];

		app.SendEvent(new CharInput { Value = 'q' });
		Press(app, KeyCode.Q);

		Assert.Equal(new[] { KeyCode.Q }, keys);
		Assert.Equal(new[] { 'q' }, chars);
	}

	[Fact]
	public void Passthrough_focus_consumes_nothing()
	{
		var app = MakeApp();
		var ids = Spawn(app, (c, ids) =>
			ids.Add(c.Spawn().Insert(new UiNode()).Insert<EditableText>().Insert<UiFocusPassthrough>().Id));
		app.GetResource<InputFocus>().Entity = ids[0];
		var seen = 0;
		app.AddSystem((Res<KeyboardInput> k) => { if (k.Value.IsPressedOnce(KeyCode.W)) seen++; })
			.InStage(BevyStage.Update).Build();

		Press(app, KeyCode.W);
		Assert.Equal(1, seen);
	}
}

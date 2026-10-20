using System.Numerics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Input;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;
using Xunit;
using ClayColor = Clay.Color;
using UiNode = TinyEcs.Bevy.UI.Node;
using BevyStage = TinyEcs.Bevy.Stage;

namespace TinyEcs.Tests;

// Real layout + the real schedule: pointer through UiPointer AND the raw
// MouseInput (the window / popup gestures read the latter), keys through
// KeyboardInput.
internal sealed class WidgetRig
{
	public readonly App App;
	public readonly MouseInput Mouse = new();
	public readonly KeyboardInput Keys = new();
	private readonly List<Action<Commands>> _pending = new();
	private float _time;

	public WidgetRig()
	{
		App = new App(new World(), ThreadingMode.Single);
		App.AddResource(Mouse);
		App.AddResource(Keys);
		App.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600) });
		App.AddPlugin(new UiWindowPlugin());
		App.AddPlugin(new UiWidgetsPlugin());
		App.AddSystem((Commands c) =>
		{
			foreach (var a in _pending) a(c);
			_pending.Clear();
		}).InStage(BevyStage.Update).SingleThreaded().Build();
	}

	/// <summary>Run commands in the next frame's Update, then step that frame.</summary>
	public void Run(Action<Commands> action)
	{
		_pending.Add(action);
		Frame();
	}

	public World World => App.GetWorld();
	public InputFocus Focus => App.GetResource<InputFocus>();

	public ulong Spawn(Func<Commands, ulong> build)
	{
		ulong id = 0;
		App.AddSystem((Commands c) => { id = build(c); }).InStage(BevyStage.Startup).SingleThreaded().Build();
		App.RunStartup();
		Frame();
		Frame();
		return id;
	}

	public void Frame(Vector2? at = null, bool down = false)
	{
		var pos = at ?? App.GetResource<UiPointer>().Position;
		Mouse.SetSnapshot(pos, down ? MouseButtons.Left : MouseButtons.None);
		Mouse.Update(_time += 16f);
		var p = App.GetResource<UiPointer>();
		p.Position = pos;
		p.Down = down;
		App.Update();
	}

	public void Click(float x, float y)
	{
		Frame(new Vector2(x, y));
		Frame(new Vector2(x, y), down: true);
		Frame(new Vector2(x, y));
		Frame(new Vector2(x, y));
	}

	public void Press(params KeyCode[] keys)
	{
		Keys.SetSnapshot(keys); Keys.Update(_time); Frame();
		Keys.SetSnapshot([]); Keys.Update(_time); Frame();
	}

	public static UiNode Box(float x, float y, float w = 100, float h = 30) => new()
	{
		Display = Display.Flex,
		PositionType = PositionType.Absolute,
		Left = Val.Px(x), Top = Val.Px(y),
		Width = Val.Px(w), Height = Val.Px(h),
	};

	public static UiNode Row(float h = 30) => new()
	{
		Display = Display.Flex,
		Width = Val.Px(100), Height = Val.Px(h),
	};

	public static EntityCommands Hit(EntityCommands e) => e.Insert(new BackgroundColor(ClayColor.White)).Insert(new Interaction());
}

[Collection("ClayUi")]
public class UiButtonWidgetTests
{
	[Fact]
	public void Click_on_a_button_or_its_label_activates_it()
	{
		var rig = new WidgetRig();
		var activated = new List<ulong>();
		rig.App.AddObserver<On<Activate>>(t => activated.Add(t.Event.Entity));
		var button = rig.Spawn(c =>
		{
			var b = WidgetRig.Hit(c.Spawn().Insert(WidgetRig.Box(10, 10, 100, 40))).Insert(new Button());
			var label = c.Spawn().Insert(new UiNode { Width = Val.Px(50), Height = Val.Px(20) })
				.Insert(new Text("ok")).Insert(new TextColor(ClayColor.Black));
			b.AddChild(label);
			return b.Id;
		});

		rig.Click(20, 20);
		Assert.Equal(new[] { button }, activated);
	}

	[Fact]
	public void Disabled_button_does_not_activate()
	{
		var rig = new WidgetRig();
		var count = 0;
		rig.App.AddObserver<On<Activate>>(_ => count++);
		var button = rig.Spawn(c => WidgetRig.Hit(c.Spawn().Insert(WidgetRig.Box(10, 10))).Insert(new Button()).Insert(new InteractionDisabled()).Id);
		rig.Click(20, 20);
		rig.Focus.Set(button);
		rig.Press(KeyCode.Enter);
		Assert.Equal(0, count);
	}

	[Fact]
	public void Focused_button_activates_on_enter_and_space_and_claims_the_key_from_later_readers()
	{
		var rig = new WidgetRig();
		var count = 0;
		var hotkeySaw = 0;
		rig.App.AddObserver<On<Activate>>(_ => count++);
		// A game key reader ordered after the focus dispatcher (the hotkey contract).
		rig.App.AddSystem((Res<KeyboardInput> kb) =>
			{
				if (kb.Value.IsPressedOnce(KeyCode.Enter) || kb.Value.IsPressedOnce(KeyCode.Space)) hotkeySaw++;
			})
			.InStage(BevyStage.First).After(UiFocusPlugin.DispatchLabel).Build();
		var button = rig.Spawn(c => WidgetRig.Hit(c.Spawn().Insert(WidgetRig.Box(10, 10))).Insert(new Button()).Id);

		rig.Press(KeyCode.Enter); // not focused: nothing, and the reader sees it
		Assert.Equal(0, count);
		Assert.Equal(1, hotkeySaw);

		rig.Focus.Set(button);
		rig.Press(KeyCode.Enter);
		rig.Press(KeyCode.Space);
		Assert.Equal(2, count);
		Assert.Equal(1, hotkeySaw);
	}

	[Fact]
	public void Activate_bubbles_to_ancestors()
	{
		var rig = new WidgetRig();
		var seenOnPanel = 0;
		rig.Spawn(c =>
		{
			var panel = c.Spawn().Insert(WidgetRig.Box(0, 0, 300, 300))
				.Observe((On<Activate> _) => seenOnPanel++);
			var b = WidgetRig.Hit(c.Spawn().Insert(WidgetRig.Row())).Insert(new Button());
			panel.AddChild(b);
			return b.Id;
		});
		rig.Click(10, 10);
		Assert.Equal(1, seenOnPanel);
	}
}

[Collection("ClayUi")]
public class UiCheckboxRadioWidgetTests
{
	[Fact]
	public void Controlled_checkbox_reports_but_does_not_change()
	{
		var rig = new WidgetRig();
		var values = new List<bool>();
		rig.App.AddObserver<On<ValueChange<bool>>>(t => values.Add(t.Event.Value));
		var box = rig.Spawn(c => WidgetRig.Hit(c.Spawn().Insert(WidgetRig.Box(10, 10))).Insert(new Checkbox()).Id);

		rig.Click(20, 20);
		Assert.Equal(new[] { true }, values);
		Assert.False(rig.World.Entity(box).Has<Checked>());

		// The app writes the state back; the next click asks for the opposite.
		rig.World.Entity(box).Set(new Checked());
		rig.Click(20, 20);
		Assert.Equal(new[] { true, false }, values);
	}

	[Fact]
	public void SetChecked_and_ToggleChecked_write_the_marker_without_a_ValueChange()
	{
		var rig = new WidgetRig();
		var changes = 0;
		rig.App.AddObserver<On<ValueChange<bool>>>(_ => changes++);
		var box = rig.Spawn(c => WidgetRig.Hit(c.Spawn().Insert(WidgetRig.Box(10, 10))).Insert(new Checkbox()).Id);

		rig.Run(c => c.Entity(box).EmitTrigger(new SetChecked(true)));
		Assert.True(rig.World.Entity(box).Has<Checked>());
		rig.Run(c => c.Entity(box).EmitTrigger(new ToggleChecked()));
		Assert.False(rig.World.Entity(box).Has<Checked>());
		Assert.Equal(0, changes);
	}

	[Fact]
	public void Focused_self_updating_checkbox_toggles_on_space()
	{
		var rig = new WidgetRig();
		var box = rig.Spawn(c => WidgetRig.Hit(c.Spawn().Insert(WidgetRig.Box(10, 10))).Insert(new Checkbox()).Insert(new SelfUpdate()).Id);
		rig.Focus.Set(box);
		rig.Press(KeyCode.Space);
		Assert.True(rig.World.Entity(box).Has<Checked>());
	}

	private static ulong[] RadioGroup(WidgetRig rig, bool selfUpdate, bool disableSecond = false)
	{
		var ids = new ulong[4];
		ids[0] = rig.Spawn(c =>
		{
			var group = c.Spawn().Insert(new UiNode { Display = Display.Flex, FlexDirection = FlexDirection.Column, Width = Val.Px(100), Height = Val.Px(120) })
				.Insert(new RadioGroup());
			if (selfUpdate) group.Insert(new SelfUpdate());
			for (var i = 1; i <= 3; i++)
			{
				var b = WidgetRig.Hit(c.Spawn().Insert(WidgetRig.Row())).Insert(new RadioButton());
				if (i == 1) b.Insert(new Checked());
				if (i == 2 && disableSecond) b.Insert(new InteractionDisabled());
				group.AddChild(b);
				ids[i] = b.Id;
			}
			return group.Id;
		});
		return ids;
	}

	[Fact]
	public void Radio_click_reports_the_button_and_self_update_moves_Checked()
	{
		var rig = new WidgetRig();
		var picked = new List<(ulong Source, ulong Value)>();
		rig.App.AddObserver<On<ValueChange<ulong>>>(t => picked.Add((t.Event.Source, t.Event.Value)));
		var ids = RadioGroup(rig, selfUpdate: true);

		rig.Click(10, 70); // third button (rows of 30)
		Assert.Equal(new[] { (ids[0], ids[3]) }, picked);
		Assert.False(rig.World.Entity(ids[1]).Has<Checked>());
		Assert.True(rig.World.Entity(ids[3]).Has<Checked>());

		rig.Click(10, 70); // already checked: nothing
		Assert.Single(picked);
	}

	[Fact]
	public void Radio_arrows_skip_disabled_and_controlled_group_stays_put()
	{
		var rig = new WidgetRig();
		var picked = new List<ulong>();
		rig.App.AddObserver<On<ValueChange<ulong>>>(t => picked.Add(t.Event.Value));
		var ids = RadioGroup(rig, selfUpdate: false, disableSecond: true);

		rig.Focus.Set(ids[0]);
		rig.Press(KeyCode.Down);
		Assert.Equal(new[] { ids[3] }, picked);
		Assert.True(rig.World.Entity(ids[1]).Has<Checked>()); // controlled: unchanged
		rig.Press(KeyCode.Up); // from 1 (still checked) wraps to 3
		Assert.Equal(new[] { ids[3], ids[3] }, picked);
	}
}

[Collection("ClayUi")]
public class UiSliderWidgetTests
{
	private static (ulong Track, ulong Thumb) Slider(WidgetRig rig, float value, bool selfUpdate)
	{
		ulong thumb = 0;
		var track = rig.Spawn(c =>
		{
			var t = WidgetRig.Hit(c.Spawn().Insert(WidgetRig.Box(0, 0, 220, 20)))
				.Insert(new Slider { ThumbLength = 20 })
				.Insert(new SliderValue(value))
				.Insert(new SliderRange(0, 100))
				.Insert(new SliderStep(5));
			if (selfUpdate) t.Insert(new SelfUpdate());
			var th = WidgetRig.Hit(c.Spawn().Insert(new UiNode())).Insert(new SliderThumb());
			t.AddChild(th);
			thumb = th.Id;
			return t.Id;
		});
		return (track, thumb);
	}

	[Fact]
	public void Controlled_drag_reports_intermediate_then_final_without_moving()
	{
		var rig = new WidgetRig();
		var changes = new List<(float Value, bool Final)>();
		rig.App.AddObserver<On<ValueChange<float>>>(t => changes.Add((t.Event.Value, t.Event.IsFinal)));
		var (track, thumb) = Slider(rig, 0, selfUpdate: false);
		Assert.True(rig.World.Entity(thumb).Has<ClipToParent>());

		rig.Frame(new Vector2(10, 10));
		rig.Frame(new Vector2(10, 10), down: true);  // grab the thumb at 0
		rig.Frame(new Vector2(110, 10), down: true); // +100px of a 200px travel = 50
		rig.Frame(new Vector2(110, 10));             // release

		Assert.Contains((50f, false), changes);
		Assert.Equal((50f, true), changes[^1]);
		Assert.Equal(0f, rig.World.Entity(track).Get<SliderValue>().Value);
	}

	[Fact]
	public void Keys_step_and_jump_and_self_update_writes_the_value()
	{
		var rig = new WidgetRig();
		var (track, _) = Slider(rig, 50, selfUpdate: true);
		rig.Focus.Set(track);
		rig.Press(KeyCode.Right);
		Assert.Equal(55f, rig.World.Entity(track).Get<SliderValue>().Value);
		rig.Press(KeyCode.Left);
		rig.Press(KeyCode.Left);
		Assert.Equal(45f, rig.World.Entity(track).Get<SliderValue>().Value);
		rig.Press(KeyCode.End);
		Assert.Equal(100f, rig.World.Entity(track).Get<SliderValue>().Value);
		rig.Press(KeyCode.Home);
		Assert.Equal(0f, rig.World.Entity(track).Get<SliderValue>().Value);
	}

	[Fact]
	public void SetSliderValue_clamps_and_moves_the_thumb()
	{
		var rig = new WidgetRig();
		var (track, thumb) = Slider(rig, 0, selfUpdate: false);
		rig.Run(c => c.Entity(track).EmitTrigger(new SetSliderValue(500)));
		rig.Frame();
		Assert.Equal(100f, rig.World.Entity(track).Get<SliderValue>().Value);
		Assert.Equal(200f, rig.World.Entity(thumb).Get<UiNode>().Left.Value);
		rig.Run(c => c.Entity(track).EmitTrigger(new SetSliderValue(-2, SliderValueMode.Steps)));
		Assert.Equal(90f, rig.World.Entity(track).Get<SliderValue>().Value);
	}

	[Fact]
	public void Disabled_slider_ignores_pointer_and_keys()
	{
		var rig = new WidgetRig();
		var changes = 0;
		rig.App.AddObserver<On<ValueChange<float>>>(_ => changes++);
		var (track, _) = Slider(rig, 50, selfUpdate: true);
		rig.World.Entity(track).Set(new InteractionDisabled());
		rig.Click(200, 10);
		rig.Focus.Set(track);
		rig.Press(KeyCode.Right);
		Assert.Equal(0, changes);
	}
}

[Collection("ClayUi")]
public class UiPopoverMenuWidgetTests
{
	[Fact]
	public void Popover_places_below_and_flips_above_at_the_surface_edge()
	{
		var pop = new Popover { Side = PopoverSide.Bottom, Align = PopoverAlign.Start, Gap = 2 };
		var surface = new Vector2(800, 600);
		var below = PopoverPlugin.Compute(pop, new Vector2(100, 100), new Vector2(80, 20), new Vector2(120, 60), surface);
		Assert.Equal(new Vector2(100, 122), below);

		var flipped = PopoverPlugin.Compute(pop, new Vector2(100, 560), new Vector2(80, 20), new Vector2(120, 60), surface);
		Assert.Equal(new Vector2(100, 498), flipped);

		var end = PopoverPlugin.Compute(pop with { Align = PopoverAlign.End }, new Vector2(10, 100), new Vector2(80, 20), new Vector2(120, 60), surface);
		Assert.Equal(0f, end.X); // 10+80-120 clamped into the surface
	}

	[Fact]
	public void Popover_keeps_inside_a_Within_box()
	{
		var pop = new Popover { Side = PopoverSide.Bottom, Gap = 4 };
		// Window 100..300 tall: a 60px list under an anchor at 260 flips above it.
		var p = PopoverPlugin.Compute(pop, new Vector2(120, 260), new Vector2(80, 20), new Vector2(80, 60), new Vector2(100, 100), new Vector2(400, 300));
		Assert.Equal(new Vector2(120, 196), p);
	}

	[Fact]
	public void A_press_on_the_menu_owner_is_left_to_its_toggle_even_without_a_popover()
	{
		var rig = new WidgetRig();
		ulong popup = 0;
		rig.App.AddObserver<On<MenuOpen>, Commands>((t, c) =>
		{
			popup = c.Spawn()
				.Insert(WidgetRig.Box(10, 100, 100, 60))
				.Insert(new BackgroundColor(ClayColor.Gray))
				.Insert(new MenuPopup { Owner = t.EntityId }).Id;
		});
		rig.Spawn(c => WidgetRig.Hit(c.Spawn().Insert(WidgetRig.Box(10, 10, 100, 30))).Insert(new MenuButton()).Id);
		rig.Click(20, 20);
		Assert.True(rig.World.Exists(popup));
		rig.Click(20, 20);
		Assert.False(rig.World.Exists(popup));
		rig.Click(20, 20);
		Assert.True(rig.World.Exists(popup)); // reopened, not dismissed-then-reopened in one click
	}

	// A menu button at (10,10); MenuOpen spawns a popover menu of three items.
	private static (ulong Button, List<ulong> Activated, Func<ulong> Popup, List<ulong> Items) Menu(WidgetRig rig)
	{
		var activated = new List<ulong>();
		var items = new List<ulong>();
		ulong popup = 0;
		rig.App.AddObserver<On<MenuOpen>, Commands>((t, c) =>
		{
			items.Clear();
			var p = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, FlexDirection = FlexDirection.Column, PositionType = PositionType.Absolute, Width = Val.Px(100), Height = Val.Px(90) })
				.Insert(new BackgroundColor(ClayColor.Gray))
				.Insert(new GlobalZIndex(10))
				.Insert(new MenuPopup { Owner = t.EntityId })
				.Insert(new Popover { Anchor = t.EntityId, Side = PopoverSide.Bottom });
			for (var i = 0; i < 3; i++)
			{
				var item = WidgetRig.Hit(c.Spawn().Insert(WidgetRig.Row())).Insert(new MenuItem())
					.Observe((On<Activate> a) => activated.Add(a.EntityId));
				p.AddChild(item);
				items.Add(item.Id);
			}
			popup = p.Id;
		});
		var button = rig.Spawn(c => WidgetRig.Hit(c.Spawn().Insert(WidgetRig.Box(10, 10, 100, 30))).Insert(new MenuButton()).Id);
		return (button, activated, () => popup, items);
	}

	[Fact]
	public void Menu_opens_below_its_button_and_an_item_click_activates_then_closes()
	{
		var rig = new WidgetRig();
		var (button, activated, popup, items) = Menu(rig);

		rig.Click(20, 20);
		Assert.True(rig.World.Exists(popup()));
		rig.Frame(); rig.Frame();
		Assert.Equal(40f, rig.World.Entity(popup()).Get<UiNode>().Top.Value);
		Assert.Equal(0ul, rig.Focus.Entity); // opened by the mouse: focus untouched

		rig.Click(20, 75); // second row: 40 + 30..60
		Assert.Equal(new[] { items[1] }, activated);
		Assert.False(rig.World.Exists(popup()));
		Assert.Equal(0ul, rig.Focus.Entity);
	}

	[Fact]
	public void Menu_keyboard_walk_activate_and_escape()
	{
		var rig = new WidgetRig();
		var (button, activated, popup, items) = Menu(rig);
		rig.Focus.Set(button);
		rig.Press(KeyCode.Down);
		Assert.True(rig.World.Exists(popup()));
		Assert.Equal(popup(), rig.Focus.Entity);
		rig.Press(KeyCode.Down);
		Assert.Equal(items[0], rig.Focus.Entity);
		Assert.True(rig.World.Entity(items[0]).Has<Focused>());
		rig.Press(KeyCode.Up); // wraps to the last
		Assert.Equal(items[2], rig.Focus.Entity);
		rig.Press(KeyCode.Enter);
		Assert.Equal(new[] { items[2] }, activated);
		Assert.False(rig.World.Exists(popup()));

		rig.Press(KeyCode.Down);
		Assert.True(rig.World.Exists(popup()));
		rig.Press(KeyCode.Escape);
		Assert.False(rig.World.Exists(popup()));
		Assert.Equal(button, rig.Focus.Entity);
	}

	[Fact]
	public void Outside_press_dismisses_and_the_menu_button_toggles_closed()
	{
		var rig = new WidgetRig();
		var (_, activated, popup, _) = Menu(rig);

		rig.Click(20, 20);
		rig.Frame();
		Assert.True(rig.World.Exists(popup()));
		rig.Click(20, 20); // the anchor: no dismissal on press, the release toggles it shut
		Assert.False(rig.World.Exists(popup()));

		rig.Click(20, 20);
		rig.Frame();
		Assert.True(rig.World.Exists(popup()));
		rig.Click(500, 500);
		Assert.False(rig.World.Exists(popup()));
		Assert.Empty(activated);
	}
}

[Collection("ClayUi")]
public class UiListTabsWidgetTests
{
	private static ulong[] List(WidgetRig rig, int count, bool selfUpdate)
	{
		var ids = new ulong[count + 1];
		ids[0] = rig.Spawn(c =>
		{
			var list = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, FlexDirection = FlexDirection.Column, PositionType = PositionType.Absolute, Width = Val.Px(100), Height = Val.Px(90), Overflow = Overflow.Scroll })
				.Insert(new ScrollPosition())
				.Insert(new ListBox());
			if (selfUpdate) list.Insert(new SelfUpdate());
			for (var i = 1; i <= count; i++)
			{
				var item = WidgetRig.Hit(c.Spawn().Insert(WidgetRig.Row())).Insert(new ListItem());
				list.AddChild(item);
				ids[i] = item.Id;
			}
			return list.Id;
		});
		return ids;
	}

	[Fact]
	public void ListBox_click_selects_and_arrows_walk_and_scroll_into_view()
	{
		var rig = new WidgetRig();
		var ids = List(rig, 6, selfUpdate: true);
		rig.Click(10, 40);
		Assert.True(rig.World.Entity(ids[2]).Has<Selected>());

		rig.Focus.Set(ids[0]);
		rig.Press(KeyCode.End);
		Assert.True(rig.World.Entity(ids[6]).Has<Selected>());
		Assert.False(rig.World.Entity(ids[2]).Has<Selected>());
		// Item 6 spans 150..180 in a 90px viewport: scrolled by 90.
		Assert.Equal(90f, rig.World.Entity(ids[0]).Get<ScrollPosition>().OffsetY);
		rig.Press(KeyCode.Down); // no wrap at the end
		Assert.True(rig.World.Entity(ids[6]).Has<Selected>());
		rig.Press(KeyCode.Home);
		rig.Frame();
		Assert.True(rig.World.Entity(ids[1]).Has<Selected>());
		Assert.Equal(0f, rig.World.Entity(ids[0]).Get<ScrollPosition>().OffsetY);
	}

	[Fact]
	public void Controlled_ListBox_reports_and_SetSelected_writes()
	{
		var rig = new WidgetRig();
		var picked = new List<ulong>();
		rig.App.AddObserver<On<ValueChange<ulong>>>(t => picked.Add(t.Event.Value));
		var ids = List(rig, 3, selfUpdate: false);
		rig.Click(10, 40);
		Assert.Equal(new[] { ids[2] }, picked);
		Assert.False(rig.World.Entity(ids[2]).Has<Selected>());
		rig.Run(c => c.Entity(ids[2]).EmitTrigger(new SetSelected()));
		Assert.True(rig.World.Entity(ids[2]).Has<Selected>());
	}

	[Fact]
	public void Tabs_click_and_arrows_with_self_update()
	{
		var rig = new WidgetRig();
		var tabs = new ulong[4];
		tabs[0] = rig.Spawn(c =>
		{
			var list = c.Spawn().Insert(new UiNode { Display = Display.Flex, Width = Val.Px(300), Height = Val.Px(30) })
				.Insert(new TabList()).Insert(new SelfUpdate());
			for (var i = 1; i <= 3; i++)
			{
				var tab = WidgetRig.Hit(c.Spawn().Insert(new UiNode { Width = Val.Px(100), Height = Val.Px(30) })).Insert(new Tab());
				if (i == 1) tab.Insert(new Selected());
				list.AddChild(tab);
				tabs[i] = tab.Id;
			}
			return list.Id;
		});
		rig.Click(150, 10);
		Assert.True(rig.World.Entity(tabs[2]).Has<Selected>());
		Assert.False(rig.World.Entity(tabs[1]).Has<Selected>());
		rig.Focus.Set(tabs[0]);
		rig.Press(KeyCode.Right);
		Assert.True(rig.World.Entity(tabs[3]).Has<Selected>());
		rig.Press(KeyCode.Right); // wraps
		Assert.True(rig.World.Entity(tabs[1]).Has<Selected>());
	}
}

[Collection("ClayUi")]
public class UiPickableControlTests
{
	[Fact]
	public void UiPickable_node_gets_an_empty_custom_and_is_picked()
	{
		var rig = new WidgetRig();
		var id = rig.Spawn(c => c.Spawn().Insert(WidgetRig.Box(10, 10)).Insert(new UiPickable()).Insert(new Interaction()).Id);
		Assert.True(rig.World.Entity(id).Has<UiCustom>());
		rig.Frame(new Vector2(20, 20));
		rig.Frame(new Vector2(20, 20));
		Assert.True(rig.World.Entity(id).Has<Hovered>());
	}

	[Fact]
	public void UiControl_bundle_inserts_interaction_and_the_window_opt_outs()
	{
		var rig = new WidgetRig();
		var id = rig.Spawn(c => c.Spawn().Insert(WidgetRig.Box(10, 10)).InsertBundle(new UiControl()).Id);
		var e = rig.World.Entity(id);
		Assert.True(e.Has<Interaction>());
		Assert.True(e.Has<UiNoWindowDrag>());
		Assert.True(e.Has<UiContainsByBounds>());
	}

	[Fact]
	public void Hover_tint_shows_hover_on_the_focused_entity()
	{
		var rig = new WidgetRig();
		rig.App.AddPlugin(new UiHoverTintPlugin());
		var normal = new ClayColor(1, 1, 1, 255);
		var hover = new ClayColor(9, 9, 9, 255);
		var id = rig.Spawn(c => c.Spawn().Insert(WidgetRig.Box(10, 10)).Insert(new BackgroundColor(normal)).Insert(new Interaction())
			.Insert(new UiHoverTint { Normal = normal, Hover = hover }).Id);
		rig.Frame(new Vector2(500, 500));
		Assert.Equal(normal.R, rig.World.Entity(id).Get<BackgroundColor>().Value.R);
		rig.Focus.Set(id);
		rig.Frame();
		rig.Frame();
		Assert.Equal(hover.R, rig.World.Entity(id).Get<BackgroundColor>().Value.R);
	}
}

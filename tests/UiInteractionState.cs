using System.Numerics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;
using Xunit;
using ClayColor = Clay.Color;
using UiNode = TinyEcs.Bevy.UI.Node;
using BevyStage = TinyEcs.Bevy.Stage;

namespace TinyEcs.Tests;

// Interaction state markers (Hovered / Pressed / InteractionDisabled /
// Checked), the shared pick behind Interaction, and the UiGestureGate.
[Collection("ClayUi")]
public class UiInteractionStateTests
{
	private static App MakeApp()
	{
		var app = new App(new World(), ThreadingMode.Single);
		app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600) });
		return app;
	}

	private static UiNode Box(float x, float y, float w = 100, float h = 50) => new()
	{
		Display = Display.Flex,
		PositionType = PositionType.Absolute,
		Left = Val.Px(x), Top = Val.Px(y),
		Width = Val.Px(w), Height = Val.Px(h),
	};

	// Spawned straight into the world: the first app.Update lays them out.
	private static ulong Spawn(App app, System.Func<EntityView, EntityView> build)
		=> build(app.GetWorld().Entity()).ID;

	private static ulong SpawnButton(App app, float x, float y, System.Func<EntityView, EntityView>? extra = null)
		=> Spawn(app, e =>
		{
			e = e.Set(Box(x, y)).Set(new BackgroundColor(ClayColor.White)).Set(new Interaction());
			return extra != null ? extra(e) : e;
		});

	[Fact]
	public void Hovered_marker_follows_the_pointer_on_transitions()
	{
		var app = MakeApp();
		var a = SpawnButton(app, 0, 0);
		var b = SpawnButton(app, 200, 0);
		var w = app.GetWorld();
		var pointer = app.GetResource<UiPointer>();

		pointer.Position = new Vector2(10, 10);
		app.Update();
		app.Update(); // marker insert is deferred
		Assert.True(w.Entity(a).Has<Hovered>());
		Assert.False(w.Entity(b).Has<Hovered>());

		pointer.Position = new Vector2(210, 10);
		app.Update();
		app.Update();
		Assert.False(w.Entity(a).Has<Hovered>());
		Assert.True(w.Entity(b).Has<Hovered>());

		pointer.Position = new Vector2(500, 500);
		app.Update();
		app.Update();
		Assert.False(w.Entity(b).Has<Hovered>());
	}

	[Fact]
	public void Pressed_marker_spans_the_held_gesture_even_off_target()
	{
		var app = MakeApp();
		var btn = SpawnButton(app, 0, 0);
		var w = app.GetWorld();
		var pointer = app.GetResource<UiPointer>();

		pointer.Position = new Vector2(10, 10);
		pointer.Down = true;
		app.Update();
		app.Update();
		Assert.True(w.Entity(btn).Has<Pressed>());

		pointer.Position = new Vector2(500, 500); // drift off while held
		app.Update();
		Assert.True(w.Entity(btn).Has<Pressed>());

		pointer.Down = false;
		app.Update();
		app.Update();
		Assert.False(w.Entity(btn).Has<Pressed>());
	}

	[Fact]
	public void Disabled_widget_blocks_but_gets_no_state_or_click()
	{
		var app = MakeApp();
		// An interactive panel behind; the disabled button sits on top of it.
		var panel = Spawn(app, e => e.Set(Box(0, 0, 300, 200)).Set(new BackgroundColor(ClayColor.Gray)).Set(new Interaction()));
		var btn = SpawnButton(app, 10, 10, e => e.Set(new InteractionDisabled()));
		app.GetWorld().AddChild(panel, btn);

		var clicks = new System.Collections.Generic.List<ulong>();
		app.AddObserver<On<UiClick>>(t => clicks.Add(t.EntityId));

		var pointer = app.GetResource<UiPointer>();
		pointer.Position = new Vector2(20, 20);
		app.Update();
		pointer.Down = true; app.Update();
		pointer.Down = false; app.Update();
		app.Update();

		var w = app.GetWorld();
		Assert.Empty(clicks);
		Assert.Equal(Interaction.None, w.Entity(btn).Get<Interaction>());
		Assert.Equal(Interaction.None, w.Entity(panel).Get<Interaction>());
		Assert.False(w.Entity(btn).Has<Hovered>());
		Assert.False(w.Entity(btn).Has<Pressed>());
	}

	// One hit-test model: a PAINTED element blocks whatever is behind it, even
	// when it is not interactive itself (an overlay, another window's chrome).
	[Fact]
	public void Painted_non_interactive_overlay_blocks_the_button_below()
	{
		var app = MakeApp();
		var btn = SpawnButton(app, 0, 0);
		Spawn(app, e => e.Set(Box(0, 0, 50, 50)).Set(new BackgroundColor(ClayColor.Red)).Set(new GlobalZIndex(5)));

		var clicks = 0;
		app.AddObserver<On<UiClick>>(_ => clicks++);
		var pointer = app.GetResource<UiPointer>();

		pointer.Position = new Vector2(10, 10); // under the overlay
		app.Update();
		pointer.Down = true; app.Update();
		pointer.Down = false; app.Update();
		Assert.Equal(0, clicks);

		pointer.Position = new Vector2(80, 10); // button area the overlay leaves free
		app.Update();
		pointer.Down = true; app.Update();
		pointer.Down = false; app.Update();
		Assert.Equal(1, clicks);
		Assert.Equal(Interaction.Hovered, app.GetWorld().Entity(btn).Get<Interaction>());
	}

	[Fact]
	public void Non_interactive_child_resolves_to_its_interactive_parent()
	{
		var app = MakeApp();
		var btn = SpawnButton(app, 0, 0);
		var label = Spawn(app, e => e.Set(new UiNode { Width = Val.Px(40), Height = Val.Px(20) }).Set(new BackgroundColor(ClayColor.Blue)));
		app.GetWorld().AddChild(btn, label);

		ulong clicked = 0;
		app.AddObserver<On<UiClick>>(t => clicked = t.EntityId);
		var pointer = app.GetResource<UiPointer>();
		pointer.Position = new Vector2(5, 5);
		app.Update();
		pointer.Down = true; app.Update();
		pointer.Down = false; app.Update();
		Assert.Equal(btn, clicked);
	}

	// A button that paints nothing itself (only a small label child) is a pick
	// surface over its whole box with UiContainsByBounds — ranked below its
	// label, above what was painted before it.
	[Fact]
	public void Bare_bounds_button_hits_over_its_whole_box()
	{
		var app = MakeApp();
		var btn = Spawn(app, e => e.Set(Box(0, 0, 120, 40)).Set(new Interaction()).Set(new UiContainsByBounds()));
		var label = Spawn(app, e => e.Set(new UiNode { Width = Val.Px(20), Height = Val.Px(20) }).Set(new BackgroundColor(ClayColor.Blue)));
		app.GetWorld().AddChild(btn, label);
		var plain = Spawn(app, e => e.Set(Box(200, 0, 120, 40)).Set(new Interaction()));

		var clicks = new System.Collections.Generic.List<ulong>();
		app.AddObserver<On<UiClick>>(t => clicks.Add(t.EntityId));
		var pointer = app.GetResource<UiPointer>();

		pointer.Position = new Vector2(100, 30); // outside the label, inside the button box
		app.Update();
		pointer.Down = true; app.Update();
		pointer.Down = false; app.Update();
		pointer.Position = new Vector2(5, 5);     // on the label
		app.Update();
		pointer.Down = true; app.Update();
		pointer.Down = false; app.Update();
		pointer.Position = new Vector2(250, 20);  // paints nothing, no bounds opt-in
		app.Update();
		pointer.Down = true; app.Update();
		pointer.Down = false; app.Update();

		Assert.Equal(new[] { btn, btn }, clicks);
		var w = app.GetWorld();
		Assert.True(w.Entity(btn).Get<ComputedNode>().PaintOrder < w.Entity(label).Get<ComputedNode>().PaintOrder);
		Assert.False(w.Entity(plain).Has<ComputedNode>());
	}

	[Fact]
	public void Self_updating_checkbox_toggles_its_Checked_marker()
	{
		var app = MakeApp();
		app.AddPlugin(new CheckboxPlugin());
		var box = SpawnButton(app, 0, 0, e => e.Set(new Checkbox()).Set(new SelfUpdate()));
		var w = app.GetWorld();
		var pointer = app.GetResource<UiPointer>();
		pointer.Position = new Vector2(10, 10);

		pointer.Down = true; app.Update();
		pointer.Down = false; app.Update();
		app.Update();
		Assert.True(w.Entity(box).Has<Checked>());

		pointer.Down = true; app.Update();
		pointer.Down = false; app.Update();
		app.Update();
		Assert.False(w.Entity(box).Has<Checked>());
	}

	[Fact]
	public void Disabled_checkbox_ignores_label_clicks()
	{
		var app = MakeApp();
		app.AddPlugin(new CheckboxPlugin());
		var box = SpawnButton(app, 0, 0, e => e.Set(new Checkbox()).Set(new SelfUpdate()).Set(new InteractionDisabled()));
		SpawnButton(app, 200, 0, e => e.Set(new CheckboxLabel { Target = box }));
		var pointer = app.GetResource<UiPointer>();
		pointer.Position = new Vector2(210, 10);
		pointer.Down = true; app.Update();
		pointer.Down = false; app.Update();
		app.Update();
		Assert.False(app.GetWorld().Entity(box).Has<Checked>());
	}

	[Fact]
	public void Pick_cache_and_interaction_agree_on_the_hovered_element()
	{
		var app = MakeApp();
		var btn = SpawnButton(app, 0, 0);
		var pointer = app.GetResource<UiPointer>();
		pointer.Position = new Vector2(10, 10);
		app.Update();

		var cache = app.GetResource<UiPickCache>();
		Assert.True(cache.Valid);
		Assert.Equal(btn, cache.Hit.Entity);
		Assert.Equal(btn, app.GetResource<UiClayContext>().HoveredEntity);
	}
}

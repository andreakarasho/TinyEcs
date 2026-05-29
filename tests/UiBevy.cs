using System.Linq;
using System.Numerics;
using Clay;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;
using Xunit;
using ClayColor = Clay.Color;
using UiButton = TinyEcs.Bevy.UI.Button;
using UiNode = TinyEcs.Bevy.UI.Node;
using UiText = TinyEcs.Bevy.UI.Text;
using BevyStage = TinyEcs.Bevy.Stage;

namespace TinyEcs.Tests;

public class UiBevyTests
{
	private static App MakeApp(Vector2? size = null)
	{
		var world = new World();
		var app = new App(world, ThreadingMode.Single);
		app.AddPlugin(new UiPlugin { LogicalSize = size ?? new Vector2(800, 600) });
		return app;
	}

	[Fact]
	public void Layout_emits_render_command_for_solid_root()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(100), Height = Val.Px(50) })
				.Insert(new BackgroundColor(ClayColor.White));
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.Run();

		var cmds = app.GetResource<UiRenderCommands>();
		Assert.True(cmds.Count > 0);
		Assert.Contains(cmds.Span.ToArray(), c => c.CommandType == RenderCommandType.Rectangle);
	}

	[Fact]
	public void Layout_walks_nested_hierarchy()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			var root = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Column,
					Width = Val.Px(200), Height = Val.Px(100),
				})
				.Insert(new BackgroundColor(ClayColor.Rgba(20, 20, 20, 255)));

			var child = c.Spawn()
				.Insert(new UiNode { Width = Val.Px(50), Height = Val.Px(20) })
				.Insert(new BackgroundColor(ClayColor.Red));

			c.AddChild(root, child);
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.Run();

		var cmds = app.GetResource<UiRenderCommands>().Span;
		// Both root and child should emit a Rectangle.
		var rects = cmds.ToArray().Count(c => c.CommandType == RenderCommandType.Rectangle);
		Assert.Equal(2, rects);
	}

	[Fact]
	public void HitTest_marks_hovered_interaction()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true });
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.RunStartup();

		// Place the pointer over the only element (top-left, well inside its 200x100 box).
		app.GetResource<UiPointer>().Position = new Vector2(50, 50);
		app.GetResource<UiPointer>().Down = false;

		app.Update();

		Interaction state = Interaction.None;
		ulong seen = 0;
		foreach (var (entity, ptr) in app.GetWorld().Query<Data<Interaction>>())
		{
			seen = entity.Ref;
			state = ptr.Ref;
		}
		Assert.Equal(Interaction.Hovered, state);
		Assert.NotEqual(0ul, seen);
	}

	[Fact]
	public void PointerDown_marks_pressed_and_release_fires_click()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true });
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		int clickCount = 0;
		app.AddObserver<On<UiClick>>(trigger => clickCount++);

		app.RunStartup();

		var pointer = app.GetResource<UiPointer>();
		pointer.Position = new Vector2(40, 40);

		// Frame 1: press
		pointer.Down = true;
		app.Update();
		Interaction pressed = Interaction.None;
		foreach (var (_e, p) in app.GetWorld().Query<Data<Interaction>>()) pressed = p.Ref;
		Assert.Equal(Interaction.Pressed, pressed);

		// Frame 2: release while still over the same entity → click.
		pointer.Down = false;
		app.Update();
		Assert.Equal(1, clickCount);
	}

	[Fact]
	public void Click_does_not_fire_when_press_started_outside_entity()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(100), Height = Val.Px(50) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true });
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		int clickCount = 0;
		app.AddObserver<On<UiClick>>(trigger => clickCount++);

		app.RunStartup();

		var pointer = app.GetResource<UiPointer>();

		// Press outside the 100x50 box at (0,0).
		pointer.Position = new Vector2(500, 500);
		pointer.Down = true;
		app.Update();

		// Drag onto the box while still pressed.
		pointer.Position = new Vector2(40, 25);
		app.Update();

		// Release on the box.
		pointer.Down = false;
		app.Update();
		Assert.Equal(0, clickCount);
	}

	[Fact]
	public void DoubleClick_fires_on_two_clicks_within_window()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true });
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		int clickCount = 0;
		int doubleClickCount = 0;
		app.AddObserver<On<UiClick>>(_ => clickCount++);
		app.AddObserver<On<UiDoubleClick>>(_ => doubleClickCount++);

		app.RunStartup();

		var pointer = app.GetResource<UiPointer>();
		var ctx = app.GetResource<UiClayContext>();
		pointer.Position = new Vector2(40, 40);
		// Default window is 0.35s; advance time well under that between clicks.
		ctx.DeltaTime = 0.05f;

		// First click (press + release).
		pointer.Down = true;
		app.Update();
		pointer.Down = false;
		app.Update();
		Assert.Equal(1, clickCount);
		Assert.Equal(0, doubleClickCount);

		// Second click within the window -> UiDoubleClick.
		pointer.Down = true;
		app.Update();
		pointer.Down = false;
		app.Update();
		Assert.Equal(2, clickCount);
		Assert.Equal(1, doubleClickCount);
	}

	[Fact]
	public void DoubleClick_does_not_fire_when_second_click_is_too_late()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true });
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		int doubleClickCount = 0;
		app.AddObserver<On<UiDoubleClick>>(_ => doubleClickCount++);

		app.RunStartup();

		var pointer = app.GetResource<UiPointer>();
		var ctx = app.GetResource<UiClayContext>();
		pointer.Position = new Vector2(40, 40);

		// First click.
		ctx.DeltaTime = 0.0f;
		pointer.Down = true;
		app.Update();
		pointer.Down = false;
		app.Update();

		// Advance time past the window before the second click.
		ctx.DeltaTime = ctx.DoubleClickWindow + 0.1f;
		app.Update();
		ctx.DeltaTime = 0.0f;

		// Second click — too late.
		pointer.Down = true;
		app.Update();
		pointer.Down = false;
		app.Update();
		Assert.Equal(0, doubleClickCount);
	}

	[Fact]
	public void DoubleClick_requires_same_entity_for_both_clicks()
	{
		var app = MakeApp();
		ulong leftId = 0, rightId = 0;
		app.AddSystem((Commands c) =>
		{
			// Two side-by-side hit-test rects.
			var left = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(0), Top = Val.Px(0),
					Width = Val.Px(100), Height = Val.Px(100),
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true });
			leftId = left.Id;

			var right = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(200), Top = Val.Px(0),
					Width = Val.Px(100), Height = Val.Px(100),
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true });
			rightId = right.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		int doubleClickCount = 0;
		app.AddObserver<On<UiDoubleClick>>(_ => doubleClickCount++);

		app.RunStartup();

		var pointer = app.GetResource<UiPointer>();
		var ctx = app.GetResource<UiClayContext>();
		ctx.DeltaTime = 0.05f;

		// Click left rect.
		pointer.Position = new Vector2(50, 50);
		pointer.Down = true;
		app.Update();
		pointer.Down = false;
		app.Update();

		// Click right rect within window — different entity, no UiDoubleClick.
		pointer.Position = new Vector2(250, 50);
		pointer.Down = true;
		app.Update();
		pointer.Down = false;
		app.Update();

		Assert.Equal(0, doubleClickCount);
	}

	[Fact]
	public void DoubleClick_clears_latch_so_triple_click_is_click_plus_dclick()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true });
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		int doubleClickCount = 0;
		app.AddObserver<On<UiDoubleClick>>(_ => doubleClickCount++);

		app.RunStartup();

		var pointer = app.GetResource<UiPointer>();
		var ctx = app.GetResource<UiClayContext>();
		pointer.Position = new Vector2(40, 40);
		ctx.DeltaTime = 0.05f;

		// Three back-to-back clicks within the dclick window.
		for (int i = 0; i < 3; i++)
		{
			pointer.Down = true;
			app.Update();
			pointer.Down = false;
			app.Update();
		}

		// Click 1 -> nothing. Click 2 -> dclick (latch cleared). Click 3 -> nothing.
		Assert.Equal(1, doubleClickCount);
	}

	[Fact]
	public void OverflowScroll_registers_scroll_container()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			var root = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Column,
					Width = Val.Px(200),
					Height = Val.Px(100),
					Overflow = Overflow.Scroll,
				})
				.Insert(new BackgroundColor(ClayColor.White));

			// Stuff enough rows to overflow.
			for (var i = 0; i < 20; i++)
			{
				var row = c.Spawn()
					.Insert(new UiNode { Width = Val.Px(180), Height = Val.Px(24) })
					.Insert(new BackgroundColor(ClayColor.Red));
				c.AddChild(root, row);
			}
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.Run();

		var ctx = app.GetResource<UiClayContext>();
		Assert.Single(ctx.ScrollClayToEntity);

		var (clayId, _) = ctx.ScrollClayToEntity.First();
		var data = ctx.GetScrollContainerData(clayId);
		Assert.True(data.Found);
		Assert.True(data.OverflowsY);
	}

	[Fact]
	public void ScrollPosition_reflects_programmatic_scroll()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			var root = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Column,
					Width = Val.Px(200),
					Height = Val.Px(100),
					Overflow = Overflow.Scroll,
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new ScrollPosition());

			for (var i = 0; i < 20; i++)
			{
				var row = c.Spawn()
					.Insert(new UiNode { Width = Val.Px(180), Height = Val.Px(24) })
					.Insert(new BackgroundColor(ClayColor.Red));
				c.AddChild(root, row);
			}
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.Run();

		// First frame: ScrollPosition starts at 0.
		ScrollPosition sp = default;
		foreach (var (_e, s) in app.GetWorld().Query<Data<ScrollPosition>>()) sp = s.Ref;
		Assert.Equal(0f, sp.OffsetY);

		// Programmatically scroll the root entity.
		ulong rootId = 0;
		foreach (var (e, _) in app.GetWorld().Query<Data<ScrollPosition>>()) rootId = e.Ref;
		app.GetResource<UiClayContext>().SetScrollPosition(rootId, new Vector2(0, 40));

		app.Update();

		foreach (var (_e, s) in app.GetWorld().Query<Data<ScrollPosition>>()) sp = s.Ref;
		Assert.Equal(40f, sp.OffsetY);
	}

	[Fact]
	public void ScrollPosition_bidirectional_user_write_pushes_to_clay()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			var root = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Column,
					Width = Val.Px(200),
					Height = Val.Px(100),
					Overflow = Overflow.Scroll,
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new ScrollPosition());

			for (var i = 0; i < 20; i++)
			{
				var row = c.Spawn()
					.Insert(new UiNode { Width = Val.Px(180), Height = Val.Px(24) })
					.Insert(new BackgroundColor(ClayColor.Red));
				c.AddChild(root, row);
			}
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.Run(); // populate scroll containers + ScrollPosition

		// Mutate ScrollPosition directly via a system on the next frame.
		app.AddSystem((Query<Data<ScrollPosition>> q) =>
		{
			foreach (var (_e, sp) in q)
				sp.Ref.OffsetY = 30;
		})
		.InStage(BevyStage.Update).SingleThreaded().Build();

		app.Update();

		ScrollPosition sp = default;
		foreach (var (_e, s) in app.GetWorld().Query<Data<ScrollPosition>>()) sp = s.Ref;
		Assert.Equal(30f, sp.OffsetY);

		// Subsequent frame without mutation should preserve the value (no echo loop).
		app.Update();
		foreach (var (_e, s) in app.GetWorld().Query<Data<ScrollPosition>>()) sp = s.Ref;
		Assert.Equal(30f, sp.OffsetY);
	}

	[Fact]
	public void Text_node_emits_text_render_command()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			var root = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(300), Height = Val.Px(60) });

			var label = c.Spawn()
				.Insert(new UiNode())
				.Insert(new UiText("hello"))
				.Insert(new TextFont { Size = 16 })
				.Insert(new TextColor(ClayColor.White));

			c.AddChild(root, label);
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.Run();

		var cmds = app.GetResource<UiRenderCommands>().Span.ToArray();
		Assert.Contains(cmds, c => c.CommandType == RenderCommandType.Text);
	}

	[Fact]
	public void Pointer_outside_element_yields_no_interaction()
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

		app.RunStartup();

		app.GetResource<UiPointer>().Position = new Vector2(500, 500);
		app.Update();

		Interaction state = Interaction.None;
		foreach (var (_e, p) in app.GetWorld().Query<Data<Interaction>>()) state = p.Ref;
		Assert.Equal(Interaction.None, state);
	}

	[Fact]
	public void ScrollbarPlugin_positions_thumb_from_target_state()
	{
		var app = MakeApp();
		app.AddPlugin(new ScrollbarPlugin());

		ulong targetId = 0, barId = 0, thumbId = 0;
		app.AddSystem((Commands c) =>
		{
			var target = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Column,
					Width = Val.Px(200),
					Height = Val.Px(100),
					Overflow = Overflow.Scroll,
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new ScrollPosition());

			for (var i = 0; i < 20; i++)
			{
				var row = c.Spawn()
					.Insert(new UiNode { Width = Val.Px(180), Height = Val.Px(24) })
					.Insert(new BackgroundColor(ClayColor.Red));
				c.AddChild(target, row);
			}

			var bar = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(10), Height = Val.Px(100) })
				.Insert(new Scrollbar
				{
					Target = target.Id,
					Orientation = ScrollbarOrientation.Vertical,
					MinThumbLength = 16,
				})
				.Insert(new BackgroundColor(ClayColor.Gray));

			var thumb = c.Spawn()
				.Insert(new UiNode())
				.Insert(new ScrollbarThumb())
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new ScrollbarDragState())
				.Insert(new BackgroundColor(ClayColor.White));

			c.AddChild(bar, thumb);

			targetId = target.Id;
			barId = bar.Id;
			thumbId = thumb.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		// 3 frames: 1) layout target+bar+thumb. 2) plugin reads scroll data, sets thumb size.
		//           3) layout re-runs with new thumb dims so ComputedNode reflects them.
		app.Update();
		app.Update();
		app.Update();

		var world = app.GetWorld();
		var thumbView = world.Entity(thumbId);
		ref var thumbNode = ref thumbView.Get<UiNode>();
		Assert.Equal(PositionType.Absolute, thumbNode.PositionType);
		Assert.True(thumbNode.Height.Value > 0);
		// 100px visible / 24*20=480px content → ratio 0.208 → thumb ~20.8 (>= minThumb 16).
		Assert.InRange(thumbNode.Height.Value, 16f, 50f);

		// Clay must actually render the thumb with a non-zero box.
		Assert.True(thumbView.Has<ComputedNode>(), "thumb missing ComputedNode");
		ref var thumbComputed = ref thumbView.Get<ComputedNode>();
		Assert.True(thumbComputed.Size.X > 0, $"thumb width zero: {thumbComputed.Size}");
		Assert.True(thumbComputed.Size.Y > 0, $"thumb height zero: {thumbComputed.Size}");
	}

	[Fact]
	public void Checkbox_toggles_on_click()
	{
		var app = MakeApp();
		app.AddPlugin(new CheckboxPlugin());

		ulong boxId = 0;
		app.AddSystem((Commands c) =>
		{
			var box = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(40), Height = Val.Px(40) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new Checkbox { Checked = false });
			boxId = box.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.RunStartup();

		var pointer = app.GetResource<UiPointer>();
		pointer.Position = new Vector2(20, 20);

		// Press + release on the box → click → toggles checkbox.
		pointer.Down = true; app.Update();
		pointer.Down = false; app.Update();
		// Click handler runs the frame after the event is flushed.
		app.Update();
		app.Update();

		var world = app.GetWorld();
		Assert.True(world.Entity(boxId).Get<Checkbox>().Checked);

		// Second click toggles back off.
		pointer.Down = true; app.Update();
		pointer.Down = false; app.Update();
		app.Update();
		app.Update();
		Assert.False(world.Entity(boxId).Get<Checkbox>().Checked);
	}

	[Fact]
	public void Slider_thumb_positioned_from_value()
	{
		var app = MakeApp();
		app.AddPlugin(new SliderPlugin());

		ulong thumbId = 0;
		app.AddSystem((Commands c) =>
		{
			var slider = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(20) })
				.Insert(new BackgroundColor(ClayColor.Gray))
				.Insert(new Slider
				{
					Min = 0, Max = 100, Value = 50,
					ThumbLength = 20,
					Orientation = ScrollbarOrientation.Horizontal,
				});

			var thumb = c.Spawn()
				.Insert(new UiNode())
				.Insert(new SliderThumb())
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new SliderDragState())
				.Insert(new BackgroundColor(ClayColor.White));
			thumbId = thumb.Id;

			c.AddChild(slider, thumb);
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		// Need 3 frames so the layout, plugin sizing, and re-layout all settle.
		app.Update();
		app.Update();
		app.Update();

		var world = app.GetWorld();
		ref var thumbNode = ref world.Entity(thumbId).Get<UiNode>();
		Assert.Equal(PositionType.Absolute, thumbNode.PositionType);
		// Value 50/100 on a 200px-20px track → thumb Left around 90.
		Assert.InRange(thumbNode.Left.Value, 80f, 100f);
	}

	[Fact]
	public void Scrollbar_click_on_track_jumps_scroll()
	{
		var app = MakeApp();
		app.AddPlugin(new ScrollbarPlugin());

		app.AddSystem((Commands c) =>
		{
			var target = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Column,
					Width = Val.Px(200),
					Height = Val.Px(100),
					Overflow = Overflow.Scroll,
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new ScrollPosition());

			for (var i = 0; i < 20; i++)
			{
				var row = c.Spawn()
					.Insert(new UiNode { Width = Val.Px(180), Height = Val.Px(24) })
					.Insert(new BackgroundColor(ClayColor.Red));
				c.AddChild(target, row);
			}

			// Bar lives at x=200..210, y=0..100.
			var bar = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					Width = Val.Px(10),
					Height = Val.Px(100),
					PositionType = PositionType.Absolute,
					Left = Val.Px(200),
					Top = Val.Px(0),
				})
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new Scrollbar
				{
					Target = target.Id,
					Orientation = ScrollbarOrientation.Vertical,
					MinThumbLength = 16,
				})
				.Insert(new BackgroundColor(ClayColor.Gray));

			var thumb = c.Spawn()
				.Insert(new UiNode())
				.Insert(new ScrollbarThumb())
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new ScrollbarDragState())
				.Insert(new BackgroundColor(ClayColor.White));
			c.AddChild(bar, thumb);
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.Update(); // layout
		app.Update(); // plugin positions thumb; second layout pass

		// Click on the rail near the bottom (y = 90 out of 100 → ~90% of maxScroll).
		var pointer = app.GetResource<UiPointer>();
		pointer.Position = new Vector2(205, 90);
		pointer.Down = true;
		app.Update(); // hit-test fires UiPointerDown
		pointer.Down = false;
		app.Update(); // Drag system consumes the event
		app.Update(); // ScrollPosition synced from Clay

		ScrollPosition sp = default;
		foreach (var (_e, s) in app.GetWorld().Query<Data<ScrollPosition>>()) sp = s.Ref;
		Assert.True(sp.OffsetY > 100f, $"expected large scroll, got {sp.OffsetY}");
	}

	[Fact]
	public void Slider_click_on_track_jumps_value()
	{
		var app = MakeApp();
		app.AddPlugin(new SliderPlugin());

		ulong trackId = 0;
		app.AddSystem((Commands c) =>
		{
			var track = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(20) })
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new Slider
				{
					Min = 0, Max = 100, Value = 0,
					ThumbLength = 20,
					Orientation = ScrollbarOrientation.Horizontal,
				})
				.Insert(new BackgroundColor(ClayColor.Gray));

			var thumb = c.Spawn()
				.Insert(new UiNode())
				.Insert(new SliderThumb())
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new SliderDragState())
				.Insert(new BackgroundColor(ClayColor.White));
			c.AddChild(track, thumb);
			trackId = track.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.Update();
		app.Update();

		// Click at x = 150 on a 200-wide track → 75% → value 75.
		var pointer = app.GetResource<UiPointer>();
		pointer.Position = new Vector2(150, 10);
		pointer.Down = true;
		app.Update();
		pointer.Down = false;
		app.Update();
		app.Update();

		var slider = app.GetWorld().Entity(trackId).Get<Slider>();
		Assert.InRange(slider.Value, 70f, 80f);
	}

	[Fact]
	public void Absolute_zindex_orders_render_commands()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			var parent = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(200) })
				.Insert(new BackgroundColor(ClayColor.White));

			var lower = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(10), Top = Val.Px(10),
					Width = Val.Px(40), Height = Val.Px(40),
				})
				.Insert(new BackgroundColor(ClayColor.Red))
				.Insert(new ZIndex(1));

			var higher = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(20), Top = Val.Px(20),
					Width = Val.Px(40), Height = Val.Px(40),
				})
				.Insert(new BackgroundColor(ClayColor.Blue))
				.Insert(new ZIndex(5));

			c.AddChild(parent, lower);
			c.AddChild(parent, higher);
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.Run();

		// Higher-z (Blue) must appear in the render stream after lower-z (Red).
		var cmds = app.GetResource<UiRenderCommands>().Span;
		int redIdx = -1, blueIdx = -1;
		for (var i = 0; i < cmds.Length; i++)
		{
			if (cmds[i].CommandType != RenderCommandType.Rectangle) continue;
			ref readonly var c = ref cmds[i].Rectangle;
			if (c.BackgroundColor.R == 255 && c.BackgroundColor.B == 0) redIdx = i;
			if (c.BackgroundColor.B == 255 && c.BackgroundColor.R == 0) blueIdx = i;
		}
		Assert.True(redIdx >= 0 && blueIdx >= 0, $"red={redIdx} blue={blueIdx}");
		Assert.True(blueIdx > redIdx, $"expected blue (z=5) after red (z=1); red={redIdx} blue={blueIdx}");
	}

	[Fact]
	public void Absolute_right_bottom_anchors_to_opposite_edges()
	{
		var app = MakeApp(new Vector2(400, 300));

		ulong childId = 0;
		app.AddSystem((Commands c) =>
		{
			var parent = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(400), Height = Val.Px(300) })
				.Insert(new BackgroundColor(ClayColor.White));

			var child = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Width = Val.Px(40),
					Height = Val.Px(20),
					Right = Val.Px(10),
					Bottom = Val.Px(15),
				})
				.Insert(new BackgroundColor(ClayColor.Red));

			c.AddChild(parent, child);
			childId = child.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		// First frame seeds ComputedNode on the parent; second frame's layout
		// reads it to translate Right/Bottom into pixel offsets.
		app.Update();
		app.Update();

		var computed = app.GetWorld().Entity(childId).Get<ComputedNode>();
		// Parent is 400x300, child is 40x20. Right=10, Bottom=15 means
		// child's right edge sits 10px from parent's right (x = 400 - 40 - 10 = 350)
		// and bottom edge sits 15px from parent's bottom (y = 300 - 20 - 15 = 265).
		Assert.Equal(350f, computed.Position.X);
		Assert.Equal(265f, computed.Position.Y);
	}

	[Fact]
	public void HitTest_topmost_zindex_wins_over_lower_sibling()
	{
		var app = MakeApp();
		ulong lowerId = 0, higherId = 0;
		app.AddSystem((Commands c) =>
		{
			var parent = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(200) })
				.Insert(new BackgroundColor(ClayColor.White));

			var lower = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(20), Top = Val.Px(20),
					Width = Val.Px(80), Height = Val.Px(80),
				})
				.Insert(new BackgroundColor(ClayColor.Red))
				.Insert(new ZIndex(1))
				.Insert(new Interaction());
			lowerId = lower.Id;

			var higher = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(20), Top = Val.Px(20),
					Width = Val.Px(80), Height = Val.Px(80),
				})
				.Insert(new BackgroundColor(ClayColor.Blue))
				.Insert(new ZIndex(5))
				.Insert(new Interaction());
			higherId = higher.Id;

			c.AddChild(parent, lower);
			c.AddChild(parent, higher);
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.RunStartup();
		// First Update lays out + builds render commands. PointerOverIds depends on
		// the tree existing at SetPointerState time (PreLayout uses previous frame's tree).
		app.Update();
		app.GetResource<UiPointer>().Position = new Vector2(50, 50);
		app.Update();

		var w = app.GetWorld();
		Assert.Equal(Interaction.Hovered, w.Entity(higherId).Get<Interaction>());
		Assert.Equal(Interaction.None,    w.Entity(lowerId).Get<Interaction>());
	}

	[Fact]
	public void HitTest_inner_child_wins_over_interactive_parent()
	{
		var app = MakeApp();
		ulong parentId = 0, childId = 0;
		app.AddSystem((Commands c) =>
		{
			var parent = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(200) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction());
			parentId = parent.Id;

			var child = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(40), Top = Val.Px(40),
					Width = Val.Px(60), Height = Val.Px(60),
				})
				.Insert(new BackgroundColor(ClayColor.Red))
				.Insert(new Interaction());
			childId = child.Id;

			c.AddChild(parent, child);
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.RunStartup();
		app.Update();
		// Pointer over child (which sits inside parent bbox).
		app.GetResource<UiPointer>().Position = new Vector2(60, 60);
		app.Update();

		var w = app.GetWorld();
		Assert.Equal(Interaction.Hovered, w.Entity(childId).Get<Interaction>());
		Assert.Equal(Interaction.None,    w.Entity(parentId).Get<Interaction>());
	}

	[Fact]
	public void Clay_PointerOverIds_topmost_first_after_layout()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			// Parent + flex child in the same layout tree (no absolute positioning,
			// which would split into separate floating roots and short-circuit DFS
			// via Capture mode).
			var parent = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(200) })
				.Insert(new BackgroundColor(ClayColor.White));

			var child = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(60), Height = Val.Px(60) })
				.Insert(new BackgroundColor(ClayColor.Red));

			c.AddChild(parent, child);
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.RunStartup();
		app.Update();
		// Child sits at top-left of parent in flex layout; (30,30) lands inside it.
		app.GetResource<UiPointer>().Position = new Vector2(30, 30);
		app.Update();

		var ctx = global::Clay.Clay.Context!;
		var ids = ctx.PointerOverIds.AsReadOnlySpan();
		Assert.True(ids.Length >= 2, $"expected >=2 ids (parent + child overlap), got {ids.Length}");

		// First entry = topmost (child); a later entry = ancestor (parent or
		// Clay's implicit root). Bbox area is a stand-in for depth here.
		var first = ctx.GetElementData(ids[0]).BoundingBox;
		var last = ctx.GetElementData(ids[ids.Length - 1]).BoundingBox;
		Assert.True(first.Width * first.Height <= last.Width * last.Height,
			$"first {first.Width}x{first.Height} should not be larger than last {last.Width}x{last.Height}");
	}

	[Fact]
	public void UiScale_doubles_computed_size_for_px_values()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(100), Height = Val.Px(50) })
				.Insert(new BackgroundColor(ClayColor.White));
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.GetResource<UiScale>().Value = 2f;
		app.Run();

		ComputedNode computed = default;
		foreach (var (_e, c) in app.GetWorld().Query<Data<ComputedNode>>()) computed = c.Ref;
		Assert.Equal(200f, computed.Size.X);
		Assert.Equal(100f, computed.Size.Y);
	}

	[Fact]
	public void UiScale_scales_padding_and_border()
	{
		var app = MakeApp();
		ulong childId = 0;
		app.AddSystem((Commands c) =>
		{
			var root = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					Width = Val.Px(200), Height = Val.Px(200),
					Padding = UiRect.All(10),
				})
				.Insert(new BackgroundColor(ClayColor.White));

			var child = c.Spawn()
				.Insert(new UiNode { Width = Val.Px(50), Height = Val.Px(50) })
				.Insert(new BackgroundColor(ClayColor.Red));
			childId = child.Id;

			c.AddChild(root, child);
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.GetResource<UiScale>().Value = 2f;
		app.Run();

		// Padding doubles → child offset by 20px from root's top-left.
		var childComputed = app.GetWorld().Entity(childId).Get<ComputedNode>();
		Assert.Equal(20f, childComputed.Position.X);
		Assert.Equal(20f, childComputed.Position.Y);
		// Child size also doubles.
		Assert.Equal(100f, childComputed.Size.X);
	}

	[Fact]
	public void Despawn_removes_entity_from_next_layout()
	{
		var app = MakeApp();
		ulong targetId = 0;
		app.AddSystem((Commands c) =>
		{
			c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(100), Height = Val.Px(50) })
				.Insert(new BackgroundColor(ClayColor.White));

			var target = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(100), Height = Val.Px(50) })
				.Insert(new BackgroundColor(ClayColor.Red));
			targetId = target.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.RunStartup();
		app.Update();

		// Two roots, two Rectangle render commands.
		var beforeCount = 0;
		foreach (var cmd in app.GetResource<UiRenderCommands>().Span)
			if (cmd.CommandType == RenderCommandType.Rectangle) beforeCount++;
		Assert.Equal(2, beforeCount);

		// Despawn the red root.
		app.GetWorld().Entity(targetId).Delete();
		app.Update();

		var afterCount = 0;
		foreach (var cmd in app.GetResource<UiRenderCommands>().Span)
			if (cmd.CommandType == RenderCommandType.Rectangle) afterCount++;
		Assert.Equal(1, afterCount);
	}

	[Fact]
	public void Despawn_hovered_entity_does_not_crash_next_frame()
	{
		var app = MakeApp();
		ulong targetId = 0;
		app.AddSystem((Commands c) =>
		{
			var target = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true });
			targetId = target.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.RunStartup();
		app.GetResource<UiPointer>().Position = new Vector2(50, 50);
		app.Update();

		// Entity should be hovered now.
		Assert.Equal(Interaction.Hovered, app.GetWorld().Entity(targetId).Get<Interaction>());

		// Despawn while hovered, then run another frame. Must not throw.
		app.GetWorld().Entity(targetId).Delete();
		var ex = Record.Exception(() => app.Update());
		Assert.Null(ex);
	}

	[Fact]
	public void Despawn_pressed_entity_does_not_fire_click_on_release()
	{
		var app = MakeApp();
		ulong targetId = 0;
		app.AddSystem((Commands c) =>
		{
			var target = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true });
			targetId = target.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		int clickCount = 0;
		app.AddObserver<On<UiClick>>(t => clickCount++);

		app.RunStartup();
		var pointer = app.GetResource<UiPointer>();
		pointer.Position = new Vector2(50, 50);

		// Press.
		pointer.Down = true;
		app.Update();
		Assert.Equal(Interaction.Pressed, app.GetWorld().Entity(targetId).Get<Interaction>());

		// Kill the entity, then release.
		app.GetWorld().Entity(targetId).Delete();
		pointer.Down = false;
		var ex = Record.Exception(() => app.Update());
		Assert.Null(ex);
		Assert.Equal(0, clickCount);
	}

	[Fact]
	public void ComputedNode_written_after_layout()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(120), Height = Val.Px(40) })
				.Insert(new BackgroundColor(ClayColor.White));
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.Run();

		ComputedNode computed = default;
		foreach (var (_e, c) in app.GetWorld().Query<Data<ComputedNode>>()) computed = c.Ref;
		Assert.Equal(120f, computed.Size.X);
		Assert.Equal(40f, computed.Size.Y);
	}

	// Regression: Val.Percent uses Bevy's 0..100 range. Val.Percent(100) on a root
	// must produce a ComputedNode that fills the logical surface. A prior bug
	// shipped Val.Percent(1f) in samples and saw a 1% root (~8x6 on an 800x600
	// surface), which pushed centered children to the top-left corner.
	[Fact]
	public void Root_with_Percent_100_fills_logical_size()
	{
		var size = new Vector2(800, 600);
		var app = MakeApp(size);
		ulong rootId = 0;
		app.AddSystem((Commands c) =>
		{
			rootId = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					Width = Val.Percent(100f),
					Height = Val.Percent(100f),
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.Run();

		var computed = app.GetWorld().Entity(rootId).Get<ComputedNode>();
		Assert.Equal(size.X, computed.Size.X);
		Assert.Equal(size.Y, computed.Size.Y);
	}

	[Fact]
	public void Root_with_Percent_50_fills_half_logical_size()
	{
		var size = new Vector2(800, 600);
		var app = MakeApp(size);
		ulong rootId = 0;
		app.AddSystem((Commands c) =>
		{
			rootId = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					Width = Val.Percent(50f),
					Height = Val.Percent(50f),
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.Run();

		var computed = app.GetWorld().Entity(rootId).Get<ComputedNode>();
		Assert.Equal(size.X * 0.5f, computed.Size.X);
		Assert.Equal(size.Y * 0.5f, computed.Size.Y);
	}

	// Verifies the centering pipeline that the TinyEcsGame sample relies on:
	// a full-surface root with JustifyContent.Center + AlignItems.Center should
	// position a fixed-size child at the surface midpoint.
	[Fact]
	public void Center_alignment_on_percent_root_positions_child_at_midpoint()
	{
		var size = new Vector2(800, 600);
		var app = MakeApp(size);
		ulong childId = 0;
		app.AddSystem((Commands c) =>
		{
			var root = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					FlexDirection = FlexDirection.Row,
					JustifyContent = JustifyContent.Center,
					AlignItems = AlignItems.Center,
					Width = Val.Percent(100f),
					Height = Val.Percent(100f),
				});

			var child = c.Spawn()
				.Insert(new UiNode { Width = Val.Px(200), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.Red));
			childId = child.Id;

			c.AddChild(root, child);
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.Run();

		var computed = app.GetWorld().Entity(childId).Get<ComputedNode>();
		Assert.Equal((size.X - 200) * 0.5f, computed.Position.X, precision: 1);
		Assert.Equal((size.Y - 100) * 0.5f, computed.Position.Y, precision: 1);
	}

	[Fact]
	public void Absolute_child_inherits_root_z_when_it_has_none()
	{
		// A floating child with no z of its own must render on its root's
		// layer, so a window can carry a single z on its root and the whole
		// subtree rides it (no per-child z propagation).
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			var root = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, PositionType = PositionType.Absolute,
					Left = Val.Px(0), Top = Val.Px(0), Width = Val.Px(100), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.Rgba(255, 0, 0, 255)))
				.Insert(new GlobalZIndex(7));

			var child = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, PositionType = PositionType.Absolute,
					Left = Val.Px(10), Top = Val.Px(10), Width = Val.Px(20), Height = Val.Px(20) })
				.Insert(new BackgroundColor(ClayColor.Rgba(0, 255, 0, 255)));

			c.AddChild(root, child);
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.Run();

		var cmds = app.GetResource<UiRenderCommands>().Span.ToArray();
		var childCmd = cmds.First(c => c.CommandType == RenderCommandType.Rectangle
			&& c.Rectangle.BackgroundColor.G > 200 && c.Rectangle.BackgroundColor.R < 50);
		Assert.Equal((short)7, childCmd.ZIndex);
	}

	[Fact]
	public void Absolute_child_keeps_its_own_z_over_inherited()
	{
		// Backward-compat: an element that sets its own z is unchanged —
		// inheritance only fills in for elements with none.
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			var root = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, PositionType = PositionType.Absolute,
					Left = Val.Px(0), Top = Val.Px(0), Width = Val.Px(100), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.Rgba(255, 0, 0, 255)))
				.Insert(new GlobalZIndex(7));

			var child = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, PositionType = PositionType.Absolute,
					Left = Val.Px(10), Top = Val.Px(10), Width = Val.Px(20), Height = Val.Px(20) })
				.Insert(new BackgroundColor(ClayColor.Rgba(0, 255, 0, 255)))
				.Insert(new GlobalZIndex(3));

			c.AddChild(root, child);
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		app.Run();

		var cmds = app.GetResource<UiRenderCommands>().Span.ToArray();
		var childCmd = cmds.First(c => c.CommandType == RenderCommandType.Rectangle
			&& c.Rectangle.BackgroundColor.G > 200 && c.Rectangle.BackgroundColor.R < 50);
		Assert.Equal((short)3, childCmd.ZIndex);
	}

	private static App MakeInteractiveApp(out System.Func<ulong> spawnedId)
	{
		var app = MakeApp();
		ulong id = 0;
		app.AddSystem((Commands c) =>
		{
			id = c.Spawn()
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();
		spawnedId = () => id;
		return app;
	}

	[Fact]
	public void Move_fires_with_delta_when_pointer_moves_over_entity()
	{
		var app = MakeInteractiveApp(out _);

		int moveCount = 0;
		Vector2 lastDelta = default;
		app.AddObserver<On<UiMove>>(trigger => { moveCount++; lastDelta = trigger.Event.Delta; });

		app.RunStartup();
		var pointer = app.GetResource<UiPointer>();

		// Frame 1: pointer enters → Over, not Move. Latches LastPosition.
		pointer.Position = new Vector2(50, 50);
		app.Update();
		Assert.Equal(0, moveCount);

		// Frame 2: pointer moved while still over the same entity → Move with delta.
		pointer.Position = new Vector2(70, 60);
		app.Update();
		Assert.Equal(1, moveCount);
		Assert.Equal(new Vector2(20, 10), lastDelta);
	}

	[Fact]
	public void Move_does_not_fire_on_first_hover_frame()
	{
		var app = MakeInteractiveApp(out _);

		int moveCount = 0;
		app.AddObserver<On<UiMove>>(_ => moveCount++);

		app.RunStartup();
		app.GetResource<UiPointer>().Position = new Vector2(50, 50);
		app.Update();

		Assert.Equal(0, moveCount);
	}

	[Fact]
	public void Move_does_not_fire_when_pointer_stationary()
	{
		var app = MakeInteractiveApp(out _);

		int moveCount = 0;
		app.AddObserver<On<UiMove>>(_ => moveCount++);

		app.RunStartup();
		var pointer = app.GetResource<UiPointer>();
		pointer.Position = new Vector2(50, 50);
		app.Update(); // becomes hovered
		app.Update(); // same position → no Move

		Assert.Equal(0, moveCount);
	}

	[Fact]
	public void Scroll_dispatched_to_hovered_entity()
	{
		var app = MakeInteractiveApp(out var idOf);

		int scrollCount = 0;
		Vector2 lastDelta = default;
		ulong lastEntity = 0;
		app.AddObserver<On<UiScroll>>(trigger =>
		{
			scrollCount++;
			lastDelta = trigger.Event.Delta;
			lastEntity = trigger.EntityId;
		});

		app.RunStartup();
		app.GetResource<UiPointer>().Position = new Vector2(50, 50);
		app.GetResource<UiClayContext>().ScrollDelta = new Vector2(0, 3);
		app.Update();

		Assert.Equal(1, scrollCount);
		Assert.Equal(new Vector2(0, 3), lastDelta);
		Assert.Equal(idOf(), lastEntity);
	}

	[Fact]
	public void Scroll_not_fired_when_no_entity_hovered()
	{
		var app = MakeInteractiveApp(out _);

		int scrollCount = 0;
		app.AddObserver<On<UiScroll>>(_ => scrollCount++);

		app.RunStartup();
		app.GetResource<UiPointer>().Position = new Vector2(500, 500); // off element
		app.GetResource<UiClayContext>().ScrollDelta = new Vector2(0, 3);
		app.Update();

		Assert.Equal(0, scrollCount);
	}

	[Fact]
	public void Scroll_not_fired_when_delta_zero()
	{
		var app = MakeInteractiveApp(out _);

		int scrollCount = 0;
		app.AddObserver<On<UiScroll>>(_ => scrollCount++);

		app.RunStartup();
		app.GetResource<UiPointer>().Position = new Vector2(50, 50);
		// ScrollDelta left at default (zero).
		app.Update();

		Assert.Equal(0, scrollCount);
	}
}

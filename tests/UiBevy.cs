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
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(200), Height = Val.Px(100) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true });
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		int clickCount = 0;
		app.AddObserver<On<UiClick>>((world, trigger) => clickCount++);

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
				.Insert(new UiRoot())
				.Insert(new UiNode { Display = Display.Flex, Width = Val.Px(100), Height = Val.Px(50) })
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true });
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		int clickCount = 0;
		app.AddObserver<On<UiClick>>((world, trigger) => clickCount++);

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
	public void OverflowScroll_registers_scroll_container()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			var root = c.Spawn()
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
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
				.Insert(new UiRoot())
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
	public void ComputedNode_written_after_layout()
	{
		var app = MakeApp();
		app.AddSystem((Commands c) =>
		{
			c.Spawn()
				.Insert(new UiRoot())
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
}

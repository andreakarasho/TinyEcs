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

// See UiBevyTests: Clay's process-global context forbids parallel UI tests.
[Collection("ClayUi")]
public class UiWindowTests
{
	private static App MakeApp()
	{
		var world = new World();
		var app = new App(world, ThreadingMode.Single);
		app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600) });
		app.AddPlugin(new UiWindowPlugin());
		return app;
	}

	private static ulong SpawnWindow(App app, float x, float y, float w = 200, float h = 100, bool noDrag = false)
	{
		ulong id = 0;
		app.AddSystem((Commands c) =>
		{
			var e = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(x), Top = Val.Px(y),
					Width = Val.Px(w), Height = Val.Px(h),
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new UiMovable())
				.Insert(new GlobalZIndex(1));
			if (noDrag)
				e.Insert(new UiMovableNoDrag());
			id = e.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();
		app.RunStartup();
		return id;
	}

	[Fact]
	public void Drag_moves_window_and_bumps_z()
	{
		var app = MakeApp();
		var win = SpawnWindow(app, 100, 100);

		var pointer = app.GetResource<UiPointer>();

		// Press inside the window.
		pointer.Position = new Vector2(150, 130);
		pointer.Down = true;
		app.Update();

		// Drag 40 px right, 20 px down.
		pointer.Position = new Vector2(190, 150);
		app.Update();

		var node = app.GetWorld().Entity(win).Get<UiNode>();
		Assert.Equal(140f, node.Left.Value);
		Assert.Equal(120f, node.Top.Value);

		Assert.True(app.GetWorld().Entity(win).Get<GlobalZIndex>().Value >= 1); // bumped on latch

		// Release stops tracking.
		pointer.Down = false;
		app.Update();
		pointer.Position = new Vector2(400, 400);
		app.Update();
		node = app.GetWorld().Entity(win).Get<UiNode>();
		Assert.Equal(140f, node.Left.Value);
	}

	[Fact]
	public void NoDrag_window_stays_put()
	{
		var app = MakeApp();
		var win = SpawnWindow(app, 100, 100, noDrag: true);

		var pointer = app.GetResource<UiPointer>();
		pointer.Position = new Vector2(150, 130);
		pointer.Down = true;
		app.Update();
		pointer.Position = new Vector2(250, 230);
		app.Update();

		var node = app.GetWorld().Entity(win).Get<UiNode>();
		Assert.Equal(100f, node.Left.Value);
		Assert.Equal(100f, node.Top.Value);
	}

	[Fact]
	public void Press_on_bare_canvas_drags_nothing()
	{
		var app = MakeApp();
		var win = SpawnWindow(app, 100, 100);

		var pointer = app.GetResource<UiPointer>();
		// Press outside the window, then sweep over it while held.
		pointer.Position = new Vector2(500, 500);
		pointer.Down = true;
		app.Update();
		pointer.Position = new Vector2(150, 130);
		app.Update();
		pointer.Position = new Vector2(190, 150);
		app.Update();

		var node = app.GetWorld().Entity(win).Get<UiNode>();
		Assert.Equal(100f, node.Left.Value);
	}

	[Fact]
	public void ForcedDrag_latches_spawned_window_under_cursor()
	{
		var app = MakeApp();
		var win = SpawnWindow(app, 100, 100);

		var pointer = app.GetResource<UiPointer>();
		pointer.Position = new Vector2(300, 300);
		pointer.Down = true;
		app.Update();

		app.GetResource<ForcedWindowDrag>().Owner = win;
		app.Update(); // latch: window re-centers under the cursor

		var node = app.GetWorld().Entity(win).Get<UiNode>();
		Assert.Equal(200f, node.Left.Value);  // 300 - 200/2
		Assert.Equal(250f, node.Top.Value);   // 300 - 100/2

		pointer.Position = new Vector2(320, 310);
		app.Update();
		node = app.GetWorld().Entity(win).Get<UiNode>();
		Assert.Equal(220f, node.Left.Value);
	}
}

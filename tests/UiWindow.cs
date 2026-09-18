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

// The z-order bookkeeping UiWindowPlugin composes: a declared GlobalZIndex is a
// floor that ratchets the counter (never down), a negative value is a topmost
// request resolved to the counter's current top at insert time.
//
// App.RunStartup() is ONE-SHOT: a second call returns without executing the
// Startup stage (silently — a spawn system added after the first run never
// runs until an Update-stage system covers the case). Every test below
// registers all of its spawn systems first and calls RunStartup exactly once;
// the runtime-open case (a menu that appears mid-session) uses a Stage.Update
// spawner + app.Update() instead.
[Collection("ClayUi")]
public class UiWindowZOrderTests
{
	private static App MakeApp()
	{
		var world = new World();
		var app = new App(world, ThreadingMode.Single);
		app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600) });
		app.AddPlugin(new UiWindowPlugin());
		return app;
	}

	private sealed class IdBox { public ulong Id; }

	private static IdBox AddSpawn(App app, float x, float y, int z) => AddSpawn(app, x, y, z, BevyStage.Startup);

	// `stage` Update = a runtime spawn (guarded to fire once): two roots spawned in
	// the SAME flush see each other in an order the observers do not define, so a
	// test about "which existed first" must put them in separate flushes.
	private static IdBox AddSpawn(App app, float x, float y, int z, BevyStage stage)
	{
		var box = new IdBox();
		app.AddSystem((Commands c) =>
		{
			if (box.Id != 0) return;
			var e = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(x), Top = Val.Px(y),
					Width = Val.Px(200), Height = Val.Px(100),
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new UiMovable())
				.Insert(new GlobalZIndex(z));
			box.Id = e.Id;
		})
		.InStage(stage).SingleThreaded().Build();
		return box;
	}

	[Fact]
	public void A_declared_floor_ratchets_the_counter_up_and_never_down()
	{
		var app = MakeApp();
		var counter = app.GetResource<UiZCounter>();
		var world = app.GetWorld();

		// Low first, then high in a later flush: neither spawn is covered by an
		// existing window, so both keep their declared value and only the counter moves.
		var low = AddSpawn(app, 30, 10, z: 30);
		var high = AddSpawn(app, 10, 10, z: 60, BevyStage.Update);
		app.RunStartup();
		app.Update();

		Assert.Equal(61, counter.Next);
		Assert.Equal(60, world.Entity(high.Id).Get<GlobalZIndex>().Value);
		Assert.Equal(30, world.Entity(low.Id).Get<GlobalZIndex>().Value);

		// A lower floor never moves the counter back down.
		counter.Next = 5;
		Assert.Equal(61, counter.Next);
	}

	[Fact]
	public void A_root_spawning_under_an_existing_window_is_lifted_to_the_top()
	{
		var app = MakeApp();
		var counter = app.GetResource<UiZCounter>();
		var world = app.GetWorld();

		// High first, then a root declaring a floor some other window already sits
		// above: a tie or an under-spawn would pick the wrong window in the pixel
		// pick, so the newcomer takes the counter's top instead (what gumps do).
		var high = AddSpawn(app, 10, 10, z: 60);
		var low = AddSpawn(app, 30, 10, z: 30, BevyStage.Update);
		app.RunStartup();
		app.Update();

		Assert.Equal(60, world.Entity(high.Id).Get<GlobalZIndex>().Value);
		Assert.Equal(61, world.Entity(low.Id).Get<GlobalZIndex>().Value);
		Assert.Equal(62, counter.Next);
	}

	[Fact]
	public void A_negative_z_is_a_topmost_request_resolved_at_insert()
	{
		var app = MakeApp();
		var counter = app.GetResource<UiZCounter>();
		var world = app.GetWorld();

		var win = AddSpawn(app, 10, 10, z: 60);
		var menu = AddSpawn(app, 40, 20, z: -1);
		app.RunStartup();

		var winZ = world.Entity(win.Id).Get<GlobalZIndex>().Value;
		var menuZ = world.Entity(menu.Id).Get<GlobalZIndex>().Value;
		Assert.Equal(60, winZ);   // the floor itself is never rewritten
		Assert.Equal(61, menuZ);  // resolved to the counter's top after the floor
		Assert.True(menuZ > winZ);

		// Runtime open (the real context-menu case): the next request lands above
		// the first — the counter has moved past it, so the newest requester is
		// the topmost thing on screen.
		var menu2 = new IdBox();
		app.AddSystem((Commands c) =>
		{
			menu2.Id = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(50), Top = Val.Px(30),
					Width = Val.Px(200), Height = Val.Px(100),
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new UiMovable())
				.Insert(new GlobalZIndex(-1)).Id;
		})
		.InStage(BevyStage.Update).SingleThreaded().Build();
		app.Update();

		var menu2Z = world.Entity(menu2.Id).Get<GlobalZIndex>().Value;
		Assert.True(menu2Z > menuZ);
		Assert.Equal(counter.Next, menu2Z + 1);
	}
}

// The shrink-only correction that keeps floating windows inside the UI surface
// (the host's GumpBoundsPlugin used to own the window half; it is the library's
// UiSurfaceBoundsPlugin now).
[Collection("ClayUi")]
public class UiSurfaceBoundsTests
{
	private static App MakeApp()
	{
		var world = new World();
		var app = new App(world, ThreadingMode.Single);
		app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600) });
		app.AddPlugin(new UiSurfaceBoundsPlugin());
		return app;
	}

	private static ulong SpawnWindow(App app, float x, float y)
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
					Width = Val.Px(200), Height = Val.Px(100),
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new UiMovable())
				.Insert(new GlobalZIndex(1));
			id = e.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();
		app.RunStartup();
		app.Update(); // layout: the ComputedNode the clamp reads from
		return id;
	}

	[Fact]
	public void A_shrunk_surface_clamps_movable_windows_inside_it()
	{
		var app = MakeApp();
		var win = SpawnWindow(app, 500, 300);
		var world = app.GetWorld();

		var comp = world.Entity(win).Get<ComputedNode>();
		Assert.Equal(200f, comp.Size.X);
		Assert.Equal(100f, comp.Size.Y);

		// The surface shrinks to 300x200: the window at (500,300) is stranded
		// off-screen. It must come back fully inside [0, surface - size].
		app.GetResource<UiSurface>().LogicalSize = new Vector2(300, 200);
		app.Update();
		app.Update();

		var node = world.Entity(win).Get<UiNode>();
		Assert.Equal(100f, node.Left.Value); // 300 - 200
		Assert.Equal(100f, node.Top.Value);  // 200 - 100
	}

	[Fact]
	public void A_growing_surface_never_moves_windows()
	{
		var app = MakeApp();
		var win = SpawnWindow(app, 50, 40);
		var world = app.GetWorld();

		app.GetResource<UiSurface>().LogicalSize = new Vector2(1600, 1200);
		app.Update();
		app.Update();

		var node = world.Entity(win).Get<UiNode>();
		Assert.Equal(50f, node.Left.Value);
		Assert.Equal(40f, node.Top.Value);
	}
}

using System.Numerics;
using Clay;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;
using Xunit;
using ClayColor = Clay.Color;
using UiNode = TinyEcs.Bevy.UI.Node;
using UiText = TinyEcs.Bevy.UI.Text;
using BevyStage = TinyEcs.Bevy.Stage;

namespace TinyEcs.Tests;

// See UiBevyTests: Clay's process-global context forbids parallel UI tests.
[Collection("ClayUi")]
public class UiInteractionExtrasTests
{
	private static App MakeApp()
	{
		var world = new World();
		var app = new App(world, ThreadingMode.Single);
		app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600) });
		return app;
	}

	private static ulong SpawnBox(App app, float x, float y, System.Action<EntityCommands> extra = null)
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
					Width = Val.Px(100), Height = Val.Px(50),
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true });
			extra?.Invoke(e);
			id = e.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();
		app.RunStartup();
		return id;
	}

	[Fact]
	public void HoverIntent_fires_after_delay_and_ends_on_leave()
	{
		var app = MakeApp();
		var box = SpawnBox(app, 100, 100, e => e.Insert(new UiHoverIntent { DelayMs = 100 }));

		var started = 0; var ended = 0;
		Vector2 startPos = default;
		app.AddObserver<On<UiHoverStart>>(t => { started++; startPos = t.Event.Position; });
		app.AddObserver<On<UiHoverEnd>>(_ => ended++);

		var pointer = app.GetResource<UiPointer>();
		var time = app.GetResource<Time>();
		pointer.Position = new Vector2(150, 120);

		app.Update(); // hover begins, clock at 0
		Assert.Equal(0, started);

		time.Total = 50f;
		app.Update();
		Assert.Equal(0, started); // still under the delay

		time.Total = 150f;
		app.Update();
		Assert.Equal(1, started);
		Assert.Equal(new Vector2(150, 120), startPos);

		time.Total = 500f;
		app.Update();
		Assert.Equal(1, started); // fires once per rest

		pointer.Position = new Vector2(500, 500);
		app.Update();
		Assert.Equal(1, ended);
	}

	[Fact]
	public void HoverIntent_does_not_fire_for_untagged_entity()
	{
		var app = MakeApp();
		SpawnBox(app, 100, 100); // no UiHoverIntent

		var started = 0;
		app.AddObserver<On<UiHoverStart>>(_ => started++);

		var pointer = app.GetResource<UiPointer>();
		var time = app.GetResource<Time>();
		pointer.Position = new Vector2(150, 120);
		app.Update();
		time.Total = 10_000f;
		app.Update();

		Assert.Equal(0, started);
	}

	[Fact]
	public void ContainsByBounds_skips_pixel_hit_test()
	{
		var app = MakeApp();
		var solid = SpawnBox(app, 100, 100, e => e.Insert(new UiContainsByBounds()));
		var ghost = SpawnBox(app, 300, 100);

		// Host hook rejects EVERYTHING — only UiContainsByBounds elements
		// should still register hover/clicks.
		app.GetResource<UiClayContext>().PixelHitTest = (_, _, _) => false;

		var clicks = 0;
		ulong clicked = 0;
		app.AddObserver<On<UiClick>>(t => { clicks++; clicked = t.EntityId; });

		var pointer = app.GetResource<UiPointer>();

		// Click the marked box: lands.
		pointer.Position = new Vector2(150, 120);
		pointer.Down = true; app.Update();
		pointer.Down = false; app.Update();
		Assert.Equal(1, clicks);
		Assert.Equal(solid, clicked);

		// Click the unmarked box: rejected by the hook, no click.
		pointer.Position = new Vector2(350, 120);
		pointer.Down = true; app.Update();
		pointer.Down = false; app.Update();
		Assert.Equal(1, clicks);
		Assert.NotEqual(ghost, clicked);
	}

	[Fact]
	public void CheckboxLabel_click_toggles_target()
	{
		var app = MakeApp();
		app.AddPlugin(new CheckboxPlugin());

		ulong boxId = 0, labelId = 0;
		app.AddSystem((Commands c) =>
		{
			boxId = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(100), Top = Val.Px(100),
					Width = Val.Px(20), Height = Val.Px(20),
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new Checkbox())
				.Id;

			labelId = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(140), Top = Val.Px(100),
					Width = Val.Px(80), Height = Val.Px(20),
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new CheckboxLabel { Target = boxId })
				.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();

		var changes = 0;
		app.AddObserver<On<CheckboxChanged>>(_ => changes++);
		app.RunStartup();

		var pointer = app.GetResource<UiPointer>();
		pointer.Position = new Vector2(160, 110); // on the LABEL
		pointer.Down = true; app.Update();
		pointer.Down = false; app.Update();

		Assert.Equal(1, changes);
		Assert.True(app.GetWorld().Entity(boxId).Get<Checkbox>().Checked);
	}
}

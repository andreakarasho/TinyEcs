using System.Numerics;
using Clay;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Input;
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
public class UiTextFieldTests
{
	private static App MakeApp()
	{
		var world = new World();
		var app = new App(world, ThreadingMode.Single);
		app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600) });
		app.AddPlugin(new TextFieldPlugin());
		return app;
	}

	private static ulong SpawnField(App app, string initial = "", float x = 100, float y = 100)
	{
		ulong id = 0;
		app.AddSystem((Commands c) =>
		{
			id = c.Spawn()
				.Insert(new UiNode
				{
					Display = Display.Flex,
					PositionType = PositionType.Absolute,
					Left = Val.Px(x), Top = Val.Px(y),
					Width = Val.Px(200), Height = Val.Px(30),
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new UiText(initial))
				.Insert(new TextField())
				.Id;
		})
		.InStage(BevyStage.Startup).SingleThreaded().Build();
		app.RunStartup();
		return id;
	}

	// External SendEvent lands in the channel's write buffer; the end-of-frame
	// flush promotes it, so readers see it one Update later - hence two Updates.
	private static void Type(App app, string text)
	{
		foreach (var ch in text)
			app.SendEvent(new CharInput { Value = ch });
		app.Update();
		app.Update();
	}

	private static void Click(App app, float x, float y)
	{
		var pointer = app.GetResource<UiPointer>();
		pointer.Position = new Vector2(x, y);
		pointer.Down = true;
		app.Update();
		pointer.Down = false;
		app.Update();
	}

	private static void PressKey(App app, KeyCode key, params KeyCode[] held)
	{
		var kb = app.GetResource<KeyboardInput>();
		// Modifiers must be down for two frames before the key edge — IsPressed
		// (the ctrl/shift checks) requires both old and new state down.
		if (held.Length > 0)
		{
			kb.SetSnapshot(held);
			kb.Update(0f);
			app.Update();
		}
		var down = new KeyCode[held.Length + 1];
		held.CopyTo(down, 0);
		down[held.Length] = key;
		kb.SetSnapshot(down);
		kb.Update(0f);
		app.Update();
		kb.SetSnapshot([]);
		kb.Update(0f);
		app.Update();
	}

	[Fact]
	public void Click_focuses_and_typing_edits_text()
	{
		var app = MakeApp();
		var field = SpawnField(app);

		Click(app, 110, 110);
		Assert.Equal(field, app.GetResource<TextFieldEditor>().Focused);

		Type(app, "hi");

		Assert.Equal("hi", app.GetWorld().Entity(field).Get<UiText>().Value);
	}

	[Fact]
	public void Backspace_deletes_and_changed_fires()
	{
		var app = MakeApp();
		var field = SpawnField(app);
		var changed = 0;
		app.AddObserver<On<TextFieldChanged>>(_ => changed++);

		Click(app, 110, 110);
		Type(app, "ab");
		Assert.Equal("ab", app.GetWorld().Entity(field).Get<UiText>().Value);
		Assert.Equal(1, changed);

		PressKey(app, KeyCode.Back);
		Assert.Equal("a", app.GetWorld().Entity(field).Get<UiText>().Value);
		Assert.Equal(2, changed);
	}

	[Fact]
	public void Home_then_typing_inserts_at_caret()
	{
		var app = MakeApp();
		var field = SpawnField(app, initial: "world");

		// Click at the field's right side: caret lands at the end.
		Click(app, 290, 110);
		PressKey(app, KeyCode.Home);
		Type(app, ">");

		Assert.Equal(">world", app.GetWorld().Entity(field).Get<UiText>().Value);
	}

	[Fact]
	public void Blur_on_outside_press_stops_editing()
	{
		var app = MakeApp();
		var field = SpawnField(app, initial: "keep");

		Click(app, 110, 110);
		Assert.Equal(field, app.GetResource<TextFieldEditor>().Focused);

		Click(app, 500, 500); // bare canvas
		Assert.Equal(0ul, app.GetResource<TextFieldEditor>().Focused);

		Type(app, "x");
		Assert.Equal("keep", app.GetWorld().Entity(field).Get<UiText>().Value);
	}

	[Fact]
	public void Enter_fires_submit_with_value()
	{
		var app = MakeApp();
		var field = SpawnField(app);
		string submitted = null;
		app.AddObserver<On<TextFieldSubmit>>(t => submitted = t.Event.Value);

		Click(app, 110, 110);
		Type(app, "go");
		PressKey(app, KeyCode.Enter);

		Assert.Equal("go", submitted);
	}

	[Fact]
	public void SelectAll_then_typing_replaces()
	{
		var app = MakeApp();
		var field = SpawnField(app, initial: "old text");

		Click(app, 110, 110);
		PressKey(app, KeyCode.A, KeyCode.LeftControl);
		Type(app, "n");

		Assert.Equal("n", app.GetWorld().Entity(field).Get<UiText>().Value);
	}
}

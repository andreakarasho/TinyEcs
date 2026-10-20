using System.Numerics;
using Clay;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Input;
using TinyEcs.Bevy.UI;
using Xunit;
using ClayColor = Clay.Color;
using UiNode = TinyEcs.Bevy.UI.Node;
using UiText = TinyEcs.Bevy.UI.Text;
using BevyStage = TinyEcs.Bevy.Stage;

namespace TinyEcs.Tests;

// The text editor (UiTextEditPlugin) on a SELF-EDITABLE node: one entity with
// Text + TextInput + EditableText, focused by a press on it.
// See UiBevyTests: Clay's process-global context forbids parallel UI tests.
[Collection("ClayUi")]
public class UiTextFieldTests
{
	private static App MakeApp()
	{
		var world = new World();
		var app = new App(world, ThreadingMode.Single);
		app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600) });
		app.AddPlugin(new UiTextEditPlugin());
		return app;
	}

	private static ulong SpawnField(App app, string initial = "", float x = 100, float y = 100, bool masked = false)
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
					Width = Val.Px(200), Height = Val.Px(30),
				})
				.Insert(new BackgroundColor(ClayColor.White))
				.Insert(new Interaction())
				.Insert(new FocusPolicy { Block = true })
				.Insert(new UiText(masked ? string.Empty : initial))
				.Insert<TextInput>()
				.Insert<EditableText>();
			if (masked) e.Insert(new MaskedText { Value = initial, MaskChar = '*' });
			id = e.Id;
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
		// One more idle frame so the release edge is history (the editor drops the
		// text of a Ctrl chord whose letter was down or just released).
		kb.Update(0f);
		app.Update();
	}

	private static string Value(App app, ulong field) => app.GetWorld().Entity(field).Get<UiText>().Value;

	[Fact]
	public void Click_focuses_and_typing_edits_text()
	{
		var app = MakeApp();
		var field = SpawnField(app);

		Click(app, 110, 110);
		Assert.Equal(field, app.GetResource<InputFocus>().Entity);
		Assert.Equal(field, app.GetResource<ActiveTextEdit>().Entity);

		Type(app, "hi");

		Assert.Equal("hi", Value(app, field));
	}

	[Fact]
	public void Backspace_deletes_and_changed_fires()
	{
		var app = MakeApp();
		var field = SpawnField(app);
		var changed = 0;
		app.AddObserver<On<TextEditChanged>>(_ => changed++);

		Click(app, 110, 110);
		Type(app, "ab");
		Assert.Equal("ab", Value(app, field));
		Assert.Equal(1, changed);

		Type(app, "\b");
		Assert.Equal("a", Value(app, field));
		Assert.Equal(2, changed);
	}

	[Fact]
	public void Home_then_typing_inserts_at_caret()
	{
		var app = MakeApp();
		var field = SpawnField(app, initial: "world");

		Click(app, 290, 110);
		PressKey(app, KeyCode.Home);
		Type(app, ">");

		Assert.Equal(">world", Value(app, field));
	}

	[Fact]
	public void Blur_on_outside_press_stops_editing()
	{
		var app = MakeApp();
		var field = SpawnField(app, initial: "keep");

		Click(app, 110, 110);
		Assert.Equal(field, app.GetResource<InputFocus>().Entity);

		Click(app, 500, 500); // bare canvas
		Assert.Equal(0ul, app.GetResource<InputFocus>().Entity);

		Type(app, "x");
		Assert.Equal("keep", Value(app, field));
	}

	[Fact]
	public void Enter_fires_submit_with_value()
	{
		var app = MakeApp();
		SpawnField(app);
		string? submitted = null;
		app.AddObserver<On<TextEditSubmit>>(t => submitted = t.Event.Value);

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

		Assert.Equal("n", Value(app, field));
	}

	[Fact]
	public void Masked_field_edits_the_secret_and_shows_the_mask()
	{
		var app = MakeApp();
		var field = SpawnField(app, initial: "pw", masked: true);

		Click(app, 110, 110);
		PressKey(app, KeyCode.End);
		Type(app, "x");

		Assert.Equal("pwx", app.GetWorld().Entity(field).Get<MaskedText>().Value);
		app.Update(); // the mask mirrors in the next PreUpdate
		Assert.Equal("***", Value(app, field));
	}

	[Fact]
	public void Copy_paste_goes_through_the_clipboard_resource()
	{
		var app = MakeApp();
		var backend = new FakeClipboard();
		app.GetResource<UiClipboard>().Backend = backend;
		var field = SpawnField(app, initial: "abc");

		Click(app, 110, 110);
		PressKey(app, KeyCode.A, KeyCode.LeftControl);
		PressKey(app, KeyCode.C, KeyCode.LeftControl);
		Assert.Equal("abc", backend.Text);

		PressKey(app, KeyCode.End);
		PressKey(app, KeyCode.V, KeyCode.LeftControl);
		Assert.Equal("abcabc", Value(app, field));
	}

	[Fact]
	public void Refused_os_write_keeps_the_copy_in_app()
	{
		var clip = new UiClipboard { Backend = new FakeClipboard { Refuse = true } };
		clip.Text = "mine";
		Assert.True(clip.OsRefusedWrite);
		Assert.Equal("mine", clip.Text);
	}

	[Fact]
	public void Focused_field_consumes_typing_keys_but_not_enter()
	{
		var app = MakeApp();
		SpawnField(app);
		Click(app, 110, 110);

		var kb = app.GetResource<KeyboardInput>();
		var seen = new List<string>();
		app.AddSystem((Res<KeyboardInput> k) =>
		{
			if (k.Value.IsPressedOnce(KeyCode.W)) seen.Add("W");
			if (k.Value.IsPressedOnce(KeyCode.Enter)) seen.Add("Enter");
		}).InStage(BevyStage.Update).Build();

		kb.SetSnapshot([KeyCode.W]); kb.Update(0f); app.Update();
		kb.SetSnapshot([]); kb.Update(0f); app.Update();
		kb.SetSnapshot([KeyCode.Enter]); kb.Update(0f); app.Update();

		Assert.Equal(new[] { "Enter" }, seen);
	}

	[Fact]
	public void Wrapped_multiline_field_spawned_by_UiTextField_inserts_newlines()
	{
		var app = MakeApp();
		ulong glyph = 0;
		app.AddSystem((Commands c, Res<ActiveTextEdit> edit) =>
		{
			var frame = c.Spawn().Insert(new UiNode
			{
				PositionType = PositionType.Absolute,
				Left = Val.Px(50), Top = Val.Px(50), Width = Val.Px(300), Height = Val.Px(200),
			}).Insert(new BackgroundColor(ClayColor.White));
			glyph = UiTextField.Spawn(c, edit.Value.Measurer, frame, new UiTextFieldOptions
			{
				Font = new TextFont { FontId = 0, Size = 16 },
				Color = ClayColor.Black,
				Initial = "a",
				Multiline = true,
				WrapWidth = 280,
			});
		}).InStage(BevyStage.Startup).SingleThreaded().Build();
		app.RunStartup();
		app.Update();

		app.GetResource<InputFocus>().Entity = glyph;
		app.Update();
		PressKey(app, KeyCode.End);
		PressKey(app, KeyCode.Enter);
		Type(app, "b");

		Assert.Equal("a\nb", app.GetWorld().Entity(glyph).Get<MultilineText>().Value);
	}

	private sealed class FakeClipboard : IClipboardBackend
	{
		public string Text = string.Empty;
		public bool Refuse;
		public string? Get() => Text;
		public bool Set(string text)
		{
			if (Refuse) return false;
			Text = text;
			return true;
		}
	}
}

using System.Text;
using Clay;
using StbTextEdit;

namespace TinyEcs.Bevy.UI.Widgets;

// Single-line editable text field. Place `TextField` on an entity that also
// carries `Text` + `Interaction` (+ optionally `TextFont`). TextFieldPlugin
// gives it focus-on-click, caret placement + drag selection (through the
// layout's ComputedNode + the app's ITextMeasurer), stb_textedit key
// navigation (arrows/Home/End/shift-selection/Ctrl+A-C-X-V-Z), CharInput
// typing and write-back into `Text`.
//
// Caret/selection VISUALS are deliberately not drawn here — rendering is
// host-specific. Read TextFieldEditor (Focused/Caret/Selection/CaretX) to
// paint them. Multiline, masking and bespoke fonts-with-kerning stay host-side
// (see ClassicUO's TextEditPlugin for a full example).

/// <summary>Editable single-line field marker. MaxLength 0 = unbounded.</summary>
public struct TextField
{
	public int MaxLength;
}

/// <summary>Entity trigger: the field's value changed this frame.</summary>
public struct TextFieldChanged
{
	public string Value;
}

/// <summary>Entity trigger: Enter pressed while the field was focused.</summary>
public struct TextFieldSubmit
{
	public string Value;
}

/// <summary>The focused field's editor state. Implements stb_textedit's
/// handler over a StringBuilder, measuring through the app's ITextMeasurer so
/// caret/click/selection x positions line up with rendered glyphs.</summary>
public sealed class TextFieldEditor : ITextEditHandler
{
	internal ulong Entity;
	internal ushort FontId;
	internal ushort FontSize = 16;
	internal int MaxLength;
	internal ITextMeasurer? Measurer;
	internal readonly TextEditState State = new(singleLine: true);
	internal readonly StringBuilder Buffer = new();
	internal bool MouseSelecting;

	// In-app clipboard (Ctrl+C/X/V). Not the OS clipboard — keeps the widget
	// platform-free; hosts wanting cross-app paste can pre-load it.
	public static string Clipboard = string.Empty;

	/// <summary>Focused field entity, 0 = none.</summary>
	public ulong Focused => Entity;

	public int Caret => State.Cursor;

	public (int Start, int End) Selection
	{
		get
		{
			var a = State.SelectStart; var b = State.SelectEnd;
			return a <= b ? (a, b) : (b, a);
		}
	}

	public string Value => Buffer.ToString();

	/// <summary>Caret x offset in px from the field's text origin.</summary>
	public float CaretX() => WidthUpTo(State.Cursor);

	internal float WidthUpTo(int count)
	{
		if (count <= 0 || Measurer == null) return 0;
		if (count > Buffer.Length) count = Buffer.Length;
		Span<char> tmp = count <= 256 ? stackalloc char[count] : new char[count];
		Buffer.CopyTo(0, tmp, count);
		return Measurer.MeasureText(tmp, FontId, FontSize, 0).Width;
	}

	// ── ITextEditHandler ──────────────────────────────────────────────
	public int Length => Buffer.Length;
	public char GetChar(int index) => Buffer[index];

	public float GetCharWidth(int lineStartIndex, int charIndex)
	{
		if (charIndex < 0 || charIndex >= Buffer.Length) return 0;
		return WidthUpTo(charIndex + 1) - WidthUpTo(charIndex);
	}

	public void LayoutRow(out TextEditRow row, int lineStartIndex)
	{
		// Single-line: the whole buffer is one row. YMax must be > 0 (clicks
		// pass y=0) or stb's Click can't locate the row.
		float h = Measurer?.MeasureText("Wg", FontId, FontSize, 0).Height ?? FontSize;
		if (h <= 0) h = FontSize;
		row = new TextEditRow
		{
			X0 = 0,
			X1 = WidthUpTo(Buffer.Length),
			BaselineYDelta = h,
			YMin = 0,
			YMax = h,
			NumChars = Buffer.Length,
		};
	}

	public bool InsertChars(int index, ReadOnlySpan<char> chars)
	{
		if (MaxLength > 0 && Buffer.Length + chars.Length > MaxLength)
		{
			var room = MaxLength - Buffer.Length;
			if (room <= 0) return false;
			chars = chars[..room];
		}
		Buffer.Insert(index, chars.ToString());
		return true;
	}

	public void DeleteChars(int index, int count) => Buffer.Remove(index, count);
}

public sealed class TextFieldPlugin : IPlugin
{
	public void Build(App app)
	{
		// Keyboard edges + CharInput come from TinyEcs.Bevy.Input; install-once
		// in case the host hasn't already.
		app.AddPlugin(new Input.InputPlugin());
		app.AddResource(new TextFieldEditor());

		// Focus + caret-from-click. Runs in UiPostLayoutStage after
		// InteractionSystem.PostLayout (declaration order), so PressedEntity and
		// ComputedNode are fresh for the frame the press landed.
		app.AddSystem((
			Res<UiPointer> pointer,
			Res<UiClayContext> ctx,
			Res<UiTextMeasure> measure,
			ResMut<TextFieldEditor> editor,
			Local<bool> prevDown,
			Query<Data<TextField, Text>> fields,
			Query<Data<TextFont>> fonts,
			Query<Data<ComputedNode>> computed) =>
			FocusAndCaret(pointer, ctx, measure, editor, prevDown, fields, fonts, computed))
			.InStage(UiPlugin.UiPostLayoutStage).SingleThreaded().Build();

		var routeKeysFn = RouteKeys;
		var routeCharsFn = RouteChars;
		var writeBackFn = WriteBack;
		app.AddSystem(routeKeysFn).InStage(Stage.Update).Build();
		app.AddSystem(routeCharsFn).InStage(Stage.Update).Build();
		app.AddSystem(writeBackFn).InStage(Stage.Update).Build();
	}

	private static void FocusAndCaret(
		Res<UiPointer> pointer,
		Res<UiClayContext> ctx,
		Res<UiTextMeasure> measure,
		ResMut<TextFieldEditor> editor,
		Local<bool> prevDown,
		Query<Data<TextField, Text>> fields,
		Query<Data<TextFont>> fonts,
		Query<Data<ComputedNode>> computed)
	{
		var ed = editor.Value;
		ref readonly var p = ref pointer.Value;
		bool pressEdge = p.Down && !prevDown.Value;
		prevDown.Value = p.Down;

		ed.Measurer ??= measure.Value.Measurer;

		if (!p.Down)
			ed.MouseSelecting = false;

		if (pressEdge)
		{
			var pressed = ctx.Value.PressedEntity;
			if (pressed != 0 && fields.Contains(pressed))
			{
				if (ed.Entity != pressed)
				{
					var (_, field, text) = fields.Get(pressed);
					ed.Entity = pressed;
					ed.MaxLength = field.Ref.MaxLength;
					ed.FontId = 0; ed.FontSize = 16;
					if (fonts.Contains(pressed))
					{
						var (_, f) = fonts.Get(pressed);
						ed.FontId = f.Ref.FontId;
						ed.FontSize = f.Ref.Size;
					}
					ed.Buffer.Clear();
					ed.Buffer.Append(text.Ref.Value ?? string.Empty);
					ed.State.Initialize(singleLine: true);
					ed.State.Cursor = ed.Buffer.Length;
				}

				// Caret at the clicked glyph.
				if (computed.Contains(pressed))
				{
					var (_, cn) = computed.Get(pressed);
					TextEdit.Click(ed, ed.State, p.Position.X - cn.Ref.Position.X, 0);
					ed.MouseSelecting = true;
				}
			}
			else
			{
				ed.Entity = 0; // pressed elsewhere (or bare canvas) — blur
			}
		}
		else if (ed.MouseSelecting && p.Down && ed.Entity != 0 && computed.Contains(ed.Entity))
		{
			var (_, cn) = computed.Get(ed.Entity);
			TextEdit.Drag(ed, ed.State, p.Position.X - cn.Ref.Position.X, 0);
		}
	}

	private static void RouteKeys(
		Commands commands,
		Res<Input.KeyboardInput> kb,
		ResMut<TextFieldEditor> editor)
	{
		var ed = editor.Value;
		if (ed.Entity == 0) return;

		bool shift = kb.Value.IsPressed(Input.KeyCode.LeftShift) || kb.Value.IsPressed(Input.KeyCode.RightShift);
		bool ctrl = kb.Value.IsPressed(Input.KeyCode.LeftControl) || kb.Value.IsPressed(Input.KeyCode.RightControl);

		if (ctrl)
		{
			if (kb.Value.IsPressedOnce(Input.KeyCode.A)) { TextEdit.Key(ed, ed.State, TextEditKey.TextStart); TextEdit.Key(ed, ed.State, TextEditKey.TextEnd, shift: true); }
			if (kb.Value.IsPressedOnce(Input.KeyCode.C)) { var s = Selected(ed); if (s.Length > 0) TextFieldEditor.Clipboard = s; }
			if (kb.Value.IsPressedOnce(Input.KeyCode.X)) { var s = Selected(ed); if (s.Length > 0) { TextFieldEditor.Clipboard = s; TextEdit.Cut(ed, ed.State); } }
			if (kb.Value.IsPressedOnce(Input.KeyCode.V) && TextFieldEditor.Clipboard.Length > 0) TextEdit.Paste(ed, ed.State, TextFieldEditor.Clipboard.AsSpan());
			if (kb.Value.IsPressedOnce(Input.KeyCode.Z)) TextEdit.Key(ed, ed.State, shift ? TextEditKey.Redo : TextEditKey.Undo);
			if (kb.Value.IsPressedOnce(Input.KeyCode.Left)) TextEdit.Key(ed, ed.State, TextEditKey.WordLeft, shift);
			if (kb.Value.IsPressedOnce(Input.KeyCode.Right)) TextEdit.Key(ed, ed.State, TextEditKey.WordRight, shift);
			return;
		}

		if (kb.Value.IsPressedOnce(Input.KeyCode.Left)) TextEdit.Key(ed, ed.State, TextEditKey.Left, shift);
		if (kb.Value.IsPressedOnce(Input.KeyCode.Right)) TextEdit.Key(ed, ed.State, TextEditKey.Right, shift);
		if (kb.Value.IsPressedOnce(Input.KeyCode.Home)) TextEdit.Key(ed, ed.State, TextEditKey.LineStart, shift);
		if (kb.Value.IsPressedOnce(Input.KeyCode.End)) TextEdit.Key(ed, ed.State, TextEditKey.LineEnd, shift);
		if (kb.Value.IsPressedOnce(Input.KeyCode.Delete)) TextEdit.Key(ed, ed.State, TextEditKey.Delete);
		// Backspace lives here on the KEY edge; RouteChars drops '\b' so
		// backends that also synthesize it as a TextInput char don't double-delete.
		if (kb.Value.IsPressedOnce(Input.KeyCode.Back)) TextEdit.Key(ed, ed.State, TextEditKey.Backspace);
		if (kb.Value.IsPressedOnce(Input.KeyCode.Enter))
			commands.Entity(ed.Entity).EmitTrigger(new TextFieldSubmit { Value = ed.Value }, propagate: true);
	}

	private static void RouteChars(
		EventReader<Input.CharInput> reader,
		ResMut<TextFieldEditor> editor)
	{
		var ed = editor.Value;
		if (ed.Entity == 0)
		{
			foreach (var _ in reader.Read()) { } // drain so stale chars don't land on the next focus
			return;
		}

		foreach (var ev in reader.Read())
		{
			var ch = ev.Value;
			// Control chars route through RouteKeys ('\b' Backspace, '\r'/'\n'
			// Enter, Tab/Home/End synthesized by some backends, 127 Delete).
			if (ch < ' ' || ch == (char)127) continue;
			TextEdit.InputChar(ed, ed.State, ch);
		}
	}

	private static void WriteBack(
		Commands commands,
		ResMut<TextFieldEditor> editor,
		Query<Data<TextField, Text>> fields)
	{
		var ed = editor.Value;
		if (ed.Entity == 0) return;
		if (!fields.Contains(ed.Entity)) { ed.Entity = 0; return; } // field despawned

		var (_, _, text) = fields.Get(ed.Entity);
		var value = ed.Value;
		if (!string.Equals(text.Ref.Value, value, System.StringComparison.Ordinal))
		{
			text.Ref.Value = value;
			commands.Entity(ed.Entity).EmitTrigger(new TextFieldChanged { Value = value }, propagate: true);
		}
	}

	private static string Selected(TextFieldEditor ed)
	{
		var (s0, s1) = ed.Selection;
		return s0 == s1 ? string.Empty : ed.Buffer.ToString(s0, s1 - s0);
	}
}

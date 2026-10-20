namespace TinyEcs.Bevy.UI;

// Components and resources of the text editor (UiTextEditPlugin). One editor
// (ActiveTextEdit) serves whichever field holds InputFocus.

/// <summary>On every editable field's glyph entity — the I-beam cursor and "a
/// field is focused" checks key off it. Also on fields with bespoke editors that
/// do not carry <see cref="EditableText"/>.</summary>
public struct TextInput;

/// <summary>Opt-in: the shared editor edits THIS entity's <see cref="Text"/>
/// (or <see cref="MaskedText"/> / <see cref="MultilineText"/>) while it holds
/// <see cref="InputFocus"/>. A field without it keeps its own char readers.</summary>
public struct EditableText
{
	/// <summary>Multi-line editor for a SELF-EDITABLE node (Text + TextInput +
	/// EditableText, no host-spawned caret/selection children): Enter inserts
	/// '\n', Tab indents, Up/Down walk rows, Home/End are per row and the caret /
	/// selection lay out per row. Lines break at hard '\n' only, so pair it with
	/// <see cref="TextWrap.None"/> — distinct from <see cref="MultilineText"/>,
	/// the spawned field's wrapped variant.</summary>
	public bool Multiline;
}

/// <summary>On an <see cref="EditableText"/> field: select + copy only. Every edit
/// is reverted at write-back.</summary>
public struct ReadOnlyText;

/// <summary>Real value of a masked field (a password). <see cref="UiTextEditPlugin"/>
/// mirrors <see cref="MaskChar"/> × length into the sibling <see cref="Text"/>;
/// editors and consumers read/write <see cref="Value"/>, the renderer only sees the mask.</summary>
public struct MaskedText
{
	public string Value;
	public char MaskChar;
}

/// <summary>A wrapped multi-line field (<see cref="UiTextField"/> with Multiline).
/// The value lives here, not in a <see cref="Text"/>: the host renders it with
/// its own wrapped-text drawer at <see cref="WrapWidth"/>, and the editor maps
/// the caret / selection through the same wrap
/// (<see cref="ITextEditMeasurer.WrapRows"/> with <see cref="Style"/>).
/// The editor stamps the caret / selection fields every frame for the focused
/// field (Sel* = -1 and CaretOn = false otherwise); the host draws them in-content
/// so they scroll and clip with the glyphs.</summary>
public struct MultilineText
{
	public int WrapWidth;
	public string Value;
	/// <summary>Host text-layout style (rich-text flags, colours) handed through to
	/// <see cref="ITextEditMeasurer.WrapRows"/>; null = plain.</summary>
	public object? Style;

	public int SelStart, SelEnd;
	public bool CaretOn;
	public float CaretX, CaretY, CaretH;

	public MultilineText(int wrapWidth, string value, object? style = null)
	{
		WrapWidth = wrapWidth;
		Value = value;
		Style = style;
		SelStart = SelEnd = -1;
		CaretOn = false;
		CaretX = CaretY = CaretH = 0;
	}
}

/// <summary>A blinking "_" glyph for a bespoke field (no <see cref="EditableText"/>):
/// shown while <see cref="Target"/> holds focus. Place it as a flex-row sibling
/// after the input's Text.</summary>
public struct TextCaret { public ulong Target; }

/// <summary>On a spawned field's glyph: where its text origin is for mouse caret
/// mapping — the frame's ComputedNode x + OffsetX - Scroll. Single-line: Row is
/// the absolute text row, RowLeft its unscrolled Left, Scroll how far the editor
/// slid it to keep the caret inside a clipping frame.</summary>
public struct TextFieldGeom { public ulong Frame; public float OffsetX; public ulong Row; public float RowLeft, Scroll; }

/// <summary>The caret bar overlay of a spawned field. Offset* shift it from the
/// overlay parent's origin to the text origin; Max* (parent-local, 0 = unbounded)
/// hide/clip it past the field's content edge (an absolute child escapes the clip).</summary>
public struct TextEditCaret { public ulong Target; public float OffsetX, OffsetY, MaxX, MaxY; }

/// <summary>The selection highlight overlay of a spawned field.</summary>
public struct TextEditSelection { public ulong Target; public float OffsetX, OffsetY, MaxX, MaxY; }

/// <summary>A press on this element (or inside it) does not blur the focused
/// self-editable field — e.g. an autocomplete row that completes into it.</summary>
public struct UiNoBlur;

/// <summary>An autocomplete offer for the focused field: <see cref="Items"/> all
/// start with <see cref="Prefix"/>, the word in front of the caret. While it names
/// the focused multiline field, Up/Down walk it and Tab inserts the highlighted item
/// (<see cref="ActiveTextEdit.CompletionSelected"/>).</summary>
public sealed class TextCompletion
{
	public ulong Entity;
	public string Prefix = string.Empty;
	public string[] Items = System.Array.Empty<string>();
}

/// <summary>The OS clipboard, provided by the host.</summary>
public interface IClipboardBackend
{
	/// <summary>Current OS clipboard text, null/empty when none.</summary>
	string? Get();

	/// <summary>Write the OS clipboard; false when the OS refused.</summary>
	bool Set(string text);
}

/// <summary>Clipboard for the text editor's copy / cut / paste. Writes go to the
/// host's OS clipboard (<see cref="Backend"/>) and are kept in-app as the fallback.
/// After a refused OS write the getter prefers the in-app copy: reading the OS back
/// would return whatever was there BEFORE the copy.</summary>
public sealed class UiClipboard
{
	public IClipboardBackend? Backend;

	private string _local = string.Empty;
	private bool _osWriteFailed;

	/// <summary>The last write did not reach the OS clipboard (always true without a
	/// <see cref="Backend"/>): the copy is only pastable inside the app.</summary>
	public bool OsRefusedWrite => _osWriteFailed || Backend == null;

	public string Text
	{
		get
		{
			if (!OsRefusedWrite)
			{
				var os = Backend!.Get();
				if (!string.IsNullOrEmpty(os)) return os;
			}
			return _local;
		}
		set
		{
			_local = value ?? string.Empty;
			_osWriteFailed = Backend == null || !Backend.Set(_local);
		}
	}
}

/// <summary>On a self-editable field: a press elsewhere does not blur it (a tool
/// window whose keyboard gesture targets its own text, e.g. select-all + copy).</summary>
public struct UiKeepFocusOnClickAway;

/// <summary>Bubbling entity trigger on a field whose value the editor just changed.</summary>
public struct TextEditChanged
{
	public string Value;
}

/// <summary>Bubbling entity trigger: Enter pressed in a focused single-line field.</summary>
public struct TextEditSubmit
{
	public string Value;
}

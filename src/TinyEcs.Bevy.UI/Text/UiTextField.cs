using System.Numerics;
using ClayColor = Clay.Color;

namespace TinyEcs.Bevy.UI;

/// <summary>Parameters of <see cref="UiTextField.Spawn"/>.</summary>
public struct UiTextFieldOptions
{
	/// <summary>Text origin inside the frame (the content row's Left / Top).</summary>
	public Vector2 ContentOffset;
	public TextFont Font;
	/// <summary>Glyph colour, also the caret bar's.</summary>
	public ClayColor Color;
	public string? Initial;
	/// <summary>Password field: the value lives in <see cref="MaskedText"/>.</summary>
	public bool Masked;
	/// <summary>'\0' = '*'.</summary>
	public char MaskChar;
	/// <summary>Wrapped multi-line field: the value lives in <see cref="MultilineText"/>
	/// (wrapped at <see cref="WrapWidth"/> with <see cref="MultilineStyle"/>), which the
	/// host renders — attach its drawer in <see cref="PrepareGlyph"/>.</summary>
	public bool Multiline;
	public int WrapWidth;
	public object? MultilineStyle;
	/// <summary>Single-line: put the glyph row in FLOW layout (the content offset
	/// becomes row padding) instead of absolute — needed inside an Overflow.Scroll
	/// container, where an absolute row escapes the scroll clip.</summary>
	public bool FlowRow;
	/// <summary>Single-line, non-flow: &gt; 0 fixes the row to this width and clips
	/// the glyphs past it (a box with a button inside it). 0 = the row grows.</summary>
	public int ClipWidth;
	/// <summary>Selection highlight; default = translucent blue.</summary>
	public ClayColor SelectionColor;
	/// <summary>Applied to every spawned sub-entity (glyph, selection, caret, row) —
	/// scene / page markers that gate visibility.</summary>
	public Action<EntityCommands>? Decorate;
	/// <summary>Applied to the glyph right after it is spawned, BEFORE the value
	/// components land (a host renderer, a visibility guard for secrets).</summary>
	public Action<EntityCommands>? PrepareGlyph;
}

/// <summary>
/// Builds a focusable, bounds-hittable editable text field on an already-spawned
/// frame entity (the caller owns the background). A content row holds the glyph
/// plus a selection highlight (under it) and a caret bar (over it), positioned
/// every frame by <see cref="UiTextEditPlugin"/>; a press on the frame, the row or
/// the overlays focuses the glyph and places the caret. The returned glyph id is
/// the focus target and where the value lives (<see cref="Text"/>,
/// <see cref="MaskedText"/> or <see cref="MultilineText"/>).
/// </summary>
public static class UiTextField
{
	public static readonly ClayColor DefaultSelectionColor = new(70, 110, 180, 120);

	public static ulong Spawn(Commands commands, ITextEditMeasurer measurer, EntityCommands frame, in UiTextFieldOptions o)
	{
		var font = o.Font;
		var initial = o.Initial ?? string.Empty;
		var maskChar = o.MaskChar == '\0' ? '*' : o.MaskChar;
		var selColor = o.SelectionColor.A == 0 && o.SelectionColor.R == 0 && o.SelectionColor.G == 0 && o.SelectionColor.B == 0
			? DefaultSelectionColor : o.SelectionColor;
		var decorate = o.Decorate;
		var multiline = o.Multiline;
		var clipWidth = o.ClipWidth;

		// UiNoWindowDrag on every hittable part so a press on the field never
		// latches a window move — that frees press+drag for caret placement / text
		// selection. TextInput on the frame itself (not just the glyph) so an
		// I-beam test lights the WHOLE field box, not only the short text.
		frame.Insert(Interaction.None).Insert<UiContainsByBounds>().Insert<UiNoWindowDrag>().Insert<TextInput>();

		var glyph = commands.Spawn();
		o.PrepareGlyph?.Invoke(glyph);
		if (multiline)
		{
			// The value lives in MultilineText (wrapped at WrapWidth by the host
			// drawer); the editor maps caret/selection through the same wrap. No
			// Text component (it would render a second, unwrapped copy).
			glyph
				.Insert(new Node { Width = Val.Px(o.WrapWidth), Height = Val.Px(font.Size) })
				.Insert(new MultilineText(o.WrapWidth, initial, o.MultilineStyle))
				.Insert(font)
				.Insert<TextInput>()
				.Insert<EditableText>()
				.Insert<UiNoWindowDrag>()
				// A host drawer usually has no pixel hit-test for wrapped text, so the
				// pick would fall through to the window under the glyphs and a
				// drag-to-select would move the window. Bounds-hit the glyph rect.
				.Insert<UiContainsByBounds>()
				.Insert(new TextFieldGeom { Frame = frame.Id, OffsetX = o.ContentOffset.X });
		}
		else
		{
			glyph
				.Insert(new Node { Width = Val.Auto, Height = Val.Auto })
				.Insert(new Text(o.Masked ? string.Empty : initial))
				.Insert(font)
				.Insert(new TextColor(o.Color))
				.Insert<TextInput>()
				.Insert<EditableText>()
				.Insert<UiNoWindowDrag>();
			if (o.Masked)
				glyph.Insert(new MaskedText { Value = initial, MaskChar = maskChar });
		}
		decorate?.Invoke(glyph);
		ulong glyphId = glyph.Id;

		// Record a press for caret placement / selection-clear (the editor applies
		// it after focus syncs). Attached to every hittable part of the field.
		// glyphId is an immutable capture whose lifetime covers the observer's.
		static void AttachPress(EntityCommands ec, ulong glyphId) =>
			ec.Observe((On<UiPointerDown> t, ResMut<InputFocus> focused, ResMut<ActiveTextEdit> edit) =>
			{
				focused.Value.Entity = glyphId;
				edit.Value.PendingClickEntity = glyphId;
				edit.Value.PendingClickX = t.Event.Position.X;
				edit.Value.PendingClickY = t.Event.Position.Y;
			});

		// Selection highlight behind the glyphs and a caret bar over them, both
		// absolute. Absolute nodes are Clay floats that capture the pointer, so
		// without their own Interaction + press handler a click on the highlighted
		// word would never reach the field (no re-placing the caret on a selection).
		float vCentre = 0f;
		if (!multiline)
		{
			// Text draws at its natural top; a boxed FIELD wants its ink centred in
			// the cell, so the single-line row shifts by the font's ink offset (the
			// overlays ride the row).
			var (inkTop, inkH, cell) = measurer.CaretMetrics(font.FontId, font.Size);
			vCentre = (cell - inkH) / 2f - inkTop;
		}

		var selection = commands.Spawn()
			.Insert(new Node
			{
				PositionType = PositionType.Absolute,
				Left = Val.Px(0), Top = Val.Px(0),
				Width = Val.Px(0), Height = Val.Px(font.Size),
				Display = Display.None,
			})
			.Insert(new BackgroundColor(selColor))
			.Insert(Interaction.None)
			.Insert<UiContainsByBounds>()
			.Insert<UiNoWindowDrag>()
			// TextInput so an I-beam cursor test holds over the highlight too.
			.Insert<TextInput>()
			// Its own float (not clipped by the row's clip): clip it too, or a
			// selection over the slid-out head of the text paints left of the field.
			.Insert<ClipToParent>()
			.Insert(new TextEditSelection { Target = glyphId, MaxX = clipWidth });
		decorate?.Invoke(selection);
		AttachPress(selection, glyphId);

		var caret = commands.Spawn()
			.Insert(new Node
			{
				PositionType = PositionType.Absolute,
				Left = Val.Px(0), Top = Val.Px(0),
				Width = Val.Px(2), Height = Val.Px(font.Size),
				Display = Display.None,
			})
			.Insert(new BackgroundColor(o.Color))
			.Insert(Interaction.None)
			.Insert<UiContainsByBounds>()
			.Insert<UiNoWindowDrag>()
			.Insert<TextInput>()
			.Insert<ClipToParent>()
			.Insert(new TextEditCaret { Target = glyphId, MaxX = clipWidth });
		decorate?.Invoke(caret);
		AttachPress(caret, glyphId);

		// Single-line: absolute row at the content offset (overlays sit on top),
		// ClipToParent so the frame's clip (and any scroll body's) still applies —
		// the editor's FollowCaret slides it left to keep the caret in view.
		// Multiline: a FLOWING row so it (and its growing glyph) is clipped and
		// scrolled by the frame's Overflow.Scroll.
		// FlowRow: the row sits on whole pixels — a fractional ink-centring inside an
		// integral flow origin would land the glyph half a pixel off its overlays.
		float rowLeft = o.FlowRow ? MathF.Floor(o.ContentOffset.X) : o.ContentOffset.X;
		float rowTop = o.FlowRow ? MathF.Floor(o.ContentOffset.Y + vCentre) : o.ContentOffset.Y + vCentre;
		var rowNode = multiline
			? new Node { FlexDirection = FlexDirection.Row, AlignItems = AlignItems.Start, Width = Val.Auto, Height = Val.Auto }
			: new Node
			{
				PositionType = PositionType.Absolute,
				Left = Val.Px(rowLeft),
				Top = Val.Px(rowTop),
				FlexDirection = FlexDirection.Row,
				AlignItems = AlignItems.Center,
				// A ClipWidth row is its own clip, so FollowCaret can't slide it; past
				// the width the caret hides (MaxX).
				Width = clipWidth > 0 ? Val.Px(clipWidth) : Val.Auto,
				Height = Val.Auto,
				Overflow = clipWidth > 0 ? Overflow.Clip : Overflow.Visible,
			};
		var row = commands.Spawn()
			.Insert(rowNode)
			.Insert(Interaction.None)
			.Insert<UiContainsByBounds>()
			.Insert<UiNoWindowDrag>();
		if (!multiline)
		{
			row.Insert<ClipToParent>();
			// Text origin for mouse caret/selection: the editor reads the frame's
			// ComputedNode + OffsetX - Scroll.
			glyph.Insert(new TextFieldGeom { Frame = frame.Id, OffsetX = rowLeft, Row = row.Id, RowLeft = rowLeft });
		}
		decorate?.Invoke(row);
		AttachPress(row, glyphId);
		row.AddChild(selection);
		row.AddChild(glyph);
		row.AddChild(caret);

		AttachPress(frame, glyphId);
		frame.AddChild(row);

		return glyphId;
	}
}

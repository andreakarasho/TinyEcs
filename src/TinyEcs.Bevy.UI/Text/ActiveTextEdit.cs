// The single live text editor: the StbTextEdit state + a buffer for the focused
// field, plus the ITextEditHandler StbTextEdit reads/measures/mutates it
// through. UiTextEditPlugin owns the SYSTEMS (mouse/key/char routing,
// write-back, overlay positioning).
//
// Everything geometric lives here because caret x, selection spans, click
// hit-testing and stb’s own row walk must all come off ONE measurement of the
// field’s font — the same one the renderer draws with — or they disagree by a
// pixel and the bar drifts off the glyphs.

using System.Text;
using TinyEcs.Bevy.Input;
using StbTextEdit;

namespace TinyEcs.Bevy.UI;

public sealed class ActiveTextEdit : ITextEditHandler
{
    public ulong Entity;            // focused editable glyph, 0 = none
    public ushort FontId;
    public ushort FontSize = 12;
    // Font for a field that carries no TextFont (the host's default face).
    public ushort DefaultFontId;
    // Pins the caret blink phase (UiTextEditPlugin.CaretOn) for deterministic
    // screenshots; null = blink normally.
    public bool? CaretBlinkHold;
    public bool Masked;
    public char MaskChar = '*';
    public bool Multiline;          // '\n' allowed; stb runs non-single-line
    // The multiline value lives in a MultilineText — UiTextField's multiline
    // variant, where the host owns the wrap width and draws the wrapped glyphs.
    // Multiline WITHOUT this is a SELF-EDITABLE code field: the value stays a
    // plain Text component, Clay draws it, and lines break at hard '\n' only.
    public bool CustomMultiline;
    public int WrapWidth = 200;     // wrap column for CustomMultiline fields
    // MultilineText.Style of the bound field: the wrap must measure with the same
    // host style the renderer draws with (a rich-text mode changes per-row
    // heights) or the caret Y drifts vs render.
    public object? WrapStyle;

    // A press on a field records (entity, logical screen-x) here via its
    // UiPointerDown observer; RouteMouse converts it to a caret position.
    public ulong PendingClickEntity;
    public float PendingClickX;
    public float PendingClickY;

    // The last left press anywhere (logical px, engine ms), recorded by RouteMouse
    // every frame. A field that takes focus WITHOUT a press routed to it — its owner swaps
    // its read-only view for the editable node under the cursor, then focuses it by
    // resource (a code view that turns editable on click) — seats the caret from this press instead
    // of at the buffer end, which is where the player's click actually landed.
    public float LastPressX, LastPressY;
    public float LastPressTime = float.NegativeInfinity;
    // Set once a field has used that press (routed by its observer, or seeded): a
    // consumed press never seats a second caret. Fresh presses reset it.
    public bool LastPressConsumed = true;

    // > 0: such a focus arrived while the press was fresh; RouteMouse converts it into
    // a PendingClick once the field's geometry is known, and counts it down on the
    // frames it is not, so a field that never gets an overlay drops it.
    public int SeedClickFrames;
    public const int SeedClickMaxFrames = 10;
    // Generous on purpose: an unfocused / throttled client can take over a second
    // between the press and the focus (an owner that polls at 100 ms needs two polls, each of
    // which degrades to frame granularity). The consumed flag + the content-box test
    // are what make the seed safe, not the clock.
    public const float SeedClickWindowMs = 3000f;

    // True only while a left-drag that *began* on this field is in progress. Gates
    // the drag-select branch so holding the button down after a press that started
    // outside the field (anywhere else on screen) doesn't extend the selection.
    public bool MouseSelecting;

    // Engine-clock time (Time.Total, ms) of the last press recorded on the
    // focused field. RouteMouse calls a press a double-click when two land within
    // DoubleClickWindow. We can't lean on MouseInput.IsPressedDouble here: it's
    // a one-frame pulse on the press-once edge, but the field's PendingClick fires
    // off the UiPointerDown press edge (IsPressed = two consecutive down frames),
    // which is always a frame later — by then IsPressedDouble has been reset, so
    // the two never coincide at this callsite. Reset on field change.
    public float LastClickTime = float.NegativeInfinity;
    // Consecutive presses inside the double-click window (1-based). RouteMouse
    // ranks each press single (1) / double (2) / triple (3) off this; a 4th
    // press within the window restarts at 1. Reset on field change and unbind.
    public int ClickStreak;

    // The letter key of the Ctrl chord currently in flight (KeyCode.None when
    // idle). RouteChars uses it to drop the chord's composed text — see the
    // leak note there. Set by RouteKeys, cleared by RouteChars on release.
    public KeyCode ChordKey;

    // Goal x (px from the text origin) of a run of Up/Down/Page moves, so walking
    // down through short lines and back out keeps the original column. stb keeps
    // its own equivalent but INTERNAL to the library, and the self-editable
    // vertical move is ours (see TextEditPlugin.MoveRows). Null = not in a
    // vertical run; every horizontal move, edit and click clears it.
    public float? RowGoalX;

    // The highlighted index of the live TextCompletion offer (the row the
    // player has arrowed / hovered / clicked to), and the PREFIX it was first matched
    // against. The host owns this because it owns the keys while the field is focused
    // (the offer's owner cannot swallow Up/Down); RouteKeys resets it to 0 whenever the prefix
    // changes so Tab / arrows never index a stale list. Hosts publish it to whoever
    // draws the suggestion popup.
    public int CompletionSelected;
    public string CompletionPrefix = string.Empty;

    // Last applied sequence of a host's one-shot "insert item N" request (fire on
    // change, so a request that lingers is pasted exactly once).
    public int CompletionPickSeq;

    // Same, for a host's one-shot copy / cut / paste request (a context menu).
    public int ClipboardActionSeq;

    // How many rows PageUp/PageDown jump. stb resets this to 0 on Initialize
    // (which would make Page* a no-op), so SyncActiveEditor stamps it per field.
    public const int RowsPerPage = 10;

    public readonly TextEditState State = new(singleLine: true);
    public readonly StringBuilder Buffer = new();

    // Hard-newline line index over Buffer. Reported as the field's (line, column)
    // — a code editor's lines are BUFFER lines, which the row table below can
    // subdivide further when word-wrap is on.
    private readonly TextLineIndex _lines = new();
    private int _version;
    private int _linesVersion = -1;

    public TextLineIndex Lines
    {
        get
        {
            if (_linesVersion != _version)
            {
                _lines.Rebuild(Buffer);
                _linesVersion = _version;
            }
            return _lines;
        }
    }

    // Rendered row table for the self-editable multiline field: (start, count,
    // width, height) per VISUAL line, in raw-buffer indices — exactly what
    // the host renderer stacks when it paints this text at this width. The
    // per-row HEIGHT is the point: a renderer whose line pitch is the tallest
    // glyph on that line makes a line of "one" shorter than one holding "Hgpy" —
    // stacking by a single font-cell constant drifts a few px per line and the
    // caret ends up between the rows it is supposed to sit on.
    //
    // A seam (like MeasureWidth) so tests can pin the metrics.
    public Func<string, ushort, int, List<(int Start, int Count, int Width, int Height)>> MeasureRows;

    private readonly List<(int Start, int Count, int Width, int Height)> _rows = new();
    private int _rowsVersion = -1, _rowsWidth = -1;
    private ushort _rowsFont;

    /// Row table for the current buffer, normalized so EVERY caret position has a
    /// row: the wrap emits nothing for an empty buffer and no final row after a
    /// trailing '\n', but the caret sits in both places. Cached per (buffer,
    /// width, font) — stb calls LayoutRow several times per keystroke and the
    /// overlay reads it every frame, while the wrap only changes on an edit.
    public List<(int Start, int Count, int Width, int Height)> Rows
    {
        get
        {
            if (_rowsVersion == _version && _rowsWidth == WrapWidth && _rowsFont == FontId)
                return _rows;
            _rowsVersion = _version; _rowsWidth = WrapWidth; _rowsFont = FontId;

            var s = Display();
            var fallbackH = (int)MathF.Max(1, LineHeight);
            _rows.Clear();
            foreach (var r in MeasureRows(s, FontId, WrapWidth))
                _rows.Add((r.Start, r.Count, r.Width, r.Height > 0 ? r.Height : fallbackH));

            var covered = _rows.Count == 0 ? 0 : _rows[^1].Start + _rows[^1].Count;
            // A wrap may emit no rows for TRAILING EMPTY LINES (or nothing at all
            // headless), so the tail is laid out here, one row per hard line. One blob
            // row for the whole remainder put every trailing empty line on the same
            // visual row: a click on the 3rd of them landed the caret at the buffer end
            // (Delete then had nothing to eat) while the gutter beside it counted 4.
            while (covered < s.Length)
            {
                var nl = s.IndexOf('\n', covered);
                var end = nl < 0 ? s.Length : nl + 1;
                var text = s.Substring(covered, (nl < 0 ? end : nl) - covered);
                _rows.Add((covered, end - covered, text.Length == 0 ? 0 : (int)MeasureWidth(text, FontId), fallbackH));
                covered = end;
            }
            if (_rows.Count == 0 || (s.Length > 0 && s[^1] == '\n'))
                _rows.Add((s.Length, 0, 0, fallbackH));
            return _rows;
        }
    }

    /// Visual row holding a caret index, and the caret's column within it. A
    /// caret exactly at a row end belongs to the NEXT row (it sits before the
    /// character that opened it), which is what walking Start + Count gives.
    public (int Row, int Column) RowAt(int cursor)
    {
        var rows = Rows;
        var c = Math.Clamp(cursor, 0, Buffer.Length);
        for (var i = 0; i < rows.Count; i++)
            if (c < rows[i].Start + rows[i].Count)
                return (i, c - rows[i].Start);
        var last = rows.Count - 1;
        return (last, Math.Max(0, c - rows[last].Start));
    }

    /// Sum of the row heights above `row` — the renderer's own stacking.
    public float RowTop(int row)
    {
        var rows = Rows;
        float y = 0;
        for (var i = 0; i < row && i < rows.Count; i++)
            y += rows[i].Height;
        return y;
    }

    /// Caret index on `row` closest to `goalX` (measured from the text origin),
    /// clamped to the row's text — the column-preserving target of an Up/Down.
    /// Rounds at the half-glyph like a click does, so arrowing down a ragged
    /// block lands on the same visual column a click there would.
    public int IndexForX(int row, float goalX)
    {
        var rows = Rows;
        if (rows.Count == 0) return 0;
        row = Math.Clamp(row, 0, rows.Count - 1);
        var s = Display();
        var start = Math.Clamp(rows[row].Start, 0, s.Length);
        var end = Math.Min(start + rows[row].Count, s.Length);
        if (end > start && s[end - 1] == '\n') end--;
        float prev = 0;
        for (var i = start; i < end; i++)
        {
            var w = MeasureWidth(s.Substring(start, i - start + 1), FontId);
            if (goalX < prev + (w - prev) * 0.5f) return i;
            prev = w;
        }
        return end;
    }

    // Replace the whole buffer (focus arrival / external write). Goes through
    // here rather than Buffer directly so the line index invalidates.
    public void SetBuffer(string value)
    {
        Buffer.Clear();
        Buffer.Append(value);
        _version++;
    }

    // Clipboard for Ctrl+C/X/V: the app's UiClipboard (OS clipboard through the
    // host backend, in-app fallback). Bound by UiTextEditPlugin.
    public UiClipboard ClipboardStore = new();

    // The last write did not reach the OS clipboard: the copy is real but only
    // pastable INSIDE the app. Read by tools whose whole point is the copy so a
    // refused write cannot masquerade as a good one.
    public bool OsClipboardRefusedWrite => ClipboardStore.OsRefusedWrite;

    public string Clipboard
    {
        get => ClipboardStore.Text;
        set => ClipboardStore.Text = value;
    }

    // Result of the last Ctrl+C / Ctrl+X. Copy is otherwise SILENT in both
    // directions: an empty selection skips the clipboard write entirely, so the
    // clipboard keeps its previous contents and the gesture looks like it worked.
    // A tool whose whole point is the copy (a layout inspector) reads these to
    // say what actually landed. Attempts is the change ticker; Chars == 0 means
    // nothing was selected and the clipboard was left alone.
    public int CopyAttempts;
    public int CopyChars;
    public int CopyLines;

    public string Display() => Masked ? new string(MaskChar, Buffer.Length) : Buffer.ToString();

    // Text width source for all caret / selection / click math. A seam so tests
    // can pin the metrics; defaults to the bound Measurer.
    public Func<string, ushort, float> MeasureWidth;

    // The editor metrics (caret band, line pitch, wrap). UiTextEditPlugin binds the
    // app's layout measurer, so caret math and glyph layout share one source.
    public ITextEditMeasurer Measurer = TextEditMeasurer.For(new Clay.SimpleTextMeasurer());

    private readonly List<TextRow> _wrapScratch = new();

    public ActiveTextEdit()
    {
        MeasureWidth = (text, fontId) => Measurer.MeasureWidth(text, fontId, FontSize);
        MeasureRows = (text, fontId, wrapWidth) =>
        {
            _wrapScratch.Clear();
            Measurer.WrapRows(text, fontId, FontSize, wrapWidth, null, _wrapScratch);
            var list = new List<(int Start, int Count, int Width, int Height)>(_wrapScratch.Count);
            foreach (var r in _wrapScratch) list.Add((r.Start, r.Count, (int)r.Width, (int)r.Height));
            return list;
        };
        MeasureLineHeight = fontId => Measurer.LineHeight(fontId, FontSize);
    }

    public void Bind(ITextEditMeasurer measurer) => Measurer = measurer;

    /// <summary>Caret ink band for the field's font.</summary>
    public (float Top, float Height, float Cell) CaretMetrics() => Measurer.CaretMetrics(FontId, FontSize);

    // Rows of the CustomMultiline (MultilineText) field through the host wrap, in
    // buffer indices, with the rendered row heights.
    private List<TextRow> CustomRows()
    {
        _wrapScratch.Clear();
        Measurer.WrapRows(Display(), FontId, FontSize, WrapWidth, WrapStyle, _wrapScratch);
        return _wrapScratch;
    }

    // Width in px of the display text's first `count` chars, in the field font.
    public float WidthUpTo(int count)
    {
        if (count <= 0) return 0;
        var s = Display();
        if (count > s.Length) count = s.Length;
        return MeasureWidth(s.Substring(0, count), FontId);
    }

    // ── ITextEditHandler ──────────────────────────────────────────────
    public int Length => Buffer.Length;
    public char GetChar(int index) => Masked ? MaskChar : Buffer[index];

    // stb's row-break width sentinel. The upstream stb_textedit breaks its
    // column walk on STB_TEXTEDIT_GETWIDTH_NEWLINE; this port dropped that check,
    // so a newline reported as 0-wide let Up/Down keep walking PAST the end of a
    // shorter line and land on the next line's first column. A width no goal-x
    // can exceed reproduces the break: the walk stops at the newline, i.e. the
    // caret clamps to the line end. Never reached by the click path — LocateCoord
    // only enters its per-char loop for an x inside the row's text width, and the
    // newline is always the row's last char.
    private const float NewlineBreakWidth = 1e6f;

    // Width of the char at OFFSET charIndex within the row starting at
    // lineStartIndex — stb passes a row-relative offset, not an absolute index
    // (all three call sites do: LocateCoord, FindCharPosition, vertical move).
    public float GetCharWidth(int lineStartIndex, int charIndex)
    {
        var s = Display();
        int start = Math.Clamp(lineStartIndex, 0, s.Length);
        int abs = start + charIndex;
        if (charIndex < 0 || abs >= s.Length) return 0;
        if (s[abs] == '\n') return NewlineBreakWidth;
        int rel = abs - start;
        float a = rel <= 0 ? 0 : MeasureWidth(s.Substring(start, rel), FontId);
        float b = MeasureWidth(s.Substring(start, rel + 1), FontId);
        return b - a;
    }

    public void LayoutRow(out TextEditRow row, int lineStartIndex)
    {
        float lh = LineHeight;
        if (Multiline && !CustomMultiline)
        {
            // Self-editable field: the renderer's own row table drives both the
            // row extents and the row PITCH, so stb's click-to-line mapping and
            // the caret's y agree with where the glyphs actually landed.
            var rows = Rows;
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].Start != lineStartIndex) continue;
                float rh = rows[i].Height > 0 ? rows[i].Height : lh;
                row = new TextEditRow
                {
                    X0 = 0, X1 = rows[i].Width, BaselineYDelta = rh, YMin = 0, YMax = rh,
                    NumChars = rows[i].Count,
                };
                return;
            }
            // Past the last row (stb's Down walk asks for the row that would
            // start at the end of a buffer with no trailing newline): empty, or
            // reporting the real last row would re-walk it and overshoot.
            row = new TextEditRow { X0 = 0, X1 = 0, BaselineYDelta = lh, YMin = 0, YMax = lh, NumChars = 0 };
            return;
        }

        if (CustomMultiline)
        {
            // Each wrapped/explicit line is one row; stb walks rows by start index
            // for navigation and by accumulated row HEIGHT when mapping a click's y
            // to a line. The host wrap reports buffer indices and the RENDERED row
            // height — feeding stb a shorter pitch made a click map to a lower line
            // than the one under the cursor.
            var wrapped = CustomRows();
            for (int i = 0; i < wrapped.Count; i++)
            {
                var ln = wrapped[i];
                if (ln.Start != lineStartIndex) continue;
                float lnH = ln.Height > 0 ? ln.Height : lh;
                row = new TextEditRow { X0 = 0, X1 = ln.Width, BaselineYDelta = lnH, YMin = 0, YMax = lnH, NumChars = ln.Count };
                return;
            }
            // Trailing empty line (cursor past the last newline) or empty buffer.
            row = new TextEditRow { X0 = 0, X1 = 0, BaselineYDelta = lh, YMin = 0, YMax = lh, NumChars = Math.Max(0, Buffer.Length - lineStartIndex) };
            return;
        }

        // Single-line: the whole buffer is one row. YMax must be > 0 (and > the
        // click y, which we always pass as 0) or stb's Click can't locate the row
        // — a zero-height row contains no point, so the caret never moves.
        float w = WidthUpTo(Buffer.Length);
        row = new TextEditRow
        {
            X0 = 0,
            X1 = w,
            BaselineYDelta = lh,
            YMin = 0,
            YMax = lh,
            NumChars = Buffer.Length,
        };
    }

    // Line pitch for multiline caret/selection geometry. A seam like
    // MeasureWidth; a 0 pitch falls back to 20 so lines never collapse to y = 0.
    public Func<ushort, float> MeasureLineHeight;

    public float LineHeight => MeasureLineHeight(FontId) is var h && h > 0 ? h : 20;

    /// Caret (line, column) for a cursor index — self-editable multiline only;
    /// single-line and the wrapped-custom path report line 0.
    public (int Line, int Column) CaretLineColumn(int cursor)
    {
        if (!Multiline || CustomMultiline) return (0, Math.Clamp(cursor, 0, Buffer.Length));
        var lines = Lines;
        var c = Math.Clamp(cursor, 0, lines.TextLength);
        return (lines.LineOf(c), lines.ColumnOf(c));
    }

    // Caret pixel position for a cursor index: (x within its line, y = stacked
    // line tops). Single-line collapses to (WidthUpTo, 0).
    public (float X, float Y) CaretXY(int cursor)
    {
        var s = Display();
        if (!Multiline) return (WidthUpTo(cursor), 0);
        if (!CustomMultiline)
        {
            // Self-editable field: x is the measured advance of the row's own text
            // up to the caret, y is the sum of the rows ABOVE it — the same row
            // table stb navigates through LayoutRow and the renderer stacks by, so
            // the bar cannot disagree with either.
            var (row, column) = RowAt(cursor);
            var rows = Rows;
            var start = rows[row].Start;
            var take = Math.Clamp(column, 0, Math.Max(0, s.Length - start));
            float cx = take <= 0 ? 0 : MeasureWidth(s.Substring(start, take), FontId);
            return (cx, RowTop(row));
        }
        // The host wrap drives index→row + x (buffer char counts) and the row
        // pitch (the rendered heights), so the caret sits on the drawn glyphs.
        var lines = CustomRows();
        float y = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            var ln = lines[i];
            int end = ln.Start + ln.Count;
            bool last = i == lines.Count - 1;
            // A cursor exactly AT a line end that follows a hard '\n' belongs to
            // the next line's start, not this line's tail — so don't match here
            // (let the loop fall through). Only claim the boundary on the last
            // line, and only when it isn't a trailing newline (that becomes a
            // fresh empty row handled after the loop).
            bool endsWithNewline = end > ln.Start && end <= s.Length && s[end - 1] == '\n';
            if (cursor < end || (last && !endsWithNewline))
            {
                int rel = Math.Clamp(cursor - ln.Start, 0, ln.Count);
                float x = rel <= 0 ? 0 : MeasureWidth(s.Substring(ln.Start, rel), FontId);
                return (x, y);
            }
            y += ln.Height > 0 ? ln.Height : LineHeight;
        }
        // Cursor past the last rendered line (trailing newline): caret on a fresh
        // empty row below, at column 0.
        return (0, y);
    }

    public bool InsertChars(int index, ReadOnlySpan<char> chars)
    {
        Buffer.Insert(index, chars.ToString());
        _version++;
        RowGoalX = null;
        return true;
    }

    public void DeleteChars(int index, int count)
    {
        Buffer.Remove(index, count);
        _version++;
        RowGoalX = null;
    }

    // Unbind from the current field and drop everything that belonged to it.
    // Clearing the BUFFER (and the selection with it) is the point: a field can
    // vanish under the editor at any time — a window despawned + respawned (the
    // window re-pushed with the same id) — and
    // a buffer left holding the dead field's text is text Ctrl+A/Ctrl+C would
    // still copy, silently handing out the OLD window's text while a new one is on
    // screen. Nothing is lost: every focus arrival reloads from the field.
    public void Unbind()
    {
        // Runs every frame while nothing is focused (a login form, a closed window):
        // stay a no-op once already unbound so State.Initialize isn't re-run per
        // frame.
        if (Entity == 0 && Buffer.Length == 0) return;
        Entity = 0;
        SetBuffer(string.Empty);
        Multiline = false;
        CustomMultiline = false;
        State.Initialize(singleLine: true);
        PendingClickEntity = 0;
        MouseSelecting = false;
        LastClickTime = float.NegativeInfinity;
        ClickStreak = 0;
        CompletionSelected = 0;
        CompletionPrefix = string.Empty;
        CompletionPickSeq = 0;
        ClipboardActionSeq = 0;
    }
}

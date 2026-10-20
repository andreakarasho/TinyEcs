// Caret + selection geometry for SELF-EDITABLE text nodes: ONE entity carrying
// Text + TextInput + EditableText, with no spawned caret/selection children —
// the field shape a remote / scripted UI builder can produce (it has no way to
// attach per-part press observers the way UiTextField does).
//
// UiTextField fields are untouched: they carry TextFieldGeom and keep their
// entity-based TextEditCaret / TextEditSelection overlays.
//
// Why an overlay resource instead of two child entities: the field belongs to
// its owner's reconciler, which despawns and respawns its subtree on rebuild, so
// injected children get clobbered — and an Absolute child is an independent Clay
// float root that captures the pointer over its whole box and escapes the
// owner's clip. So the geometry is derived from Clay's own laid-out boxes into
// SelfEditOverlay and the HOST RENDERER paints it inline in the command stream,
// at the field's own commands: the selection on the field's background rect
// (SelPaintId, so it sits UNDER the glyphs) and the caret right after its text
// (CaretPaintId, so it sits ON them). Painting inline also means the field's
// scissor and any window declared later still cover it.
//
// MULTI-LINE (EditableText.Multiline): the value is still a plain Text
// component, so Clay lays out and draws it; lines break at hard '\n' only, which
// is why such a field must also carry TextWrap.None. The selection becomes one
// rect per spanned row.

using Clay;
using ClayColor = Clay.Color;

namespace TinyEcs.Bevy.UI;

// One selection band. Multi-line selections need one per spanned line; a
// single-line field always produces exactly one.
public struct SelfEditSelRect
{
    public float X, Y, W, H;
}

// Per-frame caret/selection geometry for the focused self-editable field, in
// Clay/UI-layout space (same space as UiPointer.Position and the render
// commands). Rebuilt every frame in Stage.PreUpdate from the LAST laid-out
// frame's boxes — one frame of lag on the field's position, invisible, and it
// keeps the builder free of any ordering dependency on the layout stage.
public sealed class SelfEditOverlay
{
    public bool Active;
    public ulong Entity;

    // Rendered text top-left, TRUNCATED the way a pixel-snapping glyph draw
    // truncates the command box. Taking the raw float here would put the caret
    // half a pixel off the glyph run whenever the layout lands on a fraction. Also the
    // origin RouteMouse maps a click x/y against, so the caret lands on the glyph
    // the player pointed at.
    public float TextX, TextY;
    // Width of the field's TEXT command — the width the renderer wraps at.
    // Fed back into ActiveTextEdit.WrapWidth so the caret's row table is the
    // rendered one.
    public float TextW;
    // The field's content box (its box minus padding): the caret and the
    // selection are clamped to it — the field cannot scroll its own text (Clay
    // owns the glyph draw), so past the edge they would paint over the
    // neighbouring widgets. Top/Bottom only bound the multiline path; a
    // single-line caret is centred in its box by construction.
    public float ContentLeft, ContentRight, ContentTop, ContentBottom;

    // Render-command ids to paint on. Selection goes on the field's background
    // rectangle (before the glyphs), caret on its text command (after them).
    public uint SelPaintId, CaretPaintId;

    public bool CaretOn;
    public float CaretX, CaretY, CaretW, CaretH;
    public ClayColor CaretColor;

    // Multi-line field: caret (line, column) and the line span the selection
    // covers. Published for diagnostics and read by the caret-into-view scroll
    // pass.
    public bool Multiline;
    public float LineHeight;
    public int CaretLine, CaretColumn;
    public int SelLineFirst, SelLineLast;

    public bool SelOn;
    // Band 0, mirrored for the single-line readers (diagnostics + tests). The
    // renderer always walks SelRects.
    public float SelX, SelY, SelW, SelH;

    // Grown, never shrunk: the overlay is rebuilt every frame and must not
    // allocate once a field's tallest selection has been seen.
    public SelfEditSelRect[] SelRects = new SelfEditSelRect[8];
    public int SelRectCount;

    public void AddSelRect(float x, float y, float w, float h)
    {
        if (SelRectCount == SelRects.Length)
            Array.Resize(ref SelRects, SelRects.Length * 2);
        SelRects[SelRectCount++] = new SelfEditSelRect { X = x, Y = y, W = w, H = h };
        if (SelRectCount == 1)
        {
            SelX = x; SelY = y; SelW = w; SelH = h;
        }
        SelOn = true;
    }
}

public static class SelfEditGeometry
{
    public const float CaretWidth = 2f;
    // A selected line break has no glyph. Without a stub the newline at the end
    // of a fully-selected line reads as "the selection stops at the last
    // character", which on an EMPTY line means no band at all.
    public const float NewlineStubWidth = 4f;
    // Same highlight as UiTextField's selection overlay.
    public static readonly ClayColor SelectionColor = new(70, 110, 180, 120);

    // Clay ids a node's own text: LayoutSystem calls AddText before it emits any
    // child element, and Clay hashes a text element as
    // HashNumber(indexAmongChildren, parentId) — so index 0.
    public static uint TextClayId(uint nodeClayId) => ElementId.HashNumber(0u, nodeClayId).Id;

    public static uint NodeClayId(ulong entity) => ElementId.HashNumber((uint)entity).Id;

    // Caret x for a prefix width, clamped into [left, right - CaretWidth]. False
    // when the content box is too narrow to hold the bar at all.
    public static bool CaretX(float textX, float prefixWidth, float left, float right, out float x)
    {
        x = textX + prefixWidth;
        var max = right - CaretWidth;
        if (max < left) return false;
        x = Math.Clamp(x, left, max);
        return true;
    }

    // Selection span [x0, x1) measured from the text origin, clipped to the
    // content box. False when nothing of it is visible.
    public static bool SelectionSpan(
        float textX, float startWidth, float endWidth, float left, float right,
        out float x, out float width)
    {
        var x0 = MathF.Max(textX + startWidth, left);
        var x1 = MathF.Min(textX + endWidth, right);
        x = x0;
        width = x1 - x0;
        return width >= 1f;
    }

    // Vertical clip of a [y, y+h) band against the content box. False when the
    // band is entirely outside — a line scrolled/clipped out of the field.
    public static bool ClipY(float y, float h, float top, float bottom, out float clippedY, out float clippedH)
    {
        clippedY = MathF.Max(y, top);
        clippedH = MathF.Min(y + h, bottom) - clippedY;
        return clippedH >= 1f;
    }
}

public static class SelfEditOverlaySystem
{
    // Keyed on FOCUS, not on the bound editor: focus is stamped by the
    // UiPointerDown observer during the layout stage, so the geometry is ready
    // for RouteMouse (Stage.Update) to turn the recorded press into a caret index
    // on the very frame SyncActiveEditor binds the field — whatever order the two
    // systems ended up in. Caret/selection then need the bound buffer, so they
    // light up one frame later (invisible).
    public static void Build(
        Res<InputFocus> focused,
        Res<ActiveTextEdit> edit,
        Res<Time> time,
        Res<UiClayContext> ctx,
        ResMut<SelfEditOverlay> overlay,
        Query<Data<Node>, Filter<With<EditableText>, Without<TextFieldGeom>>> fieldsQ,
        Query<Data<TextFont>> fontsQ,
        Query<Data<TextColor>> colorsQ)
    {
        var o = overlay.Value;
        o.Active = false;
        o.CaretOn = false;
        o.SelOn = false;
        o.SelRectCount = 0;
        o.Multiline = false;

        var entity = focused.Value.Entity;
        if (entity == 0) return;
        if (!fieldsQ.TryGet(entity, out var fieldRow)) return;
        if (!ctx.Value.TryGetElementBoundingBox(entity, out var box)) return;

        var (_, node) = fieldRow;
        var left = box.X + Px(node.Ref.Padding.Left);
        var right = box.X + box.Width - Px(node.Ref.Padding.Right);
        if (right <= left) return;

        var a = edit.Value;
        var bound = a.Entity == entity;
        // Only the SELF-editable multiline shape reaches here: the wrapped
        // variant (CustomMultiline) always comes with TextFieldGeom, filtered out
        // by fieldsQ, and its caret is drawn in-content by the host.
        var multiline = bound && a.Multiline && !a.CustomMultiline;
        ushort fontId = a.DefaultFontId, fontSize = 12;
        if (bound) { fontId = a.FontId; fontSize = a.FontSize; }
        else if (fontsQ.TryGet(entity, out var fontRow)) { var (_, f) = fontRow; fontId = f.Ref.FontId; fontSize = f.Ref.Size; }

        var nodeId = SelfEditGeometry.NodeClayId(entity);
        var textId = SelfEditGeometry.TextClayId(nodeId);
        var (inkTop, inkHeight, cell) = a.Measurer.CaretMetrics(fontId, fontSize);

        float textX = MathF.Truncate(left), textY = MathF.Truncate(box.Y + Px(node.Ref.Padding.Top));
        float textW = right - left;
        bool textFound = false, rectFound = false;
        foreach (ref readonly var cmd in ctx.Value.LastCommands)
        {
            if (cmd.Id == textId && cmd.CommandType == RenderCommandType.Text)
            {
                // Truncate, don't round: matches the (int) cast the glyph draw
                // applies to the same box.
                textX = MathF.Truncate(cmd.BoundingBox.X);
                textY = MathF.Truncate(cmd.BoundingBox.Y);
                textW = cmd.BoundingBox.Width;
                textFound = true;
            }
            else if (cmd.Id == nodeId && cmd.CommandType == RenderCommandType.Rectangle)
                rectFound = true;
        }
        // No text command (an empty field can be culled before it reaches the
        // stream): derive the origin from the box, cross-axis centring included,
        // or an empty field's caret sits at the box top instead of on the line.
        // Never for multiline — there the text block owns the box top.
        if (!textFound && !multiline && node.Ref.AlignItems == AlignItems.Center)
            textY = MathF.Truncate(box.Y + (box.Height - cell) * 0.5f);

        o.Active = true;
        o.Entity = entity;
        o.TextX = textX;
        o.TextY = textY;
        o.TextW = textW;
        o.ContentLeft = left;
        o.ContentRight = right;
        o.ContentTop = box.Y + Px(node.Ref.Padding.Top);
        o.ContentBottom = box.Y + box.Height - Px(node.Ref.Padding.Bottom);
        o.SelPaintId = rectFound ? nodeId : textId;
        o.CaretPaintId = textFound ? textId : nodeId;
        if (!bound) return;

        o.CaretColor = new ClayColor(255, 255, 255, 255);
        if (colorsQ.TryGet(entity, out var colorRow))
        {
            var (_, color) = colorRow;
            o.CaretColor = color.Ref.Value;
        }
        o.CaretW = SelfEditGeometry.CaretWidth;
        o.CaretH = inkHeight;

        var s0 = Math.Min(a.State.SelectStart, a.State.SelectEnd);
        var s1 = Math.Max(a.State.SelectStart, a.State.SelectEnd);

        if (!multiline)
        {
            o.CaretY = textY + inkTop;
            var hasCaret = SelfEditGeometry.CaretX(
                textX, a.WidthUpTo(a.State.Cursor), left, right, out var caretX);
            o.CaretOn = hasCaret && UiTextEditPlugin.CaretOn(time.Value.Total, edit.Value.CaretBlinkHold);
            o.CaretX = caretX;
            o.CaretColumn = a.State.Cursor;
            if (s0 == s1) return;
            if (SelfEditGeometry.SelectionSpan(
                    textX, a.WidthUpTo(s0), a.WidthUpTo(s1), left, right, out var selX, out var selW))
                o.AddSelRect(selX, textY + inkTop, selW, inkHeight);
            return;
        }

        o.Multiline = true;
        o.LineHeight = a.LineHeight;
        var (caretLine, caretCol) = a.CaretLineColumn(a.State.Cursor);
        o.CaretLine = caretLine;
        o.CaretColumn = caretCol;

        var (cx, cy) = a.CaretXY(a.State.Cursor);
        var caretTop = textY + cy + inkTop;
        var caretInBox = SelfEditGeometry.CaretX(textX, cx, left, right, out var mcaretX);
        var caretYVisible = SelfEditGeometry.ClipY(
            caretTop, inkHeight, o.ContentTop, o.ContentBottom, out var clippedCaretY, out var clippedCaretH);
        o.CaretX = mcaretX;
        // Unclipped y/h stay published when the line is out of view: the
        // caret-into-view pass needs where the caret WANTS to be, not where it
        // would have been trimmed to.
        o.CaretY = caretYVisible ? clippedCaretY : caretTop;
        o.CaretH = caretYVisible ? clippedCaretH : inkHeight;
        o.CaretOn = caretInBox && caretYVisible && UiTextEditPlugin.CaretOn(time.Value.Total, edit.Value.CaretBlinkHold);

        if (s0 == s1)
        {
            o.SelLineFirst = o.SelLineLast = caretLine;
            return;
        }

        o.SelLineFirst = a.CaretLineColumn(s0).Line;
        o.SelLineLast = a.CaretLineColumn(s1).Line;

        // One band per VISUAL row, on the same ink band as that row's caret: the
        // rows come from the renderer's own table, so a band cannot sit between
        // the glyphs it highlights.
        var rows = a.Rows;
        var display = a.Display();
        var firstRow = a.RowAt(s0).Row;
        var lastRow = a.RowAt(s1).Row;
        for (var row = firstRow; row <= lastRow && row < rows.Count; row++)
        {
            var rowStart = rows[row].Start;
            var rowEnd = Math.Min(rowStart + rows[row].Count, display.Length);
            // A row's trailing '\n' is not a glyph — never measure it.
            if (rowEnd > rowStart && display[rowEnd - 1] == '\n') rowEnd--;
            var from = Math.Clamp(s0, rowStart, rowEnd);
            var to = Math.Clamp(s1, rowStart, rowEnd);
            var x0 = from > rowStart ? a.MeasureWidth(display.Substring(rowStart, from - rowStart), a.FontId) : 0f;
            var x1 = to > rowStart ? a.MeasureWidth(display.Substring(rowStart, to - rowStart), a.FontId) : 0f;
            // The row's own break is inside the selection: show it as a stub past
            // the last glyph, so a fully-selected (or empty) row still reads as
            // selected rather than as a gap.
            if (row < lastRow) x1 += SelfEditGeometry.NewlineStubWidth;
            if (!SelfEditGeometry.SelectionSpan(textX, x0, x1, left, right, out var bandX, out var bandW))
                continue;
            var bandTop = textY + a.RowTop(row) + inkTop;
            if (!SelfEditGeometry.ClipY(bandTop, inkHeight, o.ContentTop, o.ContentBottom, out var bandY, out var bandH))
                continue;
            o.AddSelRect(bandX, bandY, bandW, bandH);
        }
    }

    private static float Px(in Val v) => v.Type == ValType.Px ? v.Value : 0f;
}

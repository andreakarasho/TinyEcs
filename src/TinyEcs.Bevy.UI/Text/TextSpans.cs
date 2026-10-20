// Per-span text colours for a Bevy.UI `Text` node.
//
// Clay carries exactly ONE colour per text command (TextRenderData.TextColor), so a
// syntax-coloured line used to mean one entity per token. That works at rest and not at
// all inside an editable node: a self-editable field is ONE Text node, and splitting it
// would take the caret/selection maths with it. So the colours ride BESIDE the string,
// keyed by buffer index, and are resolved per glyph at draw time — the placement maths is
// untouched, which is why the caret stays pixel-identical with spans on.
//
// The table below is the bridge from a render command back to the entity: the span arrays
// are collected once per frame by the host renderer and keyed by the id Clay gives
// a node's OWN text, the same id SelfEditOverlay paints its caret on.

using ClayColor = Clay.Color;

namespace TinyEcs.Bevy.UI;

/// <summary>One coloured run of a <c>Text</c> node's buffer: <c>[Start, Start+Length)</c>
/// in the SAME character indices the editor and the caret use.</summary>
public struct TextSpan
{
    public int Start;
    public int Length;
    public ClayColor Color;
}

/// <summary>
/// Optional companion of <c>Text</c>: every character covered by a span draws in that
/// span's colour, everything else in the node's <c>TextColor</c>.
/// <para>Spans must be ordered by <see cref="TextSpan.Start"/> and must not overlap — the
/// renderer walks them with a single forward cursor, so anything out of order, negative or
/// past the end of the buffer is simply ignored (a guest can't corrupt the draw).</para>
/// <para>A renderer whose font bakes its colour into the glyph has no per-glyph tint to
/// override and ignores spans on such a node.</para>
/// </summary>
public struct TextSpans
{
    public TextSpan[] Spans;
}

/// <summary>
/// A forward walk over an ordered span list. The glyph loop visits buffer indices in
/// increasing order (rows in order, characters in order within a row), so one cursor
/// resolves the whole run without re-scanning — and a span list that is NOT ordered
/// simply loses the entries the cursor has already walked past, which is the documented
/// "ignored" behaviour rather than a fault.
/// </summary>
public struct TextSpanCursor
{
    private int _index;

    /// <summary>The colour covering <paramref name="charIndex"/>, if any.
    /// <paramref name="charIndex"/> must not go backwards between calls.</summary>
    public bool TryColor(ReadOnlySpan<TextSpan> spans, int charIndex, out ClayColor color)
    {
        while (_index < spans.Length && spans[_index].Start + spans[_index].Length <= charIndex)
            _index++;
        if (_index < spans.Length && charIndex >= spans[_index].Start)
        {
            color = spans[_index].Color;
            return true;
        }
        color = default;
        return false;
    }
}

/// <summary>
/// The frame's span arrays, keyed by the render-command id of each owning node's text.
/// Rebuilt at the top of the GUI render pass; empty (and free) on every frame where no
/// entity carries <see cref="TextSpans"/>.
/// </summary>
public sealed class TextSpanTable
{
    private readonly Dictionary<uint, TextSpan[]> _byTextId = new();

    public int Count => _byTextId.Count;

    public void Clear()
    {
        if (_byTextId.Count != 0)
            _byTextId.Clear();
    }

    public void Add(ulong entity, TextSpan[]? spans)
    {
        if (spans == null || spans.Length == 0)
            return;
        _byTextId[SelfEditGeometry.TextClayId(SelfEditGeometry.NodeClayId(entity))] = spans;
    }

    public TextSpan[]? Find(uint textCommandId) =>
        _byTextId.TryGetValue(textCommandId, out var spans) ? spans : null;
}

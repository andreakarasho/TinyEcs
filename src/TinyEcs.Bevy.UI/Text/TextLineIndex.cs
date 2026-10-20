// Hard-newline line index over the live edit buffer. Backs the MULTI-LINE
// self-editable field (Text + TextInput + EditableText{Multiline} — the
// code-editor shape): stb walks rows through ActiveTextEdit.LayoutRow, the
// caret/selection overlay lays out per line, and both need "which line is
// index N on, where does that line start/end" without re-wrapping text.
//
// Lines break at '\n' ONLY. That is the contract of a code field: word wrap is
// off (TextWrap.None) so a rendered line and a buffer line are the
// same thing, and the caret math needs no font measurement to find them.
//
// Rebuilt in place from the StringBuilder (no per-frame allocation once the
// backing list has grown): the overlay runs every frame and stb calls LayoutRow
// several times per keystroke.

using System.Text;

namespace TinyEcs.Bevy.UI;

public sealed class TextLineIndex
{
    // Buffer index each line starts at. Always non-empty after Rebuild: an empty
    // buffer is one empty line, and a trailing '\n' opens a further empty line.
    private readonly List<int> _starts = new() { 0 };

    public int Count => _starts.Count;
    public int TextLength { get; private set; }

    public void Rebuild(StringBuilder buffer)
    {
        _starts.Clear();
        _starts.Add(0);
        var len = buffer.Length;
        for (var i = 0; i < len; i++)
            if (buffer[i] == '\n')
                _starts.Add(i + 1);
        TextLength = len;
    }

    public int Clamp(int line) => line < 0 ? 0 : line >= _starts.Count ? _starts.Count - 1 : line;

    public int Start(int line) => _starts[Clamp(line)];

    /// End of the line's TEXT: the terminating '\n' is excluded, so
    /// End - Start is the caret column count on that line.
    public int End(int line)
    {
        line = Clamp(line);
        return line + 1 < _starts.Count ? _starts[line + 1] - 1 : TextLength;
    }

    /// Row length as stb wants it: the terminating '\n' INCLUDED, so
    /// rowStart + NumChars is the next row's start.
    public int NumChars(int line)
    {
        line = Clamp(line);
        return (line + 1 < _starts.Count ? _starts[line + 1] : TextLength) - _starts[line];
    }

    /// Line containing a caret index. A caret sitting right after a '\n'
    /// belongs to the line the newline OPENED, not the one it closed — which is
    /// exactly what counting the newlines before the index gives.
    public int LineOf(int index)
    {
        if (index <= 0) return 0;
        var found = _starts.BinarySearch(index);
        return found >= 0 ? found : ~found - 1;
    }

    public int ColumnOf(int index)
    {
        var line = LineOf(index);
        return index - _starts[line];
    }

    /// Caret index for a (line, column), both clamped into the buffer.
    public int IndexOf(int line, int column)
    {
        line = Clamp(line);
        var start = _starts[line];
        var end = End(line);
        if (column <= 0) return start;
        return start + column > end ? end : start + column;
    }
}

using Clay;

namespace TinyEcs.Bevy.UI;

/// <summary>One laid-out row of a wrapped run: <c>[Start, Start+Count)</c> in buffer
/// indices (Count includes a trailing space / '\n'), its width and its height (the
/// row PITCH the renderer stacks by, which may vary per row).</summary>
public struct TextRow
{
	public int Start, Count;
	public float Width, Height;

	public TextRow(int start, int count, float width, float height)
	{
		Start = start; Count = count; Width = width; Height = height;
	}
}

/// <summary>
/// Caret metrics + line wrapping on top of the layout measurer. The text editor
/// measures caret x, selection spans, click hit-tests and row walks through it, so
/// it must agree glyph-for-glyph with what the host renderer draws. A host whose
/// <see cref="UiPlugin.TextMeasurer"/> implements this gets exact carets; otherwise
/// <see cref="TextEditMeasurer"/> derives them from <see cref="ITextMeasurer"/>.
/// </summary>
public interface ITextEditMeasurer : ITextMeasurer
{
	/// <summary>Advance width of an unwrapped run.</summary>
	float MeasureWidth(string text, ushort fontId, ushort fontSize);

	/// <summary>Line pitch of the font (fallback row height).</summary>
	float LineHeight(ushort fontId, ushort fontSize);

	/// <summary>Glyph-ink band within a line: Top = where cap ink begins, Height =
	/// cap top to descender bottom, Cell = the full line cell. Carets and selection
	/// bands are sized to the ink, not the cell.</summary>
	(float Top, float Height, float Cell) CaretMetrics(ushort fontId, ushort fontSize);

	/// <summary>Rows <paramref name="text"/> breaks into at <paramref name="wrapWidth"/>
	/// (hard '\n' always breaks), exactly as the renderer stacks them. <paramref name="style"/>
	/// is the host's opaque layout style (<see cref="MultilineText.Style"/>), null = plain.
	/// May emit no row for an empty text or a trailing empty line.</summary>
	void WrapRows(string text, ushort fontId, ushort fontSize, int wrapWidth, object? style, List<TextRow> rows);
}

/// <summary>Resolves the editor metrics for a layout measurer.</summary>
public static class TextEditMeasurer
{
	public static ITextEditMeasurer For(ITextMeasurer measurer)
		=> measurer as ITextEditMeasurer ?? new Fallback(measurer);

	/// <summary>Metrics derived from a plain <see cref="ITextMeasurer"/>: widths from
	/// MeasureText, line pitch / ink from "Wg", greedy word wrap.</summary>
	private sealed class Fallback : ITextEditMeasurer
	{
		private readonly ITextMeasurer _m;
		public Fallback(ITextMeasurer m) => _m = m;

		public Dimensions MeasureText(ReadOnlySpan<char> text, ushort fontId, ushort fontSize, ushort letterSpacing, float maxWidth = 0)
			=> _m.MeasureText(text, fontId, fontSize, letterSpacing, maxWidth);

		public float MeasureWidth(string text, ushort fontId, ushort fontSize)
			=> text.Length == 0 ? 0 : _m.MeasureText(text, fontId, fontSize, 0).Width;

		public float LineHeight(ushort fontId, ushort fontSize)
		{
			var h = _m.MeasureText("Wg", fontId, fontSize, 0).Height;
			return h > 0 ? h : fontSize;
		}

		public (float Top, float Height, float Cell) CaretMetrics(ushort fontId, ushort fontSize)
		{
			var h = LineHeight(fontId, fontSize);
			return (0, h, h);
		}

		public void WrapRows(string text, ushort fontId, ushort fontSize, int wrapWidth, object? style, List<TextRow> rows)
		{
			var lh = LineHeight(fontId, fontSize);
			var lineStart = 0;
			while (lineStart < text.Length)
			{
				var nl = text.IndexOf('\n', lineStart);
				var lineEnd = nl < 0 ? text.Length : nl;
				var rowStart = lineStart;
				while (true)
				{
					var fit = lineEnd;
					if (wrapWidth > 0 && MeasureWidth(text.Substring(rowStart, lineEnd - rowStart), fontId, fontSize) > wrapWidth)
					{
						// Break after the last space that keeps the row inside the width;
						// a single over-long word breaks at the last fitting char.
						fit = rowStart;
						for (var i = rowStart; i < lineEnd; i++)
						{
							if (MeasureWidth(text.Substring(rowStart, i - rowStart + 1), fontId, fontSize) > wrapWidth) break;
							fit = i + 1;
						}
						var space = text.LastIndexOf(' ', Math.Max(rowStart, fit - 1), Math.Max(0, fit - rowStart));
						if (space >= rowStart && space + 1 < lineEnd) fit = space + 1;
						if (fit == rowStart) fit = rowStart + 1;
					}
					var last = fit >= lineEnd;
					var count = (last && nl >= 0 ? lineEnd + 1 : fit) - rowStart;
					rows.Add(new TextRow(rowStart, count,
						MeasureWidth(text.Substring(rowStart, fit - rowStart).TrimEnd(' '), fontId, fontSize), lh));
					rowStart += count;
					if (last) break;
				}
				lineStart = nl < 0 ? text.Length : nl + 1;
			}
		}
	}
}

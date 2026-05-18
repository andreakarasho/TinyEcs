using System.Runtime.CompilerServices;
using Clay;

namespace TinyEcs.Bevy.UI;

internal static class ClayMap
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SizingAxis ToSizing(in Val val, in Val min, in Val max, float scale)
	{
		var minPx = min.Type == ValType.Px ? min.Value * scale : 0f;
		var maxPx = max.Type == ValType.Px ? max.Value * scale : float.MaxValue;

		return val.Type switch
		{
			ValType.Px      => SizingAxis.Fixed(val.Value * scale),
			ValType.Percent => new SizingAxis { Percent = val.Value, Type = SizingType.Percent },
			_               => SizingAxis.Fit(minPx, maxPx),
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Padding ToPadding(in UiRect r, float scale)
	{
		return new Padding(
			(ushort)MathF.Max(0, r.Left.Value * scale),
			(ushort)MathF.Max(0, r.Right.Value * scale),
			(ushort)MathF.Max(0, r.Top.Value * scale),
			(ushort)MathF.Max(0, r.Bottom.Value * scale));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BorderWidth ToBorderWidth(in UiRect r, float scale)
	{
		return new BorderWidth(
			(ushort)MathF.Max(0, r.Left.Value * scale),
			(ushort)MathF.Max(0, r.Right.Value * scale),
			(ushort)MathF.Max(0, r.Top.Value * scale),
			(ushort)MathF.Max(0, r.Bottom.Value * scale));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AlignX MapJustify(JustifyContent j) => j switch
	{
		JustifyContent.Center => AlignX.Center,
		JustifyContent.End    => AlignX.Right,
		_                     => AlignX.Left,
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AlignY MapAlign(AlignItems a) => a switch
	{
		AlignItems.Center => AlignY.Center,
		AlignItems.End    => AlignY.Bottom,
		_                 => AlignY.Top,
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static LayoutDirection MapDirection(FlexDirection d)
		=> d == FlexDirection.Column ? LayoutDirection.TopToBottom : LayoutDirection.LeftToRight;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static CornerRadius ToCornerRadius(in BorderRadius r, float scale)
		=> new(r.TopLeft * scale, r.TopRight * scale, r.BottomLeft * scale, r.BottomRight * scale);
}

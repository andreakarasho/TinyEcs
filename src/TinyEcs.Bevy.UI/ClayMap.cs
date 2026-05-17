using System.Runtime.CompilerServices;
using Clay;

namespace TinyEcs.Bevy.UI;

internal static class ClayMap
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static SizingAxis ToSizing(in Val val, in Val min, in Val max)
	{
		var minPx = min.Type == ValType.Px ? min.Value : 0f;
		var maxPx = max.Type == ValType.Px ? max.Value : float.MaxValue;

		return val.Type switch
		{
			ValType.Px      => SizingAxis.Fixed(val.Value),
			ValType.Percent => new SizingAxis { Percent = val.Value, Type = SizingType.Percent },
			_               => SizingAxis.Fit(minPx, maxPx),
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Padding ToPadding(in UiRect r)
	{
		return new Padding(
			(ushort)MathF.Max(0, r.Left.Value),
			(ushort)MathF.Max(0, r.Right.Value),
			(ushort)MathF.Max(0, r.Top.Value),
			(ushort)MathF.Max(0, r.Bottom.Value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BorderWidth ToBorderWidth(in UiRect r)
	{
		return new BorderWidth(
			(ushort)MathF.Max(0, r.Left.Value),
			(ushort)MathF.Max(0, r.Right.Value),
			(ushort)MathF.Max(0, r.Top.Value),
			(ushort)MathF.Max(0, r.Bottom.Value));
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
	public static CornerRadius ToCornerRadius(in BorderRadius r)
		=> new(r.TopLeft, r.TopRight, r.BottomLeft, r.BottomRight);
}

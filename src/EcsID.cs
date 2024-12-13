namespace TinyEcs;

public static class EcsIdEx
{
#if USE_PAIR
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPair(this EcsID id)
		=> IDOp.IsPair(id);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EcsID First(this EcsID id)
		=> id.IsPair() ? IDOp.GetPairFirst(id) : 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EcsID Second(this EcsID id)
		=> id.IsPair() ? IDOp.GetPairSecond(id) : 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static (EcsID first, EcsID second) Pair(this EcsID id)
		=> (IDOp.GetPairFirst(id), IDOp.GetPairSecond(id));
		// => id.IsPair() ? (IDOp.GetPairFirst(id), IDOp.GetPairSecond(id)) : (0, 0);
#endif

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsValid(this EcsID id)
		=> id != 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EcsID RealId(this EcsID id)
		=> IDOp.RealID(id);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Generation(this EcsID id)
		=> (int) IDOp.GetGeneration(id);
}

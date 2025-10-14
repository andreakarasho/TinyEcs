namespace TinyEcs;

public static class Extensions
{
	public static void SortNoAlloc<T>(this Span<T> span, Comparison<T> comparison)
	{
#if NET
		MemoryExtensions.Sort(span, comparison);
#else
		span.Sort(comparison);
#endif
	}
}

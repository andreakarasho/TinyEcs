namespace TinyEcs;

internal static class UnorderedSetHasher
{
	private const ulong Prime = 0x9E3779B185EBCA87UL; // Large 64-bit prime (Golden ratio)

	public static ulong HashUnordered(Span<ulong> values)
	{
		ulong hash = 0;

		foreach (ref readonly var value in values)
		{
			hash ^= Mix(value);
			hash *= Prime;
		}

		return hash;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong Combine(ulong currentHash, ulong mixed)
	{
		return (currentHash ^ mixed) * Prime;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong Mix(ulong x)
	{
		// A simple mixer (variant of MurmurHash3 finalizer)
		x ^= x >> 30;
		x *= 0xbf58476d1ce4e5b9UL;
		x ^= x >> 27;
		x *= 0x94d049bb133111ebUL;
		x ^= x >> 31;
		return x;
	}
}

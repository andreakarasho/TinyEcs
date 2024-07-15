namespace TinyEcs;

public struct RollingHash
{
	private const ulong Base = 31; // A prime base for hashing
    private const ulong Modulus = 1_000_000_007; // A large prime modulus

    private ulong _hash;
    private ulong _basePower;

	private static readonly ulong _inverseCache = ModInverse2(Base, Modulus);


    public RollingHash()
    {
        _hash = 0;
        _basePower = 1;
    }

	public readonly ulong Hash => _hash;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(ulong value)
    {
        _hash = (_hash * Base + value) % Modulus;
        _basePower = (_basePower * Base) % Modulus;
    }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(ulong value)
    {
        _basePower = (_basePower * _inverseCache) % Modulus;
        _hash = (_hash + Modulus - (value * _basePower % Modulus)) % Modulus;
    }


    // Compute modular inverse of a with respect to m using Extended Euclidean Algorithm
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ModInverse2(ulong a, ulong m)
    {
	    ulong m0 = m, x0 = 0, x1 = 1;

	    while (a > 1)
	    {
		    ulong q = a / m;
		    ulong t = m;

		    m = a % m;
		    a = t;
		    t = x0;

		    x0 = x1 - q * x0;
		    x1 = t;
	    }

	    return (x1 + m0) % m0;
    }
}

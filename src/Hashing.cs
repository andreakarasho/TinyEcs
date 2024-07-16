namespace TinyEcs;


public struct RollingHash
{
	private const ulong Base = 31; // A prime base for hashing
    private const ulong Modulus = 1_000_000_007; // A large prime modulus

    private ulong _hash;
    private ulong _product;

	private static readonly ulong _inverseCache = ModInverse2(Base, Modulus);


    public RollingHash()
    {
        _hash = 0;
        _product = 1;
    }

	public readonly ulong Hash => _hash;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(ulong value)
    {
		_hash = (_hash + value * _product) % Modulus;
        _product = (_product * Base) % Modulus;
    }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(ulong value)
    {
		var inverseBase = _inverseCache;
        _product = (_product * inverseBase) % Modulus;
        _hash = (_hash + Modulus - (value * _product % Modulus)) % Modulus;
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

	public static ulong Calculate(params Span<ulong> values)
	{
		var hash = 0ul;
		var product = 1ul;

		foreach (ref var value in values)
		{
			hash = (hash + value * product) % Modulus;
        	product = (product * Base) % Modulus;
		}

		return hash;
	}
}

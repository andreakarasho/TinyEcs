namespace TinyEcs;

internal static class Hashing
{
	const ulong FIXED = 314159;

	public static ulong Calculate(ReadOnlySpan<ComponentInfo> components)
	{
		var hc = (ulong)components.Length;
		foreach (ref readonly var val in components)
			hc = unchecked(hc * FIXED + val.ID);
		return hc;
	}

	public static ulong Calculate(ReadOnlySpan<Term> terms)
	{
		var hc = (ulong)terms.Length;
		foreach (ref readonly var val in terms)
			hc = unchecked(hc * FIXED + (ulong)val.IDs.Select(s => s.ID).Sum(s => s.ID) + (byte)val.Op);
		return hc;
	}

	public static ulong Calculate(ReadOnlySpan<QueryTerm> terms)
	{
		var hc = (ulong)terms.Length;
		foreach (ref readonly var val in terms)
			hc = unchecked(hc * FIXED + (ulong)val.Id + (byte)val.Op);
		return hc;
	}

	public static ulong Calculate(IEnumerable<EcsID> terms)
	{
		var hc = (ulong)terms.Count();
		foreach (var val in terms)
			hc = unchecked(hc * FIXED + val);
		return hc;
	}
}

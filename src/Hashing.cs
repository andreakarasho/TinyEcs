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

	public static ulong Calculate(ReadOnlySpan<IQueryTerm> terms)
	{
		var hc = (ulong)terms.Length;
		foreach (ref readonly var val in terms)
			hc = unchecked(hc * FIXED + (ulong)val.Id + (byte)val.Op +
				(val is ContainerQueryTerm container ? container.Terms.Aggregate(0Ul, static (a, b) => a + b.Id) : 0));
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

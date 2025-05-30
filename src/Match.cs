using System.Collections.Frozen;

namespace TinyEcs;

public enum ArchetypeSearchResult
{
	Continue,
	Found,
	Stop
}

public static class FilterMatch
{
	public static ArchetypeSearchResult Match(Archetype archetype, ReadOnlySpan<IQueryTerm> terms)
	{
		foreach (ref readonly var term in terms)
		{
			var result = term.Match(archetype);

			if (result == ArchetypeSearchResult.Stop || (term.Op == TermOp.With && result == ArchetypeSearchResult.Continue))
				return result;
		}

		return ArchetypeSearchResult.Found;
	}
}

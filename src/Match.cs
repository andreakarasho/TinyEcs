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
	public static ArchetypeSearchResult Match(FrozenSet<EcsID> archetypeIds, ReadOnlySpan<IQueryTerm> terms)
	{
		foreach (ref readonly var term in terms)
		{
			switch (term.Op)
			{
				case TermOp.With:

					if (!archetypeIds.Contains(term.Id))
						return ArchetypeSearchResult.Continue;

					break;

				case TermOp.Without:

					if (archetypeIds.Contains(term.Id))
						return ArchetypeSearchResult.Stop;

					break;
			}
		}

		return ArchetypeSearchResult.Found;
	}
}

namespace TinyEcs;

public enum ArchetypeSearchResult
{
	Continue,
	Found,
	Stop
}

public static class FilterMatch
{
	public static ArchetypeSearchResult Match(ReadOnlySpan<EcsID> archetypeIds, ReadOnlySpan<IQueryTerm> terms)
	{
		int i = 0, j = 0;

		while (i < archetypeIds.Length && j < terms.Length)
		{
			switch (terms[j].Op)
			{
				case TermOp.DataAccess:
				case TermOp.With:

					if (archetypeIds[i] == terms[j].Id)
						j += 1;

					break;

				case TermOp.Without:

					if (archetypeIds[i] == terms[j].Id)
						return ArchetypeSearchResult.Stop;

					if (archetypeIds[i] > terms[j].Id)
						j += 1;

					break;

				default:
				case TermOp.Optional:
					j += 1;
					break;
			}

			i += 1;
		}

		while (j < terms.Length)
		{
			if (terms[j].Op is not TermOp.Without or TermOp.Optional)
				break;

			j += 1;
		}

		return j == terms.Length ? ArchetypeSearchResult.Found : ArchetypeSearchResult.Continue;
	}
}

namespace TinyEcs;

static class Match
{
	public static int Validate(IComparer<ulong> comparer, ReadOnlySpan<ComponentInfo> ids, ReadOnlySpan<Term> terms)
	{
		int idIndex = 0;
        int termIndex = 0;

        while (idIndex < ids.Length && termIndex < terms.Length)
        {
            var id = ids[idIndex].ID;
            ref readonly var term = ref terms[termIndex];

			if (comparer.Compare(id.Value, term.ID.Value) == 0)
			{
				switch (term.Op)
                {
                    case TermOp.With:
                        termIndex++;
                        break;
                    case TermOp.Without:
                        return -1; // Forbidden ID found
                    case TermOp.Optional:
                        termIndex++;
                        break;
                }
                idIndex++;
			}
			else if (id < term.ID)
            {
                idIndex++;
            }
            else if (id > term.ID)
            {
                if (term.Op == TermOp.With)
                {
                    return 1; // Required ID not found
                }
                termIndex++;
            }
        }

        // Check any remaining required terms
        while (termIndex < terms.Length)
        {
            if (terms[termIndex].Op == TermOp.With)
            {
                return 1; // Required ID not found
            }
            termIndex++;
        }

        return 0;
	}
}

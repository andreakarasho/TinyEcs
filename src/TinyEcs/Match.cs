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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ArchetypeSearchResult MatchBits(Archetype archetype, ulong[] with, ulong[] without)
	{
		var bits = archetype.ComponentBits;
		var len = bits.Length;
		for (var i = 0; i < len; i++)
		{
			var b = bits[i];
			if ((b & without[i]) != 0) return ArchetypeSearchResult.Stop;
			if ((b & with[i]) != with[i]) return ArchetypeSearchResult.Continue;
		}
		return ArchetypeSearchResult.Found;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ArchetypeSearchResult MatchSwitch(Archetype archetype, ulong[] ids, TermOp[] ops)
	{
		for (var i = 0; i < ids.Length; i++)
		{
			var has = archetype.HasIndex(ids[i]);
			switch (ops[i])
			{
				case TermOp.Without:
					if (has) return ArchetypeSearchResult.Stop;
					break;
				case TermOp.With:
					if (!has) return ArchetypeSearchResult.Continue;
					break;
				case TermOp.Optional:
					break;
			}
		}
		return ArchetypeSearchResult.Found;
	}
}

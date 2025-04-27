using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

public interface IQueryTerm : IComparable<IQueryTerm>
{
	EcsID Id { get; init; }
	TermOp Op { get; init; }

	int IComparable<IQueryTerm>.CompareTo([NotNull] IQueryTerm? other)
	{
		var res = Id.CompareTo(other!.Id);
		if (res != 0)
			return res;
		return Op.CompareTo(other.Op);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	ArchetypeSearchResult Match(Archetype archetype);
}

[DebuggerDisplay("{Id} - {Op}")]
public readonly struct WithTerm(EcsID id) : IQueryTerm
{
	public ulong Id { get; init; } = id;
	public TermOp Op { get; init; } = TermOp.With;

	public readonly ArchetypeSearchResult Match(Archetype archetype)
	{
		return archetype.HasIndex(Id) ? ArchetypeSearchResult.Found : ArchetypeSearchResult.Continue;
	}
}

[DebuggerDisplay("{Id} - {Op}")]
public readonly struct WithoutTerm(EcsID id) : IQueryTerm
{
	public ulong Id { get; init; } = id;
	public TermOp Op { get; init; } = TermOp.Without;

	public readonly ArchetypeSearchResult Match(Archetype archetype)
	{
		return archetype.HasIndex(Id) ? ArchetypeSearchResult.Stop : ArchetypeSearchResult.Continue;
	}
}

[DebuggerDisplay("{Id} - {Op}")]
public readonly struct OptionalTerm(EcsID id) : IQueryTerm
{
	public ulong Id { get; init; } = id;
	public TermOp Op { get; init; } = TermOp.Optional;

	public readonly ArchetypeSearchResult Match(Archetype archetype)
	{
		return ArchetypeSearchResult.Found;
	}
}

[DebuggerDisplay("{Id} - {Op}")]
public readonly struct ChangedTerm(EcsID id) : IQueryTerm
{
	public ulong Id { get; init; } = id;
	public TermOp Op { get; init; } = TermOp.With;

	public readonly ArchetypeSearchResult Match(Archetype archetype)
	{
		return archetype.HasIndex(Id) ? ArchetypeSearchResult.Found : ArchetypeSearchResult.Continue;
	}
}


public enum TermOp : byte
{
	With,
	Without,
	Optional,
	Changed
}

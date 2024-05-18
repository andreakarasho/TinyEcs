using System.Collections.Immutable;

namespace TinyEcs;

[DebuggerDisplay("{IDs} - {Op}")]
public readonly struct Term : IComparable<Term>
{
	public readonly ImmutableSortedSet<EcsID> IDs;
    public readonly TermOp Op;

    public Term(EcsID id, TermOp op)
    {
        IDs = new SortedSet<EcsID>() { id }.ToImmutableSortedSet();
        Op = op;
    }

    public Term(EcsID[] ids, TermOp op)
    {
        IDs = new SortedSet<EcsID>(ids).ToImmutableSortedSet();
        Op = op;
    }

    public readonly int CompareTo(Term other)
    {
        return IDs[0].CompareTo(other.IDs[0]);
    }

	public static implicit operator ulong (Term term) => term.IDs[0];
	public static implicit operator Term (ulong id) => new (id, TermOp.With);
}

public enum TermOp : byte
{
	With,
    Without,
    Optional,
    AtLeastOne,
    Exactly,
    None
}

public interface IFilter { }

public readonly struct With<T> : IFilter where T : struct {
	static readonly Term Term = new (Lookup.Component<T>.Value.ID, TermOp.With);

	public static implicit operator Term(With<T> _) => Term;
}
public readonly struct Without<T> : IFilter where T : struct { }
public readonly struct Not<T> : IFilter where T : struct { }
public readonly struct Or<T> : IFilter where T : struct { }
public readonly struct Optional<T> : IFilter where T : struct { }

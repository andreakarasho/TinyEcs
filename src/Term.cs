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

    public Term(IEnumerable<EcsID> ids, TermOp op)
    {
        IDs = new SortedSet<EcsID>(ids).ToImmutableSortedSet();
        Op = op;
    }

    public readonly int CompareTo(Term other)
    {
		var idComparison = IDs[0].CompareTo(other.IDs[0]);
        if (idComparison != 0)
        {
            return idComparison;
        }
        return Op.CompareTo(other.Op);
    }
}

public enum TermOp : byte
{
	With,
    Without,
    Optional,
    AtLeastOne,
    Exactly,
    None,
	Or
}

public interface IFilter { }

public readonly struct With<T> : IFilter where T : struct { }
public readonly struct Without<T> : IFilter where T : struct { }
public readonly struct Not<T> : IFilter where T : struct { }
public readonly struct Optional<T> : IFilter where T : struct { }
public readonly struct AtLeast<T> : ITuple, IAtLeast, IFilter where T : ITuple
{
	static readonly ITuple _value = default(T)!;

	public object this[int index] => _value[index];

	public int Length => _value.Length;
}
public readonly struct Exactly<T> : ITuple, IExactly, IFilter where T : ITuple
{
	static readonly ITuple _value = default(T)!;

	public object this[int index] => _value[index];

	public int Length => _value.Length;
}
public readonly struct None<T> : ITuple, INone, IFilter where T : ITuple
{
	static readonly ITuple _value = default(T)!;

	public object this[int index] => _value[index];

	public int Length => _value.Length;
}

public readonly struct Or<T> : ITuple, IOr, IFilter where T : ITuple
{
	static readonly ITuple _value = default(T)!;

	public object this[int index] => _value[index];

	public int Length => _value.Length;
}


public interface IAtLeast { }
public interface IExactly { }
public interface INone { }
public interface IOr { }

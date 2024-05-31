using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

[DebuggerDisplay("{Id} - {Op}")]
public class QueryTerm(EcsID id, TermOp op) : IComparable<QueryTerm>
{
	public EcsID Id { get; } = id;
	public TermOp Op { get; } = op;

	public int CompareTo([NotNull] QueryTerm? other)
	{
		var res = Id.CompareTo(other!.Id);
		if (res != 0)
			return res;
		return Op.CompareTo(other.Op);
	}
}

public class ContainerQueryTerm(QueryTerm[] terms, TermOp op) : QueryTerm(0, op)
{
	public QueryTerm[] Terms { get; } = terms;
}

public enum TermOp : byte
{
	DataAccess,
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
//public readonly struct Not<T> : IFilter where T : struct { }
public readonly struct Optional<T> where T : struct { }
public readonly struct AtLeast<T> : ITuple, IAtLeast, IFilter where T : struct, ITuple
{
	static readonly ITuple _value = default(T)!;

	public object? this[int index] => _value[index];

	public int Length => _value.Length;
}
public readonly struct Exactly<T> : ITuple, IExactly, IFilter where T : struct, ITuple
{
	static readonly ITuple _value = default(T)!;

	public object? this[int index] => _value[index];

	public int Length => _value.Length;
}
public readonly struct None<T> : ITuple, INone, IFilter where T : struct, ITuple
{
	static readonly ITuple _value = default(T)!;

	public object? this[int index] => _value[index];

	public int Length => _value.Length;
}
public readonly struct Or<T> : IOr, IFilter where T : struct, ITuple
{
	static readonly T _value = default;

	ITuple IOr.Value => _value;
}


public interface IAtLeast : IFilter { }
public interface IExactly : IFilter { }
public interface INone : IFilter { }
public interface IOr : IFilter { internal ITuple Value { get; } }


using System.Collections.Immutable;
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
}

[DebuggerDisplay("{Id} - {Op}")]
public readonly struct QueryTerm(EcsID id, TermOp op) : IQueryTerm
{
	public EcsID Id { get; init; } = id;
	public TermOp Op { get; init; } = op;
}

[DebuggerDisplay("{Id} - {Op} - {Terms}")]
public class ContainerQueryTerm(IQueryTerm[] terms, TermOp op) : IQueryTerm
{
	public EcsID Id { get; init; } = 0;
	public TermOp Op { get; init; } = op;
	public ImmutableArray<IQueryTerm> Terms { get; } = [.. terms];
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

/// <summary>
/// Used in query filters to find entities with the corrisponding component/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct With<T> : IWith where T : struct
{
	static readonly T _value = default;

	object IWith.Value => _value;
}

/// <summary>
/// Used in query filters to find entities without the corrisponding component/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct Without<T> : IWithout where T : struct
{
	static readonly T _value = default;

	object IWithout.Value => _value;
}

/// <summary>
/// Used in query filters to find entities with or without the corrisponding component/tag.<br/>
/// You would Unsafe.IsNullRef&lt;T&gt;(); to check if the value has been found.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct Optional<T> : IOptional where T : struct
{
	static readonly T _value = default;

	object IOptional.Value => _value;
}

/// <summary>
/// Used in query filters to find entities with any of the corrisponding components/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct AtLeast<T> : ITuple, IAtLeast, IFilter where T : struct, ITuple
{
	static readonly ITuple _value = default(T)!;

	public object? this[int index] => _value[index];

	public int Length => _value.Length;
}

/// <summary>
/// Used in query filters to find entities with exactly the corrisponding components/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct Exactly<T> : ITuple, IExactly, IFilter where T : struct, ITuple
{
	static readonly ITuple _value = default(T)!;

	public object? this[int index] => _value[index];

	public int Length => _value.Length;
}

/// <summary>
/// Used in query filters to find entities with none the corrisponding components/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct None<T> : ITuple, INone, IFilter where T : struct, ITuple
{
	static readonly ITuple _value = default(T)!;

	public object? this[int index] => _value[index];

	public int Length => _value.Length;
}

/// <summary>
/// Used in query filters to accomplish the 'or' logic.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct Or<T> : IOr, IFilter where T : struct, ITuple
{
	static readonly T _value = default;

	ITuple IOr.Value => _value;
}


public interface IOptional { internal object Value { get; } }
public interface IWith : IFilter { internal object Value { get; } }
public interface IWithout : IFilter { internal object Value { get; } }
public interface IAtLeast : IFilter { }
public interface IExactly : IFilter { }
public interface INone : IFilter { }
public interface IOr : IFilter { internal ITuple Value { get; } }

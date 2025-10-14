using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TinyEcs.Bevy;

public interface ITermCreator
{
	public static abstract void Build(QueryBuilder builder);
}

public interface IQueryIterator<TData>
	where TData : struct, allows ref struct
{
	TData GetEnumerator();

	[UnscopedRef]
	ref TData Current { get; }

	bool MoveNext();
}

public interface IData<TData> : ITermCreator, IQueryIterator<TData>
	where TData : struct, allows ref struct
{
	public static abstract TData CreateIterator(QueryIterator iterator);
}

public interface IFilter<TFilter> : ITermCreator, IQueryIterator<TFilter>
	where TFilter : struct, allows ref struct
{
	void SetTicks(uint lastRun, uint thisRun);
	public static abstract TFilter CreateIterator(QueryIterator iterator);
}

[SkipLocalsInit]
public ref struct Empty : IData<Empty>, IFilter<Empty>, TinyEcs.IQueryComponentAccess, TinyEcs.IQueryFilterAccess
{
	private readonly bool _asFilter;
	private QueryIterator _iterator;

	private static readonly System.Type[] s_emptyTypes = System.Array.Empty<System.Type>();
	public static System.ReadOnlySpan<System.Type> ReadComponents => s_emptyTypes;
	public static System.ReadOnlySpan<System.Type> WriteComponents => s_emptyTypes;

	internal Empty(QueryIterator iterator, bool asFilter)
	{
		_iterator = iterator;
		_asFilter = asFilter;
	}

	public static void Build(QueryBuilder builder) { }


	[UnscopedRef]
	public ref Empty Current => ref this;

	public readonly void Deconstruct(out ReadOnlySpan<EntityView> entities, out int count)
	{
		entities = _iterator.Entities();
		count = entities.Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Empty GetEnumerator() => this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool MoveNext() => _asFilter || _iterator.Next();

	public readonly void SetTicks(uint lastRun, uint thisRun) { }

	static Empty IData<Empty>.CreateIterator(QueryIterator iterator)
	{
		return new Empty(iterator, false);
	}

	static Empty IFilter<Empty>.CreateIterator(QueryIterator iterator)
	{
		return new Empty(iterator, true);
	}
}

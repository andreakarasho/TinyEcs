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

public interface IData<TData> : ITermCreator
	where TData : struct, allows ref struct
{
	/// <summary>
	/// Fill <paramref name="row"/>'s column pointers + entity span from the
	/// iterator's current chunk. Called once per chunk transition.
	/// </summary>
	public static abstract void LoadChunk(ref TData row, QueryIterator iterator);

	/// <summary>
	/// Bump per-row pointers to the next element within the current chunk.
	/// Returns false when the chunk is exhausted, prompting the caller to
	/// pull the next chunk and call <see cref="LoadChunk"/>.
	/// </summary>
	public static abstract bool TryAdvance(ref TData row);
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

	static void IData<Empty>.LoadChunk(ref Empty row, QueryIterator iterator)
	{
		row._iterator = iterator;
	}

	static bool IData<Empty>.TryAdvance(ref Empty row) => false;

	static Empty IFilter<Empty>.CreateIterator(QueryIterator iterator)
	{
		return new Empty(iterator, true);
	}
}

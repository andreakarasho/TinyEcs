using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

public delegate void QueryFilterDelegateWithEntity(EntityView entity);

public sealed class QueryBuilder
{
	private readonly World _world;
	private readonly Dictionary<EcsID, IQueryTerm> _components = new();
	private Query? _query;

	internal QueryBuilder(World world) => _world = world;

	public World World => _world;

	public QueryBuilder Data<T>() where T : struct
	{
		ref readonly var cmp = ref _world.Component<T>();
		EcsAssert.Panic(cmp.Size > 0, "You can't access Tag as Component");
		return Term(new QueryTerm(cmp.ID, TermOp.DataAccess));
	}

	public QueryBuilder With<T>() where T : struct
		=> With(_world.Component<T>().ID);

#if USE_PAIR
	public QueryBuilder With<TAction, TTarget>()
		where TAction : struct
		where TTarget : struct
		=> With(_world.Component<TAction>().ID, _world.Component<TTarget>().ID);

	public QueryBuilder With<TAction>(EcsID target)
		where TAction : struct
		=> With(_world.Component<TAction>().ID, target);

	public QueryBuilder With(EcsID action, EcsID target)
		=> With(IDOp.Pair(action, target));
#endif

	public QueryBuilder With(EcsID id)
		=> Term(new QueryTerm(id, TermOp.With));

	public QueryBuilder Without<T>() where T : struct
		=> Without(_world.Component<T>().ID);

#if USE_PAIR
	public QueryBuilder Without<TAction, TTarget>()
		where TAction : struct
		where TTarget : struct
		=> Without(_world.Component<TAction>().ID, _world.Component<TTarget>().ID);

	public QueryBuilder Without<TAction>(EcsID target)
		where TAction : struct
		=> Without(_world.Component<TAction>().ID, target);

	public QueryBuilder Without(EcsID action, EcsID target)
		=> Without(IDOp.Pair(action, target));
#endif

	public QueryBuilder Without(EcsID id)
		=> Term(new QueryTerm(id, TermOp.Without));

	public QueryBuilder Optional<T>() where T : struct
	{
		ref readonly var cmp = ref _world.Component<T>();
		EcsAssert.Panic(cmp.Size > 0, "You can't access Tag as Component");
		return Optional(cmp.ID);
	}

	public QueryBuilder Optional(EcsID id)
		=> Term(new QueryTerm(id, TermOp.Optional));

	public QueryBuilder AtLeast(params EcsID[] ids)
		=> Term(new ContainerQueryTerm(ids.Select(s => new QueryTerm(s, TermOp.Optional)).Cast<IQueryTerm>().ToArray(), TermOp.AtLeastOne));

	public QueryBuilder Term(IQueryTerm term)
	{
		_components[term.Id] = term;
		return this;
	}

	public Query Build()
	{
		_query = null;
		var terms = _components.Values.ToArray();
		_query ??= new Query(_world, terms);
		return _query;
	}
}

public sealed class Query
{
	private readonly ImmutableArray<IQueryTerm> _terms;
	private readonly List<Archetype> _matchedArchetypes;
	private ulong _lastArchetypeIdMatched = 0;
	private readonly int[] _indices;

	internal Query(World world, ReadOnlySpan<IQueryTerm> terms) : this (world, terms.ToImmutableArray())
	{

	}

	internal Query(World world, ImmutableArray<IQueryTerm> terms)
	{
		World = world;
		_matchedArchetypes = new List<Archetype>();

		_terms = terms.Where(s => s.Op != TermOp.Or)
			.ToImmutableSortedSet()
			.ToImmutableArray();

		TermsAccess = terms.Where(s => s.Op == TermOp.DataAccess || s.Op == TermOp.Optional)
			.ToImmutableArray();

		_indices = new int[TermsAccess.Length];
		_indices.AsSpan().Fill(-1);
	}

	internal World World { get; }
	internal ImmutableArray<IQueryTerm> TermsAccess { get; }



	internal void Match()
	{
		if (_lastArchetypeIdMatched == World.LastArchetypeId)
			return;

		_lastArchetypeIdMatched = World.LastArchetypeId;
		_matchedArchetypes.Clear();
		World.Root.GetSuperSets(_terms.AsSpan(), _matchedArchetypes);
	}

	public int Count()
	{
		Match();
		return _matchedArchetypes.Sum(static s => s.Count);
	}

	public ref T Single<T>() where T : struct
	{
		var count = Count();
		EcsAssert.Panic(count == 1, "'Single' must match one and only one entity.");

		var it = GetEnumerator();
		if (it.MoveNext())
			return ref it.Current.GetReference<T>(it.IndexAt(0));
		return ref Unsafe.NullRef<T>();
	}

	public EntityView Single()
	{
		var count = Count();
		EcsAssert.Panic(count == 1, "Multiple entities found for a single archetype");

		var it = GetEnumerator();
		if (it.MoveNext())
			return it.Current.EntityAt(0);
		return EntityView.Invalid;
	}

	public ref T Get<T>(EcsID entity) where T : struct
	{
		ref var record = ref World.GetRecord(entity);
		var idx = record.Archetype.GetComponentIndex<T>();
		if (idx < 0)
			return ref Unsafe.NullRef<T>();
		return ref record.Chunk.GetReferenceAt<T>(idx, record.Row);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ComponentsSpanIterator GetEnumerator()
	{
		Match();

		var qryInternal = new QueryInternal(_matchedArchetypes);
		return new(qryInternal, TermsAccess, _indices);
	}
}


[System.Runtime.CompilerServices.SkipLocalsInit]
public struct ComponentsSpanIterator
{
	private QueryInternal _queryIt;
	private QueryChunkIterator _chunkIt;
	private readonly ImmutableArray<IQueryTerm> _terms;
	private readonly int[] _indices;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ComponentsSpanIterator(QueryInternal queryIt, ImmutableArray<IQueryTerm> terms, int[] indices)
	{
		_queryIt = queryIt;
		_terms = terms;
		_indices = indices;
	}

	public readonly int Count => _chunkIt.Current.Count;

	internal readonly ref readonly ArchetypeChunk Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref _chunkIt.Current;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly int IndexAt(int index)
	{
		return _indices[index];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<T> Data<T>(int index) where T : struct
	{
		return _chunkIt.Current.GetSpan<T>(IndexAt(index));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ReadOnlySpan<EntityView> Entities()
	{
		return _chunkIt.Current.GetEntities();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool MoveNext()
	{
		while (true)
		{
			if (_chunkIt.MoveNext())
				return true;
			if (_queryIt.MoveNext())
			{
				var arch = _queryIt.Current;
				for (var i = 0; i < _indices.Length; ++i)
					_indices[i] = arch.GetComponentIndex(_terms[i].Id);
				_chunkIt = new QueryChunkIterator(_queryIt.Current.MemChunks);
				continue;
			}

			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ComponentsSpanIterator GetEnumerator() => this;
}

// public ref struct ComponentsIterator<T0>
// 	where T0 : struct
// {
// 	private ComponentsSpanIterator<T0> _iterator;
// 	private Ptr<T0> _current, _last;

// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	internal ComponentsIterator(ComponentsSpanIterator<T0> queryIterator)
// 	{
// 		_iterator = queryIterator;
// 	}

// 	[UnscopedRef]
// 	public ref Ptr<T0> Current
// 	{
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		get => ref _current;
// 	}

// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public unsafe bool MoveNext()
// 	{
// 		if (!Unsafe.IsAddressLessThan(ref _current.Ref, ref _last.Ref))
// 		{
// 			if (!_iterator.MoveNext())
// 				return false;

// 			_iterator.Deconstruct(out var entities, out var s0);

// 			_current.Pointer = (T0*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(s0));
// 			_last.Pointer = _current.Pointer + entities.Length - 1;
// 		}
// 		else
// 		{
// 			_current.Pointer += 1;
// 		}

// 		return true;
// 	}

// 	public ComponentsIterator<T0> GetEnumerator() => this;
// }

public struct QueryInternal
{
	private readonly List<Archetype> _archetypes;
	private int _index;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal QueryInternal(List<Archetype> archetypes)
	{
		_archetypes = archetypes;
		_index = -1;
	}

	public readonly Archetype Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _archetypes[_index];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool MoveNext()
	{
		while (true)
		{
			if (++_index >= _archetypes.Count)
				break;

			var archetype = _archetypes[_index];
			if (archetype != null && archetype.Count > 0)
				return true;
		}

		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryInternal GetEnumerator() => this;
}

public struct QueryChunkIterator
{
	private readonly ReadOnlyMemory<ArchetypeChunk> _chunks;
	private int _index;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal QueryChunkIterator(ReadOnlyMemory<ArchetypeChunk> chunks)
	{
		_chunks = chunks;
		_index = -1;
	}

	public readonly ref readonly ArchetypeChunk Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref _chunks.Span[_index];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool MoveNext()
	{
		while (true)
		{
			if (++_index >= _chunks.Length)
				break;

			if (_chunks.Span[_index].Count > 0)
				return true;
		}

		return false;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryChunkIterator GetEnumerator() => this;
}

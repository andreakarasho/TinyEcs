using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

public sealed class QueryBuilder
{
	private readonly World _world;
	private readonly Dictionary<EcsID, IQueryTerm> _components = new();
	private Query? _query;

	internal QueryBuilder(World world) => _world = world;

	public World World => _world;

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
		=> Term(new WithTerm(id));

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
		=> Term(new WithoutTerm(id));

	public QueryBuilder Optional<T>() where T : struct
	{
		ref readonly var cmp = ref _world.Component<T>();
		EcsAssert.Panic(cmp.Size > 0, "You can't access Tag as Component");
		return Optional(cmp.ID);
	}

	public QueryBuilder Optional(EcsID id)
		=> Term(new OptionalTerm(id));

	public QueryBuilder Term(IQueryTerm term)
	{
		_components[term.Id] = term;
		return this;
	}

	public Query Build()
	{
		_query = null;
		_query ??= new Query(_world, _components.Values.ToArray());
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
		_matchedArchetypes = new ();

		_terms = terms
			.ToImmutableSortedSet()
			.ToImmutableArray();

		TermsAccess = terms.Where(s => Lookup.GetComponent(s.Id).Size > 0)
			.ToImmutableArray();

		_indices = new int[TermsAccess.Length];
		_indices.AsSpan().Fill(-1);
	}

	internal World World { get; }
	internal ImmutableArray<IQueryTerm> TermsAccess { get; }



	private void Match()
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

	public QueryIterator Iter()
	{
		Match();

		return Iter(CollectionsMarshal.AsSpan(_matchedArchetypes), 0, -1);
	}

	public QueryIterator Iter(EcsID entity)
	{
		Match();

		ref var record = ref World.GetRecord(entity);

		var found = false;
		foreach (var arch in _matchedArchetypes)
		{
			if (arch.Id == record.Archetype.Id)
			{
				found = true;
				break;
			}
		}

		if (!found)
			return Iter([], 0, 0);

		var archetypes = new ReadOnlySpan<Archetype>(ref record.Archetype);
		return Iter(archetypes, record.Row, 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public QueryIterator Iter(ReadOnlySpan<Archetype> archetypes, int start, int count)
	{
		return new(archetypes, TermsAccess, _indices, start, count);
	}
}



[SkipLocalsInit]
public ref struct QueryIterator
{
	private ReadOnlySpan<Archetype>.Enumerator _archetypeIterator;
	private ReadOnlySpan<ArchetypeChunk>.Enumerator _chunkIterator;
	private readonly ImmutableArray<IQueryTerm> _terms;
	private readonly Span<int> _indices;
	private readonly int _start, _startSafe, _count;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal QueryIterator(ReadOnlySpan<Archetype> archetypes, ImmutableArray<IQueryTerm> terms, Span<int> indices, int start, int count)
	{
		_archetypeIterator = archetypes.GetEnumerator();
		_terms = terms;
		_indices = indices;
		_start = start;
		_startSafe = start & Archetype.CHUNK_THRESHOLD;
		_count = count;
	}

	public readonly int Count
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _count > 0 ? Math.Min(_count, _chunkIterator.Current.Count) : _chunkIterator.Current.Count;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref T DataRef<T>(int index) where T : struct
	{
		return ref Unsafe.Add(ref _chunkIterator.Current.GetReference<T>(_indices[index]), _startSafe);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref T DataRefWithSize<T>(int index, out int sizeInBytes) where T : struct
	{
		return ref Unsafe.AddByteOffset(ref _chunkIterator.Current.GetReferenceWithSize<T>(_indices[index], out sizeInBytes), sizeInBytes * _startSafe);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<T> Data<T>(int index) where T : struct
	{
		return _chunkIterator.Current.GetSpan<T>(_indices[index])
			.Slice(_startSafe, Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<T> Data<T>() where T : struct
	{
		return _chunkIterator.Current.GetSpan<T>(_archetypeIterator.Current.GetComponentIndex<T>())
			.Slice(_startSafe, Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ReadOnlySpan<EntityView> Entities()
	{
		return _chunkIterator.Current.GetEntities()
			.Slice(_startSafe, Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal readonly Span<EntityView> EntitiesDangerous()
	{
		return _chunkIterator.Current.Entities
			.AsSpan(_startSafe, Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal readonly ref readonly EntityView EntityAt(int index)
	{
		return ref _chunkIterator.Current.EntityAt(index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Next()
	{
		while (true)
		{
			while (_chunkIterator.MoveNext())
			{
				if (_chunkIterator.Current.Count > 0)
					return true;
			}

			while (true)
			{
				if (!_archetypeIterator.MoveNext())
					return false;

				if (_archetypeIterator.Current.Count <= 0)
					continue;

				break;
			}

			ref readonly var arch = ref _archetypeIterator.Current;
			for (var i = 0; i < _indices.Length; ++i)
				_indices[i] = arch.GetComponentIndex(_terms[i].Id);
			_chunkIterator = arch.Chunks[(_start >> Archetype.CHUNK_LOG2) ..].GetEnumerator();
		}
	}
}

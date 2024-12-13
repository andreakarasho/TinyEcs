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

		var it = Iter();
		if (it.Next())
			return ref it.Data<T>()[0];
		return ref Unsafe.NullRef<T>();
	}

	public EntityView Single()
	{
		var count = Count();
		EcsAssert.Panic(count == 1, "Multiple entities found for a single archetype");

		var it = Iter();
		if (it.Next())
			return it.Entities()[0];
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

	public QueryIterator Iter()
	{
		Match();

		return new(CollectionsMarshal.AsSpan(_matchedArchetypes), TermsAccess, _indices);
	}
}



[SkipLocalsInit]
public ref struct QueryIterator
{
	private ReadOnlySpan<Archetype>.Enumerator _archetypeIterator;
	private ReadOnlySpan<ArchetypeChunk>.Enumerator _chunkIterator;
	private readonly ImmutableArray<IQueryTerm> _terms;
	private readonly Span<int> _indices;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal QueryIterator(ReadOnlySpan<Archetype> archetypes, ImmutableArray<IQueryTerm> terms, Span<int> indices)
	{
		_archetypeIterator = archetypes.GetEnumerator();
		_terms = terms;
		_indices = indices;
	}

	public readonly int Count
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _chunkIterator.Current.Count;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref T DataRef<T>(int index) where T : struct
	{
		return ref _chunkIterator.Current.GetReference<T>(_indices[index]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref T DataRefWithSize<T>(int index, out int sizeInByes) where T : struct
	{
		return ref _chunkIterator.Current.GetReferenceWithSize<T>(_indices[index], out sizeInByes);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<T> Data<T>(int index) where T : struct
	{
		return _chunkIterator.Current.GetSpan<T>(_indices[index]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<T> Data<T>() where T : struct
	{
		return _chunkIterator.Current.GetSpan<T>(_archetypeIterator.Current.GetComponentIndex<T>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ReadOnlySpan<EntityView> Entities()
	{
		return _chunkIterator.Current.GetEntities();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal readonly Span<EntityView> EntitiesDangerous()
	{
		return _chunkIterator.Current.Entities.AsSpan(0, Count);
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
			_chunkIterator = arch.Chunks.GetEnumerator();
		}
	}
}

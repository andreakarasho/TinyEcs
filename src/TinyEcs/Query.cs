using System.Collections;
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
	private readonly IQueryTerm[] _terms;
	private readonly List<Archetype> _matchedArchetypes;
	private EcsID _lastArchetypeIdMatched;
	private readonly int[] _indices;

	internal Query(World world, IQueryTerm[] terms)
	{
		World = world;
		_matchedArchetypes = new();

		_terms = new IQueryTerm[terms.Length];
		terms.CopyTo(_terms, 0);
		Array.Sort(_terms);

		TermsAccess = terms
			.Where(s => Lookup.GetComponent(s.Id).Size > 0)
			.ToArray();

		_indices = new int[TermsAccess.Length];
		_indices.AsSpan().Fill(-1);
	}

	internal World World { get; }
	internal IQueryTerm[] TermsAccess { get; }



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

		if (World.Exists(entity))
		{
			ref var record = ref World.GetRecord(entity);
			foreach (var arch in _matchedArchetypes)
			{
				if (arch.Id != record.Archetype.Id) continue;
				var archetypes = new ReadOnlySpan<Archetype>(ref record.Archetype);
				return Iter(archetypes, record.Row, 1);
			}
		}

		return Iter([], 0, 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private QueryIterator Iter(ReadOnlySpan<Archetype> archetypes, int start, int count)
	{
		return new(archetypes, TermsAccess, _indices, start, count);
	}
}


[SkipLocalsInit]
public ref struct QueryIterator
{
	private ReadOnlySpan<Archetype>.Enumerator _archetypeIterator;
	private ReadOnlySpan<ArchetypeChunk>.Enumerator _chunkIterator;
	private readonly ReadOnlySpan<IQueryTerm> _terms;
	private readonly Span<int> _indices;
	private readonly int _start, _startSafe, _count;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal QueryIterator(ReadOnlySpan<Archetype> archetypes, ReadOnlySpan<IQueryTerm> terms, Span<int> indices, int start, int count)
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

	public readonly Archetype Archetype => _archetypeIterator.Current;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly int GetColumnIndexOf<T>() where T : struct
	{
		return _indices.IndexOf(_archetypeIterator.Current.GetComponentIndex<T>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly DataRow<T> GetColumn<T>(int index) where T : struct
	{
#if NET9_0_OR_GREATER
		Unsafe.SkipInit(out DataRow<T> data);
#else
		var data = new DataRow<T>();
#endif

		if (index < 0 || index >= _indices.Length)
		{
			data.Value.Ref = ref Unsafe.NullRef<T>();
			data.Size = 0;
			return data;
		}

		var i = _indices[index];
		if (i < 0)
		{
			data.Value.Ref = ref Unsafe.NullRef<T>();
			data.Size = 0;
			return data;
		}

		ref readonly var chunk = ref _chunkIterator.Current;
		ref var column = ref chunk.GetColumn(i);
		ref var reference = ref MemoryMarshal.GetArrayDataReference(Unsafe.As<T[]>(column.Data));

		data.Size = Unsafe.SizeOf<T>();
		data.Value.Ref = ref Unsafe.Add(ref reference, _startSafe);

		return data;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<uint> GetChangedTicks(int index)
	{
		if (index >= _indices.Length)
		{
			return Span<uint>.Empty;
		}

		var i = _indices[index];
		if (i < 0)
		{
			return Span<uint>.Empty;
		}

		ref readonly var chunk = ref _chunkIterator.Current;
		ref var column = ref chunk.GetColumn(i);
		ref var stateRef = ref MemoryMarshal.GetArrayDataReference(column.ChangedTicks);

		var span = MemoryMarshal.CreateSpan(ref stateRef, column.ChangedTicks.Length);
		if (!span.IsEmpty)
			span = span.Slice(_startSafe, Count);
		return span;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<uint> GetAddedTicks(int index)
	{
		if (index >= _indices.Length)
		{
			return Span<uint>.Empty;
		}

		var i = _indices[index];
		if (i < 0)
		{
			return Span<uint>.Empty;
		}

		ref readonly var chunk = ref _chunkIterator.Current;
		ref var column = ref chunk.GetColumn(i);
		ref var stateRef = ref MemoryMarshal.GetArrayDataReference(column.AddedTicks);

		var span = MemoryMarshal.CreateSpan(ref stateRef, column.AddedTicks.Length);
		if (!span.IsEmpty)
			span = span.Slice(_startSafe, Count);
		return span;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<T> Data<T>(int index) where T : struct
	{
		var span = _chunkIterator.Current.GetSpan<T>(_indices[index]);

		if (!span.IsEmpty)
			span = span.Slice(_startSafe, Count);

		return span;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ReadOnlySpan<EntityView> Entities()
	{
		var entities = _chunkIterator.Current.GetEntities();

		if (!entities.IsEmpty)
			entities = entities.Slice(_startSafe, Count);

		return entities;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ReadOnlyMemory<EntityView> EntitiesAsMemory()
	{
		var entities = _chunkIterator.Current.Entities.AsMemory(0, _chunkIterator.Current.Count);

		if (!entities.IsEmpty)
			entities = entities.Slice(_startSafe, Count);

		return entities;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Next()
	{
	REDO:
		while (_chunkIterator.MoveNext())
		{
			if (_chunkIterator.Current.Count > 0)
				return true;
		}

	REDO_1:
		if (!_archetypeIterator.MoveNext())
			return false;

		if (_archetypeIterator.Current.Count <= 0)
			goto REDO_1;

		ref readonly var arch = ref _archetypeIterator.Current;
		for (var i = 0; i < _indices.Length; ++i)
			_indices[i] = arch.GetComponentIndex(_terms[i].Id);
		_chunkIterator = arch.Chunks[(_start >> Archetype.CHUNK_LOG2)..].GetEnumerator();

		goto REDO;
	}
}

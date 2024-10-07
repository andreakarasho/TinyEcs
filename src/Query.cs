using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

public delegate void QueryFilterDelegateWithEntity(EntityView entity);

public sealed class QueryBuilder : IDisposable
{
	private readonly World _world;
	private readonly HashSet<IQueryTerm> _components = new();
	private Query? _query;

	internal QueryBuilder(World world) => _world = world;

	public QueryBuilder Data<T>() where T : struct
		=> Term(new QueryTerm(_world.Component<T>().ID, TermOp.DataAccess));

	public QueryBuilder With<T>() where T : struct
		=> With(_world.Component<T>().ID);

	public QueryBuilder With<TAction, TTarget>()
		where TAction : struct
		where TTarget : struct
		=> With(_world.Component<TAction>().ID, _world.Component<TTarget>().ID);

	public QueryBuilder With<TAction>(EcsID target)
		where TAction : struct
		=> With(_world.Component<TAction>().ID, target);

	public QueryBuilder With(EcsID action, EcsID target)
		=> With(IDOp.Pair(action, target));

	public QueryBuilder With(EcsID id)
		=> Term(new QueryTerm(id, TermOp.With));

	public QueryBuilder Without<T>() where T : struct
		=> Without(_world.Component<T>().ID);

	public QueryBuilder Without<TAction, TTarget>()
		where TAction : struct
		where TTarget : struct
		=> Without(_world.Component<TAction>().ID, _world.Component<TTarget>().ID);

	public QueryBuilder Without<TAction>(EcsID target)
		where TAction : struct
		=> Without(_world.Component<TAction>().ID, target);

	public QueryBuilder Without(EcsID action, EcsID target)
		=> Without(IDOp.Pair(action, target));

	public QueryBuilder Without(EcsID id)
		=> Term(new QueryTerm(id, TermOp.Without));

	public QueryBuilder Optional<T>() where T :struct
		=> Optional(_world.Component<T>().ID);

	public QueryBuilder Optional(EcsID id)
		=> Term(new QueryTerm(id, TermOp.Optional));

	public QueryBuilder Term(IQueryTerm term)
	{
		_components.Add(term);
		return this;
	}

	public Query Build()
	{
		_query?.Dispose();
		_query = null;
		var terms = _components.ToArray();
		_query ??= new Query(_world, terms);
		return _query;
	}

	public void Dispose() => _query?.Dispose();
}

public partial class Query : IDisposable
{
	private readonly ImmutableArray<IQueryTerm> _terms;
	private readonly List<Archetype> _matchedArchetypes;
	private ulong _lastArchetypeIdMatched = 0;
	private Query? _subQuery;

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

		ref var subQuery = ref _subQuery;
		foreach (var or in terms
			.OfType<ContainerQueryTerm>()
			.Where(s => s.Op == TermOp.Or))
		{
			var roll = IQueryTerm.GetHash(or.Terms.AsSpan());
			subQuery = World.GetQuery
			(
				roll.Hash,
				[.. or.Terms],
				static (world, terms) => new Query(world, terms)
			);

			subQuery = ref subQuery._subQuery;
		}
	}

	internal World World { get; set; }
	internal Lazy<CountdownEvent> ThreadCounter { get; } = new(() => new CountdownEvent(1));
	internal ImmutableArray<IQueryTerm> TermsAccess { get; }

	public void Dispose()
	{
		_subQuery?.Dispose();
		if (ThreadCounter.IsValueCreated)
			ThreadCounter.Value.Dispose();
	}

	internal void Match()
	{
		_subQuery?.Match();

		if (_lastArchetypeIdMatched == World.LastArchetypeId)
			return;

		_lastArchetypeIdMatched = World.LastArchetypeId;
		_matchedArchetypes.Clear();
		World.Root.GetSuperSets(_terms.AsSpan(), _matchedArchetypes);
	}

	public int Count()
	{
		Match();

		var count = _matchedArchetypes.Sum(static s => s.Count);
		if (count == 0 && _subQuery != null)
		{
			return _subQuery.Count();
		}

		return count;
	}

	public ref T Single<T>() where T : struct
	{
		var count = Count();
		EcsAssert.Panic(count == 1, "'Single' must match one and only one entity.");

		foreach (var arch in this)
		{
			var column = arch.GetComponentIndex<T>();
			EcsAssert.Panic(column >= 0, "component not found");
			ref var value = ref arch.GetChunk(0).GetReference<T>(column);
			return ref value;
		}

		return ref Unsafe.NullRef<T>();
	}

	public EntityView Single()
	{
		var count = Count();
		EcsAssert.Panic(count == 1, "Multiple entities found for a single archetype");

		foreach (var arch in this)
		{
			return arch.GetChunk(0).EntityAt(0);
		}

		return EntityView.Invalid;
	}

	public ref T Get<T>(EcsID entity) where T : struct
	{
		ref var record = ref World.GetRecord(entity);

		foreach (var arch in this)
		{
			if (arch.Id != record.Archetype.Id)
				continue;

			var column = arch.GetComponentIndex<T>();
			EcsAssert.Panic(column >= 0, "component not found");
			ref var value = ref record.Chunk.GetReference<T>(column);
			return ref value;
		}

		return ref Unsafe.NullRef<T>();
	}

	public QueryInternal GetEnumerator()
	{
		Match();

		if (_subQuery != null)
		{
			if (_matchedArchetypes.All(static s => s.Count == 0))
			{
				return _subQuery.GetEnumerator();
			}
		}

		return new (CollectionsMarshal.AsSpan(_matchedArchetypes));
	}

	public void Each(QueryFilterDelegateWithEntity fn)
	{
		World.BeginDeferred();

		foreach (var arch in this)
		{
			foreach (ref readonly var chunk in arch)
			{
				ref var entity = ref chunk.EntityAt(0);
				ref var last = ref Unsafe.Add(ref entity, chunk.Count);
				while (Unsafe.IsAddressLessThan(ref entity, ref last))
				{
					fn(entity);
					entity = ref Unsafe.Add(ref entity, 1);
				}
			}
		}

		World.EndDeferred();
	}

	public ComponentsSpanIterator Iter() => new(this.GetEnumerator());
}


[System.Runtime.CompilerServices.SkipLocalsInit]
public ref struct ComponentsSpanIterator
{
	private QueryInternal _queryIt;

	private QueryChunkIterator _chunkIt;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ComponentsSpanIterator(QueryInternal queryIt)
	{
		_queryIt = queryIt;
	}

	[System.Diagnostics.CodeAnalysis.UnscopedRef]
	public ReadOnlySpan<EntityView> Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			ref var chunk = ref _chunkIt.Current;
			return chunk.Entities.AsSpan(0, chunk.Count);
		}
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
				_chunkIt = new QueryChunkIterator(_queryIt.Current.Chunks);
				continue;
			}

			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ComponentsSpanIterator GetEnumerator() => this;
}

public ref struct ComponentsIterator<T0>
	where T0 : struct
{
	private ComponentsSpanIterator<T0> _iterator;
	private Ptr<T0> _current, _last;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ComponentsIterator(ComponentsSpanIterator<T0> queryIterator)
	{
		_iterator = queryIterator;
	}

	[UnscopedRef]
	public ref Ptr<T0> Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref _current;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe bool MoveNext()
	{
		if (!Unsafe.IsAddressLessThan(ref _current.Ref, ref _last.Ref))
		{
			if (!_iterator.MoveNext())
				return false;

			_iterator.Deconstruct(out var entities, out var s0);

			_current.Pointer = (T0*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(s0));
			_last.Pointer = _current.Pointer + entities.Length - 1;
		}
		else
		{
			_current.Pointer += 1;
		}

		return true;
	}

	public ComponentsIterator<T0> GetEnumerator() => this;
}

public ref struct QueryInternal
{
#if NET
	private ref Archetype _value;
	private readonly ref Archetype _first, _last;
#else
	private Ref<Archetype> _value;
	private readonly Ref<Archetype> _first, _last;
#endif


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal QueryInternal(Span<Archetype> archetypes)
	{
#if NET
		_first = ref MemoryMarshal.GetReference(archetypes);
		_last = ref Unsafe.Add(ref _first, archetypes.Length);
		_value = ref Unsafe.NullRef<Archetype>();
#else
		_first = new(ref MemoryMarshal.GetReference(archetypes));
		_last = new(ref Unsafe.Add(ref _first.Value, archetypes.Length));
		_value = new(ref Unsafe.NullRef<Archetype>());
#endif
	}

	public readonly ref Archetype Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref
		_value
#if !NET
		.Value
#endif
		;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool MoveNext()
	{
		while (true)
		{
#if NET
			_value = ref Unsafe.IsNullRef(ref _value) ? ref _first : ref Unsafe.Add(ref _value, 1);
			if (!Unsafe.IsAddressLessThan(ref _value, ref _last))
				break;

			if (_value.Count > 0)
				return true;
#else
			ref Archetype value = ref _value.Value;
			value = ref Unsafe.IsNullRef(ref value) ? ref _first.Value : ref Unsafe.Add(ref value, 1);
			if (!Unsafe.IsAddressLessThan(ref value, ref _last.Value))
				break;

			if (value.Count > 0)
				return true;
#endif
		}

		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryInternal GetEnumerator() => this;
}

public ref struct QueryChunkIterator
{
#if NET
	private readonly ref ArchetypeChunk _first, _last;
	private ref ArchetypeChunk _value;
#else
	private readonly Ref<ArchetypeChunk> _first, _last;
	private Ref<ArchetypeChunk> _value;
#endif

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal QueryChunkIterator(Span<ArchetypeChunk> chunks)
	{
#if NET
		_first = ref MemoryMarshal.GetReference(chunks);
		_last = ref Unsafe.Add(ref _first, chunks.Length);
		_value = ref Unsafe.NullRef<ArchetypeChunk>();
#else
		_first = new(ref MemoryMarshal.GetReference(chunks));
		_last = new(ref Unsafe.Add(ref _first.Value, chunks.Length));
		_value = new(ref Unsafe.NullRef<ArchetypeChunk>());
#endif
	}

	public readonly ref ArchetypeChunk Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref
		_value
#if !NET
		.Value
#endif
		;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool MoveNext()
	{
		while (true)
		{
#if NET
			_value = ref Unsafe.IsNullRef(ref _value) ? ref _first : ref Unsafe.Add(ref _value, 1);
			if (!Unsafe.IsAddressLessThan(ref _value, ref _last))
				break;

			if (_value.Count > 0)
				return true;
#else
			ref var value = ref _value.Value;
			value = ref Unsafe.IsNullRef(ref value) ? ref _first.Value : ref Unsafe.Add(ref value, 1);
			if (!Unsafe.IsAddressLessThan(ref value, ref _last.Value))
				break;

			if (value.Count > 0)
				return true;
#endif
		}

		return false;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryChunkIterator GetEnumerator() => this;
}

internal ref struct QueryArchetypeChunkIterator
{
#if NET
	private readonly ref EntityView _first, _last;
	private ref EntityView _value;
#else
	private readonly Ref<EntityView> _first, _last;
	private Ref<EntityView> _value;
#endif

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal QueryArchetypeChunkIterator(ref ArchetypeChunk chunk)
	{
#if NET
		_first = ref chunk.EntityAt(0);
		_last = ref Unsafe.Add(ref _first, chunk.Count);
		_value = ref Unsafe.NullRef<EntityView>();
#else
		_first = new(ref chunk.EntityAt(0));
		_last = new(ref Unsafe.Add(ref _first.Value, chunk.Count));
		_value = new(ref Unsafe.NullRef<EntityView>());
#endif
	}

	public readonly ref EntityView Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref
		_value
#if !NET
		.Value
#endif
		;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool MoveNext()
	{
#if NET
		_value = ref Unsafe.IsNullRef(ref _value) ? ref _first : ref Unsafe.Add(ref _value, 1);
		if (Unsafe.IsAddressLessThan(ref _value, ref _last))
			return true;
#else
		ref var value = ref _value.Value;
		value = ref Unsafe.IsNullRef(ref value) ? ref _first.Value : ref Unsafe.Add(ref value, 1);
		if (Unsafe.IsAddressLessThan(ref value, ref _last.Value))
			return true;
#endif
		return false;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryArchetypeChunkIterator GetEnumerator() => this;
}

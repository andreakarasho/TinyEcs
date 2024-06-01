using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

public ref struct QueryInternal
{
#if NET
	private ref Archetype _value;
	private readonly ref Archetype _first, _last;
#else
	private Ref<Archetype> _value;
	private readonly Ref<Archetype> _first, _last;
#endif


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

	public readonly ref Archetype Current => ref
		_value
#if !NET
		.Value
#endif
		;


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

	public readonly QueryInternal GetEnumerator() => this;
}

public delegate void QueryFilterDelegateWithEntity(EntityView entity);



public sealed class QueryBuilder
{
	private readonly World _world;
	private readonly SortedSet<QueryTerm> _components = new();

	internal QueryBuilder(World world) => _world = world;

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
	{
		_components.Add(new(id, TermOp.With));
		return this;
	}

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
	{
		_components.Add(new (id, TermOp.Without));
		return this;
	}

	public QueryBuilder Optional<T>() where T :struct
		=> Optional(_world.Component<T>().ID);

	public QueryBuilder Optional(EcsID id)
	{
		_components.Add(new (id, TermOp.Optional));
		return this;
	}

	public Query Build()
	{
		var terms = _components.ToImmutableArray();
		return _world.GetQuery(
			Hashing.Calculate(terms.AsSpan()),
			terms,
			static (world, terms) => new Query(world, terms)
		);
	}
}


public sealed partial class Query<TQueryData> : Query
	where TQueryData : struct
{
	internal Query(World world) : base(world, Lookup.Query<TQueryData>.Terms)
	{
	}
}

public sealed partial class Query<TQueryData, TQueryFilter> : Query
	where TQueryData : struct where TQueryFilter : struct
{
	internal Query(World world) : base(world, Lookup.Query<TQueryData, TQueryFilter>.Terms)
	{
	}
}

public partial class Query : IDisposable
{
	private readonly ImmutableArray<QueryTerm> _terms;
	private readonly List<Archetype> _matchedArchetypes;
	private ulong _lastArchetypeIdMatched = 0;
	private Query? _subQuery;

	internal Query(World world, ImmutableArray<QueryTerm> terms)
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
			subQuery = World.GetQuery
			(
				Hashing.Calculate(or.Terms.AsSpan()),
				[.. or.Terms],
				static (world, terms) => new Query(world, terms)
			);

			subQuery = ref subQuery._subQuery;
		}
	}

	public World World { get; internal set; }

	internal CountdownEvent ThreadCounter { get; } = new CountdownEvent(1);
	internal ImmutableArray<QueryTerm> TermsAccess { get; }

	public void Dispose()
	{
		_subQuery?.Dispose();
		ThreadCounter.Dispose();
	}

	internal void Match()
	{
		_subQuery?.Match();

		var allArchetypes = World.Archetypes;

		if (allArchetypes.IsEmpty || _lastArchetypeIdMatched == allArchetypes[^1].Id)
			return;

		var ids = _terms
			.Where(s => s.Op == TermOp.With || s.Op == TermOp.Exactly)
			.Select(s => s.Id);

		var first = World.FindArchetype(Hashing.Calculate(ids));
		if (first == null)
			return;

		_lastArchetypeIdMatched = allArchetypes[^1].Id;
		_matchedArchetypes.Clear();
		World.MatchArchetypes(first, _terms.AsSpan(), _matchedArchetypes);
	}

	public RefEnumerator<T0, T1> Each3<T0, T1>() where T0 : struct where T1 : struct
	{
		return new RefEnumerator<T0, T1>(_matchedArchetypes);
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
		EcsAssert.Panic(count == 1, "Multiple entities found for a single archetype");

		foreach (var arch in this)
		{
			var column = arch.GetComponentIndex<T>();
			EcsAssert.Panic(column > 0, "component not found");
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
}

public unsafe ref struct RefEnumerator<T0, T1> where T0 : struct where T1 : struct
{
    private readonly List<Archetype> _matchedArchetypes;
	private Span<ArchetypeChunk> _chunks;
    private int _archIndex;
    private int _chunkIndex;
    private int _itemIndex;
    private int _spanLength;
	private Row<T0, T1> _ref;
    private int _col0, _col1;

    public RefEnumerator(List<Archetype> matchedArchetypes)
    {
        _matchedArchetypes = matchedArchetypes;
        _archIndex = -1;
        _chunkIndex = -1;
        _itemIndex = 0;
        _spanLength = 0;
    }

	[UnscopedRef]
    public ref Row<T0, T1> Current => ref _ref;

    public bool MoveNext()
    {
        while (true)
        {
            if (_chunkIndex >= 0 && _archIndex >= 0)
            {
                if (++_itemIndex < _spanLength)
                {
					_ref.Advance();

                    return true;
                }
            }

            _itemIndex = 0;

            if (_archIndex >= 0 && ++_chunkIndex < _chunks.Length)
            {
				ref var chunk = ref _chunks[_chunkIndex];
				ref var span0 = ref chunk.GetReference<T0>(_col0);
				ref var span1 = ref chunk.GetReference<T1>(_col1);
				_ref = new Row<T0, T1>(ref Unsafe.Subtract(ref span0, 1), ref Unsafe.Subtract(ref span1, 1));
				_spanLength = chunk.Count;
				_itemIndex = -1;

				continue;
            }

            _chunkIndex = -1;

            if (++_archIndex < _matchedArchetypes.Count)
			{
				var arch = _matchedArchetypes[_archIndex];
				if (arch.Count > 0)
				{
					_col0 = arch.GetComponentIndex<T0>();
					_col1 = arch.GetComponentIndex<T1>();
					_chunks = arch.Chunks;
					continue;
				}
			}

            return false;
        }
    }

    public readonly RefEnumerator<T0, T1> GetEnumerator() => this;
}


[SkipLocalsInit]
public unsafe ref struct Row<T0, T1> where T0 : struct where T1 : struct
{
#if NET
	public ref T0 Val0;
	public ref T1 Val1;
#else
	private IntPtr _val0;
	private IntPtr _val1;
	public ref T0 Val0 => ref Unsafe.AsRef<T0>(_val0.ToPointer());
	public ref T1 Val1 => ref Unsafe.AsRef<T1>(_val1.ToPointer());
#endif

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Row(ref T0 value0, ref T1 value1)
	{
#if NET
		Val0 = ref value0;
		Val1 = ref value1;
#else
		_val0 = (IntPtr)Unsafe.AsPointer(ref value0);
		_val1 = (IntPtr)Unsafe.AsPointer(ref value1);
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void Advance()
	{
#if NET
		Val0 = ref Unsafe.Add(ref Val0, 1);
		Val1 = ref Unsafe.Add(ref Val1, 1);
#else
		_val0 = (IntPtr)(((T0*)_val0.ToPointer()) + 1);
		_val1 = (IntPtr)(((T1*)_val1.ToPointer()) + 1);
#endif
	}
}

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

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

		if (allArchetypes.Count == 0 || _lastArchetypeIdMatched == allArchetypes[^1].Id)
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

internal ref struct QueryChunkIterator
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




public partial class Query
{
	public RefEnumeratorTest<T0, T1> IterTest<T0, T1>() where T0 : struct where T1 : struct
	{
		return new (GetEnumerator());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public RefEnumeratorTest2<T0, T1> IterTest2<T0, T1>() where T0 : struct where T1 : struct
	{
		return new (GetEnumerator());
	}

	// public void EachJob2<T0>(QueryFilterDelegate<T0> fn)
	// 	where T0 : struct
	// {
	// 	var query = this;
	// 	EcsAssert.Panic(query.TermsAccess.Length == 1, "mismatched sign");
	// 	World.BeginDeferred();
	// 	var cde = query.ThreadCounter;
	// 	cde.Reset();

	// 	foreach (var (_, t0AA) in query.Iter<T0>())
	// 	{
	// 		cde.AddCount(1);
	// 		var inc0 = t0AA.IsEmpty ? 0 : 1;

	// 		System.Threading.ThreadPool.QueueUserWorkItem(state =>
	// 		{
	// 			ref var index = ref Unsafe.Unbox<int>(state!);

	// 			cde.Signal();
	// 		}, t0AA);
	// 	}

	// 	foreach (var arch in query)
	// 	{
	// 		var column0 = arch.GetComponentIndex<T0>();
	// 		var inc0 = column0 < 0 ? 0 : 1;
	// 		var chunks = arch.MemChunks;
	// 		cde.AddCount(chunks.Length);
	// 		for (var i = 0; i < chunks.Length; ++i)
	// 		{
	// 			System.Threading.ThreadPool.QueueUserWorkItem(state =>
	// 			{
	// 				ref var index = ref Unsafe.Unbox<int>(state!);
	// 				ref readonly var chunk = ref chunks.Span[index];
	// 				var done = 0;
	// 				ref var t0A = ref chunk.GetReference<T0>(column0);
	// 				while (done <= chunk.Count - 4)
	// 				{
	// 					fn(ref t0A);
	// 					t0A = ref Unsafe.Add(ref t0A, inc0);
	// 					fn(ref t0A);
	// 					t0A = ref Unsafe.Add(ref t0A, inc0);
	// 					fn(ref t0A);
	// 					t0A = ref Unsafe.Add(ref t0A, inc0);
	// 					fn(ref t0A);
	// 					t0A = ref Unsafe.Add(ref t0A, inc0);
	// 					done += 4;
	// 				}

	// 				while (done < chunk.Count)
	// 				{
	// 					fn(ref t0A);
	// 					t0A = ref Unsafe.Add(ref t0A, inc0);
	// 					done += 1;
	// 				}

	// 				cde.Signal();
	// 			}, i);
	// 		}
	// 	}

	// 	cde.Signal();
	// 	cde.Wait();
	// 	World.EndDeferred();
	// }

	public void Each2<T0, T1, T2>(QueryFilterDelegate<T0, T1, T2> fn)
		where T0 : struct where T1 : struct where T2 : struct
	{
		var query = this;
		EcsAssert.Panic(query.TermsAccess.Length == 3, "mismatched sign");
		EcsAssert.Panic(query.TermsAccess[0].Id == query.World.Entity<T0>().ID, $"'{typeof(T0)}' doesn't match the QueryData sign");
		EcsAssert.Panic(query.TermsAccess[1].Id == query.World.Entity<T1>().ID, $"'{typeof(T1)}' doesn't match the QueryData sign");
		EcsAssert.Panic(query.TermsAccess[2].Id == query.World.Entity<T2>().ID, $"'{typeof(T2)}' doesn't match the QueryData sign");

		World.BeginDeferred();

		foreach ((var entities, var t0AA, var t1AA, var t2AA) in query.Iter<T0, T1, T2>())
		{
			var count = entities.Length;
			var inc0 = t0AA.IsEmpty ? 0 : 1;
			var inc1 = t1AA.IsEmpty ? 0 : 1;
			var inc2 = t2AA.IsEmpty ? 0 : 1;

			ref var t0A = ref MemoryMarshal.GetReference(t0AA);
			ref var t1A = ref MemoryMarshal.GetReference(t1AA);
			ref var t2A = ref MemoryMarshal.GetReference(t2AA);

			while (count - 4 > 0)
			{
				fn(ref t0A, ref t1A, ref t2A);
				t0A = ref Unsafe.Add(ref t0A, inc0);
				t1A = ref Unsafe.Add(ref t1A, inc1);
				t2A = ref Unsafe.Add(ref t2A, inc2);
				fn(ref t0A, ref t1A, ref t2A);
				t0A = ref Unsafe.Add(ref t0A, inc0);
				t1A = ref Unsafe.Add(ref t1A, inc1);
				t2A = ref Unsafe.Add(ref t2A, inc2);
				fn(ref t0A, ref t1A, ref t2A);
				t0A = ref Unsafe.Add(ref t0A, inc0);
				t1A = ref Unsafe.Add(ref t1A, inc1);
				t2A = ref Unsafe.Add(ref t2A, inc2);
				fn(ref t0A, ref t1A, ref t2A);
				t0A = ref Unsafe.Add(ref t0A, inc0);
				t1A = ref Unsafe.Add(ref t1A, inc1);
				t2A = ref Unsafe.Add(ref t2A, inc2);

				count -= 4;
			}

			while (count > 0)
			{
				fn(ref t0A, ref t1A, ref t2A);
				t0A = ref Unsafe.Add(ref t0A, inc0);
				t1A = ref Unsafe.Add(ref t1A, inc1);
				t2A = ref Unsafe.Add(ref t2A, inc2);

				count -= 1;
			}
		}

		World.EndDeferred();
	}
}

[SkipLocalsInit]
public ref struct RefEnumeratorTest<T0, T1> where T0 : struct where T1 : struct
{
	private QueryInternal _queryIt;
	private QueryChunkIterator _chunkIt;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RefEnumeratorTest(QueryInternal queryit)
    {
		_queryIt = queryit;
    }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Deconstruct(out Span<T0> val0, out Span<T1> val1)
    {
		ref var arch = ref _queryIt.Current;
		ref var chunk = ref _chunkIt.Current;

		val0 = chunk.GetSpan<T0>(arch.GetComponentIndex<T0>());
		val1 = chunk.GetSpan<T1>(arch.GetComponentIndex<T1>());
    }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Deconstruct(out Span<EntityView> entities, out Span<T0> val0, out Span<T1> val1)
    {
		ref var arch = ref _queryIt.Current;
		ref var chunk = ref _chunkIt.Current;

		entities = chunk.Entities.AsSpan(0, chunk.Count);
		val0 = chunk.GetSpan<T0>(arch.GetComponentIndex<T0>());
		val1 = chunk.GetSpan<T1>(arch.GetComponentIndex<T1>());
    }

	[UnscopedRef]
    public ref RefEnumeratorTest<T0, T1> Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref this;
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
    public readonly RefEnumeratorTest<T0, T1> GetEnumerator() => this;
}

[SkipLocalsInit]
public unsafe ref struct RefEnumeratorTest2<T0, T1> where T0 : struct where T1 : struct
{
	private RefEnumerator<T0, T1> _refEnum;
	private int _count;
	// private T0* _span0;
	// private T1* _span1;

	private Span<T0> _span0;
	private Span<T1> _span1;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RefEnumeratorTest2(QueryInternal queryIt)
    {
		_refEnum = new (queryIt);
    }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Deconstruct(out T0* val0, out T1* val1)
    {
		fixed (T0* v0 = &_span0[_count])
		fixed (T1* t0 = &_span1[_count])
		{
			val0 = v0;
			val1 = t0;
		}
    }

	[UnscopedRef]
    public ref RefEnumeratorTest2<T0, T1> Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref this;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        while (true)
        {
			if (_count-- > 0)
			{
				// _span0 -= 1;
				// _span1 -= 1;

				return true;
			}

			if (_refEnum.MoveNext())
			{
				(var entities, var ref0, var ref1) = _refEnum.Current;
				_count = entities.Length;

				_span0 = ref0;
				_span1 = ref1;

				// ref var a = ref Unsafe.Add(ref MemoryMarshal.GetReference(ref0), _count);
				// ref var b = ref Unsafe.Add(ref MemoryMarshal.GetReference(ref1), _count);

				// _span0 = (T0*) Unsafe.AsPointer(ref a);
				// _span1 = (T1*) Unsafe.AsPointer(ref b);

				continue;
			}

            return false;
        }
    }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly RefEnumeratorTest2<T0, T1> GetEnumerator() => this;
}

// [SkipLocalsInit]
// public unsafe ref struct Row<T> where T : struct
// {
// #if NET
// 	public ref T Val0;
// #else
// 	private IntPtr _val0;
// 	public ref T Val0 => ref Unsafe.AsRef<T>(_val0.ToPointer());
// #endif


// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	internal void Set(ref T value0)
// 	{
// #if NET
// 		Val0 = ref value0;
// #else
// 		_val0 = (IntPtr)Unsafe.AsPointer(ref value0);
// #endif
// 	}

// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	internal void Advance()
// 	{
// #if NET
// 		Val0 = ref Unsafe.Add(ref Val0, 1);
// #else
// 		_val0 = (IntPtr)(((T*)_val0.ToPointer()) + 1);
// #endif
// 	}
// }


[SkipLocalsInit]
public unsafe struct Row<T> where T : struct
{
	private void* _val0;
	public ref T Val0 => ref *(T*)_val0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void Set(ref T value0)
	{
		_val0 = Unsafe.AsPointer(ref value0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void Advance()
	{
		_val0 = ((T*)_val0) + 1;
	}
}

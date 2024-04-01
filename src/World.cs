using System.Collections.Immutable;
using Microsoft.Collections.Extensions;

namespace TinyEcs;

public sealed partial class World : IDisposable
{
    private readonly Archetype _archRoot;
    private readonly EntitySparseSet<EcsRecord> _entities = new();
    private readonly DictionarySlim<ulong, Archetype> _typeIndex = new();
    private Archetype[] _archetypes = new Archetype[16];
    private int _archetypeCount;
    private readonly ComponentComparer _comparer;
	private readonly EcsID _maxCmpId;
	private readonly Dictionary<ulong, Query> _cachedQueries = new ();

    public World(ulong maxComponentId = 256)
    {
        _comparer = new ComponentComparer(this);
        _archRoot = new Archetype(
            this,
            ReadOnlySpan<ComponentInfo>.Empty,
            _comparer
        );

		_maxCmpId = maxComponentId;
        _entities.MaxID = maxComponentId;
		OnPluginInitialization?.Invoke(this);
    }

	public event Action<EntityView>? OnEntityCreated, OnEntityDeleted;
	public event Action<EntityView, ComponentInfo>? OnComponentSet, OnComponentUnset;
	public static event Action<World>? OnPluginInitialization;

    public int EntityCount => _entities.Length;

    public ReadOnlySpan<Archetype> Archetypes => _archetypes.AsSpan(0, _archetypeCount);


    public void Dispose()
    {
        _entities.Clear();
        _archRoot.Clear();
        _typeIndex.Clear();

		foreach (var query in _cachedQueries.Values)
			query.Dispose();

		_cachedQueries.Clear();

        Array.Clear(_archetypes, 0, _archetypeCount);
        _archetypeCount = 0;
    }

    public void Optimize()
    {
        InternalOptimize(_archRoot);

        static void InternalOptimize(Archetype root)
        {
            root.Optimize();

            foreach (ref var edge in CollectionsMarshal.AsSpan(root._edgesRight))
            {
                InternalOptimize(edge.Archetype);
            }
        }
    }

    public ref readonly ComponentInfo Component<T>() where T : struct
	{
        ref readonly var lookup = ref Lookup.Component<T>.Value;

		EcsAssert.Panic(lookup.ID < _maxCmpId);

		if (!Exists(lookup.ID))
		{
			var e = Entity(lookup.ID)
				.Set(lookup);
		}

        // if (lookup.ID == 0 || !Exists(lookup.ID))
        // {
        //     EcsID id = lookup.ID;
        //     if (id == 0 && _lastCompID < ECS_MAX_COMPONENT_FAST_ID)
        //     {
        //         do
        //         {
        //             id = _lastCompID++;
        //         } while (Exists(id) && id <= ECS_MAX_COMPONENT_FAST_ID);
        //     }

        //     if (id >= ECS_MAX_COMPONENT_FAST_ID)
        //     {
        //         id = 0;
        //     }

        //     id = Entity(id);
        //     var size = GetSize<T>();

        //     lookup = new EcsComponent(id, size);
        //     _ = CreateComponent(id, size);
        // }

        // if (Exists(lookup.ID))
        // {
        //     var name = Lookup.Entity<T>.Name;
        //     ref var cmp2 = ref MemoryMarshal.GetReference(
        //         MemoryMarshal.Cast<byte, EcsComponent>(
        //             GetRaw(lookup.ID, EcsComponent, GetSize<EcsComponent>())
        //         )
        //     );

        //     EcsAssert.Panic(cmp2.Size == lookup.Size, $"invalid size for {Lookup.Entity<T>.Name}");
        // }

        return ref lookup;
    }

    // public EntityView Entity(string name)
    // {
	// 	_entityNames.TryGetValue(name, out var id);

    //     var entity = Entity(id);
	// 	if (id == 0)
	// 	{
	// 		_entityNames.Add(name, entity.ID);
	// 		GetRecord(entity.ID).Name = name;
	// 	}

	// 	return entity;
    // }

	public EntityView Entity<T>() where T : struct
	{
		return Entity(Component<T>().ID);
	}

    public EntityView Entity(EcsID id = default)
    {
        return id == 0 || !Exists(id) ? NewEmpty(id) : new(this, id);
    }

    internal EntityView NewEmpty(ulong id = 0)
    {
		// if (IsDeferred)
		// {
		// 	if (id == 0)
		// 	{
		// 		id = _worldState.NewEntities++ + _entities.MaxID;
		// 	}

		// 	// _operations.Enqueue(new DeferredOp() {
		// 	// 	Op = DeferredOpTypes.CreateEntity,
		// 	// 	Entity = id,
		// 	// });

		// 	_commands.Entity(id);

		// 	return new EntityView(this, id);
		// }

        ref var record = ref (
            id > 0 ? ref _entities.Add(id, default!) : ref _entities.CreateNew(out id)
        );
        record.Archetype = _archRoot;
        record.Row = _archRoot.Add(id);

        var e = new EntityView(this, id);

		OnEntityCreated?.Invoke(e);

		return e;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref EcsRecord GetRecord(EcsID id)
    {
        ref var record = ref _entities.Get(id);
        EcsAssert.Assert(!Unsafe.IsNullRef(ref record), $"entity {id} is dead or doesn't exist!");
        return ref record;
    }

    public void Delete(EcsID entity)
    {
		if (IsDeferred)
		{
			DeleteDeferred(entity);

			return;
		}

		OnEntityDeleted?.Invoke(new (this, entity));

        ref var record = ref GetRecord(entity);

        var removedId = record.Archetype.Remove(ref record);
        EcsAssert.Assert(removedId == entity);

        _entities.Remove(removedId);
    }

    public bool Exists(EcsID entity)
    {
        if (IDOp.IsPair(entity))
        {
            var first = IDOp.GetPairFirst(entity);
            var second = IDOp.GetPairSecond(entity);
            return _entities.Contains(first) && _entities.Contains(second);
        }

        return _entities.Contains(entity);
    }

    internal void DetachComponent(EcsID entity, ref readonly ComponentInfo cmp)
    {
		OnComponentUnset?.Invoke(Entity(entity), cmp);
        ref var record = ref GetRecord(entity);
        InternalAttachDetach(ref record, in cmp, false);
    }

    private bool InternalAttachDetach(
        ref EcsRecord record,
        ref readonly ComponentInfo cmp,
        bool add
    )
    {
        EcsAssert.Assert(!Unsafe.IsNullRef(ref record));

        var arch = AttachToArchetype(record.Archetype, in cmp, add);
        if (arch == null)
            return false;

        record.Row = record.Archetype.MoveEntity(arch, record.Row);
        record.Archetype = arch!;

        return true;
    }

    [SkipLocalsInit]
    private Archetype? AttachToArchetype(Archetype root, ref readonly ComponentInfo cmp, bool add)
    {
        if (!add && root.GetComponentIndex(in cmp) < 0)
            return null;

        var initType = root.Components;
        var cmpCount = Math.Max(0, initType.Length + (add ? 1 : -1));

        const int STACKALLOC_SIZE = 16;
		ComponentInfo[]? buffer = null;
		scoped var span = cmpCount <= STACKALLOC_SIZE ? stackalloc ComponentInfo[STACKALLOC_SIZE] : (buffer = ArrayPool<ComponentInfo>.Shared.Rent(cmpCount));

		span = span[..cmpCount];

        if (!add)
        {
            for (int i = 0, j = 0; i < initType.Length; ++i)
            {
                if (initType[i].ID != cmp.ID)
                {
                    span[j++] = initType[i];
                }
            }
        }
        else if (!span.IsEmpty)
        {
            initType.CopyTo(span);
            span[^1] = cmp;
            span.Sort(_comparer);
        }

		ref var arch = ref GetArchetype(span, true);
		if (arch == null)
		{
			arch = _archRoot.InsertVertex(root, span, in cmp);

			if (_archetypeCount >= _archetypes.Length)
				Array.Resize(ref _archetypes, _archetypes.Length * 2);
			_archetypes[_archetypeCount++] = arch;
		}

		if (buffer != null)
            ArrayPool<ComponentInfo>.Shared.Return(buffer);

        return arch;
    }

    private ref Archetype? GetArchetype(Span<ComponentInfo> components, bool create)
	{
		var hash = Hashing.Calculate(components);
		ref var arch = ref Unsafe.NullRef<Archetype>();
		if (create)
		{
			arch = ref _typeIndex.GetOrAddValueRef(hash, out var exists)!;
			if (!exists)
			{

			}
		}
		else if (_typeIndex.TryGetValue(hash, out arch))
		{

		}

		// ref var arch = ref create ? ref CollectionsMarshal.GetValueRefOrAddDefault(
		// 	_typeIndex,
		// 	hash,
		// 	out exists
		// ) : ref CollectionsMarshal.GetValueRefOrNullRef(_typeIndex, hash);

		return ref arch;
	}

	internal Array? Set(EntityView entity, ref EcsRecord record, ref readonly ComponentInfo cmp)
    {
        var emit = false;
        var column = record.Archetype.GetComponentIndex(in cmp);
        if (column < 0)
        {
            emit = InternalAttachDetach(ref record, in cmp, true);
            column = record.Archetype.GetComponentIndex(in cmp);
        }

        Array? array = null;

        if (cmp.Size > 0)
	        array = record.GetChunk().RawComponentData(column);

        if (emit)
        {
			OnComponentSet?.Invoke(entity, cmp);
        }

        return array;
    }

    internal bool Has(EcsID entity, ref readonly ComponentInfo cmp)
    {
        ref var record = ref GetRecord(entity);
        return record.Archetype.GetComponentIndex(in cmp) >= 0;
    }

    public ReadOnlySpan<ComponentInfo> GetType(EcsID id)
    {
        ref var record = ref GetRecord(id);
        return record.Archetype.Components.AsSpan();
    }

    public void PrintGraph()
    {
        _archRoot.Print();
    }

	public Query<TQuery> Query<TQuery>() where TQuery : struct
	{
		return (Query<TQuery>) GetQuery(
			Lookup.Query<TQuery>.Hash,
		 	Lookup.Query<TQuery>.Terms,
		 	static (world, _) => new Query<TQuery>(world)
		);
	}

	public Query<TQuery, TFilter> Query<TQuery, TFilter>() where TQuery : struct where TFilter : struct
	{
		return (Query<TQuery, TFilter>) GetQuery(
			Lookup.Query<TQuery, TFilter>.Hash,
			Lookup.Query<TQuery, TFilter>.Terms,
		 	static (world, _) => new Query<TQuery, TFilter>(world)
		);
	}

	public void Each(QueryFilterDelegateWithEntity fn)
	{
		foreach (var arch in GetQuery(0, ImmutableArray<Term>.Empty, static (world, terms) => new Query(world, terms)))
		{
			foreach (ref readonly var chunk in arch)
			{
				ref var entity = ref chunk.Entities[0];
				ref var last = ref Unsafe.Add(ref entity, chunk.Count);
				while (Unsafe.IsAddressLessThan(ref entity, ref last))
				{
					fn(entity);
					entity = ref Unsafe.Add(ref entity, 1);
				}
			}
		}
	}

	internal Query GetQuery(ulong hash, ImmutableArray<Term> terms, Func<World, ImmutableArray<Term>, Query> factory)
	{
		if (!_cachedQueries.TryGetValue(hash, out var query))
		{
			query = factory(this, terms);
			_cachedQueries.Add(hash, query);
		}

		query.Match();

		return query;
	}

    public IQueryConstruct QueryBuilder() => new QueryBuilder(this);

	internal Archetype? FindArchetype(ulong hash)
	{
		if (!_typeIndex.TryGetValue(hash, out var arch))
		{
			arch = _archRoot;
		}

		return arch;
	}

	internal void MatchArchetypes(Archetype root, ReadOnlySpan<Term> terms, List<Archetype> matched)
	{
		var result = root.FindMatch(terms);
		if (result < 0)
		{
			return;
		}

		if (result == 0)
		{
			matched.Add(root);
		}

		var span = CollectionsMarshal.AsSpan(root._edgesRight);
		if (span.IsEmpty)
			return;

		ref var start = ref MemoryMarshal.GetReference(span);
		ref var end = ref Unsafe.Add(ref start, span.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			MatchArchetypes(start.Archetype, terms, matched);
			start = ref Unsafe.Add(ref start, 1);
		}
	}
}

internal static class Hashing
{
	public static ulong Calculate(ReadOnlySpan<ComponentInfo> components)
	{
		var hc = (ulong)components.Length;
		foreach (ref readonly var val in components)
			hc = unchecked(hc * 314159 + val.ID);
		return hc;
	}

	public static ulong Calculate(ReadOnlySpan<Term> terms)
	{
		var hc = (ulong)terms.Length;
		foreach (ref readonly var val in terms)
			if (val.Op == TermOp.With)
				hc = unchecked(hc * 314159 + val.ID);
		return hc;
	}
}

internal static class Lookup
{
	private static ulong _index = 0;

	private static readonly Dictionary<ulong, Func<int, Array>> _arrayCreator = new ();
	private static readonly Dictionary<Type, ulong> _typesConvertion = new();

	public static Array? GetArray(ulong hashcode, int count)
	{
		var ok = _arrayCreator.TryGetValue(hashcode, out var fn);
		EcsAssert.Assert(ok, $"component not found with hashcode {hashcode}");
		return fn?.Invoke(count) ?? null;
	}

	private static ulong GetID(Type type)
	{
		var ok = _typesConvertion.TryGetValue(type, out var id);
		EcsAssert.Assert(ok, $"component not found with type {type}");
		return id;
	}

	[SkipLocalsInit]
    internal static class Component<T> where T : struct
	{
        public static readonly int Size = GetSize();
        public static readonly string Name = typeof(T).ToString();
        public static readonly ulong HashCode = (ulong)System.Threading.Interlocked.Increment(ref Unsafe.As<ulong, int>(ref _index)) - 0;

		public static readonly ComponentInfo Value = new ComponentInfo(HashCode, Size);

		static Component()
		{
			_arrayCreator.Add(Value.ID, count => Size > 0 ? new T[count] : Array.Empty<T>());
			_typesConvertion.Add(typeof(T), Value.ID);
			_typesConvertion.Add(typeof(With<T>), Value.ID);
			_typesConvertion.Add(typeof(Not<T>), IDOp.Pair(0xFF_FF_FF_FF, Value.ID));
			_typesConvertion.Add(typeof(Without<T>), IDOp.Pair(0xFF_FF_FF_FF, Value.ID));
		}

		private static int GetSize()
		{
			var size = RuntimeHelpers.IsReferenceOrContainsReferences<T>() ? IntPtr.Size : Unsafe.SizeOf<T>();

			if (size != 1)
				return size;

			// credit: BeanCheeseBurrito from Flecs.NET
			Unsafe.SkipInit<T>(out var t1);
			Unsafe.SkipInit<T>(out var t2);
			Unsafe.As<T, byte>(ref t1) = 0x7F;
			Unsafe.As<T, byte>(ref t2) = 0xFF;

			return ValueType.Equals(t1, t2) ? 0 : size;
		}
    }

	static void ParseTuple(ITuple tuple, SortedSet<Term> terms)
	{
		for (var i = 0; i < tuple.Length; ++i)
		{
			var type = tuple[i]!.GetType();

			if (typeof(ITuple).IsAssignableFrom(type))
			{
				ParseTuple((ITuple)tuple[i]!, terms);
				continue;
			}

			var id = GetID(type);
			terms.Add(new Term()
			{
				ID = IDOp.GetPairSecond(id),
				Op = IDOp.GetPairFirst(id) == 0 ? TermOp.With : TermOp.Without
			});
		}
	}

	static void ParseType<T>(SortedSet<Term> terms) where T : struct
	{
		var type = typeof(T);
		if (_typesConvertion.TryGetValue(type, out var id))
		{
			terms.Add(new Term()
			{
				ID = IDOp.GetPairSecond(id),
				Op = IDOp.GetPairFirst(id) == 0 ? TermOp.With : TermOp.Without
			});

			return;
		}

		if (typeof(ITuple).IsAssignableFrom(type))
		{
			ParseTuple((ITuple)default(T), terms);

			return;
		}

		EcsAssert.Assert(false, $"type not found {type}");
	}

    internal static class Query<TQuery, TFilter> where TQuery : struct where TFilter : struct
	{
		public static readonly ImmutableArray<Term> Terms;
		public static readonly ImmutableArray<Term> Columns;
		public static readonly ImmutableDictionary<ulong, Term> Withs, Withouts;
		public static readonly ulong Hash;

		static Query()
		{
			var list = new SortedSet<Term>();
			ParseType<TQuery>(list);
			Columns = list.ToImmutableArray();

			ParseType<TFilter>(list);
			Terms = list.ToImmutableArray();

			Withs = list.Where(s => s.Op == TermOp.With).ToImmutableDictionary(s => s.ID, k => k);
			Withouts = list.Where(s => s.Op == TermOp.Without).ToImmutableDictionary(s => s.ID, k => k);

			Hash = Hashing.Calculate(Withs.Values.ToArray());
		}
	}

	internal static class Query<T> where T : struct
	{
		public static readonly ImmutableArray<Term> Terms;
		public static readonly ImmutableDictionary<ulong, Term> Withs, Withouts;
		public static readonly ulong Hash;

		static Query()
		{
			var list = new SortedSet<Term>();
			ParseType<T>(list);
			Terms = list.ToImmutableArray();

			Withs = list.Where(s => s.Op == TermOp.With).ToImmutableDictionary(s => s.ID, k => k);
			Withouts = list.Where(s => s.Op == TermOp.Without).ToImmutableDictionary(s => s.ID, k => k);

			Hash = Hashing.Calculate(Withs.Values.ToArray());
		}
	}
}

struct EcsRecord
{
	public Archetype Archetype;
    public int Row;

    public readonly ref ArchetypeChunk GetChunk() => ref Archetype.GetChunk(Row);
}

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
	private readonly object _newEntLock = new object();


    public World(ulong maxComponentId = 256)
    {
        _comparer = new ComponentComparer(this);
        _archRoot = new Archetype(
            this,
            ImmutableArray<ComponentInfo>.Empty,
            _comparer
        );

		_maxCmpId = maxComponentId;
        _entities.MaxID = maxComponentId;
		OnPluginInitialization?.Invoke(this);


		_ = Entity<Wildcard>();
    }

	public event Action<EntityView>? OnEntityCreated, OnEntityDeleted;
	public event Action<EntityView, ComponentInfo>? OnComponentSet, OnComponentUnset;
	public static event Action<World>? OnPluginInitialization;

    public int EntityCount => _entities.Length;
	internal Archetype Root => _archRoot;

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

		var isPair = IDOp.IsPair(lookup.ID);

		EcsAssert.Panic(isPair || lookup.ID < _maxCmpId,
			"Increase the minimum number for components when initializing the world [ex: new World(1024)]");

		if (!isPair && !Exists(lookup.ID))
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
		lock (_newEntLock)
		{
			ref var record = ref (
				id > 0 ? ref _entities.Add(id, default!) : ref _entities.CreateNew(out id)
			);
			record.Archetype = _archRoot;
			record.Row = _archRoot.Add(id);

			var e = new EntityView(this, id);

			OnEntityCreated?.Invoke(e);

			return e;
		}
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

		lock (_newEntLock)
		{
			OnEntityDeleted?.Invoke(new (this, entity));

			ref var record = ref GetRecord(entity);

			var removedId = record.Archetype.Remove(ref record);
			EcsAssert.Assert(removedId == entity);

			_entities.Remove(removedId);
		}
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

	private void DetachComponent(ref EcsRecord record, ref readonly ComponentInfo cmp)
	{
		var oldArch = record.Archetype;

		if (oldArch.GetComponentIndex(in cmp) < 0)
            return;

		OnComponentUnset?.Invoke(record.GetChunk().EntityAt(record.Row), cmp);

		var newSign = oldArch.Components.Remove(cmp);
		EcsAssert.Assert(newSign.Length < oldArch.Components.Length, "bad");

		ref var newArch = ref GetArchetype(newSign, true);
		if (newArch == null)
		{
			newArch = _archRoot.InsertVertex(oldArch, newSign, in cmp);

			if (_archetypeCount >= _archetypes.Length)
				Array.Resize(ref _archetypes, _archetypes.Length * 2);
			_archetypes[_archetypeCount++] = newArch;
		}

		record.Row = record.Archetype.MoveEntity(newArch!, record.Row);
        record.Archetype = newArch!;
	}

	private int AttachComponent(ref EcsRecord record, ref readonly ComponentInfo cmp)
	{
		var oldArch = record.Archetype;

		var index = oldArch.GetComponentIndex(in cmp);
		if (index >= 0)
            return index;

		var newSign = oldArch.Components.Add(cmp).Sort(_comparer);
		EcsAssert.Assert(newSign.Length > oldArch.Components.Length, "bad");

		ref var newArch = ref GetArchetype(newSign, true);
		if (newArch == null)
		{
			newArch = _archRoot.InsertVertex(oldArch, newSign, in cmp);

			if (_archetypeCount >= _archetypes.Length)
				Array.Resize(ref _archetypes, _archetypes.Length * 2);
			_archetypes[_archetypeCount++] = newArch;
		}

		record.Row = record.Archetype.MoveEntity(newArch, record.Row);
        record.Archetype = newArch!;

		OnComponentSet?.Invoke(record.GetChunk().EntityAt(record.Row), cmp);

		return newArch.GetComponentIndex(cmp.ID);
	}

    private ref Archetype? GetArchetype(ImmutableArray<ComponentInfo> components, bool create)
	{
		var hash = Hashing.Calculate(components.AsSpan());
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

	internal Array? Set(ref EcsRecord record, ref readonly ComponentInfo cmp)
    {
        var column = AttachComponent(ref record, in cmp);
        var array = cmp.Size > 0 ? record.GetChunk().RawComponentData(column) : null;
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
		BeginDeferred();

		foreach (var arch in GetQuery(0, ImmutableArray<Term>.Empty, static (world, terms) => new Query(world, terms)))
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

		EndDeferred();
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

    public QueryBuilder QueryBuilder() => new QueryBuilder(this);

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
	private static readonly Dictionary<Type, Term> _typesConvertion = new();
	private static readonly Dictionary<Type, ComponentInfo> _componentInfos = new();

	public static Array? GetArray(ulong hashcode, int count)
	{
		var ok = _arrayCreator.TryGetValue(hashcode, out var fn);
		EcsAssert.Assert(ok, $"component not found with hashcode {hashcode}");
		return fn?.Invoke(count) ?? null;
	}

	private static Term GetTerm(Type type)
	{
		var ok = _typesConvertion.TryGetValue(type, out var term);
		EcsAssert.Assert(ok, $"component not found with type {type}");
		return term;
	}

	[SkipLocalsInit]
    internal static class Component<T> where T : struct
	{
        public static readonly int Size = GetSize();
        public static readonly string Name = typeof(T).ToString();
        public static readonly ulong HashCode;
		public static readonly ComponentInfo Value;

		static Component()
		{
			if (typeof(ITuple).IsAssignableFrom(typeof(T)))
			{
				var tuple = (ITuple)default(T);
				EcsAssert.Panic(tuple.Length == 2, "Relations must be composed by 2 arguments only.");

				var firstId = GetTerm(tuple[0]!.GetType());
				var secondId = GetTerm(tuple[1]!.GetType());
				var pairId = IDOp.Pair(firstId.ID, secondId.ID);

				HashCode = pairId;
				Size = 0;

				if (_componentInfos.TryGetValue(tuple[1]!.GetType(), out var secondCmpInfo))
				{
					Size = secondCmpInfo.Size;
				}
			}
			else
			{
				HashCode = (ulong)System.Threading.Interlocked.Increment(ref Unsafe.As<ulong, int>(ref _index));
			}

			Value = new ComponentInfo(HashCode, Size);
			_arrayCreator.Add(Value.ID, count => Size > 0 ? new T[count] : Array.Empty<T>());

			_typesConvertion.Add(typeof(T), Term.With(Value.ID));
			_typesConvertion.Add(typeof(With<T>), Term.With(Value.ID));
			_typesConvertion.Add(typeof(Not<T>), Term.Without(Value.ID));
			_typesConvertion.Add(typeof(Without<T>), Term.Without(Value.ID));

			_componentInfos.Add(typeof(T), Value);
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

			var term = GetTerm(type);
			terms.Add(term);
		}
	}

	static void ParseType<T>(SortedSet<Term> terms) where T : struct
	{
		var type = typeof(T);
		if (_typesConvertion.TryGetValue(type, out var term))
		{
			terms.Add(term);

			return;
		}

		if (typeof(ITuple).IsAssignableFrom(type))
		{
			ParseTuple((ITuple)default(T), terms);

			return;
		}

		EcsAssert.Panic(false, $"Type {type} is not registered. Register {type} using world.Entity<T>() or assign it to an entity.");
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

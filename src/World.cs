using System.Collections.Immutable;
using Microsoft.Collections.Extensions;

namespace TinyEcs;

public sealed partial class World : IDisposable
{
    const ulong ECS_MAX_COMPONENT_FAST_ID = 256;
    const ulong ECS_START_USER_ENTITY_DEFINE = ECS_MAX_COMPONENT_FAST_ID;

    private readonly Archetype _archRoot;
    private readonly EntitySparseSet<EcsRecord> _entities = new();
    private readonly DictionarySlim<ulong, Archetype> _typeIndex = new();
	private readonly Dictionary<string, EcsID> _entityNames = new();
    private Archetype[] _archetypes = new Archetype[16];
    private int _archetypeCount;
    private readonly ComponentComparer _comparer;
    private readonly Commands _commands;
    private int _frame;
    private EcsID _lastCompID = 1;

    public World()
    {
        _comparer = new ComponentComparer(this);
        _archRoot = new Archetype(
            this,
            ReadOnlySpan<EcsComponent>.Empty,
            _comparer
        );
        _commands = new(this);

        //InitializeDefaults();
        //_entities.MaxID = ECS_START_USER_ENTITY_DEFINE;

		OnPluginInitialization?.Invoke(this);
    }

	public event Action<EntityView>? OnEntityCreated, OnEntityDeleted;
	public event Action<EntityView, EcsComponent> OnComponentSet, OnComponentUnset;
	public static event Action<World>? OnPluginInitialization;


    public int EntityCount => _entities.Length;

    public ReadOnlySpan<Archetype> Archetypes => _archetypes.AsSpan(0, _archetypeCount);

    public CommandEntityView DeferredEntity() => _commands.Entity();

    public void Merge() => _commands.Merge();

    public void Dispose()
    {
        _entities.Clear();
        _archRoot.Clear();
        _typeIndex.Clear();
        _commands.Clear();
		_entityNames.Clear();

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

    internal unsafe ref readonly EcsComponent Component<T>() where T : struct
	{
        ref readonly var lookup = ref Lookup.Component<T>.Value;

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

    public EntityView Entity(string name)
    {
		_entityNames.TryGetValue(name, out var id);

        var entity = Entity(id);
		if (id == 0)
		{
			_entityNames.Add(name, entity.ID);
			GetRecord(entity.ID).Name = name;
		}

		return entity;
    }

    public EntityView Entity(EcsID id = default)
    {
        return id == 0 || !Exists(id) ? NewEmpty(id) : new(this, id);
    }

    internal EntityView NewEmpty(ulong id = 0)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref EcsRecord GetRecord(EcsID id)
    {
        ref var record = ref _entities.Get(id);
        EcsAssert.Assert(!Unsafe.IsNullRef(ref record));
        return ref record;
    }

    public void Delete(EcsID entity)
    {
		OnEntityDeleted?.Invoke(new (this, entity));

        ref var record = ref GetRecord(entity);

        var removedId = record.Archetype.Remove(ref record);
        EcsAssert.Assert(removedId == entity);

        _entities.Remove(removedId);

		if (!string.IsNullOrEmpty(record.Name))
			_entityNames.Remove(record.Name);
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

    internal void DetachComponent(EcsID entity, ref readonly EcsComponent cmp)
    {
		OnComponentUnset?.Invoke(Entity(entity), cmp);
        ref var record = ref GetRecord(entity);
        InternalAttachDetach(ref record, in cmp, false);
    }

    private bool InternalAttachDetach(
        ref EcsRecord record,
        ref readonly EcsComponent cmp,
        bool add
    )
    {
        EcsAssert.Assert(!Unsafe.IsNullRef(ref record));

        var arch = CreateArchetype(record.Archetype, in cmp, add);
        if (arch == null)
            return false;

        record.Row = record.Archetype.MoveEntity(arch, record.Row);
        record.Archetype = arch!;

        return true;
    }

    [SkipLocalsInit]
    private Archetype? CreateArchetype(Archetype root, ref readonly EcsComponent cmp, bool add)
    {
        if (!add && root.GetComponentIndex(in cmp) < 0)
            return null;

        var initType = root.Components;
        var cmpCount = Math.Max(0, initType.Length + (add ? 1 : -1));

        const int STACKALLOC_SIZE = 16;
		EcsComponent[]? buffer = null;
		scoped var span = cmpCount <= STACKALLOC_SIZE ? stackalloc EcsComponent[STACKALLOC_SIZE] : (buffer = ArrayPool<EcsComponent>.Shared.Rent(cmpCount));

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
            ArrayPool<EcsComponent>.Shared.Return(buffer);

        return arch;
    }

    private ref Archetype? GetArchetype(Span<EcsComponent> components, bool create)
	{
		var hash = getHash(components, false);
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static ulong getHash(Span<EcsComponent> components, bool checkSize)
		{
			var hc = (ulong)components.Length;
			foreach (ref var val in components)
				hc = unchecked(hc * 314159 + val.ID);
			return hc;
		}
	}

	internal Array? Set(EntityView entity, ref EcsRecord record, ref readonly EcsComponent cmp)
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
            //EmitEvent(EcsEventOnSet, entity, cmp.ID);
        }

        return array;
    }

    internal bool Has(EcsID entity, ref readonly EcsComponent cmp)
    {
        ref var record = ref GetRecord(entity);
        return record.Archetype.GetComponentIndex(in cmp) >= 0;
    }

    public ReadOnlySpan<EcsComponent> GetType(EcsID id)
    {
        ref var record = ref GetRecord(id);
        return record.Archetype.Components;
    }

    public void PrintGraph()
    {
        _archRoot.Print();
    }

    public FilterQuery<TFilter> Filter<TFilter>() where TFilter : struct
    {
	    return new FilterQuery<TFilter>(this);
    }

    public FilterQuery Filter(ReadOnlySpan<Term> terms)
    {
	    return new FilterQuery(_archetypes.AsSpan(0, _archetypeCount), terms);
    }

    public IQueryConstruct QueryBuilder() => new QueryBuilder(this);
	
	internal Archetype? FindArchetype(ulong hash, ReadOnlySpan<Term> terms)
	{
		if (!_typeIndex.TryGetValue(hash, out var arch))
		{
			arch = FindFirst(_archRoot, terms);
		}

		return arch;
	}

	public void System<TFilter, T0, T1>(QueryFilterDelegate<T0, T1> fn) 
		where TFilter : struct where T0 : struct where T1 : struct
	{
		var hash = Lookup.Query<(T0, T1), TFilter>.Hash;
		var terms = Lookup.Query<(T0, T1), TFilter>.Terms.AsSpan();
		var withouts = Lookup.Query<(T0, T1), TFilter>.Withouts;

		var arch = FindArchetype(hash, terms);
		if (arch == null)
			return;

		InternalQuery(arch, fn, terms, withouts);

		static void InternalQuery
		(
			Archetype root, 
			QueryFilterDelegate<T0, T1> fn, 
			ReadOnlySpan<Term> terms,
			IDictionary<ulong, Term> withouts
		)
		{
			if (root.Count > 0)
			{
				var column0 = root.GetComponentIndex<T0>();
				var column1 = root.GetComponentIndex<T1>();

				foreach (ref readonly var chunk in root)
				{
					ref var c0 = ref chunk.GetReference<T0>(column0);
					ref var c1 = ref chunk.GetReference<T1>(column1);
					ref var last = ref Unsafe.Add(ref c0, chunk.Count);

					while (Unsafe.IsAddressLessThan(ref c0, ref last))
					{
						fn(ref c0, ref c1);
						c0 = ref Unsafe.Add(ref c0, 1);
						c1 = ref Unsafe.Add(ref c1, 1);
					}
				}
			}

			var span = CollectionsMarshal.AsSpan(root._edgesRight);

			ref var start = ref MemoryMarshal.GetReference(span);
			ref var end = ref Unsafe.Add(ref start, span.Length);

			while (Unsafe.IsAddressLessThan(ref start, ref end))
			{
				if (!withouts.ContainsKey(start.ComponentID))
					InternalQuery(start.Archetype, fn, terms, withouts);

				start = ref Unsafe.Add(ref start, 1);
			}
		}
	}

	static Archetype? FindFirst(Archetype root, ReadOnlySpan<Term> terms)
	{
		var result = root.FindMatch(terms);
		if (result < 0)
		{
			return null;
		}

		if (result == 0)
		{
			return root;
		}

		var span = CollectionsMarshal.AsSpan(root._edgesRight);
		if (span.IsEmpty)
			return null;

		ref var start = ref MemoryMarshal.GetReference(span);
		ref var end = ref Unsafe.Add(ref start, span.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			var res = FindFirst(start.Archetype, terms);
			if (res != null)
				return res;

			start = ref Unsafe.Add(ref start, 1);
		}

		return null;
	}
}

public ref struct QueryInternal
{
	private readonly ReadOnlySpan<Archetype> _archetypes;
	private readonly ReadOnlySpan<Term> _terms;
	private int _index;

	internal QueryInternal(ReadOnlySpan<Archetype> archetypes, ReadOnlySpan<Term> terms)
	{
		_archetypes = archetypes;
		_terms = terms;
		_index = -1;
	}

	public readonly Archetype Current => _archetypes[_index];

	public bool MoveNext()
	{
		while (++_index < _archetypes.Length)
		{
			var arch = _archetypes[_index];
			var result = arch.FindMatch(_terms);
			if (result == 0 && arch.Count > 0)
				return true;
		}

		return false;
	}

	public void Reset() => _index = -1;

	public readonly QueryInternal GetEnumerator() => this;
}

public delegate void QueryFilterDelegateWithEntity(EntityView entity);

public readonly ref partial struct FilterQuery<TFilter> where TFilter : struct
{
	private readonly World _world;

	internal FilterQuery(World world)
	{
		_world = world;
	}

	public void Query(QueryFilterDelegateWithEntity fn)
	{
		var terms = Lookup.Query<TFilter>.Terms;
		var query = new QueryInternal(_world.Archetypes, terms.AsSpan());
		foreach (var arch in query)
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

	public QueryInternal GetEnumerator()
    {
        return new QueryInternal(_world.Archetypes, Lookup.Query<TFilter>.Terms.AsSpan());
    }
}

public readonly ref struct FilterQuery
{
	private readonly ReadOnlySpan<Archetype> _archetypes;
	private readonly ReadOnlySpan<Term> _terms;

	internal FilterQuery(ReadOnlySpan<Archetype> archetypes, ReadOnlySpan<Term> terms)
	{
		_archetypes = archetypes;
		_terms = terms;
	}

	public QueryInternal GetEnumerator()
	{
		return new QueryInternal(_archetypes, _terms);
	}
}

public interface IQueryConstruct
{
	IQueryBuild With<T>() where T : struct;
	IQueryBuild With(EcsID id);
	IQueryBuild Without<T>() where T : struct;
	IQueryBuild Without(EcsID id);
}

public interface IQueryBuild
{
	QueryInternal Build();
}

public sealed class QueryBuilder : IQueryConstruct, IQueryBuild
{
	private readonly World _world;
	private readonly HashSet<Term> _components = new();
	private Term[]? _terms;

	internal QueryBuilder(World world) => _world = world;

	public IQueryBuild With<T>() where T : struct
		=> With(_world.Component<T>().ID);

	public IQueryBuild With(EcsID id)
	{
		_components.Add(Term.With(id));
		return this;
	}

	public IQueryBuild Without<T>() where T : struct
		=> Without(_world.Component<T>().ID);

	public IQueryBuild Without(EcsID id)
	{
		_components.Add(Term.Without(id));
		return this;
	}

	public QueryInternal Build()
	{
		_terms ??= _components.ToArray();
		return new QueryInternal(_world.Archetypes, _terms);
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
        public static readonly ulong HashCode = (ulong)System.Threading.Interlocked.Increment(ref Unsafe.As<ulong, int>(ref _index)) - 1;

		public static readonly EcsComponent Value = new EcsComponent(HashCode, Size);

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

			var id = Lookup.GetID(type);
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static ulong GetHash(ReadOnlySpan<Term> terms)
	{
		var hc = (ulong)terms.Length;
		foreach (ref readonly var val in terms)
			hc = unchecked(hc * 314159 + val.ID);
		return hc;
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

			Hash = GetHash(Withs.Values.ToArray());
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

			Hash = GetHash(Withs.Values.ToArray());
		}
	}
}

struct EcsRecord
{
	public Archetype Archetype;
    public int Row;
	public string? Name;

    public readonly ref ArchetypeChunk GetChunk() => ref Archetype.GetChunk(Row);
}

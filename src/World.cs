using Microsoft.Collections.Extensions;

namespace TinyEcs;

public sealed partial class World : IDisposable
{
    const ulong ECS_MAX_COMPONENT_FAST_ID = 256;
    const ulong ECS_START_USER_ENTITY_DEFINE = ECS_MAX_COMPONENT_FAST_ID;

    private readonly Archetype _archRoot;
    private readonly EntitySparseSet<EcsRecord> _entities = new();
    private readonly DictionarySlim<ulong, Archetype> _typeIndex = new();
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
    }

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

    public void ChildOf(EcsID id, EcsID child)
    {
	    var pair = IDOp.Pair(Component<ChildOf>().ID, id);
	    var cmp = new EcsComponent(pair, 0);
	    ref var record = ref GetRecord(child);
	    _ = Set(ref record, in cmp);
    }

    public void Relation<TRoot>(EcsID id, EcsID target) where TRoot : struct
    {
	    Set<(TRoot, EcsID)>(id, (default, target));
	    Set<(EcsID, TRoot)>(target, (id, default));
    }

    public EcsID Pair(EcsID first, EcsID second) => IDOp.Pair(first, second);
    public EcsID Pair<TAction>(EcsID target) where TAction : struct => IDOp.Pair(Component<TAction>().ID, target);

    public void AddChild(EcsID id, EcsID child)
    {
	    if (Has<Parent>(id))
	    {
		    ref var parent = ref Get<Parent>(id);
		    EcsAssert.Panic(parent.ParentId != child, "you can't add the child as parent");
	    }

	    if (Has<Parent>(child))
	    {
		    ref var parent = ref Get<Parent>(child);
		    if (parent.ParentId == id)
			    return;

		    ref var children = ref Get<Children>(parent.ParentId);
		    children.Ids.Remove(child);
	    }

	    Set(child, new Parent() { ParentId = id });

	    if (Has<Children>(id))
	    {
		    ref var children = ref Get<Children>(id);
		    children.Ids.Add(child);
	    }
	    else
	    {
		    var children = new Children();
		    children.Ids.Add(child);
		    Set(id, children);
	    }
    }

    public void RemoveChild(EcsID id)
    {
	    EcsAssert.Panic(Has<Parent>(id), $"{id} is not a child!?");

	    ref var parent = ref Get<Parent>(id);
	    ref var children = ref Get<Children>(parent.ParentId);
	    children.Ids.Remove(id);
    }

    public EntityView Entity(string name)
    {
        // TODO

        EcsID id = 0;

        return Entity(id);
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

        return new(this, id);
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

    internal void DetachComponent(EcsID entity, ref readonly EcsComponent cmp)
    {
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
			arch = ref _typeIndex.GetOrAddValueRef(hash, out var exists);
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

	internal Array? Set(ref EcsRecord record, ref readonly EcsComponent cmp)
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
	    return new FilterQuery<TFilter>(_archetypes.AsMemory(0, _archetypeCount));
    }

    public FilterQuery Filter(ReadOnlySpan<Term> terms)
    {
	    return new FilterQuery(_archetypes.AsMemory(0, _archetypeCount), terms);
    }

    public IQueryConstruct QueryBuilder() => new QueryBuilder(this);
}

public readonly ref struct QueryInternal
{
	private readonly ReadOnlySpan<Archetype> _archetypes;
	private readonly ReadOnlySpan<Term> _terms;

	internal QueryInternal(ReadOnlySpan<Archetype> archetypes, ReadOnlySpan<Term> terms)
	{
		_archetypes = archetypes;
		_terms = terms;
	}

	public QueryIterator GetEnumerator()
	{
		return new QueryIterator(_archetypes, _terms);
	}
}

public ref struct QueryIterator
{
	private readonly ReadOnlySpan<Term> _terms;
	private readonly ReadOnlySpan<Archetype> _archetypes;
	private int _index;

	internal QueryIterator(ReadOnlySpan<Archetype> archetypes, ReadOnlySpan<Term> terms)
	{
		_archetypes = archetypes;
		_terms = terms;
		_index = -1;
	}

	public Archetype Current => _archetypes[_index];

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
}

public readonly ref partial struct FilterQuery<TFilter> where TFilter : struct
{
	private readonly ReadOnlyMemory<Archetype> _archetypes;

	internal FilterQuery(ReadOnlyMemory<Archetype> archetypes)
	{
		_archetypes = archetypes;
	}

	public FilterQuery<TFilter> Pair(EcsID first, EcsID second)
	{
		return this;
	}

	public QueryIterator GetEnumerator()
    {
        return new QueryIterator(_archetypes.Span, Lookup.Query<TFilter>.Terms);
    }
}

public readonly ref struct FilterQuery
{
	private readonly ReadOnlyMemory<Archetype> _archetypes;
	private readonly ReadOnlySpan<Term> _terms;

	internal FilterQuery(ReadOnlyMemory<Archetype> archetypes, ReadOnlySpan<Term> terms)
	{
		_archetypes = archetypes;
		_terms = terms;
	}

	public QueryIterator GetEnumerator()
	{
		return new QueryIterator(_archetypes.Span, _terms);
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

	static void ParseTuple(ITuple tuple, HashSet<Term> terms)
	{
		for (var i = 0; i < tuple.Length; ++i)
		{
			var type = tuple[i]!.GetType();

			if (typeof(ITuple).IsAssignableFrom(type))
			{
				ParseTuple((ITuple)tuple[i], terms);
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

	static void ParseType<T>(HashSet<Term> terms) where T : struct
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
		}

		EcsAssert.Assert(false, $"type not found {type}");
	}

    internal static class Query<TQuery, TFilter> where TQuery : struct where TFilter : struct
	{
		public static readonly Term[] Terms;
		public static readonly Term[] Columns;

		static Query()
		{
			var list = new HashSet<Term>();
			ParseType<TQuery>(list);
			Columns = list.ToArray();
			Array.Sort(Columns);

			ParseType<TFilter>(list);
			Terms = list.ToArray();
			Array.Sort(Terms);
		}
	}

	internal static class Query<T> where T : struct
	{
		public static readonly Term[] Terms;

		static Query()
		{
			var list = new HashSet<Term>();
			ParseType<T>(list);
			Terms = list.ToArray();
			Array.Sort(Terms);
		}
	}
}

struct EcsRecord
{
	public Archetype Archetype;
    public int Row;

    public readonly ref ArchetypeChunk GetChunk() => ref Archetype.GetChunk(Row);
}

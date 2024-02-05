namespace TinyEcs;

public sealed partial class World : IDisposable
{
    const ulong ECS_MAX_COMPONENT_FAST_ID = 256;
    const ulong ECS_START_USER_ENTITY_DEFINE = ECS_MAX_COMPONENT_FAST_ID;

    private readonly Archetype _archRoot;
    private readonly EntitySparseSet<EcsRecord> _entities = new();
    private readonly Dictionary<EcsID, Archetype> _typeIndex = new();
    private readonly Dictionary<EcsID, Table> _tableIndex = new();
    private readonly ComponentComparer _comparer;
    private readonly Commands _commands;
    private int _frame;
    private EcsID _lastCompID = 1;

    public World()
    {
        _comparer = new ComponentComparer(this);
        _archRoot = new Archetype(
            this,
            new(0, ReadOnlySpan<EcsComponent>.Empty, _comparer),
            ReadOnlySpan<EcsComponent>.Empty,
            _comparer
        );
        _commands = new(this);

        InitializeDefaults();
        //_entities.MaxID = ECS_START_USER_ENTITY_DEFINE;
    }

    public int EntityCount => _entities.Length;

    public float DeltaTime { get; private set; }

    public void Dispose()
    {
        _entities.Clear();
        _archRoot.Clear();
        _typeIndex.Clear();
        _tableIndex.Clear();
        _commands.Clear();
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

    // internal unsafe EntityView CreateComponent(EcsID id, int size)
    // {
    //     if (id == 0)
    //     {
    //         id = NewEmpty();
    //     }
    //     else if (Exists(id) && Has<EcsComponent>(id))
    //     {
    //         ref var cmp = ref Get<EcsComponent>(id);
    //         EcsAssert.Panic(cmp.Size == size);

    //         return Entity(id);
    //     }

    //     size = Math.Max(size, 0);

    //     var cmp2 = new EcsComponent(id, size);
    //     Set(id, cmp2);
    //     Set(id, EcsPanic, EcsDelete);

    //     if (size == 0)
    //         Set(id, EcsTag);

    //     return Entity(id);
    // }

    // private unsafe int GetSize<T>() 
    // {
    //     var size = sizeof(T);

    //     if (size != 1)
    //         return size;

    //     // credit: BeanCheeseBurrito from Flecs.NET
    //     Unsafe.SkipInit<T>(out var t1);
    //     Unsafe.SkipInit<T>(out var t2);
    //     Unsafe.As<T, byte>(ref t1) = 0x7F;
    //     Unsafe.As<T, byte>(ref t2) = 0xFF;

    //     return ValueType.Equals(t1, t2) ? 0 : size;
    // }

    internal unsafe ref readonly EcsComponent Component<T>() where T : struct
	{
        ref readonly var lookup = ref Lookup.Entity<T>.Component;

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

    public Query Query() => new(this);

    public unsafe EntityView Event(
        delegate* <ref Iterator, void> callback,
        ReadOnlySpan<Term> terms,
        ReadOnlySpan<EcsID> events
    )
    {
        var obs = Entity().Set(new EcsEvent(callback, terms));

        foreach (ref readonly var id in events)
            obs.Set(id);

        return obs;
    }

    public EntityView Entity(ReadOnlySpan<char> name)
    {
        // TODO
        EcsID id = 0;

        return Entity(id);
    }

    public EntityView Entity(EcsID id = default)
    {
        return id == 0 || !Exists(id) ? NewEmpty(id) : new(this, id);
    }

    public EntityView Entity<T>(EcsID id = default) where T : struct
	{
        return Entity(Component<T>().ID);
    }

    internal EntityView NewEmpty(ulong id = 0)
    {
        ref var record = ref (
            id > 0 ? ref _entities.Add(id, default!) : ref _entities.CreateNew(out id)
        );
        record.Archetype = _archRoot;
        record.Row = _archRoot.Add(id).Item1;

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
        InternalAttachDetach(entity, ref record, in cmp, false);
    }

    private bool InternalAttachDetach(
        EcsID entity,
        ref EcsRecord record,
        ref readonly EcsComponent cmp,
        bool add
    )
    {
        EcsAssert.Assert(!Unsafe.IsNullRef(ref record));

        // if (add)
        // {
        //     // TODO: Use Events to handle this case.
        //     //		 The event must check for component with Exclusive tag
        //     if (IDOp.IsPair(cmp.ID))
        //     {
        //         var first = IDOp.GetPairFirst(cmp.ID);
        //         var second = IDOp.GetPairSecond(cmp.ID);

        //         if (Has(first, EcsExclusive))
        //         {
        //             var cmp2 = new EcsComponent(IDOp.Pair(first, EcsAny), 0);
        //             var column = record.Archetype.GetComponentIndex(ref cmp2);

        //             if (column >= 0 && record.Archetype.ComponentInfo[column].ID != cmp.ID)
        //             {
        //                 DetachComponent(entity, ref record.Archetype.ComponentInfo[column]);
        //             }
        //         }
        //     }
        // }
        // else
        // {
        //     EmitEvent(EcsEventOnUnset, entity, cmp.ID);
        // }

        //var arch = MakeArchetype(record.Archetype, ref cmp, add);

        var arch = CreateArchetype(record.Archetype, in cmp, add);
        if (arch == null)
            return false;

        record.Row = record.Archetype.MoveEntity(arch, record.Row);
        record.Archetype = arch!;

        return true;
    }

    private Archetype? MakeArchetype(Archetype root, ref EcsComponent cmp, bool add)
    {
        // ignore if the entity doesn't have the component
        if (!add && root.GetComponentIndex(ref cmp) < 0)
            return null;

        var edges = add ? root._edgesRight : root._edgesLeft;

        foreach (ref var edge in CollectionsMarshal.AsSpan(edges))
        {
            if (edge.ComponentID == cmp.ID)
            {
                var super = add ? edge.Archetype : root;
                var sub = !add ? edge.Archetype : root;
                if (super.IsSuperset(sub.ComponentInfo))
                {
                    return edge.Archetype;
                }
            }
        }

        var newSign = new EcsComponent[root.ComponentInfo.Length + (add ? 1 : -1)];
        if (add)
        {
            root.ComponentInfo.CopyTo(newSign, 0);
            newSign[^1] = cmp;
            Array.Sort(newSign, _comparer);
        }
        else
        {
            for (int i = 0, j = 0; i < root.ComponentInfo.Length; ++i)
            {
                if (root.ComponentInfo[i].ID != cmp.ID)
                {
                    newSign[j++] = root.ComponentInfo[i];
                }
            }
        }

        ref var table = ref Unsafe.NullRef<Table>();

        if (cmp.Size != 0)
        {
            var tableHash = Hash(newSign, true);
            table = ref CollectionsMarshal.GetValueRefOrAddDefault(
                _tableIndex,
                tableHash,
                out var exists
            )!;
            if (!exists)
            {
                table = new(tableHash, newSign, _comparer);
            }
        }
        else
        {
            table = ref Unsafe.AsRef(root.Table)!;
        }

        var arch = _archRoot.InsertVertex(root, table, newSign, ref cmp);

        return arch;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static EcsID Hash(UnsafeSpan<EcsComponent> components, bool checkSize)
        {
            unchecked
            {
                EcsID hash = 5381;

                while (components.CanAdvance())
                {
                    if (!checkSize || components.Value.Size > 0)
                        hash = ((hash << 5) + hash) + components.Value.ID;

                    components.Advance();
                }

                return hash;
            }
        }
    }

    [SkipLocalsInit]
    internal Archetype? CreateArchetype(Archetype root, ref readonly EcsComponent cmp, bool add)
    {
        // var column = root.GetComponentIndex(ref cmp);

        // if (add && column >= 0)
        // {
        // 	return null;
        // }
        // else if (!add && column < 0)
        // {
        // 	return null;
        // }

        if (!add && root.GetComponentIndex(in cmp) < 0)
            return null;

        var initType = root.ComponentInfo;
        var cmpCount = Math.Max(0, initType.Length + (add ? 1 : -1));

        const int STACKALLOC_SIZE = 16;

        EcsComponent[]? buffer = ArrayPool<EcsComponent>.Shared.Rent(cmpCount);
		Span<EcsComponent> span = buffer;


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

        var hash = Hash(span, false);

        ref var arch = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _typeIndex,
            hash,
            out var exists
        );
        if (!exists)
        {
            ref var table = ref Unsafe.NullRef<Table>();

            if (cmp.Size != 0)
            {
                var tableHash = Hash(span, true);
                table = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    _tableIndex,
                    tableHash,
                    out exists
                )!;
                if (!exists)
                {
                    table = new(tableHash, span, _comparer);
                }
            }
            else
            {
                table = ref Unsafe.AsRef(root.Table)!;
            }

            arch = _archRoot.InsertVertex(root, table, span, in cmp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static EcsID Hash(UnsafeSpan<EcsComponent> components, bool checkSize)
        {
            unchecked
            {
                EcsID hash = 5381;

                while (components.CanAdvance())
                {
                    if (!checkSize || components.Value.Size > 0)
                        hash = ((hash << 5) + hash) + components.Value.ID;

                    components.Advance();
                }

                return hash;
            }
        }

        if (buffer != null)
        {
            ArrayPool<EcsComponent>.Shared.Return(buffer);
        }

        return arch;
    }

    internal void Set<T>(EcsID entity, ref readonly EcsComponent cmp, ref readonly T data) where T : struct
    {
        EcsAssert.Assert(cmp.Size == Lookup.Entity<T>.Size);

        ref var record = ref GetRecord(entity);

        var emit = false;
        var column = record.Archetype.GetComponentIndex(in cmp);
        if (column < 0)
        {
            emit = InternalAttachDetach(entity, ref record, in cmp, true);
            column = record.Archetype.GetComponentIndex(in cmp);
        }

        if (cmp.Size > 0)
        {
			record.Archetype.Table
				.ComponentData<T>(column, record.Archetype.EntitiesTableRows[record.Row], 1)[0] = data;
        }

        if (emit)
        {
            //EmitEvent(EcsEventOnSet, entity, cmp.ID);
        }
    }

    [SkipLocalsInit]
    public unsafe void EmitEvent(EcsID eventID, EcsID entity, EcsID component)
    {
        // EcsAssert.Assert(Exists(eventID));
        // EcsAssert.Assert(Exists(entity));
        // EcsAssert.Assert(Exists(component));

        // Query(
        //     stackalloc Term[] { Term.With(EcsEvent), Term.With(eventID), },
        //     &OnEvent,
        //     new ObserverInfo()
        //     {
        //         Entity = entity,
        //         Event = eventID,
        //         LastComponent = Term.With(component)
        //     }
        // );
    }

    private struct ObserverInfo
    {
        public EcsID Entity;
        public EcsID Event;
        public Term LastComponent;
    }

    static unsafe void OnEvent(ref Iterator it)
    {
        if (it.Count == 0)
            return;

        //var columns = Span<Array>.Empty;
        //ref var eventInfo = ref Unsafe.Unbox<ObserverInfo>(it.UserData!);
        //ref var record = ref it.World.GetRecord(eventInfo.Entity);
        //var iterator = new Iterator(
        //    it.Commands,
        //    1,
        //    record.Archetype.Table,
        //    stackalloc EcsID[1] { eventInfo.Entity },
        //    stackalloc int[1] { record.Archetype.EntitiesTableRows[record.Row] },
        //    null,
        //    columns,
        //    eventInfo.Event,
        //    eventInfo.LastComponent
        //);

        //var evA = it.Field<EcsEvent>(0);

        //for (int i = 0; i < it.Count; ++i)
        //{
        //    ref var ev = ref evA[i];

        //    if (
        //        record.Archetype.FindMatch(ev.Terms) == 0
        //        && ev.Terms.BinarySearch(eventInfo.LastComponent, it.World._comparer) >= 0
        //    )
        //    {
        //        ev.Callback(ref iterator);
        //    }
        //}
    }

    internal bool Has(EcsID entity, ref readonly EcsComponent cmp)
    {
        ref var record = ref GetRecord(entity);
        return record.Archetype.GetComponentIndex(in cmp) >= 0;
    } 

    public ReadOnlySpan<EcsComponent> GetType(EcsID id)
    {
        ref var record = ref GetRecord(id);
        return record.Archetype.ComponentInfo;
    }

    public void PrintGraph()
    {
        _archRoot.Print();
    }

    [SkipLocalsInit]
    public unsafe void RunPhase(EcsID phase)
    {
        Span<Term> terms = stackalloc Term[] { Term.With(Entity<EcsSystem>()), Term.With(phase), };

        Query(terms, RunSystems);
    }

    public void Step(float deltaTime = 0.0f)
    {
        DeltaTime = deltaTime;

        _commands.Merge();

        if (_frame == 0)
        {
            RunPhase(Component<EcsSystemPhasePreStartup>().ID);
            RunPhase(Component<EcsSystemPhaseOnStartup>().ID);
            RunPhase(Component<EcsSystemPhasePostStartup>().ID);
        }

        RunPhase(Component<EcsSystemPhasePreUpdate>().ID);
        RunPhase(Component<EcsSystemPhaseOnUpdate>().ID);
        RunPhase(Component<EcsSystemPhasePostUpdate>().ID);

        _commands.Merge();
        _frame += 1;

        if (_frame % 10 == 0)
        {
            Optimize();
        }
    }

    static unsafe void RunSystems(ref Iterator it)
    {
        var emptyIt = new Iterator(
            it.Commands,
            0,
            it.World._archRoot.Table,
            Span<EntityView>.Empty,
            Span<int>.Empty,
            null,
            Span<Array>.Empty,
            0
        );
        var sysA = it.Field<EcsSystem>(0);

        for (int i = 0; i < it.Count; ++i)
        {
            ref var sys = ref sysA[i];

            if (!float.IsNaN(sys.Tick))
            {
                // TODO: check for it.DeltaTime > 0?
                sys.TickCurrent += it.DeltaTime;

                if (sys.TickCurrent < sys.Tick)
                {
                    continue;
                }

                sys.TickCurrent = 0;
            }

            if (sys.Query != 0)
            {
                it.World.Query(sys.Terms, sys.Callback);
            }
            else
            {
                sys.Callback(ref emptyIt);
            }
        }
    }

    public unsafe void Query(
        Span<Term> terms,
		IteratorDelegate action,
        object? userData = null
    )
    {
		var arrayBuf = ArrayPool<Array>.Shared.Rent(terms.Length);
		Span<Array> columns = arrayBuf.AsSpan(0, terms.Length);
		Span<Term> sortedTerms = stackalloc Term[terms.Length];
        terms.CopyTo(sortedTerms);
        sortedTerms.Sort();

		try
		{
			QueryRec(_archRoot, terms, sortedTerms, _commands, action, userData, columns);
		}
		finally
		{
			ArrayPool<Array>.Shared.Return(arrayBuf);
		}

        static void QueryRec(
            Archetype root,
            Span<Term> terms,
            Span<Term> sortedTerms,
            Commands commands,
			IteratorDelegate action,
            object? userData,
            Span<Array> matchedColumns
        )
        {
            var result = root.FindMatch(sortedTerms);
            if (result < 0)
            {
                return;
            }

            if (result == 0 && root.Count > 0)
            {
                var matched = terms;
                for (int i = 0, j = 0; i < matched.Length; ++i)
                {
					if (matched[i].Op == TermOp.Without)
						continue;

                    var columnIdx = root.Table.GetComponentIndex(matched[i].ID);
                    if (columnIdx <= -1)
                        continue;

					matchedColumns[j++] = root.Table.RawComponentData(columnIdx);
                }

                var it = new Iterator(commands, root, userData, matchedColumns);
                action(ref it);
            }

            var span = CollectionsMarshal.AsSpan(root._edgesRight);
            if (span.IsEmpty)
            {
                return;
            }

            ref var start = ref MemoryMarshal.GetReference(span);
            ref var end = ref Unsafe.Add(ref start, span.Length);

            while (Unsafe.IsAddressLessThan(ref start, ref end))
            {
                QueryRec(
                    start.Archetype,
                    terms,
                    sortedTerms,
                    commands,
                    action,
                    userData,
                    matchedColumns
                );

                start = ref Unsafe.Add(ref start, 1);
            }
        }
    }
}

internal static class Lookup
{
	private static readonly Dictionary<ulong, Func<int, Array>> _arrayCreator = new Dictionary<ulong, Func<int, Array>>();

	public static Array? GetArray(ulong hashcode, int count)
	{
		var ok = _arrayCreator.TryGetValue(hashcode, out var fn);
		EcsAssert.Assert(ok, $"invalid hashcode {hashcode}");
		return fn?.Invoke(count) ?? null;
	}

	[SkipLocalsInit]
    internal static class Entity<T> where T : struct
	{
        public static unsafe readonly int Size = GetSize();
        public static readonly string Name = typeof(T).ToString();
		public static readonly int HashCode = typeof(T).GetHashCode();

		public static readonly EcsComponent Component = new EcsComponent((ulong) HashCode, Size, Name);

		static Entity()
		{
			_arrayCreator.Add(Component.ID, count => new T[count]);
		}

		private static unsafe int GetSize()
		{
			var size = Unsafe.SizeOf<T>();

			if (size != 1 || RuntimeHelpers.IsReferenceOrContainsReferences<T>())
				return size;

			// credit: BeanCheeseBurrito from Flecs.NET
			Unsafe.SkipInit<T>(out var t1);
			Unsafe.SkipInit<T>(out var t2);
			Unsafe.As<T, byte>(ref t1) = 0x7F;
			Unsafe.As<T, byte>(ref t2) = 0xFF;

			return ValueType.Equals(t1, t2) ? 0 : size;
		}
    }
}

struct EcsRecord
{
    public Archetype Archetype;
    public int Row;
}

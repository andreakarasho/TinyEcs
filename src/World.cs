namespace TinyEcs;

public sealed partial class World : IDisposable
{
	internal delegate Query QueryFactoryDel(World world, ReadOnlySpan<IQueryTerm> terms);

    private readonly Archetype _archRoot;
    private readonly EntitySparseSet<EcsRecord> _entities = new ();
    private readonly Dictionary<EcsID, Archetype> _typeIndex = new ();
    private readonly ComponentComparer _comparer;
	private readonly EcsID _maxCmpId;
	private readonly Dictionary<EcsID, Query> _cachedQueries = new ();
	private readonly FastIdLookup<EcsID> _cachedComponents = new ();
	private readonly Dictionary<string, EcsID> _names = new ();
	private readonly object _newEntLock = new ();

	private static readonly Comparison<ComponentInfo> _comparisonCmps = (a, b)
		=> ComponentComparer.CompareTerms(null!, a.ID, b.ID);
	private static readonly Comparison<EcsID> _comparisonIds = (a, b)
		=> ComponentComparer.CompareTerms(null!, a, b);



	internal Archetype Root => _archRoot;
	internal EcsID LastArchetypeId { get; set; }


	internal ref EcsRecord NewId(out EcsID newId, ulong id = 0)
	{
		ref var record = ref (
			id > 0 ?
			ref _entities.Add(id, default!)
			:
			ref _entities.CreateNew(out id)
		);

		newId = id;
		return ref record;
	}

	public void Each(QueryFilterDelegateWithEntity fn)
	{
		BeginDeferred();

		foreach (var arch in GetQuery(0, [], static (world, terms) => new Query(world, terms)))
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

	internal ref readonly ComponentInfo Component<T>() where T : struct
	{
        ref readonly var lookup = ref Lookup.Component<T>.Value;

		var isPair = lookup.ID.IsPair();
		EcsAssert.Panic(isPair || lookup.ID < _maxCmpId,
			"Increase the minimum number for components when initializing the world [ex: new World(1024)]");

		if (!isPair /*!Exists(lookup.ID)*/)
		{
			ref var idx = ref _cachedComponents.GetOrCreate(lookup.ID, out var exists);

			if (!exists)
			{
				idx = Entity(lookup.ID).Set(lookup).ID;
			}
		}

		// if (!isPair && !Exists(lookup.ID))
		// {
		// 	Entity(lookup.ID).Set(lookup);
		// }

		return ref lookup;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref EcsRecord GetRecord(EcsID id)
    {
        ref var record = ref _entities.Get(id);
		if (Unsafe.IsNullRef(ref record))
        	EcsAssert.Panic(false, $"entity {id} is dead or doesn't exist!");
        return ref record;
    }

	private void Detach(EcsID entity, EcsID id)
	{
		ref var record = ref GetRecord(entity);
		var oldArch = record.Archetype;

		if (oldArch.GetAnyIndex(id) < 0)
            return;

		OnComponentUnset?.Invoke(record.EntityView(), new ComponentInfo(id, -1, false));

		BeginDeferred();

		var foundArch = oldArch.TraverseLeft(id);
		if (foundArch == null && oldArch.All.Length - 1 <= 0)
		{
			foundArch = _archRoot;
		}

		if (foundArch == null)
		{
			var hash = new RollingHash();
			foreach (ref readonly var cmp in oldArch.All.AsSpan())
			{
				if (cmp.ID != id)
					hash.Add(cmp.ID);
			}

			if (!_typeIndex.TryGetValue(hash.Hash, out foundArch))
			{
				var arr = new ComponentInfo[oldArch.All.Length - 1];
				for (int i = 0, j = 0; i < oldArch.All.Length; ++i)
				{
					ref readonly var item = ref oldArch.All.ItemRef(i);
					if (item.ID != id)
						arr[j++] = item;
				}

				foundArch = NewArchetype(oldArch, arr, id);
			}
		}

		record.Chunk = record.Archetype.MoveEntity(foundArch!, ref record.Chunk, record.Row, true, out record.Row);
        record.Archetype = foundArch!;
		EndDeferred();


		if (id.IsPair())
		{
			(var first, var second) = id.Pair();
			(first, second) = (GetAlive(first), GetAlive(second));

			ref var firstRec = ref GetRecord(first);
			ref var secondRec = ref GetRecord(second);
			firstRec.Flags &= ~EntityFlags.IsAction;
			secondRec.Flags &= ~EntityFlags.IsTarget;

			if ((firstRec.Flags & EntityFlags.HasRules) != 0)
			{
				ExecuteRule(ref record, entity, ref firstRec, first, id, false);
			}
			else if ((secondRec.Flags & EntityFlags.HasRules) != 0)
			{
				ExecuteRule(ref record, entity, ref secondRec, second, id, false);
			}
		}
	}

	private (Array?, int) Attach(EcsID entity, EcsID id, int size, bool isManaged)
	{
		ref var record = ref GetRecord(entity);
		var oldArch = record.Archetype;

		var column = size > 0 ? oldArch.GetComponentIndex(id) : oldArch.GetAnyIndex(id);
		if (column >= 0)
            return (size > 0 ? record.Chunk.Data![column] : null, record.Row);

		BeginDeferred();

		var foundArch = oldArch.TraverseRight(id);
		if (foundArch == null)
		{
			var hash = new RollingHash();

			var found = false;
			foreach (ref readonly var cmp in oldArch.All.AsSpan())
			{
				if (!found && cmp.ID > id)
				{
					hash.Add(id);
					found = true;
				}

				hash.Add(cmp.ID);
			}

			if (!found)
				hash.Add(id);

			if (!_typeIndex.TryGetValue(hash.Hash, out foundArch))
			{
				var arr = new ComponentInfo[oldArch.All.Length + 1];
				oldArch.All.CopyTo(arr);
				arr[^1] = new ComponentInfo(id, size, isManaged);
				arr.AsSpan().SortNoAlloc(_comparisonCmps);

				foundArch = NewArchetype(oldArch, arr, id);
			}
		}

		record.Chunk = record.Archetype.MoveEntity(foundArch!, ref record.Chunk, record.Row, false, out record.Row);
        record.Archetype = foundArch!;
		EndDeferred();

		OnComponentSet?.Invoke(record.EntityView(), new ComponentInfo(id, size, isManaged));

		if (id.IsPair())
		{
			(var first, var second) = id.Pair();
			(first, second) = (GetAlive(first), GetAlive(second));

			ref var firstRec = ref GetRecord(first);
			ref var secondRec = ref GetRecord(second);
			firstRec.Flags |= EntityFlags.IsAction;
			secondRec.Flags |= EntityFlags.IsTarget;

			if ((firstRec.Flags & EntityFlags.HasRules) != 0)
			{
				ExecuteRule(ref record, entity, ref firstRec, first, id, true);
			}
			else if ((secondRec.Flags & EntityFlags.HasRules) != 0)
			{
				ExecuteRule(ref record, entity, ref secondRec, second, id, true);
			}
		}

		column = size > 0 ? foundArch.GetComponentIndex(id) : foundArch.GetAnyIndex(id);
		return (size > 0 ? record.Chunk.Data![column] : null, record.Row);
	}

	internal bool IsAttached(ref EcsRecord record, EcsID id)
	{
		if (record.Archetype.HasIndex(id))
			return true;

		if (id.IsPair())
		{
			(var a, var b) = FindPair(ref record, id.First(), id.Second());

			return a.IsValid() && b.IsValid();
		}

		return id == Defaults.Wildcard.ID;
	}

	private void ExecuteRule(ref EcsRecord entityRecord, EcsID entity, ref EcsRecord ruleRecord, EcsID ruleId, EcsID id, bool onSet)
	{
		var i = 0;
		EcsID target;
		while ((target = FindPairFromFirst(ref ruleRecord, Defaults.Rule.ID, i++).second).IsValid())
		{
			if (target == Defaults.Symmetric.ID)
			{
				(var first, var second) = id.Pair();

				var has = Has(second, first, entity);
				if (!onSet)
					has = !has;

				if (!has)
				{
					if (onSet)
						Add(second, first, entity);
					else
						Unset(second, first, entity);
				}
			}
			else if (target == Defaults.Unique.ID)
			{
				if (onSet)
				{
					(var first, var second) = id.Pair();
					var idx = 0;
					EcsID targetId;
					while ((targetId = FindPairFromFirst(ref entityRecord, first, idx++).second).IsValid())
					{
						if (targetId != second)
						{
							Unset(entity, first, targetId);
						}
					}
				}
			}
			else if (target == Defaults.Unset.ID)
			{
				if (!onSet)
				{
					if (ruleId == Defaults.Identifier.ID)
					{
						var second = GetAlive(id.Second());

						if (second == Defaults.Name.ID)
							GetRecord(entity).Flags &= ~EntityFlags.HasName;
					}
				}
			}
		}
	}

	private Archetype NewArchetype(Archetype oldArch, ComponentInfo[] sign, EcsID id)
	{
		var archetype = _archRoot.InsertVertex(oldArch, sign, id);
		_typeIndex.Add(archetype.Id, archetype);
		LastArchetypeId = archetype.Id;
		return archetype;
	}

	internal ref T GetUntrusted<T>(EcsID entity, EcsID id, int size) where T : struct
	{
		if (IsDeferred && !Has(entity, id))
		{
			Unsafe.SkipInit<T>(out var val);
			var isManaged = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
			return ref Unsafe.Unbox<T>(SetDeferred(entity, id, val, size, isManaged)!);
		}

        ref var record = ref GetRecord(entity);
		var column = record.Archetype.GetComponentIndex(id);
		if (column < 0)
			return ref Unsafe.NullRef<T>();

		return ref Unsafe.As<T[]>(record.Chunk.Data![column])[record.Row & TinyEcs.Archetype.CHUNK_THRESHOLD];
    }

	internal Query GetQuery(EcsID hash, ReadOnlySpan<IQueryTerm> terms, QueryFactoryDel factory)
	{
		if (!_cachedQueries.TryGetValue(hash, out var query))
		{
			query = factory(this, terms);
			_cachedQueries.Add(hash, query);
		}

		query.Match();

		return query;
	}
}

struct EcsRecord
{
	public Archetype Archetype;
    public int Row;
	public EntityFlags Flags;
	public ArchetypeChunk Chunk;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref readonly EntityView EntityView() => ref Chunk.EntityAt(Row & TinyEcs.Archetype.CHUNK_THRESHOLD);
}

[Flags]
enum EntityFlags
{
	None = 1 << 0,
	IsAction = 1 << 1,
	IsTarget = 1 << 2,
	IsUnique = 1 << 3,
	IsSymmetric = 1 << 4,
	HasName = 1 << 5,

	HasRules = 1 << 6,
}

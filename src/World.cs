namespace TinyEcs;

public sealed partial class World : IDisposable
{
	internal delegate Query QueryFactoryDel(World world, ReadOnlySpan<IQueryTerm> terms);

	private readonly Archetype _archRoot;
	private readonly EntitySparseSet<EcsRecord> _entities = new();
	private readonly Dictionary<EcsID, Archetype> _typeIndex = new();
	private readonly ComponentComparer _comparer;
	private readonly EcsID _maxCmpId;
	private readonly FastIdLookup<EcsID> _cachedComponents = new();
	private readonly object _newEntLock = new();
	private uint _ticks;

	private static readonly Comparison<ComponentInfo> _comparisonCmps = (a, b)
		=> ComponentComparer.CompareTerms(null!, a.ID, b.ID);
	private static readonly Comparison<EcsID> _comparisonIds = (a, b)
		=> ComponentComparer.CompareTerms(null!, a, b);
	private static readonly Comparison<IQueryTerm> _comparisonTerms = (a, b)
		=> a.CompareTo(b);



	internal Archetype Root => _archRoot;
	internal EcsID LastArchetypeId { get; set; }
	internal RelationshipEntityMapper RelationshipEntityMapper { get; }
	internal NamingEntityMapper NamingEntityMapper { get; }

	/// <summary>
	/// Maximum component ID. Component entities have IDs from 1 to this value.
	/// Regular entities have IDs starting from MaxComponentId + 1.
	/// </summary>
	public EcsID MaxComponentId => _maxCmpId;

	public uint Update() => ++_ticks;

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

	internal ref readonly ComponentInfo Component<T>() where T : struct
	{
		ref readonly var lookup = ref Lookup.Component<T>.Value;

		EcsAssert.Panic(lookup.ID < _maxCmpId,
			"Increase the minimum number for components when initializing the world [ex: new World(1024)]");

		ref var idx = ref _cachedComponents.GetOrCreate(lookup.ID, out var exists);
		if (!exists)
		{
			idx = Entity(lookup.ID).Set(lookup).ID;

			NamingEntityMapper.SetName(idx, Lookup.Component<T>.Name);

#if USE_PAIR
			if (!lookup.ID.IsPair())
			{
				ref var record = ref GetRecord(lookup.ID);

				if ((record.Flags & EntityFlags.HasName) == 0)
				{
					record.Flags |= EntityFlags.HasName;
					var name = Lookup.Component<T>.Name;
					_names[name] = lookup.ID;
					Set<Identifier>(lookup.ID, new (name), Defaults.Name.ID);
				}
			}
#endif
		}

		return ref lookup;
	}


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

		OnComponentUnset?.Invoke(this, entity, new ComponentInfo(id, -1));

		BeginDeferred();

		var foundArch = oldArch.TraverseLeft(id);
		if (foundArch == null && oldArch.All.Length - 1 <= 0)
		{
			foundArch = _archRoot;
		}

		if (foundArch == null)
		{
			var hash = 0ul;
			foreach (ref readonly var cmp in oldArch.All.AsSpan())
			{
				if (cmp.ID != id)
					hash = UnorderedSetHasher.Combine(hash, cmp.ID);
			}

			if (!_typeIndex.TryGetValue(hash, out foundArch))
			{
				var arr = new ComponentInfo[oldArch.All.Length - 1];
				for (int i = 0, j = 0; i < oldArch.All.Length; ++i)
				{
					ref readonly var item = ref oldArch.All[i];
					if (item.ID != id)
						arr[j++] = item;
				}

				foundArch = NewArchetype(oldArch, arr, id);
			}
		}

		record.Chunk = record.Archetype.MoveEntity(foundArch!, ref record.Chunk, record.Row, true, out record.Row);
		record.Archetype = foundArch!;
		EndDeferred();

#if USE_PAIR
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
#endif
	}

	private (Array?, int) Attach(EcsID entity, EcsID id, int size)
	{
		ref var record = ref GetRecord(entity);
		var oldArch = record.Archetype;

		var column = size > 0 ? oldArch.GetComponentIndex(id) : oldArch.GetAnyIndex(id);
		if (column >= 0)
		{
			if (size > 0)
			{
				record.Chunk.MarkChanged(column, record.Row, _ticks);
			}
			return (size > 0 ? record.Chunk.Columns![column].Data : null, record.Row);
		}

		BeginDeferred();

		var foundArch = oldArch.TraverseRight(id);
		if (foundArch == null)
		{
			var hash = 0ul;

			var found = false;
			foreach (ref readonly var cmp in oldArch.All.AsSpan())
			{
				if (!found && cmp.ID > id)
				{
					hash = UnorderedSetHasher.Combine(hash, id);
					found = true;
				}

				hash = UnorderedSetHasher.Combine(hash, cmp.ID);
			}

			if (!found)
				hash = UnorderedSetHasher.Combine(hash, id);

			if (!_typeIndex.TryGetValue(hash, out foundArch))
			{
				var arr = new ComponentInfo[oldArch.All.Length + 1];
				oldArch.All.CopyTo(arr, 0);
				arr[^1] = new ComponentInfo(id, size);
				arr.AsSpan().SortNoAlloc(_comparisonCmps);

				foundArch = NewArchetype(oldArch, arr, id);
			}
		}

		record.Chunk = record.Archetype.MoveEntity(foundArch!, ref record.Chunk, record.Row, false, out record.Row);
		record.Archetype = foundArch!;
		EndDeferred();

		OnComponentSet?.Invoke(this, entity, new ComponentInfo(id, size));

#if USE_PAIR
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
#endif

		column = size > 0 ? foundArch.GetComponentIndex(id) : foundArch.GetAnyIndex(id);
		if (size > 0)
		{
			record.Chunk.MarkAdded(column, record.Row, _ticks);
		}
		return (size > 0 ? record.Chunk.Columns![column].Data : null, record.Row);
	}

	internal bool IsAttached(ref EcsRecord record, EcsID id)
	{
		if (record.Archetype.HasIndex(id))
			return true;

#if USE_PAIR
		if (id.IsPair())
		{
			(var a, var b) = FindPair(ref record, id.First(), id.Second());

			return a.IsValid() && b.IsValid();
		}
#endif

		return id == Defaults.Wildcard.ID;
	}

#if USE_PAIR
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
#endif

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
			return ref Unsafe.Unbox<T>(SetDeferred(entity, id, val, size)!);
		}

		ref var record = ref GetRecord(entity);
		var column = record.Archetype.GetComponentIndex(id);
		return ref record.Chunk.GetReferenceAt<T>(column, record.Row);
	}
}

struct EcsRecord
{
	public Archetype Archetype;
	public int Row;
#if USE_PAIR
	public EntityFlags Flags;
#endif
	public ArchetypeChunk Chunk;
}

#if USE_PAIR
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
#endif

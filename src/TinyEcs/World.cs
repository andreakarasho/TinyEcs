using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

public sealed partial class World : IDisposable
{
	internal delegate Query QueryFactoryDel(World world, ReadOnlySpan<IQueryTerm> terms);

	private readonly Archetype _archRoot;
	private readonly EntitySparseSet<EcsRecord> _entities = new();
	private readonly Dictionary<EcsID, Archetype> _typeIndex = new();
	private readonly ComponentComparer _comparer;
	private readonly EcsID _maxCmpId;
	private readonly int _componentBitsetWords;
	private readonly FastIdLookup<EcsID> _cachedComponents = new();
	private readonly object _newEntLock = new();

	// Per-world component registry. Component types share a global dense "slot"
	// (Lookup.Component<T>.HashCode), but each world assigns its own EcsID to a
	// type on first use, so component ids are isolated per world.
	private ComponentInfo[] _slotComponents = new ComponentInfo[64];
	private bool[] _slotRegistered = new bool[64];
	private readonly Dictionary<EcsID, int> _idToSlot = new();
	private ulong _componentCounter;
	private uint _ticks;
	private ulong _structuralChangeVersion;

	private static readonly Comparison<ComponentInfo> _comparisonCmps = (a, b)
		=> ComponentComparer.CompareTerms(null!, a.ID, b.ID);
	private static readonly Comparison<EcsID> _comparisonIds = (a, b)
		=> ComponentComparer.CompareTerms(null!, a, b);
	private static readonly Comparison<IQueryTerm> _comparisonTerms = (a, b)
		=> a.CompareTo(b);



	internal Archetype Root => _archRoot;
	internal EcsID LastArchetypeId { get; set; }
	internal ulong StructuralChangeVersion => _structuralChangeVersion;
	internal int ComponentBitsetWords => _componentBitsetWords;
	internal RelationshipEntityMapper RelationshipEntityMapper { get; }
	internal NamingEntityMapper NamingEntityMapper { get; }

	/// <summary>
	/// Maximum component ID. Component entities have IDs from 1 to this value.
	/// Regular entities have IDs starting from MaxComponentId + 1.
	/// </summary>
	public EcsID MaxComponentId => _maxCmpId;

	/// <summary>
	/// Current tick value used for change detection.
	/// </summary>
	public uint CurrentTick => _ticks;

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
		// Global dense slot for this component type (stable for the process).
		var slot = (int)Lookup.Component<T>.HashCode;
		if (slot >= _slotComponents.Length)
			GrowSlots(slot);

		if (!_slotRegistered[slot])
		{
			var id = ++_componentCounter;
			EcsAssert.Panic(id < _maxCmpId,
				"Increase the minimum number for components when initializing the world [ex: new World(1024)]");

			_slotComponents[slot] = new ComponentInfo(id, Lookup.Component<T>.Size);
			_slotRegistered[slot] = true;
			_idToSlot[id] = slot;
		}

		return ref _slotComponents[slot];
	}

	private void GrowSlots(int slot)
	{
		var newLen = _slotComponents.Length;
		while (newLen <= slot) newLen *= 2;
		Array.Resize(ref _slotComponents, newLen);
		Array.Resize(ref _slotRegistered, newLen);
	}

	// Resolve a per-world component id back to its global slot, used to reach the
	// world-agnostic column/array factories registered in Lookup.
	internal Column CreateColumn(EcsID id, int count)
		=> Lookup.CreateColumn((ulong)_idToSlot[id], count);

	internal ref readonly ComponentInfo GetComponentInfo(EcsID id)
		=> ref _slotComponents[_idToSlot[id]];


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
			var hash = oldArch.ComputeHashWithout(id);

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

		record.Row = record.Archetype.MoveEntity(foundArch!, record.Row, true);
		record.Archetype = foundArch!;
		_structuralChangeVersion++;
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

	private (Column?, int) Attach(EcsID entity, EcsID id, int size)
	{
		ref var record = ref GetRecord(entity);
		var oldArch = record.Archetype;

		var column = size > 0 ? oldArch.GetComponentIndex(id) : oldArch.GetAnyIndex(id);
		if (column >= 0)
		{
			// Component already exists - this is an update, not an add
			OnComponentSet?.Invoke(this, entity, new ComponentInfo(id, size));

			if (size > 0)
			{
				record.Archetype.MarkChanged(column, record.Row, _ticks);
			}
			return (size > 0 ? record.Archetype.Columns![column] : null, record.Row);
		}

		// Component doesn't exist - this is a new addition
		BeginDeferred();

		var foundArch = oldArch.TraverseRight(id);
		if (foundArch == null)
		{
			// Compute hash first to check cache before allocating
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
				// Only allocate array if archetype doesn't exist
				var arr = new ComponentInfo[oldArch.All.Length + 1];
				oldArch.All.CopyTo(arr, 0);
				arr[^1] = new ComponentInfo(id, size);
				arr.AsSpan().SortNoAlloc(_comparisonCmps);

				foundArch = NewArchetype(oldArch, arr, id);
			}
		}

		record.Row = record.Archetype.MoveEntity(foundArch!, record.Row, false);
		record.Archetype = foundArch!;
		_structuralChangeVersion++;
		EndDeferred();

		// Fire both OnComponentAdded (first time) and OnComponentSet (all times)
		OnComponentAdded?.Invoke(this, entity, new ComponentInfo(id, size));
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
			record.Archetype.MarkAdded(column, record.Row, _ticks);
			record.Archetype.MarkChanged(column, record.Row, _ticks);
		}
		return (size > 0 ? record.Archetype.Columns![column] : null, record.Row);
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

		return id == Defaults.Wildcard.ID;
#else
		return false;
#endif
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

	internal bool TryGetArchetype(EcsID id, out Archetype? archetype)
	{
		return _typeIndex.TryGetValue(id, out archetype);
	}

	internal ref T GetUntrusted<T>(EcsID entity, EcsID id, int size) where T : struct
	{
		// Check deferred cache first if in deferred mode
		if (IsDeferred && _deferredComponentCache.TryGetValue((entity, id), out var cached))
		{
			return ref Unsafe.Unbox<T>(cached);
		}

		ref var record = ref GetRecord(entity);
		var column = record.Archetype.GetComponentIndex(id);

		if (column < 0)
		{
			EcsAssert.Panic(false, $"Component {id} not found on entity {entity}");
		}

		return ref record.Archetype.GetReferenceAt<T>(column, record.Row);
	}
}

struct EcsRecord
{
	public Archetype Archetype;
	public int Row;
#if USE_PAIR
	public EntityFlags Flags;
#endif
}

#if USE_PAIR
[Flags]
enum EntityFlags
{
	None = 0,
	IsAction = 1 << 0,
	IsTarget = 1 << 1,
	IsUnique = 1 << 2,
	IsSymmetric = 1 << 3,
	HasName = 1 << 4,

	HasRules = 1 << 5,
}
#endif

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
	private readonly object _componentLock = new();

	// Per-world component registry. Component types share a global dense "slot"
	// (Lookup.Component<T>.HashCode), but each world assigns its own EcsID to a
	// type on first use, so component ids are isolated per world.
	private ComponentInfo[] _slotComponents = new ComponentInfo[64];
	private bool[] _slotRegistered = new bool[64];
	private readonly Dictionary<EcsID, int> _idToSlot = new();
	private ulong _componentCounter;
	private uint _ticks;
	private uint _frameTick;
	private uint _prevFrameTick;
	private uint _lastCheckTick;
	private ulong _frameCount;
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
	/// Current tick value used for change detection. Advances once per SYSTEM
	/// RUN (plus once per deferred-command flush and once per observer flush),
	/// so it is NOT a frame counter — see <see cref="FrameCount"/> for that.
	/// </summary>
	/// <remarks>
	/// Plain read, deliberately: this sits on the query-iteration hot path and
	/// aligned 32-bit reads are already atomic in .NET. Nothing spins on it —
	/// the scheduler takes each system's tick from the Interlocked.Increment
	/// return value, never from a re-read of this property.
	/// </remarks>
	public uint CurrentTick => _ticks;

	/// <summary>
	/// <see cref="CurrentTick"/> as it stood when the current frame started.
	/// Everything written during this frame carries a tick strictly greater
	/// than this.
	/// </summary>
	public uint FrameTick => _frameTick;

	/// <summary><see cref="FrameTick"/> of the previous frame.</summary>
	public uint PreviousFrameTick => _prevFrameTick;

	/// <summary>
	/// Number of completed <see cref="Update"/> calls (frames). Monotonic and
	/// frame-granular — this is the counter to export to scripting/modding
	/// guests, never <see cref="CurrentTick"/>.
	/// </summary>
	public ulong FrameCount => _frameCount;

	/// <summary>
	/// Opens a new frame: records the frame tick boundary and advances the
	/// change tick once. Per-system advancing happens in the scheduler.
	/// </summary>
	public uint Update()
	{
		_prevFrameTick = _frameTick;
		_frameCount++;
		_frameTick = BumpTick();
		return _frameTick;
	}

	/// <summary>
	/// Advance the change tick by one and return the new value. Atomic: stages
	/// run systems in parallel, and each run must get its own tick.
	/// </summary>
	public uint BumpTick() => Interlocked.Increment(ref _ticks);

	/// <summary>
	/// Clamp every stored change/added tick (and report whether callers should
	/// clamp their own cached <c>lastRun</c>) once the world tick has advanced
	/// <see cref="ChangeTick.CheckTickThreshold"/> since the previous pass.
	/// Bevy's <c>check_change_ticks</c>: without it a tick left untouched for
	/// ~2^31 ticks wraps around into "newer than now" and change detection
	/// starts firing spuriously forever.
	/// </summary>
	/// <returns>
	/// <c>true</c> when a pass ran, meaning cached per-system <c>lastRun</c>
	/// values must be clamped with <see cref="ChangeTick.ClampAge"/> too.
	/// </returns>
	public bool CheckChangeTicks()
	{
		var now = _ticks;
		if (ChangeTick.Age(_lastCheckTick, now) < ChangeTick.CheckTickThreshold)
			return false;

		_lastCheckTick = now;

		foreach (var archetype in _typeIndex.Values)
		{
			var columns = archetype.Columns;
			if (columns == null)
				continue;

			var count = archetype.Count;
			if (count <= 0)
				continue;

			foreach (var column in columns)
			{
				// Only live rows: a free slot holds a stale 0 that a clamp would
				// turn into "recently changed" for whichever entity moves in.
				var changed = column.ChangedTicks.AsSpan(0, Math.Min(count, column.ChangedTicks.Length));
				for (var i = 0; i < changed.Length; i++)
					changed[i] = ChangeTick.ClampAge(changed[i], now);

				var added = column.AddedTicks.AsSpan(0, Math.Min(count, column.AddedTicks.Length));
				for (var i = 0; i < added.Length; i++)
					added[i] = ChangeTick.ClampAge(added[i], now);
			}
		}

		return true;
	}

	/// <summary>
	/// Test hook: park the change tick near a chosen value so a test can drive
	/// the counter across the 32-bit wrap in a handful of frames.
	/// </summary>
	internal void SetTicksForTesting(uint ticks)
	{
		_ticks = ticks;
		_frameTick = ticks;
		_prevFrameTick = ticks;
		// _lastCheckTick is deliberately NOT moved: the jump stands in for a long
		// real run, so the next CheckChangeTicks must come due exactly as it
		// would have after that run.
	}

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

		// First-use registration mutates the slot arrays (GrowSlots resizes
		// them). Parallel system scheduling can have several systems first-touch
		// new component types via their query builds on the same frame; without
		// serialization two GrowSlots race and one thread indexes the stale
		// (smaller) array -> IndexOutOfRangeException.
		//
		// Fast path: snapshot the registry array into a local so the bounds check
		// and the index read use the SAME instance — a concurrent resize can swap
		// the field but can't make this snapshot inconsistent. Slow path locks the
		// actual mutation. The final element read is lock-free but safe: once a
		// slot is registered every present/future array is >= that size and the
		// old array stays alive (GC), so [slot] is always in-bounds.
		var reg = _slotRegistered;
		if (slot < reg.Length && reg[slot])
			return ref _slotComponents[slot];

		lock (_componentLock)
		{
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
				// Stamped with the GLOBAL tick, not the calling system's run
				// tick: the two differ when a sibling system of the same
				// parallel batch bumped the counter in between, so a system can
				// see its OWN direct (non-deferred) write as "changed" on its
				// next run. Accepted — same trade Bevy makes for
				// `&mut World` access; route through Commands (deferred, flushed
				// with one tick for the whole batch) when that matters.
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

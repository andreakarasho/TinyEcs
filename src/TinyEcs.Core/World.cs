namespace TinyEcs;

public sealed partial class World : IDisposable
{
	private readonly Archetype _archRoot;
	private readonly EntitySparseSet<EcsRecord> _entities = new();
	private readonly Dictionary<EntityID, Archetype> _typeIndex = new ();
	private readonly Dictionary<EntityID, Table> _tableIndex = new ();
	private readonly Dictionary<nint, EcsComponent> _components = new();
	private readonly ComponentComparer _comparer;
	private readonly Commands _commands;
	private int _frame;

	public World()
	{
		_comparer = new ComponentComparer(this);
		_archRoot = new Archetype(this, new (0, ReadOnlySpan<EcsComponent>.Empty, _comparer), ReadOnlySpan<EcsComponent>.Empty, _comparer);
		_commands = new (this);

		RegisterDefaults();
	}


	public int EntityCount => _entities.Length;

	public float DeltaTime { get; private set; }



	public void Dispose()
	{
		_entities.Clear();
		_archRoot.Clear();
		_typeIndex.Clear();
		_components.Clear();
		_tableIndex.Clear();
		_commands.Clear();
	}

	private void RegisterDefaults()
	{
		// _ = ref Tag<EcsExclusive>();
		// _ = ref Tag<EcsTag>();
		// _ = ref Tag<EcsAny>();
		// _ = ref Tag<EcsPanic>();
		// _ = ref Tag<EcsDelete>();
		// _ = ref Tag<EcsChildOf>();
		// _ = ref Tag<EcsEnabled>();
		// _ = ref Tag<EcsPhase>();

		// _ = ref Tag<EcsSystemPhasePreStartup>();
		// _ = ref Tag<EcsSystemPhaseOnStartup>();
		// _ = ref Tag<EcsSystemPhasePostStartup>();
		// _ = ref Tag<EcsSystemPhasePreUpdate>();
		// _ = ref Tag<EcsSystemPhaseOnUpdate>();
		// _ = ref Tag<EcsSystemPhasePostUpdate>();

		SetTag<EcsExclusive>(Tag<EcsChildOf>().ID);
		SetTag<EcsExclusive>(Tag<EcsPhase>().ID);
	}

	// public void Optimize()
	// {
	// 	InternalOptimize(_archRoot);

	// 	static void InternalOptimize(Archetype root)
	// 	{
	// 		root.Optimize();

	// 		foreach (ref var edge in CollectionsMarshal.AsSpan(root._edgesRight))
	// 		{
	// 			InternalOptimize(edge.Archetype);
	// 		}
	// 	}
	// }

	public unsafe ref EcsComponent Tag<T>() where T : unmanaged, IComponentStub
	{
		ref var cmp = ref Component<T>();
		EcsAssert.Assert(cmp.Size <= 0);
		return ref cmp;
	}

	public unsafe ref EcsComponent Component<T>() where T : unmanaged, IComponentStub
	{
		ref var cmp = ref CollectionsMarshal.GetValueRefOrAddDefault(_components, typeof(T).TypeHandle.Value, out var exists);
		if (!exists)
		{
			var ent = SpawnEmpty();
			Unsafe.SkipInit<T>(out var obj);
			var size = obj is ITag ? 0 : sizeof(T);
			cmp = new EcsComponent(ent.ID, size);
			Set(cmp.ID, cmp);
			SetTag<EcsEnabled>(cmp.ID);
			SetPair<EcsPanic, EcsDelete>(cmp.ID);

			if (size == 0)
			{
				SetTag<EcsTag>(cmp.ID);
			}
		}

		return ref cmp;
	}

	public EntityID Pair<TKind, TTarget>()
	where TKind : unmanaged, IComponentStub
	where TTarget : unmanaged, IComponentStub
		=> IDOp.Pair(Tag<TKind>().ID, Tag<TTarget>().ID);

	public EntityID Pair<TKind>(EntityID target)
	where TKind : unmanaged, IComponentStub
		=> IDOp.Pair(Tag<TKind>().ID, target);

	public QueryBuilder Query()
		=> new (this);

	public unsafe EntityView System(delegate*<ref Iterator, void> system, EntityID query, ReadOnlySpan<Term> terms, float tick)
		=> Spawn()
			.Set(new EcsSystem(system, query, terms, tick));

	public EntityView Entity(EntityID id)
	{
		EcsAssert.Assert(Exists(id));
		return new(this, id);
	}

	public EntityView Spawn()
		=> SpawnEmpty().SetTag<EcsEnabled>();

	internal EntityView SpawnEmpty(EntityID id = 0)
	{
		ref var record = ref (id > 0 ? ref _entities.Add(id, default!) : ref _entities.CreateNew(out id));
		record.Archetype = _archRoot;
		record.Row = _archRoot.Add(id).Item1;

		return new EntityView(this, id);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ref EcsRecord GetRecord(EntityID id)
	{
		ref var record = ref _entities.Get(id);
		EcsAssert.Assert(!Unsafe.IsNullRef(ref record));
		return ref record;
	}

	public void Despawn(EntityID entity)
	{
		ref var record = ref GetRecord(entity);

		EcsAssert.Panic(!Has<EcsPanic, EcsDelete>(entity), $"You cannot delete entity {entity}");

		//if (GetParent(entity) != 0)
		{
			Query()
				.With<EcsChildOf>(entity)
				.Iterate(static (ref Iterator it) => {
					for (int i = it.Count - 1; i >= 0; i--)
					{
						it.Entity(i).Despawn();
					}
				});
		}

		var removedId = record.Archetype.Remove(ref record);
		EcsAssert.Assert(removedId == entity);

		var last = record.Archetype.Entities[record.Row];
		_entities.Get(last) = record;
		_entities.Remove(removedId);
	}

	public bool Exists(EntityID entity)
		=> _entities.Contains(entity);

	internal void DetachComponent(EntityID entity, ref EcsComponent cmp)
	{
		ref var record = ref GetRecord(entity);
		InternalAttachDetach(ref record, ref cmp, false);
	}

	private bool InternalAttachDetach(ref EcsRecord record, ref EcsComponent cmp, bool add)
	{
		EcsAssert.Assert(!Unsafe.IsNullRef(ref record));

		var arch = CreateArchetype(record.Archetype, ref cmp, add);
		if (arch == null)
			return false;

		record.Row = record.Archetype.MoveEntity(arch, record.Row);
		record.Archetype = arch!;

		return true;
	}

	[SkipLocalsInit]
	internal Archetype? CreateArchetype(Archetype root, ref EcsComponent cmp, bool add)
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

		if (!add && root.GetComponentIndex(ref cmp) < 0)
			return null;

		var initType = root.ComponentInfo;
		var cmpCount = Math.Max(0, initType.Length + (add ? 1 : -1));

		const int STACKALLOC_SIZE = 16;

		EcsComponent[]? buffer = null;
		Span<EcsComponent> span = cmpCount <= STACKALLOC_SIZE ?
		 stackalloc EcsComponent[STACKALLOC_SIZE] :
		 (buffer = ArrayPool<EcsComponent>.Shared.Rent(cmpCount));

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

		ref var arch = ref CollectionsMarshal.GetValueRefOrAddDefault(_typeIndex, hash, out var exists);
		if (!exists)
		{
			ref var table = ref Unsafe.NullRef<Table>();

			if (cmp.Size != 0)
			{
				var tableHash = Hash(span, true);
				table = ref CollectionsMarshal.GetValueRefOrAddDefault(_tableIndex, tableHash, out exists)!;
				if (!exists)
				{
					table = new Table(tableHash, span, _comparer);
				}
			}
			else
			{
				table = ref Unsafe.AsRef(root.Table)!;
			}

			arch = _archRoot.InsertVertex(root, table, span, ref cmp);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static EntityID Hash(UnsafeSpan<EcsComponent> components, bool checkSize)
		{
			unchecked
			{
				EntityID hash = 5381;

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

	internal void Set(EntityID entity, ref EcsComponent cmp, ReadOnlySpan<byte> data)
	{
		EcsAssert.Assert(cmp.Size == data.Length);

		ref var record = ref GetRecord(entity);

		var column = record.Archetype.GetComponentIndex(ref cmp);
		if (column < 0)
		{
			InternalAttachDetach(ref record, ref cmp, true);
		}

		if (cmp.Size <= 0)
			return;

		column = record.Archetype.GetComponentIndex(ref cmp);
		var buf = record.Archetype.Table.ComponentData<byte>
		(
			column,
			record.Archetype.EntitiesTableRows[record.Row] * cmp.Size,
			cmp.Size
		);

		EcsAssert.Assert(data.Length == buf.Length);
		data.CopyTo(buf);
	}

	internal bool Has(EntityID entity, ref EcsComponent cmp)
	{
		ref var record = ref GetRecord(entity);
		return record.Archetype.GetComponentIndex(ref cmp) >= 0;
	}

	public void SetPair(EntityID entity, EntityID first, EntityID second)
	{
		var id = IDOp.Pair(first, second);
		if (Exists(id) && Has<EcsComponent>(id))
		{
			ref var cmp2 = ref Get<EcsComponent>(id);
			Set(entity, ref cmp2, ReadOnlySpan<byte>.Empty);
			return;
		}

		if (Has<EcsExclusive>(first))
		{
			ref var record = ref GetRecord(entity);
			var id2 = IDOp.Pair(first, Tag<EcsAny>().ID);
			var cmp3 = new EcsComponent(id2, 0);
			var column = record.Archetype.GetComponentIndex(ref cmp3);

			if (column >= 0)
			{
				DetachComponent(entity, ref record.Archetype.ComponentInfo[column]);
			}
		}

		var cmp = new EcsComponent(id, 0);
		Set(entity, ref cmp, ReadOnlySpan<byte>.Empty);
	}

	public void SetTag(EntityID entity, EntityID tag)
	{
		if (Exists(tag) && Has<EcsComponent>(tag))
		{
			ref var cmp2 = ref Get<EcsComponent>(tag);
			Set(entity, ref cmp2, ReadOnlySpan<byte>.Empty);

			return;
		}

		var cmp = new EcsComponent(tag, 0);
		Set(entity, ref cmp, ReadOnlySpan<byte>.Empty);
	}

	public bool Has(EntityID entity, EntityID first, EntityID second)
	{
		var id = IDOp.Pair(first, second);
		if (Exists(id) && Has<EcsComponent>(id))
		{
			ref var cmp2 = ref Get<EcsComponent>(id);
			return Has(entity, ref cmp2);
		}

		var cmp = new EcsComponent(id, 0);
		return Has(entity, ref cmp);
	}

	public EntityID GetParent(EntityID id)
	{
		ref var record = ref GetRecord(id);

		var pair = Pair<EcsChildOf, EcsAny>();
		var cmp = new EcsComponent(pair, 0);
		var column = record.Archetype.GetComponentIndex(ref cmp);

		if (column >= 0)
		{
			ref var meta = ref record.Archetype.ComponentInfo[column];

			return IDOp.GetPairSecond(meta.ID);
		}

		return 0;
	}

	public ReadOnlySpan<EcsComponent> GetType(EntityID id)
	{
		ref var record = ref GetRecord(id);
		return record.Archetype.ComponentInfo;
	}

	public void PrintGraph()
	{
		_archRoot.Print();
	}

	[SkipLocalsInit]
	public unsafe void RunPhase(EntityID phase)
	{
		Span<Term> terms = stackalloc Term[] {
			new () { ID = Tag<EcsEnabled>().ID, Op = TermOp.With },
			new () { ID = Component<EcsSystem>().ID, Op = TermOp.With },
			new () { ID = phase, Op = TermOp.With}
		};

		Query(terms, &RunSystems);
	}

	public void Step(float deltaTime = 0.0f)
	{
		DeltaTime = deltaTime;

		_commands.Merge();

		if (_frame == 0)
		{
			RunPhase(Pair<EcsPhase, EcsSystemPhasePreStartup>());
			RunPhase(Pair<EcsPhase, EcsSystemPhaseOnStartup>());
			RunPhase(Pair<EcsPhase, EcsSystemPhasePostStartup>());
		}

		RunPhase(Pair<EcsPhase, EcsSystemPhasePreUpdate>());
		RunPhase(Pair<EcsPhase, EcsSystemPhaseOnUpdate>());
		RunPhase(Pair<EcsPhase, EcsSystemPhasePostUpdate>());

		_commands.Merge();
		_frame += 1;
	}

	static unsafe void RunSystems(ref Iterator it)
	{
		var sysA = it.Field<EcsSystem>();

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
				it.World.Query(sys.Terms, sys.Func);
			}
			else
			{
				sys.Func(ref it);
			}
		}
	}

	public unsafe void Query
	(
		Span<Term> terms,
		delegate* <ref Iterator, void> action,
		object? userData = null
	)
	{
		terms.Sort(static (a, b) => a.ID.CompareTo(b.ID));

		QueryRec(_archRoot, terms, _commands, action, userData);

		static void QueryRec
		(
			Archetype root,
			UnsafeSpan<Term> terms,
			Commands? commands,
			delegate* <ref Iterator, void> action,
			object? userData
		)
		{
			var result = root.FindMatch(terms);
			if (result < 0)
			{
				return;
			}

			if (result == 0 && root.Count > 0)
			{
				var it = new Iterator(commands, root, userData);
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
				QueryRec(start.Archetype, terms, commands, action, userData);

				start = ref Unsafe.Add(ref start, 1);
			}
		}
	}
}


struct EcsRecord
{
	public Archetype Archetype;
	public int Row;
}

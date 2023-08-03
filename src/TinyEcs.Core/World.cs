namespace TinyEcs;

public sealed class World : IDisposable
{
	private readonly Archetype _archRoot;
	internal readonly EntitySparseSet<EcsRecord> _entities = new();
	private readonly Dictionary<EntityID, Archetype> _typeIndex = new ();
	private readonly Dictionary<EntityID, Table> _tableIndex = new ();
	private readonly Dictionary<int, EntityID> _components = new();


	public World()
	{
		_archRoot = new Archetype(this, new (ReadOnlySpan<EcsComponent>.Empty), ReadOnlySpan<EcsComponent>.Empty);
	}


	public int EntityCount => _entities.Length;

	public float DeltaTime { get; internal set; }



	public void Dispose()
	{
		_entities.Clear();
		_archRoot.Clear();
		_typeIndex.Clear();
		_components.Clear();
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

	public EntityID Component<T>() where T : unmanaged
	{
		//ref var cmpID = ref _components.Get((EntityID)TypeInfo<T>.Hash);
		ref var cmpID = ref CollectionsMarshal.GetValueRefOrAddDefault(_components, TypeInfo<T>.Hash, out var exists);
		if (/*Unsafe.IsNullRef(ref cmpID)*/ !exists || !IsAlive(cmpID))
		{
			var ent = SpawnEmpty();
			cmpID = ent.ID;
			//cmpID = ref _components.Add((EntityID)TypeInfo<T>.Hash, ent.ID);
			Set(cmpID, new EcsComponent(cmpID, TypeInfo<T>.Size));
			Set<EcsEnabled>(cmpID);
		}

		return cmpID;
	}

	public EntityID Component<TKind, TTarget>() where TKind : unmanaged where TTarget : unmanaged
		=> IDOp.Pair(Component<TKind>(), Component<TTarget>());

	public QueryBuilder Query()
	{
		return new QueryBuilder(this);
	}

	public unsafe EntityView System(delegate*<ref Iterator, void> system, EntityID query, ReadOnlySpan<Term> terms, float tick)
		=> Spawn()
			.Set(new EcsSystem(system, query, terms, tick));

	public EntityView Spawn()
		=> SpawnEmpty().Set<EcsEnabled>();

	internal EntityView SpawnEmpty(EntityID id = 0)
	{
		ref var record = ref (id > 0 ? ref _entities.Add(id, default!) : ref _entities.CreateNew(out id));
		record.Archetype = _archRoot;
		record.ArchetypeRow = _archRoot.Add(id);
		record.TableRow = _archRoot.Table.Increase();

		return new EntityView(this, id);
	}

	public void Despawn(EntityID entity)
	{
		ref var record = ref _entities.Get(entity);
		EcsAssert.Assert(!Unsafe.IsNullRef(ref record));

		var removedId = record.Archetype.Remove(ref record);
		EcsAssert.Assert(removedId == entity);

		var last = record.Archetype.Entities[record.ArchetypeRow];
		_entities.Get(last) = record;
		_entities.Remove(removedId);
	}

	public bool IsAlive(EntityID entity)
		=> _entities.Contains(entity);

	private void AttachComponent(EntityID entity, EntityID component, int size)
	{
		InternalAttachDetach(entity, component, size, true);
	}

	internal void DetachComponent(EntityID entity, EntityID component, int size)
	{
		InternalAttachDetach(entity, component, size, false);
	}

	private bool InternalAttachDetach(EntityID entity, EntityID component, int size, bool add)
	{
		ref var record = ref _entities.Get(entity);
		EcsAssert.Assert(!Unsafe.IsNullRef(ref record));

		var arch = CreateArchetype(record.Archetype, component, size, add);
		if (arch == null)
			return false;

		(var newRow, var newTableRow) = record.Archetype.MoveEntity(arch!, record.ArchetypeRow, record.TableRow, size);
		record.ArchetypeRow = newRow;
		record.TableRow = newTableRow;
		record.Archetype = arch!;

		return true;
	}

	[SkipLocalsInit]
	internal Archetype? CreateArchetype(Archetype root, EntityID component, int size, bool add)
	{
		var column = root.GetComponentIndex(component, size);
		if (add && column >= 0)
		{
			return null;
		}
		else if (!add && column < 0)
		{
			return null;
		}

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
				if (initType[i].ID != component)
				{
					span[j++] = initType[i];
				}
			}
		}
		else if (!span.IsEmpty)
		{
			initType.CopyTo(span);
			span[^1] = new EcsComponent(component, size);
		}

		span.Sort(static (s, k) => s.ID.CompareTo(k.ID));
		var hash = Hash(span);

		Table? table;
		if (size != 0)
		{
			if (!_tableIndex.TryGetValue(hash, out table))
			{
				table = new Table(span);
				_tableIndex[hash] = table;
			}
		}
		else
		{
			table = root.Table;
		}

		ref var arch = ref CollectionsMarshal.GetValueRefOrAddDefault(_typeIndex, hash, out var exists);
		if (!exists)
		{
			arch = _archRoot.InsertVertex(root, table, span, component);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static EntityID Hash(UnsafeSpan<EcsComponent> components)
		{
			unchecked
			{
				EntityID hash = 5381;

				while (components.CanAdvance())
				{
					hash = ((hash << 5) + hash) + components.Value.ID;

					components.Advance();
				}

				return hash;
			}
		}

		// var arch = FetchArchetype(record.Archetype, add, span);

        // static Archetype? FetchArchetype(Archetype root, bool add, ReadOnlySpan<EcsComponent> cmp)
        // {
		// 	if (cmp.SequenceEqual(root.ComponentInfo))
		// 	{
		// 		return root;
		// 	}

        //     var edges = add ? root._edgesRight : root._edgesLeft;
        //     foreach (ref var edge in CollectionsMarshal.AsSpan(edges))
        //     {
        //         var sub = FetchArchetype(edge.Archetype, add, cmp);
        //         if (sub != null)
        //             return sub;
        //     }

        //     return null;
        // }

		// if (arch == null)
		// {
		// 	arch = _archRoot.InsertVertex(record.Archetype, span, component);
		// }

		if (buffer != null)
		{
			ArrayPool<EcsComponent>.Shared.Return(buffer);
		}

		return arch;
	}

	internal void Set(EntityID entity, EntityID component, ReadOnlySpan<byte> data)
	{
		ref var record = ref _entities.Get(entity);
		EcsAssert.Assert(!Unsafe.IsNullRef(ref record));

		var column = record.Archetype.GetComponentIndex(component, data.Length);
		if (column < 0)
		{
			AttachComponent(entity, component, data.Length);
		}

		var buf = record.Archetype.GetComponentRaw(component, data.Length, record.TableRow, 1);

		EcsAssert.Assert(data.Length == buf.Length);
		data.CopyTo(buf);
	}

	internal bool Has(EntityID entity, EntityID component, int size)
	{
		ref var record = ref _entities.Get(entity);
		EcsAssert.Assert(!Unsafe.IsNullRef(ref record));

		return record.Archetype.GetComponentIndex(component, size) >= 0;
	}

	private Span<byte> Get(EntityID entity, EntityID component, int size)
	{
		ref var record = ref _entities.Get(entity);
		EcsAssert.Assert(!Unsafe.IsNullRef(ref record));

		return record.Archetype.GetComponentRaw(component, size, record.TableRow, 1);
	}

	public unsafe void Set<T>(EntityID entity, T component = default) where T : unmanaged
		=> Set(
				entity,
				Component<T>(),
				new ReadOnlySpan<byte>(&component, TypeInfo<T>.Size)
			);

	public void Unset<T>(EntityID entity) where T : unmanaged
	   => DetachComponent(entity, Component<T>(), TypeInfo<T>.Size);

	public bool Has<T>(EntityID entity) where T : unmanaged
		=> Has(entity, Component<T>(), TypeInfo<T>.Size);

	public ref T Get<T>(EntityID entity) where T : unmanaged
	{
		var raw = Get(entity, Component<T>(), TypeInfo<T>.Size);

		EcsAssert.Assert(!raw.IsEmpty);
		EcsAssert.Assert(TypeInfo<T>.Size == raw.Length);

		return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(raw));
	}

	public void SetSingleton<T>(T cmp = default) where T : unmanaged
		=> Set(Component<T>(), cmp);

	public ref T GetSingleton<T>() where T : unmanaged
		=> ref Get<T>(Component<T>());


	public void PrintGraph()
	{
		_archRoot.Print();
	}

	public unsafe void Query
	(
		Span<Term> terms,
		Commands? commands,
		delegate* <ref Iterator, void> action,
		object? userData = null
	)
	{
		terms.Sort(static (a, b) => a.ID.CompareTo(b.ID));

		QueryRec(_archRoot, terms, commands, action, userData);

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
	public int ArchetypeRow;
	public int TableRow;
}

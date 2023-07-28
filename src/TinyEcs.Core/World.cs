namespace TinyEcs;

public sealed class World : IDisposable
{
	private static readonly object _lock = new();
	private static EntityID _worldIDCount;

	internal readonly Archetype _archRoot;
	internal readonly EntitySparseSet<EcsRecord> _entities = new();
	private readonly Dictionary<EntityID, Archetype> _typeIndex = new ();

	public World()
	{
		_archRoot = new Archetype(this, ReadOnlySpan<EcsComponent>.Empty);

		// hacky
		lock (_lock)
			ID = ++_worldIDCount;
	}


	public EntityID ID { get; }

	public int EntityCount => _entities.Length;

	public float DeltaTime { get; internal set; }



	public void Dispose()
	{
		_entities.Clear();
		_archRoot.Clear();
		_typeIndex.Clear();
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
		=> TypeInfo<T>.GetID(this);

	public EntityID Component<TKind, TTarget>() where TKind : unmanaged where TTarget : unmanaged
		=> IDOp.Pair(Component<TKind>(), Component<TTarget>());

	public QueryBuilder Query()
	{
		return new QueryBuilder(this);
	}

	public unsafe SystemBuilder System(delegate* managed<ref Iterator, void> system)
		=> new SystemBuilder(this,
			Spawn()
				.Set(new EcsSystem(system))
				.Set(new EcsSystemTick() { Value = 0 })
				.Set<EcsQuery>());

	public EntityView Spawn()
		=> SpawnEmpty().Set<EcsEnabled>();

	internal EntityView SpawnEmpty(EntityID id = 0)
	{
		ref var record = ref (id > 0 ? ref _entities.Add(id, default!) : ref _entities.CreateNew(out id));
		record.Archetype = _archRoot;
		record.Row = _archRoot.Add(id);

		return new EntityView(this, id);
	}

	public void Despawn(EntityID entity)
	{
		ref var record = ref _entities.Get(entity);
		EcsAssert.Assert(!Unsafe.IsNullRef(ref record));

		var removedId = record.Archetype.Remove(record.Row);
		EcsAssert.Assert(removedId == entity);

		var last = record.Archetype.Entities[record.Row];
		_entities.Get(last) = record;
		_entities.Remove(removedId);
	}

	public bool IsAlive(EntityID entity)
		=> _entities.Contains(entity);

	private void AttachComponent(EntityID entity, EntityID component, int size)
	{
		InternalAttachDetach(entity, component, size);
	}

	internal void DetachComponent(EntityID entity, EntityID component)
	{
		InternalAttachDetach(entity, component, -1);
	}

	private bool InternalAttachDetach(EntityID entity, EntityID component, int size)
	{
		ref var record = ref _entities.Get(entity);
		EcsAssert.Assert(!Unsafe.IsNullRef(ref record));

		var arch = CreateArchetype(record.Archetype, component, size);
		if (arch == null)
			return false;

		var newRow = Archetype.MoveEntity(record.Archetype, arch!, record.Row);
		record.Row = newRow;
		record.Archetype = arch!;

		return true;
	}

	[SkipLocalsInit]
	internal Archetype? CreateArchetype(Archetype root, EntityID component, int size)
	{
		var column = root.GetComponentIndex(component);
		var add = size > 0;

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

		const int STACKALLOC_SIZE = 32;

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

		ref var arch = ref CollectionsMarshal.GetValueRefOrAddDefault(_typeIndex, Hash(span), out var exists);
		if (!exists)
		{
			arch = _archRoot.InsertVertex(root, span, component);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static EntityID Hash(Span<EcsComponent> components)
		{
			unchecked
			{
				EntityID hash = 5381;

				foreach (ref readonly var id in components)
				{
					hash = ((hash << 5) + hash) + id.ID;
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

		var column = record.Archetype.GetComponentIndex(component);
		if (column < 0)
		{
			AttachComponent(entity, component, data.Length);
		}

		var buf = record.Archetype.GetComponentRaw(component, record.Row, 1);

		EcsAssert.Assert(data.Length == buf.Length);
		data.CopyTo(buf);
	}

	internal bool Has(EntityID entity, EntityID component)
		=> !Get(entity, component).IsEmpty;

	private Span<byte> Get(EntityID entity, EntityID component)
	{
		ref var record = ref _entities.Get(entity);
		EcsAssert.Assert(!Unsafe.IsNullRef(ref record));

		return record.Archetype.GetComponentRaw(component, record.Row, 1);
	}

	public unsafe void Set<T>(EntityID entity, T component = default) where T : unmanaged
		=> Set(
				entity,
				Component<T>(),
				new ReadOnlySpan<byte>(&component, TypeInfo<T>.Size)
			);

	public void Unset<T>(EntityID entity) where T : unmanaged
	   => DetachComponent(entity, Component<T>());

	public bool Has<T>(EntityID entity) where T : unmanaged
		=> Has(entity, Component<T>());

	public ref T Get<T>(EntityID entity) where T : unmanaged
	{
		var raw = Get(entity, Component<T>());

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
		PrintRec(_archRoot, 0, 0);

		static void PrintRec(Archetype root, int depth, EntityID rootComponent)
		{
			Console.WriteLine("{0}[{1}] |{2}|", new string('.', depth), string.Join(", ", root.Components), rootComponent);

			foreach (ref readonly var edge in CollectionsMarshal.AsSpan(root._edgesRight))
			{
				PrintRec(edge.Archetype, depth + 1, edge.ComponentID);
			}
		}
	}

	public void Query(Span<EntityID> with, Span<EntityID> without, Commands? commands, IteratorDelegate action)
	{
		with.Sort();
		without.Sort();

		QueryRec(_archRoot, with, without, commands, action);

		static void QueryRec(Archetype root, ReadOnlySpan<EntityID> with, ReadOnlySpan<EntityID> without, Commands? commands, IteratorDelegate action)
		{
			if (root.Count > 0 && root.IsSuperset(with))
			{
				var it = new Iterator(commands, root);
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
				if (without.IndexOf(start.ComponentID) < 0)
				{
					QueryRec(start.Archetype, with, without, commands, action);
				}

				start = ref Unsafe.Add(ref start, 1);
			}
		}
	}

	public void AttachTo(EntityID childID, EntityID parentID)
	{
		//Detach(childID);

		//if (Has<EcsParent>(parentID))
		//{
		//	ref var parent = ref Get<EcsParent>(parentID);
		//	parent.ChildrenCount += 1;

		//	ref var firstChild = ref Get<EcsChild>(parent.FirstChild);
		//	firstChild.Prev = childID;

		//	Set(childID, new EcsChild()
		//	{
		//		Parent = parentID,
		//		Prev = 0,
		//		Next = parent.FirstChild
		//	});

		//	parent.FirstChild = childID;

		//	return;
		//}

		//Set(parentID, new EcsParent()
		//{
		//	ChildrenCount = 1,
		//	FirstChild = childID
		//});

		//Set(childID, new EcsChild()
		//{
		//	Parent = parentID,
		//	Prev = 0,
		//	Next = 0
		//});
	}

	public void Detach(EntityID id)
	{
		//if (!Has<EcsChild>(id))
		//	return;

		//ref var child = ref Get<EcsChild>(id);
		//ref var parent = ref Get<EcsParent>(child.Parent);

		//parent.ChildrenCount -= 1;

		//if (parent.ChildrenCount <= 0)
		//{
		//	Unset<EcsParent>(child.Parent);
		//}
		//else
		//{
		//	if (parent.FirstChild == id)
		//	{
		//		parent.FirstChild = child.Next;
		//		child.Prev = 0;
		//	}
		//	else
		//	{
		//		if (child.Prev != 0)
		//		{
		//			Get<EcsChild>(child.Prev).Next = child.Next;
		//		}

		//		if (child.Next != 0)
		//		{
		//			Get<EcsChild>(child.Next).Prev = child.Prev;
		//		}
		//	}

		//}

		//Unset<EcsChild>(id);
	}

	public void RemoveChildren(EntityID id)
	{
		// if (!Has<EcsParent>(id))
		// 	return;

		// ref var parent = ref Get<EcsParent>(id);

		// while (parent.ChildrenCount > 0)
		// {
		// 	Detach(parent.FirstChild);
		// }
	}
}


struct EcsRecord
{
	public Archetype Archetype;
	public int Row;
}

using System.Numerics;

namespace TinyEcs;

public sealed class Commands2
{
	private readonly World _main;
	private readonly EntitySparseSet<EntityID> _register, _despawn;
	private readonly EntitySparseSet<SetComponent> _set;
	private readonly EntitySparseSet<UnsetComponent> _unset;

	internal Commands2(World main)
	{
		_main = main;
		_register = new();
		_despawn = new();
		_set = new();
		_unset = new();
	}


	public CommandEntityView2 Entity(EntityID id)
	{
		EcsAssert.Assert(_main.IsAlive(id));

		return new CommandEntityView2(this, id);
	}

	public CommandEntityView2 Spawn()
	{
		var ent = _main.SpawnEmpty();
		_ = RegisterEntity(ent.ID);

		return new CommandEntityView2(this, ent.ID)
			.Set<EcsEnabled>();
	}

	public void Despawn(EntityID id)
	{
		EcsAssert.Assert(_main.IsAlive(id));

		var ent = RegisterEntity(id);
		ref var entity = ref _despawn.Get(ent);
		if (!Unsafe.IsNullRef(ref entity))
		{
			_despawn.Add(ent, id);
		}
	}

	public unsafe ref T Set<T>(EntityID id, T cmp = default) where T : unmanaged
	{
		EcsAssert.Assert(_main.IsAlive(id));

		if (_main.Has<T>(id))
		{
			ref var value = ref _main.Get<T>(id);
			value = cmp;

			return ref value;
		}

		var ent = RegisterEntity(id);
		var cmpID = _main.Component<T>();

		ref var set = ref _set.CreateNew(out _);
		set.Entity = ent;
		set.ComponentID = cmpID;
		set.Size = sizeof(T);

		if (set.Data.Length < set.Size)
		{
			set.Data = new byte[Math.Max(sizeof(ulong), (int) BitOperations.RoundUpToPowerOf2((uint) set.Size))];
		}

		ref var reference = ref MemoryMarshal.GetReference(set.Data.Span);
		fixed (byte* ptr = &reference)
			Unsafe.Copy(ptr, ref cmp);

		return ref Unsafe.As<byte, T>(ref reference);
	}

	public unsafe void Set(EntityID id, EntityID cmp)
	{

	}

	public unsafe void Set<TKind, TTarget>(EntityID id) where TKind : unmanaged where TTarget : unmanaged
	{

	}

	public void Unset<T>(EntityID id) where T : unmanaged
	{
		EcsAssert.Assert(_main.IsAlive(id));

		var ent = RegisterEntity(id);
		var cmpID = _main.Component<T>();

		ref var unset = ref _unset.CreateNew(out _);
		unset.Entity = ent;
		unset.ComponentID = cmpID;
	}

	public ref T Get<T>(EntityID entity) where T : unmanaged
	{
		EcsAssert.Assert(_main.IsAlive(entity));

		if (_main.Has<T>(entity))
		{
			return ref _main.Get<T>(entity);
		}

		Unsafe.SkipInit<T>(out var cmp);

		return ref Set(entity, cmp);
	}


	public void Merge()
	{
		foreach (ref var set in _set)
		{
			EcsAssert.Assert(_main.IsAlive(set.Entity));

			_main.Set(set.Entity, set.ComponentID, set.Data.Span.Slice(0, set.Size));
		}

		foreach (ref var unset in _unset)
		{
			EcsAssert.Assert(_main.IsAlive(unset.Entity));

			_main.DetachComponent(unset.Entity, unset.ComponentID);
		}

		foreach (ref var e in _despawn)
		{
			_main.Despawn(e);
		}

		_register.Clear();
		_despawn.Clear();
		_set.Clear();
		_unset.Clear();
	}

	private EntityID RegisterEntity(EntityID id)
	{
		ref var ent = ref _register.Get(id);
		if (Unsafe.IsNullRef(ref ent))
		{
			_register.CreateNew(out var ent2) = id;
			return ent2;
		}

		return ent;
	}


	private struct SetComponent
	{
		public EntityID Entity;
		public EntityID ComponentID;
		public int Size;
		public Memory<byte> Data;
	}

	private struct UnsetComponent
	{
		public EntityID Entity;
		public EntityID ComponentID;
	}
}

public readonly ref struct CommandEntityView2
{
	private readonly EntityID _id;
	private readonly Commands2 _cmds;

	internal CommandEntityView2(Commands2 cmds, EntityID id)
	{
		_cmds = cmds;
		_id = id;
	}

	public readonly EntityID ID => _id;

	public readonly CommandEntityView2 Set<T>(T cmp = default) where T : unmanaged
	{
		_cmds.Set(_id, cmp);
		return this;
	}

	// public readonly CommandEntityView2 Set(EntityID id)
	// {
	// 	_cmds.Add(_id, id);
	// 	return this;
	// }

	// public readonly CommandEntityView2 Add<TKind>(EntityID id) where TKind : unmanaged
	// {
	// 	_cmds.Add(_id, _cmds.Main.Component<TKind>(), id);
	// 	return this;
	// }

	// public readonly CommandEntityView2 Set(EntityID first, EntityID second)
	// {
	// 	_cmds.Add(_id, first, second);
	// 	return this;
	// }

	// public readonly CommandEntityView2 Set<TKind, TTarget>()
	// 	where TKind : unmanaged
	// 	where TTarget : unmanaged
	// {
	// 	_cmds.Set<TKind, TTarget>(_id);
	// 	return this;
	// }

	public readonly CommandEntityView2 Unset<T>() where T : unmanaged
	{
		_cmds.Unset<T>(_id);
		return this;
	}

	public readonly CommandEntityView2 Despawn()
	{
		_cmds.Despawn(_id);
		return this;
	}

	public readonly ref T Get<T>() where T : unmanaged
	{
		return ref _cmds.Get<T>(_id);
	}

	// public readonly bool Has<T>() where T : unmanaged
	// {
	// 	return _cmds.Has<T>(_id);
	// }
}


public sealed class Commands : IDisposable
{
	private readonly World _main, _mergeWorld;

	public Commands(World main)
	{
		_main = main;
		_mergeWorld = new World();
	}

	public World Main => _main;


	public void Merge()
	{
		// we pass the Commands, but must not be used to edit entities!

		_mergeWorld.Query(
			stackalloc EntityID[] {
				_mergeWorld.Component<EcsEnabled>(),
				_mergeWorld.Component<ComponentAdded>(),
			},
			Span<EntityID>.Empty,
			ComponentSetSystem
		);

		_mergeWorld.Query(
			stackalloc EntityID[] {
				_mergeWorld.Component<EcsEnabled>(),
				_mergeWorld.Component<ComponentEdited>(),
			},
			Span<EntityID>.Empty,
			ComponentEditedSystem
		);

		_mergeWorld.Query(
			stackalloc EntityID[] {
				_mergeWorld.Component<EcsEnabled>(),
				_mergeWorld.Component<ComponentRemoved>(),
			},
			Span<EntityID>.Empty,
			ComponentUnsetSystem
		);

		_mergeWorld.Query(
			stackalloc EntityID[] {
				_mergeWorld.Component<EcsEnabled>(),
				_mergeWorld.Component<EntityDestroyed>(),
			},
			Span<EntityID>.Empty,
			EntityDestroyedSystem
		);

		_mergeWorld.Query(
			stackalloc EntityID[] {
				_mergeWorld.Component<EcsEnabled>(),
				_mergeWorld.Component<MarkDestroy>(),
			},
			Span<EntityID>.Empty,
			MarkDestroySystem
		);
	}


	static void EntityDestroyedSystem(Iterator it)
	{
		var main = it.Commands!.Main;

		var opA = it.Field<EntityDestroyed>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var op = ref opA[i];

			main.Despawn(op.Target);
		}
	}

	static void ComponentSetSystem(Iterator it)
	{
		var main = it.Commands!.Main;

		var opA = it.Field<ComponentAdded>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var op = ref opA[i];

			var raw = it.GetComponentRaw(op.ID, i, 1);
			main.Set(op.Target, op.Component, raw);
		}
	}

	static void ComponentEditedSystem(Iterator it)
	{
		var main = it.Commands!.Main;

		var opA = it.Field<ComponentEdited>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var op = ref opA[i];

			var raw = it.GetComponentRaw(op.ID, i, 1);
			main.Set(op.Target, op.Component, raw);
		}
	}

	static void ComponentUnsetSystem(Iterator it)
	{
		var main = it.Commands!.Main;

		var opA = it.Field<ComponentRemoved>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var op = ref opA[i];

			main.DetachComponent(op.Target, op.Component);
		}
	}

	static void MarkDestroySystem(Iterator it)
	{
		for (int i = 0; i < it.Count; ++i)
		{
			var entity = it.Entity(i);

			it.Commands!._mergeWorld.Despawn(entity);
		}
	}


	public CommandEntityView Entity(EntityID id)
	{
		EcsAssert.Assert(_main.IsAlive(id));

		return new CommandEntityView(this, id);
	}

	public CommandEntityView Spawn()
	{
		var mainEnt = _main.SpawnEmpty();
		return new CommandEntityView(this, mainEnt)
			.Set<EcsEnabled>();
	}

	public void Despawn(EntityID entity)
	{
		EcsAssert.Assert(_main.IsAlive(entity));

		_mergeWorld.Spawn()
			.Set<MarkDestroy>()
			.Set(new EntityDestroyed()
			{
				Target = entity
			});
	}

	public void Set<T>(EntityID entity, T cmp = default) where T : unmanaged
	{
		EcsAssert.Assert(_main.IsAlive(entity));

		var idMain = _main.Component<T>();
		var idMerge = _mergeWorld.Component<ComponentPocWithValue<T>>();

		_mergeWorld.Spawn()
			.Set<MarkDestroy>()
			.Set(new ComponentAdded()
			{
				Target = entity,
				Component = idMain,
				ID = idMerge
			})
			.Set(new ComponentPocWithValue<T>() { Value = cmp });
	}

	public void Set<T0, T1>(EntityID entity)
		where T0 : unmanaged
		where T1 : unmanaged
	{
		EcsAssert.Assert(_main.IsAlive(entity));

		var idMain0 = _main.Component<T0>();
		var idMain1 = _main.Component<T1>();

		Add(entity, idMain0, idMain1);
	}

	public void Add(EntityID entity, EntityID cmp)
	{
		EcsAssert.Assert(_main.IsAlive(entity));
		EcsAssert.Assert(_main.IsAlive(cmp));

		var idMain = cmp;
		var idMerge = _mergeWorld.Component<ComponentPocEntity>();

		_mergeWorld.Spawn()
			.Set<MarkDestroy>()
			.Set(new ComponentAdded()
			{
				Target = entity,
				Component = idMain,
				ID = idMerge
			})
			.Set(new ComponentPocEntity() { Value = cmp });
	}

	public void Add(EntityID entity, EntityID first, EntityID second)
	{
		EcsAssert.Assert(_main.IsAlive(entity));
		EcsAssert.Assert(_main.IsAlive(first));
		EcsAssert.Assert(_main.IsAlive(second));

		var idMain = IDOp.Pair(first, second);
		var idMerge = _mergeWorld.Component<ComponentPocEntityPair>();

		_mergeWorld.Spawn()
			.Set<MarkDestroy>()
			.Set(new ComponentAdded()
			{
				Target = entity,
				Component = idMain,
				ID = idMerge
			})
			.Set(new ComponentPocEntityPair() { First = first, Second = second });
	}

	public void Unset<T>(EntityID entity) where T : unmanaged
	{
		EcsAssert.Assert(_main.IsAlive(entity));

		_mergeWorld.Spawn()
			.Set<MarkDestroy>()
			.Set(new ComponentRemoved()
			{
				Target = entity,
				Component = _main.Component<T>()
			});
	}

	public ref T Get<T>(EntityID entity) where T : unmanaged
	{
		EcsAssert.Assert(_main.IsAlive(entity));

		if (_main.Has<T>(entity))
		{
			return ref _main.Get<T>(entity);
		}

		var idMain = _main.Component<T>();
		var idMerge = _mergeWorld.Component<ComponentPocWithValue<T>>();

		Unsafe.SkipInit<T>(out var value);

		var e = _mergeWorld.Spawn()
			.Set<MarkDestroy>()
			.Set(new ComponentEdited()
			{
				Target = entity,
				Component = idMain,
				ID = idMerge,
			})
			.Set(new ComponentPocWithValue<T>() { Value = value });

		return ref e.Get<ComponentPocWithValue<T>>().Value;
	}

	public bool Has<T>(EntityID entity) where T : unmanaged
	{
		EcsAssert.Assert(_main.IsAlive(entity));

		return _main.Has<T>(entity);
	}

	public void Dispose()
	{
		_mergeWorld?.Dispose();
	}



	struct EntityCreated
	{
		public EntityID Target;
	}

	struct EntityDestroyed
	{
		public EntityID Target;
	}

	struct ComponentEdited
	{
		public EntityID Target;
		public EntityID ID;
		public EntityID Component;
		// public void* Data;
		// public UnsafeMemory Pool;
	}

	struct ComponentAdded
	{
		public EntityID Target;
		public EntityID ID;
		public EntityID Component;
	}

	struct ComponentRemoved
	{
		public EntityID Target;
		public EntityID Component;
	}

	struct MarkDestroy { }

	struct ComponentPocEntity
	{
		public EntityID Value;
	}

	struct ComponentPocEntityPair
	{
		public EntityID First;
		public EntityID Second;
	}

	struct ComponentPocWithValue<T> where T : unmanaged
	{
		public T Value;
	}
}

public readonly ref struct CommandEntityView
{
	private readonly EntityID _id;
	private readonly Commands _cmds;

	internal CommandEntityView(Commands cmds, EntityID id)
	{
		_cmds = cmds;
		_id = id;
	}

	public readonly EntityID ID => _id;

	public readonly CommandEntityView Set<T>(T cmp = default) where T : unmanaged
	{
		_cmds.Set(_id, cmp);
		return this;
	}

	public readonly CommandEntityView Set(EntityID id)
	{
		_cmds.Add(_id, id);
		return this;
	}

	public readonly CommandEntityView Add<TKind>(EntityID id) where TKind : unmanaged
	{
		_cmds.Add(_id, _cmds.Main.Component<TKind>(), id);
		return this;
	}

	public readonly CommandEntityView Set(EntityID first, EntityID second)
	{
		_cmds.Add(_id, first, second);
		return this;
	}

	public readonly CommandEntityView Set<TKind, TTarget>()
		where TKind : unmanaged
		where TTarget : unmanaged
	{
		_cmds.Set<TKind, TTarget>(_id);
		return this;
	}

	public readonly CommandEntityView Unset<T>() where T : unmanaged
	{
		_cmds.Unset<T>(_id);
		return this;
	}

	public readonly CommandEntityView Despawn()
	{
		_cmds.Despawn(_id);
		return this;
	}

	public readonly ref T Get<T>() where T : unmanaged
	{
		return ref _cmds.Get<T>(_id);
	}

	public readonly bool Has<T>() where T : unmanaged
	{
		return _cmds.Has<T>(_id);
	}
}

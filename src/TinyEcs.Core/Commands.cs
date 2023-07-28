namespace TinyEcs;

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

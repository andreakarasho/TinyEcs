namespace TinyEcs;

public unsafe sealed class Commands : IDisposable
{
	private readonly World _main, _mergeWorld;
	private readonly QueryBuilder _entityCreated, _entityDestroyed, _componentSet, _componentUnset, _toBeDestroyed;


	public Commands(World main)
	{
		_main = main;
		_mergeWorld = new World();

		_entityCreated = _mergeWorld.Query()
			.With<EntityCreated>();

		_entityDestroyed = _mergeWorld.Query()
			.With<EntityDestroyed>();

		_componentSet = _mergeWorld.Query()
			.With<ComponentAdded>();

		_componentUnset = _mergeWorld.Query()
			.With<ComponentRemoved>();

		_toBeDestroyed = _mergeWorld.Query()
			.With<MarkDestroy>();
	}

	public World Main => _main;


	public void Merge()
	{
		// we pass the Commands, but must not be used to edit entities!
		QueryEx.Fetch(_mergeWorld, _entityCreated.ID, this, &EntityCreatedSystem, 0f);
		QueryEx.Fetch(_mergeWorld, _componentSet.ID, this, &ComponentSetSystem, 0f);
		QueryEx.Fetch(_mergeWorld, _componentUnset.ID, this, &ComponentUnsetSystem, 0f);
		QueryEx.Fetch(_mergeWorld, _entityDestroyed.ID, this, &EntityDestroyedSystem, 0f);
		QueryEx.Fetch(_mergeWorld, _toBeDestroyed.ID, this, &MarkDestroySystem, 0f);
	}


	static void EntityCreatedSystem(Commands cmds, ref EntityIterator it)
	{
		var archetype = it.Archetype;
		var main = cmds.Main;
		var merge = it.World;

		var created = TypeInfo<EntityCreated>.GetID(merge);
		var destroyed = TypeInfo<EntityDestroyed>.GetID(merge);
		var componentAdded = TypeInfo<ComponentAdded>.GetID(merge);
		var componentRemoved = TypeInfo<ComponentRemoved>.GetID(merge);
		var markDestroy = TypeInfo<MarkDestroy>.GetID(merge);
		var entityEnabledCmp = TypeInfo<EcsEnabled>.GetID(merge);
		var ecsComp = TypeInfo<EcsComponent>.GetID(merge);

		var opA = it.Field<EntityCreated>();

		for (int i = 0; i < it.Count; ++i)
		{
			//var target = main.SpawnEmpty();

			//for (int j = 0; j < archetype.ComponentInfo.Length; ++j)
			//{
			//	ref readonly var cmp = ref archetype.ComponentInfo[j];

			//	// TODO: find a better way to find unecessary components
			//	//       maybe apply a flag to the component ID?
			//	if (cmp.ID == created || cmp.ID == destroyed ||
			//		cmp.ID == componentAdded || cmp.ID == componentRemoved ||
			//		cmp.ID == markDestroy || cmp.ID == entityViewComp ||
			//		cmp.ID == entityEnabledCmp || cmp.ID == ecsComp)
			//		continue;

			//	var raw = archetype.GetComponentRaw(cmp.ID, i, 1);
				

			//	Debug.Assert(main.IsAlive(cmp.ID));
			//	Debug.Assert(main.Has<EcsComponent>(cmp.ID));
			//	var id = TypeInfo<Serial>.GetID(main);
			//	ref var ff = ref main.Get<EcsComponent>(id);

			//	var e = merge.Spawn()
			//		.Set<MarkDestroy>()
			//		.Set(new ComponentAdded()
			//		{
			//			Target = target,
			//			Component = cmp.ID,
			//			ID = cmp.ID
			//		})
			//		.Set(cmp);

			//	merge.SetComponentData(e, cmp.ID, raw);
			//}

			//main.Set(target, new EntityView(main.ID, target));
			//main.Set<EcsEnabled>(target);
		}
	}

	static void EntityDestroyedSystem(Commands cmds, ref EntityIterator it)
	{
		var main = cmds.Main;

		var opA = it.Field<EntityDestroyed>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var op = ref opA[i];

			main.Despawn(op.Target);
		}
	}

	static void ComponentSetSystem(Commands cmds, ref EntityIterator it)
	{
		var main = cmds.Main;

		var opA = it.Field<ComponentAdded>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var op = ref opA[i];

			var raw = it.Archetype.GetComponentRaw(op.ID, i, 1);
			main.SetComponentData(op.Target, op.Component, raw);
		}
	}

	static void ComponentUnsetSystem(Commands cmds, ref EntityIterator it)
	{
		var main = cmds.Main;
		var merge = it.World;

		var opA = it.Field<ComponentRemoved>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var op = ref opA[i];

			main.DetachComponent(op.Target, op.Component);
		}
	}

	static void MarkDestroySystem(Commands cmds, ref EntityIterator it)
	{
		for (int i = 0; i < it.Count; ++i)
		{
			var entity = it.Entity(i);

			cmds._mergeWorld.Despawn(entity);
		}
	}


	public CommandEntityView Entity(EntityID id)
	{
		Debug.Assert(_main.IsAlive(id));

		return new CommandEntityView(this, id);
	}

	public CommandEntityView Spawn()
	{		
		var mainEnt = _main.SpawnEmpty();
		//var mergeEnt = _mergeWorld.Spawn()
		//	.Set<MarkDestroy>()
		//	.Set(new EntityCreated()
		//	{
		//		Target = 0
		//	});

		return new CommandEntityView(this, mainEnt)
			.Set<EcsEnabled>();
	}

	public void Despawn(EntityID entity)
	{
		Debug.Assert(_main.IsAlive(entity));

		_mergeWorld.Spawn()
			.Set<MarkDestroy>()
			.Set(new EntityDestroyed()
			{
				Target = entity
			});
	}

	public void Set<T>(EntityID entity, T cmp = default) where T : unmanaged
	{
		Debug.Assert(_main.IsAlive(entity));

		var idMain = TypeInfo<T>.GetID(_main);
		var idMerge = TypeInfo<ComponentPoc<T>>.GetID(_mergeWorld);

		_mergeWorld.Spawn()
			.Set<MarkDestroy>()
			.Set(new ComponentAdded()
			{
				Target = entity,
				Component = idMain,
				ID = idMerge
			})
			.Set(new ComponentPoc<T>() { Value = cmp });
	}

	public void Set<T0, T1>(EntityID entity) 
		where T0 : unmanaged 
		where T1 : unmanaged
	{
		Debug.Assert(_main.IsAlive(entity));

		var idMain0 = TypeInfo<T0>.GetID(_main);
		var idMain1 = TypeInfo<T1>.GetID(_main);

		Add(entity, idMain0, idMain1);

		//var pair = IDOp.Pair(idMain0, idMain1);
		//var idMerge = TypeInfo<ComponentPocPair<T0, T1>>.GetID(_mergeWorld);

		//_mergeWorld.Spawn()
		//	.Set<MarkDestroy>()
		//	.Set(new ComponentAdded()
		//	{
		//		Target = entity,
		//		Component = pair,
		//		ID = idMerge
		//	})
		//	.Set(new ComponentPocPair<T0, T1>() { });
	}

	public void Add(EntityID entity, EntityID cmp)
	{
		Debug.Assert(_main.IsAlive(entity));
		Debug.Assert(_main.IsAlive(cmp));

		var idMain = cmp;
		var idMerge = TypeInfo<ComponentPocEntity>.GetID(_mergeWorld);

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
		Debug.Assert(_main.IsAlive(entity));
		Debug.Assert(_main.IsAlive(first));
		Debug.Assert(_main.IsAlive(second));

		var idMain = IDOp.Pair(first, second);
		var idMerge = TypeInfo<ComponentPocEntityPair>.GetID(_mergeWorld);

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
		Debug.Assert(_main.IsAlive(entity) || _mergeWorld.IsAlive(entity));

		_mergeWorld.Spawn()
			.Set<MarkDestroy>()
			.Set(new ComponentRemoved()
			{
				Target = entity,
				Component = TypeInfo<T>.GetID(_main)
			});
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

	struct MarkDestroy{ }

	struct ComponentPocEntity
	{
		public EntityID Value;
	}

	struct ComponentPocEntityPair
	{
		public EntityID First;
		public EntityID Second;
	}

	struct ComponentPoc<T> where T : unmanaged
	{
		public T Value;
	}

	struct ComponentPocPair<T0, T1> 
		where T0 : unmanaged
		where T1 : unmanaged
	{
		public T0 First;
		public T1 Second;
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
		_cmds.Add(_id, id);
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
}

sealed unsafe class Ecs
{
	private readonly World _world;
	private readonly Commands _cmds;

	private readonly QueryBuilder
		_querySystemUpdate,
		_querySystemPreUpdate,
		_querySystemPostUpdate;

	private readonly QueryBuilder
		_querySystemStartup,
		_querySystemPreStartup,
		_querySystemPostStartup;

	private ulong _frame;


	public Ecs()
	{
		_world = new World();
		_cmds = new Commands(_world);

		_querySystemUpdate = Query()
			.With<EcsEnabled>()
			.With<EcsSystem>()
			.With<EcsSystemPhaseOnUpdate>();

		_querySystemPreUpdate = Query()
			.With<EcsEnabled>()
			.With<EcsSystem>()
			.With<EcsSystemPhasePreUpdate>();

		_querySystemPostUpdate = Query()
			.With<EcsEnabled>()
			.With<EcsSystem>()
			.With<EcsSystemPhasePostUpdate>();


		_querySystemStartup = Query()
			.With<EcsEnabled>()
			.With<EcsSystem>()
			.With<EcsSystemPhaseOnStartup>();

		_querySystemPreStartup = Query()
			.With<EcsEnabled>()
			.With<EcsSystem>()
			.With<EcsSystemPhasePreStartup>();

		_querySystemPostStartup = Query()
			.With<EcsEnabled>()
			.With<EcsSystem>()
			.With<EcsSystemPhasePostStartup>();
	}

	public EntityView Entity(EntityID id)
	{
		Debug.Assert(_world.IsAlive(id));

		return new EntityView(_world, id);
	}

	public CommandEntityView Spawn()
		=> _cmds.Spawn();

	public void Despawn(EntityID entity)
		=> _cmds.Despawn(entity);

	public void Set<T>(EntityID entity, T value = default) where T : unmanaged
		=> _cmds.Set(entity, value);

	public void Unset<T>(EntityID entity) where T : unmanaged
		=> _cmds.Unset<T>(entity);

	public void SetSingleton<T>(T cmp = default) where T : unmanaged
		=> _world.Set(TypeInfo<T>.GetID(_world), cmp);

	public ref T GetSingleton<T>() where T : unmanaged
		=> ref _world.Get<T>(TypeInfo<T>.GetID(_world));


	public QueryBuilder Query()
		=> _world.Query();

	public unsafe SystemBuilder AddStartupSystem(delegate* managed<Commands, ref EntityIterator, void> system)
		=> _world.System(system)
			.Set<EcsSystemPhaseOnStartup>();

	public unsafe SystemBuilder AddSystem(delegate* managed<Commands, ref EntityIterator, void> system)
		=> _world.System(system)
			.Set<EcsSystemPhaseOnUpdate>();

	public unsafe void Step(float delta)
	{
		_cmds.Merge();

		if (_frame == 0)
		{
			QueryEx.Fetch(_world, _querySystemPreStartup.ID, _cmds, &RunSystems, delta);
			QueryEx.Fetch(_world, _querySystemStartup.ID, _cmds, &RunSystems, delta);
			QueryEx.Fetch(_world, _querySystemPostStartup.ID, _cmds, &RunSystems, delta);
		}

		QueryEx.Fetch(_world, _querySystemPreUpdate.ID, _cmds, &RunSystems, delta);
		QueryEx.Fetch(_world, _querySystemUpdate.ID, _cmds, &RunSystems, delta);
		QueryEx.Fetch(_world, _querySystemPostUpdate.ID, _cmds, &RunSystems, delta);

		_cmds.Merge();
		_frame += 1;
	}

	static unsafe void RunSystems(Commands cmds, ref EntityIterator it)
	{
		var sysA = it.Field<EcsSystem>();
		var sysTickA = it.Field<EcsSystemTick>();
		var queryA = it.Field<EcsQuery>();

		var emptyIt = new EntityIterator(it.World._archRoot, 0, it.DeltaTime);

		for (int i = 0; i < it.Count; ++i)
		{
			ref var sys = ref sysA[i];
			ref var query = ref queryA[i];
			ref var tick = ref sysTickA[i];

			if (tick.Value > 0.00f)
			{
				// TODO: check for it.DeltaTime > 0?
				tick.Current += it.DeltaTime;

				if (tick.Current < tick.Value)
				{
					continue;
				}

				tick.Current = 0;
			}

			if (query.ID != 0)
			{
				QueryEx.Fetch(it.World, query.ID, cmds, sys.Func, it.DeltaTime);
			}
			else
			{
				sys.Func(cmds, ref emptyIt);
			}
		}

	}
}


#if NETSTANDARD2_1
internal readonly ref struct Ref<T>
{
    private readonly Span<T> span;

    public Ref(ref T value)
    {
        span = MemoryMarshal.CreateSpan(ref value, 1);
    }

    public ref T Value => ref MemoryMarshal.GetReference(span);
}

public static class SortExtensions
{
	public static void Sort<T>(this Span<T> span) where T : IComparable<T>
	{
		for (int i = 0; i < span.Length - 1; i++)
		{
			for (int j = 0; j < span.Length - i - 1; j++)
			{
				if (span[j].CompareTo(span[j + 1]) > 0)
				{
					// Swap the elements
					T temp = span[j];
					span[j] = span[j + 1];
					span[j + 1] = temp;
				}
			}
		}
	}
}
#endif
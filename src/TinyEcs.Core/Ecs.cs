namespace TinyEcs;


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
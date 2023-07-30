namespace TinyEcs;

sealed unsafe class Ecs
{
	private readonly World _world;
	private readonly Commands _cmds;
	private ulong _frame;

	public Ecs()
	{
		_world = new World();
		_cmds = new Commands(_world);
	}

	public EntityView Entity(EntityID id)
	{
		EcsAssert.Assert(_world.IsAlive(id));

		return new EntityView(_world, id);
	}

	public CommandEntityView Spawn()
		=> _cmds.Spawn();

	public EntityID Component<T>() where T : unmanaged
		=> _world.Component<T>();

	public EntityID Component<TKind, TTarget>()
	where TKind : unmanaged
	where TTarget : unmanaged
		=> _world.Component<TKind, TTarget>();

	public void Despawn(EntityID entity)
		=> _cmds.Despawn(entity);

	public QueryBuilder Query()
		=> _world.Query();

	public unsafe SystemBuilder StartupSystem(delegate*<ref Iterator, void> system)
		=> _world.System(system)
			.Set<EcsSystemPhaseOnStartup>();

	public unsafe SystemBuilder System(delegate*<ref Iterator, void> system)
		=> _world.System(system)
			.Set<EcsSystemPhaseOnUpdate>();

	public unsafe SystemBuilder System(delegate*<ref Iterator, void> system, in QueryBuilder query)
		=> _world.System(system)
			.Set<EcsSystemPhaseOnUpdate>()
			.Set(new EcsQuery() { ID = query.Build() });

	public void SetSingleton<T>(T cmp = default) where T : unmanaged
		=> _world.SetSingleton(cmp);

	public ref T GetSingleton<T>() where T : unmanaged
		=> ref _world.GetSingleton<T>();

	[SkipLocalsInit]
	public unsafe void Step(float delta = 0.0f)
	{
		_world.DeltaTime = delta;

		_cmds.Merge();

		Span<EntityID> with = stackalloc EntityID[] {
			_world.Component<EcsEnabled>(),
			_world.Component<EcsSystem>(),
			0
		};

		Span<EntityID> sequence = stackalloc EntityID[3];

		if (_frame == 0)
		{
			sequence[0] = _world.Component<EcsSystemPhasePreStartup>();
			sequence[1] = _world.Component<EcsSystemPhaseOnStartup>();
			sequence[2] = _world.Component<EcsSystemPhasePostStartup>();

			for (int i = 0; i < 3; ++i)
			{
				with[^1] = sequence[i];

				_world.Query(
					with,
					Span<EntityID>.Empty,
					_cmds,
					&RunSystems
				);
			}
		}

		sequence[0] = _world.Component<EcsSystemPhasePreUpdate>();
		sequence[1] = _world.Component<EcsSystemPhaseOnUpdate>();
		sequence[2] = _world.Component<EcsSystemPhasePostUpdate>();

		for (int i = 0; i < 3; ++i)
		{
			with[^1] = sequence[i];

			_world.Query(
				with,
				Span<EntityID>.Empty,
				_cmds,
				&RunSystems
			);
		}

		_cmds.Merge();
		_frame += 1;
	}


	static unsafe void RunSystems(ref Iterator it)
	{
		var sysA = it.Field<EcsSystem>();
		var sysTickA = it.Field<EcsSystemTick>();
		var queryA = it.Field<EcsQuery>();

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
				Fetch(it.World, query.ID, it.Commands!, sys.Func);
			}
			else
			{
				sys.Func(ref it);
			}
		}
	}

	static unsafe void Fetch(World world, EntityID query, Commands cmds, delegate*<ref Iterator, void> system)
	{
		EcsAssert.Assert(world.IsAlive(query));
		EcsAssert.Assert(world.Has<EcsQueryBuilder>(query));

		ref var record = ref world._entities.Get(query);
		EcsAssert.Assert(!Unsafe.IsNullRef(ref record));

        var components = record.Archetype.Components;
		Span<EntityID> cmps = stackalloc EntityID[components.Length];

		var withIdx = 0;
		var withoutIdx = components.Length;

        for (int i = 0; i < components.Length; ++i)
		{
			ref var meta = ref components[i];

			if ((meta & EcsConst.ECS_QUERY_WITH) == EcsConst.ECS_QUERY_WITH)
			{
				cmps[withIdx++] = meta  & ~EcsConst.ECS_QUERY_WITH;
			}
			else if ((meta  & EcsConst.ECS_QUERY_WITHOUT) == EcsConst.ECS_QUERY_WITHOUT)
			{
				cmps[--withoutIdx] = meta  & ~EcsConst.ECS_QUERY_WITHOUT;
			}
		}

		var with = cmps.Slice(0, withIdx);
		var without = cmps.Slice(withoutIdx);

        if (!with.IsEmpty)
		{
			with.Sort();
			without.Sort();

			world.Query(with, without, cmds, system);
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

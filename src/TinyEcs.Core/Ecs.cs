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

	public unsafe SystemBuilder AddStartupSystem(delegate* managed<Iterator, void> system)
		=> _world.System(system)
			.Set<EcsSystemPhaseOnStartup>();

	public unsafe SystemBuilder AddSystem(delegate* managed<Iterator, void> system)
		=> _world.System(system)
			.Set<EcsSystemPhaseOnUpdate>();

	public unsafe SystemBuilder AddSystem(delegate* managed<Iterator, void> system, in QueryBuilder query)
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
		_cmds.Merge();

		if (_frame == 0)
		{
			_world.Query(
				stackalloc EntityID[] {
					_world.Component<EcsEnabled>(),
					_world.Component<EcsSystem>(),
					_world.Component<EcsSystemPhasePreStartup>()
				},
				Span<EntityID>.Empty,
				RunSystems
			);

			_world.Query(
				stackalloc EntityID[] {
					_world.Component<EcsEnabled>(),
					_world.Component<EcsSystem>(),
					_world.Component<EcsSystemPhaseOnStartup>()
				},
				Span<EntityID>.Empty,
				RunSystems
			);

			_world.Query(
				stackalloc EntityID[] {
					_world.Component<EcsEnabled>(),
					_world.Component<EcsSystem>(),
					_world.Component<EcsSystemPhasePostStartup>()
				},
				Span<EntityID>.Empty,
				RunSystems
			);
		}

		_world.Query(
			stackalloc EntityID[] {
				_world.Component<EcsEnabled>(),
				_world.Component<EcsSystem>(),
				_world.Component<EcsSystemPhasePreUpdate>()
			},
			Span<EntityID>.Empty,
			RunSystems
		);

		_world.Query(
			stackalloc EntityID[] {
				_world.Component<EcsEnabled>(),
				_world.Component<EcsSystem>(),
				_world.Component<EcsSystemPhaseOnUpdate>()
			},
			Span<EntityID>.Empty,
			RunSystems
		);

		_world.Query(
			stackalloc EntityID[] {
				_world.Component<EcsEnabled>(),
				_world.Component<EcsSystem>(),
				_world.Component<EcsSystemPhasePostUpdate>()
			},
			Span<EntityID>.Empty,
			RunSystems
		);

		_cmds.Merge();
		_frame += 1;
	}


	static unsafe void RunSystems(Iterator it)
	{
		var deltaTime = 0f;

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
				tick.Current += deltaTime;

				if (tick.Current < tick.Value)
				{
					continue;
				}

				tick.Current = 0;
			}

			if (query.ID != 0)
			{
				Fetch(it.World, query.ID, it.Commands!, sys.Func, deltaTime);
			}
			else
			{
				sys.Func(it);
			}
		}
	}

	static unsafe void Fetch(World world, EntityID query, Commands cmds, delegate*<Iterator, void> system, float deltaTime)
	{
		EcsAssert.Assert(world.IsAlive(query));
		EcsAssert.Assert(world.Has<EcsQueryBuilder>(query));

		ref var record = ref world._entities.Get(query);
		EcsAssert.Assert(!Unsafe.IsNullRef(ref record));

        var components = record.Archetype.Components;
		Span<EntityID> cmps = stackalloc EntityID[components.Length + 0];

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

			world.Query(with, without, it => {
				system(it);
			});
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



public delegate void IteratorDelegate(Iterator it);

public readonly ref struct Iterator
{
	private readonly Archetype _archetype;

	internal Iterator(Commands? commands, Archetype archetype)
	{
		Commands = commands;
		_archetype = archetype;
	}

	public Commands? Commands { get; }
	public World World => _archetype.World;
	public int Count => _archetype.Count;
	public float DeltaTime => World.DeltaTime;


	public readonly Span<T> Field<T>() where T : unmanaged
		=> _archetype.Field<T>();

	public readonly bool Has<T>() where T : unmanaged
		=> _archetype.Has<T>();

	public readonly EntityView Entity(int i)
		=> _archetype.Entity(i);

	internal readonly Span<byte> GetComponentRaw(EntityID id, int row, int count)
		=> _archetype.GetComponentRaw(id, row, count);
}

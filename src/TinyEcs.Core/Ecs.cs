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

	public unsafe EntityView StartupSystem(delegate*<ref Iterator, void> system)
		=> _world.System(system, 0, ReadOnlySpan<Term>.Empty, float.NaN)
			.Set<EcsSystemPhaseOnStartup>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system)
		=> _world.System(system, 0, ReadOnlySpan<Term>.Empty, float.NaN)
			.Set<EcsSystemPhaseOnUpdate>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system, float tick)
		=> _world.System(system, 0, ReadOnlySpan<Term>.Empty, tick)
			.Set<EcsSystemPhaseOnUpdate>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system, in QueryBuilder query)
		=> _world.System(system, query.Build(), query.Terms, float.NaN)
			.Set<EcsSystemPhaseOnUpdate>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system, in QueryBuilder query, float tick)
		=> _world.System(system, query.Build(), query.Terms, tick)
			.Set<EcsSystemPhaseOnUpdate>();

	public void SetSingleton<T>(T cmp = default) where T : unmanaged
		=> _world.SetSingleton(cmp);

	public ref T GetSingleton<T>() where T : unmanaged
		=> ref _world.GetSingleton<T>();

	[SkipLocalsInit]
	public unsafe void Step(float delta = 0.0f)
	{
		_world.DeltaTime = delta;

		_cmds.Merge();

		Span<Term> terms = stackalloc Term[] {
			new () { ID = _world.Component<EcsEnabled>(), Op = TermOp.With },
			new () { ID = _world.Component<EcsSystem>(), Op = TermOp.With },
			new () { ID = 0, Op = TermOp.With}
		};

		Span<EntityID> sequence = stackalloc EntityID[3];

		if (_frame == 0)
		{
			sequence[0] = _world.Component<EcsSystemPhasePreStartup>();
			sequence[1] = _world.Component<EcsSystemPhaseOnStartup>();
			sequence[2] = _world.Component<EcsSystemPhasePostStartup>();

			for (int i = 0; i < 3; ++i)
			{
				terms[^1].ID = sequence[i];
				_world.Query(terms, _cmds, &RunSystems);
			}
		}

		sequence[0] = _world.Component<EcsSystemPhasePreUpdate>();
		sequence[1] = _world.Component<EcsSystemPhaseOnUpdate>();
		sequence[2] = _world.Component<EcsSystemPhasePostUpdate>();

		for (int i = 0; i < 3; ++i)
		{
			terms[^1].ID = sequence[i];
			_world.Query(terms, _cmds, &RunSystems);
		}

		_cmds.Merge();
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
				it.World.Query(sys.Terms, it.Commands!, sys.Func);
			}
			else
			{
				sys.Func(ref it);
			}
		}
	}
}

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
		EcsAssert.Assert(_world.Exists(id));

		return new EntityView(_world, id);
	}

	public void Print() => _world.PrintGraph();

	public CommandEntityView Spawn()
		=> _cmds.Spawn();

	public ref EcsComponent Component<T>() where T : unmanaged
		=> ref _world.Component<T>();

	public EntityID Component<TKind, TTarget>()
	where TKind : unmanaged
	where TTarget : unmanaged
		=> _world.Pair<TKind, TTarget>();

	public void Despawn(EntityID entity)
		=> _cmds.Despawn(entity);

	public QueryBuilder Query()
		=> _world.Query();

	public unsafe EntityView StartupSystem(delegate*<ref Iterator, void> system)
		=> _world.System(system, 0, ReadOnlySpan<Term>.Empty, float.NaN)
			.SetTag<EcsSystemPhaseOnStartup>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system)
		=> _world.System(system, 0, ReadOnlySpan<Term>.Empty, float.NaN)
			.SetTag<EcsSystemPhaseOnUpdate>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system, float tick)
		=> _world.System(system, 0, ReadOnlySpan<Term>.Empty, tick)
			.SetTag<EcsSystemPhaseOnUpdate>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system, in QueryBuilder query)
		=> _world.System(system, query.Build(), query.Terms, float.NaN)
			.SetTag<EcsSystemPhaseOnUpdate>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system, in QueryBuilder query, float tick)
		=> _world.System(system, query.Build(), query.Terms, tick)
			.SetTag<EcsSystemPhaseOnUpdate>();

	public void SetSingleton<T>(T cmp = default) where T : unmanaged
		=> _world.SetSingleton(cmp);

	public ref T GetSingleton<T>() where T : unmanaged
		=> ref _world.GetSingleton<T>();

	public unsafe void Step(float delta = 0.0f)
	{
		_world.DeltaTime = delta;

		_cmds.Merge();

		if (_frame == 0)
		{
			_world.RunPhase(_world.Tag<EcsSystemPhasePreStartup>().ID, _cmds);
			_world.RunPhase(_world.Tag<EcsSystemPhaseOnStartup>().ID, _cmds);
			_world.RunPhase(_world.Tag<EcsSystemPhasePostStartup>().ID, _cmds);
		}

		_world.RunPhase(_world.Tag<EcsSystemPhasePreUpdate>().ID, _cmds);
		_world.RunPhase(_world.Tag<EcsSystemPhaseOnUpdate>().ID, _cmds);
		_world.RunPhase(_world.Tag<EcsSystemPhasePostUpdate>().ID, _cmds);

		_cmds.Merge();
		_frame += 1;
	}
}

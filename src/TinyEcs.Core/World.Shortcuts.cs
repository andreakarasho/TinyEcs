namespace TinyEcs;

public sealed partial class World
{
	public void Set<TKind, TTarget>(EntityID entity)
	where TKind : unmanaged, IComponentStub
	where TTarget : unmanaged, IComponentStub
	{
		Set(entity, Component<TKind>().ID, Component<TKind>().ID);
	}

	public void Set<TKind>(EntityID entity, EntityID target)
	where TKind : unmanaged, ITag
	{
		Set(entity, Component<TKind>().ID, target);
	}

	public void Set<T>(EntityID entity)
	where T : unmanaged, ITag
	{
		ref var cmp = ref Component<T>();

		EcsAssert.Assert(cmp.Size <= 0);

		Set(entity, ref cmp, ReadOnlySpan<byte>.Empty);
	}

	[SkipLocalsInit]
	public unsafe void Set<T>(EntityID entity, T component = default)
	where T : unmanaged, IComponent
	{
		ref var cmp = ref Component<T>();

		EcsAssert.Assert(cmp.Size > 0);

		Set
		(
			entity,
			ref cmp,
			new ReadOnlySpan<byte>(&component, cmp.Size)
		);
	}

	public void Unset<T>(EntityID entity)
	where T : unmanaged, IComponentStub
		=> DetachComponent(entity, ref Component<T>());

	public bool Has<T>(EntityID entity)
	where T : unmanaged, IComponentStub
		=> Has(entity, ref Component<T>());

	public bool Has<TKind>(EntityID entity, EntityID target)
	where TKind : unmanaged, ITag
		=> Has(entity, Component<TKind>().ID, target);

	public bool Has<TKind, TTarget>(EntityID entity)
	where TKind : unmanaged, ITag
	where TTarget : unmanaged, IComponentStub
		=> Has(entity, Component<TKind>().ID, Component<TKind>().ID);

	public ref T Get<T>(EntityID entity)
	where T : unmanaged, IComponent
	{
		ref var record = ref GetRecord(entity);
		var raw = record.Archetype.ComponentData<T>(record.Row, 1);

		EcsAssert.Assert(!raw.IsEmpty);

		return ref MemoryMarshal.GetReference(raw);
	}

	[SkipLocalsInit]
	public void SetSingleton<T>(T component = default)
	where T : unmanaged, IComponent
		=> Set(Component<T>().ID, component);

	public ref T GetSingleton<T>()
	where T : unmanaged, IComponent
		=> ref Get<T>(Component<T>().ID);

	public unsafe EntityView StartupSystem(delegate*<ref Iterator, void> system)
		=> System(system, 0, ReadOnlySpan<Term>.Empty, float.NaN)
			.Set<EcsPhase, EcsSystemPhaseOnStartup>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system)
		=> System(system, 0, ReadOnlySpan<Term>.Empty, float.NaN)
			.Set<EcsPhase, EcsSystemPhaseOnUpdate>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system, float tick)
		=> System(system, 0, ReadOnlySpan<Term>.Empty, tick)
			.Set<EcsPhase, EcsSystemPhaseOnUpdate>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system, in QueryBuilder query)
		=> System(system, query.Build(), query.Terms, float.NaN)
			.Set<EcsPhase, EcsSystemPhaseOnUpdate>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system, in QueryBuilder query, float tick)
		=> System(system, query.Build(), query.Terms, tick)
			.Set<EcsPhase, EcsSystemPhaseOnUpdate>();

	public void RunPhase<TPhase>() where TPhase : unmanaged, ITag
		=> RunPhase(Pair<EcsPhase, TPhase>());

	public void EmitEvent<TEvent>(EntityID entity, EntityID component)
	where TEvent : unmanaged, IEvent
	{
		EmitEvent(Component<TEvent>().ID, entity, component);
	}

	public void EmitEvent<TEvent, TComponent>(EntityID entity)
	where TEvent : unmanaged, IEvent
	where TComponent : unmanaged, IComponentStub
	{
		EmitEvent(Component<TEvent>().ID, entity, Component<TComponent>().ID);
	}

	public void EmitEvent<TEvent, TKind, TTarget>(EntityID entity)
	where TEvent : unmanaged, IEvent
	where TKind : unmanaged, ITag
	where TTarget : unmanaged, IComponentStub
	{
		EmitEvent(Component<TEvent>().ID, entity, Pair<TKind, TTarget>());
	}

	public void EmitEvent<TEvent, TKind>(EntityID entity, EntityID target)
	where TEvent : unmanaged, IEvent
	where TKind : unmanaged, ITag
	{
		EmitEvent(Component<TEvent>().ID, entity, Pair<TKind>(target));
	}
}

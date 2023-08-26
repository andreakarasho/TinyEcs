namespace TinyEcs;

public sealed partial class World
{
	public void Set<TKind, TTarget>(EcsID entity)
	where TKind : unmanaged, IComponentStub
	where TTarget : unmanaged, IComponentStub
	{
		Set(entity, Component<TKind>().ID, Component<TKind>().ID);
	}

	public void Set<TKind>(EcsID entity, EcsID target)
	where TKind : unmanaged, ITag
	{
		Set(entity, Component<TKind>().ID, target);
	}

	public void Set<T>(EcsID entity)
	where T : unmanaged, ITag
	{
		ref var cmp = ref Component<T>();

		EcsAssert.Assert(cmp.Size <= 0);

		Set(entity, ref cmp, ReadOnlySpan<byte>.Empty);
	}

	[SkipLocalsInit]
	public unsafe void Set<T>(EcsID entity, T component = default)
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

	public void Unset<T>(EcsID entity)
	where T : unmanaged, IComponentStub
		=> DetachComponent(entity, ref Component<T>());

	public bool Has<T>(EcsID entity)
	where T : unmanaged, IComponentStub
		=> Has(entity, ref Component<T>());

	public bool Has<TKind>(EcsID entity, EcsID target)
	where TKind : unmanaged, ITag
		=> Has(entity, Component<TKind>().ID, target);

	public bool Has<TKind, TTarget>(EcsID entity)
	where TKind : unmanaged, ITag
	where TTarget : unmanaged, IComponentStub
		=> Has(entity, Component<TKind>().ID, Component<TKind>().ID);

	public ref T Get<T>(EcsID entity)
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

	public void RunPhase<TPhase>() where TPhase : unmanaged, ITag
		=> RunPhase(Pair<EcsPhase, TPhase>());

	public void EmitEvent<TEvent>(EcsID entity, EcsID component)
	where TEvent : unmanaged, IEvent
	{
		EmitEvent(Component<TEvent>().ID, entity, component);
	}

	public void EmitEvent<TEvent, TComponent>(EcsID entity)
	where TEvent : unmanaged, IEvent
	where TComponent : unmanaged, IComponentStub
	{
		EmitEvent(Component<TEvent>().ID, entity, Component<TComponent>().ID);
	}

	public void EmitEvent<TEvent, TKind, TTarget>(EcsID entity)
	where TEvent : unmanaged, IEvent
	where TKind : unmanaged, ITag
	where TTarget : unmanaged, IComponentStub
	{
		EmitEvent(Component<TEvent>().ID, entity, Pair<TKind, TTarget>());
	}

	public void EmitEvent<TEvent, TKind>(EcsID entity, EcsID target)
	where TEvent : unmanaged, IEvent
	where TKind : unmanaged, ITag
	{
		EmitEvent(Component<TEvent>().ID, entity, Pair<TKind>(target));
	}
}

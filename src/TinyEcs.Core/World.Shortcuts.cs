namespace TinyEcs;

public sealed partial class World
{
	public void SetPair<TKind, TTarget>(EntityID entity) where TKind : unmanaged where TTarget : unmanaged
	{
		SetPair(entity, Tag<TKind>().ID, Tag<TKind>().ID);
	}

	public void SetPair<TKind>(EntityID entity, EntityID target) where TKind : unmanaged
	{
		SetPair(entity, Tag<TKind>().ID, target);
	}

	public void SetTag<T>(EntityID entity) where T : unmanaged
	{
		ref var cmp = ref Tag<T>();

		EcsAssert.Assert(cmp.Size <= 0);

		Set(entity, ref cmp, ReadOnlySpan<byte>.Empty);
	}

	[SkipLocalsInit]
	public unsafe void Set<T>(EntityID entity, T component = default) where T : unmanaged
	{
		ref var cmp = ref Component<T>(false);

		EcsAssert.Assert(cmp.Size > 0);

		Set
		(
			entity,
			ref cmp,
			new ReadOnlySpan<byte>(&component, cmp.Size)
		);
	}

	public void Unset<T>(EntityID entity) where T : unmanaged
		=> DetachComponent(entity, ref Component<T>());

	public bool Has<T>(EntityID entity) where T : unmanaged
		=> Has(entity, ref Component<T>());

	public bool Has<TKind>(EntityID entity, EntityID target) where TKind : unmanaged
		=> Has(entity, Tag<TKind>().ID, target);

	public bool Has<TKind, TTarget>(EntityID entity) where TKind : unmanaged where TTarget : unmanaged
		=> Has(entity, Tag<TKind>().ID, Tag<TKind>().ID);

	public ref T Get<T>(EntityID entity) where T : unmanaged
	{
		ref var record = ref GetRecord(entity);
		var raw = record.Archetype.ComponentData<T>(record.Row, 1);

		EcsAssert.Assert(!raw.IsEmpty);

		return ref MemoryMarshal.GetReference(raw);
	}

	[SkipLocalsInit]
	public void SetSingleton<T>(T component = default) where T : unmanaged
		=> Set(Component<T>().ID, component);

	public ref T GetSingleton<T>() where T : unmanaged
		=> ref Get<T>(Component<T>().ID);


	public unsafe EntityView StartupSystem(delegate*<ref Iterator, void> system)
		=> System(system, 0, ReadOnlySpan<Term>.Empty, float.NaN)
			.SetPair<EcsPhase, EcsSystemPhaseOnStartup>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system)
		=> System(system, 0, ReadOnlySpan<Term>.Empty, float.NaN)
			.SetPair<EcsPhase, EcsSystemPhaseOnUpdate>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system, float tick)
		=> System(system, 0, ReadOnlySpan<Term>.Empty, tick)
			.SetPair<EcsPhase, EcsSystemPhaseOnUpdate>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system, in QueryBuilder query)
		=> System(system, query.Build(), query.Terms, float.NaN)
			.SetPair<EcsPhase, EcsSystemPhaseOnUpdate>();

	public unsafe EntityView System(delegate*<ref Iterator, void> system, in QueryBuilder query, float tick)
		=> System(system, query.Build(), query.Terms, tick)
			.SetPair<EcsPhase, EcsSystemPhaseOnUpdate>();

	public void RunPhase<TPhase>() where TPhase : unmanaged
		=> RunPhase(Pair<EcsPhase, TPhase>());
}

namespace TinyEcs;

public sealed partial class World
{
	public void SetPair<TKind, TTarget>(EntityID entity) where TKind : unmanaged where TTarget : unmanaged
	{
		SetPair(entity, Component<TKind>(true).ID, Component<TTarget>(true).ID);
	}

	public void SetPair<TKind>(EntityID entity, EntityID target) where TKind : unmanaged
	{
		SetPair(entity, Component<TKind>(true).ID, target);
	}

	public void SetTag<T>(EntityID entity) where T : unmanaged
	{
		ref var cmp = ref Component<T>(true);

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
		=> Has(entity, Component<TKind>().ID, target);

	public bool Has<TKind, TTarget>(EntityID entity) where TKind : unmanaged where TTarget : unmanaged
		=> Has(entity, Component<TKind>().ID, Component<TTarget>().ID);

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
}

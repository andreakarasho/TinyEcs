namespace TinyEcs;

#if NET5_0_OR_GREATER
[SkipLocalsInit]
#endif
[StructLayout(LayoutKind.Sequential)]
public readonly struct EntityView : IEquatable<EntityID>, IEquatable<EntityView>
{
	public readonly EntityID ID;
	public readonly World World;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal EntityView(World world, EntityID id)
	{
		World = world;
		ID = id;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Equals(ulong other)
	{
		return ID == other;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Equals(EntityView other)
	{
		return ID == other.ID /*&& World?.ID == other.World?.ID*/;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Set<T>(T component = default) where T : unmanaged
	{
		World.Set(ID, component);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Add(EntityID first, EntityID second)
	{
		var id = IDOp.Pair(first, second);
		World.Set(ID, id, stackalloc byte[1]);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Unset<T>() where T : unmanaged
	{
		World.Unset<T>(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Enable()
	{
		World.Set<EcsEnabled>(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Disable()
	{
		World.Unset<EcsEnabled>(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref T Get<T>() where T : unmanaged
		=> ref World.Get<T>(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Has<T>() where T : unmanaged
		=> World.Has<T>(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Has<TKind, TTarget>()
		where TKind : unmanaged
		where TTarget : unmanaged
	{
		var world = World;
		var id = IDOp.Pair(world.Component<TKind>(), world.Component<TTarget>());

		return world.Has(ID, id);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Despawn()
		=> World.Despawn(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool IsAlive()
		=> World.IsAlive(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool IsEnabled()
		=> Has<EcsEnabled>();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool IsEntity()
		=> (ID & EcsConst.ECS_ID_FLAGS_MASK) == 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool IsPair()
		=> IDOp.IsPair(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityID First()
		=> IDOp.GetPairFirst(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityID Second()
		=> IDOp.GetPairSecond(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Each(Action<EntityView> action)
	{
		ref var record = ref World._entities.Get(ID);
		EcsAssert.Assert(!Unsafe.IsNullRef(ref record));

		for (int i = 0; i < record.Archetype.Components.Length; ++i)
		{
			action(new EntityView(World, record.Archetype.Components[i]));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator EntityID(in EntityView d) => d.ID;


	public static readonly EntityView Invalid = new(null, 0);
}

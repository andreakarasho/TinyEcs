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
		return ID == other.ID;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Tag<T>() where T : unmanaged
	{
		World.Tag<T>(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Set<T>(T component = default) where T : unmanaged
	{
		World.Set(ID, component);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Pair<TKind, TTarget>() where TKind : unmanaged where TTarget : unmanaged
	{
		return Pair(World.Component<TKind>(true).ID, World.Component<TTarget>(true).ID);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Pair(EntityID first, EntityID second)
	{
		World.Pair(ID, first, second);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Unset<T>() where T : unmanaged
	{
		World.Unset<T>(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Unset<TKind, TTarget>() where TKind : unmanaged where TTarget : unmanaged
	{
		return Unset(World.Component<TKind>(true).ID, World.Component<TTarget>(true).ID);
	}

	public readonly EntityView Unset<TKind>(EntityID target) where TKind : unmanaged
	{
		return Unset(World.Component<TKind>(true).ID, target);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Unset(EntityID first, EntityID second)
	{
		var id = IDOp.Pair(first, second);
		if (World.IsAlive(id) && World.Has<EcsComponent>(id))
		{
			ref var cmp2 = ref World.Get<EcsComponent>(id);
			World.DetachComponent(ID, ref cmp2);
			return this;
		}

		var cmp = new EcsComponent(id, 0);
		World.DetachComponent(ID, ref cmp);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Enable()
	{
		World.Tag<EcsEnabled>(ID);
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
		var id = world.Component<TKind, TTarget>();
		if (world.IsAlive(id) && world.Has<EcsComponent>(id))
		{
			ref var cmp2 = ref world.Get<EcsComponent>(id);
			return world.Has(id, ref cmp2);
		}

		var cmp = new EcsComponent(id, 0);
		return world.Has(ID, ref cmp);
	}

	public readonly EntityView ChildOf(EntityID parent)
	{
		World.Pair(ID, World.Component<EcsChild>(true).ID, parent);
		World.Tag<EcsParent>(parent);
		return this;
	}

	public readonly void EachChildren(Action<EntityView> action)
	{
		EcsAssert.Assert(World.Has<EcsParent>(ID));

		World.Query()
			.With<EcsChild>(ID)
			.Iterate((ref Iterator it) => {
				for (int i = 0, count = it.Count; i < count; ++i)
					action(it.Entity(i));
			});
	}

	public readonly void ClearChildren()
	{
		var id = World.Component<EcsChild>().ID;
		var myID = ID; // lol
		EachChildren(v => v.Unset(id, myID));
		World.Unset<EcsParent>(ID);
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
		ref var record = ref World.GetRecord(ID);

		for (int i = 0; i < record.Archetype.ComponentInfo.Length; ++i)
		{
			action(new EntityView(World, record.Archetype.ComponentInfo[i].ID));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator EntityID(in EntityView d) => d.ID;


	public static readonly EntityView Invalid = new(null, 0);
}

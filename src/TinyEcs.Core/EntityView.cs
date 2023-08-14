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
	public readonly EntityView SetTag<T>() where T : unmanaged
	{
		World.SetTag<T>(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView SetTag(EntityID id)
	{
		World.SetTag(ID, id);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Set<T>(T component = default) where T : unmanaged
	{
		World.Set(ID, component);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView SetPair<TKind, TTarget>() where TKind : unmanaged where TTarget : unmanaged
	{
		return SetPair(World.Tag<TKind>().ID, World.Tag<TTarget>().ID);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView SetPair<TKind>(EntityID target) where TKind : unmanaged
	{
		return SetPair(World.Tag<TKind>().ID, target);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView SetPair(EntityID first, EntityID second)
	{
		World.SetPair(ID, first, second);
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
		return Unset(World.Tag<TKind>().ID, World.Tag<TTarget>().ID);
	}

	public readonly EntityView Unset<TKind>(EntityID target) where TKind : unmanaged
	{
		return Unset(World.Tag<TKind>().ID, target);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Unset(EntityID first, EntityID second)
	{
		var id = IDOp.Pair(first, second);
		if (World.Exists(id) && World.Has<EcsComponent>(id))
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
		World.SetTag<EcsEnabled>(ID);
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
		var id = world.Pair<TKind, TTarget>();
		if (world.Exists(id) && world.Has<EcsComponent>(id))
		{
			ref var cmp2 = ref world.Get<EcsComponent>(id);
			return world.Has(id, ref cmp2);
		}

		var cmp = new EcsComponent(id, 0);
		return world.Has(ID, ref cmp);
	}

	public readonly EntityView ChildOf(EntityID parent)
	{
		World.SetPair<EcsChildOf>(ID, parent);
		return this;
	}

	public readonly void Children(Action<EntityView> action)
	{
		World.Query()
			.With<EcsChildOf>(ID)
			.Iterate((ref Iterator it) => {
				for (int i = 0, count = it.Count; i < count; ++i)
					action(it.Entity(i));
			});
	}

	public readonly void ClearChildren()
	{
		var id = World.Component<EcsChildOf>().ID;
		var myID = ID; // lol
		Children(v => v.Unset(id, myID));
	}

	public readonly EntityView Parent()
	{
		return new (World, World.GetParent(ID));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Despawn()
		=> World.Despawn(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Exists()
		=> World.Exists(ID);

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

	public readonly ReadOnlySpan<EcsComponent> Type()
		=> World.GetType(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator EntityID(in EntityView d) => d.ID;


	public static readonly EntityView Invalid = new(null, 0);
}

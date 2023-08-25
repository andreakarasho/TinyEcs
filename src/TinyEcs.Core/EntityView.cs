namespace TinyEcs;

#if NET5_0_OR_GREATER
[SkipLocalsInit]
#endif
[StructLayout(LayoutKind.Sequential)]
public readonly struct EntityView<TContext> : IEquatable<EcsID>, IEquatable<EntityView<TContext>>
{
	public readonly EcsID ID;
	public readonly World<TContext> World;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal EntityView(World<TContext> world, EcsID id)
	{
		World = world;
		ID = id;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Equals(EcsID other)
	{
		return ID == other;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Equals(EntityView<TContext> other)
	{
		return ID == other.ID;
	}

	public readonly EntityView<TContext> Set<T>() where T : unmanaged, ITag
	{
		World.Set<T>(ID);
		return this;
	}

	public readonly EntityView<TContext> Set(EcsID id)
	{
		World.Set(ID, id);
		return this;
	}

	public readonly EntityView<TContext> Set<T>(T component = default)
	where T : unmanaged, IComponent
	{
		World.Set(ID, component);
		return this;
	}

	public readonly EntityView<TContext> Set<TKind, TTarget>()
	where TKind : unmanaged, ITag
	where TTarget : unmanaged, IComponentStub
	{
		return Set(World.Component<TKind>().ID, World.Component<TTarget>().ID);
	}

	public readonly EntityView<TContext> Set<TKind>(EcsID target)
	where TKind : unmanaged, ITag
	{
		return Set(World.Component<TKind>().ID, target);
	}

	public readonly EntityView<TContext> Set(EcsID first, EcsID second)
	{
		World.Set(ID, first, second);
		return this;
	}

	public readonly EntityView<TContext> Unset<T>()
	where T : unmanaged, IComponentStub
	{
		World.Unset<T>(ID);
		return this;
	}

	public readonly EntityView<TContext> Unset<TKind, TTarget>()
	where TKind : unmanaged, ITag
	where TTarget : unmanaged, IComponentStub
	{
		return Unset(World.Component<TKind>().ID, World.Component<TTarget>().ID);
	}

	public readonly EntityView<TContext> Unset<TKind>(EcsID target)
	where TKind : unmanaged, ITag
	{
		return Unset(World.Component<TKind>().ID, target);
	}

	public readonly EntityView<TContext> Unset(EcsID first, EcsID second)
	{
		var id = IDOp.Pair(first, second);
		var cmp = new EcsComponent(id, 0);
		World.DetachComponent(ID, ref cmp);
		return this;
	}

	public readonly EntityView<TContext> Enable()
	{
		World.Unset<EcsDisabled>(ID);
		return this;
	}

	public readonly EntityView<TContext> Disable()
	{
		World.Set<EcsDisabled>(ID);
		return this;
	}

	public readonly ref T Get<T>() where T : unmanaged, IComponent
		=> ref World.Get<T>(ID);

	public readonly bool Has<T>() where T : unmanaged, IComponentStub
		=> World.Has<T>(ID);

	public readonly bool Has<TKind, TTarget>()
		where TKind : unmanaged, ITag
		where TTarget : unmanaged, IComponentStub
	{
		var world = World;
		var id = world.Pair<TKind, TTarget>();
		var cmp = new EcsComponent(id, 0);
		return world.Has(ID, ref cmp);
	}

	public readonly EntityView<TContext> ChildOf(EcsID parent)
	{
		World.Set<EcsChildOf>(ID, parent);
		return this;
	}

	public readonly void Children(Action<EntityView<TContext>> action)
	{
		World.Query()
			.With<EcsChildOf>(ID)
			.Iterate((ref Iterator<TContext> it) => {
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

	public readonly EntityView<TContext> Parent()
	{
		return World.Entity(World.GetParent(ID));
	}

	public readonly void Delete()
		=> World.Delete(ID);

	public readonly bool Exists()
		=> World.Exists(ID);

	public readonly bool IsEnabled()
		=> !Has<EcsDisabled>();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool IsEntity()
		=> (ID & EcsConst.ECS_ID_FLAGS_MASK) == 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool IsPair()
		=> IDOp.IsPair(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EcsID First()
		=> IDOp.GetPairFirst(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EcsID Second()
		=> IDOp.GetPairSecond(ID);

	public readonly void Each(Action<EntityView<TContext>> action)
	{
		ref var record = ref World.GetRecord(ID);

		for (int i = 0; i < record.Archetype.ComponentInfo.Length; ++i)
		{
			action(World.Entity(record.Archetype.ComponentInfo[i].ID));
		}
	}

	public readonly ReadOnlySpan<EcsComponent> Type()
		=> World.GetType(ID);





	public static readonly EntityView<TContext> Invalid = new(null, 0);


	public unsafe readonly EntityView<TContext> System
	(
		delegate*<ref Iterator<TContext>, void> callback,
		params Term[] terms
	) => System(callback, float.NaN, terms);

	public unsafe readonly EntityView<TContext> System
	(
		delegate*<ref Iterator<TContext>, void> callback,
		float tick,
		params Term[] terms
	)
	{
		EcsID query = terms.Length > 0 ?
			World.New()
				.Set<EcsPanic, EcsDelete>() : 0;

		Array.Sort(terms);

		Set(new EcsSystem<TContext>
		(
			callback,
			query,
			terms,
			tick
		));

		return Set<EcsPanic, EcsDelete>();
	}


	public unsafe readonly EntityView<TContext> Event
	(
		delegate*<ref Iterator<TContext>, void> callback
	)
	{

		return this;
	}

	public readonly EntityView<TContext> Component<T>()
	where T : unmanaged, IComponentStub
	{
		World.Component<T>(ID);
		return this;
	}

	public override int GetHashCode()
	{
		return ID.GetHashCode();
	}

	public static implicit operator EcsID(EntityView<TContext> d) => d.ID;
	public static implicit operator Term(EntityView<TContext> d) => Term.With(d.ID);

	public static Term operator !(EntityView<TContext> id) => Term.Without(id.ID);
	public static Term operator -(EntityView<TContext> id) => Term.Without(id.ID);
	public static Term operator +(EntityView<TContext> id) => Term.With(id.ID);
}

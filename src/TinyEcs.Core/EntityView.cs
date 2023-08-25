namespace TinyEcs;

#if NET5_0_OR_GREATER
[SkipLocalsInit]
#endif
[StructLayout(LayoutKind.Sequential)]
public readonly struct EntityView<TContext> : IEquatable<EntityID>, IEquatable<EntityView<TContext>>
{
	public readonly EntityID ID;
	public readonly World<TContext> World;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal EntityView(World<TContext> world, EntityID id)
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
	public readonly bool Equals(EntityView<TContext> other)
	{
		return ID == other.ID;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView<TContext> Set<T>() where T : unmanaged, ITag
	{
		World.Set<T>(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView<TContext> Set(EntityID id)
	{
		World.Set(ID, id);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView<TContext> Set<T>(T component = default)
	where T : unmanaged, IComponent
	{
		World.Set(ID, component);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView<TContext> Set<TKind, TTarget>()
	where TKind : unmanaged, ITag
	where TTarget : unmanaged, IComponentStub
	{
		return Set(World.Component<TKind>().ID, World.Component<TTarget>().ID);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView<TContext> Set<TKind>(EntityID target)
	where TKind : unmanaged, ITag
	{
		return Set(World.Component<TKind>().ID, target);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView<TContext> Set(EntityID first, EntityID second)
	{
		World.Set(ID, first, second);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView<TContext> Unset<T>()
	where T : unmanaged, IComponentStub
	{
		World.Unset<T>(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView<TContext> Unset<TKind, TTarget>()
	where TKind : unmanaged, ITag
	where TTarget : unmanaged, IComponentStub
	{
		return Unset(World.Component<TKind>().ID, World.Component<TTarget>().ID);
	}

	public readonly EntityView<TContext> Unset<TKind>(EntityID target)
	where TKind : unmanaged, ITag
	{
		return Unset(World.Component<TKind>().ID, target);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView<TContext> Unset(EntityID first, EntityID second)
	{
		var id = IDOp.Pair(first, second);
		var cmp = new EcsComponent(id, 0);
		World.DetachComponent(ID, ref cmp);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView<TContext> Enable()
	{
		World.Set<EcsEnabled>(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView<TContext> Disable()
	{
		World.Unset<EcsEnabled>(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref T Get<T>() where T : unmanaged, IComponent
		=> ref World.Get<T>(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Has<T>() where T : unmanaged, IComponentStub
		=> World.Has<T>(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Has<TKind, TTarget>()
		where TKind : unmanaged, ITag
		where TTarget : unmanaged, IComponentStub
	{
		var world = World;
		var id = world.Pair<TKind, TTarget>();
		var cmp = new EcsComponent(id, 0);
		return world.Has(ID, ref cmp);
	}

	public readonly EntityView<TContext> ChildOf(EntityID parent)
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Delete()
		=> World.Delete(ID);

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator EntityID(in EntityView<TContext> d) => d.ID;


	public static readonly EntityView<TContext> Invalid = new(null, 0);

	public unsafe readonly EntityView<TContext> System
	(
		delegate*<ref Iterator<TContext>, void> callback,
		SystemDescription<TContext> description
	)
	{
		if (description.Query == 0)
			description.Query = World.New()
				.Set<EcsPanic, EcsDelete>();

		description.Terms.Sort();

		Set(new EcsSystem<TContext>
		(
			callback,
			description.Query,
			description.Terms,
			description.Tick
		));

		return Set<EcsPanic, EcsDelete>();
	}


	[SkipLocalsInit]
	public unsafe readonly EntityView<TContext> System
	(
		delegate*<ref Iterator<TContext>, void> callback,
		Span<EntityID> with,
		Span<EntityID> without,
		float tick
	)
	{
		var query = World.New()
			.Set<EcsPanic, EcsDelete>();

		Span<Term> terms = stackalloc Term[with.Length + without.Length];
		var termsWith = terms.Slice(0, with.Length);
		var termsWithout = terms.Slice(with.Length);

		for (int i = 0; i < with.Length; ++i)
			termsWith[i] = Term.With(with[i]);

		for (int i = 0; i < without.Length; ++i)
			termsWithout[i] = Term.Without(without[i]);

		terms.Sort();

		return Set(new EcsSystem<TContext>
		(
			callback,
			query,
			terms,
			tick
		)).Set<EcsPanic, EcsDelete>();
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

	// public static implicit operator Term(EntityView<TContext> entity)
	// 	=> new() { ID = entity.ID, Op = TermOp.With };

	public static implicit operator Term(in EntityView<TContext> entity)
		=> new() { ID = entity.ID, Op = TermOp.With };
}


// public readonly ref struct Without
// {
// 	private readonly EntityID _id;

// 	public Without(EntityID id)
// 	{
// 		_id = id;
// 	}

// 	public static implicit operator Term(Without entity)
// 		=> new() { ID = entity._id, Op = TermOp.Without };
// }


public unsafe ref struct SystemDescription<TContext>
{
	public SystemDescription() => this = default;
	public SystemDescription(params Term[] terms) { this = default; Terms = terms; }

	public EntityID Query = 0;
	public Span<Term> Terms = Span<Term>.Empty;
	public float Tick = float.NaN;
}

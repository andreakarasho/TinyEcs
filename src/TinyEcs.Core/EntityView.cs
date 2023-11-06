namespace TinyEcs;

#if NET5_0_OR_GREATER
[SkipLocalsInit]
#endif
[StructLayout(LayoutKind.Sequential)]
public unsafe readonly struct EntityView : IEquatable<EcsID>, IEquatable<EntityView>
{
    public static readonly EntityView Invalid = new(null, 0);

    public readonly EcsID ID;
    public readonly World World;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EntityView(World world, EcsID id)
    {
        World = world;
        ID = id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(EcsID other) => ID == other;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(EntityView other) => ID == other.ID;

    public readonly override int GetHashCode() => ID.GetHashCode();

    public readonly override bool Equals(object? obj) => obj is EntityView ent && Equals(ent);

    public readonly EntityView Set<T>() where T : unmanaged
    {
        World.Set<T>(ID);
        return this;
    }

    public readonly EntityView Set(EcsID id)
    {
        World.Set(ID, id);
        return this;
    }

    public readonly EntityView Set<T>(T component) where T : unmanaged
    {
        World.Set(ID, component);
        return this;
    }

    public readonly EntityView Set<TKind, TTarget>()
        where TKind : unmanaged
        where TTarget : unmanaged
    {
        return Set(World.Entity<TKind>(), World.Entity<TTarget>());
    }

    public readonly EntityView Set<TKind>(EcsID target) where TKind : unmanaged
    {
        return Set(World.Entity<TKind>(), target);
    }

    public readonly EntityView Set(EcsID first, EcsID second)
    {
        World.Set(ID, first, second);
        return this;
    }

    public readonly EntityView Unset<T>() where T : unmanaged
    {
        World.Unset<T>(ID);
        return this;
    }

    public readonly EntityView Unset<TKind, TTarget>()
        where TKind : unmanaged
        where TTarget : unmanaged
    {
        return Unset(World.Entity<TKind>(), World.Entity<TTarget>());
    }

    public readonly EntityView Unset<TKind>(EcsID target) where TKind : unmanaged
    {
        return Unset(World.Entity<TKind>(), target);
    }

    public readonly EntityView Unset(EcsID first, EcsID second)
    {
        var id = IDOp.Pair(first, second);
        var cmp = new EcsComponent(id, 0);
        World.DetachComponent(ID, ref cmp);
        return this;
    }

    public readonly EntityView Enable()
    {
        World.Unset<EcsDisabled>(ID);
        return this;
    }

    public readonly EntityView Disable()
    {
        World.Set<EcsDisabled>(ID);
        return this;
    }

    public readonly ReadOnlySpan<EcsComponent> Type() => World.GetType(ID);

    public readonly ref T Get<T>() where T : unmanaged => ref World.Get<T>(ID);

    public readonly bool Has<T>() where T : unmanaged => World.Has<T>(ID);

    public readonly bool Has<TKind, TTarget>()
        where TKind : unmanaged
        where TTarget : unmanaged
    {
        return World.Has(ID, World.Entity<TKind>(), World.Entity<TTarget>());
    }

    public readonly EntityView ChildOf(EcsID parent)
    {
        World.Set<EcsChildOf>(ID, parent);
        return this;
    }

    public readonly void Children(Action<EntityView> action)
    {
        World
            .Query()
            .With<EcsChildOf>(ID)
            .Iterate(
                (ref Iterator it) =>
                {
                    for (int i = 0, count = it.Count; i < count; ++i)
                        action(it.Entity(i));
                }
            );
    }

    public readonly void ClearChildren()
    {
        var id = World.Entity<EcsChildOf>();
        var myID = ID; // lol
        Children(v => v.Unset(id, myID));
    }

    public readonly EntityView Parent() => World.Entity(World.GetParent(ID));

    public readonly void Delete() => World.Delete(ID);

    public readonly bool Exists() => World.Exists(ID);

    public readonly bool IsEnabled() => !Has<EcsDisabled>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsEntity() => (ID & EcsConst.ECS_ID_FLAGS_MASK) == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsPair() => IDOp.IsPair(ID);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EcsID First() => IDOp.GetPairFirst(ID);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EcsID Second() => IDOp.GetPairSecond(ID);

    public readonly void Each(Action<EntityView> action)
    {
        ref var record = ref World.GetRecord(ID);

        for (int i = 0; i < record.Archetype.ComponentInfo.Length; ++i)
        {
            action(World.Entity(record.Archetype.ComponentInfo[i].ID));
        }
    }

    public unsafe readonly EntityView System(
        delegate* <ref Iterator, void> callback,
        params Term[] terms
    ) => System(callback, float.NaN, terms);

    public unsafe readonly EntityView System(
        delegate* <ref Iterator, void> callback,
        float tick,
        params Term[] terms
    )
    {
        EcsID query = terms.Length > 0 ? World.Entity().Set<EcsPanic, EcsDelete>() : 0;

        Array.Sort(terms);

        Set(new EcsSystem(callback, query, terms, tick));

        return Set<EcsPanic, EcsDelete>();
    }

    public unsafe readonly EntityView Event(delegate* <ref Iterator, void> callback)
    {
        return this;
    }

    public static implicit operator EcsID(EntityView d) => d.ID;

    public static implicit operator Term(EntityView d) => Term.With(d.ID);

    public static Term operator !(EntityView id) => Term.Without(id.ID);

    public static Term operator -(EntityView id) => Term.Without(id.ID);

    public static Term operator +(EntityView id) => Term.With(id.ID);
}

namespace TinyEcs;

public sealed partial class World
{
    //public void Set<TKind, TTarget>(EcsID entity)
    //    where TKind : unmanaged
    //    where TTarget : unmanaged
    //{
    //    Set(entity, Entity<TKind>(), Entity<TKind>());
    //}

    //public void Set<TKind>(EcsID entity, EcsID target) where TKind : unmanaged
    //{
    //    Set(entity, Entity<TKind>(), target);
    //}

    public void Set<T>(EcsID entity) where T : unmanaged
    {
        ref readonly var cmp = ref Component<T>();

        EcsAssert.Assert(cmp.Size <= 0, "this is not a tag");

		Unsafe.SkipInit<T>(out var def);
        Set(entity, in cmp, in def);
    }

    [SkipLocalsInit]
    public unsafe void Set<T>(EcsID entity, T component) where T : unmanaged
    {
        ref readonly var cmp = ref Component<T>();

        EcsAssert.Assert(cmp.Size > 0, "this is not a component");

        Set(entity, in cmp, in component);
    }

    public void Unset<T>(EcsID entity) where T : unmanaged =>
        DetachComponent(entity, in Component<T>());

    public bool Has<T>(EcsID entity) where T : unmanaged => Has(entity, in Component<T>());

    //public bool Has<TKind>(EcsID entity, EcsID target) where TKind : unmanaged =>
    //    Has(entity, Entity<TKind>(), target);

    //public bool Has<TKind, TTarget>(EcsID entity)
    //    where TKind : unmanaged
    //    where TTarget : unmanaged => Has(entity, Entity<TKind>(), Entity<TKind>());

    public ref T Get<T>(EcsID entity) where T : unmanaged
    {
        ref var record = ref GetRecord(entity);
        var raw = record.Archetype.ComponentData<T>(record.Row, 1);

        EcsAssert.Assert(!raw.IsEmpty);

        return ref MemoryMarshal.GetReference(raw);
    }

    [SkipLocalsInit]
    public void SetSingleton<T>(T component = default) where T : unmanaged =>
        Set(Entity<T>(), component);

    public ref T GetSingleton<T>() where T : unmanaged => ref Get<T>(Entity<T>());

    public void RunPhase<TPhase>() where TPhase : unmanaged => RunPhase(Pair<EcsPhase, TPhase>());

    public void EmitEvent<TEvent>(EcsID entity, EcsID component) where TEvent : unmanaged
    {
        EmitEvent(Entity<TEvent>(), entity, component);
    }

    public void EmitEvent<TEvent, TComponent>(EcsID entity)
        where TEvent : unmanaged
        where TComponent : unmanaged
    {
        EmitEvent(Entity<TEvent>(), entity, Entity<TComponent>());
    }

    public void EmitEvent<TEvent, TKind, TTarget>(EcsID entity)
        where TEvent : unmanaged
        where TKind : unmanaged
        where TTarget : unmanaged
    {
        EmitEvent(Entity<TEvent>(), entity, Pair<TKind, TTarget>());
    }

    public void EmitEvent<TEvent, TKind>(EcsID entity, EcsID target)
        where TEvent : unmanaged
        where TKind : unmanaged
    {
        EmitEvent(Entity<TEvent>(), entity, Pair<TKind>(target));
    }
}

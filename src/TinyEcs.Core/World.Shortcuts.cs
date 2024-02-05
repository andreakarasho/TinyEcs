namespace TinyEcs;

public sealed partial class World
{
    public void Set<T>(EcsID entity)
    {
        ref readonly var cmp = ref Component<T>();

        EcsAssert.Assert(cmp.Size <= 0, "this is not a tag");

		Unsafe.SkipInit<T>(out var def);
        Set(entity, in cmp, in def);
    }

    [SkipLocalsInit]
    public unsafe void Set<T>(EcsID entity, T component) 
    {
        ref readonly var cmp = ref Component<T>();

        EcsAssert.Assert(cmp.Size > 0, "this is not a component");

        Set(entity, in cmp, in component);
    }

    public void Unset<T>(EcsID entity) =>
        DetachComponent(entity, in Component<T>());

    public bool Has<T>(EcsID entity) => Has(entity, in Component<T>());

    public ref T Get<T>(EcsID entity) 
    {
        ref var record = ref GetRecord(entity);
        var raw = record.Archetype.ComponentData<T>(record.Row, 1);

        EcsAssert.Assert(!raw.IsEmpty);

        return ref MemoryMarshal.GetReference(raw);
    }

    public void RunPhase<TPhase>() => RunPhase(Component<TPhase>().ID);
}

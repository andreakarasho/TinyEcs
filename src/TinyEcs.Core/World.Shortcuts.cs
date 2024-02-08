namespace TinyEcs;

public sealed partial class World
{
    public void Set<T>(EcsID entity) where T : struct
	{
        ref readonly var cmp = ref Component<T>();

        EcsAssert.Assert(cmp.Size <= 0, "this is not a tag");

		Unsafe.SkipInit<T>(out var def);
        Set(entity, in cmp, in def);
    }

    [SkipLocalsInit]
    public unsafe void Set<T>(EcsID entity, T component) where T : struct
	{
        ref readonly var cmp = ref Component<T>();

        EcsAssert.Assert(cmp.Size > 0, "this is not a component");

        Set(entity, in cmp, in component);
    }

    public void Unset<T>(EcsID entity) where T : struct =>
        DetachComponent(entity, in Component<T>());

    public bool Has<T>(EcsID entity) where T : struct => Has(entity, in Component<T>());

    public ref T Get<T>(EcsID entity) where T : struct
	{
        ref var record = ref GetRecord(entity);
        var column = record.Archetype.GetComponentIndex(Lookup.Entity<T>.HashCode);
        var raw = record.Chunk.GetSpan<T>(column);

        return ref raw[record.Row % 4096];
    }

    public void RunPhase<TPhase>() where TPhase : struct => RunPhase(in Component<TPhase>());
}

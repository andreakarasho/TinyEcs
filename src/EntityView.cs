namespace TinyEcs;

#if NET5_0_OR_GREATER
[SkipLocalsInit]
#endif
[StructLayout(LayoutKind.Sequential)]
[DebuggerDisplay("ID: {ID}")]
public readonly struct EntityView : IEquatable<EcsID>, IEquatable<EntityView>
{
    public static readonly EntityView Invalid = new(null!, 0);

    public readonly EcsID ID;
    public readonly World World;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EntityView(World world, EcsID id)
    {
        World = world;
        ID = id;
    }

	public string Name => ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(EcsID other) => ID == other;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(EntityView other) => ID == other.ID;

    public readonly override int GetHashCode() => ID.GetHashCode();

    public readonly override bool Equals(object? obj) => obj is EntityView ent && Equals(ent);

    public readonly EntityView Set<T>() where T : struct
	{
        World.Set<T>(ID);
        return this;
    }

    public readonly EntityView Set<T>(T component) where T : struct
	{
        World.Set(ID, component);
        return this;
    }

	// public readonly EntityView Set<TAction, TTarget>()
	// 	where TAction : struct where TTarget : struct
	// {
	// 	World.Set(ID, World.Component<TAction>().ID, World.Component<TTarget>().ID);
	// 	return this;
	// }

	// public readonly EntityView Set<TAction>(EcsID target)
	// 	where TAction : struct
	// {
	// 	World.Set(ID, World.Component<TAction>().ID, target);
	// 	return this;
	// }

	// public readonly EntityView Set(EcsID action, EcsID target)
	// {
	// 	World.Set(ID, action, target);
	// 	return this;
	// }

    public readonly EntityView Unset<T>() where T : struct
	{
        World.Unset<T>(ID);
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

    public readonly ref T Get<T>() where T : struct => ref World.Get<T>(ID);

    public readonly bool Has<T>() where T : struct => World.Has<T>(ID);

    public readonly void Delete() => World.Delete(ID);

    public readonly bool Exists() => World.Exists(ID);

    public readonly bool IsEnabled() => !Has<EcsDisabled>();

	public override string ToString() => World.GetRecord(ID).Name ?? $"{ID}";


	public static implicit operator EcsID(EntityView d) => d.ID;

    public static bool operator ==(EntityView a, EntityView b) => a.ID.Equals(b.ID);
    public static bool operator !=(EntityView a, EntityView b) => !(a == b);
}

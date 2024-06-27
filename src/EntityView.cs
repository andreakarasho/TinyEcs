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

	public readonly int Generation => ID.Generation;
	//public int RealID => ID.ID;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(EcsID other)
		=> ID == other;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(EntityView other)
		=> ID == other.ID;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly override int GetHashCode()
		=> ID.GetHashCode();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly override bool Equals(object? obj)
		=> obj is EntityView ent && Equals(ent);



	/// <summary>
	/// Add a Tag to the entity.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Add<T>() where T : struct
	{
        World.Add<T>(ID);
        return this;
    }

	/// <summary>
	/// Set a Component to the entity.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="component"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Set<T>(T component) where T : struct
	{
        World.Set(ID, component);
        return this;
    }

	/// <summary>
	/// Add an Id to the entity.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Add(EcsID id)
	{
		World.Set(ID, id);
		return this;
	}

	/// <summary>
	/// Add an Id to the entity.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Add(EntityView id)
		=> Set(id.ID);

	/// <summary>
	/// Remove a component or a tag from the entity.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Unset<T>() where T : struct
	{
        World.Unset<T>(ID);
        return this;
    }

	/// <summary>
	/// Remove a component Id or a tag Id from the entity.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Unset(EcsID id)
	{
        World.Unset(ID, id);
        return this;
    }

	/// <summary>
	/// Remove a component Id or a tag Id from the entity.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Unset(EntityView id)
		=> Unset(id.ID);

	/// <summary>
	/// The archetype sign. The sign is unique.
	/// </summary>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<ComponentInfo> Type()
		=> World.GetType(ID);

	/// <summary>
	/// Get a component from the entity.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T Get<T>() where T : struct
		=> ref World.Get<T>(ID);

	/// <summary>
	/// Check if the entity has a component or tag.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Has<T>() where T : struct
		=> World.Has<T>(ID);

	/// <summary>
	/// Check if the entity has a component Id or tag Id.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Has(EcsID id)
		=> World.Has(ID, id);

	/// <summary>
	/// Check if the entity has a component Id or tag Id.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Has(EntityView id)
		=> World.Has(ID, id.ID);

	/// <summary>
	/// Delete the entity.<br>
	/// Associated children are deleted too.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Delete()
		=> World.Delete(ID);

	/// <summary>
	/// Check if the entity is valid and alive.
	/// </summary>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Exists()
		=> World.Exists(ID);


	public static implicit operator EcsID(EntityView d) => d.ID;

    public static bool operator ==(EntityView a, EntityView b) => a.ID.Equals(b.ID);
    public static bool operator !=(EntityView a, EntityView b) => !(a == b);
}

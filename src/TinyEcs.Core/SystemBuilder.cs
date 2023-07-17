namespace TinyEcs;

public readonly struct SystemBuilder : IEquatable<EntityID>, IEquatable<SystemBuilder>
{
	public readonly EntityID ID;
	internal readonly World World;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal SystemBuilder(World world, EntityID id)
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
	public readonly bool Equals(SystemBuilder other)
	{
		return ID == other.ID;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal readonly SystemBuilder Set<T>(T component = default) where T : unmanaged
	{
		World.Set(ID, component);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly SystemBuilder SetQuery(EntityID query)
	{
		World.Set(ID, new EcsQuery() { ID = query });
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly SystemBuilder SetTick(float tick)
	{
		World.Set(ID, new EcsSystemTick() { Value = tick });
		return this;
	}
}

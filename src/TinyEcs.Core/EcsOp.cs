namespace TinyEcs;

static class IDOp
{
	public static void Toggle(ref EntityID id)
	{
		id ^= EcsConst.ECS_TOGGLE;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EntityID GetGeneration(EntityID id)
	{
		return ((id & EcsConst.ECS_GENERATION_MASK) >> 32);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EntityID IncreaseGeneration(EntityID id)
	{
		return ((id & ~EcsConst.ECS_GENERATION_MASK) | ((0xFFFF & (GetGeneration(id) + 1)) << 32));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EntityID RealID(EntityID id)
	{
		return id &= EcsConst.ECS_ENTITY_MASK;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool HasFlag(EntityID id, byte flag)
	{
		return (id & flag) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsComponent(EntityID id)
	{
		return (id & EcsConst.ECS_COMPONENT_MASK) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EntityID SetAsComponent(EntityID id)
	{
		return id |= EcsConst.ECS_ID_FLAGS_MASK;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EntityID Pair(EntityID first, EntityID second)
	{
		return EcsConst.ECS_PAIR | ((first << 32) + (uint)second);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPair(EntityID id)
	{
		return ((id) & EcsConst.ECS_ID_FLAGS_MASK) == EcsConst.ECS_PAIR;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EntityID GetPairFirst(EntityID id)
	{
		return (uint)((id & EcsConst.ECS_COMPONENT_MASK) >> 32);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EntityID GetPairSecond(EntityID id)
	{
		return (uint)id;
	}
}

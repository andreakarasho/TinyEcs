namespace TinyEcs;

static class IDOp
{
	public static void Toggle(ref EcsID id)
	{
		id ^= EcsConst.ECS_TOGGLE;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EcsID GetGeneration(EcsID id)
	{
		return ((id & EcsConst.ECS_GENERATION_MASK) >> 32);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EcsID IncreaseGeneration(EcsID id)
	{
		return ((id & ~EcsConst.ECS_GENERATION_MASK) | ((0xFFFF & (GetGeneration(id) + 1)) << 32));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EcsID RealID(EcsID id)
	{
		return id &= EcsConst.ECS_ENTITY_MASK;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool HasFlag(EcsID id, byte flag)
	{
		return (id & flag) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsComponent(EcsID id)
	{
		return (id & EcsConst.ECS_COMPONENT_MASK) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EcsID SetAsComponent(EcsID id)
	{
		return id |= EcsConst.ECS_ID_FLAGS_MASK;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EcsID Pair(EcsID first, EcsID second)
	{
		return EcsConst.ECS_PAIR | ((first << 32) + (uint)second);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPair(EcsID id)
	{
		return ((id) & EcsConst.ECS_ID_FLAGS_MASK) == EcsConst.ECS_PAIR;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EcsID GetPairFirst(EcsID id)
	{
		return (uint)((id & EcsConst.ECS_COMPONENT_MASK) >> 32);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EcsID GetPairSecond(EcsID id)
	{
		return (uint)id;
	}
}

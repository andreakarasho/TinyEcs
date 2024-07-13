namespace TinyEcs;

public static class IDOp
{
	public static void Toggle(ref ulong id)
	{
		id ^= EcsConst.ECS_TOGGLE;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong GetGeneration(ulong id)
	{
		return ((id & EcsConst.ECS_GENERATION_MASK) >> 32);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong IncreaseGeneration(ulong id)
	{
		return ((id & ~EcsConst.ECS_GENERATION_MASK) | ((0xFFFF & (GetGeneration(id) + 1)) << 32));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong RealID(ulong id)
	{
		return id &= EcsConst.ECS_ENTITY_MASK;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool HasFlag(ulong id, byte flag)
	{
		return (id & flag) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsComponent(ulong id)
	{
		return (id & ~EcsConst.ECS_COMPONENT_MASK) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong SetAsComponent(ulong id)
	{
		return id |= EcsConst.ECS_ID_FLAGS_MASK;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong Pair(ulong first, ulong second)
	{
		return EcsConst.ECS_PAIR | ((first << 32) + (uint)second);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPair(ulong id)
	{
		return ((id) & EcsConst.ECS_ID_FLAGS_MASK) == EcsConst.ECS_PAIR;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong GetPairFirst(ulong id)
	{
		return (uint)((id & EcsConst.ECS_COMPONENT_MASK) >> 32);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong GetPairSecond(ulong id)
	{
		return (uint)id;
	}
}

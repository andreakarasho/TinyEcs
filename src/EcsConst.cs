namespace TinyEcs;

static class EcsConst
{
	public const ulong ECS_ENTITY_MASK = 0xFFFFFFFFul;
	public const ulong ECS_GENERATION_MASK = (0xFFFFul << 32);
	public const ulong ECS_ID_FLAGS_MASK = (0xFFul << 60);
	public const ulong ECS_COMPONENT_MASK = ~ECS_ID_FLAGS_MASK;

	public const ulong ECS_TOGGLE = 1ul << 61;
	public const ulong ECS_PAIR = 1ul << 63;
}

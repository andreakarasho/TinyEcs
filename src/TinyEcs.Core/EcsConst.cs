namespace TinyEcs;

static class EcsConst
{
	public const EntityID ECS_ENTITY_MASK = 0xFFFFFFFFul;
	public const EntityID ECS_GENERATION_MASK = (0xFFFFul << 32);
	public const EntityID ECS_ID_FLAGS_MASK = (0xFFul << 60);
	public const EntityID ECS_COMPONENT_MASK = ~ECS_ID_FLAGS_MASK;

	public const EntityID ECS_TOGGLE = 1ul << 61;
	public const EntityID ECS_PAIR = 1ul << 63;
}
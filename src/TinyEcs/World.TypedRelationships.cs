namespace TinyEcs;

public sealed partial class World
{
	private readonly Dictionary<Type, object> _typedRelationshipMappers = new();

	internal TypedRelationshipMapper<TKind> GetTypedRelationshipMapper<TKind>(
		CleanupPolicy policy = CleanupPolicy.UnlinkDescendants)
		where TKind : struct
	{
		var type = typeof(TKind);
		if (!_typedRelationshipMappers.TryGetValue(type, out var mapper))
		{
			mapper = new TypedRelationshipMapper<TKind>(this, policy);
			_typedRelationshipMappers[type] = mapper;
		}
		return (TypedRelationshipMapper<TKind>)mapper;
	}
}

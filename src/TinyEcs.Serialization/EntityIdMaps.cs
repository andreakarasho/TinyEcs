namespace TinyEcs.Serialization;

/// <summary>
/// Provides the original 64-bit id that should be written for an entity reference
/// while serializing a world. Custom component serializers receive this when they
/// need to persist an <see cref="EcsID"/> field.
/// </summary>
public interface IEntityIdResolver
{
	ulong Resolve(EcsID id);
}

/// <summary>
/// Maps an original (serialized) entity id back to its newly-allocated id in the
/// destination world. Returns 0 (invalid) when the source id was not part of the
/// serialized payload.
/// </summary>
public interface IEntityIdRemapper
{
	EcsID Remap(ulong originalId);
}

internal sealed class IdentityResolver : IEntityIdResolver
{
	public static readonly IdentityResolver Instance = new();
	public ulong Resolve(EcsID id) => id;
}

internal sealed class DictionaryRemapper : IEntityIdRemapper
{
	private readonly Dictionary<ulong, EcsID> _map;

	public DictionaryRemapper(Dictionary<ulong, EcsID> map) => _map = map;

	public EcsID Remap(ulong originalId)
		=> _map.TryGetValue(originalId, out var mapped) ? mapped : (EcsID)0;
}

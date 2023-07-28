namespace TinyEcs;

public ref struct QueryBuilder
{
	private readonly World _world;
	private readonly List<EntityID> _with, _without;
	private EntityID _id;

	internal QueryBuilder(World world)
	{
		_world = world;
		_with = new List<EntityID>();
		_without = new List<EntityID>();
	}

	public readonly QueryBuilder With<T>() where T : unmanaged
		=> With(_world.Component<T>());

	public readonly QueryBuilder With<TKind, TTarget>() where TKind : unmanaged where TTarget : unmanaged
		=> With(_world.Component<TKind>(), _world.Component<TTarget>());

	public readonly QueryBuilder With(EntityID id)
	{
		_with.Add(id);
		return this;
	}

	public readonly QueryBuilder With(EntityID first, EntityID second)
	{
		_with.Add(IDOp.Pair(first, second));
		return this;
	}

	public readonly QueryBuilder Without<T>() where T : unmanaged
		=> Without(_world.Component<T>());

	public readonly QueryBuilder Without<TKind, TTarget>() where TKind : unmanaged where TTarget : unmanaged
		=> Without(_world.Component<TKind>(), _world.Component<TTarget>());

	public readonly QueryBuilder Without(EntityID id)
	{
		_without.Add(id);
		return this;
	}

	public readonly QueryBuilder Without(EntityID first, EntityID second)
	{
		_without.Add(IDOp.Pair(first, second));
		return this;
	}

	public EntityView Build()
	{
		if (_id != 0)
			return new EntityView(_world, _id);

		_with.Sort();
		_without.Sort();

		var spanWith = CollectionsMarshal.AsSpan(_with);
		var spawnWithout = CollectionsMarshal.AsSpan(_without);

		var ent = _world.Spawn()
			.Set<EcsQueryBuilder>();

		_id = ent.ID;

		Span<byte> empty = stackalloc byte[1];

		foreach (var cmp in spanWith)
			_world.Set(ent.ID, cmp | EcsConst.ECS_QUERY_WITH, empty);

		foreach (var cmp in spawnWithout)
			_world.Set(ent.ID, cmp | EcsConst.ECS_QUERY_WITHOUT, empty);

		return ent;
	}

	public readonly void Iterate(IteratorDelegate action, Commands? commands = null)
	{
		_world.Query(CollectionsMarshal.AsSpan(_with), CollectionsMarshal.AsSpan(_without), commands, action);
	}
}

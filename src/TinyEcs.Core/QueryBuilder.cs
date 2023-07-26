namespace TinyEcs;



public readonly ref struct Query2
{
	private readonly World _world;
	private readonly List<EntityID> _with, _without;

	internal Query2(World world)
	{
		_world = world;
		_with = new List<EntityID>();
		_without = new List<EntityID>();
	}

	public readonly Query2 With<T>() where T : unmanaged
	{
		_with.Add(_world.Component<T>());
		return this;
	}

	public readonly Query2 Without<T>() where T : unmanaged
	{
		_without.Add(_world.Component<T>());
		return this;
	}

	public readonly void Build()
	{
		var spanWith = CollectionsMarshal.AsSpan(_with);
		var spawnWithout = CollectionsMarshal.AsSpan(_without);

		var ent = _world.Spawn()
			.Set<EcsQuery>();

		Span<byte> empty = stackalloc byte[1];

		foreach (var cmp in spanWith)
			_world.SetComponentData(ent.ID, cmp | EcsConst.ECS_QUERY_WITH, empty);

		foreach (var cmp in spawnWithout)
			_world.SetComponentData(ent.ID, cmp | EcsConst.ECS_QUERY_WITHOUT, empty);
	}

	public readonly void Iterate(Action<Archetype> action)
	{
		_world.Query(CollectionsMarshal.AsSpan(_with), CollectionsMarshal.AsSpan(_without), action);
	}
}



public readonly struct QueryBuilder : IEquatable<EntityID>, IEquatable<QueryBuilder>
{
	public readonly EntityID ID;
	internal readonly World World;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal QueryBuilder(World world, EntityID id)
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
	public readonly bool Equals(QueryBuilder other)
	{
		return ID == other.ID;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryBuilder With<T>() where T : unmanaged
	{
		World.SetComponentData(ID, World.Component<T>() | EcsConst.ECS_QUERY_WITH, stackalloc byte[1]);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryBuilder With<TKind, TTarget>()
		where TKind : unmanaged
		where TTarget : unmanaged
	{
		return With(World.Component<TKind>(), World.Component<TTarget>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryBuilder With(EntityID first, EntityID second)
	{
		var id = IDOp.Pair(first, second) | EcsConst.ECS_QUERY_WITH;
		World.SetComponentData(ID, id, stackalloc byte[1]);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryBuilder With(EntityID id)
	{
        World.SetComponentData(ID, id | EcsConst.ECS_QUERY_WITH, stackalloc byte[1]);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryBuilder Without<T>() where T : unmanaged
	{
		World.SetComponentData(ID, World.Component<T>() | EcsConst.ECS_QUERY_WITHOUT, stackalloc byte[1]);
		return this;
	}
}

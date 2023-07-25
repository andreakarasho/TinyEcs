namespace TinyEcs;


// sealed class Query
// {
// 	private readonly World _world;
// 	private readonly HashSet<EntityID> _with, _without;

// 	internal Query(World world)
// 	{
// 		_world = world;
// 		_with = new ();
// 		_without = new ();
// 	}

// 	public Query With<T>() where T : unmanaged
// 	{
// 		_with.Add(_world.Component<T>());
// 		return this;
// 	}

// 	public Query Without<T>() where T : unmanaged
// 	{
// 		_without.Add(_world.Component<T>());
// 		return this;
// 	}


// 	public Query With(ReadOnlySpan<EntityID> with)
// 	{


// 		return this;
// 	}

// 	public Query Without(ReadOnlySpan<EntityID> without)
// 	{

// 		return this;
// 	}

// 	public void Iterate()
// 	{
// 		_with.
// 	}
// }


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



	public unsafe QueryIterator GetEnumerator()
	{
		ref var record = ref World._entities.Get(ID);
		Debug.Assert(!Unsafe.IsNullRef(ref record));

		var components = record.Archetype.ComponentInfo;
		Span<EntityID> cmps = new EntityID[components.Length];

		var withIdx = 0;
		var withoutIdx = components.Length;

        for (int i = 0; i < components.Length; ++i)
		{
			ref readonly var meta = ref components[i];

			if ((meta.ID & EcsConst.ECS_QUERY_WITH) == EcsConst.ECS_QUERY_WITH)
			{
				cmps[withIdx++] = meta.ID  & ~EcsConst.ECS_QUERY_WITH;
			}
			else if ((meta.ID  & EcsConst.ECS_QUERY_WITHOUT) == EcsConst.ECS_QUERY_WITHOUT)
			{
				cmps[--withoutIdx] = meta.ID  & ~EcsConst.ECS_QUERY_WITHOUT;
			}
		}

		var with = cmps.Slice(0, withIdx);
		var without = cmps.Slice(0, components.Length).Slice(withoutIdx);

		if (with.IsEmpty)
		{
			return default;
		}

		with.Sort();
		without.Sort();

		// var arch = World.GetArchetype(, with, without);
		// if (arch == null)
		// 	return default;

		return new QueryIterator(World._archRoot, with, without);
	}
}

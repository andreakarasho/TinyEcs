namespace TinyEcs;

public readonly struct QueryBuilder : IEquatable<EntityID>, IEquatable<QueryBuilder>
{
	internal const EntityID FLAG_WITH = (0x01ul << 60);
	internal const EntityID FLAG_WITHOUT = (0x02ul << 60);


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
		World.Set(ID, new EcsQueryParameter<T>() { Component = TypeInfo<T>.GetID(World) | FLAG_WITH });
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryBuilder With<TKind, TTarget>()
		where TKind : unmanaged
		where TTarget : unmanaged
	{
		return With(TypeInfo<TKind>.GetID(World), TypeInfo<TTarget>.GetID(World));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryBuilder With(EntityID first, EntityID second)
	{
		var id = IDOp.Pair(first, second);
		World.Set(ID, id | FLAG_WITH);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryBuilder With(EntityID id)
	{
		World.Set(ID, id | FLAG_WITH);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryBuilder Without<T>() where T : unmanaged
	{
		World.Set(ID, new EcsQueryParameter<T>() { Component = TypeInfo<T>.GetID(World) | FLAG_WITHOUT });
		return this;
	}

	public QueryIterator GetEnumerator()
	{
		ref var record = ref World._entities.Get(ID);
		Debug.Assert(!Unsafe.IsNullRef(ref record));

		var components = record.Archetype.ComponentInfo;
		var cmps = ArrayPool<EntityID>.Shared.Rent(components.Length);

		var withIdx = 0;
		var withoutIdx = components.Length;

		//cmps[withoutIdx] = ComponentStorage.GetOrAdd<EcsQuery>(world).ID;

		for (int i = 0; i < components.Length; ++i)
		{
			ref readonly var meta = ref components[i];

			var cmp = Unsafe.As<byte, EntityID>(ref MemoryMarshal.GetReference(record.Archetype.GetComponentRaw(meta.ID, record.Row, 1)));

			if ((cmp & QueryBuilder.FLAG_WITH) != 0)
			{
				cmps[withIdx++] = cmp & ~QueryBuilder.FLAG_WITH;
			}
			else if ((cmp & QueryBuilder.FLAG_WITHOUT) != 0)
			{
				cmps[--withoutIdx] = cmp & ~QueryBuilder.FLAG_WITHOUT;
			}
		}

		var with = cmps.AsSpan(0, withIdx);
		var without = cmps.AsSpan(0, components.Length).Slice(withoutIdx);

		with.Sort();
		without.Sort();

		if (with.IsEmpty)
		{
			return default;
		}

		var stack = new Stack<Archetype>();
		stack.Push(World._archRoot);

		return new QueryIterator(stack, cmps, with, without);
	}

	internal static unsafe Archetype FetchArchetype
	(
		Stack<Archetype> stack,
		ReadOnlySpan<EntityID> with,
		ReadOnlySpan<EntityID> without
	)
	{
		if (stack.Count == 0 || !stack.TryPop(out var archetype) || archetype == null)
		{
			return null;
		}

		var span = CollectionsMarshal.AsSpan(archetype._edgesRight);
		if (!span.IsEmpty)
		{
			ref var last = ref span[^1];

			for (int i = 0; i < span.Length; ++i)
			{
				ref var edge = ref Unsafe.Subtract(ref last, i);

				if (without.IndexOf(edge.ComponentID) < 0)
				{
					stack.Push(edge.Archetype);
				}
			}
		}

		if (archetype.Count > 0 && archetype.IsSuperset(with))
		{
			// query ok, call the system now
			return archetype;
		}

		return null;
	}
}
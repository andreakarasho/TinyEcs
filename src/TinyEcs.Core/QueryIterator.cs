namespace TinyEcs;

public unsafe ref struct QueryIterator
{
	private Archetype? _current;
	private readonly ReadOnlySpan<EntityID> _with, _without;

	internal QueryIterator(Archetype root, ReadOnlySpan<EntityID> with, ReadOnlySpan<EntityID> without)
	{
		_with = with;
		_without = without;
		_current = root;
	}

	public bool MoveNext()
	{
		_current = World.GetArchetype(_current!, _with, _without);

		return _current != null;
	}

	public readonly EntityIterator Current => new(_current!, 0f);
}



internal static class QueryEx
{
	public static unsafe void Fetch(World world, EntityID query, Commands cmds, delegate*<Commands, ref EntityIterator, void> system, float deltaTime)
	{
		Debug.Assert(world.IsAlive(query));
		Debug.Assert(world.Has<EcsQueryBuilder>(query));

		ref var record = ref world._entities.Get(query);
		Debug.Assert(!Unsafe.IsNullRef(ref record));

        var components = record.Archetype.ComponentInfo;
		Span<EntityID> cmps = stackalloc EntityID[components.Length + 0];

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
		var without = cmps.Slice(withoutIdx);

        if (!with.IsEmpty)
		{
			with.Sort();
			without.Sort();

			world.Query(with, without, arch => {
				var it = new EntityIterator(arch, deltaTime);
				system(cmds, ref it);
			});
		}
	}
}

namespace TinyEcs;

public unsafe ref struct QueryIterator
{
	private Archetype _current;
	private readonly ReadOnlySpan<EntityID> _with, _without;
	private readonly EntityID[] _buffer;
	private readonly Stack<Archetype> _stack;

	internal QueryIterator(Stack<Archetype> stack, EntityID[] buffer, ReadOnlySpan<EntityID> with, ReadOnlySpan<EntityID> without)
	{
		_stack = stack;
		_current = stack.Peek();
		_with = with;
		_without = without;
		_buffer = buffer;
	}

	public bool MoveNext()
	{
		if (_stack == null)
			return false;

		do
		{
			_current = QueryBuilder.FetchArchetype(_stack, _with, _without);

		} while (_stack.Count > 0 && _current == null);

		return _current != null;
	}

	public readonly EntityIterator Current => new(_current, 0f);

	public readonly void Dispose()
	{
		if (_buffer != null)
			ArrayPool<EntityID>.Shared.Return(_buffer);
	}
}

internal static class QueryEx
{
	public static unsafe void Fetch(World world, EntityID query, Commands cmds, delegate*<Commands, ref EntityIterator, void> system, float deltaTime)
	{
		Debug.Assert(world.Has<EcsQueryBuilder>(query));

		ref var record = ref world._entities.Get(query);
		Debug.Assert(!Unsafe.IsNullRef(ref record));

        var components = record.Archetype.ComponentInfo;
		Span<EntityID> cmps = stackalloc EntityID[components.Length + 0];

		var withIdx = 0;
		var withoutIdx = components.Length;

        //cmps[withoutIdx] = ComponentStorage.GetOrAdd<EcsQuery>(world).ID;

        var withID = world.Component<EcsQueryParameterWith>();
        var withoutID = world.Component<EcsQueryParameterWithout>();

        for (int i = 0; i < components.Length; ++i)
		{
			ref var meta = ref components[i];
			Debug.Assert(!Unsafe.IsNullRef(ref meta));

            if (!IDOp.IsPair(meta.ID))
                continue;

            var first = IDOp.GetPairFirst(meta.ID);
            var second = IDOp.GetPairSecond(meta.ID);

            if (first == withID)
            {
                cmps[withIdx++] = second;
            }
            else if (first == withoutID)
            {
                cmps[--withoutIdx] = second;
            }
		}

		var with = cmps.Slice(0, withIdx);
		var without = cmps.Slice(withoutIdx);

		with.Sort();
		without.Sort();

        if (!with.IsEmpty)
		{
			FetchArchetype(world._archRoot, with, without, cmds, system, deltaTime);
		}
	}

	private static unsafe void FetchArchetype
	(
		Archetype archetype,
		ReadOnlySpan<EntityID> with,
		ReadOnlySpan<EntityID> without,
		Commands cmds,
		delegate* managed<Commands, ref EntityIterator, void> system,
		float deltaTime
	)
	{
		if (archetype == null)
		{
			return;
		}

		if (archetype.Count > 0 && archetype.IsSuperset(with))
		{
			// query ok, call the system now
			var it = new EntityIterator(archetype, deltaTime);
			system(cmds, ref it);
		}

		var span = CollectionsMarshal.AsSpan(archetype._edgesRight);
		if (!span.IsEmpty)
		{
			ref var start = ref MemoryMarshal.GetReference(span);
			ref var end = ref Unsafe.Add(ref start, span.Length);

			while (Unsafe.IsAddressLessThan(ref start, ref end))
			{
                if (without.IndexOf(start.ComponentID) < 0)
                    FetchArchetype(start.Archetype, with, without, cmds, system, deltaTime);

                start = ref Unsafe.Add(ref start, 1);
            }
		}
	}
}

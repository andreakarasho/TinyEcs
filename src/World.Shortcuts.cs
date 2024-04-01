namespace TinyEcs;

public sealed partial class World
{
    public void Set<T>(EcsID entity) where T : struct
	{
        ref readonly var cmp = ref Component<T>();
        EcsAssert.Assert(cmp.Size <= 0, "this is not a tag");

		if (IsDeferred)
		{
			SetDeferred<T>(entity);

			return;
		}

        ref var record = ref GetRecord(entity);
        _ = Set(Entity(entity), ref record, in cmp);
    }

    [SkipLocalsInit]
    public void Set<T>(EcsID entity, T component) where T : struct
	{
		ref readonly var cmp = ref Component<T>();
        EcsAssert.Assert(cmp.Size > 0, "this is not a component");

		if (IsDeferred)
		{
			SetDeferred(entity, component);

			return;
		}

        ref var record = ref GetRecord(entity);
        var raw = Set(Entity(entity), ref record, in cmp)!;
        ref var array = ref Unsafe.As<Array, T[]>(ref raw);
        array[record.Row & Archetype.CHUNK_THRESHOLD] = component;
	}

    public void Unset<T>(EcsID entity) where T : struct
	{
		if (IsDeferred)
		{
			UnsetDeferred<T>(entity);

			return;
		}

		DetachComponent(entity, in Component<T>());
	}

    public bool Has<T>(EcsID entity) where T : struct
		=> (Exists(entity) && Has(entity, in Component<T>())) || (IsDeferred && HasDeferred<T>(entity));

    public ref T Get<T>(EcsID entity) where T : struct
	{
		ref readonly var cmp = ref Component<T>();

		if (IsDeferred)
		{
			return ref GetDeferred<T>(entity);
		}

        ref var record = ref GetRecord(entity);
        var column = record.Archetype.GetComponentIndex(in cmp);
        ref var chunk = ref record.GetChunk();
        return ref Unsafe.Add(ref chunk.GetReference<T>(column), record.Row & Archetype.CHUNK_THRESHOLD);
    }

    public ref T TryGet<T>(EcsID entity) where T : struct
    {
		if (IsDeferred)
		{
			return ref GetDeferred<T>(entity);
		}

	    ref var record = ref GetRecord(entity);
	    var column = record.Archetype.GetComponentIndex(Component<T>().ID);
	    if (column < 0)
		    return ref Unsafe.NullRef<T>();

	    ref var chunk = ref record.GetChunk();
	    return ref Unsafe.Add(ref chunk.GetReference<T>(column), record.Row & Archetype.CHUNK_THRESHOLD);
    }
}

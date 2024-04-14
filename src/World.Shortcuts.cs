namespace TinyEcs;

public sealed partial class World
{
    public void Set<T>(EcsID entity) where T : struct
	{
        ref readonly var cmp = ref Component<T>();
        EcsAssert.Panic(cmp.Size <= 0, "this is not a tag");

		if (IsDeferred)
		{
			SetDeferred<T>(entity);

			return;
		}

        _ = AttachComponent(entity, cmp.ID, cmp.Size);
    }

    [SkipLocalsInit]
    public void Set<T>(EcsID entity, T component) where T : struct
	{
		ref readonly var cmp = ref Component<T>();
        EcsAssert.Panic(cmp.Size > 0, "this is not a component");

		if (IsDeferred)
		{
			SetDeferred(entity, component);

			return;
		}

        (var raw, var row) = AttachComponent(entity, cmp.ID, cmp.Size);
        ref var array = ref Unsafe.As<Array, T[]>(ref raw!);
        array[row & Archetype.CHUNK_THRESHOLD] = component;
	}

	public void Set(EcsID entity, EcsID id)
	{
		if (IsDeferred)
		{
			SetDeferred(entity, id);

			return;
		}

		_ = AttachComponent(entity, id, 0);
	}

    public void Unset<T>(EcsID entity) where T : struct
	{
		Unset(entity, Component<T>().ID);
	}

	public void Unset(EcsID entity, EcsID id)
	{
		if (IsDeferred)
		{
			UnsetDeferred(entity, id);

			return;
		}

		DetachComponent(entity, id);
	}

    public bool Has<T>(EcsID entity) where T : struct
		=> (Exists(entity) && Has(entity, Component<T>().ID)) || (IsDeferred && HasDeferred<T>(entity));

    public ref T Get<T>(EcsID entity) where T : struct
	{
		ref readonly var cmp = ref Component<T>();

		if (IsDeferred && !Has(entity, cmp.ID))
		{
			return ref GetDeferred<T>(entity);
		}

        ref var record = ref GetRecord(entity);
        var column = record.Archetype.GetComponentIndex(cmp.ID);
        ref var chunk = ref record.GetChunk();
        return ref Unsafe.Add(ref chunk.GetReference<T>(column), record.Row & Archetype.CHUNK_THRESHOLD);
    }

    public ref T TryGet<T>(EcsID entity) where T : struct
    {
		ref readonly var cmp = ref Component<T>();

		if (IsDeferred && !Has(entity, cmp.ID))
		{
			return ref GetDeferred<T>(entity);
		}

	    ref var record = ref GetRecord(entity);
	    var column = record.Archetype.GetComponentIndex(cmp.ID);
	    if (column < 0)
		    return ref Unsafe.NullRef<T>();

	    ref var chunk = ref record.GetChunk();
	    return ref Unsafe.Add(ref chunk.GetReference<T>(column), record.Row & Archetype.CHUNK_THRESHOLD);
    }

	public void Deferred(Action<World> fn)
	{
		BeginDeferred();
		fn(this);
		EndDeferred();
	}
}

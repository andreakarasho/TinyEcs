namespace TinyEcs;

public sealed partial class World
{
    public void Set<T>(EcsID entity) where T : struct
	{
        ref readonly var cmp = ref Component<T>();
        EcsAssert.Panic(cmp.Size <= 0, "this is not a tag");

		if (IsDeferred && !Has(entity, cmp.ID))
		{
			SetDeferred<T>(entity);

			return;
		}

		BeginDeferred();
        _ = AttachComponent(entity, cmp.ID, cmp.Size);
		EndDeferred();
    }

    [SkipLocalsInit]
    public void Set<T>(EcsID entity, T component) where T : struct
	{
		ref readonly var cmp = ref Component<T>();
        EcsAssert.Panic(cmp.Size > 0, "this is not a component");

		if (IsDeferred && !Has(entity, cmp.ID))
		{
			SetDeferred(entity, component);

			return;
		}

		BeginDeferred();
        (var raw, var row) = AttachComponent(entity, cmp.ID, cmp.Size);
        ref var array = ref Unsafe.As<Array, T[]>(ref raw!);
        array[row & Archetype.CHUNK_THRESHOLD] = component;
		EndDeferred();
	}

	public void Set(EcsID entity, EcsID id)
	{
		if (IsDeferred && !Has(entity, id))
		{
			SetDeferred(entity, id);

			return;
		}

		BeginDeferred();
		_ = AttachComponent(entity, id, 0);
		EndDeferred();
	}

    public void Unset<T>(EcsID entity) where T : struct
		=> Unset(entity, Component<T>().ID);

	public void Unset(EcsID entity, EcsID id)
	{
		if (IsDeferred)
		{
			UnsetDeferred(entity, id);

			return;
		}

		BeginDeferred();
		DetachComponent(entity, id);
		EndDeferred();
	}

    public bool Has<T>(EcsID entity) where T : struct
		=> Exists(entity) && Has(entity, Component<T>().ID);

    public ref T Get<T>(EcsID entity) where T : struct
	{
		ref readonly var cmp = ref Component<T>();

		if (IsDeferred && !Has(entity, cmp.ID))
		{
			Unsafe.SkipInit<T>(out var val);
			return ref SetDeferred(entity, val);
			// if (HasDeferred(entity, cmp.ID))
			// 	return ref GetDeferred<T>(entity);
		}

		BeginDeferred();
        ref var record = ref GetRecord(entity);
        var column = record.Archetype.GetComponentIndex(cmp.ID);
        ref var chunk = ref record.GetChunk();
        ref var value = ref column < 0 ? ref Unsafe.NullRef<T>() : ref Unsafe.Add(ref chunk.GetReference<T>(column), record.Row & Archetype.CHUNK_THRESHOLD);
		EndDeferred();

		return ref value;
    }

	public void Deferred(Action<World> fn)
	{
		BeginDeferred();
		fn(this);
		EndDeferred();
	}
}

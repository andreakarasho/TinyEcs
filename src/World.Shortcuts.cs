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

        _ = Set(ref GetRecord(entity), in cmp);
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
        var raw = Set(ref record, in cmp)!;
        ref var array = ref Unsafe.As<Array, T[]>(ref raw);
        array[record.Row & Archetype.CHUNK_THRESHOLD] = component;
	}

	public void Set<TAction, TTarget>(EcsID entity, TTarget? target = default)
		where TAction : struct
		where TTarget : struct
	{
		// TODO: deferred support

		ref readonly var firstCmp = ref Component<TAction>();
		ref readonly var secondCmp = ref Component<TTarget>();
		ref readonly var linkedCmp = ref Component<(TAction, TTarget)>();
		ref readonly var linkedCmpWildcard0 = ref Component<(Wildcard, TTarget)>();
		ref readonly var linkedCmpWildcard1 = ref Component<(TAction, Wildcard)>();

		ref var record = ref GetRecord(entity);
		var raw = Set(ref record, in linkedCmp);

		if (raw != null)
		{
			ref var array = ref Unsafe.As<Array, TTarget[]>(ref raw);
			array[record.Row & Archetype.CHUNK_THRESHOLD] = target!.Value;
		}
	}

	public void Set<TAction>(EcsID entity, EcsID target)
		where TAction : struct
	{
		// TODO: deferred support

		ref readonly var firstCmp = ref Component<TAction>();

		var pair = IDOp.Pair(firstCmp.ID, target);
		var cmp = new ComponentInfo(pair, 0);
		ref var record = ref GetRecord(entity);
		var raw = Set(ref record, in cmp);
	}

	public ref TTarget Get<TAction, TTarget>(EcsID entity)
		where TAction : struct
		where TTarget : struct
	{
		// TODO: deferred support

		ref readonly var firstCmp = ref Component<TAction>();
		ref readonly var secondCmp = ref Component<TTarget>();
		ref readonly var linkedCmp = ref Component<(TAction, TTarget)>();
		ref readonly var linkedCmpWildcard0 = ref Component<(Wildcard, TTarget)>();
		ref readonly var linkedCmpWildcard1 = ref Component<(TAction, Wildcard)>();

		ref var record = ref GetRecord(entity);
		var column = record.Archetype.GetComponentIndex(in linkedCmp);
        ref var chunk = ref record.GetChunk();
        return ref Unsafe.Add(ref chunk.GetReference<TTarget>(column), record.Row & Archetype.CHUNK_THRESHOLD);
	}

    public void Unset<T>(EcsID entity) where T : struct
	{
		if (IsDeferred)
		{
			UnsetDeferred<T>(entity);

			return;
		}

		DetachComponent(ref GetRecord(entity), in Component<T>());
	}

    public bool Has<T>(EcsID entity) where T : struct
		=> (Exists(entity) && Has(entity, in Component<T>())) || (IsDeferred && HasDeferred<T>(entity));

    public ref T Get<T>(EcsID entity) where T : struct
	{
		ref readonly var cmp = ref Component<T>();

		if (IsDeferred && !Has(entity, in cmp))
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
		ref readonly var cmp = ref Component<T>();

		if (IsDeferred && !Has(entity, in cmp))
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

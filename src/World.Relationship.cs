namespace TinyEcs;

partial class World
{
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
		ref readonly var firstCmp = ref Component<TAction>();
		Set(entity, firstCmp.ID, target);
	}

	public void Set(EcsID entity, EcsID action, EcsID target)
	{
		// TODO: deferred support

		var pair = IDOp.Pair(action, target);
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

	public bool Has<TAction ,TTarget>(EcsID entity)
		where TAction : struct
		where TTarget : struct
	{
		// TODO: deferred support

		ref readonly var firstCmp = ref Component<TAction>();
		ref readonly var secondCmp = ref Component<TTarget>();
		ref readonly var linkedCmp = ref Component<(TAction, TTarget)>();

		ref var record = ref GetRecord(entity);
		var column = record.Archetype.GetComponentIndex(in linkedCmp);
		return column >= 0;
	}

	public bool Has<TAction>(EcsID entity, EcsID target)
		where TAction : struct
	{
		// TODO: deferred support

		ref readonly var firstCmp = ref Component<TAction>();
		var pairId = IDOp.Pair(firstCmp.ID, target);
		var cmp = new ComponentInfo(pairId, 0);

		ref var record = ref GetRecord(entity);
		var column = record.Archetype.GetComponentIndex(in cmp);
		return column >= 0;
	}

	public void Unset<TAction, TTarget>(EcsID entity)
		where TAction : struct
		where TTarget : struct
	{
		// TODO: deferred support

		ref readonly var firstCmp = ref Component<TAction>();
		ref readonly var secondCmp = ref Component<TTarget>();
		ref readonly var linkedCmp = ref Component<(TAction, TTarget)>();

		ref var record = ref GetRecord(entity);
		DetachComponent(ref record, in linkedCmp);
	}

	public void Unset<TAction>(EcsID entity, EcsID target)
		where TAction : struct
	{
		ref readonly var firstCmp = ref Component<TAction>();
		Unset(entity, firstCmp.ID, target);
	}

	public void Unset(EcsID entity, EcsID action, EcsID target)
	{
		// TODO: deferred support

		var pairId = IDOp.Pair(action, target);
		var cmp = new ComponentInfo(pairId, 0);

		ref var record = ref GetRecord(entity);
		DetachComponent(ref record, in cmp);
	}
}

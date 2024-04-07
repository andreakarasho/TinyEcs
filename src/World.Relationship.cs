namespace TinyEcs;

partial class World
{
	// This is an hack to make queries working using With<(A, B)>
	private ref readonly ComponentInfo Hack<TAction, TTarget>()
		where TAction : struct
		where TTarget : struct
	{
		// Spawn components
		ref readonly var firstCmp = ref Component<TAction>();
		ref readonly var secondCmp = ref Component<TTarget>();

		// Create the relation (A, B)
		ref readonly var linkedCmp = ref Component<(TAction, TTarget)>();

		// Create the relation (*, B)
		ref readonly var linkedCmpWildcard0 = ref Component<(Wildcard, TTarget)>();

		// Create the relation (A, *)
		ref readonly var linkedCmpWildcard1 = ref Component<(TAction, Wildcard)>();

		return ref linkedCmp;
	}

	public void Set<TAction, TTarget>(EcsID entity, TTarget? target = default)
		where TAction : struct
		where TTarget : struct
	{
		// TODO: deferred support

		ref readonly var linkedCmp = ref Hack<TAction, TTarget>();

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
		Set(entity, Component<TAction>().ID, target);
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

		ref readonly var linkedCmp = ref Hack<TAction, TTarget>();

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

		ref readonly var linkedCmp = ref Hack<TAction, TTarget>();

		ref var record = ref GetRecord(entity);
		var column = record.Archetype.GetComponentIndex(in linkedCmp);
		return column >= 0;
	}

	public bool Has<TAction>(EcsID entity, EcsID target)
		where TAction : struct
	{
		// TODO: deferred support

		return Has(entity, Component<TAction>().ID, target);
	}

	public bool Has(EcsID entity, EcsID action, EcsID target)
	{
		// TODO: deferred support

		var pairId = IDOp.Pair(action, target);
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

		ref readonly var linkedCmp = ref Hack<TAction, TTarget>();

		ref var record = ref GetRecord(entity);
		DetachComponent(ref record, in linkedCmp);
	}

	public void Unset<TAction>(EcsID entity, EcsID target)
		where TAction : struct
	{
		Unset(entity, Component<TAction>().ID, target);
	}

	public void Unset(EcsID entity, EcsID action, EcsID target)
	{
		// TODO: deferred support

		var pairId = IDOp.Pair(action, target);
		var cmp = new ComponentInfo(pairId, 0);

		ref var record = ref GetRecord(entity);
		DetachComponent(ref record, in cmp);
	}

	public EcsID Target<TAction>(EcsID entity, int index = 0)
		where TAction : struct
	{
		return Target(entity, Component<TAction>().ID, index);
	}

	public EcsID Target(EcsID entity, EcsID action, int index = 0)
	{
		ref var record = ref GetRecord(entity);

		var found = 0;
		foreach (ref readonly var cmp in record.Archetype.Components.AsSpan())
		{
			if (!IDOp.IsPair(cmp.ID) || IDOp.GetPairFirst(cmp.ID) != action) continue;

			if (found++ < index) continue;

			return IDOp.GetPairSecond(cmp.ID);
		}

		return EntityView.Invalid;
	}
}


public static class RelationshipEx
{
	public static EntityView Set<TAction, TTarget>(this EntityView entity, TTarget? target = default)
		where TAction : struct
		where TTarget : struct
	{
		entity.World.Set<TAction, TTarget>(entity.ID, target);
		return entity;
	}

	public static EntityView Set<TAction>(this EntityView entity, EcsID target)
		where TAction : struct
	{
		entity.World.Set<TAction>(entity.ID, target);
		return entity;
	}

	public static EntityView Set(this EntityView entity, EcsID action, EcsID target)
	{
		entity.World.Set(entity.ID, action, target);
		return entity;
	}

	public static EntityView Unset<TAction, TTarget>(this EntityView entity)
		where TAction : struct
		where TTarget : struct
	{
		entity.World.Unset<TAction, TTarget>(entity.ID);
		return entity;
	}

	public static EntityView Unset<TAction>(this EntityView entity, EcsID target)
		where TAction : struct
	{
		entity.World.Unset<TAction>(entity.ID, target);
		return entity;
	}

	public static EntityView Unset(this EntityView entity, EcsID action, EcsID target)
	{
		entity.World.Unset(entity.ID, action, target);
		return entity;
	}

	public static bool Has<TAction, TTarget>(this EntityView entity)
		where TAction : struct
		where TTarget : struct
	{
		return entity.World.Has<TAction, TTarget>(entity.ID);
	}

	public static bool Has<TAction>(this EntityView entity, EcsID target)
		where TAction : struct
	{
		return entity.World.Has<TAction>(entity.ID, target);
	}

	public static bool Has(this EntityView entity, EcsID action, EcsID target)
	{
		return entity.World.Has(entity.ID, action, target);
	}

	public static ref TTarget Get<TAction, TTarget>(this EntityView entity)
		where TAction : struct
		where TTarget : struct
	{
		return ref entity.World.Get<TAction, TTarget>(entity.ID);
	}

	public static EcsID Target<TAction>(this EntityView entity, int index = 0)
		where TAction : struct
	{
		return entity.World.Target<TAction>(entity.ID, index);
	}

	public static EcsID Target(this EntityView entity, EcsID action, int index = 0)
	{
		return entity.World.Target(entity.ID, action, index);
	}
}

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

	private void CheckUnique(EcsID entity, EcsID action)
	{
		if (Exists(action))
		{
			// only one (A, *)
			if (Entity(action).Has<Unique>())
			{
				var targetId = Target(entity, action);
				if (targetId != 0)
				{
					Unset(entity, action, targetId);
				}
			}
		}
	}

	private void CheckSymmetric(EcsID entity, EcsID action, EcsID target)
	{
		// (R, B) to A will also add (R, A) to B.
		if (Exists(action) && Entity(action).Has<Symmetric>() && !Has(target, action, entity))
		{
			Set(target, action, entity);
		}
	}

	public void Set<TAction, TTarget>(EcsID entity, TTarget? target = default)
		where TAction : struct
		where TTarget : struct
	{
		ref readonly var linkedCmp = ref Hack<TAction, TTarget>();

		CheckUnique(entity, Entity<TAction>());

		if (target.HasValue)
			Set(entity, (default(TAction), target.Value));
		else
			Set<(TAction, TTarget)>(entity);

		CheckSymmetric(entity, Entity<TAction>(), Entity<TTarget>());
	}

	public void Set<TAction>(EcsID entity, EcsID target)
		where TAction : struct
	{
		Set(entity, Component<TAction>().ID, target);
	}

	public void Set(EcsID entity, EcsID action, EcsID target)
	{
		var pairId = IDOp.Pair(action, target);

		CheckUnique(entity, action);

		if (IsDeferred)
		{
			SetDeferred(entity, pairId);

			return;
		}

		ref var record = ref GetRecord(entity);
		_ = Set(ref record, pairId, 0);

		CheckSymmetric(entity, action, target);
	}

	public ref TTarget Get<TAction, TTarget>(EcsID entity)
		where TAction : struct
		where TTarget : struct
	{
		ref readonly var linkedCmp = ref Hack<TAction, TTarget>();
		return ref Get<(TAction, TTarget)>(entity).Item2;
	}

	public bool Has<TAction, TTarget>(EcsID entity)
		where TAction : struct
		where TTarget : struct
	{
		ref readonly var linkedCmp = ref Hack<TAction, TTarget>();
		return Has<(TAction, TTarget)>(entity);
	}

	public bool Has<TAction>(EcsID entity, EcsID target)
		where TAction : struct
	{
		return Has(entity, Component<TAction>().ID, target);
	}

	public bool Has(EcsID entity, EcsID action, EcsID target)
	{
		var pairId = IDOp.Pair(action, target);

		return (Exists(entity) && Has(entity, pairId)) ||
				(IsDeferred && HasDeferred(entity, pairId));
	}

	public void Unset<TAction, TTarget>(EcsID entity)
		where TAction : struct
		where TTarget : struct
	{
		ref readonly var linkedCmp = ref Hack<TAction, TTarget>();

		Unset<(TAction, TTarget)>(entity);
	}

	public void Unset<TAction>(EcsID entity, EcsID target)
		where TAction : struct
	{
		Unset(entity, Component<TAction>().ID, target);
	}

	public void Unset(EcsID entity, EcsID action, EcsID target)
	{
		var pairId = IDOp.Pair(action, target);

		if (IsDeferred)
		{
			UnsetDeferred(entity, pairId);
			return;
		}

		ref var record = ref GetRecord(entity);
		DetachComponent(ref record, pairId);
	}

	public EcsID Target<TAction>(EcsID entity, int index = 0)
		where TAction : struct
	{
		return Target(entity, Component<TAction>().ID, index);
	}

	public EcsID Target(EcsID entity, EcsID action, int index = 0)
	{
		var pair = IDOp.Pair(action, Wildcard.ID);
		return FindPair(entity, pair, index).Item2;
	}

	public EcsID Action<TTarget>(EcsID entity, int index = 0)
		where TTarget : struct
	{
		return Action(entity, Component<TTarget>().ID, index);
	}

	public EcsID Action(EcsID entity, EcsID target, int index = 0)
	{
		var pair = IDOp.Pair(Wildcard.ID, target);
		return FindPair(entity, pair, index).Item1;
	}

	public (EcsID, EcsID) FindPair(EcsID entity, EcsID pair, int index = 0)
	{
		ref var record = ref GetRecord(entity);

		var found = 0;
		foreach (ref readonly var cmp in record.Archetype.Pairs.AsSpan())
		{
			if (_comparer.Compare(cmp.ID.Value, pair.Value) != 0) continue;

			if (found++ < index) continue;

			return (IDOp.GetPairFirst(cmp.ID), IDOp.GetPairSecond(cmp.ID));
		}

		return (EntityView.Invalid, EntityView.Invalid);
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

	public static EcsID Action<TTarget>(this EntityView entity, int index = 0)
		where TTarget : struct
	{
		return entity.World.Action<TTarget>(entity.ID, index);
	}

	public static EcsID Action(this EntityView entity, EcsID target, int index = 0)
	{
		return entity.World.Action(entity.ID, target, index);
	}
}

public static class ChildOfEx
{
	public static EntityView AddChild(this EntityView entity, EcsID child)
	{
		entity.World.Set<ChildOf>(child, entity.ID);
		return entity;
	}
}

public static class NameEx
{
	public static string Name(this EntityView entity)
	{
		if (entity.Has<Identifier, Name>())
		{
			return entity.Get<Identifier, Name>().Value;
		}

		return entity.ID.Value.ToString();
	}
}

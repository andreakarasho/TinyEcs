using static TinyEcs.Defaults;

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
		ref readonly var linkedCmp = ref Component<Pair<TAction, TTarget>>();

		// Create the relation (*, B)
		ref readonly var linkedCmpWildcard0 = ref Component<Pair<Wildcard, TTarget>>();

		// Create the relation (A, *)
		ref readonly var linkedCmpWildcard1 = ref Component<Pair<TAction, Wildcard>>();

		return ref linkedCmp;
	}

	/// <summary>
	/// Assign (Action, Target).
	/// </summary>
	/// <typeparam name="TAction"></typeparam>
	/// <typeparam name="TTarget"></typeparam>
	/// <param name="entity"></param>
	/// <returns></returns>
	public void Add<TAction, TTarget>(EcsID entity)
		where TAction : struct
		where TTarget : struct
	{
		ref readonly var linkedCmp = ref Hack<TAction, TTarget>();

		Add<Pair<TAction, TTarget>>(entity);
	}

	/// <summary>
	/// Assign (Action, Target<br/>Target is a component.
	/// </summary>
	/// <typeparam name="TAction"></typeparam>
	/// <typeparam name="TTarget"></typeparam>
	/// <param name="entity"></param>
	/// <param name="target"></param>
	/// <returns></returns>
	public void Set<TAction, TTarget>(EcsID entity, TTarget target)
		where TAction : struct
		where TTarget : struct
	{
		ref readonly var linkedCmp = ref Hack<TAction, TTarget>();

		// Set(entity, new Pair<TAction, TTarget>() { Action = default, Target = target});

		EcsAssert.Panic(linkedCmp.Size > 0, "this is not a component");

		if (IsDeferred && !Has(entity, linkedCmp.ID))
		{
			SetDeferred(entity, linkedCmp.ID, new Pair<TAction, TTarget>() { Action = default, Target = target}, linkedCmp.Size, linkedCmp.IsManaged);

			return;
		}

        (var raw, var row) = Attach(entity, linkedCmp.ID, linkedCmp.Size, linkedCmp.IsManaged);
        var array = Unsafe.As<Pair<TAction, TTarget>[]>(raw!);
        array[row & TinyEcs.Archetype.CHUNK_THRESHOLD].Target = target;
	}

	/// <summary>
	/// Assign (Action, Target).<br/>Target is an entity.
	/// </summary>
	/// <typeparam name="TAction"></typeparam>
	/// <param name="entity"></param>
	/// <param name="target"></param>
	/// <returns></returns>
	public void Add<TAction>(EcsID entity, EcsID target)
		where TAction : struct
	{
		ref readonly var act = ref Component<TAction>();

		var pairId = IDOp.Pair(act.ID, target);

		if (IsDeferred && !Has(entity, act.ID, target))
		{
			SetDeferred(entity, pairId, null, 0, false);

			return;
		}

		_ = Attach(entity, pairId, 0, false);
	}

	/// <summary>
	/// Assign (Action, Target).<br/>Action and Target are entities.<para/>
	/// ⚠️ Do not set EntityView or EcsID here! ⚠️
	/// </summary>
	/// <typeparam name="TAction"></typeparam>
	/// <param name="entity"></param>
	/// <param name="target"></param>
	/// <param name="action"></param>
	/// <returns></returns>
	public void Set<TAction>(EcsID entity, TAction action, EcsID target)
		where TAction : struct
	{
		ref readonly var act = ref Component<TAction>();

		var pairId = IDOp.Pair(act.ID, target);

		if (IsDeferred && !Has(entity, act.ID, target))
		{
			SetDeferred(entity, pairId, action, act.Size, act.IsManaged);

			return;
		}

		(var array, var row) = Attach(entity, pairId, act.Size, act.IsManaged);
		var cmpArr = Unsafe.As<TAction[]>(array!);
		cmpArr[row & TinyEcs.Archetype.CHUNK_THRESHOLD] = action;
	}

	/// <summary>
	/// Assign (Action, Target).<br/>Action and Target are entities.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="action"></param>
	/// <param name="target"></param>
	/// <returns></returns>
	public void Add(EcsID entity, EcsID action, EcsID target)
	{
		var pairId = IDOp.Pair(action, target);

		if (IsDeferred && !Has(entity, action, target))
		{
			SetDeferred(entity, pairId, null, 0, false);

			return;
		}

		_ = Attach(entity, pairId, 0, false);
	}

	/// <summary>
	/// Retrive Target component value from (Action, Target).
	/// </summary>
	/// <typeparam name="TAction"></typeparam>
	/// <typeparam name="TTarget"></typeparam>
	/// <param name="entity"></param>
	/// <returns></returns>
	public ref TTarget Get<TAction, TTarget>(EcsID entity)
		where TAction : struct
		where TTarget : struct
	{
		ref readonly var targetCmp = ref Component<TTarget>();
		var pairId = IDOp.Pair(Component<TAction>().ID, targetCmp.ID);
		return ref GetUntrusted<Pair<TAction, TTarget>>(entity, pairId, targetCmp.Size).Target;
		// return ref Get<Pair<TAction, TTarget>>(entity).Target;
	}

	/// <summary>
	/// Retrive Target component value from (Action, Target).<br/>Target is an entity.
	/// </summary>
	/// <typeparam name="TAction"></typeparam>
	/// <param name="entity"></param>
	/// <param name="target"></param>
	/// <returns></returns>
	public ref TAction Get<TAction>(EcsID entity, EcsID target)
		where TAction : struct
	{
		ref readonly var act = ref Component<TAction>();
		var pairId = IDOp.Pair(act.ID, target);
		return ref GetUntrusted<TAction>(entity, pairId, act.Size);
	}

	/// <summary>
	/// Check if the entity has (Action, Target).
	/// </summary>
	/// <typeparam name="TAction"></typeparam>
	/// <typeparam name="TTarget"></typeparam>
	/// <param name="entity"></param>
	/// <returns></returns>
	public bool Has<TAction, TTarget>(EcsID entity)
		where TAction : struct
		where TTarget : struct
	{
		return Has(entity, Component<TAction>().ID, Component<TTarget>().ID);
	}

	/// <summary>
	/// Check if the entity has (Action, Target).<br/>Target is an entity.
	/// </summary>
	/// <typeparam name="TAction"></typeparam>
	/// <param name="entity"></param>
	/// <param name="target"></param>
	/// <returns></returns>
	public bool Has<TAction>(EcsID entity, EcsID target)
		where TAction : struct
	{
		return Has(entity, Component<TAction>().ID, target);
	}

	/// <summary>
	/// Check if the entity has (Action, Target).<br/>Action and Target are entities.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="action"></param>
	/// <param name="target"></param>
	/// <returns></returns>
	public bool Has(EcsID entity, EcsID action, EcsID target)
	{
		var pairId = IDOp.Pair(action, target);
		return Has(entity, pairId);
	}

	/// <summary>
	/// Remove (Action, Target).
	/// </summary>
	/// <typeparam name="TAction"></typeparam>
	/// <typeparam name="TTarget"></typeparam>
	/// <param name="entity"></param>
	/// <returns></returns>
	public void Unset<TAction, TTarget>(EcsID entity)
		where TAction : struct
		where TTarget : struct
	{
		Unset(entity, Component<TAction>().ID, Component<TTarget>().ID);
	}

	/// <summary>
	/// Remove (Action, Target).<br/>Target is an entity.
	/// </summary>
	/// <typeparam name="TAction"></typeparam>
	/// <param name="entity"></param>
	/// <param name="target"></param>
	/// <returns></returns>
	public void Unset<TAction>(EcsID entity, EcsID target)
		where TAction : struct
	{
		Unset(entity, Component<TAction>().ID, target);
	}

	/// <summary>
	/// Remove (Action, Target) tag.<br/>Action and Target are entities.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="action"></param>
	/// <param name="target"></param>
	/// <returns></returns>
	public void Unset(EcsID entity, EcsID action, EcsID target)
	{
		var pairId = IDOp.Pair(action, target);
		Unset(entity, pairId);
	}

	/// <summary>
	/// Retrive the Target entity of (Action, *) at a specific index.<br/>Default index is 0.
	/// </summary>
	/// <typeparam name="TAction"></typeparam>
	/// <param name="entity"></param>
	/// <param name="index"></param>
	/// <returns></returns>
	public EcsID Target<TAction>(EcsID entity, int index = 0)
		where TAction : struct
	{
		return Target(entity, Component<TAction>().ID, index);
	}

	/// <summary>
	/// Retrive the Target entity of (Action, *) at a specific index.<br/>Default index is 0.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="action"></param>
	/// <param name="index"></param>
	/// <returns></returns>
	public EcsID Target(EcsID entity, EcsID action, int index = 0)
	{
		return GetAlive(FindPair(ref GetRecord(entity), action, Wildcard.ID, index).second);
	}

	/// <summary>
	/// Retrive the Action entity of (*, Target) at a specific index.<br/>Default index is 0.
	/// </summary>
	/// <typeparam name="TTarget"></typeparam>
	/// <param name="entity"></param>
	/// <param name="index"></param>
	/// <returns></returns>
	public EcsID Action<TTarget>(EcsID entity, int index = 0)
		where TTarget : struct
	{
		return GetAlive(Action(entity, Component<TTarget>().ID, index));
	}

	/// <summary>
	/// Retrive the Action entity of (*, Target) at a specific index.<br/>Default index is 0.
	/// Target is an entity.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="target"></param>
	/// <param name="index"></param>
	/// <returns></returns>
	public EcsID Action(EcsID entity, EcsID target, int index = 0)
	{
		return GetAlive(FindPair(ref GetRecord(entity), Wildcard.ID, target, index).first);
	}

	/// <summary>
	/// Find a (Action, Target) pair at a specific index.<br/>Default index is 0.
	/// </summary>
	/// <typeparam name="TAction"></typeparam>
	/// <typeparam name="TTarget"></typeparam>
	/// <param name="entity"></param>
	/// <param name="index"></param>
	/// <returns></returns>
	internal (EcsID first, EcsID second) FindPair(ref EcsRecord record, EcsID first, EcsID second, int index = 0)
	{
		if (first == Wildcard.ID)
		{
			return FindPairFromSecond(ref record, second, index);
		}
		else if (second == Wildcard.ID)
		{
			return FindPairFromFirst(ref record, first, index);
		}
		else
		{
			var pair = IDOp.Pair(first, second);
			if (record.Archetype.HasIndex(pair))
				return (first, second);
		}

		return EntityView.Invalid.ID.Pair();
	}

	internal (EcsID first, EcsID second) FindPairFromFirst(ref EcsRecord record, EcsID first, int index = 0)
	{
		var idx = record.Archetype.GetPairIndex(first);
		if (idx < 0 || idx + index >= record.Archetype.Sign.Length)
			return EntityView.Invalid.ID.Pair();
		var result = record.Archetype.Sign[idx + index].Pair();
		if (result.first == first)
			return result;
		return EntityView.Invalid.ID.Pair();
	}

	internal (EcsID first, EcsID second) FindPairFromSecond(ref EcsRecord record, EcsID second, int index = 0)
	{
		var pair = IDOp.Pair(Wildcard.ID, second);
		var found = 0;
		foreach (ref readonly var cmp in record.Archetype.Pairs.AsSpan())
		{
			var result = _comparer.Compare(cmp.ID, pair);

			if (result < 0)
				break;

			if (result != 0)
				continue;

			if (found++ < index)
				continue;

			return cmp.ID.Pair();
		}

		return EntityView.Invalid.ID.Pair();
	}
}


public static class RelationshipEx
{
	/// <inheritdoc cref="World.Set{TAction, TTarget}(EcsID, TTarget)"/>
	public static EntityView Set<TAction, TTarget>(this EntityView entity, TTarget target)
		where TAction : struct
		where TTarget : struct
	{
		entity.World.Set<TAction, TTarget>(entity.ID, target);
		return entity;
	}

	/// <inheritdoc cref="World.Set{TAction}(EcsID, TAction, EcsID)"/>
	public static EntityView Set<TAction>(this EntityView entity, TAction action, EcsID target)
		where TAction : struct
	{
		if (action is EntityView a)
			entity.World.Add(entity.ID, a, target);
		else if (action is EcsID b)
			entity.World.Add(entity.ID, b, target);
		else
			entity.World.Set(entity.ID, action, target);
		return entity;
	}

	/// <inheritdoc cref="World.Add{TAction, TTarget}(EcsID)"/>
	public static EntityView Add<TAction, TTarget>(this EntityView entity)
		where TAction : struct
		where TTarget : struct
	{
		entity.World.Add<TAction, TTarget>(entity.ID);
		return entity;
	}

	/// <inheritdoc cref="World.Add{TAction}(EcsID, EcsID)"/>
	public static EntityView Add<TAction>(this EntityView entity, EcsID target)
		where TAction : struct
	{
		entity.World.Add<TAction>(entity.ID, target);
		return entity;
	}

	/// <inheritdoc cref="World.Add(EcsID, EcsID, EcsID)"/>
	public static EntityView Add(this EntityView entity, EcsID action, EcsID target)
	{
		entity.World.Add(entity.ID, action, target);
		return entity;
	}

	/// <inheritdoc cref="World.Unset{TAction, TTarget}(EcsID)"/>
	public static EntityView Unset<TAction, TTarget>(this EntityView entity)
		where TAction : struct
		where TTarget : struct
	{
		entity.World.Unset<TAction, TTarget>(entity.ID);
		return entity;
	}

	/// <inheritdoc cref="World.Unset{TAction}(EcsID, EcsID)"/>
	public static EntityView Unset<TAction>(this EntityView entity, EcsID target)
		where TAction : struct
	{
		entity.World.Unset<TAction>(entity.ID, target);
		return entity;
	}

	/// <inheritdoc cref="World.Unset(EcsID, EcsID, EcsID)"/>
	public static EntityView Unset(this EntityView entity, EcsID action, EcsID target)
	{
		entity.World.Unset(entity.ID, action, target);
		return entity;
	}

	/// <inheritdoc cref="World.Has{TAction, TTarget}(EcsID)"/>
	public static bool Has<TAction, TTarget>(this EntityView entity)
		where TAction : struct
		where TTarget : struct
	{
		return entity.World.Has<TAction, TTarget>(entity.ID);
	}

	/// <inheritdoc cref="World.Has{TAction}(EcsID, EcsID)"/>
	public static bool Has<TAction>(this EntityView entity, EcsID target)
		where TAction : struct
	{
		return entity.World.Has<TAction>(entity.ID, target);
	}

	/// <inheritdoc cref="World.Has(EcsID, EcsID, EcsID)"/>
	public static bool Has(this EntityView entity, EcsID action, EcsID target)
	{
		return entity.World.Has(entity.ID, action, target);
	}

	/// <inheritdoc cref="World.Get{TAction, TTarget}(EcsID)"/>
	public static ref TTarget Get<TAction, TTarget>(this EntityView entity)
		where TAction : struct
		where TTarget : struct
	{
		return ref entity.World.Get<TAction, TTarget>(entity.ID);
	}

	/// <inheritdoc cref="World.Get{TAction}(EcsID, EcsID)"/>
	public static ref TAction Get<TAction>(this EntityView entity, EcsID target)
		where TAction : struct
	{
		return ref entity.World.Get<TAction>(entity.ID, target);
	}

	/// <inheritdoc cref="World.Target{TAction}(EcsID, int)"/>
	public static EcsID Target<TAction>(this EntityView entity, int index = 0)
		where TAction : struct
	{
		return entity.World.Target<TAction>(entity.ID, index);
	}

	/// <inheritdoc cref="World.Target(EcsID, EcsID, int)"/>
	public static EcsID Target(this EntityView entity, EcsID action, int index = 0)
	{
		return entity.World.Target(entity.ID, action, index);
	}

	/// <inheritdoc cref="World.Action{TTarget}(EcsID, int)"/>
	public static EcsID Action<TTarget>(this EntityView entity, int index = 0)
		where TTarget : struct
	{
		return entity.World.Action<TTarget>(entity.ID, index);
	}

	/// <inheritdoc cref="World.Action(EcsID, EcsID, int)"/>
	public static EcsID Action(this EntityView entity, EcsID target, int index = 0)
	{
		return entity.World.Action(entity.ID, target, index);
	}

	/// <inheritdoc cref="World.FindPair(EcsID, EcsID, int)"/>
	public static (EcsID, EcsID) FindPair<TAction, TTarget>(this EntityView entity, int index = 0)
		where TAction : struct
		where TTarget : struct
	{
		return entity.World.FindPair(ref entity.World.GetRecord(entity.ID), entity.World.Component<TAction>().ID, entity.World.Component<TTarget>().ID, index);
	}
}

public static class ChildOfEx
{
	/// <summary>
	/// Shortcut to add ChildOf relation to an entity.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="child"></param>
	/// <returns></returns>
	public static EntityView AddChild(this EntityView entity, EcsID child)
	{
		entity.World.Add<ChildOf>(child, entity.ID);
		return entity;
	}

	/// <summary>
	/// Shortcut to remove ChildOf relation from an entity.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="child"></param>
	/// <returns></returns>
	public static EntityView RemoveChild(this EntityView entity, EcsID child)
	{
		entity.World.Unset<ChildOf>(child, entity.ID);
		return entity;
	}
}

public static class NameEx
{
	/// <summary>
	/// Get the name of the entity if exists.<br/>Otherwise returns the entity Id as string.
	/// </summary>
	/// <param name="entity"></param>
	/// <returns></returns>
	public static string Name(this EntityView entity)
	{
		return entity.World.Name(entity.ID);
	}
}

public interface IPair
{
	internal object Action { get; }
	internal object Target { get; }
}

public struct Pair<TAction, TTarget> : IPair
	where TAction : struct
	where TTarget : struct
{
	static readonly TAction _action = default;
	static readonly TTarget _target = default;

	readonly object IPair.Action => _action;
	readonly object IPair.Target => _target;

	public TAction Action;
	public TTarget Target;
}

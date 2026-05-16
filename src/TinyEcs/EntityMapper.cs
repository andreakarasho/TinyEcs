namespace TinyEcs;


public abstract class EntityMapper<TParentComponent, TChildrenComponent>
	where TParentComponent : struct, IParentComponent
	where TChildrenComponent : struct, IChildrenComponent
{
	private readonly Dictionary<EcsID, EcsID> _childrenToParent = new();
	private readonly Dictionary<EcsID, List<EcsID>> _parentsToChildren = new();
	private readonly World _world;
	private readonly CleanupPolicy _policy;

	protected EntityMapper(World world, CleanupPolicy policy)
	{
		_world = world;
		_policy = policy;

		world.OnEntityDeleted += OnEntityRemoved;
	}

	public virtual void Clear()
	{
		_childrenToParent.Clear();
		_parentsToChildren.Clear();
		_world.OnEntityDeleted -= OnEntityRemoved;
	}

	public EcsID GetParent(EcsID childId)
	{
		_childrenToParent.TryGetValue(childId, out EcsID parentId);
		return parentId;
	}

	public List<EcsID>? GetChildren(EcsID parentId)
	{
		_parentsToChildren.TryGetValue(parentId, out var children);
		return children;
	}

	public void Add(EcsID parentId, EcsID childId, int index = -1)
	{
		// update current parent
		RemoveChild(childId);

		_childrenToParent.Add(childId, parentId);

		if (!_parentsToChildren.TryGetValue(parentId, out var children))
		{
			children = new();
			_parentsToChildren.Add(parentId, children);

			_world.Set(parentId, new TChildrenComponent() { Value = children });
		}

		_world.Set<TParentComponent>(childId, new() { Id = parentId });
		_world.SetChanged<TChildrenComponent>(parentId);

		if (index >= 0 && index < children.Count)
			children.Insert(index, childId);
		else
			children.Add(childId);
	}

	public void Remove(EcsID id)
	{
		RemoveChild(id);
		RemoveParent(id);
	}

	private bool RemoveChild(EcsID childId)
	{
		// remove the child
		if (!_childrenToParent.Remove(childId, out var parentId))
			return false;

		// update children list on parent
		if (_parentsToChildren.TryGetValue(parentId, out var children))
		{
			_world.Unset<TParentComponent>(childId);
			children.Remove(childId);

			_world.SetChanged<TChildrenComponent>(parentId);

			if (children.Count == 0)
				RemoveParent(parentId);
		}

		// if child is a parent, remove associated children too
		RemoveParent(childId);

		return true;
	}

	private bool RemoveParent(EcsID parentId)
	{
		if (!_parentsToChildren.Remove(parentId, out var children))
			return false;

		foreach (var id in children)
		{
			if (_childrenToParent.Remove(id))
			{
				_world.Unset<TParentComponent>(id);
				ApplyPolicy(id);
			}

			// if child is a parent, remove associated children too
			RemoveParent(id);
		}

		children.Clear();

		_world.Unset<TChildrenComponent>(parentId);

		return true;
	}

	private void OnEntityRemoved(World world, EcsID id)
		=> Remove(id);

	private void ApplyPolicy(EcsID id)
	{
		switch (_policy)
		{
			case CleanupPolicy.UnlinkDescendants:
				break;
			case CleanupPolicy.DeleteDescendants:
				_world.Delete(id);
				break;
		}
	}
}


public sealed class RelationshipEntityMapper : EntityMapper<Parent, Children>
{
	internal RelationshipEntityMapper(World world) : base(world, CleanupPolicy.DeleteDescendants)
	{
	}
}

public sealed class NamingEntityMapper
{
	private readonly Dictionary<string, EcsID> _names = new();
	private readonly Dictionary<EcsID, string> _entitiesWithName = new();
	private readonly World _world;

	internal NamingEntityMapper(World world)
	{
		_world = world;
		_world.OnEntityDeleted += OnEntityDelete;
	}

	public EcsID SetName(EcsID id, string name)
	{
		if (_names.TryGetValue(name, out var entity))
		{
			if (entity != id)
				return entity;

			return id;
		}

		if (string.IsNullOrEmpty(name))
			return 0;

		id = _world.Entity(id);
		_names.Add(name, id);
		_entitiesWithName.Add(id, name);
		_world.Set(id, new Name() { Value = name });
		return id;
	}

	public void UnsetName(EcsID id)
	{
		if (_entitiesWithName.Remove(id, out var name))
			_names.Remove(name);
	}

	public string? GetName(EcsID id)
	{
		if (!id.IsValid() || !_entitiesWithName.TryGetValue(id, out var name))
			return string.Empty;
		return name;
	}

	public void Clear()
	{
		_names.Clear();
		_entitiesWithName.Clear();
		_world.OnEntityDeleted -= OnEntityDelete;
	}

	private void OnEntityDelete(World world, EcsID id)
	{
		UnsetName(id);
	}
}



public static class EntityMapperEx
{
	public static void AddChild(this World world, EcsID parent, EcsID child, int index = -1)
	{
		// Defer parent-child wiring while the world is in a deferred scope so the
		// mapper dict, Parent component, and Children component all become visible
		// together at merge time. Immediate writes mid-scope would let observers
		// (or other commands) see an inconsistent intermediate state.
		if (world.IsDeferred)
			world.AddChildDeferred(parent, child, index);
		else
			world.RelationshipEntityMapper.Add(parent, child, index);
	}

	public static void RemoveChild(this World world, EcsID child)
	{
		if (world.IsDeferred)
			world.RemoveChildDeferred(child);
		else
			world.RelationshipEntityMapper.Remove(child);
	}

	/// <summary>
	/// Get the parent entity of <paramref name="child"/> via the relationship mapper.
	/// Returns 0 when the entity has no parent (or the mapper hasn't applied the
	/// pending AddChild yet — that only happens during a deferred scope).
	/// </summary>
	public static EcsID GetParent(this World world, EcsID child)
	{
		return world.RelationshipEntityMapper.GetParent(child);
	}

	public static EntityView AddChild(this EntityView entity, EcsID childId, int index = -1)
	{
		entity.World.AddChild(entity.ID, childId, index);
		return entity;
	}

	public static EntityView RemoveChild(this EntityView entity, EcsID childId)
	{
		entity.World.RemoveChild(childId);
		return entity;
	}


	public static string Name(this EntityView entity)
		=> entity.World.Name(entity.ID);
}

public static class TypedRelationshipEx
{
	public static void AddChild<TKind>(this World world, EcsID parent, EcsID child, int index = -1)
		where TKind : struct
	{
		if (world.IsDeferred)
			world.AddChildTypedDeferred<TKind>(parent, child, index);
		else
			world.GetTypedRelationshipMapper<TKind>().Add(parent, child, index);
	}

	public static void RemoveChild<TKind>(this World world, EcsID child)
		where TKind : struct
	{
		if (world.IsDeferred)
			world.RemoveChildTypedDeferred<TKind>(child);
		else
			world.GetTypedRelationshipMapper<TKind>().Remove(child);
	}

	public static EcsID GetParent<TKind>(this World world, EcsID child)
		where TKind : struct
		=> world.GetTypedRelationshipMapper<TKind>().GetParent(child);

	public static List<EcsID>? GetChildren<TKind>(this World world, EcsID parent)
		where TKind : struct
		=> world.GetTypedRelationshipMapper<TKind>().GetChildren(parent);

	public static EntityView AddChild<TKind>(this EntityView entity, EcsID childId, int index = -1)
		where TKind : struct
	{
		entity.World.AddChild<TKind>(entity.ID, childId, index);
		return entity;
	}

	public static EntityView RemoveChild<TKind>(this EntityView entity, EcsID childId)
		where TKind : struct
	{
		entity.World.RemoveChild<TKind>(childId);
		return entity;
	}

	public static EcsID GetParent<TKind>(this EntityView entity)
		where TKind : struct
		=> entity.World.GetParent<TKind>(entity.ID);
}

public partial interface IParentComponent
{
	EcsID Id { get; init; }
}

public partial interface IChildrenComponent
{
	List<EcsID> Value { get; set; }

	List<EcsID>.Enumerator GetEnumerator();
}

public partial struct Parent : IParentComponent
{
	public EcsID Id { get; init; }
}

public struct Children : IChildrenComponent
{
	private static readonly List<EcsID> _empty = [];

	private List<EcsID> _value;

	List<EcsID> IChildrenComponent.Value
	{
		readonly get => _value;
		set => _value = value;
	}

	public int Count => _value.Count;

	public readonly List<EcsID>.Enumerator GetEnumerator()
		=> _value?.GetEnumerator() ?? _empty.GetEnumerator();
}


public partial struct Parent<TKind> : IParentComponent where TKind : struct
{
	public EcsID Id { get; init; }
}

public struct Children<TKind> : IChildrenComponent where TKind : struct
{
	private static readonly List<EcsID> _empty = [];

	private List<EcsID> _value;

	List<EcsID> IChildrenComponent.Value
	{
		readonly get => _value;
		set => _value = value;
	}

	public int Count => _value?.Count ?? 0;

	public readonly List<EcsID>.Enumerator GetEnumerator()
		=> _value?.GetEnumerator() ?? _empty.GetEnumerator();
}

public sealed class TypedRelationshipMapper<TKind>
	: EntityMapper<Parent<TKind>, Children<TKind>>
	where TKind : struct
{
	internal TypedRelationshipMapper(World world, CleanupPolicy policy)
		: base(world, policy) { }
}


public struct Name
{
	public string Value { get; internal set; }
}


public enum CleanupPolicy
{
	UnlinkDescendants,
	DeleteDescendants
}

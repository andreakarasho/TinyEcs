namespace TinyEcs;


public abstract class EntityMapper<TParentComponent, TChildrenComponent>
	where TParentComponent : struct
	where TChildrenComponent : struct, IChildrenComponent
{
	private readonly Dictionary<EcsID, EcsID> _childrenToParent = new();
	private readonly Dictionary<EcsID, HashSet<EcsID>> _parentsToChildren = new();
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

	public HashSet<EcsID>? GetChildren(EcsID parentId)
	{
		_parentsToChildren.TryGetValue(parentId, out var children);
		return children;
	}

	public void Add(EcsID parentId, EcsID childId)
	{
		// update current parent
		RemoveChild(childId);

		_childrenToParent.Add(childId, parentId);

		if (!_parentsToChildren.TryGetValue(parentId, out var children))
		{
			children = new();
			_parentsToChildren.Add(parentId, children);

			_world.Set(parentId, new TChildrenComponent() { Value = children });
			_world.Add<TParentComponent>(parentId);
		}

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
			children.Remove(childId);
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
				ApplyPolicy(id);

			// if child is a parent, remove associated children too
			RemoveParent(id);
		}

		children.Clear();
		_world.Unset<TParentComponent>(parentId);
		_world.Unset<TChildrenComponent>(parentId);

		return true;
	}

	private static void OnEntityRemoved(World world, EcsID id)
		=> world.RelationshipEntityMapper.Remove(id);

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
	private readonly Dictionary<string, EcsID> _names = new ();
	private readonly Dictionary<EcsID, string> _entitiesWithName = new();
	private readonly World _world;

	internal NamingEntityMapper(World world)
	{
		_world = world;
		_world.OnEntityDeleted += OnEntityDelete;
	}

	public EcsID SetName(EcsID id, string name)
	{
		UnsetName(id);
		if (_names.TryGetValue(name, out var entity))
		{
			if (entity != id)
				return entity;

			return id;
		}

		if (string.IsNullOrEmpty(name))
			return 0;

		id = _world.Entity();
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
	public static void AddChild2(this World world, EcsID parent, EcsID child)
	{
		world.RelationshipEntityMapper.Add(parent, child);
	}

	public static void RemoveChild2(this World world, EcsID child)
	{
		world.RelationshipEntityMapper.Remove(child);
	}

	public static EntityView AddChild2(this EntityView entity, EcsID childId)
	{
		entity.World.AddChild2(entity.ID, childId);
		return entity;
	}

	public static EntityView RemoveChild2(this EntityView entity, EcsID childId)
	{
		entity.World.RemoveChild2(childId);
		return entity;
	}


	public static string Name(this EntityView entity)
		=> entity.World.Name(entity.ID);
}

public partial interface IChildrenComponent
{
	HashSet<EcsID> Value { get; set; }

	public HashSet<EcsID>.Enumerator GetEnumerator() => Value.GetEnumerator();
}


public partial struct Parent { }

public struct Children : IChildrenComponent
{
	HashSet<EcsID> IChildrenComponent.Value { get; set; }
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

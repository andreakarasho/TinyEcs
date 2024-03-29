﻿using System.Runtime.CompilerServices;

namespace TinyEcs;

public readonly struct Hierarchy { }


public struct Relationship<T> where T : struct
{
    public int Count;
    public EcsID First;
    public EcsID Parent;
    public EcsID Next;
    public EcsID Prev;

	public readonly RelationshipIterator<T> Children(World world) => new (world, First);
}

public readonly struct Parent<T> where T : struct { }
public readonly struct Child<T> where T : struct { }

public ref struct RelationshipIterator<T> where T : struct
{
	private readonly World _world;
	private EcsID _currentEntity, _next;

	internal RelationshipIterator(World world, EcsID first)
	{
		_world = world;
		_currentEntity = first;
		_next = first;
	}

	public readonly EcsID Current => _currentEntity;

	public bool MoveNext()
	{
		_currentEntity = _next;

		if (_next != 0)
			_next = _world.Get<Relationship<T>>(_next).Next;

		return _currentEntity != 0;
	}

	public readonly RelationshipIterator<T> GetEnumerator() => this;
}

public static class RelationshipPlugin
{
	[ModuleInitializer]
	internal static void ModuleInit()
	{
		World.OnPluginInitialization += world => {
			world.Component<Parent<Hierarchy>>();
			world.Component<Child<Hierarchy>>();
			world.Component<Relationship<Hierarchy>>();
			world.BindDeletion<Hierarchy>();
		};
	}

	private static void BindDeletion<T>(this World world) where T : struct
	{
		world.OnEntityDeleted += e => {
			if (e.Has<Parent<T>>())
			{
				var first = e.Get<Relationship<T>>().First;
				while (first != 0 && e.World.Exists(first))
				{
					var next = e.World.Get<Relationship<T>>(first).Next;
					e.World.Delete(first);
					first = next;
				}
			}

			if (e.Has<Child<T>>())
			{
				ref var rel = ref e.Get<Relationship<T>>();
				if (rel.Parent != 0 && e.World.Exists(rel.Parent))
					e.World.Entity(rel.Parent).RemoveChild<T>(e);
			}
		};
	}

	public static void AddChild<T>(this EntityView entity, EcsID child) where T : struct
		=> entity.World.AddChild<T>(entity.ID, child);

	public static void RemoveChild<T>(this EntityView entity, EcsID child) where T : struct
		=> entity.World.RemoveChild<T>(entity.ID, child);

	public static void ClearChildren<T>(this EntityView entity) where T : struct
		=> entity.World.ClearChildren<T>(entity.ID);

	public static RelationshipIterator<T> Children<T>(this EntityView entity) where T : struct
	{
		if (!entity.Has<Relationship<T>>())
			return default;

		return entity.Get<Relationship<T>>().Children(entity.World)!;
	}

	public static void AddChild<T>(this World world, EcsID parentId, EcsID childId) where T : struct
    {
		var hierarchy = world.Entity<T>();
		if (!hierarchy.Has<T>())
		{
			hierarchy.Set<T>();
			world.BindDeletion<T>();
		}

		if (!world.Has<Relationship<T>>(parentId))
		{
			world.Set(parentId, new Relationship<T>());
		}

		if (!world.Has<Relationship<T>>(childId))
		{
			world.Set(childId, new Relationship<T>());
		}

		ref var parentRelationship = ref world.Get<Relationship<T>>(parentId);
        ref var childRelationship = ref world.Get<Relationship<T>>(childId);

        // Update child's parent
        childRelationship.Parent = parentId;

        if (parentRelationship.Count == 0)
        {
            parentRelationship.First = childId;
            parentRelationship.Count = 1;
        }
        else
        {
            // Traverse to the end of the children list
            var current = parentRelationship.First;
            while (world.Get<Relationship<T>>(current).Next != 0)
            {
                current = world.Get<Relationship<T>>(current).Next;
            }

            // Add the child at the end
            ref var lastRelationship = ref world.Get<Relationship<T>>(current);
            lastRelationship.Next = childId;
            childRelationship.Prev = current;
            parentRelationship.Count++;
        }

		world.Set<Parent<T>>(parentId);
		world.Set<Child<T>>(childId);
    }

    public static void RemoveChild<T>(this World world, EcsID parentId, EcsID childId) where T : struct
    {
		if (!world.Has<Relationship<T>>(parentId))
			return;

		ref var parentRelationship = ref world.Get<Relationship<T>>(parentId);
        ref var childRelationship = ref world.Get<Relationship<T>>(childId);

        if (childRelationship.Prev != 0)
        {
            // Update previous sibling's next pointer
            ref var prevRelationship = ref world.Get<Relationship<T>>(childRelationship.Prev);
            prevRelationship.Next = childRelationship.Next;
        }
        else
        {
            // Update parent's first child pointer if removing the first child
            parentRelationship.First = childRelationship.Next;
        }

        if (childRelationship.Next != 0)
        {
            // Update next sibling's previous pointer
            ref var nextRelationship = ref world.Get<Relationship<T>>(childRelationship.Next);
            nextRelationship.Prev = childRelationship.Prev;
        }

        // Reset child's parent and sibling pointers
        childRelationship.Parent = 0;
        childRelationship.Next = 0;
        childRelationship.Prev = 0;
        parentRelationship.Count--;

		world.Unset<Relationship<T>>(childId);
		world.Unset<Child<T>>(childId);

		if (parentRelationship.Count <= 0)
		{
			world.Unset<Parent<T>>(parentId);

			if (parentRelationship.Parent == 0)
				world.Unset<Relationship<T>>(parentId);
		}
    }

    public static void ClearChildren<T>(this World world, EcsID parentId) where T : struct
    {
		if (!world.Has<Relationship<T>>(parentId))
			return;

        ref var parentRelationship = ref world.Get<Relationship<T>>(parentId);
        var currentChild = parentRelationship.First;

        while (currentChild != 0)
        {
            var nextChild = world.Get<Relationship<T>>(currentChild).Next;
            world.RemoveChild<T>(parentId, currentChild);
            currentChild = nextChild;
        }

		world.Unset<Parent<T>>(parentId);
    }
}

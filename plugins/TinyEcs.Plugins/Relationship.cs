namespace TinyEcs;

public struct Relationship
{
    public int Count;
    public EcsID First;
    public EcsID Parent;
    public EcsID Next;
    public EcsID Prev;
}

public readonly struct Parent { }
public readonly struct Child { }

public static class RelationshipPlugin
{
	public static void AddChild(this EntityView entity, EcsID child)
		=> entity.World.AddChild(entity.ID, child);

	public static void RemoveChild(this EntityView entity, EcsID child)
		=> entity.World.RemoveChild(entity.ID, child);

	public static void ClearChildren(this EntityView entity)
		=> entity.World.ClearChildren(entity.ID);

	public static void AddChild(this World world, EcsID parentId, EcsID childId)
    {
		if (!world.Has<Relationship>(parentId))
		{
			world.Set(parentId, new Relationship());
		}

		if (!world.Has<Relationship>(childId))
		{
			world.Set(childId, new Relationship());
		}

		ref var parentRelationship = ref world.Get<Relationship>(parentId);
        ref var childRelationship = ref world.Get<Relationship>(childId);

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
            while (world.Get<Relationship>(current).Next != 0)
            {
                current = world.Get<Relationship>(current).Next;
            }

            // Add the child at the end
            ref var lastRelationship = ref world.Get<Relationship>(current);
            lastRelationship.Next = childId;
            childRelationship.Prev = current;
            parentRelationship.Count++;
        }

		world.Set<Parent>(parentId);
		world.Set<Child>(childId);
    }

    public static void RemoveChild(this World world, EcsID parentId, EcsID childId)
    {
		if (!world.Has<Relationship>(parentId))
			return;

		ref var parentRelationship = ref world.Get<Relationship>(parentId);
        ref var childRelationship = ref world.Get<Relationship>(childId);

        if (childRelationship.Prev != 0)
        {
            // Update previous sibling's next pointer
            ref var prevRelationship = ref world.Get<Relationship>(childRelationship.Prev);
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
            ref var nextRelationship = ref world.Get<Relationship>(childRelationship.Next);
            nextRelationship.Prev = childRelationship.Prev;
        }

        // Reset child's parent and sibling pointers
        childRelationship.Parent = 0;
        childRelationship.Next = 0;
        childRelationship.Prev = 0;
        parentRelationship.Count--;

		world.Unset<Relationship>(childId);
		world.Unset<Child>(childId);

		if (parentRelationship.Count <= 0)
		{
			world.Unset<Parent>(parentId);

			if (parentRelationship.Parent == 0)
				world.Unset<Relationship>(parentId);
		}
    }

    public static void ClearChildren(this World world, EcsID parentId)
    {
		if (!world.Has<Relationship>(parentId))
			return;

        ref var parentRelationship = ref world.Get<Relationship>(parentId);
        var currentChild = parentRelationship.First;

        while (currentChild != 0)
        {
            var nextChild = world.Get<Relationship>(currentChild).Next;
            world.RemoveChild(parentId, currentChild);
            currentChild = nextChild;
        }

		world.Unset<Parent>(parentId);
    }
}

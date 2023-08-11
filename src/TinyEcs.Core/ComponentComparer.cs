namespace TinyEcs;

sealed class ComponentComparer : IComparer<EntityID>, IComparer<EcsComponent>
{
	private readonly World _world;

	public ComponentComparer(World world)
	{
		_world = world;
	}

	public int Compare(EcsComponent x, EcsComponent y)
	{
		return Compare(x.ID, y.ID);
	}

	public int Compare(EntityID x, EntityID y)
	{
		if (IDOp.IsPair(x) && IDOp.IsPair(y))
		{
			if (IDOp.GetPairFirst(x) == IDOp.GetPairFirst(y))
			{
				var any = _world.Component<EcsAny>(true).ID;
				var secondY = IDOp.GetPairSecond(y);

				if (secondY == any)
				{
					return 0;
				}
			}
		}

		return x.CompareTo(y);
	}
}

namespace TinyEcs;

sealed class ComponentComparer : IComparer<EcsComponent>
{
	private readonly World _world;

	public ComponentComparer(World world)
	{
		_world = world;
	}

	public int Compare(EcsComponent x, EcsComponent y)
	{
		if (IDOp.IsPair(x.ID) && IDOp.IsPair(y.ID))
		{
			if (IDOp.GetPairFirst(x.ID) == IDOp.GetPairFirst(y.ID))
			{
				var any = _world.Component<EcsAny>(true).ID;
				var secondY = IDOp.GetPairSecond(y.ID);

				if (secondY == any)
				{
					return 0;
				}
			}
		}

		return (x.ID, x.Size).CompareTo((y.ID, y.Size));
	}
}

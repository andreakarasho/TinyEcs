namespace TinyEcs;

sealed class ComponentComparer : IComparer<EntityID>, IComparer<Term>, IComparer<EcsComponent>
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
		return CompareTerms(_world, x, y);
	}

	public int Compare(Term x, Term y)
	{
		return CompareTerms(_world, x.ID, y.ID);
	}

	public static int CompareTerms(World world, EntityID a, EntityID b)
	{
		if (IDOp.IsPair(a) && IDOp.IsPair(b))
		{
			if (IDOp.GetPairFirst(a) == IDOp.GetPairFirst(b))
			{
				var any = world.Component<EcsAny>().ID;
				var secondY = IDOp.GetPairSecond(b);

				if (secondY == any)
				{
					return 0;
				}
			}
		}

		return a.CompareTo(b);
	}
}

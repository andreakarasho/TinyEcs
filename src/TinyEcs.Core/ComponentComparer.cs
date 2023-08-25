namespace TinyEcs;

sealed class ComponentComparer<TContext> : IComparer<EcsID>, IComparer<Term>, IComparer<EcsComponent>
{
	private readonly World<TContext> _world;

	public ComponentComparer(World<TContext> world)
	{
		_world = world;
	}


	public int Compare(EcsComponent x, EcsComponent y)
	{
		return CompareTerms(_world, x.ID, y.ID);
	}

	public int Compare(EcsID x, EcsID y)
	{
		return CompareTerms(_world, x, y);
	}

	public int Compare(Term x, Term y)
	{
		return CompareTerms(_world, x.ID, y.ID);
	}

	public static int CompareTerms(World<TContext> world, ulong a, ulong b)
	{
		if (IDOp.IsPair(a) && IDOp.IsPair(b))
		{
			if (IDOp.GetPairFirst(a) == IDOp.GetPairFirst(b))
			{
				var any = world.Entity<EcsAny>();
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

namespace TinyEcs;

sealed class ComponentComparer : IComparer<ulong>, IComparer<Term>, IComparer<ComponentInfo>
{
	private readonly World _world;

	public ComponentComparer(World world)
	{
		_world = world;
	}


	public int Compare(ComponentInfo x, ComponentInfo y)
	{
		return CompareTerms(_world, x.ID, y.ID);
	}

	public int Compare(ulong x, ulong y)
	{
		return CompareTerms(_world, x, y);
	}

	public int Compare(Term x, Term y)
	{
		return CompareTerms(_world, x.ID, y.ID);
	}

	public static int CompareTerms(World world, ulong a, ulong b)
	{
		if (IDOp.IsPair(a) && IDOp.IsPair(b))
		{
			if (IDOp.GetPairFirst(a) == IDOp.GetPairFirst(b))
			{
				var secondY = IDOp.GetPairSecond(b);

				if (secondY == Wildcard.ID)
				{
					return 0;
				}
			}
			else if (IDOp.GetPairSecond(a) == IDOp.GetPairSecond(b))
			{
				var firstY = IDOp.GetPairFirst(b);

				if (firstY == Wildcard.ID)
				{
					return 0;
				}
			}
			// TODO: fix (*, *)
			// else if (IDOp.GetPairFirst(a) == Wildcard.ID)
			// {
			// 	return 0;
			// }
			// else if (IDOp.GetPairSecond(b) == Wildcard.ID)
			// {
			// 	return 0;
			// }
		}

		return a.CompareTo(b);
	}
}

namespace TinyEcs;

sealed class ComponentComparer : IComparer<int>, IComparer<Term>, IComparer<EcsComponent>
{
	private readonly World _world;

	public ComponentComparer(World world)
	{
		_world = world;
	}


	public int Compare(EcsComponent x, EcsComponent y)
	{
		return CompareTerms(_world, x.ID, y.ID);
	}

	public int Compare(int x, int y)
	{
		return CompareTerms(_world, x, y);
	}

	public int Compare(Term x, Term y)
	{
		return CompareTerms(_world, x.ID, y.ID);
	}

	public static int CompareTerms(World world, int a, int b)
	{
		//if (IDOp.IsPair(a) && IDOp.IsPair(b))
		//{
		//	if (IDOp.GetPairFirst(a) == IDOp.GetPairFirst(b))
		//	{
		//		var secondY = IDOp.GetPairSecond(b);

		//		if (secondY == Lookup.Entity<EcsAny>.Component.ID)
		//		{
		//			return 0;
		//		}
		//	}
		//}

		return a.CompareTo(b);
	}
}

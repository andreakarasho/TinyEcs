using System.Diagnostics.CodeAnalysis;

using static TinyEcs.Defaults;

namespace TinyEcs;

sealed class ComponentComparer :
	IComparer<ulong>,
	//IComparer<Term>,
	IComparer<ComponentInfo>
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

	// public int Compare(Term x, Term y)
	// {
	// 	return CompareTerms(_world, x.ID, y.ID);
	// }

	public static int CompareTerms(World world, ulong a, ulong b)
	{
#if USE_PAIR
		if (IDOp.IsPair(a) && IDOp.IsPair(b))
		{
			var actionA = IDOp.GetPairFirst(a);
			var targetA = IDOp.GetPairSecond(a);
			var actionB = IDOp.GetPairFirst(b);
			var targetB = IDOp.GetPairSecond(b);

			if (actionB == Wildcard.ID && targetB == Wildcard.ID)  // (*, *) case
			{
				return 0;
			}
			else if (actionB == Wildcard.ID || targetB == Wildcard.ID)  // Other wildcard cases
			{
				// If either actionB or targetB is a wildcard, handle those comparisons
				if (actionA == actionB || targetA == targetB)
				{
					return 0;
				}
			}
		}
#endif

		return a.CompareTo(b);
	}
}

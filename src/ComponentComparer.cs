using System.Diagnostics.CodeAnalysis;

using static TinyEcs.Defaults;

namespace TinyEcs;

sealed class ComponentComparer :
	IComparer<ulong>,
	IComparer<Term>,
	IComparer<ComponentInfo>,
	IEqualityComparer<ulong>,
	IEqualityComparer<ComponentInfo>
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

		return a.CompareTo(b);
	}

	public bool Equals(ulong x, ulong y)
	{
		return CompareTerms(_world, x, y) == 0;
	}

	public int GetHashCode([DisallowNull] ulong obj)
	{
		return IDOp.IsPair(obj) &&
			(IDOp.GetPairFirst(obj) == Wildcard.ID || IDOp.GetPairSecond(obj) == Wildcard.ID) ?
			 1 : obj.GetHashCode();
	}

	public bool Equals(ComponentInfo x, ComponentInfo y)
	{
		return CompareTerms(_world, x.ID, y.ID) == 0;
	}

	public int GetHashCode([DisallowNull] ComponentInfo obj)
	{
		return obj.ID.GetHashCode();
	}
}

namespace TinyEcs;

public struct Term : IComparable<Term>
{
	public ulong ID;
	public TermOp Op;


	public static Term With(EcsID id)
		=> new () { ID = id, Op = TermOp.With };

	public static Term Without(EcsID id)
		=> new () { ID = id, Op = TermOp.Without };

	public readonly int CompareTo(Term other)
	{
		return ID.CompareTo(other.ID);
	}

	public static implicit operator EcsID(Term id) => id.ID;
	public static implicit operator Term(EcsID id) => With(id);

	public static Term operator !(Term id) => id.Not();
	public static Term operator -(Term id) => id.Not();
	public static Term operator +(Term id) => With(id);
}

public static class TermExt
{
	public static Term Not(this ref Term term)
	{
		term.Op = TermOp.Without;
		return term;
	}
}

public enum TermOp : byte
{
	With,
	Without
}

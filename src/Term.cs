namespace TinyEcs;

[DebuggerDisplay("{ID} - {Op}")]
public struct Term : IComparable<Term>
{
    public int ID;
    public TermOp Op;

    public static Term With(int id) => new() { ID = id, Op = TermOp.With };

    public static Term Without(int id) => new() { ID = id, Op = TermOp.Without };

    public readonly int CompareTo(Term other)
    {
        return ID.CompareTo(other.ID);
    }

    public static implicit operator int(Term id) => id.ID;

    public static implicit operator Term(int id) => With(id);

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

public readonly struct With<T> : IFilter where T : struct
{
	public static implicit operator Term(With<T> _) => Term.With(Lookup.Entity<T>.Component.ID);
}

public readonly struct Without<T> : IFilter where T : struct
{
	public static implicit operator Term(Without<T> _) => Term.Without(Lookup.Entity<T>.Component.ID);
}

public readonly struct Not<T> : IFilter where T : struct
{
	public static implicit operator Term(Not<T> _) => Term.Without(Lookup.Entity<T>.Component.ID);
}

public readonly struct Or<T> : IFilter where T : struct { }

public interface IFilter { }

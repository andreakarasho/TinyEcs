using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

public interface IQueryTerm : IComparable<IQueryTerm>
{
	EcsID Id { get; init; }
	TermOp Op { get; init; }

	int IComparable<IQueryTerm>.CompareTo([NotNull] IQueryTerm? other)
	{
		var res = Id.CompareTo(other!.Id);
		if (res != 0)
			return res;
		return Op.CompareTo(other.Op);
	}

	public static RollingHash GetHash(params ReadOnlySpan<IQueryTerm> terms)
	{
		var roll = new RollingHash();
		foreach (ref readonly var term in terms)
		{
			roll.Add(term.Id);
			roll.Add((ulong)term.Op);

			if (term is ContainerQueryTerm container)
				roll.Add(container.Terms.Aggregate(0Ul, static (a, b) => a + b.Id));
		}
		return roll;
	}
}

[DebuggerDisplay("{Id} - {Op}")]
public readonly struct QueryTerm(EcsID id, TermOp op) : IQueryTerm
{
	public EcsID Id { get; init; } = id;
	public TermOp Op { get; init; } = op;
}

[DebuggerDisplay("{Id} - {Op} - {Terms}")]
public class ContainerQueryTerm(IQueryTerm[] terms, TermOp op) : IQueryTerm
{
	public EcsID Id { get; init; } = 0;
	public TermOp Op { get; init; } = op;
	public ImmutableArray<IQueryTerm> Terms { get; } = [.. terms];
}

public enum TermOp : byte
{
	DataAccess,
	With,
    Without,
    Optional,
    AtLeastOne,
    Exactly,
    None,
	Or
}

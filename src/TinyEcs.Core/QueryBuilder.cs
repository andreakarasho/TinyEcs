namespace TinyEcs;

[SkipLocalsInit]
public unsafe ref struct QueryBuilder<TContext>
{
	const int TERMS_COUNT = 16;

	private readonly World<TContext> _world;
	private EcsID _id;
	private int _termIndex;
	private unsafe fixed byte _terms[TERMS_COUNT * (sizeof(uint) + sizeof(byte))];

	private ref Term CurrentTerm
	{
		get
		{
			fixed (byte* termPtr = &_terms[_termIndex * sizeof(Term)])
			{
				return ref Unsafe.AsRef<Term>(termPtr);
			}
		}
	}

	internal Span<Term> Terms
	{
		get
		{
			fixed (byte* termPtr = &_terms[0])
			{
				return new Span<Term>(termPtr, _termIndex);
			}
		}
	}

	internal Span<Term> AllTerms
	{
		get
		{
			fixed (byte* termPtr = &_terms[0])
			{
				return new Span<Term>(termPtr, TERMS_COUNT);
			}
		}
	}

	internal QueryBuilder(World<TContext> world)
	{
		_world = world;
	}

	public QueryBuilder<TContext> With<T>() where T : unmanaged, IComponentStub
		=> With(_world.Component<T>().ID);

	public QueryBuilder<TContext> With<TKind, TTarget>()
	where TKind : unmanaged, IComponentStub
	where TTarget : unmanaged, IComponentStub
		=> With(_world.Component<TKind>().ID, _world.Component<TTarget>().ID);

	public QueryBuilder<TContext> With<TKind>(EcsID target)
	where TKind : unmanaged, IComponentStub
		=> With(IDOp.Pair(_world.Component<TKind>().ID, target));

	public QueryBuilder<TContext> With(EcsID first, EcsID second)
		=> With(IDOp.Pair(first, second));

	public QueryBuilder<TContext> With(EcsID id)
	{
		EcsAssert.Assert(_termIndex + 1 < TERMS_COUNT);

		ref var term = ref CurrentTerm;
		term.ID = id;
		term.Op = TermOp.With;

		_termIndex += 1;

		return this;
	}

	public QueryBuilder<TContext> Without<T>() where T : unmanaged, IComponentStub
		=> Without(_world.Component<T>().ID);

	public QueryBuilder<TContext> Without<TKind, TTarget>()
	where TKind : unmanaged, IComponentStub
	where TTarget : unmanaged, IComponentStub
		=> Without(_world.Component<TKind>().ID, _world.Component<TTarget>().ID);

	public QueryBuilder<TContext> Without<TKind>(EcsID target)
	where TKind : unmanaged, IComponentStub
		=> Without(IDOp.Pair(_world.Component<TKind>().ID, target));

	public QueryBuilder<TContext> Without(EcsID first, EcsID second)
		=> Without(IDOp.Pair(first, second));

	public QueryBuilder<TContext> Without(EcsID id)
	{
		EcsAssert.Assert(_termIndex + 1 < TERMS_COUNT);

		ref var term = ref CurrentTerm;
		term.ID = id;
		term.Op = TermOp.Without;

		_termIndex += 1;

		return this;
	}

	public EntityView<TContext> Build()
	{
		if (_id != 0)
			return _world.Entity(_id);

		var ent = _world.New()
			.Set<EcsPanic, EcsDelete>();

		_id = ent.ID;

		return ent;
	}

	public unsafe void Iterate(IteratorDelegate<TContext> action)
	{
		_world.Query(Terms, &IterateSys, action);

		static void IterateSys(ref Iterator<TContext> it)
		{
			if (it.UserData is IteratorDelegate<TContext> del)
				del.Invoke(ref it);
		}
	}
}

public struct Term : IComparable<Term>
{
	public ulong ID;
	public TermOp Op;

	public Term With2 { get { Op = TermOp.With; return this; } }
	public Term Without2 { get { Op = TermOp.With; return this; } }

	public static Term With(EcsID id)
		=> new () { ID = id, Op = TermOp.With };

	public static Term Without(EcsID id)
		=> new () { ID = id, Op = TermOp.Without };

	public int CompareTo(Term other)
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
	public static Term Not(this Term term)
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

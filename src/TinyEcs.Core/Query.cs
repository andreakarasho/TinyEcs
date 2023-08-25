namespace TinyEcs;

[SkipLocalsInit]
public unsafe ref struct Query<TContext>
{
	const int TERMS_COUNT = 16;

	private readonly World<TContext> _world;
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

	internal Query(World<TContext> world)
	{
		_world = world;
	}

	public Query<TContext> With<T>() where T : unmanaged, IComponentStub
		=> With(_world.Component<T>().ID);

	public Query<TContext> With<TKind, TTarget>()
	where TKind : unmanaged, IComponentStub
	where TTarget : unmanaged, IComponentStub
		=> With(_world.Component<TKind>().ID, _world.Component<TTarget>().ID);

	public Query<TContext> With<TKind>(EcsID target)
	where TKind : unmanaged, IComponentStub
		=> With(IDOp.Pair(_world.Component<TKind>().ID, target));

	public Query<TContext> With(EcsID first, EcsID second)
		=> With(IDOp.Pair(first, second));

	public Query<TContext> With(EcsID id)
	{
		EcsAssert.Assert(_termIndex + 1 < TERMS_COUNT);

		ref var term = ref CurrentTerm;
		term.ID = id;
		term.Op = TermOp.With;

		_termIndex += 1;

		return this;
	}

	public Query<TContext> Without<T>() where T : unmanaged, IComponentStub
		=> Without(_world.Component<T>().ID);

	public Query<TContext> Without<TKind, TTarget>()
	where TKind : unmanaged, IComponentStub
	where TTarget : unmanaged, IComponentStub
		=> Without(_world.Component<TKind>().ID, _world.Component<TTarget>().ID);

	public Query<TContext> Without<TKind>(EcsID target)
	where TKind : unmanaged, IComponentStub
		=> Without(IDOp.Pair(_world.Component<TKind>().ID, target));

	public Query<TContext> Without(EcsID first, EcsID second)
		=> Without(IDOp.Pair(first, second));

	public Query<TContext> Without(EcsID id)
	{
		EcsAssert.Assert(_termIndex + 1 < TERMS_COUNT);

		ref var term = ref CurrentTerm;
		term.ID = id;
		term.Op = TermOp.Without;

		_termIndex += 1;

		return this;
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

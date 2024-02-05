namespace TinyEcs;

[SkipLocalsInit]
public unsafe ref struct Query
{
    const int TERMS_COUNT = 16;

    private readonly World _world;
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

    internal Query(World world)
    {
        _world = world;
    }

    public Query With<T>() => With(_world.Component<T>().ID);

    public Query With(EcsID id)
    {
        EcsAssert.Assert(_termIndex + 1 < TERMS_COUNT);

        ref var term = ref CurrentTerm;
        term.ID = id;
        term.Op = TermOp.With;

        _termIndex += 1;

        return this;
    }

    public Query Without<T>() => Without(_world.Component<T>().ID);

    public Query Without(EcsID id)
    {
        EcsAssert.Assert(_termIndex + 1 < TERMS_COUNT);

        ref var term = ref CurrentTerm;
        term.ID = id;
        term.Op = TermOp.Without;

        _termIndex += 1;

        return this;
    }

    public unsafe void Iterate(IteratorDelegate action) => _world.Query(Terms, action);

	public void System(IteratorDelegate fn) => System<EcsSystemPhaseOnUpdate>(fn);
	
	public void System<TPhase>(IteratorDelegate fn)
	{
		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem(fn, query, terms, float.NaN))
			.Set<TPhase>();
	}
}

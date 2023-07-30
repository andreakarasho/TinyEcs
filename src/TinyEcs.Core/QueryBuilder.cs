namespace TinyEcs;

public unsafe ref struct QueryBuilder
{
	const int TERMS_COUNT = 16;

	private readonly World _world;
	private EntityID _id;
	private int _termIndex;
	private unsafe fixed byte _terms[TERMS_COUNT * (sizeof(uint) + sizeof(byte))];

	private Span<Term> _allTerms
	{
		get
		{
			fixed (byte* termPtr = &_terms[0])
			{
				return new Span<Term>(termPtr, TERMS_COUNT);
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

	internal QueryBuilder(World world)
	{
		_world = world;
	}

	public QueryBuilder With<T>() where T : unmanaged
		=> With(_world.Component<T>());

	public QueryBuilder With<TKind, TTarget>() where TKind : unmanaged where TTarget : unmanaged
		=> With(_world.Component<TKind>(), _world.Component<TTarget>());

	public QueryBuilder With(EntityID first, EntityID second)
		=> With(IDOp.Pair(first, second));

	public QueryBuilder With(EntityID id)
	{
		EcsAssert.Assert(_termIndex + 1 < TERMS_COUNT);

		ref var term = ref _allTerms[_termIndex++];
		term.ID = id;
		term.Op = TermOp.With;

		return this;
	}

	public QueryBuilder Without<T>() where T : unmanaged
		=> Without(_world.Component<T>());

	public QueryBuilder Without<TKind, TTarget>() where TKind : unmanaged where TTarget : unmanaged
		=> Without(_world.Component<TKind>(), _world.Component<TTarget>());

	public QueryBuilder Without(EntityID first, EntityID second)
		=> Without(IDOp.Pair(first, second));

	public QueryBuilder Without(EntityID id)
	{
		EcsAssert.Assert(_termIndex + 1 < TERMS_COUNT);

		ref var term = ref _allTerms[_termIndex++];
		term.ID = id;
		term.Op = TermOp.Without;

		return this;
	}


	public EntityView Build()
	{
		if (_id != 0)
			return new EntityView(_world, _id);

		var ent = _world.Spawn()
			.Set<EcsQueryBuilder>();

		_id = ent.ID;

		return ent;
	}

	public unsafe void Iterate(IteratorDelegate action, Commands? commands = null)
	{
		_world.Query(Terms, commands, &IterateSys, action);
	}

	static void IterateSys(ref Iterator it)
	{
		if (it.UserData is IteratorDelegate del)
			del.Invoke(ref it);
	}
}

public struct Term
{
	public EntityID ID;
	public TermOp Op;
}

public enum TermOp : byte
{
	With,
	Without
}

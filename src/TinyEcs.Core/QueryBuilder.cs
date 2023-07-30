namespace TinyEcs;

public unsafe ref struct QueryBuilder
{
	const int TERMS_COUNT = 16;

	private readonly World _world;
	private EntityID _id;
	private int _termIndex;
	private unsafe fixed byte _terms[TERMS_COUNT * (sizeof(uint) + sizeof(byte))];

	private Span<Term> Terms
	{
		get
		{
			fixed (byte* termPtr = &_terms[0])
			{
				return new Span<Term>(termPtr, TERMS_COUNT);
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

	public QueryBuilder With(EntityID id)
	{
		EcsAssert.Assert(_termIndex + 1 < TERMS_COUNT);

		ref var term = ref Terms[_termIndex++];
		term.ID = id;
		term.Op = TermOp.With;

		return this;
	}

	public QueryBuilder With(EntityID first, EntityID second)
	{
		EcsAssert.Assert(_termIndex + 1 < TERMS_COUNT);

		ref var term = ref Terms[_termIndex++];
		term.ID = IDOp.Pair(first, second);
		term.Op = TermOp.With;

		return this;
	}

	public QueryBuilder Without<T>() where T : unmanaged
		=> Without(_world.Component<T>());

	public QueryBuilder Without<TKind, TTarget>() where TKind : unmanaged where TTarget : unmanaged
		=> Without(_world.Component<TKind>(), _world.Component<TTarget>());

	public QueryBuilder Without(EntityID id)
	{
		EcsAssert.Assert(_termIndex + 1 < TERMS_COUNT);

		ref var term = ref Terms[_termIndex++];
		term.ID = id;
		term.Op = TermOp.Without;

		return this;
	}

	public QueryBuilder Without(EntityID first, EntityID second)
	{
		EcsAssert.Assert(_termIndex + 1 < TERMS_COUNT);

		ref var term = ref Terms[_termIndex++];
		term.ID = IDOp.Pair(first, second);
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


		var terms = Terms.Slice(0, _termIndex);
		Span<byte> empty = stackalloc byte[1];

		foreach (ref var term in terms)
		{
			_world.Set(ent.ID, term.ID | term.Op switch
				{
					TermOp.With => EcsConst.ECS_QUERY_WITH,
					TermOp.Without => EcsConst.ECS_QUERY_WITHOUT,
					_ => 0
				}
				,
				empty
			);
		}
		// fixed (byte* ptr = _terms)
		// {
		// 	var size = sizeof(Term) * _termIndex;
		// 	var rawTerms = new ReadOnlySpan<byte>(ptr, size);

		// 	var kindID = _world.Component<EcsQueryTerms>();

		// 	for (int i = 0; i < terms.Length; ++i)
		// 	{
		// 		ref var term = ref terms[i];

		// 		_world.Set(ent.ID, IDOp.Pair(kindID, (EntityID) i), rawTerms[(i * sizeof(Term)) .. sizeof(Term)]);
		// 	}
		// }

		return ent;
	}

	public unsafe void Iterate(IteratorDelegate action, Commands? commands = null)
	{
		var terms = Terms.Slice(0, _termIndex);
		if (terms.IsEmpty)
			return;

		Span<EntityID> termIDs = stackalloc EntityID[terms.Length];
		var withIdx = 0;
		var withoutIdx = termIDs.Length - 1;
		for (int i = 0; i < terms.Length; ++i)
		{
			ref var term = ref terms[i];
			if (term.Op == TermOp.With)
			{
				termIDs[withIdx++] = term.ID;
			}
			else
			{
				termIDs[withoutIdx--] = term.ID;
			}
		}

		_world.Query(termIDs.Slice(0, withIdx), termIDs.Slice(withIdx), commands, &IterateSys, action);

		// terms.Sort(static (a, b) => a.Op.CompareTo(b.Op));

		// var first = terms[0];
		// var index = 1;
		// for (; index < terms.Length; ++index)
		// {
		// 	if (terms[index].Op != first.Op)
		// 	{
		// 		break;
		// 	}
		// }

		// var slice0 = terms.Slice(0, index);
		// var slice1 = terms.Slice(index);

		// if (slice0[0].Op == TermOp.With)
		// {
		// 	_world.Query(slice0, slice1, commands, &IterateSys, action);
		// }
		// else
		// {
		// 	_world.Query(slice1, slice0, commands, &IterateSys, action);
		// }
	}

	static void IterateSys(ref Iterator it)
	{
		if (it.UserData is IteratorDelegate del)
			del.Invoke(ref it);
	}

	struct Term
	{
		public EntityID ID;
		public TermOp Op;
	}

	enum TermOp : byte
	{
		With,
		Without
	}
}

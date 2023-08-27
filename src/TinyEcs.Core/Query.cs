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

	public Query With<T>() where T : unmanaged, IComponentStub
		=> With(_world.Entity<T>());

	public Query With<TKind, TTarget>()
	where TKind : unmanaged, IComponentStub
	where TTarget : unmanaged, IComponentStub
		=> With(_world.Entity<TKind>(), _world.Entity<TTarget>());

	public Query With<TKind>(EcsID target)
	where TKind : unmanaged, IComponentStub
		=> With(IDOp.Pair(_world.Entity<TKind>(), target));

	public Query With(EcsID first, EcsID second)
		=> With(IDOp.Pair(first, second));

	public Query With(EcsID id)
	{
		EcsAssert.Assert(_termIndex + 1 < TERMS_COUNT);

		ref var term = ref CurrentTerm;
		term.ID = id;
		term.Op = TermOp.With;

		_termIndex += 1;

		return this;
	}

	public Query Without<T>() where T : unmanaged, IComponentStub
		=> Without(_world.Entity<T>());

	public Query Without<TKind, TTarget>()
	where TKind : unmanaged, IComponentStub
	where TTarget : unmanaged, IComponentStub
		=> Without(_world.Entity<TKind>(), _world.Entity<TTarget>());

	public Query Without<TKind>(EcsID target)
	where TKind : unmanaged, IComponentStub
		=> Without(IDOp.Pair(_world.Entity<TKind>(), target));

	public Query Without(EcsID first, EcsID second)
		=> Without(IDOp.Pair(first, second));

	public Query Without(EcsID id)
	{
		EcsAssert.Assert(_termIndex + 1 < TERMS_COUNT);

		ref var term = ref CurrentTerm;
		term.ID = id;
		term.Op = TermOp.Without;

		_termIndex += 1;

		return this;
	}

	public unsafe void Iterate(IteratorDelegate action)
	{
		// var ptr = (Callback*)NativeMemory.Alloc(1, (nuint) sizeof(Callback));
		// ptr->GCHandle = (nint) GCHandle.Alloc(action);
		// ptr->Func = Marshal.GetFunctionPointerForDelegate(action);

		// _world.Query(Terms, (delegate* <ref Iterator, void>)ptr->Func, action);

		_world.Query(Terms, &IterateSys, action);

		static void IterateSys(ref Iterator it)
		{
			if (it.UserData is IteratorDelegate del)
				del.Invoke(ref it);
		}
	}
}

struct Callback
{
	public nint Func;
	public nint GCHandle;
}

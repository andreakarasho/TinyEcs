namespace TinyEcs;

[SkipLocalsInit]
public sealed unsafe partial class Query : IDisposable
{
    public const int TERMS_COUNT = 25;

    private readonly World _world;
    private readonly Vec<Term> _terms = Vec<Term>.Init(TERMS_COUNT);

	private Span<Term> Terms => _terms.Span;

    internal Query(World world)
    {
        _world = world;
    }

    public void Dispose()
    {
	    _terms.Dispose();
    }

    public Query With<T>() where T : struct => With(_world.Component<T>().ID);

    private Query With(int id)
    {
	    if (Exists(id)) return this;

	    ref var term = ref _terms.AddRef();
	    term.ID = id;
        term.Op = TermOp.With;

		return this;
    }

    public Query Without<T>() where T : struct => Without(_world.Component<T>().ID);

    private Query Without(int id)
    {
	    if (Exists(id)) return this;

	    ref var term = ref _terms.AddRef();
		term.ID = id;
        term.Op = TermOp.Without;

		return this;
    }

    private bool Exists(int id)
    {
	    foreach (ref var term in _terms.Span)
		    if (term.ID == id)
			    return true;
	    return false;
    }

	private readonly List<Archetype> _cachedArchetypes = new List<Archetype>();

	public ArchetypeEnumerator GetEnumerator()
	{
		_cachedArchetypes.Clear();
		_world.FindArchetypes(Terms, _cachedArchetypes);

		return new ArchetypeEnumerator(CollectionsMarshal.AsSpan(_cachedArchetypes));
	}

	public ref struct ArchetypeEnumerator
	{
		private readonly Span<Archetype> _list;
		private int _index;

		public ArchetypeEnumerator(Span<Archetype> list)
		{
			_list = list;
			_index = -1;
		}

		public Archetype Current => _list[_index];

		public bool MoveNext() => ++_index < _list.Length;
	}
}

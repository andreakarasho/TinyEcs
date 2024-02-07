namespace TinyEcs;

[SkipLocalsInit]
public sealed unsafe partial class Query
{
    public const int TERMS_COUNT = 25;

    private readonly World _world;
	private readonly List<Term> _terms = new List<Term>();

	private Span<Term> Terms => CollectionsMarshal.AsSpan(_terms);

    internal Query(World world)
    {
        _world = world;
    }

    public Query With<T>() where T : struct => With(_world.Component<T>().ID);

    private Query With(int id)
    {
		var term = new Term();
        term.ID = id;
        term.Op = TermOp.With;

		_terms.Add(term);

		return this;
    }

    public Query Without<T>() where T : struct => Without(_world.Component<T>().ID);

    private Query Without(int id)
    {
		var term = new Term();
		term.ID = id;
        term.Op = TermOp.Without;

		_terms.Add(term);

		return this;
    }

    public void Iterate(IteratorDelegate action) => _world.Query(Terms, action);

	public void System(IteratorDelegate fn) => System<EcsSystemPhaseOnUpdate>(fn);

	public void System<TPhase>(IteratorDelegate fn) where TPhase : struct
	{
		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem(fn, query, terms, float.NaN))
			.Set<TPhase>();
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

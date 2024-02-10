namespace TinyEcs;

[SkipLocalsInit]
public sealed partial class Query : IDisposable
{
    public const int TERMS_COUNT = 25;

    private readonly World _world;
    private readonly Vec<Term> _terms = Vec<Term>.Init(TERMS_COUNT);

    internal Query(World world)
    {
        _world = world;
    }

    public void Dispose()
    {
	    _terms.Dispose();
    }

    public Query With<T>() where T : struct
	    => With(_world.Component<T>().ID);

    private Query With(int id)
    {
	    if (Exists(id)) return this;

	    ref var term = ref _terms.AddRef();
	    term.ID = id;
        term.Op = TermOp.With;

		return this;
    }

    public Query Without<T>() where T : struct
	    => Without(_world.Component<T>().ID);

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
		_world.FindArchetypes(_terms.Span, _cachedArchetypes);

		return new ArchetypeEnumerator(CollectionsMarshal.AsSpan(_cachedArchetypes));
	}

	static void QueryRec(Archetype root, Span<Term> sortedTerms)
	{
		var result = root.FindMatch(sortedTerms);
		if (result < 0)
		{
			return;
		}

		if (result == 0 && root.Count > 0)
		{
			// found
		}

		var span = CollectionsMarshal.AsSpan(root._edgesRight);
		if (span.IsEmpty)
		{
			return;
		}

		ref var start = ref MemoryMarshal.GetReference(span);
		ref var end = ref Unsafe.Add(ref start, span.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			QueryRec(start.Archetype, sortedTerms);

			start = ref Unsafe.Add(ref start, 1);
		}
	}

	public delegate void QueryTemplateWithEntity(ref readonly EntityView entity);
	public void EachWithEntity(QueryTemplateWithEntity fn)
	{
		foreach (var archetype in this)
		{
			foreach (ref readonly var chunk in archetype)
			{
				ref var firstEnt = ref chunk.Entities[0];
				ref var last = ref Unsafe.Add(ref firstEnt, chunk.Count);
				while (Unsafe.IsAddressLessThan(ref firstEnt, ref last))
				{
					fn(in firstEnt);

					firstEnt = ref Unsafe.Add(ref firstEnt, 1);
				}
			}
		}
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

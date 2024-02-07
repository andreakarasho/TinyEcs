namespace TinyEcs;

public sealed partial class Archetype
{
    const int ARCHETYPE_INITIAL_CAPACITY = 16;

    private readonly World _world;
    private readonly ComponentComparer _comparer;
    private int _capacity, _count;
    private EntityView[] _entities;
    private readonly int[] _lookup;
    internal List<EcsEdge> _edgesLeft, _edgesRight;

	private readonly Array[] _componentsData;


	internal Archetype(
        World world,
        ReadOnlySpan<EcsComponent> components,
        ComponentComparer comparer
    )
    {
        _comparer = comparer;
        _world = world;
        _capacity = ARCHETYPE_INITIAL_CAPACITY;
        _count = 0;
        _entities = new EntityView[ARCHETYPE_INITIAL_CAPACITY];
        _edgesLeft = new List<EcsEdge>();
        _edgesRight = new List<EcsEdge>();
        Components = components.ToArray();

        var maxID = -1;
        for (var i = 0; i < components.Length; ++i)
	        maxID = Math.Max(maxID, components[i].ID);

        _lookup = new int[maxID + 1];
        _lookup.AsSpan().Fill(-1);
        for (var i = 0; i < components.Length; ++i)
	        _lookup[components[i].ID] = i;

		_componentsData = new Array[Components.Length];
		ResizeComponentArray(_capacity);
	}

    internal EntityView[] Entities => _entities;
    public World World => _world;
    public int Count => _count;
    public readonly EcsComponent[] Components;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetComponentIndex(ref readonly EcsComponent cmp)
    {
	    return GetComponentIndex(cmp.ID);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal int GetComponentIndex(int id)
	{
		return id >= 0 && id < _lookup.Length ? _lookup[id] : -1;
	}

	internal int Add(EcsID id)
    {
        if (_capacity == _count)
        {
			_capacity <<= 3;

            Array.Resize(ref _entities, _capacity);
			ResizeComponentArray(_capacity);
		}

        _entities[_count] = new(_world, id);
        return _count++;
    }

    internal EcsID Remove(ref EcsRecord record)
    {
		var removed = SwapWithLast(record.Row);

		for (int i = 0; i < Components.Length; ++i)
		{
			var leftArray = RawComponentData(i);

			var tmp = leftArray.GetValue(_count - 1);
			leftArray.SetValue(tmp, record.Row);
		}

		--_count;

        return removed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Span<T> ComponentData<T>() where T : struct
    {
	    var column = GetComponentIndex(Lookup.Entity<T>.HashCode);
	    EcsAssert.Assert(column >= 0 && column < _componentsData.Length);

	    ref var array = ref Unsafe.As<Array, T[]>(ref _componentsData[column]);
	    return array.AsSpan(0, Count);
    }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Array RawComponentData(int column)
	{
		EcsAssert.Assert(column >= 0 && column < _componentsData.Length);

		return _componentsData[column];
	}

	internal Archetype InsertVertex(
        Archetype left,
        ReadOnlySpan<EcsComponent> components,
        ref readonly EcsComponent component
    )
    {
        var vertex = new Archetype(left._world, components, _comparer);
        MakeEdges(left, vertex, component.ID);
        InsertVertex(vertex);
        return vertex;
    }

    internal int MoveEntity(Archetype to, int fromRow)
    {
		var removed = SwapWithLast(fromRow);
		var toRow = to.Add(removed);
        MoveTo(fromRow, to, toRow);

        --_count;

        return toRow;
    }

    private void MoveTo(int fromRow, Archetype to, int toRow)
	{
		var isLeft = to.Components.Length < Components.Length;
		int i = 0,
			j = 0;
		var count = isLeft ? to.Components.Length : Components.Length;

		ref var x = ref (isLeft ? ref j : ref i);
		ref var y = ref (!isLeft ? ref j : ref i);

		var last = _count - 1;

		for (; x < count; ++x, ++y)
		{
			while (Components[i].ID != to.Components[j].ID)
			{
				// advance the sign with less components!
				++y;
			}

			var fromArray = RawComponentData(i);
			var toArray = to.RawComponentData(j);

			// copy the moved entity to the target archetype
			Array.Copy(fromArray, fromRow, toArray, toRow, 1);

			// swap last with the hole
			Array.Copy(fromArray, last, fromArray, fromRow, 1);
		}

		//_count = fromCount;
	}

	private void ResizeComponentArray(int capacity)
	{
		for (int i = 0; i < Components.Length; ++i)
		{
			var tmp = Lookup.GetArray(Components[i].ID, capacity);
			_componentsData[i]?.CopyTo(tmp!, 0);
			_componentsData[i] = tmp!;

			_capacity = capacity;
		}
	}

	internal void Clear()
    {
        _count = 0;
        _capacity = ARCHETYPE_INITIAL_CAPACITY;
        Array.Resize(ref _entities, _capacity);
    }

    internal void Optimize()
    {
        var pow = (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)_count);
        var newCapacity = Math.Max(ARCHETYPE_INITIAL_CAPACITY, pow);
        if (newCapacity < _capacity)
        {
            _capacity = newCapacity;
            Array.Resize(ref _entities, _capacity);
        }
    }

    private EcsID SwapWithLast(int fromRow)
    {
        ref var fromRec = ref _world.GetRecord(_entities[fromRow]);
        ref var lastRec = ref _world.GetRecord(_entities[_count - 1]);
        lastRec.Row = fromRec.Row;

        var removed = _entities[fromRow];
        _entities[fromRow] = _entities[_count - 1];

        return removed;
    }

    private static void MakeEdges(Archetype left, Archetype right, int id)
    {
        left._edgesRight.Add(new EcsEdge() { Archetype = right, ComponentID = id });
        right._edgesLeft.Add(new EcsEdge() { Archetype = left, ComponentID = id });
    }

    private void InsertVertex(Archetype newNode)
    {
        var nodeTypeLen = Components.Length;
        var newTypeLen = newNode.Components.Length;

        if (nodeTypeLen > newTypeLen - 1)
        {
            return;
        }

        if (nodeTypeLen < newTypeLen - 1)
        {
#if NET5_0_OR_GREATER
            foreach (ref var edge in CollectionsMarshal.AsSpan(_edgesRight))
#else
            foreach (var edge in _edgesRight)
#endif
            {
                edge.Archetype.InsertVertex(newNode);
            }

            return;
        }

        if (!IsSuperset(newNode.Components))
        {
            return;
        }

        var i = 0;
        var newNodeTypeLen = newNode.Components.Length;
        for (; i < newNodeTypeLen && Components[i].ID == newNode.Components[i].ID; ++i) { }

        MakeEdges(newNode, this, Components[i].ID);
    }

    internal bool IsSuperset(Span<EcsComponent> other)
    {
	    int i = 0, j = 0;
	    while (i < Components.Length && j < other.Length)
	    {
		    if (Components[i].ID == other[j].ID)
		    {
			    j++;
		    }

		    i++;
	    }

	    return j == other.Length;
    }

    internal int FindMatch(Span<Term> searching)
    {
	    var currents = Components;
	    var i = 0;
	    var j = 0;

        while (i < currents.Length && j < searching.Length)
        {
	        ref var current = ref currents[i];
	        ref var search = ref searching[j];

            if (current.ID.CompareTo(search.ID) == 0)
            {
                if (search.Op != TermOp.With)
                    return -1;

                ++j;
            }
            else if (current.ID > search.ID && search.Op != TermOp.With)
            {
	            ++j;
                continue;
            }

            ++i;
        }

        while (j < searching.Length && searching[j].Op != TermOp.With)
	        ++j;

        return i == j ? 0 : 1;
    }

    public void Print()
    {
        PrintRec(this, 0, 0);

        static void PrintRec(Archetype root, int depth, int rootComponent)
        {
            Console.WriteLine(
                "{0}Parent [{1}] common ID: {2}",
                new string('\t', depth),
                string.Join(", ", root.Components.Select(s => s.ID)),
                rootComponent
            );

            if (root._edgesRight.Count > 0)
                Console.WriteLine("{0}Children: ", new string('\t', depth));

            //Console.WriteLine("{0}[{1}] |{2}| - Table [{3}]", new string('.', depth), string.Join(", ", root.ComponentInfo.Select(s => s.ID)), rootComponent, string.Join(", ", root.Table.Components.Select(s => s.ID)));

            foreach (ref readonly var edge in CollectionsMarshal.AsSpan(root._edgesRight))
            {
                PrintRec(edge.Archetype, depth + 1, edge.ComponentID);
            }
        }
    }
}

struct EcsEdge
{
    public int ComponentID;
    public Archetype Archetype;
}

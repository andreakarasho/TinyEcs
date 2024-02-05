namespace TinyEcs;

public sealed class Archetype
{
    const int ARCHETYPE_INITIAL_CAPACITY = 16;

    private readonly World _world;
    private readonly ComponentComparer _comparer;
    private int _capacity, _count;
    private EntityView[] _entities;
    private int[] _entitiesTableRows;
    internal List<EcsEdge> _edgesLeft, _edgesRight;
    private readonly Table _table;

    internal Archetype(
        World world,
        Table table,
        ReadOnlySpan<EcsComponent> components,
        ComponentComparer comparer
    )
    {
        _comparer = comparer;
        _world = world;
        _table = table;
        _capacity = ARCHETYPE_INITIAL_CAPACITY;
        _count = 0;
        _entities = new EntityView[ARCHETYPE_INITIAL_CAPACITY];
        _entitiesTableRows = new int[ARCHETYPE_INITIAL_CAPACITY];
        _edgesLeft = new List<EcsEdge>();
        _edgesRight = new List<EcsEdge>();
        ComponentInfo = components.ToArray();
    }

    internal EntityView[] Entities => _entities;
    internal int[] EntitiesTableRows => _entitiesTableRows;
    public World World => _world;
    public int Count => _count;
    internal Table Table => _table;

    public readonly EcsComponent[] ComponentInfo;

    internal int GetComponentIndex(ref readonly EcsComponent cmp)
    {
        if (cmp.Size <= 0)
        {
            return Array.BinarySearch(ComponentInfo, cmp, _comparer);
        }

        return _table.GetComponentIndex(in cmp);
    }

    internal (int, int) Add(EcsID id, int tableRow = -1)
    {
        if (_capacity == _count)
        {
            _capacity *= 2;

            Array.Resize(ref _entities, _capacity);
            Array.Resize(ref _entitiesTableRows, _capacity);
        }

        _entities[_count] = new(_world, id);
        var row = tableRow < 0 ? _table.Add(id) : tableRow;
        _entitiesTableRows[_count] = row;

        return (_count++, row);
    }

    internal EcsID Remove(ref EcsRecord record)
    {
        (var removed, var removedRow) = SwapWithLast(record.Row);

        _table.Remove(removedRow);

        --_count;

        return removed;
    }

    internal Archetype InsertVertex(
        Archetype left,
        Table table,
        ReadOnlySpan<EcsComponent> components,
        ref readonly EcsComponent component
    )
    {
        var vertex = new Archetype(left._world, table, components, _comparer);
        MakeEdges(left, vertex, component.ID);
        InsertVertex(vertex);
        return vertex;
    }

    internal int MoveEntity(Archetype to, int fromRow)
    {
        (var removed, var removedRow) = SwapWithLast(fromRow);

        var sameTable = _table.Hash == to.Table.Hash;
        (var toRow, var toTableRow) = to.Add(removed, sameTable ? removedRow : -1);

        if (!sameTable)
            _table.MoveTo(removedRow, to._table, toTableRow);

        --_count;

        return toRow;
    }

    internal Span<T> ComponentData<T>(int row, int count) 
    {
        EcsAssert.Assert(row >= 0);
        EcsAssert.Assert(row < _entities.Length);

        ref readonly var cmp = ref _world.Component<T>();
        EcsAssert.Assert(cmp.Size > 0);

        var column = GetComponentIndex(in cmp);
        EcsAssert.Assert(column >= 0);

        return _table.ComponentData<T>(column, _entitiesTableRows[row], count);
    }

    internal void Clear()
    {
        _count = 0;
        _capacity = ARCHETYPE_INITIAL_CAPACITY;
        Array.Resize(ref _entities, _capacity);
        Array.Resize(ref _entitiesTableRows, _capacity);
    }

    internal void Optimize()
    {
        var pow = (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)_count);
        var newCapacity = Math.Max(ARCHETYPE_INITIAL_CAPACITY, pow);
        if (newCapacity < _capacity)
        {
            _capacity = newCapacity;
            Array.Resize(ref _entities, _capacity);
            Array.Resize(ref _entitiesTableRows, _capacity);
        }
    }

    private (EcsID, int) SwapWithLast(int fromRow)
    {
        ref var fromRec = ref _world.GetRecord(_entities[fromRow]);
        ref var lastRec = ref _world.GetRecord(_entities[_count - 1]);
        lastRec.Row = fromRec.Row;

        var removed = _entities[fromRow];
        _entities[fromRow] = _entities[_count - 1];

        var removedRow = _entitiesTableRows[fromRow];
        _entitiesTableRows[fromRow] = _entitiesTableRows[_count - 1];

        return (removed, removedRow);
    }

    private static void MakeEdges(Archetype left, Archetype right, EcsID id)
    {
        left._edgesRight.Add(new EcsEdge() { Archetype = right, ComponentID = id });
        right._edgesLeft.Add(new EcsEdge() { Archetype = left, ComponentID = id });
    }

    private void InsertVertex(Archetype newNode)
    {
        var nodeTypeLen = ComponentInfo.Length;
        var newTypeLen = newNode.ComponentInfo.Length;

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

        if (!IsSuperset(newNode.ComponentInfo))
        {
            return;
        }

        var i = 0;
        var newNodeTypeLen = newNode.ComponentInfo.Length;
        for (; i < newNodeTypeLen && ComponentInfo[i].ID == newNode.ComponentInfo[i].ID; ++i) { }

        MakeEdges(newNode, this, ComponentInfo[i].ID);
    }

    internal bool IsSuperset(UnsafeSpan<EcsComponent> other)
    {
        var thisComps = new UnsafeSpan<EcsComponent>(ComponentInfo);

        while (thisComps.CanAdvance() && other.CanAdvance())
        {
            if (thisComps.Value.ID == other.Value.ID)
            {
                other.Advance();
            }

            thisComps.Advance();
        }

        return Unsafe.AreSame(ref other.Value, ref other.End);
    }

    internal int FindMatch(UnsafeSpan<Term> searching)
    {
        var currents = new UnsafeSpan<EcsComponent>(ComponentInfo);

        while (currents.CanAdvance() && searching.CanAdvance())
        {
            if (ComponentComparer.CompareTerms(_world, currents.Value.ID, searching.Value.ID) == 0)
            {
                if (searching.Value.Op != TermOp.With)
                    return -1;

                searching.Advance();
            }
            else if (currents.Value.ID > searching.Value.ID && searching.Value.Op != TermOp.With)
            {
                searching.Advance();
                continue;
            }

            currents.Advance();
        }

        while (searching.CanAdvance() && searching.Value.Op != TermOp.With)
            searching.Advance();

        return Unsafe.AreSame(ref searching.Value, ref searching.End) ? 0 : 1;
    }

    public void Print()
    {
        PrintRec(this, 0, 0);

        static void PrintRec(Archetype root, int depth, EcsID rootComponent)
        {
            Console.WriteLine(
                "{0}Parent [{1}] common ID: {2}",
                new string('\t', depth),
                string.Join(", ", root.ComponentInfo.Select(s => s.ID)),
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
    public EcsID ComponentID;
    public Archetype Archetype;
}

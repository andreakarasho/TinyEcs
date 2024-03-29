using System.Collections.Immutable;
using Microsoft.Collections.Extensions;

namespace TinyEcs;

public struct ArchetypeChunk
{
	public Array[]? Components;
	public EntityView[] Entities;
	public int Count;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref T GetReference<T>(int column) where T : struct
	{
		EcsAssert.Assert(column >= 0 && column < Components!.Length);
		ref var array = ref Unsafe.As<Array, T[]>(ref Components![column]);
#if NET
		return ref MemoryMarshal.GetArrayDataReference(array);
#else
		return ref array[0];
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<T> GetSpan<T>(int column) where T : struct
	{
		EcsAssert.Assert(column >= 0 && column < Components!.Length);
		ref var array = ref Unsafe.As<Array, T[]>(ref Components![column]);
		return array.AsSpan(0, Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal readonly Array RawComponentData(int column)
	{
		EcsAssert.Assert(column >= 0 && column < Components!.Length);
		return Components![column];
	}
}

public ref struct ChunkEnumerator
{
	private readonly Span<ArchetypeChunk> _chunks;
	private int _index;

	internal ChunkEnumerator(Span<ArchetypeChunk> chunks)
	{
		_chunks = chunks;
		_index = -1;
	}

	public readonly ref readonly ArchetypeChunk Current => ref _chunks[_index];

	public bool MoveNext() => ++_index < _chunks.Length;
}

public sealed class Archetype
{
    const int ARCHETYPE_INITIAL_CAPACITY = 16;

    private ArchetypeChunk[] _chunks;
    private const int CHUNK_THRESHOLD = 4096;

    private readonly World _world;
    private readonly ComponentComparer _comparer;
    //private readonly int[] _lookup;
    private readonly DictionarySlim<ulong, int> _lookup;
    private int _count;
    internal List<EcsEdge> _edgesLeft, _edgesRight;

	internal Archetype(
        World world,
        ReadOnlySpan<ComponentInfo> components,
        ComponentComparer comparer
    )
    {
        _comparer = comparer;
        _world = world;
        _edgesLeft = new List<EcsEdge>();
        _edgesRight = new List<EcsEdge>();
        Components = components.ToImmutableArray();

        // var maxID = -1;
        // for (var i = 0; i < components.Length; ++i)
	       //  maxID = Math.Max(maxID, components[i].ID);

        // _lookup = new int[maxID + 1];
        // _lookup.AsSpan().Fill(-1);
        // for (var i = 0; i < components.Length; ++i)
	       //  _lookup[components[i].ID] = i;

       _lookup = new DictionarySlim<ulong, int>();
       for (var i = 0; i < components.Length; ++i)
	       _lookup.GetOrAddValueRef(components[i].ID, out _) = i;

        _chunks = new ArchetypeChunk[ARCHETYPE_INITIAL_CAPACITY];

		Id = Hashing.Calculate(components);
    }

    public World World => _world;
    public int Count => _count;
    public readonly ImmutableArray<ComponentInfo> Components;
	public ulong Id { get; }
    internal Span<ArchetypeChunk> Chunks => _chunks.AsSpan(0, (_count + CHUNK_THRESHOLD - 1) / CHUNK_THRESHOLD);
	internal int EmptyChunks => _chunks.Length - Chunks.Length;

    [SkipLocalsInit]
    internal ref ArchetypeChunk GetChunk(int index)
    {
		index /= CHUNK_THRESHOLD;

	    if (index >= _chunks.Length)
		    Array.Resize(ref _chunks, Math.Max(ARCHETYPE_INITIAL_CAPACITY, _chunks.Length * 2));

	    ref var chunk = ref _chunks[index];
	    if (chunk.Components == null)
	    {
		    chunk.Entities = new EntityView[CHUNK_THRESHOLD];
		    chunk.Components = new Array[Components.Length];
		    for (var i = 0; i < Components.Length; ++i)
			    chunk.Components[i] = Components[i].Size > 0 ? Lookup.GetArray(Components[i].ID, CHUNK_THRESHOLD)! : null!;
	    }

	    return ref chunk;
    }

    public ChunkEnumerator GetEnumerator()
    {
	    return new ChunkEnumerator(Chunks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetComponentIndex(ref readonly ComponentInfo cmp)
    {
	    return GetComponentIndex(cmp.ID);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetComponentIndex(ulong id)
	{
		if (!_lookup.TryGetValue(id, out var v))
			return -1;
		return v;
		//return id >= 0 && id < _lookup.Count ? _lookup[id] : -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetComponentIndex<T>() where T : struct
	{
		var id = Lookup.Component<T>.HashCode;
		return GetComponentIndex(id);
	}

	internal int Add(EcsID id)
	{
		ref var chunk = ref GetChunk(_count);
		chunk.Entities[chunk.Count++] = new(_world, id);
        return _count++;
    }

    internal EcsID Remove(ref EcsRecord record)
    {
		var removed = SwapWithLast(record.Row);
		ref var chunk = ref GetChunk(record.Row);
		ref var lastChunk = ref GetChunk(_count - 1);

		for (var i = 0; i < Components.Length; ++i)
		{
			if (Components[i].Size <= 0)
				continue;

			var arrayToBeRemoved = chunk.RawComponentData(i);
			var lastValidArray = lastChunk.RawComponentData(i);

			Array.Copy(lastValidArray, (_count - 1) % lastChunk.Count, arrayToBeRemoved, record.Row % chunk.Count, 1);
		}

		lastChunk.Count -= 1;
		--_count;

        return removed;
    }

	internal Archetype InsertVertex(
        Archetype left,
        ReadOnlySpan<ComponentInfo> components,
        ref readonly ComponentInfo component
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

		// Cleanup
		var empty = EmptyChunks;
		var half = Math.Max(ARCHETYPE_INITIAL_CAPACITY, _chunks.Length / 2);
		if (empty > half)
			Array.Resize(ref _chunks, half);

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

		ref var fromChunk = ref GetChunk(fromRow);
		ref var toChunk = ref to.GetChunk(toRow);

		for (; x < count; ++x, ++y)
		{
			while (Components[i].ID != to.Components[j].ID)
			{
				// advance the sign with less components!
				++y;
			}

			if (Components[i].Size <= 0)
				continue;

			var fromArray = fromChunk.RawComponentData(i);
			var toArray = toChunk.RawComponentData(j);

			// copy the moved entity to the target archetype
			Array.Copy(fromArray, fromRow % fromChunk.Count, toArray, toRow % toChunk.Count, 1);

			// swap last with the hole
			Array.Copy(fromArray, last % toChunk.Count, fromArray, fromRow % fromChunk.Count, 1);
		}

		fromChunk.Count -= 1;
	}

	internal void Clear()
    {
        _count = 0;
    }

    internal void Optimize()
    {

    }

    private EcsID SwapWithLast(int fromRow)
    {
	    ref var chunkFrom = ref GetChunk(fromRow);
	    ref var chunkTo = ref GetChunk(_count - 1);

        ref var fromRec = ref _world.GetRecord(chunkFrom.Entities[fromRow % chunkFrom.Count]);
        ref var lastRec = ref _world.GetRecord(chunkTo.Entities[(_count - 1) % chunkTo.Count]);
        lastRec.Row = fromRec.Row;

        var removed = chunkFrom.Entities[fromRow % chunkFrom.Count];
        chunkFrom.Entities[fromRow % chunkFrom.Count] = chunkTo.Entities[(_count - 1) % chunkTo.Count];

        return removed;
    }

    private static void MakeEdges(Archetype left, Archetype right, ulong id)
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
	        foreach (ref var edge in CollectionsMarshal.AsSpan(_edgesRight))
            {
                edge.Archetype.InsertVertex(newNode);
            }

            return;
        }

        if (!IsSuperset(newNode.Components.AsSpan()))
        {
            return;
        }

        var i = 0;
        var newNodeTypeLen = newNode.Components.Length;
        for (; i < newNodeTypeLen && Components[i].ID == newNode.Components[i].ID; ++i) { }

        MakeEdges(newNode, this, Components[i].ID);
    }

    private bool IsSuperset(ReadOnlySpan<ComponentInfo> other)
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

    internal int FindMatch(ReadOnlySpan<Term> searching)
    {
	    var currents = Components.AsSpan();
	    var i = 0;
	    var j = 0;

        while (i < currents.Length && j < searching.Length)
        {
	        ref readonly var current = ref currents[i];
	        ref readonly var search = ref searching[j];

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

        return j == searching.Length ? 0 : 1;
    }

    public void Print()
    {
        PrintRec(this, 0, 0);

        static void PrintRec(Archetype root, int depth, ulong rootComponent)
        {
            Console.WriteLine(
                "{0}- Parent [{1}] common ID: {2}",
                new string('\t', depth),
                string.Join(", ", root.Components.Select(s => Lookup.GetArray(s.ID, 0).ToString() )),
                rootComponent
            );

            if (root._edgesRight.Count > 0)
                Console.WriteLine("{0}  Children: ", new string('\t', depth));

            foreach (ref readonly var edge in CollectionsMarshal.AsSpan(root._edgesRight))
            {
                PrintRec(edge.Archetype, depth + 1, edge.ComponentID);
            }
        }
    }
}

struct EcsEdge
{
    public ulong ComponentID;
    public Archetype Archetype;
}

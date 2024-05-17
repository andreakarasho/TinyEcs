using System.Collections.Immutable;
using Microsoft.Collections.Extensions;

namespace TinyEcs;

public struct ArchetypeChunk
{
	internal Array[]? Components;
	internal EntityView[] Entities;

	public int Count { get; internal set; }


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref EntityView EntityAt(int row)
		=> ref Entities[row & Archetype.CHUNK_THRESHOLD];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref T GetReference<T>(int column) where T : struct
	{
		if (column < 0 || column >= Components!.Length)
			return ref Unsafe.NullRef<T>();

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
		if (column < 0 || column >= Components!.Length)
			return MemoryMarshal.CreateSpan(ref Unsafe.NullRef<T>(), 1);

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
    internal const int CHUNK_THRESHOLD = 0xFFF;
	internal const int CHUNK_SIZE = 4096;

    private readonly World _world;
    private readonly ComponentComparer _comparer;
    private readonly Dictionary<ulong, int> _lookup;
    private int _count;
    internal List<EcsEdge> _edgesLeft, _edgesRight;

	internal Archetype(
        World world,
        ImmutableArray<ComponentInfo> components,
        ComponentComparer comparer
    )
    {
        _comparer = comparer;
        _world = world;
        _edgesLeft = new List<EcsEdge>();
        _edgesRight = new List<EcsEdge>();
        Components = components;
		Pairs = components.Where(x => x.ID.IsPair).ToImmutableArray();
		Id = Hashing.Calculate(components.AsSpan());
        _chunks = new ArchetypeChunk[ARCHETYPE_INITIAL_CAPACITY];
       	_lookup = new Dictionary<ulong, int>(/*_comparer*/);

       	for (var i = 0; i < components.Length; ++i)
		{
			_lookup.Add(components[i].ID, i);
		}
    }

    public World World => _world;
    public int Count => _count;
    public readonly ImmutableArray<ComponentInfo> Components, Pairs;
	public ulong Id { get; }
    internal Span<ArchetypeChunk> Chunks => _chunks.AsSpan(0, (_count + CHUNK_SIZE - 1) / CHUNK_SIZE);
	internal Memory<ArchetypeChunk> MemChunks => _chunks.AsMemory(0, (_count + CHUNK_SIZE - 1) / CHUNK_SIZE);
	internal int EmptyChunks => _chunks.Length - Chunks.Length;


    [SkipLocalsInit]
    internal ref ArchetypeChunk GetChunk(int index)
    {
		index /= CHUNK_SIZE;

	    if (index >= _chunks.Length)
		    Array.Resize(ref _chunks, Math.Max(ARCHETYPE_INITIAL_CAPACITY, _chunks.Length * 2));

	    ref var chunk = ref _chunks[index];
	    if (chunk.Components == null)
	    {
		    chunk.Entities = new EntityView[CHUNK_SIZE];
		    chunk.Components = new Array[Components.Length];
		    for (var i = 0; i < Components.Length; ++i)
			{
				chunk.Components[i] = Components[i].Size > 0 ? Lookup.GetArray(Components[i].ID, CHUNK_SIZE)! : null!;
			}
	    }

	    return ref chunk;
    }

    public ChunkEnumerator GetEnumerator()
    {
	    return new ChunkEnumerator(Chunks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetComponentIndex(EcsID id)
	{
		return _lookup.TryGetValue(id.Value, out var v) ? v : -1;
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
		chunk.EntityAt(chunk.Count++) = new(_world, id);
        return _count++;
    }

	private EcsID RemoveByRow(int row)
	{
		_count -= 1;
		EcsAssert.Assert(_count >= 0, "Negative count");

		ref var chunk = ref GetChunk(row);
		ref var lastChunk = ref GetChunk(_count);
		var removed = chunk.EntityAt(row);

		if (row < _count)
		{
			EcsAssert.Assert(lastChunk.EntityAt(_count) != EntityView.Invalid, "Entity is invalid. This should never happen!");

			chunk.EntityAt(row) = lastChunk.EntityAt(_count);

			for (var i = 0; i < Components.Length; ++i)
			{
				if (Components[i].Size <= 0)
					continue;

				var arrayToBeRemoved = chunk.RawComponentData(i);
				var lastValidArray = lastChunk.RawComponentData(i);

				Array.Copy(lastValidArray, _count & CHUNK_THRESHOLD, arrayToBeRemoved, row & CHUNK_THRESHOLD, 1);
			}

			_world.GetRecord(chunk.EntityAt(row)).Row = row;
		}

		lastChunk.EntityAt(_count) = EntityView.Invalid;

		for (var i = 0; i < Components.Length; ++i)
		{
			if (Components[i].Size <= 0)
				continue;

			var lastValidArray = lastChunk.RawComponentData(i);
			Array.Clear(lastValidArray, _count & CHUNK_THRESHOLD, 1);
		}

		lastChunk.Count -= 1;
		EcsAssert.Assert(lastChunk.Count >= 0, "Negative chunk count");

		TrimChunksIfNeeded();

        return removed;
	}

    internal EcsID Remove(ref EcsRecord record)
		=> RemoveByRow(record.Row);

	internal Archetype InsertVertex(
        Archetype left,
        ImmutableArray<ComponentInfo> components,
        EcsID id
    )
    {
        var vertex = new Archetype(left._world, components, _comparer);
        MakeEdges(left, vertex, id);
        InsertVertex(vertex);
        return vertex;
    }

    internal int MoveEntity(Archetype newArch, int oldRow)
    {
		ref var fromChunk = ref GetChunk(oldRow);
		var newRow = newArch.Add(fromChunk.EntityAt(oldRow));

		var isLeft = newArch.Components.Length < Components.Length;
		int i = 0,
			j = 0;
		var count = isLeft ? newArch.Components.Length : Components.Length;

		ref var x = ref (isLeft ? ref j : ref i);
		ref var y = ref (!isLeft ? ref j : ref i);

		ref var toChunk = ref newArch.GetChunk(newRow);

		for (; x < count; ++x, ++y)
		{
			while (Components[i].ID != newArch.Components[j].ID)
			{
				// advance the sign with less components!
				++y;
			}

			if (Components[i].Size <= 0)
				continue;

			var fromArray = fromChunk.RawComponentData(i);
			var toArray = toChunk.RawComponentData(j);

			// copy the moved entity to the target archetype
			Array.Copy(fromArray, oldRow & CHUNK_THRESHOLD, toArray, newRow & CHUNK_THRESHOLD, 1);
		}

		_ = RemoveByRow(oldRow);
		return newRow;
    }

	internal void Clear()
    {
        _count = 0;
		_edgesLeft.Clear();
		_edgesRight.Clear();
		TrimChunksIfNeeded();
    }

	private void TrimChunksIfNeeded()
	{
		// Cleanup
		var empty = EmptyChunks;
		var half = Math.Max(ARCHETYPE_INITIAL_CAPACITY, _chunks.Length / 2);
		if (empty > half)
			Array.Resize(ref _chunks, half);
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

			if (_comparer.Compare(current.ID.Value, search.ID.Value) == 0)
            {
                if (search.Op == TermOp.Without)
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
                string.Join(", ", root.Components.Select(s => Lookup.GetArray(s.ID, 0)!.ToString() )),
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

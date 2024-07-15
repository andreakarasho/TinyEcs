using System.Collections.Immutable;

namespace TinyEcs;

public struct ArchetypeChunk
{
	internal Array[]? Data;
	internal EntityView[] Entities;

	public int Count { get; internal set; }


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref EntityView EntityAt(int row)
#if NET
		=> ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Entities), row & Archetype.CHUNK_THRESHOLD);
#else
		=> ref Unsafe.Add(ref MemoryMarshal.GetReference(Entities.AsSpan()), row & Archetype.CHUNK_THRESHOLD);
#endif

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref T GetReference<T>(int column) where T : struct
	{
		if (column < 0 || column >= Data!.Length)
			return ref Unsafe.NullRef<T>();

		ref var array = ref Unsafe.As<Array, T[]>(ref Data![column]);
#if NET
		return ref MemoryMarshal.GetArrayDataReference(array);
#else
		return ref MemoryMarshal.GetReference(array.AsSpan());
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<T> GetSpan<T>(int column) where T : struct
	{
		if (column < 0 || column >= Data!.Length)
			return Span<T>.Empty;

		ref var array = ref Unsafe.As<Array, T[]>(ref Data![column]);
		return array.AsSpan(0, Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal readonly Array? RawComponentData(int column)
	{
		if (column < 0 || column >= Data!.Length)
			return null;
		return Data![column];
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
    const int ARCHETYPE_INITIAL_CAPACITY = 4;

    private ArchetypeChunk[] _chunks;
    internal const int CHUNK_THRESHOLD = 0xFFF;
	internal const int CHUNK_SIZE = 4096;

    private readonly World _world;
    private readonly ComponentComparer _comparer;

	private readonly FastIdLookup<int> _lookup = new ();
	private readonly EcsID[] _ids;
    private int _count;
	internal FastIdLookup<EcsEdge> _add, _remove;

	internal Archetype(
        World world,
        ReadOnlySpan<ComponentInfo> sign,
        ComponentComparer comparer
    )
    {
        _comparer = comparer;
        _world = world;

        All = [ .. sign];
        Components = All.Where(x => x.Size > 0).ToImmutableArray();
        Tags = All.Where(x => x.Size <= 0).ToImmutableArray();
		Pairs = All.Where(x => x.ID.IsPair()).ToImmutableArray();

		Id = Hashing.Calculate(All.AsSpan());
        _chunks = new ArchetypeChunk[ARCHETYPE_INITIAL_CAPACITY];

       	for (var i = 0; i < sign.Length; ++i)
		{
			_lookup.Add(sign[i].ID, i);
		}

		_ids = All.Select(s => s.ID).ToArray();

		_add = new FastIdLookup<EcsEdge>();
		_remove = new FastIdLookup<EcsEdge>();
    }

    public World World => _world;
    public int Count => _count;
    public readonly ImmutableArray<ComponentInfo> All, Components, Tags, Pairs;
	public ulong Id { get; }
    internal Span<ArchetypeChunk> Chunks => _chunks.AsSpan(0, (_count + CHUNK_SIZE - 1) / CHUNK_SIZE);
	internal Memory<ArchetypeChunk> MemChunks => _chunks.AsMemory(0, (_count + CHUNK_SIZE - 1) / CHUNK_SIZE);
	internal int EmptyChunks => _chunks.Length - ((_count + CHUNK_SIZE - 1) / CHUNK_SIZE);


    // [SkipLocalsInit]
    internal ref ArchetypeChunk GetChunk(int index)
    {
		index /= CHUNK_SIZE;

	    if (index >= _chunks.Length)
		    Array.Resize(ref _chunks, Math.Max(ARCHETYPE_INITIAL_CAPACITY, _chunks.Length * 2));

	    ref var chunk = ref _chunks[index];
	    if (chunk.Data == null)
	    {
		    chunk.Entities = new EntityView[CHUNK_SIZE];
		    chunk.Data = new Array[All.Length];
		    for (var i = 0; i < All.Length; ++i)
			{
				chunk.Data[i] = All[i].Size > 0 ? Lookup.GetArray(All[i].ID, CHUNK_SIZE)! : null!;
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
		ref var idx = ref _lookup.TryGet(id, out var exists);
		return exists ? idx : -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetComponentIndex<T>() where T : struct
	{
		var id = Lookup.Component<T>.HashCode;
		return GetComponentIndex(id);
	}

	internal int Add(EntityView ent)
	{
		ref var chunk = ref GetChunk(_count);
		chunk.EntityAt(chunk.Count++) = ent;
		return _count++;
	}

	internal int Add(EcsID id)
		=> Add(new(_world, id));

	private EcsID RemoveByRow(int row)
	{
		_count -= 1;
		EcsAssert.Assert(_count >= 0, "Negative count");

		ref var chunk = ref GetChunk(row);
		ref var lastChunk = ref GetChunk(_count);
		var removed = chunk.EntityAt(row).ID;

		if (row < _count)
		{
			EcsAssert.Assert(lastChunk.EntityAt(_count).ID.IsValid(), "Entity is invalid. This should never happen!");

			chunk.EntityAt(row) = lastChunk.EntityAt(_count);

			var srcIdx = _count & CHUNK_THRESHOLD;
			var dstIdx = row & CHUNK_THRESHOLD;
			for (var i = 0; i < All.Length; ++i)
			{
				if (All[i].Size <= 0)
					continue;

				var arrayToBeRemoved = chunk.RawComponentData(i);
				var lastValidArray = lastChunk.RawComponentData(i);

				Array.Copy(lastValidArray, srcIdx, arrayToBeRemoved, dstIdx, 1);
			}

			_world.GetRecord(chunk.EntityAt(row)).Row = row;
		}

		// lastChunk.EntityAt(_count) = EntityView.Invalid;
		//
		// for (var i = 0; i < All.Length; ++i)
		// {
		// 	if (All[i].Size <= 0)
		// 		continue;
		//
		// 	var lastValidArray = lastChunk.RawComponentData(i);
		// 	Array.Clear(lastValidArray, _count & CHUNK_THRESHOLD, 1);
		// }

		lastChunk.Count -= 1;
		EcsAssert.Assert(lastChunk.Count >= 0, "Negative chunk count");

		TrimChunksIfNeeded();

        return removed;
	}

    internal EcsID Remove(ref EcsRecord record)
		=> RemoveByRow(record.Row);

	internal Archetype InsertVertex(
        Archetype left,
        ReadOnlySpan<ComponentInfo> sign,
        EcsID id
    )
    {
        var vertex = new Archetype(left._world, sign, _comparer);
        MakeEdges(left, vertex, id);
        InsertVertex(vertex);
        return vertex;
    }

    internal int MoveEntity(Archetype newArch, int oldRow)
    {
		ref var fromChunk = ref GetChunk(oldRow);
		var newRow = newArch.Add(fromChunk.EntityAt(oldRow));

		var isLeft = newArch.All.Length < All.Length;
		int i = 0,
			j = 0;
		var count = isLeft ? newArch.All.Length : All.Length;

		ref var x = ref (isLeft ? ref j : ref i);
		ref var y = ref (!isLeft ? ref j : ref i);

		ref var toChunk = ref newArch.GetChunk(newRow);

		for (; x < count; ++x, ++y)
		{
			while (All[i].ID != newArch.All[j].ID)
			{
				// advance the sign with less components!
				++y;
			}

			if (All[i].Size <= 0)
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
        _add.Clear();
        _remove.Clear();
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

    private static void MakeEdges(Archetype left, Archetype right, EcsID id)
    {
		left._add.Add(id, new EcsEdge() { Archetype = right, ComponentID = id });
        right._remove.Add(id, new EcsEdge() { Archetype = left, ComponentID = id });
    }

    private void InsertVertex(Archetype newNode)
    {
        var nodeTypeLen = All.Length;
        var newTypeLen = newNode.All.Length;

        if (nodeTypeLen > newTypeLen - 1)
        {
            return;
        }

        if (nodeTypeLen < newTypeLen - 1)
        {
	        foreach ((var id, var edge) in _add)
            {
	            edge.Archetype.InsertVertex(newNode);
            }

            return;
        }

        if (!IsSuperset(newNode.All.AsSpan()))
        {
            return;
        }

        var i = 0;
        var newNodeTypeLen = newNode.All.Length;
        for (; i < newNodeTypeLen && All[i].ID == newNode.All[i].ID; ++i) { }

        MakeEdges(newNode, this, All[i].ID);
    }

    private bool IsSuperset(ReadOnlySpan<ComponentInfo> other)
    {
	    int i = 0, j = 0;
	    while (i < All.Length && j < other.Length)
	    {
		    if (All[i].ID == other[j].ID)
		    {
			    j++;
		    }

		    i++;
	    }

	    return j == other.Length;
    }

    internal int FindMatch(ReadOnlySpan<IQueryTerm> searching)
    {
		return Match.Validate(_comparer, _ids, searching);
    }

    public void Print()
    {
        PrintRec(this, 0, 0);

        static void PrintRec(Archetype root, int depth, ulong rootComponent)
        {
            Console.WriteLine(
                "{0}- Parent [{1}] common ID: {2}",
                new string('\t', depth),
                string.Join(", ", root.All.Select(s => Lookup.GetArray(s.ID, 0)!.ToString() )),
                rootComponent
            );

            var add = root._add;
            if (add.Count > 0)
                Console.WriteLine("{0}  Children: ", new string('\t', depth));

            foreach ((var id, var edge) in add)
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

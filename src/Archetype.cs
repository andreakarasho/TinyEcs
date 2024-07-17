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

    internal const int CHUNK_THRESHOLD = 0xFFF;
	internal const int CHUNK_SIZE = 4096;
	private const int CHUNK_LOG2 = 12;


    private readonly World _world;
    private ArchetypeChunk[] _chunks;
    private readonly ComponentComparer _comparer;
	private readonly FastIdLookup<int> _lookup = new ();
	private readonly EcsID[] _ids;
    private int _count;
	private RollingHash _rolling;


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
        _chunks = new ArchetypeChunk[ARCHETYPE_INITIAL_CAPACITY];

		_rolling = new RollingHash();
       	for (var i = 0; i < sign.Length; ++i)
		{
			_lookup.Add(sign[i].ID, i);
			_rolling.Add(sign[i].ID);
		}

		_ids = All.Select(s => s.ID).ToArray();
		_add = new FastIdLookup<EcsEdge>();
		_remove = new FastIdLookup<EcsEdge>();
    }


    public World World => _world;
    public int Count => _count;
    public readonly ImmutableArray<ComponentInfo> All, Components, Tags, Pairs;
	public EcsID Id => _rolling.Hash;
    internal Span<ArchetypeChunk> Chunks => _chunks.AsSpan(0, (_count + CHUNK_SIZE - 1) / CHUNK_SIZE);
	internal Memory<ArchetypeChunk> MemChunks => _chunks.AsMemory(0, (_count + CHUNK_SIZE - 1) / CHUNK_SIZE);
	internal int EmptyChunks => _chunks.Length - ((_count + CHUNK_SIZE - 1) / CHUNK_SIZE);
	internal FastIdLookup<EcsEdge> _add, _remove;


	private ref ArchetypeChunk GetOrCreateChunk(int index)
	{
		index >>= CHUNK_LOG2;

	    if (index >= _chunks.Length)
		    Array.Resize(ref _chunks, Math.Max(ARCHETYPE_INITIAL_CAPACITY, _chunks.Length * 2));

	    ref var chunk = ref _chunks[index];
		if (chunk.Data == null)
		{
			chunk.Entities = new EntityView[CHUNK_SIZE];
			chunk.Data = new Array[All.Length];
			for (var i = 0; i < All.Length; ++i)
				chunk.Data[i] = All[i].Size > 0 ? Lookup.GetArray(All[i].ID, CHUNK_SIZE)! : null!;
		}
	   
		return ref chunk;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ref ArchetypeChunk GetChunk(int index)
		=> ref _chunks[index >> CHUNK_LOG2];

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
		ref var chunk = ref GetOrCreateChunk(_count);
		chunk.EntityAt(chunk.Count++) = ent;
		return _count++;
	}

	internal ref ArchetypeChunk Add2(EntityView ent, out int newRow)
	{
		ref var chunk = ref GetOrCreateChunk(_count);
		chunk.EntityAt(chunk.Count++) = ent;
		newRow = _count++;
		return ref chunk;
	}

	internal int Add(EcsID id)
		=> Add(new(_world, id));

	private EcsID RemoveByRow(int row)
	{
		_count -= 1;
		EcsAssert.Assert(_count >= 0, "Negative count");

		ref var chunk = ref _chunks[row >> CHUNK_LOG2];
		ref var lastChunk = ref _chunks[_count >> CHUNK_LOG2];
		var removed = chunk.EntityAt(row).ID;

		if (row < _count)
		{
			EcsAssert.Assert(lastChunk.EntityAt(_count).ID.IsValid(), "Entity is invalid. This should never happen!");

			chunk.EntityAt(row) = lastChunk.EntityAt(_count);

			var srcIdx = _count & CHUNK_THRESHOLD;
			var dstIdx = row & CHUNK_THRESHOLD;
			for (var i = 0; i < All.Length; ++i)
			{
				var size = All[i].Size;
				if (size <= 0)
					continue;

				var arrayToBeRemoved = chunk.Data![i];
				var lastValidArray = lastChunk.Data![i];

				CopyFast(lastValidArray, srcIdx, arrayToBeRemoved, dstIdx, 1, size, All[i].IsManaged);
			}

			_world.GetRecord(chunk.EntityAt(row).ID).Row = row;
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

    internal int MoveEntity(Archetype newArch, int oldRow, bool isRemove)
    {
		ref var fromChunk = ref _chunks[oldRow >> CHUNK_LOG2];
		ref var toChunk = ref newArch.Add2(fromChunk.EntityAt(oldRow), out var newRow);

		int i = 0, j = 0;
		var count = isRemove ? newArch.All.Length : All.Length;

		ref var x = ref (isRemove ? ref j : ref i);
		ref var y = ref (!isRemove ? ref j : ref i);

		var srcIdx = oldRow & CHUNK_THRESHOLD;
		var dstIdx = newRow & CHUNK_THRESHOLD;

		for (; x < count; ++x, ++y)
		{
			while (All[i].ID != newArch.All[j].ID)
			{
				// advance the sign with less components!
				++y;
			}

			var size = All[i].Size;
			if (size <= 0)
				continue;

			var fromArray = fromChunk.Data![i];
			var toArray = toChunk.Data![j];

			// copy the moved entity to the target archetype
			CopyFast(fromArray!, srcIdx, toArray!, dstIdx, 1, size, All[i].IsManaged);
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    internal sealed class RawArrayData
    {
	    public uint Length;
	    public uint Padding;
	    public byte Data;
    }

	private static void CopyFast(Array src, int srcIdx, Array dst, int dstIdx, int count, int elementSize, bool isManaged)
	{
		if (isManaged)
		{
			Array.Copy(src, srcIdx, dst, dstIdx, count);
		}
		else
		{
			ref var srcB = ref Unsafe.AddByteOffset(ref Unsafe.As<Array, RawArrayData>(ref src).Data, (uint)(srcIdx * elementSize));
			ref var dstB = ref Unsafe.AddByteOffset(ref Unsafe.As<Array, RawArrayData>(ref dst).Data, (uint)(dstIdx * elementSize));

			// var span0 = MemoryMarshal.CreateSpan(ref srcB, count * elementSize);
			// var span1 = MemoryMarshal.CreateSpan(ref dstB, count * elementSize);
			// span0.CopyTo(span1);

			Unsafe.CopyBlock(ref dstB, ref srcB, (uint)(count * elementSize));
		}
	}
}

struct EcsEdge
{
    public EcsID ComponentID;
    public Archetype Archetype;
}

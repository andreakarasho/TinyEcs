using System.Collections.Frozen;
using System.Collections.Immutable;

namespace TinyEcs;

[SkipLocalsInit]
public struct ArchetypeChunk
{
	internal readonly Array[]? Data;
	internal readonly EntityView[] Entities;


	internal ArchetypeChunk(ImmutableArray<ComponentInfo> sign, int chunkSize)
	{
		Entities = new EntityView[chunkSize];
		Data = new Array[sign.Length];
		for (var i = 0; i < sign.Length; ++i)
			Data[i] = sign[i].Size > 0 ? Lookup.GetArray(sign[i].ID, chunkSize)! : null!;
	}

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

		var array = Unsafe.As<T[]>(Data![column]);
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

		var array = Unsafe.As<T[]>(Data![column]);
		return array.AsSpan(0, Count);
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
	private readonly FrozenDictionary<EcsID, int> _lookup;
	private readonly EcsID[] _ids;
	private readonly List<EcsEdge> _add, _remove;
	private int _count;


	internal Archetype(
		World world,
		ComponentInfo[] sign,
		ComponentComparer comparer
	)
	{
		_comparer = comparer;
		_world = world;
		All = sign.ToImmutableArray();
		Components = All.Where(x => x.Size > 0).ToImmutableArray();
		Tags = All.Where(x => x.Size <= 0).ToImmutableArray();
		Pairs = All.Where(x => x.ID.IsPair()).ToImmutableArray();
		_chunks = new ArchetypeChunk[ARCHETYPE_INITIAL_CAPACITY];

		var roll = new RollingHash();
		var dict = new Dictionary<EcsID, int>();
		for (var i = 0; i < sign.Length; ++i)
		{
			dict.Add(sign[i].ID, i);
			roll.Add(sign[i].ID);
		}

		Id = roll.Hash;

		_lookup = dict.ToFrozenDictionary();

		_ids = All.Select(s => s.ID).ToArray();
		_add = new ();
		_remove = new ();
	}


	public World World => _world;
	public int Count => _count;
	public readonly ImmutableArray<ComponentInfo> All, Components, Tags, Pairs;
	public EcsID Id { get; }
	internal Span<ArchetypeChunk> Chunks => _chunks.AsSpan(0, (_count + CHUNK_SIZE - 1) >> CHUNK_LOG2);
	internal Memory<ArchetypeChunk> MemChunks => _chunks.AsMemory(0, (_count + CHUNK_SIZE - 1) >> CHUNK_LOG2);
	internal int EmptyChunks => _chunks.Length - ((_count + CHUNK_SIZE - 1) >> CHUNK_LOG2);


	private ref ArchetypeChunk GetOrCreateChunk(int index)
	{
		index >>= CHUNK_LOG2;

		if (index >= _chunks.Length)
			Array.Resize(ref _chunks, Math.Max(ARCHETYPE_INITIAL_CAPACITY, _chunks.Length * 2));

		ref var chunk = ref _chunks[index];
		if (chunk.Data == null)
		{
			chunk = new ArchetypeChunk(All, CHUNK_SIZE);
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
		ref readonly var idx = ref _lookup.GetValueRefOrNullRef(id);
		return Unsafe.IsNullRef(ref Unsafe.AsRef(in idx))? -1 : idx;
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

		ref var chunk = ref GetChunk(row);
		ref var lastChunk = ref GetChunk(_count);
		var removed = chunk.EntityAt(row).ID;

		if (row < _count)
		{
			EcsAssert.Assert(lastChunk.EntityAt(_count).ID.IsValid(), "Entity is invalid. This should never happen!");

			chunk.EntityAt(row) = lastChunk.EntityAt(_count);

			var srcIdx = _count & CHUNK_THRESHOLD;
			var dstIdx = row & CHUNK_THRESHOLD;
			var items = All;
			for (var i = 0; i < items.Length; ++i)
			{
				var size = items[i].Size;
				if (size <= 0)
					continue;

				var arrayToBeRemoved = chunk.Data![i];
				var lastValidArray = lastChunk.Data![i];

				CopyFast(lastValidArray, srcIdx, arrayToBeRemoved, dstIdx, 1, size, items[i].IsManaged);
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
		ComponentInfo[] sign,
		EcsID id
	)
	{
		var vertex = new Archetype(left._world, sign, _comparer);
		var a = left.All.Length < vertex.All.Length ? left : vertex;
		var b = left.All.Length < vertex.All.Length ? vertex : left;
		MakeEdges(a, b, id);
		InsertVertex(vertex);
		return vertex;
	}

	internal int MoveEntity(Archetype newArch, int oldRow, bool isRemove)
	{
		ref var fromChunk = ref GetChunk(oldRow);
		ref var toChunk = ref newArch.Add2(fromChunk.EntityAt(oldRow), out var newRow);

		int i = 0, j = 0;
		var count = isRemove ? newArch.All.Length : All.Length;

		ref var x = ref (isRemove ? ref j : ref i);
		ref var y = ref (!isRemove ? ref j : ref i);

		var srcIdx = oldRow & CHUNK_THRESHOLD;
		var dstIdx = newRow & CHUNK_THRESHOLD;
		var items = All;
		var newItems = newArch.All;
		for (; x < count; ++x, ++y)
		{
			while (items[i].ID != newItems[j].ID)
			{
				// advance the sign with less components!
				++y;
			}

			var size = items[i].Size;
			if (size <= 0)
				continue;

			var fromArray = fromChunk.Data![i];
			var toArray = toChunk.Data![j];

			// copy the moved entity to the target archetype
			CopyFast(fromArray!, srcIdx, toArray!, dstIdx, 1, size, items[i].IsManaged);
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
		left._add.Add(new EcsEdge() { Archetype = right, Id = id });
		right._remove.Add(new EcsEdge() { Archetype = left, Id = id });
	}

	private void InsertVertex(Archetype newNode)
	{
		var nodeTypeLen = All.Length;
		var newTypeLen = newNode.All.Length;

		// if (nodeTypeLen > newTypeLen - 1)
		// {
		// 	foreach (ref var edge in CollectionsMarshal.AsSpan(_remove))
		// 	{
		// 		edge.Archetype.InsertVertex(newNode);
		// 	}

		// 	return;
		// }

		if (nodeTypeLen < newTypeLen - 1)
		{
			foreach (ref var edge in CollectionsMarshal.AsSpan(_add))
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

	internal Archetype? TraverseLeft(EcsID nodeId)
		=> Traverse(this, nodeId, false);

	internal Archetype? TraverseRight(EcsID nodeId)
		=> Traverse(this, nodeId, true);

	private static Archetype? Traverse(Archetype root, EcsID nodeId, bool onAdd)
	{
		foreach (ref var edge in CollectionsMarshal.AsSpan(onAdd ? root._add : root._remove))
		{
			if (edge.Id == nodeId)
				return edge.Archetype;
		}

		// foreach (ref var edge in CollectionsMarshal.AsSpan(onAdd ? root._add : root._remove))
		// {
		// 	var found = onAdd ? edge.Archetype.TraverseRight(nodeId) : edge.Archetype.TraverseLeft(nodeId);
		// 	if (found != null)
		// 		return found;
		// }

		return null;
	}

	internal void GetSuperSets(ReadOnlySpan<IQueryTerm> terms, List<Archetype> matched)
	{
		var result = Match.Validate(_comparer, _ids, terms);
		if (result < 0)
		{
			return;
		}

		if (result == 0)
		{
			matched.Add(this);
		}

		var add = _add;
		if (add.Count <= 0)
			return;

		foreach (ref var edge in CollectionsMarshal.AsSpan(add))
		{
			edge.Archetype.GetSuperSets(terms, matched);
		}
	}

	public void Print(int depth)
    {
        Console.WriteLine(new string(' ', depth * 2) + $"Node: [{string.Join(", ", All.Select(s => s.ID))}]");

        foreach (ref var edge in CollectionsMarshal.AsSpan(_add))
        {
            Console.WriteLine(new string(' ', (depth + 1) * 2) + $"Edge: {edge.Id}");
            edge.Archetype.Print(depth + 2);
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
			ref var srcB = ref Unsafe.AddByteOffset(ref Unsafe.As<RawArrayData>(src).Data, (uint)(srcIdx * elementSize));
			ref var dstB = ref Unsafe.AddByteOffset(ref Unsafe.As<RawArrayData>(dst).Data, (uint)(dstIdx * elementSize));

			// var span0 = MemoryMarshal.CreateSpan(ref srcB, count * elementSize);
			// var span1 = MemoryMarshal.CreateSpan(ref dstB, count * elementSize);
			// span0.CopyTo(span1);

			Unsafe.CopyBlock(ref dstB, ref srcB, (uint)(count * elementSize));
		}
	}
}

struct EcsEdge
{
	public EcsID Id;
	public Archetype Archetype;
}

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Numerics;
using Microsoft.VisualBasic;

namespace TinyEcs;

[SkipLocalsInit]
internal struct ArchetypeChunk
{
	internal readonly Array[]? Data;
	internal readonly EntityView[] Entities;


	internal ArchetypeChunk(ImmutableArray<ComponentInfo> sign, int chunkSize)
	{
		Entities = new EntityView[chunkSize];
		Data = new Array[sign.Length];
		for (var i = 0; i < sign.Length; ++i)
			Data[i] = Lookup.GetArray(sign[i].ID, chunkSize)!;
	}

	public int Count { get; internal set; }


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref EntityView EntityAt(int row)
		=> ref Unsafe.Add(ref Entities.AsSpan(0, Count)[0], row & Archetype.CHUNK_THRESHOLD);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref T GetReference<T>(int column) where T : struct
	{
		if (column < 0 || column >= Data!.Length)
			return ref Unsafe.NullRef<T>();

		var span = new Span<T>(Unsafe.As<T[]>(Data[column]), 0, Count);
		return ref MemoryMarshal.GetReference(span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref T GetReferenceWithSize<T>(int column, out int sizeInBytes) where T : struct
	{
		if (column < 0 || column >= Data!.Length)
		{
			sizeInBytes = 0;
			return ref Unsafe.NullRef<T>();
		}

		sizeInBytes = Unsafe.SizeOf<T>();

		var span = new Span<T>(Unsafe.As<T[]>(Data[column]), 0, Count);
		return ref MemoryMarshal.GetReference(span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref T GetReferenceAt<T>(int column, int row) where T : struct
	{
		ref var reference = ref GetReference<T>(column);
		if (Unsafe.IsNullRef(ref reference))
			return ref reference;
		return ref Unsafe.Add(ref reference, row & Archetype.CHUNK_THRESHOLD);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<T> GetSpan<T>(int column) where T : struct
	{
		if (column < 0 || column >= Data!.Length)
			return Span<T>.Empty;

		var span = new Span<T>(Unsafe.As<T[]>(Data[column]), 0, Count);
		return span;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ReadOnlySpan<EntityView> GetEntities()
		=> Entities.AsSpan(0, Count);
}

public sealed class Archetype
{
	const int ARCHETYPE_INITIAL_CAPACITY = 1;

	internal const int CHUNK_SIZE = 4096;
	private const int CHUNK_LOG2 = 12;
	internal const int CHUNK_THRESHOLD = CHUNK_SIZE - 1;


	private readonly World _world;
	private ArchetypeChunk[] _chunks;
	private readonly ComponentComparer _comparer;
	private readonly FrozenDictionary<EcsID, int> _componentsLookup, _allLookup
#if USE_PAIR
		, _pairsLookup
#endif
		;
	private readonly EcsID[] _ids;
	internal readonly List<EcsEdge> _add, _remove;
	private int _count;
	private readonly int[] _fastLookup;

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
#if USE_PAIR
		Pairs = All.Where(x => x.ID.IsPair()).ToImmutableArray();
#endif
		_chunks = new ArchetypeChunk[ARCHETYPE_INITIAL_CAPACITY];


		var roll = new RollingHash();
		var dict = new Dictionary<EcsID, int>();
		var allDict = new Dictionary<EcsID, int>();
		var maxId = -1;
		for (int i = 0, cur = 0; i < sign.Length; ++i)
		{
			roll.Add(sign[i].ID);

			if (sign[i].Size > 0)
			{
				dict.Add(sign[i].ID, cur++);
				maxId = Math.Max(maxId, (int)sign[i].ID);
			}

			allDict.Add(sign[i].ID, i);
		}

		Id = roll.Hash;

		_fastLookup = new int[maxId + 1];
		_fastLookup.AsSpan().Fill(-1);
		foreach ((var id, var i) in dict)
		{
#if USE_PAIR
			if (!id.IsPair())
#endif
			_fastLookup[(int)id] = i;
		}

		_componentsLookup = dict.ToFrozenDictionary();
		_allLookup = allDict.ToFrozenDictionary();
#if USE_PAIR
		_pairsLookup = allDict
			.Where(s => s.Key.IsPair())
				.GroupBy(s => s.Key.First())
			.ToFrozenDictionary(s => s.Key, v => v.First().Value);
#endif

		_ids = All.Select(s => s.ID).ToArray();
		_add = new ();
		_remove = new ();
	}


	public World World => _world;
	public int Count => _count;
	public readonly ImmutableArray<ComponentInfo> All, Components, Tags, Pairs;
	public EcsID Id { get; }
	internal ReadOnlySpan<ArchetypeChunk> Chunks => _chunks.AsSpan(0, (_count + CHUNK_SIZE - 1) >> CHUNK_LOG2);
	internal int EmptyChunks => _chunks.Length - ((_count + CHUNK_SIZE - 1) >> CHUNK_LOG2);
	internal EcsID[] Sign => _ids;

	private ref ArchetypeChunk GetOrCreateChunk(int index)
	{
		index >>= CHUNK_LOG2;

		if (index >= _chunks.Length)
			Array.Resize(ref _chunks, Math.Max(ARCHETYPE_INITIAL_CAPACITY, _chunks.Length * 2));

		ref var chunk = ref _chunks[index];
		if (chunk.Data == null)
		{
			chunk = new ArchetypeChunk(Components, CHUNK_SIZE);
		}

		return ref chunk;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ref ArchetypeChunk GetChunk(int index)
		=> ref _chunks[index >> CHUNK_LOG2];

	internal int GetComponentIndex(EcsID id)
	{
#if USE_PAIR
		if (id.IsPair())
		{
			return _componentsLookup.GetValueOrDefault(id, -1);
		}
#endif
		return (int)id >= _fastLookup.Length ? -1 : _fastLookup[(int)id];
	}

#if USE_PAIR
	internal int GetPairIndex(EcsID id)
	{
		return _pairsLookup.GetValueOrDefault(id, -1);
	}
#endif

	internal int GetAnyIndex(EcsID id)
	{
		return _allLookup.GetValueOrDefault(id, -1);
	}

	internal bool HasIndex(EcsID id)
	{
		return _allLookup.ContainsKey(id);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetComponentIndex<T>() where T : struct
	{
		var id = Lookup.Component<T>.HashCode;
		var size = Lookup.Component<T>.Size;
		return size > 0 ? GetComponentIndex(id) : GetAnyIndex(id);
	}

	internal ref ArchetypeChunk Add(EntityView ent, out int row)
	{
		ref var chunk = ref GetOrCreateChunk(_count);
		chunk.EntityAt(chunk.Count++) = ent;
		row = _count++;
		return ref chunk;
	}

	internal ref ArchetypeChunk Add(EcsID id, out int newRow)
		=> ref Add(new(_world, id), out newRow);

	private EcsID RemoveByRow(ref ArchetypeChunk chunk, int row)
	{
		_count -= 1;
		EcsAssert.Assert(_count >= 0, "Negative count");

		// ref var chunk = ref GetChunk(row);
		ref var lastChunk = ref GetChunk(_count);
		var removed = chunk.EntityAt(row).ID;

		if (row < _count)
		{
			EcsAssert.Assert(lastChunk.EntityAt(_count).ID.IsValid(), "Entity is invalid. This should never happen!");

			chunk.EntityAt(row) = lastChunk.EntityAt(_count);

			var srcIdx = _count & CHUNK_THRESHOLD;
			var dstIdx = row & CHUNK_THRESHOLD;
			var items = Components;
			for (var i = 0; i < items.Length; ++i)
			{
				var arrayToBeRemoved = chunk.Data![i];
				var lastValidArray = lastChunk.Data![i];

				CopyData(lastValidArray, srcIdx, arrayToBeRemoved, dstIdx, 1, items[i].Size, items[i].IsManaged);
			}

			ref var rec = ref _world.GetRecord(chunk.EntityAt(row).ID);
			rec.Chunk = chunk;
			rec.Row = row;
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
		=> RemoveByRow(ref record.Chunk, record.Row);

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

	internal ref ArchetypeChunk MoveEntity(Archetype newArch, ref ArchetypeChunk fromChunk, int oldRow, bool isRemove, out int newRow)
	{
		ref var toChunk = ref newArch.Add(fromChunk.EntityAt(oldRow), out newRow);

		int i = 0, j = 0;
		var count = isRemove ? newArch.Components.Length : Components.Length;

		ref var x = ref (isRemove ? ref j : ref i);
		ref var y = ref (!isRemove ? ref j : ref i);

		var srcIdx = oldRow & CHUNK_THRESHOLD;
		var dstIdx = newRow & CHUNK_THRESHOLD;
		var items = Components;
		var newItems = newArch.Components;
		for (; x < count; ++x, ++y)
		{
			while (items[i].ID != newItems[j].ID)
			{
				// advance the sign with less components!
				++y;
			}

			var fromArray = fromChunk.Data![i];
			var toArray = toChunk.Data![j];

			// copy the moved entity to the target archetype
			CopyData(fromArray!, srcIdx, toArray!, dstIdx, 1, items[i].Size, items[i].IsManaged);
		}

		_ = RemoveByRow(ref fromChunk, oldRow);

		return ref toChunk;
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

	internal void RemoveEmptyArchetypes(ref int removed, Dictionary<EcsID, Archetype> cache)
	{
		for (var i = _add.Count - 1; i >= 0; --i)
		{
			var edge = _add[i];
			edge.Archetype.RemoveEmptyArchetypes(ref removed, cache);

			if (edge.Archetype.Count == 0 && edge.Archetype._add.Count == 0)
			{
				cache.Remove(edge.Archetype.Id);
				_remove.Clear();
				_add.RemoveAt(i);

				removed += 1;
			}
		}
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

	internal int MatchWith(ReadOnlySpan<IQueryTerm> terms)
	{
		var result = Match.Validate(_comparer, _ids, terms);
		// if (result < 0)
		// 	return -1;

		return result;
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

	private static void CopyData(Array src, int srcIdx, Array dst, int dstIdx, int count, int elementSize, bool isManaged)
	{
		if (isManaged)
		{
			Array.Copy(src, srcIdx, dst, dstIdx, count);
		}
		else
		{
			ref var srcB = ref Unsafe.AddByteOffset(ref Unsafe.As<RawArrayData>(src).Data, (uint)(srcIdx * elementSize));
			ref var dstB = ref Unsafe.AddByteOffset(ref Unsafe.As<RawArrayData>(dst).Data, (uint)(dstIdx * elementSize));

			if (Vector.IsHardwareAccelerated)
			{
				CopySimd(ref srcB, ref dstB, elementSize * count);
				return;
			}

			Unsafe.CopyBlock(ref dstB, ref srcB, (uint)(count * elementSize));
		}
	}

	private static unsafe void CopySimd(ref byte src, ref byte dst, int totalBytes)
	{
		int vectorSize = Vector<byte>.Count; // SIMD chunk size
		int offset = 0;

		// Perform vectorized copy
		while (offset + vectorSize <= totalBytes)
		{
			var vector = Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.Add(ref src, offset));
			Unsafe.WriteUnaligned(ref Unsafe.Add(ref dst, offset), vector);
			offset += vectorSize;
		}

		// Process remaining bytes in chunks of 8 (long)
		const int wordSize = sizeof(long); // 8 bytes
		while (offset + wordSize <= totalBytes)
		{
			long word = Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref src, offset));
			Unsafe.WriteUnaligned(ref Unsafe.Add(ref dst, offset), word);
			offset += wordSize;
		}

		// Process remaining bytes one by one
		while (offset < totalBytes)
		{
			Unsafe.Add(ref dst, offset) = Unsafe.Add(ref src, offset);
			offset++;
		}
	}
}

struct EcsEdge
{
	public EcsID Id;
	public Archetype Archetype;
}

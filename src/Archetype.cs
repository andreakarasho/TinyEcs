using System.Collections.Frozen;
using System.Numerics;

namespace TinyEcs;


[SkipLocalsInit]
internal readonly struct Column
{
	public readonly Array Data;
	// public readonly uint[] Changed;

	internal Column(ref readonly ComponentInfo component, int chunkSize)
	{
		Data = Lookup.GetArray(component.ID, chunkSize)!;
		// Changed = new uint[chunkSize];
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyTo(int srcIdx, ref readonly Column dest, int dstIdx)
	{
		Array.Copy(Data, srcIdx, dest.Data, dstIdx, 1);
	}
}

[SkipLocalsInit]
internal struct ArchetypeChunk
{
	internal readonly Column[]? Columns;
	internal readonly EntityView[] Entities;

	internal ArchetypeChunk(ReadOnlySpan<ComponentInfo> sign, int chunkSize)
	{
		Entities = new EntityView[chunkSize];
		Columns = new Column[sign.Length];
		for (var i = 0; i < sign.Length; ++i)
			Columns[i] = new(in sign[i], chunkSize); // Lookup.GetArray(sign[i].ID, chunkSize)!;
	}

	public int Count { get; internal set; }


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref EntityView EntityAt(int row)
		=> ref Unsafe.Add(ref Entities.AsSpan(0, Count)[0], row & Archetype.CHUNK_THRESHOLD);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref T GetReference<T>(int column) where T : struct
	{
		if (column < 0 || column >= Columns!.Length)
			return ref Unsafe.NullRef<T>();

		var span = new Span<T>(Unsafe.As<T[]>(Columns[column].Data), 0, Count);
		return ref MemoryMarshal.GetReference(span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref T GetReferenceWithSize<T>(int column, out int sizeInBytes) where T : struct
	{
		if (column < 0 || column >= Columns!.Length)
		{
			sizeInBytes = 0;
			return ref Unsafe.NullRef<T>();
		}

		sizeInBytes = Unsafe.SizeOf<T>();

		var span = new Span<T>(Unsafe.As<T[]>(Columns[column].Data), 0, Count);
		return ref MemoryMarshal.GetReference(span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref Column GetColumn(int column)
	{
		if (column < 0 || column >= Columns!.Length)
		{
			return ref Unsafe.NullRef<Column>();
		}

		return ref Columns[column];
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
		if (column < 0 || column >= Columns!.Length)
			return Span<T>.Empty;

		var span = new Span<T>(Unsafe.As<T[]>(Columns[column].Data), 0, Count);
		return span;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ReadOnlySpan<EntityView> GetEntities()
		=> Entities.AsSpan(0, Count);
}

public sealed class Archetype : IComparable<Archetype>
{
	const int ARCHETYPE_INITIAL_CAPACITY = 1;

	internal const int CHUNK_SIZE = 4096;
	internal const int CHUNK_LOG2 = 12;
	internal const int CHUNK_THRESHOLD = CHUNK_SIZE - 1;


	private readonly World _world;
	private ArchetypeChunk[] _chunks;
	private readonly ComponentComparer _comparer;
	private readonly FrozenDictionary<EcsID, int> _componentsLookup, _allLookup
#if USE_PAIR
		, _pairsLookup
#endif
		;
	private readonly FrozenSet<EcsID> _ids;
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
		All = sign;
		Components = All.Where(x => x.Size > 0).ToArray();
		Tags = All.Where(x => x.Size <= 0).ToArray();
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

		_ids = All.Select(s => s.ID).ToFrozenSet();
		_add = new ();
		_remove = new ();
	}


	public World World => _world;
	public int Count => _count;
	public readonly ComponentInfo[] All, Components, Tags, Pairs;
	public EcsID Id { get; }
	internal ReadOnlySpan<ArchetypeChunk> Chunks => _chunks.AsSpan(0, (_count + CHUNK_SIZE - 1) >> CHUNK_LOG2);
	internal int EmptyChunks => _chunks.Length - ((_count + CHUNK_SIZE - 1) >> CHUNK_LOG2);

	private ref ArchetypeChunk GetOrCreateChunk(int index)
	{
		index >>= CHUNK_LOG2;

		if (index >= _chunks.Length)
			Array.Resize(ref _chunks, Math.Max(ARCHETYPE_INITIAL_CAPACITY, _chunks.Length * 2));

		ref var chunk = ref _chunks[index];
		if (chunk.Columns == null)
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
			for (var i = 0; i < Components.Length; ++i)
			{
				lastChunk.Columns![i].CopyTo(srcIdx, in chunk.Columns![i], dstIdx);
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

	internal Archetype InsertVertex(Archetype left, ComponentInfo[] sign, EcsID id)
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

			fromChunk.Columns![i].CopyTo(srcIdx, in toChunk.Columns![j], dstIdx);
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
		var result = MatchWith(terms);
		if (result == ArchetypeSearchResult.Stop)
		{
			return;
		}

		if (result == ArchetypeSearchResult.Found)
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

	internal ArchetypeSearchResult MatchWith(ReadOnlySpan<IQueryTerm> terms)
	{
		return FilterMatch.Match(_ids, terms);
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

	public int CompareTo(Archetype? other)
	{
		return Id.CompareTo(other?.Id);
	}
}

struct EcsEdge
{
	public EcsID Id;
	public Archetype Archetype;
}

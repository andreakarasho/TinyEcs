using System.Collections.Frozen;
using System.Threading;

namespace TinyEcs;


[SkipLocalsInit]
internal readonly struct Column
{
	public readonly Array Data;
	public readonly uint[] ChangedTicks, AddedTicks;

	internal Column(ref readonly ComponentInfo component, int chunkSize)
	{
		Data = Lookup.GetArray(component.ID, chunkSize)!;
		ChangedTicks = new uint[chunkSize];
		AddedTicks = new uint[chunkSize];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MarkChanged(int index, uint ticks)
	{
		ChangedTicks[index] = ticks;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MarkAdded(int index, uint ticks)
	{
		AddedTicks[index] = ticks;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyTo(int srcIdx, ref readonly Column dest, int dstIdx)
	{
		Array.Copy(Data, srcIdx, dest.Data, dstIdx, 1);
		dest.ChangedTicks[dstIdx] = ChangedTicks[srcIdx];
		dest.AddedTicks[dstIdx] = AddedTicks[srcIdx];
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
			Columns[i] = new(in sign[i], chunkSize);
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MarkChanged(int column, int row, uint ticks)
	{
		Columns![column].MarkChanged(row & Archetype.CHUNK_THRESHOLD, ticks);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MarkAdded(int column, int row, uint ticks)
	{
		Columns![column].MarkAdded(row & Archetype.CHUNK_THRESHOLD, ticks);
	}
}

public sealed class Archetype : IComparable<Archetype>
{
	const int ARCHETYPE_INITIAL_CAPACITY = 1;

	internal const int CHUNK_SIZE = 4096;
	internal const int CHUNK_LOG2 = 12;
	internal const int CHUNK_THRESHOLD = CHUNK_SIZE - 1;

	private static long _traversalVersion;


	private readonly World _world;
	private ArchetypeChunk[] _chunks;
	private readonly ComponentComparer _comparer;
	private readonly FrozenDictionary<EcsID, int> _componentsLookup, _allLookup
#if USE_PAIR
		, _pairsLookup
#endif
		;
	internal readonly List<EcsEdge> _add, _remove;
	private int _count;
	private long _lastTraversalVersion;
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


		var hash = 0ul;
		var dict = new Dictionary<EcsID, int>();
		var allDict = new Dictionary<EcsID, int>();
		var maxId = -1;
		for (int i = 0, cur = 0; i < sign.Length; ++i)
		{
			hash = UnorderedSetHasher.Combine(hash, sign[i].ID);

			if (sign[i].Size > 0)
			{
				dict.Add(sign[i].ID, cur++);
				maxId = Math.Max(maxId, (int)sign[i].ID);
			}

			allDict.Add(sign[i].ID, i);
		}

		Id = hash;

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

		_add = new();
		_remove = new();
	}


	public World World => _world;
	public int Count => _count;
	public readonly ComponentInfo[] All, Components, Tags, Pairs = Array.Empty<ComponentInfo>();
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
		var leftIsSubset = left.All.Length < vertex.All.Length;

		if (leftIsSubset)
		{
			ConnectSubsetSuperset(left, vertex, id, true);
			InsertVertex(vertex, left);
		}
		else
		{
			ConnectSubsetSuperset(vertex, left, id, false);
			InsertVertex(vertex, null);
		}

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

	private static void ConnectSubsetSuperset(Archetype subset, Archetype superset, EcsID id, bool registerInSubsetAdd)
	{
		if (registerInSubsetAdd)
		{
			var addEdges = CollectionsMarshal.AsSpan(subset._add);
			var exists = false;
			foreach (ref var edge in addEdges)
			{
				if (edge.Archetype == superset && edge.Id == id)
				{
					exists = true;
					break;
				}
			}

			if (!exists)
			{
				subset._add.Add(new EcsEdge { Archetype = superset, Id = id });
			}
		}

		var removeEdges = CollectionsMarshal.AsSpan(superset._remove);
		foreach (ref var edge in removeEdges)
		{
			if (edge.Archetype == subset && edge.Id == id)
				return;
		}

		superset._remove.Add(new EcsEdge { Archetype = subset, Id = id });
	}

	private static void AddEdgeOnly(Archetype subset, Archetype superset, EcsID id)
	{
		var addEdges = CollectionsMarshal.AsSpan(subset._add);
		foreach (ref var edge in addEdges)
		{
			if (edge.Archetype == superset && edge.Id == id)
				return;
		}

		subset._add.Add(new EcsEdge { Archetype = superset, Id = id });
	}

	private void InsertVertex(Archetype newNode, Archetype? preferredParent)
	{
		var all = newNode.All;
		if (all.Length == 0)
			return;

		var world = newNode._world;
		var addParent = preferredParent;

		for (var i = all.Length - 1; i >= 0; --i)
		{
			ref readonly var component = ref all[i];
			var subsetId = newNode.ComputeHashWithout(component.ID);

			if (!world.TryGetArchetype(subsetId, out var subset))
				continue;

			var register = false;
			if (preferredParent != null && ReferenceEquals(subset, preferredParent))
			{
				register = true;
				addParent = preferredParent;
			}
			else if (addParent == null)
			{
				addParent = subset;
				register = true;
			}

			ConnectSubsetSuperset(subset!, newNode, component.ID, register);
		}

		if (addParent == null)
		{
			AddEdgeOnly(this, newNode, all[^1].ID);
		}
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
		var stack = ArrayPool<Archetype>.Shared.Rent(64);
		var version = Interlocked.Increment(ref _traversalVersion);
		var top = 0;

		try
		{
			stack[top++] = this;

			while (top > 0)
			{
				var node = stack[--top];

				if (node._lastTraversalVersion == version)
				{
					continue;
				}

				node._lastTraversalVersion = version;

				var result = node.MatchWith(terms);
				if (result == ArchetypeSearchResult.Stop)
				{
					continue;
				}

				if (result == ArchetypeSearchResult.Found && node._count > 0)
				{
					matched.Add(node);
				}

				var add = node._add;
				if (add.Count <= 0)
				{
					continue;
				}

				foreach (ref var edge in CollectionsMarshal.AsSpan(add))
				{
					var next = edge.Archetype;
					if (next._lastTraversalVersion == version)
					{
						continue;
					}

					if (top == stack.Length)
					{
						var newStack = ArrayPool<Archetype>.Shared.Rent(stack.Length << 1);
						Array.Copy(stack, 0, newStack, 0, top);
						ArrayPool<Archetype>.Shared.Return(stack, clearArray: false);
						stack = newStack;
					}

					stack[top++] = next;
				}
			}
		}
		finally
		{
			ArrayPool<Archetype>.Shared.Return(stack, clearArray: false);
		}
	}

	internal ArchetypeSearchResult MatchWith(ReadOnlySpan<IQueryTerm> terms)
	{
		return FilterMatch.Match(this, terms);
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

	internal ulong ComputeHashWithout(EcsID id)
	{
		var hash = 0ul;
		foreach (ref readonly var cmp in All.AsSpan())
		{
			if (cmp.ID != id)
				hash = UnorderedSetHasher.Combine(hash, cmp.ID);
		}
		return hash;
	}
}

struct EcsEdge
{
	public EcsID Id;
	public Archetype Archetype;
}

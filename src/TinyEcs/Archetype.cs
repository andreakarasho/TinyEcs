using System.Collections.Frozen;
using System.Threading;

namespace TinyEcs;


public sealed class Archetype : IComparable<Archetype>
{
	const int INITIAL_CAPACITY = 8;

	private static long _traversalVersion;

	private readonly World _world;
	private readonly ComponentComparer _comparer;
	private readonly FrozenDictionary<EcsID, int> _componentsLookup, _allLookup
#if USE_PAIR
		, _pairsLookup
#endif
		;
	internal readonly List<EcsEdge> _add, _remove;
	private int _count;
	private int _capacity;
	private long _lastTraversalVersion;
	private readonly int[] _fastLookup;
	private readonly ulong[] _componentBits;
	private readonly int _bitsetMaxId;

	internal Column[]? _columns;
	internal EntityView[] _entities;

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

		_capacity = 0;
		_entities = Array.Empty<EntityView>();
		_columns = Components.Length > 0 ? new Column[Components.Length] : null;
		if (_columns != null)
		{
			for (var i = 0; i < Components.Length; ++i)
				_columns[i] = _world.CreateColumn(Components[i].ID, 0);
		}

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

		var words = world.ComponentBitsetWords;
		_bitsetMaxId = words << 6;
		_componentBits = words > 0 ? new ulong[words] : Array.Empty<ulong>();
		for (var i = 0; i < sign.Length; ++i)
		{
			var sid = sign[i].ID;
#if USE_PAIR
			if (sid.IsPair()) continue;
#endif
			if (sid < (ulong)_bitsetMaxId)
				_componentBits[(int)(sid >> 6)] |= 1ul << (int)(sid & 63);
		}

		_add = new();
		_remove = new();
	}


	public World World => _world;
	public int Count => _count;
	public readonly ComponentInfo[] All, Components, Tags, Pairs = Array.Empty<ComponentInfo>();
	public EcsID Id { get; }

	internal Column[]? Columns => _columns;
	internal ReadOnlySpan<EntityView> Entities => _entities.AsSpan(0, _count);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ref EntityView EntityAt(int row) => ref _entities[row];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref T GetReference<T>(int column) where T : struct
	{
		if (_columns == null || column < 0 || column >= _columns.Length)
			return ref Unsafe.NullRef<T>();

		return ref ((Column<T>)_columns[column]).GetFirstRef();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref T GetReferenceWithSize<T>(int column, out int sizeInBytes) where T : struct
	{
		if (_columns == null || column < 0 || column >= _columns.Length)
		{
			sizeInBytes = 0;
			return ref Unsafe.NullRef<T>();
		}

		sizeInBytes = Unsafe.SizeOf<T>();
		return ref ((Column<T>)_columns[column]).GetFirstRef();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref T GetReferenceAt<T>(int column, int row) where T : struct
	{
		if (_columns == null || column < 0 || column >= _columns.Length)
			return ref Unsafe.NullRef<T>();

		return ref ((Column<T>)_columns[column]).GetRef(row);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<T> GetSpan<T>(int column) where T : struct
	{
		if (_columns == null || column < 0 || column >= _columns.Length)
			return Span<T>.Empty;

		return ((Column<T>)_columns[column]).AsSpan(_count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Column? GetColumn(int column)
	{
		if (_columns == null || column < 0 || column >= _columns.Length)
			return null;
		return _columns[column];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Column<T>? GetColumn<T>(int column) where T : struct
	{
		if (_columns == null || column < 0 || column >= _columns.Length)
			return null;
		return (Column<T>)_columns[column];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MarkChanged(int column, int row, uint ticks)
	{
		_columns![column].MarkChanged(row, ticks);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MarkAdded(int column, int row, uint ticks)
	{
		_columns![column].MarkAdded(row, ticks);
	}

	internal int GetComponentIndex(EcsID id)
	{
#if USE_PAIR
		if (id.IsPair())
		{
			return _componentsLookup.GetValueOrDefault(id, -1);
		}
#endif
		var i = (uint)id;
		return i >= (uint)_fastLookup.Length ? -1 : _fastLookup[i];
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool HasIndex(EcsID id)
	{
#if USE_PAIR
		if (id.IsPair())
			return _allLookup.ContainsKey(id);
#endif
		if (id < (ulong)_bitsetMaxId)
			return (_componentBits[(int)(id >> 6)] & (1ul << (int)(id & 63))) != 0;
		return _allLookup.ContainsKey(id);
	}

	internal ReadOnlySpan<ulong> ComponentBits => _componentBits;
	internal int BitsetMaxId => _bitsetMaxId;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetComponentIndex<T>() where T : struct
	{
		var id = _world.Component<T>().ID;
		var size = Lookup.Component<T>.Size;
		return size > 0 ? GetComponentIndex(id) : GetAnyIndex(id);
	}

	private void EnsureCapacity(int needed)
	{
		if (needed <= _capacity)
			return;

		var newCap = _capacity == 0 ? INITIAL_CAPACITY : _capacity;
		while (newCap < needed) newCap *= 2;

		Array.Resize(ref _entities, newCap);
		if (_columns != null)
		{
			for (var i = 0; i < _columns.Length; ++i)
				_columns[i].EnsureCapacity(newCap);
		}

		_capacity = newCap;
	}

	internal int Add(EntityView ent)
	{
		EnsureCapacity(_count + 1);
		_entities[_count] = ent;
		return _count++;
	}

	internal int Add(EcsID id) => Add(new EntityView(_world, id));

	private EcsID RemoveByRow(int row)
	{
		_count -= 1;
		EcsAssert.Assert(_count >= 0, "Negative count");

		var removed = _entities[row].ID;

		if (row < _count)
		{
			EcsAssert.Assert(_entities[_count].ID.IsValid(), "Entity is invalid. This should never happen!");

			_entities[row] = _entities[_count];

			if (_columns != null)
			{
				for (var i = 0; i < _columns.Length; ++i)
					_columns[i].CopyTo(_count, _columns[i], row);
			}

			ref var rec = ref _world.GetRecord(_entities[row].ID);
			rec.Row = row;
		}

		return removed;
	}

	internal EcsID Remove(ref EcsRecord record) => RemoveByRow(record.Row);

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

	internal int MoveEntity(Archetype newArch, int oldRow, bool isRemove)
	{
		var newRow = newArch.Add(_entities[oldRow]);

		int i = 0, j = 0;
		var count = isRemove ? newArch.Components.Length : Components.Length;

		ref var x = ref (isRemove ? ref j : ref i);
		ref var y = ref (!isRemove ? ref j : ref i);

		var items = Components;
		var newItems = newArch.Components;
		for (; x < count; ++x, ++y)
		{
			while (items[i].ID != newItems[j].ID)
			{
				// advance the sign with less components!
				++y;
			}

			_columns![i].CopyTo(oldRow, newArch._columns![j], newRow);
		}

		_ = RemoveByRow(oldRow);

		return newRow;
	}

	internal void Clear()
	{
		_count = 0;
		_add.Clear();
		_remove.Clear();
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

		return null;
	}

	internal void GetSuperSets(Query query, List<Archetype> matched)
	{
		var stack = ArrayPool<Archetype>.Shared.Rent(64);
		var version = Interlocked.Increment(ref _traversalVersion);
		var top = 0;
		var fastPath = query.FastPath;
		var withMask = query.WithMask;
		var withoutMask = query.WithoutMask;
		var termIds = query.TermIds;
		var termOps = query.TermOps;

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

				var result = fastPath
					? FilterMatch.MatchBits(node, withMask!, withoutMask!)
					: FilterMatch.MatchSwitch(node, termIds, termOps);
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

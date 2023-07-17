namespace TinyEcs;

sealed unsafe class Archetype
{
	const int ARCHETYPE_INITIAL_CAPACITY = 16;

	private readonly World _world;
	private int _capacity, _count;
	private EntityID[] _entityIDs;
	internal byte[][] _componentsData;
	internal List<EcsEdge> _edgesLeft, _edgesRight;
	private readonly EntitySparseSet<EntityID> _lookup;
	private readonly EntityID[] _components;

	public Archetype(World world, ReadOnlySpan<EcsComponent> components)
	{
		_world = world;
		_capacity = ARCHETYPE_INITIAL_CAPACITY;
		_count = 0;
		_components = new EntityID[components.Length];
		ComponentInfo = new EcsComponent[components.Length];
		_entityIDs = new EntityID[ARCHETYPE_INITIAL_CAPACITY];
		_componentsData = new byte[components.Length][];
		_edgesLeft = new List<EcsEdge>();
		_edgesRight = new List<EcsEdge>();
		_lookup = new EntitySparseSet<EntityID>();

		for (var i = 0; i < components.Length; i++)
		{
			_components[i] = components[i].ID;
			_lookup.Add(components[i].ID, (EntityID)i);
		}

		ResizeComponentArray(ARCHETYPE_INITIAL_CAPACITY);
	}


	public int Count => _count;
	public EntityID[] Entities => _entityIDs;
	public EntityID[] Components => _components;
	public readonly EcsComponent[] ComponentInfo;
	public World World => _world;



	public int GetComponentIndex(EntityID component)
	{
		ref var idx = ref _lookup.Get(component);

		return Unsafe.IsNullRef(ref idx) ? -1 : (int)idx;
	}

	public int Add(EntityID entityID)
	{
		if (_capacity == _count)
		{
			_capacity *= 2;

			Array.Resize(ref _entityIDs, _capacity);
			ResizeComponentArray(_capacity);
		}

		_entityIDs[_count] = entityID;

		return _count++;
	}

	public EntityID Remove(int row)
	{
		var removed = _entityIDs[row];
		_entityIDs[row] = _entityIDs[_count - 1];

		for (int i = 0; i < _components.Length; ++i)
		{
			ref readonly var meta = ref ComponentInfo[i];
			var leftArray = _componentsData[i].AsSpan();

			var removeComponent = leftArray.Slice(meta.Size * row, meta.Size);
			var swapComponent = leftArray.Slice(meta.Size * (_count - 1), meta.Size);

			swapComponent.CopyTo(removeComponent);
		}

		--_count;

		return removed;
	}

	public Archetype InsertVertex(Archetype left, ReadOnlySpan<EcsComponent> newType, EntityID component)
	{
		var vertex = new Archetype(left._world, newType);
		MakeEdges(left, vertex, component);
		InsertVertex(vertex);
		return vertex;
	}

	public static int MoveEntity(Archetype from, Archetype to, int fromRow)
	{
		var removed = from._entityIDs[fromRow];
		from._entityIDs[fromRow] = from._entityIDs[from._count - 1];

		var toRow = to.Add(removed);

		Copy(from, fromRow, to, toRow);

		--from._count;

		return toRow;
	}

	public Span<byte> GetComponentRaw(EntityID component, int row, int count)
	{
		var column = GetComponentIndex(component);
		if (column < 0)
		{
			return Span<byte>.Empty;
		}

		Debug.Assert(row < Count);

		ref readonly var meta = ref ComponentInfo[column];

		return _componentsData[column].AsSpan(meta.Size * row, meta.Size * count);
	}

	public void Clear() 
		=> _count = 0;

	static void Copy(Archetype from, int fromRow, Archetype to, int toRow)
	{
		var isLeft = to._components.Length < from._components.Length;
		int i = 0, j = 0;
		var count = isLeft ? to._components.Length : from._components.Length;

		ref var x = ref (isLeft ? ref j : ref i);
		ref var y = ref (!isLeft ? ref j : ref i);

		for (; x < count; ++x, ++y)
		{
			while (from._components[i] != to._components[j])
			{
				// advance the sign with less components!
				++y;
			}

			ref readonly var meta = ref from.ComponentInfo[i];
			var leftArray = from._componentsData[i].AsSpan();
			var rightArray = to._componentsData[j].AsSpan();
			var insertComponent = rightArray.Slice(meta.Size * toRow, meta.Size);
			var removeComponent = leftArray.Slice(meta.Size * fromRow, meta.Size);
			var swapComponent = leftArray.Slice(meta.Size * (from._count - 1), meta.Size);
			removeComponent.CopyTo(insertComponent);
			swapComponent.CopyTo(removeComponent);
		}
	}

	private static void MakeEdges(Archetype left, Archetype right, EntityID id)
	{
		left._edgesRight.Add(new EcsEdge() { Archetype = right, ComponentID = id });
		right._edgesLeft.Add(new EcsEdge() { Archetype = left, ComponentID = id });
	}

	private void InsertVertex(Archetype newNode)
	{
		var nodeTypeLen = _components.Length;
		var newTypeLen = newNode._components.Length;

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

		if (!IsSuperset(newNode._components))
		{
			return;
		}

		var i = 0;
		var newNodeTypeLen = newNode._components.Length;
		for (; i < newNodeTypeLen && _components[i] == newNode._components[i]; ++i) { }

		MakeEdges(newNode, this, _components[i]);
	}

	private void ResizeComponentArray(int capacity)
	{
		for (int i = 0; i < _components.Length; ++i)
		{
			ref readonly var meta = ref ComponentInfo[i];
			Array.Resize(ref _componentsData[i], meta.Size * capacity);
			_capacity = capacity;
		}
	}

	public bool IsSuperset(ReadOnlySpan<EntityID> other)
	{
		int i = 0, j = 0;
		while (i < _components.Length && j < other.Length)
		{
			if (_components[i] == other[j])
			{
				j++;
			}

			i++;
		}

		return j == other.Length;
	}
}

struct EcsEdge
{
	public EntityID ComponentID;
	public Archetype Archetype;
}
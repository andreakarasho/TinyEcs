using System.Numerics;

namespace TinyEcs;

public sealed unsafe class Archetype
{
	const int ARCHETYPE_INITIAL_CAPACITY = 16;

	private readonly World _world;
	private int _capacity, _count;
	private EntityID[] _entityIDs;
	private readonly EntityID[] _components;
	internal List<EcsEdge> _edgesLeft, _edgesRight;
	private readonly Table _table;


	internal Archetype(World world, Table table, ReadOnlySpan<EcsComponent> components)
	{
		_world = world;
		_table = table;
		_capacity = ARCHETYPE_INITIAL_CAPACITY;
		_count = 0;
		_entityIDs = new EntityID[ARCHETYPE_INITIAL_CAPACITY];
		_edgesLeft = new List<EcsEdge>();
		_edgesRight = new List<EcsEdge>();
		_components = new EntityID[components.Length];
		ComponentInfo = components.ToArray();
		for (int i = 0; i < components.Length; ++i)
		{
			_components[i] = components[i].ID;
		}
	}

	internal EntityID[] Entities => _entityIDs;
	public World World => _world;
	public int Count => _count;
	internal Table Table => _table;

	public readonly EcsComponent[] ComponentInfo;
	public EntityID[] Components => _components;



	internal int GetComponentIndex(EntityID component, int size)
	{
		if (size <= 0)
		{
			return Array.BinarySearch(_components, component);
		}

		return _table.GetComponentIndex(component);
	}

	internal int Add(EntityID entityID)
	{
		if (_capacity == _count)
		{
			_capacity *= 2;

			Array.Resize(ref _entityIDs, _capacity);
		}


		//Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entityIDs), _count) = entityID;
		_entityIDs[_count] = entityID;

		return _count++;
	}

	internal EntityID Remove(ref EcsRecord record)
	{
		var removed = _entityIDs[record.ArchetypeRow];
		_entityIDs[record.ArchetypeRow] = _entityIDs[_count - 1];

		_table.Remove(ref record);

		--_count;

		return removed;
	}

	internal Archetype InsertVertex(Archetype left, Table newType, ReadOnlySpan<EcsComponent> components, EntityID component)
	{
		var vertex = new Archetype(left._world, newType, components);
		MakeEdges(left, vertex, component);
		InsertVertex(vertex);
		return vertex;
	}

	internal (int, int) MoveEntity(Archetype to, int fromRow, int fromTableRow, int size)
	{
		var removed = _entityIDs[fromRow];
		_entityIDs[fromRow] = _entityIDs[_count - 1];

		var toRow = to.Add(removed);
		var toTableRow = fromTableRow;

		if (size != 0)
		{
			toTableRow = _table.MoveTo(fromTableRow, to._table);
		}

		--_count;

		return (toRow, toTableRow);
	}

	internal Span<byte> GetComponentRaw(EntityID component, int size, int row, int count)
	{
		if (size <= 0 && GetComponentIndex(component, size) >= 0)
			return Span<byte>.Empty;

		return _table.GetComponentRaw(component, row, count);
	}

	internal void Clear()
	{
		_count = 0;
		_capacity = ARCHETYPE_INITIAL_CAPACITY;
		Array.Resize(ref _entityIDs, _capacity);
	}

	internal void Optimize()
	{
		// var pow = (int) BitOperations.RoundUpToPowerOf2((uint) _count);
		// var newCapacity = Math.Max(ARCHETYPE_INITIAL_CAPACITY, pow);
		// if (newCapacity < _capacity)
		// {
		// 	_capacity = newCapacity;
		// 	Array.Resize(ref _entityIDs, _capacity);
		// 	ResizeComponentArray(_capacity);
		// }
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool IsSuperset(UnsafeSpan<EntityID> other)
	{
		var thisComps = new UnsafeSpan<EntityID>(_components);

		while (thisComps.CanAdvance() && other.CanAdvance())
		{
			if (thisComps.Value == other.Value)
			{
				other.Advance();
			}

			thisComps.Advance();
		}

		return Unsafe.AreSame(ref other.Value, ref other.End);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal int FindMatch(UnsafeSpan<Term> searching)
	{
		var currents = new UnsafeSpan<EntityID>(_components);

		while (currents.CanAdvance() && searching.CanAdvance())
		{
			if (currents.Value == searching.Value.ID)
			{
				if (searching.Value.Op != TermOp.With)
				{
					return -1;
				}

				searching.Advance();
			}
			else if (currents.Value > searching.Value.ID && searching.Value.Op != TermOp.With)
			{
				searching.Advance();
				continue;
			}

			currents.Advance();
		}

		while (searching.CanAdvance() && searching.Value.Op != TermOp.With)
			searching.Advance();

		return Unsafe.AreSame(ref searching.Value, ref searching.End) ? 0 : 1;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public UnsafeSpan<T> Field<T>() where T : unmanaged
	{
		var id = World.Component<T>();

		var span = GetComponentRaw(id, TypeInfo<T>.Size, 0, _count);
		ref var start = ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));
		ref var end = ref Unsafe.Add(ref start, _count);

		return new UnsafeSpan<T>(ref start, ref end);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Has<T>() where T : unmanaged
	{
		var id = World.Component<T>();
		var column = GetComponentIndex(id, TypeInfo<T>.Size);

		return column >= 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public EntityView Entity(int row)
		=> new (World, Entities[row]);


	public void Print()
	{
		PrintRec(this, 0, 0);

		static void PrintRec(Archetype root, int depth, EntityID rootComponent)
		{
			Console.WriteLine("{0}[{1}] |{2}|", new string('.', depth), string.Join(", ", root.Components), rootComponent);

			foreach (ref readonly var edge in CollectionsMarshal.AsSpan(root._edgesRight))
			{
				PrintRec(edge.Archetype, depth + 1, edge.ComponentID);
			}
		}
	}
}

struct EcsEdge
{
	public EntityID ComponentID;
	public Archetype Archetype;
}

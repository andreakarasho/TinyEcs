using System.Numerics;

namespace TinyEcs;

public sealed unsafe class Archetype
{
	const int ARCHETYPE_INITIAL_CAPACITY = 16;

	private readonly World _world;
	private int _capacity, _count;
	private EntityID[] _entityIDs;

	internal List<EcsEdge> _edgesLeft, _edgesRight;

	private readonly Table _table;


	internal Archetype(World world, Table table)
	{
		_world = world;
		_table = table;
		_capacity = ARCHETYPE_INITIAL_CAPACITY;
		_count = 0;
		_entityIDs = new EntityID[ARCHETYPE_INITIAL_CAPACITY];
		_edgesLeft = new List<EcsEdge>();
		_edgesRight = new List<EcsEdge>();

		//ResizeComponentArray(ARCHETYPE_INITIAL_CAPACITY);
	}

	internal EntityID[] Entities => _entityIDs;
	public World World => _world;
	public int Count => _count;
	internal Table Table => _table;


	internal int GetComponentIndex(EntityID component)
	{
		return _table.GetComponentIndex(component);
	}

	internal (int, int) Add(EntityID entityID)
	{
		if (_capacity == _count)
		{
			_capacity *= 2;

			Array.Resize(ref _entityIDs, _capacity);
			//ResizeComponentArray(_capacity);
		}

		var tableRow = _table.Increase();

		//Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_entityIDs), _count) = entityID;
		_entityIDs[_count] = entityID;

		return (_count++, tableRow);
	}

	internal EntityID Remove(ref EcsRecord record)
	{
		var removed = _entityIDs[record.ArchetypeRow];
		_entityIDs[record.ArchetypeRow] = _entityIDs[_count - 1];

		_table.Remove(ref record);

		--_count;

		return removed;
	}

	internal Archetype InsertVertex(Archetype left, Table newType, EntityID component)
	{
		var vertex = new Archetype(left._world, newType);
		MakeEdges(left, vertex, component);
		InsertVertex(vertex);
		return vertex;
	}

	internal static (int, int) MoveEntity(Archetype from, Archetype to, int fromRow, int fromTableRow)
	{
		var removed = from._entityIDs[fromRow];
		from._entityIDs[fromRow] = from._entityIDs[from._count - 1];

		(var toRow, var toTableRow) = to.Add(removed);

		from._table.MoveTo(fromTableRow, to._table, toTableRow);

		--from._count;

		return (toRow, toTableRow);
	}

	internal Span<byte> GetComponentRaw(EntityID component, int row, int count)
	{
		return _table.GetComponentRaw(component, row, count);
	}

	internal void Clear()
	{
		_count = 0;
		_capacity = ARCHETYPE_INITIAL_CAPACITY;
		Array.Resize(ref _entityIDs, _capacity);
		//ResizeComponentArray(_capacity);
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
		var nodeTypeLen = _table.Components.Length;
		var newTypeLen = newNode._table.Components.Length;

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

		if (!IsSuperset(newNode._table.Components))
		{
			return;
		}

		var i = 0;
		var newNodeTypeLen = newNode._table.Components.Length;
		for (; i < newNodeTypeLen && _table.Components[i] == newNode._table.Components[i]; ++i) { }

		MakeEdges(newNode, this, _table.Components[i]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool IsSuperset(UnsafeSpan<EntityID> other)
	{
		var thisComps = new UnsafeSpan<EntityID>(_table.Components);

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
	internal int FindMatch(UnsafeSpan<Term> other)
	{
		var thisComps = new UnsafeSpan<EntityID>(_table.Components);

		while (thisComps.CanAdvance() && other.CanAdvance())
		{
			if (thisComps.Value == other.Value.ID)
			{
				if (other.Value.Op != TermOp.With)
				{
					return -1;
				}

				other.Advance();
			}
			else if (other.Value.Op != TermOp.With)
			{
				other.Advance();
				continue;
			}

			thisComps.Advance();
		}

		while (other.CanAdvance() && other.Value.Op != TermOp.With)
			other.Advance();

		return Unsafe.AreSame(ref other.Value, ref other.End) ? 0 : 1;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public UnsafeSpan<T> Field<T>() where T : unmanaged
	{
		var id = World.Component<T>();

		var span = _table.GetComponentRaw(id, 0, _count);
		ref var start = ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));
		ref var end = ref Unsafe.Add(ref start, _count);

		return new UnsafeSpan<T>(ref start, ref end);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Has<T>() where T : unmanaged
	{
		var id = World.Component<T>();
		var column = GetComponentIndex(id);

		return column >= 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public EntityView Entity(int row)
		=> new (World, Entities[row]);
}

struct EcsEdge
{
	public EntityID ComponentID;
	public Archetype Archetype;
}

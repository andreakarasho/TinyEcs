using System.Numerics;

namespace TinyEcs;

public sealed unsafe class Archetype
{
	const int ARCHETYPE_INITIAL_CAPACITY = 16;

	private readonly World _world;
	private int _capacity, _count;
	private ArchetypeEntity[] _entities;
	internal List<EcsEdge> _edgesLeft, _edgesRight;
	private readonly Table _table;


	internal Archetype(World world, Table table, ReadOnlySpan<EcsComponent> components)
	{
		_world = world;
		_table = table;
		_capacity = ARCHETYPE_INITIAL_CAPACITY;
		_count = 0;
		_entities = new ArchetypeEntity[ARCHETYPE_INITIAL_CAPACITY];
		_edgesLeft = new List<EcsEdge>();
		_edgesRight = new List<EcsEdge>();
		ComponentInfo = components.ToArray();
	}

	internal ArchetypeEntity[] Entities => _entities;
	public World World => _world;
	public int Count => _count;
	internal Table Table => _table;

	public readonly EcsComponent[] ComponentInfo;



	internal int GetComponentIndex(ref EcsComponent cmp)
	{
		if (cmp.Size <= 0)
		{
			return Array.BinarySearch(ComponentInfo, cmp);
		}

		return _table.GetComponentIndex(ref cmp);
	}

	internal (int, int) Add(EntityID entityID, int tableRow = -1)
	{
		if (_capacity == _count)
		{
			_capacity *= 2;

			Array.Resize(ref _entities, _capacity);
		}

		ref var archEnt = ref _entities[_count];
		archEnt.Entity = entityID;
		archEnt.TableRow = tableRow < 0 ? _table.Add(entityID) : tableRow;

		if (archEnt.TableRow < _table.Entities.Length)
			EcsAssert.Assert(_table.Entities[archEnt.TableRow] == entityID);

		return (_count++, archEnt.TableRow);
	}

	internal EntityID Remove(ref EcsRecord record)
	{
		var removed = _entities[record.Row];
		_entities[record.Row] = _entities[_count - 1];

		_table.Remove(removed.TableRow);

		--_count;

		return removed.Entity;
	}

	internal Archetype InsertVertex(Archetype left, Table newType, ReadOnlySpan<EcsComponent> components, ref EcsComponent component)
	{
		var vertex = new Archetype(left._world, newType, components);
		MakeEdges(left, vertex, component.ID);
		InsertVertex(vertex);
		return vertex;
	}

	internal int MoveEntity(Archetype to, int fromRow, bool keepTable)
	{
		var removed = _entities[fromRow];
		_entities[fromRow] = _entities[_count - 1];

		if (removed.Entity == 23)
		{

		}

		(var toRow, var toTableRow) = to.Add(removed.Entity, keepTable ? removed.TableRow : -1);

		if (!keepTable)
			_table.MoveTo(removed.TableRow, to._table, toTableRow);

		--_count;

		return toRow;
	}

	internal Span<byte> GetComponentRaw(ref EcsComponent cmp, int row, int count)
	{
		if (cmp.Size <= 0 && GetComponentIndex(ref cmp) >= 0)
			return Span<byte>.Empty;

		return _table.GetComponentRaw(ref cmp, _entities[row].TableRow, count);
	}

	internal void Clear()
	{
		_count = 0;
		_capacity = ARCHETYPE_INITIAL_CAPACITY;
		Array.Resize(ref _entities, _capacity);
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
		var nodeTypeLen = ComponentInfo.Length;
		var newTypeLen = newNode.ComponentInfo.Length;

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

		if (!IsSuperset(newNode.ComponentInfo))
		{
			return;
		}

		var i = 0;
		var newNodeTypeLen = newNode.ComponentInfo.Length;
		for (; i < newNodeTypeLen && ComponentInfo[i].ID == newNode.ComponentInfo[i].ID; ++i) { }

		MakeEdges(newNode, this, ComponentInfo[i].ID);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool IsSuperset(UnsafeSpan<EcsComponent> other)
	{
		var thisComps = new UnsafeSpan<EcsComponent>(ComponentInfo);

		while (thisComps.CanAdvance() && other.CanAdvance())
		{
			if (thisComps.Value.ID == other.Value.ID)
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
		var currents = new UnsafeSpan<EcsComponent>(ComponentInfo);

		while (currents.CanAdvance() && searching.CanAdvance())
		{
			if (currents.Value.ID == searching.Value.ID)
			{
				if (searching.Value.Op != TermOp.With)
				{
					return -1;
				}

				searching.Advance();
			}
			else if (currents.Value.ID > searching.Value.ID && searching.Value.Op != TermOp.With)
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
		ref var cmp = ref World.Component<T>();
		var span = GetComponentRaw(ref cmp, 0, _count);
		ref var start = ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));
		ref var end = ref Unsafe.Add(ref start, _count);

		return new UnsafeSpan<T>(ref start, ref end);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Has<T>() where T : unmanaged
	{
		ref var cmp = ref World.Component<T>();
		var column = GetComponentIndex(ref cmp);
		return column >= 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public EntityView Entity(int row)
		=> new (World, Entities[row].Entity);


	public void Print()
	{
		PrintRec(this, 0, 0);

		static void PrintRec(Archetype root, int depth, EntityID rootComponent)
		{
			Console.WriteLine("{0}[{1}] |{2}|", new string('.', depth), string.Join(", ", root.ComponentInfo.Select(s => s.ID)), rootComponent);

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

struct ArchetypeEntity
{
	public EntityID Entity;
	public int TableRow;
}

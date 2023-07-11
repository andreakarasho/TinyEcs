using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Buffers;

using EntityID = System.UInt64;

namespace TinyEcs;

public sealed partial class World : IDisposable
{
	private static readonly object _lock = new();
	internal static readonly EntitySparseSet<World> _allWorlds = new();

	private readonly EntityID _worldID;

	internal readonly Archetype _archRoot;
	internal readonly Dictionary<EntityID, Archetype> _typeIndex = new();
	internal readonly EntitySparseSet<EcsRecord> _entities = new();
	private readonly EntitySparseSet<EcsComponent> _components = new();


	public World()
	{
		_archRoot = new Archetype(this, ReadOnlySpan<EntityID>.Empty);

		lock (_lock)
			_allWorlds.CreateNew(out _worldID) = this;
	}


	public EntityID ID => _worldID;

	public int EntityCount => _entities.Length;


	public void Dispose()
	{
		_entities.Clear();
		_typeIndex.Clear();
		_components.Clear();
		_archRoot.Clear();

		lock (_lock)
			_allWorlds.Remove(_worldID);
	}

	public QueryBuilder Query()
	{
		var query = Spawn()
			.Set<EcsQuery>();

		return new QueryBuilder(_worldID, query);
	}

	public unsafe SystemBuilder System(delegate* managed<Commands, ref EntityIterator, void> system)
		=> new SystemBuilder(_worldID,
			Spawn()
				.Set(new EcsSystem(system))
				.Set(new EcsSystemTick() { Value = 0 })
				.Set<QueryBuilder>());

	public EntityView Spawn()
	{
		var e = CreateEntityRaw();

		return e
			.Set(e)
			.Set<EcsEnabled>();
	}


	public ref EcsComponent Component<T>() where T : unmanaged
		=> ref AddOrGetComponent((EntityID) ComponentStorage.TypeInfo<T>.GlobalIndex, ComponentStorage.TypeInfo<T>.Size);

	public ref EcsComponent Component(EntityID id, int size = 1)
		=> ref AddOrGetComponent(id, size);

	private ref EcsComponent AddOrGetComponent(EntityID id, int size)
	{
		ref var meta = ref _components.Get(id);
		if (Unsafe.IsNullRef(ref meta))
		{
			meta = ref _components.Add(id, new EcsComponent(id, size, (int)id));

			var cmp = CreateEntityRaw();
			cmp.Set(cmp)
				.Set(meta)
				.Set<EcsEnabled>();
		}

		return ref meta;
	}

	internal EntityView CreateEntityRaw(EntityID id = 0)
	{
		ref var record = ref (id > 0 ? ref _entities.Add(id, default!) : ref _entities.CreateNew(out id));
		record.Archetype = _archRoot;
		record.Row = _archRoot.Add(id);

		return new EntityView(_worldID, id);
	}

	public void Despawn(EntityID entity)
	{
		RemoveChildren(entity);
		Detach(entity);

		ref var record = ref _entities.Get(entity);
		Debug.Assert(!Unsafe.IsNullRef(ref record));

		var removedId = record.Archetype.Remove(record.Row);
		Debug.Assert(removedId == entity);

		var last = record.Archetype.Entities[record.Row];
		_entities.Get(last) = record;
		_entities.Remove(removedId);
	}

	public bool IsAlive(EntityID entity)
		=> _entities.Contains(entity);

	private void AttachComponent(EntityID entity, ref EcsComponent meta)
	{
		ref var record = ref _entities.Get(entity);
		Debug.Assert(!Unsafe.IsNullRef(ref record));

		var column = record.Archetype.GetComponentIndex(ref meta);
		if (column >= 0)
		{
			return;
		}

		InternalAttachDetach(ref record, ref meta, true);
	}

	internal void DetachComponent(EntityID entity, ref EcsComponent meta)
	{
		ref var record = ref _entities.Get(entity);
		Debug.Assert(!Unsafe.IsNullRef(ref record));

		var column = record.Archetype.GetComponentIndex(ref meta);
		if (column < 0)
		{
			return;
		}

		InternalAttachDetach(ref record, ref meta, false);
	}

	private void InternalAttachDetach(ref EcsRecord record, ref EcsComponent meta, bool add)
	{
		var initType = record.Archetype.Components;
		var cmpCount = Math.Max(0, initType.Length + (add ? 1 : -1));
		Span<EntityID> span = stackalloc EntityID[cmpCount];

		if (!add)
		{
			for (int i = 0, j = 0; i < initType.Length; ++i)
			{
				if (initType[i] != meta.ID)
				{
					span[j++] = initType[i];
				}
			}
		}
		else if (!span.IsEmpty)
		{
			initType.CopyTo(span);
			span[^1] = meta.ID;
		}

		span.Sort();

		var hash = ComponentHasher.Calculate(span);

#if NET5_0_OR_GREATER
		ref var arch = ref CollectionsMarshal.GetValueRefOrAddDefault(_typeIndex, hash, out var exists);
		if (!exists)
		{
			arch = _archRoot.InsertVertex(record.Archetype, span, ref meta);
		}
#else
        if (!_typeIndex.TryGetValue(hash, out var arch))
        {
			arch = _archRoot.InsertVertex(record.Archetype, span, ref meta);
            _typeIndex[hash] = arch;
		}
#endif

		var newRow = Archetype.MoveEntity(record.Archetype, arch!, record.Row);
		record.Row = newRow;
		record.Archetype = arch!;
	}

	internal void SetComponentData(EntityID entity, ref EcsComponent meta, ReadOnlySpan<byte> data)
	{
		ref var record = ref _entities.Get(entity);
		Debug.Assert(!Unsafe.IsNullRef(ref record));

		var buf = record.Archetype.GetComponentRaw(ref meta, record.Row, 1);
		if (buf.IsEmpty)
		{
			AttachComponent(entity, ref meta);
			buf = record.Archetype.GetComponentRaw(ref meta, record.Row, 1);
		}

		Debug.Assert(data.Length == buf.Length);
		data.CopyTo(buf);
	}

	private bool Has(EntityID entity, ref EcsComponent meta)
		=> !Get(entity, ref meta).IsEmpty;

	private Span<byte> Get(EntityID entity, ref EcsComponent meta)
	{
		ref var record = ref _entities.Get(entity);
		Debug.Assert(!Unsafe.IsNullRef(ref record));

		return record.Archetype.GetComponentRaw(ref meta, record.Row, 1);
	}
}

static unsafe class ComponentStorage
{
	public static class TypeInfo<T> where T : unmanaged
	{
		public static readonly int GlobalIndex = NextID.Get();
		public static readonly int Size = sizeof(T);
	}

	private static class NextID
	{
		private static int _next = 0;
		public static int Get() => ++_next;
	}
}

sealed unsafe class Archetype
{
	const int ARCHETYPE_INITIAL_CAPACITY = 16;

	private readonly World _world;
	private int _capacity, _count;
	private EntityID[] _entityIDs;
	internal byte[][] _componentsData;
	internal List<EcsEdge> _edgesLeft, _edgesRight;
	private readonly int[] _lookup;
	private readonly EntityID[] _components;

	public Archetype(World world, ReadOnlySpan<EntityID> components)
	{
		_world = world;
		_capacity = ARCHETYPE_INITIAL_CAPACITY;
		_count = 0;
		_components = components.ToArray();
		_entityIDs = new EntityID[ARCHETYPE_INITIAL_CAPACITY];
		_componentsData = new byte[components.Length][];
		_edgesLeft = new List<EcsEdge>();
		_edgesRight = new List<EcsEdge>();


		var maxID = -1;
		for (int i = 0; i < components.Length; i++)
		{
			ref var meta = ref _world.Component(components[i]);
			maxID = Math.Max(meta.GlobalIndex, maxID);
		}

		_lookup = new int[maxID + 1];
		_lookup.AsSpan().Fill(-1);
		for (int i = 0; i < components.Length; ++i)
		{
			ref var meta = ref _world.Component(components[i]);
			_lookup[meta.GlobalIndex] = i;
		}

		ResizeComponentArray(ARCHETYPE_INITIAL_CAPACITY);
	}


	public int Count => _count;
	public EntityID[] Entities => _entityIDs;
	public EntityID[] Components => _components;
	public World World => _world;



	public int GetComponentIndex(ref EcsComponent meta)
	{
		var index = meta.GlobalIndex;
		return index >= _lookup.Length ? -1 : _lookup[index];
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
			ref var meta = ref _world.Component(_components[i]);
			var leftArray = _componentsData[i].AsSpan();

			var removeComponent = leftArray.Slice(meta.Size * row, meta.Size);
			var swapComponent = leftArray.Slice(meta.Size * (_count - 1), meta.Size);

			swapComponent.CopyTo(removeComponent);
		}

		--_count;

		return removed;
	}

	public Archetype InsertVertex(Archetype left, ReadOnlySpan<EntityID> newType, ref EcsComponent meta)
	{
		var vertex = new Archetype(left._world, newType);
		MakeEdges(left, vertex, meta.ID);
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

	public Span<byte> GetComponentRaw(ref EcsComponent meta, int row, int count)
	{
		var column = GetComponentIndex(ref meta);
		if (column <= -1)
		{
			return Span<byte>.Empty;
		}

		Debug.Assert(row < Count);

		return _componentsData[column].AsSpan(meta.Size * row, meta.Size * count);
	}

	public void Clear() => _count = 0;

	static void Copy(Archetype from, int fromRow, Archetype to, int toRow)
	{
		var isLeft = to._components.Length < from._components.Length;
		int i = 0, j = 0;
		var count = isLeft ? to._components.Length : from._components.Length;
		var world = from._world;

		ref var x = ref (isLeft ? ref j : ref i);
		ref var y = ref (!isLeft ? ref j : ref i);

		for (; x < count; ++x, ++y)
		{
			while (from._components[i] != to._components[j])
			{
				// advance the sign with less components!
				++y;
			}

			ref var meta = ref world.Component(from._components[i]);
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
			ref var meta = ref _world.Component(_components[i]);
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

[SkipLocalsInit]
public readonly ref struct EntityIterator
{
	private readonly Archetype _archetype;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal EntityIterator([NotNull] Archetype archetype, float delta)
	{
		_archetype = archetype;
		Count = archetype.Count;
		DeltaTime = delta;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal EntityIterator([NotNull] Archetype archetype, int count, float delta)
	{
		_archetype = archetype;
		Count = count;
		DeltaTime = delta;
	}

	public readonly int Count;
	public readonly float DeltaTime;


	internal readonly World World => _archetype.World;
	internal readonly Archetype Archetype => _archetype;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly unsafe Span<T> Field<T>() where T : unmanaged
	{
		ref var meta = ref World.Component<T>();

		ref var value = ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(_archetype.GetComponentRaw(ref meta, 0, Count)));

		Debug.Assert(!Unsafe.IsNullRef(ref value));

		return MemoryMarshal.CreateSpan(ref value, Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly unsafe bool Has<T>() where T : unmanaged
	{
		ref var meta = ref World.Component<T>();
		var data = _archetype.GetComponentRaw(ref meta, 0, Count);
		if (data.IsEmpty)
			return false;

		ref var value = ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(data));

		return !Unsafe.IsNullRef(ref value);
	}
}


struct EcsEdge
{
	public EntityID ComponentID;
	public Archetype Archetype;
}

struct EcsRecord
{
	public Archetype Archetype;
	public int Row;
}

static class ComponentHasher
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EntityID Calculate(Span<EntityID> components)
	{
		unchecked
		{
			EntityID hash = 5381;

			foreach (ref readonly var id in components)
			{
				hash = ((hash << 5) + hash) + id;
			}

			return hash;
		}
	}
}



static class IDOp
{
	public static void Toggle(ref EntityID id)
	{
		id ^= EcsConst.ECS_TOGGLE;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EntityID GetGeneration(EntityID id)
	{
		return ((id & EcsConst.ECS_GENERATION_MASK) >> 32);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EntityID IncreaseGeneration(EntityID id)
	{
		return ((id & ~EcsConst.ECS_GENERATION_MASK) | ((0xFFFF & (GetGeneration(id) + 1)) << 32));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EntityID RealID(EntityID id)
	{
		return id &= EcsConst.ECS_ENTITY_MASK;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool HasFlag(EntityID id, byte flag)
	{
		return (id & flag) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsComponent(EntityID id)
	{
		return (id & EcsConst.ECS_COMPONENT_MASK) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EntityID SetAsComponent(EntityID id)
	{
		return id |= EcsConst.ECS_ID_FLAGS_MASK;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EntityID Pair(EntityID first, EntityID second)
	{
		return EcsConst.ECS_PAIR | ((first << 32) + (uint)second);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPair(EntityID id)
	{
		return ((id) & EcsConst.ECS_ID_FLAGS_MASK) == EcsConst.ECS_PAIR;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EntityID GetPairFirst(EntityID id)
	{
		return (uint)((id & EcsConst.ECS_COMPONENT_MASK) >> 32);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static EntityID GetPairSecond(EntityID id)
	{
		return (uint)id;
	}
}

public sealed partial class World
{
	public unsafe void Set<T>(EntityID entity, T component = default) where T : unmanaged
	{
		ref var meta = ref Component<T>();
		SetComponentData(entity, ref meta, MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref component), meta.Size));
	}

	public void Unset<T>(EntityID entity) where T : unmanaged
	   => DetachComponent(entity, ref Component<T>());

	public bool Has<T>(EntityID entity) where T : unmanaged
		=> Has(entity, ref Component<T>());

	public unsafe ref T Get<T>(EntityID entity) where T : unmanaged
	{
		var raw = Get(entity, ref Component<T>());

		Debug.Assert(!raw.IsEmpty);
		Debug.Assert(sizeof(T) == raw.Length);

		return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(raw));
	}

	public void AttachTo(EntityID childID, EntityID parentID)
	{
		Detach(childID);

		if (Has<EcsParent>(parentID))
		{
			ref var parent = ref Get<EcsParent>(parentID);
			parent.ChildrenCount += 1;

			ref var firstChild = ref Get<EcsChild>(parent.FirstChild);
			firstChild.Prev = childID;

			Set(childID, new EcsChild()
			{
				Parent = parentID,
				Prev = 0,
				Next = parent.FirstChild
			});

			parent.FirstChild = childID;

			return;
		}

		Set(parentID, new EcsParent()
		{
			ChildrenCount = 1,
			FirstChild = childID
		});

		Set(childID, new EcsChild()
		{
			Parent = parentID,
			Prev = 0,
			Next = 0
		});
	}

	public void Detach(EntityID id)
	{
		if (!Has<EcsChild>(id))
			return;

		ref var child = ref Get<EcsChild>(id);
		ref var parent = ref Get<EcsParent>(child.Parent);

		parent.ChildrenCount -= 1;

		if (parent.ChildrenCount <= 0)
		{
			Unset<EcsParent>(child.Parent);
		}
		else
		{
			if (parent.FirstChild == id)
			{
				parent.FirstChild = child.Next;
				child.Prev = 0;
			}
			else
			{
				if (child.Prev != 0)
				{
					Get<EcsChild>(child.Prev).Next = child.Next;
				}

				if (child.Next != 0)
				{
					Get<EcsChild>(child.Next).Prev = child.Prev;
				}
			}

		}

		Unset<EcsChild>(id);
	}

	public void RemoveChildren(EntityID id)
	{
		if (!Has<EcsParent>(id))
			return;

		ref var parent = ref Get<EcsParent>(id);

		while (parent.ChildrenCount > 0)
		{
			Detach(parent.FirstChild);
		}
	}
}



class Vec<T0> where T0 : unmanaged
{
	private T0[] _array;
	private int _count;

	public Vec(int initialSize = 2)
	{
		_array = new T0[initialSize];
		_count = 0;
	}

	public int Count => _count;
	public ref T0 this[int index] => ref _array[index];
	public ReadOnlySpan<T0> Span => _count <= 0 ? ReadOnlySpan<T0>.Empty : MemoryMarshal.CreateReadOnlySpan(ref _array[0], _count);

	public void Add(in T0 elem)
	{
		GrowIfNecessary(_count + 1);

		this[_count] = elem;

		++_count;
	}

	public void Clear()
	{
		_count = 0;
	}

	public void Sort() => Array.Sort(_array, 0, _count);

	public int IndexOf(T0 item) => Array.IndexOf(_array, item, 0, _count);

	private void GrowIfNecessary(int length)
	{
		if (length >= _array.Length)
		{
			var newLength = _array.Length > 0 ? _array.Length * 2 : 2;
			while (length >= newLength)
				newLength *= 2;
			Array.Resize(ref _array, newLength);
		}
	}
}

sealed class EntitySparseSet<T>
{
	private struct Chunk
	{
		public int[] Sparse;
		public T[] Values;
	}

	const int CHUNK_SIZE = 4096;

	private Chunk[] _chunks;
	private int _count;
	private EntityID _maxID;
	private Vec<EntityID> _dense;

	public EntitySparseSet()
	{
		_dense = new Vec<EntityID>();
		_chunks = Array.Empty<EntitySparseSet<T>.Chunk>();
		_count = 1;
		_maxID = EntityID.MinValue;

		_dense.Add(0);
	}

	public int Length => _count - 1;



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref T CreateNew(out EntityID id)
	{
		var count = _count++;
		var denseCount = _dense.Count;

		Debug.Assert(count <= denseCount);

		if (count < denseCount)
		{
			id = _dense[count];
		}
		else
		{
			id = NewID(count);
		}


		ref var chunk = ref GetChunk((int)id >> 12);

		if (Unsafe.IsNullRef(ref chunk) || chunk.Sparse == null)
			return ref Unsafe.NullRef<T>();

		return ref chunk.Values[(int)id & 0xFFF];
	}

	private EntityID NewID(int dense)
	{
		var index = ++_maxID;
		_dense.Add(0);

		ref var chunk = ref GetChunkOrCreate((int)index >> 12);
		Debug.Assert(chunk.Sparse[(int)index & 0xFFF] == 0);

		SparseAssignIndex(ref chunk, index, dense);

		return index;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref T Get(EntityID outerIdx)
	{
		ref var chunk = ref GetChunk((int)outerIdx >> 12);
		if (Unsafe.IsNullRef(ref chunk) || chunk.Sparse == null)
			return ref Unsafe.NullRef<T>();

		var gen = SplitGeneration(ref outerIdx);
		var realID = (int)outerIdx & 0xFFF;

		var dense = chunk.Sparse[realID];
		if (dense == 0 || dense >= _count)
			return ref Unsafe.NullRef<T>();

		var curGen = _dense[dense] & EcsConst.ECS_GENERATION_MASK;
		if (gen != curGen)
			return ref Unsafe.NullRef<T>();

		return ref chunk.Values[realID];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(EntityID outerIdx)
		=> !Unsafe.IsNullRef(ref Get(outerIdx));

	public ref T Add(EntityID outerIdx, T value)
	{
		var gen = SplitGeneration(ref outerIdx);
		var realID = (int)outerIdx & 0xFFF;
		ref var chunk = ref GetChunkOrCreate((int)outerIdx >> 12);
		var dense = chunk.Sparse[realID];

		if (dense != 0)
		{
			var count = _count;
			if (dense == count)
			{
				_count++;
			}
			else if (dense > count)
			{
				SwapDense(ref chunk, dense, count);
				dense = count;
				_count++;
			}

			Debug.Assert(gen == 0 || _dense[dense] == (outerIdx | gen));
		}
		else
		{
			_dense.Add(0);

			var denseCount = _dense.Count - 1;
			var count = _count++;

			if (outerIdx >= _maxID)
			{
				_maxID = outerIdx;
			}

			if (count < denseCount)
			{
				var unused = _dense[count];
				ref var unusedChunk = ref GetChunkOrCreate((int)unused >> 12);
				SparseAssignIndex(ref unusedChunk, unused, denseCount);
			}

			SparseAssignIndex(ref chunk, outerIdx, count);
			_dense[count] |= gen;
		}

		chunk.Values[realID] = value;
		return ref chunk.Values[realID];
	}

	public void Remove(EntityID outerIdx)
	{
		ref var chunk = ref GetChunk((int)outerIdx >> 12);
		if (Unsafe.IsNullRef(ref chunk) || chunk.Sparse == null)
			return;

		var gen = SplitGeneration(ref outerIdx);
		var realID = (int)outerIdx & 0xFFF;
		var dense = chunk.Sparse[realID];

		if (dense == 0)
			return;

		var curGen = _dense[dense] & EcsConst.ECS_GENERATION_MASK;
		if (gen != curGen)
		{
			return;
		}

		_dense[dense] = outerIdx | IDOp.IncreaseGeneration(curGen);

		var count = _count;
		if (dense == (count - 1))
		{
			_count--;
		}
		else if (dense < count)
		{
			SwapDense(ref chunk, dense, count - 1);
			_count--;
		}
		else
		{
			return;
		}

		chunk.Values[realID] = default!;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Clear()
	{
		_count = 1;
		_maxID = uint.MinValue;
		Array.Clear(_chunks, 0, _chunks.Length);
		_dense.Clear();
		_dense.Add(0);
	}

	private void SwapDense(ref Chunk chunkA, int a, int b)
	{
		Debug.Assert(a != b);

		var idxA = _dense[a];
		var idxB = _dense[b];

		ref var chunkB = ref GetChunkOrCreate((int)idxB >> 12);
		SparseAssignIndex(ref chunkA, idxA, b);
		SparseAssignIndex(ref chunkB, idxB, a);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SparseAssignIndex(ref Chunk chunk, EntityID index, int dense)
	{
		chunk.Sparse[(int)index & 0xFFF] = dense;
		_dense[dense] = index;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static EntityID SplitGeneration(ref EntityID index)
	{
		//if ((index & EcsConst.ECS_ID_FLAGS_MASK) != 0)
		//{
		//    index &= ~(EcsConst.ECS_GENERATION_MASK | EcsConst.ECS_ID_FLAGS_MASK);
		//    //return 0;
		//}

		var gen = index & EcsConst.ECS_GENERATION_MASK;
		Debug.Assert(gen == (index & (0xFFFF_FFFFul << 32)));
		index -= gen;
		return gen;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref Chunk GetChunk(int index)
		=> ref (index >= _chunks.Length ? ref Unsafe.NullRef<Chunk>() : ref _chunks[index]);

	private ref Chunk GetChunkOrCreate(int index)
	{
		if (index >= _chunks.Length)
		{
			var oldLength = _chunks.Length;
			var newLength = oldLength > 0 ? oldLength << 1 : 2;
			while (index >= newLength)
				newLength <<= 1;

			Array.Resize(ref _chunks, newLength);
		}

		ref var chunk = ref _chunks[index];

		if (chunk.Sparse == null)
		{
			chunk.Sparse = new int[CHUNK_SIZE];
			chunk.Values = new T[CHUNK_SIZE];
		}

		return ref chunk;
	}

	public unsafe SparseSetEnumerator GetEnumerator()
	{
		return new SparseSetEnumerator(this);
	}

	internal ref struct SparseSetEnumerator
	{
		private readonly EntitySparseSet<T> _sparseSet;
		private int _index;

		internal SparseSetEnumerator(EntitySparseSet<T> sparseSet)
		{
			_sparseSet = sparseSet;
			_index = 0;
		}

		public bool MoveNext() => ++_index < _sparseSet._count;

		public readonly T Current => _sparseSet._chunks[_sparseSet._dense[_index] >> 12]
										.Values[_sparseSet._dense[_index] & 0xFFF];
	}
}


static class EcsConst
{
	public const EntityID ECS_ENTITY_MASK = 0xFFFFFFFFul;
	public const EntityID ECS_GENERATION_MASK = (0xFFFFul << 32);
	public const EntityID ECS_ID_FLAGS_MASK = (0xFFul << 60);
	public const EntityID ECS_COMPONENT_MASK = ~ECS_ID_FLAGS_MASK;

	public const EntityID ECS_TOGGLE = 1ul << 61;
	public const EntityID ECS_PAIR = 1ul << 63;
}

public readonly struct EcsComponent
{
	public readonly EntityID ID;
	public readonly int Size;
	public readonly int GlobalIndex;

	public EcsComponent(EntityID id, int size, int globalIndex)
	{
		ID = id;
		Size = size;
		GlobalIndex = globalIndex;
	}
}

public unsafe struct EcsQuery
{
}

public struct EcsQueryParameter<T> where T : unmanaged
{
	public EntityID Component;
}

public unsafe readonly struct EcsSystem
{
	public readonly delegate* managed<Commands, ref EntityIterator, void> Func;

	public EcsSystem(delegate* managed<Commands, ref EntityIterator, void> func)
	{
		Func = func;
	}
}

internal struct EcsSystemTick
{
	public float Value;
	public float Current;
}

public readonly struct EcsRelation<TAction, TTarget>
	where TAction : unmanaged
	where TTarget : unmanaged
{
	public readonly TTarget Target;

	public EcsRelation() => Target = default;
	public EcsRelation(in TTarget target) => Target = target;
}

public struct EcsParent
{
	public int ChildrenCount;
	public EntityID FirstChild;
}

public struct EcsChild
{
	public EntityID Parent;
	public EntityID Prev, Next;
}

public readonly struct EcsEnabled { }

public struct EcsSystemPhaseOnUpdate { }
public struct EcsSystemPhasePreUpdate { }
public struct EcsSystemPhasePostUpdate { }
public struct EcsSystemPhaseOnStartup { }
public struct EcsSystemPhasePreStartup { }
public struct EcsSystemPhasePostStartup { }

public enum SystemPhase
{
	OnUpdate,
	OnPreUpdate,
	OnPostUpdate,

	OnStartup,
	OnPreStartup,
	OnPostStartup
}

public readonly struct SystemBuilder : IEquatable<EntityID>, IEquatable<SystemBuilder>
{
	public readonly EntityID ID;
	internal readonly EntityID WorldID;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal SystemBuilder(EntityID world, EntityID id)
	{
		WorldID = world;
		ID = id;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Equals(ulong other)
	{
		return ID == other;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Equals(SystemBuilder other)
	{
		return ID == other.ID;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal readonly SystemBuilder Set<T>(T component = default) where T : unmanaged
	{
		World._allWorlds.Get(WorldID).Set(ID, component);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly SystemBuilder SetQuery(in QueryBuilder query)
	{
		var world = World._allWorlds.Get(WorldID);
		world.Set(ID, query);

		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly SystemBuilder SetTick(float tick)
	{
		var world = World._allWorlds.Get(WorldID);
		world.Set(ID, new EcsSystemTick() { Value = tick });

		return this;
	}
}

public readonly struct QueryBuilder : IEquatable<EntityID>, IEquatable<QueryBuilder>
{
	internal const EntityID FLAG_WITH = (0x01ul << 60);
	internal const EntityID FLAG_WITHOUT = (0x02ul << 60);


	public readonly EntityID ID;
	internal readonly EntityID WorldID;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal QueryBuilder(EntityID world, EntityID id)
	{
		WorldID = world;
		ID = id;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Equals(ulong other)
	{
		return ID == other;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Equals(QueryBuilder other)
	{
		return ID == other.ID;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryBuilder With<T>() where T : unmanaged
	{
		var world = World._allWorlds.Get(WorldID);
		ref var meta = ref world.Component<T>();
		world.Set(ID, new EcsQueryParameter<T>() { Component = meta.ID | FLAG_WITH });

		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryBuilder Without<T>() where T : unmanaged
	{
		var world = World._allWorlds.Get(WorldID);
		ref var meta = ref world.Component<T>();
		world.Set(ID, new EcsQueryParameter<T>() { Component = meta.ID | FLAG_WITHOUT });

		return this;
	}

	public QueryIterator GetEnumerator()
	{
		var world = World._allWorlds.Get(WorldID);

		ref var record = ref world._entities.Get(ID);
		Debug.Assert(!Unsafe.IsNullRef(ref record));

		var components = record.Archetype.Components;
		var cmps = ArrayPool<EntityID>.Shared.Rent(components.Length);

		var withIdx = 0;
		var withoutIdx = components.Length;

		//cmps[withoutIdx] = ComponentStorage.GetOrAdd<EcsQuery>(world).ID;

		for (int i = 0; i < components.Length; ++i)
		{
			ref var meta = ref world.Component(components[i]);
			Debug.Assert(!Unsafe.IsNullRef(ref meta));

			var cmp = Unsafe.As<byte, EntityID>(ref MemoryMarshal.GetReference(record.Archetype.GetComponentRaw(ref meta, record.Row, 1)));

			if ((cmp & QueryBuilder.FLAG_WITH) != 0)
			{
				cmps[withIdx++] = cmp & ~QueryBuilder.FLAG_WITH;
			}
			else if ((cmp & QueryBuilder.FLAG_WITHOUT) != 0)
			{
				cmps[--withoutIdx] = cmp & ~QueryBuilder.FLAG_WITHOUT;
			}
		}

		var with = cmps.AsSpan(0, withIdx);
		var without = cmps.AsSpan(0, components.Length).Slice(withoutIdx);

		with.Sort();
		without.Sort();

		if (with.IsEmpty)
		{
			return default;
		}
		var stack = new Stack<Archetype>();
		stack.Push(world._archRoot);

		return new QueryIterator(stack, cmps, with, without);
	}

	internal static unsafe Archetype FetchArchetype
	(
		Stack<Archetype> stack,
		ReadOnlySpan<EntityID> with,
		ReadOnlySpan<EntityID> without
	)
	{
		if (stack.Count == 0 || !stack.TryPop(out var archetype) || archetype == null)
		{
			return null;
		}

		var span = CollectionsMarshal.AsSpan(archetype._edgesRight);
		if (!span.IsEmpty)
		{
			ref var last = ref span[^1];

			for (int i = 0; i < span.Length; ++i)
			{
				ref var edge = ref Unsafe.Subtract(ref last, i);

				if (without.IndexOf(edge.ComponentID) < 0)
				{
					stack.Push(edge.Archetype);
				}
			}
		}

		if (archetype.Count > 0 && archetype.IsSuperset(with))
		{
			// query ok, call the system now
			return archetype;
		}

		return null;
	}
}


#if NET5_0_OR_GREATER
[SkipLocalsInit]
#endif
[StructLayout(LayoutKind.Sequential)]
public readonly struct EntityView : IEquatable<EntityID>, IEquatable<EntityView>
{
	public readonly EntityID ID;
	public readonly EntityID WorldID;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal EntityView(EntityID world, EntityID id)
	{
		WorldID = world;
		ID = id;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Equals(ulong other)
	{
		return ID == other;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Equals(EntityView other)
	{
		return ID == other.ID;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Set<T>(T component = default) where T : unmanaged
	{
		World._allWorlds.Get(WorldID).Set(ID, component);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Unset<T>() where T : unmanaged
	{
		World._allWorlds.Get(WorldID).Unset<T>(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Set<TPredicate, TTarget>()
		where TPredicate : unmanaged
		where TTarget : unmanaged
	{
		World._allWorlds.Get(WorldID).Set<EcsRelation<TPredicate, TTarget>>(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Unset<TPredicate, TTarget>()
		where TPredicate : unmanaged
		where TTarget : unmanaged
	{
		World._allWorlds.Get(WorldID).Unset<EcsRelation<TPredicate, TTarget>>(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Set<TPredicate>(in EntityView targetID)
		where TPredicate : unmanaged
	{
		var world = World._allWorlds.Get(WorldID);
		world.Set(ID, new EcsRelation<TPredicate, EntityView>(in targetID));

		return this;
	}

	public readonly EntityView AttachTo(EntityView parent)
	{
		World._allWorlds.Get(WorldID).AttachTo(ID, parent);
		return this;
	}

	public readonly EntityView Detach()
	{
		World._allWorlds.Get(WorldID).Detach(ID);
		return this;
	}

	public readonly EntityView RemoveChildren()
	{
		World._allWorlds.Get(WorldID).RemoveChildren(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Enable()
	{
		World._allWorlds.Get(WorldID).Set<EcsEnabled>(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Disable()
	{
		World._allWorlds.Get(WorldID).Unset<EcsEnabled>(ID);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref T Get<T>() where T : unmanaged
		=> ref World._allWorlds.Get(WorldID).Get<T>(ID);

	//[MethodImpl(MethodImplOptions.AggressiveInlining)]
	//public readonly ref T Get<T>(EntityID id) where T : unmanaged
	//{
	//	var world = World._allWorlds.Get(WorldID);

	//	ref var meta = ref world._components.Get(id);

		
	//}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Has<T>() where T : unmanaged
		=> World._allWorlds.Get(WorldID).Has<T>(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref EcsRelation<TPredicate, TTarget> Get<TPredicate, TTarget>()
		where TPredicate : unmanaged where TTarget : unmanaged
		=> ref Get<EcsRelation<TPredicate, TTarget>>();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Has<TPredicate, TTarget>()
		where TPredicate : unmanaged where TTarget : unmanaged
		=> Has<EcsRelation<TPredicate, TTarget>>();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Destroy()
		=> World._allWorlds.Get(WorldID).Despawn(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool IsAlive()
		=> World._allWorlds.Get(WorldID).IsAlive(ID);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool IsEnabled()
		=> Has<EcsEnabled>();


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator EntityID(in EntityView d) => d.ID;


	public static readonly EntityView Invalid = new(0, 0);
}


public unsafe ref struct QueryIterator
{
	private Archetype _current;
	private readonly ReadOnlySpan<EntityID> _with, _without;
	private readonly EntityID[] _buffer;
	private readonly Stack<Archetype> _stack;

	internal QueryIterator(Stack<Archetype> stack, EntityID[] buffer, ReadOnlySpan<EntityID> with, ReadOnlySpan<EntityID> without)
	{
		_stack = stack;
		_current = stack.Peek();
		_with = with;
		_without = without;
		_buffer = buffer;
	}

	public bool MoveNext()
	{
		do
		{
			_current = QueryBuilder.FetchArchetype(_stack, _with, _without);	
		} while (_stack.Count > 0 && _current == null);
		
		return _current != null;
	}

	public readonly EntityIterator Current => new (_current, 0f);

	public readonly void Dispose()
	{
		if (_buffer != null)
			ArrayPool<EntityID>.Shared.Return(_buffer);
	}
}


public static class QueryEx
{
	public static unsafe void Fetch(this in QueryBuilder query, Commands cmds, delegate* <Commands, ref EntityIterator, void> system, float deltaTime)
	{
		var world = World._allWorlds.Get(query.WorldID);

		ref var record = ref world._entities.Get(query.ID);
		Debug.Assert(!Unsafe.IsNullRef(ref record));

		var components = record.Archetype.Components;
		Span<EntityID> cmps = stackalloc EntityID[components.Length + 0];

		var withIdx = 0;
		var withoutIdx = components.Length;

		//cmps[withoutIdx] = ComponentStorage.GetOrAdd<EcsQuery>(world).ID;

		for (int i = 0; i < components.Length; ++i)
		{
			ref var meta = ref world.Component(components[i]);
			Debug.Assert(!Unsafe.IsNullRef(ref meta));

			var cmp = Unsafe.As<byte, EntityID>(ref MemoryMarshal.GetReference(record.Archetype.GetComponentRaw(ref meta, record.Row, 1)));

			if ((cmp & QueryBuilder.FLAG_WITH) != 0)
			{
				cmps[withIdx++] = cmp & ~QueryBuilder.FLAG_WITH;
			}
			else if ((cmp & QueryBuilder.FLAG_WITHOUT) != 0)
			{
				cmps[--withoutIdx] = cmp & ~QueryBuilder.FLAG_WITHOUT;
			}
		}

		var with = cmps.Slice(0, withIdx);
		var without = cmps.Slice(withoutIdx);

		with.Sort();
		without.Sort();

		if (!with.IsEmpty)
		{
			FetchArchetype(world._archRoot, with, without, cmds, system, deltaTime);
		}
	}

	private static unsafe void FetchArchetype
	(
		Archetype archetype,
		ReadOnlySpan<EntityID> with,
		ReadOnlySpan<EntityID> without,
		Commands cmds,
		delegate* managed<Commands, ref EntityIterator, void> system,
		float deltaTime
	)
	{
		if (archetype == null)
		{
			return;
		}

		if (archetype.Count > 0 && archetype.IsSuperset(with))
		{
			// query ok, call the system now
			var it = new EntityIterator(archetype, deltaTime);
			system(cmds, ref it);
		}

		var span = CollectionsMarshal.AsSpan(archetype._edgesRight);
		if (!span.IsEmpty)
		{
			ref var last = ref span[^1];

			for (int i = 0; i < span.Length; ++i)
			{
				ref var edge = ref Unsafe.Subtract(ref last, i);

				if (without.IndexOf(edge.ComponentID) < 0)
					FetchArchetype(edge.Archetype, with, without, cmds, system, deltaTime);
			}
		}
	}
}

public unsafe sealed class Commands : IDisposable
{
	private readonly World _main, _mergeWorld;
	private readonly QueryBuilder _entityCreated, _entityDestroyed, _componentSet, _componentUnset, _toBeDestroyed;


	public Commands(World main)
	{
		_main = main;
		_mergeWorld = new World();

		_entityCreated = _mergeWorld.Query()
			.With<EntityCreated>();

		_entityDestroyed = _mergeWorld.Query()
			.With<EntityDestroyed>();

		_componentSet = _mergeWorld.Query()
			.With<ComponentAdded>();

		_componentUnset = _mergeWorld.Query()
			.With<ComponentRemoved>();

		_toBeDestroyed = _mergeWorld.Query()
			.With<MarkDestroy>();
	}

	public World Main => _main;


	public void Merge()
	{
		// we pass the Commands, but must not be used to edit entities!
		_entityCreated.Fetch(this, &EntityCreatedSystem, 0f);
		_entityDestroyed.Fetch(this, &EntityDestroyedSystem, 0f);
		_componentSet.Fetch(this, &ComponentSetSystem, 0f);
		_componentUnset.Fetch(this, &ComponentUnsetSystem, 0f);
		_toBeDestroyed.Fetch(this, &MarkDestroySystem, 0f);
	}


	static void EntityCreatedSystem(Commands cmds, ref EntityIterator it)
	{
		var archetype = it.Archetype;
		var main = cmds.Main;
		var merge = it.World;

		ref var created = ref merge.Component<EntityCreated>();
		ref var destroyed = ref merge.Component<EntityDestroyed>();
		ref var componentAdded = ref merge.Component<ComponentAdded>();
		ref var componentRemoved = ref merge.Component<ComponentRemoved>();
		ref var markDestroy = ref merge.Component<MarkDestroy>();

		var opA = it.Field<EntityCreated>();

		for (int i = 0; i < it.Count; ++i)
		{
			var target = main.CreateEntityRaw();

			foreach (var cmp in archetype.Components)
			{
				// TODO: find a better way to find unecessary components
				//       maybe apply a flag to the component ID?
				if (cmp == created.ID || cmp == destroyed.ID ||
					cmp == componentAdded.ID || cmp == componentRemoved.ID ||
					cmp == markDestroy.ID)
					continue;

				ref var meta = ref main.Component(cmp, merge.Component(cmp).Size);
				main.SetComponentData(target, ref meta, archetype.GetComponentRaw(ref meta, i, 1));
			}

			main.Set(target, new EntityView(main.ID, target));
			main.Set<EcsEnabled>(target);
		}
	}

	static void EntityDestroyedSystem(Commands cmds, ref EntityIterator it)
	{
		var main = cmds.Main;

		var opA = it.Field<EntityDestroyed>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var op = ref opA[i];

			main.Despawn(op.Target);
		}
	}

	static void ComponentSetSystem(Commands cmds, ref EntityIterator it)
	{
		var main = cmds.Main;
		var merge = it.World;

		var opA = it.Field<ComponentAdded>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var op = ref opA[i];

			ref var meta = ref main.Component(op.Component, merge.Component(op.Component).Size);
			main.SetComponentData(op.Target, ref meta, it.Archetype.GetComponentRaw(ref meta, i, 1));
		}
	}

	static void ComponentUnsetSystem(Commands cmds, ref EntityIterator it)
	{
		var main = cmds.Main;
		var merge = it.World;

		var opA = it.Field<ComponentRemoved>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var op = ref opA[i];

			ref var meta = ref main.Component(op.Component, merge.Component(op.Component).Size);
			main.DetachComponent(op.Target, ref meta);
		}
	}

	static void MarkDestroySystem(Commands cmds, ref EntityIterator it)
	{
		var entA = it.Field<EntityView>();

		for (int i = 0; i < it.Count; ++i)
		{
			ref var entity = ref entA[i];
			cmds._mergeWorld.Despawn(entity);
		}
	}


	public EntityView Entity(EntityID id)
	{
		if (_main.IsAlive(id))
		{
			if (!_mergeWorld.IsAlive(id))
			{
				var newId = _mergeWorld.Spawn();

				id = newId.ID;
			}
		}

		if (_mergeWorld.IsAlive(id))
			return new EntityView(_mergeWorld.ID, id);

		return EntityView.Invalid;
	}

	public EntityView Spawn()
	{
		return _mergeWorld.Spawn()
			.Set<MarkDestroy>()
			.Set(new EntityCreated()
			{
				Target = 0
			});
	}

	public void Despawn(EntityID entity)
	{
		Debug.Assert(_main.IsAlive(entity) || _mergeWorld.IsAlive(entity));

		_mergeWorld.Spawn()
			.Set<MarkDestroy>()
			.Set(new EntityDestroyed()
			{
				Target = entity
			});
	}

	public void Set<T>(EntityID entity, T cmp = default) where T : unmanaged
	{
		Debug.Assert(_main.IsAlive(entity) || _mergeWorld.IsAlive(entity));

		_mergeWorld.Spawn()
			.Set<MarkDestroy>()
			.Set(new ComponentAdded()
			{
				Target = entity,
				Component = _mergeWorld.Component<T>().ID
			})
			.Set(cmp);
	}

	public void Unset<T>(EntityID entity) where T : unmanaged
	{
		Debug.Assert(_main.IsAlive(entity) || _mergeWorld.IsAlive(entity));

		_mergeWorld.Spawn()
			.Set<MarkDestroy>()
			.Set(new ComponentRemoved()
			{
				Target = entity,
				Component = _mergeWorld.Component<T>().ID
			});
	}

	public void AttachTo(EntityView entity, EntityView parent)
	{
		Debug.Assert(entity.WorldID == _main.ID);
		Debug.Assert(parent.WorldID == _main.ID);

		_mergeWorld.Spawn()
			.Set<MarkDestroy>()
			.Set(new ChildAdded()
			{
				Target = entity,
				Parent = parent
			});
	}

	public void Dispose()
	{
		_mergeWorld?.Dispose();
	}



	struct EntityCreated
	{
		public EntityID Target;
	}

	struct EntityDestroyed
	{
		public EntityID Target;
	}

	struct ComponentAdded
	{
		public EntityID Target;
		public EntityID Component;
	}

	struct ComponentRemoved
	{
		public EntityID Target;
		public EntityID Component;
	}

	struct ChildAdded
	{
		public EntityID Target;
		public EntityID Parent;
	}

	struct MarkDestroy
	{
	}
}


sealed unsafe class Ecs
{
	private readonly World _world;
	private readonly Commands _cmds;

	private readonly QueryBuilder
		_querySystemUpdate,
		_querySystemPreUpdate,
		_querySystemPostUpdate;

	private readonly QueryBuilder
		_querySystemStartup,
		_querySystemPreStartup,
		_querySystemPostStartup;

	private ulong _frame;


	public Ecs()
	{
		_world = new World();
		_cmds = new Commands(_world);

		_querySystemUpdate = Query()
			.With<EcsEnabled>()
			.With<EcsSystem>()
			.With<EcsSystemPhaseOnUpdate>();

		_querySystemPreUpdate = Query()
			.With<EcsEnabled>()
			.With<EcsSystem>()
			.With<EcsSystemPhasePreUpdate>();

		_querySystemPostUpdate = Query()
			.With<EcsEnabled>()
			.With<EcsSystem>()
			.With<EcsSystemPhasePostUpdate>();


		_querySystemStartup = Query()
			.With<EcsEnabled>()
			.With<EcsSystem>()
			.With<EcsSystemPhaseOnStartup>();

		_querySystemPreStartup = Query()
			.With<EcsEnabled>()
			.With<EcsSystem>()
			.With<EcsSystemPhasePreStartup>();

		_querySystemPostStartup = Query()
			.With<EcsEnabled>()
			.With<EcsSystem>()
			.With<EcsSystemPhasePostStartup>();
	}

	public EntityView Spawn()
		=> _cmds.Spawn();

	public void Despawn(EntityID entity)
		=> _cmds.Despawn(entity);

	public void Set<T>(EntityID entity, T value = default) where T : unmanaged
		=> _cmds.Set(entity, value);

	public void Unset<T>(EntityID entity) where T : unmanaged
		=> _cmds.Unset<T>(entity);

	public void SetSingleton<T>(T cmp = default) where T : unmanaged
		=> _world.Set(_world.Component<T>().ID, cmp);

	public ref T GetSingleton<T>() where T : unmanaged
		=> ref _world.Get<T>(_world.Component<T>().ID);


	public QueryBuilder Query()
		=> _world.Query();

	public unsafe SystemBuilder AddStartupSystem(delegate* managed<Commands, ref EntityIterator, void> system)
		=> _world.System(system)
			.Set<EcsSystemPhaseOnStartup>();

	public unsafe SystemBuilder AddSystem(delegate* managed<Commands, ref EntityIterator, void> system)
		=> _world.System(system)
			.Set<EcsSystemPhaseOnUpdate>();

	public unsafe void Step(float delta)
	{
		_cmds.Merge();

		if (_frame == 0)
		{
			_querySystemPreStartup.Fetch(_cmds, &RunSystems, delta);
			_querySystemStartup.Fetch(_cmds, &RunSystems, delta);
			_querySystemPostStartup.Fetch(_cmds, &RunSystems, delta);
		}

		_querySystemPreUpdate.Fetch(_cmds, &RunSystems, delta);
		_querySystemUpdate.Fetch(_cmds, &RunSystems, delta);
		_querySystemPostUpdate.Fetch(_cmds, &RunSystems, delta);

		_cmds.Merge();
		_frame += 1;
	}

	static unsafe void RunSystems(Commands cmds, ref EntityIterator it)
	{
		var sysA = it.Field<EcsSystem>();
		var sysTickA = it.Field<EcsSystemTick>();
		var queryA = it.Field<QueryBuilder>();

		var emptyIt = new EntityIterator(it.World._archRoot, 0, it.DeltaTime);

		for (int i = 0; i < it.Count; ++i)
		{
			ref var sys = ref sysA[i];
			ref var query = ref queryA[i];
			ref var tick = ref sysTickA[i];

			if (tick.Value > 0.00f)
			{
				// TODO: check for it.DeltaTime > 0?
				tick.Current += it.DeltaTime;

				if (tick.Current < tick.Value)
				{
					continue;
				}

				tick.Current = 0;
			}

			if (query.ID != 0)
			{
				query.Fetch(cmds, sys.Func, it.DeltaTime);
			}
			else
			{
				sys.Func(cmds, ref emptyIt);
			}
		}

	}
}


#if NETSTANDARD2_1
internal readonly ref struct Ref<T>
{
    private readonly Span<T> span;

    public Ref(ref T value)
    {
        span = MemoryMarshal.CreateSpan(ref value, 1);
    }

    public ref T Value => ref MemoryMarshal.GetReference(span);
}

public static class SortExtensions
{
	public static void Sort<T>(this Span<T> span) where T : IComparable<T>
	{
		for (int i = 0; i < span.Length - 1; i++)
		{
			for (int j = 0; j < span.Length - i - 1; j++)
			{
				if (span[j].CompareTo(span[j + 1]) > 0)
				{
					// Swap the elements
					T temp = span[j];
					span[j] = span[j + 1];
					span[j + 1] = temp;
				}
			}
		}
	}
}
#endif
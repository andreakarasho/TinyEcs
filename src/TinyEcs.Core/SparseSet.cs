namespace TinyEcs;

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
	private readonly Vec<EntityID> _dense;

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

		EcsAssert.Assert(count <= denseCount);

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
		EcsAssert.Assert(chunk.Sparse[(int)index & 0xFFF] == 0);

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

			EcsAssert.Assert(gen == 0 || _dense[dense] == (outerIdx | gen));
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
		_maxID = uint.MinValue;
		for (int i = 0; i < _chunks.Length; ++i)
		{
			ref var chunk = ref _chunks[i];
			if (chunk.Sparse != null)
				Array.Clear(chunk.Sparse, 0, chunk.Sparse.Length);
		}

		_dense.Clear();
		_dense.Add(0);

		_count = 1;
	}

	private void SwapDense(ref Chunk chunkA, int a, int b)
	{
		EcsAssert.Assert(a != b);

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
		if (IDOp.IsPair(index))
		{
			index &= ~EcsConst.ECS_ID_FLAGS_MASK;
			//index &= ~(EcsConst.ECS_GENERATION_MASK /*| EcsConst.ECS_ID_FLAGS_MASK*/);
		}

		var gen = index & EcsConst.ECS_GENERATION_MASK;
		EcsAssert.Assert(gen == (index & (0xFFFF_FFFFul << 32)));
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

		public readonly ref T Current => ref _sparseSet._chunks[_sparseSet._dense[_index] >> 12]
										.Values[_sparseSet._dense[_index] & 0xFFF];
	}
}



sealed class Vec<T0> where T0 : unmanaged
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

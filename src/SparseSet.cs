namespace TinyEcs;

internal sealed class EntitySparseSet<T>
{
	private struct Chunk
	{
		public int[]? Sparse;
		public T[] Values;
	}

	const int CHUNK_SIZE = 4096;

	private Chunk[] _chunks;
	private int _count;
	private ulong _maxID;
	private readonly Vec<ulong> _dense;

	public EntitySparseSet()
	{
		_dense = new Vec<ulong>();
		_chunks = Array.Empty<Chunk>();
		_count = 1;
		_maxID = ulong.MinValue;

		_dense.Add(0);
	}

	public int Length => _count - 1;

	public ref ulong MaxID => ref _maxID;


	public ref T CreateNew(out ulong id)
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

	private ulong NewID(int dense)
	{
		var index = ++_maxID;
		_dense.Add(0);

		ref var chunk = ref GetChunkOrCreate((int)index >> 12);
		EcsAssert.Assert(chunk.Sparse![(int)index & 0xFFF] == 0);

		SparseAssignIndex(ref chunk, index, dense);

		return index;
	}

	public ref T Get(ulong outerIdx)
	{
		ref var chunk = ref GetChunk((int)outerIdx >> 12);
		if (Unsafe.IsNullRef(ref chunk) || chunk.Sparse == null)
			return ref Unsafe.NullRef<T>();

		var realID = (int)outerIdx & 0xFFF;
		var dense = chunk.Sparse[realID];
		if (dense == 0 || dense >= _count)
			return ref Unsafe.NullRef<T>();

		var gen = SplitGeneration(ref outerIdx);
		var curGen = _dense[dense] & EcsConst.ECS_GENERATION_MASK;
		if (gen != curGen)
			return ref Unsafe.NullRef<T>();

		return ref chunk.Values[realID];
	}

	public ulong GetNoGeneration(ulong outerIdx)
	{
		ref var chunk = ref GetChunk((int)outerIdx >> 12);
		if (Unsafe.IsNullRef(ref chunk) || chunk.Sparse == null)
			return 0;

		var realID = (int)outerIdx & 0xFFF;
		var dense = chunk.Sparse[realID];
		if (dense == 0 || dense >= _count)
			return 0;

		return _dense[dense];
	}

	public bool Contains(ulong outerIdx)
	{
		ref var chunk = ref GetChunkOrCreate((int)outerIdx >> 12);
		if (Unsafe.IsNullRef(ref chunk) || chunk.Sparse == null)
			return false;

		var realID = (int)outerIdx & 0xFFF;
		var dense = chunk.Sparse[realID];
		if (dense == 0 || dense >= _count)
			return false;

		var gen = SplitGeneration(ref outerIdx);
		var curGen = _dense[dense] & EcsConst.ECS_GENERATION_MASK;
		if (gen != curGen)
			return false;

		EcsAssert.Assert(dense == chunk.Sparse[realID]);

		return true;
	}

	public ref T Add(ulong outerIdx, T value)
	{
		var gen = SplitGeneration(ref outerIdx);
		var realID = (int)outerIdx & 0xFFF;
		ref var chunk = ref GetChunkOrCreate((int)outerIdx >> 12);
		var dense = chunk.Sparse![realID];

		if (dense != 0)
		{
			var count = _count;
			if (dense >= count)
			{
				SwapDense(ref chunk, dense, count);
				dense = count;
				_count += 1;
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

	public void Remove(ulong outerIdx)
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

	public void Clear()
	{
		if (_count <= 1)
			return;

		_maxID = uint.MinValue;

		for (int i = 0; i < _chunks.Length; ++i)
		{
			ref var chunk = ref _chunks[i];
			chunk = ref Unsafe.NullRef<Chunk>();
		}

		_chunks = Array.Empty<EntitySparseSet<T>.Chunk>();
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
	private void SparseAssignIndex(ref Chunk chunk, ulong index, int dense)
	{
		chunk.Sparse![(int)index & 0xFFF] = dense;
		_dense[dense] = index;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong SplitGeneration(ref ulong index)
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

	public ref struct SparseSetEnumerator
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



[SkipLocalsInit]
internal sealed class Vec<T> where T : unmanaged
{
	private const int DefaultCapacity = 4;

	private T[] _data;


	public Vec(int capacity = 0)
	{
		Capacity = Math.Max(capacity, DefaultCapacity);
		_data = new T[Capacity];
		Count = 0;
	}

	public int Capacity { get; private set; }
	public int Count { get; private set; }
	public Span<T> Span => _data.AsSpan(0, Count);
	public ref T this[int i] => ref _data[i];


	public void Clear()
	{
		Capacity = DefaultCapacity;
		Count = 0;
		Array.Resize(ref _data, Capacity);
	}

	public void Add(T item)
		=> AddRef() = item;

	public ref T AddRef()
	{
		if (Count >= Capacity)
			EnsureCapacity(Capacity * 2);

		return ref _data[Count++];
	}

	public void EnsureCapacity(int newCapacity)
	{
		if (newCapacity <= Capacity)
			return;

		Array.Resize(ref _data, newCapacity);
		Capacity = newCapacity;
	}
}

using System.Runtime.CompilerServices;


namespace TinyEcs;

sealed class SparseSet<T>
{
    private struct Chunk
    {
        public int[] Sparse;
        public SimpleVector<T> Values;
    }

    const int CHUNK_SIZE = 4096;
    const int TOLERANCE = -1;

    private Chunk[] _chunks;
    public SimpleVector<int> _dense;
    private int _count;


    public SparseSet(int initialCapacity = 0)
    {
        _dense = new SimpleVector<int>(initialCapacity);
        _chunks = new Chunk[initialCapacity];
    }


    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

    public ref T this[int i]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ref var chunk = ref GetChunk(i);
            ref var idx = ref chunk.Sparse[i % CHUNK_SIZE];
            return ref (idx > TOLERANCE ? ref chunk.Values[idx] : ref Unsafe.NullRef<T>());
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(int outerIdx)
    {
        var chunkIdx = outerIdx >> 12;
        if (chunkIdx >= _chunks.Length)
            return false;

        ref var chunk = ref GetChunk(outerIdx);
        if (Unsafe.IsNullRef(ref chunk))
            return false;

        return chunk.Sparse[outerIdx % CHUNK_SIZE] > TOLERANCE;
    }

    public ref T Add(int outerIdx, T value)
    {
        ref var chunk = ref GetChunkOrCreate(outerIdx);
        chunk.Sparse[outerIdx % CHUNK_SIZE] = chunk.Values.Length;
        chunk.Values.Add(value);

        _dense.Add(outerIdx);
        ++_count;

        return ref chunk.Values[chunk.Sparse[outerIdx % CHUNK_SIZE]];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(int outerIdx)
    {
        ref var chunk = ref GetChunk(outerIdx);
        if (Unsafe.IsNullRef(ref chunk))
            return;

        var innerIndex = chunk.Sparse[outerIdx % CHUNK_SIZE];
        chunk.Sparse[outerIdx % CHUNK_SIZE] = TOLERANCE;
        chunk.Values.Remove(innerIndex);
        _dense.Remove(innerIndex);

        --_count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        Array.Clear(_chunks);
        _count = 0;
        _dense.Clear();
    }

    public void Copy(in SparseSet<T> other)
    {
//        if (_sparse.Length < other._sparse.Length)
//            Array.Resize(ref _sparse, other._sparse.Length);
//        else if (_sparse.Length > other._sparse.Length)
//        {
//#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER || NET5_0_OR_GREATER
//            Array.Fill(_sparse, -1, other._sparse.Length, _sparse.Length - other._sparse.Length);
//#else
//                for (int i = other._sparse.Length; i < _sparse.Length; i++)
//                    _sparse[i] = -1;
//#endif
//        }
//        Array.Copy(other._sparse, _sparse, other._sparse.Length);

//        _values.Copy(other._values);
//        _dense.Copy(other._dense);
    }

    private ref Chunk GetChunk(int index)
    {
        var chunkIndex = index >> 12;

        if (chunkIndex >= _chunks.Length)
        {
            return ref Unsafe.NullRef<Chunk>();
        }

        ref var chunk = ref _chunks[chunkIndex];
        if (chunk.Sparse == null)
        {
            chunk.Sparse = new int[CHUNK_SIZE];
            chunk.Values = new SimpleVector<T>(CHUNK_SIZE);

            if (TOLERANCE != 0)
                Array.Fill(chunk.Sparse, TOLERANCE);
        }    

        return ref chunk;
    }

    private ref Chunk GetChunkOrCreate(int index)
    {
        var chunkIndex = index >> 12;

        if (chunkIndex >= _chunks.Length)
        {
            var oldLength = _chunks.Length;
            var newLength = oldLength > 0 ? oldLength << 1 : 2;
            while (chunkIndex >= newLength)
                newLength <<= 1;

            Array.Resize(ref _chunks, newLength);
        }

        return ref GetChunk(index);
    }
}

sealed class SimpleVector<T>
{
    public T[] _elements;
    public int _end = 0;

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _end;
    }

    public int Reserved
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _elements.Length;
    }

    public ref T this[int i]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _elements[i];
    }

    public SimpleVector(int reserved = 0)
    {
        _elements = new T[reserved];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Copy(in SimpleVector<T> other)
    {
        _end = other._end;
        if (_elements.Length < _end)
            Array.Resize(ref _elements, other._elements.Length);
        Array.Copy(other._elements, _elements, _end);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(int idx)
    {
        _end--;
        if (idx < _end)
            _elements[idx] = _elements[_end];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _end = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T element)
    {
        if (_end >= _elements.Length)
        {
            var newLength = _elements.Length > 0 ? _elements.Length * 2 : 2;
            while (_end >= newLength)
                newLength *= 2;
            Array.Resize(ref _elements, newLength);
        }
        _elements[_end] = element;
        _end++;
    }
}

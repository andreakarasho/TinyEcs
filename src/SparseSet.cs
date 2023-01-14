using System.Runtime.CompilerServices;


namespace TinyEcs;

sealed class SparseSet
{
    const int CHUNK_SIZE = 4096;

    private Chunk[] _chunks = new Chunk[32];
    private int _count;

    public int Count => _count;

    public void Add(int value)
    {
        var chunkIndex = value >> 12;

        ref var chunk = ref GetChunk(chunkIndex);
        chunk.Sparse[value % CHUNK_SIZE] = chunk.Count;
        chunk.Dense[chunk.Count] = value;
        ++chunk.Count;

        ++_count;
    }

    public bool Has(int value)
    {
        var chunkIndex = value >> 12;

        ref var chunk = ref GetChunk(chunkIndex);
        var index = chunk.Sparse[value % CHUNK_SIZE];
        return index != -1 && chunk.Dense[index] == value;
    }

    private ref Chunk GetChunk(int index)
    {
        GrowIfNecessary(index);

        ref var chunk = ref _chunks[index];
        chunk.Sparse ??= new int[CHUNK_SIZE];
        chunk.Dense ??= new int[CHUNK_SIZE];

        return ref chunk;
    }

    private void GrowIfNecessary(int chunkIndex)
    {
        if (chunkIndex >= _chunks.Length)
        {
            var newSize = GetPow2(chunkIndex + 1);
            Array.Resize(ref _chunks, newSize);
        }
    }

    struct Chunk
    {
        public int[] Sparse, Dense;
        public int Count;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetPow2(int v)
    {
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v++;

        return v;
    }
}

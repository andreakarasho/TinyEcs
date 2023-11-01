using TinyEcs;

sealed unsafe class Table
{
    const int ARCHETYPE_INITIAL_CAPACITY = 16;

    private readonly ComponentComparer _comparer;
    private readonly void*[] _componentsData;
    private int _capacity;
    private int _count;

    internal Table(ulong hash, ReadOnlySpan<EcsComponent> components, ComponentComparer comparer)
    {
        Hash = hash;
        _comparer = comparer;
        _capacity = ARCHETYPE_INITIAL_CAPACITY;
        _count = 0;

        int valid = 0;
        foreach (ref readonly var cmp in components)
        {
            if (cmp.Size > 0)
                ++valid;
        }

        _componentsData = new void*[valid];
        Components = new EcsComponent[valid];

        valid = 0;
        foreach (ref readonly var cmp in components)
        {
            if (cmp.Size <= 0)
                continue;

            Components[valid++] = cmp;
        }

        ResizeComponentArray(_capacity);
    }

    public ulong Hash { get; }
    public int Rows => _count;
    public int Columns => Components.Length;
    public readonly EcsComponent[] Components;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* GetData(int column, int row, int size) =>
        (byte*)_componentsData[column] + row * size;

    internal int Add(EcsID id)
    {
        if (_capacity == _count)
        {
            _capacity *= 2;

            ResizeComponentArray(_capacity);
        }

        return _count++;
    }

    internal int GetComponentIndex(ref EcsComponent cmp)
    {
        return Array.BinarySearch(Components, cmp, _comparer);
    }

    internal int GetComponentIndex(ulong cmp)
    {
        return BinarySearch(Components, cmp);
    }

    private static int BinarySearch(EcsComponent[] array, ulong target)
    {
        int left = 0;
        int right = array.Length - 1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;

            if (array[mid].ID == target)
            {
                return mid; // Target found
            }
            else if (array[mid].ID < target)
            {
                left = mid + 1; // Target is in the right half
            }
            else
            {
                right = mid - 1; // Target is in the left half
            }
        }

        return -1; // Target not found
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Span<T> ComponentData<T>(int column, int row, int count) where T : unmanaged
    {
        EcsAssert.Assert(column >= 0 && column < _componentsData.Length);
        return new Span<T>((T*)_componentsData[column] + row, count);
    }

    internal void Remove(int row)
    {
        for (int i = 0; i < Components.Length; ++i)
        {
            ref readonly var meta = ref Components[i];

            var leftArray = ComponentData<byte>(i, 0, meta.Size * _capacity);

            var removeComponent = leftArray.Slice(meta.Size * row, meta.Size);
            var swapComponent = leftArray.Slice(meta.Size * (_count - 1), meta.Size);

            swapComponent.CopyTo(removeComponent);
        }

        --_count;
    }

    internal void MoveTo(int fromRow, Table to, int toRow)
    {
        var isLeft = to.Components.Length < Components.Length;
        int i = 0,
            j = 0;
        var count = isLeft ? to.Components.Length : Components.Length;

        ref var x = ref (isLeft ? ref j : ref i);
        ref var y = ref (!isLeft ? ref j : ref i);

        var fromCount = _count - 1;

        for (; x < count; ++x, ++y)
        {
            while (Components[i].ID != to.Components[j].ID)
            {
                // advance the sign with less components!
                ++y;
            }

            ref readonly var meta = ref Components[i];

            var leftArray = ComponentData<byte>(i, 0, meta.Size * _capacity);
            var rightArray = to.ComponentData<byte>(j, 0, meta.Size * to._capacity);

            var insertComponent = rightArray.Slice(meta.Size * toRow, meta.Size);
            var removeComponent = leftArray.Slice(meta.Size * fromRow, meta.Size);
            var swapComponent = leftArray.Slice(meta.Size * fromCount, meta.Size);
            removeComponent.CopyTo(insertComponent);
            swapComponent.CopyTo(removeComponent);
        }

        _count = fromCount;
    }

    private void ResizeComponentArray(int capacity)
    {
        for (int i = 0; i < Components.Length; ++i)
        {
            ref readonly var meta = ref Components[i];

            _componentsData[i] = (byte*)
                NativeMemory.Realloc(_componentsData[i], (nuint)capacity * (nuint)meta.Size);

            _capacity = capacity;
        }
    }
}

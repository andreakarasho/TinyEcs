using TinyEcs;

sealed class Table
{
	const int ARCHETYPE_INITIAL_CAPACITY = 16;

	private readonly byte[][] _componentsData;
	private readonly EcsComponent[] _componentInfo;
	private int _capacity;
	private int _count;


	internal Table(ulong hash, ReadOnlySpan<EcsComponent> components)
	{
		Hash = hash;
		_capacity = ARCHETYPE_INITIAL_CAPACITY;
		_count = 0;

		int valid = 0;
		foreach (ref readonly var cmp in components)
		{
			if (cmp.Size > 0)
				++valid;
		}

		_componentsData = new byte[valid][];
		_componentInfo = new EcsComponent[valid];

		valid = 0;
		foreach (ref readonly var cmp in components)
		{
			if (cmp.Size <= 0)
				continue;

			_componentInfo[valid++] = cmp;
		}

		ResizeComponentArray(_capacity);
	}


	public ulong Hash { get; }
	public int Rows => _count;
	public int Columns => _componentInfo.Length;
	public EcsComponent[] Components => _componentInfo;


	internal int Add(EntityID id)
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
		return Array.BinarySearch(_componentInfo, cmp);
	}

	internal Span<byte> GetComponentRaw(ref EcsComponent component, int row, int count)
	{
		var column = GetComponentIndex(ref component);
		if (column < 0)
		{
			return Span<byte>.Empty;
		}

		return GetComponentRaw(column, row, count, component.Size);
	}

	internal Span<byte> GetComponentRaw(int column, int row, int count, int size)
	{
		return _componentsData[column].AsSpan(size * row, size * count);
	}

	internal void Remove(int row)
	{
		for (int i = 0; i < _componentInfo.Length; ++i)
		{
			ref readonly var meta = ref _componentInfo[i];
			var leftArray = _componentsData[i].AsSpan();

			var removeComponent = leftArray.Slice(meta.Size * row, meta.Size);
			var swapComponent = leftArray.Slice(meta.Size * (_count - 1), meta.Size);

			swapComponent.CopyTo(removeComponent);
		}

		--_count;
	}

	internal void MoveTo(int fromRow, Table to, int toRow)
	{
		var isLeft = to._componentInfo.Length < _componentInfo.Length;
		int i = 0, j = 0;
		var count = isLeft ? to._componentInfo.Length : _componentInfo.Length;

		ref var x = ref (isLeft ? ref j : ref i);
		ref var y = ref (!isLeft ? ref j : ref i);

		var fromCount = _count - 1;

		for (; x < count; ++x, ++y)
		{
			while (_componentInfo[i].ID != to._componentInfo[j].ID)
			{
				// advance the sign with less components!
				++y;
			}

			ref readonly var meta = ref _componentInfo[i];

			var leftArray = _componentsData[i].AsSpan();
			var rightArray = to._componentsData[j].AsSpan();
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
		for (int i = 0; i < _componentInfo.Length; ++i)
		{
			ref readonly var meta = ref _componentInfo[i];
			Array.Resize(ref _componentsData[i], meta.Size * capacity);
			_capacity = capacity;
		}
	}
}

using TinyEcs;

sealed class Table
{
	const int ARCHETYPE_INITIAL_CAPACITY = 16;

	private readonly byte[][] _componentsData;
	private readonly EcsComponent[] _componentInfo;
	private int _capacity;
	private int _count;
	private EntityID[] _entities;


	internal Table(ReadOnlySpan<EcsComponent> components)
	{
		_capacity = ARCHETYPE_INITIAL_CAPACITY;
		_entities = new EntityID[_capacity];
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

	public int Count => _count;
	public EntityID[] Entities => _entities;

	internal int Add(EntityID id)
	{
		if (_capacity == _count)
		{
			_capacity *= 2;

			ResizeComponentArray(_capacity);
			Array.Resize(ref _entities, _capacity);
		}

		_entities[_count] = id;

		return _count++;
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

		return _componentsData[column].AsSpan(component.Size * row, component.Size * count);
	}

	internal void Remove(int row)
	{
		var removed = _entities[row];
		_entities[row] = _entities[_count - 1];

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

	[SkipLocalsInit]
	internal void MoveTo(int fromRow, Table to, int toRow)
	{
		if (_count == 0)
			return;

		if (fromRow < _entities.Length)
		{
			var removed = _entities[fromRow];
			_entities[fromRow] = _entities[_count - 1];
		}

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

			// var uLeft = new UnsafeSpan<byte>(_componentsData[i]);
			// var uRight = new UnsafeSpan<byte>(to._componentsData[j]);

			// var toIndex = meta.Size * toRow;
			// var fromIndex = meta.Size * fromRow;
			// ref var left = ref Unsafe.Add(ref uLeft.Value, fromIndex);

			// // remove -> insert
			// Unsafe.CopyBlockUnaligned
			// (
			// 	ref Unsafe.Add(ref uRight.Value, toIndex),
			// 	ref left,
			// 	(uint) meta.Size
			// );

			// // swap -> remove
			// Unsafe.CopyBlockUnaligned
			// (
			// 	ref left,
			// 	ref Unsafe.Add(ref uLeft.Value, meta.Size * fromCount),
			// 	(uint) meta.Size
			// );
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

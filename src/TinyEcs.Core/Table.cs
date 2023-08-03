using TinyEcs;

sealed class Table
{
	const int ARCHETYPE_INITIAL_CAPACITY = 16;

	private readonly byte[][] _componentsData;
	private readonly EcsComponent[] _componentInfo;
	private readonly Dictionary<EntityID, int> _lookup = new();
	private int _capacity;
	private int _count;

	internal Table(ReadOnlySpan<EcsComponent> components)
	{
		_capacity = ARCHETYPE_INITIAL_CAPACITY;
		_count = 0;
		_componentsData = new byte[components.Length][];
		_componentInfo = components.ToArray();

		for (var i = 0; i < components.Length; i++)
		{
			EcsAssert.Assert(components[i].Size > 0);
			_lookup.Add(components[i].ID, i);
		}

		ResizeComponentArray(_capacity);
	}


	internal int Increase()
	{
		if (_capacity == _count)
		{
			_capacity *= 2;

			ResizeComponentArray(_capacity);
		}

		return _count++;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal int GetComponentIndex(EntityID component)
	{
		ref var idx = ref CollectionsMarshal.GetValueRefOrNullRef(_lookup, component);
		//ref var idx = ref _lookup.Get(component);

		return Unsafe.IsNullRef(ref idx) ? -1 : (int)idx;
	}

	internal Span<byte> GetComponentRaw(EntityID component, int row, int count)
	{
		var column = GetComponentIndex(component);
		if (column < 0)
		{
			// FIXME: this will be valid for empty custom components
			return Span<byte>.Empty;
		}

		ref readonly var meta = ref _componentInfo[column];

		return _componentsData[column].AsSpan(meta.Size * row, meta.Size * count);
	}

	internal void Remove(ref EcsRecord record)
	{
		for (int i = 0; i < _componentInfo.Length; ++i)
		{
			ref readonly var meta = ref _componentInfo[i];
			var leftArray = _componentsData[i].AsSpan();

			var removeComponent = leftArray.Slice(meta.Size * record.TableRow, meta.Size);
			var swapComponent = leftArray.Slice(meta.Size * (_count - 1), meta.Size);

			swapComponent.CopyTo(removeComponent);
		}

		--_count;
	}

	[SkipLocalsInit]
	internal int MoveTo(int fromRow, Table to)
	{
		var isLeft = to._componentInfo.Length < _componentInfo.Length;
		int i = 0, j = 0;
		var count = isLeft ? to._componentInfo.Length : _componentInfo.Length;

		ref var x = ref (isLeft ? ref j : ref i);
		ref var y = ref (!isLeft ? ref j : ref i);

		var fromCount = _count - 1;
		var toRow = to.Increase();

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
		return toRow;
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

using TinyEcs;

sealed class Table
{
	const int ARCHETYPE_INITIAL_CAPACITY = 16;

	private readonly byte[][] _componentsData;
	private readonly EntityID[] _components;
	private readonly Dictionary<EntityID, int> _lookup = new();
	private int _capacity;
	private int _count;

	public Table(ReadOnlySpan<EcsComponent> components)
	{
		_capacity = ARCHETYPE_INITIAL_CAPACITY;
		_count = 0;
		_componentsData = new byte[components.Length][];
		_components = new EntityID[components.Length];
		ComponentInfo = components.ToArray();

		for (var i = 0; i < components.Length; i++)
		{
			_components[i] = components[i].ID;
			_lookup.Add(components[i].ID, i);
		}

		ResizeComponentArray(_capacity);
	}


	public EntityID[] Components => _components;
	public readonly EcsComponent[] ComponentInfo;


	public int Increase()
	{
		if (_capacity == _count)
		{
			_capacity *= 2;

			ResizeComponentArray(_capacity);
		}

		return _count++;
	}

	public int Decrease()
	{

		return --_count;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetComponentIndex(EntityID component)
	{
		ref var idx = ref CollectionsMarshal.GetValueRefOrNullRef(_lookup, component);
		//ref var idx = ref _lookup.Get(component);

		return Unsafe.IsNullRef(ref idx) ? -1 : (int)idx;
	}

	public Span<byte> GetComponentRaw(EntityID component, int row, int count)
	{
		var column = GetComponentIndex(component);
		if (column < 0)
		{
			return Span<byte>.Empty;
		}

		//EcsAssert.Assert(row < Count); // this is not true when removing

		ref readonly var meta = ref ComponentInfo[column];

		return _componentsData[column].AsSpan(meta.Size * row, meta.Size * count);
	}

	public void Remove(ref EcsRecord record)
	{
		for (int i = 0; i < ComponentInfo.Length; ++i)
		{
			ref readonly var meta = ref ComponentInfo[i];
			var leftArray = _componentsData[i].AsSpan();

			var removeComponent = leftArray.Slice(meta.Size * record.TableRow, meta.Size);
			var swapComponent = leftArray.Slice(meta.Size * (_count - 1), meta.Size);

			swapComponent.CopyTo(removeComponent);
		}

		--_count;
	}

	[SkipLocalsInit]
	public void CopyTo(int fromRow, Table to, int toRow)
	{
		var isLeft = to.Components.Length < Components.Length;
		int i = 0, j = 0;
		var count = isLeft ? to.Components.Length : Components.Length;

		ref var x = ref (isLeft ? ref j : ref i);
		ref var y = ref (!isLeft ? ref j : ref i);

		ref var cmpFromStart = ref MemoryMarshal.GetArrayDataReference(ComponentInfo);
		var fromCount = _count - 1;

		for (; x < count; ++x, ++y)
		{
			while (Components[i] != to.Components[j])
			{
				// advance the sign with less components!
				++y;
			}

			ref var meta = ref Unsafe.Add(ref cmpFromStart, i);

			var leftArray = _componentsData[i].AsSpan();
			var rightArray = to._componentsData[j].AsSpan();
			var insertComponent = rightArray.Slice(meta.Size * toRow, meta.Size);
			var removeComponent = leftArray.Slice(meta.Size * fromRow, meta.Size);
			var swapComponent = leftArray.Slice(meta.Size * fromCount, meta.Size);
			removeComponent.CopyTo(insertComponent);
			swapComponent.CopyTo(removeComponent);

			// var uLeft = new UnsafeSpan<byte>(from._componentsData[i]);
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
	}

	private void ResizeComponentArray(int capacity)
	{
		for (int i = 0; i < _components.Length; ++i)
		{
			ref readonly var meta = ref ComponentInfo[i];
			Array.Resize(ref _componentsData[i], meta.Size * capacity);
			_capacity = capacity;
		}
	}
}

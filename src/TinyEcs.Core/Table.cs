// using TinyEcs;
//
// sealed unsafe class Table
// {
//     const int ARCHETYPE_INITIAL_CAPACITY = 16;
//
//     private readonly ComponentComparer _comparer;
//     private readonly Array[] _componentsData;
//     private int _capacity;
//     private int _count;
//
//     internal Table(ulong hash, ReadOnlySpan<EcsComponent> components, ComponentComparer comparer)
//     {
//         Hash = hash;
//         _comparer = comparer;
//         _capacity = ARCHETYPE_INITIAL_CAPACITY;
//         _count = 0;
//
//         int valid = 0;
//         foreach (ref readonly var cmp in components)
//         {
//             if (cmp.Size > 0)
//                 ++valid;
//         }
//
//         _componentsData = new Array[valid];
//         Components = new EcsComponent[valid];
//
//         valid = 0;
//         foreach (ref readonly var cmp in components)
//         {
//             if (cmp.Size <= 0)
//                 continue;
//
//             Components[valid++] = cmp;
//         }
//
//         ResizeComponentArray(_capacity);
//     }
//
//     public ulong Hash { get; }
//     public int Rows => _count;
//     public int Columns => Components.Length;
//     public readonly EcsComponent[] Components;
//
//
//     internal int Add(EcsID id)
//     {
//         if (_capacity == _count)
//         {
// 			_capacity <<= 3;
//
//             ResizeComponentArray(_capacity);
//         }
//
//         return _count++;
//     }
//
//     internal int GetComponentIndex(ref readonly EcsComponent cmp)
//     {
//         return Array.BinarySearch(Components, cmp, _comparer);
//     }
//
//     internal int GetComponentIndex(ulong cmp)
//     {
//         return BinarySearch(Components, cmp);
//
// 		static int BinarySearch(EcsComponent[] array, ulong target)
// 		{
// 			int left = 0;
// 			int right = array.Length - 1;
//
// 			while (left <= right)
// 			{
// 				int mid = left + (right - left) / 2;
//
// 				if (array[mid].ID == target)
// 				{
// 					return mid; // Target found
// 				}
// 				else if (array[mid].ID < target)
// 				{
// 					left = mid + 1; // Target is in the right half
// 				}
// 				else
// 				{
// 					right = mid - 1; // Target is in the left half
// 				}
// 			}
//
// 			return -1; // Target not found
// 		}
// 	}
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     internal Span<T> ComponentData<T>(int column, int row, int count) where T : struct
// 	{
//         EcsAssert.Assert(column >= 0 && column < _componentsData.Length);
//
// 		ref var array = ref Unsafe.As<Array, T[]>(ref _componentsData[column]);
// 		return array.AsSpan(row, count);
//     }
//
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	internal Array RawComponentData(int column)
// 	{
// 		EcsAssert.Assert(column >= 0 && column < _componentsData.Length);
//
// 		return _componentsData[column];
// 	}
//
//     internal void Remove(int row)
//     {
//         for (int i = 0; i < Components.Length; ++i)
//         {
// 			var leftArray = RawComponentData(i);
//
// 			var tmp = leftArray.GetValue(_count - 1);
// 			leftArray.SetValue(tmp, row);
//         }
//
//         --_count;
//     }
//
//     internal void MoveTo(int fromRow, Table to, int toRow)
//     {
//         var isLeft = to.Components.Length < Components.Length;
//         int i = 0,
//             j = 0;
//         var count = isLeft ? to.Components.Length : Components.Length;
//
//         ref var x = ref (isLeft ? ref j : ref i);
//         ref var y = ref (!isLeft ? ref j : ref i);
//
//         var fromCount = _count - 1;
//
//         for (; x < count; ++x, ++y)
//         {
//             while (Components[i].ID != to.Components[j].ID)
//             {
//                 // advance the sign with less components!
//                 ++y;
//             }
//
// 			var leftArray = RawComponentData(i);
// 			var rightArray = to.RawComponentData(j);
//
// 			var insertComponent = rightArray.GetValue(toRow);
// 			var removeComponent = leftArray.GetValue(fromRow);
// 			var swapComponent = leftArray.GetValue(fromCount);
//
// 			rightArray.SetValue(removeComponent, toRow);
// 			leftArray.SetValue(swapComponent, fromRow);
//         }
//
//         _count = fromCount;
//     }
//
//     private void ResizeComponentArray(int capacity)
//     {
//         for (int i = 0; i < Components.Length; ++i)
//         {
// 			var tmp = Lookup.GetArray(Components[i].ID, capacity);
// 			_componentsData[i]?.CopyTo(tmp!, 0);
// 			_componentsData[i] = tmp!;
//
// 			_capacity = capacity;
//         }
//     }
// }

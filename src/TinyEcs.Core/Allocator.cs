namespace TinyEcs
{
	public unsafe struct UnsafeMemory
	{
		public byte* Alloc;
		public void* Free;
		public int BlockSize;
		public int NumBlocks;
		public int Allocated;
	}


	// UnmanagedMemoryPool and stuff from --> https://www.jacksondunstan.com/articles/3770
	public static unsafe class UnsafeMemoryPool
	{
		public static readonly int SizeOfPointer = sizeof(void*);

		public static readonly int MinimumPoolBlockSize = SizeOfPointer;


		public static void Memset(void* ptr, byte value, int count)
		{
			count /= 8;
			if (count > 0)
				NativeMemory.Fill(ptr, (nuint)count, value);
		}

		public static nint Alloc(int size)
		{
			size = ((size + 7) & (-8));

			var ptr = NativeMemory.Alloc((nuint)size);

			return (nint)ptr;
		}

		public static nint Calloc(int size)
		{
			nint ptr = Alloc(size);

			Memset((void*)ptr, 0, size);

			return ptr;
		}

		public static void* Alloc(this ref UnsafeMemory pool)
		{
			void* pRet = pool.Free;

			pool.Free = *((byte**)pool.Free);

			pool.Allocated += 1;

			return pRet;
		}

		public static void* Calloc(this ref UnsafeMemory pool)
		{
			void* ptr = Alloc(ref pool);

			Memset(ptr, 0, pool.BlockSize);

			return ptr;
		}

		public static UnsafeMemory AllocPool(int blockSize, int numBlocks)
		{
			Debug.Assert(blockSize >= MinimumPoolBlockSize);
			Debug.Assert(numBlocks > 0);

			blockSize = ((blockSize + 7) & (-8));

			UnsafeMemory pool = new UnsafeMemory
			{
				Free = null,
				NumBlocks = numBlocks,
				BlockSize = blockSize,

				Alloc = (byte*)Alloc(blockSize * numBlocks),
				Allocated = 0
			};

			FreeAll(ref pool);

			return pool;
		}

		public static void Free(nint ptr)
		{
			if (ptr != nint.Zero)
			{
				NativeMemory.Free((void*)ptr);
			}
		}

		public static void Free(this ref UnsafeMemory pool, void* ptr)
		{
			if (ptr != null)
			{
				void** pHead = (void**)ptr;
				*pHead = pool.Free;
				pool.Free = pHead;
				pool.Allocated -= 1;
			}
		}

		public static void FreeAll(this ref UnsafeMemory pool)
		{
			void** pCur = (void**)pool.Alloc;
			byte* pNext = pool.Alloc + pool.BlockSize;

			for (int i = 0, count = pool.NumBlocks - 1; i < count; ++i)
			{
				*pCur = pNext;
				pCur = (void**)pNext;
				pNext += pool.BlockSize;
			}

			*pCur = default(void*);

			pool.Free = pool.Alloc;

			pool.Allocated = 0;
		}

		public static void FreePool(this ref UnsafeMemory pool)
		{
			Free((nint)pool.Alloc);
			pool.Alloc = null;
			pool.Free = null;
		}
	}
}

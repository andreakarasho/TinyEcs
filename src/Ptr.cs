namespace TinyEcs;

[SkipLocalsInit]
public unsafe struct Ptr<T> where T : struct
{
	public T* Pointer;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetRef(ref T p)
	{
		Pointer = (T*)Unsafe.AsPointer(ref p);
	}

	public ref T Ref
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref *Pointer;
	}
}

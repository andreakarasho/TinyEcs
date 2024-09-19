namespace TinyEcs;

public unsafe struct Ptr<T> where T : struct
{
	public T* Pointer;

	public ref T Ref
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref *Pointer;
	}
}

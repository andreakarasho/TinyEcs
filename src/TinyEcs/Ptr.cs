namespace TinyEcs;

[SkipLocalsInit]
public ref struct Ptr<T> where T : struct
{
	public ref T Ref;

	public readonly bool IsValid() => !Unsafe.IsNullRef(ref Ref);
}

[SkipLocalsInit]
public readonly ref struct PtrRO<T> where T : struct
{
	public PtrRO(ref readonly T r) => Ref = ref r;

	public readonly ref readonly T Ref;
}

[SkipLocalsInit]
internal ref struct DataRow<T> where T : struct
{
	public Ptr<T> Value;
	public nint Size;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Next()
	{
		Value.Ref = ref Unsafe.AddByteOffset(ref Value.Ref, Size);
	}
}

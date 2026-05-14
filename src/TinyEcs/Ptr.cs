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
public ref struct DataRow<T> where T : struct
{
	public ref T Base;
	public int Index;
	public nint Size;

	public Ptr<T> Value
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			Ptr<T> p = default;
			if (Size == 0)
				p.Ref = ref Unsafe.NullRef<T>();
			else
				p.Ref = ref Unsafe.Add(ref Base, Index);
			return p;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Next() => ++Index;
}

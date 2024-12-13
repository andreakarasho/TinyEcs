namespace TinyEcs;

[SkipLocalsInit]
public ref struct Ptr<T> where T : struct
{
	public ref T Ref;
}

[SkipLocalsInit]
public readonly ref struct PtrRO<T> where T : struct
{

	public PtrRO(ref T r) => Ref = ref r;

	public readonly ref T Ref;
}

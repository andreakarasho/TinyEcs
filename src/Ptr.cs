namespace TinyEcs;

[SkipLocalsInit]
public ref struct Ptr<T> where T : struct
{
	// internal ref ComponentState State;
	internal ref T Value;

	public readonly ref T Ref
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Value;
	}
	// public readonly ref T Rw
	// {
	// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	// 	get
	// 	{
	// 		if (State != ComponentState.Changed)
	// 			State = ComponentState.Changed;
	// 		return ref Value;
	// 	}
	// }

	// public readonly bool IsChanged => State == ComponentState.Changed;
	// public readonly bool IsAdded => State == ComponentState.Added;

	// public void ClearState() => State = ComponentState.None;
	// public void MarkChanged() => State = ComponentState.Changed;
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
	// public nint StateSize;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Next()
	{
		Value.Value = ref Unsafe.AddByteOffset(ref Value.Ref, Size);
		// Value.State = ref Unsafe.AddByteOffset(ref Value.State, StateSize);
	}
}

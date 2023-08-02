namespace TinyEcs;

[SkipLocalsInit]
public ref struct UnsafeSpan<T> where T : unmanaged
{
	private ref T _start;
	private readonly ref T _end;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public UnsafeSpan(ref T start, ref T end)
	{
		_start = ref start;
		_end = ref end;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public UnsafeSpan(Span<T> span)
	{
		_start = ref MemoryMarshal.GetReference(span);
		_end = ref Unsafe.Add(ref _start, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public UnsafeSpan(T[] span)
	{
		_start = ref MemoryMarshal.GetArrayDataReference(span);
		_end = ref Unsafe.Add(ref _start, span.Length);
	}


	public ref T Value => ref _start;
	public readonly ref T End => ref _end;


	public readonly ref T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.Add(ref _start, index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool CanAdvance() => Unsafe.IsAddressLessThan(ref _start, ref _end);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref T Advance() => ref _start = ref Unsafe.Add(ref _start, 1);


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator UnsafeSpan<T>(T[] span)
		=> new (span);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator UnsafeSpan<T>(Span<T> span)
		=> new (span);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe implicit operator Span<T>(UnsafeSpan<T> span)
	{
		var size = (T*)Unsafe.AsPointer(ref span.End) - (T*)Unsafe.AsPointer(ref span.Value);
		return MemoryMarshal.CreateSpan(ref span.Value, (int) size);
	}
}

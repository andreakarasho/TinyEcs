namespace TinyEcs;

public delegate void IteratorDelegate(ref Iterator it);

public readonly ref struct Iterator
{
	private readonly Archetype _archetype;

	internal Iterator(Commands? commands, Archetype archetype, object? userData)
	{
		Commands = commands;
		_archetype = archetype;
		UserData = userData;
	}

	public Commands? Commands { get; }
	public readonly World World => _archetype.World;
	public readonly int Count => _archetype.Count;
	public readonly float DeltaTime => World.DeltaTime;
	public object? UserData { get; }


	public readonly Span<T> Span<T>() where T : unmanaged
		=> _archetype.Field<T>();

	public readonly bool Has<T>() where T : unmanaged
		=> _archetype.Has<T>();

	public readonly EntityView Entity(int i)
		=> _archetype.Entity(i);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly UnsafeSpan<T> Field<T>() where T : unmanaged
	{
		var span = _archetype.Field<T>();
		ref var start = ref MemoryMarshal.GetReference(span);
		ref var end = ref Unsafe.Add(ref start, span.Length);

		return new UnsafeSpan<T>(ref start, ref end);
	}
}



[SkipLocalsInit]
public readonly ref struct UnsafeSpan<T> where T : unmanaged
{
	private readonly ref T _start;
	private readonly ref T _end;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public UnsafeSpan(ref T start, ref T end)
	{
		_start = ref start;
		_end = ref end;
	}

	public readonly ref T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.Add(ref _start, index);
	}

	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public readonly ref T Get(int i)
	// {
	// 	return ref Unsafe.Add(ref _reference, i);
	// }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Valid() => Unsafe.IsAddressLessThan(ref _start, ref _end);
}

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

	internal Archetype Archetype => _archetype;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<T> Span<T>() where T : unmanaged
		=> Field<T>();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly UnsafeSpan<T> Field<T>() where T : unmanaged
	{
		ref var cmp = ref World.Component<T>();
		var span = _archetype.GetComponentRaw(ref cmp, 0, Count);
		ref var start = ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));
		ref var end = ref Unsafe.Add(ref start, Count);

		return new UnsafeSpan<T>(ref start, ref end);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Has<T>() where T : unmanaged
	{
		ref var cmp = ref World.Component<T>();
		var column = _archetype.GetComponentIndex(ref cmp);
		return column >= 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Entity(int row)
		=> new (World, _archetype.Entities[row].Entity);
}

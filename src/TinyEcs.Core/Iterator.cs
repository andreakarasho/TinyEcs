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


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<T> Span<T>() where T : unmanaged
		=> Field<T>();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Has<T>() where T : unmanaged
		=> _archetype.Has<T>();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Entity(int i)
		=> _archetype.Entity(i);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly UnsafeSpan<T> Field<T>() where T : unmanaged
		=> _archetype.Field<T>();
}

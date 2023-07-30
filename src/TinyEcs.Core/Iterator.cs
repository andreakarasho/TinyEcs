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
	public World World => _archetype.World;
	public int Count => _archetype.Count;
	public float DeltaTime => World.DeltaTime;
	public object? UserData { get; }

	public readonly Span<T> Field<T>() where T : unmanaged
		=> _archetype.Field<T>();

	public readonly bool Has<T>() where T : unmanaged
		=> _archetype.Has<T>();

	public readonly EntityView Entity(int i)
		=> _archetype.Entity(i);

	internal readonly Span<byte> GetComponentRaw(EntityID id, int row, int count)
		=> _archetype.GetComponentRaw(id, row, count);
}

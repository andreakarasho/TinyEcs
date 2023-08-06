namespace TinyEcs;

public delegate void IteratorDelegate(ref Iterator it);

public readonly ref struct Iterator
{
	private readonly Archetype _archetype;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
	public readonly FieldIterator<T> Field<T>() where T : unmanaged
	{
		ref var cmp = ref World.Component<T>();
		var span = _archetype.GetWholeComponentBuffer(ref cmp);
		ref var start = ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));
		return new FieldIterator<T>(ref start, _archetype.Entities);
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

[SkipLocalsInit]
public readonly ref struct FieldIterator<T> where T : unmanaged
{
	private readonly ref T _firstElement;
	private readonly ref ArchetypeEntity _firstEntity;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal FieldIterator(ref T firstElement, ArchetypeEntity[] entities)
	{
		_firstElement = ref firstElement;
		_firstEntity = ref MemoryMarshal.GetArrayDataReference(entities);
	}

	public readonly ref T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.Add(ref _firstElement, Unsafe.Add(ref _firstEntity, index).TableRow);
	}
}

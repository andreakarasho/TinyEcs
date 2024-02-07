namespace TinyEcs;

public delegate void IteratorDelegate(ref Iterator it);

public readonly ref struct Iterator
{
    private readonly Span<EntityView> _entities;
    private readonly Archetype _archetype;
    private readonly Span<Array> _columns;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Iterator(
        Commands commands,
        Archetype archetype,
        object? userData,
        Span<Array> columns
    )
        : this(
            commands,
            archetype.Count,
            archetype,
            archetype.Entities,
            userData,
            columns
        ) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Iterator(
        Commands commands,
        int count,
		Archetype archetype,
        Span<EntityView> entities,
        object? userData,
        Span<Array> columns
    )
    {
        Commands = commands;
        World = commands.World;
        UserData = userData;
        _archetype = archetype;
        _entities = entities;
        Count = count;
        DeltaTime = commands.World.DeltaTime;
        _columns = columns;
    }

    public readonly Commands Commands { get; }
    public readonly World World { get; }
    public readonly int Count { get; }
    public readonly float DeltaTime { get; }
    public readonly object? UserData { get; }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Span<T> Field<T>() where T : struct
    {
	    return _archetype.ComponentData<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T FieldRef<T>() where T : struct
    {
	    return ref MemoryMarshal.GetReference(_archetype.ComponentData<T>());
    }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ref EntityView Entity(int row) => ref _entities[row];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly CommandEntityView EntityDeferred(int row) => Commands.Entity(_entities[row]);
}

[SkipLocalsInit]
public unsafe readonly ref struct FieldIterator<T> where T : struct
{
    private readonly ref T _firstElement;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal FieldIterator(Span<T> elements)
    {
        _firstElement = ref MemoryMarshal.GetReference(elements);
    }

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(ref _firstElement, index);
    }
}

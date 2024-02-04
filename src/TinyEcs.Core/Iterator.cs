namespace TinyEcs;

public delegate void IteratorDelegate(ref Iterator it);

public readonly ref struct Iterator
{
    private readonly Span<EcsID> _entities;
    private readonly Span<int> _entitiesToTableRows;
    private readonly Table _table;
    private readonly Span<nuint> _columns;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Iterator(
        Commands commands,
        Archetype archetype,
        object? userData,
        Span<nuint> columns,
        EcsID eventID = default,
        EcsID eventComponent = default
    )
        : this(
            commands,
            archetype.Count,
            archetype.Table,
            archetype.Entities,
            archetype.EntitiesTableRows,
            userData,
            columns,
            eventID,
            eventComponent
        ) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Iterator(
        Commands commands,
        int count,
        Table table,
        Span<EcsID> entities,
        Span<int> toRows,
        object? userData,
        Span<nuint> columns,
        EcsID eventID = default,
        EcsID eventComponent = default
    )
    {
        Commands = commands;
        World = commands.World;
        UserData = userData;
        EventID = eventID;
        EventTriggeredComponent = eventComponent;
        _table = table;
        _entities = entities;
        _entitiesToTableRows = toRows;
        Count = count;
        DeltaTime = commands.World.DeltaTime;
        _columns = columns;
    }

    public readonly Commands Commands { get; }
    public readonly World World { get; }
    public readonly int Count { get; }
    public readonly float DeltaTime { get; }
    public readonly object? UserData { get; }
    public readonly EcsID EventID { get; }
    public readonly EcsID EventTriggeredComponent { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe readonly FieldIterator<T> Field<T>(int index) where T : unmanaged
    {
		return new FieldIterator<T>(_table.ComponentData<T>(index, 0, Count, sizeof(T)), _entitiesToTableRows);
        var data = (T*)_columns[index];
        return new FieldIterator<T>(data, _entitiesToTableRows);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ref T Single<T>(int index) where T : unmanaged
    {
        var data = (T*)_columns[index];
        return ref data[0];
    }

    public readonly EntityView Entity(int row) => World.Entity(_entities[row]);

    public readonly CommandEntityView EntityDeferred(int row) => Commands.Entity(_entities[row]);
}

[SkipLocalsInit]
public unsafe readonly ref struct FieldIterator<T> where T : unmanaged
{
    private readonly ref T _firstElement;
    private readonly ref int _firstEntity;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal FieldIterator(Span<T> elements, Span<int> entities)
    {
        _firstElement = ref MemoryMarshal.GetReference(elements);
        _firstEntity = ref MemoryMarshal.GetReference(entities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal FieldIterator(T* elements, Span<int> entities)
    {
        _firstElement = ref Unsafe.AsRef<T>(elements);
        _firstEntity = ref MemoryMarshal.GetReference(entities);
    }

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(ref _firstElement, Unsafe.Add(ref _firstEntity, index));
    }
}

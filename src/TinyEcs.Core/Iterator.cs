namespace TinyEcs;

public delegate void IteratorDelegate(ref Iterator it);

public readonly ref struct Iterator
{
	private readonly Archetype _archetype;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Iterator(Commands commands, Archetype archetype, object? userData)
	{
		Commands = commands;
		_archetype = archetype;
		UserData = userData;
	}

	public Commands Commands { get; }
	public readonly World World => _archetype.World;
	public readonly int Count => _archetype.Count;
	public readonly float DeltaTime => World.DeltaTime;
	public object? UserData { get; }
	internal Archetype Archetype => _archetype;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe readonly FieldIterator<T> Field<T>() where T : unmanaged, IComponent
	{
		ref var cmp = ref World.Component<T>();

		var column = _archetype.GetComponentIndex(ref cmp);
		EcsAssert.Assert(column >= 0);
		EcsAssert.Assert(cmp.Size == sizeof(T));

		var span = _archetype.Table.ComponentData<T>(column, 0, _archetype.Table.Rows);
		return new FieldIterator<T>(span, _archetype.EntitiesTableRows);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Has<T>() where T : unmanaged, IComponentStub
	{
		ref var cmp = ref World.Component<T>();
		var column = _archetype.GetComponentIndex(ref cmp);
		return column >= 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityView Entity(int row)
		=> new (World, _archetype.Entities[row]);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly CommandEntityView EntityDefer(int row)
		=> Commands.Entity(Entity(row));
}

[SkipLocalsInit]
public readonly ref struct FieldIterator<T> where T : unmanaged
{
	private readonly ref T _firstElement;
	private readonly ref int _firstEntity;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal FieldIterator(Span<T> elements, int[] entities)
	{
		_firstElement = ref MemoryMarshal.GetReference(elements);
		_firstEntity = ref MemoryMarshal.GetArrayDataReference(entities);
	}

	public readonly ref T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref Unsafe.Add(ref _firstElement, Unsafe.Add(ref _firstEntity, index));
	}
}

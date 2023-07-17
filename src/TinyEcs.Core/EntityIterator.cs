namespace TinyEcs;

[SkipLocalsInit]
public readonly ref struct EntityIterator
{
	private readonly Archetype _archetype;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal EntityIterator([NotNull] Archetype archetype, float delta)
	{
		_archetype = archetype;
		Count = archetype.Count;
		DeltaTime = delta;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal EntityIterator([NotNull] Archetype archetype, int count, float delta)
	{
		_archetype = archetype;
		Count = count;
		DeltaTime = delta;
	}

	public readonly int Count;
	public readonly float DeltaTime;


	internal readonly World World => _archetype.World;
	internal readonly Archetype Archetype => _archetype;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly unsafe Span<T> Field<T>() where T : unmanaged
	{
		var id = TypeInfo<T>.GetID(World);
		ref var value = ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(_archetype.GetComponentRaw(id, 0, Count)));

		Debug.Assert(!Unsafe.IsNullRef(ref value));

		return MemoryMarshal.CreateSpan(ref value, Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly unsafe bool Has<T>() where T : unmanaged
	{
		var id = TypeInfo<T>.GetID(World);
		var data = _archetype.GetComponentRaw(id, 0, Count);
		if (data.IsEmpty)
			return false;

		ref var value = ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(data));

		return !Unsafe.IsNullRef(ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly EntityID Entity(int row) 
		=> _archetype.Entities[row];
}


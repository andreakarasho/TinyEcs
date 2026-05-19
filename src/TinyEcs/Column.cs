namespace TinyEcs;

internal abstract class Column
{
	public uint[] ChangedTicks;
	public uint[] AddedTicks;

	protected Column(int initialCapacity)
	{
		ChangedTicks = initialCapacity > 0 ? new uint[initialCapacity] : Array.Empty<uint>();
		AddedTicks = initialCapacity > 0 ? new uint[initialCapacity] : Array.Empty<uint>();
	}

	public abstract int Capacity { get; }
	public abstract void CopyTo(int srcIdx, Column dst, int dstIdx);
	public abstract void EnsureCapacity(int capacity);
	public abstract void SetUntyped(int row, object value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MarkChanged(int index, uint ticks) => ChangedTicks[index] = ticks;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MarkAdded(int index, uint ticks) => AddedTicks[index] = ticks;

	protected void GrowTicks(int newCapacity)
	{
		Array.Resize(ref ChangedTicks, newCapacity);
		Array.Resize(ref AddedTicks, newCapacity);
	}
}

internal sealed class Column<T> : Column where T : struct
{
	internal T[] Data;

	public Column(int initialCapacity) : base(initialCapacity)
	{
		Data = initialCapacity > 0 ? new T[initialCapacity] : Array.Empty<T>();
	}

	public override int Capacity => Data.Length;

	public override void SetUntyped(int row, object value) => Data[row] = (T)value;

	public override void EnsureCapacity(int capacity)
	{
		if (Data.Length >= capacity)
			return;

		var newCap = Data.Length == 0 ? 8 : Data.Length;
		while (newCap < capacity) newCap *= 2;

		Array.Resize(ref Data, newCap);
		GrowTicks(newCap);
	}

	public override void CopyTo(int srcIdx, Column dst, int dstIdx)
	{
		var typed = (Column<T>)dst;
		typed.Data[dstIdx] = Data[srcIdx];
		typed.ChangedTicks[dstIdx] = ChangedTicks[srcIdx];
		typed.AddedTicks[dstIdx] = AddedTicks[srcIdx];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref T GetRef(int index) => ref Data[index];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref T GetFirstRef() => ref MemoryMarshal.GetArrayDataReference(Data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<T> AsSpan(int count) => Data.AsSpan(0, count);
}

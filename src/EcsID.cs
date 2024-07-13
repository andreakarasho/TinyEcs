namespace TinyEcs;

public static class EcsIdEx
{
	public static bool IsPair(this ref readonly EcsID id)
		=> IDOp.IsPair(id);

	public static EcsID First(this ref readonly EcsID id)
		=> id.IsPair() ? IDOp.GetPairFirst(id) : 0;

	public static EcsID Second(this ref readonly EcsID id)
		=> id.IsPair() ? IDOp.GetPairSecond(id) : 0;

	public static (EcsID, EcsID) Pair(this ref readonly EcsID id)
		=> (id.First(), id.Second());

	public static bool IsValid(this ref readonly EcsID id)
		=> id != 0;

	public static EcsID RealId(this ref readonly EcsID id)
		=> IDOp.RealID(id);

	public static int Generation(this ref readonly EcsID id)
		=> (int) IDOp.GetGeneration(id);
}

/// <summary>
/// The ecs entity rappresentation which is a wrapper around an ulong
/// </summary>
// [StructLayout(LayoutKind.Explicit)]
// public readonly struct EcsID : IEquatable<ulong>, IComparable<ulong>, IEquatable<EcsID>, IComparable<EcsID>
// {
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	internal EcsID(ulong value) => Value = value;
//
//
// 	/// <summary>
// 	/// ID + Generation
// 	/// </summary>
// 	[FieldOffset(0)]
// 	public readonly ulong Value;
//
// 	/// <summary>
// 	/// ID only
// 	/// </summary>
// 	[FieldOffset(0)]
// 	internal readonly int ID;
//
// 	/// <summary>
// 	/// Generation count.<br/>
// 	/// This number rappresent how many times the real ID has been recycled.
// 	/// </summary>
// 	[FieldOffset(4)]
// 	internal readonly int Generation;
//
//
// 	public readonly bool IsValid => Value != 0;
// 	public readonly bool IsPair => IDOp.IsPair(Value);
// 	public readonly EcsID First => IsPair ? IDOp.GetPairFirst(Value) : 0;
// 	public readonly EcsID Second => IsPair ? IDOp.GetPairSecond(Value) : 0;
// 	public readonly (EcsID, EcsID) Pair => (First, Second);
//
//
// 	public readonly int CompareTo(ulong other) => Value.CompareTo(other);
// 	public readonly int CompareTo(EcsID other) => Value.CompareTo(other.Value);
// 	public readonly bool Equals(ulong other) => Value == other;
// 	public readonly bool Equals(EcsID other) => Value == other.Value;
//
//
// 	// public static implicit operator ulong(EcsID id) => id.Value;
// 	// public static implicit operator EcsID(ulong value) => new (value);
// 	public static bool operator ==(EcsID id, EcsID other) => id.Value.Equals(other.Value);
// 	public static bool operator !=(EcsID id, EcsID other) => !id.Value.Equals(other.Value);
//
//
// 	public readonly override bool Equals(object? obj) => obj is EcsID ent && Equals(ent);
// 	public readonly override int GetHashCode() => Value.GetHashCode();
// 	public readonly override string ToString()
// 	{
// 		if (IsPair)
// 			return $"({First}, {Second}) | {Value}";
// 		return $"[{ID} - @{Generation} | {Value}]";
// 	}
// }

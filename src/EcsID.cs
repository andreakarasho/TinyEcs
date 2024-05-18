namespace TinyEcs;

[StructLayout(LayoutKind.Explicit)]
public readonly struct EcsID : IEquatable<ulong>, IComparable<ulong>, IEquatable<EcsID>, IComparable<EcsID>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal EcsID(ulong value) => Value = value;


	[FieldOffset(0)]
	public readonly ulong Value;

	[FieldOffset(0)]
	internal readonly int ID;

	[FieldOffset(4)]
	internal readonly int Generation;


	public readonly bool IsValid => Value != 0;
	public readonly bool IsPair => IDOp.IsPair(Value);
	public readonly EcsID First => IDOp.GetPairFirst(Value);
	public readonly EcsID Second => IDOp.GetPairSecond(Value);
	public readonly (EcsID, EcsID) Pair => (First, Second);


	public readonly int CompareTo(ulong other) => Value.CompareTo(other);
	public readonly int CompareTo(EcsID other) => Value.CompareTo(other.Value);
	public readonly bool Equals(ulong other) => Value == other;
	public readonly bool Equals(EcsID other) => Value == other.Value;


	public static implicit operator ulong(EcsID id) => id.Value;
	public static implicit operator EcsID(ulong value) => new (value);
	public static implicit operator Term(EcsID value) => new Term(value, TermOp.With);
	public static bool operator ==(EcsID id, EcsID other) => id.Value.Equals(other.Value);
	public static bool operator !=(EcsID id, EcsID other) => !id.Value.Equals(other.Value);

	// public static Term operator !(EcsID id) => Term.Without(id.Value);
	// public static Term operator -(EcsID id) => Term.Without(id.Value);
	// public static Term operator +(EcsID id) => Term.With(id.Value);


	public readonly override bool Equals(object? obj) => obj is EcsID ent && Equals(ent);
	public readonly override int GetHashCode() => Value.GetHashCode();
	public readonly override string ToString() => Value.ToString();
}

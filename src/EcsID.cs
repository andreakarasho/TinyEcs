namespace TinyEcs;

[StructLayout(LayoutKind.Explicit)]
public readonly struct EcsID : IEquatable<ulong>, IComparable<ulong>, IEquatable<EcsID>, IComparable<EcsID>
{
	[FieldOffset(0)]
	public readonly ulong Value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public EcsID(ulong value) => Value = value;


	public readonly int CompareTo(ulong other) => Value.CompareTo(other);
	public readonly int CompareTo(EcsID other) => Value.CompareTo(other.Value);
	public readonly bool Equals(ulong other) => Value == other;
	public readonly bool Equals(EcsID other) => Value == other.Value;


	public static implicit operator ulong(EcsID id) => id.Value;
	public static implicit operator EcsID(ulong value) => new (value);

	public static bool operator ==(EcsID id, EcsID other) => id.Value.Equals(other.Value);
	public static bool operator !=(EcsID id, EcsID other) => !id.Value.Equals(other.Value);

	// public static Term operator !(EcsID id) => Term.Without(id.Value);
	// public static Term operator -(EcsID id) => Term.Without(id.Value);
	// public static Term operator +(EcsID id) => Term.With(id.Value);


	public readonly override bool Equals(object? obj) => obj is EcsID ent && Equals(ent);
	public readonly override int GetHashCode() => Value.GetHashCode();
	public readonly override string ToString() =>  Value.ToString();
}

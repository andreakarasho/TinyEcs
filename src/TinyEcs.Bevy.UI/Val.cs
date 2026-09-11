namespace TinyEcs.Bevy.UI;

public enum ValType : byte
{
	Auto = 0,
	Px = 1,
	Percent = 2,
}

public struct Val : IEquatable<Val>
{
	public ValType Type;
	public float Value;

	public Val(ValType type, float value)
	{
		Type = type;
		Value = value;
	}

	public static readonly Val Auto = new(ValType.Auto, 0);
	public static Val Px(float v) => new(ValType.Px, v);
	public static Val Percent(float v) => new(ValType.Percent, v);

	public readonly bool IsAuto => Type == ValType.Auto;

	// Exact (bitwise-ish) compare, not epsilon: callers use it to decide whether
	// a layout write is a real change worth marking for the relayout gate, and a
	// sub-epsilon drift that never converges would pin the gate open.
	public readonly bool Equals(Val other) => Type == other.Type && Value == other.Value;
	public readonly override bool Equals(object? obj) => obj is Val v && Equals(v);
	public readonly override int GetHashCode() => HashCode.Combine((byte)Type, Value);
	public static bool operator ==(Val a, Val b) => a.Equals(b);
	public static bool operator !=(Val a, Val b) => !a.Equals(b);
}

public struct UiRect
{
	public Val Left, Right, Top, Bottom;

	public UiRect(Val l, Val r, Val t, Val b) { Left = l; Right = r; Top = t; Bottom = b; }

	public static readonly UiRect Zero = new(Val.Px(0), Val.Px(0), Val.Px(0), Val.Px(0));
	public static UiRect All(float px) { var v = Val.Px(px); return new UiRect(v, v, v, v); }
	public static UiRect Symmetric(float h, float v) => new(Val.Px(h), Val.Px(h), Val.Px(v), Val.Px(v));
	public static UiRect Horizontal(float px) => new(Val.Px(px), Val.Px(px), Val.Px(0), Val.Px(0));
	public static UiRect Vertical(float px) => new(Val.Px(0), Val.Px(0), Val.Px(px), Val.Px(px));
}

public enum Display : byte
{
	Flex = 0,
	None = 1,
}

public enum PositionType : byte
{
	Relative = 0,
	Absolute = 1,
}

public enum Overflow : byte
{
	Visible = 0,
	Clip = 1,
	Scroll = 2,
}

public enum FlexDirection : byte
{
	Row = 0,
	Column = 1,
}

public enum JustifyContent : byte
{
	Start = 0,
	Center = 1,
	End = 2,
}

public enum AlignItems : byte
{
	Start = 0,
	Center = 1,
	End = 2,
}

namespace TinyEcs.Bevy.UI;

public enum ValType : byte
{
	Auto = 0,
	Px = 1,
	Percent = 2,
}

public struct Val
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

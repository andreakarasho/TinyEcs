namespace TinyEcs.Bevy.Input;

/// <summary>
/// Mouse button identifiers. Numeric values mirror the classic XNA layout
/// (None, Left, Middle, Right, XButton1, XButton2) so XNA-family backends can
/// map with a direct cast.
/// </summary>
public enum MouseButton
{
	None = 0,
	Left = 1,
	Middle = 2,
	Right = 3,
	XButton1 = 4,
	XButton2 = 5,

	Count = 6,
}

/// <summary>
/// Per-frame button-down snapshot fed by the backend via
/// <see cref="MouseInput.SetSnapshot"/>. Flag bit for button <c>b</c> is
/// <c>1 &lt;&lt; ((int)b - 1)</c> — see <see cref="MouseButtonsExtensions.ToFlag"/>.
/// </summary>
[Flags]
public enum MouseButtons
{
	None = 0,
	Left = 1 << 0,
	Middle = 1 << 1,
	Right = 1 << 2,
	XButton1 = 1 << 3,
	XButton2 = 1 << 4,
}

public static class MouseButtonsExtensions
{
	public static MouseButtons ToFlag(this MouseButton button)
		=> button is > MouseButton.None and < MouseButton.Count
			? (MouseButtons)(1 << ((int)button - 1))
			: MouseButtons.None;
}

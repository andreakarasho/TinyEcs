namespace TinyEcs.Bevy.Input;

/// <summary>
/// One typed character, sent as a Bevy event by the host backend (e.g. an SDL
/// TEXTINPUT hook). Carries composed text input — IME/layout/shift handling is
/// the platform's job — as opposed to the raw key edges on
/// <see cref="KeyboardInput"/>. Text widgets read this; shortcuts read key edges.
/// Backspace arrives here as '\b' (the text editor deletes on it, not on the key).
/// </summary>
public struct CharInput
{
	public char Value;
}

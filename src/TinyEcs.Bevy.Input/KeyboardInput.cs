namespace TinyEcs.Bevy.Input;

/// <summary>
/// Backend-agnostic keyboard state with edge detection. Same contract as
/// <see cref="MouseInput"/>: backend feeds the set of currently-down keys via
/// <see cref="SetSnapshot"/> once per frame, then calls <see cref="Update"/>.
/// </summary>
public sealed class KeyboardInput
{
	// KeyCode values are Win32 VK codes, max 254.
	private const int MaxKeys = 256;

	private readonly bool[] _pending = new bool[MaxKeys];
	private readonly bool[] _old = new bool[MaxKeys];
	private readonly bool[] _new = new bool[MaxKeys];
	// Keys the backend still reported as down at the moment focus came back. See
	// Update: they read as UP until the backend reports them released.
	private readonly bool[] _stale = new bool[MaxKeys];
	private bool _pendingActive = true;
	// Matches _pendingActive's default: without this the FIRST Update would look
	// like a focus-regain and swallow the first key edge of the process.
	private bool _active = true;

	private readonly KeyCode[] _pressedBuf = new KeyCode[MaxKeys];
	private int _pressedCount;

	/// <summary>
	/// Feed the keys currently held down. <paramref name="active"/> is the
	/// window-focus gate (see <see cref="MouseInput.SetSnapshot"/>).
	/// </summary>
	public void SetSnapshot(ReadOnlySpan<KeyCode> pressed, bool active = true)
	{
		Array.Clear(_pending);
		foreach (var key in pressed)
		{
			var idx = (int)key;
			if (idx >= 0 && idx < MaxKeys)
				_pending[idx] = true;
		}
		_pendingActive = active;
	}

	/// <summary>Advance one frame.</summary>
	public void Update(float totalTimeMs)
	{
		var wasActive = _active;

		Array.Copy(_new, _old, MaxKeys);
		Array.Copy(_pending, _new, MaxKeys);
		_active = _pendingActive;

		// Focus regained: a key held across the switch (the Alt of an alt-tab,
		// the Enter that dismissed another window) pressed while we weren't
		// focused. Seed old = new so it reads as already-held and only a press
		// that STARTS after focus fires an edge.
		if (_active && !wasActive)
		{
			Array.Copy(_new, _old, MaxKeys);
			// ...and hide it from the HELD reads too, until the backend says it
			// came up. Suppressing only the EDGE is not enough when the key-up
			// never arrives: the window switcher swallows the Alt-up of an
			// alt-tab, and a backend that keeps its own key cache (FNA's
			// Keyboard.keys, which SDL3_FNAPlatform never clears on
			// FOCUS_LOST) then reports that Alt held for the rest of the
			// process — silently disabling every shortcut that stands down
			// while Alt is down.
			Array.Copy(_new, _stale, MaxKeys);
		}

		_pressedCount = 0;
		for (var i = 0; i < MaxKeys; i++)
		{
			// A stranded key stops being stale once it is genuinely up; from then
			// on a fresh press is a real press.
			if (_stale[i] && !_new[i])
				_stale[i] = false;
			if (_new[i] && !_stale[i])
				_pressedBuf[_pressedCount++] = (KeyCode)i;
		}
	}

	public bool IsPressed(KeyCode key) => _active && !_stale[(int)key] && _new[(int)key] && _old[(int)key];

	public bool IsPressedOnce(KeyCode key) => _active && !_stale[(int)key] && _new[(int)key] && !_old[(int)key];

	public bool IsReleased(KeyCode key) => _active && !_stale[(int)key] && !_new[(int)key] && _old[(int)key];

	/// <summary>
	/// Keys down this frame — empty while the window is inactive, so consumers
	/// that poll the held set (hotkey capture, mod input feeds) see no input
	/// typed into another app. Valid until the next <see cref="Update"/>.
	/// </summary>
	public ReadOnlySpan<KeyCode> PressedKeys
		=> _active ? _pressedBuf.AsSpan(0, _pressedCount) : ReadOnlySpan<KeyCode>.Empty;
}

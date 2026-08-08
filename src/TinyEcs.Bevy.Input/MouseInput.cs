using System.Numerics;

namespace TinyEcs.Bevy.Input;

/// <summary>
/// Backend-agnostic mouse state with edge detection (pressed / pressed-once /
/// released), double-click tracking, per-frame consume flags and wheel-consume
/// semantics.
///
/// Contract: once per frame the backend feeds a raw snapshot via
/// <see cref="SetSnapshot"/> (positions already in logical pixels — the
/// backend owns DPI scaling) and then calls <see cref="Update"/> with a
/// monotonic clock in milliseconds. Systems query the edge/consume API after
/// that. The library never polls an OS device itself.
/// </summary>
public sealed class MouseInput
{
	/// <summary>Max gap between two clicks of the same button to count as a double click (ms).</summary>
	public float DoubleClickDelta = 300f;

	private Vector2 _pendingPos;
	private MouseButtons _pendingDown;
	private float _pendingWheel;
	private bool _pendingActive = true;

	private Vector2 _oldPos, _newPos;
	private MouseButtons _oldDown, _newDown;
	// Matches _pendingActive's default: without this the FIRST Update would look
	// like a focus-regain and swallow the first press edge of the process.
	private bool _active = true;
	// Last position reported while focused: what Position keeps answering while
	// the window is inactive, so hover/pick/tooltip consumers don't track a
	// cursor the app doesn't own.
	private Vector2 _lastActivePos;

	private float _lastClickTime, _currentTime;
	private readonly MouseButton?[] _lastClickButtons = new MouseButton?[2];
	private Vector2 _lastClickPosition;

	// Buttons consumed by a UI handler this frame. Cleared at the start of
	// Update so the flag only suppresses reads for the remainder of the tick —
	// lets a UI close handler eat a right-click before world systems see it.
	private readonly bool[] _consumed = new bool[(int)MouseButton.Count];

	/// <summary>
	/// Feed the raw state for the upcoming frame. <paramref name="active"/> is
	/// the window-focus gate: while false, edge checks report nothing, the wheel
	/// reads zero and Position freezes (state still advances so no stale edges
	/// fire on refocus). <paramref name="wheelDelta"/> is this frame's scroll in
	/// notches.
	/// </summary>
	public void SetSnapshot(Vector2 position, MouseButtons down, float wheelDelta = 0f, bool active = true)
	{
		_pendingPos = position;
		_pendingDown = down;
		// Dropped, not deferred: an unfocused window still receives wheel events
		// on Windows ("scroll inactive windows"), and accumulating them would
		// dump the whole spin into the first focused frame.
		if (active)
			_pendingWheel += wheelDelta;
		_pendingActive = active;
	}

	/// <summary>Advance one frame. <paramref name="totalTimeMs"/> is a monotonic clock in milliseconds.</summary>
	public void Update(float totalTimeMs)
	{
		for (var i = 0; i < _consumed.Length; i++)
			_consumed[i] = false;
		WheelConsumed = false;

		var wasActive = _active;

		_oldPos = _newPos;
		_oldDown = _newDown;
		_newPos = _pendingPos;
		_newDown = _pendingDown;
		_active = _pendingActive;
		Wheel = _pendingWheel;
		_pendingWheel = 0f;
		_currentTime = totalTimeMs;

		// Focus regained: the click that raised the window (and any key/button
		// held through the switch) happened while we weren't focused, so its
		// press edge must not reach gameplay. Seeding old = new on this frame
		// makes everything currently down read as "already held", so only a
		// press that STARTS after focus counts. Same for the position: no jump
		// offset from wherever the cursor wandered while we were away.
		if (_active && !wasActive)
		{
			_oldDown = _newDown;
			_oldPos = _newPos;
		}

		if (_active)
			_lastActivePos = _newPos;

		for (var button = MouseButton.None + 1; button < MouseButton.Count; button++)
		{
			if (IsPressedDouble(button))
			{
				_lastClickButtons[0] = _lastClickButtons[1] = null;
			}

			if (IsPressedOnce(button))
			{
				_lastClickPosition = _newPos;

				if (_lastClickButtons[0] == null)
				{
					_lastClickButtons[0] = button;
					_lastClickTime = _currentTime + DoubleClickDelta;
				}
				else if (_lastClickButtons[0] == button && _lastClickButtons[1] == null)
				{
					_lastClickButtons[1] = button;
				}

				break;
			}

			if (IsReleased(button))
			{
				_lastClickPosition = Vector2.Zero;
			}
		}

		if (_currentTime > _lastClickTime)
		{
			_lastClickButtons[0] = _lastClickButtons[1] = null;
		}
	}

	/// <summary>Cursor position, frozen at its last focused value while the window is inactive.</summary>
	public Vector2 Position => _active ? _newPos : _lastActivePos;
	public Vector2 PositionOffset => _active ? _newPos - _oldPos : Vector2.Zero;
	/// <summary>Offset from the position of the last button press. Zero-anchored after release.</summary>
	public Vector2 DraggingOffset => _active ? _newPos - _lastClickPosition : Vector2.Zero;

	public float Wheel { get; private set; }

	/// <summary>
	/// Set when a UI handler has consumed this frame's scroll (e.g. a hovered
	/// scrollable). ConsumeWheel ALSO zeroes the Wheel reading so a downstream
	/// consumer that doesn't honour the flag still sees a no-op.
	/// </summary>
	public bool WheelConsumed { get; private set; }

	public void ConsumeWheel()
	{
		WheelConsumed = true;
		Wheel = 0f;
	}

	public bool IsPressed(MouseButton button)
		=> !IsConsumed(button) && _active && Down(_newDown, button) && Down(_oldDown, button);

	public bool IsPressedOnce(MouseButton button)
		=> !IsConsumed(button) && _active && Down(_newDown, button) && !Down(_oldDown, button);

	public bool IsReleased(MouseButton button)
		=> !IsConsumed(button) && _active && !Down(_newDown, button) && Down(_oldDown, button);

	public bool IsPressedDouble(MouseButton button)
		=> !IsConsumed(button) && _active && _lastClickButtons[0] == button && _lastClickButtons[1] == button;

	public void Consume(MouseButton button)
	{
		var idx = (int)button;
		if (idx >= 0 && idx < _consumed.Length)
			_consumed[idx] = true;
	}

	public bool IsConsumed(MouseButton button)
	{
		var idx = (int)button;
		return idx >= 0 && idx < _consumed.Length && _consumed[idx];
	}

	private static bool Down(MouseButtons state, MouseButton button)
		=> (state & button.ToFlag()) != 0;
}

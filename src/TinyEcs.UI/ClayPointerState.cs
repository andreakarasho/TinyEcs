using System.Numerics;

namespace TinyEcs.UI;

/// <summary>
/// Captures pointer updates that should be forwarded to the Clay runtime.
/// </summary>
public struct ClayPointerState
{
	private Vector2 _position;

	/// <summary>
	/// Current pointer position in UI coordinates.
	/// </summary>
	public Vector2 Position
	{
		readonly get => _position;
		set
		{
			if (value == _position)
				return;
			MoveDelta += value - _position;
			_position = value;
			Dirty = true;
		}
	}

	/// <summary>
	/// Accumulated pointer movement since the last frame.
	/// </summary>
	public Vector2 MoveDelta { readonly get; private set; }

	/// <summary>
	/// Accumulated scroll delta since the last frame.
	/// </summary>
	public Vector2 ScrollDelta { readonly get; private set; }

	/// <summary>
	/// Indicates whether the primary pointer button is held.
	/// </summary>
	public bool PrimaryDown
	{
		readonly get => _primaryDown;
		set
		{
			if (value == _primaryDown)
				return;
			_primaryDown = value;
			Dirty = true;
		}
	}

	/// <summary>
	/// Primary button state captured at the end of the previous frame.
	/// </summary>
	internal bool LastPrimaryDown { readonly get; private set; }

	/// <summary>
	/// When true, Clay drag scrolling is enabled for <see cref="UpdateScrollContainers"/>.
	/// </summary>
	public bool EnableDragScrolling { readonly get; set; }

	/// <summary>
	/// Time elapsed (in seconds) since the last pointer update.
	/// </summary>
	public float DeltaTime { readonly get; set; }

	/// <summary>
	/// Internal flag indicating whether Clay should receive an update this frame.
	/// </summary>
	internal bool Dirty { readonly get; private set; }

	private bool _primaryDown;

	/// <summary>
	/// Apply an absolute pointer position without accumulating delta.
	/// Useful when the caller manages deltas manually.
	/// </summary>
	public void SetPositionImmediate(Vector2 position)
	{
		_position = position;
		Dirty = true;
	}

	/// <summary>
	/// Add scroll delta for the current frame.
	/// </summary>
	public void AddScroll(Vector2 delta)
	{
		if (delta == Vector2.Zero)
			return;
		ScrollDelta += delta;
		Dirty = true;
	}

	/// <summary>
	/// Explicitly accumulate pointer movement.
	/// </summary>
	public void AddMove(Vector2 delta)
	{
		if (delta == Vector2.Zero)
			return;
		MoveDelta += delta;
		Dirty = true;
	}

	/// <summary>
	/// Clears accumulated state after it has been applied to Clay.
	/// </summary>
	internal void ResetFrame()
	{
		MoveDelta = Vector2.Zero;
		ScrollDelta = Vector2.Zero;
		Dirty = false;
		DeltaTime = 0f;
		LastPrimaryDown = _primaryDown;
	}
}

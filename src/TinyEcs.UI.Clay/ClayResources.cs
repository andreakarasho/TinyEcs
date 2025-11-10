using System.Numerics;
using Clay_cs;

namespace TinyEcs.UI.Clay;

/// <summary>
/// Mouse button flags for compact multi-button support.
/// Can be combined with bitwise OR.
/// </summary>
[Flags]
public enum ClayMouseButton
{
	None = 0,
	Left = 1 << 0,
	Right = 1 << 1,
	Middle = 1 << 2,
	Button4 = 1 << 3,
	Button5 = 1 << 4
}

/// <summary>
/// Platform-agnostic pointer input state resource.
/// Renderer/platform updates this resource, Clay systems consume it.
/// </summary>
public class ClayPointerState
{
	/// <summary>
	/// Current pointer position in screen coordinates.
	/// </summary>
	public Vector2 Position;

	/// <summary>
	/// Mouse buttons currently held down (bitwise flags).
	/// Internal use only - use IsLeftDown, IsRightDown, etc. properties instead.
	/// </summary>
	public ClayMouseButton ButtonsDown;

	/// <summary>
	/// Mouse buttons pressed this frame (bitwise flags).
	/// Internal use only - use IsLeftPressed, IsRightPressed, etc. properties instead.
	/// </summary>
	public ClayMouseButton ButtonsPressed;

	/// <summary>
	/// Mouse buttons released this frame (bitwise flags).
	/// Internal use only - use IsLeftReleased, IsRightReleased, etc. properties instead.
	/// </summary>
	public ClayMouseButton ButtonsReleased;

	/// <summary>
	/// Scroll delta for this frame (mouse wheel / trackpad).
	/// </summary>
	public Vector2 ScrollDelta;

	/// <summary>
	/// Time delta since last frame in seconds.
	/// </summary>
	public float DeltaTime;

	/// <summary>
	/// Enable drag scrolling for scroll containers.
	/// </summary>
	public bool EnableDragScrolling;

	// Button state properties for easy access
	/// <summary>Whether the left mouse button is currently down.</summary>
	public bool IsLeftDown
	{
		get => ButtonsDown.HasFlag(ClayMouseButton.Left);
		set
		{
			if (value) ButtonsDown |= ClayMouseButton.Left;
			else ButtonsDown &= ~ClayMouseButton.Left;
		}
	}

	/// <summary>Whether the right mouse button is currently down.</summary>
	public bool IsRightDown
	{
		get => ButtonsDown.HasFlag(ClayMouseButton.Right);
		set
		{
			if (value) ButtonsDown |= ClayMouseButton.Right;
			else ButtonsDown &= ~ClayMouseButton.Right;
		}
	}

	/// <summary>Whether the middle mouse button is currently down.</summary>
	public bool IsMiddleDown
	{
		get => ButtonsDown.HasFlag(ClayMouseButton.Middle);
		set
		{
			if (value) ButtonsDown |= ClayMouseButton.Middle;
			else ButtonsDown &= ~ClayMouseButton.Middle;
		}
	}

	/// <summary>Whether mouse button 4 is currently down.</summary>
	public bool IsButton4Down
	{
		get => ButtonsDown.HasFlag(ClayMouseButton.Button4);
		set
		{
			if (value) ButtonsDown |= ClayMouseButton.Button4;
			else ButtonsDown &= ~ClayMouseButton.Button4;
		}
	}

	/// <summary>Whether mouse button 5 is currently down.</summary>
	public bool IsButton5Down
	{
		get => ButtonsDown.HasFlag(ClayMouseButton.Button5);
		set
		{
			if (value) ButtonsDown |= ClayMouseButton.Button5;
			else ButtonsDown &= ~ClayMouseButton.Button5;
		}
	}

	/// <summary>Whether the left mouse button was pressed this frame.</summary>
	public bool IsLeftPressed
	{
		get => ButtonsPressed.HasFlag(ClayMouseButton.Left);
		set
		{
			if (value) ButtonsPressed |= ClayMouseButton.Left;
			else ButtonsPressed &= ~ClayMouseButton.Left;
		}
	}

	/// <summary>Whether the right mouse button was pressed this frame.</summary>
	public bool IsRightPressed
	{
		get => ButtonsPressed.HasFlag(ClayMouseButton.Right);
		set
		{
			if (value) ButtonsPressed |= ClayMouseButton.Right;
			else ButtonsPressed &= ~ClayMouseButton.Right;
		}
	}

	/// <summary>Whether the middle mouse button was pressed this frame.</summary>
	public bool IsMiddlePressed
	{
		get => ButtonsPressed.HasFlag(ClayMouseButton.Middle);
		set
		{
			if (value) ButtonsPressed |= ClayMouseButton.Middle;
			else ButtonsPressed &= ~ClayMouseButton.Middle;
		}
	}

	/// <summary>Whether mouse button 4 was pressed this frame.</summary>
	public bool IsButton4Pressed
	{
		get => ButtonsPressed.HasFlag(ClayMouseButton.Button4);
		set
		{
			if (value) ButtonsPressed |= ClayMouseButton.Button4;
			else ButtonsPressed &= ~ClayMouseButton.Button4;
		}
	}

	/// <summary>Whether mouse button 5 was pressed this frame.</summary>
	public bool IsButton5Pressed
	{
		get => ButtonsPressed.HasFlag(ClayMouseButton.Button5);
		set
		{
			if (value) ButtonsPressed |= ClayMouseButton.Button5;
			else ButtonsPressed &= ~ClayMouseButton.Button5;
		}
	}

	/// <summary>Whether the left mouse button was released this frame.</summary>
	public bool IsLeftReleased
	{
		get => ButtonsReleased.HasFlag(ClayMouseButton.Left);
		set
		{
			if (value) ButtonsReleased |= ClayMouseButton.Left;
			else ButtonsReleased &= ~ClayMouseButton.Left;
		}
	}

	/// <summary>Whether the right mouse button was released this frame.</summary>
	public bool IsRightReleased
	{
		get => ButtonsReleased.HasFlag(ClayMouseButton.Right);
		set
		{
			if (value) ButtonsReleased |= ClayMouseButton.Right;
			else ButtonsReleased &= ~ClayMouseButton.Right;
		}
	}

	/// <summary>Whether the middle mouse button was released this frame.</summary>
	public bool IsMiddleReleased
	{
		get => ButtonsReleased.HasFlag(ClayMouseButton.Middle);
		set
		{
			if (value) ButtonsReleased |= ClayMouseButton.Middle;
			else ButtonsReleased &= ~ClayMouseButton.Middle;
		}
	}

	/// <summary>Whether mouse button 4 was released this frame.</summary>
	public bool IsButton4Released
	{
		get => ButtonsReleased.HasFlag(ClayMouseButton.Button4);
		set
		{
			if (value) ButtonsReleased |= ClayMouseButton.Button4;
			else ButtonsReleased &= ~ClayMouseButton.Button4;
		}
	}

	/// <summary>Whether mouse button 5 was released this frame.</summary>
	public bool IsButton5Released
	{
		get => ButtonsReleased.HasFlag(ClayMouseButton.Button5);
		set
		{
			if (value) ButtonsReleased |= ClayMouseButton.Button5;
			else ButtonsReleased &= ~ClayMouseButton.Button5;
		}
	}

	public void AddScroll(Vector2 delta)
	{
		ScrollDelta += delta;
	}

	public void ClearScrollDeltas()
	{
		ScrollDelta = Vector2.Zero;
	}

	/// <summary>
	/// Reset transient state (pressed/released flags, scroll delta).
	/// Called at the beginning of each frame.
	/// </summary>
	public void ResetTransientState()
	{
		ButtonsPressed = ClayMouseButton.None;
		ButtonsReleased = ClayMouseButton.None;
		ClearScrollDeltas();
		ScrollDelta = Vector2.Zero;
	}
}

/// <summary>
/// Global Clay UI state resource.
/// Contains Clay context, arena, and render commands.
/// </summary>
public unsafe class ClayUiState : IDisposable
{
	/// <summary>
	/// Clay arena handle (managed memory).
	/// </summary>
	public ClayArenaHandle Arena;

	/// <summary>
	/// Clay context pointer.
	/// </summary>
	public Clay_Context* Context;

	/// <summary>
	/// Current layout dimensions (screen size).
	/// </summary>
	public Clay_Dimensions LayoutDimensions;

	/// <summary>
	/// Render commands output from Clay_EndLayout().
	/// Valid only after layout has been calculated.
	/// Stored as pointer and length since ReadOnlySpan cannot be a field.
	/// </summary>
	public Clay_RenderCommand* RenderCommandsPtr;
	public int RenderCommandsLength;

	/// <summary>
	/// Get render commands as a ReadOnlySpan.
	/// </summary>
	public ReadOnlySpan<Clay_RenderCommand> RenderCommands =>
		RenderCommandsPtr != null
			? new ReadOnlySpan<Clay_RenderCommand>(RenderCommandsPtr, RenderCommandsLength)
			: ReadOnlySpan<Clay_RenderCommand>.Empty;

	/// <summary>
	/// Whether Clay debug mode is enabled.
	/// </summary>
	public bool DebugModeEnabled;

	/// <summary>
	/// Root entity IDs (entities without ClayParent component).
	/// Layout starts from these roots.
	/// </summary>
	public List<ulong> RootEntities = new();

	public void Dispose()
	{
		Arena.Dispose();
		Arena = default;
		Context = null;
	}
}

/// <summary>
/// Clay UI plugin configuration options.
/// </summary>
public class ClayUiOptions
{
	/// <summary>
	/// Initial layout dimensions (screen size).
	/// Can be updated later via ClayUiState.LayoutDimensions.
	/// </summary>
	public Clay_Dimensions LayoutDimensions = new Clay_Dimensions(800, 600);

	/// <summary>
	/// Arena size in bytes. Default is 1MB.
	/// Increase if you have many UI elements.
	/// </summary>
	public uint ArenaSize = 1024 * 1024;

	/// <summary>
	/// Enable Clay debug mode (visual debugging tools).
	/// </summary>
	public bool EnableDebugMode = false;

	/// <summary>
	/// Enable culling (only render visible elements).
	/// </summary>
	public bool EnableCulling = true;

	/// <summary>
	/// Text measurement function delegate.
	/// REQUIRED: Must be set before initializing Clay UI.
	/// This function measures text dimensions for layout calculation.
	/// Signature: unsafe Clay_Dimensions(Clay_StringSlice text, Clay_TextElementConfig* config, void* userData)
	/// </summary>
	public unsafe ClayMeasureTextDelegate? MeasureTextFunction = null;

	/// <summary>
	/// Error handler function delegate.
	/// OPTIONAL: Called when Clay encounters an error (capacity exceeded, invalid state, etc).
	/// If not provided, errors will be silently ignored.
	/// Signature: void(Clay_ErrorData data)
	/// </summary>
	public ClayErrorDelegate? ErrorHandler = null;
}

/// <summary>
/// Pointer event types for Clay UI interaction.
/// </summary>
public enum ClayPointerEventType
{
	Enter,
	Exit,
	Move,
	Pressed,
	Released,
	Click,
	Scroll
}

/// <summary>
/// Pointer event data emitted by Clay interaction system.
/// This is wrapped with On&lt;ClayPointerEvent&gt; when emitted as a trigger.
/// Use app.AddObserver&lt;On&lt;ClayPointerEvent&gt;&gt;(...) to observe pointer events.
/// </summary>
public readonly struct ClayPointerEvent
{
	/// <summary>
	/// Type of pointer event.
	/// </summary>
	public required ClayPointerEventType EventType { get; init; }

	/// <summary>
	/// Pointer position in screen coordinates.
	/// </summary>
	public required Vector2 Position { get; init; }

	/// <summary>
	/// Pointer position in local element coordinates.
	/// </summary>
	public required Vector2 LocalPosition { get; init; }

	/// <summary>
	/// Mouse button(s) involved in this event (bitwise flags).
	/// For Pressed/Released/Click events, this is the button that triggered the event.
	/// For Move/Enter/Exit events, this is the buttons currently held down.
	/// Internal use only - use IsLeftButton, IsRightButton, etc. properties instead.
	/// </summary>
	public required ClayMouseButton Button { get; init; }

	/// <summary>
	/// Scroll delta (for Scroll events).
	/// </summary>
	public required Vector2 ScrollDelta { get; init; }

	/// <summary>
	/// Whether this event should bubble up to parent elements.
	/// Controls propagation behavior when wrapped with On&lt;ClayPointerEvent&gt;.
	/// </summary>
	public required bool Bubbles { get; init; }

	// Button convenience properties
	/// <summary>Whether the left mouse button is involved in this event.</summary>
	public bool IsLeftButton => Button.HasFlag(ClayMouseButton.Left);

	/// <summary>Whether the right mouse button is involved in this event.</summary>
	public bool IsRightButton => Button.HasFlag(ClayMouseButton.Right);

	/// <summary>Whether the middle mouse button is involved in this event.</summary>
	public bool IsMiddleButton => Button.HasFlag(ClayMouseButton.Middle);

	/// <summary>Whether mouse button 4 is involved in this event.</summary>
	public bool IsButton4 => Button.HasFlag(ClayMouseButton.Button4);

	/// <summary>Whether mouse button 5 is involved in this event.</summary>
	public bool IsButton5 => Button.HasFlag(ClayMouseButton.Button5);
}

/// <summary>
/// Platform-agnostic text input state resource.
/// Renderer/platform updates this resource, text input widgets consume it.
/// Similar to ClayPointerState, this allows the user to inject text input from any engine (Raylib, SDL, etc.).
/// </summary>
public class ClayTextInputState
{
	/// <summary>
	/// Queue of characters typed this frame.
	/// Populated by the platform/renderer, consumed by text input widgets.
	/// </summary>
	private readonly Queue<char> _charQueue = new();

	/// <summary>
	/// Whether backspace was pressed this frame.
	/// </summary>
	public bool BackspacePressed;

	/// <summary>
	/// Whether delete was pressed this frame.
	/// </summary>
	public bool DeletePressed;

	/// <summary>
	/// Whether enter/return was pressed this frame.
	/// </summary>
	public bool EnterPressed;

	/// <summary>
	/// Whether escape was pressed this frame.
	/// </summary>
	public bool EscapePressed;

	/// <summary>
	/// Whether left arrow was pressed this frame.
	/// </summary>
	public bool LeftPressed;

	/// <summary>
	/// Whether right arrow was pressed this frame.
	/// </summary>
	public bool RightPressed;

	/// <summary>
	/// Whether home key was pressed this frame.
	/// </summary>
	public bool HomePressed;

	/// <summary>
	/// Whether end key was pressed this frame.
	/// </summary>
	public bool EndPressed;

	/// <summary>
	/// Whether Ctrl+A (select all) was pressed this frame.
	/// </summary>
	public bool SelectAllPressed;

	/// <summary>
	/// Whether Ctrl+C (copy) was pressed this frame.
	/// </summary>
	public bool CopyPressed;

	/// <summary>
	/// Whether Ctrl+V (paste) was pressed this frame.
	/// </summary>
	public bool PastePressed;

	/// <summary>
	/// Whether Ctrl+X (cut) was pressed this frame.
	/// </summary>
	public bool CutPressed;

	/// <summary>
	/// Add a character to the input queue.
	/// Called by platform/renderer to inject typed characters.
	/// </summary>
	public void AddChar(char c)
	{
		_charQueue.Enqueue(c);
	}

	/// <summary>
	/// Get all characters typed this frame.
	/// Called by text input widgets to consume input.
	/// </summary>
	public IEnumerable<char> GetChars()
	{
		while (_charQueue.Count > 0)
		{
			yield return _charQueue.Dequeue();
		}
	}

	/// <summary>
	/// Check if there are any pending characters.
	/// </summary>
	public bool HasChars => _charQueue.Count > 0;

	/// <summary>
	/// Reset transient state (key presses, character queue).
	/// Called at the beginning of each frame.
	/// </summary>
	public void ResetTransientState()
	{
		_charQueue.Clear();
		BackspacePressed = false;
		DeletePressed = false;
		EnterPressed = false;
		EscapePressed = false;
		LeftPressed = false;
		RightPressed = false;
		HomePressed = false;
		EndPressed = false;
		SelectAllPressed = false;
		CopyPressed = false;
		PastePressed = false;
		CutPressed = false;
	}
}


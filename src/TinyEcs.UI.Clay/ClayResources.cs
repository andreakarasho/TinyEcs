using System.Numerics;
using Clay_cs;

namespace TinyEcs.UI.Clay;

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
	/// Whether the primary button (left mouse / touch) is currently down.
	/// </summary>
	public bool PrimaryDown;

	/// <summary>
	/// Whether the primary button was pressed this frame.
	/// </summary>
	public bool PrimaryPressed;

	/// <summary>
	/// Whether the primary button was released this frame.
	/// </summary>
	public bool PrimaryReleased;

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

	/// <summary>
	/// Stack of scroll deltas to accumulate this frame.
	/// Cleared at end of frame.
	/// </summary>

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
		PrimaryPressed = false;
		PrimaryReleased = false;
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
	/// Maximum number of UI elements. Default is 8192.
	/// </summary>
	public int MaxElementCount = 8192;

	/// <summary>
	/// Maximum number of words to cache for text measurement. Default is 16384.
	/// </summary>
	public int MaxMeasureTextCacheWordCount = 16384;

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
public struct ClayPointerEvent
{
	/// <summary>
	/// Type of pointer event.
	/// </summary>
	public ClayPointerEventType EventType;

	/// <summary>
	/// Pointer position in screen coordinates.
	/// </summary>
	public Vector2 Position;

	/// <summary>
	/// Pointer position in local element coordinates.
	/// </summary>
	public Vector2 LocalPosition;

	/// <summary>
	/// Whether primary button is down.
	/// </summary>
	public bool IsPrimaryButton;

	/// <summary>
	/// Scroll delta (for Scroll events).
	/// </summary>
	public Vector2 ScrollDelta;

	/// <summary>
	/// Whether this event should bubble up to parent elements.
	/// Controls propagation behavior when wrapped with On&lt;ClayPointerEvent&gt;.
	/// </summary>
	public bool Bubbles;
}


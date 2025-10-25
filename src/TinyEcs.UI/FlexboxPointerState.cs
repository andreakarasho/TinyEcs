using System.Numerics;

namespace TinyEcs.UI;

/// <summary>
/// Resource that accumulates pointer input state for Flexbox UI system.
/// Parallel to ClayPointerState. Reusable across frames.
///
/// Updated every frame by input system (e.g., Raylib plugin).
/// Consumed by FlexboxUiSystems.ApplyPointerInput to fire UiPointerEvent events.
/// </summary>
public sealed class FlexboxPointerState
{
    /// <summary>
    /// Current pointer position in screen coordinates.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Previous pointer position (for delta calculation).
    /// </summary>
    public Vector2 PreviousPosition { get; internal set; }

    /// <summary>
    /// Whether primary button (left mouse / touch) is currently down.
    /// </summary>
    public bool PrimaryDown { get; set; }

    /// <summary>
    /// Whether primary button was down last frame.
    /// </summary>
    public bool PreviousPrimaryDown { get; internal set; }

    /// <summary>
    /// Scroll delta accumulated this frame (positive Y = scroll up).
    /// Reset to zero after processing.
    /// </summary>
    public Vector2 ScrollDelta { get; private set; }

    /// <summary>
    /// Delta time for smooth scrolling.
    /// </summary>
    public float DeltaTime { get; set; }

    /// <summary>
    /// Whether drag scrolling is enabled (for scroll containers).
    /// </summary>
    public bool EnableDragScrolling { get; set; } = false;

    /// <summary>
    /// Position delta since last frame.
    /// </summary>
    public Vector2 MoveDelta => Position - PreviousPosition;

    /// <summary>
    /// Whether primary button was just pressed this frame.
    /// </summary>
    public bool IsPrimaryPressed => PrimaryDown && !PreviousPrimaryDown;

    /// <summary>
    /// Whether primary button was just released this frame.
    /// </summary>
    public bool IsPrimaryReleased => !PrimaryDown && PreviousPrimaryDown;

    /// <summary>
    /// Adds scroll delta (called by input system).
    /// </summary>
    public void AddScroll(Vector2 delta)
    {
        ScrollDelta += delta;
    }

    /// <summary>
    /// Marks the end of frame, saving current state as previous.
    /// Called by pointer processing system after events are dispatched.
    /// </summary>
    public void EndFrame()
    {
        PreviousPosition = Position;
        PreviousPrimaryDown = PrimaryDown;
        ScrollDelta = Vector2.Zero;
    }

    /// <summary>
    /// Resets all state to default.
    /// </summary>
    public void Reset()
    {
        Position = Vector2.Zero;
        PreviousPosition = Vector2.Zero;
        PrimaryDown = false;
        PreviousPrimaryDown = false;
        ScrollDelta = Vector2.Zero;
        DeltaTime = 0f;
        EnableDragScrolling = false;
    }
}

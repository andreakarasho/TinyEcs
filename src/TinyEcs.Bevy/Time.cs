namespace TinyEcs.Bevy;

/// <summary>
/// Engine clock resource. The host backend feeds it once per frame, early in
/// the frame (typically <c>Stage.Update</c> alongside the game tick), before
/// any system that reads it:
/// <code>
/// time.Frame = elapsedSeconds;
/// time.Total += time.Frame * 1000f;
/// </code>
/// Same contract as the input resources: the library never reads a wall
/// clock itself. <see cref="Total"/> is monotonic milliseconds from boot;
/// wall-clock APIs jump on system clock changes and break deterministic
/// replay, so consumers should use this instead.
/// </summary>
public sealed class Time
{
	/// <summary>Monotonic clock in milliseconds, accumulated from frame deltas.</summary>
	public float Total { get; set; }

	/// <summary>Last frame's delta in seconds.</summary>
	public float Frame { get; set; }
}

/// <summary>Registers the <see cref="Time"/> resource. Install-once; safe to add from multiple plugins.</summary>
public readonly struct TimePlugin : IPlugin
{
	public void Build(App app) => app.AddResource(new Time());
}

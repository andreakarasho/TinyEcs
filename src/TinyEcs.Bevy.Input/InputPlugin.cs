namespace TinyEcs.Bevy.Input;

/// <summary>
/// Registers <see cref="MouseInput"/> and <see cref="KeyboardInput"/> as app
/// resources. The host backend owns the per-frame feed: poll the OS/framework
/// device, call <c>SetSnapshot</c> then <c>Update</c> on each resource early
/// in the frame (typically <c>Stage.First</c>, single-threaded), before any
/// system that reads input.
/// </summary>
public readonly struct InputPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddResource(new MouseInput());
		app.AddResource(new KeyboardInput());
	}
}

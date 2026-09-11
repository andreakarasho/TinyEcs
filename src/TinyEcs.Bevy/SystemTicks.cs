namespace TinyEcs.Bevy;

/// <summary>
/// The change tick captured for the system currently running on THIS thread.
/// <para>
/// bevy_ecs hands each system run its own <c>Tick</c> through
/// <c>SystemMeta</c>. TinyEcs' <see cref="ISystemParam.Fetch"/> seam only
/// carries the <see cref="App"/>, and reading <c>World.CurrentTick</c> inside
/// <c>Fetch</c> would be wrong under parallel execution — a sibling system of
/// the same batch can bump the counter between two params of the same system,
/// leaving them with different windows. So the scheduler captures the tick ONCE
/// per run (<see cref="Advance"/>, called from
/// <see cref="SystemDescriptor.RunProfiled"/>) into this thread-static slot, and
/// every param of that run reads the same value from it.
/// </para>
/// <para>
/// Thread-static is sound because a system descriptor runs start-to-finish on
/// one thread: the batch drain hands a whole descriptor to a single worker.
/// </para>
/// </summary>
public static class SystemTicks
{
	[ThreadStatic]
	private static uint s_current;

	/// <summary>
	/// The change tick of the system run in progress on this thread. Valid for
	/// the duration of a system body, an observer flush, or an OnEnter/OnExit
	/// handler.
	/// </summary>
	public static uint Current => s_current;

	/// <summary>
	/// Bump the world's change tick and publish the new value as this thread's
	/// current system tick. Called once per system run and once per observer
	/// flush.
	/// </summary>
	public static uint Advance(TinyEcs.World world) => s_current = world.BumpTick();
}

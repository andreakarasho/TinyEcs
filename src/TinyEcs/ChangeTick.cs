namespace TinyEcs;

/// <summary>
/// Wrapping arithmetic over the world's 32-bit change tick, ported from
/// bevy_ecs' <c>Tick</c>.
/// <para>
/// The world tick advances once per SYSTEM RUN (plus once per deferred-command
/// flush and once per observer flush), not once per frame. At ~300 systems a
/// frame and 1000 fps that is ~300 kHz, which wraps <see cref="uint"/> in a few
/// hours of uptime. So nothing here may compare tick MAGNITUDES: every question
/// is asked as a comparison of wrapping AGES relative to "now", and stale ticks
/// are periodically clamped forward by <see cref="World.CheckChangeTicks"/>
/// before they can wrap all the way around into looking fresh.
/// </para>
/// </summary>
public static class ChangeTick
{
	/// <summary>
	/// How far the world tick may advance between two
	/// <see cref="World.CheckChangeTicks"/> passes. Bevy uses 518_400_000; 2^28
	/// (~268M) triggers a pass roughly every 15 minutes at 300 kHz, which keeps
	/// the O(rows) walk rare while leaving a wide safety margin.
	/// </summary>
	public const uint CheckTickThreshold = 1u << 28;

	/// <summary>
	/// Ticks are only meaningful up to this age. Beyond it they are clamped —
	/// both on read (<see cref="InWindow"/>) and in storage
	/// (<see cref="World.CheckChangeTicks"/>) — so an ancient tick reads as
	/// "not newer than anything" instead of wrapping into "newer than now".
	/// Mirrors Bevy's <c>MAX_CHANGE_AGE = u32::MAX - (2 * CHECK_TICK_THRESHOLD - 1)</c>:
	/// the gap leaves room for one full threshold of drift before AND after a pass.
	/// </summary>
	public const uint MaxChangeAge = uint.MaxValue - (2 * CheckTickThreshold - 1);

	/// <summary>
	/// Wrapping age of <paramref name="tick"/> relative to <paramref name="now"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint Age(uint tick, uint now) => now - tick;

	/// <summary>
	/// Bevy's <c>Tick::is_newer_than</c>: true when <paramref name="tick"/> falls
	/// in the window <c>(lastRun, thisRun]</c>.
	/// <para>
	/// Phrased as "was it stamped more recently than the consumer last ran" —
	/// both ages measured from <paramref name="thisRun"/> and clamped to
	/// <see cref="MaxChangeAge"/>. The clamp is what makes this correct across
	/// the wrap: a window can legitimately be wider than 2^31 ticks (a system
	/// gated out for a long time, or a <c>lastRun</c> that has itself been
	/// clamped), which a signed-difference comparison gets backwards.
	/// </para>
	/// <para>
	/// Equal-to-<paramref name="thisRun"/> counts (the flush that just ran is
	/// visible); equal-to-<paramref name="lastRun"/> does not (nothing is ever
	/// reported twice).
	/// </para>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool InWindow(uint tick, uint lastRun, uint thisRun)
	{
		var sinceStamp = Math.Min(Age(tick, thisRun), MaxChangeAge);
		var sinceLastRun = Math.Min(Age(lastRun, thisRun), MaxChangeAge);
		return sinceLastRun > sinceStamp;
	}

	/// <summary>
	/// Open-ended form of <see cref="InWindow"/> for consumers that keep only a
	/// "since" tick and always read up to the present: true when
	/// <paramref name="tick"/> is strictly newer than <paramref name="since"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsNewerThan(uint tick, uint since, uint now)
		=> InWindow(tick, since, now);

	/// <summary>
	/// Clamp a stored tick that has aged past <see cref="MaxChangeAge"/> forward
	/// to exactly that age. Keeps it comparable (and keeps it from wrapping into
	/// the future) while preserving "old".
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint ClampAge(uint tick, uint now)
		=> Age(tick, now) > MaxChangeAge ? now - MaxChangeAge : tick;
}

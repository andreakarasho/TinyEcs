using System;
using System.Collections.Generic;

namespace TinyEcs.Bevy;

/// <summary>
/// Opt-in per-system CPU profiler. Times every <see cref="ISystem.Run"/> on the
/// per-frame stages and dumps an aggregated table (sorted by total CPU) on an
/// interval, then resets.
/// </summary>
/// <remarks>
/// Bevy parity: Bevy wraps each system run in a <c>tracing</c> span named after
/// the system and lets a subscriber (tracing-chrome / Tracy) aggregate. There is
/// no tracing layer here, so we time <see cref="System.Diagnostics.Stopwatch"/>
/// ticks around the run and aggregate ourselves.
///
/// Registered as a resource by the App; read it with Res/ResMut&lt;SystemProfiler&gt;.
/// Cost when disabled: a resource fetch + bool check per system run. Enable with env
/// <c>TINYECS_PROFILE=1</c> (the <see cref="Enabled"/> default) or by setting it on the resource.
/// </remarks>
public sealed class SystemProfiler
{
	/// <summary>Master switch. Defaults from env <c>TINYECS_PROFILE=1</c>.</summary>
	public bool Enabled = Environment.GetEnvironmentVariable("TINYECS_PROFILE") == "1";

	/// <summary>Seconds between dumps. Counters reset after each dump.</summary>
	public double ReportIntervalSeconds = 5.0;

	/// <summary>Top-N rows per dump (by total CPU). 0 = all.</summary>
	public int TopN = 25;

	// Layout diagnostics — set by the UI LayoutSystem each run when Enabled,
	// surfaced in the dump. Node count is the linear driver of layout CPU.
	public int LayoutRoots;
	public int LayoutNodes;
	public int LayoutCulled;

	// Frames the layout pass skipped (input fingerprint unchanged) since the
	// last dump. High skip counts on a static screen = the gate is working.
	public int LayoutSkipped;

	// Bit mask of the fingerprint groups that differed on the LAST relayout
	// (bit index = group order in LayoutSystem.ComputeGroupHashes). A gate
	// stuck open shows the flapping input here.
	public int LayoutDirtyMask;

	// Split of the layout system's last-frame cost: the entity tree walk
	// (EmitNode: TryGet + BuildDecl) vs Clay's EndLayout solve. Stopwatch ticks.
	public long LayoutWalkTicks;
	public long LayoutSolveTicks;

	// Finer split inside the walk (summed over all nodes this frame): building
	// the declaration (ResolveZ + BuildDecl = my component lookups) vs Clay's
	// ConfigureOpenElement interop. Isolates whether per-node cost is lookups.
	public long LayoutBuildTicks;
	public long LayoutConfigTicks;

	/// <summary>
	/// Frame-spike capture. A frame whose wall time exceeds this dumps a one-line
	/// per-system breakdown of THAT frame (+ GC deltas) instead of hiding in the
	/// window average. Env <c>TINYECS_SPIKE_MS</c> overrides; 0 disables.
	/// </summary>
	public double SpikeThresholdMs = double.TryParse(Environment.GetEnvironmentVariable("TINYECS_SPIKE_MS"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 16.0;

	/// <summary>Max systems listed per spike line.</summary>
	public int SpikeTopN = 8;

	// Per-window frame wall times (ms), drained into the report as p50/p95/p99/max.
	internal readonly List<double> FrameMs = new(16384);
	internal int FramesOver16, FramesOver33, FramesOver100;
}

/// <summary>
/// Captures the profiler's per-window tables for the host to drain through the ECS
/// resource graph (e.g. a system logging via <c>Res&lt;ILogger&gt;</c>) instead of a
/// static sink. Register one (<c>app.AddResource(new SystemProfileReport())</c>) to opt
/// in; absent, the dump falls back to <see cref="System.Console"/> (standalone default).
/// </summary>
public sealed class SystemProfileReport
{
	private readonly System.Collections.Concurrent.ConcurrentQueue<string> _tables = new();

	/// <summary>Called by the scheduler when a profile window closes.</summary>
	public void Publish(string table) => _tables.Enqueue(table);

	/// <summary>Drain one pending table; false when none left. Host loops until false.</summary>
	public bool TryRead(out string table) => _tables.TryDequeue(out table!);
}

/// <summary>Recovers a readable "Type.Method" label from a system delegate (cold path, registration only).</summary>
internal static class SystemName
{
	public static string Of(Delegate d)
	{
		var m = d.Method;
		var t = m.DeclaringType;
		return t is null ? m.Name : t.Name + "." + m.Name;
	}
}

// Opt-in per-mod cost census (env TINYECS_MOD_PROFILE=1): per mod and per system /
// observer / packet direction / host function — calls, rows and bytes pushed, host-side
// build time (query snapshot + JSON + FlatBuffers), guest call time (includes any
// re-entrant host function work), command apply time. Printed every ~5 s as per-frame
// averages, then reset. Off = one static bool test per call site.

using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace TinyEcs.Bevy.Modding;

internal static class ModProfiler
{
    public static readonly bool Enabled = Environment.GetEnvironmentVariable("TINYECS_MOD_PROFILE") == "1";

    internal sealed class Stat
    {
        public long Calls, Skipped, Rows, BytesIn, BytesOut, BuildTicks, GuestTicks, ApplyTicks;
    }

    private sealed class ModStats
    {
        public readonly Dictionary<string, Stat> Entries = new(StringComparer.Ordinal);
        public readonly long[] PacketIds = new long[512];
    }

    private static readonly Dictionary<string, ModStats> s_mods = new(StringComparer.Ordinal);
    private static long s_windowStart = Stopwatch.GetTimestamp();
    private static double s_lastFrameTime = double.NaN;
    private static long s_frames;

    public static long Now() => Stopwatch.GetTimestamp();

    // "<short system name> {Q(term,term) R(path) E(path)}" — closure-named systems are
    // only identifiable by what they read.
    public static string Label(ModSystemSpec sys)
    {
        var name = sys.Name;
        var cut = name.LastIndexOf("::", StringComparison.Ordinal);
        var head = cut > 0 ? name.LastIndexOf("::", cut - 1, StringComparison.Ordinal) : -1;
        var sb = new StringBuilder(head > 0 ? name[(head + 2)..] : name).Append(" {");
        foreach (var p in sys.Params)
        {
            switch (p.Kind)
            {
                case ModParamKind.Query:
                    sb.Append(" Q(");
                    foreach (var t in p.Query!.Terms)
                        sb.Append(t.kind switch { ModQueryTermKind.Changed => "chg:", ModQueryTermKind.Added => "add:", ModQueryTermKind.Without => "!", ModQueryTermKind.With => "w:", _ => "" })
                          .Append(Short(t.typePath)).Append(',');
                    sb.Length--;
                    sb.Append(')');
                    break;
                case ModParamKind.Res:
                case ModParamKind.ResMut:
                    sb.Append(p.Kind == ModParamKind.ResMut ? " RM(" : " R(").Append(Short(p.TypePath!)).Append(')');
                    break;
                case ModParamKind.Events:
                    sb.Append(" E(").Append(Short(p.TypePath!)).Append(')');
                    break;
            }
        }
        return sb.Append(" }").ToString();
    }

    private static string Short(string path)
    {
        var i = path.IndexOf(':');
        return i >= 0 ? path[(i + 1)..] : path;
    }

    public static Stat Get(string mod, string key)
    {
        if (!s_mods.TryGetValue(mod, out var m))
            s_mods[mod] = m = new ModStats();
        if (!m.Entries.TryGetValue(key, out var s))
            m.Entries[key] = s = new Stat();
        return s;
    }

    public static void Packet(string mod, ModPacketDirection dir, byte id, int bytes, long ticks)
    {
        var s = Get(mod, dir == ModPacketDirection.Incoming ? "packet:in" : "packet:out");
        s.Calls++;
        s.BytesIn += bytes;
        s.GuestTicks += ticks;
        s_mods[mod].PacketIds[(int)dir * 256 + id]++;
    }

    // Called once per mod system run with the engine clock; a new value = a new frame.
    public static void Frame(double timeTotal)
    {
        if (timeTotal == s_lastFrameTime)
            return;
        s_lastFrameTime = timeTotal;
        s_frames++;
        var now = Stopwatch.GetTimestamp();
        if (now - s_windowStart < Stopwatch.Frequency * 5)
            return;
        Report(now);
        s_windowStart = now;
        s_frames = 0;
    }

    private static void Report(long now)
    {
        if (s_frames == 0)
            return;
        var ms = 1000.0 / Stopwatch.Frequency;
        var f = (double)s_frames;
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendFormat(inv, "[mod-profile] {0} frames in {1:F1}s\n", s_frames, (now - s_windowStart) * ms / 1000.0);
        foreach (var (mod, m) in s_mods)
        {
            double total = 0;
            // fn:* host-function time runs INSIDE a guest call already counted.
            foreach (var (key, s) in m.Entries)
                if (!key.StartsWith("fn:", StringComparison.Ordinal))
                    total += (s.BuildTicks + s.GuestTicks + s.ApplyTicks) * ms;
            sb.AppendFormat(inv, "[mod-profile] {0}: {1:F3} ms/frame total\n", mod, total / f);
            var list = new List<KeyValuePair<string, Stat>>(m.Entries);
            list.Sort((a, b) => (b.Value.BuildTicks + b.Value.GuestTicks + b.Value.ApplyTicks)
                .CompareTo(a.Value.BuildTicks + a.Value.GuestTicks + a.Value.ApplyTicks));
            foreach (var (key, s) in list)
            {
                if (s.Calls == 0 && s.Skipped == 0)
                    continue;
                sb.AppendFormat(inv,
                    "[mod-profile]   {0,-40} calls/f {1,6:F2} skip/f {2,5:F2} rows/f {3,7:F1} in/f {4,7:F0}B out/f {5,6:F0}B build {6:F3} guest {7:F3} apply {8:F3} ms/f\n",
                    key, s.Calls / f, s.Skipped / f, s.Rows / f, s.BytesIn / f, s.BytesOut / f,
                    s.BuildTicks * ms / f, s.GuestTicks * ms / f, s.ApplyTicks * ms / f);
            }
            AppendTopIds(sb, m.PacketIds, 0, "in", f);
            AppendTopIds(sb, m.PacketIds, 256, "out", f);
            m.Entries.Clear();
            Array.Clear(m.PacketIds);
        }
        Console.Write(sb.ToString());
    }

    private static void AppendTopIds(StringBuilder sb, long[] ids, int baseIdx, string dir, double frames)
    {
        var top = new List<(int Id, long N)>();
        for (var i = 0; i < 256; i++)
            if (ids[baseIdx + i] > 0)
                top.Add((i, ids[baseIdx + i]));
        if (top.Count == 0)
            return;
        top.Sort((a, b) => b.N.CompareTo(a.N));
        sb.Append("[mod-profile]   packet ids ").Append(dir).Append(" (per frame):");
        for (var i = 0; i < Math.Min(8, top.Count); i++)
            sb.AppendFormat(CultureInfo.InvariantCulture, " 0x{0:X2}={1:F2}", top[i].Id, top[i].N / frames);
        sb.Append('\n');
    }
}

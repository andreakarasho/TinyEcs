// Message interception chain: a host's network layer hands each message (incoming or
// outgoing, the full wire bytes, id first) to the mods' packet observers (trigger
// `on-packet(filter)`). Mods run in load order, a mod's packet observers in declaration
// order; each sees the previous one's replacement, and a block stops the chain. A
// message a mod injected itself skips every packet observer of that mod. The lib
// assigns no meaning to the bytes beyond "byte 0 is the id the filters are keyed by".
//
// A mod's interest is the union of its packet observers' filters (ModHostContext.
// PacketInterest), so a mod-less or interest-less set costs one bit test per message.
// Drive Run ONLY from a single-threaded host system that is not inside a mod call (it
// re-enters guests).

namespace TinyEcs.Bevy.Modding;

public enum ModPacketDirection : byte
{
    Incoming = 0,
    Outgoing = 1,
}

public enum ModPacketVerdict : byte
{
    Pass = 0,
    Block = 1,
    Replace = 2,
}

/// Registered as a resource by <see cref="ModdingPlugin"/>.
public sealed class ModPacketChain
{
    internal ModRuntimes? Runtimes;
    // Ping-pong copies of the current replacement: the next observer's input must outlive
    // the previous one's reply buffer, and executors may share reply scratch across mods.
    private byte[] _a = new byte[512], _b = new byte[512];

    internal static bool Interested(ModHostContext mod, ModPacketDirection dir, byte id)
        => (mod.PacketInterest[(int)dir * 4 + (id >> 6)] & (1UL << (id & 63))) != 0;

    /// True when some enabled mod observes `id` in `dir` — the cheap pre-check a
    /// network layer makes before building anything.
    public bool Wants(ModPacketDirection dir, byte id)
    {
        var runtimes = Runtimes;
        if (runtimes == null)
            return false;
        foreach (var rt in runtimes.Runtimes)
            if (rt.Enabled && Interested(rt.Ctx, dir, id))
                return true;
        return false;
    }

    /// True when some enabled mod observes anything in `dir` — the gate a network
    /// layer uses to install (or drop) its per-message hook.
    public bool WantsAny(ModPacketDirection dir)
    {
        var runtimes = Runtimes;
        if (runtimes == null)
            return false;
        var baseWord = (int)dir * 4;
        foreach (var rt in runtimes.Runtimes)
        {
            if (!rt.Enabled)
                continue;
            var bits = rt.Ctx.PacketInterest;
            if ((bits[baseWord] | bits[baseWord + 1] | bits[baseWord + 2] | bits[baseWord + 3]) != 0)
                return true;
        }
        return false;
    }

    /// Runs the chain over `packet`. Every packet observer of `injector` (the mod that
    /// injected this message, or null) is skipped. Returns Pass (forward `packet`
    /// unchanged), Block (drop it), or Replace (forward `result` instead — valid until
    /// the next Run). An observer that fails counts toward its mod's auto-disable budget
    /// and passes the message on unchanged.
    public ModPacketVerdict Run(ModPacketDirection dir, ReadOnlySpan<byte> packet, ModHostContext? injector, out ReadOnlySpan<byte> result)
    {
        result = default;
        var runtimes = Runtimes;
        if (runtimes == null || packet.IsEmpty)
            return ModPacketVerdict.Pass;

        var current = packet;
        var replaced = false;
        var useA = true;
        // Index loops: a failure can't add/remove runtimes or observers, but keep it
        // allocation-free.
        for (var i = 0; i < runtimes.Runtimes.Count; i++)
        {
            var rt = runtimes.Runtimes[i];
            if (!rt.Enabled || ReferenceEquals(rt.Ctx, injector) || !Interested(rt.Ctx, dir, current[0]))
                continue;

            var observers = rt.Ctx.PacketObservers;
            // rt.Enabled again: a failure below may have just disabled the mod.
            for (var j = 0; j < observers.Count && rt.Enabled; j++)
            {
                var obs = observers[j];
                if (!obs.SeesPacket(dir, current[0]))
                    continue;

                ModPacketVerdict verdict;
                ReadOnlySpan<byte> replacement;
                var t0 = ModProfiler.Enabled ? ModProfiler.Now() : 0;
                try
                {
                    verdict = rt.Instance.CallPacketObserver(obs.Name, dir, current, out replacement);
                }
                catch (Exception e)
                {
                    ModdingPlugin.NoteFailure(rt, $"packet observer '{obs.Name}'", e);
                    continue;
                }
                if (ModProfiler.Enabled)
                    ModProfiler.Packet(rt.Ctx.Name, dir, current[0], current.Length, ModProfiler.Now() - t0);

                if (verdict == ModPacketVerdict.Block)
                    return ModPacketVerdict.Block;
                if (verdict != ModPacketVerdict.Replace || replacement.IsEmpty)
                    continue;

                ref var buf = ref useA ? ref _a : ref _b;
                if (buf.Length < replacement.Length)
                    buf = new byte[Math.Max(replacement.Length, buf.Length * 2)];
                replacement.CopyTo(buf);
                current = buf.AsSpan(0, replacement.Length);
                useA = !useA;
                replaced = true;
            }
        }

        if (!replaced)
            return ModPacketVerdict.Pass;
        result = current;
        return ModPacketVerdict.Replace;
    }
}

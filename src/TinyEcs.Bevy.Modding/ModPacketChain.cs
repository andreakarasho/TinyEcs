// Message interception chain: a host's network layer hands each message (incoming or
// outgoing, the full wire bytes, id first) to the mods that asked for that id. Mods
// run in load order; each sees the previous one's replacement, and a block stops the
// chain. A message a mod injected itself skips that mod. The lib assigns no meaning to
// the bytes beyond "byte 0 is the id the interest set is keyed by".
//
// Interest is opt-in (Intercept), stored per mod context, so a mod-less or
// interest-less set costs one bit test per message. Drive Run ONLY from a
// single-threaded host system that is not inside a mod call (it re-enters guests).

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
    // Ping-pong copies of the current replacement: the next mod's input must outlive
    // that mod's own reply buffer, and executors may share reply scratch across mods.
    private byte[] _a = new byte[512], _b = new byte[512];

    /// Adds `ids` to the mod's interest for `dir`. Cleared when the mod reloads.
    public static void Intercept(ModHostContext mod, ModPacketDirection dir, ReadOnlySpan<byte> ids)
    {
        var bits = mod.PacketInterest;
        var baseWord = (int)dir * 4;
        foreach (var id in ids)
            bits[baseWord + (id >> 6)] |= 1UL << (id & 63);
    }

    internal static bool Interested(ModHostContext mod, ModPacketDirection dir, byte id)
        => (mod.PacketInterest[(int)dir * 4 + (id >> 6)] & (1UL << (id & 63))) != 0;

    /// True when some enabled mod intercepts `id` in `dir` — the cheap pre-check a
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

    /// True when some enabled mod intercepts anything in `dir` — the gate a network
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

    /// Runs the chain over `packet`. `injector` (the mod that injected this message, or
    /// null) is skipped. Returns Pass (forward `packet` unchanged), Block (drop it), or
    /// Replace (forward `result` instead — valid until the next Run). A mod that fails
    /// counts toward its auto-disable budget and passes the message on unchanged.
    public ModPacketVerdict Run(ModPacketDirection dir, ReadOnlySpan<byte> packet, ModHostContext? injector, out ReadOnlySpan<byte> result)
    {
        result = default;
        var runtimes = Runtimes;
        if (runtimes == null || packet.IsEmpty)
            return ModPacketVerdict.Pass;

        var current = packet;
        var replaced = false;
        var useA = true;
        // Index loop: a mod failure can't add/remove runtimes, but keep it allocation-free.
        for (var i = 0; i < runtimes.Runtimes.Count; i++)
        {
            var rt = runtimes.Runtimes[i];
            if (!rt.Enabled || ReferenceEquals(rt.Ctx, injector) || !Interested(rt.Ctx, dir, current[0]))
                continue;

            ModPacketVerdict verdict;
            ReadOnlySpan<byte> replacement;
            try
            {
                verdict = rt.Instance.OnPacket(dir, current, out replacement);
            }
            catch (Exception e)
            {
                ModdingPlugin.NoteFailure(rt, "on-packet", e);
                continue;
            }

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

        if (!replaced)
            return ModPacketVerdict.Pass;
        result = current;
        return ModPacketVerdict.Replace;
    }
}

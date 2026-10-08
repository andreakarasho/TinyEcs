using System.Text.Json;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// The generic message interception chain (ModPacketChain) over packet observers, and
// the host-function table (ModHostFunctions). Fake IModInstances stand in for guests —
// no wasm runtime.
public class ModPacketChainTests
{
    private sealed class PacketMod(string name, Func<string, byte[], (ModPacketVerdict, byte[]?)> onPacket) : IModInstance
    {
        public readonly string Name = name;
        public readonly List<(string Observer, byte[] Packet)> Seen = new();
        private byte[] _reply = Array.Empty<byte>();

        public PacketMod(string name, Func<byte[], (ModPacketVerdict, byte[]?)> onPacket)
            : this(name, (_, p) => onPacket(p)) { }

        public void Setup() { }
        public void RunSystem(ModSystemSpec sys) { }
        public void CallObserver(string export, ulong entity, string json) { }
        public void Reload(in ModSource source) { }
        public void Dispose() { }

        public ModPacketVerdict CallPacketObserver(string export, ModPacketDirection dir, ReadOnlySpan<byte> packet, out ReadOnlySpan<byte> replacement)
        {
            Seen.Add((export, packet.ToArray()));
            var (verdict, bytes) = onPacket(export, packet.ToArray());
            _reply = bytes ?? Array.Empty<byte>();
            replacement = _reply;
            return verdict;
        }
    }

    private static (ModPacketChain Chain, ModRuntimes Runtimes) Chain(World world, params PacketMod[] mods)
    {
        var runtimes = new ModRuntimes();
        var slot = 0;
        foreach (var m in mods)
        {
            var ctx = new ModHostContext { World = world, Registry = new ModComponentRegistry(), Name = m.Name, Slot = slot++ };
            runtimes.Runtimes.Add(new ModRuntime { Manifest = new ModManifest { Name = m.Name }, Instance = m, Ctx = ctx });
        }
        return (new ModPacketChain { Runtimes = runtimes }, runtimes);
    }

    private static void Observe(ModRuntime rt, string name, ModPacketDirection dir, params byte[] ids)
        => new AppImpl(rt.Ctx).AddPacketObserver(name, dir, ids);

    private static readonly Func<byte[], (ModPacketVerdict, byte[]?)> Pass = _ => (ModPacketVerdict.Pass, null);

    [Fact]
    public void Only_observers_whose_filter_matches_the_id_and_direction_are_called()
    {
        using var world = new World();
        var a = new PacketMod("a", Pass);
        var b = new PacketMod("b", Pass);
        var (chain, rts) = Chain(world, a, b);
        Observe(rts.Runtimes[0], "in", ModPacketDirection.Incoming, 0x1C);
        Observe(rts.Runtimes[1], "out", ModPacketDirection.Outgoing, 0x1C);

        Assert.True(chain.Wants(ModPacketDirection.Incoming, 0x1C));
        Assert.False(chain.Wants(ModPacketDirection.Incoming, 0x1D));
        Assert.True(chain.WantsAny(ModPacketDirection.Outgoing));

        Assert.Equal(ModPacketVerdict.Pass, chain.Run(ModPacketDirection.Incoming, new byte[] { 0x1C, 1 }, null, out _));
        Assert.Single(a.Seen);
        Assert.Empty(b.Seen);

        chain.Run(ModPacketDirection.Incoming, new byte[] { 0x1D, 1 }, null, out _);
        Assert.Single(a.Seen);
    }

    [Fact]
    public void An_empty_id_filter_sees_every_id_in_its_direction()
    {
        using var world = new World();
        var all = new PacketMod("all", Pass);
        var (chain, rts) = Chain(world, all);
        Observe(rts.Runtimes[0], "every", ModPacketDirection.Outgoing);

        Assert.True(chain.Wants(ModPacketDirection.Outgoing, 0x00));
        Assert.True(chain.Wants(ModPacketDirection.Outgoing, 0xFF));
        Assert.False(chain.WantsAny(ModPacketDirection.Incoming));

        chain.Run(ModPacketDirection.Outgoing, new byte[] { 0xEF }, null, out _);
        chain.Run(ModPacketDirection.Incoming, new byte[] { 0xEF }, null, out _);
        Assert.Single(all.Seen);
    }

    [Fact]
    public void A_mods_observers_run_in_declaration_order_each_with_its_own_filter()
    {
        using var world = new World();
        var mod = new PacketMod("m", (obs, p) => obs switch
        {
            "bump" => (ModPacketVerdict.Replace, new byte[] { p[0], (byte)(p[1] + 1) }),
            "double" => (ModPacketVerdict.Replace, new byte[] { p[0], (byte)(p[1] * 2) }),
            _ => (ModPacketVerdict.Block, null),
        });
        var (chain, rts) = Chain(world, mod);
        Observe(rts.Runtimes[0], "bump", ModPacketDirection.Incoming, 0x20);
        Observe(rts.Runtimes[0], "other-id", ModPacketDirection.Incoming, 0x21);
        Observe(rts.Runtimes[0], "double", ModPacketDirection.Incoming, 0x20);

        var verdict = chain.Run(ModPacketDirection.Incoming, new byte[] { 0x20, 3 }, null, out var result);

        Assert.Equal(ModPacketVerdict.Replace, verdict);
        Assert.Equal(new byte[] { 0x20, 8 }, result.ToArray());
        Assert.Equal(new[] { "bump", "double" }, mod.Seen.Select(s => s.Observer));
    }

    [Fact]
    public void Replacements_chain_in_load_order_and_block_stops_the_chain()
    {
        using var world = new World();
        var first = new PacketMod("first", p => (ModPacketVerdict.Replace, new byte[] { p[0], (byte)(p[1] + 1) }));
        var second = new PacketMod("second", p => (ModPacketVerdict.Replace, new byte[] { p[0], (byte)(p[1] * 10) }));
        var (chain, rts) = Chain(world, first, second);
        foreach (var rt in rts.Runtimes)
            Observe(rt, "o", ModPacketDirection.Incoming, 0xAE);

        var verdict = chain.Run(ModPacketDirection.Incoming, new byte[] { 0xAE, 2 }, null, out var result);

        Assert.Equal(ModPacketVerdict.Replace, verdict);
        Assert.Equal(new byte[] { 0xAE, 30 }, result.ToArray());
        Assert.Equal(new byte[] { 0xAE, 3 }, second.Seen[0].Packet); // saw first's replacement

        // A blocking mod first: the second never sees the message.
        var blocker = new PacketMod("blocker", _ => (ModPacketVerdict.Block, null));
        var after = new PacketMod("after", Pass);
        var (chain2, rts2) = Chain(world, blocker, after);
        foreach (var rt in rts2.Runtimes)
            Observe(rt, "o", ModPacketDirection.Outgoing, 0x02);
        Assert.Equal(ModPacketVerdict.Block, chain2.Run(ModPacketDirection.Outgoing, new byte[] { 0x02 }, null, out _));
        Assert.Empty(after.Seen);
    }

    [Fact]
    public void An_injected_message_skips_all_its_injectors_observers_and_disabled_mods()
    {
        using var world = new World();
        var injector = new PacketMod("injector", _ => (ModPacketVerdict.Block, null));
        var other = new PacketMod("other", Pass);
        var off = new PacketMod("off", _ => (ModPacketVerdict.Block, null));
        var (chain, rts) = Chain(world, injector, other, off);
        foreach (var rt in rts.Runtimes)
            Observe(rt, "o", ModPacketDirection.Outgoing, 0x06);
        Observe(rts.Runtimes[0], "every", ModPacketDirection.Outgoing);
        rts.Runtimes[2].Enabled = false;

        var verdict = chain.Run(ModPacketDirection.Outgoing, new byte[] { 0x06, 0 }, rts.Runtimes[0].Ctx, out _);

        Assert.Equal(ModPacketVerdict.Pass, verdict);
        Assert.Empty(injector.Seen);
        Assert.Single(other.Seen);
        Assert.Empty(off.Seen);
    }

    [Fact]
    public void A_failing_observer_passes_the_message_on_and_counts_a_failure()
    {
        using var world = new World();
        var bad = new PacketMod("bad", _ => throw new InvalidOperationException("guest trap"));
        var good = new PacketMod("good", Pass);
        var (chain, rts) = Chain(world, bad, good);
        foreach (var rt in rts.Runtimes)
            Observe(rt, "o", ModPacketDirection.Incoming, 0x11);

        Assert.Equal(ModPacketVerdict.Pass, chain.Run(ModPacketDirection.Incoming, new byte[] { 0x11 }, null, out _));
        Assert.Single(good.Seen);
        Assert.Equal(1, rts.Runtimes[0].FailureCount);
    }

    // ── ModHostFunctions ────────────────────────────────────────────────────────

    [Fact]
    public void Host_function_round_trip_unit_result_and_traps()
    {
        using var world = new World();
        var ctx = new ModHostContext { World = world, Registry = new ModComponentRegistry(), Name = "m" };
        var fns = new ModHostFunctions();
        fns.Add("t:a/b#add", (mod, a, w) => w.WriteNumberValue(a[0].GetInt32() + a[1].GetInt32()));
        var sideEffect = "";
        fns.Add("t:a/b#note", (mod, a, w) => sideEffect = a[0].GetString()!);

        Assert.Equal("5", fns.Call(ctx, "t:a/b#add", "[2,3]"));
        Assert.Null(fns.Call(ctx, "t:a/b#note", "[\"hi\"]"));
        Assert.Equal("hi", sideEffect);
        Assert.True(fns.Call(ctx, "t:a/b#add"u8, "[1,1]"u8).SequenceEqual("2"u8));

        Assert.Throws<ModCallException>(() => fns.Call(ctx, "t:a/b#missing", "[]"));
        Assert.Throws<ModCallException>(() => fns.Call(ctx, "t:a/b#add", "{not json"));
        Assert.Throws<ModCallException>(() => fns.Call(ctx, "t:a/b#add", "{}"));
        Assert.Throws<ModCallException>(() => fns.Call(ctx, "t:a/b#add", "[\"x\",1]"));
        Assert.Throws<ModCallException>(() => fns.Call(ctx, "t:a/b#add", "[1]"));
    }
}

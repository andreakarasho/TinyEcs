using System.Text.Json.Serialization;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// End-to-end round trip through the component-model backend: the real ModdingPlugin
// loads fixtures/component_guest.wasm (a wasm32-wasip2 component built from
// fixtures/component-guest against abi/tinyecs-mod.wit — rebuild with its build.sh),
// routes it by its preamble to ComponentModBackend, and runs it through the stage
// scheduler. See fixtures/component-guest/src/lib.rs for what the guest does.
public struct CmPos { public int X { get; set; } public int Y { get; set; } }
public struct CmVel { public int X { get; set; } public int Y { get; set; } }
public struct CmFrozen { }
// Not a zero-size tag: TinyEcs fires no OnInsert for tags, and the guest observes this one.
public struct CmTag { public int V { get; set; } }
public struct CmSeen { public long Value { get; set; } }
public sealed class CmScore { public long Value { get; set; } }
public sealed class CmAdded { public long Value { get; set; } }
public struct CmPing { public int N { get; set; } }

[JsonSerializable(typeof(CmPos))]
[JsonSerializable(typeof(CmVel))]
[JsonSerializable(typeof(CmFrozen))]
[JsonSerializable(typeof(CmTag))]
[JsonSerializable(typeof(CmSeen))]
[JsonSerializable(typeof(CmScore))]
[JsonSerializable(typeof(CmAdded))]
[JsonSerializable(typeof(CmPing))]
internal partial class CmJsonContext : JsonSerializerContext { }

public sealed class ComponentModBackendTests : IDisposable
{
    private readonly string _modFolder = Path.Combine(Path.GetTempPath(), "tinyecs-cm-" + Guid.NewGuid().ToString("N"));

    public ComponentModBackendTests()
    {
        var dir = Path.Combine(_modFolder, "component-guest");
        Directory.CreateDirectory(dir);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "fixtures", "component_guest.wasm"), Path.Combine(dir, "mod.wasm"));
        File.WriteAllText(Path.Combine(dir, "mod.json"), """{ "name": "component-guest", "version": "0.1.0", "wasm": "mod.wasm" }""");
    }

    public void Dispose()
    {
        try { Directory.Delete(_modFolder, recursive: true); } catch (IOException) { }
    }

    private App NewApp(bool posReadOnly = false)
    {
        var reg = new ModComponentRegistry();
        reg.Register("test/pos", new ModComponent<CmPos>(CmJsonContext.Default.CmPos, readOnly: posReadOnly));
        reg.Register("test/vel", new ModComponent<CmVel>(CmJsonContext.Default.CmVel));
        reg.Register("test/frozen", new ModComponent<CmFrozen>(CmJsonContext.Default.CmFrozen));
        reg.Register("test/tag", new ModComponent<CmTag>(CmJsonContext.Default.CmTag));
        reg.Register("test/seen", new ModComponent<CmSeen>(CmJsonContext.Default.CmSeen));
        reg.RegisterResource("test/score", new ModResource<CmScore>(CmJsonContext.Default.CmScore));
        reg.RegisterResource("test/added", new ModResource<CmAdded>(CmJsonContext.Default.CmAdded));
        reg.RegisterEvent("test/ping", new ModEvent<CmPing>(CmJsonContext.Default.CmPing));

        var app = new App(ThreadingMode.Single);
        app.AddResource(new CmScore());
        app.AddResource(new CmAdded());
        app.AddResource(new ModdingConfig { Registry = reg, ModFolder = _modFolder });
        app.AddPlugin<ModdingPlugin>();
        return app;
    }

    [Fact]
    public void Component_mod_loads_through_the_component_backend()
    {
        var app = NewApp();
        app.RunStartup();

        var control = app.GetResource<ModControl>();
        var info = Assert.Single(control.Mods);
        Assert.Equal("component-guest", info.Name);
        Assert.True(info.Enabled, info.LastError);

        var runtimes = app.GetResource<ModRuntimes>();
        Assert.IsType<ComponentModBackend>(runtimes.ComponentBackend);
        var ctx = runtimes.Runtimes[0].Ctx;
        Assert.Equal(new[] { "tick", "count_added" }, ctx.Systems.Select(s => s.Name));
        Assert.All(ctx.Systems, s => Assert.Equal(ModSchedule.Update, s.Stage));
        Assert.Equal(new[] { "tick" }, ctx.Systems[1].After);
        var obs = Assert.Single(ctx.Observers);
        Assert.Equal(ModObserverKind.Insert, obs.Kind);
        Assert.Equal("test/tag", obs.TypePath);
    }

    [Fact]
    public void Guest_queries_writes_rows_issues_commands_and_observes()
    {
        var app = NewApp();
        var world = app.GetWorld();
        var mover = world.Entity().Set(new CmPos { X = 0, Y = 0 }).Set(new CmVel { X = 2, Y = 1 }).ID;
        var frozen = world.Entity().Set(new CmPos { X = 5, Y = 5 }).Set(new CmVel { X = 1, Y = 1 }).Set(new CmFrozen()).ID;
        app.RunStartup();

        // Frame 1: tick moves `mover` (row.set on a `mut` term; `frozen` is excluded by
        // `without`), bumps the res-mut score to 1, and — since it is the first run —
        // spawns a frozen entity and sends a ping. count_added (after tick) matches
        // nothing: mover/frozen got test/pos at tick 0 (before any run), and the spawn's
        // components land at the stage's deferred flush, after the runner.
        app.Update();
        Assert.Equal(new CmPos { X = 2, Y = 1 }, world.Get<CmPos>(mover));
        Assert.Equal(new CmPos { X = 5, Y = 5 }, world.Get<CmPos>(frozen));
        Assert.Equal(1, app.GetResource<CmScore>().Value);
        Assert.Equal(0, app.GetResource<CmAdded>().Value);

        var spawned = SpawnedByMod(world);
        Assert.Equal(new CmPos { X = 100, Y = 0 }, world.Get<CmPos>(spawned));
        Assert.True(world.Has<CmFrozen>(spawned));
        world.Entity().Set(new CmPos { X = 7, Y = 7 }); // host-side add between frames

        // Frame 2: the ping sent last frame arrives through the events param (+100),
        // mover crosses x >= 3 so tick inserts test/tag via commands, which fires the
        // guest's on-add observer; it answers with test/seen = the entity id. The `added`
        // term now matches the guest's spawn and the host-side add (2).
        app.Update();
        Assert.Equal(new CmPos { X = 4, Y = 2 }, world.Get<CmPos>(mover));
        Assert.Equal(102, app.GetResource<CmScore>().Value);
        Assert.Equal(2, app.GetResource<CmAdded>().Value);
        Assert.True(world.Has<CmTag>(mover));
        Assert.True(world.Has<CmSeen>(mover), "on-add observer did not run");
        Assert.Equal((long)mover, world.Get<CmSeen>(mover).Value);
        Assert.False(world.Has<CmTag>(frozen));

        var info = Assert.Single(app.GetResource<ModControl>().Mods);
        Assert.True(info.Enabled, info.LastError);
        Assert.Equal("", info.LastError);
    }

    // The guest's on-packet observers run synchronously in the packet chain through
    // `observe-packet`, with their params evaluated per call: on_in (incoming 0x10,
    // res-mut score) blocks or appends 0x42; on_out (every outgoing id) replaces.
    [Fact]
    public void Packet_observers_filter_and_return_verdicts_through_observe_packet()
    {
        var app = NewApp();
        app.RunStartup();
        var ctx = app.GetResource<ModRuntimes>().Runtimes[0].Ctx;
        Assert.Equal(2, ctx.PacketObservers.Count);
        var chain = app.GetResource<ModPacketChain>();
        Assert.True(chain.Wants(ModPacketDirection.Incoming, 0x10));
        Assert.False(chain.Wants(ModPacketDirection.Incoming, 0x11));
        Assert.True(chain.Wants(ModPacketDirection.Outgoing, 0xEF));

        Assert.Equal(ModPacketVerdict.Replace, chain.Run(ModPacketDirection.Incoming, new byte[] { 0x10, 1 }, null, out var repl));
        Assert.Equal(new byte[] { 0x10, 1, 0x42 }, repl.ToArray());
        Assert.Equal(1000, app.GetResource<CmScore>().Value);

        Assert.Equal(ModPacketVerdict.Block, chain.Run(ModPacketDirection.Incoming, new byte[] { 0x10, 0 }, null, out _));
        Assert.Equal(ModPacketVerdict.Pass, chain.Run(ModPacketDirection.Incoming, new byte[] { 0x11, 0 }, null, out _));
        Assert.Equal(2000, app.GetResource<CmScore>().Value);

        Assert.Equal(ModPacketVerdict.Replace, chain.Run(ModPacketDirection.Outgoing, new byte[] { 0x05, 7 }, null, out var outRepl));
        Assert.Equal(new byte[] { 0x99 }, outRepl.ToArray());

        var info = Assert.Single(app.GetResource<ModControl>().Mods);
        Assert.True(info.Enabled, info.LastError);
    }

    // The guest's `tick` system declares `mut test/pos`: with test/pos read-only the
    // mod must not load at all (not load and silently drop its writes).
    [Fact]
    public void A_mut_term_on_a_read_only_component_fails_the_load()
    {
        var app = NewApp(posReadOnly: true);
        app.RunStartup();

        Assert.Empty(app.GetResource<ModRuntimes>().Runtimes);
        Assert.Empty(app.GetResource<ModControl>().Mods);
    }

    [Fact]
    public void A_mut_term_on_a_read_only_component_is_rejected_by_name()
    {
        var reg = new ModComponentRegistry();
        reg.Register("test/pos", new ModComponent<CmPos>(CmJsonContext.Default.CmPos, readOnly: true));
        var ctx = new ModHostContext { World = new World(), Registry = reg, Name = "cm" };
        var sys = new ComponentSystem("tick");

        var e = Assert.Throws<InvalidOperationException>(() => sys.AddQuery(ctx, [("mut", "test/pos")]));
        Assert.Equal("mod 'cm': system 'tick' declares Mut on read-only 'test/pos'", e.Message);

        sys.AddQuery(ctx, [("ref", "test/pos")]); // reading it is fine
    }

    [Fact]
    public void Reload_reinstantiates_and_reruns_setup()
    {
        var app = NewApp();
        var world = app.GetWorld();
        var mover = world.Entity().Set(new CmPos()).Set(new CmVel { X = 1, Y = 0 }).ID;
        app.RunStartup();
        app.Update();

        var control = app.GetResource<ModControl>();
        control.Reload(0);
        app.Update(); // reload applied at the end of this frame
        app.Update();

        var info = Assert.Single(control.Mods);
        Assert.True(info.Enabled, info.LastError);
        var ctx = app.GetResource<ModRuntimes>().Runtimes[0].Ctx;
        Assert.Equal(2, ctx.Systems.Count);
        Assert.Equal(2, ctx.PacketObservers.Count); // re-declared, not doubled
        Assert.Equal(ModPacketVerdict.Replace, app.GetResource<ModPacketChain>().Run(ModPacketDirection.Outgoing, new byte[] { 1 }, null, out _));
        Assert.Equal(3, world.Get<CmPos>(mover).X);
    }

    private static ulong SpawnedByMod(World world)
    {
        var q = world.QueryBuilder().With<ModEntity>().With<CmPos>().Build();
        var it = q.Iter();
        ulong found = 0;
        var count = 0;
        while (it.Next())
            foreach (var e in it.Entities())
            {
                found = e.ID;
                count++;
            }
        Assert.Equal(1, count);
        return found;
    }
}

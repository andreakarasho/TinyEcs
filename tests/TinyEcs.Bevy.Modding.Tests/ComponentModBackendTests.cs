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
// What the typed observers saw (fixture on-tag-typed / on-tag-bare).
public struct CmSeenTyped { public long Value { get; set; } }
public struct CmSeenBare { public long Value { get; set; } }
public sealed class CmScore { public long Value { get; set; } }
public sealed class CmAdded { public long Value { get; set; } }
public struct CmPing { public int N { get; set; } }
// The run-on-change systems' inputs / outputs (see the fixture's `watch` and `beat`).
public struct CmWatched { public int V { get; set; } }
public sealed class CmKnob { public long Value { get; set; } }
public struct CmPoke { public int N { get; set; } }
public sealed class CmClock { public long Value { get; set; } }
public sealed class CmProbe
{
    public long Runs { get; set; }
    public long Unchanged { get; set; }
    public long Knob { get; set; }
    public long Rows { get; set; }
    public long Pokes { get; set; }
}
public sealed class CmBeats { public long Value { get; set; } }

[JsonSerializable(typeof(CmPos))]
[JsonSerializable(typeof(CmVel))]
[JsonSerializable(typeof(CmFrozen))]
[JsonSerializable(typeof(CmTag))]
[JsonSerializable(typeof(CmSeen))]
[JsonSerializable(typeof(CmSeenTyped))]
[JsonSerializable(typeof(CmSeenBare))]
[JsonSerializable(typeof(CmScore))]
[JsonSerializable(typeof(CmAdded))]
[JsonSerializable(typeof(CmPing))]
[JsonSerializable(typeof(CmWatched))]
[JsonSerializable(typeof(CmKnob))]
[JsonSerializable(typeof(CmPoke))]
[JsonSerializable(typeof(CmClock))]
[JsonSerializable(typeof(CmProbe))]
[JsonSerializable(typeof(CmBeats))]
internal partial class CmJsonContext : JsonSerializerContext { }

public sealed class ComponentModBackendTests : IDisposable
{
    private readonly string _modFolder = Path.Combine(Path.GetTempPath(), "tinyecs-cm-" + Guid.NewGuid().ToString("N"));

    public ComponentModBackendTests() => InstallFixture("component_guest.wasm");

    private void InstallFixture(string file)
    {
        var dir = Path.Combine(_modFolder, "component-guest");
        Directory.CreateDirectory(dir);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "fixtures", file), Path.Combine(dir, "mod.wasm"), overwrite: true);
        File.WriteAllText(Path.Combine(dir, "mod.json"), """{ "name": "component-guest", "version": "0.1.0", "wasm": "mod.wasm" }""");
    }

    public void Dispose()
    {
        try { Directory.Delete(_modFolder, recursive: true); } catch (IOException) { }
    }

    // The paths the TypedTrigger hook lowered, in order.
    private readonly List<string> _lowered = new();
    private static readonly Wasmtime.ByteVector FieldV = Wasmtime.ByteVector.Constant("v");

    private App NewApp(bool posReadOnly = false, bool typedTriggers = true)
    {
        var reg = new ModComponentRegistry();
        reg.Register("test/pos", new ModComponent<CmPos>(CmJsonContext.Default.CmPos, readOnly: posReadOnly));
        reg.Register("test/vel", new ModComponent<CmVel>(CmJsonContext.Default.CmVel));
        reg.Register("test/frozen", new ModComponent<CmFrozen>(CmJsonContext.Default.CmFrozen));
        reg.Register("test/tag", new ModComponent<CmTag>(CmJsonContext.Default.CmTag));
        reg.Register("test/seen", new ModComponent<CmSeen>(CmJsonContext.Default.CmSeen));
        reg.Register("test/seen-typed", new ModComponent<CmSeenTyped>(CmJsonContext.Default.CmSeenTyped));
        reg.Register("test/seen-bare", new ModComponent<CmSeenBare>(CmJsonContext.Default.CmSeenBare));
        reg.RegisterResource("test/score", new ModResource<CmScore>(CmJsonContext.Default.CmScore));
        reg.RegisterResource("test/added", new ModResource<CmAdded>(CmJsonContext.Default.CmAdded));
        reg.RegisterEvent("test/ping", new ModEvent<CmPing>(CmJsonContext.Default.CmPing));
        reg.Register("test/watched", new ModComponent<CmWatched>(CmJsonContext.Default.CmWatched));
        reg.RegisterResource("test/knob", new ModResource<CmKnob>(CmJsonContext.Default.CmKnob));
        reg.RegisterEvent("test/poke", new ModEvent<CmPoke>(CmJsonContext.Default.CmPoke));
        reg.RegisterResource("test/clock", new ModResource<CmClock>(CmJsonContext.Default.CmClock, readOnly: true));
        reg.RegisterResource("test/probe", new ModResource<CmProbe>(CmJsonContext.Default.CmProbe));
        reg.RegisterResource("test/beats", new ModResource<CmBeats>(CmJsonContext.Default.CmBeats));

        var app = new App(ThreadingMode.Single);
        app.AddResource(new CmScore());
        app.AddResource(new CmAdded());
        app.AddResource(new CmKnob());
        app.AddResource(new CmClock());
        app.AddResource(new CmProbe());
        app.AddResource(new CmBeats());
        var config = new ModdingConfig { Registry = reg, ModFolder = _modFolder };
        if (typedTriggers)
            config.ComponentImports.Add(imports => imports.TypedTrigger = LowerTag);
        app.AddResource(config);
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
        Assert.IsType<ComponentModBackend>(runtimes.Backend);
        var ctx = runtimes.Runtimes[0].Ctx;
        Assert.Equal(new[] { "tick", "count-added", "watch", "beat" }, ctx.Systems.Select(s => s.Name));
        Assert.Equal(new[] { ModSchedule.Update, ModSchedule.Update, ModSchedule.PostUpdate, ModSchedule.PostUpdate }, ctx.Systems.Select(s => s.Stage));
        Assert.Equal(new[] { false, false, true, true }, ctx.Systems.Select(s => s.RunOnChange));
        Assert.Equal(new[] { "tick" }, ctx.Systems[1].After);
        Assert.Equal(3, ctx.Observers.Count);
        Assert.All(ctx.Observers, obs =>
        {
            Assert.Equal(ModObserverKind.Insert, obs.Kind);
            Assert.Equal("test/tag", obs.TypePath);
        });
    }

    [Fact]
    public void Guest_queries_writes_rows_issues_commands_and_observes()
    {
        var app = NewApp();
        var world = app.GetWorld();
        var mover = world.Entity().Set(new CmPos { X = 0, Y = 0 }).Set(new CmVel { X = 2, Y = 1 }).ID;
        var frozen = world.Entity().Set(new CmPos { X = 5, Y = 5 }).Set(new CmVel { X = 1, Y = 1 }).Set(new CmFrozen()).ID;
        app.RunStartup();

        // Frame 1: tick moves `mover` (query.set on a `mut` term; `frozen` is excluded by
        // `without`), bumps the res-mut score to 1, and — since it is the first run —
        // spawns a frozen entity and sends a ping. count-added (after tick) matches
        // mover + frozen: a first run sees every change made before it (they got test/pos
        // before any system ran).
        app.Update();
        Assert.Equal(new CmPos { X = 2, Y = 1 }, world.Get<CmPos>(mover));
        Assert.Equal(new CmPos { X = 5, Y = 5 }, world.Get<CmPos>(frozen));
        Assert.Equal(1, app.GetResource<CmScore>().Value);
        Assert.Equal(2, app.GetResource<CmAdded>().Value);

        var spawned = SpawnedByMod(world);
        Assert.Equal(new CmPos { X = 100, Y = 0 }, world.Get<CmPos>(spawned));
        Assert.True(world.Has<CmFrozen>(spawned));
        world.Entity().Set(new CmPos { X = 7, Y = 7 }); // host-side add between frames

        // Frame 2: the ping sent last frame arrives through the events param (+100),
        // mover crosses x >= 3 so tick inserts test/tag via commands, which fires the
        // guest's on-add observer; it answers with test/seen = the entity id. The `added`
        // term now matches the guest's spawn and the host-side add (2 more).
        app.Update();
        Assert.Equal(new CmPos { X = 4, Y = 2 }, world.Get<CmPos>(mover));
        Assert.Equal(102, app.GetResource<CmScore>().Value);
        Assert.Equal(4, app.GetResource<CmAdded>().Value);
        Assert.True(world.Has<CmTag>(mover));
        Assert.True(world.Has<CmSeen>(mover), "on-add observer did not run");
        Assert.Equal((long)mover, world.Get<CmSeen>(mover).Value);
        Assert.False(world.Has<CmTag>(frozen));

        var info = Assert.Single(app.GetResource<ModControl>().Mods);
        Assert.True(info.Enabled, info.LastError);
        Assert.Equal("", info.LastError);
    }

    // The guest's on-packet observers run synchronously in the packet chain through
    // their exports, with their params evaluated per call: on-in (incoming 0x10,
    // res-mut score) blocks or appends 0x42; on-out (every outgoing id) replaces.
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
        Assert.Equal(4, ctx.Systems.Count);
        Assert.Equal(2, ctx.PacketObservers.Count); // re-declared, not doubled
        Assert.Equal(ModPacketVerdict.Replace, app.GetResource<ModPacketChain>().Run(ModPacketDirection.Outgoing, new byte[] { 1 }, null, out _));
        Assert.Equal(3, world.Get<CmPos>(mover).X);
    }

    [Fact]
    public void Core_module_mod_is_rejected_at_load()
    {
        var dir = Path.Combine(_modFolder, "old-core");
        Directory.CreateDirectory(dir);
        // The smallest valid core module: magic + version 1, layer 00 00.
        File.WriteAllBytes(Path.Combine(dir, "mod.wasm"), [0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00]);
        File.WriteAllText(Path.Combine(dir, "mod.json"), """{ "name": "old-core", "version": "0.1.0", "wasm": "mod.wasm" }""");

        var app = NewApp();
        var output = new StringWriter();
        var stdout = Console.Out;
        Console.SetOut(output);
        try { app.RunStartup(); }
        finally { Console.SetOut(stdout); }

        var info = Assert.Single(app.GetResource<ModControl>().Mods);
        Assert.Equal("component-guest", info.Name);
        Assert.Contains("old-core is a core-wasm (p1) module; mods must be wasm32-wasip2 components", output.ToString());
    }

    // `watch` (run-on-change: changed test/watched, res test/knob, events test/poke) runs
    // only when an input changed; `beat` (commands only) runs every frame.
    [Fact]
    public void Run_on_change_system_runs_only_when_an_input_changed()
    {
        var app = NewApp();
        var world = app.GetWorld();
        app.RunStartup();

        app.Update(); // the first run never skips
        var probe = app.GetResource<CmProbe>();
        Assert.Equal((1L, 0L, 0L, 0L, 0L), (probe.Runs, probe.Unchanged, probe.Knob, probe.Rows, probe.Pokes));
        app.Update();
        app.Update();
        Assert.Equal(1, app.GetResource<CmProbe>().Runs);
        Assert.Equal(3, app.GetResource<CmBeats>().Value); // no input params: never skips

        Poke(app, 1); // an event: runs; the knob is unchanged since its last get
        app.Update();
        probe = app.GetResource<CmProbe>();
        Assert.Equal((2L, 1L, -1L, 0L, 1L), (probe.Runs, probe.Unchanged, probe.Knob, probe.Rows, probe.Pokes));
        app.Update(); // the event was consumed
        Assert.Equal(2, app.GetResource<CmProbe>().Runs);

        world.Entity().Set(new CmWatched { V = 1 }); // a changed row: runs, once
        app.Update();
        probe = app.GetResource<CmProbe>();
        Assert.Equal((3L, 1L, 1L), (probe.Runs, probe.Unchanged, probe.Rows));
        app.Update();
        Assert.Equal(3, app.GetResource<CmProbe>().Runs);

        app.GetResource<CmKnob>().Value = 5; // a resource change: runs, reports changed
        app.Update();
        probe = app.GetResource<CmProbe>();
        Assert.Equal((4L, 0L, 5L, 0L), (probe.Runs, probe.Unchanged, probe.Knob, probe.Rows));
        app.Update();
        app.Update();
        Assert.Equal(4, app.GetResource<CmProbe>().Runs);
        Assert.Equal(10, app.GetResource<CmBeats>().Value);

        var info = Assert.Single(app.GetResource<ModControl>().Mods);
        Assert.Equal("", info.LastError);
    }

    // The first run of a run-on-change system sees the rows changed before it.
    [Fact]
    public void Run_on_change_first_run_sees_earlier_changes()
    {
        var app = NewApp();
        app.GetWorld().Entity().Set(new CmWatched { V = 1 });
        app.RunStartup();
        app.Update();
        var probe = app.GetResource<CmProbe>();
        Assert.Equal((1L, 1L), (probe.Runs, probe.Rows));
        app.Update();
        Assert.Equal(1, app.GetResource<CmProbe>().Runs);
    }

    // commands.set-resource traps on a read-only or unregistered path, and the call's
    // other commands are dropped. (A trapped component instance can't be re-entered, so
    // every later call of the mod fails until it is reloaded.)
    [Theory]
    [InlineData(13, "commands.set-resource: 'test/clock' is read-only")]
    [InlineData(14, "commands.set-resource: 'test/nope' is not a registered resource")]
    public void Set_resource_traps_on_read_only_and_unknown_paths(int poke, string message)
    {
        var app = NewApp();
        app.RunStartup();
        app.Update();
        Assert.Equal(1, app.GetResource<CmProbe>().Runs);

        Poke(app, poke);
        var output = new StringWriter();
        var stdout = Console.Out;
        Console.SetOut(output);
        try { app.Update(); }
        finally { Console.SetOut(stdout); }

        Assert.Contains("system 'watch' 'component-guest' failed", output.ToString());
        Assert.Contains(message, output.ToString());
        Assert.Equal(1, app.GetResource<CmProbe>().Runs); // its set-resource was discarded
        Assert.Equal(0, app.GetResource<CmClock>().Value);
    }

    // A component built before per-system exports (it exports the generic `run` /
    // `observe` dispatchers, not one function per system) fails at load, naming the
    // first system it has no export for, instead of running with nothing wired.
    [Fact]
    public void A_guest_without_per_system_exports_fails_to_load_naming_the_missing_export()
    {
        InstallFixture("component_guest_v0.wasm");
        var app = NewApp();
        var output = new StringWriter();
        var stdout = Console.Out;
        Console.SetOut(output);
        try
        {
            app.RunStartup();
            app.Update();
        }
        finally { Console.SetOut(stdout); }

        Assert.Contains("system 'tick' is declared in setup but the mod exports no 'tick' function", output.ToString());
    }

    // The fixture's typed observers take test/tag as `record tag-value { v: s32 }`.
    private Wasmtime.ComponentValue LowerTag(string path, ReadOnlySpan<byte> json)
    {
        _lowered.Add(path);
        var tag = System.Text.Json.JsonSerializer.Deserialize(json, CmJsonContext.Default.CmTag);
        var rec = new Wasmtime.RecordBuilder(1, disposeNames: false);
        rec.Set(0, new Wasmtime.ByteVector(FieldV), Wasmtime.ComponentValue.CreateInt32(tag.V));
        return Wasmtime.ComponentValue.CreateRecord(rec, externallyOwned: true);
    }

    // An observer export may take its trigger typed: `(entity, record, ..)` gets the value
    // lowered by ComponentModImports.TypedTrigger, `(entity, ..)` none; `trigger-data`
    // observers of the same trigger keep their JSON.
    [Fact]
    public void Typed_observer_exports_get_the_trigger_value_as_a_record()
    {
        var app = NewApp();
        var world = app.GetWorld();
        app.RunStartup();
        var ctx = app.GetResource<ModRuntimes>().Runtimes[0].Ctx;
        Assert.Equal(3, ctx.Observers.Count);

        var e = world.Entity().Set(new CmTag { V = 7 }).ID;
        app.Update();

        Assert.Equal(7, world.Get<CmSeenTyped>(e).Value);
        Assert.Equal((long)e, world.Get<CmSeenBare>(e).Value);
        Assert.Equal((long)e, world.Get<CmSeen>(e).Value);
        Assert.Equal(new[] { "test/tag" }, _lowered);
        var info = Assert.Single(app.GetResource<ModControl>().Mods);
        Assert.True(info.Enabled, info.LastError);
    }

    // A typed observer on a host without the hook: the mod fails to load, loudly.
    [Fact]
    public void A_typed_observer_without_the_hook_fails_the_load()
    {
        var app = NewApp(typedTriggers: false);
        var output = new StringWriter();
        var stdout = Console.Out;
        Console.SetOut(output);
        try { app.RunStartup(); }
        finally { Console.SetOut(stdout); }

        Assert.DoesNotContain(app.GetResource<ModControl>().Mods, m => m.Enabled);
        Assert.Contains("takes its trigger typed, but the host defines no typed triggers", output.ToString());
    }

    private static void Poke(App app, int n)
    {
        var reg = app.GetResource<ModdingConfig>().Registry;
        Assert.True(reg.TryGetEvent("test/poke", out var ev));
        ev.Emit(app.GetWorld(), 0, "{\"N\":" + n + "}");
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

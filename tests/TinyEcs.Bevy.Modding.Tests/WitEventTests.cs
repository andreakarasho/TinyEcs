using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using Xunit;
using WitApp = Wit.Tinyecs.Modding.App;

namespace TinyEcs.Bevy.Modding.Tests;

// Custom (host-registered) events: a mod emits by name via the bridge, the host
// fires it as a typed On<T>, and both host observers and a mod's custom(name)
// observer receive it. Driven host-side; the guest callback dispatch is separate.
public class WitEventTests
{
    private static ModHostContext Ctx(App app, ModComponentRegistry reg)
        => new() { World = app.GetWorld(), Registry = reg, App = app };

    [Fact]
    public void Mod_emit_reaches_a_host_observer()
    {
        var app = new App();
        var reg = new ModComponentRegistry();
        reg.RegisterEvent("test/ping", new ModEvent<WitPing>(WitJsonContext.Default.WitPing));

        var hostSaw = new List<int>();
        app.AddObserver<On<WitPing>>(t => hostSaw.Add(t.Event.N));

        new CommandsImpl(Ctx(app, reg)).EmitEvent("test/ping", 0, "{\"N\":9}");

        Assert.Equal(new[] { 9 }, hostSaw);
    }

    [Fact]
    public void Custom_event_round_trips_mod_emit_to_mod_observer()
    {
        var app = new App();
        var reg = new ModComponentRegistry();
        reg.RegisterEvent("test/ping", new ModEvent<WitPing>(WitJsonContext.Default.WitPing));

        var fires = new List<(string Name, ulong Entity, string Json)>();
        ModdingPlugin.RegisterObserver(
            app,
            new ModObserverSpec { Name = "on_ping", Kind = WitApp.ObserverEvent.Case.Custom, TypePath = "test/ping" },
            reg,
            (n, e, j) => fires.Add((n, e, j)));

        new CommandsImpl(Ctx(app, reg)).EmitEvent("test/ping", 0, "{\"N\":7}");

        var fire = Assert.Single(fires.Where(f => f.Name == "on_ping"));
        Assert.Contains("\"N\":7", fire.Json);
    }

    [Fact]
    public void Emit_unregistered_event_is_a_noop()
    {
        var app = new App();
        var ctx = Ctx(app, new ModComponentRegistry());
        // Must not throw; nothing observes it.
        new CommandsImpl(ctx).EmitEvent("test/missing", 0, "{}");
    }
}

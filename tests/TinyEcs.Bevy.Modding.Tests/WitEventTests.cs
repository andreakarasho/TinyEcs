using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// Custom (host-registered) events: a mod emits by name via the bridge (the value in
// Encoding.Typed), the host fires it as a typed On<T>, and both host observers and a
// mod's custom(name) observer receive it. Driven host-side; the guest callback dispatch
// is separate.
public class WitEventTests
{
    private static ModHostContext Ctx(App app, ModComponentRegistry reg)
        => new() { World = app.GetWorld(), Registry = reg, App = app };

    private static ModComponentRegistry Ping() => new ModComponentRegistry().Event<WitPing>("test/ping", Codecs.WritePing, Codecs.ReadPing);

    [Fact]
    public void Mod_emit_reaches_a_host_observer()
    {
        var app = new App();
        var reg = Ping();

        var hostSaw = new List<int>();
        app.AddObserver<On<WitPing>>(t => hostSaw.Add(t.Event.N));

        new CommandsImpl(Ctx(app, reg)).EmitEvent(reg.BinaryEvent("test/ping"), 0, Bin.S32(9));

        Assert.Equal(new[] { 9 }, hostSaw);
    }

    [Fact]
    public void Custom_event_round_trips_mod_emit_to_mod_observer()
    {
        var app = new App();
        var reg = Ping();

        var fires = new List<(string Name, ulong Entity, byte[] Value)>();
        ModdingPlugin.RegisterObserver(
            app,
            new ModObserverSpec { Name = "on_ping", Kind = ModObserverKind.Custom, TypePath = "test/ping" },
            reg,
            static () => true,
            (string n, ulong e, ReadOnlySpan<byte> v) => fires.Add((n, e, v.ToArray())));

        var target = app.GetWorld().Entity().ID;
        new CommandsImpl(Ctx(app, reg)).EmitEvent(reg.BinaryEvent("test/ping"), target, Bin.S32(7));

        var fire = Assert.Single(fires.Where(f => f.Name == "on_ping"));
        Assert.Equal(target, fire.Entity);
        Assert.Equal(Bin.S32(7), fire.Value);
    }

    [Fact]
    public void An_unwanted_fire_is_dropped()
    {
        var app = new App();
        var reg = Ping();
        var fires = 0;
        ModdingPlugin.RegisterObserver(
            app,
            new ModObserverSpec { Name = "on_ping", Kind = ModObserverKind.Custom, TypePath = "test/ping" },
            reg,
            static () => false,
            (string _, ulong _, ReadOnlySpan<byte> _) => fires++);

        new CommandsImpl(Ctx(app, reg)).EmitEvent(reg.BinaryEvent("test/ping"), 0, Bin.S32(7));
        Assert.Equal(0, fires);
    }

    // Placeholder ids inside an event value resolve like a component's.
    [Fact]
    public void An_emitted_value_resolves_entity_fields_through_the_buffer_resolver()
    {
        var app = new App();
        var reg = new ModComponentRegistry().Event<V3Link>("test/link", Codecs.WriteV3Link, Codecs.ReadV3Link);
        var seen = new List<ulong>();
        app.AddObserver<On<V3Link>>(t => seen.Add(t.Event.Target));

        new CommandsImpl(Ctx(app, reg), id => ModEntityRef.IsPlaceholder(id) ? 1000 + ModEntityRef.TempId(id) : id)
            .EmitEvent(reg.BinaryEvent("test/link"), 0, Bin.U64(ModEntityRef.PlaceholderBit | 4));

        Assert.Equal(new[] { 1004UL }, seen);
    }

    [Fact]
    public void An_unregistered_event_is_refused_by_name()
    {
        var e = Assert.Throws<InvalidOperationException>(() => new ModComponentRegistry().BinaryEvent("test/missing"));
        Assert.Contains("unregistered event 'test/missing'", e.Message);
    }
}

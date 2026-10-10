using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// Host-side coverage for the observer surface (add-observer): RegisterObserver wires the
// right host global observer and the fire carries (name, entity, value) — the value in
// Encoding.Typed, empty for a tag / spawn / despawn. The guest-callback dispatch is
// covered by ComponentModBackendTests against a real component.
public class WitObserverTests
{
    private static List<(string Name, ulong Entity, byte[] Value)> Observe(App app, ModComponentRegistry reg, ModObserverSpec spec)
    {
        var fires = new List<(string, ulong, byte[])>();
        ModdingPlugin.RegisterObserver(app, spec, reg, static () => true,
            (string n, ulong e, ReadOnlySpan<byte> v) => fires.Add((n, e, v.ToArray())));
        return fires;
    }

    [Fact]
    public void Spawn_observer_buffers_the_fire()
    {
        var app = new App();
        var fires = Observe(app, new ModComponentRegistry(), new ModObserverSpec { Name = "on_spawn", Kind = ModObserverKind.Spawn });

        app.AddSystem((Commands c) => { c.Spawn().Insert(new WitPos { X = 1, Y = 2 }); })
           .InStage(Stage.Update).Build();
        app.Run();

        var fire = Assert.Single(fires);
        Assert.Equal("on_spawn", fire.Name);
        Assert.NotEqual(0ul, fire.Entity);
        Assert.Empty(fire.Value);
    }

    [Fact]
    public void Insert_observer_buffers_entity_and_component_value()
    {
        var app = new App();
        var fires = Observe(app, new ModComponentRegistry().Pos(),
            new ModObserverSpec { Name = "on_pos", Kind = ModObserverKind.Insert, TypePath = "test/pos" });

        app.AddSystem((Commands c) => { c.Spawn().Insert(new WitPos { X = 3, Y = 7 }); })
           .InStage(Stage.Update).Build();
        app.Run();

        var fire = Assert.Single(fires.Where(f => f.Name == "on_pos"));
        Assert.NotEqual(0ul, fire.Entity);
        Assert.Equal(new WitPos { X = 3, Y = 7 }, Bin.Decode<WitPos>(Codecs.ReadPos, fire.Value));
    }

    [Fact]
    public void Remove_observer_carries_the_removed_value()
    {
        var app = new App();
        var fires = Observe(app, new ModComponentRegistry().Pos(),
            new ModObserverSpec { Name = "on_unpos", Kind = ModObserverKind.Remove, TypePath = "test/pos" });

        var id = app.GetWorld().Entity().Set(new WitPos { X = 4, Y = 5 }).ID;
        app.GetWorld().Entity(id).Unset<WitPos>();

        var fire = Assert.Single(fires);
        Assert.Equal(id, fire.Entity);
        Assert.Equal(new WitPos { X = 4, Y = 5 }, Bin.Decode<WitPos>(Codecs.ReadPos, fire.Value));
    }

    // A path registered as a tag (presence only) fires with empty bytes.
    [Fact]
    public void A_tag_path_fires_with_an_empty_value()
    {
        var app = new App();
        var reg = new ModComponentRegistry().Tag<WitPos>("test/present", new ModPresence<WitPos>());
        var fires = Observe(app, reg, new ModObserverSpec { Name = "on_present", Kind = ModObserverKind.Insert, TypePath = "test/present" });

        app.AddSystem((Commands c) => { c.Spawn().Insert(new WitPos { X = 1, Y = 1 }); })
           .InStage(Stage.Update).Build();
        app.Run();

        var fire = Assert.Single(fires);
        Assert.NotEqual(0ul, fire.Entity);
        Assert.Empty(fire.Value);
    }

    // A type-path not in the registry (or without a codec) can't be observed: the mod's
    // setup fails, naming it.
    [Fact]
    public void Insert_observer_for_an_unregistered_path_is_refused_by_name()
    {
        var app = new App();
        var e = Assert.Throws<InvalidOperationException>(() => Observe(app, new ModComponentRegistry(),
            new ModObserverSpec { Name = "on_missing", Kind = ModObserverKind.Insert, TypePath = "test/missing" }));
        Assert.Contains("unregistered component 'test/missing'", e.Message);

        var noCodec = new ModComponentRegistry();
        noCodec.Register("test/pos", new ModComponent<WitPos>());
        e = Assert.Throws<InvalidOperationException>(() => Observe(app, noCodec,
            new ModObserverSpec { Name = "on_pos", Kind = ModObserverKind.Insert, TypePath = "test/pos" }));
        Assert.Contains("'test/pos' has no binary codec", e.Message);
    }

    // A hand-written mapper without IModComponentObserverOf can't hand its value over.
    [Fact]
    public void A_mapper_without_typed_observers_cannot_be_observed()
    {
        var app = new App();
        var reg = new ModComponentRegistry();
        reg.Register("test/odd", new TypedOnly());
        reg.RegisterBinaryComponent<WitPos>("test/odd", Codecs.WritePos, Codecs.ReadPos);
        var e = Assert.Throws<InvalidOperationException>(() => Observe(app, reg,
            new ModObserverSpec { Name = "o", Kind = ModObserverKind.Insert, TypePath = "test/odd" }));
        Assert.Contains("can't be observed", e.Message);
    }

    private sealed class TypedOnly : IModComponent, IModComponentOf<WitPos>
    {
        public bool Has(World world, ulong entity) => world.Has<WitPos>(entity);
        public void CollectEntities(World world, ref TinyEcs.Collections.PooledList<ulong> into) { }
        public void Remove(World world, ulong entity) { }
        public WitPos Get(World world, ulong entity) => world.Get<WitPos>(entity);
        public void Set(World world, ulong entity, in WitPos value) => world.Set(entity, value);
    }
}

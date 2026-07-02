using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// Host-side coverage for the observer surface (add-observer in tinyecs.wit):
// RegisterObserver wires the right host global observer and the fire is buffered
// with the correct (name, entity, json). The guest-callback dispatch (FlushObservers
// -> Instance.Call) needs a WASM fixture and is not covered here.
public class WitObserverTests
{
    [Fact]
    public void Spawn_observer_buffers_the_fire()
    {
        var app = new App();
        var fires = new List<(string Name, ulong Entity, string Json)>();
        ModdingPlugin.RegisterObserver(
            app,
            new ModObserverSpec { Name = "on_spawn", Kind = ModObserverKind.Spawn },
            new ModComponentRegistry(),
            (n, e, j) => fires.Add((n, e, j)));

        app.AddSystem((Commands c) => { c.Spawn().Insert(new WitPos { X = 1, Y = 2 }); })
           .InStage(Stage.Update).Build();
        app.Run();

        var fire = Assert.Single(fires);
        Assert.Equal("on_spawn", fire.Name);
        Assert.NotEqual(0ul, fire.Entity);
        Assert.Equal("", fire.Json);
    }

    [Fact]
    public void Insert_observer_buffers_entity_and_component_json()
    {
        var app = new App();
        var reg = new ModComponentRegistry();
        reg.Register("test/pos", new ModComponent<WitPos>(WitJsonContext.Default.WitPos));

        var fires = new List<(string Name, ulong Entity, string Json)>();
        ModdingPlugin.RegisterObserver(
            app,
            new ModObserverSpec { Name = "on_pos", Kind = ModObserverKind.Insert, TypePath = "test/pos" },
            reg,
            (n, e, j) => fires.Add((n, e, j)));

        app.AddSystem((Commands c) => { c.Spawn().Insert(new WitPos { X = 3, Y = 7 }); })
           .InStage(Stage.Update).Build();
        app.Run();

        var fire = Assert.Single(fires.Where(f => f.Name == "on_pos"));
        Assert.NotEqual(0ul, fire.Entity);
        Assert.Contains("\"X\":3", fire.Json);
        Assert.Contains("\"Y\":7", fire.Json);
    }

    [Fact]
    public void Insert_observer_for_unregistered_path_is_a_noop()
    {
        var app = new App();
        var fires = new List<(string, ulong, string)>();
        // type-path not in the registry -> no observer wired, no throw.
        ModdingPlugin.RegisterObserver(
            app,
            new ModObserverSpec { Name = "on_missing", Kind = ModObserverKind.Insert, TypePath = "test/missing" },
            new ModComponentRegistry(),
            (n, e, j) => fires.Add((n, e, j)));

        app.AddSystem((Commands c) => { c.Spawn().Insert(new WitPos { X = 1, Y = 1 }); })
           .InStage(Stage.Update).Build();
        app.Run();

        Assert.Empty(fires);
    }
}

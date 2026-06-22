using System.Text.Json.Serialization;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using TinyEcs.Collections;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// Host-side contract tests for tinyecs.wit, driven through the PUBLIC modding
// surface (ModComponentRegistry / ModComponent<T> / ModResource<T>) — no WASM
// guest. These cover the (de)serialization + registry path that the WIT resources
// map onto: entity-commands.insert -> SetJson, component.get -> GetJson, a query's
// entity collection -> CollectEntities, commands.resource-get/set -> ModResource.
// The full guest<->host round-trip is covered by ModdingPluginTests.
public struct WitPos { public int X { get; set; } public int Y { get; set; } }
public struct WitTag { } // zero-size marker — the case the WIT uses QueryBuilder for
public struct WitScore { public int Value { get; set; } }
public struct WitPing { public int N { get; set; } } // custom event payload

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(WitPos))]
[JsonSerializable(typeof(WitTag))]
[JsonSerializable(typeof(WitScore))]
[JsonSerializable(typeof(WitPing))]
internal partial class WitJsonContext : JsonSerializerContext { }

public class WitContractTests
{
    private static ModComponent<WitPos> PosComp() => new(WitJsonContext.Default.WitPos);
    private static ModComponent<WitTag> TagComp() => new(WitJsonContext.Default.WitTag);

    [Fact]
    public void Registry_round_trips_components_and_resources()
    {
        var registry = new ModComponentRegistry();
        registry.Register("test/pos", PosComp());
        registry.RegisterResource("test/score", new ModResource<WitScore>(WitJsonContext.Default.WitScore));

        Assert.True(registry.TryGet("test/pos", out var comp));
        Assert.NotNull(comp);
        Assert.False(registry.TryGet("test/missing", out _));

        Assert.True(registry.TryGetResource("test/score", out var res));
        Assert.NotNull(res);
        Assert.False(registry.TryGetResource("test/missing", out _));
    }

    [Fact]
    public void Component_json_round_trips_on_an_entity()
    {
        using var world = new World();
        var comp = (IModComponent)PosComp();
        var id = world.Entity().ID;

        Assert.False(comp.Has(world, id));

        // entity-commands.insert
        comp.SetJson(world, id, "{\"X\":3,\"Y\":7}");
        Assert.True(comp.Has(world, id));

        // component.get
        var json = comp.GetJson(world, id);
        var pos = world.Get<WitPos>(id);
        Assert.Equal(3, pos.X);
        Assert.Equal(7, pos.Y);
        Assert.Contains("\"X\":3", json);
        Assert.Contains("\"Y\":7", json);

        // entity-commands.remove
        comp.Remove(world, id);
        Assert.False(comp.Has(world, id));
    }

    [Fact]
    public void CollectEntities_finds_all_matches_including_zero_size_markers()
    {
        using var world = new World();
        var comp = (IModComponent)TagComp();

        var a = world.Entity(); a.Set(new WitTag());
        var b = world.Entity(); b.Set(new WitTag());
        var c = world.Entity(); c.Set(new WitTag());
        world.Entity(); // no tag — must not be collected

        // Not `using`: CollectEntities takes `into` by ref, and a using-variable
        // can't be passed by ref (CS1657). Dispose by hand.
        var into = new PooledList<ulong>(8);
        try
        {
            comp.CollectEntities(world, ref into);

            var found = new HashSet<ulong>();
            for (var i = 0; i < into.Count; i++)
                found.Add(into[i]);

            Assert.Equal(3, into.Count);
            Assert.Contains(a.ID, found);
            Assert.Contains(b.ID, found);
            Assert.Contains(c.ID, found);
        }
        finally
        {
            into.Dispose();
        }
    }

    [Fact]
    public void Resource_get_set_round_trips()
    {
        var app = new App();
        var res = (IModResource)new ModResource<WitScore>(WitJsonContext.Default.WitScore);

        // commands.resource-get on an absent resource
        Assert.Equal("null", res.GetJson(app));

        app.AddResource(new WitScore { Value = 10 });
        Assert.Contains("\"Value\":10", res.GetJson(app));

        // commands.resource-set
        res.SetJson(app, "{\"Value\":42}");
        Assert.Equal(42, app.GetResource<WitScore>().Value);
    }
}

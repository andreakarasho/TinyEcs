using System.Buffers;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using TinyEcs.Collections;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// Host-side contract tests for the registry, driven through the PUBLIC modding surface
// (ModComponentRegistry / ModComponent<T> / ModResource<T> and their binary views) — no
// wasm guest. These cover the Encoding.Typed path every value crosses: a row's column ->
// TryWrite, an insert -> Set, a query's entity collection -> CollectEntities, a resource
// read / write -> the resource's binary view.
public class WitContractTests
{
    [Fact]
    public void Registry_round_trips_components_and_resources()
    {
        var registry = new ModComponentRegistry()
            .Pos()
            .Resource("test/score", new ModResource<WitScore>(), Codecs.WriteScore, Codecs.ReadScore);

        Assert.True(registry.TryGet("test/pos", out var comp));
        Assert.NotNull(comp);
        Assert.False(registry.TryGet("test/missing", out _));
        Assert.True(registry.TryGetBinary("test/pos", out var codec));
        Assert.Same(comp, codec.Component);

        Assert.True(registry.TryGetResource("test/score", out var res));
        Assert.NotNull(res);
        Assert.False(registry.TryGetResource("test/missing", out _));
        Assert.Empty(registry.MissingCodecs());
    }

    [Fact]
    public void A_registered_path_without_a_codec_is_listed_missing()
    {
        var registry = new ModComponentRegistry();
        registry.Register("test/pos", new ModComponent<WitPos>());
        registry.RegisterResource("test/score", new ModResource<WitScore>());
        registry.RegisterEvent("test/ping", new ModEvent<WitPing>());
        Assert.Equal(new[] { "test/pos", "test/score", "test/ping" }, registry.MissingCodecs());
    }

    [Fact]
    public void Component_value_round_trips_on_an_entity()
    {
        using var world = new World();
        var codec = new ModComponentRegistry().Pos().Binary("test/pos");
        var id = world.Entity().ID;

        Assert.False(codec.Component.Has(world, id));
        var none = new ArrayBufferWriter<byte>();
        Assert.False(codec.TryWrite(world, id, none));
        Assert.Equal(0, none.WrittenCount);

        // insert
        codec.Set(world, id, Bin.Written(w => { ModBinary.S32(w, 3); ModBinary.S32(w, 7); }), null);
        Assert.True(codec.Component.Has(world, id));
        Assert.Equal(new WitPos { X = 3, Y = 7 }, world.Get<WitPos>(id));

        // read
        var w = new ArrayBufferWriter<byte>();
        Assert.True(codec.TryWrite(world, id, w));
        var r = new ModBinaryReader(w.WrittenSpan);
        Assert.Equal((3, 7), (r.S32(), r.S32()));
        Assert.Equal(0, r.Remaining);

        // remove
        codec.Component.Remove(world, id);
        Assert.False(codec.Component.Has(world, id));
    }

    [Fact]
    public void CollectEntities_finds_all_matches_including_zero_size_markers()
    {
        using var world = new World();
        var comp = (IModComponent)new ModComponent<WitTag>();

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
        var res = new ModComponentRegistry()
            .Resource("test/score", new ModResource<WitScore>(), Codecs.WriteScore, Codecs.ReadScore)
            .BinaryResource("test/score");

        // An absent resource writes nothing.
        var w = new ArrayBufferWriter<byte>();
        Assert.False(res.TryWrite(app, "m", w));
        Assert.Equal(0, w.WrittenCount);

        app.AddResource(new WitScore { Value = 10 });
        Assert.True(res.TryWrite(app, "m", w));
        Assert.Equal(Bin.S32(10), w.WrittenSpan.ToArray());

        res.Set(app, Bin.S32(42), "m");
        Assert.Equal(42, app.GetResource<WitScore>().Value);
    }

    // Resources carry no change ticks: "unchanged since this param's previous run" is a
    // byte compare of the observed values (ModResValue), absent never being "unchanged".
    [Fact]
    public void Resource_unchanged_detection_compares_the_observed_bytes()
    {
        var v = new ModResValue();
        Assert.False(v.Observe(Bin.S32(1), present: true)); // first observation
        Assert.True(v.Observe(Bin.S32(1), present: true));
        Assert.False(v.Observe(Bin.S32(2), present: true));
        Assert.False(v.Observe(default, present: false));
        Assert.True(v.Observe(default, present: false));
        Assert.False(v.Observe(Bin.S32(2), present: true));

        // UnchangedSinceGot: equal to the value the guest last took.
        v.MarkGot();
        Assert.True(v.Observe(Bin.S32(2), present: true));
        Assert.True(v.UnchangedSinceGot);
        v.Observe(Bin.S32(3), present: true);
        Assert.False(v.UnchangedSinceGot);

        // After a failed call the guest may not have taken it in.
        v.MarkGot();
        v.Forget();
        Assert.False(v.Observe(Bin.S32(3), present: true));
    }
}

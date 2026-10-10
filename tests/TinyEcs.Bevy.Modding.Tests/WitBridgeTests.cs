using TinyEcs;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// Drives the internal bridge structs directly (reachable via InternalsVisibleTo): the
// command applier every backend shares (CommandsImpl / EntityCommandsImpl, values as
// Encoding.Typed payloads) and the system declaration (SystemImpl).
public class WitBridgeTests
{
    private static ModHostContext Ctx(World world, ModComponentRegistry? reg = null)
        => new() { World = world, Registry = reg ?? new ModComponentRegistry(), Slot = 3 };

    private static ModPayload Pos(ModComponentRegistry reg, int x, int y)
        => new("test/pos", Bin.Encode(Codecs.WritePos, new WitPos { X = x, Y = y }), reg.Binary("test/pos"));

    [Fact]
    public void Spawn_tags_the_mod_entity_and_inserts_the_bundle_through_the_codec()
    {
        using var world = new World();
        var reg = new ModComponentRegistry().Pos().Tag<WitTag>("test/tag", new ModComponent<WitTag>());
        var ctx = Ctx(world, reg);

        var id = new CommandsImpl(ctx).Spawn([Pos(reg, 5, 6), new ModPayload("test/tag", default, reg.Binary("test/tag"))]).Id;

        Assert.Equal(3, world.Get<ModEntity>(id).Slot);
        Assert.Equal(new WitPos { X = 5, Y = 6 }, world.Get<WitPos>(id));
        Assert.True(world.Has<WitTag>(id));
    }

    [Fact]
    public void Insert_overwrites_and_remove_ignores_unregistered_paths()
    {
        using var world = new World();
        var reg = new ModComponentRegistry().Pos();
        var ctx = Ctx(world, reg);
        var id = world.Entity().Set(new WitPos { X = 1, Y = 1 }).ID;

        var e = new CommandsImpl(ctx).EntityById(id);
        e.Insert([Pos(reg, 7, 8)]);
        Assert.Equal(new WitPos { X = 7, Y = 8 }, world.Get<WitPos>(id));

        e.Remove(["test/nope", "test/pos"]);
        Assert.False(world.Has<WitPos>(id));
    }

    [Fact]
    public void Add_child_and_despawn_take_the_subtree()
    {
        using var world = new World();
        var ctx = Ctx(world);
        var commands = new CommandsImpl(ctx);
        var parent = commands.SpawnEmpty().Id;
        var child = commands.SpawnEmpty().Id;

        commands.EntityById(parent).AddChild(child, uint.MaxValue);
        commands.EntityById(parent).Despawn();

        Assert.False(world.Exists(parent));
        Assert.False(world.Exists(child));
    }

    [Fact]
    public void Query_terms_split_into_filters_and_reading_terms_in_declaration_order()
    {
        var sys = new SystemImpl("q");
        sys.AddQuery([
            new ModQueryTerm(ModQueryTermKind.Mut, "a"),
            new ModQueryTerm(ModQueryTermKind.With, "b"),
            new ModQueryTerm(ModQueryTermKind.Ref, "c"),
            new ModQueryTerm(ModQueryTermKind.Without, "d"),
            new ModQueryTerm(ModQueryTermKind.Added, "e"),
        ]);
        var q = sys.Spec.Params[0].Query!;
        Assert.Equal(5, q.Terms.Count);
        Assert.Equal(new[] { ("a", true), ("c", false), ("e", false) }, q.Components);
    }

    [Fact]
    public void A_mut_term_on_a_read_only_component_is_rejected_by_name()
    {
        var reg = new ModComponentRegistry().Pos(readOnly: true);
        var ctx = Ctx(new World(), reg);
        ctx.Name = "m";
        Assert.True(reg.TryGet("test/pos", out var comp));
        var e = Assert.Throws<InvalidOperationException>(() => ModQueryTerm.RejectReadOnlyMut(ctx, "system 's'", comp, "test/pos"));
        Assert.Equal("mod 'm': system 's' declares Mut on read-only 'test/pos'", e.Message);
    }
}

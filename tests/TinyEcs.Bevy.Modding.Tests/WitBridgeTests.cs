using TinyEcs;
using TinyEcs.Bevy.Modding;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// Drives the internal bridge structs directly (reachable via InternalsVisibleTo).
// Covers WIT surface the public-registry tests can't: component.set's mutability
// trap, and entity.children / entity.parent.
public class WitBridgeTests
{
    private static ModHostContext Ctx(World world, ModComponentRegistry? reg = null)
        => new() { World = world, Registry = reg ?? new ModComponentRegistry() };

    [Fact]
    public void Component_set_traps_when_not_mutable()
    {
        using var world = new World();
        var ctx = Ctx(world);
        ulong id = world.Entity().ID;

        var comp = new ComponentImpl(ctx, id, "test/pos", mutable: false);
        var ex = Assert.Throws<InvalidOperationException>(() => comp.Set("{\"X\":1,\"Y\":2}"));
        Assert.Contains("test/pos", ex.Message);
        Assert.Contains("not declared mutable", ex.Message);
    }

    [Fact]
    public void Component_set_writes_when_mutable()
    {
        using var world = new World();
        var reg = new ModComponentRegistry();
        reg.Register("test/pos", new ModComponent<WitPos>(WitJsonContext.Default.WitPos));
        var ctx = Ctx(world, reg);
        ulong id = world.Entity().ID;

        new ComponentImpl(ctx, id, "test/pos", mutable: true).Set("{\"X\":5,\"Y\":6}");

        Assert.True(world.Has<WitPos>(id));
        Assert.Equal(5, world.Get<WitPos>(id).X);
        Assert.Equal(6, world.Get<WitPos>(id).Y);
    }

    [Fact]
    public void Entity_children_and_parent_resolve()
    {
        using var world = new World();
        var ctx = Ctx(world);
        ulong parent = world.Entity().ID;
        ulong c1 = world.Entity().ID;
        ulong c2 = world.Entity().ID;
        world.AddChild(parent, c1);
        world.AddChild(parent, c2);

        var children = new EntityImpl(ctx, parent).Children();
        var ids = new HashSet<ulong>();
        foreach (var ch in children)
            ids.Add(ch.EcsId);

        Assert.Equal(2, children.Length);
        Assert.Contains(c1, ids);
        Assert.Contains(c2, ids);

        var p = new EntityImpl(ctx, c1).Parent();
        Assert.NotNull(p);
        Assert.Equal(parent, p!.Value.EcsId);
    }

    [Fact]
    public void Entity_parent_is_null_for_root()
    {
        using var world = new World();
        var ctx = Ctx(world);
        ulong root = world.Entity().ID;
        Assert.Null(new EntityImpl(ctx, root).Parent());
    }
}

using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Modding;
using TinyEcs.Bevy.UI;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// Host-side coverage of the generic modding plugin's bridges. References only the
// lib + TinyEcs (no game-specific glue, no wasm runtime), so a green run proves the
// runtime's plugin wiring is reusable on its own. The full guest<->host round-trip
// (loading a real core-wasm module and ticking its systems) is exercised by the
// consuming host's own test suite against its built mods.
public class ModdingPluginTests
{
    // The pointer bridges: UiOver / UiOut over a mod-owned entity re-emit as the
    // mod-facing ModHover event on that entity (Over true / false); host entities
    // are ignored. No wasm needed — pure host ECS.
    [Fact]
    public void Hover_bridge_emits_ModHover_on_UiOver_UiOut_for_mod_entities_only()
    {
        var app = new App(ThreadingMode.Single);
        app.AddResource(new ModdingConfig()); // empty: no mod folder, observers still wire in Build
        app.AddPlugin<ModdingPlugin>();

        var world = app.GetWorld();
        var ent = world.Entity();
        ent.Set(new ModEntity());
        var id = ent.ID;
        var hostId = world.Entity().ID;

        var seen = new List<(ulong Entity, bool Over)>();
        app.AddObserver<On<ModHover>>(t => seen.Add((t.EntityId, t.Event.Over)));

        app.RunStartup(); // no mods to load; the bridge observers are live

        // Fire from inside a system (Commands-driven trigger), matching how Bevy.UI
        // emits these in-game. One-shot: UiOver on frame 0, UiOut on frame 1.
        app.AddSystem((Commands c, Local<int> frame) =>
        {
            if (frame.Value == 0)
            {
                c.Entity(id).EmitTrigger(new UiOver(), propagate: false);
                c.Entity(hostId).EmitTrigger(new UiOver(), propagate: false);
            }
            else if (frame.Value == 1)
                c.Entity(id).EmitTrigger(new UiOut(), propagate: false);
            frame.Value++;
        }).InStage(Stage.First).Build();

        app.Update();
        Assert.Equal(new[] { (id, true) }, seen);

        app.Update();
        Assert.Equal(new[] { (id, true), (id, false) }, seen);
    }

    [Fact]
    public void Click_bridge_emits_ModClick_and_right_click_carries_the_press_point()
    {
        var app = new App(ThreadingMode.Single);
        app.AddResource(new ModdingConfig());
        app.AddPlugin<ModdingPlugin>();

        var world = app.GetWorld();
        var ent = world.Entity();
        ent.Set(new ModEntity());
        var id = ent.ID;

        var clicks = new List<ulong>();
        var rights = new List<(ulong, float, float)>();
        app.AddObserver<On<ModClick>>(t => clicks.Add(t.EntityId));
        app.AddObserver<On<ModRightClick>>(t => rights.Add((t.EntityId, t.Event.X, t.Event.Y)));
        app.RunStartup();

        app.AddSystem((Commands c, Local<int> frame) =>
        {
            if (frame.Value++ != 0)
                return;
            c.Entity(id).EmitTrigger(new UiClick(), propagate: false);
            c.Entity(id).EmitTrigger(new UiRightClick { Position = new System.Numerics.Vector2(12, 34) }, propagate: false);
        }).InStage(Stage.First).Build();

        app.Update();
        Assert.Equal(new[] { id }, clicks);
        Assert.Equal(new[] { (id, 12f, 34f) }, rights);
    }
}

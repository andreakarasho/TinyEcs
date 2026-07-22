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
// parent repo's ClassicUO.Ecs.Tests against the built ecs-mods.
public class ModdingPluginTests
{
    // The hover bridge: UiOver over a mod-owned entity sets the sparse ModHovered
    // marker (so mods poll it instead of scanning every Interaction byte), UiOut
    // clears it. No wasm needed — pure host ECS. Mirrors the click bridge's
    // On<UiClick> -> ModClicked, which the wasm round-trip tests exercise.
    [Fact]
    public void Hover_bridge_sets_and_clears_ModHovered_on_UiOver_UiOut()
    {
        var app = new App(ThreadingMode.Single);
        app.AddResource(new ModdingConfig()); // empty: no mod folder, observers still wire in Build
        app.AddPlugin<ModdingPlugin>();

        var world = app.GetWorld();
        var ent = world.Entity();
        ent.Set(new ModEntity());
        var id = ent.ID;

        app.RunStartup(); // no mods to load; the bridge observers are live

        // Fire from inside a system (Commands-driven trigger), matching how Bevy.UI
        // emits these in-game. One-shot: UiOver on frame 0, UiOut on frame 1.
        app.AddSystem((Commands c, Local<int> frame) =>
        {
            if (frame.Value == 0)
                c.Entity(id).EmitTrigger(new UiOver(), propagate: false);
            else if (frame.Value == 1)
                c.Entity(id).EmitTrigger(new UiOut(), propagate: false);
            frame.Value++;
        }).InStage(Stage.First).Build();

        app.Update(); // UiOver -> ModHovered inserted
        Assert.True(world.Has<ModHovered>(id), "UiOver did not set ModHovered on the mod entity");

        app.Update(); // UiOut -> ModHovered removed
        Assert.False(world.Has<ModHovered>(id), "UiOut did not clear ModHovered");
    }
}

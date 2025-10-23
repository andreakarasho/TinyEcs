using System.Numerics;
using TinyEcs.Bevy;
using TinyEcs.UI;
using TinyEcs.UI.Widgets;

namespace TinyEcs.Tests;

public class UiButtonWidgetTests
{
    [Fact]
    public void Button_Hover_Press_Updates_Background()
    {
        var app = new App();
        app.AddReactiveUi(); // Use the new reactive UI system

        ulong buttonId = 0;
        var style = ClayButtonStyle.Default with
        {
            Size = new Vector2(100, 30),
        };

        app.AddSystem((Commands commands) =>
        {
            var button = ButtonWidget.Create(commands, style, "OK");
            buttonId = button.Id;
        })
        .InStage(Stage.Startup)
        .Build();

        app.RunStartup();
        var world = app.GetWorld();

        // Initial - button should have Interaction and Interactive components
        Assert.True(world.Has<UiNode>(buttonId));
        Assert.True(world.Has<Interaction>(buttonId));
        Assert.True(world.Has<Interactive>(buttonId));
        Assert.True(world.Has<ClayButtonStyle>(buttonId));

        var interaction = world.Get<Interaction>(buttonId);
        Assert.Equal(Interaction.None, interaction);

        // Simulate hover by setting Interaction to Hovered
        world.Set(buttonId, Interaction.Hovered);
        app.Update(); // Run observers to update visuals

        var node = world.Get<UiNode>(buttonId);
        Assert.Equal(style.HoverBackground, node.Declaration.backgroundColor);

        // Simulate press by setting Interaction to Pressed
        world.Set(buttonId, Interaction.Pressed);
        app.Update(); // Run observers to update visuals

        node = world.Get<UiNode>(buttonId);
        Assert.Equal(style.PressedBackground, node.Declaration.backgroundColor);

        // Simulate release by setting Interaction back to Hovered
        world.Set(buttonId, Interaction.Hovered);
        app.Update(); // Run observers to update visuals

        node = world.Get<UiNode>(buttonId);
        Assert.Equal(style.HoverBackground, node.Declaration.backgroundColor);

        // Simulate exit by setting Interaction to None
        world.Set(buttonId, Interaction.None);
        app.Update(); // Run observers to update visuals

        node = world.Get<UiNode>(buttonId);
        Assert.Equal(style.Background, node.Declaration.backgroundColor);
    }
}

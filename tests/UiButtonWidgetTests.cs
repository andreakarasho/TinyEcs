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
        app.AddUiWidgets();

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

        // Initial
        Assert.True(world.Has<UiNode>(buttonId));
        Assert.True(world.Has<ButtonState>(buttonId));
        Assert.True(world.Has<ClayButtonStyle>(buttonId));

        // Hover -> HoverBackground
        world.EmitTrigger(new UiPointerTrigger(new UiPointerEvent(
            UiPointerEventType.PointerEnter,
            target: buttonId,
            currentTarget: buttonId,
            elementKey: 0,
            position: Vector2.Zero,
            moveDelta: Vector2.Zero,
            scrollDelta: Vector2.Zero,
            isPrimaryButton: false
        )));

        var node = world.Get<UiNode>(buttonId);
        Assert.Equal(style.HoverBackground, node.Declaration.backgroundColor);

        // Press -> PressedBackground
        world.EmitTrigger(new UiPointerTrigger(new UiPointerEvent(
            UiPointerEventType.PointerDown,
            target: buttonId,
            currentTarget: buttonId,
            elementKey: 0,
            position: Vector2.Zero,
            moveDelta: Vector2.Zero,
            scrollDelta: Vector2.Zero,
            isPrimaryButton: true
        )));

        node = world.Get<UiNode>(buttonId);
        Assert.Equal(style.PressedBackground, node.Declaration.backgroundColor);

        // Release -> HoverBackground again
        world.EmitTrigger(new UiPointerTrigger(new UiPointerEvent(
            UiPointerEventType.PointerUp,
            target: buttonId,
            currentTarget: buttonId,
            elementKey: 0,
            position: Vector2.Zero,
            moveDelta: Vector2.Zero,
            scrollDelta: Vector2.Zero,
            isPrimaryButton: false
        )));

        node = world.Get<UiNode>(buttonId);
        Assert.Equal(style.HoverBackground, node.Declaration.backgroundColor);

        // Exit -> Normal background
        world.EmitTrigger(new UiPointerTrigger(new UiPointerEvent(
            UiPointerEventType.PointerExit,
            target: buttonId,
            currentTarget: buttonId,
            elementKey: 0,
            position: Vector2.Zero,
            moveDelta: Vector2.Zero,
            scrollDelta: Vector2.Zero,
            isPrimaryButton: false
        )));

        node = world.Get<UiNode>(buttonId);
        Assert.Equal(style.Background, node.Declaration.backgroundColor);
    }
}


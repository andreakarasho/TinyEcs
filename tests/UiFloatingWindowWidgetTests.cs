using System.Numerics;
using TinyEcs.Bevy;
using TinyEcs.UI;
using TinyEcs.UI.Widgets;

namespace TinyEcs.Tests;

public class UiFloatingWindowWidgetTests
{
    [Fact]
    public void FloatingWindow_Drag_From_TitleBar_Moves_Window()
    {
        var app = new App();
        app.AddUiWidgets();

        ulong windowId = 0;
        ulong titleBarId = 0;
        var initialPos = new Vector2(50, 60);

        app.AddSystem((Commands commands) =>
        {
            var window = FloatingWindowWidget.Create(commands, ClayFloatingWindowStyle.Default with { InitialSize = new Vector2(200, 120) }, "Win", initialPos);
            windowId = window.Id;
        })
        .InStage(Stage.Startup)
        .Build();

        app.RunStartup();
        var world = app.GetWorld();

        Assert.True(world.Has<FloatingWindowState>(windowId));
        Assert.True(world.Has<FloatingWindowLinks>(windowId));
        titleBarId = world.Get<FloatingWindowLinks>(windowId).TitleBarId;
        Assert.NotEqual(0UL, titleBarId);

        // PointerDown on title bar (currentTarget is window)
        world.EmitTrigger(new UiPointerTrigger(new UiPointerEvent(
            UiPointerEventType.PointerDown,
            target: titleBarId,
            currentTarget: windowId,
            elementKey: 0,
            position: initialPos + new Vector2(10, 5),
            moveDelta: Vector2.Zero,
            scrollDelta: Vector2.Zero,
            isPrimaryButton: true
        )));

        // Move twice
        world.EmitTrigger(new UiPointerTrigger(new UiPointerEvent(
            UiPointerEventType.PointerMove,
            target: windowId,
            currentTarget: windowId,
            elementKey: 0,
            position: Vector2.Zero,
            moveDelta: new Vector2(4, 3),
            scrollDelta: Vector2.Zero,
            isPrimaryButton: true
        )));

        world.EmitTrigger(new UiPointerTrigger(new UiPointerEvent(
            UiPointerEventType.PointerMove,
            target: windowId,
            currentTarget: windowId,
            elementKey: 0,
            position: Vector2.Zero,
            moveDelta: new Vector2(6, 2),
            scrollDelta: Vector2.Zero,
            isPrimaryButton: true
        )));

        // Release
        world.EmitTrigger(new UiPointerTrigger(new UiPointerEvent(
            UiPointerEventType.PointerUp,
            target: windowId,
            currentTarget: windowId,
            elementKey: 0,
            position: Vector2.Zero,
            moveDelta: Vector2.Zero,
            scrollDelta: Vector2.Zero,
            isPrimaryButton: false
        )));

        // Validate new position (sum of move deltas)
        ref var win = ref world.Get<FloatingWindowState>(windowId);
        var expected = initialPos + new Vector2(4 + 6, 3 + 2);
        Assert.Equal(expected, win.Position);

        // Floating offset mirrors position
        var node = world.Get<UiNode>(windowId);
        Assert.Equal(win.Position.X, node.Declaration.floating.offset.x);
        Assert.Equal(win.Position.Y, node.Declaration.floating.offset.y);
    }
}

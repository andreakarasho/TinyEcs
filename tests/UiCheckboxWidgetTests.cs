using System.Numerics;
using TinyEcs.Bevy;
using TinyEcs.UI;
using TinyEcs.UI.Widgets;

namespace TinyEcs.Tests;

public class UiCheckboxWidgetTests
{
    [Fact]
    public void Checkbox_Toggles_State_On_Click()
    {
        var app = new App();
        app.AddUiWidgets();

        ulong containerId = 0;

        app.AddSystem((Commands commands) =>
        {
            var cb = CheckboxWidget.Create(commands, ClayCheckboxStyle.Default, initialChecked: false, label: "Test");
            containerId = cb.Id;
        })
        .InStage(Stage.Startup)
        .Build();

        app.RunStartup();
        var world = app.GetWorld();

        Assert.True(world.Has<CheckboxLinks>(containerId));
        ref var links = ref world.Get<CheckboxLinks>(containerId);
        Assert.NotEqual(0UL, links.BoxEntity);
        var boxId = links.BoxEntity;

        // Initial unchecked
        ref var state = ref world.Get<CheckboxState>(boxId);
        Assert.False(state.Checked);

        // Click container -> toggle
        world.EmitTrigger(new UiPointerTrigger(new UiPointerEvent(
            UiPointerEventType.PointerDown,
            target: containerId,
            currentTarget: containerId,
            elementKey: 0,
            position: Vector2.Zero,
            moveDelta: Vector2.Zero,
            scrollDelta: Vector2.Zero,
            isPrimaryButton: true
        )));

        state = ref world.Get<CheckboxState>(boxId);
        Assert.True(state.Checked);

        // Background color should match checked style
        var node = world.Get<UiNode>(boxId);
        var style = world.Get<ClayCheckboxStyle>(boxId);
        Assert.Equal(style.CheckedColor, node.Declaration.backgroundColor);

        // Checkmark text should exist
        Assert.True(world.Has<UiText>(boxId));
    }
}


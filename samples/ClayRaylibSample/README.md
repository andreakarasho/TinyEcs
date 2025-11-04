# Clay UI + Raylib Sample

A complete working example demonstrating the TinyEcs.UI.Clay integration with Raylib rendering.

## Features

- ✅ **Clay UI Layout** - High-performance retained-mode UI layout using Clay
- ✅ **Raylib Rendering** - Full rendering implementation for Clay render commands
- ✅ **Mouse Interaction** - Click detection and pointer events
- ✅ **ECS Architecture** - Follows TinyEcs.Bevy patterns (system parameters, plugins, observers)
- ✅ **Single-Threaded** - All Raylib calls properly marked as single-threaded

## Running

```bash
cd samples/ClayRaylibSample
dotnet run
```

## Architecture

### Plugins

**ClayRaylibRenderPlugin** - Handles all Raylib rendering integration:
- Input system: Updates `ClayPointerState` from Raylib mouse input
- Begin/End drawing systems: Manages Raylib drawing frame
- Render system: Translates Clay render commands to Raylib draw calls

### Supported Clay Render Commands

- ✅ **Rectangle** - Solid color rectangles with optional corner radius
- ✅ **Border** - Border drawing (all sides)
- ✅ **Text** - Text rendering with font size and color
- ✅ **Scissor** - Clipping/masking regions
- ⏸️ **Image** - Not implemented (TODO)
- ⏸️ **Custom** - Not implemented (TODO)

### System Execution Order

```
Stage.First
  └─ raylib:update-input - Update pointer state from Raylib

Stage.PreUpdate
  └─ clay:* - Clay layout calculation (from ClayUiPlugin)

Stage.PostUpdate
  ├─ clay:interaction - Clay pointer interaction (from ClayUiPlugin)
  ├─ raylib:begin-draw - BeginDrawing()
  ├─ raylib:render-clay - Render all Clay commands
  └─ raylib:end-draw - EndDrawing() + FPS counter
```

## Code Highlights

### Creating UI Elements

```csharp
// Create a button with text
var buttonNode = ClayNode.Default;
buttonNode.Layout = new Clay_LayoutConfig
{
    sizing = new Clay_Sizing(
        Clay_SizingAxis.Fixed(200),
        Clay_SizingAxis.Fixed(60)
    ),
    padding = Clay_Padding.All(8)
};
buttonNode.Rectangle = new Clay_RectangleRenderData
{
    backgroundColor = new Clay_Color(70, 130, 180, 255)
};
buttonNode.CornerRadius = Clay_CornerRadius.All(8);

var button = commands.SpawnClayElement(buttonNode);
parent.AddClayChild(button);

// Add click handler
button.Observe<On<ClayPointerTrigger>>(trigger =>
{
    var evt = trigger.Event.Event;
    if (evt.EventType == ClayPointerEventType.Click)
    {
        Console.WriteLine("Button clicked!");
    }
});
```

### Rendering Clay Commands

```csharp
private static void RenderClayUI(Res<ClayUiState> state)
{
    var commands = state.Value.RenderCommands;

    foreach (ref readonly var cmd in commands)
    {
        switch (cmd.commandType)
        {
            case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_RECTANGLE:
                RenderRectangle(cmd);
                break;

            case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_TEXT:
                RenderText(cmd);
                break;

            // ... handle other command types
        }
    }
}
```

## Performance

Clay layout calculation is **extremely fast** (microseconds for typical UIs). The sample runs at 60 FPS with smooth interactions.

## Extending

To add more UI elements:

1. Create elements in `CreateUI()` using `commands.SpawnClayElement()`
2. Configure layout with `ClayNode.Layout`
3. Add visual styling with `Rectangle`, `Border`, `CornerRadius`, etc.
4. Add interactivity with `.Observe<On<ClayPointerTrigger>>()`

To add custom rendering:

1. Handle `CLAY_RENDER_COMMAND_TYPE_CUSTOM` in `RenderClayUI()`
2. Use `cmd.renderData.custom` to access custom render data
3. Draw with Raylib primitives

## See Also

- [TinyEcs.UI.Clay README](../../src/TinyEcs.UI.Clay/README.md) - Full Clay integration documentation
- [Clay Documentation](../../externals/clay/README.md) - Clay layout library docs
- [TinyEcs.Bevy Documentation](../../CLAUDE.md) - ECS framework documentation

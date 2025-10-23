# Rendering Guide - Reactive UI System

## TL;DR - Nothing Changed!

**The rendering code is EXACTLY THE SAME as before.** The reactive system only changes HOW widget state is managed, not how Clay render commands are processed.

## Complete Rendering Example

```csharp
using TinyEcs.Bevy;
using TinyEcs.UI;
using Clay_cs;

var app = new App(ThreadingMode.Single);

// 1. Add reactive UI (handles state management)
app.AddReactiveUi(new ClayUiOptions
{
    LayoutDimensions = new Clay_Dimensions(1280f, 720f),
    ArenaSize = 512 * 1024,
    UseEntityHierarchy = true,
    AutoCreatePointerState = true
});

// 2. Update pointer input (from your rendering backend)
app.AddSystem((ResMut<ClayPointerState> pointer) =>
{
    ref var state = ref pointer.Value;

    // Update from Raylib
    state.Position = Raylib.GetMousePosition();
    state.PrimaryDown = Raylib.IsMouseButtonDown(MouseButton.Left);
    state.AddScroll(new Vector2(0, Raylib.GetMouseWheelMove() * 20f));
    state.DeltaTime = Raylib.GetFrameTime();
})
.InStage(Stage.PreUpdate)
.Before("ui:clay:pointer")
.Build();

// 3. Render Clay commands (UNCHANGED!)
app.AddSystem((Res<ClayUiState> uiState, Res<Font> font) =>
{
    var commands = uiState.Value.RenderCommands; // Same as before!

    foreach (ref readonly var cmd in commands)
    {
        switch (cmd.commandType)
        {
            case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_RECTANGLE:
                RenderRectangle(cmd);
                break;

            case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_TEXT:
                RenderText(cmd, font.Value);
                break;

            case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_BORDER:
                RenderBorder(cmd);
                break;

            case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_START:
                Raylib.BeginScissorMode(/*...*/);
                break;

            case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_SCISSOR_END:
                Raylib.EndScissorMode();
                break;
        }
    }
})
.InStage(Stage.Update)
.After("ui:clay:layout") // Ensure layout ran first
.Build();

// 4. Game loop
while (!Raylib.WindowShouldClose())
{
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.Black);

    app.Update(); // Runs all systems

    Raylib.EndDrawing();
}
```

## How It Works - Step by Step

### Before (Manual Entity Observers)

```
Frame Start
  ↓
Pointer Input → UiPointerEvent
  ↓
Button Entity Observer runs
  ↓
Observer sets: node.backgroundColor = hoverColor
  ↓
Clay Layout Pass
  ↓
Clay Render Commands (with hover color)
  ↓
Your Renderer draws commands
  ↓
Frame End
```

### After (Reactive System)

```
Frame Start
  ↓
Pointer Input → UiPointerEvent
  ↓
UpdateInteractionState sets: button.Interaction = Hovered
  ↓
OnButtonInteractionChanged reacts to Changed<Interaction>
  ↓
Observer sets: node.backgroundColor = hoverColor
  ↓
Clay Layout Pass
  ↓
Clay Render Commands (with hover color)
  ↓
Your Renderer draws commands ← SAME AS BEFORE
  ↓
Frame End
```

**Key Point:** Your rendering code sees the exact same `Clay_RenderCommand` stream in both cases!

## System Execution Order

```
Stage.PreUpdate:
  1. Your pointer input system     (updates ClayPointerState)
  2. ui:clay:pointer              (converts to UiPointerEvent)
  3. ui:interaction:update        (updates Interaction components)
  4. ui:observers:button-visuals  (reacts to Interaction changes)
  5. ui:clay:mark-nodes           (marks changed nodes)

Stage.Update:
  1. ui:clay:layout               (Clay layout pass)
  2. ui:interaction:compute-z     (compute z-indices)
  3. Your rendering system        (reads RenderCommands)
```

## Raylib Integration (Full Example)

See `samples/TinyEcsGame/RaylibClayUiPlugin.cs` for the complete implementation.

Key points:

```csharp
// Update pointer in PreUpdate (before "ui:clay:pointer")
.InStage(Stage.PreUpdate)
.Before("ui:clay:pointer")

// Render in your rendering stage (after "ui:clay:layout")
.InStage(RenderingStage)
.After("ui:clay:layout")
```

## Rendering Helper Functions

```csharp
private void RenderRectangle(Clay_RenderCommand cmd)
{
    var config = cmd.renderData.rectangle;
    var bounds = cmd.boundingBox;
    var color = ToRaylibColor(config.backgroundColor);
    var rect = new Rectangle(bounds.x, bounds.y, bounds.width, bounds.height);

    if (HasCornerRadius(config.cornerRadius))
    {
        var avgRadius = GetAverageRadius(config.cornerRadius);
        Raylib.DrawRectangleRounded(rect, avgRadius / Min(bounds), 8, color);
    }
    else
    {
        Raylib.DrawRectangleRec(rect, color);
    }
}

private void RenderText(Clay_RenderCommand cmd, Font font)
{
    var config = cmd.renderData.text;
    var bounds = cmd.boundingBox;
    var text = config.text.GetString();
    var color = ToRaylibColor(config.textColor);

    Raylib.DrawTextEx(
        font,
        text,
        new Vector2(bounds.x, bounds.y),
        config.fontSize,
        1f,
        color);
}

private void RenderBorder(Clay_RenderCommand cmd)
{
    var config = cmd.renderData.border;
    var bounds = cmd.boundingBox;
    var color = ToRaylibColor(config.color);

    // Draw each border edge
    if (config.width.left > 0)
        Raylib.DrawRectangle(/*...*/);
    if (config.width.right > 0)
        Raylib.DrawRectangle(/*...*/);
    if (config.width.top > 0)
        Raylib.DrawRectangle(/*...*/);
    if (config.width.bottom > 0)
        Raylib.DrawRectangle(/*...*/);
}

private Color ToRaylibColor(Clay_Color c) => new Color(c.r, c.g, c.b, c.a);
```

## Z-Index and Render Order

The reactive system computes `ComputedZIndex` on entities, but this is **metadata** for your use.

Clay handles draw order via:

1. Element order in the layout tree
2. Floating elements (via `Clay_FloatingElementConfig.zIndex`)

If you want to sort elements by `ComputedZIndex` before rendering:

```csharp
app.AddSystem((
    Res<ClayUiState> uiState,
    Query<Data<ComputedZIndex, UiNode>> sortableNodes) =>
{
    // Get render commands as usual
    var commands = uiState.Value.RenderCommands;

    // Option 1: Render as-is (Clay's order)
    foreach (ref readonly var cmd in commands)
        RenderCommand(cmd);

    // Option 2: Sort by ComputedZIndex first (custom ordering)
    var sorted = new List<(float z, Clay_RenderCommand cmd)>();
    foreach (ref readonly var cmd in commands)
    {
        // Map command to entity, get ComputedZIndex, sort, render
        // (Advanced use case - usually not needed)
    }
})
.InStage(Stage.Update)
.After("ui:interaction:compute-z")
.Build();
```

**Recommendation:** Let Clay handle ordering. The `ComputedZIndex` is mainly for UI logic (e.g., "which window is on top?"), not rendering.

## Common Pitfalls

### ❌ Wrong: Rendering before layout

```csharp
app.AddSystem(RenderUI)
    .InStage(Stage.PreUpdate) // TOO EARLY!
    .Build();
```

### ✅ Correct: Rendering after layout

```csharp
app.AddSystem(RenderUI)
    .InStage(Stage.Update)
    .After("ui:clay:layout") // Ensure layout ran
    .Build();
```

### ❌ Wrong: Updating pointer after Clay processed it

```csharp
app.AddSystem(UpdatePointer)
    .InStage(Stage.Update) // TOO LATE!
    .Build();
```

### ✅ Correct: Updating pointer before Clay processes it

```csharp
app.AddSystem(UpdatePointer)
    .InStage(Stage.PreUpdate)
    .Before("ui:clay:pointer")
    .Build();
```

## Migration Checklist

If you have existing rendering code:

-   [ ] **No changes needed** - Keep using `ClayUiState.RenderCommands`
-   [ ] **System ordering** - Ensure rendering runs after `"ui:clay:layout"`
-   [ ] **Pointer input** - Ensure it runs before `"ui:clay:pointer"`
-   [ ] **Optional** - Switch to `app.AddReactiveUi()` for new features

## Performance Notes

**Rendering performance is unchanged.** The reactive system adds:

-   `UpdateInteractionState` - O(events) - processes pointer events
-   `OnButtonInteractionChanged` - O(changed buttons) - only runs when Interaction changes
-   `ComputeZIndices` - O(UI nodes) - depth-first traversal

All of these run **before** the Clay layout pass, so rendering latency is identical.

## Summary

**The reactive UI system is a DROP-IN replacement for state management.**

Your rendering code:

1. Still reads `ClayUiState.RenderCommands`
2. Still iterates through `Clay_RenderCommand` array
3. Still renders each command type the same way

The only difference is that widget colors/states update via reactive observers instead of per-entity observers. The final `RenderCommands` output is identical.

See working example in: `samples/TinyEcsGame/RaylibClayUiPlugin.cs`

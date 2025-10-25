# Flexbox UI System for TinyEcs

This document describes the new **Flexbox-based UI system** for TinyEcs, which runs **parallel** to the existing Clay UI system. Both can coexist in the same project.

---

## Overview

The Flexbox UI system provides an alternative layout engine based on the CSS Flexbox model, integrated into TinyEcs using the same reflection-free, ECS-driven architecture as the Clay UI system.

### Key Features

- **Flexbox Layout Engine**: Industry-standard CSS Flexbox model for familiar, predictable layouts
- **Reflection-Free**: All components and systems use compile-time types, no `GetType()` or reflection
- **Entity-Based Architecture**: UI elements are entities with components (FlexboxNode, FlexboxText, etc.)
- **Observer-Driven Interactions**: Uses entity observers for single-widget behavior
- **Reuses Existing Infrastructure**: Shares `UiPointerEvent`, `UiPointerTrigger`, `Interaction`, and `Interactive` components
- **Parallel to Clay**: Both Clay and Flexbox UI can run in the same app
- **Commands-Only API**: No direct `World` access - all mutations via `Commands` system parameter

---

## Architecture

### Core Components

#### **FlexboxNode** (component)
Describes Flexbox layout properties for a UI entity.

```csharp
entity.Set(new FlexboxNode
{
    FlexDirection = FlexDirection.Row,
    JustifyContent = Justify.Center,
    AlignItems = Align.Center,
    Width = FlexValue.Points(400f),
    Height = FlexValue.Auto(),
    PaddingLeft = 16f,
    BackgroundColor = new Vector4(0.2f, 0.2f, 0.2f, 1f),
    BorderRadius = 8f
});
```

**Properties**:
- **Layout**: `FlexDirection`, `JustifyContent`, `AlignItems`, `AlignSelf`, `AlignContent`, `FlexWrap`, `PositionType`, `Display`, `Overflow`
- **Flex**: `FlexGrow`, `FlexShrink`, `FlexBasis`
- **Dimensions**: `Width`, `Height`, `MinWidth`, `MinHeight`, `MaxWidth`, `MaxHeight`
- **Spacing**: `MarginTop/Right/Bottom/Left`, `PaddingTop/Right/Bottom/Left`, `BorderTop/Right/Bottom/Left`
- **Position**: `Top`, `Right`, `Bottom`, `Left`
- **Rendering**: `BackgroundColor`, `BorderColor`, `BorderRadius`

#### **FlexboxText** (component)
Text content for a Flexbox UI element.

```csharp
entity.Set(new FlexboxText("Hello World", fontSize: 16f, color: Vector4.One));
```

#### **FlexboxNodeParent** (component)
Specifies parent relationship in the UI hierarchy (synced to ECS `Parent`/`Children`).

```csharp
entity.Set(new FlexboxNodeParent(parentEntityId, index: 0));
```

#### **FlexboxInteractive** (component)
Marks element as interactive (receives pointer events).

```csharp
entity.Set(new FlexboxInteractive { IsFocusable = true });
```

---

### Resources

#### **FlexboxUiState**
Holds Flexbox layout state and computed results.

```csharp
public sealed class FlexboxUiState
{
    public float ContainerWidth { get; set; }  // Root container dimensions
    public float ContainerHeight { get; set; }

    // Internal: entity → Flexbox.Node mapping
    // Internal: entity → ComputedLayout results
    // Internal: elementId → entityId mapping for hit testing
}
```

#### **FlexboxPointerState**
Accumulates pointer input (mouse/touch) for Flexbox UI.

```csharp
public sealed class FlexboxPointerState
{
    public Vector2 Position { get; set; }
    public bool PrimaryDown { get; set; }
    public Vector2 ScrollDelta { get; }
    public float DeltaTime { get; set; }
}
```

---

### Systems

The Flexbox UI plugin registers three core systems:

1. **`ui:flexbox:sync-hierarchy`** (PreUpdate)
   - Syncs `FlexboxNodeParent` → ECS `Parent`/`Children`
   - Ensures entity hierarchy matches UI parent relationships

2. **`ui:flexbox:layout`** (Update)
   - Builds Flexbox node tree from entity hierarchy
   - Computes layouts using Flexbox layout engine
   - Stores results in `FlexboxUiState.EntityToLayout`

3. **`ui:flexbox:pointer`** (PreUpdate, after sync-hierarchy)
   - Hit-tests pointer against computed layouts
   - Fires `UiPointerEvent` events for enter/exit/down/up/move/scroll
   - Propagates events up parent chain via `UiPointerTrigger`

---

## Plugins

### FlexboxUiPlugin
Core Flexbox UI plugin that registers layout and pointer systems.

```csharp
app.AddPlugin(new FlexboxUiPlugin
{
    AutoCreatePointerState = true,
    ContainerWidth = 1920f,
    ContainerHeight = 1080f
});
```

### RaylibFlexboxUiPlugin (in samples/)
Raylib integration for rendering Flexbox UI.

```csharp
app.AddPlugin(new RaylibFlexboxUiPlugin
{
    RenderingStage = customRenderingStage  // Optional
});
```

**Systems**:
- `ui:raylib:flexbox:update-pointer` (PreUpdate) - Updates `FlexboxPointerState` from Raylib mouse input
- `ui:raylib:flexbox:render` (custom stage) - Renders `FlexboxNode` entities using computed layouts

---

## Widgets

### FlexboxButtonWidget
Creates a clickable button with hover/pressed states.

```csharp
FlexboxButtonWidget.Create(
    commands,
    text: "Click Me",
    onClick: (world, trigger) => Console.WriteLine("Clicked!"),
    style: FlexboxButtonStyle.Default()
);
```

**Features**:
- Automatic interaction state updates (None → Hovered → Pressed)
- Customizable colors, padding, border radius
- Observer-based click handling

### FlexboxLabelWidget
Displays text with configurable styling.

```csharp
FlexboxLabelWidget.CreateHeading1(commands, "Title");
FlexboxLabelWidget.CreateBody(commands, "Body text");
FlexboxLabelWidget.CreateCaption(commands, "Small text");
```

**Presets**:
- `Heading1`, `Heading2`, `Heading3` - Bold headings (32px, 24px, 20px)
- `Body` - Regular text (16px)
- `Caption` - Small text (12px)

### FlexboxPanelWidget
Container with background, padding, and configurable layout direction.

```csharp
var panelId = FlexboxPanelWidget.CreateColumn(commands, FlexboxPanelStyle.Card())
    .Id;

// Add children
FlexboxLabelWidget.CreateBody(commands, "Child")
    .Insert(new FlexboxNodeParent(panelId));
```

**Presets**:
- `Default()` - Gray background, 16px padding
- `Transparent()` - No background, no padding
- `Card()` - Elevated card style, 20px padding, rounded corners

---

## Event System

Flexbox UI **reuses** the existing Clay UI event infrastructure:

### UiPointerEvent (global events)
Broadcast to all systems via `EventWriter<UiPointerEvent>`.

```csharp
app.AddSystem((EventReader<UiPointerEvent> events) =>
{
    foreach (var evt in events.Read())
    {
        if (evt.Type == UiPointerEventType.PointerDown)
            Console.WriteLine($"Clicked entity {evt.Target}");
    }
});
```

**Event Types**:
- `PointerEnter`, `PointerExit` - Hover changes
- `PointerDown`, `PointerUp` - Button press/release
- `PointerMove` - Movement within element
- (Note: `Scroll` may need to be added to `UiPointerEventType`)

### UiPointerTrigger (entity observers)
Per-entity observers for isolated widget behavior.

```csharp
commands.Spawn()
    .Insert(new FlexboxNode { ... })
    .Observe<On<UiPointerTrigger>>((world, trigger) =>
    {
        if (trigger.Event.Type == UiPointerEventType.PointerDown)
            Console.WriteLine("This entity was clicked!");
    });
```

**Key Pattern**: Use observers for single-entity behavior, global events for cross-cutting concerns.

### Interaction Component
Automatically updated by `UiInteractionSystems.UpdateInteractionState`.

```csharp
public enum Interaction
{
    None,
    Hovered,
    Pressed
}
```

**Usage**:
```csharp
// React to interaction changes
app.AddSystem((Query<Data<Interaction, FlexboxNode>, Filter<Changed<Interaction>>> widgets) =>
{
    foreach (var (interaction, node) in widgets)
    {
        ref var state = ref interaction.Ref;
        ref var nodeRef = ref node.Ref;

        nodeRef.BackgroundColor = state switch
        {
            Interaction.None => DefaultColor,
            Interaction.Hovered => HoverColor,
            Interaction.Pressed => PressedColor,
            _ => DefaultColor
        };
    }
});
```

---

## Comparison: Clay UI vs Flexbox UI

| Feature | Clay UI | Flexbox UI |
|---------|---------|------------|
| **Layout Engine** | Clay (immediate-mode, custom) | Flexbox (CSS model, Yoga-based) |
| **Components** | `UiNode`, `UiText`, `UiNodeParent` | `FlexboxNode`, `FlexboxText`, `FlexboxNodeParent` |
| **State Resource** | `ClayUiState` | `FlexboxUiState` |
| **Pointer Resource** | `ClayPointerState` | `FlexboxPointerState` |
| **Plugin** | `ClayUiPlugin` | `FlexboxUiPlugin` |
| **Event System** | ✅ Shared (`UiPointerEvent`, `UiPointerTrigger`) | ✅ Shared |
| **Interaction** | ✅ Shared (`Interaction`, `Interactive`) | ✅ Shared |
| **Rendering** | Clay render commands | Computed layouts (custom renderer) |
| **Text Measurement** | Built-in Clay callback | Manual measure function |
| **Scrolling** | Built-in scroll containers | Manual scroll implementation |
| **Floating Elements** | Built-in floating config | Manual absolute positioning |

---

## Usage Example

### Basic Setup

```csharp
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI;

var app = new App(ThreadingMode.Single);

// Add Flexbox UI plugin
app.AddPlugin(new FlexboxUiPlugin
{
    AutoCreatePointerState = true,
    ContainerWidth = 1280f,
    ContainerHeight = 720f
});

// Add Raylib renderer (from samples/)
app.AddPlugin(new RaylibFlexboxUiPlugin());

// Add interaction system
app.AddSystem(UiInteractionSystems.UpdateInteractionState)
    .InStage(Stage.PreUpdate)
    .Label("ui:flexbox:interaction")
    .After("ui:flexbox:pointer")
    .Build();

// Build UI in startup
app.AddSystem((Commands commands) =>
{
    // Create root container
    var rootId = commands.Spawn()
        .Insert(new FlexboxNode
        {
            FlexDirection = FlexDirection.Column,
            JustifyContent = Justify.Center,
            AlignItems = Align.Center,
            Width = FlexValue.Percent(100f),
            Height = FlexValue.Percent(100f)
        })
        .Id;

    // Add button
    FlexboxButtonWidget.Create(
        commands,
        text: "Click Me",
        onClick: (world, trigger) => Console.WriteLine("Button clicked!"),
        style: FlexboxButtonStyle.Default()
    ).Insert(new FlexboxNodeParent(rootId));
})
.InStage(Stage.Startup)
.Build();

app.RunStartup();

while (!windowShouldClose)
{
    app.Update();
}
```

### Dynamic UI Updates

```csharp
// Update text dynamically
commands.Entity(labelEntityId)
    .Insert(new FlexboxText($"Counter: {counter}", 16f, Vector4.One));

// Update layout properties
commands.Entity(buttonEntityId)
    .Insert(new FlexboxNode
    {
        // ... updated properties
    });

// Change parent
commands.Entity(childEntityId)
    .Insert(new FlexboxNodeParent(newParentId));
```

---

## Implementation Status

### ✅ Completed

- Core Flexbox layout system (`FlexboxLayoutSystems`)
- Pointer event processing (`FlexboxPointerSystems`)
- Plugin infrastructure (`FlexboxUiPlugin`)
- Components (`FlexboxNode`, `FlexboxText`, `FlexboxNodeParent`, `FlexboxInteractive`)
- Resources (`FlexboxUiState`, `FlexboxPointerState`)
- Basic widgets (`FlexboxButtonWidget`, `FlexboxLabelWidget`, `FlexboxPanelWidget`)
- Raylib renderer integration (`RaylibFlexboxUiPlugin`)
- Example application (`FlexboxUiExample.cs`)

### 🚧 Known Issues (compilation errors to fix)

1. **Interaction API mismatch**: Widgets use `Interaction.State` but should use `Interaction` enum directly
2. **Query deconstruction**: `.Ref` property access needs updating
3. **EntityCommands API**: `.Unset()` method name may be `.Remove()`
4. **Observer signature**: Observer callbacks need correct parameter count
5. **System registration**: Method group conversion to `ISystem`
6. **UiPointerEventType.Scroll**: May need to add this event type to enum
7. **Text measurement**: `ConfigureTextNode` uses `ref` parameter in lambda (not allowed)

### 🔮 Future Enhancements

- **Checkbox widget**: Toggle boolean input with visual state
- **Slider widget**: Numeric range input with drag tracking
- **Scroll container widget**: Scrollable containers with clipping
- **Floating window widget**: Draggable/resizable windows
- **Image widget**: Display textures/sprites in layout
- **Grid layout**: CSS Grid model for 2D layouts
- **Animations**: Transition properties over time
- **Themes**: Reusable style presets

---

## Rules & Best Practices

### Mandatory Rules

1. **Use TinyEcs.Bevy package**: All systems, plugins, and components must use TinyEcs.Bevy
2. **Prefer observers for single-widget behavior**: Use `entity.Observe<On<UiPointerTrigger>>()` for isolated reactions
3. **Forbidden to use World directly**: All mutations via `Commands` system parameter
4. **Use system parameters**: `Commands`, `Query`, `Res`, `ResMut`, `EventWriter`, `EventReader`
5. **No reflection**: Zero `GetType()`, `GetProperty()`, or runtime reflection

### Recommended Patterns

1. **Two-system interaction pattern**:
   - System 1 (PreUpdate): Update visuals based on state changes (`Changed<Interaction>`)
   - System 2 (Update): Handle pointer events to modify state

2. **Observer for widget-specific logic**:
   ```csharp
   commands.Spawn()
       .Insert(new FlexboxNode { ... })
       .Observe<On<UiPointerTrigger>>((world, trigger) =>
       {
           // Widget-specific click handling
       });
   ```

3. **Global event for cross-cutting concerns**:
   ```csharp
   app.AddSystem((EventReader<UiPointerEvent> events) =>
   {
       // Logging, analytics, debugging
   });
   ```

4. **FlexValue helpers**:
   - `FlexValue.Points(100f)` - Fixed pixel size
   - `FlexValue.Percent(50f)` - Percentage of parent
   - `FlexValue.Auto()` - Auto-size based on content

---

## File Structure

```
src/TinyEcs.UI/
├─ FlexboxUiState.cs              # Resource: layout state and results
├─ FlexboxPointerState.cs         # Resource: pointer input accumulation
├─ FlexboxNode.cs                 # Components: FlexboxNode, FlexboxText, etc.
├─ FlexboxLayoutSystems.cs        # Systems: hierarchy sync, layout computation
├─ FlexboxPointerSystems.cs       # Systems: hit testing, event dispatch
├─ FlexboxUiPlugin.cs             # Plugin: registers core systems
└─ Widgets/Flexbox/
    ├─ FlexboxButtonWidget.cs     # Button widget + systems
    ├─ FlexboxLabelWidget.cs      # Label widget
    └─ FlexboxPanelWidget.cs      # Panel widget

samples/MyBattleground/
├─ RaylibFlexboxUiPlugin.cs       # Raylib renderer integration
└─ FlexboxUiExample.cs            # Example application
```

---

## Migration from Clay UI

Both systems can coexist! To migrate incrementally:

1. **Keep Clay UI running**: No need to remove `ClayUiPlugin`
2. **Add Flexbox plugin**: `app.AddPlugin(new FlexboxUiPlugin())`
3. **Create new UI with Flexbox**: Use `FlexboxNode` instead of `UiNode`
4. **Reuse event systems**: Both share `UiPointerEvent` and `Interaction`
5. **Gradual transition**: Convert widgets one at a time

---

## Troubleshooting

**Layout not updating after property change**:
- Ensure you're inserting the entire `FlexboxNode` component (not just modifying fields)
- Call `state.MarkDirty()` if needed

**Pointer events not firing**:
- Check that entity has `FlexboxInteractive` component
- Verify `FlexboxPointerState` resource exists
- Ensure system runs after `ui:flexbox:pointer`

**Text not displaying**:
- Add `FlexboxText` component to entity
- Check that `RaylibFlexboxUiPlugin` is registered
- Verify text color alpha > 0

**Wrong Z-order (overlapping)**:
- Flexbox UI doesn't implement Z-index yet
- Render order is based on entity creation order

---

## Contributing

When adding features to Flexbox UI:

1. Follow reflection-free design (no `typeof()`, `GetType()`)
2. Use `Commands` exclusively (no direct `World` access)
3. Prefer observers for widget-specific behavior
4. Add unit tests in `tests/TinyEcs.Tests/FlexboxUiTests.cs`
5. Document new widgets in this README
6. Update `FlexboxUiExample.cs` with usage examples

---

## License

Same as TinyEcs project.

# TinyEcs.UI.Clay

A Clay layout library integration for TinyEcs.Bevy, providing high-performance retained-mode UI layout with platform-agnostic input handling.

## Overview

This library integrates the Clay layout library (https://github.com/nicbarker/clay) with TinyEcs.Bevy's ECS architecture to provide:

- **High-performance layout**: Microsecond-level layout calculations using Clay's flex-box-like system
- **Retained mode rendering**: Layout only recalculates when components change
- **Platform-agnostic input**: Bridge pattern allows any renderer/platform to provide input
- **ECS-native**: All UI elements are entities with components
- **Multi-threading safe**: Systems use proper system parameters
- **Zero-reflection**: Compatible with NativeAOT/bflat

## Architecture

### Components

#### `ClayNode`
Core layout and style component containing all Clay configuration:
- `Layout`: Clay_LayoutConfig (sizing, padding, gaps, alignment, direction)
- `Rectangle`: Clay_RectangleRenderData? (background rendering)
- `Border`: Clay_BorderElementConfig? (border configuration)
- `CornerRadius`: Clay_CornerRadius? (rounded corners)
- `Text`: Clay_TextElementConfig? (text styling)
- `Image`: Clay_ImageElementConfig? (image configuration)
- `Floating`: Clay_FloatingElementConfig? (floating/absolute positioning)
- `Clip`: Clay_ClipElementConfig? (scroll containers)
- `Custom`: Clay_CustomElementConfig? (custom rendering)

#### Marker Components
- `ClayElement`: Marks entity as managed by Clay
- `ClayDirty`: Marks layout as needing recalculation

#### Hierarchy Components
- `ClayParent`: Parent entity reference
- `ClayChildren`: List of child entity IDs

#### Computed Components
- `ClayElementId`: Links ECS entity to Clay element ID
- `ClayComputedLayout`: Layout bounds (X, Y, Width, Height) after calculation
- `ClayText`: Text content string
- `ClayScrollContainer`: Scroll state (offset, velocity)

### Resources

#### `ClayPointerState`
Platform-agnostic input state updated by renderer:
- `Position`: Pointer position
- `PrimaryDown/Pressed/Released`: Button state
- `ScrollDelta`: Mouse wheel / trackpad scroll
- `DeltaTime`: Time since last frame
- `EnableDragScrolling`: Drag-to-scroll flag

**Usage**: Renderer updates this resource each frame before `app.Update()`.

#### `ClayUiState`
Global Clay state:
- `Arena`: Clay memory arena
- `Context`: Clay context pointer
- `LayoutDimensions`: Current screen size
- `RenderCommands`: Output from Clay layout (pointer + length)
- `RootEntities`: List of root entities (no parent)
- `ClayIdToEntity`: Lookup table for interaction events
- `LayoutDirty`: Recalculation flag

### Plugins

#### `ClayUiPlugin` (Main Plugin)
Composes all sub-plugins and initializes Clay:
- Creates Clay arena and context
- Inserts resources
- Adds ClayLayoutPlugin, ClayInteractionPlugin, ClayHierarchyPlugin

**Systems**:
- `ResetPointerTransientState` (Stage.First): Clears pressed/released flags

#### `ClayLayoutPlugin`
Manages layout calculation in retained mode (Stage.PreUpdate):
1. `TrackRootEntities`: Updates root entity list
2. `MarkDirtyOnNodeChange`: Detects `Changed<ClayNode>`
3. `MarkDirtyOnHierarchyChange`: Detects parent/child changes
4. `CalculateLayout`: Builds Clay hierarchy and calculates layout (only if dirty)
5. `ReadComputedLayout`: Reads computed bounds back to ECS components

**Key behavior**: Layout only recalculates when `LayoutDirty` flag is set.

#### `ClayInteractionPlugin`
Handles pointer interactions (Stage.PostUpdate):
- Queries elements under pointer using `Clay.GetPointerOverIds()`
- Emits `ClayPointerEvent` events
- Emits `ClayPointerTrigger` triggers for observers
- Event types: Down, Up, Click, Scroll

#### `ClayHierarchyPlugin`
Synchronizes parent-child relationships:
- `OnParentInserted`: Adds entity to parent's children list
- `OnParentRemoved`: Removes entity from parent's children list

### Events

#### `ClayPointerEvent`
Emitted when pointer interacts with element:
- `EntityId`: Entity that was interacted with
- `EventType`: Down, Up, Click, Move, Enter, Exit, Scroll
- `Position`: Screen coordinates
- `LocalPosition`: Element-local coordinates
- `IsPrimaryButton`: Whether primary button is down
- `ScrollDelta`: Scroll amount (for Scroll events)

#### `ClayPointerTrigger`
Trigger wrapper for observers:
```csharp
entity.Observe<On<ClayPointerTrigger>>(trigger => {
    var evt = trigger.Event.Event;
    if (evt.EventType == ClayPointerEventType.Click) {
        // Handle click
    }
});
```

## Usage

### Basic Setup

```csharp
using var world = new World();
var app = new App(world);

// Add Clay UI plugin
app.AddClayUi(new ClayUiOptions
{
    LayoutDimensions = new Clay_Dimensions(800, 600),
    ArenaSize = 1024 * 1024,  // 1MB arena
    MaxElementCount = 1024,
    EnableDebugMode = false,
    EnableCulling = true
});

// Create UI in startup system
app.AddSystem((Commands commands) => {
    // Create root container
    var root = commands.SpawnClayElement(ClayNode.Default with {
        Layout = new Clay_LayoutConfig {
            sizing = new Clay_Sizing(
                Clay_SizingAxis.Grow(),
                Clay_SizingAxis.Grow()
            ),
            padding = Clay_Padding.All(16),
            childGap = 16
        }
    });

    // Create button
    var button = commands.SpawnClayElement(ClayNode.Default with {
        Layout = new Clay_LayoutConfig {
            sizing = new Clay_Sizing(
                Clay_SizingAxis.Fixed(200),
                Clay_SizingAxis.Fixed(60)
            )
        }
    });
    root.AddChild(button.Id);

    // Add click handler
    button.Observe<On<ClayPointerTrigger>>(trigger => {
        var evt = trigger.Event.Event;
        if (evt.EventType == ClayPointerEventType.Click) {
            Console.WriteLine("Button clicked!");
        }
    });
})
.InStage(Stage.Startup)
.Build();

app.RunStartup();
```

### Updating Input

Renderer/platform updates pointer state each frame:

```csharp
// Each frame before app.Update()
world.UpdateClayPointer(
    position: mousePosition,
    primaryDown: isMouseDown,
    primaryPressed: wasMousePressed,
    primaryReleased: wasMouseReleased,
    scrollDelta: scrollDelta,
    deltaTime: deltaTime
);

app.Update();
```

### Reading Render Commands

After layout calculation (PostUpdate stage), read render commands:

```csharp
app.AddSystem((Res<ClayUiState> state) => {
    var commands = state.Value.RenderCommands;

    foreach (ref readonly var cmd in commands) {
        switch (cmd.commandType) {
            case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_RECTANGLE:
                // Draw rectangle
                DrawRect(cmd.boundingBox, cmd.renderData.rectangle.color);
                break;
            case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_TEXT:
                // Draw text
                DrawText(cmd.renderData.text, cmd.boundingBox);
                break;
            case Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_BORDER:
                // Draw border
                break;
            // ... handle other command types
        }
    }
})
.InStage(Stage.Update)
.After("clay:calculate-layout")
.Build();
```

### Handling Window Resize

```csharp
// When window resizes
world.SetClayLayoutDimensions(newWidth, newHeight);
```

## Extension Methods

### App Extensions
- `app.AddClayUi()`: Add plugin with default options
- `app.AddClayUi(options)`: Add plugin with custom options

### Commands Extensions
- `commands.SpawnClayElement(node)`: Create Clay element entity
- `commands.SpawnClayText(text, config)`: Create text element
- Use standard `parent.AddChild(childId)` for hierarchy (no wrapper needed)

### World Extensions
- `world.SetClayLayoutDimensions(w, h)`: Update screen size
- `world.UpdateClayPointer(...)`: Update pointer state

## System Execution Order

### Stage.First
- `clay:reset-pointer` - Reset transient pointer state

### Stage.PreUpdate
- `clay:track-roots` - Update root entity list
- `clay:mark-dirty` - Mark dirty on ClayNode changes
- `clay:mark-dirty-hierarchy` - Mark dirty on hierarchy changes
- `clay:calculate-layout` - Calculate Clay layout (if dirty)
- `clay:read-layout` - Read computed layout to components

### Stage.PostUpdate
- `clay:interaction` - Process pointer interactions and emit events

## Performance Characteristics

- **Layout recalculation**: Only when components change (retained mode)
- **Layout time**: Microseconds for typical UIs (Clay is extremely fast)
- **Memory**: Static arena-based allocation (no GC pressure)
- **Threading**: Systems can run in parallel (proper access tracking)

## Current Status

⚠️ **Work in Progress** - The implementation is partially complete:

### ✅ Completed
- Component design
- Resource design
- Plugin architecture
- Layout calculation system
- Interaction system
- Hierarchy synchronization
- Extension methods

### 🚧 In Progress
- Fixing compilation errors (Clay API integration details)
- Example implementation
- Testing

### 📋 Todo
- Text measurement function integration
- Scroll container support
- Floating element support
- Custom element support
- Full example with Raylib renderer
- Documentation improvements

## Known Issues

1. **Compilation errors**: Need to adjust to actual Clay C# API structure
   - Clay_ElementDeclaration uses `backgroundColor`, `cornerRadius`, etc. directly
   - Not using separate render data structs as initially assumed

2. **Text measurement**: Need to hook up text measurement function from renderer

3. **Missing API details**: Some Clay API methods need correct namespace/access patterns

## Next Steps

1. Fix compilation errors by aligning with actual Clay API
2. Implement text measurement hook
3. Create working example with renderer
4. Add tests
5. Performance benchmarking

## Contributing

When making changes:
- Follow TinyEcs.Bevy patterns (system parameters, no direct World access)
- Use `entityCmd.AddChild(child)` for hierarchy
- Optimize queries with filters (With, Without, Changed, Optional)
- Consider multi-threading safety
- Update this README

## See Also

- [Clay library](https://github.com/nicbarker/clay)
- [TinyEcs.Bevy documentation](../../CLAUDE.md)
- [Clay documentation](../../externals/clay/README.md)

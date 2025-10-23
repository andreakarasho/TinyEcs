# Reactive UI Architecture (Bevy-Inspired)

This document describes the new reactive UI architecture for TinyEcs.UI, inspired by Bevy's observer-driven UI system.

## Overview

The reactive UI system replaces manual event handling with automatic state-driven behavior. Widget visuals update automatically when component state changes, using TinyEcs.Bevy's observer and change detection systems.

### Key Principles (from Bevy)

1. **Components represent state** - `Interaction`, `CheckboxState`, `SliderState`
2. **Systems update state** - Interaction detection from pointer events
3. **Observers react to changes** - Visual updates triggered by `Changed<T>` filters
4. **Events for high-level actions** - `OnClick`, `OnToggle`, `OnValueChanged`

## Architecture Components

### 1. Interaction Components (`UiInteraction.cs`)

**Core State Components:**

-   `Interaction` enum - Current interaction state (None/Hovered/Pressed)
-   `Interactive` - Marks elements as interactive (enables Interaction updates)
-   `Focused` - Currently focused element marker
-   `ComputedZIndex` - Computed rendering order from hierarchy traversal
-   `ZIndex` - Manual z-index override (local or global)

**Focus Management:**

-   `FocusManager` resource - Tracks global focus state
-   `FocusSource` enum - How element gained focus (Pointer/Keyboard/Programmatic)

**Event Triggers:**

-   `OnClick<TMarker>` - Element clicked (down + up on same element)
-   `OnToggle` - Checkbox state changed
-   `OnValueChanged` - Slider value changed
-   `OnFocusGained` / `OnFocusLost` - Focus state changes

### 2. Interaction Systems (`UiInteractionSystems.cs`)

**UpdateInteractionState**

-   Reads `UiPointerEvent` stream
-   Updates `Interaction` components on all `Interactive` entities
-   Runs in `Stage.PreUpdate` after `"ui:clay:pointer"`

**UpdateFocus**

-   Processes focus requests and pointer clicks
-   Updates `FocusManager` resource and `Focused` components
-   Emits `OnFocusGained` / `OnFocusLost` triggers

**ComputeZIndices**

-   Depth-first hierarchy traversal (like Bevy's `ui_z_system`)
-   Assigns `ComputedZIndex` based on:
    -   Hierarchical depth (parents before children)
    -   Sibling order (later siblings on top)
    -   Manual `ZIndex` component overrides
-   Runs in `Stage.Update` after layout

### 3. Widget Observers (`UiWidgetObservers.cs`)

**Reactive Visual Updates:**

```csharp
// Button: Reacts to Changed<Interaction>
OnButtonInteractionChanged(
    Query<Data<Interaction, ClayButtonStyle, UiNode>,
          Filter<Changed<Interaction>, With<Button>>>)
{
    // Automatically updates backgroundColor when interaction changes
}

// Checkbox: Reacts to Changed<CheckboxState>
OnCheckboxStateChanged(
    Query<Data<CheckboxState, CheckboxLinks, ClayCheckboxStyle>,
          Filter<Changed<CheckboxState>, With<Checkbox>>>)
{
    // Automatically updates visuals when checked state changes
}

// Slider: Reacts to Changed<SliderState>
OnSliderValueChanged(
    Query<Data<SliderState, SliderLinks, ClaySliderStyle>,
          Filter<Changed<SliderState>>>)
{
    // Automatically updates fill/handle position when value changes
}
```

**Event Detection:**

-   `OnButtonClicked` - Detects clicks and emits `OnClick<Button>` triggers
-   `OnCheckboxClicked` - Handles toggle and emits `OnToggle` triggers

### 4. Refactored Widgets

**ButtonWidget:**

```csharp
ButtonWidget.Create(commands, style, "Click Me", parent)
    .Insert(Interactive.Default)        // Enable interaction
    .Insert(Interaction.None)           // Initial state
    .Insert(new Button());              // Marker for observers
```

**CheckboxWidget:**

```csharp
CheckboxWidget.Create(commands, style, initialChecked, "Option", parent)
    .Insert(new CheckboxState { Checked = initialChecked })
    .Insert(Interactive.Default)
    .Insert(new Checkbox());
```

**User Code - Reacting to Events:**

```csharp
// React to any button click
app.AddObserver<OnClick<Button>>((trigger) =>
{
    Console.WriteLine($"Button {trigger.EntityId} clicked!");
});

// React to checkbox toggles
app.AddObserver<OnToggle>((trigger) =>
{
    Console.WriteLine($"Checkbox {trigger.EntityId} = {trigger.NewValue}");
});
```

## Plugin Architecture

### ReactiveUiPlugin (Recommended)

Comprehensive plugin that sets up the complete system:

```csharp
app.AddPlugin(new ReactiveUiPlugin
{
    Options = ClayUiOptions.Default,
    EnableWidgetObservers = true,        // Reactive visuals
    EnableInteractionDetection = true,   // Interaction component updates
    EnableFocusManagement = true,        // Focus tracking
    EnableZIndexComputation = true       // Hierarchical ordering
});

// Or simpler:
app.AddReactiveUi();
```

### Individual Plugins

For fine-grained control:

```csharp
app.AddClayUi();              // Core Clay layout system
app.AddUiInteraction();       // Interaction detection + focus + z-index
app.AddUiWidgetObservers();   // Reactive widget visuals
```

## System Execution Order

```
Stage.PreUpdate:
  1. ui:clay:sync-hierarchy     - Sync UiNodeParent to Parent/Children
  2. ui:clay:pointer            - Process pointer input
  3. ui:interaction:update      - Update Interaction components
  4. ui:interaction:focus       - Update FocusManager
  5. ui:observers:button-visuals   - React to Interaction changes
  6. ui:observers:checkbox-visuals - React to CheckboxState changes
  7. ui:observers:slider-visuals   - React to SliderState changes
  8. ui:clay:mark-nodes         - Mark changed nodes
  9. ui:clay:mark-text          - Mark changed text

Stage.Update:
  1. ui:observers:button-click     - Detect button clicks
  2. ui:observers:checkbox-toggle  - Detect checkbox toggles
  3. ui:clay:layout                - Clay layout pass
  4. ui:interaction:compute-z      - Compute Z-indices
```

## Comparison: Old vs New

### Old Approach (Entity Observers)

```csharp
// Each widget has its own observer
button.Observe<UiPointerTrigger>((trigger, query) =>
{
    var evt = trigger.Event;
    // Manually handle all pointer events
    switch (evt.Type)
    {
        case PointerEnter: /* update visuals */ break;
        case PointerExit:  /* update visuals */ break;
        case PointerDown:  /* update visuals */ break;
        case PointerUp:    /* update visuals */ break;
    }
});
```

**Problems:**

-   Duplicate logic across all widget instances
-   No centralized state tracking
-   Hard to implement global features (focus, keyboard nav)
-   Observers fire per-entity (not batched)

### New Approach (Reactive Components)

```csharp
// Widget just declares its state
button.Insert(Interactive.Default);
button.Insert(Interaction.None);
button.Insert(new Button());

// System updates Interaction automatically
UpdateInteractionState(events, interactives);

// Observer reacts to state changes (batched)
OnButtonInteractionChanged(
    Query<Filter<Changed<Interaction>, With<Button>>>)
{
    // Update all changed buttons at once
}
```

**Benefits:**

-   Single observer per widget type (not per instance)
-   Batched processing via queries
-   Centralized interaction logic
-   Easy to add global features (focus, z-ordering)
-   Follows Bevy's proven architecture

## Migration Guide

### For Widget Implementers

**Before:**

```csharp
widget.Observe<UiPointerTrigger>((trigger) =>
{
    // Handle events manually
});
```

**After:**

```csharp
widget.Insert(Interactive.Default);
widget.Insert(Interaction.None);
widget.Insert(new MyWidgetMarker());

// Add observer in plugin
app.AddObserver<OnInsert<Interaction>>((trigger) =>
{
    // React to state changes
});
```

### For Users

**Before:**

```csharp
// Had to use entity observers to react to clicks
button.Observe<UiPointerTrigger>((trigger) =>
{
    if (trigger.Event.Type == PointerDown)
        DoSomething();
});
```

**After:**

```csharp
// Use high-level event triggers
app.AddObserver<OnClick<Button>>((trigger) =>
{
    if (trigger.EntityId == myButtonId)
        DoSomething();
});
```

## Z-Index and Rendering Order

Like Bevy, rendering order is computed via hierarchy traversal:

```csharp
ComputeZIndices(roots, allNodes, childrenQuery, zIndexQuery, commands);
```

**Computation Rules:**

1. Parents render before children
2. Later siblings render on top of earlier siblings
3. Manual `ZIndex.Local` adds offset within hierarchy level
4. Manual `ZIndex.Global` overrides all hierarchy (e.g., tooltips, modals)

**Usage:**

```csharp
// Local z-index (within parent context)
entity.Insert(ZIndex.FromLocal(10));

// Global z-index (always on top)
modal.Insert(ZIndex.FromGlobal(1000));
```

**Note:** Unlike Bevy, we use Clay for layout (not Taffy), and we sort elements instead of using native z-index (since Clay doesn't have z-index beyond floating elements).

## Performance Considerations

**Advantages:**

-   Change detection (`Changed<T>`) means observers only run when needed
-   Queries batch all entities with same components
-   No per-entity observer overhead

**Best Practices:**

-   Use `Changed<T>` filters to avoid processing unchanged entities
-   Mark components with `MarkChanged<T>` only when necessary
-   Group related state in single components (e.g., `CheckboxState`)

## Future Enhancements

1. **Keyboard Navigation** - Tab/arrow key focus movement
2. **Accessibility** - ARIA-like attributes, screen reader support
3. **Animation** - Transition states (e.g., button press animation)
4. **Layout Invalidation** - Only recompute layout when needed
5. **Render Batching** - Sort by ComputedZIndex before render pass

## Related Files

-   `src/TinyEcs.UI/UiInteraction.cs` - Component definitions
-   `src/TinyEcs.UI/UiInteractionSystems.cs` - System implementations
-   `src/TinyEcs.UI/Widgets/UiWidgetObservers.cs` - Widget-specific observers
-   `src/TinyEcs.UI/ReactiveUiPlugin.cs` - Comprehensive plugin
-   `src/TinyEcs.UI/Widgets/ButtonWidget.cs` - Refactored button
-   `src/TinyEcs.UI/Widgets/CheckboxWidget.cs` - Refactored checkbox

## References

-   Bevy UI: https://bevyengine.org/learn/book/getting-started/ui/
-   Bevy Observer Pattern: https://bevyengine.org/news/bevy-0-15/#ecs-hooks-and-observers
-   TinyEcs.Bevy: `src/TinyEcs.Bevy/BevyObservers.cs`

# TinyEcs.UI Reactive Architecture - Implementation Summary

## What Was Done

Implemented a **Bevy-inspired reactive UI system** for TinyEcs.UI that replaces per-entity observers with centralized state-driven behavior.

## New Files Created

### Core System Files

1. **UiInteraction.cs** - Components and types for reactive interaction

    - `Interaction` enum (None/Hovered/Pressed)
    - `Interactive` marker component
    - `Focused` component & `FocusManager` resource
    - `ComputedZIndex` & `ZIndex` for rendering order
    - Event triggers: `OnClick<T>`, `OnToggle`, `OnValueChanged`, `OnFocusGained/Lost`

2. **UiInteractionSystems.cs** - Systems for interaction detection

    - `UpdateInteractionState` - Updates Interaction from pointer events
    - `UpdateFocus` - Manages focus state
    - `ComputeZIndices` - Depth-first hierarchy traversal for render order
    - `UiInteractionPlugin` - Registers all systems

3. **Widgets/UiWidgetObservers.cs** - Reactive widget behaviors

    - `OnButtonInteractionChanged` - Auto-updates button colors via `Changed<Interaction>`
    - `OnCheckboxStateChanged` - Auto-updates checkbox visuals via `Changed<CheckboxState>`
    - `OnSliderValueChanged` - Auto-updates slider position via `Changed<SliderState>`
    - Click detection systems that emit high-level events
    - `UiWidgetObserversPlugin` - Registers all observers

4. **ReactiveUiPlugin.cs** - Comprehensive plugin

    - Bundles ClayUi + Interaction + Widget Observers
    - `app.AddReactiveUi()` - One-line setup

5. **Examples/ReactiveUiExample.cs** - Usage demonstrations

    - Basic reactive UI example
    - Custom widget observers
    - Focus management

6. **REACTIVE_UI_ARCHITECTURE.md** - Complete documentation

## Refactored Files

### ButtonWidget.cs

**Before:** Entity observer handling all pointer events

```csharp
button.Observe<UiPointerTrigger>((trigger) => { /* manual event handling */ });
```

**After:** Declarative components + automatic reactivity

```csharp
button.Insert(Interactive.Default);
button.Insert(Interaction.None);
button.Insert(new Button());
// Visuals update automatically via UiWidgetObservers
```

### CheckboxWidget.cs

**Before:** Entity observer with manual state + visual updates
**After:** `CheckboxState` component + reactive observer that updates visuals on state change

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│ User Input (ClayPointerState)                               │
└───────────────────────┬─────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ ClayUiSystems.ApplyPointerInput                             │
│ → Emits UiPointerEvent stream                               │
└───────────────────────┬─────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ UiInteractionSystems.UpdateInteractionState                 │
│ → Updates Interaction component on all Interactive entities │
└───────────────────────┬─────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ UiWidgetObservers (React to Changed<Interaction>)           │
│ → OnButtonInteractionChanged updates backgroundColor        │
│ → OnCheckboxStateChanged updates visuals                    │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ High-Level Events (OnClick, OnToggle)                       │
│ → User observers react to semantic events                   │
└─────────────────────────────────────────────────────────────┘
```

## Key Benefits

### 1. **Follows Bevy's Proven Pattern**

-   Components represent state (`Interaction`, `CheckboxState`)
-   Systems update state based on input
-   Observers react to state changes via `Changed<T>` filters
-   Events for high-level actions

### 2. **Performance**

-   Batched processing via queries (all buttons processed together)
-   Change detection means observers only run when needed
-   No per-entity observer overhead

### 3. **Extensibility**

-   Easy to add new widget types (just add marker component)
-   Global observers can react to any widget
-   Custom behavior via marker components + observers

### 4. **Reflection-Free**

-   All using TinyEcs.Bevy system parameters (Query, Commands, EventReader, etc.)
-   No World.Get() or World.Has() calls
-   Fully AOT-compatible

### 5. **Hierarchical Ordering**

-   Automatic Z-index computation via depth-first traversal
-   Matches Bevy's `ui_z_system` approach
-   Manual overrides via `ZIndex` component

## Usage Example

```csharp
// Setup
var app = new App(ThreadingMode.Single);
app.AddReactiveUi(); // One line!

// Create widgets
ButtonWidget.Create(commands, style, "Click Me", parent);
CheckboxWidget.Create(commands, style, false, "Option", parent);

// React to events
app.AddObserver<OnClick<Button>>((trigger) =>
{
    Console.WriteLine($"Button {trigger.EntityId} clicked!");
});

app.AddObserver<OnToggle>((trigger) =>
{
    Console.WriteLine($"Checkbox = {trigger.NewValue}");
});
```

## System Execution Order

```
Stage.PreUpdate:
  ui:clay:pointer              (process input)
  ui:interaction:update        (update Interaction components)
  ui:interaction:focus         (update focus)
  ui:observers:button-visuals  (react to Interaction changes)
  ui:observers:checkbox-visuals
  ui:observers:slider-visuals

Stage.Update:
  ui:observers:button-click    (detect clicks, emit OnClick)
  ui:observers:checkbox-toggle (toggle state, emit OnToggle)
  ui:clay:layout               (Clay layout pass)
  ui:interaction:compute-z     (compute z-indices)
```

## Migration Path

### Old Entity Observers (still work)

```csharp
widget.Observe<UiPointerTrigger>((trigger) => { /* handle events */ });
```

### New Reactive System (recommended)

```csharp
widget.Insert(Interactive.Default);
widget.Insert(Interaction.None);
// Automatic visual updates via observers
```

Both patterns can coexist during migration.

## What's Next

The reactive system is ready for:

1. **Keyboard navigation** - Tab/arrow focus movement (foundation in place)
2. **Accessibility** - Focus tracking enables screen reader support
3. **Animations** - Transition between Interaction states
4. **Custom widgets** - Easy to add new reactive widget types

## Files Modified Summary

**Created (7 new files):**

-   `UiInteraction.cs`
-   `UiInteractionSystems.cs`
-   `Widgets/UiWidgetObservers.cs`
-   `ReactiveUiPlugin.cs`
-   `Examples/ReactiveUiExample.cs`
-   `REACTIVE_UI_ARCHITECTURE.md`
-   This summary

**Refactored (2 files):**

-   `Widgets/ButtonWidget.cs` - Now uses Interaction component
-   `Widgets/CheckboxWidget.cs` - Now uses Interaction component

**Unchanged:**

-   `ClayUiPlugin.cs` - Core Clay system (works with new reactive layer)
-   `Widgets/UiWidgetsPlugin.cs` - Can coexist with new system
-   Other widgets (SliderWidget, FloatingWindowWidget, etc.) - Can be migrated later

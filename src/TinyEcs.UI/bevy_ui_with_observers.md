# Bevy UI Rendering and Ordering System

Bevy's UI system is entirely **ECS-driven** --- every UI element
(button, panel, text, image, etc.) is an **entity** composed of
**components**. These components define layout, appearance, and
interactivity. The entire system is declarative and data-oriented.

------------------------------------------------------------------------

## 1. UI Structure and Components

UI elements are built from **bundles**, which are collections of common
components:

-   **`Node`** -- Marks an entity as a UI node.
-   **`Style`** -- Defines layout rules (using Flexbox via Taffy).
-   **`BackgroundColor`**, **`UiImage`**, **`BorderColor`** -- Define
    visual appearance.
-   **`Text`** -- For text rendering.
-   **`Interaction`** -- Marks an element as interactive.
-   **`Parent`** / **`Children`** -- Define hierarchy.

Each UI element is part of a **hierarchical tree**, where the parent's
layout determines how its children are positioned.

------------------------------------------------------------------------

## 2. Layout and Flexbox Engine

Bevy's layout engine is powered by **Taffy**, a Rust Flexbox
implementation.\
Each frame, Bevy computes the layout for every visible UI node:

``` rust
Style {
    size: Size::new(Val::Percent(100.0), Val::Px(50.0)),
    justify_content: JustifyContent::Center,
    align_items: AlignItems::Center,
    ..default()
}
```

Layouts are automatically recalculated when window size or style
properties change.

------------------------------------------------------------------------

## 3. Hierarchical Composition

The UI is composed as a **tree of entities**:

    Root Node
     ├── Header
     │    └── Title Text
     └── Button
          └── Label

This hierarchy defines both **spatial layout** and **render order**.

------------------------------------------------------------------------

## 4. Interaction with Observers

In the new Bevy UI model, **interaction is handled via Observers**
rather than Systems.

An **observer** listens for component state changes (such as
`Interaction` events) directly within the ECS, allowing event-driven
logic.

Example:

``` rust
fn button_observer(mut observer: Observer<Button>, mut color: Mut<BackgroundColor>) {
    if observer.changed::<Interaction>() {
        match observer.component::<Interaction>() {
            Interaction::Clicked => *color = Color::RED.into(),
            Interaction::Hovered => *color = Color::YELLOW.into(),
            Interaction::None => *color = Color::GRAY.into(),
        }
    }
}
```

This reactive approach makes UI logic **more modular and efficient**,
since observers respond only when data changes rather than polling every
frame.

------------------------------------------------------------------------

## 5. UI Rendering Process

Bevy's renderer processes UI in **two ECS worlds**:

1.  **App World** -- Contains gameplay and UI entities.
2.  **Render World** -- Optimized ECS for GPU rendering.

Each frame: 1. **Extract Phase** -- Copies visible UI nodes into the
render world. 2. **Prepare Phase** -- Builds vertex buffers, applies
layouts. 3. **Queue Phase** -- Sorts and batches draw calls. 4. **Render
Phase** -- Draws quads to the screen.

------------------------------------------------------------------------

## 6. Draw Order and Z-Index Computation

Bevy determines which entity is drawn first based on **hierarchical
traversal** and **Z-indexing**.

### Process:

1.  **`ui_z_system`** walks the UI tree depth-first.
2.  Each entity receives a **`ComputedZIndex`**, derived from:
    -   Hierarchical depth
    -   Sibling order
    -   `ZIndex` component (if present)
3.  During the **Queue Phase**, all draw calls are **sorted by
    ComputedZIndex**.

### Example Ordering

  Entity    Hierarchy    ZIndex   ComputedZIndex   Draw Order
  --------- ------------ -------- ---------------- ------------
  Root      Top-level    \-       0.00             1st
  Panel     Child        \-       0.10             2nd
  Button    Child        \-       0.20             3rd
  Label     Child        \-       0.30             4th
  Overlay   Global(10)   +10      10.00            Last

Thus, **parents draw before children**, and **later siblings draw over
earlier ones**.

------------------------------------------------------------------------

## 7. Rendering Phases Summary

  Stage     Responsibility                        Key Systems
  --------- ------------------------------------- --------------------
  Layout    Compute positions, sizes (Taffy)      `ui_layout_system`
  Z-Index   Compute draw order                    `ui_z_system`
  Extract   Copy visible data into render world   `extract_ui_nodes`
  Prepare   Create GPU buffers                    `prepare_ui_nodes`
  Queue     Sort & batch by Z-index               `queue_ui_draws`
  Render    Execute GPU draw calls                `draw_ui_pass`

------------------------------------------------------------------------

## 8. ZIndex Overrides

You can manually override draw order:

``` rust
commands.spawn((
    NodeBundle::default(),
    ZIndex::Global(10), // Always on top
));
```

------------------------------------------------------------------------

## 9. Summary

-   Bevy UI is **ECS-driven and declarative**.
-   Layout uses **Flexbox** for dynamic sizing and alignment.
-   **Observers** replace traditional systems for interaction handling.
-   Draw order is computed via **hierarchy traversal + ZIndex**.
-   Rendering is fully GPU-accelerated and sorted by computed depth.

------------------------------------------------------------------------

### ✅ Key Insight

> Bevy determines UI draw order through **depth-first traversal** of the
> UI hierarchy, assigning each node a `ComputedZIndex`.\
> UI interactions are now event-driven via **Observers**, responding
> only when states change, making the system both efficient and
> reactive.

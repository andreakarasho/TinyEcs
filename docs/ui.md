# UI Layer (`TinyEcs.Bevy.UI`)

ECS-driven retained-mode UI built on the Bevy layer, with layout from
[Clay.NET](https://github.com/andreakarasho/Clay.NET) and Bevy-style
component types (`Node`, `BackgroundColor`, `Interaction`, …).

## Project requirements
- Targets: .NET 9.0 / 10.0
- Depends on `TinyEcs.Bevy` and the Clay.NET submodule
- Renderer-agnostic — the UI assembly only produces `RenderCommand`s; the
  caller draws them (the sample uses Raylib).

## Plugin setup

```csharp
var world = new World();
var app   = new App(world, ThreadingMode.Single); // UI systems run single-threaded
app.AddPlugin(new UiPlugin
{
    LogicalSize = new Vector2(1280, 720),
    MaxElements = 8192,
    TextMeasurer = new MyTextMeasurer(),   // optional; default falls back to a basic one
});
```

`UiPlugin` adds four custom stages between `Stage.Update` and `Stage.Last`:

| Stage              | Job                                                |
|--------------------|----------------------------------------------------|
| `UiPickPreGestureStage` | (after `Stage.First`) refresh `UiPickCache` for the frame's gestures. |
| `UiPreLayoutStage` | Feed pointer state to Clay, reset `Interaction`.   |
| `UiLayoutStage`    | Walk the entity tree, declare to Clay, `EndLayout`, write `ComputedNode`. |
| `UiPickPostLayoutStage` | Refresh `UiPickCache` against the fresh layout. |
| `UiPostLayoutStage`| Resolve hover / press from the pick, fire `UiClick` / `UiOver` / etc. |
| `UiRenderStage`    | Publish render commands to `UiRenderCommands`.     |

Resources registered: `UiScale`, `UiSurface`, `UiPointer`, `UiRenderCommands`,
`UiClayContext`, `UiTextureRegistry`, `UiFontRegistry`, `UiPickCache`,
`UiGestureGate`.

## The relayout gate

Clay is immediate-mode, but the tree only needs re-solving when a layout INPUT
changed, so `UiLayoutStage` is gated. The signal is **change ticks**: one
`Changed<T>` query per layout component (`Node`, `BackgroundColor`,
`BorderColor`, `BorderRadius`, `UiImage`, `Text`, `TextFont`, `TextColor`,
`TextWrap`, `ZIndex`, `GlobalZIndex`, `BoxShadow`, `UiCustom`,
`ScrollPosition`), plus a stored-last compare for `UiSurface.LogicalSize` /
`UiScale` (resources have no ticks).

**Contract for producers**: `commands.Insert(...)` / `entity.Set(...)` bump the
tick for you. An **in-place `.Ref` write does not** — Bevy's `set_if_neq`
situation. After mutating a layout component through a query ref, mark it:

```csharp
foreach (var (e, node) in nodes)
{
    var left = Val.Px(x);
    if (node.Ref.Left == left) continue;   // never mark a no-op write:
    node.Ref.Left = left;                  // the gate would never close
    nodes.SetChanged<Node>(e.Ref);
}
```

Structural edits ticks cannot see are covered by observers the plugin
registers: `OnRemove<Node>`, `OnRemove<T>` for every other layout component,
and `OnInsert`/`OnRemove<Parent>` (add-child and reparent); each marks its
entity. Anything else
invisible to both — a font or text-measurer registered after the text nodes
exist, retained Clay state poked directly — calls
`UiClayContext.MarkLayoutDirty()`.

**Timing.** The gate's window is `(this system's previous run, this run]`, and
the change tick advances per system run (see docs/bevy.md, "The
change-detection window"). So a gameplay system that mutates a layout component
earlier in the same frame opens the gate on **that** frame — the UI never lags a
mutation — and it opens it **once**, so a one-shot marked write costs one Clay
pass, not two.

A pass that resizes an element still costs a second pass, for an unrelated
reason: `ComputedNode` is layout OUTPUT, `BuildDecl` reads the PARENT's
`ComputedNode` for `Right`/`Bottom` anchoring, so the writeback marks every
element whose size changed until the feedback settles. That shows up as
`SystemProfiler.LayoutDirtyMask` bit `UiLayoutChanged.ForceBit` on the follow-up
frame, with the component's own probe bit clear.

**Per-root cache.** A relayout re-walks (ECS lookups + declaration build) only
the roots that hold a dirty entity: one with a changed layout input, one an
observer marked (removal, reparent, despawn — `UiClayContext.DirtyEntities`),
or one whose size the writeback changed. A dirty entity also dirties the root it
was last emitted under, so a node that moved or died leaves that root's stream.
Every other root replays the declarations it recorded last time straight into
Clay. `MarkLayoutDirty()` and a surface / `UiScale` change re-walk every root.
`SystemProfiler.LayoutReplayed` counts the replayed roots.

A skipped frame leaves the retained Clay tree, `LastCommands` and every
`ComputedNode` untouched, so hit-testing and pointer events stay live;
`RenderSystem.Publish` and the `ComputedNode` writeback are themselves gated on
`UiClayContext.LayoutGeneration`, which only advances on a real relayout.
`SystemProfiler.LayoutSkipped` / `LayoutDirtyMask` (enable with
`TINYECS_PROFILE=1`) diagnose a gate stuck open.

## Anatomy of a UI element

A node is an entity with at least:
- `Node` — layout (display, size, flex, padding, …). The layout pass treats
  any `Node`-bearing entity without a `Parent` as a UI root, so a top-level
  node needs nothing extra; child nodes get attached with `commands.AddChild`.
- Optional: `BackgroundColor`, `BorderColor`, `BorderRadius`, `Outline`,
  `BoxShadow`, `Text`/`TextFont`/`TextColor`, `UiImage`, `ZIndex` /
  `GlobalZIndex`, `Interaction`, `FocusPolicy`, `Button`.

```csharp
app.AddSystem((Commands c) =>
{
    var root = c.Spawn()
        .Insert(new Node
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Column,
            Width = Val.Px(400), Height = Val.Px(300),
            Padding = UiRect.All(12),
            Gap = Val.Px(8),
        })
        .Insert(new BackgroundColor(Color.Rgba(20, 20, 24, 255)));

    var label = c.Spawn()
        .Insert(new Node { Width = Val.Auto, Height = Val.Auto })
        .Insert(new Text("Hello"))
        .Insert(new TextFont { FontId = 0, Size = 18 })
        .Insert(new TextColor(Color.White));

    c.AddChild(root, label);
})
.InStage(Stage.Startup).SingleThreaded().Build();
```

## `Node`

Layout container modeled after Bevy's `Node`:

```csharp
public struct Node
{
    public Display Display;            // Flex | None
    public PositionType PositionType;  // Relative | Absolute
    public Overflow Overflow;          // Visible | Clip | Scroll
    public FlexDirection FlexDirection;
    public JustifyContent JustifyContent;
    public AlignItems AlignItems;

    public Val Width, Height;
    public Val MinWidth, MinHeight, MaxWidth, MaxHeight;
    public Val Left, Top, Right, Bottom;   // only respected on Absolute
    public UiRect Padding;
    public UiRect Border;
    public Val Gap;
    public float AspectRatio;
}
```

`Node.Default` carries sane defaults (`Display.Flex`, `Auto` sizes, etc).

### `Val`

```csharp
Val.Auto                // size to content
Val.Px(16)              // pixels
Val.Percent(50)         // % of parent
```

### `UiRect`

```csharp
UiRect.Zero
UiRect.All(8)
UiRect.Symmetric(horizontal: 8, vertical: 4)
UiRect.Horizontal(8)
UiRect.Vertical(4)
```

### Absolute positioning

```csharp
new Node
{
    PositionType = PositionType.Absolute,
    Right  = Val.Px(10),
    Bottom = Val.Px(15),
    Width  = Val.Px(40),
    Height = Val.Px(20),
}
```

`Left` / `Top` / `Right` / `Bottom` are only honored when
`PositionType == Absolute`. `Right` / `Bottom` anchoring needs the parent's
`ComputedNode`, so they take effect from the second frame onward.

## Visual components

| Type            | Purpose                                                     |
|-----------------|-------------------------------------------------------------|
| `BackgroundColor` | Solid fill behind the node.                               |
| `BorderColor`     | Border stroke color (width comes from `Node.Border`).     |
| `BorderRadius`    | Per-corner radius. Use `BorderRadius.All(r)` for uniform. |
| `Outline`         | Outset stroke outside the border.                         |
| `BoxShadow`       | Drop shadow (offset / blur / spread).                     |
| `UiImage`         | Image fill (texture id + tint).                           |
| `Text` / `TextFont` / `TextColor` | Text content + font id/size + color.      |

### `ZIndex` and `GlobalZIndex`

- `ZIndex { Value }` — local layer. Higher wins over siblings.
- `GlobalZIndex { Value }` — overrides `ZIndex` and is treated as global.

**Caveat:** Clay only z-sorts floating elements, so z-index is currently
respected on `PositionType.Absolute` nodes. Non-absolute siblings stack in
tree declaration order.

## Bundles

Convenience aggregates that insert a common set of components:

| Bundle         | Inserts                                                |
|----------------|--------------------------------------------------------|
| `NodeBundle`   | `Node`, `BackgroundColor` (if non-transparent).        |
| `TextBundle`   | `Node`, `Text`, `TextFont`, `TextColor`.               |
| `ButtonBundle` | `Node`, `Button`, `Interaction`, `FocusPolicy`, `BackgroundColor`. |
| `UiControl`    | `Interaction`, `UiNoWindowDrag`, `UiContainsByBounds`.  |
| `ImageBundle`  | `Node`, `UiImage`.                                     |

```csharp
app.AddSystem((Commands commands) =>
{
    commands.SpawnBundle(new ButtonBundle
    {
        Node = new Node { Width = Val.Px(120), Height = Val.Px(36) },
        Background = new BackgroundColor(Color.Rgba(50, 60, 80, 255)),
        Focus = FocusPolicy.BlockAll,
    });
})
.InStage(Stage.Startup).Build();
```

## Interaction

Add `Interaction` to a node to opt into hit-testing. The post-layout pass
sets it each frame:

```csharp
public enum Interaction : byte { None, Hovered, Pressed }
```

`FocusPolicy { Block }` controls whether the node consumes the pointer
(`true` — default) or lets it fall through. `Button` gets its behaviour
(`Activate`) from `ButtonPlugin` (see Widgets).

Sparse state markers, inserted / removed through `Commands` only on
transitions (filter on `With<Hovered>` / `Added<Pressed>` instead of polling):

| Marker                | Meaning                                                   |
|-----------------------|-----------------------------------------------------------|
| `Hovered`             | The `Interaction` entity under the pointer.               |
| `Pressed`             | A left press began here and the button is still held.     |
| `InteractionDisabled` | Still paints and blocks the pick, but gets no Interaction state, markers or pointer triggers. |
| `Checked`             | On state of a checkbox / radio button (the marker IS the state). |
| `Selected`            | The chosen list item / tab (the marker IS the state).     |
| `Focused`             | On the entity holding `InputFocus` (kept by `UiFocusPlugin`). |

`UiHoverTint { Normal, Hover }` + `UiHoverTintPlugin` swap a node's
`BackgroundColor` from its `Interaction` (a `Focused` node shows `Hover` too).

`UiGestureGate` arbitrates drags: the first gesture system to see a press
`TryClaim`s it (`UiGestureMode.Move / Resize / Window`), the others skip
latching until the owner `Release`s it on button-up. `UiWindowPlugin`'s drag
claims `Window`.

## Pointer events

Fired as entity-targeted triggers via `commands.EmitTrigger` during
`UiPostLayoutStage`. Observe globally or per-entity:

| Trigger          | When                                                      |
|------------------|-----------------------------------------------------------|
| `UiOver`         | Pointer entered this entity (transition from non-hover).  |
| `UiOut`          | Pointer left this entity.                                 |
| `UiPointerDown`  | Pointer pressed over this entity (press edge).            |
| `UiPointerUp`    | Pointer released — fires on the **press origin** entity.  |
| `UiClick`        | Press + release happened on the **same** entity.          |
| `UiDoubleClick`  | Second `UiClick` on same entity within `DoubleClickWindow`.|
| `UiMove`         | Pointer moved while over this entity. Carries `Delta`.    |
| `UiScroll`       | Wheel input dispatched to entity under pointer (`Delta`). |

`UiMove` skips the frame an entity first becomes hovered (that's a `UiOver`)
and only fires when the cursor actually moved. `UiScroll` reads the host-fed
`UiClayContext.ScrollDelta` (same input that drives scroll containers) and
dispatches it to the hovered entity; `Delta` follows Bevy's convention
(positive Y = scrolled up).

**All UI triggers are emitted with `propagate: true`** — after the hit
entity's observers run, the trigger bubbles up the parent chain so a
container can react to a child button's click. An observer can stop the
bubble by calling `trigger.Propagate(false)` (triggers are passed by ref so
this works).

```csharp
app.AddObserver<On<UiClick>>((world, trigger) =>
{
    // Container observes child clicks via propagation.
    Console.WriteLine($"click bubbled through #{trigger.EntityId}");
    // Stop if a modal owns this subtree.
    if (world.Entity(trigger.EntityId).Has<ModalRoot>())
        trigger.Propagate(false);
});
```

```csharp
app.AddSystem((Commands commands) =>
{
    commands.SpawnBundle(new ButtonBundle { /* ... */ })
        .Observe<On<UiClick>>(t => Console.WriteLine("clicked"))
        .Observe<On<UiOver>>(t  => Console.WriteLine("hover in"))
        .Observe<On<UiOut>>(t   => Console.WriteLine("hover out"));
})
.InStage(Stage.Startup).Build();
```

`RelativeCursorPosition { Normalized, InBounds }` is written to the hovered
entity each frame — useful for hover gradients, drag handles, etc.

## Hit-testing

There is ONE hit-test, `UiPick` (`Picking.cs`), shared by the Interaction /
pointer-event pipeline and by any host gesture (window drag, close, widget
lookups). `UiPick.Topmost` returns the topmost **pick surface** under the
point, ranked by `ComputedNode.PaintOrder` (render-command index: z, then tree
order). A pick surface is an element that paints — `UiCustom` (even with null
`Data`), `BackgroundColor` or `Text` — or is tagged `UiContainsByBounds`. Bare
layout nodes and image/border-only elements are NOT surfaces (tag them
`UiContainsByBounds` to make their box hit). It also drops elements under a
`Display.None` ancestor and clips by `Overflow.Scroll/Clip` ancestors; an
`Absolute` float escapes ancestor clips unless it carries `ClipToParent`,
matching what the renderer draws.

A painted element blocks whatever is behind it whether or not it is
interactive. The `Interaction` target is the nearest `Interaction`
ancestor-or-self of the topmost hit (a label inside a button clicks the
button). `UiPick.MovableRoot` / `AncestorWith<T>` walk a hit to its window or
widget.

`UiGesturePick` is a composite system param bundling the queries plus
`UiPickCache` (`Topmost`, `TopmostMovable`, `TopmostWith(query)`, `Hits`,
`IsTopmostWindow`); answers for the cached pointer position are free.

Pixel-perfect picking — set the hook on `UiClayContext`:
```csharp
app.GetResource<UiClayContext>().PixelHitTest =
    (ulong entity, object? customData, Vector2 pos, in ComputedNode box) =>
        box.Contains(pos) && MySpriteIsOpaqueAt(customData, pos, box);
```
It is called for every surface that is not `UiContainsByBounds`, with the
element's `UiCustom.Data`, and owns the whole test (bounds included, so it may
widen the box). `false` lets the pick fall through to what is behind.

## Scrolling

Set `Node.Overflow = Overflow.Scroll` to make a container scrollable. The
layout pass writes `ScrollPosition { OffsetX, OffsetY }` on the entity each
frame. Mutate it to scroll programmatically, or call
`UiClayContext.SetScrollPosition(entityId, offset)`.

Feed input scroll per frame via `UiClayContext`:
```csharp
var ctx = app.GetResource<UiClayContext>();
ctx.ScrollDelta = new Vector2(0, wheelDelta);
ctx.DeltaTime = frameTime;
ctx.EnableDragScrolling = true;
```

`ScrollContainerData` for a given entity:
```csharp
var data = ctx.GetScrollContainerData(elementClayId);
```

## Pointer + surface resources

```csharp
var p = app.GetResource<UiPointer>();
p.Position = mousePos;
p.Down     = leftButtonHeld;
// `WasDown` is maintained internally — don't touch.

var s = app.GetResource<UiSurface>();
s.LogicalSize  = new Vector2(1280, 720);   // what your code positions in
s.PhysicalSize = new Vector2(2560, 1440);  // backing framebuffer (for HiDPI)
```

## `UiScale`

Global multiplier applied to every pixel-typed `Val` before layout, plus to
padding, border widths, gaps, corner radii, shadow offsets, text font size,
and the logical surface size handed to Clay. `Val.Percent` and `Val.Auto`
are unaffected. Defaults to `1`.

```csharp
app.GetResource<UiScale>().Value = 2f;   // crisp HiDPI at 200%
```

Because the resource scales the **layout output** (everything Clay computes
ends up in scaled pixels), your pointer feed must live in the same space —
multiply incoming mouse coordinates by the scale before writing to
`UiPointer.Position`, or, equivalently, feed your renderer in scaled pixels
and let the OS layer hand you scaled pointer coords already.

## Entity lifecycle

Nodes can be despawned at any time — including while they are hovered or
pressed — without crashing the UI pipeline:

- `ClayToEntity` and `ScrollClayToEntity` are rebuilt every `UiLayoutStage`,
  so stale entries can't linger.
- `LastSyncedScroll` is purged of dead entity ids at the end of the same
  stage, so id recycling can't resurrect a stale offset.
- A click only fires when press and release resolve to the same
  `Interaction` entity, so a click on an entity that vanished mid-press
  silently produces no `UiClick`.

## Rendering

`UiRenderStage` publishes a flat list of `RenderCommand`s to
`UiRenderCommands`. Consume from your renderer:

```csharp
app.AddSystem((Res<UiRenderCommands> cmds, Res<UiTextureRegistry> tex,
               Res<UiFontRegistry> fonts) =>
{
    foreach (var cmd in cmds.Value.Span)
    {
        switch (cmd.CommandType)
        {
            case RenderCommandType.Rectangle:    /* draw rect    */ break;
            case RenderCommandType.Border:       /* draw border  */ break;
            case RenderCommandType.Text:         /* draw text    */ break;
            case RenderCommandType.Image:        /* draw image   */ break;
            case RenderCommandType.ScissorStart: /* push clip    */ break;
            case RenderCommandType.ScissorEnd:   /* pop clip     */ break;
        }
    }
})
.InStage(Stage.Last).SingleThreaded().Build();
```

Register textures / fonts:
```csharp
var texId  = app.GetResource<UiTextureRegistry>().Register(myTexture);
app.GetResource<UiFontRegistry>().Register(fontId: 1, myFont);
```

## `ComputedNode`

Written to every visible node after `UiLayoutStage`:

```csharp
public struct ComputedNode
{
    public Vector2 Size;
    public Vector2 Position;
    public Vector4 Padding;
    public Vector4 Border;
    public uint    ClayId;
}
```

Useful for follow-up layout decisions, drag-and-drop math, animation, etc.

## Widgets (`TinyEcs.Bevy.UI.Widgets`)

Headless widgets in the spirit of Bevy's `bevy_ui_widgets`: behaviour on marker
components plus semantic events; the look (colours, sprites, fonts) is the
host's. `UiWidgetsPlugin` adds them all (each plugin can also be added alone);
they pull in `UiFocusPlugin`.

**Events.**
- `Activate { Entity }` — bubbling: the widget was activated (click release over
  it or a descendant, or Enter / Space while it holds focus).
- `ValueChange<T> { Source, Value, IsFinal }` — bubbling, emitted on `Source`:
  the user asked for `Value`. `IsFinal` is false for the intermediate values of
  a gesture (a slider drag) and true for the one it settles on.

**Controlled by default.** A widget only REPORTS a change; the app writes the
state back (the marker / component, or a command event). Tag the widget
`SelfUpdate` to let it apply its own `ValueChange` (Bevy's
`checkbox_self_update` & co). State lives in markers: `Checked` (checkbox, radio
button), `Selected` (list item, tab).

| Widget | Components | Emits | Keys (when it or a descendant is focused) | Commands |
|---|---|---|---|---|
| `ButtonPlugin` | `Button` | `Activate` | Enter, Space | — |
| `CheckboxPlugin` | `Checkbox`, `Checked`, `CheckboxLabel { Target }` (clickable caption) | `ValueChange<bool>` | Enter, Space | `SetChecked(bool)`, `ToggleChecked` |
| `RadioPlugin` | `RadioGroup` (ancestor), `RadioButton`, `Checked` | `ValueChange<ulong>` (the button) on the group | arrows, Home/End on the group; Enter/Space on a button | `SetChecked` |
| `SliderPlugin` | `Slider { ThumbLength, Orientation, TrackClick }`, `SliderValue`, `SliderRange { Start, End }`, `SliderStep`, child `SliderThumb` | `ValueChange<float>` (IsFinal on release) | arrows ±step, Home/End | `SetSliderValue { Value, Mode = Absolute/Relative/Steps }` |
| `ListBoxPlugin` | `ListBox`, `ListItem`, `Selected` | `ValueChange<ulong>` (the item) on the list | Up/Down, Home/End, PageUp/PageDown | `SetSelected` |
| `TabsPlugin` | `TabList`, `Tab`, `Selected` | `ValueChange<ulong>` (the tab) on the list | arrows (wrap), Home/End | `SetSelected` |
| `PopoverPlugin` | `Popover { Anchor, Side, Align, Gap, NoFlip, Within }` | — | — | — |
| `MenuPlugin` | `MenuButton`, `MenuPopup { Owner }`, `MenuItem`, `MenuKeepOpen` | `MenuOpen` on the button; `Activate` per item | Down/Enter/Space opens; Up/Down/Home/End walk items; Escape closes | `MenuClose` |

`InteractionDisabled` widgets (or a disabled group / list) ignore input. A click
resolves to the NEAREST widget up the tree from the hit, so captions and icons
inside a widget need no handlers of their own.

**Slider.** The thumb is positioned (Absolute, in PostUpdate) from
`SliderValue` within `SliderRange`; it gets `ClipToParent`, so a slider inside a
scroll box clips with its row. `TrackClick.Snap` (default) centres the thumb
under the press and drags from there; `Drag` drags from the current value;
`Step` moves one step towards the press. Missing `SliderValue` / `SliderRange`
are inserted as 0 and 0..1.

**Popover.** Placed in `UiPreLayoutStage` from last frame's boxes: on `Side` of
the anchor, `Gap` away, aligned `Start / Center / End`; flipped to the opposite
side when that overflows less, then clamped inside the surface (or `Within`'s
box). A fresh popover lands from its declared `Px` size and settles once it has
its own box. Absolute offsets are made relative to its parent's box.

**Menu.** `MenuButton` (a `Button`) toggles: it closes its open `MenuPopup` or
emits `MenuOpen`, which the app answers by spawning the popup (`MenuPopup
{ Owner = button }`, usually with a `Popover { Anchor = button }`). A
`MenuPopup` is a `UiPopup` — an outside press or any right press despawns it;
a press on its owner / popover anchor is left to the button's toggle. Each
`MenuItem` (a `Button`) emits `Activate`, then closes the popup — after every
`Activate` observer ran — unless the item or popup carries `MenuKeepOpen`.
Focus: a menu opened from a FOCUSED button takes focus (arrows, Escape) and
hands it back to the owner when it closes; one opened by the mouse leaves focus
alone, so a host that shares the keyboard (arrows = walk) keeps it.

**ListBox.** When an item gains `Selected`, the nearest `Overflow.Scroll`
ancestor scrolls just enough to show it (from Clay's boxes, which exist for
culled elements too).

```csharp
// Controlled checkbox: the app owns the state.
commands.Spawn().Insert(node).Insert(new Interaction()).Insert(new Checkbox())
    .Observe((On<ValueChange<bool>> t, Commands c) =>
    {
        settings.Music = t.Event.Value;
        c.Entity(t.Event.Source).EmitTrigger(new SetChecked(t.Event.Value));
    });

// Dropdown: a MenuButton + a MenuPopup anchored under it.
button.Insert(new MenuButton()).Observe((On<MenuOpen> t, Commands c) =>
{
    var menu = c.Spawn()
        .Insert(new Node { PositionType = PositionType.Absolute, Width = Val.Px(150), Height = Val.Px(90) })
        .Insert(new MenuPopup { Owner = t.EntityId })
        .Insert(new Popover { Anchor = t.EntityId, Side = PopoverSide.Bottom });
    foreach (var choice in choices)
        menu.AddChild(c.Spawn().Insert(rowNode).Insert(new Interaction()).Insert(new MenuItem())
            .Observe((On<Activate> _) => Pick(choice)).Id);
});
```

### Pick and control helpers

- `UiPickable` — a node that paints nothing but must be a pick surface at its
  own paint position (an invisible hit / drag area): the plugin inserts an empty
  `UiCustom` (renderers skip null-data custom commands; a host `PixelHitTest`
  sees null data and should answer by bounds).
- `UiControl` bundle — the usual in-window control: `Interaction`,
  `UiNoWindowDrag` and `UiContainsByBounds`.
- `UiHoverTint` also shows `Hover` on a `Focused` entity (keyboard navigation).

### `UiScrollbarPlugin`

A vertical bar for any `Overflow.Scroll` + `ScrollPosition` container: tag it
`UiScrollbar { Width, Style }`. The bar is an OVERLAY, not entities — geometry
derived post-layout from Clay's scroll data into `ScrollbarOverlays`
(`ByClayId` for the renderer, `Bars` for the gesture). The container reserves
the lane as right padding so flow children never run under it.

- Gesture (PreUpdate, raw `MouseInput`): thumb drag, track click pages one
  viewport, skinned arrows step `ScrollbarGeometry.ArrowStep`. A press claims the
  `UiGestureGate` (so the window under it doesn't drag) and holds
  `UiPointer.Down` low for the whole gesture (so no row under the lane clicks).
  A window stacked over the lane takes the press.
- `UiScrollbarLane { Gap, ExtendTop, ExtendBottom, ThumbArt }` puts the lane
  OUTSIDE the container (no padding reserved); `ThumbArt != 0` is a thumb-only
  flag.
- Painting is the host's: on each `ScissorEnd` command, look up
  `ScrollbarOverlays.ByClayId[cmd.Id]` and draw track/thumb (and arrows for
  `UiScrollbarStyle.Skinned`). The skinned style's arrow / thumb heights come
  from `UiScrollbarSkin` hooks the host sets; unset = flat geometry.

See `samples/TinyEcsGame` (`UiDemoPlugin` + `RaylibUiPlugin.DrawScrollbar`).

### `UiWheelRoutingPlugin`

Stage.First: the wheel over a scroll container scrolls the topmost one (by
window z, then the smaller box) through its `ScrollPosition`, and is consumed
(`MouseInput.ConsumeWheel`, Clay's `ScrollDelta` zeroed) whenever the cursor is
over painted UI, so world consumers reading `WheelConsumed` stay put. A painted
node that is really the world (a full-screen viewport) is tagged
`UiWheelPassThrough`. A host gesture that needs the notch first orders itself
`.Before(UiWheelRoutingPlugin.Label)`; a host capturing the wheel consumes it
before the router runs.

### `EcsInspectorPlugin`

Debug window listing every non-empty archetype: a clickable header per
archetype (entity count + component names), expanding to a table with one row
per entity (first 50) and one column per component — data components show their
value (`ToString`, or `field=value` when the type keeps the default one), tags a
filled square. Rebuilt every second.

```csharp
app.AddPlugin(new EcsInspectorPlugin { Style = new EcsInspectorStyle { /* fonts, colours */ } });
commands.EmitTrigger(new EcsInspectorToggle()); // open / close
```

The window root carries `UiMovable`. Decorate through marker components with
`OnAdd<T>` observers: `EcsInspectorList` (the scroll container, e.g. attach a
scrollbar), `EcsInspectorHeader` (archetype rows, e.g. hover feedback),
`EcsInspectorCell` (truncated cells and column names; `FullText` holds the whole
string, e.g. for a tooltip).

It reads storage through `WorldArchetypes`, a read-only system param over
`World.Archetypes` / `World.ComponentType(id)` / `Archetype.EntityIdAt(row)` /
`Archetype.GetBoxed(column, row)`. Run systems using it `SingleThreaded()`.
`GetBoxed` allocates and value formatting uses reflection — debug tooling only.

## Keyboard focus (`UiFocusPlugin`)

bevy_input_focus-style. One resource, `InputFocus { Entity }` (`Get` / `Set` /
`Clear`; 0 = none), names the entity that receives the keyboard. Anything may
write it; the plugin reacts:

| Piece | Behaviour |
|---|---|
| `Focusable` | a left press on the entity focuses it |
| `AutoFocus` | takes focus as soon as it is inserted (an `OnAdd` observer), then removed |
| `FocusGained` / `FocusLost` | entity triggers, fired in PostUpdate when `InputFocus` changes hands; the `Focused` marker moves with them |
| `FocusedKey { Key, Shift, Ctrl, Alt }` | bubbling trigger per key press edge, on the focused entity |
| `FocusedChar { Value }` | bubbling trigger per composed `CharInput` |
| `TabIndex(n)` + `TabGroup { Order, Modal }` | Tab / Shift+Tab walk the visible `TabIndex >= 0` entities: groups by `Order`, then index; a `Modal` group traps Tab inside it. Ungrouped stops share an implicit group 0 |
| `UiFocusPassthrough` | focused, but nothing is consumed for it (a chat line that shares keys with the game) |

**Key capture.** The dispatcher runs in `Stage.First`
(`UiFocusPlugin.DispatchLabel`): order the host's input feed before it and the
game's key readers (hotkeys, movement) after it. When the focused entity edits
text (`EditableText`), it consumes on `KeyboardInput` the keys that type or move
the caret — printable keys, Space, Backspace/Delete, Left/Right/Home/End, plus
Enter/Tab/Up/Down/PageUp/PageDown for a multiline field — so filtered reads
(`IsPressed*`) elsewhere never see them. Enter / Tab / Escape / function keys
stay visible to the host in a single-line field (submit, next field, close).
The editor reads the `*Raw` edges.

**Widget keys.** `UiKeyCapture { Keys }` (`UiKeys` flags: `Activate` = Enter /
Space, `Vertical`, `Horizontal`, `HomeEnd`, `Paging`, `Escape`) names the keys a
widget handles itself. The dispatcher unions the capture of the focused entity
and its ancestors (a `FocusedKey` bubbles, so a menu item's Up/Down is its
menu's) and consumes those keys in the same Stage.First pass — so a focused
button's Enter or a list's arrows never also reach a hotkey or movement reader
ordered after `DispatchLabel`. The widget plugins insert it on their roles
(`Button`: Activate, `ListBox`: Vertical + HomeEnd + Paging, …) unless one is
already there. The `FocusedKey` handlers themselves are observers (they run at
the end of the stage); the consume is what keeps later readers off the key.

`KeyboardInput` carries the consume model: `Consume(key)` / `IsConsumed`, the
filtered `IsPressed` / `IsPressedOnce` / `IsReleased`, the unfiltered
`IsPressedRaw` / `IsPressedOnceRaw` / `IsReleasedRaw`, and `ConsumeText()` /
`TextConsumed` — a handler that claimed this frame's key (a hotkey bound to a
letter) drops the frame's typed text. All marks clear in `Update`. A host whose
own input adapter owns a `KeyboardInput` registers THAT instance before adding
the plugin (`InputPlugin` never overwrites one), so consumes are shared both ways.

## Text editing (`UiTextEditPlugin`)

One stb_textedit editor, `ActiveTextEdit`, serves whichever field holds
`InputFocus`. It adds `UiFocusPlugin`.

| Component | Meaning |
|---|---|
| `TextInput` | the entity is a text input (I-beam, "a field is focused") |
| `EditableText { Multiline }` | the shared editor edits it; `Multiline` = code-editor mode on a plain `Text` (hard newline rows only — pair with `TextWrap.None`) |
| `MaskedText { Value, MaskChar }` | password: the real value; `Text` mirrors the mask |
| `ReadOnlyText` | select + copy only, edits revert |
| `MultilineText { WrapWidth, Value, Style }` | wrapped field drawn by the HOST (value not in `Text`); the editor stamps `SelStart/SelEnd/CaretOn/CaretX/Y/H` every frame for the host to paint in-content |
| `TextSpans` | per-range colours of a `Text`, resolved per glyph by the renderer |
| `TextCaret { Target }` | blinking "_" glyph for bespoke fields without `EditableText` |
| `UiNoBlur` / `UiKeepFocusOnClickAway` | a press here does not blur the focused self-editable field / the field is not blurred by presses elsewhere |

Triggers: `TextEditChanged { Value }` (write-back changed the value) and
`TextEditSubmit { Value }` (Enter in a single-line field), both bubbling.

Features: click / drag / double (word) / triple (line) select, arrows, Home/End,
Ctrl word moves, Ctrl+A/C/X/V, Ctrl+Z / Ctrl+Y / Ctrl+Shift+Z, Ctrl+Backspace/
Delete by word, typematic repeat on `Res<Time>` (`KeyRepeat`), multiline Enter /
Tab-indent / Shift+Tab-outdent / column-preserving Up/Down, completion offers
(`TextCompletion { Entity, Prefix, Items }`: Up/Down walk, Tab inserts), and
horizontal caret-follow in a clipping frame. Backspace arrives as the `'\b'`
`CharInput`.

**Two field shapes.**
- `UiTextField.Spawn(commands, measurer, frame, options)` builds a field on a
  frame entity the caller owns: content row + glyph + selection highlight +
  caret bar (absolute nodes the editor positions), click-to-focus on every
  part, `FlowRow` for scroll bodies, `ClipWidth` for boxes with a button inside.
  `PrepareGlyph` runs before the value lands (a host render component, a
  visibility guard for secrets); `Decorate` tags every sub-entity.
- A SELF-EDITABLE node: one entity with `Text + TextInput + EditableText`. A
  press focuses it (and a press elsewhere blurs it); the caret / selection
  geometry is computed into `SelfEditOverlay` from Clay's laid-out boxes, and
  the host paints it inline at the field's own render commands
  (`SelPaintId` under the glyphs, `CaretPaintId` over them).
  `SelfEditCaretScroll` keeps a multiline caret inside its scroll container.

**Metrics.** `ITextEditMeasurer : ITextMeasurer` adds `MeasureWidth`,
`LineHeight`, `CaretMetrics` (the ink band: Top / Height / Cell) and
`WrapRows(text, font, size, wrapWidth, style, rows)`. If the
`UiPlugin.TextMeasurer` implements it, carets match the glyphs exactly;
otherwise `TextEditMeasurer.For` derives them from `MeasureText`.

**Clipboard.** `UiClipboard` resource; the host sets `Backend`
(`IClipboardBackend { Get, Set }`) to reach the OS clipboard. Without one (or
after a refused write, `OsRefusedWrite`) copies stay in-app.

## Windows

`UiWindowPlugin` owns the window gestures. Every one resolves its press through
`UiPick` over the raw `MouseInput` (UI-layout space; register the host's
backend-fed instance before adding the plugin), so a press on ANY painted child
of a window — pixel-perfect with `PixelHitTest` — counts; windows need no
`Interaction`.

| Gesture | Stage | Behaviour |
|---------|-------|-----------|
| Drag (`DragLabel`) | Update | Left press over a `UiMovable` root's subtree brings the root to front (`BringToFront`, z bump that never demotes a declared floor) and latches a move; `Node.Left/Top` follow in whole pixels. `ForcedWindowDrag.Owner` hands a just-spawned window to the held cursor. |
| Right-click close | PreUpdate | Press + release over the same window emits `UiCloseRequested` on it (drag-off cancels). The right button is consumed while parked over a window. |
| Popup dismissal | PreUpdate | A left press outside a `UiPopup` subtree, or any right press, despawns it. |

Opt-outs: `UiMovableNoDrag` (root: focus, no move), `UiNoWindowDrag` (an
in-window control found by bounds), `UiDragYield` (the topmost hit owns its own
press, e.g. a draggable item), `UiNoRightClickClose` (root keeps its own
right-click gesture; the press edge is left unconsumed).

**Closing.** `UiCloseRequested` is an entity event (not propagated). The
plugin's default observer despawns the root unless it carries `UiManualClose`;
other observers run too, so a host routes a close (notify a server, tear down
through its own path) by observing the event, and tags windows it closes itself
`UiManualClose`. Emit it yourself for a programmatic close.

**Host knobs.** `UiWindowModifiers` (written by the host each frame):
`DragRequiresModifier` / `DragModifierHeld`, `DragSuppressed` (a drag payload
rides the cursor — feed it `.Before(UiWindowPlugin.DragLabel)`),
`AnchorDetachHeld`, `CloseAnchoredRequiresModifier` /
`CloseAnchoredModifierHeld` / `CloseAnchoredGroup`. `ActiveWindowDrag`
publishes the dragged root and its exact per-frame delta (and `JustReleased`)
for late consumers.

Companions:
- `UiAnchorPlugin` — `UiAnchorable { Kind }` windows dropped onto a same-kind
  neighbour snap flush to its nearest edge and share a `UiAnchorMember` group;
  a grouped window drags its group (or detaches with `AnchorDetachHeld`); a
  translucent ghost previews the snap.
- `UiResizablePlugin` — `UiResizable { Min/Max, Grip, GripInset, WholePixels }`
  resizes the root from a bottom-right grip square.
- `UiPositionCachePlugin` — `UiPositionKey { Key }` roots record their position
  every frame and get it back when a window with the same key is inserted.
- `UiSurfaceBoundsPlugin` — clamps windows back inside a shrinking surface.
- `UiNodeOps` — compare-before-write `Node` helpers (`SetDisplay`, `SetXY`,
  `SetSize`, `HideChildren`, `ShowChildren`, `Hidden`) that mark
  `SetChanged<Node>` only on a real change.

## Sample

`samples/TinyEcsGame/UiDemoPlugin.cs` exercises the entire surface against
a Raylib renderer: layout, scroll + overlay scrollbar, ZIndex layering,
sliders, checkboxes, a popover menu, hover styling, and pointer event observers.

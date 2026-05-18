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
| `UiPreLayoutStage` | Feed pointer state to Clay, reset `Interaction`.   |
| `UiLayoutStage`    | Walk the entity tree, declare to Clay, `EndLayout`.|
| `UiPostLayoutStage`| Hit-test, fire `UiClick` / `UiOver` / etc, write `ComputedNode`. |
| `UiRenderStage`    | Publish render commands to `UiRenderCommands`.     |

Resources registered: `UiScale`, `UiSurface`, `UiPointer`, `UiRenderCommands`,
`UiClayContext`, `UiTextureRegistry`, `UiFontRegistry`.

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
| `ImageBundle`  | `Node`, `UiImage`.                                     |

```csharp
commands.SpawnBundle(new ButtonBundle
{
    Node = new Node { Width = Val.Px(120), Height = Val.Px(36) },
    Background = new BackgroundColor(Color.Rgba(50, 60, 80, 255)),
    Focus = FocusPolicy.BlockAll,
});
```

## Interaction

Add `Interaction` to a node to opt into hit-testing. The post-layout pass
sets it each frame:

```csharp
public enum Interaction : byte { None, Hovered, Pressed }
```

`FocusPolicy { Block }` controls whether the node consumes the pointer
(`true` — default) or lets it fall through. `Button` is just a marker.

## Pointer events

Fired as entity-targeted triggers via `commands.EmitTrigger` during
`UiPostLayoutStage`. Observe globally or per-entity (entity observers can
opt into propagation):

| Trigger          | When                                                      |
|------------------|-----------------------------------------------------------|
| `UiOver`         | Pointer entered this entity (transition from non-hover).  |
| `UiOut`          | Pointer left this entity.                                 |
| `UiPointerDown`  | Pointer pressed over this entity (press edge).            |
| `UiPointerUp`    | Pointer released — fires on the **press origin** entity.  |
| `UiClick`        | Press + release happened on the **same** entity.          |

```csharp
commands.SpawnBundle(new ButtonBundle { /* ... */ })
    .Observe<On<UiClick>>(t => Console.WriteLine("clicked"))
    .Observe<On<UiOver>>(t  => Console.WriteLine("hover in"))
    .Observe<On<UiOut>>(t   => Console.WriteLine("hover out"));
```

`RelativeCursorPosition { Normalized, InBounds }` is written to the hovered
entity each frame — useful for hover gradients, drag handles, etc.

## Hit-testing

Topmost-first hit detection runs against Clay's cached `PointerOverIds`
(deepest last-child first within a tree, highest-ZIndex root first across
trees). Each candidate is filtered through `Clay.PointerOver(id)` so scroll
scissor clips and custom hit-tests apply.

Custom pixel-perfect picking — set the handler directly on the `UiClayContext`
resource so it isn't dependent on whatever ClayContext happens to be current:
```csharp
app.GetResource<UiClayContext>().Context.CustomHitTest =
    (id, bbox, point) =>
    {
        // Return false to discard the hover (e.g., transparent pixel of a sprite).
        return true;
    };
```
The handler fires for every `PointerOver(id)` call after its bounding-box and
scroll-clip checks pass. Our hit-test loop runs `PointerOver` on each
`PointerOverIds` entry, so a `false` return from your handler removes that
element from the topmost-first picking pass — making the next interactive
candidate (further back) the new hover target.

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

## Widgets

Optional plugins built on the primitives above. Each adds its own systems
and components; add them after `UiPlugin`.

### `CheckboxPlugin`

```csharp
app.AddPlugin(new CheckboxPlugin());

commands.SpawnBundle(new ButtonBundle { /* ... */ })
    .Insert(new Checkbox { Checked = false });
```

Flips `Checkbox.Checked` on every `UiClick` and emits a `CheckboxChanged`
entity-targeted trigger with the new value.

### `SliderPlugin`

```csharp
app.AddPlugin(new SliderPlugin());

var track = commands.Spawn()
    .Insert(new Node { /* track size */ })
    .Insert(new Slider
    {
        Min = 0, Max = 100, Value = 50,
        ThumbLength = 20,
        Orientation = ScrollbarOrientation.Horizontal,
    })
    .Insert(new Interaction());

var thumb = commands.Spawn()
    .Insert(new Node { PositionType = PositionType.Absolute, /* thumb size */ })
    .Insert(new SliderThumb())
    .Insert(new Interaction());

commands.AddChild(track, thumb);
```

Drag updates `Slider.Value` and emits `SliderChanged`.

### `ScrollbarPlugin`

Track + thumb pair driven by a target entity's `ScrollPosition`. See
`samples/TinyEcsGame/UiDemoPlugin.cs` for a full setup.

## Sample

`samples/TinyEcsGame/UiDemoPlugin.cs` exercises the entire surface against
a Raylib renderer: layout, scroll, ZIndex layering, sliders, checkboxes,
hover styling, and pointer event observers.

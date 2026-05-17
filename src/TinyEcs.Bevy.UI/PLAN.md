# TinyEcs.Bevy.UI — Design Plan

Port of `bevy_ui` on top of TinyEcs.Bevy. Layout + draw command generation delegated to **Clay.NET** (pure C# Clay impl). Input/interaction/state lives in ECS, mirrored after `bevy_ui` ≈ 0.14.

> Goals: **simple, flat, efficient.** No regression tests. No abstraction tax. No reflection. Minimal allocs/frame.

---

## 1. Architecture

```
┌──────────────────────────────────────────────────────────┐
│  User-Spawned Entities                                   │
│  (Node, Style, BackgroundColor, Children, …)             │
└────────────────────────────┬─────────────────────────────┘
                             │
                  Stage.UiPreLayout      ── prep: pointer state, focus
                             │
                  Stage.UiLayout         ── traverse hierarchy → Clay
                             │                  └─ Clay.BeginLayout()
                             │                  └─ recursive emit
                             │                  └─ Clay.EndLayout() → cmds
                             │
                  Stage.UiPostLayout     ── write back ComputedNode
                             │                  + dispatch Interaction
                             │
                  Stage.UiRender         ── publish RenderCommands resource
                             │            (consumed by backend plugin)
                             │
                  user backend (e.g. Raylib) reads resource, draws.
```

Single owner per frame: layout pass owns the Clay context. Render commands published as resource `UiRenderCommands`. Renderers (Raylib, MonoGame, etc.) live in user code and only consume that resource.

---

## 2. Component Surface (mirrors `bevy_ui`)

All structs. Flat. No nullable refs in hot paths.

### 2.1 Marker

```csharp
public struct UiRoot { }                  // top-level nodes (no Parent)
```

### 2.2 Style — the `Node` struct (Bevy's `Style` + `Node` merged)

Flat struct mirroring Clay's `LayoutConfig` 1:1 plus Bevy ergonomics:

```csharp
public struct Node
{
    public Display         Display;        // Flex | None
    public PositionType    PositionType;   // Relative | Absolute
    public Overflow        Overflow;       // Visible | Clip
    public FlexDirection   FlexDirection;
    public JustifyContent  JustifyContent;
    public AlignItems      AlignItems;
    public Val             Width, Height;
    public Val             MinWidth, MinHeight;
    public Val             MaxWidth, MaxHeight;
    public UiRect          Padding;        // l,r,t,b
    public UiRect          Margin;         // mapped to Floating offset
    public UiRect          Border;
    public Val             Gap;
    public float           AspectRatio;    // 0 = ignored
}
```

`Val` mirrors Bevy: `Auto | Px(f) | Percent(0..1) | Vw | Vh`. Mapped to `Clay.SizingAxis` at emit time.

### 2.3 Visual components

```csharp
public struct BackgroundColor { public Color Value; }
public struct BorderColor     { public Color Value; }
public struct BorderRadius    { public float TL, TR, BL, BR; }
public struct Outline         { public Color Color; public float Width, Offset; }
public struct BoxShadow       { public Color Color; public Val OffsetX, OffsetY, Spread, Blur; }
public struct UiImage         { public uint TextureId; public Color Tint; }
public struct ZIndex          { public int Value; }    // local
public struct GlobalZIndex    { public int Value; }    // global override
```

### 2.4 Text

```csharp
public struct Text          { public string Value; }   // class? string boxed once
public struct TextFont      { public ushort FontId; public ushort Size; }
public struct TextColor     { public Color Value; }
public struct TextLayoutInfo { public float Width, Height; } // output
```

### 2.5 Interaction

```csharp
public enum Interaction : byte { None, Hovered, Pressed }
public struct FocusPolicy   { public bool Block; }   // Block | Pass
public struct RelativeCursorPosition { public Vector2 Normalized; public bool InBounds; }
public struct Button { }                              // marker
```

### 2.6 Computed (written by layout pass)

```csharp
public struct ComputedNode
{
    public Vector2 Size;
    public Vector2 Position;        // absolute screen
    public Vector4 Padding;         // l,r,t,b
    public Vector4 Border;
    public uint    ClayId;          // last-frame Clay element id (for hit testing)
}
```

Hierarchy: reuse TinyEcs.Bevy `Parent` / `Children` if present, otherwise add a tiny `UiHierarchy` relation here (children stored as `List<ulong>` resource indexed by parent). Decision: **reuse Bevy's existing Parent/Children**; if not present, add minimal helpers in `Hierarchy.cs`.

---

## 3. Resources

```csharp
public sealed class UiScale            { public float Value = 1f; }
public sealed class UiSurface          { public Vector2 LogicalSize; public Vector2 PhysicalSize; }
public sealed class UiPointer          { public Vector2 Position; public bool Down, Pressed, Released; }
public sealed class UiRenderCommands   { public RenderCommand[] Buffer; public int Count; }
public sealed class UiTextMeasurer     { public ITextMeasurer Impl; }  // user supplies
public sealed class UiClayContext      { internal ClayContext Context; } // hidden state
public sealed class UiTextureRegistry  { /* uint → user texture handle */ }
public sealed class UiFontRegistry     { /* ushort → user font handle  */ }
```

All allocated once at plugin build. Buffers grow geometrically.

---

## 4. Stages

Added by `UiPlugin`:

| Stage         | After          | Purpose                                                |
|---------------|----------------|--------------------------------------------------------|
| `UiPreLayout` | `PreUpdate`    | Sample pointer; clear `Interaction` to `None`          |
| `UiLayout`    | `Update`       | Walk tree, emit Clay decls, run `EndLayout()`          |
| `UiPostLayout`| `UiLayout`     | Hit test; write `ComputedNode`/`Interaction`           |
| `UiRender`    | `PostUpdate`   | Copy Clay render cmds → `UiRenderCommands` resource    |

All single-threaded (Clay context is global-thread-affine within a frame).

---

## 5. Layout Pipeline (the hot path)

One system, `UiLayoutSystem`, in `UiLayout` stage:

```
1. Clay.SetContext(ctx); Clay.BeginLayout(deltaTime)
2. For each entity with UiRoot (sorted by GlobalZIndex):
       EmitNode(entityId)
3. cmds = Clay.EndLayout()
4. Store cmds span into UiRenderCommands.Buffer (memcpy)
```

`EmitNode` is **iterative with an explicit stack** (no recursion blow-up, zero allocs):

```csharp
struct Frame { public ulong Entity; public byte ChildCursor; }
Span<Frame> stack = stackalloc Frame[MAX_DEPTH]; // MAX_DEPTH = 64
```

Per node:
- Build `ElementDeclaration` from `Node` + visual components (struct copy, no alloc)
- `Clay.Element(decl)` (push)
- If `Text` present → `Clay.Text(...)`
- Push children frame, iterate
- `Clay.CloseElement()` on pop

Query plan:
- `Query<Data<Node>, Filter<With<UiRoot>>>` — roots only
- `Query<Data<Node, Optional<BackgroundColor>, Optional<BorderColor>, Optional<BorderRadius>, Optional<UiImage>, Optional<Text>, Optional<TextColor>, Optional<TextFont>, Optional<ZIndex>, Optional<Children>>>` — random access by entity id for child walks (use `query.Get(id)`)

> **Empty struct rule applied.** `UiRoot`, `Button` markers go in `Filter<With<>>`, NEVER in `Data<>`.

---

## 6. Hit Testing & Interaction

`UiPostLayoutSystem`:

1. Read `UiPointer.Position` and last frame's `UiRenderCommands` (bounding boxes by `ClayId`).
2. Walk render cmds **back-to-front** (top first via `ZIndex` order), find first whose bbox contains pointer **and** whose entity has `FocusPolicy.Block` (or default Block).
3. Set that entity's `Interaction`:
   - down-edge → `Pressed`
   - hover only → `Hovered`
4. All other UI entities cleared by pre-layout pass.

Computed bounding boxes written back via `commands.Entity(id).Insert(new ComputedNode {...})` for entities whose ClayId we just resolved. Avoid spurious change-detection: only re-insert when bbox actually moved (compare to previous).

Bevy-style events (`Click`, `Over`, `Out`) emitted via `EventWriter<UiClick>` etc. on edges.

---

## 7. Mapping Bevy → Clay

| Bevy                       | Clay                                  |
|----------------------------|---------------------------------------|
| `Val::Auto`                | `SizingAxis.Fit()`                    |
| `Val::Px(n)`               | `SizingAxis.Fixed(n)`                 |
| `Val::Percent(p)`          | `SizingAxis.PercentOf(p / 100)`       |
| `FlexDirection::Row`       | `LayoutDirection.LeftToRight`         |
| `FlexDirection::Column`    | `LayoutDirection.TopToBottom`         |
| `JustifyContent`           | `ChildAlignment.X` mapping            |
| `AlignItems`               | `ChildAlignment.Y` mapping            |
| `BackgroundColor`          | `ElementDeclaration.BackgroundColor`  |
| `BorderRadius`             | `ElementDeclaration.CornerRadius`     |
| `BoxShadow`                | `ElementDeclaration.Shadow`           |
| `Overflow::Clip`           | `LayoutConfig.ClipContent = true`     |
| `PositionType::Absolute`   | `ElementDeclaration.Floating`         |
| `ZIndex`                   | sort order at emit time               |

Mapping done by a single static method `ClayMap.Build(in Node, in Vis..., out ElementDeclaration)` — pure function, inlined.

---

## 8. File Layout (flat, ~10 files max)

```
src/TinyEcs.Bevy.UI/
  TinyEcs.Bevy.UI.csproj
  PLAN.md                       (this file)
  UiPlugin.cs                   (entry point, stage wiring, resources)
  Components.cs                 (every component above, one file)
  Val.cs                        (Val + UiRect + enums)
  ClayMap.cs                    (Node→ElementDeclaration translation)
  LayoutSystem.cs               (tree walk, Clay emit)
  InteractionSystem.cs          (pre/post layout, hit test)
  RenderSystem.cs               (publish cmd buffer)
  Bundles.cs                    (NodeBundle, TextBundle, ButtonBundle, ImageBundle)
  Events.cs                     (UiClick, UiOver, UiOut)
```

That's it. No `Internal/`, `Abstractions/`, `Helpers/` folders. Flat.

---

## 9. Bundles (ergonomic spawning)

```csharp
public struct NodeBundle : IBundle
{
    public Node Node;
    public BackgroundColor Background;
    public BorderColor BorderColor;
    public BorderRadius BorderRadius;
    public ZIndex ZIndex;

    public readonly void Insert(EntityView e)        { e.Set(Node); e.Set(Background); /* … */ }
    public readonly void Insert(EntityCommands e)    { e.Insert(Node); e.Insert(Background); /* … */ }
}

public struct TextBundle  : IBundle { public Node Node; public Text Text; public TextColor Color; public TextFont Font; /* … */ }
public struct ButtonBundle: IBundle { public Node Node; public Button Marker; public Interaction Interaction; public FocusPolicy Focus; public BackgroundColor Bg; }
public struct ImageBundle : IBundle { public Node Node; public UiImage Image; public BackgroundColor Tint; }
```

---

## 10. UiPlugin entry

```csharp
public sealed class UiPlugin : IPlugin
{
    public ITextMeasurer? TextMeasurer;
    public Vector2 LogicalSize = new(1280, 720);

    public void Build(App app)
    {
        app.AddResource(new UiScale());
        app.AddResource(new UiSurface { LogicalSize = LogicalSize, PhysicalSize = LogicalSize });
        app.AddResource(new UiPointer());
        app.AddResource(new UiRenderCommands());
        app.AddResource(new UiClayContext(...));            // boots Clay
        app.AddResource(new UiTextureRegistry());
        app.AddResource(new UiFontRegistry());

        var pre   = Stage.Custom("UiPreLayout");
        var lay   = Stage.Custom("UiLayout");
        var post  = Stage.Custom("UiPostLayout");
        var ren   = Stage.Custom("UiRender");
        app.AddStage(pre ).After(Stage.PreUpdate);
        app.AddStage(lay ).After(Stage.Update);
        app.AddStage(post).After(lay);
        app.AddStage(ren ).After(Stage.PostUpdate);

        app.AddSystem(InteractionSystem.PreLayout).InStage(pre ).SingleThreaded().Build();
        app.AddSystem(LayoutSystem.Run         ).InStage(lay ).SingleThreaded().Build();
        app.AddSystem(InteractionSystem.PostLayout).InStage(post).SingleThreaded().Build();
        app.AddSystem(RenderSystem.Publish     ).InStage(ren ).SingleThreaded().Build();
    }
}
```

User wires their own renderer (Raylib/MonoGame/whatever) by:
1. Pushing pointer state into `UiPointer` each frame.
2. Reading `UiRenderCommands` and translating each `RenderCommand` to draw calls.
3. Providing `ITextMeasurer` (Clay needs it for text sizing).

---

## 11. What we do NOT do

- **No widget library.** No `Button` rendering opinionated style. Just the marker + `Interaction`. Users style with their own `BackgroundColor`/`BorderColor`.
- **No animation system.** Style fields are mutated by user systems; Clay rebuilds every frame.
- **No tests / no regression tests** (per user directive). Hot path verified by sample only.
- **No accessibility tree.** Out of scope.
- **No Bevy `RelativeCursorPosition` per pixel** (we emit it once per frame via post-layout).
- **No multi-camera UI.** Single surface. Add later via `UiTargetCamera` if needed.
- **No `bevy_picking` parity.** Custom 1-layer hit test on bbox stack.

---

## 12. Performance notes

- Layout pass: zero managed alloc (stackalloc traversal stack, struct decls).
- Render publish: single `Array.Copy` of `ReadOnlySpan<RenderCommand>` → grow buffer if `Count > capacity`.
- Change detection: don't rebuild Clay if **nothing dirty**. v1: rebuild every frame (Clay is fast). v2: track `Changed<Node>|Changed<BackgroundColor>|…` global flag; reuse last cmds if all clean.
- Text strings: `Text.Value` is `string` (reference). Users should keep them stable to avoid hashes thrashing in Clay's id cache.

---

## 13. Build & integration order

1. ✅ Scaffold project + Clay clone (done)
2. `Val.cs`, `Components.cs` (types only, no logic)
3. `ClayMap.cs` (pure mapping)
4. `LayoutSystem.cs` (tree walk + emit)
5. `RenderSystem.cs` (publish buffer)
6. `InteractionSystem.cs` (hit test)
7. `UiPlugin.cs` (wire stages)
8. `Bundles.cs`, `Events.cs`
9. Wire to `samples/TinyEcsGame` Raylib renderer (replace existing `RaylibClayUiPlugin`)

Each step compiles in isolation. Ship as soon as `LayoutSystem` produces non-empty cmds for a single root.

---

## 14. Open questions (decide during impl, not now)

- Children storage: Bevy `Children` already exists in TinyEcs.Bevy? → check & reuse, else build minimal.
- Text measurement: Clay needs `ITextMeasurer` per context. User-supplied resource, no default beyond Clay's `SimpleTextMeasurer`.
- Floating/Absolute: Clay handles via `FloatingConfig`. Map `PositionType.Absolute` + UiRect offsets directly.
- Scrolling: Clay supports natively. Expose via `Overflow.Scroll` later.

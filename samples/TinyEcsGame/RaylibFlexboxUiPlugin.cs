using System;
using System.Numerics;
using Raylib_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI;

namespace TinyEcsGame;

/// <summary>
/// Raylib integration plugin for Flexbox UI system.
/// Updates FlexboxPointerState from Raylib and renders Flexbox layouts.
/// </summary>
public struct RaylibFlexboxUiPlugin : IPlugin
{
    public Stage RenderingStage { get; set; }

    public void Build(App app)
    {
        var renderStage = RenderingStage;

        app.AddSystem((ResMut<FlexboxPointerState> p) => UpdatePointerState(p))
            .InStage(Stage.PreUpdate)
            .Label("ui:raylib:flexbox:update-pointer")
            .RunIfResourceExists<FlexboxPointerState>()
            .SingleThreaded()
            .Build();

        app.AddSystem((Res<FlexboxUiState> s,
                       Query<Data<FlexboxNode>> n,
                       Query<Data<FlexboxText>> t,
                       Query<Data<FlexboxScrollContainer>> sc,
                       Query<Data<Children>> children) =>
            RenderFlexboxUI(s, n, t, sc, children))
            .InStage(renderStage)
            .Label("ui:raylib:flexbox:render")
            .After("render:debug")
            .RunIfResourceExists<FlexboxUiState>()
            .SingleThreaded()
            .Build();
    }

    private static void UpdatePointerState(ResMut<FlexboxPointerState> pointerState)
    {
        if (!Raylib.IsWindowReady())
            return;

        ref var state = ref pointerState.Value;

        state.Position = Raylib.GetMousePosition();
        state.PrimaryDown = Raylib.IsMouseButtonDown(MouseButton.Left);

        var scrollY = Raylib.GetMouseWheelMove();
        if (scrollY != 0f)
        {
            state.AddScroll(new Vector2(0, scrollY * 20f));
        }

        state.DeltaTime = Raylib.GetFrameTime();
    }

    private static void RenderFlexboxUI(
        Res<FlexboxUiState> uiState,
        Query<Data<FlexboxNode>> nodes,
        Query<Data<FlexboxText>> texts,
        Query<Data<FlexboxScrollContainer>> scrollers,
        Query<Data<Children>> childrenQuery)
    {
        var state = uiState.Value;

        // Render roots first (those without Parent/Children entry)
        foreach (var (entityId, _) in nodes)
        {
            var id = entityId.Ref;
            // Consider as root if there is no Children entry for its parent; safer root detection still uses absence of Parent when building tree
            if (!state.TryGetLayout(id, out _)) { continue; }
            // Treat nodes with no parent as roots
            // We detect roots from the computed tree during layout; fallback to Parent query is avoided here
            // For rendering, draw all nodes that are not found as any other node's child
            bool isChild = false;
            foreach (var (cid, _) in nodes)
            {
                if (cid.Ref == id) continue;
                if (childrenQuery.Contains(cid.Ref))
                {
                    var ch = childrenQuery.Get(cid.Ref); ch.Deconstruct(out var chPtr);
                    foreach (var c in chPtr.Ref) { if (c == id) { isChild = true; break; } }
                }
                if (isChild) break;
            }
            if (!isChild)
            {
                RenderNodeRecursive(id, nodes, texts, scrollers, childrenQuery, state, Vector2.Zero, null);
            }
        }
    }

    private static void RenderNodeRecursive(
        ulong entityId,
        Query<Data<FlexboxNode>> nodes,
        Query<Data<FlexboxText>> texts,
        Query<Data<FlexboxScrollContainer>> scrollers,
        Query<Data<Children>> childrenQuery,
        FlexboxUiState state,
        Vector2 accumulatedOffset,
        Rectangle? clip)
    {
        if (!state.TryGetLayout(entityId, out var layout))
            return;

        // Compute child clip and offset (for scrollers)
        var childClip = clip;
        var childOffset = accumulatedOffset;
        if (scrollers.Contains(entityId))
        {
            var contentRect = new Rectangle(
                layout.ContentPosition.X + accumulatedOffset.X,
                layout.ContentPosition.Y + accumulatedOffset.Y,
                layout.ContentSize.X,
                layout.ContentSize.Y);
            childClip = childClip.HasValue ? Intersect(childClip.Value, contentRect) : contentRect;

            var sc = scrollers.Get(entityId);
            sc.Deconstruct(out var sPtr);
            childOffset += new Vector2(-sPtr.Ref.Offset.X, -sPtr.Ref.Offset.Y);
        }

        // Apply current clip for this entity draw
        if (clip.HasValue)
        {
            var r = clip.Value;
            Raylib.BeginScissorMode((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
        }

        if (nodes.Contains(entityId))
        {
            var n = nodes.Get(entityId);
            n.Deconstruct(out var nodePtr);
            ref var fxNode = ref nodePtr.Ref;

            var draw = layout; draw.Position += accumulatedOffset; draw.ContentPosition += accumulatedOffset;
            if (fxNode.BackgroundColor.W > 0f)
                DrawRectangle(draw, fxNode.BackgroundColor, fxNode.BorderRadius);
            if (fxNode.BorderColor.W > 0f)
                DrawBorder(draw, fxNode.BorderColor, fxNode.BorderRadius);

            if (texts.Contains(entityId))
            {
                var t = texts.Get(entityId);
                t.Deconstruct(out var tPtr);
                ref var td = ref tPtr.Ref;
                var tdraw = layout; tdraw.ContentPosition += childOffset;
                DrawText(tdraw, ref td);
            }
        }

        if (clip.HasValue)
            Raylib.EndScissorMode();

        // Draw children in stored list order (stable)
        if (childrenQuery.Contains(entityId))
        {
            var ch = childrenQuery.Get(entityId);
            ch.Deconstruct(out var chPtr);
            foreach (var cid in chPtr.Ref)
            {
                RenderNodeRecursive(cid, nodes, texts, scrollers, childrenQuery, state, childOffset, childClip);
            }
        }
    }

    private static Rectangle Intersect(Rectangle a, Rectangle b)
    {
        float x = MathF.Max(a.X, b.X);
        float y = MathF.Max(a.Y, b.Y);
        float r = MathF.Min(a.X + a.Width, b.X + b.Width);
        float btm = MathF.Min(a.Y + a.Height, b.Y + b.Height);
        return new Rectangle(x, y, MathF.Max(0, r - x), MathF.Max(0, btm - y));
    }

    private static void DrawRectangle(ComputedLayout layout, Vector4 color, float borderRadius)
    {
        var rect = new Rectangle(layout.Position.X, layout.Position.Y, layout.Size.X, layout.Size.Y);
        var raylibColor = new Color((byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255), (byte)(color.W * 255));

        if (borderRadius > 0f)
        {
            Raylib.DrawRectangleRounded(rect, borderRadius / Math.Max(layout.Size.X, layout.Size.Y), 8, raylibColor);
        }
        else
        {
            Raylib.DrawRectangleRec(rect, raylibColor);
        }
    }

    private static void DrawBorder(ComputedLayout layout, Vector4 color, float borderRadius)
    {
        var rect = new Rectangle(layout.Position.X, layout.Position.Y, layout.Size.X, layout.Size.Y);
        var raylibColor = new Color((byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255), (byte)(color.W * 255));

        // Use non-rounded border for simplicity (API differences across Raylib versions)
        Raylib.DrawRectangleLinesEx(rect, 2f, raylibColor);
    }

    private static void DrawText(ComputedLayout layout, ref FlexboxText textData)
    {
        if (string.IsNullOrEmpty(textData.Text))
            return;

        var raylibColor = new Color((byte)(textData.Color.X * 255), (byte)(textData.Color.Y * 255), (byte)(textData.Color.Z * 255), (byte)(textData.Color.W * 255));

        var textPos = new Vector2(layout.ContentPosition.X, layout.ContentPosition.Y);

        Raylib.DrawText(textData.Text, (int)textPos.X, (int)textPos.Y, (int)textData.FontSize, raylibColor);
    }
}









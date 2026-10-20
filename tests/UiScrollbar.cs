using System.Numerics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Input;
using TinyEcs.Bevy.UI;
using Xunit;
using ClayColor = Clay.Color;
using Node = TinyEcs.Bevy.UI.Node;

namespace TinyEcs.Tests;

// The overlay scrollbar (UiScrollbarPlugin): thumb geometry is a pure function,
// and the gesture is the part that historically breaks — a press on the bar must
// scroll the list WITHOUT the window drag also latching (both read the same raw
// press; the shared UiGestureGate arbitrates).
public class ScrollbarGeometryTests
{
    [Fact]
    public void No_bar_when_the_content_fits()
    {
        Assert.False(ScrollbarGeometry.Compute(100f, 100f, 80f, 0f, out _, out _));
        Assert.False(ScrollbarGeometry.Compute(100f, 100f, 100f, 0f, out _, out _));
    }

    [Fact]
    public void Thumb_length_is_the_visible_over_content_ratio()
    {
        Assert.True(ScrollbarGeometry.Compute(100f, 100f, 300f, 0f, out var top, out var height));
        Assert.Equal(100f / 3f, height, 0.001f);
        Assert.Equal(0f, top, 0.001f);
    }

    [Fact]
    public void Thumb_travels_the_leftover_track_across_the_scroll_range()
    {
        var travel = 100f - 100f / 3f;
        var maxScroll = 300f - 100f;

        Assert.True(ScrollbarGeometry.Compute(100f, 100f, 300f, maxScroll, out var atEnd, out _));
        Assert.Equal(travel, atEnd, 0.001f);

        Assert.True(ScrollbarGeometry.Compute(100f, 100f, 300f, maxScroll / 2f, out var mid, out _));
        Assert.Equal(travel / 2f, mid, 0.001f);
    }

    [Fact]
    public void Thumb_never_shrinks_below_the_minimum_and_never_exceeds_the_track()
    {
        // 432 rows of 18px in a 100px viewport: the proportional thumb would be
        // ~3px, which is not grabbable.
        Assert.True(ScrollbarGeometry.Compute(100f, 100f, 432f * 18f, 0f, out _, out var tiny));
        Assert.Equal(ScrollbarGeometry.MinThumbHeight, tiny, 0.001f);

        // A track shorter than the minimum thumb still yields a thumb that fits.
        Assert.True(ScrollbarGeometry.Compute(8f, 8f, 400f, 0f, out _, out var clamped));
        Assert.Equal(8f, clamped, 0.001f);
    }

    // The flat style: an 8px lane and a 28px minimum thumb. The skinned style
    // defaults to a 14px lane.
    [Fact]
    public void Style_picks_the_lane_width_and_the_minimum_thumb()
    {
        Assert.Equal(14f, ScrollbarGeometry.ResolveWidth(0f, UiScrollbarStyle.Skinned), 0.001f);
        Assert.Equal(8f, ScrollbarGeometry.ResolveWidth(0f, UiScrollbarStyle.Flat), 0.001f);
        // An explicit width still wins for both.
        Assert.Equal(20f, ScrollbarGeometry.ResolveWidth(20f, UiScrollbarStyle.Flat), 0.001f);

        Assert.True(ScrollbarGeometry.Compute(
            300f, 300f, 30_000f, 0f, out _, out var flat, ScrollbarGeometry.MinThumb(UiScrollbarStyle.Flat)));
        Assert.Equal(ScrollbarGeometry.FlatMinThumbHeight, flat, 0.001f);
    }

    [Fact]
    public void Scroll_offset_outside_the_range_clamps_the_thumb_to_the_track()
    {
        Assert.True(ScrollbarGeometry.Compute(100f, 100f, 300f, 10_000f, out var top, out var height));
        Assert.Equal(100f, top + height, 0.001f);
    }
}

[Collection("ClayUi")]
public class ScrollbarGestureTests
{
    private const int WinX = 100, WinY = 100, WinW = 200, WinH = 200;
    private const int ViewW = 200, ViewH = 100;
    private const int Rows = 10, RowH = 30; // 300px of content in a 100px viewport

    private readonly record struct Snap(float X, float Y, MouseButtons Down);
    private static Snap L(float x, float y) => new(x, y, MouseButtons.Left);
    private static Snap I(float x, float y) => new(x, y, MouseButtons.None);

    private sealed class Fix
    {
        public App App = null!;
        public MouseInput Mouse = null!;
        public ulong Window, Viewport;
        private Snap _last = I(0, 0);
        private float _t;

        public World World => App.GetWorld();

        public void Step(Snap s)
        {
            Mouse.SetSnapshot(new Vector2(s.X, s.Y), s.Down);
            Mouse.Update(_t += 16f);
            var p = App.GetResource<UiPointer>();
            p.Position = new Vector2(s.X, s.Y);
            p.Down = Mouse.IsPressed(MouseButton.Left);
            _last = s;
            App.Update();
        }

        public void Step() => Step(_last);

        public ScrollbarBar Bar()
        {
            var bars = App.GetResource<ScrollbarOverlays>().Bars;
            Assert.Single(bars);
            return bars[0];
        }

        public float Offset() => World.Get<ScrollPosition>(Viewport).OffsetY;
        public float WindowLeft() => World.Get<Node>(Window).Left.Value;
    }

    private static Fix Build(UiScrollbarLane? lane = null)
    {
        var fix = new Fix { App = new App(ThreadingMode.Single), Mouse = new MouseInput() };
        var app = fix.App;
        app.AddResource(fix.Mouse);
        app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600), MaxElements = 8192 });
        app.AddPlugin(new UiWindowPlugin());
        app.AddPlugin(new UiScrollbarPlugin());
        app.RunStartup();

        var world = app.GetWorld();
        var win = world.Entity();
        win.Set(new Node
        {
            PositionType = PositionType.Absolute,
            Left = Val.Px(WinX), Top = Val.Px(WinY),
            Width = Val.Px(WinW), Height = Val.Px(WinH),
        });
        win.Set(new BackgroundColor(new ClayColor(18, 18, 26, 255)));
        win.Set(Interaction.None);
        win.Set<UiMovable>();
        win.Set<UiContainsByBounds>();
        win.Set(new GlobalZIndex(10));
        fix.Window = win.ID;

        var view = world.Entity();
        view.Set(new Node
        {
            PositionType = PositionType.Absolute,
            FlexDirection = FlexDirection.Column,
            Left = Val.Px(0), Top = Val.Px(0),
            Width = Val.Px(ViewW), Height = Val.Px(ViewH),
            Overflow = Overflow.Scroll,
        });
        view.Set(new ScrollPosition());
        view.Set(new UiScrollbar { Style = UiScrollbarStyle.Flat });
        if (lane is { } l) view.Set(l);
        fix.Viewport = view.ID;
        world.AddChild(fix.Window, fix.Viewport);

        for (var i = 0; i < Rows; i++)
        {
            var row = world.Entity();
            row.Set(new Node { Width = Val.Px(ViewW - 20), Height = Val.Px(RowH) });
            row.Set(new BackgroundColor(new ClayColor(40, 40, 60, 255)));
            world.AddChild(fix.Viewport, row.ID);
        }

        // Two frames lay the tree out (the second also lets ReserveBarWidth's
        // padding reach Clay).
        fix.Step();
        fix.Step();
        fix.Step();
        return fix;
    }

    [Fact]
    public void Bar_hugs_the_container_right_edge_and_sizes_the_thumb_to_the_content()
    {
        var fix = Build();
        var bar = fix.Bar();

        Assert.Equal(WinX + ViewW - ScrollbarGeometry.FlatWidth, bar.TrackX, 0.1f);
        Assert.Equal(WinY, bar.TrackY, 0.1f);
        Assert.Equal(ViewH, bar.TrackHeight, 0.1f);
        Assert.Equal(Rows * RowH - ViewH, bar.MaxScroll, 0.1f);
        Assert.Equal(ViewH * (float)ViewH / (Rows * RowH), bar.ThumbHeight, 0.1f);
        Assert.Equal(WinY, bar.ThumbTop, 0.1f);
    }

    [Fact]
    public void The_container_reserves_the_bar_width_so_rows_never_run_under_it()
    {
        var fix = Build();
        var pad = fix.World.Get<Node>(fix.Viewport).Padding.Right;
        Assert.Equal(ValType.Px, pad.Type);
        Assert.Equal(ScrollbarGeometry.FlatWidth, pad.Value, 0.001f);
    }

    // A window stacked over the bar (with no bar of its own there) covers it: a
    // drag on the covered thumb must not scroll the list behind.
    [Fact]
    public void A_window_on_top_blocks_the_bar_behind_it()
    {
        var fix = Build();
        var bar = fix.Bar();
        var cover = fix.World.Entity();
        cover.Set(new Node
        {
            PositionType = PositionType.Absolute,
            Left = Val.Px(bar.TrackX - 50), Top = Val.Px(bar.TrackY - 10),
            Width = Val.Px(150), Height = Val.Px(150),
        });
        cover.Set(new BackgroundColor(new ClayColor(60, 20, 20, 255)));
        cover.Set(Interaction.None);
        cover.Set<UiMovable>();
        cover.Set(new GlobalZIndex(20));
        fix.Step();
        fix.Step();

        var x = (int)(bar.TrackX + bar.TrackWidth / 2f);
        var grabY = (int)(bar.ThumbTop + bar.ThumbHeight / 2f);
        fix.Step(L(x, grabY));
        fix.Step(L(x, grabY + 40));

        Assert.Equal(0f, fix.Offset(), 0.001f);
        Assert.False(fix.App.GetResource<ScrollbarOverlays>().PressOwned);
    }

    [Fact]
    public void Dragging_the_thumb_scrolls_the_list_and_does_not_move_the_window()
    {
        var fix = Build();
        var bar = fix.Bar();
        var x = (int)(bar.TrackX + bar.TrackWidth / 2f);
        var grabY = (int)(bar.ThumbTop + bar.ThumbHeight / 2f);
        var travel = bar.TrackHeight - bar.ThumbHeight;
        var left0 = fix.WindowLeft();

        fix.Step(L(x, grabY));
        fix.Step(L(x, grabY));
        // Half the free track = half the scroll range (the cursor moves in whole
        // pixels, so the expected offset is derived from the integer delta).
        var dy = (int)MathF.Round(travel / 2f);
        fix.Step(L(x + 4, grabY + dy));

        Assert.Equal(dy * bar.MaxScroll / travel, fix.Offset(), 0.01f);
        Assert.Equal(bar.MaxScroll / 2f, fix.Offset(), 1.5f);
        Assert.Equal(left0, fix.WindowLeft(), 0.001f);

        // Dragging past the end clamps instead of overscrolling.
        fix.Step(L(x, grabY + (int)travel * 4));
        Assert.Equal(bar.MaxScroll, fix.Offset(), 0.1f);

        Assert.True(fix.App.GetResource<ScrollbarOverlays>().PressOwned);

        fix.Step(I(x, grabY));
        Assert.Equal(UiGestureMode.None, fix.App.GetResource<UiGestureGate>().Mode);
        Assert.False(fix.App.GetResource<ScrollbarOverlays>().PressOwned);
    }

    // The thumb hover flag drives the renderer's hover tint. The
    // renderer has no mouse, so the flag is resolved post-layout with the geometry.
    [Fact]
    public void Thumb_reports_hover_only_while_the_cursor_is_on_it()
    {
        var fix = Build();
        var bar = fix.Bar();
        var x = (int)(bar.TrackX + bar.TrackWidth / 2f);

        Assert.False(bar.ThumbHovered);

        fix.Step(I(x, (int)(bar.ThumbTop + bar.ThumbHeight / 2f)));
        Assert.True(fix.Bar().ThumbHovered);

        // On the lane but below the thumb, and off the lane entirely.
        fix.Step(I(x, (int)(bar.TrackY + bar.TrackHeight - 2f)));
        Assert.False(fix.Bar().ThumbHovered);
        fix.Step(I((int)bar.TrackX - 20, (int)(bar.ThumbTop + 2f)));
        Assert.False(fix.Bar().ThumbHovered);
    }

    [Fact]
    public void Clicking_the_track_below_the_thumb_pages_down_by_one_viewport()
    {
        var fix = Build();
        var bar = fix.Bar();
        var x = (int)(bar.TrackX + bar.TrackWidth / 2f);
        var belowThumb = (int)(bar.TrackY + bar.TrackHeight - 2f);

        fix.Step(L(x, belowThumb));
        Assert.Equal(ViewH, fix.Offset(), 0.1f);

        fix.Step(I(x, belowThumb));
        fix.Step();

        // And above the thumb pages back up.
        var aboveThumb = (int)(fix.Bar().ThumbTop - 2f);
        fix.Step(L(x, aboveThumb));
        Assert.Equal(0f, fix.Offset(), 0.1f);
    }

    // UiScrollbarLane: the lane sits outside the container — Gap right of its edge,
    // grown by ExtendTop/Bottom — and the container keeps its full width.
    private const float Gap = 6f, Extend = 4f;

    [Fact]
    public void Outside_lane_sits_past_the_container_edge_and_reserves_no_padding()
    {
        var fix = Build(new UiScrollbarLane { Gap = Gap, ExtendTop = Extend, ExtendBottom = 2 * Extend });
        var bar = fix.Bar();

        Assert.Equal(WinX + ViewW + Gap, bar.TrackX, 0.1f);
        Assert.Equal(WinY - Extend, bar.TrackY, 0.1f);
        Assert.Equal(ViewH + 3 * Extend, bar.TrackHeight, 0.1f);
        Assert.NotEqual(ValType.Px, fix.World.Get<Node>(fix.Viewport).Padding.Right.Type);
    }

    // The window is exactly as wide as the list, so the outside lane overhangs it:
    // under the lane there is no UI at all, and the press is still the bar's.
    [Fact]
    public void Outside_lane_overhanging_the_window_still_drags()
    {
        var fix = Build(new UiScrollbarLane { Gap = Gap });
        var bar = fix.Bar();
        var x = (int)(bar.TrackX + bar.TrackWidth / 2f);
        var grabY = (int)(bar.ThumbTop + bar.ThumbHeight / 2f);

        fix.Step(L(x, grabY));
        fix.Step(L(x, grabY + 30));

        Assert.True(fix.Offset() > 0f);
    }

    // The lane over see-through art with another window BEHIND it (drawn below the
    // bar's window): the press is the bar's, not the window's underneath.
    [Fact]
    public void A_window_behind_the_lane_does_not_steal_the_press()
    {
        var fix = Build(new UiScrollbarLane { Gap = Gap });
        var bar = fix.Bar();
        var behind = fix.World.Entity();
        behind.Set(new Node
        {
            PositionType = PositionType.Absolute,
            Left = Val.Px(bar.TrackX - 20), Top = Val.Px(bar.TrackY - 10),
            Width = Val.Px(100), Height = Val.Px(150),
        });
        behind.Set(new BackgroundColor(new ClayColor(20, 60, 20, 255)));
        behind.Set(Interaction.None);
        behind.Set<UiMovable>();
        behind.Set(new GlobalZIndex(5));
        fix.World.Set(fix.Window, new GlobalZIndex(30)); // the bar's window back on top
        fix.Step();
        fix.Step();
        Assert.True(fix.Bar().PaintOrder > PaintOrderOf(fix, behind.ID));

        var x = (int)(bar.TrackX + bar.TrackWidth / 2f);
        var grabY = (int)(bar.ThumbTop + bar.ThumbHeight / 2f);
        fix.Step(L(x, grabY));
        fix.Step(L(x, grabY + 30));

        Assert.True(fix.Offset() > 0f);
    }

    private static int PaintOrderOf(Fix fix, ulong id) => fix.World.Get<ComputedNode>(id).PaintOrder;

    [Fact]
    public void A_window_over_an_outside_lane_blocks_it()
    {
        var fix = Build(new UiScrollbarLane { Gap = Gap });
        var bar = fix.Bar();
        var cover = fix.World.Entity();
        cover.Set(new Node
        {
            PositionType = PositionType.Absolute,
            Left = Val.Px(bar.TrackX - 20), Top = Val.Px(bar.TrackY - 10),
            Width = Val.Px(100), Height = Val.Px(150),
        });
        cover.Set(new BackgroundColor(new ClayColor(60, 20, 20, 255)));
        cover.Set(Interaction.None);
        cover.Set<UiMovable>();
        cover.Set(new GlobalZIndex(20));
        fix.Step();
        fix.Step();

        var x = (int)(bar.TrackX + bar.TrackWidth / 2f);
        var grabY = (int)(bar.ThumbTop + bar.ThumbHeight / 2f);
        fix.Step(L(x, grabY));
        fix.Step(L(x, grabY + 30));

        Assert.Equal(0f, fix.Offset(), 0.001f);
    }

    // A press on the lane must not also press (and on release click) the row the
    // lane overlaps: the bar holds UiPointer.Down low for its whole gesture.
    [Fact]
    public void A_bar_gesture_holds_the_pointer_low()
    {
        var fix = Build();
        var bar = fix.Bar();
        var x = (int)(bar.TrackX + bar.TrackWidth / 2f);
        var grabY = (int)(bar.ThumbTop + bar.ThumbHeight / 2f);

        fix.Step(L(x, grabY));
        fix.Step(L(x, grabY + 5));
        Assert.False(fix.App.GetResource<UiPointer>().Down);
    }
}

[Collection("ClayUi")]
public class UiWheelRoutingTests
{
    private static (App app, MouseInput mouse, ulong view, ulong world) Build()
    {
        var app = new App(ThreadingMode.Single);
        var mouse = new MouseInput();
        app.AddResource(mouse);
        app.AddPlugin(new UiPlugin { LogicalSize = new Vector2(800, 600), MaxElements = 8192 });
        app.AddPlugin(new UiWheelRoutingPlugin());
        app.RunStartup();

        var w = app.GetWorld();
        var backdrop = w.Entity()
            .Set(new Node { PositionType = PositionType.Absolute, Width = Val.Px(800), Height = Val.Px(600) })
            .Set(new BackgroundColor(new ClayColor(0, 0, 0, 255)))
            .Set(new GlobalZIndex(1));
        backdrop.Set<UiWheelPassThrough>();

        var win = w.Entity()
            .Set(new Node { PositionType = PositionType.Absolute, Left = Val.Px(100), Top = Val.Px(100), Width = Val.Px(200), Height = Val.Px(100) })
            .Set(new BackgroundColor(new ClayColor(18, 18, 26, 255)))
            .Set(new GlobalZIndex(10));
        win.Set<UiMovable>();
        var view = w.Entity()
            .Set(new Node { FlexDirection = FlexDirection.Column, Width = Val.Px(200), Height = Val.Px(100), Overflow = Overflow.Scroll })
            .Set(new ScrollPosition());
        w.AddChild(win.ID, view.ID);
        for (var i = 0; i < 10; i++)
        {
            var row = w.Entity().Set(new Node { Width = Val.Px(180), Height = Val.Px(30) })
                .Set(new BackgroundColor(new ClayColor(40, 40, 60, 255)));
            w.AddChild(view.ID, row.ID);
        }
        return (app, mouse, view.ID, backdrop.ID);
    }

    private static float _t;

    private static void Frame(App app, MouseInput mouse, float x, float y, float wheel)
    {
        mouse.SetSnapshot(new Vector2(x, y), MouseButtons.None, wheel);
        mouse.Update(_t += 16f);
        app.GetResource<UiPointer>().Position = new Vector2(x, y);
        app.Update();
    }

    [Fact]
    public void The_wheel_over_a_scroll_container_scrolls_it_and_is_consumed()
    {
        var (app, mouse, view, _) = Build();
        Frame(app, mouse, 150, 150, 0);
        Frame(app, mouse, 150, 150, 0);

        var consumed = false;
        app.AddSystem((Res<MouseInput> m) => consumed = m.Value.WheelConsumed).InStage(Stage.Update).Build();
        Frame(app, mouse, 150, 150, -1);

        Assert.True(consumed);
        Assert.Equal(90f, app.GetWorld().Get<ScrollPosition>(view).OffsetY, 0.1f);
    }

    [Fact]
    public void A_pass_through_hit_leaves_the_wheel_to_the_world()
    {
        var (app, mouse, view, _) = Build();
        Frame(app, mouse, 500, 400, 0);
        Frame(app, mouse, 500, 400, 0);

        var consumed = true;
        app.AddSystem((Res<MouseInput> m) => consumed = m.Value.WheelConsumed).InStage(Stage.Update).Build();
        Frame(app, mouse, 500, 400, -1);

        Assert.False(consumed);
        Assert.Equal(0f, app.GetWorld().Get<ScrollPosition>(view).OffsetY, 0.1f);
    }
}

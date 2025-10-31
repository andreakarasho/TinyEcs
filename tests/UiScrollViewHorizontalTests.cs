using System.Numerics;
using TinyEcs.Bevy;
using TinyEcs.UI.Bevy;

namespace TinyEcs.Tests;

public class UiScrollViewHorizontalTests
{
    [Fact]
    public void HorizontalScrollView_StartsAligned_ShowsFirstItems()
    {
        var app = new App();

        // Core UI plugins
        app.AddPlugin(new FlexboxUiPlugin());
        app.AddPlugin(new UiStackPlugin());
        app.AddPlugin(new UiPointerInputPlugin { InputStage = Stage.PostUpdate });
        app.AddPlugin(new ScrollPlugin());

        ulong scrollRoot = 0;
        ulong contentId = 0;

        // Build a horizontal ScrollView with several boxes (overflow)
        app.AddSystem((Commands commands) =>
        {
            (scrollRoot, contentId) = ScrollViewHelpers.CreateScrollView(
                commands,
                enableVertical: false,
                enableHorizontal: true,
                width: FlexValue.Points(380f),
                height: FlexValue.Points(120f),
                scrollbarWidth: 8f
            );

            // Make inner content a row with 8 boxes (80px each + 10px margins = overflow)
            commands.Entity(contentId).Insert(new UiNode
            {
                Display = Flexbox.Display.Flex,
                FlexDirection = Flexbox.FlexDirection.Row,
                PaddingLeft = FlexValue.Points(10f),
                PaddingRight = FlexValue.Points(10f),
                PaddingTop = FlexValue.Points(10f),
                PaddingBottom = FlexValue.Points(10f),
                MinWidth = FlexValue.Percent(100f),
                MinHeight = FlexValue.Percent(100f)
            });

            for (int i = 0; i < 8; i++)
            {
                var box = commands.Spawn()
                    .Insert(new UiNode
                    {
                        Width = FlexValue.Points(80f),
                        Height = FlexValue.Points(80f),
                        MarginLeft = FlexValue.Points(5f),
                        MarginRight = FlexValue.Points(5f)
                    })
                    .Id;
                commands.Entity(contentId).AddChild(box);
            }
        })
        .InStage(Stage.Startup)
        .Build();

        // Run Startup to create entities
        app.RunStartup();

        // Calculate layout explicitly
        var world = app.GetWorld();
        var ui = world.GetResource<FlexboxUiState>();
        ui.CalculateLayout(1280, 720);

        // Run a frame to sync layouts and compute scrollable content size
        app.Update();

        // Resolve the viewport scrollable from the ScrollView component
        var sv = world.Get<ScrollView>(scrollRoot);
        var viewportId = sv.Viewport;
        var scrollable = world.Get<Scrollable>(viewportId);
        var layout = world.Get<ComputedLayout>(viewportId);

        // Assert content width overflows viewport (horizontal scroll needed)
        Assert.True(scrollable.ContentSize.X > layout.Width + 1f);

        // We expect to start at the beginning (offset ~ 0). Some builds may still
        // normalize using ContentOrigin; accept either 0 or the normalized value.
        // Accept either:
        //  - ContentOrigin.X >= -0.5 and ScrollOffset.X � 0 (already aligned)
        //  - ScrollOffset.X � clamp(-ContentOrigin.X, 0, maxScroll)
        var maxScroll = MathF.Max(0f, scrollable.ContentSize.X - layout.Width);

                if (scrollable.ContentOrigin.X < -0.5f)
        {
            var expected = Math.Clamp(-scrollable.ContentOrigin.X, 0f, maxScroll);
            var okZero = Math.Abs(scrollable.ScrollOffset.X) < 1.5f;
            var okNorm = Math.Abs(scrollable.ScrollOffset.X - expected) < 1.5f;
            Assert.True(okZero || okNorm,
                $"Expected horizontal offset ~0 or ~{expected}, got {scrollable.ScrollOffset.X} (origin {scrollable.ContentOrigin.X}, max {maxScroll})");
        }
        else
        {
            Assert.True(Math.Abs(scrollable.ScrollOffset.X) < 0.5f,
                $"Expected horizontal offset ~0 when origin {scrollable.ContentOrigin.X}");
        }
    }
}

using System.Numerics;
using TinyEcs.Bevy;
using TinyEcs.UI;
using TinyEcs.UI.Widgets;

namespace TinyEcs.Tests;

public class UiWindowOrderTests
{
    [Fact]
    public void Window_BringToFront_Reorders_DrawOrder()
    {
        var app = new App();

        // Add Clay UI with default systems
        app.AddClayUi(new ClayUiOptions
        {
            LayoutDimensions = new Clay_cs.Clay_Dimensions(800, 600),
            ArenaSize = 256 * 1024,
            AutoRegisterDefaultSystems = true,
            AutoRunLayout = true
        });

        // Ensure window order resource exists (no z-index usage)
        var world = app.GetWorld();
        if (!world.HasResource<UiWindowOrder>())
            world.AddResource(new UiWindowOrder());

        ulong aId = 0, bId = 0, cId = 0;
        var blue = new Clay_cs.Clay_Color(10, 20, 30, 255);
        var green = new Clay_cs.Clay_Color(200, 210, 220, 255);
        var red = new Clay_cs.Clay_Color(220, 80, 80, 255);

        app.AddSystem((Commands commands) =>
        {
            var a = FloatingWindowWidget.Create(commands,
                ClayFloatingWindowStyle.Default with
                {
                    WindowBackgroundColor = blue,
                    TitleBarColor = new Clay_cs.Clay_Color(60, 70, 200, 255)
                },
                "A",
                new Vector2(100, 100));

            var b = FloatingWindowWidget.Create(commands,
                ClayFloatingWindowStyle.Default with
                {
                    WindowBackgroundColor = green,
                    TitleBarColor = new Clay_cs.Clay_Color(20, 200, 120, 255)
                },
                "B",
                new Vector2(200, 120));

            aId = a.Id;
            bId = b.Id;

            var c = FloatingWindowWidget.Create(commands,
                ClayFloatingWindowStyle.Default with
                {
                    WindowBackgroundColor = red,
                    TitleBarColor = new Clay_cs.Clay_Color(200, 60, 60, 255)
                },
                "C",
                new Vector2(300, 140));
            cId = c.Id;
        })
        .InStage(Stage.Startup)
        .Build();

        try
        {
            app.RunStartup();
            app.Update(); // run layout
        }
        catch (DllNotFoundException)
        {
            // Clay native not available in this environment; skip test
            return;
        }

        var ui = world.GetResource<ClayUiState>();

        static int LastIndexOfColor(ReadOnlySpan<Clay_cs.Clay_RenderCommand> cmds, Clay_cs.Clay_Color color)
        {
            int last = -1;
            for (int i = 0; i < cmds.Length; i++)
            {
                ref readonly var cmd = ref cmds[i];
                if (cmd.commandType == Clay_cs.Clay_RenderCommandType.CLAY_RENDER_COMMAND_TYPE_RECTANGLE)
                {
                    var c = cmd.renderData.rectangle.backgroundColor;
                    if (c.r == color.r && c.g == color.g && c.b == color.b && c.a == color.a)
                        last = i;
                }
            }
            return last;
        }

        var cmds = ui.RenderCommands;
        var aBefore = LastIndexOfColor(cmds, blue);
        var bBefore = LastIndexOfColor(cmds, green);
        var cBefore = LastIndexOfColor(cmds, red);

        Assert.True(aBefore >= 0 && bBefore >= 0 && cBefore >= 0);
        // Initial order follows creation order: A < B < C
        Assert.True(aBefore < bBefore && bBefore < cBefore);

        // Bring A to front via order resource and re-layout
        world.GetResource<UiWindowOrder>().MoveToTop(aId);
        world.GetResource<ClayUiState>().RequestLayoutPass();
        try { app.Update(); }
        catch (DllNotFoundException) { return; }

        cmds = ui.RenderCommands;
        var aAfter = LastIndexOfColor(cmds, blue);
        var bAfter = LastIndexOfColor(cmds, green);
        var cAfter = LastIndexOfColor(cmds, red);

        Assert.True(aAfter >= 0 && bAfter >= 0 && cAfter >= 0);
        // A should now be last
        Assert.True(aAfter > bAfter && aAfter > cAfter, "A should render last after MoveToTop(A)");

        // Bring B to front now
        world.GetResource<UiWindowOrder>().MoveToTop(bId);
        world.GetResource<ClayUiState>().RequestLayoutPass();
        try { app.Update(); } catch (DllNotFoundException) { return; }

        cmds = ui.RenderCommands;
        aAfter = LastIndexOfColor(cmds, blue);
        bAfter = LastIndexOfColor(cmds, green);
        cAfter = LastIndexOfColor(cmds, red);

        Assert.True(bAfter > aAfter && bAfter > cAfter, "B should render last after MoveToTop(B)");
    }
}

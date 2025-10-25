using System;
using System.Numerics;
using Flexbox;
using Raylib_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI;

namespace MyBattleground;

/// <summary>
/// Example demonstrating Flexbox UI system in TinyEcs.
/// Creates a simple UI with buttons, labels, and panels using Flexbox layout.
///
/// Run with: dotnet run --project samples/MyBattleground
/// </summary>
public struct FlexboxUiExamplePlugin : IPlugin
{
    public void Build(App app)
    {
        // Add Flexbox UI plugins
        app.AddPlugin(new FlexboxUiPlugin
        {
            AutoCreatePointerState = true,
            ContainerWidth = 1280f,
            ContainerHeight = 720f
        });

        app.AddPlugin(new RaylibFlexboxUiPlugin());

        // Add interaction system
        app.AddSystem(UiInteractionSystems.UpdateInteractionState)
            .InStage(Stage.PreUpdate)
            .Label("ui:flexbox:interaction")
            .After("ui:flexbox:pointer")
            .Build();

        // Add button visual update system
        app.AddSystem(FlexboxButtonSystems.UpdateButtonVisuals)
            .InStage(Stage.PreUpdate)
            .After("ui:flexbox:interaction")
            .Build();

        // Build UI in startup
        app.AddSystem(BuildFlexboxUI)
            .InStage(Stage.Startup)
            .Build();
    }

    private static void BuildFlexboxUI(Commands commands)
    {
        // Create root container (centered column layout)
        var rootId = FlexboxPanelWidget.CreateColumn(commands, FlexboxPanelStyle.Transparent())
            .Insert(new FlexboxNode
            {
                FlexDirection = FlexDirection.Column,
                JustifyContent = Justify.Center,
                AlignItems = Align.Center,
                Width = FlexValue.Percent(100f),
                Height = FlexValue.Percent(100f),
                BackgroundColor = new Vector4(0.1f, 0.1f, 0.15f, 1f)
            })
            .Id;

        // Create main card panel
        var cardId = FlexboxPanelWidget.CreateColumn(commands, FlexboxPanelStyle.Card())
            .Insert(new FlexboxNode
            {
                FlexDirection = FlexDirection.Column,
                JustifyContent = Justify.FlexStart,
                AlignItems = Align.Stretch,
                Width = FlexValue.Points(400f),
                Height = FlexValue.Auto(),
                PaddingTop = 24f,
                PaddingRight = 24f,
                PaddingBottom = 24f,
                PaddingLeft = 24f,
                BackgroundColor = new Vector4(0.2f, 0.2f, 0.25f, 1f),
                BorderRadius = 16f
            })
            .Insert(new FlexboxNodeParent(rootId))
            .Id;

        // Add title
        FlexboxLabelWidget.CreateHeading1(commands, "Flexbox UI Demo")
            .Insert(new FlexboxNodeParent(cardId))
            .Insert(new FlexboxNode
            {
                MarginBottom = 16f
            });

        // Add subtitle
        FlexboxLabelWidget.CreateBody(commands, "Click the buttons below to test interaction")
            .Insert(new FlexboxNodeParent(cardId))
            .Insert(new FlexboxNode
            {
                MarginBottom = 24f
            });

        // Create button container (row layout)
        var buttonRowId = FlexboxPanelWidget.CreateRow(commands, FlexboxPanelStyle.Transparent())
            .Insert(new FlexboxNode
            {
                FlexDirection = FlexDirection.Row,
                JustifyContent = Justify.SpaceBetween,
                AlignItems = Align.Center,
                Width = FlexValue.Percent(100f),
                MarginBottom = 12f
            })
            .Insert(new FlexboxNodeParent(cardId))
            .Id;

        // Add buttons
        FlexboxButtonWidget.Create(
            commands,
            "Primary",
            (trigger) => Console.WriteLine("Primary button clicked!"),
            new FlexboxButtonStyle
            {
                Width = FlexValue.Auto(),
                Height = FlexValue.Points(40f),
                PaddingHorizontal = 20f,
                PaddingVertical = 10f,
                BackgroundColor = new Vector4(0.3f, 0.5f, 0.9f, 1f),
                HoverColor = new Vector4(0.4f, 0.6f, 1f, 1f),
                PressedColor = new Vector4(0.2f, 0.4f, 0.8f, 1f),
                TextColor = Vector4.One,
                FontSize = 16f,
                BorderRadius = 6f
            })
            .Insert(new FlexboxNodeParent(buttonRowId));

        FlexboxButtonWidget.Create(
            commands,
            "Secondary",
            (trigger) => Console.WriteLine("Secondary button clicked!"),
            new FlexboxButtonStyle
            {
                Width = FlexValue.Auto(),
                Height = FlexValue.Points(40f),
                PaddingHorizontal = 20f,
                PaddingVertical = 10f,
                BackgroundColor = new Vector4(0.4f, 0.4f, 0.4f, 1f),
                HoverColor = new Vector4(0.5f, 0.5f, 0.5f, 1f),
                PressedColor = new Vector4(0.3f, 0.3f, 0.3f, 1f),
                TextColor = Vector4.One,
                FontSize = 16f,
                BorderRadius = 6f
            })
            .Insert(new FlexboxNodeParent(buttonRowId));

        // Add another row with danger button
        var buttonRow2Id = FlexboxPanelWidget.CreateRow(commands, FlexboxPanelStyle.Transparent())
            .Insert(new FlexboxNode
            {
                FlexDirection = FlexDirection.Row,
                JustifyContent = Justify.Center,
                AlignItems = Align.Center,
                Width = FlexValue.Percent(100f),
                MarginTop = 12f
            })
            .Insert(new FlexboxNodeParent(cardId))
            .Id;

        FlexboxButtonWidget.Create(
            commands,
            "Danger Action",
            (trigger) => Console.WriteLine("Danger button clicked!"),
            new FlexboxButtonStyle
            {
                Width = FlexValue.Percent(100f),
                Height = FlexValue.Points(44f),
                PaddingHorizontal = 20f,
                PaddingVertical = 12f,
                BackgroundColor = new Vector4(0.8f, 0.2f, 0.2f, 1f),
                HoverColor = new Vector4(0.9f, 0.3f, 0.3f, 1f),
                PressedColor = new Vector4(0.7f, 0.1f, 0.1f, 1f),
                TextColor = Vector4.One,
                FontSize = 16f,
                BorderRadius = 6f
            })
            .Insert(new FlexboxNodeParent(buttonRow2Id));

        // Add info footer
        FlexboxLabelWidget.CreateCaption(commands, "Built with TinyEcs Flexbox UI")
            .Insert(new FlexboxNodeParent(cardId))
            .Insert(new FlexboxNode
            {
                MarginTop = 24f
            });

        Console.WriteLine("Flexbox UI built successfully!");
    }
}

/// <summary>
/// Main entry point for Flexbox UI example.
/// Uncomment the Run() call in MyBattleground/Program.cs to test.
/// </summary>
public static class FlexboxUiExampleRunner
{
    public static void Run()
    {
        Raylib.InitWindow(1280, 720, "TinyEcs - Flexbox UI Example");
        Raylib.SetTargetFPS(60);

        var app = new App(ThreadingMode.Single);

        // Add Flexbox UI example
        app.AddPlugin(new FlexboxUiExamplePlugin());

        // Add window resize handler
        app.AddSystem((ResMut<FlexboxUiState> uiState) =>
        {
            var width = Raylib.GetScreenWidth();
            var height = Raylib.GetScreenHeight();
            ref var state = ref uiState.Value;
            if (state.ContainerWidth != width || state.ContainerHeight != height)
            {
                state.ContainerWidth = width;
                state.ContainerHeight = height;
                state.MarkDirty();
            }
        })
        .InStage(Stage.PreUpdate)
        .Label("update-container-size")
        .Build();

        // Add rendering systems
        app.AddSystem((World _) =>
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(26, 26, 38, 255));
        })
        .InStage(Stage.Last)
        .Label("begin-rendering")
        .Build();

        app.AddSystem((World _) =>
        {
            Raylib.DrawFPS(10, 10);
            Raylib.EndDrawing();
        })
        .InStage(Stage.Last)
        .Label("end-rendering")
        .After("ui:raylib:flexbox:render")
        .Build();

        app.RunStartup();

        while (!Raylib.WindowShouldClose())
        {
            app.Update();
        }

        Raylib.CloseWindow();
    }
}

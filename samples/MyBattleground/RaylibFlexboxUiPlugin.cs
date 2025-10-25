using System;
using System.Numerics;
using Raylib_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI;

namespace MyBattleground;

/// <summary>
/// Raylib integration plugin for Flexbox UI system.
/// Parallel to RaylibClayUiPlugin but renders Flexbox computed layouts.
///
/// Responsibilities:
/// - Updates FlexboxPointerState from Raylib mouse input
/// - Renders FlexboxNode entities using computed layouts from FlexboxUiState
///
/// Usage:
/// <code>
/// var app = new App();
/// app.AddPlugin(new FlexboxUiPlugin());
/// app.AddPlugin(new RaylibFlexboxUiPlugin
/// {
///     RenderingStage = customRenderingStage
/// });
/// </code>
/// </summary>
public struct RaylibFlexboxUiPlugin : IPlugin
{
    /// <summary>
    /// The stage where UI rendering occurs (e.g., custom "Rendering" stage).
    /// If not set, defaults to Stage.Last.
    /// </summary>
    public Stage RenderingStage { get; set; }

    public void Build(App app)
    {
        var renderStage = RenderingStage.CustomStageId != 0 ? RenderingStage : Stage.Last;

        // System 1: Update pointer state from Raylib input (PreUpdate)
        app.AddSystem(UpdatePointerState)
            .InStage(Stage.PreUpdate)
            .Label("ui:raylib:flexbox:update-pointer")
            .RunIfResourceExists<FlexboxPointerState>()
            .SingleThreaded() // Raylib calls must be single-threaded
            .Build();

        // System 2: Render Flexbox UI (Rendering stage)
        app.AddSystem(RenderFlexboxUI)
            .InStage(renderStage)
            .Label("ui:raylib:flexbox:render")
            .RunIfResourceExists<FlexboxUiState>()
            .SingleThreaded() // Raylib calls must be single-threaded
            .Build();
    }

    /// <summary>
    /// Updates FlexboxPointerState from Raylib mouse input.
    /// </summary>
    private static void UpdatePointerState(ResMut<FlexboxPointerState> pointerState)
    {
        if (!Raylib.IsWindowReady())
            return;

        ref var state = ref pointerState.Value;

        // Update pointer position
        state.Position = Raylib.GetMousePosition();

        // Update button state
        state.PrimaryDown = Raylib.IsMouseButtonDown(MouseButton.Left);

        // Update scroll delta
        var scrollY = Raylib.GetMouseWheelMove();
        if (scrollY != 0f)
        {
            state.AddScroll(new Vector2(0, scrollY * 20f));
        }

        // Update delta time
        state.DeltaTime = Raylib.GetFrameTime();
    }

    /// <summary>
    /// Renders Flexbox UI using computed layouts.
    /// </summary>
    private static void RenderFlexboxUI(
        Res<FlexboxUiState> uiState,
        Query<Data<FlexboxNode>> nodes,
        Query<Data<FlexboxText>> texts)
    {
        ref var state = ref uiState.Value;

        // Render all entities with computed layouts
        foreach (var (entityId, node) in nodes)
        {
            if (!state.EntityToLayout.TryGetValue(entityId.Ref, out var layout))
                continue;

            ref var nodeRef = ref node.Ref;

            // Draw background rectangle
            if (nodeRef.BackgroundColor.W > 0f) // Alpha > 0
            {
                DrawRectangle(layout, nodeRef.BackgroundColor, nodeRef.BorderRadius);
            }

            // Draw border
            if (nodeRef.BorderColor.W > 0f)
            {
                DrawBorder(layout, nodeRef.BorderColor, nodeRef.BorderRadius);
            }

            // Draw text if entity has FlexboxText component
            if (texts.Contains(entityId.Ref))
            {
                ref var textData = ref texts.Get(entityId.Ref).Ref;
                DrawText(layout, ref textData);
            }
        }
    }

    private static void DrawRectangle(ComputedLayout layout, Vector4 color, float borderRadius)
    {
        var rect = new Rectangle(
            layout.Position.X,
            layout.Position.Y,
            layout.Size.X,
            layout.Size.Y);

        var raylibColor = new Color(
            (byte)(color.X * 255),
            (byte)(color.Y * 255),
            (byte)(color.Z * 255),
            (byte)(color.W * 255));

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
        var rect = new Rectangle(
            layout.Position.X,
            layout.Position.Y,
            layout.Size.X,
            layout.Size.Y);

        var raylibColor = new Color(
            (byte)(color.X * 255),
            (byte)(color.Y * 255),
            (byte)(color.Z * 255),
            (byte)(color.W * 255));

        if (borderRadius > 0f)
        {
            Raylib.DrawRectangleRoundedLines(rect, borderRadius / Math.Max(layout.Size.X, layout.Size.Y), 8, 2f, raylibColor);
        }
        else
        {
            Raylib.DrawRectangleLinesEx(rect, 2f, raylibColor);
        }
    }

    private static void DrawText(ComputedLayout layout, ref FlexboxText textData)
    {
        if (string.IsNullOrEmpty(textData.Text))
            return;

        var raylibColor = new Color(
            (byte)(textData.Color.X * 255),
            (byte)(textData.Color.Y * 255),
            (byte)(textData.Color.Z * 255),
            (byte)(textData.Color.W * 255));

        // Use content position for text (accounts for padding)
        var textPos = new Vector2(
            layout.ContentPosition.X,
            layout.ContentPosition.Y);

        Raylib.DrawText(
            textData.Text,
            (int)textPos.X,
            (int)textPos.Y,
            (int)textData.FontSize,
            raylibColor);
    }
}

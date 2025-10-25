using TinyEcs.Bevy;

namespace TinyEcs.UI;

/// <summary>
/// Core plugin for Flexbox-based UI system.
/// Parallel to ClayUiPlugin but uses Flexbox layout engine.
///
/// Registers:
/// - FlexboxUiState resource (layout state and computed results)
/// - FlexboxPointerState resource (optional, if AutoCreatePointerState = true)
/// - Hierarchy sync system (FlexboxNodeParent → Parent/Children)
/// - Layout computation system (builds Flexbox tree and computes layouts)
/// - Pointer processing system (hit testing and event dispatch)
///
/// Usage:
/// <code>
/// var app = new App();
/// app.AddPlugin(new FlexboxUiPlugin
/// {
///     AutoCreatePointerState = true,
///     ContainerWidth = 1920f,
///     ContainerHeight = 1080f
/// });
/// </code>
/// </summary>
public struct FlexboxUiPlugin : IPlugin
{
    /// <summary>
    /// Whether to automatically create FlexboxPointerState resource.
    /// Set to false if you want to manage pointer state manually.
    /// </summary>
    public bool AutoCreatePointerState { get; set; }

    /// <summary>
    /// Container dimensions for root layout calculation.
    /// </summary>
    public float ContainerWidth { get; set; }
    public float ContainerHeight { get; set; }

    public FlexboxUiPlugin()
    {
        AutoCreatePointerState = true;
        ContainerWidth = 1920f;
        ContainerHeight = 1080f;
    }

    public void Build(App app)
    {
        // Register FlexboxUiState resource
        var uiState = new FlexboxUiState
        {
            ContainerWidth = ContainerWidth,
            ContainerHeight = ContainerHeight
        };
        app.AddResource(uiState);

        // Optionally create FlexboxPointerState
        if (AutoCreatePointerState)
        {
            app.AddResource(new FlexboxPointerState());
        }

        // System 1: Sync FlexboxNodeParent → Parent/Children hierarchy (PreUpdate)
        app.AddSystem((Commands commands,
                       Query<Data<FlexboxNodeParent>, Filter<Changed<FlexboxNodeParent>>> desired,
                       Query<Data<Parent>> parents,
                       Query<Data<Children>> children) =>
            FlexboxLayoutSystems.SyncHierarchy(commands, desired, parents, children))
            .InStage(Stage.PreUpdate)
            .Label("ui:flexbox:sync-hierarchy")
            .RunIfResourceExists<FlexboxUiState>()
            .Build();

        // System 2: Compute Flexbox layout (Update)
        app.AddSystem((ResMut<FlexboxUiState> state,
                       Query<Data<FlexboxNode>> nodes,
                       Query<Data<Parent>> parents,
                       Query<Data<Children>> childrenQuery,
                       Query<Data<FlexboxText>> texts) =>
            FlexboxLayoutSystems.ComputeLayout(state, nodes, parents, childrenQuery, texts))
            .InStage(Stage.Update)
            .Label("ui:flexbox:layout")
            .After("ui:flexbox:sync-hierarchy")
            .RunIfResourceExists<FlexboxUiState>()
            .Build();

        // System 3: Process pointer input and fire events (PreUpdate, after sync, before layout)
        app.AddSystem((ResMut<FlexboxPointerState> pointerState,
                       ResMut<FlexboxUiState> uiState,
                       EventWriter<UiPointerEvent> events,
                       Commands commands,
                       Query<Data<Parent>> parents,
                       Query<Data<FlexboxInteractive>> interactives) =>
            FlexboxPointerSystems.ApplyPointerInput(pointerState, uiState, events, commands, parents, interactives))
            .InStage(Stage.PreUpdate)
            .Label("ui:flexbox:pointer")
            .After("ui:flexbox:sync-hierarchy")
            .RunIfResourceExists<FlexboxPointerState>()
            .RunIfResourceExists<FlexboxUiState>()
            .Build();
    }
}

/// <summary>
/// Extension methods for FlexboxUiPlugin.
/// </summary>
public static class FlexboxUiPluginExtensions
{
    /// <summary>
    /// Gets the FlexboxUiState resource from the world.
    /// </summary>
    public static FlexboxUiState GetFlexboxUiState(this World world)
    {
        return world.GetResource<FlexboxUiState>();
    }

    /// <summary>
    /// Gets the FlexboxPointerState resource from the world.
    /// </summary>
    public static FlexboxPointerState GetFlexboxPointerState(this World world)
    {
        return world.GetResource<FlexboxPointerState>();
    }

    /// <summary>
    /// Updates container dimensions for Flexbox layout.
    /// </summary>
    public static void SetFlexboxContainerSize(this World world, float width, float height)
    {
        var state = world.GetResource<FlexboxUiState>();
        state.ContainerWidth = width;
        state.ContainerHeight = height;
        state.MarkDirty();
    }
}

using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI.Widgets;

namespace TinyEcs.UI;

/// <summary>
/// Installs Clay UI resources and schedules the layout pass within the Bevy-style app.
/// </summary>
public sealed class ClayUiPlugin : IPlugin
{
	public ClayUiOptions Options { get; set; } = ClayUiOptions.Default;

	public void Build(App app)
	{
		var world = app.GetWorld();
		var state = EnsureState(world);

		// Apply options directly to state
		state.Options = Options;
		state.UseEntityHierarchy = Options.UseEntityHierarchy;

		if (Options.AutoCreatePointerState && !world.HasResource<ClayPointerState>())
		{
			world.AddResource(new ClayPointerState());
		}

		if (Options.AutoRegisterDefaultSystems)
		{
			RegisterDefaultSystems(app);
		}

		// Register observer to clean up element ID mapping when UiNode is removed
		app.AddObserver<OnRemove<UiNode>, ResMut<ClayUiState>>(OnUiNodeRemoved);
	}

	private static ClayUiState EnsureState(World world)
	{
		ClayUiState state;
		if (world.HasResource<ClayUiState>())
		{
			state = world.GetResource<ClayUiState>();
		}
		else
		{
			state = new ClayUiState();
			world.AddResource(state);
		}

		return state;
	}

	private void RegisterDefaultSystems(App app)
	{
		app.AddSystem((ResMut<ClayUiState> ui, Commands commands, Query<Data<UiNodeParent>, Filter<Changed<UiNodeParent>>> desired, Query<Data<Parent>> current, Query<Data<Children>> children, Query<Data<FloatingWindowLinks>> windowLinks) =>
			ClayUiSystems.SyncUiHierarchy(ui, commands, desired, current, children, windowLinks))
		.InStage(Stage.PreUpdate)
		.Label("ui:clay:sync-hierarchy")
		.RunIfResourceExists<ClayUiState>()
		.Build();

		app.AddSystem((Commands commands, ResMut<ClayPointerState> pointer, ResMut<ClayUiState> ui, EventWriter<UiPointerEvent> events, Query<Data<Parent>> parents, Query<Data<UiNode>> allNodes) =>
			ClayUiSystems.ApplyPointerInput(commands, pointer, ui, events, parents, allNodes))
		.InStage(Stage.PreUpdate)
		.Label("ui:clay:pointer")
		.RunIfResourceExists<ClayUiState>()
		.RunIfResourceExists<ClayPointerState>()
		.After("ui:clay:sync-hierarchy")
		.Build();

		// NOTE: Scroll offset save system removed - it was duplicate work
		// ClayUiEntityLayout.RenderNode already retrieves scroll offsets during layout pass
		// (see ClayUiEntityLayout.cs:65-70)

		app.AddSystem((
			ResMut<ClayUiState> stateParam,
			Query<Data<UiNode>, Filter<Without<Parent>>> roots,
			Query<Data<UiNode>> allNodes,
			Query<Data<UiText>> texts,
			Query<Data<Children>> childLists,
			Query<Data<FloatingWindowState>> floatingWindows,
			ResMut<UiWindowOrder> windowOrder,
			Local<HashSet<ulong>> windows) =>
		{
			ClayLayoutSystems.RunLayoutPass(stateParam, roots, allNodes, texts, childLists, floatingWindows, windowOrder, windows);
		})
		.InStage(Stage.Update)
		.Label("ui:clay:layout")
		.RunIfResourceExists<ClayUiState>()
		.Build();
	}

	private static void OnUiNodeRemoved(OnRemove<UiNode> trigger, ResMut<ClayUiState> uiState)
	{
		ref var state = ref uiState.Value;

		// Remove the element ID → entity ID mapping when UiNode is removed
		if (trigger.Component.Declaration.id.id != 0)
		{
			state.ElementToEntityMap.Remove(trigger.Component.Declaration.id.id);
		}
	}
}

public static class ClayUiAppExtensions
{
	public static App AddClayUi(this App app)
	{
		app.AddPlugin(new ClayUiPlugin());
		return app;
	}

	public static App AddClayUi(this App app, ClayUiOptions options)
	{
		app.AddPlugin(new ClayUiPlugin { Options = options });

		var world = app.GetWorld();
		if (world.HasResource<ClayUiState>())
		{
			var state = world.GetResource<ClayUiState>();
			state.Options = options;
			state.UseEntityHierarchy = options.UseEntityHierarchy;
		}

		return app;
	}
}

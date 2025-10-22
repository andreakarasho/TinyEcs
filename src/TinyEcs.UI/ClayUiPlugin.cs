using TinyEcs;
using TinyEcs.Bevy;

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

		state.ApplyOptions(Options);
		state.SetEntityHierarchyEnabled(Options.UseEntityHierarchy);

		if (Options.AutoCreatePointerState && !world.HasResource<ClayPointerState>())
		{
			world.AddResource(new ClayPointerState());
		}

		if (Options.AutoRegisterDefaultSystems)
		{
			RegisterDefaultSystems(app);
		}
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

		state.AttachWorld(world);
		return state;
	}

	private void RegisterDefaultSystems(App app)
	{
		app.AddSystem((ResMut<ClayUiState> ui, Commands commands, Query<Data<UiNodeParent>> desired, Query<Data<Parent>> current, Query<Data<Children>> children) =>
			ClayUiSystems.SyncUiHierarchy(ui, commands, desired, current, children))
		.InStage(Stage.PreUpdate)
		.Label("ui:clay:sync-hierarchy")
		.RunIfResourceExists<ClayUiState>()
		.Build();

		app.AddSystem((ResMut<ClayPointerState> pointer, ResMut<ClayUiState> ui, EventWriter<UiPointerEvent> events, Query<Data<Parent>> parents) =>
			ClayUiSystems.ApplyPointerInput(pointer, ui, events, parents))
		.InStage(Stage.PreUpdate)
		.Label("ui:clay:pointer")
		.RunIfResourceExists<ClayUiState>()
		.RunIfResourceExists<ClayPointerState>()
		.After("ui:clay:sync-hierarchy")
		.Build();

		app.AddSystem((ResMut<ClayUiState> ui, Query<Data<UiNode>, Filter<Changed<UiNode>>> changedNodes) =>
			ClayUiSystems.RequestLayoutOnNodeChange(ui, changedNodes))
		.InStage(Stage.PreUpdate)
		.Label("ui:clay:mark-nodes")
		.RunIfResourceExists<ClayUiState>()
		.After("ui:clay:pointer")
		.Build();

		app.AddSystem((ResMut<ClayUiState> ui, Query<Data<UiText>, Filter<Changed<UiText>>> changedTexts) =>
			ClayUiSystems.RequestLayoutOnTextChange(ui, changedTexts))
		.InStage(Stage.PreUpdate)
		.Label("ui:clay:mark-text")
		.RunIfResourceExists<ClayUiState>()
		.After("ui:clay:mark-nodes")
		.Build();

		app.AddSystem((
			ResMut<ClayUiState> stateParam,
			Query<Data<UiNode>, Filter<Without<Parent>>> roots,
			Query<Data<UiNode>> allNodes,
			Query<Data<UiText>> texts,
			Query<Data<Children>> childLists) =>
		{
			ref var state = ref stateParam.Value;
			state.RunLayoutPass(roots, allNodes, texts, childLists);
		})
		.InStage(Stage.Update)
		.Label("ui:clay:layout")
		.RunIfResourceExists<ClayUiState>()
		.Build();
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
			state.AttachWorld(world);
			state.ApplyOptions(options);
			state.RequestLayoutPass();
		}

		return app;
	}
}

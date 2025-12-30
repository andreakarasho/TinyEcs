using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Resource that provides delta time (time since last frame) for UI systems.
/// Must be updated each frame by the application (typically in Stage.First).
/// </summary>
public class DeltaTime
{
	/// <summary>Time since last frame in seconds</summary>
	public float Seconds { get; set; }

	public DeltaTime()
	{
		Seconds = 0f;
	}
}

/// <summary>
/// Plugin that provides interaction state tracking for UI elements.
/// Uses Bevy's simple approach: just track current state (None, Hovered, Pressed).
/// Widgets check current state directly via Changed&lt;InteractionState&gt;.
/// </summary>
public struct InteractionPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// Register resources
		app.AddResource(new DeltaTime());

		// Add InteractionState to Interactive entities that don't have it
		app.AddSystem((
			Commands commands,
			Query<Data<Interactive>, Filter<Without<InteractionState>>> interactiveWithoutState) =>
		{
			foreach (var (entityId, _) in interactiveWithoutState)
			{
				commands.Entity(entityId.Ref).Insert(new InteractionState());
			}
		})
		.InStage(Stage.PreUpdate)
		.Label("interaction:add-to-interactive")
		.Build();
	}
}

/// <summary>
/// Interaction state that directly reflects pointer events.
/// This is analogous to Bevy's built-in Interaction enum.
/// </summary>
public enum Interaction
{
	/// <summary>No interaction</summary>
	None,
	/// <summary>Pointer is hovering over the element</summary>
	Hovered,
	/// <summary>Pointer is pressing the element</summary>
	Pressed
}

/// <summary>
/// Component that tracks the current interaction state.
/// Updated by pointer event handlers.
/// Widgets react to state changes via Changed&lt;InteractionState&gt;.
/// </summary>
public struct InteractionState
{
	public Interaction State;

	public InteractionState()
	{
		State = Interaction.None;
	}
}

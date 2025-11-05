using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Component that represents a toggle/switch widget.
/// Similar to a checkbox but with a sliding animation (when supported).
/// Represents an on/off binary state.
/// </summary>
public struct Toggle
{
	/// <summary>Whether the toggle is currently on (true) or off (false)</summary>
	public bool IsOn;

	/// <summary>Entity ID of the thumb/handle element</summary>
	public ulong ThumbEntity;

	/// <summary>Entity ID of the track/background element</summary>
	public ulong TrackEntity;

	public Toggle(bool initialValue = false)
	{
		IsOn = initialValue;
		ThumbEntity = 0;
		TrackEntity = 0;
	}
}

/// <summary>
/// Event triggered when a toggle state changes.
/// Use with On&lt;ToggleChanged&gt; in observers.
/// </summary>
public readonly struct ToggleChanged
{
	public readonly bool IsOn;

	public ToggleChanged(bool isOn)
	{
		IsOn = isOn;
	}
}

/// <summary>
/// Plugin that adds toggle/switch widget functionality.
/// Handles toggling state on click and updating visual state.
///
/// Usage:
/// <code>
/// app.AddPlugin(new TogglePlugin());
/// </code>
/// </summary>
public struct TogglePlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// System to toggle state when clicked
		app.AddSystem((
			Commands commands,
			Query<Data<Toggle, FluxInteraction>, Filter<Changed<FluxInteraction>>> toggles) =>
		{
			HandleToggleClick(commands, toggles);
		})
		.InStage(Stage.PreUpdate)
		.Label("toggle:handle-click")
		.After("flux:update-interaction")
		.Build();

		// System to update visual state when toggle changes
		app.AddSystem((
			Commands commands,
			Query<Data<Toggle>, Filter<Changed<Toggle>>> changedToggles,
			Query<Data<UiNode>> allNodes,
			Query<Data<BackgroundColor>> backgroundColors) =>
		{
			UpdateToggleVisuals(commands, changedToggles, allNodes, backgroundColors);
		})
		.InStage(Stage.PreUpdate)
		.Label("toggle:update-visuals")
		.After("toggle:handle-click")
		.Build();
	}

	/// <summary>
	/// Toggles state when FluxInteraction.Released is detected.
	/// </summary>
	private static void HandleToggleClick(
		Commands commands,
		Query<Data<Toggle, FluxInteraction>, Filter<Changed<FluxInteraction>>> toggles)
	{
		foreach (var (entityId, toggle, flux) in toggles)
		{
			ref var t = ref toggle.Ref;
			ref readonly var interaction = ref flux.Ref;

			// Toggle on release (click)
			if (interaction.State == FluxInteractionState.Released)
			{
				t.IsOn = !t.IsOn;

				// Re-insert to trigger change detection
				commands.Entity(entityId.Ref).Insert(t);

				// Emit ToggleChanged event both globally and per-entity
				var changeEvent = new ToggleChanged(t.IsOn);
				commands.Entity(entityId.Ref).EmitTrigger(changeEvent);  // Per-entity (BevyObservers)
				commands.EmitTrigger(changeEvent);  // Global (EventChannel)
			}
		}
	}

	/// <summary>
	/// Updates the thumb position and track color based on toggle state.
	/// </summary>
	private static void UpdateToggleVisuals(
		Commands commands,
		Query<Data<Toggle>, Filter<Changed<Toggle>>> changedToggles,
		Query<Data<UiNode>> allNodes,
		Query<Data<BackgroundColor>> backgroundColors)
	{
		foreach (var (entityId, toggle) in changedToggles)
		{
			ref readonly var t = ref toggle.Ref;

			// Update thumb position (slide to right when on, left when off)
			if (t.ThumbEntity != 0 && allNodes.Contains(t.ThumbEntity))
			{
				var (_, thumbNode) = allNodes.Get(t.ThumbEntity);
				ref var thumb = ref thumbNode.Ref;

				// Position thumb
				// When off: align to left (JustifyContent.FlexStart)
				// When on: align to right (JustifyContent.FlexEnd)
				// This is typically done via parent container's JustifyContent
				// For absolute positioning:
				if (t.IsOn)
				{
					// Move to right side
					thumb.Left = FlexValue.Auto();
					thumb.Right = FlexValue.Points(2f); // Small padding
				}
				else
				{
					// Move to left side
					thumb.Left = FlexValue.Points(2f); // Small padding
					thumb.Right = FlexValue.Auto();
				}

				thumb.PositionType = Flexbox.PositionType.Absolute;
				commands.Entity(t.ThumbEntity).Insert(thumb);
			}

			// Update track color (optional - can change background when on/off)
			if (t.TrackEntity != 0 && backgroundColors.Contains(t.TrackEntity))
			{
				var (_, bgColor) = backgroundColors.Get(t.TrackEntity);
				ref var color = ref bgColor.Ref;

				// Change track color based on state
				// On: Green/Accent color (e.g., #4CAF50)
				// Off: Gray/Neutral color (e.g., #9E9E9E)
				if (t.IsOn)
				{
					color.Color = new System.Numerics.Vector4(0.3f, 0.69f, 0.31f, 1f); // Green
				}
				else
				{
					color.Color = new System.Numerics.Vector4(0.62f, 0.62f, 0.62f, 1f); // Gray
				}

				commands.Entity(t.TrackEntity).Insert(color);
			}
		}
	}
}

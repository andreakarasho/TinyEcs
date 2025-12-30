using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Component that represents a toggle/switch widget.
/// Similar to a checkbox but with a sliding animation (when supported).
/// Represents an on/off binary state.
/// The toggle entity itself is the track - child elements are identified by marker components:
/// - ToggleThumb: the sliding thumb/handle element
/// </summary>
public struct Toggle
{
	/// <summary>Whether the toggle is currently on (true) or off (false)</summary>
	public bool IsOn;

	public Toggle(bool initialValue = false)
	{
		IsOn = initialValue;
	}
}

/// <summary>
/// Marker component for toggle thumb elements.
/// Used to identify the sliding thumb/handle inside a toggle.
/// </summary>
public struct ToggleThumb { }

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
		// System to toggle state when pressed
		app.AddSystem((
			Commands commands,
			Query<Data<Toggle, InteractionState>, Filter<Changed<InteractionState>>> toggles) =>
		{
			HandleToggleClick(commands, toggles);
		})
		.InStage(Stage.PreUpdate)
		.Label("toggle:handle-click")
		.After("interaction:add-to-interactive")
		.Build();

		// System to update visual state when toggle changes
		app.AddSystem((
			Commands commands,
			Query<Data<Toggle, BackgroundColor>, Filter<Changed<Toggle>>> changedToggles,
			Query<Data<Parent, UiNode>, Filter<With<ToggleThumb>>> thumbs) =>
		{
			UpdateToggleVisuals(commands, changedToggles, thumbs);
		})
		.InStage(Stage.PreUpdate)
		.Label("toggle:update-visuals")
		.After("toggle:handle-click")
		.Build();
	}

	/// <summary>
	/// Toggles state when Interaction.Pressed is detected.
	/// </summary>
	private static void HandleToggleClick(
		Commands commands,
		Query<Data<Toggle, InteractionState>, Filter<Changed<InteractionState>>> toggles)
	{
		foreach (var (entityId, toggle, interaction) in toggles)
		{
			ref var t = ref toggle.Ref;
			ref readonly var state = ref interaction.Ref;

			// Toggle on press
			if (state.State == Interaction.Pressed)
			{
				t.IsOn = !t.IsOn;

				// Re-insert to trigger change detection
				commands.Entity(entityId.Ref).Insert(t);

				// Emit ToggleChanged event on the entity
				var changeEvent = new ToggleChanged(t.IsOn);
				commands.Entity(entityId.Ref).EmitTrigger(changeEvent);
			}
		}
	}

	/// <summary>
	/// Updates the thumb position and track color based on toggle state.
	/// Finds thumb by looking for child entities with ToggleThumb marker.
	/// The track color is updated on the toggle entity itself.
	/// </summary>
	private static void UpdateToggleVisuals(
		Commands commands,
		Query<Data<Toggle, BackgroundColor>, Filter<Changed<Toggle>>> changedToggles,
		Query<Data<Parent, UiNode>, Filter<With<ToggleThumb>>> thumbs)
	{
		foreach (var (toggleEntityId, toggle, bgColor) in changedToggles)
		{
			ref readonly var t = ref toggle.Ref;
			var toggleId = toggleEntityId.Ref;

			// Find and update thumb position (slide to right when on, left when off)
			foreach (var (thumbEntityId, parent, thumbNode) in thumbs)
			{
				if (parent.Ref.Id != toggleId)
					continue;

				ref var thumb = ref thumbNode.Ref;

				// Position thumb
				// When off: align to left
				// When on: align to right
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
				commands.Entity(thumbEntityId.Ref).Insert(thumb);
				break;
			}

			// Update track color on the toggle entity itself
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

			commands.Entity(toggleId).Insert(color);
		}
	}
}

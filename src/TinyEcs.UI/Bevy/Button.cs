using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Headless button widget component.
/// This widget maintains a "pressed" state, which is used to indicate whether
/// the button is currently being pressed by the user. It emits an Activate
/// event when the button is clicked.
///
/// Usage:
/// <code>
/// var button = commands.Spawn()
///     .Insert(new UiNode())
///     .Insert(new Button())
///     .Insert(new Interactive())
///     .Insert(new Style { /* layout */ })
///     .Id;
/// </code>
/// </summary>
public struct Button
{
	// Empty marker component - presence indicates this is a button
}

/// <summary>
/// Component that marks a button as currently pressed.
/// Automatically added/removed by the button system.
/// </summary>
public struct Pressed
{
	// Empty marker component
}

/// <summary>
/// Component that disables interaction with a button.
/// When present, the button will not respond to clicks or keyboard input.
/// </summary>
public struct InteractionDisabled
{
	// Empty marker component
}

/// <summary>
/// Event triggered when a button is activated (clicked or Enter/Space pressed).
/// Use with On&lt;Activate&gt; in observers.
/// </summary>
public readonly struct Activate
{
	// Empty event struct - wraps in On<Activate> when triggered
}

/// <summary>
/// Plugin that adds the observers for the Button widget.
/// Add this to your app to enable button functionality.
///
/// Usage:
/// <code>
/// app.AddPlugin(new ButtonPlugin());
/// </code>
/// </summary>
public struct ButtonPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// Register all button event observers
		app.AddObserver<On<UiPointerTrigger>, Query<Data<Button>>, Query<Empty, With<Pressed>>, Query<Empty, With<InteractionDisabled>>, Commands>(ButtonOnPointerDown);
		app.AddObserver<On<UiPointerTrigger>, Query<Data<Button>>, Query<Empty, With<Pressed>>, Query<Empty, With<InteractionDisabled>>, Commands>(ButtonOnPointerUp);
	}

	/// <summary>
	/// Handles pointer down events on buttons.
	/// Sets the Pressed component when the button is pressed.
	/// </summary>
	private static void ButtonOnPointerDown(
		On<UiPointerTrigger> trigger,
		Query<Data<Button>> buttons,
		Query<Empty, With<Pressed>> pressed,
		Query<Empty, With<InteractionDisabled>> disabled,
		Commands commands)
	{
		var evt = trigger.Event.Event;
		if (evt.Type != UiPointerEventType.PointerDown)
			return;

		// Check if this entity is a button
		if (!buttons.Contains(trigger.EntityId))
			return;

		// Check state
		var isDisabled = disabled.Contains(trigger.EntityId);
		var isPressed = pressed.Contains(trigger.EntityId);

		// Only set pressed if not disabled and not already pressed
		if (!isDisabled && !isPressed)
		{
			commands.Entity(trigger.EntityId).Insert(new Pressed());
		}
	}

	/// <summary>
	/// Handles pointer up events on buttons.
	/// Removes the Pressed component and triggers Activate when the button is released.
	/// </summary>
	private static void ButtonOnPointerUp(
		On<UiPointerTrigger> trigger,
		Query<Data<Button>> buttons,
		Query<Empty, With<Pressed>> pressed,
		Query<Empty, With<InteractionDisabled>> disabled,
		Commands commands)
	{
		var evt = trigger.Event.Event;
		if (evt.Type != UiPointerEventType.PointerUp)
			return;

		// Check if this entity is a button
		if (!buttons.Contains(trigger.EntityId))
			return;

		trigger.Propagate(false);  // Prevent bubbling for button up

		// Check state
		var isDisabled = disabled.Contains(trigger.EntityId);
		var isPressed = pressed.Contains(trigger.EntityId);

		// Only activate and remove pressed if not disabled and currently pressed
		if (!isDisabled && isPressed)
		{
			// Remove pressed state
			commands.Entity(trigger.EntityId).Remove<Pressed>();

			// Emit Activate event
			commands.Entity(trigger.EntityId).EmitTrigger(new Activate());
		}
	}
}

using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Used to select the orientation of a scrollbar, slider, or other oriented control.
/// </summary>
public enum ControlOrientation
{
	/// <summary>
	/// Horizontal orientation (stretching from left to right)
	/// </summary>
	Horizontal,

	/// <summary>
	/// Vertical orientation (stretching from top to bottom)
	/// </summary>
	Vertical,
}

/// <summary>
/// A headless scrollbar widget, which can be used to build custom scrollbars.
///
/// Scrollbars operate differently than the other UI widgets in a number of respects.
///
/// Unlike sliders, scrollbars don't have keyboard focus. This is because scrollbars are usually used in
/// conjunction with a scrollable container, which is itself accessible and focusable. This also
/// means that scrollbars don't accept keyboard events, which is also the responsibility of the
/// scrollable container.
///
/// Scrollbars don't emit notification events; instead they modify the scroll position of the target
/// entity directly.
///
/// A scrollbar can have any number of child entities, but one entity must be the scrollbar thumb,
/// which is marked with the <see cref="ScrollbarThumb"/> component. Other children are ignored. The core
/// scrollbar will directly update the position and size of this entity; the application is free to
/// set any other style properties as desired.
///
/// The application is free to position the scrollbars relative to the scrolling container however
/// it wants: it can overlay them on top of the scrolling content, or use a grid layout to displace
/// the content to make room for the scrollbars.
/// </summary>
public struct Scrollbar
{
	/// <summary>
	/// Entity being scrolled.
	/// </summary>
	public ulong Target;

	/// <summary>
	/// Whether the scrollbar is vertical or horizontal.
	/// </summary>
	public ControlOrientation Orientation;

	/// <summary>
	/// Minimum length of the scrollbar thumb, in pixel units, in the direction parallel to the main
	/// scrollbar axis. The scrollbar will resize the thumb entity based on the proportion of
	/// visible size to content size, but no smaller than this. This prevents the thumb from
	/// disappearing in cases where the ratio of content size to visible size is large.
	/// </summary>
	public float MinThumbLength;

	public Scrollbar(ulong target, ControlOrientation orientation, float minThumbLength)
	{
		Target = target;
		Orientation = orientation;
		MinThumbLength = minThumbLength;
	}
}

/// <summary>
/// Marker component to indicate that the entity is a scrollbar thumb (the moving, draggable part of
/// the scrollbar). This should be a child of the scrollbar entity.
/// </summary>
public struct ScrollbarThumb
{
	// Empty marker component
}

/// <summary>
/// Component used to manage the state of a scrollbar during dragging. This component is
/// automatically added to thumb entities.
/// </summary>
public struct ScrollbarDragState
{
	/// <summary>
	/// Whether the scrollbar is currently being dragged.
	/// </summary>
	public bool Dragging;

	/// <summary>
	/// The value of the scrollbar when dragging started.
	/// </summary>
	public float DragOrigin;
}

/// <summary>
/// Plugin that adds the observers for the Scrollbar widget.
/// Add this to your app to enable scrollbar functionality.
///
/// Usage:
/// <code>
/// app.AddPlugin(new ScrollbarPlugin());
/// </code>
/// </summary>
public struct ScrollbarPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// Register all scrollbar event observers
		// Note: We use PointerDown/PointerMove/PointerUp pattern since we don't have DragStart/Drag/DragEnd events
		app.AddObserver<On<UiPointerTrigger>, Query<Data<ScrollbarThumb>>, Query<Data<Parent>>, Query<Data<Scrollbar>>, Commands>(ScrollbarOnPointerDown);
		app.AddObserver<On<UiPointerTrigger>, Query<Data<ScrollbarThumb>>, Query<Data<Parent>>, Query<Data<Scrollbar>>, Query<Data<ScrollbarDragState>>, Commands>(ScrollbarOnPointerMove);
		app.AddObserver<On<UiPointerTrigger>, Query<Data<ScrollbarThumb>>, Query<Data<ScrollbarDragState>>, Commands>(ScrollbarOnPointerUp);

		// TODO: Add system to update scrollbar thumb size and position
		// app.AddSystem(UpdateScrollbarThumb).InStage(Stage.PostUpdate).Build();
	}

	/// <summary>
	/// Handles pointer down events on scrollbar thumb - starts dragging.
	/// </summary>
	private static void ScrollbarOnPointerDown(
		On<UiPointerTrigger> trigger,
		Query<Data<ScrollbarThumb>> thumbs,
		Query<Data<Parent>> parents,
		Query<Data<Scrollbar>> scrollbars,
		Commands commands)
	{
		var evt = trigger.Event.Event;
		if (evt.Type != UiPointerEventType.PointerDown)
			return;

		// Check if clicking on a thumb
		if (!thumbs.Contains(trigger.EntityId))
			return;

		// Get the parent scrollbar
		if (!parents.Contains(trigger.EntityId))
			return;

		// TODO: Get parent entity ID and verify it's a scrollbar
		// TODO: Start drag state
		// For now, just add the drag state component
		commands.Entity(trigger.EntityId).Insert(new ScrollbarDragState
		{
			Dragging = true,
			DragOrigin = 0f // TODO: Get current scroll position
		});
	}

	/// <summary>
	/// Handles pointer move events during dragging.
	/// </summary>
	private static void ScrollbarOnPointerMove(
		On<UiPointerTrigger> trigger,
		Query<Data<ScrollbarThumb>> thumbs,
		Query<Data<Parent>> parents,
		Query<Data<Scrollbar>> scrollbars,
		Query<Data<ScrollbarDragState>> dragStates,
		Commands commands)
	{
		var evt = trigger.Event.Event;
		if (evt.Type != UiPointerEventType.PointerMove)
			return;

		// Check if this is a thumb being dragged
		if (!thumbs.Contains(trigger.EntityId))
			return;

		if (!dragStates.Contains(trigger.EntityId))
			return;

		// TODO: Calculate new scroll position based on drag delta
		// TODO: Update target scroll position
	}

	/// <summary>
	/// Handles pointer up events - ends dragging.
	/// </summary>
	private static void ScrollbarOnPointerUp(
		On<UiPointerTrigger> trigger,
		Query<Data<ScrollbarThumb>> thumbs,
		Query<Data<ScrollbarDragState>> dragStates,
		Commands commands)
	{
		var evt = trigger.Event.Event;
		if (evt.Type != UiPointerEventType.PointerUp)
			return;

		// Check if this is a thumb
		if (!thumbs.Contains(trigger.EntityId))
			return;

		if (!dragStates.Contains(trigger.EntityId))
			return;

		// Remove drag state to end dragging
		commands.Entity(trigger.EntityId).Remove<ScrollbarDragState>();
	}
}

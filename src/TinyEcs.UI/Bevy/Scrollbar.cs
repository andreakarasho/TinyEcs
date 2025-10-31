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
	/// Previous pointer position (used to calculate deltas between frames).
	/// </summary>
	public float DragOriginOffset;
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
		app.AddObserver<On<UiPointerTrigger>, Query<Data<Parent>, Filter<With<ScrollbarThumb>>>, Query<Data<Scrollbar>>, Commands>(ScrollbarOnPointerDown);
		app.AddObserver<On<UiPointerTrigger>, Query<Data<ScrollbarDragState, Parent, ComputedLayout>, Filter<With<ScrollbarThumb>>>, Query<Data<Scrollbar>>, Query<Data<Scrollable, ComputedLayout>>>(ScrollbarOnPointerMove);
		app.AddObserver<On<UiPointerTrigger>, Query<Data<ScrollbarDragState>>, Commands>(ScrollbarOnPointerUp);

		// System to update scrollbar thumb size and position based on scroll state
        app.AddSystem((
            Commands commands,
            Query<Data<Scrollbar, Children>> scrollbars,
            Query<Data<UiNode, ComputedLayout>, Filter<With<ScrollbarThumb>>> thumbs,
            Query<Data<Scrollable, ComputedLayout>> scrollables,
            Query<Data<ComputedLayout>> layouts) =>
        {
            UpdateScrollbarThumbs(commands, scrollbars, thumbs, scrollables, layouts);
        })
            .InStage(Stage.PostUpdate)
            .Label("ui:scrollbar:update-thumbs")
            .Build();
	}

	/// <summary>
	/// Handles pointer down events on scrollbar thumb - starts dragging.
	/// Stores the initial pointer position for delta calculations.
	/// </summary>
	private static void ScrollbarOnPointerDown(
		On<UiPointerTrigger> trigger,
		Query<Data<Parent>, Filter<With<ScrollbarThumb>>> thumbs,
		Query<Data<Scrollbar>> scrollbars,
		Commands commands)
	{
		var evt = trigger.Event.Event;
		if (evt.Type != UiPointerEventType.PointerDown)
			return;

		// Check if this is a thumb entity
		if (!thumbs.Contains(trigger.EntityId))
			return;

		var (_, parent) = thumbs.Get(trigger.EntityId);
		var scrollbarId = parent.Ref.Id;

		// Verify parent is a scrollbar
		if (!scrollbars.Contains(scrollbarId))
			return;

		var (_, scrollbar) = scrollbars.Get(scrollbarId);

		// Store initial pointer position for delta calculation
		float initialPointerPos;
		if (scrollbar.Ref.Orientation == ControlOrientation.Vertical)
		{
			initialPointerPos = evt.Position.Y;
		}
		else
		{
			initialPointerPos = evt.Position.X;
		}

		// Start dragging
		commands.Entity(trigger.EntityId).Insert(new ScrollbarDragState
		{
			Dragging = true,
			DragOriginOffset = initialPointerPos
		});
	}

	/// <summary>
	/// Handles pointer move events during dragging.
	/// Uses delta-based scrolling (inspired by sickle_ui) for smooth, predictable behavior.
	/// </summary>
	private static void ScrollbarOnPointerMove(
		On<UiPointerTrigger> trigger,
		Query<Data<ScrollbarDragState, Parent, ComputedLayout>, Filter<With<ScrollbarThumb>>> thumbs,
		Query<Data<Scrollbar>> scrollbars,
		Query<Data<Scrollable, ComputedLayout>> scrollables)
	{
		var evt = trigger.Event.Event;
		if (evt.Type != UiPointerEventType.PointerMove)
			return;

		// Check if this is a thumb being dragged (single combined query)
		if (!thumbs.Contains(trigger.EntityId))
			return;

		var (_, dragState, parent, thumbLayout) = thumbs.Get(trigger.EntityId);

		// Only process if actually dragging
		if (!dragState.Ref.Dragging)
			return;

		var scrollbarId = parent.Ref.Id;

		// Get scrollbar entity
		if (!scrollbars.Contains(scrollbarId))
			return;

		var (_, scrollbar) = scrollbars.Get(scrollbarId);
		var targetId = scrollbar.Ref.Target;

		// Get target scrollable with layout (combined query)
		if (!scrollables.Contains(targetId))
			return;

		var (_, scrollable, targetLayout) = scrollables.Get(targetId);

		// Delta-based scrolling: accumulate drag deltas scaled by content-to-thumb ratio
		ref var scroll = ref scrollable.Ref;
		ref var containerLayout = ref targetLayout.Ref;
		ref var thumbL = ref thumbLayout.Ref;

		if (scrollbar.Ref.Orientation == ControlOrientation.Vertical)
		{
			// Vertical scrollbar
			var contentHeight = scroll.ContentSize.Y;
			var containerHeight = containerLayout.Height;
			var overflow = contentHeight - containerHeight;

			if (overflow <= 0f)
				return;

			var thumbHeight = thumbL.Height;
			var remainingSpace = containerHeight - thumbHeight;

			if (remainingSpace <= 0f)
				return;

			// Calculate ratio: how much content scrolls per pixel of thumb movement
			var ratio = overflow / remainingSpace;

			// Get pointer delta from previous position
			var pointerDelta = evt.Position.Y - dragState.Ref.DragOriginOffset;
			dragState.Ref.DragOriginOffset = evt.Position.Y;

			// Apply scaled delta to scroll offset
			var scrollDelta = pointerDelta * ratio;
			scroll.ScrollOffset.Y = Math.Clamp(scroll.ScrollOffset.Y + scrollDelta, 0f, overflow);
		}
		else
		{
			// Horizontal scrollbar
			var contentWidth = scroll.ContentSize.X;
			var containerWidth = containerLayout.Width;
			var overflow = contentWidth - containerWidth;

			if (overflow <= 0f)
				return;

			var thumbWidth = thumbL.Width;
			var remainingSpace = containerWidth - thumbWidth;

			if (remainingSpace <= 0f)
				return;

			// Calculate ratio: how much content scrolls per pixel of thumb movement
			var ratio = overflow / remainingSpace;

			// Get pointer delta from previous position
			var pointerDelta = evt.Position.X - dragState.Ref.DragOriginOffset;
			dragState.Ref.DragOriginOffset = evt.Position.X;

			// Apply scaled delta to scroll offset
			var scrollDelta = pointerDelta * ratio;
			scroll.ScrollOffset.X = Math.Clamp(scroll.ScrollOffset.X + scrollDelta, 0f, overflow);
		}
	}

	/// <summary>
	/// Handles pointer up events - ends dragging.
	/// </summary>
	private static void ScrollbarOnPointerUp(
		On<UiPointerTrigger> trigger,
		Query<Data<ScrollbarDragState>> dragStates,
		Commands commands)
	{
		var evt = trigger.Event.Event;
		if (evt.Type != UiPointerEventType.PointerUp)
			return;

		if (!dragStates.Contains(trigger.EntityId))
			return;

		// Remove drag state to end dragging
		commands.Entity(trigger.EntityId).Remove<ScrollbarDragState>();
	}

	/// <summary>
	/// Updates scrollbar thumb positions and sizes based on scroll state.
	/// </summary>
    private static void UpdateScrollbarThumbs(
        Commands commands,
        Query<Data<Scrollbar, Children>> scrollbars,
        Query<Data<UiNode, ComputedLayout>, Filter<With<ScrollbarThumb>>> thumbs,
        Query<Data<Scrollable, ComputedLayout>> scrollables,
        Query<Data<ComputedLayout>> layouts)
    {
        // Iterate through all scrollbars
        foreach (var (scrollbarId, scrollbar, children) in scrollbars)
        {
            var targetId = scrollbar.Ref.Target;

            // Get target scrollable container
            if (!scrollables.Contains(targetId))
                continue;

            var (_, scrollable, scrollLayout) = scrollables.Get(targetId);
            ref var scroll = ref scrollable.Ref;
            ref var containerLayout = ref scrollLayout.Ref;

            // Find thumb child entity
            ulong thumbId = 0;
            foreach (var childId in children.Ref)
            {
                if (thumbs.Contains(childId))
                {
                    thumbId = childId;
                    break;
                }
            }

            if (thumbId == 0)
                continue;

            var (_, thumbNode, thumbLayout) = thumbs.Get(thumbId);

            ref var node = ref thumbNode.Ref;

            // Cross-axis size should match the scrollbar rail size, not the scrollable container
            float railWidth = 0f, railHeight = 0f;
            if (layouts.Contains(scrollbarId.Ref))
            {
                var (_, barLayout) = layouts.Get(scrollbarId.Ref);
                railWidth = barLayout.Ref.Width;
                railHeight = barLayout.Ref.Height;
            }

            if (scrollbar.Ref.Orientation == ControlOrientation.Vertical)
            {
                // Vertical scrollbar
                var contentHeight = scroll.ContentSize.Y;
                var containerHeight = containerLayout.Height;
                var containerWidth = containerLayout.Width;

                

                if (contentHeight > containerHeight)
                {
                    // Calculate thumb size (proportional to visible area)
                    var thumbHeight = Math.Max(scrollbar.Ref.MinThumbLength,
                        (containerHeight / contentHeight) * containerHeight);

                    // Calculate thumb position based on scroll offset
                    var maxScroll = contentHeight - containerHeight;
                    var scrollRatio = maxScroll > 0 ? scroll.ScrollOffset.Y / maxScroll : 0f;
                    var maxThumbY = containerHeight - thumbHeight;
                    var thumbY = scrollRatio * maxThumbY;

                    // Update thumb node size and position
                    node.Height = FlexValue.Points(thumbHeight);
                    node.Width = FlexValue.Points(railWidth > 0f ? railWidth : node.Width.Value);
                    node.Left = FlexValue.Points(0f);
                    node.Top = FlexValue.Points(thumbY);

                    commands.Entity(thumbId).Insert(node);
                    
                }
            }
            else
            {
                // Horizontal scrollbar
                var contentWidth = scroll.ContentSize.X;
                var containerWidth = containerLayout.Width;
                var containerHeight = containerLayout.Height;

                if (contentWidth > containerWidth)
                {
                    // Calculate thumb size (proportional to visible area)
                    var thumbWidth = Math.Max(scrollbar.Ref.MinThumbLength,
                        (containerWidth / contentWidth) * containerWidth);

                    // Calculate thumb position based on scroll offset
                    var maxScroll = contentWidth - containerWidth;
                    var scrollRatio = maxScroll > 0 ? scroll.ScrollOffset.X / maxScroll : 0f;
                    var maxThumbX = containerWidth - thumbWidth;
                    var thumbX = scrollRatio * maxThumbX;

                    // Update thumb node size and position
                    node.Width = FlexValue.Points(thumbWidth);
                    node.Height = FlexValue.Points(railHeight > 0f ? railHeight : node.Height.Value);
                    node.Top = FlexValue.Points(0f);
                    node.Left = FlexValue.Points(thumbX);

                    commands.Entity(thumbId).Insert(node);
                }
            }
        }
    }
}

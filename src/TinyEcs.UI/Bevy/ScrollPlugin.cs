using System.Numerics;
using Flexbox;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Resource that tracks mouse wheel scroll delta for the current frame.
/// Updated by platform-specific input adapters (e.g., Raylib).
/// </summary>
public class ScrollInputState
{
	/// <summary>Scroll delta for this frame (positive = scroll up/left, negative = scroll down/right)</summary>
	public Vector2 ScrollDelta;

	public void Clear()
	{
		ScrollDelta = Vector2.Zero;
	}
}

// One-shot guard used to clear any spurious initial horizontal offsets on the first frame.
public class ScrollResetGuard
{
	public bool Done;
}

/// <summary>
/// Plugin that handles scrolling for UI containers with the Scrollable component.
/// Processes mouse wheel input and updates scroll offsets.
/// </summary>
public struct ScrollPlugin : IPlugin
{
	public readonly void Build(App app)
	{
		// Register scroll input state
		app.AddResource(new ScrollInputState());
		app.AddResource(new ScrollResetGuard());

		// System to auto-set Overflow.Scroll when Scrollable component is added
		app.AddSystem((
			Commands commands,
			Query<Data<UiNode, Scrollable>, Filter<Added<Scrollable>>> newScrollables) =>
		{
			foreach (var (entityId, node, scrollable) in newScrollables)
			{
				ref var n = ref node.Ref;

				// Only update if not already set to Scroll
				if (n.Overflow != Overflow.Scroll)
				{
					n.Overflow = Overflow.Scroll;
					// Re-insert to trigger layout recalculation
					commands.Entity(entityId.Ref).Insert(n);
				}
			}
		})
		.InStage(Stage.PostUpdate)
		.Label("ui:scroll:auto-set-overflow")
		.After("flexbox:sync_node")
		.Build();

		// Note: Content size calculation is now done in FlexboxUiPlugin.CalculateScrollableContentSize
		// which runs before scroll transforms are applied, avoiding circular dependency.

		// System to handle scrolling (runs in PostUpdate after pointer input)
		app.AddSystem((
			Res<ScrollInputState> scrollInput,
			Res<PointerInputState> pointerInput,
			Res<UiStack> uiStack,
			Query<Data<Scrollable, ComputedLayout>> scrollables,
			Query<Data<Parent>> parents,
			Commands commands) =>
		{
			ProcessScrollInput(scrollInput, pointerInput, uiStack, scrollables, parents, commands);
		})
		.InStage(Stage.PostUpdate)
		.Label("ui:scroll:process-input")
		.After("flexbox:read_layout")
		.Build();

		// Clear scroll event tracking after all systems have processed scroll events
		app.AddSystem((Query<Data<Scrollable>> scrollables) =>
		{
			foreach (var (_, scrollable) in scrollables)
			{
				ref var scroll = ref scrollable.Ref;
				// Reset event tracking for next frame
				scroll.LastScrollAxis = null;
				scroll.LastScrollDelta = 0f;
			}
		})
		.InStage(Stage.Last)
		.Label("ui:scroll:reset-events")
		.Build();

		// Clear scroll input at end of frame
		app.AddSystem((ResMut<ScrollInputState> scrollInput) =>
		{
			scrollInput.Value.Clear();
		})
		.InStage(Stage.Last)
		.Label("ui:scroll:clear-input")
		.After("ui:scroll:reset-events")
		.Build();
	}

	/// <summary>
	/// Processes scroll input and updates scrollable containers.
	/// Uses the UI stack to find the topmost scrollable container under the mouse pointer.
	/// Enhanced with sickle_ui features: event tracking and scroll-through support.
	/// </summary>
	private static void ProcessScrollInput(
		Res<ScrollInputState> scrollInput,
		Res<PointerInputState> pointerInput,
		Res<UiStack> uiStack,
		Query<Data<Scrollable, ComputedLayout>> scrollables,
		Query<Data<Parent>> parents,
		Commands commands)
	{
		// Skip if no scroll this frame
		if (scrollInput.Value.ScrollDelta == Vector2.Zero)
			return;

		var mousePos = pointerInput.Value.Position;
		var scrollDelta = scrollInput.Value.ScrollDelta;



		// Iterate UI stack in reverse order (topmost first) to find scrollable under mouse
		for (int i = uiStack.Value.Count - 1; i >= 0; i--)
		{
			var entry = uiStack.Value.Entries[i];
			var entityId = entry.EntityId;

			// Check if this entity is scrollable
			if (!scrollables.Contains(entityId))
				continue;

			var (_, scrollable, layout) = scrollables.Get(entityId);
			ref var scroll = ref scrollable.Ref;
			ref var l = ref layout.Ref;

			// Check if mouse is over this scrollable container
			if (mousePos.X >= l.X && mousePos.X <= l.X + l.Width &&
				mousePos.Y >= l.Y && mousePos.Y <= l.Y + l.Height)
			{
				// Check if the mouse position is visible (not clipped by parent scrollables)
				if (!IsPointVisibleForScroll(mousePos, entityId, parents, scrollables))
					continue;

				// Determine scroll axis and delta
				var axis = ScrollAxis.Vertical;
				var delta = -scrollDelta.Y;

				// If only horizontal scrolling is enabled, translate vertical wheel to horizontal scroll
				if (scroll.EnableHorizontal && !scroll.EnableVertical && scrollDelta.Y != 0)
				{
					axis = ScrollAxis.Horizontal;
					delta = -scrollDelta.Y;
				}
				else if (scrollDelta.X != 0)
				{
					axis = ScrollAxis.Horizontal;
					delta = -scrollDelta.X;
				}

				// Update scroll event tracking (sickle_ui feature)
				scroll.LastScrollAxis = axis;
				scroll.LastScrollDelta = delta;
				scroll.LastScrollUnit = ScrollUnit.Line; // Default to Line for wheel events

				// Apply scroll delta
				bool scrollChanged = false;
				if (axis == ScrollAxis.Horizontal && scroll.EnableHorizontal)
				{
					scroll.ScrollOffset.X += delta * scroll.ScrollSpeed;
					var maxScroll = Math.Max(0f, scroll.ContentSize.X - l.Width);
					scroll.ScrollOffset.X = Math.Clamp(scroll.ScrollOffset.X, 0f, maxScroll);
					scrollChanged = true;
				}
				else if (axis == ScrollAxis.Vertical && scroll.EnableVertical)
				{
					scroll.ScrollOffset.Y += delta * scroll.ScrollSpeed;
					var maxScroll = Math.Max(0f, scroll.ContentSize.Y - l.Height);
					scroll.ScrollOffset.Y = Math.Clamp(scroll.ScrollOffset.Y, 0f, maxScroll);
					scrollChanged = true;
				}

				// Re-insert to trigger change detection for Changed<Scrollable> filters
				if (scrollChanged)
				{
					commands.Entity(entityId).Insert(scroll);
				}

				// Only apply to the topmost scrollable (don't propagate)
				// TODO: Implement ScrollThrough marker for scroll event bubbling
				break;
			}
		}
	}

	/// <summary>
	/// Checks if a point is visible (not clipped) by walking up the hierarchy and checking scrollable parent bounds.
	/// Similar to the hit testing version but specifically for scroll input.
	/// </summary>
	private static bool IsPointVisibleForScroll(
		Vector2 point,
		ulong entityId,
		Query<Data<Parent>> parents,
		Query<Data<Scrollable, ComputedLayout>> scrollables)
	{
		// Walk up the hierarchy
		var currentEntity = entityId;

		while (parents.Contains(currentEntity))
		{
			var (_, parentComponent) = parents.Get(currentEntity);
			var parentId = parentComponent.Ref.Id;

			// Check if parent is scrollable (has clipping)
			if (scrollables.Contains(parentId))
			{
				var (_, _, parentLayout) = scrollables.Get(parentId);

				// Check if point is within parent's clip bounds
				if (point.X < parentLayout.Ref.X || point.X > parentLayout.Ref.X + parentLayout.Ref.Width ||
					point.Y < parentLayout.Ref.Y || point.Y > parentLayout.Ref.Y + parentLayout.Ref.Height)
				{
					return false; // Point is clipped by this scrollable parent
				}
			}

			// Move up to next parent
			currentEntity = parentId;
		}

		return true; // Point is visible (not clipped by any scrollable parent)
	}
}

using System.Numerics;
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

/// <summary>
/// Plugin that handles scrolling for UI containers with the Scrollable component.
/// Processes mouse wheel input and updates scroll offsets.
/// </summary>
public struct ScrollPlugin : IPlugin
{
	public void Build(App app)
	{
		// Register scroll input state
		app.AddResource(new ScrollInputState());

		// System to calculate content size for scrollable containers
		app.AddSystem((
			Query<Data<Scrollable, ComputedLayout>> scrollables,
			Query<Data<Parent>> parents,
			Query<Data<ComputedLayout>> allLayouts) =>
		{
			UpdateScrollableContentSize(scrollables, parents, allLayouts);
		})
		.InStage(Stage.PostUpdate)
		.Label("ui:scroll:update-content-size")
		.After("flexbox:read_layout")
		.Build();

		// System to handle scrolling (runs in PostUpdate after pointer input)
		app.AddSystem((
			Res<ScrollInputState> scrollInput,
			Res<PointerInputState> pointerInput,
			Res<UiStack> uiStack,
			Query<Data<Scrollable, ComputedLayout>> scrollables,
			Query<Data<Parent>> parents) =>
		{
			ProcessScrollInput(scrollInput, pointerInput, uiStack, scrollables, parents);
		})
		.InStage(Stage.PostUpdate)
		.Label("ui:scroll:process-input")
		.After("ui:scroll:update-content-size")
		.Build();

		// Clear scroll input at end of frame
		app.AddSystem((ResMut<ScrollInputState> scrollInput) =>
		{
			scrollInput.Value.Clear();
		})
		.InStage(Stage.Last)
		.Label("ui:scroll:clear-input")
		.Build();
	}

	/// <summary>
	/// Updates the content size of scrollable containers by measuring their children.
	/// </summary>
	private static void UpdateScrollableContentSize(
		Query<Data<Scrollable, ComputedLayout>> scrollables,
		Query<Data<Parent>> parents,
		Query<Data<ComputedLayout>> allLayouts)
	{
		foreach (var (scrollableId, scrollable, scrollLayout) in scrollables)
		{
			ref var scroll = ref scrollable.Ref;
			ref var layout = ref scrollLayout.Ref;

			// Calculate bounding box of all children
			float minX = float.MaxValue, minY = float.MaxValue;
			float maxX = float.MinValue, maxY = float.MinValue;
			bool hasChildren = false;

			// Find all children of this scrollable container
			foreach (var (childId, childLayout) in allLayouts)
			{
				// Check if this entity is a child of the scrollable
				if (IsChildOf(childId.Ref, scrollableId.Ref, parents))
				{
					ref var child = ref childLayout.Ref;
					hasChildren = true;

					// Expand bounding box to include this child
					minX = Math.Min(minX, child.X);
					minY = Math.Min(minY, child.Y);
					maxX = Math.Max(maxX, child.X + child.Width);
					maxY = Math.Max(maxY, child.Y + child.Height);
				}
			}

			if (hasChildren)
			{
				// Content size is the bounding box size
				scroll.ContentSize = new Vector2(maxX - minX, maxY - minY);
			}
			else
			{
				// No children, content size is 0
				scroll.ContentSize = Vector2.Zero;
			}
		}
	}

	/// <summary>
	/// Checks if childId is a direct or indirect child of parentId.
	/// </summary>
	private static bool IsChildOf(ulong childId, ulong parentId, Query<Data<Parent>> parents)
	{
		var currentId = childId;
		while (parents.Contains(currentId))
		{
			var (_, parent) = parents.Get(currentId);
			if (parent.Ref.Id == parentId)
				return true;

			currentId = parent.Ref.Id;

			// Prevent infinite loops
			if (currentId == childId)
				break;
		}
		return false;
	}

	/// <summary>
	/// Processes scroll input and updates scrollable containers.
	/// Uses the UI stack to find the topmost scrollable container under the mouse pointer.
	/// </summary>
	private static void ProcessScrollInput(
		Res<ScrollInputState> scrollInput,
		Res<PointerInputState> pointerInput,
		Res<UiStack> uiStack,
		Query<Data<Scrollable, ComputedLayout>> scrollables,
		Query<Data<Parent>> parents)
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

				// Found the topmost scrollable under mouse - apply scroll delta
				// If only horizontal scrolling is enabled, translate vertical wheel to horizontal scroll
				if (scroll.EnableHorizontal && !scroll.EnableVertical && scrollDelta.Y != 0)
				{
					// Mouse wheel scrolling horizontally (inverted for natural direction)
					scroll.ScrollOffset.X -= scrollDelta.Y * scroll.ScrollSpeed;
					var maxScroll = Math.Max(0f, scroll.ContentSize.X - l.Width);
					scroll.ScrollOffset.X = Math.Clamp(scroll.ScrollOffset.X, 0f, maxScroll);
				}
				else
				{
					// Normal vertical/horizontal scrolling
					if (scroll.EnableVertical)
					{
						scroll.ScrollOffset.Y -= scrollDelta.Y * scroll.ScrollSpeed;
						var maxScroll = Math.Max(0f, scroll.ContentSize.Y - l.Height);
						scroll.ScrollOffset.Y = Math.Clamp(scroll.ScrollOffset.Y, 0f, maxScroll);
					}

					if (scroll.EnableHorizontal)
					{
						scroll.ScrollOffset.X += scrollDelta.X * scroll.ScrollSpeed;
						var maxScroll = Math.Max(0f, scroll.ContentSize.X - l.Width);
						scroll.ScrollOffset.X = Math.Clamp(scroll.ScrollOffset.X, 0f, maxScroll);
					}
				}

				// Only apply to the topmost scrollable (don't propagate)
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

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
			Query<Data<Scrollable, ComputedLayout>> scrollables) =>
		{
			ProcessScrollInput(scrollInput, pointerInput, scrollables);
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
	/// Finds the scrollable container under the mouse pointer and applies scroll offset.
	/// </summary>
	private static void ProcessScrollInput(
		Res<ScrollInputState> scrollInput,
		Res<PointerInputState> pointerInput,
		Query<Data<Scrollable, ComputedLayout>> scrollables)
	{
		// Skip if no scroll this frame
		if (scrollInput.Value.ScrollDelta == Vector2.Zero)
			return;

		var mousePos = pointerInput.Value.Position;
		var scrollDelta = scrollInput.Value.ScrollDelta;

		// Find scrollable container under mouse
		foreach (var (entityId, scrollable, layout) in scrollables)
		{
			ref var scroll = ref scrollable.Ref;
			ref var l = ref layout.Ref;

			// Check if mouse is over this scrollable container
			if (mousePos.X >= l.X && mousePos.X <= l.X + l.Width &&
				mousePos.Y >= l.Y && mousePos.Y <= l.Y + l.Height)
			{
				// Apply scroll delta
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
}

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

		// System to calculate content size for scrollable containers
		app.AddSystem((
			Query<Data<UiNode, Scrollable, ComputedLayout, Children>, Optional<Children>> scrollables,
			Query<Data<UiNode, ComputedLayout>> allLayouts,
			Query<Data<Children>> childrenLookup) =>
		{
			UpdateScrollableContentSize(scrollables, allLayouts, childrenLookup);
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
	/// Updates the content size of scrollable containers by measuring their children.
	/// </summary>
	private static void UpdateScrollableContentSize(
		Query<Data<UiNode, Scrollable, ComputedLayout, Children>, Optional<Children>> scrollables,
		Query<Data<UiNode, ComputedLayout>> allLayouts,
		Query<Data<Children>> childrenLookup)
	{
		foreach (var (containerNode, scrollable, scrollLayout, maybeChildren) in scrollables)
		{
			ref var node = ref containerNode.Ref;
			ref var scroll = ref scrollable.Ref;
			ref var layout = ref scrollLayout.Ref;

			// Get container padding to ensure all padding is included in content size
			float paddingLeft = 0f;
			float paddingRight = 0f;
			float paddingTop = 0f;
			float paddingBottom = 0f;

			if (node.PaddingLeft.IsDefined)
				paddingLeft = node.PaddingLeft.Value;
			if (node.PaddingRight.IsDefined)
				paddingRight = node.PaddingRight.Value;
			if (node.PaddingTop.IsDefined)
				paddingTop = node.PaddingTop.Value;
			if (node.PaddingBottom.IsDefined)
				paddingBottom = node.PaddingBottom.Value;

			// Get container borders (reduce available content area)
			float borderLeft = 0f;
			float borderRight = 0f;
			float borderTop = 0f;
			float borderBottom = 0f;

			if (node.BorderLeft.IsDefined)
				borderLeft = node.BorderLeft.Value;
			if (node.BorderRight.IsDefined)
				borderRight = node.BorderRight.Value;
			if (node.BorderTop.IsDefined)
				borderTop = node.BorderTop.Value;
			if (node.BorderBottom.IsDefined)
				borderBottom = node.BorderBottom.Value;

			// Calculate bounding box of all children
			float minX = float.MaxValue, minY = float.MaxValue;
			float maxX = float.MinValue, maxY = float.MinValue;
			bool hasChildren = false;

			// Track actual padding and borders to use (from content container if measuring grandchildren)
			float actualPaddingLeft = 0f;
			float actualPaddingRight = 0f;
			float actualPaddingTop = 0f;
			float actualPaddingBottom = 0f;
			float actualBorderLeft = 0f;
			float actualBorderRight = 0f;
			float actualBorderTop = 0f;
			float actualBorderBottom = 0f;
			bool usedContentContainerValues = false;

			if (maybeChildren.IsValid())
			{
				foreach (var childId in maybeChildren.Ref)
				{
					if (!allLayouts.Contains(childId))
						continue;

					// If the child has its own children (e.g., content container), measure grandchildren instead
					if (childrenLookup.Contains(childId))
					{
						// Read padding and borders from the content container (not the viewport)
						var (_, contentContainerNode, contentContainerLayout) = allLayouts.Get(childId);
						if (contentContainerNode.Ref.PaddingLeft.IsDefined) actualPaddingLeft = contentContainerNode.Ref.PaddingLeft.Value;
						if (contentContainerNode.Ref.PaddingRight.IsDefined) actualPaddingRight = contentContainerNode.Ref.PaddingRight.Value;
						if (contentContainerNode.Ref.PaddingTop.IsDefined) actualPaddingTop = contentContainerNode.Ref.PaddingTop.Value;
						if (contentContainerNode.Ref.PaddingBottom.IsDefined) actualPaddingBottom = contentContainerNode.Ref.PaddingBottom.Value;
						if (contentContainerNode.Ref.BorderLeft.IsDefined) actualBorderLeft = contentContainerNode.Ref.BorderLeft.Value;
						if (contentContainerNode.Ref.BorderRight.IsDefined) actualBorderRight = contentContainerNode.Ref.BorderRight.Value;
						if (contentContainerNode.Ref.BorderTop.IsDefined) actualBorderTop = contentContainerNode.Ref.BorderTop.Value;
						if (contentContainerNode.Ref.BorderBottom.IsDefined) actualBorderBottom = contentContainerNode.Ref.BorderBottom.Value;
						usedContentContainerValues = true;

						var (_, grandChildren) = childrenLookup.Get(childId);
						foreach (var gcId in grandChildren.Ref)
						{
							if (!allLayouts.Contains(gcId))
								continue;
							var (_, gcNode, gcLayout) = allLayouts.Get(gcId);
							hasChildren = true;

							float gml = 0f, gmr = 0f, gmt = 0f, gmb = 0f;
							if (gcNode.Ref.MarginLeft.IsDefined) gml = gcNode.Ref.MarginLeft.Value;
							if (gcNode.Ref.MarginRight.IsDefined) gmr = gcNode.Ref.MarginRight.Value;
							if (gcNode.Ref.MarginTop.IsDefined) gmt = gcNode.Ref.MarginTop.Value;
							if (gcNode.Ref.MarginBottom.IsDefined) gmb = gcNode.Ref.MarginBottom.Value;

							// Calculate position relative to container WITHOUT scroll offset
							// (ContentSize should measure the natural/unscrolled content bounds)
							// var gcRelX = gcLayout.Ref.X - layout.X;
							// var gcRelY = gcLayout.Ref.Y - layout.Y;
							var gcRelX = (gcLayout.Ref.X - layout.X) + scroll.ScrollOffset.X;
							var gcRelY = (gcLayout.Ref.Y - layout.Y) + scroll.ScrollOffset.Y;
							minX = Math.Min(minX, gcRelX - gml);
							minY = Math.Min(minY, gcRelY - gmt);
							maxX = Math.Max(maxX, gcRelX + gcLayout.Ref.Width + gmr);
							maxY = Math.Max(maxY, gcRelY + gcLayout.Ref.Height + gmb);
						}
					}
					else
					{
						var (_, childNode, childLayout) = allLayouts.Get(childId);
						hasChildren = true;

						float marginLeft = 0f, marginRight = 0f, marginTop = 0f, marginBottom = 0f;
						if (childNode.Ref.MarginLeft.IsDefined) marginLeft = childNode.Ref.MarginLeft.Value;
						if (childNode.Ref.MarginRight.IsDefined) marginRight = childNode.Ref.MarginRight.Value;
						if (childNode.Ref.MarginTop.IsDefined) marginTop = childNode.Ref.MarginTop.Value;
						if (childNode.Ref.MarginBottom.IsDefined) marginBottom = childNode.Ref.MarginBottom.Value;

						// Calculate position relative to container WITHOUT scroll offset
						// var relX = childLayout.Ref.X - layout.X;
						// var relY = childLayout.Ref.Y - layout.Y;
						var relX = (childLayout.Ref.X - layout.X) + scroll.ScrollOffset.X;
						var relY = (childLayout.Ref.Y - layout.Y) + scroll.ScrollOffset.Y;
						minX = Math.Min(minX, relX - marginLeft);
						minY = Math.Min(minY, relY - marginTop);
						maxX = Math.Max(maxX, relX + childLayout.Ref.Width + marginRight);
						maxY = Math.Max(maxY, relY + childLayout.Ref.Height + marginBottom);
					}
				}
			}

			if (hasChildren)
			{
				// Content size calculation:
				// - minX/minY = position where first child's margins start (after paddingLeft/Top)
				// - maxX/maxY = position where last child's margins end (before paddingRight/Bottom)
				// - (maxX - minX) = span from first margin to last margin
				// - Add paddingLeft/Top + paddingRight/Bottom to include full padding area
				// - Add borderLeft/Top + borderRight/Bottom since borders reduce the available content area
				// This ensures the content size represents the full scrollable area with symmetric padding and borders

				// Use content container values if we measured grandchildren, otherwise use viewport values
				float finalPaddingLeft = usedContentContainerValues ? actualPaddingLeft : paddingLeft;
				float finalPaddingRight = usedContentContainerValues ? actualPaddingRight : paddingRight;
				float finalPaddingTop = usedContentContainerValues ? actualPaddingTop : paddingTop;
				float finalPaddingBottom = usedContentContainerValues ? actualPaddingBottom : paddingBottom;
				float finalBorderLeft = usedContentContainerValues ? actualBorderLeft : borderLeft;
				float finalBorderRight = usedContentContainerValues ? actualBorderRight : borderRight;
				float finalBorderTop = usedContentContainerValues ? actualBorderTop : borderTop;
				float finalBorderBottom = usedContentContainerValues ? actualBorderBottom : borderBottom;

				scroll.ContentSize = new Vector2(
					Math.Max(0f, maxX - minX + finalPaddingLeft + finalPaddingRight + finalBorderLeft + finalBorderRight),
					Math.Max(0f, maxY - minY + finalPaddingTop + finalPaddingBottom + finalBorderTop + finalBorderBottom)
				);
				// Content origin relative to container top-left
				if (scroll.ContentOrigin == Vector2.Zero)
					scroll.ContentOrigin = new Vector2(minX, minY);

			}
			else
			{
				// No children, content size is 0
				scroll.ContentSize = Vector2.Zero;
				scroll.ContentOrigin = Vector2.Zero;
			}
		}
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
				if (axis == ScrollAxis.Horizontal && scroll.EnableHorizontal)
				{
					scroll.ScrollOffset.X += delta * scroll.ScrollSpeed;
					var maxScroll = Math.Max(0f, scroll.ContentSize.X - l.Width);
					scroll.ScrollOffset.X = Math.Clamp(scroll.ScrollOffset.X, 0f, maxScroll);
				}
				else if (axis == ScrollAxis.Vertical && scroll.EnableVertical)
				{
					scroll.ScrollOffset.Y += delta * scroll.ScrollSpeed;
					var maxScroll = Math.Max(0f, scroll.ContentSize.Y - l.Height);
					scroll.ScrollOffset.Y = Math.Clamp(scroll.ScrollOffset.Y, 0f, maxScroll);

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

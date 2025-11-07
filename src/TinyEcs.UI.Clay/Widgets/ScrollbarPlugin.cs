using TinyEcs.Bevy;
using Clay_cs;

namespace TinyEcs.UI.Clay.Widgets;

/// <summary>
/// Plugin that adds scrollbar widget systems to the application.
/// </summary>
public struct ScrollbarPlugin : IPlugin
{
	public void Build(App app)
	{
		// System to update scrollbar visibility
		app.AddSystem((Commands commands, Query<Data<ScrollbarVisibilityUpdate, ClayNode>> updates, Query<Data<ScrollbarState>> scrollbars) =>
		{
			foreach (var (entityId, updatePtr, nodePtr) in updates)
			{
				var update = updatePtr.Ref;
				ref var node = ref nodePtr.Ref;

				// Find the parent scrollbar to get orientation
				bool isHorizontal = false;
				foreach (var (scrollbarId, statePtr) in scrollbars)
				{
					var state = statePtr.Ref;
					if (state.ContainerEntityId == entityId.Ref)
					{
						isHorizontal = state.IsHorizontal;
						break;
					}
				}

				// Update container size to show/hide scrollbar
				var layout = node.Layout;
				float scrollbarSize = update.IsVisible ? 12f : 0f;

				if (isHorizontal)
				{
					layout.sizing = new Clay_Sizing(
						Clay_SizingAxis.Grow(),
						Clay_SizingAxis.Fixed(scrollbarSize)
					);
				}
				else
				{
					layout.sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(scrollbarSize),
						Clay_SizingAxis.Grow()
					);
				}

				node.Layout = layout;
				commands.Entity(entityId.Ref).Insert(node);
			}
		})
		.InStage(Stage.First)
		.Label("scrollbar:update-visibility")
		.Build();

		// System to update scrollbar spacer (thumb position)
		app.AddSystem((Commands commands, Query<Data<ScrollbarSpacerUpdate, ClayNode>> updates, Query<Data<ScrollbarState>> scrollbars) =>
		{
			foreach (var (entityId, updatePtr, nodePtr) in updates)
			{
				var update = updatePtr.Ref;
				ref var node = ref nodePtr.Ref;

				// Find the parent scrollbar to get orientation
				bool isHorizontal = false;
				foreach (var (scrollbarId, statePtr) in scrollbars)
				{
					var state = statePtr.Ref;
					if (state.SpacerEntityId == entityId.Ref)
					{
						isHorizontal = state.IsHorizontal;
						break;
					}
				}

				// Update spacer size to position the thumb
				var layout = node.Layout;

				if (isHorizontal)
				{
					layout.sizing = new Clay_Sizing(
						Clay_SizingAxis.Percent(update.Position),
						Clay_SizingAxis.Grow()
					);
				}
				else
				{
					layout.sizing = new Clay_Sizing(
						Clay_SizingAxis.Grow(),
						Clay_SizingAxis.Percent(update.Position)
					);
				}

				node.Layout = layout;
				commands.Entity(entityId.Ref).Insert(node);
			}
		})
		.InStage(Stage.First)
		.Label("scrollbar:update-spacer")
		.Build();

		// System to update scrollbar thumb size
		app.AddSystem((Commands commands, Query<Data<ScrollbarThumbUpdate, ClayNode>> updates, Query<Data<ScrollbarState>> scrollbars) =>
		{
			foreach (var (entityId, updatePtr, nodePtr) in updates)
			{
				var update = updatePtr.Ref;
				ref var node = ref nodePtr.Ref;

				// Find the parent scrollbar to get orientation
				bool isHorizontal = false;
				foreach (var (scrollbarId, statePtr) in scrollbars)
				{
					var state = statePtr.Ref;
					if (state.ThumbEntityId == entityId.Ref)
					{
						isHorizontal = state.IsHorizontal;
						break;
					}
				}

				// Update thumb size
				var layout = node.Layout;

				if (isHorizontal)
				{
					layout.sizing = new Clay_Sizing(
						Clay_SizingAxis.Percent(update.Size),
						Clay_SizingAxis.Grow()
					);
				}
				else
				{
					layout.sizing = new Clay_Sizing(
						Clay_SizingAxis.Grow(),
						Clay_SizingAxis.Percent(update.Size)
					);
				}

				node.Layout = layout;
				commands.Entity(entityId.Ref).Insert(node);
			}
		})
		.InStage(Stage.First)
		.Label("scrollbar:update-thumb")
		.Build();

		// System to handle scrollbar content size updates
		app.AddSystem((Commands commands, Query<Data<ScrollbarContentUpdate, ScrollbarState>> updates) =>
		{
			foreach (var (entityId, updatePtr, statePtr) in updates)
			{
				var update = updatePtr.Ref;
				var state = statePtr.Ref;

				// Recalculate viewport size
				state.ContentSize = update.ContentSize;
				state.VisibleSize = update.VisibleSize;
				state.ViewportSize = System.Math.Clamp(
					state.VisibleSize / System.Math.Max(state.ContentSize, 1f),
					0.01f,
					1f
				);

				// Clamp scroll position to new bounds
				state.ScrollPosition = System.Math.Clamp(state.ScrollPosition, 0f, 1f);

				// Update state
				commands.Entity(entityId.Ref).Insert(state);

				// Check if scrollbar should be visible
				bool isVisible = state.ContentSize > state.VisibleSize;

				// Update visibility
				commands.Entity(state.ContainerEntityId).Insert(new ScrollbarVisibilityUpdate
				{
					IsVisible = isVisible
				});

				// Trigger visual updates (only if visible)
				if (isVisible)
				{
					var spacerSize = state.ScrollPosition * (1f - state.ViewportSize);
					commands.Entity(state.SpacerEntityId).Insert(new ScrollbarSpacerUpdate
					{
						Position = spacerSize
					});

					commands.Entity(state.ThumbEntityId).Insert(new ScrollbarThumbUpdate
					{
						Size = state.ViewportSize
					});
				}
			}
		})
		.InStage(Stage.Update)
		.Label("scrollbar:update-content")
		.Build();

		// System to handle dragging with global mouse position
		// This allows dragging to continue even when mouse leaves scrollbar bounds
		app.AddSystem((Commands commands, Res<ClayPointerState> pointer, Query<Data<ScrollbarState, ClayComputedLayout>> scrollbars, Query<Data<ClayComputedLayout>> layouts) =>
		{
			foreach (var (entityId, statePtr, containerLayoutPtr) in scrollbars)
			{
				var state = statePtr.Ref;
				if (!state.IsDragging)
					continue;

				// Check if mouse button was released - stop dragging
				if (pointer.Value.PrimaryReleased)
				{
					state.IsDragging = false;
					commands.Entity(entityId.Ref).Insert(state);
					continue;
				}

				var containerLayout = containerLayoutPtr.Ref;

				// Get track and thumb layouts
				if (!layouts.Contains(state.TrackEntityId) || !layouts.Contains(state.ThumbEntityId))
					continue;

				var (_, trackLayoutPtr) = layouts.Get(state.TrackEntityId);
				var (_, thumbLayoutPtr) = layouts.Get(state.ThumbEntityId);
				var trackLayout = trackLayoutPtr.Ref;
				var thumbLayout = thumbLayoutPtr.Ref;

				// Get global mouse position
				var mousePos = state.IsHorizontal ? pointer.Value.Position.X : pointer.Value.Position.Y;

				// Convert to container-relative coordinates
				var containerPos = state.IsHorizontal ? containerLayout.X : containerLayout.Y;
				var containerLocalPos = mousePos - containerPos;

				// Convert to track-relative coordinates
				var trackPos = state.IsHorizontal ? trackLayout.X : trackLayout.Y;
				var trackOffset = trackPos - containerPos;
				var trackLocalPos = containerLocalPos - trackOffset;

				// Calculate scroll position
				var trackSize = state.IsHorizontal ? trackLayout.Width : trackLayout.Height;
				var thumbSize = state.IsHorizontal ? thumbLayout.Width : thumbLayout.Height;
				var maxScrollableArea = trackSize * (1f - state.ViewportSize);

				if (maxScrollableArea > 0)
				{
					// Center the thumb at mouse position
					var targetThumbPos = trackLocalPos - (thumbSize / 2f);
					var newScrollPosition = System.Math.Clamp(targetThumbPos / maxScrollableArea, 0f, 1f);

					// Only update if changed
					if (System.Math.Abs(newScrollPosition - state.ScrollPosition) > 0.001f)
					{
						state.ScrollPosition = newScrollPosition;
						commands.Entity(entityId.Ref).Insert(state);

						// Update visuals
						var spacerSize = state.ScrollPosition * (1f - state.ViewportSize);
						commands.Entity(state.SpacerEntityId).Insert(new ScrollbarSpacerUpdate
						{
							Position = spacerSize
						});

						commands.Entity(state.ThumbEntityId).Insert(new ScrollbarThumbUpdate
						{
							Size = state.ViewportSize
						});

						// Emit scroll event
						var maxScrollPixels = System.Math.Max(0f, state.ContentSize - state.VisibleSize);
						var scrollPixels = state.ScrollPosition * maxScrollPixels;
						commands.Entity(entityId.Ref).EmitTrigger(new ScrollbarScrolled
						{
							ScrollPosition = state.ScrollPosition,
							ScrollPixels = scrollPixels
						});
					}
				}
			}
		})
		.InStage(Stage.Update)
		.Label("scrollbar:handle-dragging")
		.Build();

		// System to handle mouse wheel scrolling
		app.AddSystem((Commands commands, Res<ClayPointerState> pointer, Query<Data<ScrollbarState, ClayComputedLayout>> scrollbars, Query<Data<ClayComputedLayout>> layouts) =>
		{
			// Only process if there's scroll input
			if (pointer.Value.ScrollDelta.Y == 0f && pointer.Value.ScrollDelta.X == 0f)
				return;

			var mouseX = pointer.Value.Position.X;
			var mouseY = pointer.Value.Position.Y;

			// Find scrollbars whose content areas contain the mouse
			foreach (var (entityId, statePtr, containerLayoutPtr) in scrollbars)
			{
				var state = statePtr.Ref;

				// Get the content area layout to check if mouse is over it
				if (!layouts.Contains(state.ContentAreaEntityId))
					continue;

				var (_, contentLayoutPtr) = layouts.Get(state.ContentAreaEntityId);
				var contentLayout = contentLayoutPtr.Ref;

				// Check if mouse is over the content area
				bool mouseOverContent = mouseX >= contentLayout.X && mouseX <= contentLayout.X + contentLayout.Width &&
										mouseY >= contentLayout.Y && mouseY <= contentLayout.Y + contentLayout.Height;

				if (!mouseOverContent)
					continue;

				// Apply scroll delta based on orientation
				float scrollDelta = state.IsHorizontal ? pointer.Value.ScrollDelta.X : pointer.Value.ScrollDelta.Y;

				// Invert Y scroll (wheel up = negative delta, should scroll up = decrease position)
				if (!state.IsHorizontal)
					scrollDelta = -scrollDelta;

				// Convert scroll delta to normalized position change
				// Scale by a reasonable factor (e.g., 20 pixels per wheel notch)
				float scrollSpeed = 20f;
				var maxScrollPixels = System.Math.Max(0f, state.ContentSize - state.VisibleSize);
				if (maxScrollPixels > 0)
				{
					float positionDelta = (scrollDelta * scrollSpeed) / maxScrollPixels;
					float newScrollPosition = System.Math.Clamp(state.ScrollPosition + positionDelta, 0f, 1f);

					// Only update if changed
					if (System.Math.Abs(newScrollPosition - state.ScrollPosition) > 0.001f)
					{
						state.ScrollPosition = newScrollPosition;
						commands.Entity(entityId.Ref).Insert(state);

						// Update visuals
						var spacerSize = state.ScrollPosition * (1f - state.ViewportSize);
						commands.Entity(state.SpacerEntityId).Insert(new ScrollbarSpacerUpdate
						{
							Position = spacerSize
						});

						commands.Entity(state.ThumbEntityId).Insert(new ScrollbarThumbUpdate
						{
							Size = state.ViewportSize
						});

						// Emit scroll event
						var scrollPixels = state.ScrollPosition * maxScrollPixels;
						commands.Entity(entityId.Ref).EmitTrigger(new ScrollbarScrolled
						{
							ScrollPosition = state.ScrollPosition,
							ScrollPixels = scrollPixels
						});
					}
				}
			}
		})
		.InStage(Stage.Update)
		.Label("scrollbar:handle-wheel")
		.Build();

		// Observer to apply scroll offset to content area when scrollbar is scrolled
		app.AddObserver<On<ScrollbarScrolled>, Commands, Query<Data<ScrollbarState>>, Query<Data<ClayNode, ClayScrollContainer>>>((trigger, commands, scrollbars, contentAreas) =>
		{
			var scrollEvent = trigger.Event;
			var scrollbarEntityId = trigger.EntityId;

			// Find the scrollbar state to get the content area entity ID
			if (!scrollbars.Contains(scrollbarEntityId))
			{
				return;
			}

			var (_, statePtr) = scrollbars.Get(scrollbarEntityId);
			var state = statePtr.Ref;

			// Check if the content area exists and has the required components
			if (!contentAreas.Contains(state.ContentAreaEntityId))
			{
				return;
			}

			var (_, nodePtr, scrollContainerPtr) = contentAreas.Get(state.ContentAreaEntityId);
			ref var node = ref nodePtr.Ref;
			var scrollContainer = scrollContainerPtr.Ref;

			// Update scroll offset based on scroll position
			// Preserve the other axis offset when updating
			if (state.IsHorizontal)
			{
				// Update X, preserve Y
				scrollContainer.ScrollOffset = new System.Numerics.Vector2(scrollEvent.ScrollPixels, scrollContainer.ScrollOffset.Y);
			}
			else
			{
				// Update Y, preserve X
				scrollContainer.ScrollOffset = new System.Numerics.Vector2(scrollContainer.ScrollOffset.X, scrollEvent.ScrollPixels);
			}

			// Update the clip config to apply the scroll offset
			if (!node.Clip.HasValue)
			{
				node.Clip = new Clay_ClipElementConfig
				{
					horizontal = state.IsHorizontal,
					vertical = !state.IsHorizontal,
					childOffset = new Clay_Vector2 { x = -scrollContainer.ScrollOffset.X, y = -scrollContainer.ScrollOffset.Y }
				};
			}
			else
			{
				var clip = node.Clip.Value;
				clip.childOffset = new Clay_Vector2 { x = -scrollContainer.ScrollOffset.X, y = -scrollContainer.ScrollOffset.Y };
				node.Clip = clip;
			}

			// Insert updated components
			commands.Entity(state.ContentAreaEntityId).Insert(node);
			commands.Entity(state.ContentAreaEntityId).Insert(scrollContainer);
		});
	}
}

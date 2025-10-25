using System;
using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Widgets;

/// <summary>
/// Registers default system-driven interactions for UI widgets using proper system parameters.
/// Uses EventReader, Queries, and Commands instead of direct World access (reflection-free).
/// - Button: hover/press background updates
/// - Checkbox: toggle on click, visual update + checkmark
/// - FloatingWindow: drag by title bar
/// - Slider: drag to adjust value
/// </summary>
public sealed class UiWidgetsPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddResource(new UiWindowOrder());

		// Global observer: Register floating windows in window order when FloatingWindowState is added
		// (Entity observers don't fire for components added in the same command batch, so we use a global observer)
		app.AddObserver((OnAdd<FloatingWindowState> trigger, ResMut<UiWindowOrder> windowsOrder) =>
		{
			windowsOrder.Value.MoveToTop(trigger.EntityId);
		});

		app.AddObserver((OnRemove<FloatingWindowState> trigger, ResMut<UiWindowOrder> windowsOrder) =>
		{
			windowsOrder.Value.Remove(trigger.EntityId);
		});

		// NOTE: Main widget interaction logic (pointer events) has been moved to entity observers.
		// This happens in widget creation code (see SliderWidget.Create(), CheckboxWidget.Create(), FloatingWindowWidget.Create()).
		// The systems below are FALLBACK ONLY - they handle continued updates when pointer leaves UI bounds entirely.

		// System 1: Fallback for window dragging when pointer leaves Clay elements
		// This system ensures windows keep updating even when mouse is outside UI bounds
		app.AddSystem((
			Res<ClayPointerState> pointerState,
			Query<Data<FloatingWindowState, UiNode>> windows,
			ResMut<ClayUiState> uiState) =>
		{
			var pointerPos = pointerState.Value.Position;
			var anyDragging = false;

			foreach (var (entityId, winState, winNode) in windows)
			{
				ref var win = ref winState.Ref;
				ref var node = ref winNode.Ref;

				if (win.IsDragging)
				{
					// Calculate absolute position using current pointer position
					win.Position = pointerPos - win.DragOffset;

					node.Declaration.floating.offset = new Clay_Vector2
					{
						x = win.Position.X,
						y = win.Position.Y
					};

					anyDragging = true;
				}
			}

			// Only request layout if we actually moved a window
			if (anyDragging)
			{
			}
		})
		.InStage(Stage.Update)
		.Label("ui:widgets:windows:fallback")
		// No explicit dependency to allow running without Clay
		.RunIfResourceExists<ClayPointerState>()
		.Build();

		// System 2: Fallback for slider dragging when pointer leaves slider/current target
		app.AddSystem((
			Res<ClayPointerState> pointerState,
			ResMut<ClayUiState> uiState,
			Query<Data<SliderState, SliderLinks, ClaySliderStyle>> sliders,
			Query<Data<UiNode>> nodes) =>
		{
			var pointerPos = pointerState.Value.Position;

			foreach (var (entityId, sliderState, sliderLinks, sliderStyle) in sliders)
			{
				ref var st = ref sliderState.Ref;
				if (!st.IsDragging) continue;

				var links = sliderLinks.Ref;
				var style = sliderStyle.Ref;

				// Compute normalized from absolute pointer position relative to container bounds
				float normalized = st.NormalizedValue;
				unsafe
				{
					var ctx = uiState.Value.Context;
					if (ctx is not null)
					{
						Clay.SetCurrentContext(ctx);
						var containerElemId = ClayId.Global($"slider-container-{entityId.Ref}").ToElementId();
						var elem = Clay.GetElementData(containerElemId);
						if (elem.found && elem.boundingBox.width > 0)
						{
							normalized = (pointerPos.X - elem.boundingBox.x) / Math.Max(1f, style.Width);
							normalized = Math.Clamp(normalized, 0f, 1f);
							st.SetNormalizedValue(normalized);
						}
					}
				}

				// Update visuals (fill width and handle position)
				var changed = false;
				if (links.FillEntity != 0 && nodes.Contains(links.FillEntity))
				{
					var fillData = nodes.Get(links.FillEntity);
					fillData.Deconstruct(out var fillNode);
					ref var fillNodeRef = ref fillNode.Ref;
					var fillWidth = style.Width * normalized;
					fillNodeRef.Declaration.layout.sizing = new Clay_Sizing(
						Clay_SizingAxis.Fixed(fillWidth),
						Clay_SizingAxis.Fixed(style.TrackHeight));
					changed = true;
				}

				if (links.HandleLayerEntity != 0 && nodes.Contains(links.HandleLayerEntity))
				{
					var layerData = nodes.Get(links.HandleLayerEntity);
					layerData.Deconstruct(out var layerNode);
					ref var layerNodeRef = ref layerNode.Ref;
					var handleX = (style.Width - style.HandleSize) * normalized;
					layerNodeRef.Declaration.layout.padding = new Clay_Padding
					{
						left = (ushort)handleX,
						right = 0,
						top = 0,
						bottom = 0
					};
					changed = true;
				}

				if (changed)
				{
				}
			}
		})
		.InStage(Stage.Update)
		.Label("ui:widgets:sliders:fallback")
		.RunIfResourceExists<ClayPointerState>()
		.Build();

		// System 3: Update scrollbar positions and apply manual scroll offset
		// This system calculates viewport/content dimensions, updates FloatingWindowState,
		// applies the scroll offset to clip.childOffset, and positions the scrollbar thumb
		app.AddSystem((
			Query<Data<FloatingWindowState, FloatingWindowLinks, UiNode>> windows,
			Query<Data<UiNode>> nodes,
			ResMut<ClayUiState> uiState) =>
		{
			unsafe
			{
				var ctx = uiState.Value.Context;
				if (ctx is null) return;

				Clay.SetCurrentContext(ctx);

				foreach (var (entityId, winState, winLinks, winNode) in windows)
				{
					var links = winLinks.Ref;
					ref var state = ref winState.Ref;

					// Skip if no scrollbar exists
					if (links.ScrollbarTrackId == 0 || links.ScrollbarThumbLayerId == 0 || links.ScrollbarThumbId == 0 || links.ScrollContainerId == 0)
						continue;

					// Get viewport dimensions from ScrollContainerId
					if (!nodes.Contains(links.ScrollContainerId))
						continue;

					var scrollContainerData = nodes.Get(links.ScrollContainerId);
					scrollContainerData.Deconstruct(out var scrollContainerNode);
					var scrollElemId = scrollContainerNode.Ref.ElementId;
					var scrollElem = Clay.GetElementData(scrollElemId);

					if (!scrollElem.found)
						continue;

					float viewportHeight = scrollElem.boundingBox.height;

					// Get content dimensions from ContentAreaId
					if (!nodes.Contains(links.ContentAreaId))
						continue;

					var contentAreaData = nodes.Get(links.ContentAreaId);
					contentAreaData.Deconstruct(out var contentAreaNode);
					var contentElemId = contentAreaNode.Ref.ElementId;
					var contentElem = Clay.GetElementData(contentElemId);

					if (!contentElem.found)
						continue;

					float contentHeight = contentElem.boundingBox.height;

					// Calculate max scroll (how much we can scroll)
					float maxScroll = Math.Max(0f, contentHeight - viewportHeight);

					// Update state with dimensions
					state.ViewportHeight = viewportHeight;
					state.ContentHeight = contentHeight;
					state.MaxScrollY = maxScroll;

					// Sync scroll position from Clay's internal state
					// (Clay handles all scrolling via UpdateScrollContainers)
					var scrollData = Clay.GetScrollContainerData(scrollContainerNode.Ref.ElementId);
					if (scrollData.found && scrollData.scrollPosition != null)
					{
						// Clay stores the childOffset, which is negative of our scroll offset
						state.ScrollOffsetY = -scrollData.scrollPosition->y;
					}

					// Clamp scroll offset to valid range
					state.ScrollOffsetY = Math.Clamp(state.ScrollOffsetY, 0f, maxScroll);

					// Only show scrollbar if content overflows
					if (maxScroll <= 0f)
					{
						// Hide scrollbar by setting size to 0
						if (nodes.Contains(links.ScrollbarTrackId))
						{
							var trackData = nodes.Get(links.ScrollbarTrackId);
							trackData.Deconstruct(out var trackNode);
							ref var track = ref trackNode.Ref;
							track.Declaration.layout.sizing = new Clay_Sizing(
								Clay_SizingAxis.Fixed(0f),
								Clay_SizingAxis.Fixed(0f));
						}
						continue;
					}

					// Calculate scrollbar dimensions
					float trackHeight = viewportHeight;

					// Thumb size proportional to viewport/content ratio
					float thumbHeight = Math.Max(20f, (viewportHeight / contentHeight) * trackHeight);

					// Thumb position proportional to scroll offset
					float scrollPercent = maxScroll > 0 ? state.ScrollOffsetY / maxScroll : 0f;
					float thumbY = scrollPercent * (trackHeight - thumbHeight);

					// Update scrollbar track sizing (restore visibility if hidden)
					if (nodes.Contains(links.ScrollbarTrackId))
					{
						var trackData = nodes.Get(links.ScrollbarTrackId);
						trackData.Deconstruct(out var trackNode);
						ref var track = ref trackNode.Ref;

						// Ensure track is visible with correct sizing
						track.Declaration.layout.sizing = new Clay_Sizing(
							Clay_SizingAxis.Fixed(8f),
							Clay_SizingAxis.Grow());
					}

					// Update scrollbar thumb layer sizing and padding to position thumb based on scroll offset
					if (nodes.Contains(links.ScrollbarThumbLayerId))
					{
						var thumbLayerData = nodes.Get(links.ScrollbarThumbLayerId);
						thumbLayerData.Deconstruct(out var thumbLayerNode);
						ref var thumbLayer = ref thumbLayerNode.Ref;

						// Set fixed sizing to match track height (prevents growth from padding)
						thumbLayer.Declaration.layout.sizing = new Clay_Sizing(
							Clay_SizingAxis.Fixed(8f),
							Clay_SizingAxis.Fixed(trackHeight));

						// Update padding to move thumb vertically
						thumbLayer.Declaration.layout.padding = new Clay_Padding
						{
							left = 0,
							right = 0,
							top = (ushort)thumbY,
							bottom = 0
						};
					}

					// Update scrollbar thumb size
					if (nodes.Contains(links.ScrollbarThumbId))
					{
						var thumbData = nodes.Get(links.ScrollbarThumbId);
						thumbData.Deconstruct(out var thumbNode);
						ref var thumb = ref thumbNode.Ref;

						thumb.Declaration.layout.sizing = new Clay_Sizing(
							Clay_SizingAxis.Fixed(6f),
							Clay_SizingAxis.Fixed(thumbHeight));
					}
				}
			}
		})
		.InStage(Stage.PreUpdate)
		.Label("ui:widgets:scrollbar:update")
		.Before("ui:clay:layout") // Run before layout so childOffset is applied
		.RunIfResourceExists<ClayUiState>()
		.Build();

		// System to handle scrollbar thumb drag start/stop - processes events AFTER Clay generates them
		app.AddSystem((
			EventReader<UiPointerEvent> events,
			Query<Data<FloatingWindowState, FloatingWindowLinks>> windows) =>
		{
			foreach (var evt in events.Read())
			{
				foreach (var (entityId, winState, winLinks) in windows)
				{
					ref var state = ref winState.Ref;
					var links = winLinks.Ref;

					// Skip if no scrollbar exists
					if (links.ScrollbarTrackId == 0 || links.ScrollbarThumbId == 0 || links.ScrollContainerId == 0)
						continue;

					if (evt.Type == UiPointerEventType.PointerDown && evt.IsPrimaryButton)
					{
						// Check if pointer is over scrollbar thumb or track
						bool onThumb = evt.Target == links.ScrollbarThumbId || evt.Target == links.ScrollbarThumbLayerId;
						bool onTrack = evt.Target == links.ScrollbarTrackId;

						if (onThumb || onTrack)
						{
							state.IsScrollbarDragging = true;
							state.ScrollbarDragStartY = evt.Position.Y;
						}
					}
					else if (evt.Type == UiPointerEventType.PointerUp)
					{
						state.IsScrollbarDragging = false;
					}
				}
			}
		})
		.InStage(Stage.PreUpdate)
		.Label("ui:widgets:scrollbar:click")
		.After("ui:clay:pointer")  // Run AFTER events are generated
		.RunIfResourceExists<ClayUiState>()
		.Build();

		// System to handle scrollbar dragging - simulates pointer being over content
		app.AddSystem((
			Query<Data<FloatingWindowState, FloatingWindowLinks>> windows,
			Query<Data<UiNode>> nodes,
			ResMut<ClayPointerState> pointer,
			ResMut<ClayUiState> uiState) =>
		{
			// When dragging scrollbar, temporarily move pointer over content area
			// so Clay's UpdateScrollContainers applies the scroll to the right element

			unsafe
			{
				var ctx = uiState.Value.Context;
				if (ctx is null) return;

				Clay.SetCurrentContext(ctx);

				foreach (var (entityId, winState, winLinks) in windows)
				{
					ref var state = ref winState.Ref;
					var links = winLinks.Ref;

					// Skip if not currently dragging
					if (!state.IsScrollbarDragging)
						continue;

					// Skip if no scrollbar exists
					if (links.ScrollbarTrackId == 0 || links.ScrollbarThumbId == 0 || links.ScrollContainerId == 0)
						continue;

					// Check if pointer moved this frame
					if (pointer.Value.MoveDelta == Vector2.Zero)
						continue;

					// Get scroll container bounds
					if (!nodes.Contains(links.ScrollContainerId))
						continue;

					var scrollContainerData = nodes.Get(links.ScrollContainerId);
					scrollContainerData.Deconstruct(out var scrollNode);
					var scrollElemId = scrollNode.Ref.ElementId;
					var scrollElem = Clay.GetElementData(scrollElemId);

					if (!scrollElem.found)
						continue;

					var bbox = scrollElem.boundingBox;

					// Calculate scroll delta from pointer movement
					float deltaY = pointer.Value.MoveDelta.Y;
					float viewportHeight = state.ViewportHeight;
					float contentHeight = state.ContentHeight;

					// Only update if we have valid dimensions
					if (viewportHeight <= 0 || contentHeight <= 0)
						continue;

					// Calculate thumb dimensions
					float trackHeight = viewportHeight;
					float thumbHeight = Math.Max(20f, (viewportHeight / contentHeight) * trackHeight);
					float maxThumbY = trackHeight - thumbHeight;

					// Avoid division by zero
					if (maxThumbY <= 0)
						continue;

					// Convert pixel delta to scroll offset delta
					float scrollOffsetDelta = (deltaY / maxThumbY) * state.MaxScrollY;

					// When dragging, always apply scroll regardless of pointer position
					// Temporarily move pointer to the content area so Clay applies scroll to the container
					var pointerPos = pointer.Value.Position;

					// Move pointer to center X of content, keep original Y to preserve delta calculation
					pointer.Value.Position = new Vector2(
						bbox.x + bbox.width / 2f,  // Center X
						pointerPos.Y);  // Keep original Y to preserve delta calculation

					// Add scroll input - use the actual scroll offset change directly
					// Positive scrollOffsetDelta means scrolling down (content moves up)
					// Clay expects negative scroll delta Y for scrolling down
					var scrollDelta = new Vector2(0, -scrollOffsetDelta * 0.05f);
					pointer.Value.AddScroll(scrollDelta);

					// Only process one dragging window per frame
					break;
				}
			}
		})
		.InStage(Stage.PreUpdate)
		.Label("ui:widgets:scrollbar:drag")
		.Before("ui:clay:pointer")  // Run BEFORE Clay processes input
		.RunIfResourceExists<ClayPointerState>()
		.RunIfResourceExists<ClayUiState>()
		.Build();

		// ===== ScrollContainerWidget Systems =====

		// System: Update scroll container dimensions and scrollbar position
		app.AddSystem((
			Query<Data<ScrollState, ScrollContainerLinks, UiNode>> scrollContainers,
			Query<Data<UiNode>> nodes,
			ResMut<ClayUiState> uiState) =>
		{
			unsafe
			{
				var ctx = uiState.Value.Context;
				if (ctx is null) return;

				Clay.SetCurrentContext(ctx);

				foreach (var (entityId, scrollState, scrollLinks, scrollNode) in scrollContainers)
				{
					ref var state = ref scrollState.Ref;
					var links = scrollLinks.Ref;

					// Get scroll container (clip element) data
					if (!nodes.Contains(links.ContentWrapperId))
						continue;

					var wrapperData = nodes.Get(links.ContentWrapperId);
					wrapperData.Deconstruct(out var wrapperNode);
					var wrapperElemId = wrapperNode.Ref.ElementId;
					var wrapperElem = Clay.GetElementData(wrapperElemId);

					if (!wrapperElem.found)
						continue;

					var scrollData = Clay.GetScrollContainerData(wrapperElemId);
					if (!scrollData.found)
						continue;

					// Sync scroll offset from Clay's internal state
					// BUT skip this if we're currently dragging the scrollbar (to prevent fighting with drag system)
					if (!state.IsScrollbarDragging)
					{
						// Clay stores childOffset as negative of scroll offset
						state.ScrollOffsetY = -scrollData.scrollPosition->y;
					}
					state.ViewportHeight = wrapperElem.boundingBox.height;

					// Get content dimensions
					var contentHeight = 0f;
					if (nodes.Contains(links.ContentAreaId))
					{
						var contentData = nodes.Get(links.ContentAreaId);
						contentData.Deconstruct(out var contentNode);
						var contentElemId = contentNode.Ref.ElementId;
						var contentElem = Clay.GetElementData(contentElemId);
						if (contentElem.found)
						{
							contentHeight = contentElem.boundingBox.height;
						}
					}

					state.ContentHeight = contentHeight;
					state.MaxScrollY = Math.Max(0, contentHeight - state.ViewportHeight);

					// Update scrollbar track if it exists
					if (links.ScrollbarTrackId != 0 && nodes.Contains(links.ScrollbarTrackId))
					{
						var trackData = nodes.Get(links.ScrollbarTrackId);
						trackData.Deconstruct(out var trackNode);
						ref var trackDecl = ref trackNode.Ref.Declaration;

						// Make track height match viewport height exactly
						trackDecl.layout.sizing = new Clay_Sizing(
							Clay_SizingAxis.Fixed(8f),
							Clay_SizingAxis.Fixed(state.ViewportHeight));
					}

					// Update scrollbar thumb if it exists
					if (links.ScrollbarThumbId == 0 || links.ScrollbarThumbLayerId == 0)
						continue;

					if (!nodes.Contains(links.ScrollbarThumbId) || !nodes.Contains(links.ScrollbarThumbLayerId))
						continue;

					var thumbData = nodes.Get(links.ScrollbarThumbId);
					var thumbLayerData = nodes.Get(links.ScrollbarThumbLayerId);
					thumbData.Deconstruct(out var thumbNode);
					thumbLayerData.Deconstruct(out var thumbLayerNode);

					ref var thumbDecl = ref thumbNode.Ref.Declaration;
					ref var thumbLayerDecl = ref thumbLayerNode.Ref.Declaration;

					// Calculate scrollbar dimensions
					float viewportHeight = state.ViewportHeight;
					float contentHeightVal = state.ContentHeight;

					if (contentHeightVal > viewportHeight)
					{
						float trackHeight = viewportHeight;
						float thumbHeight = Math.Max(20f, (viewportHeight / contentHeightVal) * trackHeight);
						float maxThumbY = trackHeight - thumbHeight;

						// Update thumb height
						thumbDecl.layout.sizing.height = Clay_SizingAxis.Fixed(thumbHeight);

						// Calculate thumb position based on scroll offset
						float scrollRatio = state.MaxScrollY > 0 ? (state.ScrollOffsetY / state.MaxScrollY) : 0;
						float thumbY = scrollRatio * maxThumbY;

						// Update thumb layer sizing to match track height (prevents padding expansion)
						thumbLayerDecl.layout.sizing = new Clay_Sizing(
							Clay_SizingAxis.Fixed(8f),
							Clay_SizingAxis.Fixed(trackHeight));

						// Update thumb layer padding to position thumb
						thumbLayerDecl.layout.padding = new Clay_Padding
						{
							left = 0,
							right = 0,
							top = (ushort)Math.Clamp(thumbY, 0, maxThumbY),
							bottom = 0
						};
					}
				}
			}
		})
		.InStage(Stage.PreUpdate)
		.Label("ui:widgets:scrollcontainer:update")
		.Before("ui:clay:layout")  // Run BEFORE layout so thumb position updates are applied
		.RunIfResourceExists<ClayUiState>()
		.Build();

		// System: Detect scrollbar clicks for scroll containers
		app.AddSystem((
			EventReader<UiPointerEvent> events,
			Query<Data<ScrollState, ScrollContainerLinks>> scrollContainers,
			Query<Data<UiNode>> nodes,
			ResMut<ClayUiState> uiState) =>
		{
			foreach (var evt in events.Read())
			{
				if (evt.Type != UiPointerEventType.PointerDown || !evt.IsPrimaryButton)
					continue;

				unsafe
				{
					var ctx = uiState.Value.Context;
					if (ctx is null) continue;

					Clay.SetCurrentContext(ctx);

					foreach (var (entityId, scrollState, scrollLinks) in scrollContainers)
					{
						ref var state = ref scrollState.Ref;
						var links = scrollLinks.Ref;

						if (links.ScrollbarThumbId == 0 || links.ScrollbarTrackId == 0)
							continue;

						var targetId = evt.Target;

						// Check if clicking on scrollbar thumb or track
						if (targetId == links.ScrollbarThumbId || targetId == links.ScrollbarTrackId || targetId == links.ScrollbarThumbLayerId)
						{
							state.IsScrollbarDragging = true;
							state.ScrollbarDragStartY = evt.Position.Y;
							break;
						}
					}
				}
			}

			// Detect mouse release
			foreach (var evt in events.Read())
			{
				if (evt.Type == UiPointerEventType.PointerUp && evt.IsPrimaryButton)
				{
					foreach (var (entityId, scrollState, scrollLinks) in scrollContainers)
					{
						ref var state = ref scrollState.Ref;
						state.IsScrollbarDragging = false;
					}
				}
			}
		})
		.InStage(Stage.PreUpdate)
		.Label("ui:widgets:scrollcontainer:click")
		.After("ui:widgets:scrollcontainer:update")
		.RunIfResourceExists<ClayUiState>()
		.Build();

		// System: Handle scrollbar dragging for scroll containers
		app.AddSystem((
			Query<Data<ScrollState, ScrollContainerLinks>> scrollContainers,
			Query<Data<UiNode>> nodes,
			ResMut<ClayPointerState> pointer,
			ResMut<ClayUiState> uiState) =>
		{
			unsafe
			{
				var ctx = uiState.Value.Context;
				if (ctx is null) return;

				Clay.SetCurrentContext(ctx);

				foreach (var (entityId, scrollState, scrollLinks) in scrollContainers)
				{
					ref var state = ref scrollState.Ref;
					var links = scrollLinks.Ref;

					// Skip if not currently dragging
					if (!state.IsScrollbarDragging)
						continue;

					// Skip if no scrollbar exists
					if (links.ScrollbarTrackId == 0 || links.ScrollbarThumbId == 0 || links.ContentWrapperId == 0)
						continue;

					// Check if pointer moved this frame
					if (pointer.Value.MoveDelta == Vector2.Zero)
						continue;

					// Get scroll container bounds
					if (!nodes.Contains(links.ContentWrapperId))
						continue;

					var wrapperData = nodes.Get(links.ContentWrapperId);
					wrapperData.Deconstruct(out var wrapperNode);
					var wrapperElemId = wrapperNode.Ref.ElementId;
					var wrapperElem = Clay.GetElementData(wrapperElemId);

					if (!wrapperElem.found)
						continue;

					var bbox = wrapperElem.boundingBox;

					// Calculate scroll delta from pointer movement
					float deltaY = pointer.Value.MoveDelta.Y;
					float viewportHeight = state.ViewportHeight;
					float contentHeight = state.ContentHeight;

					// Only update if we have valid dimensions
					if (viewportHeight <= 0 || contentHeight <= 0)
						continue;

					// Calculate thumb dimensions
					float trackHeight = viewportHeight;
					float thumbHeight = Math.Max(20f, (viewportHeight / contentHeight) * trackHeight);
					float maxThumbY = trackHeight - thumbHeight;

					// Avoid division by zero
					if (maxThumbY <= 0)
						continue;

					// Convert pixel delta to scroll offset delta
					float scrollOffsetDelta = (deltaY / maxThumbY) * state.MaxScrollY;

					// Update our local scroll offset state immediately so the thumb position updates
					state.ScrollOffsetY = Math.Clamp(state.ScrollOffsetY + scrollOffsetDelta, 0f, state.MaxScrollY);

					// When dragging, always apply scroll regardless of pointer position
					// Temporarily move pointer to the content area so Clay applies scroll to the container
					var pointerPos = pointer.Value.Position;

					// Move pointer to center X of content, keep original Y to preserve delta calculation
					pointer.Value.Position = new Vector2(
						bbox.x + bbox.width / 2f,  // Center X
						pointerPos.Y);  // Keep original Y to preserve delta calculation

					// Add scroll input - use the actual scroll offset change directly
					// Positive scrollOffsetDelta means scrolling down (content moves up)
					// Clay expects negative scroll delta Y for scrolling down
					var scrollDelta = new Vector2(0, -scrollOffsetDelta * 0.05f);
					pointer.Value.AddScroll(scrollDelta);

					// Only process one dragging container per frame
					break;
				}
			}
		})
		.InStage(Stage.PreUpdate)
		.Label("ui:widgets:scrollcontainer:drag")
		.Before("ui:clay:pointer")  // Run BEFORE Clay processes input
		.RunIfResourceExists<ClayPointerState>()
		.RunIfResourceExists<ClayUiState>()
		.Build();
	}

	private static bool IsDescendantOf(ulong child, ulong ancestor, Query<Data<Parent>> hierarchy)
	{
		if (child == 0 || ancestor == 0) return false;
		var current = child;
		var safety = 0;

		while (current != 0 && safety++ < 256)
		{
			if (current == ancestor) return true;

			// Search for parent
			bool foundParent = false;
			foreach (var (entityId, parent) in hierarchy)
			{
				if (entityId.Ref == current)
				{
					current = parent.Ref.Id;
					foundParent = true;
					break;
				}
			}

			if (!foundParent) break;
			if (current == 0) break;
		}
		return false;
	}
}

public static class UiWidgetsAppExtensions
{
	public static App AddUiWidgets(this App app)
	{
		app.AddPlugin(new UiWidgetsPlugin());
		return app;
	}
}
// Window order resource moved to TinyEcs.UI namespace (UiWindowOrder.cs)


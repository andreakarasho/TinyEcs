using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI.Widgets;

namespace TinyEcs.UI;

internal static class ClayUiSystems
{
	public static void SyncUiHierarchy(
		ResMut<ClayUiState> uiState,
		Commands commands,
		Query<Data<UiNodeParent>, Filter<Changed<UiNodeParent>>> desiredParents,
		Query<Data<Parent>> currentParents,
		Query<Data<Children>> childrenLists,
		Query<Data<FloatingWindowLinks>> windowLinks,
		Query<Data<ScrollContainerLinks>> scrollContainerLinks)
	{
		var layoutDirty = false;

		foreach ((PtrRO<ulong> entityPtr, Ptr<UiNodeParent> desiredPtr) in desiredParents)
		{
			var entityId = entityPtr.Ref;
			ref var desired = ref desiredPtr.Ref;

			// Get current parent (if any) from query, avoid direct world access
			ulong currentParent = 0;
			if (currentParents.Contains(entityId))
			{
				var parentData = currentParents.Get(entityId);
				parentData.Deconstruct(out _, out var parentPtr);
				currentParent = parentPtr.Ref.Id;
			}

			var targetParent = desired.Parent;
			// If target is a floating window, reparent to its content area when available
			if (targetParent != 0 && windowLinks.Contains(targetParent))
			{
				var linksData = windowLinks.Get(targetParent);
				linksData.Deconstruct(out var linksPtr);
				var links = linksPtr.Ref;
				// Only redirect external children to the content area.
				// Keep the window's own parts (title bar, body container, scroll container, scrollbar track, content area) attached where they are.
				if (links.ContentAreaId != 0)
				{
					var childId = entityId;
					if (childId != links.ContentAreaId &&
						childId != links.TitleBarId &&
						childId != links.BodyContainerId &&
						childId != links.ScrollContainerId &&
						childId != links.ScrollbarTrackId)
						targetParent = links.ContentAreaId;
				}
			}

			// If target is a scroll container, reparent to its content area when available
			if (targetParent != 0 && scrollContainerLinks.Contains(targetParent))
			{
				var linksData = scrollContainerLinks.Get(targetParent);
				linksData.Deconstruct(out var linksPtr);
				var links = linksPtr.Ref;
				// Only redirect external children to the content area.
				// Keep the scroll container's own parts (wrapper, content area, scrollbar) attached where they are.
				if (links.ContentAreaId != 0)
				{
					var childId = entityId;
					if (childId != links.ContentAreaId &&
						childId != links.ContentWrapperId &&
						childId != links.ScrollbarTrackId)
						targetParent = links.ContentAreaId;
				}
			}

			// Ignore reordering hints to avoid oscillation; only reparent when parent actually changes
			if (targetParent == currentParent)
			{
				// Same parent; apply index if provided and different
				if (targetParent != 0 && desired.Index >= 0 && childrenLists.Contains(targetParent))
				{
					var data = childrenLists.Get(targetParent);
					data.Deconstruct(out _, out var childListPtr);
					ref var list = ref childListPtr.Ref;

					// Find current index
					int currentIndex = -1;
					int idx = 0;
					foreach (var child in list)
					{
						if (child == entityId) { currentIndex = idx; break; }
						idx++;
					}

					if (currentIndex != desired.Index)
					{
						commands.RemoveChild(entityId);
						var clamped = desired.Index;
						if (clamped < 0) clamped = 0;
						if (clamped > list.Count) clamped = list.Count;
						commands.AddChild(targetParent, entityId, clamped);
						layoutDirty = true;
					}
				}
				continue;
			}

			// Detach if moving to root
			if (targetParent == 0 && currentParent != 0)
			{
				commands.RemoveChild(entityId);
				layoutDirty = true;
				continue;
			}

			// Attach when moving under a new parent
			if (targetParent != 0)
			{
				if (desired.Index >= 0)
					commands.AddChild(targetParent, entityId, desired.Index);
				else
					commands.AddChild(targetParent, entityId);
				layoutDirty = true;
			}
		}

		if (layoutDirty)
		{
		}
	}

	public static unsafe void ApplyPointerInput(
		Commands commands,
		ResMut<ClayPointerState> pointerState,
		ResMut<ClayUiState> uiState,
		EventWriter<UiPointerEvent> events,
		Query<Data<Parent>> parents,
		Query<Data<UiNode>> allNodes)
	{
		ref var pointer = ref pointerState.Value;
		ref var state = ref uiState.Value;

		var wasPrimaryDown = pointer.LastPrimaryDown;
		var isPrimaryDown = pointer.PrimaryDown;
		var moveDelta = pointer.MoveDelta;
		var scrollDelta = pointer.ScrollDelta;

		var pointerChanged =
			pointer.Dirty ||
			moveDelta != Vector2.Zero ||
			scrollDelta != Vector2.Zero ||
			wasPrimaryDown != isPrimaryDown;

		if (state.Context is null)
		{
			// Context not initialized yet - skip pointer processing
			pointer.ResetFrame();
			return;
		}

		Clay.SetCurrentContext(state.Context);

		if (pointerChanged)
		{
			Clay.SetPointerState(pointer.Position, pointer.PrimaryDown);

			// Let Clay handle scrolling automatically
			Clay.UpdateScrollContainers(pointer.EnableDragScrolling, scrollDelta, pointer.DeltaTime);
		}

		var hoveredIds = Clay.GetPointerOverIds();

		// Disable drag-based scrolling to avoid conflicts with window dragging
		// Only use wheel/touchpad scrolling (handled via ScrollDelta)
		pointer.EnableDragScrolling = false;
		var newCount = hoveredIds.Length;

#pragma warning disable CS9081
		Span<uint> currentKeys;
		if (newCount == 0)
		{
			currentKeys = Span<uint>.Empty;
		}
		else if (newCount <= 16)
		{
			currentKeys = stackalloc uint[newCount];
		}
		else
		{
			currentKeys = new uint[newCount];
		}

		for (var i = 0; i < newCount; ++i)
		{
			currentKeys[i] = hoveredIds[i].id;
		}

		var previousCount = state.HoveredElementIds.Count;
		Span<uint> previousKeys;
		if (previousCount == 0)
		{
			previousKeys = Span<uint>.Empty;
		}
		else if (previousCount <= 16)
		{
			previousKeys = stackalloc uint[previousCount];
		}
		else
		{
			previousKeys = new uint[previousCount];
		}
#pragma warning restore CS9081

		if (previousCount > 0)
		{
			var index = 0;
			foreach (var key in state.HoveredElementIds)
			{
				if (index >= previousKeys.Length)
					break;
				previousKeys[index++] = key;
			}
		}

		ReadOnlySpan<uint> previousReadOnly = previousKeys;
		ReadOnlySpan<uint> currentReadOnly = currentKeys;

		state.HoveredElementIds.Clear();
		for (var i = 0; i < currentReadOnly.Length; ++i)
		{
			if (currentReadOnly[i] != 0)
				state.HoveredElementIds.Add(currentReadOnly[i]);
		}

		// Pointer exits
		for (var i = 0; i < previousReadOnly.Length; ++i)
		{
			var key = previousReadOnly[i];
			if (key == 0 || state.HoveredElementIds.Contains(key))
				continue;

			DispatchPointerEventForKey(
				UiPointerEventType.PointerExit,
				key,
				pointer.Position,
				moveDelta,
				scrollDelta,
				isPrimaryDown,
				state,
				commands,
				events,
				parents,
				allNodes);
		}

		// Pointer enters
		for (var i = 0; i < currentReadOnly.Length; ++i)
		{
			var key = currentReadOnly[i];
			if (key == 0 || Contains(previousReadOnly, key))
				continue;

			DispatchPointerEventForKey(
				UiPointerEventType.PointerEnter,
				key,
				pointer.Position,
				moveDelta,
				scrollDelta,
				isPrimaryDown,
				state,
				commands,
				events,
				parents,
				allNodes);
		}

		// Pointer down
		if (isPrimaryDown && !wasPrimaryDown)
		{
			var targetKey = FindTopElementKey(currentReadOnly, state);
			if (targetKey != 0)
			{
				if (DispatchPointerEventForKey(
						UiPointerEventType.PointerDown,
						targetKey,
						pointer.Position,
						moveDelta,
						scrollDelta,
						true,
						state,
						commands,
						events,
						parents,
						allNodes))
				{
					state.HasActivePointerElement = true;
					state.ActivePointerElementId = targetKey;
				}
				else
				{
					state.HasActivePointerElement = false;
					state.ActivePointerElementId = 0;
				}
			}
			else
			{
				state.HasActivePointerElement = false;
				state.ActivePointerElementId = 0;
			}
		}

		// Pointer up
		if (!isPrimaryDown && wasPrimaryDown)
		{
			uint targetKey = 0;
			if (state.HasActivePointerElement)
			{
				targetKey = state.ActivePointerElementId;
				state.HasActivePointerElement = false;
				state.ActivePointerElementId = 0;
			}

			if (targetKey == 0)
			{
				targetKey = FindTopElementKey(currentReadOnly, state);
			}

			if (targetKey != 0)
			{
				DispatchPointerEventForKey(
					UiPointerEventType.PointerUp,
					targetKey,
					pointer.Position,
					moveDelta,
					scrollDelta,
					true,  // isPrimaryButton should be true for left mouse button
					state,
					commands,
					events,
					parents,
					allNodes);
			}
			state.HasActivePointerElement = false;
			state.ActivePointerElementId = 0;
		}

		// Pointer move
		if (moveDelta != Vector2.Zero)
		{
			var targetKey = FindTopElementKey(currentReadOnly, state);
			if (targetKey != 0)
			{
				DispatchPointerEventForKey(
					UiPointerEventType.PointerMove,
					targetKey,
					pointer.Position,
					moveDelta,
					Vector2.Zero,
					isPrimaryDown,
					state,
					commands,
					events,
					parents,
					allNodes);
			}
		}

		// Pointer scroll
		if (scrollDelta != Vector2.Zero)
		{
			var targetKey = FindTopElementKey(currentReadOnly, state);
			if (targetKey != 0)
			{
				DispatchPointerEventForKey(
					UiPointerEventType.PointerScroll,
					targetKey,
					pointer.Position,
					Vector2.Zero,
					scrollDelta,
					isPrimaryDown,
					state,
					commands,
					events,
					parents,
					allNodes);

				// Request layout to apply scroll offset changes
			}
		}

		pointer.ResetFrame();
	}

	private static bool Contains(ReadOnlySpan<uint> span, uint value)
	{
		for (var i = 0; i < span.Length; ++i)
		{
			if (span[i] == value)
				return true;
		}

		return false;
	}

	private static uint FindTopElementKey(ReadOnlySpan<uint> keys, ClayUiState state)
	{
		// Clay.GetPointerOverIds() returns elements from bottom to top
		// The last element is the topmost one, which is what we want for interactions
		if (keys.Length == 0)
			return 0;

		// Start from the topmost element (last in array)
		for (var i = keys.Length - 1; i >= 0; --i)
		{
			var key = keys[i];
			if (key == 0)
				continue;

			// O(1) lookup using the element ID → entity ID map built during layout
			if (state.ElementToEntityMap.ContainsKey(key))
				return key;
		}

		return 0;
	}

	private static bool DispatchPointerEventForKey(
		UiPointerEventType type,
		uint elementKey,
		Vector2 position,
		Vector2 moveDelta,
		Vector2 scrollDelta,
		bool isPrimary,
		ClayUiState state,
		Commands commands,
		EventWriter<UiPointerEvent> events,
		Query<Data<Parent>> parents,
		Query<Data<UiNode>> allNodes)
	{
		// O(1) lookup using the element ID → entity ID map built during layout
		if (!state.ElementToEntityMap.TryGetValue(elementKey, out var entityId))
			return false;

		// Dispatch event to the found entity
		return PropagatePointerEvent(
			type,
			elementKey,
			entityId,
			position,
			moveDelta,
			scrollDelta,
			isPrimary,
			state,
			commands,
			events,
			parents);
	}

	private static bool PropagatePointerEvent(
		UiPointerEventType type,
		uint elementKey,
		ulong targetEntity,
		Vector2 position,
		Vector2 moveDelta,
		Vector2 scrollDelta,
		bool isPrimary,
		ClayUiState state,
		Commands commands,
		EventWriter<UiPointerEvent> events,
		Query<Data<Parent>> parents)
	{
		if (targetEntity == 0)
			return false;

		var current = targetEntity;
		var depth = 0;
		var dispatched = false;

		while (current != 0 && depth++ < 256)
		{
			var pointerEvent = new UiPointerEvent(
				type,
				targetEntity,
				current,
				elementKey,
				position,
				moveDelta,
				scrollDelta,
				isPrimary);
			events.Send(pointerEvent);

			// Emit entity-specific trigger so entity observers can receive it
			commands.Entity(current).EmitTrigger(new UiPointerTrigger(pointerEvent));

			dispatched = true; if (!parents.Contains(current))
				break;

			var parentData = parents.Get(current);
			parentData.Deconstruct(out _, out var parentPtr);
			var parentId = parentPtr.Ref.Id;

			if (parentId == 0 || parentId == current)
				break;

			current = parentId;
		}

		return dispatched;
	}

	/// <summary>
	/// Checks if a pointer position is within all ancestor clipping bounds
	/// </summary>
	private static unsafe bool IsPointerWithinClipBounds(Clay_ElementId elementId, Vector2 pointerPosition)
	{
		// Check if this specific element has a scroll container (clip) config
		var scrollData = Clay.GetScrollContainerData(elementId);
		if (scrollData.found)
		{
			// This element has clipping - get its bounding box
			var elementData = Clay.GetElementData(elementId);
			if (elementData.found)
			{
				var bounds = elementData.boundingBox;

				if (pointerPosition.X < bounds.x ||
					pointerPosition.X > bounds.x + bounds.width ||
					pointerPosition.Y < bounds.y ||
					pointerPosition.Y > bounds.y + bounds.height)
				{
					// Pointer is outside this clipping region
					return false;
				}
			}
		}

		// Note: This checks each element's own clip bounds.
		// Clay's GetPointerOverIds returns all elements in hierarchy order,
		// so ancestor clips will be checked before descendants.

		return true;
	}
}

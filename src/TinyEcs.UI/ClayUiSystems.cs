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
		Query<Data<UiNodeParent>> desiredParents,
		Query<Data<Parent>> currentParents,
		Query<Data<Children>> childrenLists,
		Query<Data<FloatingWindowLinks>> windowLinks)
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
				// Keep the window's own parts (title bar, scroll container, content area) attached where they are.
				if (links.ContentAreaId != 0)
				{
					var childId = entityId;
					if (childId != links.ContentAreaId &&
						childId != links.TitleBarId &&
						childId != links.ScrollContainerId)
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
			uiState.Value.RequestLayoutPass();
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
			if (pointerChanged)
				state.RequestLayoutPass();
			pointer.ResetFrame();
			return;
		}

		Clay.SetCurrentContext(state.Context);

		if (pointerChanged)
		{
			Clay.SetPointerState(pointer.Position, pointer.PrimaryDown);

			// Only apply drag-based scrolling from MoveDelta when explicitly enabled;
			// always pass ScrollDelta from wheel/touchpad.
			var scrollInput = scrollDelta;
			if (pointer.EnableDragScrolling)
				scrollInput += moveDelta;

			Clay.UpdateScrollContainers(pointer.EnableDragScrolling, scrollInput, pointer.DeltaTime);
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

		var previousCount = state.GetHoveredElementCount();
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
			state.CopyHoveredElementIds(previousKeys);
		}

		ReadOnlySpan<uint> previousReadOnly = previousKeys;
		ReadOnlySpan<uint> currentReadOnly = currentKeys;

		state.BeginHoverUpdate();
		for (var i = 0; i < currentReadOnly.Length; ++i)
		{
			state.AddHoveredElement(currentReadOnly[i]);
		}

		// Pointer exits
		for (var i = 0; i < previousReadOnly.Length; ++i)
		{
			var key = previousReadOnly[i];
			if (key == 0 || state.IsElementHovered(key))
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
			var targetKey = FindTopElementKey(currentReadOnly, allNodes);
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
					state.SetActivePointerTarget(targetKey);
				}
				else
				{
					state.SetActivePointerTarget(0);
				}
			}
			else
			{
				state.SetActivePointerTarget(0);
			}
		}

		// Pointer up
		if (!isPrimaryDown && wasPrimaryDown)
		{
			uint targetKey;
			if (!state.TryConsumeActivePointerTarget(out targetKey) || targetKey == 0)
			{
				targetKey = FindTopElementKey(currentReadOnly, allNodes);
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
			state.SetActivePointerTarget(0);
		}

		// Pointer move
		if (moveDelta != Vector2.Zero)
		{
			var targetKey = FindTopElementKey(currentReadOnly, allNodes);
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
			var targetKey = FindTopElementKey(currentReadOnly, allNodes);
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
			}
		}

		pointer.ResetFrame();

		if (pointerChanged)
		{
			state.RequestLayoutPass();
		}
	}

	public static void RequestLayoutOnNodeChange(ResMut<ClayUiState> uiState, Query<Data<UiNode>, Filter<Changed<UiNode>>> changedNodes)
	{
		foreach (var _ in changedNodes)
		{
			uiState.Value.RequestLayoutPass();
			return;
		}
	}

	public static void RequestLayoutOnTextChange(ResMut<ClayUiState> uiState, Query<Data<UiText>, Filter<Changed<UiText>>> changedTexts)
	{
		foreach (var _ in changedTexts)
		{
			uiState.Value.RequestLayoutPass();
			return;
		}
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

	private static uint FindTopElementKey(ReadOnlySpan<uint> keys, Query<Data<UiNode>> allNodes)
	{
		for (var i = keys.Length - 1; i >= 0; --i)
		{
			var key = keys[i];
			if (key == 0)
				continue;

			// Check if any entity has this Clay element ID
			foreach (var (entityId, nodePtr) in allNodes)
			{
				ref var node = ref nodePtr.Ref;
				if (node.Declaration.id.id == key)
					return key;
			}
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
		// Find all entities with this Clay element ID
		var dispatched = false;
		foreach (var (entityIdPtr, nodePtr) in allNodes)
		{
			ref var node = ref nodePtr.Ref;
			if (node.Declaration.id.id == elementKey)
			{
				var entityId = entityIdPtr.Ref;
				dispatched |= PropagatePointerEvent(
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
		}

		return dispatched;
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
			commands.EmitTrigger(new UiPointerTrigger(pointerEvent));
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
}

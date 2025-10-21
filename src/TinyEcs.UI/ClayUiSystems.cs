using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;

namespace TinyEcs.UI;

internal static class ClayUiSystems
{
	public static void SyncUiHierarchy(ResMut<ClayUiState> uiState, Query<Data<UiNodeParent>> parents)
	{
		var layoutDirty = false;
		var world = uiState.Value.World;

		foreach ((PtrRO<ulong> entityPtr, Ptr<UiNodeParent> desiredPtr) in parents)
		{
			var entityId = entityPtr.Ref;
			ref var desired = ref desiredPtr.Ref;

			var hasParent = world.Has<Parent>(entityId);
			var currentParent = hasParent ? world.Get<Parent>(entityId).Id : 0;
			var requiresReorder = desired.Index >= 0;

			var needsUpdate = desired.Parent == 0
				? currentParent != 0
				: currentParent != desired.Parent || requiresReorder;

			if (!needsUpdate)
				continue;

			if (currentParent != 0)
			{
				world.RemoveChild(entityId);
			}

			if (desired.Parent != 0)
			{
				world.AddChild(desired.Parent, entityId, desired.Index);
			}

			layoutDirty = true;
		}

		if (layoutDirty)
		{
			uiState.Value.RequestLayoutPass();
		}
	}

	public static unsafe void ApplyPointerInput(
		ResMut<ClayPointerState> pointerState,
		ResMut<ClayUiState> uiState,
		EventWriter<UiPointerEvent> events,
		Query<Data<Parent>> parents)
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
			Clay.UpdateScrollContainers(pointer.EnableDragScrolling, pointer.ScrollDelta + pointer.MoveDelta, pointer.DeltaTime);
		}

		var hoveredIds = Clay.GetPointerOverIds();
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
				events,
				parents);
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
				events,
				parents);
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
						events,
						parents))
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
					false,
					state,
					events,
					parents);
			}

			state.SetActivePointerTarget(0);
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
					events,
					parents);
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
					events,
					parents);
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

	private static uint FindTopElementKey(ReadOnlySpan<uint> keys, ClayUiState state)
	{
		for (var i = keys.Length - 1; i >= 0; --i)
		{
			var key = keys[i];
			if (key != 0 && state.HasElementForKey(key))
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
		EventWriter<UiPointerEvent> events,
		Query<Data<Parent>> parents)
	{
		if (!state.TryGetEntitiesForElement(elementKey, out var entities) || entities.Count == 0)
			return false;

		var dispatched = false;
		for (var i = 0; i < entities.Count; ++i)
		{
			var entityId = entities[i];
			dispatched |= PropagatePointerEvent(
				type,
				elementKey,
				entityId,
				position,
				moveDelta,
				scrollDelta,
				isPrimary,
				state,
				events,
				parents);
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
			state.World.EmitTrigger(new UiPointerTrigger(pointerEvent));

			dispatched = true;

			if (!parents.Contains(current))
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

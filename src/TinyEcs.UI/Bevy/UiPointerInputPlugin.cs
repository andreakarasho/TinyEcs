using System;
using System.Numerics;
using TinyEcs.Bevy;

namespace TinyEcs.UI.Bevy;

/// <summary>
/// Resource that stores the current pointer state.
/// Update this from your renderer-specific input system (Raylib, SDL, etc.).
/// </summary>
public class PointerInputState
{
	/// <summary>Current pointer position in screen coordinates</summary>
	public Vector2 Position { get; set; }

	/// <summary>Whether the primary button (left mouse/touch) is currently down</summary>
	public bool IsPrimaryButtonDown { get; set; }

	/// <summary>Whether the primary button was just pressed this frame</summary>
	public bool IsPrimaryButtonPressed { get; set; }

	/// <summary>Whether the primary button was just released this frame</summary>
	public bool IsPrimaryButtonReleased { get; set; }

	/// <summary>Scroll wheel delta this frame (Y component)</summary>
	public Vector2 ScrollDelta { get; set; }

	public PointerInputState()
	{
		Position = Vector2.Zero;
		IsPrimaryButtonDown = false;
		IsPrimaryButtonPressed = false;
		IsPrimaryButtonReleased = false;
		ScrollDelta = Vector2.Zero;
	}
}

/// <summary>
/// Plugin that performs hit testing and emits UiPointerTrigger events for UI elements.
/// Renderer-agnostic - reads pointer state from PointerInputState resource.
///
/// Usage:
/// 1. Add this plugin to your app
/// 2. Create a system in your renderer plugin that updates PointerInputState resource each frame
/// 3. This plugin will automatically perform hit testing and emit pointer events
/// </summary>
public struct UiPointerInputPlugin : IPlugin
{
	/// <summary>
	/// Stage where pointer input should be processed (typically Update, before widget logic).
	/// </summary>
	public Stage InputStage { get; set; }

	public UiPointerInputPlugin()
	{
		InputStage = Stage.Update;
	}

	public readonly void Build(App app)
	{
		// Register the pointer state resource
		app.AddResource(new PointerInputState());

		// System to perform hit testing and emit pointer events
		app.AddSystem((
			Res<PointerInputState> pointerInput,
			Commands commands,
			Local<PointerTrackingState> trackingState,
			Res<UiStack> uiStack,
			Query<Data<Interactive, ComputedLayout>> interactiveQuery,
			Query<Data<Parent>> parentQuery,
			Query<Data<Scrollable, ComputedLayout>> scrollableQuery) =>
		{
			ProcessPointerInputSystem(pointerInput, commands, trackingState, uiStack, interactiveQuery, parentQuery, scrollableQuery);
		})
		.InStage(InputStage)
		.Label("ui:pointer:hit-test")
		.After("ui:stack:update") // Run after UI stack is updated
		.Build();
	}

	/// <summary>
	/// Internal state for tracking hover and calculating deltas between frames.
	/// </summary>
	private class PointerTrackingState
	{
		public Vector2 LastPosition;
		public ulong? HoveredEntity; // Track which entity is currently hovered

		public PointerTrackingState()
		{
			LastPosition = Vector2.Zero;
			HoveredEntity = null;
		}
	}

	/// <summary>
	/// System method for pointer input processing.
	/// </summary>
	private static void ProcessPointerInputSystem(
		Res<PointerInputState> pointerInput,
		Commands commands,
		Local<PointerTrackingState> trackingState,
		Res<UiStack> uiStack,
		Query<Data<Interactive, ComputedLayout>> interactiveQuery,
		Query<Data<Parent>> parentQuery,
		Query<Data<Scrollable, ComputedLayout>> scrollableQuery)
	{
		if (trackingState.Value == null)
			trackingState.Value = new PointerTrackingState();

		var tracking = trackingState.Value;
		var input = pointerInput.Value;

		// Calculate position delta
		var positionDelta = input.Position - tracking.LastPosition;

		// Perform hit testing to find the topmost interactive element under the cursor
		// Iterate from back to front (last entry is topmost)
		ulong? hitEntity = null;

		// Iterate the UI stack in reverse order (topmost first)
		for (int i = uiStack.Value.Count - 1; i >= 0; i--)
		{
			var entry = uiStack.Value.Entries[i];
			var entityId = entry.EntityId;

			// Check if this entity is interactive and has layout using query
			if (!interactiveQuery.Contains(entityId))
				continue;

			var (_, layout) = interactiveQuery.Get(entityId);

			// Check if pointer is inside this element's bounds
			if (!IsPointInRect(input.Position, layout.Ref.X, layout.Ref.Y, layout.Ref.Width, layout.Ref.Height))
				continue;

			// Check if the point is visible (not clipped by any scrollable parent)
			if (!IsPointVisible(input.Position, entityId, parentQuery, scrollableQuery))
				continue;

			// Found the topmost interactive element under cursor
			hitEntity = entityId;
			break; // Stop at the first (topmost) hit
		}

		// Handle hover enter/exit
		if (hitEntity != tracking.HoveredEntity)
		{
			// Exit event for previously hovered entity
			if (tracking.HoveredEntity.HasValue)
			{
				EmitPointerEvent(
					commands,
					tracking.HoveredEntity.Value,
					UiPointerEventType.PointerExit,
					input.Position,
					positionDelta,
					input.ScrollDelta,
					input.IsPrimaryButtonDown);
			}

			// Enter event for newly hovered entity
			if (hitEntity.HasValue)
			{
				EmitPointerEvent(
					commands,
					hitEntity.Value,
					UiPointerEventType.PointerEnter,
					input.Position,
					positionDelta,
					input.ScrollDelta,
					input.IsPrimaryButtonDown);
			}

			tracking.HoveredEntity = hitEntity;
		}

		// If hovering over an element, emit events
		if (hitEntity.HasValue)
		{
			var entityId = hitEntity.Value;

			// Primary button down event
			if (input.IsPrimaryButtonPressed)
			{
				EmitPointerEvent(
					commands,
					entityId,
					UiPointerEventType.PointerDown,
					input.Position,
					positionDelta,
					input.ScrollDelta,
					true);
			}

			// Primary button up event
			if (input.IsPrimaryButtonReleased)
			{
				EmitPointerEvent(
					commands,
					entityId,
					UiPointerEventType.PointerUp,
					input.Position,
					positionDelta,
					input.ScrollDelta,
					false);
			}

			// Pointer move event (only if pointer actually moved)
			if (positionDelta.LengthSquared() > 0.001f)
			{
				EmitPointerEvent(
					commands,
					entityId,
					UiPointerEventType.PointerMove,
					input.Position,
					positionDelta,
					input.ScrollDelta,
					input.IsPrimaryButtonDown);
			}

			// Scroll event
			if (Math.Abs(input.ScrollDelta.Y) > 0.001f)
			{
				EmitPointerEvent(
					commands,
					entityId,
					UiPointerEventType.PointerScroll,
					input.Position,
					positionDelta,
					input.ScrollDelta,
					input.IsPrimaryButtonDown);
			}
		}

		// Update tracking state for next frame
		tracking.LastPosition = input.Position;
	}

	/// <summary>
	/// Emits a UiPointerTrigger event for the specified entity.
	/// </summary>
	private static void EmitPointerEvent(
		Commands commands,
		ulong entityId,
		UiPointerEventType eventType,
		Vector2 position,
		Vector2 moveDelta,
		Vector2 scrollDelta,
		bool isPrimaryButton)
	{
		var pointerEvent = new UiPointerEvent(
			type: eventType,
			target: entityId,
			currentTarget: entityId,
			elementKey: (uint)entityId,
			position: position,
			moveDelta: moveDelta,
			scrollDelta: scrollDelta,
			isPrimaryButton: isPrimaryButton
		);

		var trigger = new UiPointerTrigger(pointerEvent, Propagate: false);

		// Emit the trigger on the entity
		commands.Entity(entityId).EmitTrigger(trigger);
	}

	/// <summary>
	/// Simple point-in-rectangle test for hit detection.
	/// </summary>
	private static bool IsPointInRect(Vector2 point, float x, float y, float width, float height)
	{
		return point.X >= x &&
		       point.X <= x + width &&
		       point.Y >= y &&
		       point.Y <= y + height;
	}

	/// <summary>
	/// Checks if a point is visible (not clipped) by walking up the hierarchy and checking scrollable parent bounds.
	/// </summary>
	private static bool IsPointVisible(
		Vector2 point,
		ulong entityId,
		Query<Data<Parent>> parentQuery,
		Query<Data<Scrollable, ComputedLayout>> scrollableQuery)
	{
		// Walk up the hierarchy
		var currentEntity = entityId;

		while (parentQuery.Contains(currentEntity))
		{
			var (_, parentComponent) = parentQuery.Get(currentEntity);
			var parentId = parentComponent.Ref.Id;

			// Check if parent is scrollable (has clipping)
			if (scrollableQuery.Contains(parentId))
			{
				var (_, parentLayout) = scrollableQuery.Get(parentId);

				// Check if point is within parent's clip bounds
				if (!IsPointInRect(point, parentLayout.Ref.X, parentLayout.Ref.Y, parentLayout.Ref.Width, parentLayout.Ref.Height))
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

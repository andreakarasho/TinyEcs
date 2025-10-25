using System;
using System.Collections.Generic;
using System.Numerics;
using TinyEcs.Bevy;

namespace TinyEcs.UI;

/// <summary>
/// Systems for Flexbox pointer input processing.
/// Parallel to ClayUiSystems.ApplyPointerInput but for Flexbox layout.
/// Reuses UiPointerEvent and UiPointerTrigger for event propagation.
/// </summary>
public static class FlexboxPointerSystems
{
    /// <summary>
    /// Processes pointer input and fires UiPointerEvent events based on Flexbox computed layouts.
    /// Runs in PreUpdate stage, after hierarchy sync and before layout.
    /// </summary>
    public static void ApplyPointerInput(
        ResMut<FlexboxPointerState> pointerState,
        ResMut<FlexboxUiState> uiState,
        EventWriter<UiPointerEvent> events,
        Commands commands,
        Query<Data<Parent>> parents,
        Query<Data<FlexboxInteractive>> interactives)
    {
        ref var pointer = ref pointerState.Value;
        ref var state = ref uiState.Value;

        // Perform hit testing to find hovered elements
        var currentHovered = new HashSet<uint>();
        HitTestPointer(ref state, pointer.Position, currentHovered);

        // Detect hover changes (enter/exit)
        DispatchHoverEvents(
            ref state,
            state.HoveredElementIds,
            currentHovered,
            pointer.Position,
            events,
            commands,
            parents);

        // Dispatch pointer down/up events
        if (pointer.IsPrimaryPressed)
        {
            foreach (var elementId in currentHovered)
            {
                DispatchPointerEvent(
                    ref state,
                    UiPointerEventType.PointerDown,
                    elementId,
                    pointer.Position,
                    true,
                    events,
                    commands,
                    parents);
            }
        }
        else if (pointer.IsPrimaryReleased)
        {
            foreach (var elementId in currentHovered)
            {
                DispatchPointerEvent(
                    ref state,
                    UiPointerEventType.PointerUp,
                    elementId,
                    pointer.Position,
                    true,
                    events,
                    commands,
                    parents);
            }
        }

        // Dispatch pointer move events
        if (pointer.MoveDelta != Vector2.Zero)
        {
            foreach (var elementId in currentHovered)
            {
                DispatchPointerEvent(
                    ref state,
                    UiPointerEventType.PointerMove,
                    elementId,
                    pointer.Position,
                    pointer.PrimaryDown,
                    events,
                    commands,
                    parents);
            }
        }

        // Dispatch scroll events
        if (pointer.ScrollDelta != Vector2.Zero)
        {
            foreach (var elementId in currentHovered)
            {
                DispatchPointerEvent(
                    ref state,
                    UiPointerEventType.PointerScroll,
                    elementId,
                    pointer.Position,
                    false,
                    events,
                    commands,
                    parents,
                    pointer.ScrollDelta);
            }
        }

        // Update hovered elements for next frame
        state.HoveredElementIds.Clear();
        foreach (var id in currentHovered)
            state.HoveredElementIds.Add(id);

        // Mark frame as processed
        pointer.EndFrame();
    }

    private static void HitTestPointer(
        ref FlexboxUiState state,
        Vector2 pointerPos,
        HashSet<uint> outHovered)
    {
        // Test all entities from back to front (reverse order for top-most first)
        // For simplicity, we test all entities; a proper implementation would use Z-order
        foreach (var (entityId, layout) in state.EntityToLayout)
        {
            if (IsPointInside(pointerPos, layout))
            {
                outHovered.Add(layout.ElementId);
            }
        }
    }

    private static bool IsPointInside(Vector2 point, ComputedLayout layout)
    {
        return point.X >= layout.Position.X &&
               point.X <= layout.Position.X + layout.Size.X &&
               point.Y >= layout.Position.Y &&
               point.Y <= layout.Position.Y + layout.Size.Y;
    }

    private static void DispatchHoverEvents(
        ref FlexboxUiState state,
        HashSet<uint> previousHovered,
        HashSet<uint> currentHovered,
        Vector2 pointerPos,
        EventWriter<UiPointerEvent> events,
        Commands commands,
        Query<Data<Parent>> parents)
    {
        // Dispatch PointerExit for elements that were hovered but are no longer
        foreach (var elementId in previousHovered)
        {
            if (!currentHovered.Contains(elementId))
            {
                DispatchPointerEvent(
                    ref state,
                    UiPointerEventType.PointerExit,
                    elementId,
                    pointerPos,
                    false,
                    events,
                    commands,
                    parents);
            }
        }

        // Dispatch PointerEnter for newly hovered elements
        foreach (var elementId in currentHovered)
        {
            if (!previousHovered.Contains(elementId))
            {
                DispatchPointerEvent(
                    ref state,
                    UiPointerEventType.PointerEnter,
                    elementId,
                    pointerPos,
                    false,
                    events,
                    commands,
                    parents);
            }
        }
    }

    private static void DispatchPointerEvent(
        ref FlexboxUiState state,
        UiPointerEventType eventType,
        uint elementId,
        Vector2 position,
        bool isPrimaryButton,
        EventWriter<UiPointerEvent> events,
        Commands commands,
        Query<Data<Parent>> parents,
        Vector2? scrollDelta = null)
    {
        if (!state.ElementToEntityMap.TryGetValue(elementId, out var targetEntity))
            return;

        // Propagate event up the parent chain
        PropagatePointerEvent(
            targetEntity,
            elementId,
            eventType,
            position,
            isPrimaryButton,
            events,
            commands,
            parents,
            scrollDelta);
    }

    private static void PropagatePointerEvent(
        ulong targetEntity,
        uint elementId,
        UiPointerEventType eventType,
        Vector2 position,
        bool isPrimaryButton,
        EventWriter<UiPointerEvent> events,
        Commands commands,
        Query<Data<Parent>> parents,
        Vector2? scrollDelta = null)
    {
        var current = targetEntity;

        while (current != 0)
        {
            // For Flexbox pointer events, we currently don't compute per-element move delta.
            // Pass Vector2.Zero for moveDelta; scrollDelta is provided when applicable.
            var evt = new UiPointerEvent(
                eventType,
                targetEntity,
                current,
                elementId,
                position,
                Vector2.Zero,
                scrollDelta ?? Vector2.Zero,
                isPrimaryButton);

            // Send as global event
            events.Send(evt);

            // Send as entity-specific trigger
            commands.Entity(current).EmitTrigger(new UiPointerTrigger(evt));

            // Walk up parent chain
            if (!parents.Contains(current))
                break;

            var parentData = parents.Get(current);
            parentData.Deconstruct(out _, out var parentPtr);
            var parentId = parentPtr.Ref.Id;
            if (parentId == 0 || parentId == current)
                break;

            current = parentId;
        }
    }
}

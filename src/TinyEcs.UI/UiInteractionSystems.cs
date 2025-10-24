using System;
using System.Collections.Generic;
using TinyEcs.Bevy;

namespace TinyEcs.UI;

/// <summary>
/// Systems for managing UI interaction state, similar to Bevy's interaction detection.
/// These systems update Interaction components based on pointer events, enabling
/// reactive observer patterns for widget behavior.
/// </summary>
public static class UiInteractionSystems
{
	/// <summary>
	/// Updates Interaction components based on UiPointerEvents.
	/// This is the core system that makes widgets reactive - observers can then
	/// react to Changed&lt;Interaction&gt; to update visuals or trigger behavior.
	///
	/// Runs in Stage.PreUpdate after pointer input processing.
	/// </summary>
	public static void UpdateInteractionState(
		EventReader<UiPointerEvent> events,
		Query<Data<Interaction, Interactive>> interactives,
		Local<HashSet<ulong>> touchedEntities,
		Commands commands)
	{
		// Track which entities had events this frame
		touchedEntities.Value!.Clear();

		// Process all pointer events and update Interaction states
		foreach (var evt in events.Read())
		{
			// Use CurrentTarget (entity in propagation chain) instead of Target (original clicked element)
			// This allows parent containers to react even when clicking on their children
			var targetId = evt.CurrentTarget;

			// Skip if target doesn't exist or isn't interactive
			if (!interactives.Contains(targetId))
				continue;

			touchedEntities.Value.Add(targetId);

			var data = interactives.Get(targetId);
			data.Deconstruct(out var interactionPtr, out _);
			var currentInteraction = interactionPtr.Ref;
			Interaction newInteraction = currentInteraction;

			switch (evt.Type)
			{
				case UiPointerEventType.PointerEnter:
					// Only transition to hovered if not already pressed
					if (currentInteraction != Interaction.Pressed)
						newInteraction = Interaction.Hovered;
					break;

				case UiPointerEventType.PointerExit:
					// Lost hover - go back to None unless still pressed
					if (currentInteraction != Interaction.Pressed)
						newInteraction = Interaction.None;
					break;

				case UiPointerEventType.PointerDown:
					if (evt.IsPrimaryButton)
						newInteraction = Interaction.Pressed;
					break;

				case UiPointerEventType.PointerUp:
					// When releasing, check if still hovered to return to Hovered state
					// (This will be corrected by the next PointerEnter/Exit event if needed)
					newInteraction = Interaction.Hovered;
					break;
			}

			// Only update if the interaction actually changed (triggers change detection)
			if (newInteraction != currentInteraction)
			{
				commands.Entity(targetId).Insert(newInteraction);
			}
		}

		// Reset any interactive elements that didn't receive events this frame
		// This handles cases where pointer left an element without triggering PointerExit
		foreach (var (entityId, interactionPtr, _) in interactives)
		{
			var id = entityId.Ref;
			if (!touchedEntities.Value.Contains(id))
			{
				var currentInteraction = interactionPtr.Ref;
				// Only reset if not in a sticky state
				if (currentInteraction == Interaction.Hovered)
				{
					// This element is still marked as hovered but didn't get events
					// We'll let it stay hovered - PointerExit will explicitly clear it
				}
			}
		}
	}

	// NOTE: The following functions were removed as unused:
	// - DetectClicks<TMarker> - Generic click detection (no consumers)
	// - UpdateFocus - Focus management (no text inputs implemented yet)
	// - ComputeZIndices - Z-index computation (renderer doesn't use ComputedZIndex component)
	// - ProcessNodeRecursive - Helper for ComputeZIndices
	//
	// These can be restored from git history if needed in the future.

	/// <summary>
	/// Plugin to register all interaction systems with the app.
	/// </summary>
	public sealed class UiInteractionPlugin : IPlugin
	{
		public void Build(App app)
		{
			// Register interaction update system (runs after pointer input)
			// This is CRITICAL for button hover/press visual states
			app.AddSystem((EventReader<UiPointerEvent> events, Query<Data<Interaction, Interactive>> interactives, Local<HashSet<ulong>> touchedEntities, Commands commands) =>
				UpdateInteractionState(events, interactives, touchedEntities, commands))
				.InStage(Stage.PreUpdate)
				.Label("ui:interaction:update")
				.After("ui:clay:pointer")
				.Build();

			// NOTE: Focus management and Z-index systems removed as they were unused
			// - UpdateFocus: No text inputs or keyboard navigation implemented yet
			// - ComputeZIndices: No renderer reads ComputedZIndex component
			// These can be re-added when needed (infrastructure exists in UiInteraction.cs)
		}
	}
}

public static class UiInteractionAppExtensions
{
	public static App AddUiInteraction(this App app)
	{
		app.AddPlugin(new UiInteractionSystems.UiInteractionPlugin());
		return app;
	}
}

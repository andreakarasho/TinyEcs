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
		Commands commands)
	{
		// Track which entities had events this frame
		var touchedEntities = new HashSet<ulong>();

		// Process all pointer events and update Interaction states
		foreach (var evt in events.Read())
		{
			// Use CurrentTarget (entity in propagation chain) instead of Target (original clicked element)
			// This allows parent containers to react even when clicking on their children
			var targetId = evt.CurrentTarget;

			// Skip if target doesn't exist or isn't interactive
			if (!interactives.Contains(targetId))
				continue;

			touchedEntities.Add(targetId);

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
			if (!touchedEntities.Contains(id))
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

	/// <summary>
	/// Detects click events (pointer down + up on same element) and emits OnClick triggers.
	/// This enables observers to react to clicks without manually tracking pointer state.
	///
	/// Runs in Stage.Update after interaction state is updated.
	/// </summary>
	public static void DetectClicks<TMarker>(
		EventReader<UiPointerEvent> events,
		Query<Data<Interaction, TMarker>> clickables,
		Commands commands)
		where TMarker : struct
	{
		// Track pointer down targets
		ulong pointerDownTarget = 0;

		foreach (var evt in events.Read())
		{
			if (evt.Type == UiPointerEventType.PointerDown && evt.IsPrimaryButton)
			{
				pointerDownTarget = evt.Target;
			}
			else if (evt.Type == UiPointerEventType.PointerUp && evt.IsPrimaryButton)
			{
				// If released on the same target we pressed on, it's a click
				if (pointerDownTarget == evt.Target && clickables.Contains(evt.Target))
				{
					commands.Entity(evt.Target).EmitTrigger(new ClickEvent<TMarker>());
				}
				pointerDownTarget = 0;
			}
		}
	}

	/// <summary>
	/// Manages focus state based on pointer interactions and focus requests.
	/// Updates the FocusManager resource and Focused components.
	///
	/// Runs in Stage.PreUpdate after interaction state updates.
	/// </summary>
	public static void UpdateFocus(
		EventReader<UiPointerEvent> events,
		ResMut<FocusManager> focusManager,
		Query<Data<Interactive>> interactives,
		Query<Data<Focused>> focused,
		Commands commands)
	{
		ref var manager = ref focusManager.Value;

		// Process explicit focus requests first
		if (manager.RequestFocusEntity != 0)
		{
			var requestedEntity = manager.RequestFocusEntity;
			var source = manager.RequestFocusSource;

			// Clear previous focus
			if (manager.FocusedEntity != 0 && manager.FocusedEntity != requestedEntity)
			{
				commands.Entity(manager.FocusedEntity).Remove<Focused>();
				commands.Entity(manager.FocusedEntity).EmitTrigger(new FocusLostEvent());
			}

			// Set new focus
			manager.PreviousFocusedEntity = manager.FocusedEntity;
			manager.FocusedEntity = requestedEntity;
			commands.Entity(requestedEntity).Insert(new Focused { Source = source });
			commands.Entity(requestedEntity).EmitTrigger(new FocusGainedEvent(source));         // Clear request
			manager.RequestFocusEntity = 0;
			return;
		}

		// Handle focus via pointer clicks
		foreach (var evt in events.Read())
		{
			if (evt.Type == UiPointerEventType.PointerDown && evt.IsPrimaryButton)
			{
				var targetId = evt.Target;

				// Check if target is focusable
				if (interactives.Contains(targetId))
				{
					var data = interactives.Get(targetId);
					data.Deconstruct(out _, out var interactivePtr);
					var interactive = interactivePtr.Ref;

					if (interactive.IsFocusable && manager.FocusedEntity != targetId)
					{
						// Clear previous focus
						if (manager.FocusedEntity != 0)
						{
							commands.Entity(manager.FocusedEntity).Remove<Focused>();
							commands.Entity(manager.FocusedEntity).EmitTrigger(new FocusLostEvent());
						}

						// Set new focus
						manager.PreviousFocusedEntity = manager.FocusedEntity;
						manager.FocusedEntity = targetId;
						commands.Entity(targetId).Insert(new Focused { Source = FocusSource.Pointer });
						commands.Entity(targetId).EmitTrigger(new FocusGainedEvent(FocusSource.Pointer));
					}
				}
			}
		}
	}

	/// <summary>
	/// Computes Z-index values for UI elements via depth-first hierarchy traversal.
	/// Similar to Bevy's ui_z_system. Elements are assigned rendering order based on:
	/// - Hierarchical depth (parents before children)
	/// - Sibling order (later siblings on top)
	/// - Manual ZIndex component overrides
	///
	/// Runs in Stage.Update before rendering.
	/// </summary>
	public static void ComputeZIndices(
		Query<Data<UiNode>, Filter<Without<Parent>>> roots,
		Query<Data<UiNode>> allNodes,
		Query<Data<Children>> childrenQuery,
		Query<Data<ZIndex>> zIndexQuery,
		Commands commands,
		Local<List<(ulong, float)>> workList)
	{
		if (workList.Value == null)
		{
			workList.Value = new List<(ulong, float)>();
		}

		workList.Value.Clear();
		float currentDepth = 0f;

		// Process all root nodes (no parent)
		foreach (var (rootId, rootNode) in roots)
		{
			ProcessNodeRecursive(rootId.Ref, ref currentDepth, workList.Value, childrenQuery, zIndexQuery);
		}

		// Apply computed z-indices
		foreach (var (entityId, zValue) in workList.Value)
		{
			commands.Entity(entityId).Insert(new ComputedZIndex { Value = zValue });
		}
	}

	private static void ProcessNodeRecursive(
		ulong nodeId,
		ref float currentDepth,
		List<(ulong, float)> output,
		Query<Data<Children>> childrenQuery,
		Query<Data<ZIndex>> zIndexQuery)
	{
		// Compute this node's depth
		float nodeDepth = currentDepth;

		// Apply manual Z-index offset if present
		if (zIndexQuery.Contains(nodeId))
		{
			var zIndexData = zIndexQuery.Get(nodeId);
			zIndexData.Deconstruct(out var zIndexPtr);
			var zIndex = zIndexPtr.Ref;

			if (zIndex.Global.HasValue)
			{
				// Global z-index completely overrides hierarchy
				nodeDepth = zIndex.Global.Value * 1000f;
			}
			else
			{
				// Local z-index adds offset within current hierarchy level
				nodeDepth += zIndex.Local * 0.1f;
			}
		}

		output.Add((nodeId, nodeDepth));

		// Process children
		if (childrenQuery.Contains(nodeId))
		{
			var childrenData = childrenQuery.Get(nodeId);
			childrenData.Deconstruct(out var childrenPtr);
			var children = childrenPtr.Ref;

			// Increment depth for children
			currentDepth += 1f;

			// Process each child in order
			int i = 0;
			foreach (var childId in children)
			{
				// Later siblings get slightly higher depth (render on top)
				var siblingOffset = i * 0.01f;
				var childDepth = currentDepth + siblingOffset;
				var savedDepth = currentDepth;
				currentDepth = childDepth;
				ProcessNodeRecursive(childId, ref currentDepth, output, childrenQuery, zIndexQuery);
				currentDepth = savedDepth;
				i++;
			}

			// Restore depth after processing children
			currentDepth -= 1f;
		}
	}

	/// <summary>
	/// Plugin to register all interaction systems with the app.
	/// </summary>
	public sealed class UiInteractionPlugin : IPlugin
	{
		public void Build(App app)
		{
			// Create focus manager resource
			app.GetWorld().AddResource(new FocusManager
			{
				FocusedEntity = 0,
				PreviousFocusedEntity = 0,
				RequestFocusEntity = 0,
				RequestFocusSource = FocusSource.Programmatic
			});

			// Register interaction update system (runs after pointer input)
			app.AddSystem((EventReader<UiPointerEvent> events, Query<Data<Interaction, Interactive>> interactives, Commands commands) =>
				UpdateInteractionState(events, interactives, commands))
				.InStage(Stage.PreUpdate)
				.Label("ui:interaction:update")
				.After("ui:clay:pointer")
				.Build();

			// Register focus management system
			app.AddSystem((EventReader<UiPointerEvent> events, ResMut<FocusManager> focusManager, Query<Data<Interactive>> interactives, Query<Data<Focused>> focused, Commands commands) =>
				UpdateFocus(events, focusManager, interactives, focused, commands))
				.InStage(Stage.PreUpdate)
				.Label("ui:interaction:focus")
				.After("ui:interaction:update")
				.Build();

			// Register Z-index computation (runs before rendering)
			app.AddSystem((Query<Data<UiNode>, Filter<Without<Parent>>> roots, Query<Data<UiNode>> allNodes, Query<Data<Children>> childrenQuery, Query<Data<ZIndex>> zIndexQuery, Commands commands, Local<List<(ulong, float)>> workList) =>
				ComputeZIndices(roots, allNodes, childrenQuery, zIndexQuery, commands, workList))
				.InStage(Stage.Update)
				.Label("ui:interaction:compute-z")
				.After("ui:clay:layout")
				.Build();
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

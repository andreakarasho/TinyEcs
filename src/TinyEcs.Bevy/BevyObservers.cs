using System;
using System.Collections.Generic;
using TinyEcs;

namespace TinyEcs.Bevy;

// ============================================================================
// Observer Triggers - Events that observers can react to
// ============================================================================

public interface ITrigger
{
#if NET9_0_OR_GREATER
	static abstract void Register(TinyEcs.World world);
#else
	void Register(TinyEcs.World world);
#endif
}

/// <summary>
/// Marker interface for triggers that have an EntityId field.
/// Used for reflection-free entity ID extraction.
/// </summary>
public interface IEntityTrigger
{
	ulong EntityId { get; }
}

/// <summary>
/// Marker interface for triggers that can propagate up the parent hierarchy.
/// </summary>
public interface IPropagatingTrigger
{
	bool ShouldPropagate { get; }
}

/// <summary>
/// Trigger when a component is added to an entity
/// </summary>
public readonly record struct OnAdd<T>(ulong EntityId, T Component, bool ShouldPropagate = false) : ITrigger, IEntityTrigger, IPropagatingTrigger
	where T : struct
{
#if NET9_0_OR_GREATER
	public static void Register(TinyEcs.World world)
#else
	public readonly void Register(TinyEcs.World world)
#endif
		=> world.EnableObservers<T>();

	/// <summary>
	/// Returns a new trigger with propagation enabled.
	/// When propagating, the trigger will also fire on all parent entities up the hierarchy.
	/// </summary>
	public OnAdd<T> Propagate(bool propagate = true) => this with { ShouldPropagate = propagate };
}

/// <summary>
/// Trigger when a component is inserted/updated on an entity
/// </summary>
public readonly record struct OnInsert<T>(ulong EntityId, T Component, bool ShouldPropagate = false) : ITrigger, IEntityTrigger, IPropagatingTrigger
	where T : struct
{
#if NET9_0_OR_GREATER
	public static void Register(TinyEcs.World world)
#else
	public readonly void Register(TinyEcs.World world)
#endif
		=> world.EnableObservers<T>();

	/// <summary>
	/// Returns a new trigger with propagation enabled.
	/// When propagating, the trigger will also fire on all parent entities up the hierarchy.
	/// </summary>
	public OnInsert<T> Propagate(bool propagate = true) => this with { ShouldPropagate = propagate };
}

/// <summary>
/// Trigger when a component is removed from an entity
/// </summary>
public readonly record struct OnRemove<T>(ulong EntityId, T Component, bool ShouldPropagate = false) : ITrigger, IEntityTrigger, IPropagatingTrigger
	where T : struct
{
#if NET9_0_OR_GREATER
	public static void Register(TinyEcs.World world)
#else
	public readonly void Register(TinyEcs.World world)
#endif
		=> world.EnableObservers<T>();

	/// <summary>
	/// Returns a new trigger with propagation enabled.
	/// When propagating, the trigger will also fire on all parent entities up the hierarchy.
	/// </summary>
	public OnRemove<T> Propagate(bool propagate = true) => this with { ShouldPropagate = propagate };
}

/// <summary>
/// Trigger when an entity is spawned
/// </summary>
public readonly record struct OnSpawn(ulong EntityId, bool ShouldPropagate = false) : ITrigger, IEntityTrigger, IPropagatingTrigger
{
#if NET9_0_OR_GREATER
	public static void Register(TinyEcs.World world)
#else
	public readonly void Register(TinyEcs.World world)
#endif
	{
	}

	/// <summary>
	/// Returns a new trigger with propagation enabled.
	/// When propagating, the trigger will also fire on all parent entities up the hierarchy.
	/// </summary>
	public OnSpawn Propagate(bool propagate = true) => this with { ShouldPropagate = propagate };
}

/// <summary>
/// Trigger when an entity is despawned
/// </summary>
public readonly record struct OnDespawn(ulong EntityId, bool ShouldPropagate = false) : ITrigger, IEntityTrigger, IPropagatingTrigger
{
#if NET9_0_OR_GREATER
	public static void Register(TinyEcs.World world)
#else
	public readonly void Register(TinyEcs.World world)
#endif
	{
	}

	/// <summary>
	/// Returns a new trigger with propagation enabled.
	/// When propagating, the trigger will also fire on all parent entities up the hierarchy.
	/// </summary>
	public OnDespawn Propagate(bool propagate = true) => this with { ShouldPropagate = propagate };
}

/// <summary>
/// Trigger when a custom event is fired via Commands.Trigger.
/// </summary>
public readonly record struct On<TEvent>(TEvent Event) : ITrigger
	where TEvent : struct
{
#if NET9_0_OR_GREATER
	public static void Register(TinyEcs.World world)
#else
	public readonly void Register(TinyEcs.World world)
#endif
	{
		// Custom events don't require registration.
	}
}

// ============================================================================
// Observer System - React to entity/component changes
// ============================================================================

/// <summary>
/// Observer that reacts to triggers
/// </summary>
public interface IObserver
{
	void Execute(TinyEcs.World world, object trigger);
	Type TriggerType { get; }
}

/// <summary>
/// Component that stores entity-specific observers.
/// This component is automatically added to entities that have observers attached.
/// Note: The List is a reference type, so it persists correctly even though this is a struct.
/// </summary>
internal struct EntityObservers
{
	public List<IObserver>? Observers;

	public EntityObservers()
	{
		Observers = new List<IObserver>();
	}
}

/// <summary>
/// Typed observer for specific trigger
/// </summary>
public class Observer<TTrigger> : IObserver
{
	private readonly Action<TinyEcs.World, TTrigger> _callback;

	public Type TriggerType => typeof(TTrigger);

	public Observer(Action<TinyEcs.World, TTrigger> callback)
	{
		_callback = callback;
	}

	public void Execute(TinyEcs.World world, object trigger)
	{
		_callback(world, (TTrigger)trigger);
	}
}

// ============================================================================
// Component Handlers - Type-safe handlers for component changes (no reflection!)
// ============================================================================

/// <summary>
/// Handler for component change events
/// </summary>
internal interface IComponentHandler
{
	void HandleSet(TinyEcs.World world, ulong entityId);
	void HandleAdd(TinyEcs.World world, ulong entityId);
	void HandleUnset(TinyEcs.World world, ulong entityId);
}

/// <summary>
/// Typed component handler for specific component type
/// </summary>
internal class ComponentHandler<T> : IComponentHandler where T : struct
{
	// Store component ID statically to avoid lookups
	private static ulong? _componentId;

	public void HandleSet(TinyEcs.World world, ulong entityId)
	{
		if (!world.Has<T>(entityId))
			return;

		ref var component = ref world.Get<T>(entityId);
		world.EmitTrigger(new OnInsert<T>(entityId, component));
	}

	public void HandleAdd(TinyEcs.World world, ulong entityId)
	{
		if (!world.Has<T>(entityId))
			return;

		ref var component = ref world.Get<T>(entityId);
		world.EmitTrigger(new OnAdd<T>(entityId, component));
	}

	public void HandleUnset(TinyEcs.World world, ulong entityId)
	{
		ref var component = ref world.Get<T>(entityId);
		world.EmitTrigger(new OnRemove<T>(entityId, component));
	}

	public static void SetComponentId(ulong id)
	{
		_componentId = id;
	}

	public static ulong? GetComponentId() => _componentId;
}

// ============================================================================
// Observer Extensions
// ============================================================================

public static class ObserverExtensions
{
	private static readonly Dictionary<TinyEcs.World, ObserverState> _observerStates = new();

	private static ObserverState GetObserverState(this TinyEcs.World world)
	{
		if (!_observerStates.TryGetValue(world, out var state))
		{
			state = new ObserverState();
			state.MaxComponentEntityId = world.MaxComponentId;
			_observerStates[world] = state;
			RegisterWorldHooks(world, state);
		}

		return state;
	}

	private static void RegisterWorldHooks(TinyEcs.World world, ObserverState state)
	{
		if (state.HooksRegistered) return;

		state.HooksRegistered = true;

		// Hook into entity creation - automatically emit OnSpawn
		world.OnEntityCreated += (w, entityId) =>
		{
			if (!state.HooksEnabled) return;

			// Skip component type entities
			if (IsComponentEntity(state, entityId)) return;

			w.EmitTrigger(new OnSpawn(entityId));
		};

		// Hook into entity deletion - automatically emit OnDespawn
		world.OnEntityDeleted += (w, entityId) =>
		{
			if (!state.HooksEnabled) return;

			// Skip component type entities
			if (IsComponentEntity(state, entityId)) return;

			w.EmitTrigger(new OnDespawn(entityId));
		};

		// Hook into component set - queue for deferred processing
		// (OnComponentSet fires BEFORE the value is written, for both add and update)
		world.OnComponentSet += (w, entityId, componentInfo) =>
		{
			if (!state.HooksEnabled) return;

			// Skip component type entities
			if (IsComponentEntity(state, entityId)) return;

			if (componentInfo.Size > 0 && state.ComponentHandlers.TryGetValue(componentInfo.ID, out var handler))
			{
				// Queue for processing after Set() completes - this fires OnInsert
				state.PendingComponentSets.Enqueue((entityId, handler));
			}
		};

		// Hook into component added (first time only) - queue for deferred processing
		// (OnComponentAdded fires BEFORE the value is written, only on first add)
		world.OnComponentAdded += (w, entityId, componentInfo) =>
		{
			if (!state.HooksEnabled) return;

			// Skip component type entities
			if (IsComponentEntity(state, entityId)) return;

			if (componentInfo.Size > 0 && state.ComponentHandlers.TryGetValue(componentInfo.ID, out var handler))
			{
				// Queue for processing after Set() completes - this fires OnAdd
				state.PendingComponentAdds.Enqueue((entityId, handler));
			}
		};

		// Hook into component unset - automatically emit OnRemove
		world.OnComponentUnset += (w, entityId, componentInfo) =>
		{
			if (!state.HooksEnabled) return;

			// Skip component type entities
			if (IsComponentEntity(state, entityId)) return;

			if (state.ComponentHandlers.TryGetValue(componentInfo.ID, out var handler))
			{
				// Direct typed call - no reflection!
				handler.HandleUnset(w, entityId);
			}
		};
	}

	/// <summary>
	/// Check if an entity ID represents a component type entity (not a game entity)
	/// </summary>
	private static bool IsComponentEntity(ObserverState state, ulong entityId)
	{
		// Component entities have IDs from 1 to World.MaxComponentId (configurable, default 256)
		// Regular game entities have IDs starting from MaxComponentId + 1
		return entityId <= state.MaxComponentEntityId;
	}

	/// <summary>
	/// Enable automatic observer triggers for a component type.
	/// Call this once per component type you want to observe.
	/// </summary>
	public static void EnableObservers<T>(this TinyEcs.World world) where T : struct
	{
		var state = world.GetObserverState();

		// Check if already registered
		var existingId = ComponentHandler<T>.GetComponentId();
		if (existingId.HasValue)
		{
			if (!state.ComponentHandlers.ContainsKey(existingId.Value))
			{
				state.ComponentHandlers[existingId.Value] = new ComponentHandler<T>();
			}
			return;
		}

		// Temporarily disable hooks to prevent spurious events during registration
		var wasEnabled = state.HooksEnabled;
		state.HooksEnabled = false;

		// Get component entity ID directly - no dummy entity needed!
		var componentEntity = world.Entity<T>();
		var componentId = componentEntity.ID;

		// Register typed handler first
		state.ComponentHandlers[componentId] = new ComponentHandler<T>();

		// Store component ID in the handler
		ComponentHandler<T>.SetComponentId(componentId);

		// Re-enable hooks
		state.HooksEnabled = wasEnabled;
	}

	/// <summary>
	/// Register an observer for a specific trigger
	/// </summary>
	public static void RegisterObserver<TTrigger>(this TinyEcs.World world, Action<TinyEcs.World, TTrigger> callback)
	{
		var state = world.GetObserverState();
		var triggerType = typeof(TTrigger);

		if (!state.Observers.TryGetValue(triggerType, out var observers))
		{
			observers = new List<IObserver>();
			state.Observers[triggerType] = observers;
		}

		observers.Add(new Observer<TTrigger>(callback));
	}

	/// <summary>
	/// Emit a trigger to all observers (both global and entity-specific)
	/// </summary>
	public static void EmitTrigger<TTrigger>(this TinyEcs.World world, TTrigger trigger)
	{
		var state = world.GetObserverState();
		var triggerType = typeof(TTrigger);

		// Fire global observers
		if (state.Observers.TryGetValue(triggerType, out var observers))
		{
			foreach (var observer in observers)
			{
				observer.Execute(world, trigger!);
			}
		}

		// Fire entity-specific observers if this trigger has an entity ID
		if (trigger is IEntityTrigger entityTrigger)
		{
			var currentEntityId = entityTrigger.EntityId;
			var shouldPropagate = trigger is IPropagatingTrigger propagatingTrigger && propagatingTrigger.ShouldPropagate;

			// Process entity hierarchy (current entity + parents if propagating)
			while (currentEntityId != 0)
			{
				// Fire entity-specific observers on current entity
				if (world.Has<EntityObservers>(currentEntityId))
				{
					ref var entityObservers = ref world.Get<EntityObservers>(currentEntityId);
					if (entityObservers.Observers != null)
					{
						foreach (var observer in entityObservers.Observers)
						{
							if (observer.TriggerType == triggerType)
							{
								observer.Execute(world, trigger!);
							}
						}
					}
				}

				// Stop if not propagating
				if (!shouldPropagate)
					break;

				// Move to parent entity
				if (world.Has<Parent>(currentEntityId))
				{
					ref var parent = ref world.Get<Parent>(currentEntityId);
					currentEntityId = parent.Id;
				}
				else
				{
					break;
				}
			}
		}
	}

	/// <summary>
	/// Process pending component set triggers. Call this after modifying components.
	/// </summary>
	public static void FlushObservers(this TinyEcs.World world)
	{
		var state = world.GetObserverState();

		// Process OnInsert triggers (fires for both add and update)
		while (state.PendingComponentSets.TryDequeue(out var pending))
		{
			var (entityId, handler) = pending;
			// Now the component value has been written - safe to read!
			handler.HandleSet(world, entityId);
		}

		// Process OnAdd triggers (fires only for first-time additions)
		while (state.PendingComponentAdds.TryDequeue(out var pending))
		{
			var (entityId, handler) = pending;
			// Now the component value has been written - safe to read!
			handler.HandleAdd(world, entityId);
		}
	}
}

internal class ObserverState
{
	public Dictionary<Type, List<IObserver>> Observers { get; } = new();
	public Dictionary<ulong, IComponentHandler> ComponentHandlers { get; } = new();
	public Queue<(ulong EntityId, IComponentHandler Handler)> PendingComponentSets { get; } = new();
	public Queue<(ulong EntityId, IComponentHandler Handler)> PendingComponentAdds { get; } = new();
	public bool HooksRegistered { get; set; }
	public bool HooksEnabled { get; set; } = true;
	public ulong MaxComponentEntityId { get; set; } // Component entities have IDs <= this value
}

// ============================================================================
// App Extensions for Observers
// ============================================================================

public static partial class AppObserverExtensions
{
	/// <summary>
	/// Register an observer that runs when the trigger occurs
	/// </summary>
	public static App AddObserver<TTrigger>(this App app, Action<TinyEcs.World, TTrigger> callback)
		where TTrigger : struct, ITrigger
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		world.RegisterObserver(callback);
		return app;
	}

}

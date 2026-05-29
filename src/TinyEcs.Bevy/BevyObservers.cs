using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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

	/// <summary>
	/// Enable/Disable propagation to parent entities.
	/// This can be called by observers to prevent the trigger from bubbling further.
	/// </summary>
	void Propagate(bool propagate = true);
}

/// <summary>
/// Trigger when a component is added to an entity
/// </summary>
public record struct OnAdd<T>(ulong EntityId, T Component) : ITrigger, IEntityTrigger
	where T : struct
{
#if NET9_0_OR_GREATER
	public static void Register(TinyEcs.World world)
#else
	public readonly void Register(TinyEcs.World world)
#endif
		=> world.EnableObservers<T>();
}

/// <summary>
/// Trigger when a component is inserted/updated on an entity
/// </summary>
public record struct OnInsert<T>(ulong EntityId, T Component) : ITrigger, IEntityTrigger
	where T : struct
{
#if NET9_0_OR_GREATER
	public static void Register(TinyEcs.World world)
#else
	public readonly void Register(TinyEcs.World world)
#endif
		=> world.EnableObservers<T>();
}

/// <summary>
/// Trigger when a component is removed from an entity
/// </summary>
public record struct OnRemove<T>(ulong EntityId, T Component) : ITrigger, IEntityTrigger
	where T : struct
{
#if NET9_0_OR_GREATER
	public static void Register(TinyEcs.World world)
#else
	public readonly void Register(TinyEcs.World world)
#endif
		=> world.EnableObservers<T>();
}

/// <summary>
/// Trigger when an entity is spawned
/// </summary>
public record struct OnSpawn(ulong EntityId) : ITrigger, IEntityTrigger
{
#if NET9_0_OR_GREATER
	public static void Register(TinyEcs.World world)
#else
	public readonly void Register(TinyEcs.World world)
#endif
	{
	}
}

/// <summary>
/// Trigger when an entity is despawned
/// </summary>
public record struct OnDespawn(ulong EntityId) : ITrigger, IEntityTrigger
{
#if NET9_0_OR_GREATER
	public static void Register(TinyEcs.World world)
#else
	public readonly void Register(TinyEcs.World world)
#endif
	{
	}
}

/// <summary>
/// Trigger when a custom event is fired via Commands.Trigger.
/// Can be used for both global events (EntityId = 0) and entity-specific events.
/// </summary>
public unsafe struct On<TEvent> : ITrigger, IEntityTrigger, IPropagatingTrigger
	where TEvent : struct
{
	private bool* _propagate;

	internal On(ulong entity, TEvent ev, ref bool propagate)
	{
		EntityId = entity;
		Event = ev;
		_propagate = (bool*)Unsafe.AsPointer(ref propagate);
	}

	/// <summary>
	/// Create a global event (not tied to a specific entity)
	/// </summary>
	internal On(TEvent evt) : this(0, evt, ref Unsafe.NullRef<bool>()) { }

	public ulong EntityId { get; }
	public TEvent Event { get; }
	public readonly bool ShouldPropagate => _propagate != null && *_propagate;



#if NET9_0_OR_GREATER
	public static void Register(TinyEcs.World world)
#else
	public readonly void Register(TinyEcs.World world)
#endif
	{
		// Custom events don't require registration.
	}

	/// <summary>
	/// Returns a new trigger with propagation enabled.
	/// When propagating, the trigger will also fire on all parent entities up the hierarchy.
	/// </summary>
	public void Propagate(bool propagate = true)
	{
		if (_propagate != null)
			*_propagate = propagate;
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
	void Execute(TinyEcs.World world, ITrigger trigger);
	Type TriggerType { get; }
}

public delegate void ExecuteObserverDel<T>(TinyEcs.World world, ref T trigger) where T : struct;

/// <summary>
/// Typed observer for specific trigger.
/// For propagating triggers, receives trigger by reference so observers can stop propagation.
/// </summary>
public class Observer<TTrigger> : IObserver
	where TTrigger : ITrigger
{
	private readonly Action<World, TTrigger> _callback;

	public Type TriggerType => typeof(TTrigger);

	public Observer(Action<World, TTrigger> callback)
	{
		_callback = callback;
	}

	public void Execute(TinyEcs.World world, ITrigger trigger)
	{
		world.BeginDeferred();
		_callback(world, (TTrigger)trigger);
		world.EndDeferred();
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
		world.EmitTriggerInner(new OnInsert<T>(entityId, component));
	}

	public void HandleAdd(TinyEcs.World world, ulong entityId)
	{
		if (!world.Has<T>(entityId))
			return;

		ref var component = ref world.Get<T>(entityId);
		world.EmitTriggerInner(new OnAdd<T>(entityId, component));
	}

	public void HandleUnset(TinyEcs.World world, ulong entityId)
	{
		if (!world.Has<T>(entityId))
			return;

		ref var component = ref world.Get<T>(entityId);
		world.EmitTriggerInner(new OnRemove<T>(entityId, component));
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

	/// <summary>
	/// Test hook: true if an observer state is currently registered for the world.
	/// Used to verify cleanup on world disposal.
	/// </summary>
	internal static bool HasObserverState(this TinyEcs.World world)
		=> _observerStates.ContainsKey(world);

	internal static ObserverState GetObserverState(this TinyEcs.World world)
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

		// Evict observer state when the world is disposed, otherwise the static
		// _observerStates map pins the world (and all its observer callbacks) forever.
		world.OnDisposed += static w => _observerStates.Remove(w);

		// Hook into entity creation - automatically emit OnSpawn
		world.OnEntityCreated += (w, entityId) =>
		{
			if (!state.HooksEnabled) return;

			// Skip component type entities
			if (IsComponentEntity(state, entityId)) return;

			w.EmitTriggerInner(new OnSpawn(entityId));
		};

		// Hook into entity deletion - automatically emit OnDespawn.
		// Emit BEFORE removing entity-specific observers so they can see the
		// despawn event (otherwise .Observe<OnDespawn>() never fires).
		world.OnEntityDeleted += (w, entityId) =>
		{
			if (!state.HooksEnabled) return;

			// Skip component type entities
			if (IsComponentEntity(state, entityId)) return;

			w.EmitTriggerInner(new OnDespawn(entityId));

			state.EntityObservers.Remove(entityId);
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
				state.PendingComponentActions.Enqueue((entityId, handler, PendingComponentActionType.Set));
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
				state.PendingComponentActions.Enqueue((entityId, handler, PendingComponentActionType.Add));
			}
		};

		// Hook into component unset - emit OnRemove synchronously while the
		// component value is still readable. Deferring would lose the value
		// (the archetype move that follows OnComponentUnset removes the row).
		world.OnComponentUnset += (w, entityId, componentInfo) =>
		{
			if (!state.HooksEnabled) return;

			// Skip component type entities
			if (IsComponentEntity(state, entityId)) return;

			if (state.ComponentHandlers.TryGetValue(componentInfo.ID, out var handler))
			{
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
	public static void RegisterObserver<TTrigger>(this TinyEcs.World world, Action<World, TTrigger> callback)
		where TTrigger : ITrigger
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

	public static void EmitTrigger<T>(this TinyEcs.World world, ulong entityId, T ev)
		where T : struct
	{
		var propagate = true;
		var trigger = new On<T>(entityId, ev, ref propagate);
		world.EmitTriggerInner(trigger);
	}

	/// <summary>
	/// Emit a trigger to all observers (both global and entity-specific).
	/// For propagating triggers, observers are executed synchronously so they can stop propagation mid-chain.
	/// </summary>
	public static void EmitTriggerInner<TTrigger>(this TinyEcs.World world, TTrigger trigger)
		where TTrigger : ITrigger
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

			// Process entity hierarchy (current entity + parents if propagating)
			while (currentEntityId != 0)
			{
				// Skip if entity is dead (can happen with deferred commands)
				if (!world.Exists(currentEntityId))
					break;

				if (state.EntityObservers.TryGetValue(currentEntityId, out var entityObserversList))
				{
					foreach (var observer in entityObserversList)
					{
						if (observer.TriggerType == triggerType)
						{
							observer.Execute(world, trigger);
						}
					}
				}

				// Check if propagation was stopped by any observer
				if (trigger is not IPropagatingTrigger propTrigger || !propTrigger.ShouldPropagate)
				{
					break;
				}

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

		while (state.PendingComponentActions.TryDequeue(out var pending))
		{
			var (entityId, handler, action) = pending;

			if (!world.Exists(entityId))
			{
				// Entity was deleted between enqueue and flush - skip
				continue;
			}

			switch (action)
			{
				case PendingComponentActionType.Set:
					handler.HandleSet(world, entityId);
					break;
				case PendingComponentActionType.Add:
					handler.HandleAdd(world, entityId);
					break;
				case PendingComponentActionType.Remove:
					handler.HandleUnset(world, entityId);
					break;
			}

		}
	}
}

internal enum PendingComponentActionType
{
	Set,
	Add,
	Remove
}

internal class ObserverState
{
	public Dictionary<Type, List<IObserver>> Observers { get; } = new();
	public Dictionary<ulong, List<IObserver>> EntityObservers { get; } = new();
	public Dictionary<ulong, IComponentHandler> ComponentHandlers { get; } = new();
	public Queue<(ulong EntityId, IComponentHandler Handler, PendingComponentActionType Action)> PendingComponentActions { get; } = new();
	public bool HooksRegistered { get; set; }
	public bool HooksEnabled { get; set; } = true;
	public ulong MaxComponentEntityId { get; set; } // Component entities have IDs <= this value
}

// ============================================================================
// App Extensions for Observers
// ============================================================================

public static partial class AppObserverExtensions
{
}

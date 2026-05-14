using System;
using System.Collections.Generic;
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
/// Triggers that support dynamic stop-propagation (mutable mid-bubble cancel) should
/// override <see cref="SetCurrent"/> to receive the entity being dispatched and allow
/// observers to call back into the trigger to stop the walk.
/// </summary>
public interface IPropagatingTrigger
{
	bool ShouldPropagate { get; }

	/// <summary>
	/// Hook invoked by the dispatcher before firing observers at each ancestor.
	/// Default implementation is a no-op; only triggers that expose CurrentEntityId
	/// (such as <see cref="On{TEvent}"/>) need to override this.
	/// </summary>
	void SetCurrent(ulong entityId) { }
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
/// Can be used for both global events (EntityId = 0) and entity-specific events.
/// Supports DOM-style bubble walking: <see cref="EntityId"/> is the original target,
/// <see cref="CurrentEntityId"/> is the ancestor currently dispatching, and observers
/// may call <see cref="Propagate"/> to stop the walk mid-bubble.
/// </summary>
public unsafe struct On<TEvent> : ITrigger, IEntityTrigger, IPropagatingTrigger
	where TEvent : struct
{
	private readonly bool* _propagate;
	private readonly ulong* _currentEntity;

	internal On(ulong entity, TEvent ev, ref bool propagate, ref ulong currentEntity)
	{
		EntityId = entity;
		Event = ev;
		_propagate = (bool*)Unsafe.AsPointer(ref propagate);
		_currentEntity = (ulong*)Unsafe.AsPointer(ref currentEntity);
	}

	/// <summary>
	/// Create a global event (not tied to a specific entity).
	/// Global emissions do not bubble.
	/// </summary>
	internal On(TEvent evt)
	{
		EntityId = 0;
		Event = evt;
		_propagate = null;
		_currentEntity = null;
	}

	public ulong EntityId { get; }
	public TEvent Event { get; }

	/// <summary>
	/// Entity currently dispatching this trigger during the bubble walk.
	/// Equals <see cref="EntityId"/> on the target dispatch; equals an ancestor entity ID
	/// when bubbling up the parent hierarchy.
	/// </summary>
	public readonly ulong CurrentEntityId =>
		_currentEntity != null ? *_currentEntity : EntityId;

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
	/// Enable/disable bubble propagation. Defaults to enabled for entity-targeted emissions.
	/// Call with <c>false</c> from an observer to stop the bubble walk at the current entity.
	/// </summary>
	public readonly void Propagate(bool propagate = true)
	{
		if (_propagate != null)
			*_propagate = propagate;
	}

	void IPropagatingTrigger.SetCurrent(ulong entityId)
	{
		if (_currentEntity != null)
			*_currentEntity = entityId;
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
		// Emit synchronously. The hook fires before archetype.Remove so the component
		// is still readable, and entity-specific OnRemove observers stored on the
		// entity must run before the archetype row is freed (which would also clear
		// the EntityObservers component).
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

		// Hook into entity creation - queue OnSpawn for post-merge dispatch
		world.OnEntityCreated += (w, entityId) =>
		{
			if (!state.HooksEnabled) return;

			// Skip component type entities
			if (IsComponentEntity(state, entityId)) return;

			w.QueueDirectTrigger(new OnSpawn(entityId));
		};

		// Hook into entity deletion - emit OnDespawn synchronously. The entity is
		// still alive at hook fire time (archetype.Remove happens after), so
		// entity-specific observers stored on the entity can still receive the
		// trigger before the entity is torn down. Deferring to FlushObservers would
		// lose them because EntityObservers is cleared when the archetype row is freed.
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
			var propagatingTrigger = trigger as IPropagatingTrigger;

			// Process entity hierarchy (current entity + parents if propagating)
			while (currentEntityId != 0)
			{
				// Skip dead entities. OnDespawn/OnRemove fire post-merge so the original
				// target may already be gone; walk simply stops there.
				if (!world.Exists(currentEntityId))
					break;

				// Notify dynamic-propagation triggers which ancestor is now dispatching
				// so handlers can read CurrentEntityId during the bubble walk.
				propagatingTrigger?.SetCurrent(currentEntityId);

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

				// Re-check propagation each iteration: triggers that expose a mutable
				// flag (e.g. On<T>) may have been stopped by an observer just above.
				if (propagatingTrigger == null || !propagatingTrigger.ShouldPropagate)
					break;

				// Walk to parent via the Parent component. Bubble dispatch is queued via
				// FlushObservers, which runs after world.EndDeferred drains pending ops,
				// so Has/Get reflect the merged state set by AddChild this same frame.
				if (!world.Has<Parent>(currentEntityId))
					break;

				var parentId = world.Get<Parent>(currentEntityId).Id;
				if (parentId == 0)
					break;

				currentEntityId = parentId;
			}
		}
	}

	/// <summary>
	/// Process pending component set triggers. Call this after modifying components.
	/// </summary>
	public static void FlushObservers(this TinyEcs.World world)
	{
		var state = world.GetObserverState();

		// Re-entrant call (e.g. observer wrapper calling FlushObservers after its
		// Commands.Apply): bail. The outer flush loop already drains any items added
		// during nested execution.
		if (state.IsFlushing)
			return;

		state.IsFlushing = true;
		try
		{
			FlushObserversInner(world, state);
		}
		finally
		{
			state.IsFlushing = false;
		}
	}

	private static void FlushObserversInner(TinyEcs.World world, ObserverState state)
	{
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

		// Process custom On<TEvent> triggers queued by Commands. They run here so the
		// world's deferred operations (entity spawns, parent links, component writes)
		// are already merged and any observer reading state via Has/Get sees a
		// consistent snapshot. An observer may emit further triggers (including of new
		// event types) which mutate PendingTriggerQueues, so snapshot into a reusable
		// buffer and loop until no queue has work left.
		while (true)
		{
			state.FlushBuffer.Clear();
			foreach (var queue in state.PendingTriggerQueues.Values)
				state.FlushBuffer.Add(queue);

			var drainedAny = false;
			foreach (var queue in state.FlushBuffer)
			{
				if (queue.Flush(world))
					drainedAny = true;
			}

			if (!drainedAny)
				break;
		}
	}

	/// <summary>
	/// Queue an On&lt;TEvent&gt; emission to fire during the next <see cref="FlushObservers"/>.
	/// Allocation-free on the hot path: dispatches into a per-type struct queue and
	/// enqueues by value.
	/// </summary>
	internal static void QueueCustomTrigger<TEvent>(this TinyEcs.World world, ulong entityId, TEvent evt, bool isGlobal)
		where TEvent : struct
	{
		var state = world.GetObserverState();
		if (!state.PendingTriggerQueues.TryGetValue(typeof(TEvent), out var queue))
		{
			queue = new PendingTriggerQueue<TEvent>();
			state.PendingTriggerQueues[typeof(TEvent)] = queue;
		}

		((PendingTriggerQueue<TEvent>)queue).Items.Enqueue(new PendingTriggerQueue<TEvent>.Entry
		{
			EntityId = entityId,
			Event = evt,
			IsGlobal = isGlobal
		});
	}

	/// <summary>
	/// Queue a trigger by value to fire during the next <see cref="FlushObservers"/>.
	/// Used by world hooks (entity create/delete, component unset) so dispatch happens
	/// after the surrounding deferred scope has merged.
	/// </summary>
	internal static void QueueDirectTrigger<TTrigger>(this TinyEcs.World world, TTrigger trigger)
		where TTrigger : struct, ITrigger
	{
		var state = world.GetObserverState();
		var key = typeof(TTrigger);
		if (!state.PendingTriggerQueues.TryGetValue(key, out var queue))
		{
			queue = new PendingDirectQueue<TTrigger>();
			state.PendingTriggerQueues[key] = queue;
		}

		((PendingDirectQueue<TTrigger>)queue).Items.Enqueue(trigger);
	}
}

internal class ObserverState
{
	public Dictionary<Type, List<IObserver>> Observers { get; } = new();
	public Dictionary<ulong, IComponentHandler> ComponentHandlers { get; } = new();
	public Queue<(ulong EntityId, IComponentHandler Handler)> PendingComponentSets { get; } = new();
	public Queue<(ulong EntityId, IComponentHandler Handler)> PendingComponentAdds { get; } = new();
	public Dictionary<Type, IPendingTriggerQueue> PendingTriggerQueues { get; } = new();

	/// <summary>
	/// Reusable buffer for iterating <see cref="PendingTriggerQueues"/> during a flush
	/// pass. Snapshotting into this list avoids "collection modified during enumeration"
	/// when an observer callback emits a trigger of a new event type (which inserts a
	/// new entry into the dictionary).
	/// </summary>
	public List<IPendingTriggerQueue> FlushBuffer { get; } = new();
	public bool HooksRegistered { get; set; }
	public bool HooksEnabled { get; set; } = true;
	public bool IsFlushing { get; set; }
	public ulong MaxComponentEntityId { get; set; } // Component entities have IDs <= this value
}

/// <summary>
/// Type-erased entry point for per-event-type pending-trigger queues.
/// Concrete queues hold struct entries to avoid allocations on the emit path.
/// </summary>
internal interface IPendingTriggerQueue
{
	bool HasItems { get; }
	bool Flush(TinyEcs.World world);
}

/// <summary>
/// Generic queue holding any trigger by value. Used for OnSpawn/OnDespawn/OnRemove&lt;T&gt;
/// so their dispatch happens at <see cref="ObserverExtensions.FlushObservers"/> time
/// (post-merge) instead of from inside the world hook (potentially mid-deferred-scope).
/// Zero-allocation on enqueue.
/// </summary>
internal sealed class PendingDirectQueue<TTrigger> : IPendingTriggerQueue
	where TTrigger : struct, ITrigger
{
	public readonly Queue<TTrigger> Items = new();

	public bool HasItems => Items.Count > 0;

	public bool Flush(TinyEcs.World world)
	{
		if (Items.Count == 0)
			return false;

		while (Items.TryDequeue(out var trigger))
		{
			world.EmitTrigger(trigger);
		}
		return true;
	}
}

/// <summary>
/// Typed queue of pending On&lt;TEvent&gt; emissions. Created lazily per event type
/// the first time something queues a trigger of that type. Holds struct entries
/// so EmitTrigger calls do not allocate closures or delegates.
/// </summary>
internal sealed class PendingTriggerQueue<TEvent> : IPendingTriggerQueue
	where TEvent : struct
{
	internal struct Entry
	{
		public ulong EntityId;
		public TEvent Event;
		public bool IsGlobal;
	}

	public readonly Queue<Entry> Items = new();

	public bool HasItems => Items.Count > 0;

	public bool Flush(TinyEcs.World world)
	{
		if (Items.Count == 0)
			return false;

		while (Items.TryDequeue(out var entry))
		{
			if (entry.IsGlobal)
			{
				world.EmitTrigger(new On<TEvent>(entry.Event));
			}
			else
			{
				// Stack cells live for the duration of this iteration; the trigger's
				// pointer fields are valid until EmitTrigger returns.
				var propagate = true;
				var current = entry.EntityId;
				world.EmitTrigger(new On<TEvent>(entry.EntityId, entry.Event, ref propagate, ref current));
			}
		}
		return true;
	}
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

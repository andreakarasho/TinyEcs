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
/// Trigger when a component is added to an entity
/// </summary>
public readonly record struct OnAdd<T>(ulong EntityId, T Component) : ITrigger
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
public readonly record struct OnInsert<T>(ulong EntityId, T Component) : ITrigger
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
public readonly record struct OnRemove<T>(ulong EntityId) : ITrigger
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
public readonly record struct OnSpawn(ulong EntityId) : ITrigger
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
public readonly record struct OnDespawn(ulong EntityId) : ITrigger
{
#if NET9_0_OR_GREATER
	public static void Register(TinyEcs.World world)
#else
	public readonly void Register(TinyEcs.World world)
#endif
	{
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

	public void HandleUnset(TinyEcs.World world, ulong entityId)
	{
		// Direct typed call - no reflection!
		world.EmitTrigger(new OnRemove<T>(entityId));
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
		// (OnComponentSet fires BEFORE the value is written)
		world.OnComponentSet += (w, entityId, componentInfo) =>
		{
			if (!state.HooksEnabled) return;

			// Skip component type entities
			if (IsComponentEntity(state, entityId)) return;

			if (componentInfo.Size > 0 && state.ComponentHandlers.TryGetValue(componentInfo.ID, out var handler))
			{
				// Queue for processing after Set() completes
				state.PendingComponentSets.Enqueue((entityId, handler));
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
			// Already registered
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
	/// Emit a trigger to all observers
	/// </summary>
	public static void EmitTrigger<TTrigger>(this TinyEcs.World world, TTrigger trigger)
	{
		var state = world.GetObserverState();
		var triggerType = typeof(TTrigger);

		if (state.Observers.TryGetValue(triggerType, out var observers))
		{
			foreach (var observer in observers)
			{
				observer.Execute(world, trigger!);
			}
		}
	}

	/// <summary>
	/// Process pending component set triggers. Call this after modifying components.
	/// </summary>
	public static void FlushObservers(this TinyEcs.World world)
	{
		var state = world.GetObserverState();

		while (state.PendingComponentSets.TryDequeue(out var pending))
		{
			var (entityId, handler) = pending;
			// Now the component value has been written - safe to read!
			handler.HandleSet(world, entityId);
		}
	}
}

internal class ObserverState
{
	public Dictionary<Type, List<IObserver>> Observers { get; } = new();
	public Dictionary<ulong, IComponentHandler> ComponentHandlers { get; } = new();
	public Queue<(ulong EntityId, IComponentHandler Handler)> PendingComponentSets { get; } = new();
	public bool HooksRegistered { get; set; }
	public bool HooksEnabled { get; set; } = true;
	public ulong MaxComponentEntityId { get; set; } // Component entities have IDs <= this value
}

// ============================================================================
// App Extensions for Observers
// ============================================================================

public static class AppObserverExtensions
{
	/// <summary>
	/// Register an observer that runs when the trigger occurs
	/// </summary>
	public static App Observe<TTrigger>(this App app, Action<TinyEcs.World, TTrigger> callback)
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

	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App Observe<TTrigger, T1>(this App app, Action<TTrigger, T1> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			callback(trigger, p1);
		});

		return app;
	}

	public static App Observe<TTrigger, T1, T2>(this App app, Action<TTrigger, T1, T2> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			callback(trigger, p1, p2);
		});

		return app;
	}

	public static App Observe<TTrigger, T1, T2, T3>(this App app, Action<TTrigger, T1, T2, T3> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			callback(trigger, p1, p2, p3);
		});

		return app;
	}

	public static App Observe<TTrigger, T1, T2, T3, T4>(this App app, Action<TTrigger, T1, T2, T3, T4> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			callback(trigger, p1, p2, p3, p4);
		});

		return app;
	}

	public static App Observe<TTrigger, T1, T2, T3, T4, T5>(this App app, Action<TTrigger, T1, T2, T3, T4, T5> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5);
		});

		return app;
	}

	public static App Observe<TTrigger, T1, T2, T3, T4, T5, T6>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6);
		});

		return app;
	}

	public static App Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7);
		});

		return app;
	}

	public static App Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
		where T8 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8);
		});

		return app;
	}

	public static App Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
		where T8 : ISystemParam, new()
		where T9 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		var p9 = new T9();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			p9.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9);
		});

		return app;
	}

	public static App Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
		where T8 : ISystemParam, new()
		where T9 : ISystemParam, new()
		where T10 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		var p9 = new T9();
		var p10 = new T10();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			p9.Fetch(w);
			p10.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
		});

		return app;
	}

	public static App Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
		where T8 : ISystemParam, new()
		where T9 : ISystemParam, new()
		where T10 : ISystemParam, new()
		where T11 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		var p9 = new T9();
		var p10 = new T10();
		var p11 = new T11();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			p9.Fetch(w);
			p10.Fetch(w);
			p11.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11);
		});

		return app;
	}

	public static App Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
		where T8 : ISystemParam, new()
		where T9 : ISystemParam, new()
		where T10 : ISystemParam, new()
		where T11 : ISystemParam, new()
		where T12 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		var p9 = new T9();
		var p10 = new T10();
		var p11 = new T11();
		var p12 = new T12();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			p9.Fetch(w);
			p10.Fetch(w);
			p11.Fetch(w);
			p12.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12);
		});

		return app;
	}

	public static App Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
		where T8 : ISystemParam, new()
		where T9 : ISystemParam, new()
		where T10 : ISystemParam, new()
		where T11 : ISystemParam, new()
		where T12 : ISystemParam, new()
		where T13 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		var p9 = new T9();
		var p10 = new T10();
		var p11 = new T11();
		var p12 = new T12();
		var p13 = new T13();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			p9.Fetch(w);
			p10.Fetch(w);
			p11.Fetch(w);
			p12.Fetch(w);
			p13.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13);
		});

		return app;
	}

	public static App Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
		where T8 : ISystemParam, new()
		where T9 : ISystemParam, new()
		where T10 : ISystemParam, new()
		where T11 : ISystemParam, new()
		where T12 : ISystemParam, new()
		where T13 : ISystemParam, new()
		where T14 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		var p9 = new T9();
		var p10 = new T10();
		var p11 = new T11();
		var p12 = new T12();
		var p13 = new T13();
		var p14 = new T14();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			p9.Fetch(w);
			p10.Fetch(w);
			p11.Fetch(w);
			p12.Fetch(w);
			p13.Fetch(w);
			p14.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14);
		});

		return app;
	}

	public static App Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
		where T8 : ISystemParam, new()
		where T9 : ISystemParam, new()
		where T10 : ISystemParam, new()
		where T11 : ISystemParam, new()
		where T12 : ISystemParam, new()
		where T13 : ISystemParam, new()
		where T14 : ISystemParam, new()
		where T15 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		var p9 = new T9();
		var p10 = new T10();
		var p11 = new T11();
		var p12 = new T12();
		var p13 = new T13();
		var p14 = new T14();
		var p15 = new T15();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			p9.Fetch(w);
			p10.Fetch(w);
			p11.Fetch(w);
			p12.Fetch(w);
			p13.Fetch(w);
			p14.Fetch(w);
			p15.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15);
		});

		return app;
	}
}

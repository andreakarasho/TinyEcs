using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TinyEcs;

namespace TinyEcs.Bevy;

// ============================================================================
// System Parameters - Bevy-style automatic dependency injection
// ============================================================================

/// <summary>
/// Base interface for all system parameters that can be injected into systems.
/// Lifecycle: <see cref="Initialize"/> is called once when the system is first
/// run; <see cref="Fetch"/> is called before every system run. Both receive the
/// owning <see cref="App"/>, from which the parameter can obtain the World,
/// resources, event channels, or any other App-bound state.
/// </summary>
public interface ISystemParam
{
	void Initialize(App app);
	void Fetch(App app);

	/// <summary>
	/// Gets the access information for this parameter (for parallel execution analysis)
	/// </summary>
	SystemParamAccess GetAccess();
}

/// <summary>
/// Describes what resources a system parameter accesses
/// </summary>
public class SystemParamAccess
{
	public HashSet<Type> ReadResources { get; } = new();
	public HashSet<Type> WriteResources { get; } = new();

	public bool ConflictsWith(SystemParamAccess other)
	{
		// Conflict if either writes to a resource the other reads or writes
		foreach (var write in WriteResources)
		{
			if (other.ReadResources.Contains(write) || other.WriteResources.Contains(write))
				return true;
		}

		foreach (var write in other.WriteResources)
		{
			if (ReadResources.Contains(write) || WriteResources.Contains(write))
				return true;
		}

		return false;
	}
}

/// <summary>
/// Base class for composite system parameters that group multiple inner
/// <see cref="ISystemParam"/> instances. Derived classes register their inner
/// params in the constructor via <see cref="Add{T}(T)"/>; the base class then
/// forwards <see cref="Initialize"/>, <see cref="Fetch"/>, and
/// <see cref="GetAccess"/> to every registered param.
///
/// Example:
/// <code>
/// public class CombatParams : CompositeSystemParam
/// {
///     public readonly Query&lt;Data&lt;Health, Damage&gt;&gt; Targets;
///     public readonly Res&lt;DifficultyConfig&gt; Difficulty;
///     public readonly Commands Commands;
///
///     public CombatParams()
///     {
///         Targets    = Add(new Query&lt;Data&lt;Health, Damage&gt;&gt;());
///         Difficulty = Add(new Res&lt;DifficultyConfig&gt;());
///         Commands   = Add(new Commands());
///     }
/// }
/// </code>
///
/// Note: <see cref="GetAccess"/> caches the merged access set on first call.
/// If params are added after construction, the cache becomes stale — register
/// all params in the constructor only.
/// </summary>
public abstract class CompositeSystemParam : ISystemParam
{
	private readonly List<ISystemParam> _params = new();
	private SystemParamAccess? _cachedAccess;

	/// <summary>
	/// Register an inner system parameter. Returns the param so it can be
	/// assigned to a field on the same line: <c>Field = Add(new Param());</c>.
	/// </summary>
	protected T Add<T>(T param) where T : ISystemParam
	{
		_params.Add(param);
		return param;
	}

	public virtual void Initialize(App app)
	{
		foreach (var p in _params)
			p.Initialize(app);
	}

	public virtual void Fetch(App app)
	{
		foreach (var p in _params)
			p.Fetch(app);
	}

	public virtual SystemParamAccess GetAccess()
	{
		if (_cachedAccess != null)
			return _cachedAccess;

		var combined = new SystemParamAccess();
		foreach (var p in _params)
		{
			var a = p.GetAccess();
			foreach (var r in a.ReadResources) combined.ReadResources.Add(r);
			foreach (var w in a.WriteResources) combined.WriteResources.Add(w);
		}
		_cachedAccess = combined;
		return combined;
	}
}

internal readonly struct DeferredEntityRef
{
	public readonly int SpawnIndex;
	public readonly ulong EntityId;

	public DeferredEntityRef(int spawnIndex, ulong entityId)
	{
		SpawnIndex = spawnIndex;
		EntityId = entityId;
	}
}

// ============================================================================
// Res<T> - Immutable resource access
// ============================================================================

/// <summary>
/// Immutable reference to a resource. Use for read-only access.
/// </summary>
public class Res<T> : ISystemParam where T : notnull
{
	private ResourceBox<T>? _box;

	public void Initialize(App app)
	{
		_box = null;
	}

	public void Fetch(App app)
	{
		_box = app.GetResourceBoxInternal<T>();
	}

	public SystemParamAccess GetAccess()
	{
		var access = new SystemParamAccess();
		access.ReadResources.Add(typeof(T));
		return access;
	}

	public ref readonly T Value
	{
		get
		{
			if (_box is null)
				throw new InvalidOperationException("Res<T> has not been fetched. Ensure the system runs through the Bevy scheduler.");
			return ref _box.Value;
		}
	}
}

// ============================================================================
// ResMut<T> - Mutable resource access
// ============================================================================

/// <summary>
/// Mutable reference to a resource. Use when you need to modify it.
/// </summary>
public class ResMut<T> : ISystemParam where T : notnull
{
	private ResourceBox<T>? _box;

	public void Initialize(App app)
	{
		_box = null;
	}

	public void Fetch(App app)
	{
		_box = app.GetResourceBoxInternal<T>();
	}

	public SystemParamAccess GetAccess()
	{
		var access = new SystemParamAccess();
		access.WriteResources.Add(typeof(T));
		return access;
	}

	public ref T Value
	{
		get
		{
			if (_box is null)
				throw new InvalidOperationException("ResMut<T> has not been fetched. Ensure the system runs through the Bevy scheduler.");
			return ref _box.Value;
		}
	}
}

// ============================================================================
// Local<T> - Per-system local state
// ============================================================================

/// <summary>
/// Per-system local state that persists between system runs.
/// Each system instance gets its own independent Local<T>.
/// </summary>
public class Local<T> : ISystemParam where T : new()
{
	private T? _value;

	public ref T? Value => ref _value;

	public void Initialize(App app)
	{
		// Local state is initialized once and persists
		_value = new T();
	}

	public void Fetch(App app)
	{
		// Local state doesn't need to fetch - it's already there
	}

	public SystemParamAccess GetAccess()
	{
		// Local state has no conflicts - it's per-system
		return new SystemParamAccess();
	}
}

// ============================================================================
// EventReader<T> - Read events of type T
// ============================================================================

/// <summary>
/// Reads events of type T from the event queue.
/// Events are consumed after being read.
/// </summary>
public class EventReader<T> : ISystemParam where T : notnull
{
	private EventChannel<T>? _channel;
	private readonly List<T> _events = new();
	private ulong _lastEpoch = ulong.MaxValue;
	private int _lastReadIndex;

	public void Initialize(App app)
	{
		_channel = app.GetOrCreateEventChannel<T>();
	}

	public void Fetch(App app)
	{
		_events.Clear();
		if (_channel is null)
			_channel = app.GetOrCreateEventChannel<T>();
		_channel.CopyEvents(ref _lastEpoch, ref _lastReadIndex, _events);
	}

	/// <summary>
	/// Iterate over all events of type T that occurred since the last fetch.
	/// Allocates a heap enumerator on foreach — prefer <see cref="AsSpan"/> or
	/// <see cref="GetEnumerator"/> for hot paths.
	/// </summary>
	public IEnumerable<T> Read() => _events;

	/// <summary>
	/// Zero-allocation span view over current events. Lifetime: valid until next Fetch.
	/// </summary>
	public ReadOnlySpan<T> AsSpan() =>
		System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_events);

	/// <summary>
	/// Non-allocating enumerator. Use directly in foreach: <c>foreach (var e in reader)</c>.
	/// </summary>
	public List<T>.Enumerator GetEnumerator() => _events.GetEnumerator();

	/// <summary>
	/// Check if any events of this type were sent
	/// </summary>
	public bool HasEvents => _events.Count > 0;

	/// <summary>
	/// Get the number of events
	/// </summary>
	public int Count => _events.Count;

	public SystemParamAccess GetAccess()
	{
		var access = new SystemParamAccess();
		access.ReadResources.Add(typeof(EventChannel<T>));
		return access;
	}
}

// ============================================================================
// EventWriter<T> - Write events of type T
// ============================================================================

/// <summary>
/// Writes events of type T to the event queue.
/// Events will be processed at the end of the current stage.
/// </summary>
public class EventWriter<T> : ISystemParam where T : notnull
{
	private EventChannel<T>? _channel;

	public void Initialize(App app)
	{
		_channel = app.GetOrCreateEventChannel<T>();
	}

	public void Fetch(App app)
	{
		if (_channel is null)
			_channel = app.GetOrCreateEventChannel<T>();
	}

	/// <summary>
	/// Send an event to be processed at the end of the current frame
	/// </summary>
	public void Send(T evt)
	{
		if (_channel == null)
			throw new InvalidOperationException("EventWriter has not been initialized.");

		_channel.Enqueue(evt);
	}

	public SystemParamAccess GetAccess()
	{
		var access = new SystemParamAccess();
		access.WriteResources.Add(typeof(EventChannel<T>));
		return access;
	}
}

// ============================================================================
// Commands - Deferred world operations
// ============================================================================

/// <summary>
/// Commands for deferred world operations (spawning entities, adding/removing components).
/// Operations are queued locally per-system and applied at the end of the system.
/// Thread-safe for parallel system execution.
/// </summary>
public class Commands : ISystemParam
{
	private App? _app;
	private readonly List<IDeferredCommand> _localCommands = new();
	private readonly List<ulong> _spawnedEntityIds = new();
	// Per-Commands pool of boxed command wrappers, keyed by wrapper concrete type.
	// Reuses wrapper class instances across frames to eliminate the per-command boxing
	// allocation that would otherwise occur when adding a struct command to a
	// List<IDeferredCommand>. Single-threaded by design: Commands has exclusive
	// access in GetAccess(), so concurrent boxing is impossible.
	private readonly Dictionary<Type, Stack<IPoolableBox>> _boxPool = new();

	internal App? App => _app;

	// One indirection per use — acceptable because these are user-driven calls,
	// not per-frame hot loops.
	private TinyEcs.World World => _app!.GetWorld();

	public void Initialize(App app)
	{
		_app = app;
	}

	public void Fetch(App app)
	{
		_app = app;
		_localCommands.Clear();
		_spawnedEntityIds.Clear();
	}

	/// <summary>
	/// Rent a pooled boxed wrapper for the given command struct.
	/// </summary>
	internal BoxedCommand<T> RentBox<T>(in T cmd) where T : struct, IDeferredCommand
	{
		BoxedCommand<T> box;
		if (_boxPool.TryGetValue(typeof(BoxedCommand<T>), out var stack) && stack.Count > 0)
		{
			box = (BoxedCommand<T>)stack.Pop();
		}
		else
		{
			box = new BoxedCommand<T>();
		}
		box.Value = cmd;
		return box;
	}

	/// <summary>
	/// Rent a pooled boxed wrapper for a component-related command struct.
	/// </summary>
	internal BoxedComponentCommand<T> RentComponentBox<T>(in T cmd) where T : struct, IComponentCommand
	{
		BoxedComponentCommand<T> box;
		if (_boxPool.TryGetValue(typeof(BoxedComponentCommand<T>), out var stack) && stack.Count > 0)
		{
			box = (BoxedComponentCommand<T>)stack.Pop();
		}
		else
		{
			box = new BoxedComponentCommand<T>();
		}
		box.Value = cmd;
		return box;
	}

	/// <summary>
	/// Return a boxed wrapper to the pool for reuse.
	/// </summary>
	internal void ReturnBox(IPoolableBox box)
	{
		var key = box.GetType();
		if (!_boxPool.TryGetValue(key, out var stack))
		{
			stack = new Stack<IPoolableBox>();
			_boxPool[key] = stack;
		}
		stack.Push(box);
	}

	public SystemParamAccess GetAccess()
	{
		var access = new SystemParamAccess();
		// Commands have exclusive world access (prevents parallel execution with other Commands users)
		access.WriteResources.Add(typeof(Commands));
		return access;
	}

	/// <summary>
	/// Apply all queued commands to the world. Called automatically after system execution.
	/// </summary>
	internal void Apply()
	{
		if (_localCommands.Count == 0)
			return;

		// Apply all commands in order. World writes are deferred — they apply when the
		// outer stage scope calls EndDeferred. Commands that need to read state set by
		// earlier commands in the same batch should rely on synchronously-updated
		// sources (e.g. RelationshipEntityMapper for parent lookups), not on archetype
		// component storage which lags until EndDeferred drains.
		var world = World;
		foreach (var cmd in _localCommands)
		{
			cmd.Execute(world, this);
			// Return pooled wrappers so the next frame doesn't allocate fresh ones.
			if (cmd is IPoolableBox box)
				ReturnBox(box);
		}

		_localCommands.Clear();
	}

	/// <summary>
	/// Spawn a new entity and return a builder for adding components.
	/// The entity is created immediately, but component insertions are still deferred.
	/// This allows the entity ID to be known immediately for tracking purposes.
	///
	/// Thread-safety: This method modifies world state directly. However, systems using Commands
	/// are prevented from running in parallel (Commands has exclusive resource access),
	/// so this is safe when used through the Bevy scheduler.
	/// </summary>
	public EntityCommands Spawn()
	{
		if (_app == null)
			throw new InvalidOperationException("Commands has not been initialized.");

		// Spawn the entity immediately to get the real ID
		// This is safe because systems with Commands never run in parallel
		var entity = World.Entity();
		ulong entityId = entity.ID;

		// Component insertions are still deferred for thread-safety
		return new EntityCommands(this, entityId);
	}

	/// <summary>
	/// Get entity ID by index (internal use by EntityCommands)
	/// </summary>
	internal ulong GetSpawnedEntityId(int index)
	{
		return _spawnedEntityIds[index];
	}

	/// <summary>
	/// Set entity ID by index (internal use by SpawnEntityCommand)
	/// </summary>
	internal void SetSpawnedEntityId(int index, ulong entityId)
	{
		_spawnedEntityIds[index] = entityId;
	}

	/// <summary>
	/// Get entity commands for an existing entity.
	/// Does not validate if the entity exists - commands will silently fail if entity is invalid.
	/// Use TryEntity() for checked access.
	/// </summary>
	public EntityCommands Entity(ulong entityId)
	{
		return new EntityCommands(this, entityId);
	}

	/// <summary>
	/// Try to get entity commands for an existing entity.
	/// Returns true if the entity exists, false otherwise.
	/// Similar to Bevy's get_entity() which returns Option&lt;EntityCommands&gt;.
	/// Note: Since commands are deferred, the entity state may change before execution.
	/// </summary>
	public bool TryEntity(ulong entityId, out EntityCommands entityCommands)
	{
		if (_app == null)
			throw new InvalidOperationException("Commands has not been initialized.");

		if (World.Exists(entityId))
		{
			entityCommands = new EntityCommands(this, entityId);
			return true;
		}

		entityCommands = default;
		return false;
	}

	/// <summary>
	/// Check if an entity exists in the world (at the time this method is called).
	/// Note: Since commands are deferred, the entity state may change before execution.
	/// </summary>
	public bool Exists(ulong entityId)
	{
		if (_app == null)
			throw new InvalidOperationException("Commands has not been initialized.");

		return World.Exists(entityId);
	}

	/// <summary>
	/// Add a resource to the world
	/// </summary>
	public void InsertResource<T>(T resource) where T : notnull
	{
		_localCommands.Add(RentBox(new InsertResourceCommand<T>(resource)));
	}

	/// <summary>
	/// Remove a resource from the world
	/// </summary>
	public void RemoveResource<T>() where T : notnull
	{
		_localCommands.Add(RentBox(new RemoveResourceCommand(typeof(T))));
	}

	/// <summary>
	/// Check if a resource exists in the App (at the time this method is called).
	/// Note: Since commands are deferred, the resource state may change before execution.
	/// </summary>
	public bool HasResource<T>() where T : notnull
	{
		if (_app == null)
			throw new InvalidOperationException("Commands has not been wired to an App.");

		return _app.HasResource<T>();
	}

	/// <summary>
	/// Trigger a custom observer event.
	/// </summary>
	public void EmitTrigger<TEvent>(TEvent evt) where TEvent : struct
	{
		_localCommands.Add(RentBox(new TriggerEventCommand<TEvent>(evt)));
	}

	public void AddChild(ulong parentId, ulong childId)
	{
		_localCommands.Add(RentBox(new AddChildCommand(
			new DeferredEntityRef(-1, parentId),
			new DeferredEntityRef(-1, childId))));
	}

	public void AddChild(EntityCommands parent, EntityCommands child)
	{
		_localCommands.Add(RentBox(new AddChildCommand(parent.ToDeferredRef(), child.ToDeferredRef())));
	}

	public void AddChild(EntityCommands parent, ulong childId)
	{
		_localCommands.Add(RentBox(new AddChildCommand(parent.ToDeferredRef(), new DeferredEntityRef(-1, childId))));
	}

	public void AddChild(ulong parentId, EntityCommands child)
	{
		_localCommands.Add(RentBox(new AddChildCommand(new DeferredEntityRef(-1, parentId), child.ToDeferredRef())));
	}

	public void AddChildren(EntityCommands parent, ReadOnlySpan<ulong> childIds)
	{
		if (childIds.IsEmpty) return;
		foreach (var childId in childIds)
			AddChild(parent, childId);
	}

	public void AddChildren(ulong parentId, ReadOnlySpan<ulong> childIds)
	{
		if (childIds.IsEmpty) return;
		foreach (var childId in childIds)
			AddChild(parentId, childId);
	}

	internal ulong ResolveEntityId(in DeferredEntityRef entityRef)
	{
		return entityRef.SpawnIndex >= 0
			? GetSpawnedEntityId(entityRef.SpawnIndex)
			: entityRef.EntityId;
	}

	/// <summary>
	/// Internal method to queue a non-component deferred command. The struct is
	/// wrapped in a pooled <see cref="BoxedCommand{T}"/> so the per-frame
	/// allocation amortizes to zero after warmup.
	/// </summary>
	internal void QueueCommand<T>(in T command) where T : struct, IDeferredCommand
	{
		_localCommands.Add(RentBox(command));
	}

	/// <summary>
	/// Internal method to queue a component-related deferred command. Uses a
	/// dedicated <see cref="BoxedComponentCommand{T}"/> so observer-ordering
	/// logic can identify component commands via the
	/// <see cref="IBoxedComponentCommand"/> marker without reflection.
	/// </summary>
	internal void QueueComponentCommand<T>(in T command) where T : struct, IComponentCommand
	{
		_localCommands.Add(RentComponentBox(command));
	}

	/// <summary>
	/// Reflection-free check if a queued entry wraps a component insertion or removal command.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsComponentCommand(IDeferredCommand command)
	{
		// BoxedComponentCommand<T> implements IBoxedComponentCommand;
		// BoxedCommand<T> does not. No reflection involved — direct interface check.
		return command is IBoxedComponentCommand;
	}

	/// <summary>
	/// Internal method to insert an observer command at the right position.
	/// For spawned entities: Insert after the last SpawnEntityCommand
	/// For existing entities: Insert at the current position (before pending commands)
	/// This ensures observers are attached before subsequent Insert/Remove commands execute.
	/// </summary>
	internal void InsertObserverCommand<T>(in T command) where T : struct, IDeferredCommand
	{
		var boxed = RentBox(command);

		// Find the last SpawnEntityCommand and insert right after it.
		// SpawnEntityCommand isn't currently queued via Commands.Spawn() (entities are
		// allocated eagerly), so this loop is defensive — kept for parity with the
		// original ordering policy in case SpawnEntityCommand starts being queued later.
		int insertIndex = -1;
		for (int i = _localCommands.Count - 1; i >= 0; i--)
		{
			if (_localCommands[i] is BoxedCommand<SpawnEntityCommand>)
			{
				insertIndex = i + 1;
				break;
			}
		}

		// If no spawn command found, this is for an existing entity
		// Insert at the beginning of any pending component commands
		if (insertIndex == -1)
		{
			// Find the first component command from the end and insert before it
			for (int i = _localCommands.Count - 1; i >= 0; i--)
			{
				var cmd = _localCommands[i];
				if (IsComponentCommand(cmd))
				{
					insertIndex = i;
					break;
				}
			}

			// If no component commands, insert at current end
			if (insertIndex == -1)
				insertIndex = _localCommands.Count;
		}

		_localCommands.Insert(insertIndex, boxed);
	}
}



/// <summary>
/// Builder for entity operations in Commands
/// </summary>
public ref struct EntityCommands
{
	private readonly Commands _commands;
	private readonly int _spawnIndex; // Index into _spawnedEntityIds list (-1 if not spawned)
	private readonly ulong _entityId;

	internal EntityCommands(Commands commands, int spawnIndex)
	{
		_commands = commands;
		_spawnIndex = spawnIndex;
		_entityId = 0;
	}

	internal EntityCommands(Commands commands, ulong entityId)
	{
		_commands = commands;
		_spawnIndex = -1;
		_entityId = entityId;
	}

	/// <summary>
	/// The entity ID (returns 0 if entity hasn't been spawned yet)
	/// </summary>
	public ulong Id => _spawnIndex >= 0 ? _commands.GetSpawnedEntityId(_spawnIndex) : _entityId;

	/// <summary>
	/// The owning App associated with this entity's Commands. May be null if Commands
	/// has not been wired to an App (e.g. used outside the Bevy scheduler).
	/// </summary>
	internal readonly App? OwningApp => _commands.App;

	/// <summary>
	/// Add a component to the entity
	/// </summary>
	public readonly EntityCommands Insert<T>(T component) where T : struct
	{
		_commands.QueueComponentCommand(new InsertComponentCommand<T>(ToDeferredRef(), component));
		return this;
	}

	/// <summary>
	/// Add a tag component (zero-sized) to the entity
	/// </summary>
	public readonly EntityCommands Insert<T>() where T : struct
	{
		_commands.QueueComponentCommand(new InsertComponentCommand<T>(ToDeferredRef(), default));
		return this;
	}

	/// <summary>
	/// Remove a component from the entity
	/// </summary>
	public readonly EntityCommands Remove<T>() where T : struct
	{
		_commands.QueueComponentCommand(new RemoveComponentCommand<T>(ToDeferredRef()));
		return this;
	}

	/// <summary>
	/// Attach an existing entity as a child.
	/// </summary>
	public readonly EntityCommands AddChild(ulong childId)
	{
		_commands.AddChild(this, childId);
		return this;
	}

	/// <summary>
	/// Attach another entity builder as a child.
	/// </summary>
	public readonly EntityCommands AddChild(EntityCommands child)
	{
		_commands.AddChild(this, child);
		return this;
	}

	/// <summary>
	/// Attach multiple existing entities as children.
	/// </summary>
	public readonly EntityCommands AddChildren(ReadOnlySpan<ulong> childIds)
	{
		if (childIds.IsEmpty) return this;
		_commands.AddChildren(this, childIds);
		return this;
	}

	/// <summary>
	/// Attach multiple deferred children.
	/// </summary>
	internal readonly DeferredEntityRef ToDeferredRef() => new DeferredEntityRef(_spawnIndex, _entityId);

	/// <summary>
	/// Despawn the entity
	/// </summary>
	public readonly void Despawn()
	{
		_commands.QueueCommand(new DespawnEntityCommand(ToDeferredRef()));
	}

	/// <summary>
	/// Register an observer that reacts to triggers on this specific entity.
	/// The observer is stored as a component on the entity and automatically cleaned up when the entity is despawned.
	/// NOTE: The observer is attached immediately before subsequent Insert/Remove commands to ensure it sees those events.
	/// </summary>
	public readonly EntityCommands Observe<TTrigger>(Action<TTrigger> callback)
		where TTrigger : struct, ITrigger
	{
		// Insert the observer command at the front of the queue (right after Spawn if this is a spawned entity)
		// This ensures the observer is attached BEFORE any subsequent Insert/Remove commands
		_commands.InsertObserverCommand(new AttachObserverCommand<TTrigger>(ToDeferredRef(), callback));
		return this;
	}

	/// <summary>
	/// Internal method for system parameter support. Use the Observe overloads with system parameters instead.
	/// </summary>
	internal readonly EntityCommands ObserveWithWorld<TTrigger>(Action<World, TTrigger> callback)
		where TTrigger : struct, ITrigger
	{
		_commands.InsertObserverCommand(new AttachObserverWithWorldCommand<TTrigger>(ToDeferredRef(), callback));
		return this;
	}

	/// <summary>
	/// Emit a trigger for this specific entity.
	/// The entity ID is automatically injected - just provide the event data.
	/// Set <paramref name="propagate"/> to <c>true</c> to enable bubble walk up
	/// the parent hierarchy. Default is <c>false</c> (target-only) to match
	/// Bevy's opt-in propagation semantics.
	/// </summary>
	public readonly void EmitTrigger<TEvent>(TEvent evt, bool propagate = false)
		where TEvent : struct
	{
		// Automatically wrap the event with On<TEvent> and inject the entity ID
		_commands.QueueCommand(new EntityTriggerCommand<TEvent>(_entityId, evt, propagate));
	}

}

// ============================================================================
// Deferred Command Types - Commands queued for later execution
// ============================================================================

/// <summary>
/// Base interface for deferred commands
/// </summary>
internal interface IDeferredCommand
{
	void Execute(TinyEcs.World world, Commands commands);
}

/// <summary>
/// Marker interface for component-related commands (Insert/Remove).
/// Used to identify component commands without reflection.
/// </summary>
internal interface IComponentCommand : IDeferredCommand
{
}

/// <summary>
/// Marker for boxed wrappers that can be returned to a <see cref="Commands"/> pool.
/// </summary>
internal interface IPoolableBox
{
}

/// <summary>
/// Marker for boxed wrappers around component commands (Insert/Remove). Used by
/// observer-ordering logic to identify component commands in O(1) without
/// reflection or pattern-matching on every concrete generic type.
/// </summary>
internal interface IBoxedComponentCommand
{
}

/// <summary>
/// Sealed class wrapper that holds a struct command by value. Pooled per-Commands
/// instance so the per-frame "box a struct on List&lt;IDeferredCommand&gt;.Add" path
/// allocates once per concrete generic type and then reuses the wrapper across
/// frames. Mutable Value field lets the same wrapper carry different payloads
/// over its lifetime.
/// </summary>
internal sealed class BoxedCommand<T> : IDeferredCommand, IPoolableBox where T : struct, IDeferredCommand
{
	public T Value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Execute(TinyEcs.World world, Commands commands) => Value.Execute(world, commands);
}

/// <summary>
/// Like <see cref="BoxedCommand{T}"/> but also marks the wrapper as a component
/// command (Insert/Remove) so observer ordering can identify it cheaply.
/// </summary>
internal sealed class BoxedComponentCommand<T> : IDeferredCommand, IPoolableBox, IBoxedComponentCommand
	where T : struct, IComponentCommand
{
	public T Value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Execute(TinyEcs.World world, Commands commands) => Value.Execute(world, commands);
}

/// <summary>
/// Command to spawn a new entity
/// </summary>
internal readonly struct SpawnEntityCommand : IDeferredCommand
{
	private readonly int _spawnIndex;

	public SpawnEntityCommand(int spawnIndex)
	{
		_spawnIndex = spawnIndex;
	}

	public void Execute(TinyEcs.World world, Commands commands)
	{
		var entity = world.Entity();
		commands.SetSpawnedEntityId(_spawnIndex, entity.ID);
	}
}

/// <summary>
/// Command to insert a component on an entity
/// </summary>
internal readonly struct InsertComponentCommand<T> : IComponentCommand where T : struct
{
	private readonly DeferredEntityRef _entityRef;
	private readonly T _component;

	public InsertComponentCommand(DeferredEntityRef entityRef, T component)
	{
		_entityRef = entityRef;
		_component = component;
	}

	public void Execute(TinyEcs.World world, Commands commands)
	{
		var entityId = commands.ResolveEntityId(_entityRef);
		world.Set(entityId, _component);
	}
}

/// <summary>
/// Command to remove a component from an entity
/// </summary>
internal readonly struct RemoveComponentCommand<T> : IComponentCommand where T : struct
{
	private readonly DeferredEntityRef _entityRef;

	public RemoveComponentCommand(DeferredEntityRef entityRef)
	{
		_entityRef = entityRef;
	}

	public void Execute(TinyEcs.World world, Commands commands)
	{
		var entityId = commands.ResolveEntityId(_entityRef);
		world.Unset<T>(entityId);
	}
}

/// <summary>
/// Command to despawn an entity
/// </summary>
internal readonly struct DespawnEntityCommand : IDeferredCommand
{
	private readonly DeferredEntityRef _entityRef;

	public DespawnEntityCommand(DeferredEntityRef entityRef)
	{
		_entityRef = entityRef;
	}

	public void Execute(TinyEcs.World world, Commands commands)
	{
		var entityId = commands.ResolveEntityId(_entityRef);
		if (world.Exists(entityId))
			world.Delete(entityId);
	}
}

/// <summary>
/// Command to insert a resource
/// </summary>
internal readonly struct InsertResourceCommand<T> : IDeferredCommand where T : notnull
{
	private readonly T _resource;

	public InsertResourceCommand(T resource)
	{
		_resource = resource;
	}

	public void Execute(TinyEcs.World world, Commands commands)
	{
		var app = commands.App
			?? throw new InvalidOperationException("Commands.InsertResource requires Commands to be wired to an App.");
		app.AddResource(_resource);
	}
}

/// <summary>
/// Command to attach a child entity to a parent.
/// </summary>
internal readonly struct AddChildCommand : IDeferredCommand
{
	private readonly DeferredEntityRef _parent;
	private readonly DeferredEntityRef _child;

	public AddChildCommand(DeferredEntityRef parent, DeferredEntityRef child)
	{
		_parent = parent;
		_child = child;
	}

	public void Execute(TinyEcs.World world, Commands commands)
	{
		var parentId = commands.ResolveEntityId(_parent);
		var childId = commands.ResolveEntityId(_child);
		world.AddChild(parentId, childId);
	}
}

/// <summary>
/// Command to trigger a custom observer event
/// </summary>
internal readonly struct TriggerEventCommand<TEvent> : IDeferredCommand where TEvent : struct
{
	private readonly TEvent _event;

	public TriggerEventCommand(TEvent evt)
	{
		_event = evt;
	}

	public void Execute(TinyEcs.World world, Commands commands)
	{
		// Queue the emission until FlushObservers so observers see fully-merged world
		// state (component writes, parent links, etc. applied during the current scope).
		// Allocation-free: queued by value into a typed per-event-type queue. Global
		// emissions don't bubble (no entity to walk from) so propagate is unused.
		world.QueueCustomTrigger(entityId: 0, _event, propagate: false, isGlobal: true);
	}
}

/// <summary>
/// Command to trigger an entity-specific observer event.
/// Automatically wraps the event with On&lt;TEvent&gt; and injects the entity ID.
/// </summary>
internal readonly struct EntityTriggerCommand<TEvent> : IDeferredCommand
	where TEvent : struct
{
	private readonly ulong _entityId;
	private readonly TEvent _event;
	private readonly bool _propagate;

	public EntityTriggerCommand(ulong entityId, TEvent evt, bool propagate)
	{
		_entityId = entityId;
		_event = evt;
		_propagate = propagate;
	}

	public void Execute(TinyEcs.World world, Commands commands)
	{
		// Queue the emission until FlushObservers so the bubble walk sees fully-merged
		// world state. Allocation-free: the typed queue holds the entry by value;
		// stack cells for propagate/current are allocated inside the queue's Flush loop.
		world.QueueCustomTrigger(_entityId, _event, _propagate, isGlobal: false);
	}
}

/// <summary>
/// Command to remove a resource
/// </summary>
internal readonly struct RemoveResourceCommand : IDeferredCommand
{
	private readonly Type _resourceType;

	public RemoveResourceCommand(Type resourceType)
	{
		_resourceType = resourceType;
	}

	public void Execute(TinyEcs.World world, Commands commands)
	{
		var app = commands.App
			?? throw new InvalidOperationException("Commands.RemoveResource requires Commands to be wired to an App.");
		app.RemoveResourceByType(_resourceType);
	}
}

/// <summary>
/// Command to attach an entity-specific observer to an entity.
/// Observers are stored as EntityObservers component on the entity.
/// </summary>
internal readonly struct AttachObserverCommand<TTrigger> : IDeferredCommand
	where TTrigger : struct, ITrigger
{
	private readonly DeferredEntityRef _entityRef;
	private readonly Action<TTrigger> _callback;

	public AttachObserverCommand(DeferredEntityRef entityRef, Action<TTrigger> callback)
	{
		_entityRef = entityRef;
		_callback = callback;
	}

	public void Execute(TinyEcs.World world, Commands commands)
	{
		var entityId = commands.ResolveEntityId(_entityRef);

		// Auto-register component types
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		// Get or create EntityObservers component
		if (!world.Has<EntityObservers>(entityId))
		{
			world.Set(entityId, new EntityObservers
			{
				Lists = new List<ITypedObserverList>()
			});
		}

		ref var entityObservers = ref world.Get<EntityObservers>(entityId);
		if (entityObservers.Lists == null)
		{
			entityObservers.Lists = new List<ITypedObserverList>();
		}

		// Add the callback to this entity's typed list for TTrigger.
		// Wrap to ignore the World parameter to match the typed list signature.
		var callback = _callback;
		var list = ObserverExtensions.GetOrCreateEntityList<TTrigger>(entityObservers.Lists);
		list.Callbacks.Add((w, trigger) => callback(trigger));
	}
}

/// <summary>
/// Command to attach an entity-specific observer with World access.
/// Used internally for system parameter support in entity observers.
/// </summary>
internal readonly struct AttachObserverWithWorldCommand<TTrigger> : IDeferredCommand
	where TTrigger : struct, ITrigger
{
	private readonly DeferredEntityRef _entityRef;
	private readonly Action<World, TTrigger> _callback;

	public AttachObserverWithWorldCommand(DeferredEntityRef entityRef, Action<World, TTrigger> callback)
	{
		_entityRef = entityRef;
		_callback = callback;
	}

	public void Execute(World world, Commands commands)
	{
		var entityId = commands.ResolveEntityId(_entityRef);

		// Auto-register component types
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		// Get or create EntityObservers component
		if (!world.Has<EntityObservers>(entityId))
		{
			world.Set(entityId, new EntityObservers
			{
				Lists = new List<ITypedObserverList>()
			});
		}

		ref var entityObservers = ref world.Get<EntityObservers>(entityId);
		if (entityObservers.Lists == null)
		{
			entityObservers.Lists = new List<ITypedObserverList>();
		}

		var list = ObserverExtensions.GetOrCreateEntityList<TTrigger>(entityObservers.Lists);
		list.Callbacks.Add(_callback);
	}
}

// ============================================================================
// Query Parameter - Typed ECS queries (standalone implementation)
// ============================================================================

/// <summary>
/// Typed query parameter for iterating entities with specific components.
/// Standalone Bevy-style query that directly uses the low-level TinyEcs.Query infrastructure.
/// </summary>
public class Query<TQueryData> : Query<TQueryData, Empty>
	where TQueryData : struct, IData<TQueryData>, IQueryIterator<TQueryData>, IQueryComponentAccess, allows ref struct
{
	public Query() : base() { }
}

/// <summary>
/// Typed query parameter with filtering for iterating entities with specific components.
/// Standalone implementation that bypasses the Bevy.cs Query wrapper.
/// </summary>
public class Query<TQueryData, TQueryFilter> : ISystemParam
	where TQueryData : struct, IData<TQueryData>, IQueryIterator<TQueryData>, IQueryComponentAccess, allows ref struct
	where TQueryFilter : struct, IFilter<TQueryFilter>, IQueryFilterAccess, allows ref struct
{
	private TinyEcs.Query? _lowLevelQuery;
	private TinyEcs.World? _world;
	private static readonly SystemParamAccess _access = BuildAccess();

	private static SystemParamAccess BuildAccess()
	{
		var access = new SystemParamAccess();

		// Add read/write access from data components
		foreach (var component in TQueryData.ReadComponents)
			access.ReadResources.Add(component);
		foreach (var component in TQueryData.WriteComponents)
			access.WriteResources.Add(component);

		// Add read/write access from filter components
		foreach (var component in TQueryFilter.ReadComponents)
			access.ReadResources.Add(component);
		foreach (var component in TQueryFilter.WriteComponents)
			access.WriteResources.Add(component);

		return access;
	}

	public void Initialize(App app)
	{
		// Initialization handled in Fetch
	}

	public void Fetch(App app)
	{
		var world = app.GetWorld();
		_world = world;
		if (_lowLevelQuery == null)
		{
			// Build query directly using QueryBuilder (no Bevy.cs dependency)
			var builder = world.QueryBuilder();
			TQueryData.Build(builder);
			TQueryFilter.Build(builder);
			_lowLevelQuery = builder.Build();
		}
	}

	/// <summary>
	/// Access to the underlying low-level query
	/// </summary>
	public TinyEcs.Query Inner => _lowLevelQuery!;

	// ============================================================================
	// Query Methods (standalone implementation)
	// ============================================================================

	/// <summary>
	/// Returns the number of entities matching this query
	/// </summary>
	public int Count() => _lowLevelQuery!.Count();

	/// <summary>
	/// Checks if an entity with the given ID matches this query
	/// </summary>
	public bool Contains(ulong id)
	{
		var iter = GetIter(id);
		return iter.MoveNext();
	}

	/// <summary>
	/// Gets the query data for a specific entity ID
	/// </summary>
	public TQueryData Get(ulong id)
	{
		var iter = GetIter(id);
		var success = iter.MoveNext();
		return success ? iter.Current : default;
	}

	/// <summary>
	/// Gets a single entity matching this query (throws if not exactly one match)
	/// </summary>
	public TQueryData Single()
	{
		if (!TrySingle(out var value))
			throw new InvalidOperationException("'Single' is not matching any entity.");

		return value;
	}

	/// <summary>
	/// Attempts to get a single entity matching this query.
	/// </summary>
	public bool TrySingle(out TQueryData value)
	{
		var iter = GetEnumerator();

		if (!iter.MoveNext())
		{
			value = default;
			return false;
		}

		value = iter.Current;

		if (iter.MoveNext())
			throw new InvalidOperationException("'Single' matched more than one entity.");

		return true;
	}

	/// <summary>
	/// Gets an enumerator for iterating over all matching entities
	/// </summary>
	public BevyQueryIter<TQueryData, TQueryFilter> GetEnumerator() => GetIter();

	private BevyQueryIter<TQueryData, TQueryFilter> GetIter(ulong id = 0)
	{
		// Use world's tick system for change detection
		// World.Update() is called at the start of Run()/RunStartup(), then systems execute with that tick
		// We check for changes in the current frame: [currentTick-1, currentTick)
		// This detects components modified with the current tick
		uint currentTick = _world!.CurrentTick;
		uint lastTick = currentTick > 0 ? currentTick - 1 : 0;

		var rawIter = id == 0 ? _lowLevelQuery!.Iter() : _lowLevelQuery!.Iter(id);
		return new BevyQueryIter<TQueryData, TQueryFilter>(lastTick, currentTick, rawIter);
	}

	/// <summary>
	/// Gets the access pattern for this query (used for parallel execution scheduling)
	/// </summary>
	public SystemParamAccess GetAccess() => _access;
}

public class Single<TQueryData, TQueryFilter> : Query<TQueryData, TQueryFilter>
	where TQueryData : struct, IData<TQueryData>, IQueryIterator<TQueryData>, IQueryComponentAccess, allows ref struct
	where TQueryFilter : struct, IFilter<TQueryFilter>, IQueryFilterAccess, allows ref struct
{
	public Single() : base() { }

	public bool TryGet(out TQueryData result)
	{
		if (base.TrySingle(out result))
		{
			return true;
		}

		return false;
	}

	public TQueryData Get() => base.Single();
}

public sealed class Single<TQueryData> : Single<TQueryData, Empty>
	where TQueryData : struct, IData<TQueryData>, IQueryIterator<TQueryData>, IQueryComponentAccess, allows ref struct
{
}

// ============================================================================
// Query Iterator - Standalone iterator for Bevy queries
// ============================================================================

/// <summary>
/// Iterator for Bevy-style queries. Wraps QueryIterator and provides typed iteration.
/// </summary>
public ref struct BevyQueryIter<TQueryData, TQueryFilter>
	where TQueryData : struct, IData<TQueryData>, IQueryIterator<TQueryData>, allows ref struct
	where TQueryFilter : struct, IFilter<TQueryFilter>, allows ref struct
{
	private TQueryData _dataIterator;
	private TQueryFilter _filterIterator;

	internal BevyQueryIter(uint lastTick, uint currentTick, TinyEcs.QueryIterator iterator)
	{
		_dataIterator = TQueryData.CreateIterator(iterator);
		_filterIterator = TQueryFilter.CreateIterator(iterator);
		// Set ticks for change detection (Changed<T>/Added<T> filters)
		_filterIterator.SetTicks(lastTick, currentTick);
	}

	[UnscopedRef]
	public ref TQueryData Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref _dataIterator;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool MoveNext()
	{
		// JIT constant per specialization — dead-code-eliminates the filter call when no filter is used.
		if (typeof(TQueryFilter) == typeof(Empty))
			return _dataIterator.MoveNext();

		while (true)
		{
			if (!_dataIterator.MoveNext())
				return false;

			if (!_filterIterator.MoveNext())
				continue;

			return true;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly BevyQueryIter<TQueryData, TQueryFilter> GetEnumerator() => this;
}

// ============================================================================
// Parameterized System - Supports dependency injection
// ============================================================================

public class ParameterizedSystem : ISystem
{
	private readonly Action<TinyEcs.World> _systemFn;
	private readonly ISystemParam[] _parameters;
	private bool _initialized = false;
	private SystemParamAccess? _cachedAccess;
	private App? _app;

	public ParameterizedSystem(Action<TinyEcs.World> systemFn, params ISystemParam[] parameters)
	{
		_systemFn = systemFn;
		_parameters = parameters;
	}

	/// <summary>
	/// Attach the owning <see cref="App"/> to this system. The app reference is
	/// passed to each parameter on <see cref="ISystemParam.Initialize"/> and
	/// <see cref="ISystemParam.Fetch"/>. Safe to call once during system
	/// registration; subsequent calls overwrite the previous app.
	/// </summary>
	internal void SetApp(App app)
	{
		_app = app;
	}

	public void Run(TinyEcs.World world)
	{
		if (_app is null)
			throw new InvalidOperationException("ParameterizedSystem has not been wired to an App. Use App.AddSystem to register it.");

		if (!_initialized)
		{
			// Initialize all parameters once
			foreach (var param in _parameters)
			{
				param.Initialize(_app);
			}
			_initialized = true;
		}

		// Fetch latest data for all parameters
		foreach (var param in _parameters)
		{
			param.Fetch(_app);
		}

		// Run the system
		_systemFn(world);

		// Apply any queued commands (thread-safe per-system)
		foreach (var param in _parameters)
		{
			if (param is Commands commands)
			{
				commands.Apply();
			}
		}
	}

	/// <summary>
	/// Get the combined access pattern of all parameters
	/// </summary>
	public SystemParamAccess GetAccess()
	{
		if (_cachedAccess != null)
			return _cachedAccess;

		var combinedAccess = new SystemParamAccess();
		foreach (var param in _parameters)
		{
			var access = param.GetAccess();
			foreach (var read in access.ReadResources)
				combinedAccess.ReadResources.Add(read);
			foreach (var write in access.WriteResources)
				combinedAccess.WriteResources.Add(write);
		}

		_cachedAccess = combinedAccess;
		return _cachedAccess;
	}
}

// ============================================================================
// System Function Adapters - Convert functions with params to ISystem
// ============================================================================


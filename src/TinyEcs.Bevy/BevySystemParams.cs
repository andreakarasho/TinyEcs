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
/// Base interface for all system parameters that can be injected into systems
/// </summary>
public interface ISystemParam
{
	void Initialize(TinyEcs.World world);
	void Fetch(TinyEcs.World world);

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

	public void Initialize(TinyEcs.World world)
	{
		_box = null;
	}

	public void Fetch(TinyEcs.World world)
	{
		_box = world.GetResourceBox<T>();
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

	public void Initialize(TinyEcs.World world)
	{
		_box = null;
	}

	public void Fetch(TinyEcs.World world)
	{
		_box = world.GetResourceBox<T>();
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
	public T Value { get; set; } = new T();

	public void Initialize(TinyEcs.World world)
	{
		// Local state is initialized once and persists
		Value = new T();
	}

	public void Fetch(TinyEcs.World world)
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

	public void Initialize(TinyEcs.World world)
	{
		_channel = world.GetEventChannel<T>();
	}

	public void Fetch(TinyEcs.World world)
	{
		_events.Clear();
		_channel ??= world.GetEventChannel<T>();
		_channel.CopyEvents(ref _lastEpoch, ref _lastReadIndex, _events);
	}

	/// <summary>
	/// Iterate over all events of type T that occurred since the last fetch
	/// </summary>
	public IEnumerable<T> Read()
	{
		return _events;
	}

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

	public void Initialize(TinyEcs.World world)
	{
		_channel = world.GetEventChannel<T>();
	}

	public void Fetch(TinyEcs.World world)
	{
		_channel ??= world.GetEventChannel<T>();
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
	private TinyEcs.World? _world;
	private readonly List<IDeferredCommand> _localCommands = new();
	private readonly List<ulong> _spawnedEntityIds = new();

	public void Initialize(TinyEcs.World world)
	{
		_world = world;
	}

	public void Fetch(TinyEcs.World world)
	{
		_world = world;
		_localCommands.Clear();
		_spawnedEntityIds.Clear();
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

		// Apply all commands in order
		foreach (var cmd in _localCommands)
		{
			cmd.Execute(_world!, this);
		}

		_localCommands.Clear();
	}

	/// <summary>
	/// Spawn a new entity and return a builder for adding components
	/// </summary>
	public EntityCommands Spawn()
	{
		// Reserve a slot in the list for the entity ID (will be filled when command executes)
		int index = _spawnedEntityIds.Count;
		_spawnedEntityIds.Add(0); // Placeholder - will be filled in Apply()
		_localCommands.Add(new SpawnEntityCommand(index));
		return new EntityCommands(this, index);
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
		if (_world == null)
			throw new InvalidOperationException("Commands has not been initialized.");

		if (_world.Exists(entityId))
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
		if (_world == null)
			throw new InvalidOperationException("Commands has not been initialized.");

		return _world.Exists(entityId);
	}

	/// <summary>
	/// Add a resource to the world
	/// </summary>
	public void InsertResource<T>(T resource) where T : notnull
	{
		_localCommands.Add(new InsertResourceCommand<T>(resource));
	}

	/// <summary>
	/// Remove a resource from the world
	/// </summary>
	public void RemoveResource<T>() where T : notnull
	{
		_localCommands.Add(new RemoveResourceCommand(typeof(T)));
	}

	/// <summary>
	/// Check if a resource exists in the world (at the time this method is called).
	/// Note: Since commands are deferred, the resource state may change before execution.
	/// </summary>
	public bool HasResource<T>() where T : notnull
	{
		if (_world == null)
			throw new InvalidOperationException("Commands has not been initialized.");

		return _world.HasResource<T>();
	}

	/// <summary>
	/// Trigger a custom observer event.
	/// </summary>
	public void EmitTrigger<TEvent>(TEvent evt) where TEvent : struct
	{
		_localCommands.Add(new TriggerEventCommand<TEvent>(evt));
	}

	public void AddChild(ulong parentId, ulong childId)
	{
		_localCommands.Add(new AddChildCommand(
			new DeferredEntityRef(-1, parentId),
			new DeferredEntityRef(-1, childId)));
	}

	public void AddChild(EntityCommands parent, EntityCommands child)
	{
		_localCommands.Add(new AddChildCommand(parent.ToDeferredRef(), child.ToDeferredRef()));
	}

	public void AddChild(EntityCommands parent, ulong childId)
	{
		_localCommands.Add(new AddChildCommand(parent.ToDeferredRef(), new DeferredEntityRef(-1, childId)));
	}

	public void AddChild(ulong parentId, EntityCommands child)
	{
		_localCommands.Add(new AddChildCommand(new DeferredEntityRef(-1, parentId), child.ToDeferredRef()));
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
	/// Internal method to queue a deferred command
	/// </summary>
	internal void QueueCommand(IDeferredCommand command)
	{
		_localCommands.Add(command);
	}

	/// <summary>
	/// Internal method to insert an observer command at the right position.
	/// For spawned entities: Insert after the last SpawnEntityCommand
	/// For existing entities: Insert at the current position (before pending commands)
	/// This ensures observers are attached before subsequent Insert/Remove commands execute.
	/// </summary>
	internal void InsertObserverCommand(IDeferredCommand command)
	{
		// Find the last SpawnEntityCommand and insert right after it
		int insertIndex = -1;
		for (int i = _localCommands.Count - 1; i >= 0; i--)
		{
			if (_localCommands[i] is SpawnEntityCommand)
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
				var cmdType = _localCommands[i].GetType().Name;
				if (cmdType.Contains("InsertComponent") || cmdType.Contains("RemoveComponent"))
				{
					insertIndex = i;
					break;
				}
			}

			// If no component commands, insert at current end
			if (insertIndex == -1)
				insertIndex = _localCommands.Count;
		}

		_localCommands.Insert(insertIndex, command);
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
	/// Add a component to the entity
	/// </summary>
	public readonly EntityCommands Insert<T>(T component) where T : struct
	{
		if (_spawnIndex >= 0)
			_commands.QueueCommand(new InsertComponentCommand<T>(_commands, _spawnIndex, component));
		else
			_commands.QueueCommand(new InsertComponentCommand<T>(_entityId, component));
		return this;
	}

	/// <summary>
	/// Add a tag component (zero-sized) to the entity
	/// </summary>
	public readonly EntityCommands Insert<T>() where T : struct
	{
		if (_spawnIndex >= 0)
			_commands.QueueCommand(new InsertComponentCommand<T>(_commands, _spawnIndex, default));
		else
			_commands.QueueCommand(new InsertComponentCommand<T>(_entityId, default));
		return this;
	}

	/// <summary>
	/// Remove a component from the entity
	/// </summary>
	public readonly EntityCommands Remove<T>() where T : struct
	{
		if (_spawnIndex >= 0)
			_commands.QueueCommand(new RemoveComponentCommand<T>(_commands, _spawnIndex));
		else
			_commands.QueueCommand(new RemoveComponentCommand<T>(_entityId));
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
		if (_spawnIndex >= 0)
			_commands.QueueCommand(new DespawnEntityCommand(_commands, _spawnIndex));
		else
			_commands.QueueCommand(new DespawnEntityCommand(_entityId));
	}

	/// <summary>
	/// Register an observer that reacts to triggers on this specific entity.
	/// The observer is stored as a component on the entity and automatically cleaned up when the entity is despawned.
	/// NOTE: The observer is attached immediately before subsequent Insert/Remove commands to ensure it sees those events.
	/// </summary>
	public readonly EntityCommands Observe<TTrigger>(Action<TinyEcs.World, TTrigger> callback)
		where TTrigger : struct, ITrigger
	{
		// Insert the observer command at the front of the queue (right after Spawn if this is a spawned entity)
		// This ensures the observer is attached BEFORE any subsequent Insert/Remove commands
		_commands.InsertObserverCommand(new AttachObserverCommand<TTrigger>(ToDeferredRef(), callback));
		return this;
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
internal readonly struct InsertComponentCommand<T> : IDeferredCommand where T : struct
{
	private readonly int _spawnIndex; // -1 if existing entity
	private readonly ulong _entityId;
	private readonly T _component;

	// Constructor for spawned entities (uses index)
	public InsertComponentCommand(Commands commands, int spawnIndex, T component)
	{
		_spawnIndex = spawnIndex;
		_entityId = 0;
		_component = component;
	}

	// Constructor for existing entities (uses direct ID)
	public InsertComponentCommand(ulong entityId, T component)
	{
		_spawnIndex = -1;
		_entityId = entityId;
		_component = component;
	}

	public void Execute(TinyEcs.World world, Commands commands)
	{
		var entityId = _spawnIndex >= 0 ? commands.GetSpawnedEntityId(_spawnIndex) : _entityId;
		world.Set(entityId, _component);
	}
}

/// <summary>
/// Command to remove a component from an entity
/// </summary>
internal readonly struct RemoveComponentCommand<T> : IDeferredCommand where T : struct
{
	private readonly int _spawnIndex; // -1 if existing entity
	private readonly ulong _entityId;

	// Constructor for spawned entities (uses index)
	public RemoveComponentCommand(Commands commands, int spawnIndex)
	{
		_spawnIndex = spawnIndex;
		_entityId = 0;
	}

	// Constructor for existing entities (uses direct ID)
	public RemoveComponentCommand(ulong entityId)
	{
		_spawnIndex = -1;
		_entityId = entityId;
	}

	public void Execute(TinyEcs.World world, Commands commands)
	{
		var entityId = _spawnIndex >= 0 ? commands.GetSpawnedEntityId(_spawnIndex) : _entityId;
		world.Unset<T>(entityId);
	}
}

/// <summary>
/// Command to despawn an entity
/// </summary>
internal readonly struct DespawnEntityCommand : IDeferredCommand
{
	private readonly int _spawnIndex; // -1 if existing entity
	private readonly ulong _entityId;

	// Constructor for spawned entities (uses index)
	public DespawnEntityCommand(Commands commands, int spawnIndex)
	{
		_spawnIndex = spawnIndex;
		_entityId = 0;
	}

	// Constructor for existing entities (uses direct ID)
	public DespawnEntityCommand(ulong entityId)
	{
		_spawnIndex = -1;
		_entityId = entityId;
	}

	public void Execute(TinyEcs.World world, Commands commands)
	{
		var entityId = _spawnIndex >= 0 ? commands.GetSpawnedEntityId(_spawnIndex) : _entityId;
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
		world.AddResource(_resource);
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
		world.EmitTrigger(new On<TEvent>(_event));
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
		world.GetState().Resources.Remove(_resourceType);
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
	private readonly Action<TinyEcs.World, TTrigger> _callback;

	public AttachObserverCommand(DeferredEntityRef entityRef, Action<TinyEcs.World, TTrigger> callback)
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
				Observers = new List<IObserver>()
			});
		}

		// Add the observer to the entity's observer list
		ref var entityObservers = ref world.Get<EntityObservers>(entityId);
		if (entityObservers.Observers == null)
		{
			entityObservers.Observers = new List<IObserver>();
		}

		entityObservers.Observers.Add(new Observer<TTrigger>(_callback));
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

	public void Initialize(TinyEcs.World world)
	{
		// Initialization handled in Fetch
	}

	public void Fetch(TinyEcs.World world)
	{
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

	public ParameterizedSystem(Action<TinyEcs.World> systemFn, params ISystemParam[] parameters)
	{
		_systemFn = systemFn;
		_parameters = parameters;
	}

	public void Run(TinyEcs.World world)
	{
		if (!_initialized)
		{
			// Initialize all parameters once
			foreach (var param in _parameters)
			{
				param.Initialize(world);
			}
			_initialized = true;
		}

		// Fetch latest data for all parameters
		foreach (var param in _parameters)
		{
			param.Fetch(world);
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


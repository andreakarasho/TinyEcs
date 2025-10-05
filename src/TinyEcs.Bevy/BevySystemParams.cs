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

// ============================================================================
// Res<T> - Immutable resource access
// ============================================================================

/// <summary>
/// Immutable reference to a resource. Use for read-only access.
/// </summary>
public class Res<T> : ISystemParam where T : notnull
{
	private TinyEcs.World? _world;
	public T Value { get; private set; } = default!;

	public void Initialize(TinyEcs.World world)
	{
		_world = world;
	}

	public void Fetch(TinyEcs.World world)
	{
		Value = world.GetResource<T>();
	}

	public SystemParamAccess GetAccess()
	{
		var access = new SystemParamAccess();
		access.ReadResources.Add(typeof(T));
		return access;
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
	private TinyEcs.World? _world;
	public T Value { get; private set; } = default!;

	public void Initialize(TinyEcs.World world)
	{
		_world = world;
	}

	public void Fetch(TinyEcs.World world)
	{
		Value = world.GetResource<T>();
	}

	public SystemParamAccess GetAccess()
	{
		var access = new SystemParamAccess();
		access.WriteResources.Add(typeof(T));
		return access;
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
	public T Value { get; private set; } = new T();

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
	private readonly List<DeferredCommand> _localCommands = new();
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
	/// Get entity commands for an existing entity
	/// </summary>
	public EntityCommands Entity(ulong entityId)
	{
		return new EntityCommands(this, entityId);
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
	/// Internal method to queue a deferred command
	/// </summary>
	internal void QueueCommand(DeferredCommand command)
	{
		_localCommands.Add(command);
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
	/// Despawn the entity
	/// </summary>
	public readonly void Despawn()
	{
		if (_spawnIndex >= 0)
			_commands.QueueCommand(new DespawnEntityCommand(_commands, _spawnIndex));
		else
			_commands.QueueCommand(new DespawnEntityCommand(_entityId));
	}
}

// ============================================================================
// Deferred Command Types - Commands queued for later execution
// ============================================================================

/// <summary>
/// Base interface for deferred commands
/// </summary>
internal interface DeferredCommand
{
	void Execute(TinyEcs.World world, Commands commands);
}

/// <summary>
/// Command to spawn a new entity
/// </summary>
internal readonly struct SpawnEntityCommand : DeferredCommand
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
internal readonly struct InsertComponentCommand<T> : DeferredCommand where T : struct
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
internal readonly struct RemoveComponentCommand<T> : DeferredCommand where T : struct
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
internal readonly struct DespawnEntityCommand : DeferredCommand
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
internal readonly struct InsertResourceCommand<T> : DeferredCommand where T : notnull
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
/// Command to remove a resource
/// </summary>
internal readonly struct RemoveResourceCommand : DeferredCommand
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
		if (_lowLevelQuery!.Count() != 1)
			throw new InvalidOperationException("'Single' must match one and only one entity.");

		var iter = GetEnumerator();
		if (!iter.MoveNext())
			throw new InvalidOperationException("'Single' is not matching any entity.");

		return iter.Current;
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

public static class SystemFunctionAdapters
{
	// 1 parameter
	public static ISystem Create<T1>(Action<T1> systemFn)
		where T1 : ISystemParam, new()
	{
		var p1 = new T1();
		return new ParameterizedSystem(
			world => systemFn(p1),
			p1
		);
	}

	// 2 parameters
	public static ISystem Create<T1, T2>(Action<T1, T2> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		return new ParameterizedSystem(
			world => systemFn(p1, p2),
			p1, p2
		);
	}

	// 3 parameters
	public static ISystem Create<T1, T2, T3>(Action<T1, T2, T3> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3),
			p1, p2, p3
		);
	}

	// 4 parameters
	public static ISystem Create<T1, T2, T3, T4>(Action<T1, T2, T3, T4> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4),
			p1, p2, p3, p4
		);
	}

	// 5 parameters
	public static ISystem Create<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5),
			p1, p2, p3, p4, p5
		);
	}

	// 6 parameters
	public static ISystem Create<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6),
			p1, p2, p3, p4, p5, p6
		);
	}

	// 7 parameters
	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6, p7),
			p1, p2, p3, p4, p5, p6, p7
		);
	}

	// 8 parameters
	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
		where T8 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8),
			p1, p2, p3, p4, p5, p6, p7, p8
		);
	}

	// 9 parameters
	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9();
		return new ParameterizedSystem(world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9), p1, p2, p3, p4, p5, p6, p7, p8, p9);
	}

	// 10 parameters
	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10();
		return new ParameterizedSystem(world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10), p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
	}

	// 11 parameters
	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11();
		return new ParameterizedSystem(world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11), p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11);
	}

	// 12 parameters
	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12();
		return new ParameterizedSystem(world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12), p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12);
	}

	// 13 parameters
	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
		where T13 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13();
		return new ParameterizedSystem(world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13), p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13);
	}

	// 14 parameters
	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
		where T13 : ISystemParam, new() where T14 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14();
		return new ParameterizedSystem(world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14), p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14);
	}

	// 15 parameters
	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
		where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15();
		return new ParameterizedSystem(world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15), p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15);
	}

	// 16 parameters
	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
		where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new() where T16 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15(); var p16 = new T16();
		return new ParameterizedSystem(world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16), p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16);
	}
}

// ============================================================================
// App Extensions for System Parameters
// ============================================================================

public static class AppSystemParamExtensions
{
	// 1 parameter
	public static ISystemStageSelector AddSystem<T1>(this App app, Action<T1> systemFn)
		where T1 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	// 2 parameters
	public static ISystemStageSelector AddSystem<T1, T2>(this App app, Action<T1, T2> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	// 3 parameters
	public static ISystemStageSelector AddSystem<T1, T2, T3>(this App app, Action<T1, T2, T3> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	// 4 parameters
	public static ISystemStageSelector AddSystem<T1, T2, T3, T4>(this App app, Action<T1, T2, T3, T4> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	// 5 parameters
	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5>(this App app, Action<T1, T2, T3, T4, T5> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	// 6 parameters
	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6>(this App app, Action<T1, T2, T3, T4, T5, T6> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	// 7 parameters
	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7>(this App app, Action<T1, T2, T3, T4, T5, T6, T7> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	// 8 parameters
	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		=> app.AddSystem(SystemFunctionAdapters.Create(systemFn));

	// 9-16 parameters
	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new()
		=> app.AddSystem(SystemFunctionAdapters.Create(systemFn));

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new()
		=> app.AddSystem(SystemFunctionAdapters.Create(systemFn));

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new()
		=> app.AddSystem(SystemFunctionAdapters.Create(systemFn));

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
		=> app.AddSystem(SystemFunctionAdapters.Create(systemFn));

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
		where T13 : ISystemParam, new()
		=> app.AddSystem(SystemFunctionAdapters.Create(systemFn));

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
		where T13 : ISystemParam, new() where T14 : ISystemParam, new()
		=> app.AddSystem(SystemFunctionAdapters.Create(systemFn));

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
		where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new()
		=> app.AddSystem(SystemFunctionAdapters.Create(systemFn));

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
		where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new() where T16 : ISystemParam, new()
		=> app.AddSystem(SystemFunctionAdapters.Create(systemFn));

	// ============================================================================
	// Stage-first API overloads (simpler, more explicit)
	// ============================================================================

	public static App AddSystem<T1>(this App app, Stage stage, Action<T1> systemFn)
		where T1 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));

	public static App AddSystem<T1, T2>(this App app, Stage stage, Action<T1, T2> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));

	public static App AddSystem<T1, T2, T3>(this App app, Stage stage, Action<T1, T2, T3> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));

	public static App AddSystem<T1, T2, T3, T4>(this App app, Stage stage, Action<T1, T2, T3, T4> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));

	public static App AddSystem<T1, T2, T3, T4, T5>(this App app, Stage stage, Action<T1, T2, T3, T4, T5> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));

	public static App AddSystem<T1, T2, T3, T4, T5, T6>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
		where T13 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
		where T13 : ISystemParam, new() where T14 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
		where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
		where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
		where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
		where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new() where T16 : ISystemParam, new()
		=> app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
}

// ============================================================================
// RunIf Extensions for System Parameters
// ============================================================================

public static class RunIfSystemParamExtensions
{
	// 1 parameter
	public static ISystemConfigurator RunIf<T1>(this ISystemConfigurator configurator, Func<T1, bool> condition)
		where T1 : ISystemParam, new()
	{
		var p1 = new T1();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world);
			return condition(p1);
		});
	}

	// 2 parameters
	public static ISystemConfigurator RunIf<T1, T2>(this ISystemConfigurator configurator, Func<T1, T2, bool> condition)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world);
			p2.Fetch(world);
			return condition(p1, p2);
		});
	}

	// 3 parameters
	public static ISystemConfigurator RunIf<T1, T2, T3>(this ISystemConfigurator configurator, Func<T1, T2, T3, bool> condition)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world);
			p2.Fetch(world);
			p3.Fetch(world);
			return condition(p1, p2, p3);
		});
	}

	// 4 parameters
	public static ISystemConfigurator RunIf<T1, T2, T3, T4>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4();
		return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); return condition(p1, p2, p3, p4); });
	}

	// 5-16 parameters
	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new()
	{ var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); return condition(p1, p2, p3, p4, p5); }); }

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new()
	{ var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); return condition(p1, p2, p3, p4, p5, p6); }); }

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new()
	{ var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7); }); }

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
	{ var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8); }); }

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new()
	{ var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9); }); }

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new()
	{ var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10); }); }

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new()
	{ var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11); }); }

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
	{ var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12); }); }

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new()
	{ var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13); }); }

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new()
	{ var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14); }); }

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new()
	{ var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world); p15.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15); }); }

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new() where T16 : ISystemParam, new()
	{ var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15(); var p16 = new T16(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world); p15.Fetch(world); p16.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16); }); }

	// ISystemConfiguratorLabeled overloads
	public static ISystemConfiguratorLabeled RunIf<T1>(this ISystemConfiguratorLabeled configurator, Func<T1, bool> condition)
		where T1 : ISystemParam, new()
	{
		var p1 = new T1();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world);
			return condition(p1);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, bool> condition)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world);
			p2.Fetch(world);
			return condition(p1, p2);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, bool> condition)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world);
			p2.Fetch(world);
			p3.Fetch(world);
			return condition(p1, p2, p3);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
	{ var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); return condition(p1, p2, p3, p4); }); }

	// 5-16 param overloads
	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); return condition(p1, p2, p3, p4, p5); }); }
	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); return condition(p1, p2, p3, p4, p5, p6); }); }
	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7); }); }
	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8); }); }
	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9); }); }
	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10); }); }
	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11); }); }
	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12); }); }
	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13); }); }
	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14); }); }
	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world); p15.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15); }); }
	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new() where T16 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15(); var p16 = new T16(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world); p15.Fetch(world); p16.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16); }); }

	// ISystemConfiguratorOrdered overloads
	public static ISystemConfiguratorOrdered RunIf<T1>(this ISystemConfiguratorOrdered configurator, Func<T1, bool> condition)
		where T1 : ISystemParam, new()
	{
		var p1 = new T1();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world);
			return condition(p1);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, bool> condition)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world);
			p2.Fetch(world);
			return condition(p1, p2);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, bool> condition)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world);
			p2.Fetch(world);
			p3.Fetch(world);
			return condition(p1, p2, p3);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
	{ var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); return condition(p1, p2, p3, p4); }); }

	// 5-16 param overloads
	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); return condition(p1, p2, p3, p4, p5); }); }
	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); return condition(p1, p2, p3, p4, p5, p6); }); }
	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7); }); }
	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8); }); }
	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9); }); }
	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10); }); }
	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11); }); }
	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12); }); }
	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13); }); }
	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14); }); }
	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world); p15.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15); }); }
	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, bool> condition) where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new() where T16 : ISystemParam, new() { var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15(); var p16 = new T16(); return configurator.RunIf(world => { p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world); p15.Fetch(world); p16.Fetch(world); return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16); }); }
}


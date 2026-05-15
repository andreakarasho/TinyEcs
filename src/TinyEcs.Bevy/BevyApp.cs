using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TinyEcs.Collections;

namespace TinyEcs.Bevy;

// ============================================================================
// Threading Mode
// ============================================================================

public enum ThreadingMode
{
	/// <summary>
	/// Automatically determine threading based on system count and dependencies
	/// </summary>
	Auto,

	/// <summary>
	/// Force single-threaded execution for all systems
	/// </summary>
	Single,

	/// <summary>
	/// Enable multi-threaded execution where possible
	/// </summary>
	Multi
}

// ============================================================================
// Core Interfaces
// ============================================================================

public interface IPlugin
{
	void Build(App app);
}

public interface ISystem
{
	void Run(TinyEcs.World world);
}

// ============================================================================
// Stage System
// ============================================================================

public class Stage
{
	public string Name { get; }
	internal List<SystemDescriptor> Systems { get; } = new();

	private Stage(string name)
	{
		Name = name;
	}

	// Default Bevy stages
	public static readonly Stage Startup = new("Startup");
	public static readonly Stage First = new("First");
	public static readonly Stage PreUpdate = new("PreUpdate");
	public static readonly Stage Update = new("Update");
	public static readonly Stage PostUpdate = new("PostUpdate");
	public static readonly Stage Last = new("Last");

	// Factory for custom stages
	public static Stage Custom(string name) => new(name);

	public override string ToString() => Name;
}

// Stage descriptor for ordering
public class StageDescriptor
{
	public Stage Stage { get; }
	public List<Stage> BeforeStages { get; } = new();
	public List<Stage> AfterStages { get; } = new();

	public StageDescriptor(Stage stage)
	{
		Stage = stage;
	}
}

// ============================================================================
// World Extensions (Partial class to extend TinyEcs.World)
// ============================================================================

public static class WorldExtensions
{
	// Per-world Bevy state now lives directly on the World instance via the
	// internal object? slot defined in TinyEcs/World.BevyState.cs. We keep
	// this thin accessor so every existing call site (AddResource, GetResource,
	// SetState, etc.) stays valid without churn — it just reads the field and
	// lazily constructs a WorldState on first touch.
	//
	// The slot is typed as object? in the core assembly to avoid pulling a
	// Bevy type name into TinyEcs.dll; we cast here on the Bevy side. The
	// lazy init has a benign race (see comment in World.BevyState.cs).
	internal static WorldState GetState(this TinyEcs.World world)
	{
		return (WorldState)(world.BevyStateSlot ??= new WorldState());
	}

	public static void AddResource<T>(this TinyEcs.World world, T resource) where T : notnull
	{
		var state = world.GetState();
		lock (state.SyncRoot)
		{
			state.Resources[typeof(T)] = new ResourceBox<T>(resource);
		}
	}

	public static T GetResource<T>(this TinyEcs.World world) where T : notnull
	{
		var state = world.GetState();
		lock (state.SyncRoot)
		{
			if (!state.Resources.TryGetValue(typeof(T), out var boxed))
				throw new InvalidOperationException($"Resource {typeof(T).Name} not found. Did you forget to call AddResource?");

			return ((ResourceBox<T>)boxed).Value;
		}
	}

	public static bool HasResource<T>(this TinyEcs.World world) where T : notnull
	{
		var state = world.GetState();
		lock (state.SyncRoot)
		{
			return state.Resources.ContainsKey(typeof(T));
		}
	}

	public static ref T GetResourceRef<T>(this TinyEcs.World world) where T : notnull
	{
		return ref world.GetResourceBox<T>().Value;
	}

	internal static ResourceBox<T> GetResourceBox<T>(this TinyEcs.World world) where T : notnull
	{
		var state = world.GetState();
		lock (state.SyncRoot)
		{
			if (!state.Resources.TryGetValue(typeof(T), out var boxed))
				throw new InvalidOperationException($"Resource {typeof(T).Name} not found. Did you forget to call AddResource?");

			return (ResourceBox<T>)boxed;
		}
	}

	public static void SendEvent<T>(this TinyEcs.World world, T evt) where T : notnull
	{
		world.GetEventChannel<T>().Enqueue(evt);
	}

	internal static EventChannel<T> GetEventChannel<T>(this TinyEcs.World world) where T : notnull
	{
		var state = world.GetState();
		lock (state.SyncRoot)
		{
			if (!state.EventChannels.TryGetValue(typeof(T), out var channelObj))
			{
				var channel = new EventChannel<T>();
				state.EventChannels[typeof(T)] = channel;
				return channel;
			}

			return (EventChannel<T>)channelObj;
		}
	}

	public static void RegisterObserver<T>(this TinyEcs.World world, Action<T> observer) where T : notnull
	{
		world.GetEventChannel<T>().RegisterObserver(observer);
	}

	internal static void ProcessEvents(this TinyEcs.World world)
	{
		var state = world.GetState();
		var currentTick = world.CurrentTick;
		using var channels = new PooledList<IEventChannel>(state.EventChannels.Count);
		lock (state.SyncRoot)
		{
			foreach (var channel in state.EventChannels.Values)
			{
				channels.Add(channel);
			}
		}
		foreach (var channel in channels.AsSpan)
		{
			channel.Flush(currentTick);
		}
	}

	// State management
	public static void SetState<TState>(this TinyEcs.World world, TState state) where TState : struct, Enum
	{
		var worldState = world.GetState();
		var type = typeof(TState);
		lock (worldState.SyncRoot)
		{
			if (worldState.States.TryGetValue(type, out var current))
			{
				worldState.PreviousStates[type] = current;
			}
			worldState.States[type] = state;
			worldState.PendingStateChanges.Remove(type);
			worldState.StatesProcessedThisFrame.Remove(type); // Mark for reprocessing
		}
	}

	public static TState GetState<TState>(this TinyEcs.World world) where TState : struct, Enum
	{
		var type = typeof(TState);
		var worldState = world.GetState();
		lock (worldState.SyncRoot)
		{
			if (worldState.States.TryGetValue(type, out var state))
			{
				return (TState)state;
			}
		}
		throw new InvalidOperationException($"State {typeof(TState).Name} not found. Did you call AddState<T>()?");
	}

	public static bool HasState<TState>(this TinyEcs.World world) where TState : struct, Enum
	{
		var worldState = world.GetState();
		lock (worldState.SyncRoot)
		{
			return worldState.States.ContainsKey(typeof(TState));
		}
	}

	public static void QueueState<TState>(this TinyEcs.World world, TState nextState) where TState : struct, Enum
	{
		var type = typeof(TState);
		var worldState = world.GetState();
		lock (worldState.SyncRoot)
		{
			if (worldState.States.TryGetValue(type, out var current) &&
				EqualityComparer<TState>.Default.Equals((TState)current, nextState))
			{
				worldState.PendingStateChanges.Remove(type);
				return;
			}

			worldState.PendingStateChanges[type] = new QueuedStateTransition<TState>(nextState);
			worldState.StatesProcessedThisFrame.Remove(type);
		}
	}

	internal static bool HasQueuedState<TState>(this TinyEcs.World world) where TState : struct, Enum
	{
		var worldState = world.GetState();
		lock (worldState.SyncRoot)
		{
			return worldState.PendingStateChanges.ContainsKey(typeof(TState));
		}
	}

	internal static void ClearQueuedState<TState>(this TinyEcs.World world) where TState : struct, Enum
	{
		var worldState = world.GetState();
		lock (worldState.SyncRoot)
		{
			worldState.PendingStateChanges.Remove(typeof(TState));
		}
	}

	internal static TState? GetPreviousState<TState>(this TinyEcs.World world) where TState : struct, Enum
	{
		var type = typeof(TState);
		var worldState = world.GetState();
		lock (worldState.SyncRoot)
		{
			if (worldState.PreviousStates.TryGetValue(type, out var state))
			{
				return (TState)state;
			}
		}
		return null;
	}

	internal static bool StateChanged<TState>(this TinyEcs.World world) where TState : struct, Enum
	{
		var state = world.GetState();
		var type = typeof(TState);
		lock (state.SyncRoot)
		{
			if (!state.States.TryGetValue(type, out var current))
				return false;

			if (!state.PreviousStates.TryGetValue(type, out var previous))
				return true;

			return !current.Equals(previous);
		}
	}

	// Query helpers - create cached queries
	/// <summary>
	/// Create a query with automatic tick tracking for Changed/Added filters
	/// </summary>
	public static BevyQueryIter<TQueryData, Empty> Query<TQueryData>(this TinyEcs.World world)
		where TQueryData : struct, IData<TQueryData>, IQueryIterator<TQueryData>, allows ref struct
	{
		// Use world's tick system for change detection
		// World.Update() is called at the start of Run()/RunStartup(), then systems execute with that tick
		// We check for changes in the current frame: [currentTick-1, currentTick)
		// This detects components modified with the current tick
		uint currentTick = world.CurrentTick;
		uint lastTick = currentTick > 0 ? currentTick - 1 : 0;

		var builder = world.QueryBuilder();
		TQueryData.Build(builder);
		var query = builder.Build();

		return new BevyQueryIter<TQueryData, Empty>(lastTick, currentTick, query.Iter());
	}

	/// <summary>
	/// Create a query with automatic tick tracking for Changed/Added filters
	/// </summary>
	public static BevyQueryIter<TQueryData, TQueryFilter> Query<TQueryData, TQueryFilter>(this TinyEcs.World world)
		where TQueryData : struct, IData<TQueryData>, IQueryIterator<TQueryData>, allows ref struct
		where TQueryFilter : struct, IFilter<TQueryFilter>, allows ref struct
	{
		// Use world's tick system for change detection
		// World.Update() is called at the start of Run()/RunStartup(), then systems execute with that tick
		// We check for changes in the current frame: [currentTick-1, currentTick)
		// This detects components modified with the current tick
		uint currentTick = world.CurrentTick;
		uint lastTick = currentTick > 0 ? currentTick - 1 : 0;

		var builder = world.QueryBuilder();
		TQueryData.Build(builder);
		TQueryFilter.Build(builder);
		var query = builder.Build();

		return new BevyQueryIter<TQueryData, TQueryFilter>(lastTick, currentTick, query.Iter());
	}
}

internal interface IStateChangeDetector
{
	Type StateType { get; }
	void Detect();
}

internal class WorldState
{
	public object SyncRoot { get; } = new object();
	public Dictionary<Type, object> Resources { get; } = new();
	public Dictionary<Type, IEventChannel> EventChannels { get; } = new();
	public Dictionary<Type, object> States { get; } = new();
	public Dictionary<Type, object> PreviousStates { get; } = new();
	public HashSet<Type> StatesProcessedThisFrame { get; } = new();
	public List<IStateChangeDetector> StateChangeDetectors { get; } = new();
	public Dictionary<Type, IQueuedStateTransition> PendingStateChanges { get; } = new();
}

internal sealed class ResourceBox<T> where T : notnull
{
	public T Value;

	public ResourceBox(T value)
	{
		Value = value;
	}
}

public sealed class State<TState> where TState : struct, Enum
{
	private readonly TinyEcs.World _world;

	internal State(TinyEcs.World world)
	{
		_world = world;
	}

	public TState Current => _world.GetState<TState>();
	public TState Previous => _world.GetPreviousState<TState>() ?? _world.GetState<TState>();
	public bool IsChanged => _world.StateChanged<TState>();
	public bool IsQueued => _world.HasQueuedState<TState>();
}

public sealed class NextState<TState> where TState : struct, Enum
{
	private readonly TinyEcs.World _world;

	internal NextState(TinyEcs.World world)
	{
		_world = world;
	}

	public void Set(TState nextState) => _world.QueueState(nextState);
	public bool IsQueued => _world.HasQueuedState<TState>();
	public void Clear() => _world.ClearQueuedState<TState>();
}

internal interface IQueuedStateTransition
{
	void Apply(TinyEcs.World world);
	Type StateType { get; }
}

internal readonly struct QueuedStateTransition<TState> : IQueuedStateTransition where TState : struct, Enum
{
	private readonly TState _next;

	public QueuedStateTransition(TState next)
	{
		_next = next;
	}

	public void Apply(TinyEcs.World world) => world.SetState(_next);

	public Type StateType => typeof(TState);
}

internal interface IEventChannel
{
	void Flush(uint currentTick);
}

internal sealed class EventChannel<T> : IEventChannel
{
	private readonly object _lock = new();
	private List<T> _readBuffer = new();
	private List<T> _writeBuffer = new();
	private readonly List<Action<T>> _observers = new();
	private ulong _epoch;
	private uint _activeTick = uint.MaxValue;
	private int _observerCursor;

	internal void Enqueue(T evt)
	{
		lock (_lock)
		{
			_writeBuffer.Add(evt);
		}
	}

	internal void RegisterObserver(Action<T> observer)
	{
		lock (_lock)
		{
			_observers.Add(observer);
		}
	}

	internal void CopyEvents(ref ulong epoch, ref int lastReadIndex, List<T> target)
	{
		lock (_lock)
		{
			if (epoch != _epoch)
			{
				epoch = _epoch;
				lastReadIndex = 0;
			}

			if (lastReadIndex > _readBuffer.Count)
			{
				lastReadIndex = _readBuffer.Count;
			}

			for (var i = lastReadIndex; i < _readBuffer.Count; i++)
			{
				target.Add(_readBuffer[i]);
			}

			lastReadIndex = _readBuffer.Count;
		}
	}

	public void Flush(uint currentTick)
	{
		using var toNotify = new PooledList<T>(8);
		using var observersSnapshot = new PooledList<Action<T>>(4);
		bool haveWork = false;

		lock (_lock)
		{
			var newFrame = _activeTick != currentTick;

			if (newFrame)
			{
				_activeTick = currentTick;

				// Drop last frame's events; observers already saw them via _observerCursor.
				if (_readBuffer.Count > 0)
				{
					_readBuffer.Clear();
					_epoch++;
				}

				_observerCursor = 0;

				// Promote the write buffer so readers can see this frame's events next frame.
				if (_writeBuffer.Count > 0)
				{
					(_readBuffer, _writeBuffer) = (_writeBuffer, _readBuffer);
					_writeBuffer.Clear();
					_epoch++;
				}
			}
			else if (_writeBuffer.Count > 0)
			{
				// Mid-frame append: drain pending writes into the readable buffer.
				// No _epoch bump — readers stay attached to the same epoch and resume
				// from their last index, picking up the newly appended items.
				_readBuffer.AddRange(_writeBuffer);
				_writeBuffer.Clear();
			}

			// Snapshot un-notified events for observers (each event delivered exactly once).
			if (_observers.Count > 0 && _observerCursor < _readBuffer.Count)
			{
				for (var i = _observerCursor; i < _readBuffer.Count; i++)
				{
					toNotify.Add(_readBuffer[i]);
				}
				foreach (var obs in _observers)
				{
					observersSnapshot.Add(obs);
				}
				haveWork = true;
			}

			_observerCursor = _readBuffer.Count;
		}

		if (!haveWork)
		{
			return;
		}

		foreach (var evt in toNotify.AsSpan)
		{
			foreach (var observer in observersSnapshot.AsSpan)
			{
				observer(evt);
			}
		}
	}
}

// ============================================================================
// System Descriptor
// ============================================================================

public class SystemDescriptor
{
	public ISystem System { get; }
	public List<Func<TinyEcs.World, bool>> RunConditions { get; } = new();
	public List<SystemDescriptor> BeforeSystems { get; } = new();
	public List<SystemDescriptor> AfterSystems { get; } = new();
	public string? Label { get; set; }
	public Stage? Stage { get; set; }

	/// <summary>
	/// Override threading mode for this specific system. Null means use App default.
	/// </summary>
	public ThreadingMode? ThreadingMode { get; set; }

	public SystemDescriptor(ISystem system)
	{
		System = system;
	}

	public bool ShouldRun(TinyEcs.World world)
	{
		return RunConditions.All(condition => condition(world));
	}
}

// ============================================================================
// System Implementation Helpers
// ============================================================================

public class FunctionalSystem : ISystem
{
	private readonly Action<TinyEcs.World> _systemFn;

	public FunctionalSystem(Action<TinyEcs.World> systemFn)
	{
		_systemFn = systemFn;
	}

	public void Run(TinyEcs.World world) => _systemFn(world);
}

// ============================================================================
// Fluent Configuration Interfaces
// ============================================================================

// Step 1: Must choose stage or state transition before configuring further.
public interface ISystemStageSelector
{
	ISystemConfigurator InStage(Stage stage);
	ISystemConfigurator OnEnter<TState>(TState state) where TState : struct, Enum;
	ISystemConfigurator OnExit<TState>(TState state) where TState : struct, Enum;
}

// Step 2: Configure the system. All configuration methods return the same
// interface, so they can be called in any order and any number of times.
//
// Semantics:
//   - Label("a").Label("b"): "last label wins" - only "b" resolves; "a" is removed.
//   - After/Before/Chain are additive - multiple calls add multiple dependencies.
//   - RunIf calls are additive - all conditions must pass for the system to run.
public interface ISystemConfigurator
{
	ISystemConfigurator RunIf(Func<TinyEcs.World, bool> condition);
	ISystemConfigurator RunIfResourceExists<T>() where T : notnull;
	ISystemConfigurator RunIfResourceEquals<T>(T value) where T : notnull, IEquatable<T>;
	ISystemConfigurator RunIfState<TState>(TState state) where TState : struct, Enum;
	ISystemConfigurator SingleThreaded();
	ISystemConfigurator WithThreadingMode(ThreadingMode mode);
	ISystemConfigurator After(string label);
	ISystemConfigurator Before(string label);
	ISystemConfigurator Label(string label);
	ISystemConfigurator Chain();
	App Build();
}

// ============================================================================
// App Builder (uses TinyEcs.World under the hood)
// ============================================================================

public class App
{
	internal readonly TinyEcs.World _world;
	private readonly ThreadingMode _threadingMode;
	internal readonly Dictionary<Stage, List<SystemDescriptor>> _stageSystems = new();
	private readonly List<StageDescriptor> _stageDescriptors = new();
	private readonly Dictionary<string, SystemDescriptor> _labeledSystems = new();
	private SystemDescriptor? _previousSystem = null;
	private readonly HashSet<Type> _installedPlugins = new();

	// State transition systems - use object as key to store boxed enum values (no toString() allocation)
	internal readonly Dictionary<Type, Dictionary<object, List<SystemDescriptor>>> _onEnterSystems = new();
	internal readonly Dictionary<Type, Dictionary<object, List<SystemDescriptor>>> _onExitSystems = new();
	private readonly HashSet<Type> _registeredStateTypes = new();

	// Startup tracking
	private bool _startupHasRun = false;

	// Cached sorted results - computed once after app is built
	private List<StageDescriptor>? _sortedStages = null;
	private readonly Dictionary<Stage, List<SystemDescriptor>> _sortedStageSystems = new();
	private readonly Dictionary<Stage, List<List<SystemDescriptor>>> _cachedBatches = new();
	private bool _executionOrderDirty = false;

	public App(TinyEcs.World world, ThreadingMode threadingMode = ThreadingMode.Auto)
	{
		_world = world;
		_threadingMode = threadingMode;

		// Initialize Startup stage (runs once)
		AddStage(Stage.Startup);

		// Initialize default stages in order
		AddStage(Stage.First);
		AddStage(Stage.PreUpdate).After(Stage.First);
		AddStage(Stage.Update).After(Stage.PreUpdate);
		AddStage(Stage.PostUpdate).After(Stage.Update);
		AddStage(Stage.Last).After(Stage.PostUpdate);
	}

	/// <summary>
	/// Create a new App with a new World and optional threading mode
	/// </summary>
	public App(ThreadingMode threadingMode = ThreadingMode.Auto) : this(new World(), threadingMode)
	{
	}

	// Call this after all systems and stages are added (before first Run())
	private void BuildExecutionOrder()
	{
		if (_sortedStages != null && !_executionOrderDirty)
			return; // Already built and not dirty

		// Clear caches if rebuilding
		if (_executionOrderDirty)
		{
			_sortedStageSystems.Clear();
			_cachedBatches.Clear();
			_executionOrderDirty = false;
		}

		// Sort stages once
		_sortedStages = TopologicalSortStages();

		// Sort systems for each stage once and build parallel batches
		foreach (var (stage, systems) in _stageSystems)
		{
			if (systems.Count > 0)
			{
				var sortedSystems = TopologicalSortSystems(systems);
				_sortedStageSystems[stage] = sortedSystems;
				_cachedBatches[stage] = BuildParallelBatches(sortedSystems);
			}
		}
	}

	public App AddResource<T>(T resource) where T : notnull
	{
		_world.AddResource(resource);
		return this;
	}

	public App AddState<TState>(TState initialState) where TState : struct, Enum
	{
		_world.SetState(initialState);

		if (!_world.HasResource<State<TState>>())
		{
			_world.AddResource(new State<TState>(_world));
		}

		if (!_world.HasResource<NextState<TState>>())
		{
			_world.AddResource(new NextState<TState>(_world));
		}

		return this;
	}

	internal StageDescriptor GetOrCreateStageDescriptor(Stage stage)
	{
		if (!_stageSystems.ContainsKey(stage))
		{
			_stageSystems[stage] = new List<SystemDescriptor>();
			var descriptor = new StageDescriptor(stage);
			_stageDescriptors.Add(descriptor);
			return descriptor;
		}

		return _stageDescriptors.First(d => d.Stage == stage);
	}

	public StageConfigurator AddStage(Stage stage)
	{
		var descriptor = GetOrCreateStageDescriptor(stage);
		return new StageConfigurator(this, descriptor);
	}

	public App AddPlugin(IPlugin plugin)
	{
		var pluginType = plugin.GetType();

		if (_installedPlugins.Contains(pluginType))
		{
			Console.WriteLine($"Warning: Plugin {pluginType.Name} already installed. Skipping.");
			return this;
		}

		_installedPlugins.Add(pluginType);
		plugin.Build(this);
		return this;
	}

	public App AddPlugin<T>() where T : IPlugin, new()
	{
		return AddPlugin(new T());
	}

	/// <summary>
	/// Add a system with fluent configuration (must call .InStage() or state transition methods)
	/// </summary>
	public ISystemStageSelector AddSystem(ISystem system)
	{
		var descriptor = new SystemDescriptor(system);
		var previous = _previousSystem;
		_previousSystem = descriptor;
		return new SystemConfigurator(this, descriptor, previous);
	}

	/// <summary>
	/// Add a system directly to a stage (simpler API)
	/// </summary>
	public App AddSystem(Stage stage, ISystem system)
	{
		var descriptor = new SystemDescriptor(system);
		AddSystemToStage(stage, descriptor);
		_previousSystem = descriptor;
		return this;
	}

	public App AddObserver<T>(Action<T> observer) where T : notnull
	{
		_world.RegisterObserver(observer);
		return this;
	}

	internal void AddSystemToStage(Stage stage, SystemDescriptor descriptor)
	{
		if (!_stageSystems.ContainsKey(stage))
		{
			AddStage(stage);
		}
		descriptor.Stage = stage; // Set the stage on the descriptor
		_stageSystems[stage].Add(descriptor);

		// Mark execution order as dirty if systems are added after initial build
		if (_sortedStages != null)
		{
			_executionOrderDirty = true;
		}
	}

	internal void RegisterLabel(string label, SystemDescriptor descriptor)
	{
		// "Last label wins": if this descriptor previously claimed another label,
		// remove that mapping so the old label no longer resolves to this system.
		if (!string.IsNullOrEmpty(descriptor.Label) && descriptor.Label != label)
		{
			if (_labeledSystems.TryGetValue(descriptor.Label!, out var existing) && existing == descriptor)
			{
				_labeledSystems.Remove(descriptor.Label!);
			}
		}

		_labeledSystems[label] = descriptor;
		descriptor.Label = label;
	}

	internal SystemDescriptor? GetSystemByLabel(string label)
	{
		return _labeledSystems.GetValueOrDefault(label);
	}

	internal void RegisterOnEnterSystem<TState>(TState state, SystemDescriptor descriptor) where TState : struct, Enum
	{
		var type = typeof(TState);
		// Use boxed enum directly as key - no ToString() allocation
		object stateKey = state;

		if (!_onEnterSystems.ContainsKey(type))
			_onEnterSystems[type] = new Dictionary<object, List<SystemDescriptor>>();

		if (!_onEnterSystems[type].ContainsKey(stateKey))
			_onEnterSystems[type][stateKey] = new List<SystemDescriptor>();

		_onEnterSystems[type][stateKey].Add(descriptor);

		RegisterStateChangeDetector<TState>();
	}

	internal void RegisterOnExitSystem<TState>(TState state, SystemDescriptor descriptor) where TState : struct, Enum
	{
		var type = typeof(TState);
		// Use boxed enum directly as key - no ToString() allocation
		object stateKey = state;

		if (!_onExitSystems.ContainsKey(type))
			_onExitSystems[type] = new Dictionary<object, List<SystemDescriptor>>();

		if (!_onExitSystems[type].ContainsKey(stateKey))
			_onExitSystems[type][stateKey] = new List<SystemDescriptor>();

		_onExitSystems[type][stateKey].Add(descriptor);

		RegisterStateChangeDetector<TState>();
	}

	private void RegisterStateChangeDetector<TState>() where TState : struct, Enum
	{
		var type = typeof(TState);

		if (_registeredStateTypes.Contains(type))
			return;

		_registeredStateTypes.Add(type);

		var worldState = _world.GetState();
		var detector = new StateChangeDetector<TState>(this);

		lock (worldState.SyncRoot)
		{
			worldState.StateChangeDetectors.Add(detector);
		}
	}

	// Typed state change detector avoids the closure allocation that would otherwise
	// capture the App instance + per-state-type fields. Using Nullable<TState> locals
	// keeps the previous/current state values as value types instead of boxing them.
	private sealed class StateChangeDetector<TState> : IStateChangeDetector
		where TState : struct, Enum
	{
		private readonly App _app;

		public StateChangeDetector(App app)
		{
			_app = app;
		}

		public Type StateType => typeof(TState);

		public void Detect()
		{
			var world = _app._world;
			var worldState = world.GetState();
			var type = typeof(TState);

			TState? previousState;
			TState? currentState;

			lock (worldState.SyncRoot)
			{
				if (worldState.StatesProcessedThisFrame.Contains(type))
					return;

				if (!world.StateChanged<TState>())
					return;

				worldState.StatesProcessedThisFrame.Add(type);
				previousState = world.GetPreviousState<TState>();
				currentState = world.HasState<TState>() ? world.GetState<TState>() : (TState?)null;
			}

			if (previousState.HasValue && _app._onExitSystems.TryGetValue(type, out var exitDict))
			{
				object prevStateKey = previousState.Value;
				if (exitDict.TryGetValue(prevStateKey, out var exitSystems))
				{
					foreach (var descriptor in exitSystems)
					{
						if (descriptor.ShouldRun(world))
							descriptor.System.Run(world);
					}
				}
			}

			if (currentState.HasValue && _app._onEnterSystems.TryGetValue(type, out var enterDict))
			{
				object currStateKey = currentState.Value;
				if (enterDict.TryGetValue(currStateKey, out var enterSystems))
				{
					foreach (var descriptor in enterSystems)
					{
						if (descriptor.ShouldRun(world))
							descriptor.System.Run(world);
					}
				}
			}
		}
	}

	public void RunStartup()
	{
		// Build execution order once before first run
		BuildExecutionOrder();

		if (_startupHasRun)
			return;

		_startupHasRun = true;

		RunFrame(startup: true);
	}

	public void Run()
	{
		RunStartup();

		RunFrame(startup: false);
	}

	/// <summary>
	/// Shared frame execution helper used by both <see cref="RunStartup"/> and <see cref="Run"/>.
	/// Increments the world tick, executes the appropriate stages (with observer flushes between
	/// each), then processes pending state transitions and events.
	/// </summary>
	/// <param name="startup">
	/// When <c>true</c>, only <see cref="Stage.Startup"/> is executed. When <c>false</c>, every
	/// non-startup stage in <see cref="_sortedStages"/> is executed in order.
	/// </param>
	private void RunFrame(bool startup)
	{
		// Increment world tick for change detection
		// This marks all modifications in this frame with the new tick
		_world.Update();

		if (startup)
		{
			ExecuteSystemsParallel(Stage.Startup);

			// Auto-flush observers after startup stage
			_world.FlushObservers();
		}
		else
		{
			// Use cached sorted stages (already built in RunStartup)
			foreach (var stageDesc in _sortedStages!)
			{
				if (stageDesc.Stage == Stage.Startup)
					continue;

				ExecuteSystemsParallel(stageDesc.Stage);

				// Auto-flush observers after each stage (like Bevy's apply_deferred)
				_world.FlushObservers();
			}
		}

		ProcessStateTransitions();
		_world.ProcessEvents();
	}

	/// <summary>
	/// Execute systems in parallel where possible, respecting dependency constraints
	/// </summary>
	private void ExecuteSystemsParallel(Stage stage)
	{
		// Stage.Startup always runs in single-threaded mode by default
		// This ensures deterministic initialization and proper resource setup
		bool forceStartupSingleThreaded = stage == Stage.Startup;

		// Determine if we should use parallel execution
		bool useParallel = forceStartupSingleThreaded ? false : _threadingMode switch
		{
			ThreadingMode.Single => false,
			ThreadingMode.Multi => true,
			ThreadingMode.Auto => Environment.ProcessorCount > 1,
			_ => false
		};

		// In single-threaded mode, skip batching and just run systems in topological order
		// This preserves declaration order and respects explicit dependencies
		if (!useParallel)
		{
			if (!_sortedStageSystems.TryGetValue(stage, out var systems))
				return;

			foreach (var descriptor in systems)
			{
				if (descriptor.ShouldRun(_world))
				{
					descriptor.System.Run(_world);
				}
			}
			return;
		}

		// Parallel mode: use cached batches (already computed during BuildExecutionOrder)
		if (!_cachedBatches.TryGetValue(stage, out var batches))
			return;

		// Execute each batch (systems within a batch run in parallel)
		foreach (var batch in batches)
		{
			if (batch.Count == 1)
			{
				// Single system - run directly
				var descriptor = batch[0];
				if (descriptor.ShouldRun(_world))
				{
					descriptor.System.Run(_world);
				}
			}
			else
			{
				// Multiple systems - run in parallel
				Parallel.ForEach(batch, descriptor =>
				{
					if (descriptor.ShouldRun(_world))
					{
						descriptor.System.Run(_world);
					}
				});
			}
		}
	}

	/// <summary>
	/// Build batches of systems that can run in parallel
	/// Systems in the same batch have no resource conflicts.
	/// Systems with SingleThreaded mode are placed in their own exclusive batch.
	/// </summary>
	private List<List<SystemDescriptor>> BuildParallelBatches(List<SystemDescriptor> systems)
	{
		var batches = new List<List<SystemDescriptor>>();
		var remaining = new List<SystemDescriptor>(systems);

		while (remaining.Count > 0)
		{
			var batch = new List<SystemDescriptor>();
			var batchAccess = new SystemParamAccess();
			bool batchCanBeParallel = true;

			// Process systems in forward order to preserve topological sort order
			for (int i = 0; i < remaining.Count;)
			{
				var descriptor = remaining[i];

				// Check if this system has a threading override
				bool systemRequiresSingleThread = descriptor.ThreadingMode == ThreadingMode.Single;

				// If the current batch already has systems and this one requires single-threading,
				// or if the batch requires single-threading and already has a system, skip it for this batch
				if (systemRequiresSingleThread)
				{
					if (batch.Count > 0)
					{
						// Can't add to an existing batch - will be processed in next iteration
						i++; // Move to next system
						continue;
					}
					else
					{
						// Start a new single-threaded batch with just this system
						batch.Add(descriptor);
						remaining.RemoveAt(i);
						batchCanBeParallel = false;
						break; // This batch is done - only one system allowed
					}
				}

				// If batch is already marked single-threaded, skip all other systems
				if (!batchCanBeParallel)
				{
					i++; // Move to next system
					continue;
				}

				// Get access pattern if it's a parameterized system
				var access = (descriptor.System as ParameterizedSystem)?.GetAccess();
				if (access == null)
				{
					// Non-parameterized systems have unknown access - run them sequentially
					access = new SystemParamAccess();
					// Treat as exclusive by adding a unique type marker
					access.WriteResources.Add(descriptor.System.GetType());
				}

				// Check if this system has explicit ordering dependencies with systems already in the batch
				bool hasOrderingConflict = false;
				foreach (var batchedSystem in batch)
				{
					// If this system must run before/after a system already in the batch, they can't be parallel
					if (descriptor.BeforeSystems.Contains(batchedSystem) ||
						descriptor.AfterSystems.Contains(batchedSystem) ||
						batchedSystem.BeforeSystems.Contains(descriptor) ||
						batchedSystem.AfterSystems.Contains(descriptor))
					{
						hasOrderingConflict = true;
						break;
					}
				}

				// Check if this system conflicts with the current batch
				if (!hasOrderingConflict && !batchAccess.ConflictsWith(access))
				{
					// No conflict - add to batch
					batch.Add(descriptor);
					remaining.RemoveAt(i);
					// Don't increment i - next item shifted into current position

					// Merge access patterns
					foreach (var read in access.ReadResources)
						batchAccess.ReadResources.Add(read);
					foreach (var write in access.WriteResources)
						batchAccess.WriteResources.Add(write);
				}
				else
				{
					// Conflict - skip this system for now
					i++;
				}
			}

			batches.Add(batch);
		}

		return batches;
	}

	private void ProcessStateTransitions()
	{
		var worldState = _world.GetState();
		using var transitions = new PooledList<IQueuedStateTransition>(worldState.PendingStateChanges.Count);
		using var detectors = new PooledList<IStateChangeDetector>(worldState.StateChangeDetectors.Count);
		lock (worldState.SyncRoot)
		{
			foreach (var t in worldState.PendingStateChanges.Values)
			{
				transitions.Add(t);
			}
			worldState.PendingStateChanges.Clear();
			foreach (var d in worldState.StateChangeDetectors)
			{
				detectors.Add(d);
			}
			worldState.StatesProcessedThisFrame.Clear();
		}
		foreach (var transition in transitions.AsSpan)
		{
			transition.Apply(_world);
		}
		foreach (var detector in detectors.AsSpan)
		{
			detector.Detect();
		}

		// After processing all state transitions and running OnEnter/OnExit systems,
		// update PreviousStates to match current States so StateChanged returns false next frame
		lock (worldState.SyncRoot)
		{
			foreach (var kvp in worldState.States)
			{
				worldState.PreviousStates[kvp.Key] = kvp.Value;
			}
		}
	}

	public void Update() => Run();

	public TinyEcs.World GetWorld() => _world;

	private List<StageDescriptor> TopologicalSortStages()
	{
		var count = _stageDescriptors.Count;
		var result = new List<StageDescriptor>(count);
		var visited = new HashSet<StageDescriptor>(count);
		var visiting = new HashSet<StageDescriptor>(count);

		// Build a lookup dictionary to avoid FirstOrDefault() allocations
		var stageToDescriptor = new Dictionary<Stage, StageDescriptor>(count);
		foreach (var desc in _stageDescriptors)
		{
			stageToDescriptor[desc.Stage] = desc;
		}

		void Visit(StageDescriptor node)
		{
			if (visited.Contains(node)) return;
			if (visiting.Contains(node))
				throw new InvalidOperationException("Circular dependency detected in stage ordering");

			visiting.Add(node);

			foreach (var before in node.BeforeStages)
			{
				if (stageToDescriptor.TryGetValue(before, out var beforeDescriptor))
					Visit(beforeDescriptor);
			}

			visiting.Remove(node);
			visited.Add(node);
			result.Add(node);
		}

		foreach (var stage in _stageDescriptors)
			Visit(stage);

		return result;
	}

	private List<SystemDescriptor> TopologicalSortSystems(List<SystemDescriptor> systems)
	{
		var result = new List<SystemDescriptor>(systems.Count);
		var visited = new HashSet<SystemDescriptor>(systems.Count);
		var visiting = new HashSet<SystemDescriptor>(systems.Count);

		void Visit(SystemDescriptor node)
		{
			if (visited.Contains(node)) return;
			if (visiting.Contains(node))
				throw new InvalidOperationException("Circular dependency detected in system ordering");

			visiting.Add(node);

			// Visit dependencies in their original declaration order
			// Sort BeforeSystems by their position in the original systems list to preserve declaration order
			var orderedBefore = node.BeforeSystems
				.OrderBy(s => systems.IndexOf(s))
				.ToList();

			foreach (var before in orderedBefore)
				Visit(before);

			visiting.Remove(node);
			visited.Add(node);
			result.Add(node);
		}

		// Validate all systems have a stage before sorting
		foreach (var system in systems)
		{
			if (system.Stage == null)
			{
				throw new InvalidOperationException(
					"All systems must have a stage assigned. Use .InStage(Stage.X) when adding systems.");
			}
		}

		// Visit systems in their original declaration order
		// This ensures that when there are no explicit dependencies,
		// systems run in the order they were added
		foreach (var system in systems)
			Visit(system);

		return result;
	}
}

// ============================================================================
// Configurators Implementation
// ============================================================================

public class StageConfigurator
{
	private readonly App _app;
	private readonly StageDescriptor _descriptor;

	internal StageConfigurator(App app, StageDescriptor descriptor)
	{
		_app = app;
		_descriptor = descriptor;
	}

	public StageConfigurator Before(Stage stage)
	{
		var currentStage = _descriptor.Stage;
		if (currentStage == stage)
			return this;

		var targetDescriptor = _app.GetOrCreateStageDescriptor(stage);

		if (!targetDescriptor.BeforeStages.Contains(currentStage))
			targetDescriptor.BeforeStages.Add(currentStage);

		if (!_descriptor.AfterStages.Contains(stage))
			_descriptor.AfterStages.Add(stage);

		return this;
	}

	public StageConfigurator After(Stage stage)
	{
		var currentStage = _descriptor.Stage;
		if (currentStage == stage)
			return this;

		var targetDescriptor = _app.GetOrCreateStageDescriptor(stage);

		if (!_descriptor.BeforeStages.Contains(stage))
			_descriptor.BeforeStages.Add(stage);

		if (!targetDescriptor.AfterStages.Contains(currentStage))
			targetDescriptor.AfterStages.Add(currentStage);

		return this;
	}

	public App Build() => _app;
}

public class SystemConfigurator : ISystemStageSelector, ISystemConfigurator
{
	private readonly App _app;
	private readonly SystemDescriptor _descriptor;
	private readonly SystemDescriptor? _previousSystem;
	private bool _stageAssigned = false;

	internal SystemConfigurator(App app, SystemDescriptor descriptor, SystemDescriptor? previousSystem)
	{
		_app = app;
		_descriptor = descriptor;
		_previousSystem = previousSystem;
	}

	// ISystemStageSelector
	public ISystemConfigurator InStage(Stage stage)
	{
		if (_stageAssigned && _descriptor.Stage != null)
		{
			_app._stageSystems[_descriptor.Stage].Remove(_descriptor);
		}

		_descriptor.Stage = stage;
		_app.AddSystemToStage(stage, _descriptor);
		_stageAssigned = true;
		return this;
	}

	public ISystemConfigurator OnEnter<TState>(TState state) where TState : struct, Enum
	{
		_stageAssigned = true;
		_app.RegisterOnEnterSystem(state, _descriptor);
		return this;
	}

	public ISystemConfigurator OnExit<TState>(TState state) where TState : struct, Enum
	{
		_stageAssigned = true;
		_app.RegisterOnExitSystem(state, _descriptor);
		return this;
	}

	// ISystemConfigurator
	public ISystemConfigurator RunIf(Func<TinyEcs.World, bool> condition)
	{
		_descriptor.RunConditions.Add(condition);
		return this;
	}

	public ISystemConfigurator RunIfResourceExists<T>() where T : notnull
	{
		return RunIf(world => world.HasResource<T>());
	}

	public ISystemConfigurator RunIfResourceEquals<T>(T value) where T : notnull, IEquatable<T>
	{
		return RunIf(world =>
			world.HasResource<T>() && world.GetResource<T>().Equals(value));
	}

	public ISystemConfigurator RunIfState<TState>(TState state) where TState : struct, Enum
	{
		return RunIf(world =>
		{
			if (!world.HasState<TState>()) return false;
			return world.GetState<TState>().Equals(state);
		});
	}

	public ISystemConfigurator SingleThreaded()
	{
		_descriptor.ThreadingMode = ThreadingMode.Single;
		return this;
	}

	public ISystemConfigurator WithThreadingMode(ThreadingMode mode)
	{
		_descriptor.ThreadingMode = mode;
		return this;
	}

	public ISystemConfigurator After(string label)
	{
		var target = _app.GetSystemByLabel(label);
		if (target == null)
		{
			throw new InvalidOperationException(
				$"Cannot add dependency .After(\"{label}\"): No system with label \"{label}\" has been registered. " +
				$"Make sure the system you're referencing is added before this one, or use explicit .Before()/.After() with system instances.");
		}

		if (target != _descriptor)
		{
			if (!_descriptor.BeforeSystems.Contains(target))
				_descriptor.BeforeSystems.Add(target);

			if (!target.AfterSystems.Contains(_descriptor))
				target.AfterSystems.Add(_descriptor);
		}
		return this;
	}

	public ISystemConfigurator Before(string label)
	{
		var target = _app.GetSystemByLabel(label);
		if (target == null)
		{
			throw new InvalidOperationException(
				$"Cannot add dependency .Before(\"{label}\"): No system with label \"{label}\" has been registered. " +
				$"Make sure the system you're referencing is added before this one, or use explicit .Before()/.After() with system instances.");
		}

		if (target != _descriptor)
		{
			if (!target.BeforeSystems.Contains(_descriptor))
				target.BeforeSystems.Add(_descriptor);

			if (!_descriptor.AfterSystems.Contains(target))
				_descriptor.AfterSystems.Add(target);
		}
		return this;
	}

	public ISystemConfigurator Label(string label)
	{
		_app.RegisterLabel(label, _descriptor);
		return this;
	}

	public ISystemConfigurator Chain()
	{
		if (_previousSystem != null && _previousSystem != _descriptor)
		{
			if (!_descriptor.BeforeSystems.Contains(_previousSystem))
				_descriptor.BeforeSystems.Add(_previousSystem);

			if (!_previousSystem.AfterSystems.Contains(_descriptor))
				_previousSystem.AfterSystems.Add(_descriptor);
		}
		return this;
	}

	public App Build()
	{
		if (!_stageAssigned)
		{
			throw new InvalidOperationException(
				"System must be assigned to a stage using .InStage(Stage.X) or .OnEnter()/.OnExit()");
		}
		return _app;
	}
}

// ============================================================================
// Extension Methods
// ============================================================================

public static class AppExtensions
{
	// Fluent API - must specify stage with .InStage()
	public static ISystemStageSelector AddSystem(this App app, Action<TinyEcs.World> systemFn)
	{
		return app.AddSystem(new FunctionalSystem(systemFn));
	}

	public static ISystemStageSelector AddSystem(this App app, Action systemFn)
	{
		return app.AddSystem(new FunctionalSystem(_ => systemFn()));
	}

	// Direct stage API - stage parameter first
	public static App AddSystem(this App app, Stage stage, Action<TinyEcs.World> systemFn)
	{
		return app.AddSystem(stage, new FunctionalSystem(systemFn));
	}

	public static App AddSystem(this App app, Stage stage, Action systemFn)
	{
		return app.AddSystem(stage, new FunctionalSystem(_ => systemFn()));
	}
}


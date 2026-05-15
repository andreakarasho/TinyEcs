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

/// <summary>
/// Schedule stage marker.
/// </summary>
/// <remarks>
/// Stages use reference equality. Use the static instances (<see cref="Startup"/>, <see cref="Update"/>, etc.)
/// or cache a <see cref="Custom(string)"/> result in a static field — calling <c>Stage.Custom("foo")</c>
/// twice creates two distinct stages.
/// </remarks>
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
// World Extensions (Query helpers only — Bevy state lives on App)
// ============================================================================

public static class WorldExtensions
{
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

internal class AppState
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
	// State/NextState always route through the owning App; the World-side path
	// has been removed now that the Bevy layer owns its own per-app state.
	private readonly App _app;

	internal State(App app)
	{
		_app = app;
	}

	public TState Current => _app.GetState<TState>();
	public TState Previous => _app.GetPreviousState<TState>() ?? _app.GetState<TState>();
	public bool IsChanged => _app.StateChanged<TState>();
	public bool IsQueued => _app.HasQueuedState<TState>();
}

public sealed class NextState<TState> where TState : struct, Enum
{
	private readonly App _app;

	internal NextState(App app)
	{
		_app = app;
	}

	public void Set(TState nextState) => _app.QueueState(nextState);

	public bool IsQueued => _app.HasQueuedState<TState>();
	public void Clear() => _app.ClearQueuedState<TState>();
}

internal interface IQueuedStateTransition
{
	void Apply(App app);
	Type StateType { get; }
}

internal readonly struct QueuedStateTransition<TState> : IQueuedStateTransition where TState : struct, Enum
{
	private readonly TState _next;

	public QueuedStateTransition(TState next)
	{
		_next = next;
	}

	public void Apply(App app) => app.SetState(_next);

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
	public HashSet<SystemDescriptor> BeforeSystems { get; } = new();
	public HashSet<SystemDescriptor> AfterSystems { get; } = new();
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
	/// <summary>
	/// Per-stage runtime state: raw system list, topologically sorted view,
	/// and the cached parallel-batch plan. Consolidates what used to be three
	/// parallel dictionaries (raw systems, sorted, cached batches) all keyed
	/// on <see cref="Stage"/>.
	/// </summary>
	internal sealed class StageRuntime
	{
		public readonly List<SystemDescriptor> Systems = new();
		public List<SystemDescriptor>? Sorted;
		public List<List<SystemDescriptor>>? Batches;
	}

	internal readonly TinyEcs.World _world;
	private readonly AppState _appState = new();
	private readonly ThreadingMode _threadingMode;
	private readonly bool _multipleProcessors;
	internal readonly Dictionary<Stage, StageRuntime> _stageRuntimes = new();
	private readonly List<StageDescriptor> _stageDescriptors = new();
	private readonly Dictionary<Stage, StageDescriptor> _stageDescriptorByStage = new();
	private readonly Dictionary<string, SystemDescriptor> _labeledSystems = new();
	private SystemDescriptor? _previousSystem = null;
	private readonly HashSet<Type> _installedPlugins = new();

	// State transition systems - use object as key to store boxed enum values (no toString() allocation)
	internal readonly Dictionary<(Type StateType, object StateValue), List<SystemDescriptor>> _onEnterSystems = new();
	internal readonly Dictionary<(Type StateType, object StateValue), List<SystemDescriptor>> _onExitSystems = new();
	private readonly HashSet<Type> _registeredStateTypes = new();

	// Startup tracking
	private bool _startupHasRun = false;

	// Cached sorted results - computed once after app is built
	private List<StageDescriptor>? _sortedStages = null;
	private bool _executionOrderDirty = false;

	public App(TinyEcs.World world, ThreadingMode threadingMode = ThreadingMode.Auto)
	{
		_world = world;
		_threadingMode = threadingMode;
		_multipleProcessors = Environment.ProcessorCount > 1;

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
			foreach (var runtime in _stageRuntimes.Values)
			{
				runtime.Sorted = null;
				runtime.Batches = null;
			}
			_executionOrderDirty = false;
		}

		// Sort stages once
		_sortedStages = TopologicalSortStages();

		// Sort systems for each stage once and build parallel batches
		foreach (var (_, runtime) in _stageRuntimes)
		{
			if (runtime.Systems.Count > 0)
			{
				var sortedSystems = TopologicalSortSystems(runtime.Systems);
				runtime.Sorted = sortedSystems;
				runtime.Batches = BuildParallelBatches(sortedSystems);
			}
		}
	}

	public App AddResource<T>(T resource) where T : notnull
	{
		lock (_appState.SyncRoot)
		{
			_appState.Resources[typeof(T)] = new ResourceBox<T>(resource);
		}
		return this;
	}

	// ------------------------------------------------------------------
	// App-side Bevy state API
	//
	// These methods read/write the App's own AppState instance directly.
	// The World no longer carries any Bevy state slot — all resources,
	// events, and state machines live on the App.
	// ------------------------------------------------------------------

	/// <summary>
	/// Retrieve a previously-added resource of type <typeparamref name="T"/>.
	/// </summary>
	public T GetResource<T>() where T : notnull
	{
		lock (_appState.SyncRoot)
		{
			if (!_appState.Resources.TryGetValue(typeof(T), out var boxed))
				throw new InvalidOperationException($"Resource {typeof(T).Name} not found. Did you forget to call AddResource?");

			return ((ResourceBox<T>)boxed).Value;
		}
	}

	/// <summary>
	/// Returns true if a resource of type <typeparamref name="T"/> has been registered.
	/// </summary>
	public bool HasResource<T>() where T : notnull
	{
		lock (_appState.SyncRoot)
		{
			return _appState.Resources.ContainsKey(typeof(T));
		}
	}

	/// <summary>
	/// Get a mutable reference to a resource of type <typeparamref name="T"/>.
	/// </summary>
	public ref T GetResourceRef<T>() where T : notnull
	{
		ResourceBox<T> box;
		lock (_appState.SyncRoot)
		{
			if (!_appState.Resources.TryGetValue(typeof(T), out var boxed))
				throw new InvalidOperationException($"Resource {typeof(T).Name} not found. Did you forget to call AddResource?");

			box = (ResourceBox<T>)boxed;
		}
		return ref box.Value;
	}

	/// <summary>
	/// Internal accessor used by App-aware system parameters to obtain the
	/// resource box directly from <see cref="_appState"/> without going through
	/// the world-extension shim.
	/// </summary>
	internal ResourceBox<T> GetResourceBoxInternal<T>() where T : notnull
	{
		lock (_appState.SyncRoot)
		{
			if (!_appState.Resources.TryGetValue(typeof(T), out var boxed))
				throw new InvalidOperationException($"Resource {typeof(T).Name} not found. Did you forget to call AddResource?");

			return (ResourceBox<T>)boxed;
		}
	}

	/// <summary>
	/// Remove a previously-added resource of type <typeparamref name="T"/>.
	/// </summary>
	public void RemoveResource<T>() where T : notnull
	{
		RemoveResourceCore(typeof(T));
	}

	/// <summary>
	/// Remove a resource by runtime type. Used by the deferred RemoveResource command
	/// where the type is only known as <see cref="Type"/>.
	/// </summary>
	internal void RemoveResourceByType(Type resourceType)
	{
		RemoveResourceCore(resourceType);
	}

	private bool RemoveResourceCore(Type type)
	{
		lock (_appState.SyncRoot)
		{
			return _appState.Resources.Remove(type);
		}
	}

	/// <summary>
	/// Enqueue an event of type <typeparamref name="T"/> onto its channel.
	/// </summary>
	public void SendEvent<T>(T evt) where T : notnull
	{
		GetOrCreateEventChannel<T>().Enqueue(evt);
	}

	/// <summary>
	/// Register a global observer that fires for every event of type <typeparamref name="T"/>.
	/// </summary>
	public void RegisterGlobalObserver<T>(Action<T> observer) where T : notnull
	{
		GetOrCreateEventChannel<T>().RegisterObserver(observer);
	}

	/// <summary>
	/// Lazily fetch (or create) the event channel for <typeparamref name="T"/>.
	/// Used by both App-level event send/observer registration and by App-aware
	/// system parameters (EventReader/EventWriter).
	/// </summary>
	internal EventChannel<T> GetOrCreateEventChannel<T>() where T : notnull
	{
		lock (_appState.SyncRoot)
		{
			if (!_appState.EventChannels.TryGetValue(typeof(T), out var channelObj))
			{
				var channel = new EventChannel<T>();
				_appState.EventChannels[typeof(T)] = channel;
				return channel;
			}

			return (EventChannel<T>)channelObj;
		}
	}

	/// <summary>
	/// Immediately set the current state of <typeparamref name="TState"/>.
	/// </summary>
	public void SetState<TState>(TState state) where TState : struct, Enum
	{
		var type = typeof(TState);
		lock (_appState.SyncRoot)
		{
			if (_appState.States.TryGetValue(type, out var current))
			{
				_appState.PreviousStates[type] = current;
			}
			_appState.States[type] = state;
			_appState.PendingStateChanges.Remove(type);
			_appState.StatesProcessedThisFrame.Remove(type);
		}
	}

	/// <summary>
	/// Get the current value of state <typeparamref name="TState"/>.
	/// </summary>
	public TState GetState<TState>() where TState : struct, Enum
	{
		var type = typeof(TState);
		lock (_appState.SyncRoot)
		{
			if (_appState.States.TryGetValue(type, out var state))
			{
				return (TState)state;
			}
		}
		throw new InvalidOperationException($"State {typeof(TState).Name} not found. Did you call AddState<T>()?");
	}

	/// <summary>
	/// Returns true if a state of type <typeparamref name="TState"/> has been registered.
	/// </summary>
	public bool HasState<TState>() where TState : struct, Enum
	{
		lock (_appState.SyncRoot)
		{
			return _appState.States.ContainsKey(typeof(TState));
		}
	}

	/// <summary>
	/// Queue a state transition that will be applied after the current frame.
	/// </summary>
	public void QueueState<TState>(TState next) where TState : struct, Enum
	{
		var type = typeof(TState);
		lock (_appState.SyncRoot)
		{
			if (_appState.States.TryGetValue(type, out var current) &&
				EqualityComparer<TState>.Default.Equals((TState)current, next))
			{
				_appState.PendingStateChanges.Remove(type);
				return;
			}

			_appState.PendingStateChanges[type] = new QueuedStateTransition<TState>(next);
			_appState.StatesProcessedThisFrame.Remove(type);
		}
	}

	/// <summary>
	/// Returns true if there is a pending state transition for <typeparamref name="TState"/>.
	/// </summary>
	internal bool HasQueuedState<TState>() where TState : struct, Enum
	{
		lock (_appState.SyncRoot)
		{
			return _appState.PendingStateChanges.ContainsKey(typeof(TState));
		}
	}

	/// <summary>
	/// Clear any pending state transition for <typeparamref name="TState"/>.
	/// </summary>
	internal void ClearQueuedState<TState>() where TState : struct, Enum
	{
		lock (_appState.SyncRoot)
		{
			_appState.PendingStateChanges.Remove(typeof(TState));
		}
	}

	/// <summary>
	/// Get the previously-recorded value of <typeparamref name="TState"/>, or null if none.
	/// </summary>
	internal TState? GetPreviousState<TState>() where TState : struct, Enum
	{
		var type = typeof(TState);
		lock (_appState.SyncRoot)
		{
			if (_appState.PreviousStates.TryGetValue(type, out var state))
			{
				return (TState)state;
			}
		}
		return null;
	}

	/// <summary>
	/// Returns true if the current <typeparamref name="TState"/> differs from the previously recorded one.
	/// </summary>
	internal bool StateChanged<TState>() where TState : struct, Enum
	{
		var type = typeof(TState);
		lock (_appState.SyncRoot)
		{
			if (!_appState.States.TryGetValue(type, out var current))
				return false;

			if (!_appState.PreviousStates.TryGetValue(type, out var previous))
				return true;

			return !current.Equals(previous);
		}
	}

	/// <summary>
	/// Flush all event channels for this frame. Called by <see cref="RunFrame"/>.
	/// </summary>
	internal void ProcessEvents()
	{
		var currentTick = _world.CurrentTick;
		using var channels = new PooledList<IEventChannel>(_appState.EventChannels.Count);
		lock (_appState.SyncRoot)
		{
			foreach (var channel in _appState.EventChannels.Values)
			{
				channels.Add(channel);
			}
		}
		foreach (var channel in channels.AsSpan)
		{
			channel.Flush(currentTick);
		}
	}

	public App AddState<TState>(TState initialState) where TState : struct, Enum
	{
		SetState(initialState);

		if (!HasResource<State<TState>>())
		{
			AddResource(new State<TState>(this));
		}

		if (!HasResource<NextState<TState>>())
		{
			AddResource(new NextState<TState>(this));
		}

		return this;
	}

	internal StageDescriptor GetOrCreateStageDescriptor(Stage stage)
	{
		if (_stageDescriptorByStage.TryGetValue(stage, out var existing))
			return existing;

		_stageRuntimes[stage] = new StageRuntime();
		var descriptor = new StageDescriptor(stage);
		_stageDescriptors.Add(descriptor);
		_stageDescriptorByStage[stage] = descriptor;
		return descriptor;
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
			// Silently skip duplicate plugin installs to avoid library Console output.
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
		var (descriptor, previous) = RegisterSystem(system);
		return new SystemConfigurator(this, descriptor, previous);
	}

	/// <summary>
	/// Add a system directly to a stage (simpler API)
	/// </summary>
	public App AddSystem(Stage stage, ISystem system)
	{
		var (descriptor, _) = RegisterSystem(system);
		AddSystemToStage(stage, descriptor);
		return this;
	}

	/// <summary>
	/// Common path for both AddSystem overloads: wire ParameterizedSystem to this App,
	/// create a fresh descriptor, advance the _previousSystem chain (used by .Chain()),
	/// and return both the new descriptor and the prior one.
	/// </summary>
	private (SystemDescriptor Descriptor, SystemDescriptor? Previous) RegisterSystem(ISystem system)
	{
		// Wire ParameterizedSystem to this App so its params can fetch from AppState directly.
		if (system is ParameterizedSystem ps)
			ps.SetApp(this);

		var descriptor = new SystemDescriptor(system);
		var previous = _previousSystem;
		_previousSystem = descriptor;
		return (descriptor, previous);
	}

	public App AddObserver<T>(Action<T> observer) where T : notnull
	{
		RegisterGlobalObserver(observer);
		return this;
	}

	internal void AddSystemToStage(Stage stage, SystemDescriptor descriptor)
	{
		if (!_stageRuntimes.ContainsKey(stage))
		{
			AddStage(stage);
		}
		descriptor.Stage = stage; // Set the stage on the descriptor
		_stageRuntimes[stage].Systems.Add(descriptor);

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
		// Use boxed enum directly as key - no ToString() allocation
		var key = (typeof(TState), (object)state);
		if (!_onEnterSystems.TryGetValue(key, out var list))
		{
			list = new List<SystemDescriptor>();
			_onEnterSystems[key] = list;
		}
		list.Add(descriptor);

		RegisterStateChangeDetector<TState>();
	}

	internal void RegisterOnExitSystem<TState>(TState state, SystemDescriptor descriptor) where TState : struct, Enum
	{
		// Use boxed enum directly as key - no ToString() allocation
		var key = (typeof(TState), (object)state);
		if (!_onExitSystems.TryGetValue(key, out var list))
		{
			list = new List<SystemDescriptor>();
			_onExitSystems[key] = list;
		}
		list.Add(descriptor);

		RegisterStateChangeDetector<TState>();
	}

	private void RegisterStateChangeDetector<TState>() where TState : struct, Enum
	{
		var type = typeof(TState);

		if (_registeredStateTypes.Contains(type))
			return;

		_registeredStateTypes.Add(type);

		var detector = new StateChangeDetector<TState>(this);

		lock (_appState.SyncRoot)
		{
			_appState.StateChangeDetectors.Add(detector);
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
			var appState = _app._appState;
			var world = _app._world;
			var type = typeof(TState);

			TState? previousState;
			TState? currentState;

			lock (appState.SyncRoot)
			{
				if (appState.StatesProcessedThisFrame.Contains(type))
					return;

				// Manual StateChanged check — avoid re-entering the lock.
				if (!appState.States.TryGetValue(type, out var currentObj))
					return;
				if (appState.PreviousStates.TryGetValue(type, out var previousObj) && currentObj.Equals(previousObj))
					return;

				appState.StatesProcessedThisFrame.Add(type);

				// Unbox while holding the lock. previousObj may be null when no prior state exists.
				previousState = previousObj is TState p ? p : (TState?)null;
				currentState = (TState)currentObj;
			}

			if (previousState.HasValue)
			{
				var exitKey = (type, (object)previousState.Value);
				if (_app._onExitSystems.TryGetValue(exitKey, out var exitSystems))
				{
					foreach (var descriptor in exitSystems)
					{
						if (descriptor.ShouldRun(world))
							descriptor.System.Run(world);
					}
				}
			}

			if (currentState.HasValue)
			{
				var enterKey = (type, (object)currentState.Value);
				if (_app._onEnterSystems.TryGetValue(enterKey, out var enterSystems))
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
		ProcessEvents();
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
			ThreadingMode.Auto => _multipleProcessors,
			_ => false
		};

		if (!_stageRuntimes.TryGetValue(stage, out var runtime))
			return;

		// In single-threaded mode, skip batching and just run systems in topological order
		// This preserves declaration order and respects explicit dependencies
		if (!useParallel)
		{
			var systems = runtime.Sorted;
			if (systems == null)
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
		var batches = runtime.Batches;
		if (batches == null)
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
			batches.Add(BuildOneBatch(remaining));
		}

		return batches;
	}

	/// <summary>
	/// Pulls one batch of compatible systems out of <paramref name="remaining"/>.
	/// Removes the chosen systems from the list and returns the batch.
	/// </summary>
	private static List<SystemDescriptor> BuildOneBatch(List<SystemDescriptor> remaining)
	{
		var batch = new List<SystemDescriptor>();
		var batchAccess = new SystemParamAccess();

		// Process systems in forward order to preserve topological sort order
		for (int i = 0; i < remaining.Count;)
		{
			var descriptor = remaining[i];

			if (TryAddToBatch(descriptor, batch, batchAccess))
			{
				remaining.RemoveAt(i);
				// Next item shifted into current position — single-threaded batches return immediately
				if (descriptor.ThreadingMode == ThreadingMode.Single)
					break;
			}
			else
			{
				i++;
			}
		}

		return batch;
	}

	/// <summary>
	/// Decides whether <paramref name="descriptor"/> can join <paramref name="batch"/>.
	/// On success, mutates <paramref name="batchAccess"/> to include the descriptor's access pattern.
	/// </summary>
	private static bool TryAddToBatch(SystemDescriptor descriptor, List<SystemDescriptor> batch, SystemParamAccess batchAccess)
	{
		bool systemRequiresSingleThread = descriptor.ThreadingMode == ThreadingMode.Single;

		// Single-threaded systems must occupy their own batch
		if (systemRequiresSingleThread)
		{
			if (batch.Count > 0)
				return false;
			batch.Add(descriptor);
			return true;
		}

		// If batch already contains a single-threaded system, nothing else may join
		if (batch.Count == 1 && batch[0].ThreadingMode == ThreadingMode.Single)
			return false;

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
		foreach (var batchedSystem in batch)
		{
			if (descriptor.BeforeSystems.Contains(batchedSystem) ||
				descriptor.AfterSystems.Contains(batchedSystem) ||
				batchedSystem.BeforeSystems.Contains(descriptor) ||
				batchedSystem.AfterSystems.Contains(descriptor))
			{
				return false;
			}
		}

		// Check resource conflicts
		if (batchAccess.ConflictsWith(access))
			return false;

		// No conflict - add to batch and merge access patterns
		batch.Add(descriptor);
		foreach (var read in access.ReadResources)
			batchAccess.ReadResources.Add(read);
		foreach (var write in access.WriteResources)
			batchAccess.WriteResources.Add(write);
		return true;
	}

	private void ProcessStateTransitions()
	{
		using var transitions = new PooledList<IQueuedStateTransition>(_appState.PendingStateChanges.Count);
		using var detectors = new PooledList<IStateChangeDetector>(_appState.StateChangeDetectors.Count);
		lock (_appState.SyncRoot)
		{
			foreach (var t in _appState.PendingStateChanges.Values)
			{
				transitions.Add(t);
			}
			_appState.PendingStateChanges.Clear();
			foreach (var d in _appState.StateChangeDetectors)
			{
				detectors.Add(d);
			}
			_appState.StatesProcessedThisFrame.Clear();
		}
		foreach (var transition in transitions.AsSpan)
		{
			transition.Apply(this);
		}
		foreach (var detector in detectors.AsSpan)
		{
			detector.Detect();
		}

		// After processing all state transitions and running OnEnter/OnExit systems,
		// update PreviousStates to match current States so StateChanged returns false next frame
		lock (_appState.SyncRoot)
		{
			foreach (var kvp in _appState.States)
			{
				_appState.PreviousStates[kvp.Key] = kvp.Value;
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

		// O(1) declaration-order lookup, avoids O(n) IndexOf per BeforeSystems entry.
		var declarationIndex = new Dictionary<SystemDescriptor, int>(systems.Count);
		for (int i = 0; i < systems.Count; i++)
			declarationIndex[systems[i]] = i;

		void Visit(SystemDescriptor node)
		{
			if (visited.Contains(node)) return;
			if (visiting.Contains(node))
				throw new InvalidOperationException("Circular dependency detected in system ordering");

			visiting.Add(node);

			// Visit dependencies in their original declaration order.
			// Preserves List.IndexOf semantics: cross-stage refs not in this list return -1 and sort first.
			var orderedBefore = node.BeforeSystems
				.OrderBy(s => declarationIndex.GetValueOrDefault(s, -1))
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

	/// <summary>
	/// Owning <see cref="App"/> for this configurator. Internal helper used by the
	/// generated <see cref="SystemExtensions"/> <c>RunIf</c> overloads to obtain
	/// the App reference required by <see cref="ISystemParam.Initialize"/> and
	/// <see cref="ISystemParam.Fetch"/>.
	/// </summary>
	internal App App => _app;

	// ISystemStageSelector
	public ISystemConfigurator InStage(Stage stage)
	{
		if (_stageAssigned && _descriptor.Stage != null)
		{
			_app._stageRuntimes[_descriptor.Stage].Systems.Remove(_descriptor);
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
		var app = _app;
		return RunIf(_ => app.HasResource<T>());
	}

	public ISystemConfigurator RunIfResourceEquals<T>(T value) where T : notnull, IEquatable<T>
	{
		var app = _app;
		return RunIf(_ =>
			app.HasResource<T>() && app.GetResource<T>().Equals(value));
	}

	public ISystemConfigurator RunIfState<TState>(TState state) where TState : struct, Enum
	{
		var app = _app;
		return RunIf(_ =>
		{
			if (!app.HasState<TState>()) return false;
			return app.GetState<TState>().Equals(state);
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
			_descriptor.BeforeSystems.Add(target);
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
			target.BeforeSystems.Add(_descriptor);
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
			_descriptor.BeforeSystems.Add(_previousSystem);
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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

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
	private static readonly ConditionalWeakTable<TinyEcs.World, WorldState> _worldStates = new();

	internal static WorldState GetState(this TinyEcs.World world)
	{
		return _worldStates.GetValue(world, static _ => new WorldState());
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
		List<IEventChannel> channels;
		var currentTick = world.CurrentTick;
		lock (state.SyncRoot)
		{
			channels = state.EventChannels.Values.ToList();
		}
		foreach (var channel in channels)
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

internal class WorldState
{
	public object SyncRoot { get; } = new object();
	public Dictionary<Type, object> Resources { get; } = new();
	public Dictionary<Type, IEventChannel> EventChannels { get; } = new();
	public Dictionary<Type, object> States { get; } = new();
	public Dictionary<Type, object> PreviousStates { get; } = new();
	public HashSet<Type> StatesProcessedThisFrame { get; } = new();
	public List<Action> StateChangeDetectors { get; } = new();
}

internal sealed class ResourceBox<T> where T : notnull
{
	public T Value;

	public ResourceBox(T value)
	{
		Value = value;
	}
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
		List<T>? snapshot = null;
		List<Action<T>>? observers = null;

		lock (_lock)
		{
			var newFrame = _activeTick != currentTick;
			if (newFrame)
			{
				_activeTick = currentTick;
				_observerCursor = 0;

				if (_writeBuffer.Count > 0)
				{
					(_readBuffer, _writeBuffer) = (_writeBuffer, _readBuffer);
					_writeBuffer.Clear();
					_epoch++;

					if (_observers.Count > 0 && _readBuffer.Count > 0)
					{
						snapshot = new List<T>(_readBuffer);
						observers = new List<Action<T>>(_observers);
					}
				}
				else if (_readBuffer.Count > 0)
				{
					_readBuffer.Clear();
					_epoch++;
				}
			}

			if (!newFrame && _writeBuffer.Count > 0)
			{
				var startIndex = _readBuffer.Count;
				if (_writeBuffer.Count == 1)
				{
					_readBuffer.Add(_writeBuffer[0]);
				}
				else
				{
					_readBuffer.AddRange(_writeBuffer);
				}
				_writeBuffer.Clear();

				if (_observers.Count > 0 && startIndex < _readBuffer.Count)
				{
					var count = _readBuffer.Count - startIndex;
					snapshot = new List<T>(count);
					for (var i = startIndex; i < _readBuffer.Count; i++)
					{
						snapshot.Add(_readBuffer[i]);
					}
					observers = new List<Action<T>>(_observers);
				}

				_observerCursor = _readBuffer.Count;
			}
			else if (snapshot == null && _observers.Count > 0 && _observerCursor < _readBuffer.Count)
			{
				var count = _readBuffer.Count - _observerCursor;
				if (count > 0)
				{
					snapshot = new List<T>(count);
					for (var i = _observerCursor; i < _readBuffer.Count; i++)
					{
						snapshot.Add(_readBuffer[i]);
					}
					observers = new List<Action<T>>(_observers);
				}

				_observerCursor = _readBuffer.Count;
			}
			else
			{
				_observerCursor = _readBuffer.Count;
			}

			if (snapshot != null)
			{
				_observerCursor = _readBuffer.Count;
			}
		}

		if (snapshot == null || observers == null)
		{
			return;
		}

		foreach (var evt in snapshot)
		{
			foreach (var observer in observers)
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

// Step 1: Must choose stage or state transition
public interface ISystemStageSelector
{
	ISystemConfigurator InStage(Stage stage);
	ISystemConfigurator OnEnter<TState>(TState state) where TState : struct, Enum;
	ISystemConfigurator OnExit<TState>(TState state) where TState : struct, Enum;
}

// Step 2: Configure system (optional) and build
public interface ISystemConfigurator
{
	ISystemConfigurator RunIf(Func<TinyEcs.World, bool> condition);
	ISystemConfigurator RunIfResourceExists<T>() where T : notnull;
	ISystemConfigurator RunIfResourceEquals<T>(T value) where T : notnull, IEquatable<T>;
	ISystemConfigurator RunIfState<TState>(TState state) where TState : struct, Enum;
	ISystemConfigurator SingleThreaded();
	ISystemConfigurator WithThreadingMode(ThreadingMode mode);
	ISystemConfiguratorOrdered After(string label);
	ISystemConfiguratorOrdered Before(string label);
	ISystemConfiguratorLabeled Label(string label);
	ISystemConfiguratorOrdered Chain();
	App Build();
}

// Step 3: After labeling, cannot label again
public interface ISystemConfiguratorLabeled
{
	ISystemConfiguratorLabeled RunIf(Func<TinyEcs.World, bool> condition);
	ISystemConfiguratorLabeled RunIfResourceExists<T>() where T : notnull;
	ISystemConfiguratorLabeled RunIfResourceEquals<T>(T value) where T : notnull, IEquatable<T>;
	ISystemConfiguratorLabeled RunIfState<TState>(TState state) where TState : struct, Enum;
	ISystemConfiguratorLabeled SingleThreaded();
	ISystemConfiguratorLabeled WithThreadingMode(ThreadingMode mode);
	ISystemConfiguratorOrdered After(string label);
	ISystemConfiguratorOrdered Before(string label);
	ISystemConfiguratorOrdered Chain();
	App Build();
}

// Step 4: After ordering (After/Before/Chain), cannot order again
public interface ISystemConfiguratorOrdered
{
	ISystemConfiguratorOrdered RunIf(Func<TinyEcs.World, bool> condition);
	ISystemConfiguratorOrdered RunIfResourceExists<T>() where T : notnull;
	ISystemConfiguratorOrdered RunIfResourceEquals<T>(T value) where T : notnull, IEquatable<T>;
	ISystemConfiguratorOrdered RunIfState<TState>(TState state) where TState : struct, Enum;
	ISystemConfiguratorOrdered SingleThreaded();
	ISystemConfiguratorOrdered WithThreadingMode(ThreadingMode mode);
	App Build();
}

// ============================================================================
// App Builder (uses TinyEcs.World under the hood)
// ============================================================================

public class App
{
	private readonly TinyEcs.World _world;
	private readonly ThreadingMode _threadingMode;
	internal readonly Dictionary<Stage, List<SystemDescriptor>> _stageSystems = new();
	private readonly List<StageDescriptor> _stageDescriptors = new();
	private readonly Dictionary<string, SystemDescriptor> _labeledSystems = new();
	private SystemDescriptor? _lastAddedSystem = null;
	private readonly HashSet<Type> _installedPlugins = new();

	// State transition systems - use object as key to store boxed enum values (no toString() allocation)
	private readonly Dictionary<Type, Dictionary<object, List<SystemDescriptor>>> _onEnterSystems = new();
	private readonly Dictionary<Type, Dictionary<object, List<SystemDescriptor>>> _onExitSystems = new();
	private readonly HashSet<Type> _registeredStateTypes = new();

	// Startup tracking
	private bool _startupHasRun = false;

	// Cached sorted results - computed once after app is built
	private List<StageDescriptor>? _sortedStages = null;
	private readonly Dictionary<Stage, List<SystemDescriptor>> _sortedStageSystems = new();
	private readonly Dictionary<Stage, List<List<SystemDescriptor>>> _cachedBatches = new();

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
		if (_sortedStages != null)
			return; // Already built

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
		_lastAddedSystem = descriptor;
		return new SystemConfigurator(this, descriptor);
	}

	/// <summary>
	/// Add a system directly to a stage (simpler API)
	/// </summary>
	public App AddSystem(Stage stage, ISystem system)
	{
		var descriptor = new SystemDescriptor(system);
		_lastAddedSystem = descriptor;
		AddSystemToStage(stage, descriptor);
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
	}

	internal void RegisterLabel(string label, SystemDescriptor descriptor)
	{
		_labeledSystems[label] = descriptor;
		descriptor.Label = label;
	}

	internal SystemDescriptor? GetSystemByLabel(string label)
	{
		return _labeledSystems.GetValueOrDefault(label);
	}

	internal SystemDescriptor? GetLastAddedSystem() => _lastAddedSystem;

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
		Action detector = () =>
		{
			TState? previousState = null;
			TState? currentState = null;

			lock (worldState.SyncRoot)
			{
				if (worldState.StatesProcessedThisFrame.Contains(type))
					return;

				if (!_world.StateChanged<TState>())
					return;

				worldState.StatesProcessedThisFrame.Add(type);
				previousState = _world.GetPreviousState<TState>();
				currentState = _world.HasState<TState>() ? _world.GetState<TState>() : (TState?)null;
			}

			if (previousState != null && _onExitSystems.TryGetValue(type, out var exitDict))
			{
				object prevStateKey = previousState.Value;
				if (exitDict.TryGetValue(prevStateKey, out var exitSystems))
				{
					foreach (var descriptor in exitSystems)
					{
						if (descriptor.ShouldRun(_world))
							descriptor.System.Run(_world);
					}
				}
			}

			if (currentState != null && _onEnterSystems.TryGetValue(type, out var enterDict))
			{
				object currStateKey = currentState.Value;
				if (enterDict.TryGetValue(currStateKey, out var enterSystems))
				{
					foreach (var descriptor in enterSystems)
					{
						if (descriptor.ShouldRun(_world))
							descriptor.System.Run(_world);
					}
				}
			}
		};

		lock (worldState.SyncRoot)
		{
			worldState.StateChangeDetectors.Add(detector);
		}
	}

	public void RunStartup()
	{
		if (_startupHasRun)
			return;

		_startupHasRun = true;

		// Build execution order once before first run
		BuildExecutionOrder();

		// Increment world tick for change detection
		// This marks all modifications in Startup with tick 1
		_world.Update();

		ExecuteSystemsParallel(Stage.Startup);

		// Auto-flush observers after startup stage
		_world.FlushObservers();

		ProcessStateTransitions();
		_world.ProcessEvents();
	}

	public void Run()
	{
		RunStartup();

		// Increment world tick for change detection
		// This marks all modifications in this frame with the new tick
		_world.Update();

		// Use cached sorted stages (already built in RunStartup)
		foreach (var stageDesc in _sortedStages!)
		{
			if (stageDesc.Stage == Stage.Startup)
				continue;

			ExecuteSystemsParallel(stageDesc.Stage);

			// Auto-flush observers after each stage (like Bevy's apply_deferred)
			_world.FlushObservers();
		}

		ProcessStateTransitions();
		_world.ProcessEvents();
	}

	/// <summary>
	/// Execute systems in parallel where possible, respecting dependency constraints
	/// </summary>
	private void ExecuteSystemsParallel(Stage stage)
	{
		// Use cached batches (already computed during BuildExecutionOrder)
		if (!_cachedBatches.TryGetValue(stage, out var batches))
			return;

		// Determine if we should use parallel execution
		bool useParallel = _threadingMode switch
		{
			ThreadingMode.Single => false,
			ThreadingMode.Multi => true,
			ThreadingMode.Auto => Environment.ProcessorCount > 1,
			_ => false
		};

		// Execute each batch (systems within a batch run in parallel)
		foreach (var batch in batches)
		{
			if (batch.Count == 1 || !useParallel)
			{
				// Single system or single-threaded mode - run sequentially
				foreach (var descriptor in batch)
				{
					if (descriptor.ShouldRun(_world))
					{
						descriptor.System.Run(_world);
					}
				}
			}
			else
			{
				// Multiple systems and parallel mode enabled - run in parallel
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

				// Check if this system conflicts with the current batch
				if (!batchAccess.ConflictsWith(access))
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
		List<Action> detectors;
		lock (worldState.SyncRoot)
		{
			detectors = worldState.StateChangeDetectors.ToList();
			worldState.StatesProcessedThisFrame.Clear();
		}
		foreach (var detector in detectors)
		{
			detector();
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
				.OrderBy(systems.IndexOf)
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

public class SystemConfigurator : ISystemStageSelector, ISystemConfigurator, ISystemConfiguratorLabeled, ISystemConfiguratorOrdered
{
	private readonly App _app;
	private readonly SystemDescriptor _descriptor;
	private bool _stageAssigned = false;

	internal SystemConfigurator(App app, SystemDescriptor descriptor)
	{
		_app = app;
		_descriptor = descriptor;
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

	public ISystemConfiguratorOrdered After(string label)
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

	public ISystemConfiguratorOrdered Before(string label)
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

	public ISystemConfiguratorLabeled Label(string label)
	{
		_app.RegisterLabel(label, _descriptor);
		return this;
	}

	public ISystemConfiguratorOrdered Chain()
	{
		var previousSystem = _app.GetLastAddedSystem();
		if (previousSystem != null && previousSystem != _descriptor)
		{
			if (!_descriptor.BeforeSystems.Contains(previousSystem))
				_descriptor.BeforeSystems.Add(previousSystem);

			if (!previousSystem.AfterSystems.Contains(_descriptor))
				previousSystem.AfterSystems.Add(_descriptor);
		}
		return this;
	}

	// ISystemConfiguratorLabeled implementations
	ISystemConfiguratorLabeled ISystemConfiguratorLabeled.RunIf(Func<TinyEcs.World, bool> condition)
	{
		_descriptor.RunConditions.Add(condition);
		return this;
	}

	ISystemConfiguratorLabeled ISystemConfiguratorLabeled.RunIfResourceExists<T>()
	{
		_descriptor.RunConditions.Add(world => world.HasResource<T>());
		return this;
	}

	ISystemConfiguratorLabeled ISystemConfiguratorLabeled.RunIfResourceEquals<T>(T value)
	{
		_descriptor.RunConditions.Add(world =>
			world.HasResource<T>() && world.GetResource<T>().Equals(value));
		return this;
	}

	ISystemConfiguratorLabeled ISystemConfiguratorLabeled.RunIfState<TState>(TState state)
	{
		_descriptor.RunConditions.Add(world =>
		{
			if (!world.HasState<TState>()) return false;
			return world.GetState<TState>().Equals(state);
		});
		return this;
	}

	// ISystemConfiguratorOrdered implementations
	ISystemConfiguratorOrdered ISystemConfiguratorOrdered.RunIf(Func<TinyEcs.World, bool> condition)
	{
		_descriptor.RunConditions.Add(condition);
		return this;
	}

	ISystemConfiguratorOrdered ISystemConfiguratorOrdered.RunIfResourceExists<T>()
	{
		_descriptor.RunConditions.Add(world => world.HasResource<T>());
		return this;
	}

	ISystemConfiguratorOrdered ISystemConfiguratorOrdered.RunIfResourceEquals<T>(T value)
	{
		_descriptor.RunConditions.Add(world =>
			world.HasResource<T>() && world.GetResource<T>().Equals(value));
		return this;
	}

	ISystemConfiguratorOrdered ISystemConfiguratorOrdered.RunIfState<TState>(TState state)
	{
		_descriptor.RunConditions.Add(world =>
		{
			if (!world.HasState<TState>()) return false;
			return world.GetState<TState>().Equals(state);
		});
		return this;
	}

	ISystemConfiguratorLabeled ISystemConfiguratorLabeled.SingleThreaded()
	{
		_descriptor.ThreadingMode = ThreadingMode.Single;
		return this;
	}

	ISystemConfiguratorLabeled ISystemConfiguratorLabeled.WithThreadingMode(ThreadingMode mode)
	{
		_descriptor.ThreadingMode = mode;
		return this;
	}

	ISystemConfiguratorOrdered ISystemConfiguratorOrdered.SingleThreaded()
	{
		_descriptor.ThreadingMode = ThreadingMode.Single;
		return this;
	}

	ISystemConfiguratorOrdered ISystemConfiguratorOrdered.WithThreadingMode(ThreadingMode mode)
	{
		_descriptor.ThreadingMode = mode;
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

	// Direct stage API - stage parameter first
	public static App AddSystem(this App app, Stage stage, Action<TinyEcs.World> systemFn)
	{
		return app.AddSystem(stage, new FunctionalSystem(systemFn));
	}
}


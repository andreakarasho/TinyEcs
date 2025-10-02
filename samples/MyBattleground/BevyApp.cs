using System;
using System.Collections.Generic;
using System.Linq;

namespace MyBattleground.Bevy;

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
	private static readonly Dictionary<TinyEcs.World, WorldState> _worldStates = new();

	internal static WorldState GetState(this TinyEcs.World world)
	{
		if (!_worldStates.TryGetValue(world, out var state))
		{
			state = new WorldState();
			_worldStates[world] = state;
		}
		return state;
	}

	public static void AddResource<T>(this TinyEcs.World world, T resource) where T : notnull
	{
		world.GetState().Resources[typeof(T)] = resource;
	}

	public static T GetResource<T>(this TinyEcs.World world) where T : notnull
	{
		return (T)world.GetState().Resources[typeof(T)];
	}

	public static bool HasResource<T>(this TinyEcs.World world) where T : notnull
	{
		return world.GetState().Resources.ContainsKey(typeof(T));
	}

	public static void SendEvent<T>(this TinyEcs.World world, T evt) where T : notnull
	{
		var state = world.GetState();
		var type = typeof(T);
		if (!state.EventQueues.TryGetValue(type, out var queueObj))
		{
			queueObj = new Queue<T>();
			state.EventQueues[type] = queueObj;
		}
		((Queue<T>)queueObj).Enqueue(evt);
	}

	public static void RegisterObserver<T>(this TinyEcs.World world, Action<T> observer) where T : notnull
	{
		var state = world.GetState();
		var type = typeof(T);
		if (!state.EventQueues.TryGetValue(type, out var queueObj))
		{
			queueObj = new Queue<T>();
			state.EventQueues[type] = queueObj;
		}

		var queue = (Queue<T>)queueObj;

		state.EventProcessors.Add(() =>
		{
			while (queue.Count > 0)
			{
				var evt = queue.Dequeue();
				observer(evt);
			}
		});
	}

	internal static void ProcessEvents(this TinyEcs.World world)
	{
		var state = world.GetState();
		foreach (var processor in state.EventProcessors)
		{
			processor();
		}
	}

	// State management
	public static void SetState<TState>(this TinyEcs.World world, TState state) where TState : struct, Enum
	{
		var worldState = world.GetState();
		var type = typeof(TState);
		if (worldState.States.TryGetValue(type, out var current))
		{
			worldState.PreviousStates[type] = current;
		}
		worldState.States[type] = state;
		worldState.StatesProcessedThisFrame.Remove(type); // Mark for reprocessing
	}

	public static TState GetState<TState>(this TinyEcs.World world) where TState : struct, Enum
	{
		var type = typeof(TState);
		if (world.GetState().States.TryGetValue(type, out var state))
		{
			return (TState)state;
		}
		throw new InvalidOperationException($"State {typeof(TState).Name} not found. Did you call AddState<T>()?");
	}

	public static bool HasState<TState>(this TinyEcs.World world) where TState : struct, Enum
	{
		return world.GetState().States.ContainsKey(typeof(TState));
	}

	internal static TState? GetPreviousState<TState>(this TinyEcs.World world) where TState : struct, Enum
	{
		var type = typeof(TState);
		if (world.GetState().PreviousStates.TryGetValue(type, out var state))
		{
			return (TState)state;
		}
		return null;
	}

	internal static bool StateChanged<TState>(this TinyEcs.World world) where TState : struct, Enum
	{
		var state = world.GetState();
		var type = typeof(TState);
		if (!state.States.TryGetValue(type, out var current))
			return false;

		if (!state.PreviousStates.TryGetValue(type, out var previous))
			return true;

		return !current.Equals(previous);
	}

	// Query helpers - create cached queries
	public static TinyEcs.Query<TQueryData> Query<TQueryData>(this TinyEcs.World world)
		where TQueryData : struct, TinyEcs.IData<TQueryData>, TinyEcs.IQueryIterator<TQueryData>, allows ref struct
	{
		return (TinyEcs.Query<TQueryData>)TinyEcs.Query<TQueryData>.Generate(world);
	}

	public static TinyEcs.Query<TQueryData, TQueryFilter> Query<TQueryData, TQueryFilter>(this TinyEcs.World world)
		where TQueryData : struct, TinyEcs.IData<TQueryData>, TinyEcs.IQueryIterator<TQueryData>, allows ref struct
		where TQueryFilter : struct, TinyEcs.IFilter<TQueryFilter>, allows ref struct
	{
		return (TinyEcs.Query<TQueryData, TQueryFilter>)TinyEcs.Query<TQueryData, TQueryFilter>.Generate(world);
	}
}

internal class WorldState
{
	public Dictionary<Type, object> Resources { get; } = new();
	public Dictionary<Type, object> EventQueues { get; } = new();
	public List<Action> EventProcessors { get; } = new();
	public Dictionary<Type, object> States { get; } = new();
	public Dictionary<Type, object> PreviousStates { get; } = new();
	public HashSet<Type> StatesProcessedThisFrame { get; } = new();
	public List<Action> StateChangeDetectors { get; } = new();
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

public abstract class SystemBase<T1> : ISystem where T1 : notnull
{
	public void Run(TinyEcs.World world)
	{
		var res1 = world.GetResource<T1>();
		Execute(world, res1);
	}

	protected abstract void Execute(TinyEcs.World world, T1 res1);
}

public abstract class SystemBase<T1, T2> : ISystem
	where T1 : notnull
	where T2 : notnull
{
	public void Run(TinyEcs.World world)
	{
		var res1 = world.GetResource<T1>();
		var res2 = world.GetResource<T2>();
		Execute(world, res1, res2);
	}

	protected abstract void Execute(TinyEcs.World world, T1 res1, T2 res2);
}

public abstract class SystemBase<T1, T2, T3> : ISystem
	where T1 : notnull
	where T2 : notnull
	where T3 : notnull
{
	public void Run(TinyEcs.World world)
	{
		var res1 = world.GetResource<T1>();
		var res2 = world.GetResource<T2>();
		var res3 = world.GetResource<T3>();
		Execute(world, res1, res2, res3);
	}

	protected abstract void Execute(TinyEcs.World world, T1 res1, T2 res2, T3 res3);
}

public abstract class StatefulSystemBase : ISystem
{
	public void Run(TinyEcs.World world)
	{
		Execute(world);
	}

	protected abstract void Execute(TinyEcs.World world);
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
	App Build();
}

// ============================================================================
// App Builder (uses TinyEcs.World under the hood)
// ============================================================================

public class App
{
	private readonly TinyEcs.World _world;
	internal readonly Dictionary<Stage, List<SystemDescriptor>> _stageSystems = new();
	private readonly List<StageDescriptor> _stageDescriptors = new();
	private readonly Dictionary<string, SystemDescriptor> _labeledSystems = new();
	private SystemDescriptor? _lastAddedSystem = null;
	private readonly HashSet<Type> _installedPlugins = new();

	// State transition systems - use object as key to store boxed enum values (no ToString() allocation)
	private readonly Dictionary<Type, Dictionary<object, List<SystemDescriptor>>> _onEnterSystems = new();
	private readonly Dictionary<Type, Dictionary<object, List<SystemDescriptor>>> _onExitSystems = new();
	private readonly HashSet<Type> _registeredStateTypes = new();

	// Startup tracking
	private bool _startupHasRun = false;

	// Cached sorted results - computed once after app is built
	private List<StageDescriptor>? _sortedStages = null;
	private readonly Dictionary<Stage, List<SystemDescriptor>> _sortedStageSystems = new();

	public App(TinyEcs.World world)
	{
		_world = world;

		// Initialize Startup stage (runs once)
		AddStage(Stage.Startup);

		// Initialize default stages in order
		AddStage(Stage.First);
		AddStage(Stage.PreUpdate).After(Stage.First);
		AddStage(Stage.Update).After(Stage.PreUpdate);
		AddStage(Stage.PostUpdate).After(Stage.Update);
		AddStage(Stage.Last).After(Stage.PostUpdate);
	}

	// Call this after all systems and stages are added (before first Run())
	private void BuildExecutionOrder()
	{
		if (_sortedStages != null)
			return; // Already built

		// Sort stages once
		_sortedStages = TopologicalSortStages();

		// Sort systems for each stage once
		foreach (var (stage, systems) in _stageSystems)
		{
			if (systems.Count > 0)
			{
				_sortedStageSystems[stage] = TopologicalSortSystems(systems);
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

	public StageConfigurator AddStage(Stage stage)
	{
		if (!_stageSystems.ContainsKey(stage))
		{
			_stageSystems[stage] = new List<SystemDescriptor>();
			var descriptor = new StageDescriptor(stage);
			_stageDescriptors.Add(descriptor);
			return new StageConfigurator(this, descriptor);
		}

		var existingDescriptor = _stageDescriptors.First(d => d.Stage == stage);
		return new StageConfigurator(this, existingDescriptor);
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

	public ISystemStageSelector AddSystem(ISystem system)
	{
		var descriptor = new SystemDescriptor(system);
		_lastAddedSystem = descriptor;
		return new SystemConfigurator(this, descriptor);
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
		worldState.StateChangeDetectors.Add(() =>
		{
			if (worldState.StatesProcessedThisFrame.Contains(type))
				return;

			if (!_world.StateChanged<TState>())
				return;

			worldState.StatesProcessedThisFrame.Add(type);

			var previousState = _world.GetPreviousState<TState>();
			var currentState = _world.HasState<TState>() ? _world.GetState<TState>() : (TState?)null;

			// Run OnExit systems
			if (previousState != null && _onExitSystems.TryGetValue(type, out var exitDict))
			{
				// Use boxed enum directly - no ToString() allocation
				object prevStateKey = previousState.Value;
				if (exitDict.TryGetValue(prevStateKey, out var exitSystems))
				{
					foreach (var descriptor in exitSystems)
					{
						if (descriptor.ShouldRun(_world))
						{
							descriptor.System.Run(_world);
						}
					}
				}
			}

			// Run OnEnter systems
			if (currentState != null && _onEnterSystems.TryGetValue(type, out var enterDict))
			{
				// Use boxed enum directly - no ToString() allocation
				object currStateKey = currentState.Value;
				if (enterDict.TryGetValue(currStateKey, out var enterSystems))
				{
					foreach (var descriptor in enterSystems)
					{
						if (descriptor.ShouldRun(_world))
						{
							descriptor.System.Run(_world);
						}
					}
				}
			}
		});
	}

	public void RunStartup()
	{
		if (_startupHasRun)
			return;

		_startupHasRun = true;

		// Build execution order once before first run
		BuildExecutionOrder();

		if (_sortedStageSystems.TryGetValue(Stage.Startup, out var orderedSystems))
		{
			foreach (var descriptor in orderedSystems)
			{
				if (descriptor.ShouldRun(_world))
				{
					descriptor.System.Run(_world);
				}
			}
		}

		ProcessStateTransitions();
		_world.ProcessEvents();
	}

	public void Run()
	{
		RunStartup();

		// Use cached sorted stages (already built in RunStartup)
		foreach (var stageDesc in _sortedStages!)
		{
			if (stageDesc.Stage == Stage.Startup)
				continue;

			if (!_sortedStageSystems.TryGetValue(stageDesc.Stage, out var orderedSystems))
				continue;

			foreach (var descriptor in orderedSystems)
			{
				if (descriptor.ShouldRun(_world))
				{
					descriptor.System.Run(_world);
				}
			}
		}

		ProcessStateTransitions();
		_world.ProcessEvents();
	}

	private void ProcessStateTransitions()
	{
		var worldState = _world.GetState();
		foreach (var detector in worldState.StateChangeDetectors)
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

			foreach (var before in node.BeforeSystems)
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
		_descriptor.BeforeStages.Add(stage);
		return this;
	}

	public StageConfigurator After(Stage stage)
	{
		_descriptor.AfterStages.Add(stage);
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

	public ISystemConfiguratorOrdered After(string label)
	{
		var target = _app.GetSystemByLabel(label);
		if (target != null)
		{
			_descriptor.AfterSystems.Add(target);
		}
		return this;
	}

	public ISystemConfiguratorOrdered Before(string label)
	{
		var target = _app.GetSystemByLabel(label);
		if (target != null)
		{
			target.AfterSystems.Add(_descriptor);
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
			_descriptor.AfterSystems.Add(previousSystem);
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
	public static ISystemStageSelector AddSystem(this App app, Action<TinyEcs.World> systemFn)
	{
		return app.AddSystem(new FunctionalSystem(systemFn));
	}
}

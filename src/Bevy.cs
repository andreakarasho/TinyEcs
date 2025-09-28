using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

// https://promethia-27.github.io/dependency_injection_like_bevy_from_scratch/introductions.html

#if NET9_0_OR_GREATER

[Obsolete("Use the Stage class instead.")]
public enum Stages
{
	Startup,
	FrameStart,
	BeforeUpdate,
	Update,
	AfterUpdate,
	FrameEnd,

	OnEnter,
	OnExit
}

public enum ThreadingMode
{
	Auto,
	Single,
	Multi
}

public sealed class SystemTicks
{
	public uint LastRun { get; internal set; }
	public uint ThisRun { get; internal set; }
}


public class Stage
{
	internal Stage(string name, bool runOnce)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Stage name cannot be null or whitespace.", nameof(name));
		Name = name;
		RunOnce = runOnce;
	}

	public string Name { get; }
	public bool RunOnce { get; }


	public static readonly Stage Startup = new(nameof(Startup), true);
	public static readonly Stage FrameStart = new(nameof(FrameStart), false);
	public static readonly Stage BeforeUpdate = new(nameof(BeforeUpdate), false);
	public static readonly Stage Update = new(nameof(Update), false);
	public static readonly Stage AfterUpdate = new(nameof(AfterUpdate), false);
	public static readonly Stage FrameEnd = new(nameof(FrameEnd), false);

	// These are useful just to headers for the custom OnEnter<T>/OnExit<T> stages
	internal static readonly Stage OnEnterInner = new(nameof(OnEnterInner), false);
	internal static readonly Stage OnExitInner = new(nameof(OnExitInner), false);

	public static Stage OnEnter<TState>(TState state)
		where TState : struct, Enum
	{
		var stage = new OnEnterStage<TState>(state, $"__pvt_on_enter_{typeof(TState).ToString()}.{Enum.GetName(state)}", false);
		return stage;
	}

	public static Stage OnExit<TState>(TState state)
		where TState : struct, Enum
	{
		var stage = new OnExitStage<TState>(state, $"__pvt_on_exit_{typeof(TState).ToString()}.{Enum.GetName(state)}", false);
		return stage;
	}
}

public interface IStateStage
{
	ITinySystem CreateSystem(ITinySystem sys);
}

public interface IStateEnter : IStateStage { }
public interface IStateExit : IStateStage { }

internal sealed class OnEnterStage<TState>(TState state, string name, bool runOnce = false) : Stage(name, runOnce), IStateEnter
	where TState : struct, Enum
{
	public ITinySystem CreateSystem(ITinySystem sys)
		=> new TinyOnEnterSystem<TState>(state, sys);
}

internal sealed class OnExitStage<TState>(TState state, string name, bool runOnce = false) : Stage(name, runOnce), IStateExit
	where TState : struct, Enum
{
	public ITinySystem CreateSystem(ITinySystem sys)
		=> new TinyOnExitSystem<TState>(state, sys);
}


internal sealed class StageHandler(Stage stage)
{
	private bool _initialized;

	public Stage Stage { get; } = stage;
	internal List<ITinySystem> Systems { get; } = [];


	public void AddSystem(ITinySystem system)
		=> Systems.Add(system);

	public void Initialize(RunHandler runner, World world)
	{
		if (!_initialized)
		{
			runner.Initialize(Systems, world);

			_initialized = true;
		}
	}

	public void Run(RunHandler runner, World world, uint ticks)
	{
		runner.Run(Systems, world, ticks);

		if (Stage.RunOnce)
			Systems.Clear();
	}
}

internal interface IRunHandler
{
	void Initialize(IEnumerable<ITinySystem> systems, World world);
	void Run(IEnumerable<ITinySystem> systems, World world, uint ticks);
}

internal sealed class RunHandler : IRunHandler
{
	private readonly List<ITinySystem> _singleThreads = new();
	private readonly List<ITinySystem> _multiThreads = new();

	private static readonly int ProcessorCount = Environment.ProcessorCount;

	public void Initialize(IEnumerable<ITinySystem> systems, World world)
	{
		foreach (var sys in systems)
			sys.Initialize(world);
	}

	public void Run(IEnumerable<ITinySystem> systems, World world, uint ticks)
	{
		_singleThreads.Clear();
		_multiThreads.Clear();

		foreach (var sys in systems)
		{
			if (ProcessorCount <= 1 || sys.ParamsAreLocked())
			{
				_singleThreads.Add(sys);
			}
			else
			{
				_multiThreads.Add(sys);
			}
		}

		if (_multiThreads.Count != 0)
			Parallel.ForEach(_multiThreads, system => system.ExecuteOnReady(world, ticks));

		foreach (var system in _singleThreads)
			system.ExecuteOnReady(world, ticks);
	}
}


internal sealed class StageContainer
{
	internal List<StageHandler> Stages { get; } = new();
	internal Dictionary<string, StageHandler> StageMap { get; } = new();

	public Stage Get(string name)
	{
		if (StageMap.TryGetValue(name, out var handler))
			return handler.Stage;
		throw new InvalidOperationException($"Stage '{name}' not found.");
	}

	internal void Add(Stage stage)
	{
		if (!Contains(stage))
		{
			var handler = new StageHandler(stage);
			Stages.Add(handler);
			StageMap.Add(stage.Name, handler);
		}
		else
		{
			throw new InvalidOperationException($"Stage '{stage.Name}' already exists.");
		}
	}

	public StageHandler AddBeforeOf(Stage parent, Stage stage)
	{
		if (!Contains(parent))
			throw new InvalidOperationException($"Parent stage '{parent.Name}' not found.");

		if (Contains(stage))
			throw new InvalidOperationException($"Stage '{stage.Name}' already exists.");

		var index = Stages.FindIndex(s => s.Stage == parent);
		if (index == -1)
			throw new InvalidOperationException($"Parent stage '{parent.Name}' not found.");

		var handler = new StageHandler(stage);
		Stages.Insert(index, handler);
		StageMap.Add(stage.Name, handler);
		return handler;
	}

	public StageHandler AddAfterOf(Stage parent, Stage stage)
	{
		if (!Contains(parent))
			throw new InvalidOperationException($"Parent stage '{parent.Name}' not found.");

		if (Contains(stage))
			throw new InvalidOperationException($"Stage '{stage.Name}' already exists.");

		var index = Stages.FindIndex(s => s.Stage == parent);
		if (index == -1)
			throw new InvalidOperationException($"Parent stage '{parent.Name}' not found.");

		var handler = new StageHandler(stage);
		Stages.Insert(index + 1, handler);
		StageMap.Add(stage.Name, handler);
		return handler;
	}

	public void AddSystem(ITinySystem system, Stage stage)
	{
		if (!StageMap.TryGetValue(stage.Name, out var handler))
		{
			// TODO: is there a better way to handle these special cases?
			//	     the AddAfterOf makes the current stage to be added on head instead of tail.
			if (stage is IStateEnter)
			{
				handler = AddAfterOf(Stage.OnEnterInner, stage);
			}
			else if (stage is IStateExit)
			{
				handler = AddAfterOf(Stage.OnExitInner, stage);
			}
			else
			{
				throw new InvalidOperationException($"Stage '{stage.Name}' not found.");
			}
		}

		if (stage is IStateStage stStage)
		{
			system = stStage.CreateSystem(system);
		}

		handler.AddSystem(system);
	}

	private bool Contains(Stage stage)
		=> StageMap.ContainsKey(stage.Name);
}

public partial class Scheduler
{
	private readonly World _world;
	private readonly Dictionary<Type, IEventParam> _events = new();
	private readonly StageContainer _stageContainer = new();
	private readonly RunHandler _runHandler = new();
	private bool _initialized;

	public Scheduler(World world, ThreadingMode threadingMode = ThreadingMode.Auto)
	{
		_world = world;
		ThreadingExecutionMode = threadingMode;

		_stageContainer.Add(Stage.Startup);
		_stageContainer.Add(Stage.OnExitInner);
		_stageContainer.Add(Stage.OnEnterInner);
		_stageContainer.Add(Stage.FrameStart);
		_stageContainer.Add(Stage.BeforeUpdate);
		_stageContainer.Add(Stage.Update);
		_stageContainer.Add(Stage.AfterUpdate);
		_stageContainer.Add(Stage.FrameEnd);

		AddSystemParam(world);
		AddSystemParam(new SchedulerState(this));
		AddSystemParam(new Commands(world));
	}

	public World World => _world;
	public ThreadingMode ThreadingExecutionMode { get; }



	public void Run(Func<bool> checkForExitFn, Action? cleanupFn = null)
	{
		while (!checkForExitFn())
			RunOnce();

		cleanupFn?.Invoke();
	}

	public void RunOnce()
	{
		if (!_initialized)
		{
			foreach (var stageHandler in _stageContainer.Stages)
				stageHandler.Initialize(_runHandler, _world);

			_initialized = true;
		}

		var ticks = _world.Update();

		foreach ((_, var ev) in _events)
			ev.Clear();

		foreach (var stageHandler in _stageContainer.Stages)
			stageHandler.Run(_runHandler, _world, ticks);
	}

	public Stage AddStageBeforeOf(string parentStageName, string name, bool oneShot = false)
	{
		var stage = _stageContainer.Get(parentStageName);
		if (stage == null)
			throw new InvalidOperationException($"Stage '{parentStageName}' not found.");

		return AddStageBeforeOf(stage, name, oneShot);
	}

	public Stage AddStageBeforeOf(Stage parent, string name, bool oneShot = false)
	{
		var stage = new Stage(name, oneShot);
		_stageContainer.AddBeforeOf(parent, stage);

		return stage;
	}

	public Stage AddStageAfterOf(string parentStageName, string name, bool oneShot = false)
	{
		var stage = _stageContainer.Get(parentStageName);
		if (stage == null)
			throw new InvalidOperationException($"Stage '{parentStageName}' not found.");
		return AddStageAfterOf(stage, name, oneShot);
	}

	public Stage AddStageAfterOf(Stage parent, string name, bool oneShot = false)
	{
		var stage = new Stage(name, oneShot);
		_stageContainer.AddAfterOf(parent, stage);

		return stage;
	}

	private void Add(ITinySystem sys, Stages stage)
		=> Add(sys, GetStage(stage));

	private void Add(ITinySystem sys, Stage stage)
	{
		sys.Configuration.ThreadingMode ??= ThreadingExecutionMode;
		_stageContainer.AddSystem(sys, stage);
	}

	public Scheduler AddSystem<T>(Stage stage) where T : ITinySystem, new()
	{
		var system = new T();
		Add(system, stage);
		return this;
	}

	public Scheduler AddSystem<T>(string stageName) where T : ITinySystem, new()
	{
		var stage = _stageContainer.Get(stageName);
		if (stage == null)
			throw new InvalidOperationException($"Stage '{stageName}' not found.");
		return AddSystem<T>(stage);
	}

	public ITinySystem AddSystem(string stageName, ITinySystem system)
	{
		var stage = _stageContainer.Get(stageName);
		if (stage == null)
			throw new InvalidOperationException($"Stage '{stageName}' not found.");
		return AddSystem(stage, system);
	}

	public ITinySystem AddSystem(Stage stage, ITinySystem system)
	{
		Add(system, stage);

		return system;
	}

	public Scheduler AddSystems(Stage stage, params ITinySystem[] systems)
	{
		foreach (var sys in systems)
			Add(sys, stage);
		return this;
	}

	public ITinySystem AddSystem(Action system, Stages stage = Stages.Update, ThreadingMode? threadingType = null)
	{
		if (!threadingType.HasValue)
			threadingType = ThreadingExecutionMode;

		var sys = new TinyDelegateSystem((args, ticks) =>
		{
			system();
			return true;
		});
		sys.Configuration.ThreadingMode = threadingType;
		Add(sys, stage);

		return sys;
	}

	public ITinySystem OnEnter<TState>(TState st, Action system, ThreadingMode? threadingType = null)
		where TState : struct, Enum
	{
		if (!threadingType.HasValue)
			threadingType = ThreadingExecutionMode;

		var stateChangeId = -1;

		var sys = new TinyDelegateSystem((args, ticks) =>
		{
			system();
			return true;
		})
		{ Configuration = { ThreadingMode = threadingType } }
		.RunIf((State<TState> state) => state.ShouldEnter(st, ref stateChangeId));

		Add(sys, Stage.OnEnterInner);

		return sys;
	}

	public ITinySystem OnExit<TState>(TState st, Action system, ThreadingMode? threadingType = null)
		where TState : struct, Enum
	{
		if (!threadingType.HasValue)
			threadingType = ThreadingExecutionMode;

		var stateChangeId = -1;

		var sys = new TinyDelegateSystem((args, ticks) =>
		{
			system();
			return true;
		})
		{ Configuration = { ThreadingMode = threadingType } }
		.RunIf((State<TState> state) => state.ShouldExit(st, ref stateChangeId));

		Add(sys, Stage.OnExitInner);

		return sys;
	}

	public Scheduler AddPlugin<T>() where T : IPlugin, new()
		=> AddPlugin(new T());

	public Scheduler AddPlugin(IPlugin plugin)
	{
		plugin.Build(this);

		return this;
	}

	public Scheduler AddEvent<T>() where T : notnull
	{
		if (_events.ContainsKey(typeof(T)))
			return this;

		var ev = new EventParam<T>();
		_events.Add(typeof(T), ev);
		return AddSystemParam(ev);
	}

	public Scheduler AddState<T>(T initialState = default!) where T : struct, Enum
	{
		var state = new State<T>(initialState, initialState);
		return AddSystemParam(state);
	}

	public Scheduler AddResource<T>(T resource) where T : notnull
	{
		return AddSystemParam(new Res<T>() { Value = resource });
	}

	public Scheduler AddSystemParam<T>(T param) where T : ISystemParam<World>
	{
		_world.Entity<Placeholder<T>>().Set(new Placeholder<T>() { Value = param });

		return this;
	}

	internal bool ResourceExists<T>() where T : ISystemParam<World>
	{
		return _world.Entity<Placeholder<T>>().Has<Placeholder<T>>();
	}

	internal bool InState<T>(T state) where T : struct, Enum
	{
		if (!_world.Entity<Placeholder<State<T>>>().Has<Placeholder<State<T>>>())
			return false;
		return _world.Entity<Placeholder<State<T>>>().Get<Placeholder<State<T>>>().Value.InState(state);
	}

	private static Stage GetStage(Stages stage)
	{
		return stage switch
		{
			Stages.Startup => Stage.Startup,
			Stages.FrameStart => Stage.FrameStart,
			Stages.BeforeUpdate => Stage.BeforeUpdate,
			Stages.Update => Stage.Update,
			Stages.AfterUpdate => Stage.AfterUpdate,
			Stages.FrameEnd => Stage.FrameEnd,
			Stages.OnEnter => Stage.OnEnterInner,
			Stages.OnExit => Stage.OnExitInner,
			_ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
		};
	}
}

internal struct Placeholder<T> where T : ISystemParam { public T Value; }


public interface IPlugin
{
	void Build(Scheduler scheduler);
}

public abstract class SystemParam<T> : ISystemParam<T>
{
	private int _useIndex;
	ref int ISystemParam.UseIndex => ref _useIndex;

	public SystemTicks Ticks { get; } = new();

	public void Lock(SystemTicks ticks)
	{
		Ticks.ThisRun = ticks.ThisRun;
		Ticks.LastRun = ticks.LastRun;
		Interlocked.Increment(ref _useIndex);
	}

	public void Unlock()
	{
		Interlocked.Decrement(ref _useIndex);
		Ticks.LastRun = Ticks.ThisRun;
	}
}

public interface ISystemParam
{
	internal ref int UseIndex { get; }

	void Lock(SystemTicks ticks);
	void Unlock();
}

public interface ISystemParam<TParam> : ISystemParam
{
}

public interface IIntoSystemParam<TArg>
{
	public static abstract ISystemParam<TArg> Generate(TArg arg);
}

public interface IEventParam
{
	void Clear();
}

internal sealed class EventParam<T> : SystemParam<World>, IEventParam, IIntoSystemParam<World> where T : notnull
{
	private readonly List<T> _eventsLastFrame = new(), _eventsThisFrame = new();

	internal EventParam()
	{
		Writer = new EventWriter<T>(_eventsThisFrame);
		Reader = new EventReader<T>(_eventsLastFrame);
	}

	public EventWriter<T> Writer { get; }
	public EventReader<T> Reader { get; }


	public void Clear()
	{
		_eventsLastFrame.Clear();
		_eventsLastFrame.AddRange(_eventsThisFrame);
		_eventsThisFrame.Clear();
	}

	public static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<EventParam<T>>>().Has<Placeholder<EventParam<T>>>())
			return arg.Entity<Placeholder<EventParam<T>>>().Get<Placeholder<EventParam<T>>>().Value;

		var ev = new EventParam<T>();
		arg.Entity<Placeholder<EventParam<T>>>().Set(new Placeholder<EventParam<T>>() { Value = ev });
		return ev;
	}
}

public sealed class EventWriter<T> : SystemParam<World>, IIntoSystemParam<World> where T : notnull
{
	private readonly List<T> _events;

	internal EventWriter(List<T> events)
		=> _events = events;

	public bool IsEmpty
		=> _events.Count == 0;

	public void Clear()
		=> _events.Clear();

	public void Enqueue(T ev)
		=> _events.Add(ev);

	public static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<EventParam<T>>>().Has<Placeholder<EventParam<T>>>())
			return arg.Entity<Placeholder<EventParam<T>>>().Get<Placeholder<EventParam<T>>>().Value.Writer;

		throw new NotImplementedException("EventWriter<T> must be created using the scheduler.AddEvent<T>() method");
	}
}

public sealed class EventReader<T> : SystemParam<World>, IIntoSystemParam<World> where T : notnull
{
	private readonly List<T> _events;

	internal EventReader(List<T> queue)
		=> _events = queue;

	public bool IsEmpty
		=> _events.Count == 0;

	public void Clear()
		=> _events.Clear();

	public List<T>.Enumerator GetEnumerator()
		=> _events.GetEnumerator();

	public IEnumerable<T> Values => _events;

	public static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<EventParam<T>>>().Has<Placeholder<EventParam<T>>>())
			return arg.Entity<Placeholder<EventParam<T>>>().Get<Placeholder<EventParam<T>>>().Value.Reader;

		throw new NotImplementedException("EventReader<T> must be created using the scheduler.AddEvent<T>() method");
	}
}

partial class World : SystemParam<World>, IIntoSystemParam<World>
{
	public static ISystemParam<World> Generate(World arg)
	{
		return arg;
	}
}

public class Query<TQueryData> : Query<TQueryData, Empty>, IIntoSystemParam<World>
	where TQueryData : struct, IData<TQueryData>, IQueryIterator<TQueryData>, allows ref struct
{
	internal Query(Query query) : base(query) { }

	public new static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<Query<TQueryData>>>().Has<Placeholder<Query<TQueryData>>>())
			return arg.Entity<Placeholder<Query<TQueryData>>>().Get<Placeholder<Query<TQueryData>>>().Value;

		var builder = arg.QueryBuilder();
		TQueryData.Build(builder);
		var q = new Query<TQueryData>(builder.Build());
		arg.Entity<Placeholder<Query<TQueryData>>>().Set(new Placeholder<Query<TQueryData>>() { Value = q });
		return q;
	}
}

public class Query<TQueryData, TQueryFilter> : SystemParam<World>, IIntoSystemParam<World>
	where TQueryData : struct, IData<TQueryData>, IQueryIterator<TQueryData>, allows ref struct
	where TQueryFilter : struct, IFilter<TQueryFilter>, allows ref struct
{
	private readonly Query _query;

	internal Query(Query query) => _query = query;

	public static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<Query<TQueryData, TQueryFilter>>>().Has<Placeholder<Query<TQueryData, TQueryFilter>>>())
			return arg.Entity<Placeholder<Query<TQueryData, TQueryFilter>>>().Get<Placeholder<Query<TQueryData, TQueryFilter>>>().Value;

		var builder = arg.QueryBuilder();
		TQueryData.Build(builder);
		TQueryFilter.Build(builder);
		var q = new Query<TQueryData, TQueryFilter>(builder.Build());
		arg.Entity<Placeholder<Query<TQueryData, TQueryFilter>>>().Set(new Placeholder<Query<TQueryData, TQueryFilter>>() { Value = q });
		return q;
	}

	public QueryIter<TQueryData, TQueryFilter> GetEnumerator()
		=> GetIter();

	public TQueryData Get(EcsID id)
	{
		var enumerator = GetIter(id);
		var success = enumerator.MoveNext();
		return success ? enumerator.Current : default;
	}

	public bool Contains(EcsID id)
	{
		var enumerator = GetIter(id);
		return enumerator.MoveNext();
	}

	public TQueryData Single()
	{
		EcsAssert.Panic(_query.Count() == 1, "'Single' must match one and only one entity.");
		var enumerator = GetEnumerator();
		var ok = enumerator.MoveNext();
		EcsAssert.Panic(ok, "'Single' is not matching any entity.");
		return enumerator.Current;
	}

	public int Count()
		=> _query.Count();

	private QueryIter<TQueryData, TQueryFilter> GetIter(EcsID id = 0)
		=> new(Ticks.LastRun, Ticks.ThisRun, id == 0 ? _query.Iter() : _query.Iter(id));
}

public class Single<TQueryData> : Single<TQueryData, Empty>, IIntoSystemParam<World>
	where TQueryData : struct, IData<TQueryData>, IQueryIterator<TQueryData>, allows ref struct
{
	internal Single(Query query) : base(query) { }

	public new static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<Single<TQueryData>>>().Has<Placeholder<Single<TQueryData>>>())
			return arg.Entity<Placeholder<Single<TQueryData>>>().Get<Placeholder<Single<TQueryData>>>().Value;

		var builder = arg.QueryBuilder();
		TQueryData.Build(builder);
		var q = new Single<TQueryData>(builder.Build());
		arg.Entity<Placeholder<Single<TQueryData>>>().Set(new Placeholder<Single<TQueryData>>() { Value = q });
		return q;
	}
}

public class Single<TQueryData, TQueryFilter> : SystemParam<World>, IIntoSystemParam<World>
	where TQueryData : struct, IData<TQueryData>, IQueryIterator<TQueryData>, allows ref struct
	where TQueryFilter : struct, IFilter<TQueryFilter>, allows ref struct
{
	private readonly Query _query;

	internal Single(Query query) => _query = query;

	public static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<Single<TQueryData, TQueryFilter>>>().Has<Placeholder<Single<TQueryData, TQueryFilter>>>())
			return arg.Entity<Placeholder<Single<TQueryData, TQueryFilter>>>().Get<Placeholder<Single<TQueryData, TQueryFilter>>>().Value;

		var builder = arg.QueryBuilder();
		TQueryData.Build(builder);
		TQueryFilter.Build(builder);
		var q = new Single<TQueryData, TQueryFilter>(builder.Build());
		arg.Entity<Placeholder<Single<TQueryData, TQueryFilter>>>().Set(new Placeholder<Single<TQueryData, TQueryFilter>>() { Value = q });
		return q;
	}

	public TQueryData Get()
	{
		EcsAssert.Panic(_query.Count() == 1, "'Single' must match one and only one entity.");
		var enumerator = GetIter();
		var ok = enumerator.MoveNext();
		EcsAssert.Panic(ok, "'Single' is not matching any entity.");
		return enumerator.Current;
	}

	public bool TryGet(out TQueryData data)
	{
		if (_query.Count() == 1)
		{
			var enumerator = GetIter();
			var ok = enumerator.MoveNext();
			if (ok)
			{
				data = enumerator.Current;
				return true;
			}
		}

		data = default;
		return false;
	}

	public int Count()
		=> _query.Count();

	private QueryIter<TQueryData, TQueryFilter> GetIter(EcsID id = 0)
		=> new(Ticks.LastRun, Ticks.ThisRun, id == 0 ? _query.Iter() : _query.Iter(id));
}

public sealed class State<T>(T previous, T current) : SystemParam<World>, IIntoSystemParam<World>
	where T : struct, Enum
{
	private int _stateChangeId = -1;

	internal T Previous { get; private set; } = previous;
	public T Current { get; private set; } = current;

	public static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<State<T>>>().Has<Placeholder<State<T>>>())
			return arg.Entity<Placeholder<State<T>>>().Get<Placeholder<State<T>>>().Value;

		var state = new State<T>(default, default);
		arg.Entity<Placeholder<State<T>>>().Set(new Placeholder<State<T>>() { Value = state });
		return state;
	}

	public void Set(T value)
	{
		if (!Equals(Current, value))
		{
			Previous = Current;
			Current = value;
			_stateChangeId++; // Increment the change counter
		}
	}

	internal bool InState(T? state)
	{
		return Equals(Current, state);
	}

	internal int GetChangeId() => _stateChangeId;

	internal bool ShouldEnter(T state, ref int lastProcessedChangeId)
	{
		if (!Equals(Current, state))
			return false;

		if (lastProcessedChangeId != _stateChangeId)
		{
			lastProcessedChangeId = _stateChangeId;
			return true;
		}
		return false;
	}

	internal bool ShouldExit(T state, ref int lastProcessedChangeId)
	{
		if (!Equals(Previous, state))
			return false;

		if (lastProcessedChangeId != _stateChangeId)
		{
			lastProcessedChangeId = _stateChangeId;
			return true;
		}
		return false;
	}
}

public sealed class Res<T> : SystemParam<World>, IIntoSystemParam<World>
	where T : notnull
{
	private T? _t;

	public ref T? Value => ref _t;

	public static ISystemParam<World> Generate(World arg)
	{
		var ent = arg.Entity<Placeholder<Res<T>>>();
		if (ent.Has<Placeholder<Res<T>>>())
			return ent.Get<Placeholder<Res<T>>>().Value;

		return null;
	}

	public static implicit operator T?(Res<T> reference)
		=> reference.Value;
}

public sealed class Local<T> : SystemParam<World>, IIntoSystemParam<World>
	where T : notnull
{
	private T? _t;

	public ref T? Value => ref _t;

	public static ISystemParam<World> Generate(World arg)
	{
		return new Local<T>();
	}

	public static implicit operator T?(Local<T> reference)
		=> reference.Value;
}

public sealed class SchedulerState : SystemParam<World>, IIntoSystemParam<World>
{
	private readonly Scheduler _scheduler;

	internal SchedulerState(Scheduler scheduler)
	{
		_scheduler = scheduler;
	}

	public void AddResource<T>(T resource) where T : notnull
		=> _scheduler.AddResource(resource);

	public bool ResourceExists<T>() where T : notnull
		=> _scheduler.ResourceExists<Res<T>>();

	public ref T? GetResource<T>() where T : notnull
	{
		if (_scheduler.ResourceExists<Res<T>>())
			return ref _scheduler.World.Entity<Placeholder<Res<T>>>().Get<Placeholder<Res<T>>>().Value.Value;
		throw new InvalidOperationException($"Resource of type {typeof(T)} does not exist.");
	}

	public ref T GetSystemParam<T>() where T : notnull, ISystemParam<World>
	{
		if (_scheduler.World.Entity<Placeholder<T>>().Has<Placeholder<T>>())
			return ref _scheduler.World.Entity<Placeholder<T>>().Get<Placeholder<T>>().Value;
		throw new InvalidOperationException($"SystemParam of type {typeof(T)} does not exist.");
	}

	public EventWriter<T> GetEventWriter<T>() where T : notnull
	{
		return GetSystemParam<EventParam<T>>().Writer;
	}

	public EventReader<T> GetEventReader<T>() where T : notnull
	{
		return GetSystemParam<EventParam<T>>().Reader;
	}

	public void AddState<T>(T state = default!) where T : struct, Enum
		=> _scheduler.AddState(state);

	public bool InState<T>(T state) where T : struct, Enum
		=> _scheduler.InState(state);

	public static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<SchedulerState>>().Has<Placeholder<SchedulerState>>())
			return arg.Entity<Placeholder<SchedulerState>>().Get<Placeholder<SchedulerState>>().Value;
		throw new NotImplementedException();
	}
}

public sealed class Commands : SystemParam<World>, IIntoSystemParam<World>
{
	private readonly World _world;

	internal Commands(World world)
	{
		_world = world;
	}

	public EntityCommand Entity(EcsID id = 0)
	{
		var ent = _world.Entity(id);
		return new EntityCommand(_world, ent.ID);
	}

	public static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<Commands>>().Has<Placeholder<Commands>>())
			return arg.Entity<Placeholder<Commands>>().Get<Placeholder<Commands>>().Value;
		throw new NotImplementedException();
	}
}

public readonly ref struct EntityCommand
{
	private readonly World _world;

	internal EntityCommand(World world, EcsID id) => (_world, ID) = (world, id);


	public readonly EcsID ID;

	public readonly EntityCommand Set<T>(T component) where T : struct
	{
		_world.SetDeferred(ID, component);
		return this;
	}

	public readonly EntityCommand Add<T>() where T : struct
	{
		_world.AddDeferred<T>(ID);
		return this;
	}

	public readonly EntityCommand Unset<T>() where T : struct
	{
		_world.UnsetDeferred<T>(ID);
		return this;
	}

	public readonly EntityCommand Delete()
	{
		_world.DeleteDeferred(ID);
		return this;
	}
}


public interface ITermCreator
{
	public static abstract void Build(QueryBuilder builder);
}

public interface IQueryIterator<TData>
	where TData : struct, allows ref struct
{
	TData GetEnumerator();

	[UnscopedRef]
	ref TData Current { get; }

	bool MoveNext();
}

public interface IData<TData> : ITermCreator, IQueryIterator<TData>
	where TData : struct, allows ref struct
{
	public static abstract TData CreateIterator(QueryIterator iterator);
}

public interface IFilter<TFilter> : ITermCreator, IQueryIterator<TFilter>
	where TFilter : struct, allows ref struct
{
	void SetTicks(uint lastRun, uint thisRun);
	public static abstract TFilter CreateIterator(QueryIterator iterator);
}

public ref struct Empty : IData<Empty>, IFilter<Empty>
{
	private readonly bool _asFilter;
	private QueryIterator _iterator;

	internal Empty(QueryIterator iterator, bool asFilter)
	{
		_iterator = iterator;
		_asFilter = asFilter;
	}

	public static void Build(QueryBuilder builder) { }


	[UnscopedRef]
	public ref Empty Current => ref this;

	public readonly void Deconstruct(out ReadOnlySpan<EntityView> entities, out int count)
	{
		entities = _iterator.Entities();
		count = entities.Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Empty GetEnumerator() => this;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool MoveNext() => _asFilter || _iterator.Next();

	public readonly void SetTicks(uint lastRun, uint thisRun) { }

	static Empty IData<Empty>.CreateIterator(QueryIterator iterator)
	{
		return new Empty(iterator, false);
	}

	static Empty IFilter<Empty>.CreateIterator(QueryIterator iterator)
	{
		return new Empty(iterator, true);
	}
}

/// <summary>
/// Used in query filters to find entities with the corrisponding component/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct With<T> : IFilter<With<T>>
	where T : struct
{
	[UnscopedRef]
	ref With<T> IQueryIterator<With<T>>.Current => ref this;

	public static void Build(QueryBuilder builder)
	{
		builder.With<T>();
	}

	static With<T> IFilter<With<T>>.CreateIterator(QueryIterator iterator)
	{
		return new With<T>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly With<T> IQueryIterator<With<T>>.GetEnumerator()
	{
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly bool IQueryIterator<With<T>>.MoveNext()
	{
		return true;
	}

	public readonly void SetTicks(uint lastRun, uint thisRun) { }
}

/// <summary>
/// Used in query filters to find entities without the corrisponding component/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public ref struct Without<T> : IFilter<Without<T>>
	where T : struct
{
	[UnscopedRef]
	ref Without<T> IQueryIterator<Without<T>>.Current => ref this;

	public static void Build(QueryBuilder builder)
	{
		builder.Without<T>();
	}

	static Without<T> IFilter<Without<T>>.CreateIterator(QueryIterator iterator)
	{
		return new();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly Without<T> IQueryIterator<Without<T>>.GetEnumerator()
	{
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly bool IQueryIterator<Without<T>>.MoveNext()
	{
		return true;
	}

	public readonly void SetTicks(uint lastRun, uint thisRun) { }
}

/// <summary>
/// Used in query filters to find entities with or without the corrisponding component/tag.<br/>
/// You would Unsafe.IsNullRef&lt;T&gt;(); to check if the value has been found.
/// </summary>
/// <typeparam name="T"></typeparam>
public ref struct Optional<T> : IFilter<Optional<T>>
	where T : struct
{
	[UnscopedRef]
	ref Optional<T> IQueryIterator<Optional<T>>.Current => ref this;

	public static void Build(QueryBuilder builder)
	{
		builder.Optional<T>();
	}

	static Optional<T> IFilter<Optional<T>>.CreateIterator(QueryIterator iterator)
	{
		return new();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly Optional<T> IQueryIterator<Optional<T>>.GetEnumerator()
	{
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly bool IQueryIterator<Optional<T>>.MoveNext()
	{
		return true;
	}

	public readonly void SetTicks(uint lastRun, uint thisRun) { }
}

/// <summary>
/// Used in query filters to find entities with components that have changed.
/// </summary>
/// <typeparam name="T"></typeparam>
public ref struct Changed<T> : IFilter<Changed<T>>
	where T : struct
{
	private QueryIterator _iterator;
	private Ptr<uint> _stateRow;
	private int _row, _count;
	private nint _size;
	private uint _lastRun, _thisRun;

	private Changed(QueryIterator iterator)
	{
		_iterator = iterator;
		_row = -1;
		_count = -1;
		_lastRun = 0;
		_thisRun = 0;
	}

	[UnscopedRef]
	ref Changed<T> IQueryIterator<Changed<T>>.Current => ref this;

	public static void Build(QueryBuilder builder)
	{
		builder.With<T>();
	}

	static Changed<T> IFilter<Changed<T>>.CreateIterator(QueryIterator iterator)
	{
		return new(iterator);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly Changed<T> IQueryIterator<Changed<T>>.GetEnumerator()
	{
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool IQueryIterator<Changed<T>>.MoveNext()
	{
		if (++_row >= _count)
		{
			if (!_iterator.Next())
				return false;

			_row = 0;
			_count = _iterator.Count;
			var index = _iterator.GetColumnIndexOf<T>();
			var states = _iterator.GetChangedTicks(index);

			if (states.IsEmpty)
			{
				_stateRow.Value = ref Unsafe.NullRef<uint>();
				_size = 0;
			}
			else
			{
				_stateRow.Value = ref MemoryMarshal.GetReference(states);
				_size = Unsafe.SizeOf<uint>();
			}
		}
		else
		{
			_stateRow.Value = ref Unsafe.AddByteOffset(ref _stateRow.Value, _size);
		}

		return _size > 0 && _stateRow.Value >= _lastRun && _stateRow.Value < _thisRun;
	}

	public void SetTicks(uint lastRun, uint thisRun)
	{
		_lastRun = lastRun;
		_thisRun = thisRun;
	}
}

/// <summary>
/// Used in query filters to find entities with components that have added.
/// </summary>
/// <typeparam name="T"></typeparam>
public ref struct Added<T> : IFilter<Added<T>>
	where T : struct
{
	private QueryIterator _iterator;
	private Ptr<uint> _stateRow;
	private int _row, _count;
	private nint _size;
	private uint _lastRun, _thisRun;

	private Added(QueryIterator iterator)
	{
		_iterator = iterator;
		_row = -1;
		_count = -1;
		_lastRun = 0;
		_thisRun = 0;
	}

	[UnscopedRef]
	ref Added<T> IQueryIterator<Added<T>>.Current => ref this;

	public static void Build(QueryBuilder builder)
	{
		builder.With<T>();
	}

	static Added<T> IFilter<Added<T>>.CreateIterator(QueryIterator iterator)
	{
		return new(iterator);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly Added<T> IQueryIterator<Added<T>>.GetEnumerator()
	{
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool IQueryIterator<Added<T>>.MoveNext()
	{
		if (++_row >= _count)
		{
			if (!_iterator.Next())
				return false;

			_row = 0;
			_count = _iterator.Count;
			var index = _iterator.GetColumnIndexOf<T>();
			var states = _iterator.GetAddedTicks(index);

			if (states.IsEmpty)
			{
				_stateRow.Value = ref Unsafe.NullRef<uint>();
				_size = 0;
			}
			else
			{
				_stateRow.Value = ref MemoryMarshal.GetReference(states);
				_size = Unsafe.SizeOf<uint>();
			}
		}
		else
		{
			_stateRow.Value = ref Unsafe.AddByteOffset(ref _stateRow.Value, _size);
		}

		return _size > 0 && _stateRow.Value >= _lastRun && _stateRow.Value < _thisRun;
	}

	public void SetTicks(uint lastRun, uint thisRun)
	{
		_lastRun = lastRun;
		_thisRun = thisRun;
	}
}


public ref struct MarkChanged<T> : IFilter<MarkChanged<T>>
	where T : struct
{
	private QueryIterator _iterator;
	private Ptr<uint> _stateRow;
	private int _row, _count;
	private nint _size;
	private uint _lastRun, _thisRun;

	private MarkChanged(QueryIterator iterator)
	{
		_iterator = iterator;
		_row = -1;
		_count = -1;
		_lastRun = 0;
		_thisRun = 0;
	}

	[UnscopedRef]
	ref MarkChanged<T> IQueryIterator<MarkChanged<T>>.Current => ref this;

	public static void Build(QueryBuilder builder)
	{
		// builder.With<T>();
	}

	static MarkChanged<T> IFilter<MarkChanged<T>>.CreateIterator(QueryIterator iterator)
	{
		return new(iterator);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly MarkChanged<T> IQueryIterator<MarkChanged<T>>.GetEnumerator()
	{
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool IQueryIterator<MarkChanged<T>>.MoveNext()
	{
		if (++_row >= _count)
		{
			if (!_iterator.Next())
				return false;

			_row = 0;
			_count = _iterator.Count;
			var index = _iterator.GetColumnIndexOf<T>();
			var states = _iterator.GetChangedTicks(index);

			if (states.IsEmpty)
			{
				_stateRow.Value = ref Unsafe.NullRef<uint>();
				_size = 0;
			}
			else
			{
				_stateRow.Value = ref MemoryMarshal.GetReference(states);
				_size = Unsafe.SizeOf<uint>();
			}
		}
		else
		{
			_stateRow.Value = ref Unsafe.AddByteOffset(ref _stateRow.Value, _size);
		}

		if (_size > 0)
		{
			_stateRow.Value = _thisRun;
		}

		return true;
	}

	public void SetTicks(uint lastRun, uint thisRun)
	{
		_lastRun = lastRun;
		_thisRun = thisRun;
	}
}

public partial struct Parent { }
public partial interface IChildrenComponent { }


[SkipLocalsInit]
public ref struct QueryIter<D, F>
	where D : struct, IData<D>, allows ref struct
	where F : struct, IFilter<F>, allows ref struct
{
	private D _dataIterator;
	private F _filterIterator;

	internal QueryIter(uint lastRun, uint thisRun, QueryIterator iterator)
	{
		_dataIterator = D.CreateIterator(iterator);
		_filterIterator = F.CreateIterator(iterator);
		_filterIterator.SetTicks(lastRun, thisRun);
	}

	[UnscopedRef]
	public ref D Current
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
	public readonly QueryIter<D, F> GetEnumerator() => this;
}


[AttributeUsage(AttributeTargets.Method)]
public sealed class TinySystemAttribute : Attribute
{
}

public sealed class SystemParamBuilder(World world)
{
	private readonly List<ISystemParam<World>> _params = [];
	private readonly List<Func<ISystemParam<World>>> _paramsFns = [];

	public T Add<T>() where T : ISystemParam<World>, IIntoSystemParam<World>
	{
		var param = (T)T.Generate(world);
		_params.Add(param);
		_paramsFns.Add(() => (T)T.Generate(world));
		return param;
	}

	public (ISystemParam<World>[], Func<ISystemParam<World>>[]) Build()
		=> ([.. _params], [.. _paramsFns]);
}

public sealed class SystemParamRef<T> where T : ISystemParam<World>, IIntoSystemParam<World>
{
	private readonly World _world;
	private T? _t;
	internal SystemParamRef(World world) => _world = world;

	public T Get() => _t ??= (T)T.Generate(_world);
}

public sealed class SystemConfiguration
{
	public HashSet<ITinyConditionalSystem> Conditionals { get; } = [];
	public ThreadingMode? ThreadingMode { get; set; }
}

public sealed class SystemOrder
{
	public LinkedList<ITinySystem> BeforeSystems { get; internal set; } = new();
	public LinkedList<ITinySystem> AfterSystems { get; internal set; } = new();
	public LinkedListNode<ITinySystem>? Node { get; internal set; }


	public static ITinySystem Chain(params ITinySystem[] systems)
	{
		if (systems.Length == 0)
			throw new ArgumentException("At least one system is required to create a chain.", nameof(systems));

		var first = systems[0];
		for (var i = 1; i < systems.Length; i++)
			systems[i].RunAfter(first);
		return first;
	}
}


public interface ITinyMeta
{
	ISystemParam<World>[] SystemParams { get; set; }
	SystemTicks Ticks { get; }
	SystemConfiguration Configuration { get; }


	bool ParamsAreLocked();
	void Initialize(World world);
	bool ExecuteOnReady(World world, uint ticks);

	bool BeforeExecute(World world);
	bool AfterExecute(World world);
}

public interface ITinySystem : ITinyMeta
{
	SystemOrder OrderConfiguration { get; }

	bool ParamsAreReady();
	ITinySystem RunIf<T>() where T : ITinyConditionalSystem, new();
	ITinySystem RunIf(params ITinyConditionalSystem[] conditionals);
	ITinySystem RunAfter(ITinySystem sys);
	ITinySystem RunBefore(ITinySystem sys);
}

public interface ITinyConditionalSystem : ITinyMeta
{
}


public abstract class TinySystemBase : ITinyMeta
{
	private bool _initialized;
	private Func<ISystemParam<World>>[] _paramsFns = [];

	public ISystemParam<World>[] SystemParams { get; set; } = [];
	public SystemTicks Ticks { get; } = new();
	public SystemConfiguration Configuration { get; } = new();

	public virtual void Initialize(World world)
	{
		if (_initialized)
			throw new Exception("Already initialized");

		_initialized = true;

		var builder = new SystemParamBuilder(world);
		Setup(builder);
		(SystemParams, _paramsFns) = builder.Build();

		foreach (var conditional in Configuration.Conditionals)
		{
			conditional.Initialize(world);
		}
	}

	public virtual bool BeforeExecute(World world)
		=> true;

	public virtual bool AfterExecute(World world)
		=> true;

	public bool ExecuteOnReady(World world, uint ticks)
	{
		if (!ParamsAreReady())
		{
			for (var i = 0; i < SystemParams.Length; i++)
			{
				if (SystemParams[i] == null && i < _paramsFns.Length)
				{
					SystemParams[i] = _paramsFns[i]();
				}
			}

			if (!ParamsAreReady())
				return false;
		}

		Ticks.ThisRun = ticks;
		var canRun = BeforeExecute(world);

		if (canRun)
		{
			foreach (var conditional in Configuration.Conditionals)
			{
				if (!conditional.ExecuteOnReady(world, ticks))
				{
					canRun = false;
					break;
				}
			}

			if (canRun)
			{
				canRun = Execute(world);
			}

			if (canRun)
			{
				canRun = AfterExecute(world);
			}
		}

		Ticks.LastRun = Ticks.ThisRun;

		return canRun;
	}

	public bool ParamsAreReady()
	{
		foreach (var param in SystemParams)
			if (param == null)
				return false;

		return true;
	}

	public bool ParamsAreLocked()
	{
		return Configuration.ThreadingMode switch
		{
			ThreadingMode.Single => true,
			ThreadingMode.Multi => false,
			_ => SystemParams.Any(static p => p is { UseIndex: > 0 })
		};
	}

	protected void Lock()
	{
		foreach (var param in SystemParams)
			param.Lock(Ticks);
	}

	protected void Unlock()
	{
		foreach (var param in SystemParams)
			param.Unlock();
	}

	protected abstract void Setup(SystemParamBuilder builder);
	protected abstract bool Execute(World world);
}

public abstract class TinySystem : TinySystemBase, ITinySystem
{
	public SystemOrder OrderConfiguration { get; } = new();

	public override void Initialize(World world)
	{
		base.Initialize(world);

		foreach (var afterSys in OrderConfiguration.AfterSystems)
		{
			afterSys.Initialize(world);
		}
	}

	public override bool BeforeExecute(World world)
	{
		var ticks = Ticks.ThisRun;
		foreach (var beforeSys in OrderConfiguration.BeforeSystems)
		{
			if (!beforeSys.ExecuteOnReady(world, ticks))
			{
				return false;
			}
		}

		return true;
	}

	public override bool AfterExecute(World world)
	{
		var ticks = Ticks.ThisRun;
		foreach (var afterSys in OrderConfiguration.AfterSystems)
		{
			if (!afterSys.ExecuteOnReady(world, ticks))
			{
				return false;
			}
		}

		return true;
	}


	public ITinySystem RunIf<T>() where T : ITinyConditionalSystem, new()
		=> RunIf(new T());

	public ITinySystem RunIf(params ITinyConditionalSystem[] conditionals)
	{
		// TODO: Check for duplicates?
		foreach (var sys in conditionals)
			_ = Configuration.Conditionals.Add(sys);
		return this;
	}

	public ITinySystem RunAfter(ITinySystem sys)
	{
		OrderConfiguration.Node?.List?.Remove(OrderConfiguration.Node);
		OrderConfiguration.Node = sys.OrderConfiguration.AfterSystems.AddLast(this);
		return this;
	}

	public ITinySystem RunBefore(ITinySystem sys)
	{
		OrderConfiguration.Node?.List?.Remove(OrderConfiguration.Node);
		OrderConfiguration.Node = sys.OrderConfiguration.BeforeSystems.AddLast(this);
		return this;
	}
}

public abstract class TinyConditionalSystem : TinySystemBase, ITinyConditionalSystem
{
}

public sealed partial class TinyDelegateSystem : TinySystem, ITinyConditionalSystem
{
	private readonly Func<World, SystemTicks, bool> _fn;

	public TinyDelegateSystem(Func<World, SystemTicks, bool> fn)
	{
		_fn = fn;
	}

	protected override void Setup(SystemParamBuilder builder)
	{
	}

	protected override bool Execute(World world)
	{
		return _fn(world, Ticks);
	}
}

internal abstract class TinyStateSystemAdapter<TState>(ITinySystem sys) : TinySystem
	where TState : struct, Enum
{
	protected State<TState> _state;
	protected int _stateChangedId = -1;

	public override void Initialize(World world)
	{
		base.Initialize(world);
		sys.Initialize(world);
	}

	protected override void Setup(SystemParamBuilder builder)
	{
		_state = builder.Add<State<TState>>();
	}
}

internal sealed class TinyOnEnterSystem<TState>(TState st, ITinySystem sys) : TinyStateSystemAdapter<TState>(sys)
	where TState : struct, Enum
{
	protected override bool Execute(World world)
	{
		Lock();
		world.BeginDeferred();
		var result = _state.ShouldEnter(st, ref _stateChangedId);
		if (result)
		{
			_ = sys.ExecuteOnReady(world, Ticks.ThisRun);
		}
		world.EndDeferred();
		Unlock();
		return result;
	}
}

internal sealed class TinyOnExitSystem<TState>(TState st, ITinySystem sys) : TinyStateSystemAdapter<TState>(sys)
	where TState : struct, Enum
{
	protected override bool Execute(World world)
	{
		Lock();
		world.BeginDeferred();
		var result = _state.ShouldExit(st, ref _stateChangedId);
		if (result)
		{
			_ = sys.ExecuteOnReady(world, Ticks.ThisRun);
		}
		world.EndDeferred();
		Unlock();
		return result;
	}
}

#endif

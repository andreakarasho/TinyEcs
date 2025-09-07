using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

// https://promethia-27.github.io/dependency_injection_like_bevy_from_scratch/introductions.html

#if NET9_0_OR_GREATER

public sealed partial class FuncSystem<TArg> where TArg : notnull
{
	private readonly TArg _arg;
	private readonly Func<SystemTicks, TArg, Func<SystemTicks, TArg, bool>, bool> _fn;
	private readonly List<Func<SystemTicks, TArg, bool>> _conditions;
	private readonly Func<SystemTicks, TArg, bool> _validator;
	private readonly Func<bool> _checkInUse;
	private readonly Stages _stage;
	private readonly ThreadingMode _threadingType;
	private readonly LinkedList<FuncSystem<TArg>> _after = new();
	private readonly LinkedList<FuncSystem<TArg>> _before = new();
	internal LinkedListNode<FuncSystem<TArg>>? Node { get; set; }
	internal SystemTicks Ticks { get; } = new();


	internal FuncSystem(TArg arg, Func<SystemTicks, TArg, Func<SystemTicks, TArg, bool>, bool> fn, Func<bool> checkInUse, Stages stage, ThreadingMode threadingType)
	{
		_arg = arg;
		_fn = fn;
		_conditions = new();
		_validator = ValidateConditions;
		_checkInUse = checkInUse;
		_threadingType = threadingType;
		_stage = stage;
	}

	internal void Run(uint ticks)
	{
		Ticks.ThisRun = ticks;

		foreach (var s in _before)
			s.Run(ticks);

		if (_fn(Ticks, _arg, _validator))
		{
			foreach (var s in _after)
				s.Run(ticks);
		}

		Ticks.LastRun = Ticks.ThisRun;
	}

	public FuncSystem<TArg> RunIf(Func<bool> condition)
	{
		_conditions.Add((_, _) => condition());
		return this;
	}

	public FuncSystem<TArg> RunAfter(FuncSystem<TArg> parent)
	{
		if (this == parent || Contains(parent, s => s._after))
			throw new InvalidOperationException("Circular dependency detected");

		Node?.List?.Remove(Node);
		Node = parent._after.AddLast(this);

		return this;
	}

	public FuncSystem<TArg> RunAfter(params ReadOnlySpan<FuncSystem<TArg>> systems)
	{
		foreach (var system in systems)
			system.RunAfter(this);

		return this;
	}

	public FuncSystem<TArg> RunBefore(FuncSystem<TArg> parent)
	{
		if (this == parent || Contains(parent, s => s._before))
			throw new InvalidOperationException("Circular dependency detected");

		Node?.List?.Remove(Node);
		Node = parent._before.AddLast(this);

		return this;
	}

	public FuncSystem<TArg> RunBefore(params ReadOnlySpan<FuncSystem<TArg>> systems)
	{
		foreach (var system in systems)
			system.RunBefore(this);

		return this;
	}

	private bool Contains(FuncSystem<TArg> system, Func<FuncSystem<TArg>, LinkedList<FuncSystem<TArg>>> direction)
	{
		var current = this;
		while (current != null)
		{
			if (current == system)
				return true;

			var nextNode = direction(current)?.First;
			current = nextNode?.Value;
		}
		return false;
	}

	internal bool IsResourceInUse()
	{
		return _threadingType switch
		{
			ThreadingMode.Multi => false,
			ThreadingMode.Single => true,
			_ or ThreadingMode.Auto => _checkInUse()
		};
	}

	private bool ValidateConditions(SystemTicks ticks, TArg args)
	{
		foreach (var fn in _conditions)
			if (!fn(ticks, args))
				return false;
		return true;
	}
}

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
	public uint LastRun { get; set; }
	public uint ThisRun { get; set; }
}

public partial class Scheduler
{
	private readonly World _world;
	private readonly LinkedList<FuncSystem<World>>[] _systems = new LinkedList<FuncSystem<World>>[(int)Stages.OnExit + 1];
	private readonly List<FuncSystem<World>> _singleThreads = new();
	private readonly List<FuncSystem<World>> _multiThreads = new();
	private readonly Dictionary<Type, IEventParam> _events = new();

	public Scheduler(World world, ThreadingMode threadingMode = ThreadingMode.Auto)
	{
		_world = world;
		ThreadingExecutionMode = threadingMode;

		for (var i = 0; i < _systems.Length; ++i)
			_systems[i] = new LinkedList<FuncSystem<World>>();

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
		var ticks = _world.Update();

		foreach ((_, var ev) in _events)
			ev.Clear();

		RunStage(Stages.Startup, ticks);
		_systems[(int)Stages.Startup].Clear();

		RunStage(Stages.OnExit, ticks);
		RunStage(Stages.OnEnter, ticks);

		for (var stage = Stages.FrameStart; stage <= Stages.FrameEnd; stage += 1)
			RunStage(stage, ticks);
	}

	private void RunStage(Stages stage, uint ticks)
	{
		_singleThreads.Clear();
		_multiThreads.Clear();

		var systems = _systems[(int)stage];

		if (systems.Count == 0)
			return;

		foreach (var sys in systems)
		{
			if (sys.IsResourceInUse())
			{
				_singleThreads.Add(sys);
			}
			else
			{
				_multiThreads.Add(sys);
			}
		}

		var multithreading = _multiThreads;
		var singlethreading = _singleThreads;

		if (multithreading.Count > 0)
			Parallel.ForEach(multithreading, s => s.Run(ticks));

		foreach (var system in singlethreading)
			system.Run(ticks);
	}

	internal void Add(FuncSystem<World> sys, Stages stage)
	{
		sys.Node = _systems[(int)stage].AddLast(sys);
	}

	public FuncSystem<World> AddSystem(Action system, Stages stage = Stages.Update, ThreadingMode? threadingType = null)
	{
		if (!threadingType.HasValue)
			threadingType = ThreadingExecutionMode;

		var sys = new FuncSystem<World>(_world, (ticks, args, runIf) =>
		{
			if (runIf?.Invoke(ticks, args) ?? true)
			{
				system();
				return true;
			}
			return false;
		}, () => false, stage, threadingType.Value);
		Add(sys, stage);

		return sys;
	}

	public FuncSystem<World> OnEnter<TState>(TState st, Action system, ThreadingMode? threadingType = null)
		where TState : struct, Enum
	{
		if (!threadingType.HasValue)
			threadingType = ThreadingExecutionMode;

		var stateChangeId = -1;

		var sys = new FuncSystem<World>(_world, (ticks, args, runIf) =>
		{
			if (runIf?.Invoke(ticks, args) ?? true)
			{
				system();
				return true;
			}
			return false;
		}, () => false, Stages.OnEnter, threadingType.Value)
		.RunIf((State<TState> state) => state.ShouldEnter(st, ref stateChangeId));

		Add(sys, Stages.OnEnter);

		return sys;
	}

	public FuncSystem<World> OnExit<TState>(TState st, Action system, ThreadingMode? threadingType = null)
		where TState : struct, Enum
	{
		if (!threadingType.HasValue)
			threadingType = ThreadingExecutionMode;

		var stateChangeId = -1;

		var sys = new FuncSystem<World>(_world, (ticks, args, runIf) =>
		{
			if (runIf?.Invoke(ticks, args) ?? true)
			{
				system();
				return true;
			}
			return false;
		}, () => false, Stages.OnExit, threadingType.Value)
		.RunIf((State<TState> state) => state.ShouldExit(st, ref stateChangeId));

		Add(sys, Stages.OnExit);

		return sys;
	}

	public Scheduler AddPlugin<T>() where T : IPlugin, new()
		=> AddPlugin(new T());

	public Scheduler AddPlugin<T>(T plugin) where T : IPlugin
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
}

internal struct Placeholder<T> where T : ISystemParam<World> { public T Value; }


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
	public World() : this(256) { }

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
		if (arg.Entity<Placeholder<Res<T>>>().Has<Placeholder<Res<T>>>())
			return arg.Entity<Placeholder<Res<T>>>().Get<Placeholder<Res<T>>>().Value;

		var res = new Res<T>();
		arg.Entity<Placeholder<Res<T>>>().Set(new Placeholder<Res<T>>() { Value = res });
		return res;
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
	public ref D Current => ref _dataIterator;

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
public class TinySystemAttribute(Stages stage = Stages.Update, ThreadingMode threadingMode = ThreadingMode.Auto) : Attribute
{
	public Stages Stage { get; } = stage;
	public ThreadingMode ThreadingMode { get; } = threadingMode;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class RunIf(string systemName) : Attribute
{
	public string SystemName { get; } = systemName;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class BeforeOf(string systemName) : Attribute
{
	public string SystemName { get; } = systemName;
}

[AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
public sealed class AfterOf(string systemName) : Attribute
{
	public string SystemName { get; } = systemName;
}


[AttributeUsage(System.AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class TinyPluginAttribute : Attribute
{
}

#endif

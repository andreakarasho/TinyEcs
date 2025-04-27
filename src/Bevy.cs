using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

// https://promethia-27.github.io/dependency_injection_like_bevy_from_scratch/introductions.html

#if NET

public sealed partial class FuncSystem<TArg> where TArg : notnull
{
	private readonly TArg _arg;
	private readonly Func<TArg, Func<TArg, bool>, bool> _fn;
	private readonly List<Func<TArg, bool>> _conditions;
	private readonly Func<TArg, bool> _validator;
	private readonly Func<bool> _checkInUse;
	private readonly Stages _stage;
	private readonly ThreadingMode _threadingType;
	private readonly LinkedList<FuncSystem<TArg>> _after = new();
	private readonly LinkedList<FuncSystem<TArg>> _before = new();
	internal LinkedListNode<FuncSystem<TArg>>? Node { get; set; }


	internal FuncSystem(TArg arg, Func<TArg, Func<TArg, bool>, bool> fn, Func<bool> checkInUse, Stages stage, ThreadingMode threadingType)
	{
		_arg = arg;
		_fn = fn;
		_conditions = new();
		_validator = ValidateConditions;
		_checkInUse = checkInUse;
		_threadingType = threadingType;
		_stage = stage;
	}

	internal void Run()
	{
		foreach (var s in _before)
			s.Run();

		if (_fn(_arg, _validator))
		{
			foreach (var s in _after)
				s.Run();
		}
	}

	public FuncSystem<TArg> RunIf(Func<bool> condition)
	{
		_conditions.Add((_) => condition());
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

	private bool ValidateConditions(TArg args)
	{
		foreach (var fn in _conditions)
			if (!fn(args))
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

public partial class Scheduler
{
	private readonly World _world;
	private readonly LinkedList<FuncSystem<World>>[] _systems = new LinkedList<FuncSystem<World>>[(int)Stages.OnExit + 1];
	private readonly List<FuncSystem<World>> _singleThreads = new();
	private readonly List<FuncSystem<World>> _multiThreads = new();
	private readonly Dictionary<Type, IEventParam> _events = new();

	public Scheduler(World world)
	{
		_world = world;

		for (var i = 0; i < _systems.Length; ++i)
			_systems[i] = new LinkedList<FuncSystem<World>>();

		AddSystemParam(world);
		AddSystemParam(new SchedulerState(this));
		AddSystemParam(new Commands(world));
	}

	public World World => _world;

	public void Run(Func<bool> checkForExitFn, Action? cleanupFn = null)
	{
		while (!checkForExitFn())
			RunOnce();

		cleanupFn?.Invoke();
	}

	public void RunOnce()
	{
		foreach ((_, var ev) in _events)
			ev.Clear();

		RunStage(Stages.Startup);
		_systems[(int)Stages.Startup].Clear();

		RunStage(Stages.OnExit);
		RunStage(Stages.OnEnter);

		for (var stage = Stages.FrameStart; stage <= Stages.FrameEnd; stage += 1)
			RunStage(stage);
	}

	private void RunStage(Stages stage)
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
			Parallel.ForEach(multithreading, static s => s.Run());

		foreach (var system in singlethreading)
			system.Run();
	}

	internal void Add(FuncSystem<World> sys, Stages stage)
	{
		sys.Node = _systems[(int)stage].AddLast(sys);
	}

	public FuncSystem<World> AddSystem(Action system, Stages stage = Stages.Update, ThreadingMode threadingType = ThreadingMode.Auto)
	{
		var sys = new FuncSystem<World>(_world, (args, runIf) =>
		{
			if (runIf?.Invoke(args) ?? true)
			{
				system();
				return true;
			}
			return false;
		}, () => false, stage, threadingType);
		Add(sys, stage);

		return sys;
	}

	public FuncSystem<World> OnEnter<TState>(TState st, Action system, ThreadingMode threadingType = ThreadingMode.Auto)
		where TState : struct, Enum
	{
		var stateChangeId = -1;

		var sys = new FuncSystem<World>(_world, (args, runIf) =>
		{
			if (runIf?.Invoke(args) ?? true)
			{
				system();
				return true;
			}
			return false;
		}, () => false, Stages.OnEnter, threadingType)
		.RunIf((State<TState> state) => state.ShouldEnter(st, ref stateChangeId));

		Add(sys, Stages.OnEnter);

		return sys;
	}

	public FuncSystem<World> OnExit<TState>(TState st, Action system, ThreadingMode threadingType = ThreadingMode.Auto)
		where TState : struct, Enum
	{
		var stateChangeId = -1;

		var sys = new FuncSystem<World>(_world, (args, runIf) =>
		{
			if (runIf?.Invoke(args) ?? true)
			{
				system();
				return true;
			}
			return false;
		}, () => false, Stages.OnExit, threadingType)
		.RunIf((State<TState> state) => state.ShouldExit(st, ref stateChangeId));

		Add(sys, Stages.OnExit);

		return sys;
	}

	public Scheduler AddPlugin<T>() where T : notnull, IPlugin, new()
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

	public Scheduler AddSystemParam<T>(T param) where T : notnull, ISystemParam<World>
	{
		_world.Entity<Placeholder<T>>().Set(new Placeholder<T>() { Value = param });

		return this;
	}

	internal bool ResourceExists<T>() where T : notnull, ISystemParam<World>
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
}

public interface ISystemParam
{
	internal ref int UseIndex { get; }

	void Lock() => Interlocked.Increment(ref UseIndex);
	void Unlock() => Interlocked.Decrement(ref UseIndex);
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
	private readonly List<T> _events = new();

	internal EventParam()
	{
		Writer = new EventWriter<T>(_events);
		Reader = new EventReader<T>(_events);
	}

	public EventWriter<T> Writer { get; }
	public EventReader<T> Reader { get; }


	public void Clear() => _events.Clear();

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

	public EventReaderIterator GetEnumerator()
		=> new(_events);

	public static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<EventParam<T>>>().Has<Placeholder<EventParam<T>>>())

			return arg.Entity<Placeholder<EventParam<T>>>().Get<Placeholder<EventParam<T>>>().Value.Reader;

		throw new NotImplementedException("EventReader<T> must be created using the scheduler.AddEvent<T>() method");
	}

	public ref struct EventReaderIterator
	{
		private readonly List<T> _events;
		private int _index;

		internal EventReaderIterator(List<T> events)
		{
			_events = events;
			_index = events.Count;
		}

		public readonly T Current => _events[_index];

		public bool MoveNext() => --_index >= 0;
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

	public TQueryData GetEnumerator() => TQueryData.CreateIterator(_query.Iter());

	public TQueryData Get(EcsID id)
	{
		var enumerator = TQueryData.CreateIterator(_query.Iter(id));
		var success = enumerator.MoveNext();
		return success ? enumerator : default;
	}

	public bool Contains(EcsID id)
	{
		var enumerator = TQueryData.CreateIterator(_query.Iter(id));
		return enumerator.MoveNext();
	}

	public TQueryData Single()
	{
		EcsAssert.Panic(_query.Count() == 1, "'Single' must match one and only one entity.");
		var enumerator = GetEnumerator();
		var ok = enumerator.MoveNext();
		EcsAssert.Panic(ok, "'Single' is not matching any entity.");
		return enumerator;
	}

	public int Count()
		=> _query.Count();
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
		var enumerator = TQueryData.CreateIterator(_query.Iter());
		var ok = enumerator.MoveNext();
		EcsAssert.Panic(ok, "'Single' is not matching any entity.");
		return enumerator;
	}

	public bool TryGet(out TQueryData data)
	{
		if (_query.Count() == 1)
		{
			var enumerator = TQueryData.CreateIterator(_query.Iter());
			var ok = enumerator.MoveNext();
			if (ok)
			{
				data = enumerator;
				return true;
			}
		}

		data = default;
		return false;
	}

	public int Count()
		=> _query.Count();
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

public sealed class Res<T> : SystemParam<World>, IIntoSystemParam<World> where T : notnull
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

public sealed class Local<T> : SystemParam<World>, IIntoSystemParam<World> where T : notnull
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

	public EntityCommand Entity(EcsID id)
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

public interface IFilter<TFilter> : ITermCreator
	where TFilter : struct, allows ref struct
{
	internal static abstract bool Apply(ref readonly QueryIterator iterator, int row);
}


public interface INestedFilter
{
	void BuildAsParam(QueryBuilder builder);
}

public static class FilterBuilder<T> where T : struct
{
	public static bool Build(QueryBuilder builder)
	{
		if (default(T) is INestedFilter nestedFilter)
		{
			nestedFilter.BuildAsParam(builder);
			return true;
		}

		return false;
	}
}

public ref struct Empty : IData<Empty>, IFilter<Empty>
{
	private QueryIterator _iterator;

	internal Empty(QueryIterator iterator) => _iterator = iterator;

	public static void Build(QueryBuilder builder)
	{

	}

	public static Empty CreateIterator(QueryIterator iterator)
	{
		return new Empty(iterator);
	}

	[UnscopedRef] public ref Empty Current => ref this;

	public readonly void Deconstruct(out ReadOnlySpan<EntityView> entities, out int count)
	{
		entities = _iterator.Entities();
		count = entities.Length;
	}

	public readonly Empty GetEnumerator() => this;

	public bool MoveNext() => _iterator.Next();


	static bool IFilter<Empty>.Apply(ref readonly QueryIterator iterator, int row)
	{
		return true;
	}
}

/// <summary>
/// Used in query filters to find entities with the corrisponding component/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly ref struct With<T> : IFilter<With<T>>, INestedFilter
	where T : struct
{
	static bool IFilter<With<T>>.Apply(ref readonly QueryIterator iterator, int row)
	{
		return true;
	}

	public static void Build(QueryBuilder builder)
	{
		if (!FilterBuilder<T>.Build(builder))
			builder.With<T>();
	}

	public void BuildAsParam(QueryBuilder builder)
	{
		Build(builder);
	}
}

/// <summary>
/// Used in query filters to find entities without the corrisponding component/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly ref struct Without<T> : IFilter<Without<T>>, INestedFilter
	where T : struct
{
	static bool IFilter<Without<T>>.Apply(ref readonly QueryIterator iterator, int row)
	{
		return true;
	}

	public static void Build(QueryBuilder builder)
	{
		if (!FilterBuilder<T>.Build(builder))
			builder.Without<T>();
	}

	public void BuildAsParam(QueryBuilder builder)
	{
		Build(builder);
	}
}

/// <summary>
/// Used in query filters to find entities with or without the corrisponding component/tag.<br/>
/// You would Unsafe.IsNullRef&lt;T&gt;(); to check if the value has been found.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly ref struct Optional<T> : IFilter<Optional<T>>, INestedFilter
	where T : struct
{
	static bool IFilter<Optional<T>>.Apply(ref readonly QueryIterator iterator, int row)
	{
		return true;
	}

	public static void Build(QueryBuilder builder)
	{
		builder.Optional<T>();
	}

	public void BuildAsParam(QueryBuilder builder)
	{
		Build(builder);
	}
}

/// <summary>
/// Used in query filters to find entities with components that have changed.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly ref struct Changed<T> : IFilter<Changed<T>>, INestedFilter
	where T : struct
{
	public static void Build(QueryBuilder builder)
	{
		if (!FilterBuilder<T>.Build(builder))
			builder.Changed<T>();
	}

	public void BuildAsParam(QueryBuilder builder)
	{
		Build(builder);
	}

	static bool IFilter<Changed<T>>.Apply(ref readonly QueryIterator iterator, int row)
	{
		var columns = iterator.Columns;
		for (var i = 0; i < columns; i++)
		{
			var changes = iterator.Changes(i);
			if (changes.Count > 0 && changes[row])
				return true;
		}

		return false;
	}
}

public partial struct Parent { }
public partial interface IChildrenComponent { }


[SkipLocalsInit]
public ref struct QueryIter<D, F>
	where D : struct, IData2<D>, IRow, allows ref struct
	where F : struct, IFilter<F>, allows ref struct
{
	private D _dataIterator;

	public QueryIter(QueryIterator iterator)
	{
		_dataIterator = D.CreateIterator(iterator);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool MoveNext()
	{
		while (true)
		{
			if (!_dataIterator.MoveNext())
				return false;

			if (!F.Apply(in _dataIterator.Iterator, _dataIterator.Row))
				continue;

			return true;
		}
	}

	[UnscopedRef]
	public ref D Current => ref _dataIterator;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly QueryIter<D, F> GetEnumerator()
	{
		return this;
	}
}

public interface IData2<TData> : ITermCreator, IQueryIterator<TData>
	where TData : struct, allows ref struct
{
	public static abstract TData CreateIterator(QueryIterator iterator);
}

public interface IRow
{
	[UnscopedRef]
	internal ref readonly QueryIterator Iterator { get; }
	int Row { get; }
}

[SkipLocalsInit]
public unsafe ref struct Data2<T0> : IData2<Data2<T0>>, IRow
	where T0 : struct
{
	private QueryIterator _iterator;
	private int _index, _count;
	private ReadOnlySpan<EntityView> _entities;
	private DataRow<T0> _current0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Data2(QueryIterator queryIterator)
	{
		_iterator = queryIterator;
		_index = -1;
		_count = -1;
	}

	public readonly int Row => _index;

	[UnscopedRef]
	public ref readonly QueryIterator Iterator => ref _iterator;

	public static void Build(QueryBuilder builder)
	{
		if (!FilterBuilder<T0>.Build(builder)) builder.With<T0>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Data2<T0> CreateIterator(QueryIterator iterator)
		=> new Data2<T0>(iterator);

	[System.Diagnostics.CodeAnalysis.UnscopedRef]
	public ref Data2<T0> Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Deconstruct(out Ptr<T0> ptr0)
	{
		ptr0 = _current0.Value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Deconstruct(out PtrRO<EntityView> entity, out Ptr<T0> ptr0)
	{
		entity = new(in _entities[_index]);
		ptr0 = _current0.Value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool MoveNext()
	{
		if (++_index >= _count)
		{
			if (!_iterator.Next())
				return false;

			_current0 = _iterator.GetColumn<T0>(0);
			_entities = _iterator.Entities();

			_index = 0;
			_count = _iterator.Count;
		}
		else
		{
			_current0.Next();
		}

		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Data2<T0> GetEnumerator() => this;
}

#endif

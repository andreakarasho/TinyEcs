namespace TinyEcs;

// https://promethia-27.github.io/dependency_injection_like_bevy_from_scratch/introductions.html

#if NET

public sealed partial class FuncSystem<TArg> where TArg : notnull
{
	private readonly TArg _arg;
    private readonly Action<TArg, Func<TArg, bool>> _fn;
	private readonly List<Func<TArg, bool>> _conditions;
	private readonly Func<TArg, bool> _validator;
	private readonly Func<bool> _checkInUse;
	private readonly ThreadingMode _threadingType;
	private readonly LinkedList<FuncSystem<TArg>> _after = new ();
	private readonly LinkedList<FuncSystem<TArg>> _before = new ();
	internal LinkedListNode<FuncSystem<TArg>>? Node { get; set; }


    internal FuncSystem(TArg arg, Action<TArg,Func<TArg, bool>> fn, Func<bool> checkInUse, ThreadingMode threadingType)
    {
		_arg = arg;
        _fn = fn;
		_conditions = new ();
		_validator = ValidateConditions;
		_checkInUse = checkInUse;
		_threadingType = threadingType;
    }

	internal void Run()
	{
		foreach (var s in _before)
			s.Run();

		_fn(_arg, _validator);

		foreach (var s in _after)
			s.Run();
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

	public FuncSystem<TArg> RunBefore(FuncSystem<TArg> parent)
	{
		if (this == parent || Contains(parent, s => s._before))
			throw new InvalidOperationException("Circular dependency detected");

		Node?.List?.Remove(Node);
		Node = parent._before.AddLast(this);

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
	FrameEnd
}

public enum ThreadingMode
{
	Auto,
	Single,
	Multi
}

public sealed partial class Scheduler
{
	private readonly World _world;
	private readonly LinkedList<FuncSystem<World>>[] _systems = new LinkedList<FuncSystem<World>>[(int)Stages.FrameEnd + 1];
	private readonly List<FuncSystem<World>> _singleThreads = new ();
	private readonly List<FuncSystem<World>> _multiThreads = new ();

	public Scheduler(World world)
	{
		_world = world;

		for (var i = 0; i < _systems.Length; ++i)
			_systems[i] = new ();

		AddSystemParam(world);
		AddSystemParam(new SchedulerState(this));
	}

    public void Run()
    {
		RunStage(Stages.Startup);
		_systems[(int) Stages.Startup].Clear();

		for (var stage = Stages.FrameStart; stage <= Stages.FrameEnd; stage += 1)
        	RunStage(stage);
    }

	private void RunStage(Stages stage)
	{
		_singleThreads.Clear();
		_multiThreads.Clear();

		var systems = _systems[(int) stage];

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

		if (multithreading.Any())
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
		var sys = new FuncSystem<World>(_world, (args, runIf) => { if (runIf?.Invoke(args) ?? true) system(); }, () => false, threadingType);
		Add(sys, stage);

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
		var queue = new Queue<T>();
		return AddSystemParam(new EventWriter<T>(queue))
			.AddSystemParam(new EventReader<T>(queue));
	}

	public Scheduler AddState<T>(T initialState = default!) where T : notnull, Enum
	{
		return AddResource(initialState);
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
}

internal struct Placeholder<T> where T : ISystemParam<World> { public T Value; }


public interface IPlugin
{
	void Build(Scheduler scheduler);
}

public abstract class SystemParam : ISystemParam<World>
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

public sealed class EventWriter<T> : SystemParam, IIntoSystemParam<World> where T : notnull
{
	private readonly Queue<T>? _queue;

	internal EventWriter(Queue<T> queue)
		=> _queue = queue;

	public EventWriter()
		=> throw new Exception("EventWriter must be initialized using the 'scheduler.AddEvent<T>' api");

	public bool IsEmpty
		=> _queue!.Count == 0;

	public void Clear()
		=> _queue?.Clear();

	public void Enqueue(T ev)
		=> _queue!.Enqueue(ev);

	public static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<EventWriter<T>>>().Has<Placeholder<EventWriter<T>>>())
			return arg.Entity<Placeholder<EventWriter<T>>>().Get<Placeholder<EventWriter<T>>>().Value;

		var writer = new EventWriter<T>();
		arg.Entity<Placeholder<EventWriter<T>>>().Set(new Placeholder<EventWriter<T>>() { Value = writer });
		return writer;
	}
}

public sealed class EventReader<T> : SystemParam, IIntoSystemParam<World> where T : notnull
{
	private readonly Queue<T>? _queue;

	internal EventReader(Queue<T> queue)
		=> _queue = queue;

	public EventReader()
		=> throw new Exception("EventReader must be initialized using the 'scheduler.AddEvent<T>' api");

	public bool IsEmpty
		=> _queue!.Count == 0;

	public void Clear()
		=> _queue?.Clear();

	public EventReaderIterator GetEnumerator() => new (_queue!);

	public static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<EventReader<T>>>().Has<Placeholder<EventReader<T>>>())
			return arg.Entity<Placeholder<EventReader<T>>>().Get<Placeholder<EventReader<T>>>().Value;

		var reader = new EventReader<T>();
		arg.Entity<Placeholder<EventReader<T>>>().Set(new Placeholder<EventReader<T>>() { Value = reader });
		return reader;
	}

	public ref struct EventReaderIterator
	{
		private readonly Queue<T> _queue;
		private T _data;

		internal EventReaderIterator(Queue<T> queue)
		{
			_queue = queue;
			_data = default!;
		}

		public readonly T Current => _data;

		public bool MoveNext() => _queue.TryDequeue(out _data);
	}
}

partial class World : SystemParam, IIntoSystemParam<World>
{
	public World() : this(256) { }

	public static ISystemParam<World> Generate(World arg)
	{
		return arg;
	}
}

public class Query<TQueryData> : Query<TQueryData, Empty>, IIntoSystemParam<World>
	where TQueryData : IData
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

public partial class Query<TQueryData, TQueryFilter> : SystemParam, IIntoSystemParam<World>
	where TQueryData : IData
	where TQueryFilter : IFilter
{
	private readonly Query _query;

	internal Query(Query query)
	{
		_query = query;
	}

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

	public QueryInternal GetEnumerator() => _query.GetEnumerator();
}

public sealed class Res<T> : SystemParam, IIntoSystemParam<World> where T : notnull
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator T?(Res<T> reference)
		=> reference.Value;
}

public sealed class Local<T> : SystemParam, IIntoSystemParam<World> where T : notnull
{
	private T? _t;

    public ref T? Value => ref _t;

	public static ISystemParam<World> Generate(World arg)
	{
		return new Local<T>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator T?(Local<T> reference)
		=> reference.Value;
}

public sealed class SchedulerState : SystemParam, IIntoSystemParam<World>
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

	public static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<SchedulerState>>().Has<Placeholder<SchedulerState>>())
			return arg.Entity<Placeholder<SchedulerState>>().Get<Placeholder<SchedulerState>>().Value;
		throw new NotImplementedException();
	}
}



public interface ITermCreator
{
	public static abstract void Build(QueryBuilder builder);
}

public interface IComponent { }
public interface IData : ITermCreator { }
public interface IFilter : ITermCreator { }

public interface INestedFilter
{
	void BuildAsParam(ref QueryBuilder builder);
}

internal static class FilterBuilder<T> where T : struct
{
	public static bool Build(QueryBuilder builder)
	{
		if (default(T) is INestedFilter nestedFilter)
		{
			nestedFilter.BuildAsParam(ref builder);
			return true;
		}

		return false;
	}
}


public readonly struct Empty : IFilter, IData, IComponent
{
	public static void Build(QueryBuilder builder)
	{
	}
}

/// <summary>
/// Used in query filters to find entities with the corrisponding component/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct With<T> : IFilter, INestedFilter
	where T : struct, IComponent
{
	public static void Build(QueryBuilder builder)
	{
		builder.With<T>();
	}

	public void BuildAsParam(ref QueryBuilder builder)
	{
		Build(builder);
	}
}

/// <summary>
/// Used in query filters to find entities without the corrisponding component/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct Without<T> : IFilter, INestedFilter
	where T : struct, IComponent
{
	public static void Build(QueryBuilder builder)
	{
		// if (!FilterBuilder<T>.Build(builder))
			builder.Without<T>();
	}

	public void BuildAsParam(ref QueryBuilder builder)
	{
		Build(builder);
	}
}

/// <summary>
/// Used in query filters to find entities with or without the corrisponding component/tag.<br/>
/// You would Unsafe.IsNullRef&lt;T&gt;(); to check if the value has been found.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct Optional<T> : IData
	where T : struct, IComponent
{
	public static void Build(QueryBuilder builder)
	{
		builder.Optional<T>();
	}
}

/// <summary>
/// Used in query filters to find entities with any of the corrisponding components/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct AtLeast<T> : ITuple, IFilter where T : struct, ITuple
{
	static readonly ITuple _value = default(T)!;

	public object? this[int index] => _value[index];

	public int Length => _value.Length;
	public static void Build(QueryBuilder builder)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Used in query filters to find entities with exactly the corrisponding components/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct Exactly<T> : ITuple, IFilter where T : struct, ITuple
{
	static readonly ITuple _value = default(T)!;

	public object? this[int index] => _value[index];

	public int Length => _value.Length;
	public static void Build(QueryBuilder builder)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Used in query filters to find entities with none the corrisponding components/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct None<T> : ITuple, IFilter where T : struct, ITuple
{
	static readonly ITuple _value = default(T)!;

	public object? this[int index] => _value[index];

	public int Length => _value.Length;
	public static void Build(QueryBuilder builder)
	{
		throw new NotImplementedException();
	}
}

/// <summary>
/// Used in query filters to accomplish the 'or' logic.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct Or<T> : IFilter where T : struct, ITuple
{
	public static void Build(QueryBuilder builder)
	{
		throw new NotImplementedException();
	}
}

#endif
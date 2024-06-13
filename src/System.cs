namespace TinyEcs;

// https://promethia-27.github.io/dependency_injection_like_bevy_from_scratch/introductions.html

using SysParamMap = Dictionary<Type, ISystemParam>;

public sealed partial class FuncSystem<TArg> where TArg : notnull
{
	private readonly TArg _arg;
    private readonly Action<TArg, SysParamMap, SysParamMap, Func<SysParamMap, TArg, bool>> _fn;
	private readonly SysParamMap _locals;
	private readonly List<Func<SysParamMap, SysParamMap, TArg, bool>> _conditions;
	private readonly Func<SysParamMap, TArg, bool> _validator;
	private readonly Func<bool> _checkInUse;
	private readonly ThreadingType _threadingType;

    internal FuncSystem(TArg arg, Action<TArg, SysParamMap, SysParamMap, Func<SysParamMap, TArg, bool>> fn, Func<bool> checkInUse, ThreadingType threadingType)
    {
		_arg = arg;
        _fn = fn;
		_locals = new ();
		_conditions = new ();
		_validator = ValidateConditions;
		_checkInUse = checkInUse;
		_threadingType = threadingType;
    }

	internal void Run(SysParamMap resources)
		=> _fn(_arg, resources, _locals, _validator);

	public FuncSystem<TArg> RunIf(Func<bool> condition)
	{
		_conditions.Add((_, _, _) => condition());
		return this;
	}

	internal bool IsResourceInUse()
	{
		if (_threadingType == ThreadingType.Multi)
			return false;

		return _checkInUse();
	}

	private bool ValidateConditions(SysParamMap resources, TArg args)
	{
		foreach (var fn in _conditions)
			if (!fn(resources, _locals, args))
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

public enum ThreadingType
{
	Auto,
	Single,
	Multi
}

public sealed partial class Scheduler
{
	private readonly World _world;
    private readonly List<FuncSystem<World>>[] _systems = new List<FuncSystem<World>>[(int)Stages.FrameEnd + 1];
    private readonly SysParamMap _resources = new ();

	public Scheduler(World world)
	{
		_world = world;

		for (var i = 0; i < _systems.Length; ++i)
			_systems[i] = new ();

		AddSystemParam(world);
		AddSystemParam(new SchedulerState(this));
	}


	// public bool IsState<TState>(TState state) where TState : Enum =>
	// 	ISystemParam.Get<Res<TState>>(_resources, _resources, null!)?.Value?.Equals(state) ?? false;

    public void Run()
    {
		RunStage(Stages.Startup);
		_systems[(int) Stages.Startup].Clear();

		for (var stage = Stages.FrameStart; stage <= Stages.FrameEnd; stage += 1)
        	RunStage(stage);
    }

	private void RunStage(Stages stage)
	{
		var systems = _systems[(int) stage];
		var multithreading = systems.Where(s => !s.IsResourceInUse());
		var singlethreading = systems.Except(multithreading);

		if (multithreading.Any())
		{

		}

		Parallel.ForEach(multithreading, s => s.Run(_resources));

		foreach (var system in singlethreading)
			system.Run(_resources);
	}

	public FuncSystem<World> AddSystem(Action system, Stages stage = Stages.Update, ThreadingType threadingType = ThreadingType.Auto)
	{
		var sys = new FuncSystem<World>(_world, (args, globalRes, _, runIf) => { if (runIf?.Invoke(globalRes, args) ?? true) system(); }, () => false, threadingType);
		_systems[(int)stage].Add(sys);

		return sys;
	}

	public Scheduler AddPlugin<T>() where T : IPlugin, new()
		=> AddPlugin(new T());

	public Scheduler AddPlugin<T>(T plugin) where T : IPlugin
	{
		plugin.Build(this);

		return this;
	}

	public Scheduler AddEvent<T>()
	{
		var queue = new Queue<T>();
		return AddSystemParam(new EventWriter<T>(queue))
			.AddSystemParam(new EventReader<T>(queue));
	}

	public Scheduler AddState<T>(T initialState = default!) where T : Enum
	{
		return AddResource(initialState);
	}

    public Scheduler AddResource<T>(T resource)
    {
		return AddSystemParam(new Res<T>() { Value = resource });
    }

	internal Scheduler AddSystemParam<T>(T param) where T : ISystemParam
	{
		_resources[typeof(T)] = param;

		return this;
	}

	internal bool ResourceExists<T>() where T : ISystemParam
	{
		return _resources.ContainsKey(typeof(T));
	}
}

public interface IPlugin
{
	void Build(Scheduler scheduler);
}

public interface ISystemParam
{
	internal ref int UseIndex { get; }
	void New(object arguments);

	void Lock() => Interlocked.Increment(ref UseIndex);
	void Unlock() => Interlocked.Decrement(ref UseIndex);

	internal static T Get<T>(SysParamMap globalRes, SysParamMap localRes, object arguments)
		where T : ISystemParam, new()
	{
		if (localRes.TryGetValue(typeof(T), out var value))
		{
			return (T)value;
		}

		if (!globalRes.TryGetValue(typeof(T), out value))
		{
			value = new T();
			value.New(arguments);

			if (value is ISystemParamExclusive exclusive)
				localRes.Add(typeof(T), value);
			else
				globalRes.Add(typeof(T), value);
		}

		return (T)value;
	}
}

public interface ISystemParamExclusive
{
}

public sealed class EventWriter<T> : ISystemParam
{
	private readonly Queue<T>? _queue;

	internal EventWriter(Queue<T> queue)
		=> _queue = queue;

	public EventWriter()
		=> throw new Exception("EventWriter must be initialized using the 'scheduler.AddEvent<T>' api");

	public bool IsEmpty => _queue!.Count == 0;

	public void Enqueue(T ev)
		=> _queue!.Enqueue(ev);


	public void New(object arguments) { }


	private int _useIndex;
	ref int ISystemParam.UseIndex => ref _useIndex;
	void ISystemParam.New(object arguments)
	{
		throw new NotImplementedException();
	}
}

public sealed class EventReader<T> : ISystemParam
{
	private readonly Queue<T>? _queue;

	internal EventReader(Queue<T> queue)
		=> _queue = queue;

	public EventReader()
		=> throw new Exception("EventReader must be initialized using the 'scheduler.AddEvent<T>' api");

	public bool IsEmpty => _queue!.Count == 0;

	public IEnumerable<T> Read()
	{
		while (_queue!.TryDequeue(out var result))
			yield return result;
	}

	private int _useIndex;
	ref int ISystemParam.UseIndex => ref _useIndex;
	public void New(object arguments) { }
}


partial class World : ISystemParam
{
	public World() : this(256) { }

	private int _useIndex;
	ref int ISystemParam.UseIndex => ref _useIndex;
	void ISystemParam.New(object arguments) { }
}

partial class Query<TQueryData> : ISystemParam
{
	public Query() : this (null!) { }

	private int _useIndex;
	ref int ISystemParam.UseIndex => ref _useIndex;
	void ISystemParam.New(object arguments)
	{
		World = (World) arguments;
	}
}

partial class Query<TQueryData, TQueryFilter> : ISystemParam
{
	public Query() : this (null!) { }

	private int _useIndex;
	ref int ISystemParam.UseIndex => ref _useIndex;
	void ISystemParam.New(object arguments)
	{
		World = (World) arguments;
	}
}

public sealed class Res<T> : ISystemParam
{
	private T? _t;

    public ref T? Value => ref _t;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator T?(Res<T> reference)
	{
		return reference.Value;
	}

	private int _useIndex;
	ref int ISystemParam.UseIndex => ref _useIndex;
	void ISystemParam.New(object arguments)
	{
		//throw new Exception("Resources must be initialized using 'scheduler.AddResource<T>' api");
	}
}

public sealed class Local<T> : ISystemParam, ISystemParamExclusive
{
	private T? _t;

    public ref T? Value => ref _t;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator T?(Local<T> reference)
	{
		return reference.Value;
	}

	private int _useIndex;
	ref int ISystemParam.UseIndex => ref _useIndex;
	void ISystemParam.New(object arguments)
	{
		//throw new Exception("Resources must be initialized using 'scheduler.AddResource<T>' api");
	}
}

public sealed class SchedulerState : ISystemParam
{
	private readonly Scheduler _scheduler;

	internal SchedulerState(Scheduler scheduler)
	{
		_scheduler = scheduler;
	}

	public SchedulerState()
		=> throw new Exception("You are not allowed to initialixze this object by yourself!");

	public void AddResource<T>(T resource)
	{
		_scheduler.AddResource(resource);
	}

	public bool ResourceExists<T>()
	{
		return _scheduler.ResourceExists<Res<T>>();
	}

	private int _useIndex;
	ref int ISystemParam.UseIndex => ref _useIndex;
	void ISystemParam.New(object arguments)
	{

	}
}

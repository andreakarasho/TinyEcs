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
	private readonly ThreadingMode _threadingType;
	private readonly LinkedList<FuncSystem<TArg>> _after = new ();
	private readonly LinkedList<FuncSystem<TArg>> _before = new ();
	internal LinkedListNode<FuncSystem<TArg>>? Node { get; set; }


    internal FuncSystem(TArg arg, Action<TArg, SysParamMap, SysParamMap, Func<SysParamMap, TArg, bool>> fn, Func<bool> checkInUse, ThreadingMode threadingType)
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
	{
		foreach (var s in _before)
			s.Run(resources);

		_fn(_arg, resources, _locals, _validator);

		foreach (var s in _after)
			s.Run(resources);
	}

	public FuncSystem<TArg> RunIf(Func<bool> condition)
	{
		_conditions.Add((_, _, _) => condition());
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

public enum ThreadingMode
{
	Auto,
	Single,
	Multi
}

public sealed partial class Scheduler
{
	private readonly World _world;
    private readonly SysParamMap _resources = new ();
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

		// var multithreading = systems.Where(static s => !s.IsResourceInUse());
		// var singlethreading = systems.Except(multithreading);

		if (multithreading.Any())
			Parallel.ForEach(multithreading, s => s.Run(_resources));

		foreach (var system in singlethreading)
			system.Run(_resources);
	}

	internal void Add(FuncSystem<World> sys, Stages stage)
	{
		sys.Node = _systems[(int)stage].AddLast(sys);
	}

	public FuncSystem<World> AddSystem(Action system, Stages stage = Stages.Update, ThreadingMode threadingType = ThreadingMode.Auto)
	{
		var sys = new FuncSystem<World>(_world, (args, globalRes, _, runIf) => { if (runIf?.Invoke(globalRes, args) ?? true) system(); }, () => false, threadingType);
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

	public Scheduler AddSystemParam<T>(T param) where T : notnull, ISystemParam
	{
		_resources[typeof(T)] = param;

		return this;
	}

	internal bool ResourceExists<T>() where T : notnull, ISystemParam
	{
		return _resources.ContainsKey(typeof(T));
	}
}

public interface IPlugin
{
	void Build(Scheduler scheduler);
}

public abstract class SystemParam : ISystemParam
{
	public virtual void New(object arguments)
	{
		throw new Exception("A 'SystemParam' must be initialized using the 'scheduler.AddSystemParam<T>' api");
	}

	private int _useIndex;
	ref int ISystemParam.UseIndex => ref _useIndex;
}

public interface ISystemParam
{
	internal ref int UseIndex { get; }
	void New(object arguments);

	void Lock() => Interlocked.Increment(ref UseIndex);
	void Unlock() => Interlocked.Decrement(ref UseIndex);

	internal static T Get<T>(SysParamMap globalRes, SysParamMap localRes, object arguments)
		where T : notnull, ISystemParam, new()
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

public sealed class EventWriter<T> : SystemParam where T : notnull
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
}

public sealed class EventReader<T> : SystemParam where T : notnull
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

partial class World : SystemParam
{
	public World() : this(256) { }

	public override void New(object arguments)
	{
	}
}

partial class Query<TQueryData> : ISystemParam
{
	public Query() : this(null!) { }

	private int _useIndex;
	ref int ISystemParam.UseIndex => ref _useIndex;
	void ISystemParam.New(object arguments)
	{
		World = (World) arguments;
	}
}

partial class Query<TQueryData, TQueryFilter> : ISystemParam
{
	public Query() : this(null!) { }

	private int _useIndex;
	ref int ISystemParam.UseIndex => ref _useIndex;
	void ISystemParam.New(object arguments)
	{
		World = (World) arguments;
	}
}

public sealed class Res<T> : SystemParam where T : notnull
{
	private T? _t;

    public ref T? Value => ref _t;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator T?(Res<T> reference)
		=> reference.Value;

	public override void New(object arguments)
	{
	}
}

public sealed class Local<T> : SystemParam, ISystemParamExclusive where T : notnull
{
	private T? _t;

    public ref T? Value => ref _t;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator T?(Local<T> reference)
		=> reference.Value;

	public override void New(object arguments)
	{
	}
}

public sealed class SchedulerState : SystemParam
{
	private readonly Scheduler _scheduler;

	internal SchedulerState(Scheduler scheduler)
	{
		_scheduler = scheduler;
	}

	public SchedulerState()
		=> throw new Exception("You are not allowed to initialize this object by yourself!");

	public void AddResource<T>(T resource) where T : notnull
		=> _scheduler.AddResource(resource);

	public bool ResourceExists<T>() where T : notnull
		=> _scheduler.ResourceExists<Res<T>>();
}

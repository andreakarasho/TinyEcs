namespace TinyEcs;

// https://promethia-27.github.io/dependency_injection_like_bevy_from_scratch/introductions.html

using SysParamMap = Dictionary<Type, ISystemParam>;

public partial interface ISystem
{
    internal void Run(SysParamMap resources);

	public ISystem RunIf(Func<bool> condition);
}

internal sealed partial class ErasedFunctionSystem : ISystem
{
    private readonly Action<SysParamMap, SysParamMap, Func<object, bool>> _fn;
	private readonly SysParamMap _locals;
	private readonly List<Func<SysParamMap, SysParamMap, object, bool>> _conditions;

    public ErasedFunctionSystem(Action<SysParamMap, SysParamMap, Func<object, bool>> fn)
    {
        _fn = fn;
		_locals = new ();
		_conditions = new ();
    }

	void ISystem.Run(SysParamMap resources)
		=> _fn(resources, _locals, args => _conditions.All(s => s(resources, _locals, args)));

	ISystem ISystem.RunIf(Func<bool> condition)
	{
		_conditions.Add((_, _, _) => condition());
		return this;
	}
}

public enum Stages
{
	Startup,
	BeforeUpdate,
	Update,
	AfterUpdate,
}

public sealed partial class Scheduler
{
	private readonly World _world;
    private readonly List<ISystem>[] _systems = new List<ISystem>[(int)Stages.AfterUpdate + 1];
    private readonly SysParamMap _resources = new ();

	public Scheduler(World world)
	{
		_world = world;

		for (var i = 0; i < _systems.Length; ++i)
			_systems[i] = new ();

		AddSystemParam(world);
	}


	// public bool IsState<TState>(TState state) where TState : Enum =>
	// 	ISystemParam.Get<Res<TState>>(_resources, _resources, null!)?.Value?.Equals(state) ?? false;

    public void Run()
    {
		RunStage(Stages.Startup);
		_systems[(int) Stages.Startup].Clear();

		for (var stage = Stages.BeforeUpdate; stage <= Stages.AfterUpdate; stage += 1)
        	RunStage(stage);
    }

	private void RunStage(Stages stage)
	{
		foreach (var system in _systems[(int) stage])
			system.Run(_resources);
	}

	public ISystem AddSystem(Action system, Stages stage = Stages.Update)
	{
		var sys = new ErasedFunctionSystem((_, _, runIf) => { if (runIf?.Invoke(_world) ?? true) system(); });
		_systems[(int)stage].Add(sys);

		return sys;
	}

	public Scheduler AddPlugin<T>() where T : IPlugin, new()
	{
		new T().Build(this);

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
}

public interface IPlugin
{
	void Build(Scheduler scheduler);
}

public interface ISystemParam
{
	void New(object arguments);

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

	public void Enqueue(T ev)
		=> _queue!.Enqueue(ev);

	public void New(object arguments) { }
}

public sealed class EventReader<T> : ISystemParam
{
	private readonly Queue<T>? _queue;

	internal EventReader(Queue<T> queue)
		=> _queue = queue;

	public EventReader()
		=> throw new Exception("EventReader must be initialized using the 'scheduler.AddEvent<T>' api");

	public IEnumerable<T> Read()
	{
		while (_queue!.TryDequeue(out var result))
			yield return result;
	}

	public void New(object arguments) { }
}


partial class World : ISystemParam
{
	public World() : this(256) { }

	void ISystemParam.New(object arguments) { }
}

partial class Query<TQuery> : ISystemParam
{
	public Query() : this (null!) { }

	void ISystemParam.New(object arguments)
	{
		World = (World) arguments;
	}
}

partial class Query<TQuery, TFilter> : ISystemParam
{
	public Query() : this (null!) { }

	void ISystemParam.New(object arguments)
	{
		World = (World) arguments;
	}
}

partial class Commands : ISystemParam
{
	public Commands() : this(null!) { }

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

	void ISystemParam.New(object arguments)
	{
		//throw new Exception("Resources must be initialized using 'scheduler.AddResource<T>' api");
	}
}

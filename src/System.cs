namespace TinyEcs;

// https://promethia-27.github.io/dependency_injection_like_bevy_from_scratch/introductions.html

internal interface ISystem
{
    void Run(Dictionary<Type, ISystemParam> resources);
}

internal sealed class ErasedFunctionSystem : ISystem
{
    private readonly Action<Dictionary<Type, ISystemParam>> f;

    public ErasedFunctionSystem(Action<Dictionary<Type, ISystemParam>> f)
    {
        this.f = f;
    }

    public void Run(Dictionary<Type, ISystemParam> resources) => f(resources);
}

public sealed partial class Scheduler
{
	private readonly World _world;
    private readonly List<ISystem> _systems = new ();
    private readonly Dictionary<Type, ISystemParam> _resources = new ();

	public Scheduler(World world)
	{
		_world = world;

		AddSystemParam(world);
	}

    public void Run()
    {
        foreach (var system in _systems)
        {
            system.Run(_resources);
        }
    }

	public Scheduler AddSystem(Action system)
	{
		_systems.Add(new ErasedFunctionSystem(_ => system()));

		return this;
	}

    public Scheduler AddResource<T>(T resource)
    {
		return AddSystemParam(new Res<T>() { Value = resource });
    }

	public Scheduler AddSystemParam<T>(T param) where T : ISystemParam
	{
		_resources[typeof(T)] = param;

		return this;
	}
}


public interface ISystemParam
{
	void New(object arguments);

	internal static T Get<T>(Dictionary<Type, ISystemParam> resources, object arguments) where T : ISystemParam, new()
	{
		if (!resources.TryGetValue(typeof(T), out var value))
		{
			value = new T();
			value.New(arguments);
			resources.Add(typeof(T), value);
		}

		return (T)value;
	}
}


partial class World : ISystemParam
{
	public World() : this(256) { }

	void ISystemParam.New(object arguments)
	{

	}
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
    public T? Value { get; set; }

	void ISystemParam.New(object arguments)
	{
		throw new Exception("Resources must be initialized by yourself!");
	}
}

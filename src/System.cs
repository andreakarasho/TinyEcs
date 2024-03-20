namespace TinyEcs;

// https://promethia-27.github.io/dependency_injection_like_bevy_from_scratch/introductions.html

public sealed class Res<T> : ISystemParam
{
    public T Value { get; set; }

	void ISystemParam.New(object arguments)
	{
		throw new NotImplementedException();
	}
}

public interface ISystem
{
    void Run(Dictionary<Type, ISystemParam> resources);
}

public sealed class ErasedFunctionSystem : ISystem
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
		_systems.Add(new ErasedFunctionSystem((_) => system()));

		return this;
	}

    public Scheduler AddSystem<T0>(Action<T0> system)
		where T0 : ISystemParam, new()
    {
		var fn = (Dictionary<Type, ISystemParam> res) => {
			var obj0 = SysParam.Get<T0>(res, _world);
			system(obj0);
		};
		_systems.Add(new ErasedFunctionSystem(fn));

		return this;
    }

    public Scheduler AddSystem<T0, T1>(Action<T0, T1> system)
		where T0 : ISystemParam, new()
		where T1 : ISystemParam, new()
    {
		var fn = (Dictionary<Type, ISystemParam> res) => {
			var obj0 = SysParam.Get<T0>(res, _world);
			var obj1 = SysParam.Get<T1>(res, _world);

			system(obj0, obj1);
		};
		_systems.Add(new ErasedFunctionSystem(fn));

		return this;
    }

	public Scheduler AddSystem<T0, T1, T2>(Action<T0, T1, T2> system)
		where T0 : ISystemParam, new()
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
    {
		var fn = (Dictionary<Type, ISystemParam> res) => {
			var obj0 = SysParam.Get<T0>(res, _world);
			var obj1 = SysParam.Get<T1>(res, _world);
			var obj2 = SysParam.Get<T2>(res, _world);

			system(obj0, obj1, obj2);
		};
		_systems.Add(new ErasedFunctionSystem(fn));

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
}


static class SysParam
{
	public static T Get<T>(Dictionary<Type, ISystemParam> resources, object arguments) where T : ISystemParam, new()
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

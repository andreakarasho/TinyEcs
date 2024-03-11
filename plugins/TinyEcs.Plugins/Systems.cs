using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

public abstract class EcsSystem
{
#nullable disable
	[NotNull] internal World Ecs { get; set; }
	[NotNull] public string Name { get; internal set; }
#nullable restore

	public bool IsEnabled { get; private set; } = true;
	internal bool IsDestroyed { get; set; }


	public virtual void OnCreate(World ecs) { }

	public virtual void OnStart(World ecs) { }

	public virtual void OnStop(World ecs) { }

	public virtual void OnBeforeUpdate(World ecs) { }

	public virtual void OnUpdate(World ecs) { }

	public virtual void OnAfterUpdate(World ecs) { }

	public virtual void OnCleanup(World ecs) { }

	public virtual void OnDestroy(World ecs) { }


	public void Enable() => Enable(true);

	public void Disable() => Enable(false);


	private void Enable(bool enable)
	{
		if (IsEnabled == enable)
			return;

		IsEnabled = enable;

		if (enable)
			OnStart(Ecs);
		else
			OnStop(Ecs);
	}
}


public sealed class SystemManager
{
	private readonly World _ecs;
	private readonly List<EcsSystem> _systems, _toCreate, _toDelete;
	private readonly Dictionary<Type, EcsSystem> _hashes;

	public SystemManager(World ecs)
	{
		_ecs = ecs;
		_hashes = new Dictionary<Type, EcsSystem>();
		_systems = new List<EcsSystem>();
		_toCreate = new List<EcsSystem>();
		_toDelete = new List<EcsSystem>();
	}

	public T Add<T>(string name = "") where T : EcsSystem, new()
	{
		if (_hashes.TryGetValue(typeof(T), out var system))
			return (T)system;

		system = new T
		{
			Ecs = _ecs,
			Name = string.IsNullOrWhiteSpace(name) ? typeof(T).ToString() : name
		};

		_hashes.Add(typeof(T), system);
		_systems.Add(system);
		_toCreate.Add(system);

		return (T)system;
	}

	public void Delete<T>() where T : EcsSystem
	{
		var found = Find<T>();
		if (found == null || found.IsDestroyed) return;

		if (_hashes.Remove(typeof(T)))
		{
			found.IsDestroyed = true;
			_toDelete.Add(found);
		}
	}

	public T? Find<T>() where T : EcsSystem
	{
		_ = _hashes.TryGetValue(typeof(T), out var system);
		return system as T;
	}

	public void Update()
	{
		foreach (var sys in _toDelete)
			sys.Disable();

		foreach (var sys in _toDelete)
			sys.OnCleanup(_ecs);

		foreach (var sys in _toDelete)
			sys.OnDestroy(_ecs);

		foreach (var sys in _toDelete)
			_systems.Remove(sys);

		if (_toDelete.Count > 0)
			_toDelete.Clear();

		if (_toCreate.Count > 0)
		{
			foreach (var sys in _toCreate)
			{
				sys.OnCreate(_ecs);
				if (sys.IsEnabled)
					sys.OnStart(_ecs);
			}

			_toCreate.Clear();
		}

		foreach (var sys in _systems)
		{
			if (!sys.IsEnabled) continue;

			sys.OnBeforeUpdate(_ecs);
			sys.OnUpdate(_ecs);
			sys.OnAfterUpdate(_ecs);
		}
	}

	public void Clear()
	{
		_systems.ForEach(s => s.Disable());
		_systems.ForEach(s => s.OnCleanup(_ecs));
		_systems.ForEach(s => s.OnDestroy(_ecs));
		_systems.Clear();
		_toDelete.Clear();
		_hashes.Clear();
	}
}

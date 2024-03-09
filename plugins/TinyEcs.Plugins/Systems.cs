using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

public abstract class System
{
	[NotNull] public World Ecs { get; internal set; }
	[NotNull] public string Name { get; internal set; }

	public bool IsEnabled { get; private set; } = true;
	internal bool IsDestroyed { get; set; }


	public virtual void OnCreate() { }

	public virtual void OnStart() { }

	public virtual void OnStop() { }

	public virtual void OnBeforeUpdate() { }

	public virtual void OnUpdate() { }

	public virtual void OnAfterUpdate() { }

	public virtual void OnCleanup() { }

	public virtual void OnDestroy() { }


	public void Enable() => Enable(true);

	public void Disable() => Enable(false);


	private void Enable(bool enable)
	{
		var tmp = IsEnabled;
		IsEnabled = enable;
		if (tmp == enable)
			return;

		if (enable)
			OnStart();
		else
			OnStop();
	}
}


public sealed class SystemManager
{
	private readonly World _ecs;
	private readonly List<System> _systems, _toCreate, _toDelete;
	private readonly Dictionary<Type, System> _hashes;

	public SystemManager(World ecs)
	{
		_ecs = ecs;
		_hashes = new Dictionary<Type, System>();
		_systems = new List<System>();
		_toCreate = new List<System>();
		_toDelete = new List<System>();
	}

	public T Add<T>(string name = "") where T : System, new()
	{
		if (_hashes.TryGetValue(typeof(T), out var system))
			return (T)system;

		system = new T
		{
			Ecs = _ecs,
			Name = string.IsNullOrWhiteSpace(name) ? typeof(T).Name : name
		};

		_hashes.Add(typeof(T), system);
		_systems.Add(system);
		_toCreate.Add(system);

		return (T)system;
	}

	public void Delete<T>() where T : System
	{
		var found = Find<T>();
		if (found == null || found.IsDestroyed) return;

		if (_hashes.Remove(typeof(T)))
		{
			found.IsDestroyed = true;
			_toDelete.Add(found);
		}
	}

	public T? Find<T>() where T : System
	{
		_ = _hashes.TryGetValue(typeof(T), out var system);
		return system as T;
	}

	public void Update()
	{
		foreach (var sys in _toDelete)
			sys.Disable();

		foreach (var sys in _toDelete)
			sys.OnCleanup();

		foreach (var sys in _toDelete)
			sys.OnDestroy();

		foreach (var sys in _toDelete)
			_systems.Remove(sys);

		if (_toDelete.Count > 0)
			_toDelete.Clear();

		if (_toCreate.Count > 0)
		{
			foreach (var sys in _toCreate)
			{
				sys.OnCreate();
				if (sys.IsEnabled)
					sys.OnStart();
			}

			_toCreate.Clear();
		}

		foreach (var sys in _systems)
		{
			if (!sys.IsEnabled) continue;

			sys.OnBeforeUpdate();
			sys.OnUpdate();
			sys.OnAfterUpdate();
		}

	}

	public void Clear()
	{
		_systems.ForEach(s => s.Disable());
		_systems.ForEach(s => s.OnCleanup());
		_systems.ForEach(s => s.OnDestroy());
		_systems.Clear();
		_toDelete.Clear();
	}
}

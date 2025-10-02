using System;
using System.Collections.Generic;
using TinyEcs;

namespace MyBattleground.Bevy;

// ============================================================================
// System Parameters - Bevy-style automatic dependency injection
// ============================================================================

/// <summary>
/// Base interface for all system parameters that can be injected into systems
/// </summary>
public interface ISystemParam
{
	void Initialize(TinyEcs.World world);
	void Fetch(TinyEcs.World world);
}

// ============================================================================
// Res<T> - Immutable resource access
// ============================================================================

/// <summary>
/// Immutable reference to a resource. Use for read-only access.
/// </summary>
public class Res<T> : ISystemParam where T : notnull
{
	private TinyEcs.World? _world;
	public T Value { get; private set; } = default!;

	public void Initialize(TinyEcs.World world)
	{
		_world = world;
	}

	public void Fetch(TinyEcs.World world)
	{
		Value = world.GetResource<T>();
	}
}

// ============================================================================
// ResMut<T> - Mutable resource access
// ============================================================================

/// <summary>
/// Mutable reference to a resource. Use when you need to modify it.
/// </summary>
public class ResMut<T> : ISystemParam where T : notnull
{
	private TinyEcs.World? _world;
	public T Value { get; private set; } = default!;

	public void Initialize(TinyEcs.World world)
	{
		_world = world;
	}

	public void Fetch(TinyEcs.World world)
	{
		Value = world.GetResource<T>();
	}
}

// ============================================================================
// Local<T> - Per-system local state
// ============================================================================

/// <summary>
/// Per-system local state that persists between system runs.
/// Each system instance gets its own independent Local<T>.
/// </summary>
public class Local<T> : ISystemParam where T : new()
{
	public T Value { get; private set; } = new T();

	public void Initialize(TinyEcs.World world)
	{
		// Local state is initialized once and persists
		Value = new T();
	}

	public void Fetch(TinyEcs.World world)
	{
		// Local state doesn't need to fetch - it's already there
	}
}

// ============================================================================
// EventReader<T> - Read events of type T
// ============================================================================

/// <summary>
/// Reads events of type T from the event queue.
/// Events are consumed after being read.
/// </summary>
public class EventReader<T> : ISystemParam where T : notnull
{
	private Queue<T>? _queue;
	private readonly List<T> _events = new();

	public void Initialize(TinyEcs.World world)
	{
		var state = world.GetState();
		var type = typeof(T);

		if (!state.EventQueues.TryGetValue(type, out var queueObj))
		{
			queueObj = new Queue<T>();
			state.EventQueues[type] = queueObj;
		}

		_queue = (Queue<T>)queueObj;
	}

	public void Fetch(TinyEcs.World world)
	{
		_events.Clear();

		if (_queue != null)
		{
			while (_queue.Count > 0)
			{
				_events.Add(_queue.Dequeue());
			}
		}
	}

	/// <summary>
	/// Iterate over all events of type T that occurred this frame
	/// </summary>
	public IEnumerable<T> Read()
	{
		return _events;
	}

	/// <summary>
	/// Check if any events of this type were sent
	/// </summary>
	public bool HasEvents => _events.Count > 0;

	/// <summary>
	/// Get the number of events
	/// </summary>
	public int Count => _events.Count;
}

// ============================================================================
// EventWriter<T> - Write events of type T
// ============================================================================

/// <summary>
/// Writes events of type T to the event queue.
/// Events will be processed at the end of the current stage.
/// </summary>
public class EventWriter<T> : ISystemParam where T : notnull
{
	private TinyEcs.World? _world;

	public void Initialize(TinyEcs.World world)
	{
		_world = world;
	}

	public void Fetch(TinyEcs.World world)
	{
		_world = world;
	}

	/// <summary>
	/// Send an event to be processed at the end of the current stage
	/// </summary>
	public void Send(T evt)
	{
		_world?.SendEvent(evt);
	}
}

// ============================================================================
// Query Parameter - Typed ECS queries
// ============================================================================

/// <summary>
/// Typed query parameter for iterating entities with specific components
/// </summary>
public class Query<TQueryData> : ISystemParam
	where TQueryData : struct, IData<TQueryData>, IQueryIterator<TQueryData>, allows ref struct
{
	private TinyEcs.World? _world;
	private TinyEcs.Query<TQueryData>? _query;

	public void Initialize(TinyEcs.World world)
	{
		_world = world;
	}

	public void Fetch(TinyEcs.World world)
	{
		_query = world.Query<TQueryData>();
	}

	public TinyEcs.Query<TQueryData> Inner => _query!;

	public int Count() => _query!.Count();
	public bool Contains(ulong id) => _query!.Contains(id);
	public TQueryData Get(ulong id) => _query!.Get(id);
	public TinyEcs.QueryIter<TQueryData, Empty> GetEnumerator() => _query!.GetEnumerator();
}

/// <summary>
/// Typed query parameter with filter
/// </summary>
public class Query<TQueryData, TQueryFilter> : ISystemParam
	where TQueryData : struct, IData<TQueryData>, IQueryIterator<TQueryData>, allows ref struct
	where TQueryFilter : struct, IFilter<TQueryFilter>, allows ref struct
{
	private TinyEcs.World? _world;
	private TinyEcs.Query<TQueryData, TQueryFilter>? _query;

	public void Initialize(TinyEcs.World world)
	{
		_world = world;
	}

	public void Fetch(TinyEcs.World world)
	{
		_query = world.Query<TQueryData, TQueryFilter>();
	}

	public TinyEcs.Query<TQueryData, TQueryFilter> Inner => _query!;

	public int Count() => _query!.Count();
	public bool Contains(ulong id) => _query!.Contains(id);
	public TQueryData Get(ulong id) => _query!.Get(id);
	public QueryIter<TQueryData, TQueryFilter> GetEnumerator() => _query!.GetEnumerator();
}

// ============================================================================
// Parameterized System - Supports dependency injection
// ============================================================================

public class ParameterizedSystem : ISystem
{
	private readonly Action<TinyEcs.World> _systemFn;
	private readonly ISystemParam[] _parameters;
	private bool _initialized = false;

	public ParameterizedSystem(Action<TinyEcs.World> systemFn, params ISystemParam[] parameters)
	{
		_systemFn = systemFn;
		_parameters = parameters;
	}

	public void Run(TinyEcs.World world)
	{
		if (!_initialized)
		{
			// Initialize all parameters once
			foreach (var param in _parameters)
			{
				param.Initialize(world);
			}
			_initialized = true;
		}

		// Fetch latest data for all parameters
		foreach (var param in _parameters)
		{
			param.Fetch(world);
		}

		// Run the system
		_systemFn(world);
	}
}

// ============================================================================
// System Function Adapters - Convert functions with params to ISystem
// ============================================================================

public static class SystemFunctionAdapters
{
	// 1 parameter
	public static ISystem Create<T1>(Action<T1> systemFn)
		where T1 : ISystemParam, new()
	{
		var p1 = new T1();
		return new ParameterizedSystem(
			world => systemFn(p1),
			p1
		);
	}

	// 2 parameters
	public static ISystem Create<T1, T2>(Action<T1, T2> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		return new ParameterizedSystem(
			world => systemFn(p1, p2),
			p1, p2
		);
	}

	// 3 parameters
	public static ISystem Create<T1, T2, T3>(Action<T1, T2, T3> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3),
			p1, p2, p3
		);
	}

	// 4 parameters
	public static ISystem Create<T1, T2, T3, T4>(Action<T1, T2, T3, T4> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4),
			p1, p2, p3, p4
		);
	}

	// 5 parameters
	public static ISystem Create<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5),
			p1, p2, p3, p4, p5
		);
	}

	// 6 parameters
	public static ISystem Create<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6),
			p1, p2, p3, p4, p5, p6
		);
	}

	// 7 parameters
	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6, p7),
			p1, p2, p3, p4, p5, p6, p7
		);
	}

	// 8 parameters
	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
		where T8 : ISystemParam, new()
	{
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8),
			p1, p2, p3, p4, p5, p6, p7, p8
		);
	}
}

// ============================================================================
// App Extensions for System Parameters
// ============================================================================

public static class AppSystemParamExtensions
{
	// 1 parameter
	public static ISystemStageSelector AddSystem<T1>(this App app, Action<T1> systemFn)
		where T1 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	// 2 parameters
	public static ISystemStageSelector AddSystem<T1, T2>(this App app, Action<T1, T2> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	// 3 parameters
	public static ISystemStageSelector AddSystem<T1, T2, T3>(this App app, Action<T1, T2, T3> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	// 4 parameters
	public static ISystemStageSelector AddSystem<T1, T2, T3, T4>(this App app, Action<T1, T2, T3, T4> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	// 5 parameters
	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5>(this App app, Action<T1, T2, T3, T4, T5> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	// 6 parameters
	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6>(this App app, Action<T1, T2, T3, T4, T5, T6> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	// 7 parameters
	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7>(this App app, Action<T1, T2, T3, T4, T5, T6, T7> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	// 8 parameters
	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8> systemFn)
		where T1 : ISystemParam, new()
		where T2 : ISystemParam, new()
		where T3 : ISystemParam, new()
		where T4 : ISystemParam, new()
		where T5 : ISystemParam, new()
		where T6 : ISystemParam, new()
		where T7 : ISystemParam, new()
		where T8 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}
}

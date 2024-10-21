using System.Diagnostics.CodeAnalysis;

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
	private readonly LinkedList<FuncSystem<TArg>> _after = new();
	private readonly LinkedList<FuncSystem<TArg>> _before = new();
	internal LinkedListNode<FuncSystem<TArg>>? Node { get; set; }


	internal FuncSystem(TArg arg, Action<TArg, Func<TArg, bool>> fn, Func<bool> checkInUse, ThreadingMode threadingType)
	{
		_arg = arg;
		_fn = fn;
		_conditions = new();
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
	private readonly List<FuncSystem<World>> _singleThreads = new();
	private readonly List<FuncSystem<World>> _multiThreads = new();

	public Scheduler(World world)
	{
		_world = world;

		for (var i = 0; i < _systems.Length; ++i)
			_systems[i] = new();

		AddSystemParam(world);
		AddSystemParam(new SchedulerState(this));
		AddSystemParam(new Commands(world));
	}

	public void Run()
	{
		RunStage(Stages.Startup);
		_systems[(int)Stages.Startup].Clear();

		for (var stage = Stages.FrameStart; stage <= Stages.FrameEnd; stage += 1)
			RunStage(stage);
	}

	private void RunStage(Stages stage)
	{
		_singleThreads.Clear();
		_multiThreads.Clear();

		var systems = _systems[(int)stage];

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
		var sys = new FuncSystem<World>(_world, (args, runIf) => {
			if (runIf?.Invoke(args) ?? true)
				system();
		}, () => false, threadingType);
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
		return AddSystemParam(new EventParam<T>());
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

internal sealed class EventParam<T> : SystemParam<World>, IIntoSystemParam<World> where T : notnull
{
	private readonly Queue<T> _queue = new();

	internal EventParam()
	{
		Writer = new EventWriter<T>(_queue);
		Reader = new EventReader<T>(_queue);
	}

	public EventWriter<T> Writer { get; }
	public EventReader<T> Reader { get; }


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
	private readonly Queue<T> _queue;

	internal EventWriter(Queue<T> queue)
		=> _queue = queue;

	public bool IsEmpty
		=> _queue.Count == 0;

	public void Clear()
		=> _queue.Clear();

	public void Enqueue(T ev)
		=> _queue.Enqueue(ev);

	public static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<EventParam<T>>>().Has<Placeholder<EventParam<T>>>())
			return arg.Entity<Placeholder<EventParam<T>>>().Get<Placeholder<EventParam<T>>>().Value.Writer;

		throw new NotImplementedException("EventWriter<T> must be created using the scheduler.AddEvent<T>() method");
	}
}

public sealed class EventReader<T> : SystemParam<World>, IIntoSystemParam<World> where T : notnull
{
	private readonly Queue<T> _queue;

	internal EventReader(Queue<T> queue)
		=> _queue = queue;

	public bool IsEmpty
		=> _queue.Count == 0;

	public void Clear()
		=> _queue.Clear();

	public EventReaderIterator GetEnumerator() => new(_queue!);

	public static ISystemParam<World> Generate(World arg)
	{
		if (arg.Entity<Placeholder<EventParam<T>>>().Has<Placeholder<EventParam<T>>>())
			return arg.Entity<Placeholder<EventParam<T>>>().Get<Placeholder<EventParam<T>>>().Value.Reader;

		throw new NotImplementedException("EventReader<T> must be created using the scheduler.AddEvent<T>() method");
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

partial class World : SystemParam<World>, IIntoSystemParam<World>
{
	public World() : this(256) { }

	public static ISystemParam<World> Generate(World arg)
	{
		return arg;
	}
}

public class Query<TQueryData> : Query<TQueryData, Empty>, IIntoSystemParam<World>
	where TQueryData : struct, IData<TQueryData>
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
	where TQueryData : struct, IData<TQueryData>
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IQueryIterator<TQueryData> GetEnumerator() => TQueryData.CreateIterator(_query.Iter());


	public ref T Single<T>() where T : struct, IComponent
		=> ref _query.Single<T>();

	public EntityView Single()
		=> _query.Single();

	public int Count()
		=> _query.Count();
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
}


public interface ITermCreator
{
	public static abstract void Build(QueryBuilder builder);
}
public interface IQueryIterator<TData> where TData : struct, IData<TData>
{
	public IQueryIterator<TData> GetEnumerator();

	public TData Current { get; }
	public bool MoveNext();
}

public interface IComponent
{
}

public interface IData<TData> : ITermCreator where TData : struct, IData<TData>
{
	public static abstract IQueryIterator<TData> CreateIterator(ComponentsSpanIterator iterator);
}
public interface IFilter : ITermCreator { }

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

//public struct Pair2<TAction, TTarget> : IData<Pair2<TAction, TTarget>>, IFilter, IComponent, INestedFilter
//	where TAction : struct, IComponent
//	where TTarget : struct, IComponent
//{
//	private ComponentsSpanIterator _iterator;

//	internal Pair2(ComponentsSpanIterator iterator) => _iterator = iterator;

//	public static void Build(QueryBuilder builder)
//	{
//		builder.With<TAction, TTarget>();
//	}

//	public static IQueryIterator<Pair2<TAction, TTarget>> CreateIterator(ComponentsSpanIterator iterator)
//	{
//		return default;
//	}

//	public void BuildAsParam(QueryBuilder builder)
//	{
//		Build(builder);
//	}
//}


public struct Empty : IData<Empty>, IQueryIterator<Empty>, IComponent, IFilter
{
	private ComponentsSpanIterator _iterator;

	internal Empty(ComponentsSpanIterator iterator) => _iterator = iterator;


	public static void Build(QueryBuilder builder)
	{

	}

	public static IQueryIterator<Empty> CreateIterator(ComponentsSpanIterator iterator)
	{
		return new Empty(iterator);
	}

	public readonly Empty Current => this;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Deconstruct(out ReadOnlySpan<EntityView> entities, out int count)
	{
		ref readonly var chunk = ref _iterator.Current;
		entities = chunk.Entities.AsSpan(0, chunk.Count);
		count = chunk.Count;
	}

	public readonly IQueryIterator<Empty> GetEnumerator() => this;

	public bool MoveNext() => _iterator.MoveNext();
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
public readonly struct Without<T> : IFilter, INestedFilter
	where T : struct, IComponent
{
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
public readonly struct Optional<T> : IFilter, INestedFilter
	where T : struct, IComponent
{
	public static void Build(QueryBuilder builder)
	{
		builder.Optional<T>();
	}

	public void BuildAsParam(QueryBuilder builder)
	{
		Build(builder);
	}
}

///// <summary>
///// Used in query filters to find entities with any of the corrisponding components/tag.
///// </summary>
///// <typeparam name="T"></typeparam>
//public readonly struct AtLeast<T> : ITuple, IFilter where T : struct, ITuple
//{
//	static readonly ITuple _value = default(T)!;

//	public object? this[int index] => _value[index];

//	public int Length => _value.Length;
//	public static void Build(QueryBuilder builder)
//	{
//		throw new NotImplementedException();
//	}
//}

///// <summary>
///// Used in query filters to find entities with exactly the corrisponding components/tag.
///// </summary>
///// <typeparam name="T"></typeparam>
//public readonly struct Exactly<T> : ITuple, IFilter where T : struct, ITuple
//{
//	static readonly ITuple _value = default(T)!;

//	public object? this[int index] => _value[index];

//	public int Length => _value.Length;
//	public static void Build(QueryBuilder builder)
//	{
//		throw new NotImplementedException();
//	}
//}

///// <summary>
///// Used in query filters to find entities with none the corrisponding components/tag.
///// </summary>
///// <typeparam name="T"></typeparam>
//public readonly struct None<T> : ITuple, IFilter where T : struct, ITuple
//{
//	static readonly ITuple _value = default(T)!;

//	public object? this[int index] => _value[index];

//	public int Length => _value.Length;
//	public static void Build(QueryBuilder builder)
//	{
//		throw new NotImplementedException();
//	}
//}

///// <summary>
///// Used in query filters to accomplish the 'or' logic.
///// </summary>
///// <typeparam name="T"></typeparam>
//public readonly struct Or<TFirst, TSecond> : IComponent, IFilter, INestedFilter
//	where TFirst : struct, IComponent
//	where TSecond : struct, IComponent
//{
//	public static void Build(QueryBuilder builder)
//	{
//		//builder.Optional<TFirst>();
//		//builder.Optional<TSecond>();

//		var ok0 = FilterBuilder<TFirst>.Build(builder);
//		var ok1 = FilterBuilder<TSecond>.Build(builder);

//		builder.AtLeast(builder.World.Component<TFirst>().ID, builder.World.Component<TSecond>().ID);
//	}

//	public void BuildAsParam(QueryBuilder builder)
//	{
//		Build(builder);
//	}
//}


public partial struct Parent : IComponent { }
public partial interface IChildrenComponent : IComponent { }

#endif

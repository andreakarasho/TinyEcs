// https://github.com/jasonliang-dev/entity-component-system
using System.Diagnostics;
using TinyEcs;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;

const int ENTITIES_COUNT = (524_288 * 2 * 1);

using var ecs = new World();
var scheduler = new Scheduler(ecs);

for (int i = 0; i < ENTITIES_COUNT; i++)
	ecs.Entity()
		.Set<Position>(new Position())
		.Set<Velocity>(new Velocity());

ecs.Entity().Set(new Position()).Set(new Velocity()).Set(new Mass());

scheduler.AddSystem((Query<
	Data<Position, Velocity>,
	Empty,
	Changed<Position>
> q)=>
{
	foreach ((var ent, var pos, var vel) in q)
	{
		pos.Ref.X *= vel.Ref.X;
		pos.Ref.Y *= vel.Ref.Y;
	}
}, threadingType: ThreadingMode.Single);


var query = ecs.QueryBuilder()
	.With<Position>()
	.With<Velocity>()
	.Build();

var sw = Stopwatch.StartNew();
var start = 0f;
var last = 0f;

while (true)
{
	for (int i = 0; i < 3600; ++i)
	{
		// scheduler.RunOnce();

		Execute(query);
		// ExecuteIterator(query);

		// var it = query.Iter();
		// while (it.Next())
		// {
		// 	var count = it.Count;

		// 	ref var pos = ref it.DataRef<Position>(0);
		// 	ref var vel = ref it.DataRef<Velocity>(1);
		// 	ref var lastPos = ref Unsafe.Add(ref pos, count);

		// 	while (Unsafe.IsAddressLessThan(ref pos, ref lastPos))
		// 	{
		// 		pos.X *= vel.X;
		// 		pos.Y *= vel.Y;

		// 		pos = ref Unsafe.Add(ref pos, 1);
		// 		vel = ref Unsafe.Add(ref vel, 1);
		// 	}
		// }
	}

	last = start;
	start = sw.ElapsedMilliseconds;

	Console.WriteLine("query done in {0} ms", start - last);
}


static void Execute(Query query)
{
	foreach ((var ent, var pos, var vel) in Data2<Position, Velocity>.CreateIterator(query.Iter()))
	{
		pos.Ref.X *= vel.Ref.X;
		pos.Ref.Y *= vel.Ref.Y;
	}
}

static void ExecuteIterator(Query query)
{
	var it = query.Iter();

	while (it.Next())
	{
		var span0 = it.Data<Position>(0);
		var span1 = it.Data<Velocity>(1);
		var count = it.Count;

		for (var i = 0; i < count; ++i)
		{
			ref var pos = ref span0[i];
			ref var vel = ref span1[i];

			pos.X *= vel.X;
			pos.Y *= vel.Y;
		}
	}
}

struct Position
{
	public float X, Y, Z;
}

struct Velocity
{
	public float X, Y;
}

struct Mass { public float Value; }

struct Tag { }

[SkipLocalsInit]
public ref struct Data2<T0, T1> : IData<Data2<T0, T1>>, IQueryIterator<Data2<T0, T1>>
	where T0 : struct where T1 : struct
{
	private QueryIterator _iterator;
	private readonly uint _tick;
	private int _index, _count;
	private ReadOnlySpan<EntityView> _entities;
	private DataRow<T0> _current0;
	private DataRow<T1> _current1;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Data2(QueryIterator queryIterator)
	{
		_iterator = queryIterator;
		_tick = queryIterator.Tick;
		_index = -1;
		_count = -1;
	}

	public static void Build(QueryBuilder builder)
	{
		if (!FilterBuilder<T0>.Build(builder)) builder.With<T0>();
		if (!FilterBuilder<T1>.Build(builder)) builder.With<T1>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Data2<T0, T1> CreateIterator(QueryIterator iterator)
		=> new (iterator);

	[System.Diagnostics.CodeAnalysis.UnscopedRef]
	public ref Data2<T0, T1> Current
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1)
	{
		ptr0 = _current0.Value;
		ptr1 = _current1.Value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly void Deconstruct(out PtrRO<EntityView> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1)
	{
		entity = new (in _entities[_index]);
		ptr0 = _current0.Value;
		ptr1 = _current1.Value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool MoveNext()
	{
		while (true)
		{
			if (++_index >= _count)
			{
				if (!_iterator.Next())
					return false;

				_current0.Value.Ref = ref _iterator.DataRefWithSizeAndChanged<T0>(0, out _current0.Size, ref _current0.Tick);
				_current1.Value.Ref = ref _iterator.DataRefWithSizeAndChanged<T1>(1, out _current1.Size, ref _current1.Tick);
				_entities = _iterator.Entities();
				// if (_tick == 0)
				// 	return true;

				_index = 0;
				_count = _iterator.Count;
			}
			else
			{
				_current0.Next();
				_current1.Next();

				// if (_tick == 0)
				// 	return true;

				_current0.NextTick();
				_current1.NextTick();
			}

			// if (_current0.IsChanged(in _tick) && _current1.IsChanged(in _tick))
			// {
			// 	return true;
			// }

			return true;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Data2<T0, T1> GetEnumerator() => this;
}

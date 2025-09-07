using System.Collections.Concurrent;

namespace TinyEcs;

public sealed partial class World
{
	private readonly ConcurrentQueue<DeferredOp> _operations = new();
	private WorldState _worldState = new() { Locks = 0 };

	public bool IsDeferred => _worldState.Locks > 0;
	public bool IsMerging => _worldState.Locks < 0;




	public void BeginDeferred()
	{
		if (IsMerging)
			return;

		var locks = _worldState.Begin();
		EcsAssert.Assert(locks > 0, "");
	}


	public void EndDeferred()
	{
		if (IsMerging)
			return;

		var locks = _worldState.End();
		EcsAssert.Assert(locks >= 0, "");

		if (locks == 0 && _operations.Count > 0)
		{
			_worldState.Lock();
			Merge();
			_worldState.Unlock();
		}
	}


	internal void AddDeferred<T>(EcsID entity) where T : struct
	{
		ref readonly var cmp = ref Component<T>();

		var cmd = new DeferredOp()
		{
			Op = DeferredOpTypes.SetComponent,
			Entity = entity,
			Data = null!,
			ComponentInfo = cmp
		};

		_operations.Enqueue(cmd);
	}

	internal ref T SetDeferred<T>(EcsID entity, T component) where T : struct
	{
		ref readonly var cmp = ref Component<T>();

		var cmd = new DeferredOp()
		{
			Op = DeferredOpTypes.SetComponent,
			Entity = entity,
			Data = component,
			ComponentInfo = cmp
		};

		_operations.Enqueue(cmd);

		return ref Unsafe.Unbox<T>(cmd.Data);
	}

	internal object? SetDeferred(EcsID entity, EcsID id, object? rawCmp, int size)
	{
		var cmp = new ComponentInfo(id, size);

		var cmd = new DeferredOp()
		{
			Op = DeferredOpTypes.SetComponent,
			Entity = entity,
			Data = rawCmp,
			ComponentInfo = cmp
		};

		_operations.Enqueue(cmd);
		return rawCmp;
	}

	internal void SetChangedDeferred<T>(EcsID entity) where T : struct
	{
		ref readonly var cmp = ref Component<T>();

		var cmd = new DeferredOp()
		{
			Op = DeferredOpTypes.SetChanged,
			Entity = entity,
			ComponentInfo = cmp
		};

		_operations.Enqueue(cmd);
	}

	internal void SetChangedDeferred(EcsID entity, EcsID id)
	{
		var cmp = new ComponentInfo(id, -1);

		var cmd = new DeferredOp()
		{
			Op = DeferredOpTypes.SetChanged,
			Entity = entity,
			ComponentInfo = cmp
		};

		_operations.Enqueue(cmd);
	}

	internal void UnsetDeferred<T>(EcsID entity) where T : struct
	{
		ref readonly var cmp = ref Component<T>();

		var cmd = new DeferredOp()
		{
			Op = DeferredOpTypes.UnsetComponent,
			Entity = entity,
			ComponentInfo = cmp
		};

		_operations.Enqueue(cmd);
	}

	internal void UnsetDeferred(EcsID entity, EcsID id)
	{
		var cmp = new ComponentInfo(id, 0);

		var cmd = new DeferredOp()
		{
			Op = DeferredOpTypes.UnsetComponent,
			Entity = entity,
			ComponentInfo = cmp
		};

		_operations.Enqueue(cmd);
	}

	internal void DeleteDeferred(EcsID entity)
	{
		var cmd = new DeferredOp()
		{
			Op = DeferredOpTypes.DestroyEntity,
			Entity = entity
		};

		_operations.Enqueue(cmd);
	}


	private void Merge()
	{
		while (_operations.TryDequeue(out var op))
		{
			switch (op.Op)
			{
				case DeferredOpTypes.DestroyEntity:
					if (Exists(op.Entity))
						Delete(op.Entity);
					break;

				case DeferredOpTypes.SetComponent:
					{
						(var array, var row) = Attach(op.Entity, op.ComponentInfo.ID, op.ComponentInfo.Size);
						array?.SetValue(op.Data, row & TinyEcs.Archetype.CHUNK_THRESHOLD);

						break;
					}

				case DeferredOpTypes.UnsetComponent:
					{
						Detach(op.Entity, op.ComponentInfo.ID);

						break;
					}
				case DeferredOpTypes.SetChanged:
					{
						if (Exists(op.Entity) && Has(op.Entity, op.ComponentInfo.ID))
							SetChanged(op.Entity, op.ComponentInfo.ID);
						break;
					}
			}
		}

	}


	struct WorldState
	{
		public int Locks;

		public int Lock() => Locks = -1;
		public int Unlock() => Locks = 0;
		public int Begin() => Interlocked.Increment(ref Locks);
		public int End() => Interlocked.Decrement(ref Locks);
	}

	struct DeferredOp
	{
		public DeferredOpTypes Op;
		public EcsID Entity;
		public ComponentInfo ComponentInfo;
		public object? Data;
	}

	enum DeferredOpTypes : byte
	{
		DestroyEntity,
		SetComponent,
		UnsetComponent,
		SetChanged
	}
}

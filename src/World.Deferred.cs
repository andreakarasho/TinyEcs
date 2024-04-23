using System.Collections.Concurrent;

namespace TinyEcs;

public sealed partial class World
{
	private readonly ConcurrentQueue<DeferredOp> _operations = new();
	private WorldState _worldState = new () { State = WorldStateTypes.Normal, Locks = 0 };
	private readonly object _worldStateLock = new object();


	public bool IsDeferred => _worldState.State == WorldStateTypes.Deferred;



	public void BeginDeferred()
	{
		lock (_worldStateLock)
		{
			_worldState.State = WorldStateTypes.Deferred;
			_worldState.Locks += 1;
		}
	}

	public void EndDeferred()
	{
		lock (_worldStateLock)
		{
			_worldState.Locks -= 1;

			EcsAssert.Assert(_worldState.Locks >= 0, "begin/end deferred calls mismatch");

			if (_worldState.Locks == 0)
			{
				_worldState.State = WorldStateTypes.Merging;
				Merge();
				_worldState.State = WorldStateTypes.Normal;
			}
		}
	}


	private void SetDeferred<T>(EcsID entity) where T : struct
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

	private ref T SetDeferred<T>(EcsID entity, T component) where T : struct
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

	private void SetDeferred(EcsID entity, EcsID id)
	{
		var cmp = Lookup.GetComponent(id, 0);

		var cmd = new DeferredOp()
		{
			Op = DeferredOpTypes.SetComponent,
			Entity = entity,
			Data = null!,
			ComponentInfo = cmp
		};

		_operations.Enqueue(cmd);
	}

	private void UnsetDeferred<T>(EcsID entity) where T : struct
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

	private void UnsetDeferred(EcsID entity, EcsID id)
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

	private void DeleteDeferred(EcsID entity)
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
					Delete(op.Entity);
					break;

				case DeferredOpTypes.SetComponent:
				{
					if (op.ComponentInfo.ID.IsPair)
					{
						var first = op.ComponentInfo.ID.First;
						var second = op.ComponentInfo.ID.Second;

						CheckUnique(op.Entity, first);
						CheckSymmetric(op.Entity, first, second);
					}

					(var array, var row) = AttachComponent(op.Entity, op.ComponentInfo.ID, op.ComponentInfo.Size);
					array?.SetValue(op.Data, row & Archetype.CHUNK_THRESHOLD);

					break;
				}

				case DeferredOpTypes.UnsetComponent:
				{
					DetachComponent(op.Entity, op.ComponentInfo.ID);

					break;
				}
			}
		}

	}

	enum WorldStateTypes
	{
		Normal,
		Deferred,
		Merging
	}

	struct WorldState
	{
		public WorldStateTypes State;
		public int Locks;
	}

	struct DeferredOp
	{
		public DeferredOpTypes Op;
		public EcsID Entity;
		public ComponentInfo ComponentInfo;
		public object Data;
	}

	enum DeferredOpTypes : byte
	{
		DestroyEntity,
		SetComponent,
		UnsetComponent,
	}
}

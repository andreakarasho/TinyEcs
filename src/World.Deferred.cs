using System.Collections.Concurrent;
using Microsoft.Collections.Extensions;

namespace TinyEcs;

public sealed partial class World
{
	private readonly ConcurrentQueue<DeferredOp> _operations = new();
    private readonly ConcurrentDictionary<EcsID, DictionarySlim<EcsID, object?>> _deferredSets = new();
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

			if (_worldState.Locks == 0)
			{
				_worldState.NewEntities = 0;
				_worldState.State = WorldStateTypes.Normal;
				Merge();
			}
		}
	}

	private bool StoreDeferredSet<T>(EcsID entity, T? component = null) where T : struct
	{
		if (!_deferredSets.TryGetValue(entity, out var dict))
		{
			dict = new ();
			_deferredSets[entity] = dict;
		}

		dict.GetOrAddValueRef(Component<T>().ID, out var exists) = component;

		return exists;
	}

	private void SetDeferred<T>(EcsID entity) where T : struct
	{
		if (StoreDeferredSet<T>(entity))
			return;

		var cmd = new DeferredOp()
		{
			Op = DeferredOpTypes.SetComponent,
			Entity = entity,
			Data = null!,
			ComponentInfo = Component<T>()
		};

		_operations.Enqueue(cmd);
	}

	private void SetDeferred<T>(EcsID entity, T component) where T : struct
	{
		if (StoreDeferredSet<T>(entity, component))
			return;

		var cmd = new DeferredOp()
		{
			Op = DeferredOpTypes.SetComponent,
			Entity = entity,
			Data = component,
			ComponentInfo = Component<T>()
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

		if (_deferredSets.TryGetValue(entity, out var dict))
		{
			dict.Remove(cmp.ID);
		}
	}

	private void DeleteDeferred(EcsID entity)
	{
		var cmd = new DeferredOp()
		{
			Op = DeferredOpTypes.DestroyEntity,
			Entity = entity
		};

		_operations.Enqueue(cmd);

		if (_deferredSets.TryRemove(entity, out var dict))
		{
			dict.Clear();
		}
	}

	private bool HasDeferred<T>(EcsID entity) where T : struct
		=> _deferredSets.TryGetValue(entity, out var dict) && dict.ContainsKey(Component<T>().ID);

	private ref T GetDeferred<T>(EcsID entity) where T : struct
	{
		ref readonly var cmp = ref Component<T>();
		if (_deferredSets.TryGetValue(entity, out var dict) && dict.ContainsKey(cmp.ID))
		{
			ref var obj = ref dict.GetOrAddValueRef(cmp.ID, out var _);

			var cmd = new DeferredOp()
			{
				Op = DeferredOpTypes.EditComponent,
				Entity = entity,
				ComponentInfo = cmp,
				Data = null!
			};

			_operations.Enqueue(cmd);

			return ref Unsafe.Unbox<T>(obj);
		}

		return ref Unsafe.NullRef<T>();
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
					ref var record = ref GetRecord(op.Entity);
					var array = Set(Entity(op.Entity), ref record, in op.ComponentInfo);
					array?.SetValue(op.Data, record.Row & Archetype.CHUNK_THRESHOLD);

					break;
				}

				case DeferredOpTypes.UnsetComponent:
					DetachComponent(op.Entity, in op.ComponentInfo);
					break;

				case DeferredOpTypes.EditComponent:
				{
					if (_deferredSets.TryGetValue(op.Entity, out var dict) && dict.ContainsKey(op.ComponentInfo.ID))
					{
						ref var obj = ref dict.GetOrAddValueRef(op.ComponentInfo.ID, out var _);

						ref var record = ref GetRecord(op.Entity);
						var array = Set(Entity(op.Entity), ref record, in op.ComponentInfo);
						array?.SetValue(obj, record.Row & Archetype.CHUNK_THRESHOLD);
					}

					break;
				}
			}
		}

        _deferredSets.Clear();
	}

	enum WorldStateTypes
	{
		Normal,
		Deferred
	}

	struct WorldState
	{
		public WorldStateTypes State;
		public int Locks;
		public ulong NewEntities;
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
		EditComponent,
	}
}
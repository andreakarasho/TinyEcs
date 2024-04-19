using System.Collections.Concurrent;
using Microsoft.Collections.Extensions;

namespace TinyEcs;

public sealed partial class World
{
	private readonly ConcurrentQueue<DeferredOp> _operations = new();
	private readonly ConcurrentDictionary<EcsID, EcsID> _createdEntities = new();
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

			EcsAssert.Assert(_worldState.Locks >= 0, "begin/end deferred calls mismatch");

			if (_worldState.Locks == 0)
			{
				_worldState.State = WorldStateTypes.Merging;
				Merge();
				_worldState.State = WorldStateTypes.Normal;
			}
		}
	}

	private bool StoreDeferredSet(EcsID entity, EcsID id, object? component = null)
	{
		if (!_deferredSets.TryGetValue(entity, out var dict))
		{
			dict = new ();
			_deferredSets[entity] = dict;
		}

		dict.GetOrAddValueRef(id, out var exists) = component;

		return exists;
	}

	private void SetDeferred<T>(EcsID entity) where T : struct
	{
		ref readonly var cmp = ref Component<T>();

		if (StoreDeferredSet(entity, cmp.ID))
			return;

		var cmd = new DeferredOp()
		{
			Op = DeferredOpTypes.SetComponent,
			Entity = entity,
			Data = null!,
			ComponentInfo = cmp
		};

		_operations.Enqueue(cmd);
	}

	private void SetDeferred<T>(EcsID entity, T component) where T : struct
	{
		ref readonly var cmp = ref Component<T>();

		if (StoreDeferredSet(entity, cmp.ID, component))
			return;

		var cmd = new DeferredOp()
		{
			Op = DeferredOpTypes.SetComponent,
			Entity = entity,
			Data = component,
			ComponentInfo = cmp
		};

		_operations.Enqueue(cmd);
	}

	private void SetDeferred(EcsID entity, EcsID id)
	{
		if (StoreDeferredSet(entity, id))
			return;

		var cmp = new ComponentInfo(id, 0);

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

		if (_deferredSets.TryGetValue(entity, out var dict))
		{
			dict.Remove(cmp.ID);
		}
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

		if (_deferredSets.TryGetValue(entity, out var dict))
		{
			dict.Remove(cmp.ID);
		}
	}

	private void CreateDeferred(EcsID entity)
	{
		var cmd = new DeferredOp()
		{
			Op = DeferredOpTypes.CreateEntity,
			Entity = entity,
		};

		_operations.Enqueue(cmd);

		_createdEntities[entity] = entity;
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

	private bool ExistsDeferred(EcsID entity)
		=> entity.IsPair ? _createdEntities.ContainsKey(entity.First) &&
			_createdEntities.ContainsKey(entity.Second) : _createdEntities.ContainsKey(entity);

	private bool HasDeferred<T>(EcsID entity) where T : struct
		=> HasDeferred(entity, Component<T>().ID);

	private bool HasDeferred(EcsID entity, EcsID id)
	{
		if (_deferredSets.TryGetValue(entity, out var dict))
		{
			if (dict.ContainsKey(id)) return true;

			// TODO: fix this crap
			if (id.IsPair)
			{
				foreach ((var cmpId, _) in dict)
				{
					if (_comparer.Compare(cmpId.Value, id.Value) == 0)
						return true;
				}
			}
		}

		return false;
	}

	private EcsID GetDeferred(EcsID entity, EcsID id)
	{
		if (_deferredSets.TryGetValue(entity, out var dict))
		{
			if (dict.ContainsKey(id)) return id;

			// TODO: fix this crap
			if (id.IsPair)
			{
				foreach ((var cmpId, _) in dict)
				{
					if (_comparer.Compare(cmpId.Value, id.Value) == 0)
						return cmpId.Value;
				}
			}
		}

		return 0;
	}

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

			return ref Unsafe.Unbox<T>(obj!);
		}

		return ref Unsafe.NullRef<T>();
	}


	private void Merge()
	{
		while (_operations.TryDequeue(out var op))
		{
			switch (op.Op)
			{
				case DeferredOpTypes.CreateEntity:
					var ent = Entity(op.Entity);
					EcsAssert.Assert(ent == op.Entity);
					break;

				case DeferredOpTypes.DestroyEntity:
					Delete(op.Entity);
					break;

				case DeferredOpTypes.SetComponent:
				{
					(var array, var row) = AttachComponent(op.Entity, op.ComponentInfo.ID, op.ComponentInfo.Size);
					array?.SetValue(op.Data, row & Archetype.CHUNK_THRESHOLD);

					break;
				}

				case DeferredOpTypes.UnsetComponent:
				{
					DetachComponent(op.Entity, op.ComponentInfo.ID);

					break;
				}

				case DeferredOpTypes.EditComponent:
				{
					ref readonly var cmp = ref op.ComponentInfo;
					if (_deferredSets.TryGetValue(op.Entity, out var dict) && dict.ContainsKey(cmp.ID))
					{
						ref var obj = ref dict.GetOrAddValueRef(cmp.ID, out var _);
						(var array, var row) = AttachComponent(op.Entity, op.ComponentInfo.ID, op.ComponentInfo.Size);
						array?.SetValue(obj, row & Archetype.CHUNK_THRESHOLD);
					}

					break;
				}
			}
		}

        _deferredSets.Clear();
		_createdEntities.Clear();
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
		CreateEntity,
		DestroyEntity,
		SetComponent,
		UnsetComponent,
		EditComponent,
	}
}

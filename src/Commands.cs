using System.Collections.Concurrent;
using Microsoft.Collections.Extensions;

namespace TinyEcs;

public sealed partial class Commands
{
    private readonly EntitySparseSet<EcsID> _despawn;
    private readonly EntitySparseSet<SetComponent> _set;
    private readonly EntitySparseSet<UnsetComponent> _unset;

    internal Commands(World main)
    {
        World = main;
        _despawn = new();
        _set = new();
        _unset = new();
    }

    internal World World { get; set; }


    public CommandEntityView Entity(EcsID id = default)
    {
        if (id == 0 || !World.Exists(id))
            id = World.NewEmpty(id);

        return new CommandEntityView(this, id);
    }

    public void Delete(EcsID id)
    {
        EcsAssert.Assert(World.Exists(id));

        ref var entity = ref _despawn.Get(id);
        if (Unsafe.IsNullRef(ref entity))
        {
            _despawn.Add(id, id);
        }
    }

    public void Set<T>(EcsID id) where T : struct
	{
        ref readonly var cmp = ref World.Component<T>();
		EcsAssert.Assert(cmp.Size <= 0, "this is not a tag");
		Set(id, in cmp);
	}

    public ref T Set<T>(EcsID id, T component) where T : struct
	{
        EcsAssert.Assert(World.Exists(id));

        ref readonly var cmp = ref World.Component<T>();
        EcsAssert.Assert(cmp.Size > 0);

        if (World.Has(id, in cmp))
        {
            ref var value = ref World.Get<T>(id);
            value = component;

            return ref value;
        }

		ref var objRef = ref Set(id, in cmp);
		if (!Unsafe.IsNullRef(ref objRef))
		{
			objRef = component;
		}

		return ref Unsafe.Unbox<T>(objRef);
    }

    private ref object Set(EcsID id, ref readonly ComponentInfo cmp)
    {
        EcsAssert.Assert(World.Exists(id));

        ref var set = ref _set.CreateNew(out _);
        set.Entity = id;
        set.Component = cmp;
		set.Data = null!;

        if (cmp.Size > 0)
        {
			var array = Lookup.GetArray(cmp.ID, 1);
			set.Data = array!.GetValue(0)!;
        }

		return ref set.Data;
    }

    public void Unset<T>(EcsID id) where T : struct
	{
        EcsAssert.Assert(World.Exists(id));

        ref var unset = ref _unset.CreateNew(out _);
        unset.Entity = id;
        unset.Component = World.Component<T>();
    }

    public ref T Get<T>(EcsID entity) where T : struct
	{
        EcsAssert.Assert(World.Exists(entity));

        if (World.Has<T>(entity))
        {
            return ref World.Get<T>(entity);
        }

        Unsafe.SkipInit<T>(out var cmp);

        return ref Set(entity, cmp);
    }

    public bool Has<T>(EcsID entity) where T : struct
	{
        EcsAssert.Assert(World.Exists(entity));

        return World.Has<T>(entity);
    }

    public void Merge()
    {
        if (_despawn.Length == 0 && _set.Length == 0 && _unset.Length == 0)
        {
            return;
        }

        foreach (ref var set in _set)
        {
            EcsAssert.Assert(World.Exists(set.Entity));

            ref var record = ref World.GetRecord(set.Entity);
            var array = World.Set(World.Entity(set.Entity), ref record, in set.Component);
            array?.SetValue(set.Data, record.Row & Archetype.CHUNK_THRESHOLD);

            set.Data = null!;
        }

        foreach (ref var unset in _unset)
        {
            EcsAssert.Assert(World.Exists(unset.Entity));

            World.DetachComponent(unset.Entity, ref unset.Component);
        }

        foreach (ref var despawn in _despawn)
        {
            EcsAssert.Assert(World.Exists(despawn));

            World.Delete(despawn);
        }

        Clear();
    }

    public void Clear()
    {
        _set.Clear();
        _unset.Clear();
        _despawn.Clear();
    }

	private struct SetComponent
    {
        public EcsID Entity;
        public ComponentInfo Component;
        public object Data;
    }

    private struct UnsetComponent
    {
        public EcsID Entity;
        public ComponentInfo Component;
    }
}

public readonly ref struct CommandEntityView
{
    private readonly EcsID _id;
    private readonly Commands _cmds;

    internal CommandEntityView(Commands cmds, EcsID id)
    {
        _cmds = cmds;
        _id = id;
    }

    public readonly EcsID ID => _id;

    public readonly CommandEntityView Set<T>(T cmp) where T : struct
	{
        _cmds.Set(_id, cmp);
        return this;
    }

    public readonly CommandEntityView Set<T>() where T : struct
	{
        _cmds.Set<T>(_id);
        return this;
    }

    public readonly CommandEntityView Unset<T>() where T : struct
	{
        _cmds.Unset<T>(_id);
        return this;
    }

    public readonly CommandEntityView Delete()
    {
        _cmds.Delete(_id);
        return this;
    }

    public readonly ref T Get<T>() where T : struct
	{
        return ref _cmds.Get<T>(_id);
    }

    public readonly bool Has<T>() where T : struct
	{
        return _cmds.Has<T>(_id);
    }
}


public sealed partial class World
{
	private readonly ConcurrentQueue<DeferredOp> _operations = new();
    private readonly ConcurrentDictionary<EcsID, DictionarySlim<EcsID, object?>> _deferredSets = new();
	private WorldState _worldState;


	public bool IsDeferred => _worldState.State == WorldStateTypes.Deferred;

	internal void Lock()
	{
		_worldState.State = WorldStateTypes.Deferred;
		Interlocked.Increment(ref _worldState.Locks);
	}

	internal void Unlock()
	{
		Interlocked.Decrement(ref _worldState.Locks);

		if (_worldState.Locks == 0)
		{
			_worldState.NewEntities = 0;
			_worldState.State = WorldStateTypes.Normal;
			Merge();
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
			return ref Unsafe.Unbox<T>(obj);
		}

		return ref Unsafe.NullRef<T>();
	}


	private void Merge()
	{
		var done = 0;

		while (_operations.TryDequeue(out var op))
		{
			done += 1;
			switch (op.Op)
			{
				// case DeferredOpTypes.CreateEntity:
				// 	NewEmpty(op.Entity);
				// 	break;
				case DeferredOpTypes.DestroyEntity:
					Delete(op.Entity);
					break;
				case DeferredOpTypes.SetComponent:
					ref var record = ref GetRecord(op.Entity);
					var array = Set(Entity(op.Entity), ref record, in op.ComponentInfo);
					array?.SetValue(op.Data, record.Row & Archetype.CHUNK_THRESHOLD);
					break;
				case DeferredOpTypes.UnsetComponent:
					DetachComponent(op.Entity, in op.ComponentInfo);
					break;
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
		CreateEntity,
		DestroyEntity,
		SetComponent,
		UnsetComponent
	}
}

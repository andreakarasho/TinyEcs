using System.Numerics;

namespace TinyEcs;

public sealed class Commands
{
    private readonly World _main;
    private readonly EntitySparseSet<EcsID> _despawn;
    private readonly EntitySparseSet<SetComponent> _set;
    private readonly EntitySparseSet<UnsetComponent> _unset;

    public Commands(World main)
    {
        _main = main;
        _despawn = new();
        _set = new();
        _unset = new();
    }

    internal World World => _main;

    public CommandEntityView Entity(EcsID id = default)
    {
        if (id == 0 || !_main.Exists(id))
            id = _main.NewEmpty(id);

        return new CommandEntityView(this, id);
    }

    public void Delete(EcsID id)
    {
        EcsAssert.Assert(_main.Exists(id));

        ref var entity = ref _despawn.Get(id);
        if (Unsafe.IsNullRef(ref entity))
        {
            _despawn.Add(id, id);
        }
    }

    public unsafe void Set(EcsID id, EcsID tag)
    {
        EcsAssert.Assert(!IDOp.IsPair(tag));

        if (_main.Exists(tag) && Has<EcsComponent>(tag))
        {
            ref readonly var cmp2 = ref _main.Component<EcsComponent>();
            Set(id, in cmp2);
            return;
        }

        var cmp = new EcsComponent(tag, 0, string.Empty);
        Set(id, ref cmp);
    }

    public unsafe void Set<T>(EcsID id) 
    {
        ref readonly var cmp = ref _main.Component<T>();
		EcsAssert.Assert(cmp.Size <= 0, "this is not a tag");
		Set(id, in cmp);
	}

    public unsafe ref T Set<T>(EcsID id, T component) 
    {
        EcsAssert.Assert(_main.Exists(id));

        ref readonly var cmp = ref _main.Component<T>();
        EcsAssert.Assert(cmp.Size > 0);

        if (_main.Has(id, in cmp))
        {
            ref var value = ref _main.Get<T>(id);
            value = component;

            return ref value;
        }

		ref var objRef = ref Set(id, in cmp);
		if (!Unsafe.IsNullRef(ref objRef))
		{
			objRef = component;
		}

		return ref Unsafe.As<object, T>(ref objRef);
    }

    private unsafe ref object Set(EcsID id, ref readonly EcsComponent cmp)
    {
        EcsAssert.Assert(_main.Exists(id));

        ref var set = ref _set.CreateNew(out _);
        set.Entity = id;
        set.Component = cmp.ID;

        if (set.DataLength < cmp.Size)
        {
			var array = Lookup.GetArray(cmp.ID, 1);
			set.Data = array!.GetValue(0)!;
            set.DataLength = cmp.Size;
        }

		return ref set.Data;
    }

    public void Unset<T>(EcsID id) 
    {
        EcsAssert.Assert(_main.Exists(id));

        ref readonly var cmp = ref _main.Component<T>();
        ref var unset = ref _unset.CreateNew(out _);
        unset.Entity = id;
        unset.Component = cmp.ID;
        unset.ComponentSize = cmp.Size;
    }

    public ref T Get<T>(EcsID entity) 
    {
        EcsAssert.Assert(_main.Exists(entity));

        if (_main.Has<T>(entity))
        {
            return ref _main.Get<T>(entity);
        }

        Unsafe.SkipInit<T>(out var cmp);

        return ref Set(entity, cmp);
    }

    public bool Has<T>(EcsID entity) 
    {
        EcsAssert.Assert(_main.Exists(entity));

        return _main.Has<T>(entity);
    }

    public unsafe void Merge()
    {
        if (_despawn.Length == 0 && _set.Length == 0 && _unset.Length == 0)
        {
            return;
        }

        foreach (ref var set in _set)
        {
            EcsAssert.Assert(_main.Exists(set.Entity));

            var cmp = new EcsComponent(set.Component, set.DataLength, string.Empty);
            _main.Set(
                set.Entity,
                in cmp,
				in set.Data
            );

            set.Data = null;
            set.DataLength = 0;
        }

        foreach (ref var unset in _unset)
        {
            EcsAssert.Assert(_main.Exists(unset.Entity));

            var cmp = new EcsComponent(unset.Component, unset.ComponentSize, string.Empty);
            _main.DetachComponent(unset.Entity, ref cmp);
        }

        foreach (ref var despawn in _despawn)
        {
            EcsAssert.Assert(_main.Exists(despawn));

            _main.Delete(despawn);
        }

        Clear();
    }

    public void Clear()
    {
        _set.Clear();
        _unset.Clear();
        _despawn.Clear();
    }

    private unsafe struct SetComponent
    {
        public EcsID Entity;
        public EcsID Component;
        public object Data;
        public int DataLength;
    }

    private struct UnsetComponent
    {
        public EcsID Entity;
        public EcsID Component;
        public int ComponentSize;
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

    public readonly CommandEntityView Set<T>(T cmp) 
    {
        _cmds.Set(_id, cmp);
        return this;
    }

    public readonly CommandEntityView Set<T>() 
    {
        _cmds.Set<T>(_id);
        return this;
    }

    public readonly CommandEntityView Set(EcsID id)
    {
        _cmds.Set(_id, id);
        return this;
    }
    public readonly CommandEntityView Unset<T>() 
    {
        _cmds.Unset<T>(_id);
        return this;
    }

    public readonly CommandEntityView Delete()
    {
        _cmds.Delete(_id);
        return this;
    }

    public readonly ref T Get<T>() 
    {
        return ref _cmds.Get<T>(_id);
    }

    public readonly bool Has<T>() 
    {
        return _cmds.Has<T>(_id);
    }
}

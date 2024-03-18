using System.Numerics;

namespace TinyEcs;

public sealed class Commands : ISystemParam
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

    public void Set<T>(EcsID id) where T : struct
	{
        ref readonly var cmp = ref _main.Component<T>();
		EcsAssert.Assert(cmp.Size <= 0, "this is not a tag");
		Set(id, in cmp);
	}

    public ref T Set<T>(EcsID id, T component) where T : struct
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

    private ref object Set(EcsID id, ref readonly ComponentInfo cmp)
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

    public void Unset<T>(EcsID id) where T : struct
	{
        EcsAssert.Assert(_main.Exists(id));

        ref readonly var cmp = ref _main.Component<T>();
        ref var unset = ref _unset.CreateNew(out _);
        unset.Entity = id;
        unset.Component = cmp.ID;
        unset.ComponentSize = cmp.Size;
    }

    public ref T Get<T>(EcsID entity) where T : struct
	{
        EcsAssert.Assert(_main.Exists(entity));

        if (_main.Has<T>(entity))
        {
            return ref _main.Get<T>(entity);
        }

        Unsafe.SkipInit<T>(out var cmp);

        return ref Set(entity, cmp);
    }

    public bool Has<T>(EcsID entity) where T : struct
	{
        EcsAssert.Assert(_main.Exists(entity));

        return _main.Has<T>(entity);
    }

    public void Merge()
    {
        if (_despawn.Length == 0 && _set.Length == 0 && _unset.Length == 0)
        {
            return;
        }

        foreach (ref var set in _set)
        {
            EcsAssert.Assert(_main.Exists(set.Entity));

            var cmp = new ComponentInfo(set.Component, set.DataLength);

            ref var record = ref _main.GetRecord(set.Entity);
            var array = _main.Set(_main.Entity(set.Entity), ref record, in cmp);
            array?.SetValue(set.Data, record.Row % record.GetChunk().Count);

            set.Data = null!;
            set.DataLength = 0;
        }

        foreach (ref var unset in _unset)
        {
            EcsAssert.Assert(_main.Exists(unset.Entity));

            var cmp = new ComponentInfo(unset.Component, unset.ComponentSize);
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

    private struct SetComponent
    {
        public EcsID Entity;
        public ulong Component;
        public object Data;
        public int DataLength;
    }

    private struct UnsetComponent
    {
        public EcsID Entity;
        public ulong Component;
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

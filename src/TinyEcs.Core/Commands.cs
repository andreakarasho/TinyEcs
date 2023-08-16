using System.Numerics;

namespace TinyEcs;

public sealed class Commands
{
	private readonly World _main;
	private readonly EntitySparseSet<EntityID> _despawn;
	private readonly EntitySparseSet<SetComponent> _set;
	private readonly EntitySparseSet<UnsetComponent> _unset;

	public Commands(World main)
	{
		_main = main;
		_despawn = new();
		_set = new();
		_unset = new();
	}


	public CommandEntityView Entity(EntityID id)
	{
		EcsAssert.Assert(_main.Exists(id));

		return new CommandEntityView(this, id);
	}

	public CommandEntityView Spawn()
	{
		var ent = _main.SpawnEmpty();

		return new CommandEntityView(this, ent.ID)
			.Set<EcsEnabled>();
	}

	public void Despawn(EntityID id)
	{
		EcsAssert.Assert(_main.Exists(id));

		ref var entity = ref _despawn.Get(id);
		if (Unsafe.IsNullRef(ref entity))
		{
			_despawn.Add(id, id);
		}
	}

	public unsafe void Set(EntityID id, EntityID tag)
	{
		if (_main.Exists(tag) && Has<EcsComponent>(tag))
		{
			ref var cmp2 = ref _main.Component<EcsComponent>();
			Set(id, ref cmp2);
			return;
		}

		var cmp = new EcsComponent(tag, 0);
		Set(id, ref cmp);
	}

	public unsafe void Set<T>(EntityID id)
	where T : unmanaged, ITag
	{
		ref var cmp = ref _main.Component<T>();
		Set(id, ref cmp);
	}

	public unsafe ref T Set<T>(EntityID id, T component)
	where T : unmanaged, IComponent
	{
		EcsAssert.Assert(_main.Exists(id));

		ref var cmp = ref _main.Component<T>();
		EcsAssert.Assert(cmp.Size > 0);

		if (_main.Has(id, ref cmp))
		{
			ref var value = ref _main.Get<T>(id);
			value = component;

			return ref value;
		}

		ref var reference = ref Set(id, ref cmp);

		if (!Unsafe.IsNullRef(ref reference))
		{
			fixed (byte* ptr = &reference)
				Unsafe.Copy(ptr, ref component);
		}

		return ref Unsafe.As<byte, T>(ref reference);
	}

	public unsafe void Set(EntityID id, EntityID first, EntityID second)
	{
		var cmpID = IDOp.Pair(first, second);
		if (_main.Exists(cmpID) && Has<EcsComponent>(cmpID))
		{
			ref var cmp2 = ref _main.Get<EcsComponent>(cmpID);
			Set(id, ref cmp2);
			return;
		}

		var cmp = new EcsComponent(cmpID, 0);
		Set(id, ref cmp);
	}

	public unsafe void Set<TKind>(EntityID id, EntityID target)
	where TKind : unmanaged, ITag
	{
		var cmpID = IDOp.Pair(_main.Component<TKind>().ID, target);
		var cmp = new EcsComponent(cmpID, 0);
		Set(id, ref cmp);
	}

	public unsafe void Set<TKind, TTarget>(EntityID id)
	where TKind : unmanaged, ITag
	where TTarget : unmanaged, IComponentStub
	{
		Set(id, _main.Component<TKind>().ID,  _main.Component<TTarget>().ID);
	}

	private unsafe ref byte Set(EntityID id, ref EcsComponent cmp)
	{
		EcsAssert.Assert(_main.Exists(id));

		ref var set = ref _set.CreateNew(out _);
		set.Entity = id;
		set.Component = cmp;

		if (set.Data.Length < set.Component.Size)
		{
			set.Data = new byte[Math.Max(sizeof(ulong), (int) BitOperations.RoundUpToPowerOf2((uint) set.Component.Size))];
		}

		return ref MemoryMarshal.GetReference(set.Data.Span.Slice(0, set.Component.Size));
	}

	public void Unset<T>(EntityID id) where T : unmanaged, IComponentStub
	{
		EcsAssert.Assert(_main.Exists(id));

		ref var cmp = ref _main.Component<T>();

		ref var unset = ref _unset.CreateNew(out _);
		unset.Entity = id;
		unset.Component = cmp;
	}

	public void Unset<TKind, TTarget>(EntityID id)
	where TKind : unmanaged, ITag
	where TTarget : unmanaged, IComponentStub
	{
		EcsAssert.Assert(_main.Exists(id));

		var cmpID = _main.Pair<TKind, TTarget>();

		if (_main.Exists(cmpID) && _main.Has<EcsComponent>(cmpID))
		{
			ref var cmp2 = ref _main.Get<EcsComponent>(cmpID);
			ref var unset2 = ref _unset.CreateNew(out _);
			unset2.Entity = id;
			unset2.Component = cmp2;
			return;
		}

		var cmp = new EcsComponent(cmpID, 0);
		ref var unset = ref _unset.CreateNew(out _);
		unset.Entity = id;
		unset.Component = cmp;
	}

	public ref T Get<T>(EntityID entity)
	where T : unmanaged, IComponent
	{
		EcsAssert.Assert(_main.Exists(entity));

		if (_main.Has<T>(entity))
		{
			return ref _main.Get<T>(entity);
		}

		Unsafe.SkipInit<T>(out var cmp);

		return ref Set(entity, cmp);
	}

	public bool Has<T>(EntityID entity) where T : unmanaged, IComponentStub
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

			_main.Set(set.Entity, ref set.Component, set.Component.Size <= 0 ? ReadOnlySpan<byte>.Empty : set.Data.Span.Slice(0, set.Component.Size));
		}

		foreach (ref var unset in _unset)
		{
			EcsAssert.Assert(_main.Exists(unset.Entity));

			_main.DetachComponent(unset.Entity, ref unset.Component);
		}

		foreach (ref var despawn in _despawn)
		{
			_main.Despawn(despawn);
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
		public EntityID Entity;
		public EcsComponent Component;
		public Memory<byte> Data;
	}

	private struct UnsetComponent
	{
		public EntityID Entity;
		public EcsComponent Component;
	}
}

public readonly ref struct CommandEntityView
{
	private readonly EntityID _id;
	private readonly Commands _cmds;

	internal CommandEntityView(Commands cmds, EntityID id)
	{
		_cmds = cmds;
		_id = id;
	}

	public readonly EntityID ID => _id;


	public readonly CommandEntityView Set<T>(T cmp = default) where T : unmanaged, IComponent
	{
		_cmds.Set(_id, cmp);
		return this;
	}

	public readonly CommandEntityView Set<T>() where T : unmanaged, ITag
	{
		_cmds.Set<T>(_id);
		return this;
	}

	public readonly CommandEntityView Set(EntityID id)
	{
		_cmds.Set(_id, id);
		return this;
	}

	public readonly CommandEntityView Set(EntityID first, EntityID second)
	{
		_cmds.Set(_id, first, second);
		return this;
	}

	public readonly CommandEntityView Set<TKind>(EntityID target)
	where TKind : unmanaged, ITag
	{
		_cmds.Set<TKind>(_id, target);
		return this;
	}

	public readonly CommandEntityView Set<TKind, TTarget>()
		where TKind : unmanaged, ITag
		where TTarget : unmanaged, IComponentStub
	{
		_cmds.Set<TKind, TTarget>(_id);
		return this;
	}

	public readonly CommandEntityView Unset<T>() where T : unmanaged, IComponentStub
	{
		_cmds.Unset<T>(_id);
		return this;
	}

	public readonly CommandEntityView Unset<TKind, TTarget>()
		where TKind : unmanaged, ITag
		where TTarget : unmanaged, IComponentStub
	{
		_cmds.Unset<TKind, TTarget>(_id);
		return this;
	}

	public readonly CommandEntityView Despawn()
	{
		_cmds.Despawn(_id);
		return this;
	}

	public readonly ref T Get<T>() where T : unmanaged, IComponent
	{
		return ref _cmds.Get<T>(_id);
	}

	public readonly bool Has<T>() where T : unmanaged, IComponentStub
	{
		return _cmds.Has<T>(_id);
	}

	public readonly CommandEntityView ChildOf(EntityID parent)
	{
		Set<EcsChildOf>(parent);
		return this;
	}
}

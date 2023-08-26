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
			ref var cmp2 = ref _main.Component<EcsComponent>();
			Set(id, ref cmp2);
			return;
		}

		var cmp = new EcsComponent(tag, 0);
		Set(id, ref cmp);
	}

	public unsafe void Set<T>(EcsID id)
	where T : unmanaged, ITag
	{
		ref var cmp = ref _main.Component<T>();
		Set(id, ref cmp);
	}

	public unsafe ref T Set<T>(EcsID id, T component)
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

	public unsafe void Set(EcsID id, EcsID first, EcsID second)
	{
		var cmpID = IDOp.Pair(first, second);
		var cmp = new EcsComponent(cmpID, 0);
		Set(id, ref cmp);
	}

	public unsafe void Set<TKind>(EcsID id, EcsID target)
	where TKind : unmanaged, ITag
	{
		var cmpID = _main.Pair<TKind>(target);
		var cmp = new EcsComponent(cmpID, 0);
		Set(id, ref cmp);
	}

	public unsafe void Set<TKind, TTarget>(EcsID id)
	where TKind : unmanaged, ITag
	where TTarget : unmanaged, IComponentStub
	{
		Set(id, _main.Component<TKind>().ID,  _main.Component<TTarget>().ID);
	}

	private unsafe ref byte Set(EcsID id, ref EcsComponent cmp)
	{
		EcsAssert.Assert(_main.Exists(id));

		ref var set = ref _set.CreateNew(out _);
		set.Entity = id;
		set.Component = cmp.ID;

		if (set.DataLength < cmp.Size)
		{
			set.Data = (byte*) NativeMemory.Realloc(set.Data, (nuint) cmp.Size);
			set.DataLength = cmp.Size;
		}

		return ref (cmp.Size <= 0 ? ref Unsafe.NullRef<byte>() : ref set.Data[0]);
	}

	public void Unset<T>(EcsID id)
	where T : unmanaged, IComponentStub
	{
		EcsAssert.Assert(_main.Exists(id));

		ref var cmp = ref _main.Component<T>();
		ref var unset = ref _unset.CreateNew(out _);
		unset.Entity = id;
		unset.Component = cmp.ID;
		unset.ComponentSize = cmp.Size;
	}

	public void Unset<TKind, TTarget>(EcsID id)
	where TKind : unmanaged, ITag
	where TTarget : unmanaged, IComponentStub
	{
		EcsAssert.Assert(_main.Exists(id));

		var cmpID = _main.Pair<TKind, TTarget>();

		var cmp = new EcsComponent(cmpID, 0);
		ref var unset = ref _unset.CreateNew(out _);
		unset.Entity = id;
		unset.Component = cmp.ID;
		unset.ComponentSize = cmp.Size;
	}

	public ref T Get<T>(EcsID entity)
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

	public bool Has<T>(EcsID entity)
	where T : unmanaged, IComponentStub
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

			var cmp = new EcsComponent(set.Component, set.DataLength);
			_main.Set(set.Entity, ref cmp, set.DataLength <= 0 ? ReadOnlySpan<byte>.Empty : new ReadOnlySpan<byte>(set.Data, set.DataLength));

			NativeMemory.Free(set.Data);
			set.Data = null;
			set.DataLength = 0;
		}

		foreach (ref var unset in _unset)
		{
			EcsAssert.Assert(_main.Exists(unset.Entity));

			var cmp = new EcsComponent(unset.Component, unset.ComponentSize);
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
		public byte* Data;
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


	public readonly CommandEntityView Set<T>(T cmp = default)
	where T : unmanaged, IComponent
	{
		_cmds.Set(_id, cmp);
		return this;
	}

	public readonly CommandEntityView Set<T>()
	where T : unmanaged, ITag
	{
		_cmds.Set<T>(_id);
		return this;
	}

	public readonly CommandEntityView Set(EcsID id)
	{
		_cmds.Set(_id, id);
		return this;
	}

	public readonly CommandEntityView Set(EcsID first, EcsID second)
	{
		_cmds.Set(_id, first, second);
		return this;
	}

	public readonly CommandEntityView Set<TKind>(EcsID target)
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

	public readonly CommandEntityView Unset<T>()
	where T : unmanaged, IComponentStub
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

	public readonly CommandEntityView Delete()
	{
		_cmds.Delete(_id);
		return this;
	}

	public readonly ref T Get<T>()
	 where T : unmanaged, IComponent
	{
		return ref _cmds.Get<T>(_id);
	}

	public readonly bool Has<T>()
	where T : unmanaged, IComponentStub
	{
		return _cmds.Has<T>(_id);
	}

	public readonly CommandEntityView ChildOf(EcsID parent)
		=> Set<EcsChildOf>(parent);
}

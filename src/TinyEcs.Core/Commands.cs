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
		EcsAssert.Assert(_main.IsAlive(id));

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
		EcsAssert.Assert(_main.IsAlive(id));

		ref var entity = ref _despawn.Get(id);
		if (Unsafe.IsNullRef(ref entity))
		{
			_despawn.Add(id, id);
		}
	}

	public unsafe ref T Set<T>(EntityID id, T cmp = default) where T : unmanaged
	{
		EcsAssert.Assert(_main.IsAlive(id));

		var cmpID = _main.Component<T>();
		if (_main.Has(id, cmpID))
		{
			ref var value = ref _main.Get<T>(id);
			value = cmp;

			return ref value;
		}

		ref var set = ref _set.CreateNew(out _);
		set.Entity = id;
		set.ComponentID = cmpID;
		set.Size = sizeof(T);

		if (set.Data.Length < set.Size)
		{
			set.Data = new byte[Math.Max(sizeof(ulong), (int) BitOperations.RoundUpToPowerOf2((uint) set.Size))];
		}

		ref var reference = ref MemoryMarshal.GetReference(set.Data.Span.Slice(0, set.Size));
		fixed (byte* ptr = &reference)
			Unsafe.Copy(ptr, ref cmp);

		return ref Unsafe.As<byte, T>(ref reference);
	}

	public unsafe void Add(EntityID id, EntityID cmp)
	{
		EcsAssert.Assert(_main.IsAlive(id));

		if (_main.Has(id, cmp))
		{
			return;
		}

		ref var set = ref _set.CreateNew(out _);
		set.Entity = id;
		set.ComponentID = cmp;
		set.Size = 0;

		if (set.Data.Length < set.Size)
		{
			set.Data = new byte[Math.Max(sizeof(ulong), (int) BitOperations.RoundUpToPowerOf2((uint) set.Size))];
		}
	}

	public unsafe void Add(EntityID id, EntityID first, EntityID second)
	{
		Add(id, IDOp.Pair(first, second));
	}

	public unsafe void Add<TKind, TTarget>(EntityID id) where TKind : unmanaged where TTarget : unmanaged
	{
		Add(id, _main.Component<TKind>(),  _main.Component<TTarget>());
	}

	public void Unset<T>(EntityID id) where T : unmanaged
	{
		EcsAssert.Assert(_main.IsAlive(id));

		var cmpID = _main.Component<T>();

		ref var unset = ref _unset.CreateNew(out _);
		unset.Entity = id;
		unset.ComponentID = cmpID;
	}

	public ref T Get<T>(EntityID entity) where T : unmanaged
	{
		EcsAssert.Assert(_main.IsAlive(entity));

		if (_main.Has<T>(entity))
		{
			return ref _main.Get<T>(entity);
		}

		Unsafe.SkipInit<T>(out var cmp);

		return ref Set(entity, cmp);
	}

	public bool Has<T>(EntityID entity) where T : unmanaged
	{
		EcsAssert.Assert(_main.IsAlive(entity));

		return _main.Has<T>(entity);
	}


	public void Merge()
	{
		if (_despawn.Length == 0 && _set.Length == 0 && _unset.Length == 0)
		{
			return;
		}

		foreach (ref readonly var set in _set)
		{
			EcsAssert.Assert(_main.IsAlive(set.Entity));

			_main.Set(set.Entity, set.ComponentID, set.Data.Span.Slice(0, set.Size));
		}

		foreach (ref readonly var unset in _unset)
		{
			EcsAssert.Assert(_main.IsAlive(unset.Entity));

			_main.DetachComponent(unset.Entity, unset.ComponentID);
		}

		foreach (ref readonly var despawn in _despawn)
		{
			_main.Despawn(despawn);
		}


		_set.Clear();
		_unset.Clear();
		_despawn.Clear();
	}

	private struct SetComponent
	{
		public EntityID Entity;
		public EntityID ComponentID;
		public int Size;
		public Memory<byte> Data;
	}

	private struct UnsetComponent
	{
		public EntityID Entity;
		public EntityID ComponentID;
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

	public readonly CommandEntityView Set<T>(T cmp = default) where T : unmanaged
	{
		_cmds.Set(_id, cmp);
		return this;
	}

	public readonly CommandEntityView Add(EntityID id)
	{
		_cmds.Add(_id, id);
		return this;
	}

	public readonly CommandEntityView Add(EntityID first, EntityID second)
	{
		_cmds.Add(_id, first, second);
		return this;
	}

	public readonly CommandEntityView Add<TKind, TTarget>()
		where TKind : unmanaged
		where TTarget : unmanaged
	{
		_cmds.Add<TKind, TTarget>(_id);
		return this;
	}

	public readonly CommandEntityView Unset<T>() where T : unmanaged
	{
		_cmds.Unset<T>(_id);
		return this;
	}

	public readonly CommandEntityView Despawn()
	{
		_cmds.Despawn(_id);
		return this;
	}

	public readonly ref T Get<T>() where T : unmanaged
	{
		return ref _cmds.Get<T>(_id);
	}

	public readonly bool Has<T>() where T : unmanaged
	{
		return _cmds.Has<T>(_id);
	}
}

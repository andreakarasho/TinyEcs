namespace TinyEcs;

public sealed partial class World
{
	/// <summary>
	/// Add a Tag to the entity.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="entity"></param>
    public void Add<T>(EcsID entity) where T : struct
	{
        ref readonly var cmp = ref Component<T>();
        EcsAssert.Panic(cmp.Size <= 0, "this is not a tag");

		if (IsDeferred && !Has(entity, cmp.ID))
		{
			SetDeferred<T>(entity);

			return;
		}

		BeginDeferred();
        _ = AttachComponent(entity, cmp.ID, cmp.Size);
		EndDeferred();
    }

	/// <summary>
	/// Set a Component to the entity.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="entity"></param>
	/// <param name="component"></param>
    [SkipLocalsInit]
    public void Set<T>(EcsID entity, T component) where T : struct
	{
		ref readonly var cmp = ref Component<T>();
        EcsAssert.Panic(cmp.Size > 0, "this is not a component");

		if (IsDeferred && !Has(entity, cmp.ID))
		{
			SetDeferred(entity, component);

			return;
		}

		BeginDeferred();
        (var raw, var row) = AttachComponent(entity, cmp.ID, cmp.Size);
        ref var array = ref Unsafe.As<Array, T[]>(ref raw!);
        array[row & Archetype.CHUNK_THRESHOLD] = component;
		EndDeferred();
	}

	/// <summary>
	/// Add a Tag to the entity. Tag is an entity.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="id"></param>
	public void Add(EcsID entity, EcsID id)
	{
		if (IsDeferred && !Has(entity, id))
		{
			SetDeferred(entity, id, null, 0);

			return;
		}

		BeginDeferred();
		_ = AttachComponent(entity, id, 0);
		EndDeferred();
	}

	/// <summary>
	/// Remove a component or a tag from the entity.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="entity"></param>
    public void Unset<T>(EcsID entity) where T : struct
		=> Unset(entity, Component<T>().ID);

	/// <summary>
	/// Remove a component Id or a tag Id from the entity.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="id"></param>
	public void Unset(EcsID entity, EcsID id)
	{
		if (IsDeferred)
		{
			UnsetDeferred(entity, id);

			return;
		}

		BeginDeferred();
		DetachComponent(entity, id);
		EndDeferred();
	}

	/// <summary>
	/// Check if the entity has a component or tag.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="entity"></param>
	/// <returns></returns>
    public bool Has<T>(EcsID entity) where T : struct
		=> Exists(entity) && Has(entity, Component<T>().ID);

	/// <summary>
	/// Get a component from the entity.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="entity"></param>
	/// <returns></returns>
    public ref T Get<T>(EcsID entity) where T : struct
	{
		ref readonly var cmp = ref Component<T>();
		return ref GetUntrusted<T>(entity, cmp.ID, cmp.Size);
    }

	internal ref T GetUntrusted<T>(EcsID entity, EcsID cmpId, int size) where T : struct
	{
		if (IsDeferred && !Has(entity, cmpId))
		{
			Unsafe.SkipInit<T>(out var val);
			return ref Unsafe.Unbox<T>(SetDeferred(entity, cmpId, val, size)!);
		}

		BeginDeferred();
        ref var record = ref GetRecord(entity);
        var column = record.Archetype.GetComponentIndex(cmpId);

		if (column < 0)
		{
			EndDeferred();
			return ref Unsafe.NullRef<T>();
		}

        ref var chunk = ref record.GetChunk();
		var raw = chunk.RawComponentData(column)!;
		ref var array = ref Unsafe.As<Array, T[]>(ref raw);
		ref var value = ref array[record.Row & Archetype.CHUNK_THRESHOLD];
		EndDeferred();

		return ref value;
    }

	/// <summary>
	/// Execute a deferred action.
	/// </summary>
	/// <param name="fn"></param>
	public void Deferred(Action<World> fn)
	{
		BeginDeferred();
		fn(this);
		EndDeferred();
	}
}

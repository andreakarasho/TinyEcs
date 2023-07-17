namespace TinyEcs;

static class TypeInfo<T> where T : unmanaged
{
	private static EntityID _id;
	private static readonly EntitySparseSet<EntityID> _ids = new();

	public static unsafe readonly int Size = sizeof(T);

	public static EntityID GetID(World world, EntityID id = 0)
	{
		Debug.Assert(world != null);

		ref var cmpID = ref _ids.Get(world.ID);
		if (Unsafe.IsNullRef(ref cmpID) || !world.IsAlive(cmpID))
		{
			var ent = world.SpawnEmpty();
			cmpID = ref _ids.Add(world.ID, ent.ID);
			world.Set(cmpID, new EcsComponent(cmpID, Size));
			world.Set<EcsEnabled>(cmpID);
		}

		return cmpID;

		// if (_id != 0)
		// {
		// 	Debug.Assert(id == 0 || _id == id);
		// }

		// if (_id == 0 || !world.IsAlive(_id))
		// {
		// 	Init(world, _id != 0 ? _id : id);

		// 	Debug.Assert(id == 0 || _id == id);

		// 	_id = world.SpawnEmpty();
		// 	world.Set(_id, new EcsComponent(_id, Size));
		// 	world.Set<EcsEnabled>(_id);
		// }

		// return _id;
	}

	public static void Init(World world, EntityID id)
	{
		if (_id == 0)
		{
			Debug.Assert(_id == id);
		}

		_id = id;
	}
}


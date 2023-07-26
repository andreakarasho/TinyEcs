namespace TinyEcs;

static class TypeInfo<T> where T : unmanaged
{
	private static readonly object _lock = new();
	private static readonly EntitySparseSet<EntityID> _ids = new();

	public static unsafe readonly int Size = sizeof(T);

	public static EntityID GetID(World world)
	{
		EcsAssert.Assert(world != null);

		lock (_lock)
		{
			ref var cmpID = ref _ids.Get(world!.ID);
			if (Unsafe.IsNullRef(ref cmpID) || !world.IsAlive(cmpID))
			{
				var ent = world.SpawnEmpty();
				cmpID = ref _ids.Add(world.ID, ent.ID);
				world.Set(cmpID, new EcsComponent(cmpID, Size));
				world.Set<EcsEnabled>(cmpID);
			}

			return cmpID;
		}
	}
}


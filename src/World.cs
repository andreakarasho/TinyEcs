using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Collections.Extensions;

namespace TinyEcs;

public sealed partial class World : IDisposable
{
	internal delegate Query QueryFactoryDel(World world, ReadOnlySpan<IQueryTerm> terms);

    private readonly Archetype _archRoot;
    private readonly EntitySparseSet<EcsRecord> _entities = new ();
    private readonly FastIdLookup<Archetype> _typeIndex = new ();
    private readonly ComponentComparer _comparer;
	private readonly EcsID _maxCmpId;
	private readonly Dictionary<EcsID, Query> _cachedQueries = new ();
	private readonly FastIdLookup<EcsID> _cachedComponents = new ();
	private readonly Dictionary<string, EcsID> _names = new ();
	private readonly object _newEntLock = new ();

	private static readonly Comparison<ComponentInfo> _comparisonCmps = (a, b)
		=> ComponentComparer.CompareTerms(null!, a.ID, b.ID);
	private static readonly Comparison<EcsID> _comparisonIds = (a, b)
		=> ComponentComparer.CompareTerms(null!, a, b);



	internal Archetype Root => _archRoot;
    internal List<Archetype> Archetypes { get; } = [];


	internal ref EcsRecord NewId(out EcsID newId, ulong id = 0)
	{
		ref var record = ref (
			id > 0 ?
			ref _entities.Add(id, default!)
			:
			ref _entities.CreateNew(out id)
		);

		newId = id;
		return ref record;
	}

	public void Each(QueryFilterDelegateWithEntity fn)
	{
		BeginDeferred();

		foreach (var arch in GetQuery(0, [], static (world, terms) => new Query(world, terms)))
		{
			foreach (ref readonly var chunk in arch)
			{
				ref var entity = ref chunk.EntityAt(0);
				ref var last = ref Unsafe.Add(ref entity, chunk.Count);
				while (Unsafe.IsAddressLessThan(ref entity, ref last))
				{
					fn(entity);
					entity = ref Unsafe.Add(ref entity, 1);
				}
			}
		}

		EndDeferred();
	}

	internal ref readonly ComponentInfo Component<T>() where T : struct
	{
        ref readonly var lookup = ref Lookup.Component<T>.Value;

		var isPair = lookup.ID.IsPair();
		EcsAssert.Panic(isPair || lookup.ID < _maxCmpId,
			"Increase the minimum number for components when initializing the world [ex: new World(1024)]");

		if (!isPair /*!Exists(lookup.ID)*/)
		{
			ref var idx = ref _cachedComponents.GetOrCreate(lookup.ID, out var exists);

			if (!exists)
			{
				idx = Entity(lookup.ID).Set(lookup).ID;
			}
		}

		// if (!isPair && !Exists(lookup.ID))
		// {
		// 	Entity(lookup.ID).Set(lookup);
		// }

		return ref lookup;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref EcsRecord GetRecord(EcsID id)
    {
        ref var record = ref _entities.Get(id);
		if (Unsafe.IsNullRef(ref record))
        	EcsAssert.Panic(false, $"entity {id} is dead or doesn't exist!");
        return ref record;
    }

	private void DetachComponent(EcsID entity, EcsID id)
	{
		ref var record = ref GetRecord(entity);
		var oldArch = record.Archetype;

		var column = oldArch.GetComponentIndex(id);
		if (column < 0)
            return;

		if (id.IsPair())
		{
			(var first, var second) = id.Pair();
			if ((record.Flags & EntityFlags.HasName) != 0)
			{
				if (first == Defaults.Identifier.ID && second == Defaults.Name.ID)
				{


					record.Flags &= ~EntityFlags.HasName;
				}
			}

			GetRecord(GetAlive(first)).Flags &= ~EntityFlags.IsAction;
			GetRecord(GetAlive(second)).Flags &= ~EntityFlags.IsTarget;
		}


		OnComponentUnset?.Invoke(record.EntityView(), new ComponentInfo(id, -1, false));

		BeginDeferred();

		var foundArch = oldArch.TraverseLeft(id);
		if (foundArch == null && oldArch.All.Length - 1 <= 0)
		{
			foundArch = _archRoot;
		}

		if (foundArch == null)
		{
			var hash = new RollingHash();
			foreach (ref readonly var cmp in oldArch.All.AsSpan())
			{
				if (cmp.ID != id)
					hash.Add(cmp.ID);
			}

			ref var arch = ref _typeIndex.GetOrCreate(hash.Hash, out var exists);
			if (!exists)
			{
				var arr = new ComponentInfo[oldArch.All.Length - 1];
				for (int i = 0, j = 0; i < oldArch.All.Length; ++i)
				{
					ref readonly var item = ref oldArch.All.ItemRef(i);
					if (item.ID != id)
						arr[j++] = item;
				}

				arch = NewArchetype(oldArch, arr, id);
			}

			foundArch = arch;
		}

		record.Row = record.Archetype.MoveEntity(foundArch!, record.Row, true);
        record.Archetype = foundArch!;
		EndDeferred();
	}

	private (Array?, int) AttachComponent(EcsID entity, EcsID id, int size, bool isManaged)
	{
		ref var record = ref GetRecord(entity);
		var oldArch = record.Archetype;

		var column = oldArch.GetComponentIndex(id);
		if (column >= 0)
            return (size > 0 ? record.GetChunk().Data![column] : null, record.Row);

		if (id.IsPair())
		{
			(var first, var second) = id.Pair();
			GetRecord(GetAlive(first)).Flags |= EntityFlags.IsAction;
			GetRecord(GetAlive(second)).Flags |= EntityFlags.IsTarget;
		}

		BeginDeferred();

		var foundArch = oldArch.TraverseRight(id);
		if (foundArch == null)
		{
			var hash = new RollingHash();

			var found = false;
			foreach (ref readonly var cmp in oldArch.All.AsSpan())
			{
				if (!found && cmp.ID > id)
				{
					hash.Add(id);
					found = true;
				}

				hash.Add(cmp.ID);
			}

			if (!found)
				hash.Add(id);

			ref var arch = ref _typeIndex.GetOrCreate(hash.Hash, out var exists);
			if (!exists)
			{
				var arr = new ComponentInfo[oldArch.All.Length + 1];
				oldArch.All.CopyTo(arr);
				arr[^1] = new ComponentInfo(id, size, isManaged);
				arr.AsSpan().SortNoAlloc(_comparisonCmps);

				arch = NewArchetype(oldArch, arr, id);
			}

			foundArch = arch;
		}

		record.Row = record.Archetype.MoveEntity(foundArch!, record.Row, false);
        record.Archetype = foundArch!;
		EndDeferred();

		OnComponentSet?.Invoke(record.EntityView(), new ComponentInfo(id, size, isManaged));

		return (size > 0 ? record.GetChunk().Data![foundArch!.GetComponentIndex(id)] : null, record.Row);
	}

	private Archetype NewArchetype(Archetype oldArch, ComponentInfo[] sign, EcsID id)
	{
		var archetype = _archRoot.InsertVertex(oldArch, sign, id);
		Archetypes.Add(archetype);
		return archetype;
	}

	internal ref T GetUntrusted<T>(EcsID entity, EcsID cmpId, int size) where T : struct
	{
		if (IsDeferred && !Has(entity, cmpId))
		{
			Unsafe.SkipInit<T>(out var val);
			var isManaged = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
			return ref Unsafe.Unbox<T>(SetDeferred(entity, cmpId, val, size, isManaged)!);
		}

        ref var record = ref GetRecord(entity);
		var column = record.Archetype.GetComponentIndex(cmpId);
        ref var chunk = ref record.GetChunk();
		ref var value = ref Unsafe.Add(ref chunk.GetReference<T>(column), record.Row & TinyEcs.Archetype.CHUNK_THRESHOLD);
		return ref value;
    }

	internal Query GetQuery(EcsID hash, ReadOnlySpan<IQueryTerm> terms, QueryFactoryDel factory)
	{
		if (!_cachedQueries.TryGetValue(hash, out var query))
		{
			query = factory(this, terms);
			_cachedQueries.Add(hash, query);
		}

		query.Match();

		return query;
	}
}

struct EcsRecord
{
	public Archetype Archetype;
    public int Row;
	public EntityFlags Flags;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref ArchetypeChunk GetChunk() => ref Archetype.GetChunk(Row);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref readonly EntityView EntityView() => ref Archetype.GetChunk(Row).EntityAt(Row & TinyEcs.Archetype.CHUNK_THRESHOLD);
}

[Flags]
enum EntityFlags
{
	None = 1 << 0,
	IsAction = 1 << 1,
	IsTarget = 1 << 2,
	IsUnique = 1 << 3,
	IsSymmetric = 1 << 4,
	HasName = 1 << 5
}

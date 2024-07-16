using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Collections.Extensions;

namespace TinyEcs;

public sealed partial class World : IDisposable
{
	internal delegate Query QueryFactoryDel(World world, ReadOnlySpan<IQueryTerm> terms);

    private readonly Archetype _archRoot;
    private readonly EntitySparseSet<EcsRecord> _entities = new();
    private readonly FastIdLookup<Archetype> _typeIndex = new();
    private readonly ComponentComparer _comparer;
	private readonly EcsID _maxCmpId;
	private readonly Dictionary<EcsID, Query> _cachedQueries = new ();
	private readonly object _newEntLock = new object();
	private readonly ConcurrentDictionary<string, EcsID> _namesToEntity = new ();
	private readonly FastIdLookup<EcsID> _cacheIndex = new ();
	private readonly ComponentInfo[] _cache = new ComponentInfo[128];

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
			ref var idx = ref _cacheIndex.GetOrCreate(lookup.ID, out var exists);

			if (!exists)
			{
				idx = Entity(lookup.ID).Set(lookup).ID;
			}
		}

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

		if (oldArch.GetComponentIndex(id) < 0)
            return;

		if (id.IsPair())
		{
			(var first, var second) = id.Pair();
			GetRecord(GetAlive(first)).Flags &= ~EntityFlags.IsAction;
			GetRecord(GetAlive(second)).Flags &= ~EntityFlags.IsTarget;
		}

		OnComponentUnset?.Invoke(record.GetChunk().EntityAt(record.Row), new ComponentInfo(id, -1, false));

		BeginDeferred();
		var roll = new RollingHash();
		foreach (ref readonly var oldId in oldArch.All.AsSpan())
			if (oldId.ID != id)
				roll.Add(oldId.ID);

		Archetype? found = null;
		ref var arch = ref _typeIndex.TryGet(roll.Hash, out var exists);

		if (exists)
		{
			found = arch;
		}

		if (!exists)
		{
			var count = oldArch.All.Length - 1;
			if (count <= 0)
			{
				found = _archRoot;
			}
			else
			{
				ref var newArch = ref GetArchetype(roll.Hash, true);
				if (newArch == null)
				{
					var tmp = _cache;
					for (int i = 0, j = 0; i < oldArch.All.Length; ++i)
					{
						if (oldArch.All[i].ID != id)
							tmp[j++] = oldArch.All[i];
					}

					newArch = _archRoot.InsertVertex(oldArch, tmp.AsSpan(0, oldArch.All.Length -1), id);
					Archetypes.Add(newArch);
				}

				found = newArch;
			}
		}

		record.Row = record.Archetype.MoveEntity(found!, record.Row);
        record.Archetype = found!;
		EndDeferred();
	}

	private (Array?, int) AttachComponent(EcsID entity, EcsID id, int size, bool isManaged)
	{
		ref var record = ref GetRecord(entity);
		var oldArch = record.Archetype;

		var index = oldArch.GetComponentIndex(id);
		if (index >= 0)
            return (size > 0 ? record.GetChunk().RawComponentData(index) : null, record.Row);

		if (id.IsPair())
		{
			(var first, var second) = id.Pair();
			GetRecord(GetAlive(first)).Flags |= EntityFlags.IsAction;
			GetRecord(GetAlive(second)).Flags |= EntityFlags.IsTarget;
		}

		BeginDeferred();

		var roll = new RollingHash();
		var allSpan = oldArch.All.AsSpan();

		var i = 0;
		for (; i < allSpan.Length; ++i)
		{
			ref readonly var cmp = ref allSpan[i];
			if (cmp.ID > id)
				break;

			roll.Add(cmp.ID);
		}

		roll.Add(id);

		for (; i < allSpan.Length; ++i)
		{
			roll.Add(allSpan[i].ID);
		}

		Archetype? found = null;

		ref var archFound = ref _typeIndex.TryGet(roll.Hash, out var exists);

		if (exists)
		{
			found = archFound;
		}

		if (!exists)
		{
			ref var newArch = ref GetArchetype(roll.Hash, true);
			if (newArch == null)
			{
				var tmp = _cache;
				oldArch.All.CopyTo(tmp);
				tmp[oldArch.All.Length] = new ComponentInfo(id, size, isManaged);
				newArch = _archRoot.InsertVertex(oldArch, tmp.AsSpan(0, oldArch.All.Length + 1), id);
				Archetypes.Add(newArch);
			}

			found = newArch;
		}

		record.Row = record.Archetype.MoveEntity(found!, record.Row);
        record.Archetype = found!;
		EndDeferred();

		OnComponentSet?.Invoke(record.GetChunk().EntityAt(record.Row), new ComponentInfo(id, size, isManaged));

		return (size > 0 ? record.GetChunk().RawComponentData(found!.GetComponentIndex(id)) : null, record.Row);
	}

    private ref Archetype? GetArchetype(EcsID hash, bool create)
	{
		ref var arch = ref Unsafe.NullRef<Archetype>();

		if (create)
		{
			arch = ref _typeIndex.GetOrCreate(hash, out _)!;
		}
		else
		{
			arch = ref _typeIndex.TryGet(hash, out _)!;
		}

		return ref arch;
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

	internal Archetype? FindArchetype(EcsID hash)
	{
		ref var arch = ref _typeIndex.TryGet(hash, out var exists);
		if (!exists)
			return _archRoot;
		return arch;
	}

	internal void MatchArchetypes(Archetype root, ReadOnlySpan<IQueryTerm> terms, List<Archetype> matched)
	{
		var result = root.FindMatch(terms);
		if (result < 0)
		{
			return;
		}

		if (result == 0)
		{
			matched.Add(root);
		}

		var add = root._add;
		if (add.Count <= 0)
			return;

		foreach ((var id, var edge) in add)
		{
			MatchArchetypes(edge.Archetype, terms, matched);
		}
	}
}

struct EcsRecord
{
	public Archetype Archetype;
    public int Row;
	public EntityFlags Flags;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref ArchetypeChunk GetChunk() => ref Archetype.GetOrCreateChunk(Row);
}

[Flags]
enum EntityFlags
{
	None = 1 << 0,
	IsAction = 1 << 1,
	IsTarget = 1 << 2,
	IsUnique = 1 << 3
}

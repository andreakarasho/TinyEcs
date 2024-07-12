using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Collections.Extensions;

namespace TinyEcs;

public sealed partial class World : IDisposable
{
	internal delegate Query QueryFactoryDel(World world, ReadOnlySpan<IQueryTerm> terms);

    private readonly Archetype _archRoot;
    private readonly EntitySparseSet<EcsRecord> _entities = new();
    private readonly DictionarySlim<ulong, Archetype> _typeIndex = new();
    private readonly ComponentComparer _comparer;
	private readonly EcsID _maxCmpId;
	private readonly Dictionary<ulong, Query> _cachedQueries = new ();
	private readonly object _newEntLock = new object();
	private readonly ConcurrentDictionary<string, EcsID> _namesToEntity = new ();



	internal Archetype Root => _archRoot;
    internal List<Archetype> Archetypes { get; } = [];



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

		EcsAssert.Panic(lookup.ID.IsPair || lookup.ID < _maxCmpId,
			"Increase the minimum number for components when initializing the world [ex: new World(1024)]");

		if (!lookup.ID.IsPair && !Exists(lookup.ID))
		{
			var e = Entity(lookup.ID)
				.Set(lookup);
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

	public EcsID GetAlive(EcsID id)
	{
		if (Exists(id))
			return id;

		if ((uint)id != id)
			return 0;

		var current = _entities.GetNoGeneration(id);
		if (current == 0)
			return 0;

		if (!Exists(current))
			return 0;

		return current;
	}

	private void DetachComponent(EcsID entity, EcsID id)
	{
		ref var record = ref GetRecord(entity);
		var oldArch = record.Archetype;

		if (oldArch.GetComponentIndex(id) < 0)
            return;

		var cmp = Lookup.GetComponent(id, -1);
		OnComponentUnset?.Invoke(record.GetChunk().EntityAt(record.Row), cmp);

		BeginDeferred();
		var tmp = ArrayPool<ComponentInfo>.Shared.Rent(oldArch.Components.Length - 1);
		var newSign = tmp.AsSpan(0, oldArch.Components.Length - 1);

		for (int i = 0, j = 0; i < oldArch.Components.Length; ++i)
		{
			if (oldArch.Components[i].ID != id)
				newSign[j++] = oldArch.Components[i];
		}

		ref var newArch = ref GetArchetype(newSign, true);
		if (newArch == null)
		{
			newArch = _archRoot.InsertVertex(oldArch, newSign, cmp.ID);
			Archetypes.Add(newArch);
		}

		ArrayPool<ComponentInfo>.Shared.Return(tmp);

		record.Row = record.Archetype.MoveEntity(newArch!, record.Row);
        record.Archetype = newArch!;
		EndDeferred();
	}

	private (Array?, int) AttachComponent(EcsID entity, EcsID id, int size)
	{
		ref var record = ref GetRecord(entity);
		var oldArch = record.Archetype;

		var index = oldArch.GetComponentIndex(id);
		if (index >= 0)
            return (size > 0 ? record.GetChunk().RawComponentData(index) : null, record.Row);

		var cmp = Lookup.GetComponent(id, size);

		BeginDeferred();
		var tmp = ArrayPool<ComponentInfo>.Shared.Rent(oldArch.Components.Length + 1);
		var newSign = tmp.AsSpan(0, oldArch.Components.Length + 1);
		oldArch.Components.CopyTo(newSign);
		newSign[^1] = cmp;
		newSign.Sort(_comparer);

		ref var newArch = ref GetArchetype(newSign, true);
		if (newArch == null)
		{
			newArch = _archRoot.InsertVertex(oldArch, newSign, cmp.ID);
			Archetypes.Add(newArch);
		}

		ArrayPool<ComponentInfo>.Shared.Return(tmp);

		record.Row = record.Archetype.MoveEntity(newArch, record.Row);
        record.Archetype = newArch!;
		EndDeferred();

		OnComponentSet?.Invoke(record.GetChunk().EntityAt(record.Row), cmp);

		return (size > 0 ? record.GetChunk().RawComponentData(newArch.GetComponentIndex(cmp.ID)) : null, record.Row);
	}

    private ref Archetype? GetArchetype(ReadOnlySpan<ComponentInfo> components, bool create)
	{
		var hash = Hashing.Calculate(components);
		ref var arch = ref Unsafe.NullRef<Archetype>();
		if (create)
		{
			arch = ref _typeIndex.GetOrAddValueRef(hash, out var exists)!;
			if (!exists)
			{

			}
		}
		else if (_typeIndex.TryGetValue(hash, out arch))
		{

		}

		return ref arch;
	}

	internal ref T GetUntrusted<T>(EcsID entity, EcsID cmpId, int size) where T : struct
	{
		if (IsDeferred && !Has(entity, cmpId))
		{
			Unsafe.SkipInit<T>(out var val);
			return ref Unsafe.Unbox<T>(SetDeferred(entity, cmpId, val, size)!);
		}

        ref var record = ref GetRecord(entity);
		var column = record.Archetype.GetComponentIndex(cmpId);
        ref var chunk = ref record.GetChunk();
		ref var value = ref Unsafe.Add(ref chunk.GetReference<T>(column), record.Row & Archetype.CHUNK_THRESHOLD);
		return ref value;
    }

	internal Query GetQuery(ulong hash, ReadOnlySpan<IQueryTerm> terms, QueryFactoryDel factory)
	{
		if (!_cachedQueries.TryGetValue(hash, out var query))
		{
			query = factory(this, terms);
			_cachedQueries.Add(hash, query);
		}

		query.Match();

		return query;
	}

	internal Archetype? FindArchetype(ulong hash)
	{
		if (!_typeIndex.TryGetValue(hash, out var arch))
		{
			arch = _archRoot;
		}

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

		var span = CollectionsMarshal.AsSpan(root._edgesRight);
		if (span.IsEmpty)
			return;

		ref var start = ref MemoryMarshal.GetReference(span);
		ref var end = ref Unsafe.Add(ref start, span.Length);

		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			MatchArchetypes(start.Archetype, terms, matched);
			start = ref Unsafe.Add(ref start, 1);
		}
	}
}

struct EcsRecord
{
	public Archetype Archetype;
    public int Row;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref ArchetypeChunk GetChunk() => ref Archetype.GetChunk(Row);
}

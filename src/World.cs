using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Collections.Extensions;

using static TinyEcs.Defaults;

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


	public event Action<EntityView>? OnEntityCreated, OnEntityDeleted;
	public event Action<EntityView, ComponentInfo>? OnComponentSet, OnComponentUnset;
	public static event Action<World>? OnPluginInitialization;


	internal Archetype Root => _archRoot;
    internal List<Archetype> Archetypes { get; } = new List<Archetype>();



	public void Each(QueryFilterDelegateWithEntity fn)
	{
		BeginDeferred();

		foreach (var arch in GetQuery(0, ReadOnlySpan<IQueryTerm>.Empty, static (world, terms) => new Query(world, terms)))
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

    internal EntityView NewEmpty(ulong id = 0)
    {
		lock (_newEntLock)
		{
			// if (IsDeferred)
			// {
			// 	if (id == 0)
			// 		id = ++_entities.MaxID;
			// 	CreateDeferred(id);
			// 	return new EntityView(this, id);
			// }

			ref var record = ref (
				id > 0 ? ref _entities.Add(id, default!) : ref _entities.CreateNew(out id)
			);
			record.Archetype = _archRoot;
			record.Row = _archRoot.Add(id);

			var e = new EntityView(this, id);

			OnEntityCreated?.Invoke(e);

			return e;
		}
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref EcsRecord GetRecord(EcsID id)
    {
        ref var record = ref _entities.Get(id);
        EcsAssert.Assert(!Unsafe.IsNullRef(ref record), $"entity {id} is dead or doesn't exist!");
        return ref record;
    }

	private void DetachComponent(EcsID entity, EcsID id)
	{
		ref var record = ref GetRecord(entity);
		var oldArch = record.Archetype;

		if (oldArch.GetComponentIndex(id) < 0)
            return;

		var cmp = Lookup.GetComponent(id, -1);
		OnComponentUnset?.Invoke(record.GetChunk().EntityAt(record.Row), cmp);

		var newSign = oldArch.Components.Remove(cmp, _comparer);
		EcsAssert.Assert(newSign.Length < oldArch.Components.Length, "bad");

		ref var newArch = ref GetArchetype(newSign, true);
		if (newArch == null)
		{
			newArch = _archRoot.InsertVertex(oldArch, newSign, cmp.ID);
			Archetypes.Add(newArch);
		}

		record.Row = record.Archetype.MoveEntity(newArch!, record.Row);
        record.Archetype = newArch!;
	}

	private (Array?, int) AttachComponent(EcsID entity, EcsID id, int size)
	{
		ref var record = ref GetRecord(entity);
		var oldArch = record.Archetype;

		var index = oldArch.GetComponentIndex(id);
		if (index >= 0)
            return (size > 0 ? record.GetChunk().RawComponentData(index) : null, record.Row);

		var cmp = Lookup.GetComponent(id, size);
		var newSign = oldArch.Components.Add(cmp).Sort(_comparer);
		EcsAssert.Assert(newSign.Length > oldArch.Components.Length, "bad");

		ref var newArch = ref GetArchetype(newSign, true);
		if (newArch == null)
		{
			newArch = _archRoot.InsertVertex(oldArch, newSign, cmp.ID);
			Archetypes.Add(newArch);
		}

		record.Row = record.Archetype.MoveEntity(newArch, record.Row);
        record.Archetype = newArch!;

		OnComponentSet?.Invoke(record.GetChunk().EntityAt(record.Row), cmp);

		return (size > 0 ? record.GetChunk().RawComponentData(newArch.GetComponentIndex(cmp.ID)) : null, record.Row);
	}

    private ref Archetype? GetArchetype(ImmutableArray<ComponentInfo> components, bool create)
	{
		var hash = Hashing.Calculate(components.AsSpan());
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

    public readonly ref ArchetypeChunk GetChunk() => ref Archetype.GetChunk(Row);
}

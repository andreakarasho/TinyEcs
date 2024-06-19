using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Collections.Extensions;

using static TinyEcs.Defaults;

namespace TinyEcs;

public sealed partial class World : IDisposable
{
    private readonly Archetype _archRoot;
    private readonly EntitySparseSet<EcsRecord> _entities = new();
    private readonly DictionarySlim<ulong, Archetype> _typeIndex = new();
    private readonly ComponentComparer _comparer;
	private readonly EcsID _maxCmpId;
	private readonly Dictionary<ulong, Query> _cachedQueries = new ();
	private readonly object _newEntLock = new object();
	private readonly ConcurrentDictionary<string, EcsID> _namesToEntity = new ();


    public World(ulong maxComponentId = 256)
    {
        _comparer = new ComponentComparer(this);
        _archRoot = new Archetype(
            this,
            ImmutableArray<ComponentInfo>.Empty,
            _comparer
        );

		_maxCmpId = maxComponentId;
        _entities.MaxID = maxComponentId;

		_ = Component<DoNotDelete>();
		_ = Component<Unique>();
		_ = Component<Symmetric>();
		_ = Component<Wildcard>();
		_ = Component<(Wildcard, Wildcard)>();
		_ = Component<Identifier>();
		_ = Component<Name>();
		_ = Component<ChildOf>();

		setCommon(Entity<DoNotDelete>(), nameof(DoNotDelete));
		setCommon(Entity<Unique>(), nameof(Unique));
		setCommon(Entity<Symmetric>(), nameof(Symmetric));
		setCommon(Entity<Wildcard>(), nameof(Wildcard));
		setCommon(Entity<Identifier>(), nameof(Identifier));
		setCommon(Entity<Name>(), nameof(Name));
		setCommon(Entity<ChildOf>(), nameof(ChildOf))
			.Set<Unique>();

		static EntityView setCommon(EntityView entity, string name)
			=> entity.Set<DoNotDelete>().Set<Identifier, Name>(new (name));

		OnPluginInitialization?.Invoke(this);
    }

	public event Action<EntityView>? OnEntityCreated, OnEntityDeleted;
	public event Action<EntityView, ComponentInfo>? OnComponentSet, OnComponentUnset;
	public static event Action<World>? OnPluginInitialization;

    public int EntityCount => _entities.Length;
	internal Archetype Root => _archRoot;

    public List<Archetype> Archetypes { get; } = new List<Archetype>();


    public void Dispose()
    {
        _entities.Clear();
        _archRoot.Clear();
        _typeIndex.Clear();

		foreach (var query in _cachedQueries.Values)
			query.Dispose();

		_cachedQueries.Clear();
		_namesToEntity.Clear();
        Archetypes.Clear();
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

	public EntityView Entity<T>() where T : struct
	{
		ref readonly var cmp = ref Component<T>();

		var entity = Entity(cmp.ID);

		var name = Lookup.Component<T>.Name;

		if (_namesToEntity.TryGetValue(name, out var id))
		{
			EcsAssert.Panic(entity.ID == id, $"You must declare the component before the entity '{id}' named '{name}'");
		}
		else
		{
			_namesToEntity[name] = entity;
			entity.Set<Identifier, Name>(new (name));
		}

		return entity;
	}

    public EntityView Entity(EcsID id = default)
    {
        return id == 0 || !Exists(id) ? NewEmpty(id) : new(this, id);
    }

	public EntityView Entity(string name)
	{
		if (string.IsNullOrEmpty(name))
			return EntityView.Invalid;

		EntityView entity;
		if (_namesToEntity.TryGetValue(name, out var id))
		{
			entity = Entity(id);
		}
		else
		{
			entity = Entity();
			_namesToEntity[name] = entity;
			entity.Set<Identifier, Name>(new (name));
		}

		return entity;
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

    public void Delete(EcsID entity)
    {
		if (IsDeferred)
		{
			if (Exists(entity))
				DeleteDeferred(entity);

			return;
		}

		lock (_newEntLock)
		{
			OnEntityDeleted?.Invoke(new (this, entity));

			EcsAssert.Panic(!Has<DoNotDelete>(entity), "You can't delete this entity!");

			if (Has<Identifier, Name>(entity))
			{
				var name = Get<Identifier, Name>(entity).Value;
				_namesToEntity.Remove(name, out var _);
			}

			// TODO: remove the allocations
			// TODO: check for this interesting flecs approach:
			// 		 https://github.com/SanderMertens/flecs/blob/master/include/flecs/private/api_defines.h#L289
			var term0 = new QueryTerm(IDOp.Pair(Wildcard.ID, entity), TermOp.With);
			var term1 = new QueryTerm(IDOp.Pair(entity, Wildcard.ID), TermOp.With);
			QueryRaw([term0]).Each((EntityView child) => child.Delete());
			QueryRaw([term1]).Each((EntityView child) => child.Delete());


			ref var record = ref GetRecord(entity);

			var removedId = record.Archetype.Remove(ref record);
			EcsAssert.Assert(removedId == entity);

			_entities.Remove(removedId);
		}
    }

    public bool Exists(EcsID entity)
    {
		// if (IsDeferred && ExistsDeferred(entity))
		// 	return true;

		if (entity.IsPair)
        {
            return _entities.Contains(entity.First) && _entities.Contains(entity.Second);
        }

        return _entities.Contains(entity);
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

    internal bool Has(EcsID entity, EcsID id)
    {
		// if (IsDeferred)
		// {
		// 	if (HasDeferred(entity, id))
		// 		return true;

		// 	if (ExistsDeferred(entity))
		// 		return false;
		// }

		ref var record = ref GetRecord(entity);
        var has = record.Archetype.GetComponentIndex(id) >= 0;
		if (has) return true;

		if (id.IsPair)
		{
			(var a, var b) = FindPair(entity, id);

			return a != 0 && b != 0;
		}

		return id == Wildcard.ID;
    }

    public ReadOnlySpan<ComponentInfo> GetType(EcsID id)
    {
        ref var record = ref GetRecord(id);
        return record.Archetype.Components.AsSpan();
    }

    public void PrintGraph()
    {
        _archRoot.Print();
    }

	public Query QueryRaw(ImmutableArray<QueryTerm> terms)
	{
		return GetQuery(
			Hashing.Calculate(terms.AsSpan()),
			terms,
			static (world, terms) => new Query(world, terms));
	}

	public Query Query<TQueryData>() where TQueryData : struct
	{
		return GetQuery(
			Lookup.Query<TQueryData>.Hash,
		 	Lookup.Query<TQueryData>.Terms,
		 	static (world, _) => new Query<TQueryData>(world)
		);
	}

	public Query Query<TQueryData, TQueryFilter>() where TQueryData : struct where TQueryFilter : struct
	{
		return GetQuery(
			Lookup.Query<TQueryData, TQueryFilter>.Hash,
			Lookup.Query<TQueryData, TQueryFilter>.Terms,
		 	static (world, _) => new Query<TQueryData, TQueryFilter>(world)
		);
	}

	public void Each(QueryFilterDelegateWithEntity fn)
	{
		BeginDeferred();

		foreach (var arch in GetQuery(0, ImmutableArray<QueryTerm>.Empty, static (world, terms) => new Query(world, terms)))
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

	internal Query GetQuery(ulong hash, ImmutableArray<QueryTerm> terms, Func<World, ImmutableArray<QueryTerm>, Query> factory)
	{
		if (!_cachedQueries.TryGetValue(hash, out var query))
		{
			query = factory(this, terms);
			_cachedQueries.Add(hash, query);
		}

		query.Match();

		return query;
	}

    public QueryBuilder QueryBuilder() => new QueryBuilder(this);

	internal Archetype? FindArchetype(ulong hash)
	{
		if (!_typeIndex.TryGetValue(hash, out var arch))
		{
			arch = _archRoot;
		}

		return arch;
	}

	internal void MatchArchetypes(Archetype root, ReadOnlySpan<QueryTerm> terms, List<Archetype> matched)
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

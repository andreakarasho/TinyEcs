using System.Collections.Immutable;
using static TinyEcs.Defaults;

namespace TinyEcs;

/// <summary>
/// The entities container.
/// </summary>
public sealed partial class World
{
	/// <summary>
	/// Create the world.<para/>
	/// Optionally specify the number of components the world will handle.<para/>
	/// </summary>
	/// <param name="maxComponentId"></param>
	public World(ulong maxComponentId = 256)
    {
        _comparer = new ComponentComparer(this);
        _archRoot = new Archetype(
            this,
            [],
            _comparer
        );

		_maxCmpId = maxComponentId;
        _entities.MaxID = maxComponentId;

		_ = Component<DoNotDelete>();
		_ = Component<Unique>();
		_ = Component<Symmetric>();
		_ = Component<Wildcard>();
		_ = Component<Relation<Wildcard, Wildcard>>();
		_ = Component<Identifier>();
		_ = Component<Name>();
		_ = Component<ChildOf>();

		_ = Component<OnDelete>();
		_ = Component<Delete>();
		_ = Component<Panic>();
		_ = Component<Unset>();

		setCommon(Entity<DoNotDelete>(), nameof(DoNotDelete));
		setCommon(Entity<Unique>(), nameof(Unique));
		setCommon(Entity<Symmetric>(), nameof(Symmetric));
		setCommon(Entity<Wildcard>(), nameof(Wildcard));
		setCommon(Entity<Identifier>(), nameof(Identifier));
		setCommon(Entity<Name>(), nameof(Name));
		setCommon(Entity<ChildOf>(), nameof(ChildOf))
			.Add<OnDelete, Delete>()
			.Add<Unique>();
		setCommon(Entity<OnDelete>(), nameof(OnDelete))
			.Add<Unique>();
		setCommon(Entity<Delete>(), nameof(Delete));
		setCommon(Entity<Panic>(), nameof(Panic));
		setCommon(Entity<Unset>(), nameof(Unset));

		static EntityView setCommon(EntityView entity, string name)
			=> entity.Add<DoNotDelete>().Set<Identifier, Name>(new (name));

		OnPluginInitialization?.Invoke(this);
    }



	public event Action<EntityView>? OnEntityCreated, OnEntityDeleted;
	public event Action<EntityView, ComponentInfo>? OnComponentSet, OnComponentUnset;
	public static event Action<World>? OnPluginInitialization;



	/// <summary>
	/// Count of entities alive.
	/// </summary>
	public int EntityCount => _entities.Length;



	/// <summary>
	/// Cleanup the world.
	/// </summary>
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

	/// <summary>
	/// Create or get an entity with the specified <paramref name="id"/>.<br/>
	/// When <paramref name="id"/> is not specified or is 0 a new entity is spawned.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
    public EntityView Entity(ulong id = 0)
	{
		lock (_newEntLock)
		{
			EntityView ent;
			if (id == 0 || !Exists(id))
			{
				// if (IsDeferred)
				// {
				// 	if (id == 0)
				// 		id = ++_entities.MaxID;
				// 	CreateDeferred(id);
				// 	return new EntityView(this, id);
				// }

				ref var record = ref (
					id > 0 ?
					ref _entities.Add(id, default!)
					:
					ref _entities.CreateNew(out id)
				);

				record.Archetype = _archRoot;
				record.Row = _archRoot.Add(id);

				ent = new EntityView(this, id);
				OnEntityCreated?.Invoke(ent);
			}
			else
			{
				ent = new(this, id);
			}
			return ent;
		}
	}

	/// <summary>
	/// Get or create an entity from a component.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public EntityView Entity<T>() where T : struct
	{
		ref readonly var cmp = ref Component<T>();

		var entity = Entity(cmp.ID);
		if (entity.ID.IsPair())
			return entity;

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

	/// <summary>
	/// Create or get an entity using the specified <paramref name="name"/>.<br/>
	/// A relation (Identity, Name) will be automatically added to the entity.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Delete the entity.<br/>
	/// Associated children are deleted too.
	/// </summary>
	/// <param name="entity"></param>
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

			// TODO: check for this interesting flecs approach:
			// 		 https://github.com/SanderMertens/flecs/blob/master/include/flecs/private/api_defines.h#L289
			var term0 = new QueryTerm(IDOp.Pair(Wildcard.ID, entity), TermOp.With);
			var term1 = new QueryTerm(IDOp.Pair(entity, Wildcard.ID), TermOp.With);

			QueryFilterDelegateWithEntity onQuery = (EntityView child) => {
				var action = Action(child.ID, entity);
				if (Has<OnDelete, Delete>(action))
					child.Delete();
				if (Has<OnDelete, Unset>(action))
					child.Unset(action, entity);
				if (Has<OnDelete, Panic>(action))
					EcsAssert.Panic(false, "you cant remove this entity because of {OnDelete, Panic} relation");
			};
			QueryRaw(term0).Each(onQuery);
			QueryRaw(term1).Each(onQuery);

			ref var record = ref GetRecord(entity);
			var removedId = record.Archetype.Remove(ref record);
			EcsAssert.Assert(removedId == entity);

			_entities.Remove(removedId);
		}
    }

	/// <summary>
	/// Check if the entity is valid and alive.
	/// </summary>
	/// <param name="entity"></param>
	/// <returns></returns>
    public bool Exists(EcsID entity)
    {
		if (entity.IsPair())
        {
            return GetAlive(entity.First()).IsValid() && GetAlive(entity.Second()).IsValid();
        }

        return _entities.Contains(entity);
    }

	/// <summary>
	/// The archetype sign.<br/>The sign is unique.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
    public ReadOnlySpan<ComponentInfo> GetType(EcsID id)
    {
        ref var record = ref GetRecord(id);
        return record.Archetype.All.AsSpan();
    }

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

        _ = AttachComponent(entity, cmp.ID, cmp.Size);
    }

	/// <summary>
	/// Set a Component to the entity.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="entity"></param>
	/// <param name="component"></param>
    public void Set<T>(EcsID entity, T component) where T : struct
	{
		ref readonly var cmp = ref Component<T>();
        EcsAssert.Panic(cmp.Size > 0, "this is not a component");

		if (IsDeferred && !Has(entity, cmp.ID))
		{
			SetDeferred(entity, component);

			return;
		}

        (var raw, var row) = AttachComponent(entity, cmp.ID, cmp.Size);
        ref var array = ref Unsafe.As<Array, T[]>(ref raw!);
        array[row & Archetype.CHUNK_THRESHOLD] = component;
	}

	/// <summary>
	/// Add a Tag to the entity.<br/>Tag is an entity.
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

		_ = AttachComponent(entity, id, 0);
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

		DetachComponent(entity, id);
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
	/// Check if the entity has a component or tag.<br/>
	/// Component or tag is an entity.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="id"></param>
	/// <returns></returns>
	public bool Has(EcsID entity, EcsID id)
    {
		ref var record = ref GetRecord(entity);
        var has = record.Archetype.GetComponentIndex(id) >= 0;
		if (has) return true;

		if (id.IsPair())
		{
			(var a, var b) = FindPair(entity, id);

			return a != 0 && b != 0;
		}

		return id == Wildcard.ID;
    }

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

	/// <summary>
	/// Print the archetype graph.
	/// </summary>
	public void PrintGraph()
    {
        _archRoot.Print();
    }

	/// <summary>
	/// Create a raw query specifing the terms directly.
	/// </summary>
	/// <param name="terms"></param>
	/// <returns></returns>
	public Query QueryRaw(params Span<IQueryTerm> terms)
	{
		terms.Sort();
		return GetQuery(
			Hashing.Calculate(terms),
			terms,
			static (world, terms) => new Query(world, terms));
	}

	/// <summary>
	/// Query for specific components.<para/>
	/// <example>
	/// 	Single component:
	/// <code>
	///		var query = world.Query&lt;Position&gt;();
	///		query.Each((ref Position pos) => { });
	/// </code>
	/// 	Multiple components<para/>
	/// <code>
	///		var query = world.Query&lt;(Position, Velocity)&gt;();
	///		query.Each((ref Position pos, ref Velocity vel) => { });
	/// </code>
	/// </example>
	/// </summary>
	/// <typeparam name="TQueryData"></typeparam>
	/// <returns></returns>
	public Query Query<TQueryData>() where TQueryData : struct
	{
		return GetQuery(
			Lookup.Query<TQueryData>.Hash,
		 	Lookup.Query<TQueryData>.Terms.AsSpan(),
		 	static (world, _) => new Query<TQueryData>(world)
		);
	}

	/// <summary>
	/// Query for specific components with filters.<para/>
	/// <example>
	/// 	Single filter:
	/// <code>
	///		var query = world.Query&lt;(Position, Velocity), Without&lt;Rotation&gt;&gt;();
	///		query.Each((ref Position pos, ref Velocity vel) => { });
	/// </code>
	/// 	Multiple filters<para/>
	/// <code>
	///		var query = world.Query&lt;(Position, Velocity), (With&lt;IsNpc&gt;, Without&lt;Rotation&gt;)&gt;();
	///		query.Each((ref Position pos, ref Velocity vel) => { });
	/// </code>
	/// 	The 'Or' clausole<para/>
	/// <code>
	///		var query = world.Query&lt;(Position, Velocity, Optional&lt;Rotation&gt;),
	///			(With&lt;IsNpc&gt;, Without&lt;Rotation&gt;,
	///				Or&lt;(With&lt;IsPlayer&gt;, With&lt;Rotation&gt;)&gt;)&gt;();
	///
	///		query.Each((ref Position pos, ref Velocity vel, ref Rotation maybeRot) => {
	///			if (Unsafe.IsNullRef(ref maybeRot)) {
	///				// hitting the main query
	///			} else {
	///				// hitting the Or clausole
	///			}
	///		});
	/// </code>
	/// </example>
	/// </summary>
	/// <typeparam name="TQueryData"></typeparam>
	/// <typeparam name="TQueryFilter"></typeparam>
	/// <returns></returns>
	public Query Query<TQueryData, TQueryFilter>() where TQueryData : struct where TQueryFilter : struct
	{
		return GetQuery(
			Lookup.Query<TQueryData, TQueryFilter>.Hash,
			Lookup.Query<TQueryData, TQueryFilter>.Terms.AsSpan(),
		 	static (world, _) => new Query<TQueryData, TQueryFilter>(world)
		);
	}

	/// <summary>
	///
	/// </summary>
	/// <returns></returns>
	public QueryBuilder QueryBuilder() => new QueryBuilder(this);

	/// <summary>
	/// Execute a deferred block.
	/// </summary>
	/// <param name="fn"></param>
	public void Deferred(Action<World> fn)
	{
		BeginDeferred();
		fn(this);
		EndDeferred();
	}
}

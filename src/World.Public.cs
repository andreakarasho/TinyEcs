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
		_typeIndex.Add(_archRoot.Id, _archRoot);
		LastArchetypeId = _archRoot.Id;

		_maxCmpId = maxComponentId;
        _entities.MaxID = maxComponentId;

		_ = Component<Rule>();
		_ = Component<DoNotDelete>();
		_ = Component<Unique>();
		_ = Component<Symmetric>();
		_ = Component<Wildcard>();
		_ = Component<Pair<Wildcard, Wildcard>>();
		_ = Component<Identifier>();
		_ = Component<Name>();
		_ = Component<ChildOf>();

		_ = Component<OnDelete>();
		_ = Component<Delete>();
		_ = Component<Panic>();
		_ = Component<Unset>();

		setCommon(Entity<Rule>(), nameof(Rule));
		setCommon(Entity<DoNotDelete>(), nameof(DoNotDelete));
		setCommon(Entity<Unique>(), nameof(Unique));
		setCommon(Entity<Symmetric>(), nameof(Symmetric));
		setCommon(Entity<Wildcard>(), nameof(Wildcard));
		setCommon(Entity<Identifier>(), nameof(Identifier));
		setCommon(Entity<Name>(), nameof(Name));
		setCommon(Entity<ChildOf>(), nameof(ChildOf))
			.Add<OnDelete, Delete>()
			.Rule<Unique>();
		setCommon(Entity<OnDelete>(), nameof(OnDelete))
			.Rule<Unique>();
		setCommon(Entity<Delete>(), nameof(Delete));
		setCommon(Entity<Panic>(), nameof(Panic));
		setCommon(Entity<Unset>(), nameof(Unset));

		Entity<Identifier>()
			.Rule<Unset>();

		static EntityView setCommon(EntityView entity, string name)
			=> entity.Add<DoNotDelete>().Set<Identifier>(new (name), Defaults.Name.ID);

		OnPluginInitialization?.Invoke(this);
    }



	public event Action<EntityView>? OnEntityCreated, OnEntityDeleted;
	public event Action<EntityView, ComponentInfo>? OnComponentSet, OnComponentUnset;
	public static event Action<World>? OnPluginInitialization;



	/// <summary>
	/// Count of entities alive.<br/>
	/// ⚠️ If the count doesn't match with your expectations it's because
	/// in TinyEcs components are also entities!
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
		_cachedComponents.Clear();
		_names.Clear();

		foreach (var query in _cachedQueries.Values)
			query.Dispose();

		_cachedQueries.Clear();
    }

	/// <summary>
	/// Remove all empty archetypes.
	/// </summary>
	/// <returns></returns>
	public int RemoveEmptyArchetypes()
	{
		var removed = 0;
		_archRoot?.RemoveEmptyArchetypes(ref removed, _typeIndex);
		if (removed > 0)
			LastArchetypeId = ulong.MaxValue;
		return removed;
	}

	/// <summary>
	/// Get or create an archetype with the specified components.
	/// </summary>
	/// <param name="ids"></param>
	/// <returns></returns>
	public Archetype Archetype(params Span<ComponentInfo> ids)
	{
		if (ids.IsEmpty)
			return _archRoot;

		ids.SortNoAlloc(_comparisonCmps);

		var hash = RollingHash.Calculate(ids);
		if (!_typeIndex.TryGetValue(hash, out var archetype))
		{
			var archLessOne = Archetype(ids[..^1]);
			var arr = new ComponentInfo[ids.Length];
			archLessOne.All.CopyTo(arr);
			arr[^1] = ids[^1];
			arr.AsSpan().SortNoAlloc(_comparisonCmps);
			archetype = NewArchetype(archLessOne, arr, arr[^1].ID);
		}

		return archetype;
	}

	/// <summary>
	/// Create an entity with the specified components attached.
	/// </summary>
	/// <param name="arch"></param>
	/// <returns></returns>
	public EntityView Entity(Archetype arch)
	{
		ref var record = ref NewId(out var id);
		record.Archetype = arch;
		record.Chunk = arch.Add(id, out record.Row);
		record.Flags = EntityFlags.None;

		return new EntityView(this, id);
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

				ref var record = ref NewId(out id, id);
				record.Archetype = _archRoot;
				record.Chunk = _archRoot.Add(id, out record.Row);
				record.Flags = EntityFlags.None;

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

		var entId = cmp.ID;

		if (!entId.IsPair())
		{
			ref var record = ref GetRecord(entId);

			if ((record.Flags & EntityFlags.HasName) == 0)
			{
				record.Flags |= EntityFlags.HasName;
				var name = Lookup.Component<T>.Name;
				_names[name] = entId;
				Set<Identifier>(entId, new (name), Defaults.Name.ID);
			}
		}

		return new EntityView(this, entId);
	}

	/// <summary>
	/// Create or get an entity using the specified <paramref name="name"/>.<br/>
	/// A relation (Identity, Name) will be automatically added to the entity.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public EntityView Entity(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return EntityView.Invalid;

		EntityView entity;
		if (_names.TryGetValue(name, out var id) && (GetRecord(id).Flags & EntityFlags.HasName) != 0)
		{
			entity = Entity(id);
		}
		else
		{
			entity = Entity();
			GetRecord(entity).Flags |= EntityFlags.HasName;
			_names[name] = entity;
			entity.Set<Identifier>(new (name), Defaults.Name.ID);
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

			ref var record = ref GetRecord(entity);

			EcsAssert.Panic(!Has<DoNotDelete>(entity), "You can't delete this entity!");

			if (record.Flags != EntityFlags.None)
			{
				if ((record.Flags & EntityFlags.HasName) != 0)
				{
					if (Has<Identifier, Name>(entity))
					{
						var name = Get<Identifier>(entity, Defaults.Name.ID).Data;
						_names.Remove(name, out var _);
					}
				}

				static void applyDeleteRules(World world, EcsID entity, params Span<IQueryTerm> terms)
				{
					world.BeginDeferred();
					foreach (var arch in world.QueryRaw(terms))
					{
						foreach (ref readonly var chunk in arch)
						{
							foreach (ref readonly var child in chunk.Entities.AsSpan(0, chunk.Count))
							{
								var action = world.Action(child.ID, entity);
								if (world.Has<OnDelete, Delete>(action))
									child.Delete();
								if (world.Has<OnDelete, Unset>(action))
									child.Unset(action, entity);
								if (world.Has<OnDelete, Panic>(action))
									EcsAssert.Panic(false, "you cant remove this entity because of {OnDelete, Panic} relation");
							}
						}
					}
					world.EndDeferred();
				}

				if ((record.Flags & EntityFlags.IsAction) != 0)
				{
					var term = new QueryTerm(IDOp.Pair(entity, Wildcard.ID), TermOp.With);
					applyDeleteRules(this, entity, term);
				}

				if ((record.Flags & EntityFlags.IsTarget) != 0)
				{
					var term = new QueryTerm(IDOp.Pair(Wildcard.ID, entity), TermOp.With);
					applyDeleteRules(this, entity, term);
				}
			}

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
			(var first, var second) = entity.Pair();
            return GetAlive(first).IsValid() && GetAlive(second).IsValid();
        }

        return _entities.Contains(entity);
    }

	/// <summary>
	/// Use this function to analyze pairs members.<br/>
	/// Pairs members lose their generation count. This function will bring it back!.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
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
			AddDeferred<T>(entity);

			return;
		}

        _ = Attach(entity, cmp.ID, cmp.Size, cmp.IsManaged);
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

        (var raw, var row) = Attach(entity, cmp.ID, cmp.Size, cmp.IsManaged);
        var array = Unsafe.As<T[]>(raw!);
        array[row & TinyEcs.Archetype.CHUNK_THRESHOLD] = component;
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
			SetDeferred(entity, id, null, 0, false);

			return;
		}

		_ = Attach(entity, id, 0, false);
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

		Detach(entity, id);
	}

	/// <summary>
	/// Check if the entity has a component or tag.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="entity"></param>
	/// <returns></returns>
    public bool Has<T>(EcsID entity) where T : struct
		=> Has(entity, Component<T>().ID);

	/// <summary>
	/// Check if the entity has a component or tag.<br/>
	/// Component or tag is an entity.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="id"></param>
	/// <returns></returns>
	public bool Has(EcsID entity, EcsID id)
    {
		return IsAttached(ref GetRecord(entity), id);
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
	/// Get the name associated to the entity.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	public string Name(EcsID id)
	{
		ref var record = ref GetRecord(id);
		if ((record.Flags & EntityFlags.HasName) != 0)
			return Get<Identifier>(id, Defaults.Name.ID).Data;
		return string.Empty;
	}

	/// <inheritdoc cref="Rule(EcsID, EcsID)"/>
	public void Rule<TRule>(EcsID entity) where TRule : struct
	{
		Rule(entity, Component<TRule>().ID);
	}

	/// <summary>
	/// Set a rule to an entity.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="ruleId"></param>
	public void Rule(EcsID entity, EcsID ruleId)
	{
		ref var ruleRecord = ref GetRecord(entity);
		ruleRecord.Flags |= EntityFlags.HasRules;

		Add(entity, Defaults.Rule.ID, ruleId);
	}

	/// <summary>
	/// Print the archetype graph.
	/// </summary>
	public void PrintGraph()
    {
        _archRoot.Print(0);
    }

	/// <summary>
	/// Create a raw query specifing the terms directly.
	/// </summary>
	/// <param name="terms"></param>
	/// <returns></returns>
	public Query QueryRaw(params Span<IQueryTerm> terms)
	{
		terms.Sort();
		var roll = IQueryTerm.GetHash(terms);
		return GetQuery(
			roll.Hash,
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

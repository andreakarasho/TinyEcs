using System.Collections.Immutable;

namespace TinyEcs;

public ref struct QueryInternal
{
	private readonly ReadOnlySpan<Archetype> _archetypes;
	private readonly ReadOnlySpan<Term> _terms;
	private int _index;

	internal QueryInternal(ReadOnlySpan<Archetype> archetypes, ReadOnlySpan<Term> terms)
	{
		_archetypes = archetypes;
		_terms = terms;
		_index = -1;
	}

	public readonly Archetype Current => _archetypes[_index];

	public bool MoveNext()
	{
		while (++_index < _archetypes.Length)
		{
			var arch = _archetypes[_index];
			if (arch.Count > 0)
				return true;
		}

		return false;
	}

	public void Reset() => _index = -1;

	public readonly QueryInternal GetEnumerator() => this;
}

public delegate void QueryFilterDelegateWithEntity(EntityView entity);



public interface IQueryBuild
{
	Query Build();
}

public sealed class QueryBuilder : IQueryBuild
{
	private readonly World _world;
	private readonly HashSet<Term> _components = new();

	internal QueryBuilder(World world) => _world = world;

	public QueryBuilder With<T>() where T : struct
		=> With(_world.Component<T>().ID);

	public QueryBuilder With<TAction, TTarget>()
		where TAction : struct
		where TTarget : struct
		=> With(IDOp.Pair(_world.Component<TAction>().ID, _world.Component<TTarget>().ID));

	public QueryBuilder With(EcsID id)
	{
		_components.Add(Term.With(id));
		return this;
	}

	public QueryBuilder Without<T>() where T : struct
		=> Without(_world.Component<T>().ID);

	public QueryBuilder Without<TAction, TTarget>()
		where TAction : struct
		where TTarget : struct
		=> Without(IDOp.Pair(_world.Component<TAction>().ID, _world.Component<TTarget>().ID));

	public QueryBuilder Without(EcsID id)
	{
		_components.Add(Term.Without(id));
		return this;
	}

	public Query Build()
	{
		var terms = _components.ToImmutableArray();
		return _world.GetQuery(
			Hashing.Calculate(terms.AsSpan()),
			terms,
			static (world, terms) => new Query(world, terms)
		);
	}
}


public sealed partial class Query<TQuery> : Query
	where TQuery : struct
{
	internal Query(World world) : base(world, Lookup.Query<TQuery>.Terms)
	{
	}
}

public sealed partial class Query<TQuery, TFilter> : Query
	where TQuery : struct where TFilter : struct
{
	internal Query(World world) : base(world, Lookup.Query<TQuery, TFilter>.Terms)
	{
	}
}

public partial class Query : IDisposable
{
	private readonly ImmutableArray<Term> _terms;
	private readonly List<Archetype> _matchedArchetypes;
	private ulong _lastArchetypeIdMatched = 0;

	internal Query(World world, ImmutableArray<Term> terms)
	{
		World = world;
		_terms = terms;
		_matchedArchetypes = new List<Archetype>();
	}

	public World World { get; internal set; }
	internal List<Archetype> MatchedArchetypes => _matchedArchetypes;
	internal CountdownEvent ThreadCounter { get; } = new CountdownEvent(1);

	public void Dispose() => ThreadCounter.Dispose();

	internal void Match()
	{
		var allArchetypes = World.Archetypes;

		if (allArchetypes.IsEmpty || _lastArchetypeIdMatched == allArchetypes[^1].Id)
			return;

		var terms = _terms.AsSpan();
		var first = World.FindArchetype(Hashing.Calculate(terms));
		if (first == null)
			return;

		_lastArchetypeIdMatched = allArchetypes[^1].Id;
		_matchedArchetypes.Clear();
		World.MatchArchetypes(first, terms, _matchedArchetypes);
	}

	public int Count()
	{
		Match();
		return _matchedArchetypes.Sum(static s => s.Count);
	}

	public ref T Single<T>() where T : struct
	{
		var count = Count();
		EcsAssert.Panic(count == 1, "Multiple entities found for a single archetype");

		foreach (var arch in this)
		{
			var column = arch.GetComponentIndex<T>();
			EcsAssert.Panic(column > 0, "component not found");
			ref var value = ref arch.GetChunk(0).GetReference<T>(column);
			return ref value;
		}

		return ref Unsafe.NullRef<T>();
	}

	public QueryInternal GetEnumerator()
	{
		Match();

		return new (CollectionsMarshal.AsSpan(_matchedArchetypes), _terms.AsSpan());
	}

	public void Each(QueryFilterDelegateWithEntity fn)
	{
		World.BeginDeferred();

		foreach (var arch in this)
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

		World.EndDeferred();
	}
}

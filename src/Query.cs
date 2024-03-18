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



public interface IQueryConstruct
{
	IQueryBuild With<T>() where T : struct;
	IQueryBuild With(EcsID id);
	IQueryBuild Without<T>() where T : struct;
	IQueryBuild Without(EcsID id);
}

public interface IQueryBuild
{
	Query Build();
}

public interface IQuery
{

}


public sealed class QueryBuilder : IQueryConstruct, IQueryBuild
{
	private readonly World _world;
	private readonly HashSet<Term> _components = new();

	internal QueryBuilder(World world) => _world = world;

	public IQueryBuild With<T>() where T : struct
		=> With(_world.Component<T>().ID);

	public IQueryBuild With(EcsID id)
	{
		_components.Add(Term.With(id));
		return this;
	}

	public IQueryBuild Without<T>() where T : struct
		=> Without(_world.Component<T>().ID);

	public IQueryBuild Without(EcsID id)
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


public sealed partial class Query<TQuery> : Query, ISystemParam
	where TQuery : struct
{
	internal Query(World world) : base(world, Lookup.Query<TQuery>.Terms)
	{
	}
}

public sealed partial class Query<TQuery, TFilter> : Query, ISystemParam
	where TQuery : struct where TFilter : struct
{
	internal Query(World world) : base(world, Lookup.Query<TQuery, TFilter>.Terms)
	{
	}
}

public partial class Query : IQuery
{
	private readonly World _world;
	private readonly ImmutableArray<Term> _terms;
	private readonly List<Archetype> _matchedArchetypes;
	private ulong _lastArchetypeIdMatched = 0;

	internal Query(World world, ImmutableArray<Term> terms)
	{
		_world = world;
		_terms = terms;
		_matchedArchetypes = new List<Archetype>();
	}

	internal List<Archetype> MatchedArchetypes => _matchedArchetypes;

	internal void Match()
	{
		var allArchetypes = _world.Archetypes;

		if (allArchetypes.IsEmpty || _lastArchetypeIdMatched == allArchetypes[^1].Id)
			return;

		var terms = _terms.AsSpan();
		var first = _world.FindArchetype(Hashing.Calculate(terms));
		if (first == null)
			return;

		_lastArchetypeIdMatched = allArchetypes[^1].Id;
		_matchedArchetypes.Clear();
		_world.MatchArchetypes(first, terms, _matchedArchetypes);
	}

	public QueryInternal GetEnumerator()
	{
		Match();

		return new (CollectionsMarshal.AsSpan(_matchedArchetypes), _terms.AsSpan());
	}

	public void Each(QueryFilterDelegateWithEntity fn)
	{
		foreach (var arch in this)
		{
			foreach (ref readonly var chunk in arch)
			{
				ref var entity = ref chunk.Entities[0];
				ref var last = ref Unsafe.Add(ref entity, chunk.Count);
				while (Unsafe.IsAddressLessThan(ref entity, ref last))
				{
					fn(entity);
					entity = ref Unsafe.Add(ref entity, 1);
				}
			}
		}
	}
}

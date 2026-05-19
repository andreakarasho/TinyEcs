using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

public sealed class QueryBuilder
{
	private readonly World _world;
	private readonly Dictionary<EcsID, IQueryTerm> _components = new();
	private Query? _query;

	internal QueryBuilder(World world) => _world = world;

	public World World => _world;

	public QueryBuilder With<T>() where T : struct
		=> With(_world.Component<T>().ID);

#if USE_PAIR
	public QueryBuilder With<TAction, TTarget>()
		where TAction : struct
		where TTarget : struct
		=> With(_world.Component<TAction>().ID, _world.Component<TTarget>().ID);

	public QueryBuilder With<TAction>(EcsID target)
		where TAction : struct
		=> With(_world.Component<TAction>().ID, target);

	public QueryBuilder With(EcsID action, EcsID target)
		=> With(IDOp.Pair(action, target));
#endif

	public QueryBuilder With(EcsID id)
		=> Term(new WithTerm(id));

	public QueryBuilder Without<T>() where T : struct
		=> Without(_world.Component<T>().ID);

#if USE_PAIR
	public QueryBuilder Without<TAction, TTarget>()
		where TAction : struct
		where TTarget : struct
		=> Without(_world.Component<TAction>().ID, _world.Component<TTarget>().ID);

	public QueryBuilder Without<TAction>(EcsID target)
		where TAction : struct
		=> Without(_world.Component<TAction>().ID, target);

	public QueryBuilder Without(EcsID action, EcsID target)
		=> Without(IDOp.Pair(action, target));
#endif

	public QueryBuilder Without(EcsID id)
		=> Term(new WithoutTerm(id));

	public QueryBuilder Optional<T>() where T : struct
	{
		ref readonly var cmp = ref _world.Component<T>();
		EcsAssert.Panic(cmp.Size > 0, "You can't access Tag as Component");
		return Optional(cmp.ID);
	}

	public QueryBuilder Optional(EcsID id)
		=> Term(new OptionalTerm(id));

	public QueryBuilder Term(IQueryTerm term)
	{
		_components[term.Id] = term;
		return this;
	}

	public Query Build()
	{
		_query = null;
		_query ??= new Query(_world, _components.Values.ToArray());
		return _query;
	}
}

public sealed class Query
{
	private readonly IQueryTerm[] _terms;
	private readonly ulong[] _termIds;
	private readonly TermOp[] _termOps;
	private readonly ulong[]? _withMask;
	private readonly ulong[]? _withoutMask;
	private readonly bool _fastPath;
	private readonly List<Archetype> _matchedArchetypes;
	private readonly HashSet<EcsID> _archetypesIdSet;
	private EcsID _lastArchetypeIdMatched;
	private ulong _lastStructuralVersion;
	private readonly int[] _indices;
	private readonly ulong[] _termIdsAccess;
	private readonly bool _hasOptional;

	internal Query(World world, IQueryTerm[] terms)
	{
		World = world;
		_matchedArchetypes = new();
		_archetypesIdSet = new();

		_terms = new IQueryTerm[terms.Length];
		terms.CopyTo(_terms, 0);
		Array.Sort(_terms);

		_termIds = new ulong[_terms.Length];
		_termOps = new TermOp[_terms.Length];
		for (var i = 0; i < _terms.Length; i++)
		{
			_termIds[i] = _terms[i].Id;
			_termOps[i] = _terms[i].Op;
			if (_termOps[i] == TermOp.Optional)
				_hasOptional = true;
		}

		var words = world.ComponentBitsetWords;
		var maxBit = (ulong)(words << 6);
		var fast = words > 0;
		ulong[]? withMask = null;
		ulong[]? withoutMask = null;
		if (fast)
		{
			withMask = new ulong[words];
			withoutMask = new ulong[words];
			for (var i = 0; i < _terms.Length; i++)
			{
				var id = _termIds[i];
#if USE_PAIR
				if (id.IsPair()) { fast = false; break; }
#endif
				if (id >= maxBit) { fast = false; break; }
				var w = (int)(id >> 6);
				var bit = 1ul << (int)(id & 63);
				switch (_termOps[i])
				{
					case TermOp.With: withMask[w] |= bit; break;
					case TermOp.Without: withoutMask[w] |= bit; break;
					case TermOp.Optional: break;
				}
			}
		}
		_fastPath = fast;
		_withMask = fast ? withMask : null;
		_withoutMask = fast ? withoutMask : null;

		TermsAccess = terms
			.Where(s => Lookup.GetComponent(s.Id).Size > 0)
			.ToArray();

		_termIdsAccess = new ulong[TermsAccess.Length];
		for (var i = 0; i < TermsAccess.Length; i++)
			_termIdsAccess[i] = TermsAccess[i].Id;

		_indices = new int[TermsAccess.Length];
		_indices.AsSpan().Fill(-1);
	}

	internal World World { get; }
	internal IQueryTerm[] TermsAccess { get; }
	internal ulong[] TermIds => _termIds;
	internal TermOp[] TermOps => _termOps;
	internal ulong[]? WithMask => _withMask;
	internal ulong[]? WithoutMask => _withoutMask;
	internal bool FastPath => _fastPath;
	internal bool HasOptional => _hasOptional;



	private void Match()
	{
		// Check if either new archetypes were created OR entities moved between archetypes
		if (_lastArchetypeIdMatched == World.LastArchetypeId &&
			_lastStructuralVersion == World.StructuralChangeVersion)
			return;

		_lastArchetypeIdMatched = World.LastArchetypeId;
		_lastStructuralVersion = World.StructuralChangeVersion;

		_matchedArchetypes.Clear();
		World.Root.GetSuperSets(this, _matchedArchetypes);

		_archetypesIdSet.Clear();
		foreach (var arch in _matchedArchetypes)
			_archetypesIdSet.Add(arch.Id);
	}

	public int Count()
	{
		Match();

		return _matchedArchetypes.Sum(static s => s.Count);
	}

	public QueryIterator Iter()
	{
		Match();

		return Iter(CollectionsMarshal.AsSpan(_matchedArchetypes), 0, -1);
	}

	public QueryIterator Iter(EcsID entity)
	{
		Match();

		if (World.Exists(entity))
		{
			ref var record = ref World.GetRecord(entity);
			if (_archetypesIdSet.Contains(record.Archetype.Id))
			{
				var archetypes = new ReadOnlySpan<Archetype>(ref record.Archetype);
				return Iter(archetypes, record.Row, 1);
			}
		}

		return Iter([], 0, 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private QueryIterator Iter(ReadOnlySpan<Archetype> archetypes, int start, int count)
	{
		return new(archetypes, _termIdsAccess, _indices, start, count, _hasOptional);
	}
}


[SkipLocalsInit]
public ref struct QueryIterator
{
	private ReadOnlySpan<Archetype>.Enumerator _archetypeIterator;
	private readonly ReadOnlySpan<ulong> _termIds;
	private readonly Span<int> _indices;
	private readonly int _start, _count;
	private readonly bool _hasOptional;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal QueryIterator(ReadOnlySpan<Archetype> archetypes, ReadOnlySpan<ulong> termIds, Span<int> indices, int start, int count, bool hasOptional)
	{
		_archetypeIterator = archetypes.GetEnumerator();
		_termIds = termIds;
		_indices = indices;
		_start = start;
		_count = count;
		_hasOptional = hasOptional;
	}

	public readonly bool HasOptional
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _hasOptional;
	}

	public readonly int Count
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _count > 0 ? Math.Min(_count, _archetypeIterator.Current.Count) : _archetypeIterator.Current.Count;
	}

	public readonly Archetype Archetype => _archetypeIterator.Current;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly int GetColumnIndexOf<T>() where T : struct
	{
		return _indices.IndexOf(_archetypeIterator.Current.GetComponentIndex<T>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly DataRow<T> GetColumn<T>(int index) where T : struct
	{
		if (index < 0 || index >= _indices.Length)
			return DataRow<T>.CreateAbsent();

		var i = _indices[index];
		if (i < 0)
			return DataRow<T>.CreateAbsent();

		var arch = _archetypeIterator.Current;
		var column = (Column<T>)arch.Columns![i];
		return DataRow<T>.CreateFrom(ref Unsafe.Add(ref column.GetFirstRef(), _start));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<uint> GetChangedTicks(int index)
	{
		if (index >= _indices.Length)
			return Span<uint>.Empty;

		var i = _indices[index];
		if (i < 0)
			return Span<uint>.Empty;

		var arch = _archetypeIterator.Current;
		var column = arch.Columns![i];
		var span = column.ChangedTicks.AsSpan();
		if (!span.IsEmpty)
			span = span.Slice(_start, Count);
		return span;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<uint> GetAddedTicks(int index)
	{
		if (index >= _indices.Length)
			return Span<uint>.Empty;

		var i = _indices[index];
		if (i < 0)
			return Span<uint>.Empty;

		var arch = _archetypeIterator.Current;
		var column = arch.Columns![i];
		var span = column.AddedTicks.AsSpan();
		if (!span.IsEmpty)
			span = span.Slice(_start, Count);
		return span;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Span<T> Data<T>(int index) where T : struct
	{
		var arch = _archetypeIterator.Current;
		var span = arch.GetSpan<T>(_indices[index]);

		if (!span.IsEmpty)
			span = span.Slice(_start, Count);

		return span;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly ReadOnlySpan<EntityView> Entities()
	{
		var entities = _archetypeIterator.Current.Entities;

		if (!entities.IsEmpty)
			entities = entities.Slice(_start, Count);

		return entities;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Next()
	{
	REDO:
		if (!_archetypeIterator.MoveNext())
			return false;

		if (_archetypeIterator.Current.Count <= 0)
			goto REDO;

		ref readonly var arch = ref _archetypeIterator.Current;
		for (var i = 0; i < _indices.Length; ++i)
			_indices[i] = arch.GetComponentIndex(_termIds[i]);
		return true;
	}
}

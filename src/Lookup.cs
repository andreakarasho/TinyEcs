using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Collections.Extensions;

namespace TinyEcs;

[DebuggerDisplay("ID: {ID}, Size: {Size}, IsManaged: {IsManaged}")]
public readonly struct ComponentInfo
{
    public readonly EcsID ID;
    public readonly int Size;
    public readonly bool IsManaged;

    internal ComponentInfo(EcsID id, int size, bool isManaged)
    {
        ID = id;
        Size = size;
        IsManaged = isManaged;
    }
}

internal static class Lookup
{
	private static ulong _index = 0;

	private static readonly FastIdLookup<Func<int, Array?>> _arrayCreator = new ();
	private static readonly Dictionary<Type, QueryTerm> _typesConvertion = new();
	private static readonly Dictionary<Type, ComponentInfo> _componentInfosByType = new();
	private static readonly FastIdLookup<ComponentInfo> _components = new ();
	private static readonly Dictionary<Type, EcsID> _unmatchedType = new();

	public static Array? GetArray(EcsID hashcode, int count)
	{
		ref var fn = ref _arrayCreator.TryGet(hashcode, out var exists);
		if (exists)
			return fn(count);

		if (hashcode.IsPair())
		{
			(var first, var second) = hashcode.Pair();

			fn = ref _arrayCreator.TryGet(first, out exists)!;
			if (exists)
			{
				ref var cmp = ref _components.TryGet(first, out exists);
				if (exists && cmp.Size > 0)
					return fn(count);
			}

			fn = ref _arrayCreator.TryGet(second, out exists)!;
			if (exists)
			{
				ref var cmp = ref _components.TryGet(second, out exists);
				if (exists && cmp.Size > 0)
					return fn(count);
			}
		}

		EcsAssert.Panic(false, $"component not found with hashcode {hashcode}");
		return null;
	}

	public static ref ComponentInfo GetComponent(EcsID id, int size)
	{
		ref var result = ref _components.TryGet(id, out var exists);
		if (exists)
			return ref result;
		return ref Unsafe.NullRef<ComponentInfo>();
	}

	private static QueryTerm GetTerm(object obj)
	{
		if (!_typesConvertion.TryGetValue(obj.GetType(), out var term))
		{
			term = CreateUnmatchedTerm(obj);
		}
		return term;
	}

	[SkipLocalsInit]
    internal static class Component<T> where T : struct
	{
        public static readonly int Size = GetSize();
        public static readonly string Name = GetName();
        public static readonly ulong HashCode;
		public static readonly ComponentInfo Value;

		static Component()
		{
			if (typeof(IRelation).IsAssignableFrom(typeof(T)))
			{
				var relation = (IRelation)default(T);

				EcsID actionId = 0;
				EcsID targeId = 0;
				var actionType = relation.Action.GetType();
				var targetType = relation.Target.GetType();

				if (!_componentInfosByType.TryGetValue(actionType, out var actionCmp))
				{
					actionId = _unmatchedType[actionType];
				}
				else
				{
					actionId = actionCmp.ID;
				}

				if (!_componentInfosByType.TryGetValue(targetType, out var targetCmp))
				{
					targeId = _unmatchedType[targetType];
				}
				else
				{
					targeId = targetCmp.ID;
				}

				var pairId = IDOp.Pair(actionId, targeId);

				HashCode = pairId;
				Size = Math.Max(actionCmp.Size, targetCmp.Size);
			}
			else
			{
				if (_unmatchedType.Remove(typeof(T), out var id))
					HashCode = id;
				else
					HashCode = (ulong)System.Threading.Interlocked.Increment(ref Unsafe.As<ulong, int>(ref _index));
			}

			Value = new ComponentInfo(HashCode, Size, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
			_arrayCreator.Add(Value.ID, count => Size > 0 ? new T[count] : Array.Empty<T>());

			if (Size > 0)
			{
				_typesConvertion[typeof(T)] = new (Value.ID, TermOp.DataAccess);
				_typesConvertion[typeof(Optional<T>)] = new (Value.ID, TermOp.Optional);
			}

			_typesConvertion[typeof(With<T>)] = new (Value.ID, TermOp.With);
			_typesConvertion[typeof(Without<T>)] = new (Value.ID, TermOp.Without);

			_componentInfosByType[typeof(T)] = Value;
			_components.Add(Value.ID, Value);
		}

		private static string GetName()
		{
			var name = typeof(T).ToString()
				.Replace("[", "")
				.Replace("]", "");

			var indexOf = name.LastIndexOf('.');
			if (indexOf >= 0)
				name = name[(indexOf + 1) ..];

			return name;
		}

		private static int GetSize()
		{
			var size = RuntimeHelpers.IsReferenceOrContainsReferences<T>() ? IntPtr.Size : Unsafe.SizeOf<T>();

			if (size != 1)
				return size;

			// credit: BeanCheeseBurrito from Flecs.NET
			Unsafe.SkipInit<T>(out var t1);
			Unsafe.SkipInit<T>(out var t2);
			Unsafe.As<T, byte>(ref t1) = 0x7F;
			Unsafe.As<T, byte>(ref t2) = 0xFF;

			return ValueType.Equals(t1, t2) ? 0 : size;
		}
    }

	static void ParseTuple(ITuple tuple, List<IQueryTerm> terms, Func<Type, (bool, string?)> validate)
	{
		TermOp? op = tuple switch
		{
			IAtLeast => TermOp.AtLeastOne,
			IExactly => TermOp.Exactly,
			INone => TermOp.None,
			IOr => TermOp.Or,
			_ => null
		};

		var tmpTerms = terms;
		if (op.HasValue)
		{
			tmpTerms = new ();
		}

		for (var i = 0; i < tuple.Length; ++i)
		{
			if (tuple[i] is ITuple t)
			{
				ParseTuple(t, terms, validate);
				continue;
			}

			if (tuple[i] is IOr or)
			{
				ParseOr(or, terms, validate);
				continue;
			}

			if (!op.HasValue)
			{
				(var isValid, var errorMsg) = validate(tuple[i]!.GetType());
				EcsAssert.Panic(isValid, errorMsg);
			}

			var term = GetTerm(tuple[i]!);
			tmpTerms.Add(term);
		}

		if (op.HasValue)
		{
			terms.Add(new ContainerQueryTerm([.. tmpTerms], op.Value));
		}
	}

	static void ParseOr(IOr or, List<IQueryTerm> terms, Func<Type, (bool, string?)> validate)
	{
		var tmpTerms = new List<IQueryTerm>();
		ParseTuple(or.Value, tmpTerms, validate);
		terms.Add(new ContainerQueryTerm([.. tmpTerms], TermOp.Or));
	}

	static void ParseType<T>(List<IQueryTerm> terms, Func<Type, (bool, string?)> validate) where T : struct
	{
		var type = typeof(T);
		if (typeof(ITuple).IsAssignableFrom(type))
		{
			ParseTuple((ITuple)default(T), terms, validate);

			return;
		}

		(var isValid, var errorMsg) = validate(type);
		EcsAssert.Panic(isValid, errorMsg);

		if (_typesConvertion.TryGetValue(type, out var term))
		{
			terms.Add(term);

			return;
		}

		if (typeof(IOr).IsAssignableFrom(type))
		{
			ParseOr((IOr)default(T), terms, validate);

			return;
		}

		term = CreateUnmatchedTerm(default(T));
		terms.Add(term);
	}

	private static QueryTerm CreateUnmatchedTerm(object obj)
	{
		var type = obj.GetType();
		var op = TermOp.DataAccess;
		IRelation? relation = null;
		object? subObj = null;

		if (obj is IWith with)
		{
			op = TermOp.With;
			subObj = with.Value;
			if (with.Value is IRelation rel)
			{
				relation = rel;
			}
		}
		else if (obj is IWithout without)
		{
			op = TermOp.Without;
			subObj = without.Value;
			if (without.Value is IRelation rel)
			{
				relation = rel;
			}
		}
		else if (obj is IOptional optional)
		{
			op = TermOp.Optional;
			subObj = optional.Value;
			if (optional.Value is IRelation rel)
			{
				relation = rel;
			}
		}
		else if (obj is IRelation rel)
		{
			relation = rel;
		}

		ulong idx;
		if (relation != null)
		{
			EcsID actionId;
			EcsID targetId;

			if (!_componentInfosByType.TryGetValue(relation.Action.GetType(), out var action))
			{
				if (!_unmatchedType.TryGetValue(relation.Action.GetType(), out actionId))
				{
					actionId = CreateUnmatchedTerm(relation.Action).Id;
				}
			}
			else
			{
				actionId = action.ID;
			}

			if (!_componentInfosByType.TryGetValue(relation.Target.GetType(), out var target))
			{
				if (!_unmatchedType.TryGetValue(relation.Target.GetType(), out targetId))
				{
					targetId = CreateUnmatchedTerm(relation.Target).Id;
				}
			}
			else
			{
				targetId = target.ID;
			}

			EcsAssert.Panic(actionId.IsValid(), $"invalid action id {actionId}");
			EcsAssert.Panic(targetId.IsValid(), $"invalid target id {targetId}");

			idx = IDOp.Pair(actionId, targetId);
		}
		else
		{
			if (_unmatchedType.TryGetValue(subObj?.GetType() ?? type, out var id))
				idx = id;
		 	else
				idx = (ulong)System.Threading.Interlocked.Increment(ref Unsafe.As<ulong, int>(ref _index));
		}

		var term = new QueryTerm(idx, op);
		_typesConvertion.Add(type, term);
		_unmatchedType[subObj?.GetType() ?? type] = term.Id;
		return term;
	}

    internal static class Query<TQueryData, TQueryFilter>
		where TQueryData : struct
		where TQueryFilter : struct
	{
		public static readonly ImmutableArray<IQueryTerm> Terms;
		public static readonly ulong Hash;

		static Query()
		{
			var list = new List<IQueryTerm>();

			ParseType<TQueryData>(list, ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] s)
				=> (!s.GetInterfaces().Any(k => typeof(IFilter).IsAssignableFrom(k)), $"Filter '{s}' is not allowed in QueryData"));
			ParseType<TQueryFilter>(list, ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] s)
				=> (typeof(IFilter).IsAssignableFrom(s) && s.GetInterfaces().Any(k => typeof(IFilter) == k), $"You must use a IFilter type for '{s}'"));

			Terms = list.ToImmutableArray();

			list.Sort();
			var roll = IQueryTerm.GetHash(CollectionsMarshal.AsSpan(list));
			Hash = roll.Hash;
		}
	}

	internal static class Query<TQueryData> where TQueryData : struct
	{
		public static readonly ImmutableArray<IQueryTerm> Terms;
		public static readonly ulong Hash;

		static Query()
		{
			var list = new List<IQueryTerm>();

			ParseType<TQueryData>(list, ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] s)
				=> (!s.GetInterfaces().Any(k => typeof(IFilter).IsAssignableFrom(k)), $"Filter '{s}' is not allowed in QueryData"));

			Terms = list.ToImmutableArray();

			list.Sort();
			var roll = IQueryTerm.GetHash(CollectionsMarshal.AsSpan(list));
			Hash = roll.Hash;
		}
	}
}

internal sealed class FastIdLookup<TValue>
{
	const int COMPONENT_MAX_ID = 1024;

	struct Entry
	{
		public int Index;
		public EcsID Id;
	}


#if NET
	private readonly Dictionary<EcsID, TValue> _slowLookup = new();
#else
	private readonly DictionarySlim<EcsID, TValue> _slowLookup = new();
#endif
	private readonly TValue[] _fastLookup = new TValue[COMPONENT_MAX_ID];
	private readonly bool[] _fastLookupAdded = new bool[COMPONENT_MAX_ID];

	// private readonly List<Entry> _indices = new();
	// private readonly List<TValue> _values = new ();
	// private readonly Dictionary<EcsID, TValue> _dict = new();


	public int Count => _slowLookup.Count;

	public void Add(EcsID id, TValue value)
	{
		AddToFast(id, ref value);

#if NET
		CollectionsMarshal.GetValueRefOrAddDefault(_slowLookup, id, out _) = value;
#else
		_slowLookup.GetOrAddValueRef(id, out _) = value;
#endif
	}

	public ref TValue GetOrCreate(EcsID id, out bool exists)
	{
		if (id < COMPONENT_MAX_ID)
		{
			if (_fastLookupAdded[id])
			{
				exists = true;
				return ref _fastLookup[id];
			}
		}

#if NET
		ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(_slowLookup, id, out exists);
#else
		ref var val = ref _slowLookup.GetOrAddValueRef(id, out exists)!;
#endif

		if (!exists)
			val = ref AddToFast(id, ref val)!;

		return ref val!;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ref TValue TryGet(EcsID id, out bool exists)
	{
		if (id < COMPONENT_MAX_ID)
		{
			if (_fastLookupAdded[id])
			{
				exists = true;
				return ref _fastLookup[id];
			}

			exists = false;
			return ref Unsafe.NullRef<TValue>();
		}

#if NET
		ref var val = ref CollectionsMarshal.GetValueRefOrNullRef(_slowLookup, id);
		exists = !Unsafe.IsNullRef(ref val);
		return ref val;
#else
		return ref _slowLookup.GetOrNullRef(id, out exists);
#endif
	}

	public void Clear()
	{
		Array.Clear(_fastLookup, 0, _fastLookup.Length);
		_fastLookupAdded.AsSpan().Fill(false);
		_slowLookup.Clear();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref TValue AddToFast(EcsID id, ref TValue value)
	{
		if (id < COMPONENT_MAX_ID)
		{
			ref var p = ref _fastLookup[id];
			p = value;
			_fastLookupAdded[id] = true;

			return ref p!;
		}

		return ref value;
	}

	public IEnumerator<KeyValuePair<EcsID, TValue>> GetEnumerator()
		=> _slowLookup.GetEnumerator();
}

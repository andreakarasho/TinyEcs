using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;

[DebuggerDisplay("ID: {ID}, Size: {Size}")]
public readonly struct ComponentInfo
{
    public readonly EcsID ID;
    public readonly int Size;

    internal ComponentInfo(EcsID id, int size)
    {
        ID = id;
        Size = size;
    }
}

internal static class Lookup
{
	private static ulong _index = 0;

	private static readonly Dictionary<EcsID, Func<int, Array>> _arrayCreator = new ();
	private static readonly Dictionary<Type, QueryTerm> _typesConvertion = new();
	private static readonly Dictionary<Type, ComponentInfo> _componentInfosByType = new();
	private static readonly Dictionary<EcsID, ComponentInfo> _components = new ();
	private static readonly Dictionary<Type, EcsID> _unmatchedType = new();

	public static Array? GetArray(EcsID hashcode, int count)
	{
		if (_arrayCreator.TryGetValue(hashcode, out var fn))
			return fn(count);

		if (hashcode.IsPair)
		{
			(var first, var second) = hashcode.Pair;
			if (_arrayCreator.TryGetValue(first, out fn) && _components.TryGetValue(first, out var cmp) && cmp.Size > 0)
				return fn(count);
			if (_arrayCreator.TryGetValue(second, out fn) && _components.TryGetValue(second, out cmp) && cmp.Size > 0)
				return fn(count);
		}

		EcsAssert.Panic(false, $"component not found with hashcode {hashcode}");
		return null;
	}

	public static ComponentInfo GetComponent(EcsID id, int size)
	{
		if (!_components.TryGetValue(id, out var cmp))
		{
			cmp = new ComponentInfo(id, size);
			// TODO: i don't want to store non generics stuff
			//_components.Add(id, cmp);
		}

		return cmp;
	}

	private static QueryTerm GetTerm(Type type)
	{
		var ok = _typesConvertion.TryGetValue(type, out var term);
		if (!ok)
		{
			term = CreateUnmatchedTerm(type);

			// EcsAssert.Assert(ok,
			// 	_componentInfosByType.ContainsKey(type)
			// 		? $"The tag '{type}' cannot be used as query data"
			// 		: $"Component '{type}' not found! Try to register it using 'world.Entity<{type}>()'");
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
			if (typeof(ITuple).IsAssignableFrom(typeof(T)))
			{
				var tuple = (ITuple)default(T);
				EcsAssert.Panic(tuple.Length == 2, "Relations must be composed by 2 arguments only.");

				var firstId = _componentInfosByType[tuple[0]!.GetType()].ID;
				var secondId = _componentInfosByType[tuple[1]!.GetType()].ID;
				var pairId = IDOp.Pair(firstId, secondId);

				HashCode = pairId;
				Size = 0;

				if (_componentInfosByType.TryGetValue(tuple[1]!.GetType(), out var secondCmpInfo))
				{
					Size = secondCmpInfo.Size;
				}
			}
			else
			{
				if (_unmatchedType.Remove(typeof(T), out var id) ||
					_unmatchedType.Remove(typeof(Optional<T>), out id) ||
					_unmatchedType.Remove(typeof(With<T>), out id) ||
					_unmatchedType.Remove(typeof(Without<T>), out id))
					HashCode = id;
				else
					HashCode = (ulong)System.Threading.Interlocked.Increment(ref Unsafe.As<ulong, int>(ref _index));
			}

			Value = new ComponentInfo(HashCode, Size);
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

			var type = tuple[i]!.GetType();
			if (!op.HasValue)
			{
				(var isValid, var errorMsg) = validate(type);
				EcsAssert.Panic(isValid, errorMsg);
			}

			var term = GetTerm(type);
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

		term = CreateUnmatchedTerm(type);
		terms.Add(term);

		//EcsAssert.Panic(false, $"Type '{type}' is not registered. Register '{type}' using world.Entity<T>() or assign it to an entity.");
	}

	private static QueryTerm CreateUnmatchedTerm(Type type)
	{
		var op = TermOp.DataAccess;
		if (typeof(IWith).IsAssignableFrom(type))
		{
			op = TermOp.With;
		}
		else if (typeof(IWithout).IsAssignableFrom(type))
		{
			op = TermOp.Without;
		}
		else if (typeof(IOptional).IsAssignableFrom(type))
		{
			op = TermOp.Optional;
		}

		var term = new QueryTerm((ulong)++Lookup._index, op);
		_typesConvertion.Add(type, term);
		_unmatchedType.Add(type, term.Id);
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
			Hash = Hashing.Calculate(list.ToArray());
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
			Hash = Hashing.Calculate(list.ToArray());
		}
	}
}

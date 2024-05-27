using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace TinyEcs;


internal static class Lookup
{
	private static ulong _index = 0;

	private static readonly Dictionary<ulong, Func<int, Array>> _arrayCreator = new ();
	private static readonly Dictionary<Type, Term> _typesConvertion = new();
	private static readonly Dictionary<Type, ComponentInfo> _componentInfosByType = new();
	private static readonly Dictionary<EcsID, ComponentInfo> _components = new ();

	public static Array? GetArray(ulong hashcode, int count)
	{
		var ok = _arrayCreator.TryGetValue(hashcode, out var fn);
		EcsAssert.Assert(ok, $"component not found with hashcode {hashcode}");
		return fn?.Invoke(count) ?? null;
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

	private static Term GetTerm(Type type)
	{
		var ok = _typesConvertion.TryGetValue(type, out var term);
		if (!ok)
		{
			EcsAssert.Assert(ok,
				_componentInfosByType.ContainsKey(type)
					? $"The tag '{type}' cannot be used as query data"
					: $"Component '{type}' not found! Try to register it using 'world.Entity<{type}>()'");
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
				HashCode = (ulong)System.Threading.Interlocked.Increment(ref Unsafe.As<ulong, int>(ref _index));
			}

			Value = new ComponentInfo(HashCode, Size);
			_arrayCreator.Add(Value.ID, count => Size > 0 ? new T[count] : Array.Empty<T>());

			if (Size > 0)
			{
				_typesConvertion.Add(typeof(T), new (Value.ID, TermOp.With));
				_typesConvertion.Add(typeof(Optional<T>), new (Value.ID, TermOp.Optional));
			}

			_typesConvertion.Add(typeof(With<T>), new (Value.ID, TermOp.With));
			_typesConvertion.Add(typeof(Without<T>), new (Value.ID, TermOp.Without));


			_typesConvertion.Add(typeof(Or<T>), new (Value.ID, TermOp.Or));
			_typesConvertion.Add(typeof(Or<With<T>>), new ([ (Value.ID, TermOp.With) ], TermOp.Or));
			_typesConvertion.Add(typeof(Or<Without<T>>), new ([ (Value.ID, TermOp.Without) ], TermOp.Or));
			_typesConvertion.Add(typeof(Or<Optional<T>>), new ([ (Value.ID, TermOp.Optional) ], TermOp.Or));


			_componentInfosByType.Add(typeof(T), Value);

			_components.Add(Value.ID, Value);
		}

		private static string GetName()
		{
			var name = typeof(T).ToString();

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

	static void ParseTuple(ITuple tuple, List<Term> terms, Func<Type, (bool, string?)> validate)
	{
		var mainType = tuple.GetType();
		TermOp? op = null;
		var tmpTerms = terms;

		if (typeof(IAtLeast).IsAssignableFrom(mainType))
		{
			op = TermOp.AtLeastOne;
			tmpTerms = new ();
		}
		else if (typeof(IExactly).IsAssignableFrom(mainType))
		{
			op = TermOp.Exactly;
			tmpTerms = new ();
		}
		else if (typeof(INone).IsAssignableFrom(mainType))
		{
			op = TermOp.None;
			tmpTerms = new ();
		}
		else if (typeof(IOr).IsAssignableFrom(mainType))
		{
			op = TermOp.Or;
			tmpTerms = new ();
		}

		for (var i = 0; i < tuple.Length; ++i)
		{
			var type = tuple[i]!.GetType();

			if (typeof(ITuple).IsAssignableFrom(type))
			{
				ParseTuple((ITuple)tuple[i]!, terms, validate);
				continue;
			}

			if (typeof(IOr).IsAssignableFrom(type))
			{
				if (ParseOr((IOr)tuple[i]!, terms, validate))
					continue;
			}

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
			terms.Add(new Term(tmpTerms.SelectMany(s => s.IDs), op.Value));
		}
	}

	static bool ParseOr(IOr or, List<Term> terms, Func<Type, (bool, string?)> validate)
	{
		var orValue = or.Value;
		var orValueType = orValue.GetType();
		var tmpTerms = new List<Term>();

		if (typeof(ITuple).IsAssignableFrom(orValueType))
		{
			ParseTuple((ITuple)orValue, tmpTerms, (s) => (true, ""));

			terms.Add(new Term(tmpTerms.SelectMany(s => s.IDs), TermOp.Or));
			return true;
		}

		(var isValid, var errorMsg) = validate(orValueType);
		EcsAssert.Panic(isValid, errorMsg);

		if (_typesConvertion.TryGetValue(orValueType, out var term))
		{
			tmpTerms.Add(term);
			terms.Add(new Term(tmpTerms.SelectMany(s => s.IDs), TermOp.Or));
			return true;
		}

		return false;
	}

	static void ParseType<T>(List<Term> terms, Func<Type, (bool, string?)> validate) where T : struct
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
			if (ParseOr((IOr)default(T), terms, validate))
				return;
		}

		EcsAssert.Panic(false, $"Type '{type}' is not registered. Register '{type}' using world.Entity<T>() or assign it to an entity.");
	}

    internal static class Query<TQueryData, TQueryFilter>
		where TQueryData : struct
		where TQueryFilter : struct
	{
		public static readonly ImmutableArray<Term> Terms;
		public static readonly ulong Hash;

		static Query()
		{
			var list = new List<Term>();

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
		public static readonly ImmutableArray<Term> Terms;
		public static readonly ulong Hash;

		static Query()
		{
			var list = new List<Term>();

			ParseType<TQueryData>(list, ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] s)
				=> (!s.GetInterfaces().Any(k => typeof(IFilter).IsAssignableFrom(k)), $"Filter '{s}' is not allowed in QueryData"));

			Terms = list.ToImmutableArray();

			list.Sort();
			Hash = Hashing.Calculate(list.ToArray());
		}
	}
}

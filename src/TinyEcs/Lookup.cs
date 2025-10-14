using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Collections.Extensions;

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
	private static int _index = 0;

	private static readonly FastIdLookup<Func<int, Array?>> _arrayCreator = new ();
	private static readonly FastIdLookup<ComponentInfo> _components = new ();

	public static ref readonly ComponentInfo GetComponent(EcsID id)
	{
		ref readonly var cmp = ref _components.TryGet(id, out var exists);
		if (!exists)
			EcsAssert.Panic(false, $"component not found with hashcode {id}");
		return ref cmp;
	}

	public static Array? GetArray(EcsID hashcode, int count)
	{
		ref var fn = ref _arrayCreator.TryGet(hashcode, out var exists);
		if (exists)
			return fn(count);

#if USE_PAIR
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
#endif

		EcsAssert.Panic(false, $"component not found with hashcode {hashcode}");
		return null;
	}


	[SkipLocalsInit]
    internal static class Component<T> where T : struct
	{
        public static readonly int Size = GetSize();
        public static readonly string Name = GetName();
        public static readonly ulong HashCode = (ulong)System.Threading.Interlocked.Increment(ref _index);
		public static readonly ComponentInfo Value = new (HashCode, Size);

		static Component()
		{
			_arrayCreator.Add(Value.ID, count => Size > 0 ? new T[count] : Array.Empty<T>());
			_components.Add(Value.ID, Value);
		}

		// credit: BeanCheeseBurrito from Flecs.NET
		private static string GetName()
		{
			var name = typeof(T).ToString();
			name = name
				.Replace('+', '.')
				.Replace('[', '<')
				.Replace(']', '>');

			int start = 0;
			int current = 0;
			bool skip = false;

			var stringBuilder = new StringBuilder();

			foreach (char c in name)
			{
				if (skip && (c == '<' || c == '.'))
				{
					start = current;
					skip = false;
				}
				else if (!skip && c == '`')
				{
					stringBuilder.Append(name.AsSpan(start, current - start));
					skip = true;
				}

				current++;
			}

			var str = stringBuilder.Append(name.AsSpan(start)).ToString();
			return str;
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
}

internal sealed class FastIdLookup<TValue> where TValue : notnull
{
	// private readonly EntitySparseSet<TValue> _set = new ();
	// private readonly DictionarySlim<EcsID, TValue> _slowLookup = new ();

	// public void Add(EcsID id, TValue value)
	// {
	// 	if (id.IsPair())
	// 	{
	// 		_slowLookup.GetOrAddValueRef(id, out _) = value;
	// 	}
	// 	else
	// 	{
	// 		_set.Add(id, value);
	// 	}
	// }

	// public ref TValue GetOrCreate(EcsID id, out bool exists)
	// {
	// 	if (id.IsPair())
	// 	{
	// 		return ref _slowLookup.GetOrAddValueRef(id, out exists);
	// 	}

	// 	ref var val = ref _set.Get(id);
	// 	exists = !Unsafe.IsNullRef(ref val);

	// 	if (!exists)
	// 		return ref _set.Add(id, default);
	// 	return ref val;
	// }

	// public ref TValue TryGet(EcsID id, out bool exists)
	// {
	// 	if (id.IsPair())
	// 	{
	// 		return ref _slowLookup.GetOrNullRef(id, out exists);
	// 	}

	// 	ref var val = ref _set.Get(id);
	// 	exists = !Unsafe.IsNullRef(ref val);
	// 	return ref val;
	// }

	// public void Clear()
	// {
	// 	_set.Clear();
	// 	_slowLookup.Clear();
	// }

    private const int COMPONENT_MAX_ID = 1024;

#if NET
    private readonly Dictionary<ulong, TValue> _slowLookup = new();
#else
    private readonly DictionarySlim<ulong, TValue> _slowLookup = new();
#endif
    private readonly TValue[] _fastLookup = new TValue[COMPONENT_MAX_ID];
    private readonly bool[] _fastLookupAdded = new bool[COMPONENT_MAX_ID];
    private int _fastLookupCount = 0;

    public int Count => _slowLookup.Count + _fastLookupCount;

    public void Add(ulong id, TValue value)
    {
        if (id < (ulong)COMPONENT_MAX_ID)
        {
            if (!_fastLookupAdded[id])
            {
                _fastLookupCount++;
                _fastLookupAdded[id] = true;
            }
            _fastLookup[id] = value;
        }
        else
        {
#if NET
            CollectionsMarshal.GetValueRefOrAddDefault(_slowLookup, id, out _) = value;
#else
            _slowLookup.GetOrAddValueRef(id, out _) = value;
#endif
        }
    }

    public ref TValue GetOrCreate(ulong id, out bool exists)
    {
        if (id < (ulong)COMPONENT_MAX_ID)
        {
            if (_fastLookupAdded[id])
            {
                exists = true;
                return ref _fastLookup[id];
            }

            exists = false;
            return ref AddToFast(id);
        }

#if NET
        ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(_slowLookup, id, out exists);
#else
        ref var val = ref _slowLookup.GetOrAddValueRef(id, out exists)!;
#endif

        return ref val;
    }


    public ref TValue TryGet(ulong id, out bool exists)
    {
        if (id < (ulong)COMPONENT_MAX_ID)
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
        Array.Fill(_fastLookupAdded, false);
        _fastLookupCount = 0;
        _slowLookup.Clear();
    }


    private ref TValue AddToFast(ulong id)
    {
        ref var value = ref _fastLookup[id];
        if (!_fastLookupAdded[id])
        {
            _fastLookupCount++;
            _fastLookupAdded[id] = true;
        }
        return ref value;
    }

    public IEnumerator<KeyValuePair<ulong, TValue>> GetEnumerator()
    {
        foreach (var pair in _slowLookup)
            yield return pair;

        for (ulong i = 0; i < (ulong)COMPONENT_MAX_ID; i++)
        {
            if (_fastLookupAdded[i])
                yield return new KeyValuePair<ulong, TValue>(i, _fastLookup[i]);
        }
    }
}


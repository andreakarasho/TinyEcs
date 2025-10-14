using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyEcs.Bevy;

/// <summary>
/// Used in query filters to find entities with the corrisponding component/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct With<T> : IFilter<With<T>>, IQueryFilterAccess
	where T : struct
{
	private static readonly System.Type[] s_components = { typeof(T) };
	private static readonly System.Type[] s_emptyTypes = System.Array.Empty<System.Type>();
	public static System.ReadOnlySpan<System.Type> ReadComponents => s_components;
	public static System.ReadOnlySpan<System.Type> WriteComponents => s_emptyTypes;
	[UnscopedRef]
	ref With<T> IQueryIterator<With<T>>.Current => ref this;

	public static void Build(QueryBuilder builder)
	{
		builder.With<T>();
	}

	static With<T> IFilter<With<T>>.CreateIterator(QueryIterator iterator)
	{
		return new With<T>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly With<T> IQueryIterator<With<T>>.GetEnumerator()
	{
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly bool IQueryIterator<With<T>>.MoveNext()
	{
		return true;
	}

	public readonly void SetTicks(uint lastRun, uint thisRun) { }
}

/// <summary>
/// Used in query filters to find entities without the corrisponding component/tag.
/// </summary>
/// <typeparam name="T"></typeparam>
public ref struct Without<T> : IFilter<Without<T>>, IQueryFilterAccess
	where T : struct
{
	private static readonly System.Type[] s_emptyTypes = System.Array.Empty<System.Type>();
	public static System.ReadOnlySpan<System.Type> ReadComponents => s_emptyTypes;
	public static System.ReadOnlySpan<System.Type> WriteComponents => s_emptyTypes;
	[UnscopedRef]
	ref Without<T> IQueryIterator<Without<T>>.Current => ref this;

	public static void Build(QueryBuilder builder)
	{
		builder.Without<T>();
	}

	static Without<T> IFilter<Without<T>>.CreateIterator(QueryIterator iterator)
	{
		return new();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly Without<T> IQueryIterator<Without<T>>.GetEnumerator()
	{
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly bool IQueryIterator<Without<T>>.MoveNext()
	{
		return true;
	}

	public readonly void SetTicks(uint lastRun, uint thisRun) { }
}

/// <summary>
/// Used in query filters to find entities with or without the corrisponding component/tag.<br/>
/// You would Unsafe.IsNullRef&lt;T&gt;(); to check if the value has been found.
/// </summary>
/// <typeparam name="T"></typeparam>
public ref struct Optional<T> : IFilter<Optional<T>>, IQueryFilterAccess
	where T : struct
{
	private static readonly System.Type[] s_components = { typeof(T) };
	private static readonly System.Type[] s_emptyTypes = System.Array.Empty<System.Type>();
	public static System.ReadOnlySpan<System.Type> ReadComponents => s_components;
	public static System.ReadOnlySpan<System.Type> WriteComponents => s_emptyTypes;
	[UnscopedRef]
	ref Optional<T> IQueryIterator<Optional<T>>.Current => ref this;

	public static void Build(QueryBuilder builder)
	{
		builder.Optional<T>();
	}

	static Optional<T> IFilter<Optional<T>>.CreateIterator(QueryIterator iterator)
	{
		return new();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly Optional<T> IQueryIterator<Optional<T>>.GetEnumerator()
	{
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly bool IQueryIterator<Optional<T>>.MoveNext()
	{
		return true;
	}

	public readonly void SetTicks(uint lastRun, uint thisRun) { }
}

/// <summary>
/// Used in query filters to find entities with components that have changed.
/// </summary>
/// <typeparam name="T"></typeparam>
public ref struct Changed<T> : IFilter<Changed<T>>, IQueryFilterAccess
	where T : struct
{
	private static readonly System.Type[] s_components = { typeof(T) };
	private static readonly System.Type[] s_emptyTypes = System.Array.Empty<System.Type>();
	public static System.ReadOnlySpan<System.Type> ReadComponents => s_components;
	public static System.ReadOnlySpan<System.Type> WriteComponents => s_emptyTypes;
	private QueryIterator _iterator;
	private Ptr<uint> _stateRow;
	private int _row, _count;
	private nint _size;
	private uint _lastRun, _thisRun;

	private Changed(QueryIterator iterator)
	{
		_iterator = iterator;
		_row = -1;
		_count = -1;
		_lastRun = 0;
		_thisRun = 0;
	}

	[UnscopedRef]
	ref Changed<T> IQueryIterator<Changed<T>>.Current => ref this;

	public static void Build(QueryBuilder builder)
	{
		builder.With<T>();
	}

	static Changed<T> IFilter<Changed<T>>.CreateIterator(QueryIterator iterator)
	{
		return new(iterator);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly Changed<T> IQueryIterator<Changed<T>>.GetEnumerator()
	{
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool IQueryIterator<Changed<T>>.MoveNext()
	{
		if (++_row >= _count)
		{
			if (!_iterator.Next())
				return false;

			_row = 0;
			_count = _iterator.Count;
			var index = _iterator.GetColumnIndexOf<T>();
			var states = _iterator.GetChangedTicks(index);

			if (states.IsEmpty)
			{
				_stateRow.Ref = ref Unsafe.NullRef<uint>();
				_size = 0;
			}
			else
			{
				_stateRow.Ref = ref MemoryMarshal.GetReference(states);
				_size = Unsafe.SizeOf<uint>();
			}
		}
		else
		{
			_stateRow.Ref = ref Unsafe.AddByteOffset(ref _stateRow.Ref, _size);
		}

		return _size > 0 && _stateRow.Ref >= _lastRun && _stateRow.Ref < _thisRun;
	}

	public void SetTicks(uint lastRun, uint thisRun)
	{
		_lastRun = lastRun;
		_thisRun = thisRun;
	}
}

/// <summary>
/// Used in query filters to find entities with components that have added.
/// </summary>
/// <typeparam name="T"></typeparam>
public ref struct Added<T> : IFilter<Added<T>>, IQueryFilterAccess
	where T : struct
{
	private static readonly System.Type[] s_components = { typeof(T) };
	private static readonly System.Type[] s_emptyTypes = System.Array.Empty<System.Type>();
	public static System.ReadOnlySpan<System.Type> ReadComponents => s_components;
	public static System.ReadOnlySpan<System.Type> WriteComponents => s_emptyTypes;
	private QueryIterator _iterator;
	private Ptr<uint> _stateRow;
	private int _row, _count;
	private nint _size;
	private uint _lastRun, _thisRun;

	private Added(QueryIterator iterator)
	{
		_iterator = iterator;
		_row = -1;
		_count = -1;
		_lastRun = 0;
		_thisRun = 0;
	}

	[UnscopedRef]
	ref Added<T> IQueryIterator<Added<T>>.Current => ref this;

	public static void Build(QueryBuilder builder)
	{
		builder.With<T>();
	}

	static Added<T> IFilter<Added<T>>.CreateIterator(QueryIterator iterator)
	{
		return new(iterator);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly Added<T> IQueryIterator<Added<T>>.GetEnumerator()
	{
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool IQueryIterator<Added<T>>.MoveNext()
	{
		if (++_row >= _count)
		{
			if (!_iterator.Next())
				return false;

			_row = 0;
			_count = _iterator.Count;
			var index = _iterator.GetColumnIndexOf<T>();
			var states = _iterator.GetAddedTicks(index);

			if (states.IsEmpty)
			{
				_stateRow.Ref = ref Unsafe.NullRef<uint>();
				_size = 0;
			}
			else
			{
				_stateRow.Ref = ref MemoryMarshal.GetReference(states);
				_size = Unsafe.SizeOf<uint>();
			}
		}
		else
		{
			_stateRow.Ref = ref Unsafe.AddByteOffset(ref _stateRow.Ref, _size);
		}

		return _size > 0 && _stateRow.Ref >= _lastRun && _stateRow.Ref < _thisRun;
	}

	public void SetTicks(uint lastRun, uint thisRun)
	{
		_lastRun = lastRun;
		_thisRun = thisRun;
	}
}

/// <summary>
/// Used in query filters to mark components as changed without modifying them.
/// </summary>
/// <typeparam name="T"></typeparam>
public ref struct MarkChanged<T> : IFilter<MarkChanged<T>>, IQueryFilterAccess
	where T : struct
{
	private static readonly System.Type[] s_components = { typeof(T) };
	public static System.ReadOnlySpan<System.Type> ReadComponents => s_components;
	public static System.ReadOnlySpan<System.Type> WriteComponents => s_components;
	private QueryIterator _iterator;
	private Ptr<uint> _stateRow;
	private int _row, _count;
	private nint _size;
	private uint _lastRun, _thisRun;

	private MarkChanged(QueryIterator iterator)
	{
		_iterator = iterator;
		_row = -1;
		_count = -1;
		_lastRun = 0;
		_thisRun = 0;
	}

	[UnscopedRef]
	ref MarkChanged<T> IQueryIterator<MarkChanged<T>>.Current => ref this;

	public static void Build(QueryBuilder builder)
	{
		// builder.With<T>();
	}

	static MarkChanged<T> IFilter<MarkChanged<T>>.CreateIterator(QueryIterator iterator)
	{
		return new(iterator);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	readonly MarkChanged<T> IQueryIterator<MarkChanged<T>>.GetEnumerator()
	{
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	bool IQueryIterator<MarkChanged<T>>.MoveNext()
	{
		if (++_row >= _count)
		{
			if (!_iterator.Next())
				return false;

			_row = 0;
			_count = _iterator.Count;
			var index = _iterator.GetColumnIndexOf<T>();
			var states = _iterator.GetChangedTicks(index);

			if (states.IsEmpty)
			{
				_stateRow.Ref = ref Unsafe.NullRef<uint>();
				_size = 0;
			}
			else
			{
				_stateRow.Ref = ref MemoryMarshal.GetReference(states);
				_size = Unsafe.SizeOf<uint>();
			}
		}
		else
		{
			_stateRow.Ref = ref Unsafe.AddByteOffset(ref _stateRow.Ref, _size);
		}

		if (_size > 0)
		{
			_stateRow.Ref = _thisRun;
		}

		return true;
	}

	public void SetTicks(uint lastRun, uint thisRun)
	{
		_lastRun = lastRun;
		_thisRun = thisRun;
	}
}

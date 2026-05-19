namespace TinyEcs;

[SkipLocalsInit]
public ref struct Ptr<T> where T : struct
{
	public ref T Ref;

	public readonly bool IsValid() => !Unsafe.IsNullRef(ref Ref);
}

[SkipLocalsInit]
public readonly ref struct PtrRO<T> where T : struct
{
	public PtrRO(ref readonly T r) => Ref = ref r;

	public readonly ref readonly T Ref;
}


#if NET9_0_OR_GREATER
public interface IDataRow<out TSelf, T>
	where TSelf : IDataRow<TSelf, T>, allows ref struct
	where T : struct
{
	static abstract TSelf CreateFrom(ref T baseRef);
	static abstract TSelf CreateAbsent();

	Ptr<T> Value { get; }
	void Next();
}
#endif

[SkipLocalsInit]
public ref struct DataRow<T>
#if NET9_0_OR_GREATER
	: IDataRow<DataRow<T>, T>
#endif
	where T : struct
{
	private Ptr<T> _value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public DataRow(ref T value)
	{
		_value.Ref = ref value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static DataRow<T> CreateFrom(ref T baseRef) => new(ref baseRef);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static DataRow<T> CreateAbsent()
	{
		DataRow<T> row = default;
		row._value.Ref = ref Unsafe.NullRef<T>();
		return row;
	}

	public Ptr<T> Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _value; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Next() => _value.Ref = ref Unsafe.Add(ref _value.Ref, 1);
}

[SkipLocalsInit]
public ref struct DataRowNullRef<T>
#if NET9_0_OR_GREATER
	: IDataRow<DataRowNullRef<T>, T>
#endif
	where T : struct
{
	private Ptr<T> _value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public DataRowNullRef()
	{
		_value.Ref = ref Unsafe.NullRef<T>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static DataRowNullRef<T> CreateFrom(ref T baseRef) => new();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static DataRowNullRef<T> CreateAbsent() => new();

	public Ptr<T> Value { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _value; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Next()
	{
		// nop
	}
}

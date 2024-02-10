#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
	/// <summary>
	///     Forwards the SkipLocalInit to .NetStandard2.1.
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Module
		| AttributeTargets.Class
		| AttributeTargets.Struct
		| AttributeTargets.Interface
		| AttributeTargets.Constructor
		| AttributeTargets.Method
		| AttributeTargets.Property
		| AttributeTargets.Event, Inherited = false)]
	internal sealed class SkipLocalsInitAttribute : Attribute
	{
	}


	//
	// Summary:
	//     Indicates that a parameter captures the expression passed for another parameter
	//     as a string.
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	internal sealed class CallerArgumentExpressionAttribute : Attribute
	{
		//
		// Summary:
		//     Initializes a new instance of the System.Runtime.CompilerServices.CallerArgumentExpressionAttribute
		//     class.
		//
		// Parameters:
		//   parameterName:
		//     The name of the parameter whose expression should be captured as a string.
		public CallerArgumentExpressionAttribute(string parameterName) { }
	}
}
#endif


#if NETSTANDARD2_1
// internal unsafe readonly ref struct Ref<T>
// {
//     private readonly T* _value;
//
//     internal Ref(ref T value)
//     {
//         _value = Unsafe.AsPointer(ref value);
//     }
//
//     public ref T Value => ref *_value;
// }

namespace System.Runtime.InteropServices
{
	public static class NativeMemory
	{
		public static unsafe void* Realloc(void* data, nuint size)
		{
			return (void*)Marshal.ReAllocHGlobal((IntPtr)data, (IntPtr)((UIntPtr)size).ToPointer());
		}

		public static unsafe void Free(void* data)
		{
			Marshal.FreeHGlobal((IntPtr)data);
		}

		public static unsafe void* AllocZeroed(nuint count, nuint typeSize)
		{
			var data = (void*) Marshal.AllocHGlobal((IntPtr)((UIntPtr)(count * typeSize)).ToPointer());

			return data;
		}

		public static unsafe void* Alloc(nuint count, nuint typeSize)
		{
			var data = (void*) Marshal.AllocHGlobal((IntPtr)((UIntPtr)(count * typeSize)).ToPointer());

			return data;
		}
	}

	public static class CollectionsMarshal
	{
		public static Span<T> AsSpan<T>(List<T>? list)
		{
			if (list == null)
				return Span<T>.Empty;

			return new Span<T>(Unsafe.As<StrongBox<T[]>>(list).Value, 0, list.Count);
		}
	}

	// public static class MemoryMarshal
	// {
	// 	// public static ref T GetReference<T>(Span<T> span)
	// 	// {
	// 	// 	if (span.IsEmpty)
	// 	// 		return ref Unsafe.NullRef<T>();
	// 	// 	return ref span[0];
	// 	// }
	// 	//
	// 	// public static ref T GetArrayDataReference<T>(T[]? array)
	// 	// {
	// 	// 	if (array == null || array.Length == 0)
	// 	// 		return ref Unsafe.NullRef<T>();
	// 	// 	return ref array[0];
	// 	// }
	// }
}

public static class SortExtensions
{
	public static void Sort<T>(this Span<T> span) where T : IComparable<T>
	{
		for (int i = 0; i < span.Length - 1; i++)
		{
			for (int j = 0; j < span.Length - i - 1; j++)
			{
				if (span[j].CompareTo(span[j + 1]) > 0)
				{
					// Swap the elements
					T temp = span[j];
					span[j] = span[j + 1];
					span[j + 1] = temp;
				}
			}
		}
	}

	public static void Sort<T>(this Span<T> span, IComparer<T> comparer)
	{
		for (int i = 0; i < span.Length - 1; i++)
		{
			for (int j = 0; j < span.Length - i - 1; j++)
			{
				if (comparer.Compare(span[j], span[j + 1]) > 0)
				{
					// Swap the elements
					T temp = span[j];
					span[j] = span[j + 1];
					span[j + 1] = temp;
				}
			}
		}
	}
}
#endif

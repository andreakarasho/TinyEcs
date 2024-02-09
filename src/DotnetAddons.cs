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
internal unsafe readonly ref struct Ref<T> 
{
    private readonly T* _value;

    internal Ref(ref T value)
    {
        _value = Unsafe.AsPointer(ref value);
    }

    public ref T Value => ref *_value;
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
}
#endif

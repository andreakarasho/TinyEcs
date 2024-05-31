#if !NET7_0_OR_GREATER
namespace TinyEcs
{
	public readonly ref struct Ref<T>
	{
		//
		// Summary:
		//     The 1-length System.Span`1 instance used to track the target T value.
		internal readonly Span<T> Span;

		//
		// Summary:
		//     Gets the T reference represented by the current CommunityToolkit.HighPerformance.Ref`1
		//     instance.
		public ref T Value
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return ref MemoryMarshal.GetReference(Span);
			}
		}

		//
		// Summary:
		//     Initializes a new instance of the CommunityToolkit.HighPerformance.Ref`1 struct.
		//
		//
		// Parameters:
		//   value:
		//     The reference to the target T value.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Ref(ref T value)
		{
			Span = MemoryMarshal.CreateSpan(ref value, 1);
		}

		//
		// Summary:
		//     Initializes a new instance of the CommunityToolkit.HighPerformance.Ref`1 struct.
		//
		//
		// Parameters:
		//   pointer:
		//     The pointer to the target value.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe Ref(void* pointer)
			: this(ref Unsafe.AsRef<T>(pointer))
		{
		}

		//
		// Summary:
		//     Implicitly gets the T value from a given CommunityToolkit.HighPerformance.Ref`1
		//     instance.
		//
		// Parameters:
		//   reference:
		//     The input CommunityToolkit.HighPerformance.Ref`1 instance.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator T(Ref<T> reference)
		{
			return reference.Value;
		}
	}
}
#endif


#if !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
	/// <summary>
	///     Used to indicate a byref escapes and is not scoped.
	/// </summary>
	/// <remarks>
	///     There are several cases where the C# compiler treats a <see langword="ref"/> as implicitly
	///     <see langword="scoped"/> - where the compiler does not allow the <see langword="ref"/> to escape the method.
	///     <br/>
	///     For example:
	///     <list type="number">
	///         <item><see langword="this"/> for <see langword="struct"/> instance methods.</item>
	///         <item><see langword="ref"/> parameters that refer to <see langword="ref"/> <see langword="struct"/> types.</item>
	///         <item><see langword="out"/> parameters.</item>
	///     </list>
	///     This attribute is used in those instances where the <see langword="ref"/> should be allowed to escape.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public sealed class UnscopedRefAttribute : Attribute
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="UnscopedRefAttribute"/> class.
		/// </summary>
		public UnscopedRefAttribute() { }
	}
}

namespace System.Diagnostics.CodeAnalysis
{
    //
    // Summary:
    //     Indicates that certain members on a specified System.Type are accessed dynamically,
    //     for example, through System.Reflection.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, Inherited = false)]
    public sealed class DynamicallyAccessedMembersAttribute : Attribute
    {
        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute
        //     class with the specified member types.
        //
        // Parameters:
        //   memberTypes:
        //     The types of the dynamically accessed members.
        public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes);

        //
        // Summary:
        //     Gets the System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes that
        //     specifies the type of dynamically accessed members.
        public DynamicallyAccessedMemberTypes MemberTypes { get; }
    }

	//
    // Summary:
    //     Specifies the types of members that are dynamically accessed. This enumeration
    //     has a System.FlagsAttribute attribute that allows a bitwise combination of its
    //     member values.
    [Flags]
    public enum DynamicallyAccessedMemberTypes
    {
        //
        // Summary:
        //     Specifies all members.
        All = -1,
        //
        // Summary:
        //     Specifies no members.
        None = 0,
        //
        // Summary:
        //     Specifies the default, parameterless public constructor.
        PublicParameterlessConstructor = 1,
        //
        // Summary:
        //     Specifies all public constructors.
        PublicConstructors = 3,
        //
        // Summary:
        //     Specifies all non-public constructors.
        NonPublicConstructors = 4,
        //
        // Summary:
        //     Specifies all public methods.
        PublicMethods = 8,
        //
        // Summary:
        //     Specifies all non-public methods.
        NonPublicMethods = 16,
        //
        // Summary:
        //     Specifies all public fields.
        PublicFields = 32,
        //
        // Summary:
        //     Specifies all non-public fields.
        NonPublicFields = 64,
        //
        // Summary:
        //     Specifies all public nested types.
        PublicNestedTypes = 128,
        //
        // Summary:
        //     Specifies all non-public nested types.
        NonPublicNestedTypes = 256,
        //
        // Summary:
        //     Specifies all public properties.
        PublicProperties = 512,
        //
        // Summary:
        //     Specifies all non-public properties.
        NonPublicProperties = 1024,
        //
        // Summary:
        //     Specifies all public events.
        PublicEvents = 2048,
        //
        // Summary:
        //     Specifies all non-public events.
        NonPublicEvents = 4096,
        //
        // Summary:
        //     Specifies all interfaces implemented by the type.
        Interfaces = 8192
    }
}

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

	/// <summary>
	/// Used to indicate to the compiler that a method should be called
	/// in its containing module's initializer.
	/// </summary>
	/// <remarks>
	/// When one or more valid methods
	/// with this attribute are found in a compilation, the compiler will
	/// emit a module initializer which calls each of the attributed methods.
	///
	/// Certain requirements are imposed on any method targeted with this attribute:
	/// - The method must be `static`.
	/// - The method must be an ordinary member method, as opposed to a property accessor, constructor, local function, etc.
	/// - The method must be parameterless.
	/// - The method must return `void`.
	/// - The method must not be generic or be contained in a generic type.
	/// - The method's effective accessibility must be `internal` or `public`.
	///
	/// The specification for module initializers in the .NET runtime can be found here:
	/// https://github.com/dotnet/runtime/blob/master/docs/design/specs/Ecma-335-Augments.md#module-initializer
	/// </remarks>
	[AttributeUsage(validOn: AttributeTargets.Method, Inherited = false)]
	public sealed class ModuleInitializerAttribute : Attribute
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

	internal static class IsExternalInit {}
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

public static class DotnetExtensions
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

	// public static System.Collections.Immutable.ImmutableArray<TSource> ToImmutableArray<TSource>(this IEnumerable<TSource> items)
	// {
	// 	return System.Collections.Immutable.ImmutableArray.CreateRange(items);
	// }
}
#endif

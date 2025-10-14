namespace TinyEcs;

internal static class EcsAssert
{
	[Conditional("DEBUG")]
	public static void Assert(bool condition, [CallerArgumentExpression(nameof(condition))] string? message = null)
	{
		if (!condition)
			throw new Exception(message);
	}

	public static void Panic(bool condition, string? message = null)
	{
		if (!condition)
			throw new Exception($"Panic! {message}");
	}
}

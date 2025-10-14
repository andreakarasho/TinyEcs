using System;

namespace TinyEcs;

public interface IQueryComponentAccess
{
	static abstract ReadOnlySpan<Type> ReadComponents { get; }
	static abstract ReadOnlySpan<Type> WriteComponents { get; }
}

public interface IQueryFilterAccess
{
	static abstract ReadOnlySpan<Type> ReadComponents { get; }
	static abstract ReadOnlySpan<Type> WriteComponents { get; }
}

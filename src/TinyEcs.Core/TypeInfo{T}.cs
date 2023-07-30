namespace TinyEcs;

static class TypeInfo<T> where T : unmanaged
{
	public static unsafe readonly int Size = sizeof(T);
	public static readonly int Hash = typeof(T).GetHashCode();
}

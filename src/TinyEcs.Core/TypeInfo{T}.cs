namespace TinyEcs;

static class TypeInfo<T> where T : unmanaged
{
	public static unsafe readonly int Size = typeof(T).GetFields().Length == 0 && typeof(T).GetProperties().Length == 0 ? 0 : sizeof(T);
	public static readonly int Hash = typeof(T).GetHashCode();
}

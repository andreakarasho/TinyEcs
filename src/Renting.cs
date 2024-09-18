namespace TinyEcs;

static class Renting<T> where T : new()
{
	private static readonly Stack<T> _stack = new Stack<T>();

	public static T Rent()
	{
		if (_stack.TryPop(out var val))
		{
			return val;
		}

		val = new T();
		return val;
	}

	public static void Return(T val)
	{
		_stack.Push(val);
	}
}

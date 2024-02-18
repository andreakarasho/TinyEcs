namespace TinyEcs;

[DebuggerDisplay("ID: {ID}, Size: {Size}")]
public readonly struct EcsComponent
{
    public readonly ulong ID;
    public readonly int Size;

    internal EcsComponent(ulong id, int size)
    {
        ID = id;
        Size = size;
    }
}

public readonly struct EcsDisabled { }

public struct EcsName
{
	public string Value;
}

public struct Children
{
	internal HashSet<EcsID> Ids;

	public Children()
	{
		Ids = new();
	}

	public HashSet<EcsID>.Enumerator GetEnumerator() => Ids.GetEnumerator();
}

public struct Parent
{
	public EcsID ParentId;
}

public readonly struct ChildOf { }

public readonly struct Pair<TFirst, TSecond> where TFirst : struct where TSecond : struct { }

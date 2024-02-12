namespace TinyEcs;

[DebuggerDisplay("ID: {ID}, Size: {Size}")]
public readonly struct EcsComponent
{
    public readonly int ID;
    public readonly int Size;

    internal EcsComponent(int id, int size)
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

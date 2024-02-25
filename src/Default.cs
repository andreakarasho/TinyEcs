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

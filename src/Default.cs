namespace TinyEcs;

[DebuggerDisplay("ID: {ID}, Size: {Size}")]
public readonly struct ComponentInfo
{
    public readonly ulong ID;
    public readonly int Size;

    internal ComponentInfo(ulong id, int size)
    {
        ID = id;
        Size = size;
    }
}

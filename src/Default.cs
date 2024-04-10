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

public struct Wildcard { internal static EcsID ID = Lookup.Component<Wildcard>.Value.ID; }
public struct ChildOf { }
public struct Identifier { }
public readonly struct Name { internal Name(string value) => Value = value; public readonly string Value; }
public struct Unique { }
public struct Symmetric { }
public struct DoNotDelete { }

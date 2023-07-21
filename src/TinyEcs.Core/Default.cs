namespace TinyEcs;

public readonly struct EcsComponent : IEquatable<EcsComponent>
{
	public readonly EntityID ID;
	public readonly int Size;

	public EcsComponent(EntityID id, int size)
	{
		ID = id;
		Size = size;
	}

	public override readonly bool Equals(object? other)
	{
		return other is EcsComponent c && c.Equals(this);
	}

	public readonly bool Equals(EcsComponent other)
	{
		return ID == other.ID && Size == other.Size;
	}
}

public struct EcsQueryBuilder { }
public struct EcsQuery
{
	public EntityID ID;
}


public struct EcsQueryParameterWith
{
}

public struct EcsQueryParameterWithout
{
}

public struct EcsQueryParameter<T> where T : unmanaged
{
	public EntityID Component;
}

public unsafe readonly struct EcsSystem
{
	public readonly delegate* managed<Commands, ref EntityIterator, void> Func;

	public EcsSystem(delegate* managed<Commands, ref EntityIterator, void> func)
	{
		Func = func;
	}
}

internal struct EcsSystemTick
{
	public float Value;
	public float Current;
}

public struct EcsParent
{
	public int ChildrenCount;
	public EntityID FirstChild;
}

public struct EcsChild
{
	public EntityID Parent;
	public EntityID Prev, Next;
}

public readonly struct EcsEnabled { }
public struct EcsSystemPhaseOnUpdate { }
public struct EcsSystemPhasePreUpdate { }
public struct EcsSystemPhasePostUpdate { }
public struct EcsSystemPhaseOnStartup { }
public struct EcsSystemPhasePreStartup { }
public struct EcsSystemPhasePostStartup { }

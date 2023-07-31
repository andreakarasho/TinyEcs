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

public unsafe struct EcsSystem
{
	const int TERMS_COUNT = 32;

	public readonly delegate*<ref Iterator, void> Func;
	public readonly EntityID Query;
	public readonly float Tick;
	public float TickCurrent;
	private fixed byte _terms[TERMS_COUNT * (sizeof(EntityID) + sizeof(byte))];
	private readonly int _termsCount;

	public Span<Term> Terms
	{
		get
		{
			fixed (byte* ptr = _terms)
			{
				return new Span<Term>(ptr, _termsCount);
			}
		}
	}

	public EcsSystem(delegate*<ref Iterator, void> func, EntityID query, ReadOnlySpan<Term> terms, float tick)
	{
		Func = func;
		Query = query;
		_termsCount = terms.Length;
		terms.CopyTo(Terms);
		Tick = tick;
		TickCurrent = 0f;
	}
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

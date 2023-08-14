namespace TinyEcs;

public readonly struct EcsComponent
{
	public readonly EntityID ID;
	public readonly int Size;

	public EcsComponent(EntityID id, int size)
	{
		ID = id;
		Size = size;
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

public struct EcsPhase { }
public struct EcsPanic { }
public struct EcsDelete { }
public struct EcsExclusive { }
public struct EcsAny { }
public struct EcsTag { }
public struct EcsChildOf { }
public struct EcsEnabled { }
public struct EcsSystemPhaseOnUpdate { }
public struct EcsSystemPhasePreUpdate { }
public struct EcsSystemPhasePostUpdate { }
public struct EcsSystemPhaseOnStartup { }
public struct EcsSystemPhasePreStartup { }
public struct EcsSystemPhasePostStartup { }

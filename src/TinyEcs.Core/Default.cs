namespace TinyEcs;


public interface IComponentStub { }
public interface IComponent : IComponentStub { }
public interface ITag : IComponentStub { }

internal interface IObserverComponent : ITag { }

public readonly struct EcsComponent : IComponent
{
	public readonly EntityID ID;
	public readonly int Size;

	public EcsComponent(EntityID id, int size)
	{
		ID = id;
		Size = size;
	}
}

public unsafe struct EcsSystem : IComponent
{
	const int TERMS_COUNT = 32;

	public readonly delegate*<ref Iterator, void> Callback;
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
		Callback = func;
		Query = query;
		_termsCount = terms.Length;
		terms.CopyTo(Terms);
		Tick = tick;
		TickCurrent = 0f;
	}
}


public unsafe struct EcsObserver : IComponent
{
	const int TERMS_COUNT = 32;

	public readonly delegate*<ref Iterator, void> Callback;

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

	public EcsObserver(delegate*<ref Iterator, void> callback, ReadOnlySpan<Term> terms)
	{
		Callback = callback;
		_termsCount = terms.Length;
		terms.CopyTo(Terms);
	}
}

public struct EcsObserverOnSet : IObserverComponent { }
public struct EcsObserverOnUnset : IObserverComponent { }
public struct EcsPhase : ITag { }
public struct EcsPanic : ITag { }
public struct EcsDelete : ITag { }
public struct EcsExclusive : ITag { }
public struct EcsAny : ITag { }
public struct EcsTag : ITag { }
public struct EcsChildOf : ITag { }
public struct EcsEnabled : ITag { }
public struct EcsSystemPhaseOnUpdate : ITag { }
public struct EcsSystemPhasePreUpdate : ITag { }
public struct EcsSystemPhasePostUpdate : ITag { }
public struct EcsSystemPhaseOnStartup : ITag { }
public struct EcsSystemPhasePreStartup : ITag { }
public struct EcsSystemPhasePostStartup : ITag { }

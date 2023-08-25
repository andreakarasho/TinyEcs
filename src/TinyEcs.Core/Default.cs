using System.Drawing;

namespace TinyEcs;


public interface IComponentStub { }
public interface IComponent : IComponentStub { }
public interface ITag : IComponentStub { }
public interface IEvent : ITag { }


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

public unsafe struct EcsSystem<TContext> : IComponent
{
	const int TERMS_COUNT = 32;

	public readonly delegate*<ref Iterator<TContext>, void> Callback;
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

	public EcsSystem(delegate*<ref Iterator<TContext>, void> func, EntityID query, ReadOnlySpan<Term> terms, float tick)
	{
		Callback = func;
		Query = query;
		_termsCount = terms.Length;
		terms.CopyTo(Terms);
		Terms.Sort();
		Tick = tick;
		TickCurrent = 0f;
	}
}


public unsafe struct EcsEvent<TContext> : IComponent
{
	const int TERMS_COUNT = 16;

	public readonly delegate*<ref Iterator<TContext>, void> Callback;

	private fixed byte _terms[TERMS_COUNT * (sizeof(EntityID) + sizeof(TermOp))];
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

	public EcsEvent(delegate*<ref Iterator<TContext>, void> callback, ReadOnlySpan<Term> terms)
	{
		Callback = callback;
		_termsCount = terms.Length;
		var currentTerms = Terms;
		terms.CopyTo(currentTerms);
		currentTerms.Sort();
	}
}


public struct EcsEventOnSet : IEvent { }
public struct EcsEventOnUnset : IEvent { }
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


// [StructLayout(LayoutKind.Explicit)]
// public readonly struct EntityID : IEquatable<ulong>, IComparable<ulong>
// {
// 	[FieldOffset(0)]
// 	public readonly ulong Value;

// 	public EntityID(ulong value) => Value = value;


// 	public int CompareTo(ulong other) => Value.CompareTo(other);
// 	public bool Equals(ulong other) => Value == other;


// 	public static implicit operator ulong(EntityID id) => id.Value;
// 	public static implicit operator EntityID(ulong value) => new (value);

// }

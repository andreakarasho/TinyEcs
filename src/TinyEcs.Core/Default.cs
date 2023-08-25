using System.Drawing;

namespace TinyEcs;


public interface IComponentStub { }
public interface IComponent : IComponentStub { }
public interface ITag : IComponentStub { }
public interface IEvent : ITag { }


public readonly struct EcsComponent : IComponent
{
	public readonly ulong ID;
	public readonly int Size;

	public EcsComponent(EcsID id, int size)
	{
		ID = id;
		Size = size;
	}
}

public unsafe struct EcsSystem<TContext> : IComponent
{
	const int TERMS_COUNT = 32;

	public readonly delegate*<ref Iterator<TContext>, void> Callback;
	public readonly EcsID Query;
	public readonly float Tick;
	public float TickCurrent;
	private fixed byte _terms[TERMS_COUNT * (sizeof(ulong) + sizeof(byte))];
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

	public EcsSystem(delegate*<ref Iterator<TContext>, void> func, EcsID query, ReadOnlySpan<Term> terms, float tick)
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

	private fixed byte _terms[TERMS_COUNT * (sizeof(ulong) + sizeof(TermOp))];
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
public struct EcsDisabled : ITag { }
public struct EcsSystemPhaseOnUpdate : ITag { }
public struct EcsSystemPhasePreUpdate : ITag { }
public struct EcsSystemPhasePostUpdate : ITag { }
public struct EcsSystemPhaseOnStartup : ITag { }
public struct EcsSystemPhasePreStartup : ITag { }
public struct EcsSystemPhasePostStartup : ITag { }


[StructLayout(LayoutKind.Explicit)]
public readonly struct EcsID : IEquatable<ulong>, IComparable<ulong>, IEquatable<EcsID>, IComparable<EcsID>
{
	[FieldOffset(0)]
	public readonly ulong Value;

	public EcsID(ulong value) => Value = value;


	public readonly int CompareTo(ulong other) => Value.CompareTo(other);
	public readonly bool Equals(ulong other) => Value == other;
	public readonly int CompareTo(EcsID other) => Value.CompareTo(other.Value);
	public readonly bool Equals(EcsID other) => Value == other.Value;


	public static implicit operator ulong(EcsID id) => id.Value;
	public static implicit operator EcsID(ulong value) => new (value);

	public static bool operator ==(EcsID id, EcsID other) => id.Value.Equals(other.Value);
	public static bool operator !=(EcsID id, EcsID other) => !id.Value.Equals(other.Value);

	public static Term operator !(EcsID id) => Term.Without(id.Value);
	public static Term operator -(EcsID id) => Term.Without(id.Value);
	public static Term operator +(EcsID id) => Term.With(id.Value);

	public readonly override bool Equals(object? obj)
	{
		return obj is EcsID ent && Equals(ent);
	}

	public readonly override int GetHashCode()
	{
		return Value.GetHashCode();
	}

	public override string ToString()
	{
		return Value.ToString();
	}
}

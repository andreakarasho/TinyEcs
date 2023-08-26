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



public partial class World<TContext>
{
	public const ulong EcsComponent = 1;

	public const ulong EcsPanic = 2;
	public const ulong EcsDelete = 3;
	public const ulong EcsTag = 4;

	public const ulong EcsExclusive = 5;
	public const ulong EcsChildOf = 6;
	public const ulong EcsPhase = 7;

	public const ulong EcsEventOnSet = 8;
	public const ulong EcsEventOnUnset = 9;

	public const ulong EcsPhaseOnPreStartup = 9;
	public const ulong EcsPhaseOnStartup = 10;
	public const ulong EcsPhaseOnPostStartup = 11;
	public const ulong EcsPhaseOnPreUpdate = 12;
	public const ulong EcsPhaseOnUpdate = 13;
	public const ulong EcsPhaseOnPostUpdate = 14;



	internal unsafe void InitializeDefaults()
	{
		var ecsComponent = Entity(EcsComponent);
		var ecsPanic = Entity(EcsPanic);
		var ecsDelete = Entity(EcsDelete);
		var ecsTag = Entity(EcsTag);

		var ecsExclusive = Entity(EcsExclusive);
		var ecsChildOf = Entity(EcsChildOf);
		var ecsPhase = Entity(EcsPhase);

		var ecsEventOnSet = Entity(EcsEventOnSet);
		var ecsEventOnUnset = Entity(EcsEventOnUnset);

		var ecsPreStartup = Entity(EcsPhaseOnPreStartup);
		var ecsStartup = Entity(EcsPhaseOnStartup);
		var ecsPostStartup = Entity(EcsPhaseOnPostStartup);
		var ecsPreUpdate = Entity(EcsPhaseOnPreUpdate);
		var ecsUpdate = Entity(EcsPhaseOnUpdate);
		var ecsPostUpdate = Entity(EcsPhaseOnPostUpdate);


		LinkLookup<EcsComponent>(ecsComponent);

		LinkLookup<EcsPanic>(ecsPanic);
		LinkLookup<EcsDelete>(ecsDelete);
		LinkLookup<EcsTag>(ecsTag);

		LinkLookup<EcsExclusive>(ecsExclusive);
		LinkLookup<EcsChildOf>(ecsChildOf);
		LinkLookup<EcsPhase>(ecsPhase);

		LinkLookup<EcsEventOnSet>(ecsEventOnSet);
		LinkLookup<EcsEventOnUnset>(ecsEventOnUnset);

		LinkLookup<EcsSystemPhasePreStartup>(ecsPreStartup);
		LinkLookup<EcsSystemPhaseOnStartup>(ecsStartup);
		LinkLookup<EcsSystemPhasePostStartup>(ecsPostStartup);
		LinkLookup<EcsSystemPhasePreUpdate>(ecsPreUpdate);
		LinkLookup<EcsSystemPhaseOnUpdate>(ecsUpdate);
		LinkLookup<EcsSystemPhasePostUpdate>(ecsPostUpdate);


		SetBaseTags(ecsExclusive);
		SetBaseTags(ecsChildOf);
		SetBaseTags(ecsPhase);

		SetBaseTags(ecsPanic);
		SetBaseTags(ecsDelete);
		SetBaseTags(ecsTag);

		SetBaseTags(ecsEventOnSet);
		SetBaseTags(ecsEventOnUnset);

		SetBaseTags(ecsPreStartup);
		SetBaseTags(ecsStartup);
		SetBaseTags(ecsPostStartup);
		SetBaseTags(ecsPreUpdate);
		SetBaseTags(ecsUpdate);
		SetBaseTags(ecsPostUpdate);


		ecsChildOf.Set(ecsExclusive);
		ecsPhase.Set(ecsExclusive); // NOTE: do we want to make phase singletons?



		void LinkLookup<T>(EntityView<TContext> view) where T : unmanaged, IComponentStub
			=> Lookup<TContext>.Entity<T>.Component = new (view, GetSize<T>());

		EntityView<TContext> SetBaseTags(EntityView<TContext> view)
			=> view.Set(EcsPanic, EcsDelete).Set(EcsTag);
	}
}

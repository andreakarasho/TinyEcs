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

public unsafe struct EcsSystem : IComponent
{
	const int TERMS_COUNT = 32;

	public readonly delegate*<ref Iterator, void> Callback;
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

	public EcsSystem(delegate*<ref Iterator, void> func, EcsID query, ReadOnlySpan<Term> terms, float tick)
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


public unsafe struct EcsEvent : IComponent
{
	const int TERMS_COUNT = 16;

	public readonly delegate*<ref Iterator, void> Callback;

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

	public EcsEvent(delegate*<ref Iterator, void> callback, ReadOnlySpan<Term> terms)
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



public partial class World
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

	public const ulong EcsAny = 15;

	public const ulong EcsSystem = 16;
	public const ulong EcsEvent = 17;


	internal unsafe void InitializeDefaults()
	{
		var ecsComponent = CreateWithLookup<EcsComponent>(EcsComponent);

		var ecsPanic = CreateWithLookup<EcsPanic>(EcsPanic);
		var ecsDelete = CreateWithLookup<EcsDelete>(EcsDelete);
		var ecsTag = CreateWithLookup<EcsTag>(EcsTag);

		var ecsExclusive = CreateWithLookup<EcsExclusive>(EcsExclusive);
		var ecsChildOf = CreateWithLookup<EcsChildOf>(EcsChildOf);
		var ecsPhase = CreateWithLookup<EcsPhase>(EcsPhase);

		var ecsEventOnSet = CreateWithLookup<EcsEventOnSet>(EcsEventOnSet);
		var ecsEventOnUnset = CreateWithLookup<EcsEventOnUnset>(EcsEventOnUnset);

		var ecsPreStartup = CreateWithLookup<EcsSystemPhasePreStartup>(EcsPhaseOnPreStartup);
		var ecsStartup = CreateWithLookup<EcsSystemPhaseOnStartup>(EcsPhaseOnStartup);
		var ecsPostStartup = CreateWithLookup<EcsSystemPhasePostStartup>(EcsPhaseOnPostStartup);
		var ecsPreUpdate = CreateWithLookup<EcsSystemPhasePreUpdate>(EcsPhaseOnPreUpdate);
		var ecsUpdate = CreateWithLookup<EcsSystemPhaseOnUpdate>(EcsPhaseOnUpdate);
		var ecsPostUpdate = CreateWithLookup<EcsSystemPhasePostUpdate>(EcsPhaseOnPostUpdate);

		var ecsAny = CreateWithLookup<EcsAny>(EcsAny);

		var ecsSystem = CreateWithLookup<EcsSystem>(EcsSystem);
		var ecsEvent = CreateWithLookup<EcsEvent>(EcsEvent);


		ecsChildOf.Set(EcsExclusive);
		ecsPhase.Set(ecsExclusive); // NOTE: do we want to make phase singletons?


		var cmp2 = Lookup.Entity<EcsComponent>.Component = new (ecsComponent, GetSize<EcsComponent>());
		Set(ecsComponent.ID, ref cmp2, new ReadOnlySpan<byte>(Unsafe.AsPointer(ref cmp2), cmp2.Size));
		ecsComponent.Set(EcsPanic, EcsDelete);


		AssignDefaults<EcsPanic>(ecsExclusive);
		AssignDefaults<EcsDelete>(ecsChildOf);
		AssignDefaults<EcsTag>(ecsPhase);

		AssignDefaults<EcsExclusive>(ecsPanic);
		AssignDefaults<EcsChildOf>(ecsDelete);
		AssignDefaults<EcsPhase>(ecsTag);

		AssignDefaults<EcsEventOnSet>(ecsEventOnSet);
		AssignDefaults<EcsEventOnUnset>(ecsEventOnUnset);

		AssignDefaults<EcsSystemPhasePreStartup>(ecsPreStartup);
		AssignDefaults<EcsSystemPhaseOnStartup>(ecsStartup);
		AssignDefaults<EcsSystemPhasePostStartup>(ecsPostStartup);
		AssignDefaults<EcsSystemPhasePreUpdate>(ecsPreUpdate);
		AssignDefaults<EcsSystemPhaseOnUpdate>(ecsUpdate);
		AssignDefaults<EcsSystemPhasePostUpdate>(ecsPostUpdate);

		AssignDefaults<EcsAny>(ecsAny);

		AssignDefaults<EcsSystem>(ecsSystem);
		AssignDefaults<EcsEvent>(ecsEvent);


		EntityView CreateWithLookup<T>(EcsID id) where T : unmanaged, IComponentStub
		{
			var view = Entity(id);
			Lookup.Entity<T>.Component = new (view, GetSize<T>());
			return view;
		}

		EntityView AssignDefaults<T>(EntityView view) where T : unmanaged, IComponentStub
		{
			view.Set(Lookup.Entity<T>.Component).Set(EcsPanic, EcsDelete);

			if (GetSize<T>() == 0)
				view.Set(EcsTag);

			return view;
		}
	}
}

namespace TinyEcs;

public readonly struct EcsComponent
{
    public readonly ulong ID;
    public readonly int Size;

    public EcsComponent(EcsID id, int size)
    {
        ID = id;
        Size = size;
    }
}

public unsafe struct EcsSystem
{
    const int TERMS_COUNT = 32;

    public readonly delegate* <ref Iterator, void> Callback;
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

    public EcsSystem(
        delegate* <ref Iterator, void> func,
        EcsID query,
        ReadOnlySpan<Term> terms,
        float tick
    )
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

public unsafe struct EcsEvent
{
    const int TERMS_COUNT = 16;

    public readonly delegate* <ref Iterator, void> Callback;

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

    public EcsEvent(delegate* <ref Iterator, void> callback, ReadOnlySpan<Term> terms)
    {
        Callback = callback;
        _termsCount = terms.Length;
        var currentTerms = Terms;
        terms.CopyTo(currentTerms);
        currentTerms.Sort();
    }
}

public struct EcsEventOnSet { }

public struct EcsEventOnUnset { }

public struct EcsEventOnCreate { }

public struct EcsEventOnDelete { }

public struct EcsPhase { }

public struct EcsPanic { }

public struct EcsDelete { }

public struct EcsExclusive { }

public struct EcsAny { }

public struct EcsTag { }

public struct EcsChildOf { }

public struct EcsDisabled { }

public struct EcsSystemPhaseOnUpdate { }

public struct EcsSystemPhasePreUpdate { }

public struct EcsSystemPhasePostUpdate { }

public struct EcsSystemPhaseOnStartup { }

public struct EcsSystemPhasePreStartup { }

public struct EcsSystemPhasePostStartup { }

public partial class World
{
    public static readonly EcsID EcsComponent = 1;

    public static readonly EcsID EcsPanic = 2;
    public static readonly EcsID EcsDelete = 3;
    public static readonly EcsID EcsTag = 4;

    public static readonly EcsID EcsExclusive = 5;
    public static readonly EcsID EcsChildOf = 6;
    public static readonly EcsID EcsPhase = 7;

    public static readonly EcsID EcsEventOnSet = 8;
    public static readonly EcsID EcsEventOnUnset = 9;
    public static readonly EcsID EcsEventOnCreate = 10;
    public static readonly EcsID EcsEventOnDelete = 11;

    public static readonly EcsID EcsPhaseOnPreStartup = 12;
    public static readonly EcsID EcsPhaseOnStartup = 13;
    public static readonly EcsID EcsPhaseOnPostStartup = 14;
    public static readonly EcsID EcsPhaseOnPreUpdate = 15;
    public static readonly EcsID EcsPhaseOnUpdate = 16;
    public static readonly EcsID EcsPhaseOnPostUpdate = 17;

    public static readonly EcsID EcsAny = 18;

    public static readonly EcsID EcsSystem = 19;
    public static readonly EcsID EcsEvent = 20;

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
        var ecsEventOnCreate = CreateWithLookup<EcsEventOnCreate>(EcsEventOnCreate);
        var ecsEventOnDelete = CreateWithLookup<EcsEventOnDelete>(EcsEventOnDelete);

        var ecsPreStartup = CreateWithLookup<EcsSystemPhasePreStartup>(EcsPhaseOnPreStartup);
        var ecsStartup = CreateWithLookup<EcsSystemPhaseOnStartup>(EcsPhaseOnStartup);
        var ecsPostStartup = CreateWithLookup<EcsSystemPhasePostStartup>(EcsPhaseOnPostStartup);
        var ecsPreUpdate = CreateWithLookup<EcsSystemPhasePreUpdate>(EcsPhaseOnPreUpdate);
        var ecsUpdate = CreateWithLookup<EcsSystemPhaseOnUpdate>(EcsPhaseOnUpdate);
        var ecsPostUpdate = CreateWithLookup<EcsSystemPhasePostUpdate>(EcsPhaseOnPostUpdate);

        var ecsAny = CreateWithLookup<EcsAny>(EcsAny);

        var ecsSystem = CreateWithLookup<EcsSystem>(EcsSystem);
        var ecsEvent = CreateWithLookup<EcsEvent>(EcsEvent);

        // ecsChildOf.Set(EcsExclusive);
        // ecsPhase.Set(ecsExclusive); // NOTE: do we want to make phase singletons?


        var cmp2 = Lookup.Entity<EcsComponent>.Component = new(
            ecsComponent,
            GetSize<EcsComponent>()
        );
        Set(
            ecsComponent.ID,
            ref cmp2,
            new ReadOnlySpan<byte>(Unsafe.AsPointer(ref cmp2), cmp2.Size)
        );
        ecsComponent.Set(EcsPanic, EcsDelete);

        AssignDefaults<EcsPanic>(ecsExclusive);
        AssignDefaults<EcsDelete>(ecsChildOf);
        AssignDefaults<EcsTag>(ecsPhase);

        AssignDefaults<EcsExclusive>(ecsPanic);
        AssignDefaults<EcsChildOf>(ecsDelete);
        AssignDefaults<EcsPhase>(ecsTag);

        AssignDefaults<EcsEventOnSet>(ecsEventOnSet);
        AssignDefaults<EcsEventOnUnset>(ecsEventOnUnset);
        AssignDefaults<EcsEventOnCreate>(ecsEventOnCreate);
        AssignDefaults<EcsEventOnDelete>(ecsEventOnDelete);

        AssignDefaults<EcsSystemPhasePreStartup>(ecsPreStartup);
        AssignDefaults<EcsSystemPhaseOnStartup>(ecsStartup);
        AssignDefaults<EcsSystemPhasePostStartup>(ecsPostStartup);
        AssignDefaults<EcsSystemPhasePreUpdate>(ecsPreUpdate);
        AssignDefaults<EcsSystemPhaseOnUpdate>(ecsUpdate);
        AssignDefaults<EcsSystemPhasePostUpdate>(ecsPostUpdate);

        AssignDefaults<EcsAny>(ecsAny);

        AssignDefaults<EcsSystem>(ecsSystem);
        AssignDefaults<EcsEvent>(ecsEvent);

        ecsChildOf.Set(EcsExclusive);
        ecsPhase.Set(EcsExclusive); // NOTE: do we want to make phase singletons?

        Event(
            &EcsCheckChildOfExclusive,
            stackalloc Term[] {
                Term.With(EcsExclusive)
                //IDOp.Pair(EcsChildOf, EcsAny)
            },
            stackalloc EcsID[] { EcsEventOnSet }
        );

        Event(
            &EcsDeleteAllChildrenOnParentDeletion,
            stackalloc Term[] { IDOp.Pair(EcsChildOf, EcsAny) },
            stackalloc EcsID[] { EcsEventOnDelete }
        );

        Event(
            &EcsPanicOnDelete,
            stackalloc Term[] { IDOp.Pair(EcsPanic, EcsDelete) },
            stackalloc EcsID[] { EcsEventOnDelete }
        );

        EntityView CreateWithLookup<T>(EcsID id) where T : unmanaged
        {
            var view = Entity(id);
            Lookup.Entity<T>.Component = new(view, GetSize<T>());
            return view;
        }

        static EntityView AssignDefaults<T>(EntityView view) where T : unmanaged
        {
            ref var cmp = ref Lookup.Entity<T>.Component;
            view.Set(cmp).Set(EcsPanic, EcsDelete);

            if (cmp.Size == 0)
                view.Set(EcsTag);

            return view;
        }

        static void EcsCheckChildOfExclusive(ref Iterator it)
        {
            var eventID = it.EventTriggeredComponent;
            var eventFirst = IDOp.GetPairFirst(eventID);

            for (int i = 0; i < it.Count; ++i)
            {
                var entity = it.Entity(i);
                var type = entity.Type();

                foreach (ref readonly var cmp in type)
                {
                    if (!IDOp.IsPair(cmp.ID) || cmp.ID == eventID)
                        continue;

                    var first = IDOp.GetPairFirst(cmp.ID);
                    var second = IDOp.GetPairSecond(cmp.ID);

                    if (first != eventFirst)
                        continue;

                    entity.Unset(first, second);
                }
            }
        }

        static void EcsDeleteAllChildrenOnParentDeletion(ref Iterator it) { }

        static void EcsPanicOnDelete(ref Iterator it)
        {
            EcsAssert.Panic(
                true,
                $"You cannot delete entity {it.Entity(0)} with ({nameof(EcsPanic)}, {nameof(EcsDelete)})"
            );
        }
    }
}

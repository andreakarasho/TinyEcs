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


// public static class BuiltIn<TContext>
// {
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	static unsafe EntityView<TContext> Register<T>(ReadOnlySpan<char> name)
// 	where T : unmanaged, IComponentStub
// 	{
// 		Console.WriteLine("registering: {0}", name.ToString());
// 		//return World<TContext>.Get().New(name).Component<T>();

// 		ref var lookup = ref Lookup<TContext>.Entity<T>.Component;
// 		EcsAssert.Assert(lookup.ID == 0);

// 		var world = World<TContext>.Get();

// 		var ent = world.NewEmpty();
// 		var size = typeof(T).IsAssignableTo(typeof(ITag)) ? 0 : sizeof(T);
// 		var cmp = new EcsComponent(ent, size);

// 		world.Set(cmp.ID, cmp);
// 		world.Set(ent, BuiltIn<TContext>.EcsPanic, BuiltIn<TContext>.EcsDelete);

// 		if (size == 0)
// 			world.Set(ent, BuiltIn<TContext>.EcsTag);

// 		return ent;
// 	}

// 	static BuiltIn()
// 	{
// 		// EcsComponent = World<TContext>.Get()
// 		// 	.Entity<EcsComponent>();

// 		//EcsComponent = Register<EcsComponent>(nameof(EcsComponent));
// 	}

// 	const ulong DEFAULT_START = 0;

// 	public static readonly EcsID EcsTest = DEFAULT_START + 1;


// 	//public static readonly EcsID EcsComponent = Register<EcsComponent>(nameof(EcsComponent));

// 	public static readonly EcsID EcsPanic = Register<EcsPanic>(nameof(EcsPanic));
// 	public static readonly EcsID EcsDelete = Register<EcsDelete>(nameof(EcsDelete));
// 	public static readonly EcsID EcsTag = Register<EcsTag>(nameof(EcsTag));


// 	public static readonly EcsID EcsExclusive = Register<EcsExclusive>(nameof(EcsExclusive));
// 	public static readonly EcsID EcsChildOf = Register<EcsChildOf>(nameof(EcsChildOf)).Set<EcsExclusive>();
// 	public static readonly EcsID EcsPhase = Register<EcsPhase>(nameof(EcsPhase)).Set<EcsExclusive>();


// 	public static readonly EcsID EcsEventOnSet = Register<EcsEventOnSet>(nameof(EcsEventOnSet));
// 	public static readonly EcsID EcsEventOnUnset = Register<EcsEventOnUnset>(nameof(EcsEventOnUnset));




// 	public static readonly EcsID EcsDisabled = Register<EcsDisabled>(nameof(EcsDisabled));
// 	public static readonly EcsID EcsAny = Register<EcsAny>(nameof(EcsAny));


// 	public static readonly EcsID EcsSystemPhaseOnUpdate = Register<EcsSystemPhaseOnUpdate>(nameof(EcsSystemPhaseOnUpdate));
// 	public static readonly EcsID EcsSystemPhasePreUpdate = Register<EcsSystemPhasePreUpdate>(nameof(EcsSystemPhasePreUpdate));
// 	public static readonly EcsID EcsSystemPhasePostUpdate = Register<EcsSystemPhasePostUpdate>(nameof(EcsSystemPhasePostUpdate));
// 	public static readonly EcsID EcsSystemPhaseOnStartup = Register<EcsSystemPhaseOnStartup>(nameof(EcsSystemPhaseOnStartup));
// 	public static readonly EcsID EcsSystemPhasePreStartup = Register<EcsSystemPhasePreStartup>(nameof(EcsSystemPhasePreStartup));
// 	public static readonly EcsID EcsSystemPhasePostStartup = Register<EcsSystemPhasePostStartup>(nameof(EcsSystemPhasePostStartup));
// }

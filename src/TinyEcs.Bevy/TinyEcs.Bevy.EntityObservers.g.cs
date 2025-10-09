#pragma warning disable 1591
#nullable enable

using System;

namespace TinyEcs.Bevy;

public static partial class EntityCommandsObserverExtensions
{
	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1>(this EntityCommands entity, Action<TTrigger, T1> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
	{
		// Create the observer with system parameter injection
		var p1 = new T1();
		return entity.Observe<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			callback(trigger, p1);
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2>(this EntityCommands entity, Action<TTrigger, T1, T2> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new()
	{
		// Create the observer with system parameter injection
		var p1 = new T1();
		var p2 = new T2();
		return entity.Observe<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			callback(trigger, p1, p2);
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3>(this EntityCommands entity, Action<TTrigger, T1, T2, T3> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new()
	{
		// Create the observer with system parameter injection
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		return entity.Observe<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			callback(trigger, p1, p2, p3);
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
	{
		// Create the observer with system parameter injection
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		return entity.Observe<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			callback(trigger, p1, p2, p3, p4);
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new()
	{
		// Create the observer with system parameter injection
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		return entity.Observe<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5);
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new()
	{
		// Create the observer with system parameter injection
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		return entity.Observe<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6);
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new()
	{
		// Create the observer with system parameter injection
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		return entity.Observe<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7);
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
	{
		// Create the observer with system parameter injection
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		return entity.Observe<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8);
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new()
	{
		// Create the observer with system parameter injection
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		var p9 = new T9();
		return entity.Observe<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			p9.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9);
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new()
	{
		// Create the observer with system parameter injection
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		var p9 = new T9();
		var p10 = new T10();
		return entity.Observe<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			p9.Fetch(w);
			p10.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new()
	{
		// Create the observer with system parameter injection
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		var p9 = new T9();
		var p10 = new T10();
		var p11 = new T11();
		return entity.Observe<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			p9.Fetch(w);
			p10.Fetch(w);
			p11.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11);
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
	{
		// Create the observer with system parameter injection
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		var p9 = new T9();
		var p10 = new T10();
		var p11 = new T11();
		var p12 = new T12();
		return entity.Observe<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			p9.Fetch(w);
			p10.Fetch(w);
			p11.Fetch(w);
			p12.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12);
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new()
	{
		// Create the observer with system parameter injection
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		var p9 = new T9();
		var p10 = new T10();
		var p11 = new T11();
		var p12 = new T12();
		var p13 = new T13();
		return entity.Observe<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			p9.Fetch(w);
			p10.Fetch(w);
			p11.Fetch(w);
			p12.Fetch(w);
			p13.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13);
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new()
	{
		// Create the observer with system parameter injection
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		var p9 = new T9();
		var p10 = new T10();
		var p11 = new T11();
		var p12 = new T12();
		var p13 = new T13();
		var p14 = new T14();
		return entity.Observe<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			p9.Fetch(w);
			p10.Fetch(w);
			p11.Fetch(w);
			p12.Fetch(w);
			p13.Fetch(w);
			p14.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14);
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new()
	{
		// Create the observer with system parameter injection
		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		var p9 = new T9();
		var p10 = new T10();
		var p11 = new T11();
		var p12 = new T12();
		var p13 = new T13();
		var p14 = new T14();
		var p15 = new T15();
		return entity.Observe<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			p8.Fetch(w);
			p9.Fetch(w);
			p10.Fetch(w);
			p11.Fetch(w);
			p12.Fetch(w);
			p13.Fetch(w);
			p14.Fetch(w);
			p15.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15);
		});
	}

}

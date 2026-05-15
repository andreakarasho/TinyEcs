#pragma warning disable 1591
#nullable enable

using System;

namespace TinyEcs.Bevy;

public static partial class EntityCommandsObserverExtensions
{
	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// Commands parameters are automatically applied after the observer executes.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1>(this EntityCommands entity, Action<TTrigger, T1> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
	{
		// Use ObserveWithWorld to get World access, then fetch system parameters
		var entityApp = entity.OwningApp;
		var p1 = new T1();
		if (entityApp is not null) p1.Initialize(entityApp);
		return entity.ObserveWithWorld<TTrigger>((w, trigger) =>
		{
			if (entityApp is not null) p1.Fetch(entityApp);
			callback(trigger, p1);

			// Auto-apply Commands if any parameter is Commands type
			// Collect all Commands instances and apply them in one pass
			Commands? cmd = null;
			if (p1 is Commands c1) cmd = c1;
			if (cmd != null)
			{
				cmd.Apply();
				w.FlushObservers();
			}
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// Commands parameters are automatically applied after the observer executes.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2>(this EntityCommands entity, Action<TTrigger, T1, T2> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new()
	{
		// Use ObserveWithWorld to get World access, then fetch system parameters
		var entityApp = entity.OwningApp;
		var p1 = new T1();
		if (entityApp is not null) p1.Initialize(entityApp);
		var p2 = new T2();
		if (entityApp is not null) p2.Initialize(entityApp);
		return entity.ObserveWithWorld<TTrigger>((w, trigger) =>
		{
			if (entityApp is not null) p1.Fetch(entityApp);
			if (entityApp is not null) p2.Fetch(entityApp);
			callback(trigger, p1, p2);

			// Auto-apply Commands if any parameter is Commands type
			// Collect all Commands instances and apply them in one pass
			Commands? cmd = null;
			if (p1 is Commands c1) cmd = c1;
			if (p2 is Commands c2) cmd = c2;
			if (cmd != null)
			{
				cmd.Apply();
				w.FlushObservers();
			}
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// Commands parameters are automatically applied after the observer executes.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3>(this EntityCommands entity, Action<TTrigger, T1, T2, T3> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new()
	{
		// Use ObserveWithWorld to get World access, then fetch system parameters
		var entityApp = entity.OwningApp;
		var p1 = new T1();
		if (entityApp is not null) p1.Initialize(entityApp);
		var p2 = new T2();
		if (entityApp is not null) p2.Initialize(entityApp);
		var p3 = new T3();
		if (entityApp is not null) p3.Initialize(entityApp);
		return entity.ObserveWithWorld<TTrigger>((w, trigger) =>
		{
			if (entityApp is not null) p1.Fetch(entityApp);
			if (entityApp is not null) p2.Fetch(entityApp);
			if (entityApp is not null) p3.Fetch(entityApp);
			callback(trigger, p1, p2, p3);

			// Auto-apply Commands if any parameter is Commands type
			// Collect all Commands instances and apply them in one pass
			Commands? cmd = null;
			if (p1 is Commands c1) cmd = c1;
			if (p2 is Commands c2) cmd = c2;
			if (p3 is Commands c3) cmd = c3;
			if (cmd != null)
			{
				cmd.Apply();
				w.FlushObservers();
			}
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// Commands parameters are automatically applied after the observer executes.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
	{
		// Use ObserveWithWorld to get World access, then fetch system parameters
		var entityApp = entity.OwningApp;
		var p1 = new T1();
		if (entityApp is not null) p1.Initialize(entityApp);
		var p2 = new T2();
		if (entityApp is not null) p2.Initialize(entityApp);
		var p3 = new T3();
		if (entityApp is not null) p3.Initialize(entityApp);
		var p4 = new T4();
		if (entityApp is not null) p4.Initialize(entityApp);
		return entity.ObserveWithWorld<TTrigger>((w, trigger) =>
		{
			if (entityApp is not null) p1.Fetch(entityApp);
			if (entityApp is not null) p2.Fetch(entityApp);
			if (entityApp is not null) p3.Fetch(entityApp);
			if (entityApp is not null) p4.Fetch(entityApp);
			callback(trigger, p1, p2, p3, p4);

			// Auto-apply Commands if any parameter is Commands type
			// Collect all Commands instances and apply them in one pass
			Commands? cmd = null;
			if (p1 is Commands c1) cmd = c1;
			if (p2 is Commands c2) cmd = c2;
			if (p3 is Commands c3) cmd = c3;
			if (p4 is Commands c4) cmd = c4;
			if (cmd != null)
			{
				cmd.Apply();
				w.FlushObservers();
			}
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// Commands parameters are automatically applied after the observer executes.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new()
	{
		// Use ObserveWithWorld to get World access, then fetch system parameters
		var entityApp = entity.OwningApp;
		var p1 = new T1();
		if (entityApp is not null) p1.Initialize(entityApp);
		var p2 = new T2();
		if (entityApp is not null) p2.Initialize(entityApp);
		var p3 = new T3();
		if (entityApp is not null) p3.Initialize(entityApp);
		var p4 = new T4();
		if (entityApp is not null) p4.Initialize(entityApp);
		var p5 = new T5();
		if (entityApp is not null) p5.Initialize(entityApp);
		return entity.ObserveWithWorld<TTrigger>((w, trigger) =>
		{
			if (entityApp is not null) p1.Fetch(entityApp);
			if (entityApp is not null) p2.Fetch(entityApp);
			if (entityApp is not null) p3.Fetch(entityApp);
			if (entityApp is not null) p4.Fetch(entityApp);
			if (entityApp is not null) p5.Fetch(entityApp);
			callback(trigger, p1, p2, p3, p4, p5);

			// Auto-apply Commands if any parameter is Commands type
			// Collect all Commands instances and apply them in one pass
			Commands? cmd = null;
			if (p1 is Commands c1) cmd = c1;
			if (p2 is Commands c2) cmd = c2;
			if (p3 is Commands c3) cmd = c3;
			if (p4 is Commands c4) cmd = c4;
			if (p5 is Commands c5) cmd = c5;
			if (cmd != null)
			{
				cmd.Apply();
				w.FlushObservers();
			}
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// Commands parameters are automatically applied after the observer executes.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new()
	{
		// Use ObserveWithWorld to get World access, then fetch system parameters
		var entityApp = entity.OwningApp;
		var p1 = new T1();
		if (entityApp is not null) p1.Initialize(entityApp);
		var p2 = new T2();
		if (entityApp is not null) p2.Initialize(entityApp);
		var p3 = new T3();
		if (entityApp is not null) p3.Initialize(entityApp);
		var p4 = new T4();
		if (entityApp is not null) p4.Initialize(entityApp);
		var p5 = new T5();
		if (entityApp is not null) p5.Initialize(entityApp);
		var p6 = new T6();
		if (entityApp is not null) p6.Initialize(entityApp);
		return entity.ObserveWithWorld<TTrigger>((w, trigger) =>
		{
			if (entityApp is not null) p1.Fetch(entityApp);
			if (entityApp is not null) p2.Fetch(entityApp);
			if (entityApp is not null) p3.Fetch(entityApp);
			if (entityApp is not null) p4.Fetch(entityApp);
			if (entityApp is not null) p5.Fetch(entityApp);
			if (entityApp is not null) p6.Fetch(entityApp);
			callback(trigger, p1, p2, p3, p4, p5, p6);

			// Auto-apply Commands if any parameter is Commands type
			// Collect all Commands instances and apply them in one pass
			Commands? cmd = null;
			if (p1 is Commands c1) cmd = c1;
			if (p2 is Commands c2) cmd = c2;
			if (p3 is Commands c3) cmd = c3;
			if (p4 is Commands c4) cmd = c4;
			if (p5 is Commands c5) cmd = c5;
			if (p6 is Commands c6) cmd = c6;
			if (cmd != null)
			{
				cmd.Apply();
				w.FlushObservers();
			}
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// Commands parameters are automatically applied after the observer executes.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new()
	{
		// Use ObserveWithWorld to get World access, then fetch system parameters
		var entityApp = entity.OwningApp;
		var p1 = new T1();
		if (entityApp is not null) p1.Initialize(entityApp);
		var p2 = new T2();
		if (entityApp is not null) p2.Initialize(entityApp);
		var p3 = new T3();
		if (entityApp is not null) p3.Initialize(entityApp);
		var p4 = new T4();
		if (entityApp is not null) p4.Initialize(entityApp);
		var p5 = new T5();
		if (entityApp is not null) p5.Initialize(entityApp);
		var p6 = new T6();
		if (entityApp is not null) p6.Initialize(entityApp);
		var p7 = new T7();
		if (entityApp is not null) p7.Initialize(entityApp);
		return entity.ObserveWithWorld<TTrigger>((w, trigger) =>
		{
			if (entityApp is not null) p1.Fetch(entityApp);
			if (entityApp is not null) p2.Fetch(entityApp);
			if (entityApp is not null) p3.Fetch(entityApp);
			if (entityApp is not null) p4.Fetch(entityApp);
			if (entityApp is not null) p5.Fetch(entityApp);
			if (entityApp is not null) p6.Fetch(entityApp);
			if (entityApp is not null) p7.Fetch(entityApp);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7);

			// Auto-apply Commands if any parameter is Commands type
			// Collect all Commands instances and apply them in one pass
			Commands? cmd = null;
			if (p1 is Commands c1) cmd = c1;
			if (p2 is Commands c2) cmd = c2;
			if (p3 is Commands c3) cmd = c3;
			if (p4 is Commands c4) cmd = c4;
			if (p5 is Commands c5) cmd = c5;
			if (p6 is Commands c6) cmd = c6;
			if (p7 is Commands c7) cmd = c7;
			if (cmd != null)
			{
				cmd.Apply();
				w.FlushObservers();
			}
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// Commands parameters are automatically applied after the observer executes.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
	{
		// Use ObserveWithWorld to get World access, then fetch system parameters
		var entityApp = entity.OwningApp;
		var p1 = new T1();
		if (entityApp is not null) p1.Initialize(entityApp);
		var p2 = new T2();
		if (entityApp is not null) p2.Initialize(entityApp);
		var p3 = new T3();
		if (entityApp is not null) p3.Initialize(entityApp);
		var p4 = new T4();
		if (entityApp is not null) p4.Initialize(entityApp);
		var p5 = new T5();
		if (entityApp is not null) p5.Initialize(entityApp);
		var p6 = new T6();
		if (entityApp is not null) p6.Initialize(entityApp);
		var p7 = new T7();
		if (entityApp is not null) p7.Initialize(entityApp);
		var p8 = new T8();
		if (entityApp is not null) p8.Initialize(entityApp);
		return entity.ObserveWithWorld<TTrigger>((w, trigger) =>
		{
			if (entityApp is not null) p1.Fetch(entityApp);
			if (entityApp is not null) p2.Fetch(entityApp);
			if (entityApp is not null) p3.Fetch(entityApp);
			if (entityApp is not null) p4.Fetch(entityApp);
			if (entityApp is not null) p5.Fetch(entityApp);
			if (entityApp is not null) p6.Fetch(entityApp);
			if (entityApp is not null) p7.Fetch(entityApp);
			if (entityApp is not null) p8.Fetch(entityApp);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8);

			// Auto-apply Commands if any parameter is Commands type
			// Collect all Commands instances and apply them in one pass
			Commands? cmd = null;
			if (p1 is Commands c1) cmd = c1;
			if (p2 is Commands c2) cmd = c2;
			if (p3 is Commands c3) cmd = c3;
			if (p4 is Commands c4) cmd = c4;
			if (p5 is Commands c5) cmd = c5;
			if (p6 is Commands c6) cmd = c6;
			if (p7 is Commands c7) cmd = c7;
			if (p8 is Commands c8) cmd = c8;
			if (cmd != null)
			{
				cmd.Apply();
				w.FlushObservers();
			}
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// Commands parameters are automatically applied after the observer executes.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new()
	{
		// Use ObserveWithWorld to get World access, then fetch system parameters
		var entityApp = entity.OwningApp;
		var p1 = new T1();
		if (entityApp is not null) p1.Initialize(entityApp);
		var p2 = new T2();
		if (entityApp is not null) p2.Initialize(entityApp);
		var p3 = new T3();
		if (entityApp is not null) p3.Initialize(entityApp);
		var p4 = new T4();
		if (entityApp is not null) p4.Initialize(entityApp);
		var p5 = new T5();
		if (entityApp is not null) p5.Initialize(entityApp);
		var p6 = new T6();
		if (entityApp is not null) p6.Initialize(entityApp);
		var p7 = new T7();
		if (entityApp is not null) p7.Initialize(entityApp);
		var p8 = new T8();
		if (entityApp is not null) p8.Initialize(entityApp);
		var p9 = new T9();
		if (entityApp is not null) p9.Initialize(entityApp);
		return entity.ObserveWithWorld<TTrigger>((w, trigger) =>
		{
			if (entityApp is not null) p1.Fetch(entityApp);
			if (entityApp is not null) p2.Fetch(entityApp);
			if (entityApp is not null) p3.Fetch(entityApp);
			if (entityApp is not null) p4.Fetch(entityApp);
			if (entityApp is not null) p5.Fetch(entityApp);
			if (entityApp is not null) p6.Fetch(entityApp);
			if (entityApp is not null) p7.Fetch(entityApp);
			if (entityApp is not null) p8.Fetch(entityApp);
			if (entityApp is not null) p9.Fetch(entityApp);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9);

			// Auto-apply Commands if any parameter is Commands type
			// Collect all Commands instances and apply them in one pass
			Commands? cmd = null;
			if (p1 is Commands c1) cmd = c1;
			if (p2 is Commands c2) cmd = c2;
			if (p3 is Commands c3) cmd = c3;
			if (p4 is Commands c4) cmd = c4;
			if (p5 is Commands c5) cmd = c5;
			if (p6 is Commands c6) cmd = c6;
			if (p7 is Commands c7) cmd = c7;
			if (p8 is Commands c8) cmd = c8;
			if (p9 is Commands c9) cmd = c9;
			if (cmd != null)
			{
				cmd.Apply();
				w.FlushObservers();
			}
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// Commands parameters are automatically applied after the observer executes.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new()
	{
		// Use ObserveWithWorld to get World access, then fetch system parameters
		var entityApp = entity.OwningApp;
		var p1 = new T1();
		if (entityApp is not null) p1.Initialize(entityApp);
		var p2 = new T2();
		if (entityApp is not null) p2.Initialize(entityApp);
		var p3 = new T3();
		if (entityApp is not null) p3.Initialize(entityApp);
		var p4 = new T4();
		if (entityApp is not null) p4.Initialize(entityApp);
		var p5 = new T5();
		if (entityApp is not null) p5.Initialize(entityApp);
		var p6 = new T6();
		if (entityApp is not null) p6.Initialize(entityApp);
		var p7 = new T7();
		if (entityApp is not null) p7.Initialize(entityApp);
		var p8 = new T8();
		if (entityApp is not null) p8.Initialize(entityApp);
		var p9 = new T9();
		if (entityApp is not null) p9.Initialize(entityApp);
		var p10 = new T10();
		if (entityApp is not null) p10.Initialize(entityApp);
		return entity.ObserveWithWorld<TTrigger>((w, trigger) =>
		{
			if (entityApp is not null) p1.Fetch(entityApp);
			if (entityApp is not null) p2.Fetch(entityApp);
			if (entityApp is not null) p3.Fetch(entityApp);
			if (entityApp is not null) p4.Fetch(entityApp);
			if (entityApp is not null) p5.Fetch(entityApp);
			if (entityApp is not null) p6.Fetch(entityApp);
			if (entityApp is not null) p7.Fetch(entityApp);
			if (entityApp is not null) p8.Fetch(entityApp);
			if (entityApp is not null) p9.Fetch(entityApp);
			if (entityApp is not null) p10.Fetch(entityApp);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);

			// Auto-apply Commands if any parameter is Commands type
			// Collect all Commands instances and apply them in one pass
			Commands? cmd = null;
			if (p1 is Commands c1) cmd = c1;
			if (p2 is Commands c2) cmd = c2;
			if (p3 is Commands c3) cmd = c3;
			if (p4 is Commands c4) cmd = c4;
			if (p5 is Commands c5) cmd = c5;
			if (p6 is Commands c6) cmd = c6;
			if (p7 is Commands c7) cmd = c7;
			if (p8 is Commands c8) cmd = c8;
			if (p9 is Commands c9) cmd = c9;
			if (p10 is Commands c10) cmd = c10;
			if (cmd != null)
			{
				cmd.Apply();
				w.FlushObservers();
			}
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// Commands parameters are automatically applied after the observer executes.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new()
	{
		// Use ObserveWithWorld to get World access, then fetch system parameters
		var entityApp = entity.OwningApp;
		var p1 = new T1();
		if (entityApp is not null) p1.Initialize(entityApp);
		var p2 = new T2();
		if (entityApp is not null) p2.Initialize(entityApp);
		var p3 = new T3();
		if (entityApp is not null) p3.Initialize(entityApp);
		var p4 = new T4();
		if (entityApp is not null) p4.Initialize(entityApp);
		var p5 = new T5();
		if (entityApp is not null) p5.Initialize(entityApp);
		var p6 = new T6();
		if (entityApp is not null) p6.Initialize(entityApp);
		var p7 = new T7();
		if (entityApp is not null) p7.Initialize(entityApp);
		var p8 = new T8();
		if (entityApp is not null) p8.Initialize(entityApp);
		var p9 = new T9();
		if (entityApp is not null) p9.Initialize(entityApp);
		var p10 = new T10();
		if (entityApp is not null) p10.Initialize(entityApp);
		var p11 = new T11();
		if (entityApp is not null) p11.Initialize(entityApp);
		return entity.ObserveWithWorld<TTrigger>((w, trigger) =>
		{
			if (entityApp is not null) p1.Fetch(entityApp);
			if (entityApp is not null) p2.Fetch(entityApp);
			if (entityApp is not null) p3.Fetch(entityApp);
			if (entityApp is not null) p4.Fetch(entityApp);
			if (entityApp is not null) p5.Fetch(entityApp);
			if (entityApp is not null) p6.Fetch(entityApp);
			if (entityApp is not null) p7.Fetch(entityApp);
			if (entityApp is not null) p8.Fetch(entityApp);
			if (entityApp is not null) p9.Fetch(entityApp);
			if (entityApp is not null) p10.Fetch(entityApp);
			if (entityApp is not null) p11.Fetch(entityApp);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11);

			// Auto-apply Commands if any parameter is Commands type
			// Collect all Commands instances and apply them in one pass
			Commands? cmd = null;
			if (p1 is Commands c1) cmd = c1;
			if (p2 is Commands c2) cmd = c2;
			if (p3 is Commands c3) cmd = c3;
			if (p4 is Commands c4) cmd = c4;
			if (p5 is Commands c5) cmd = c5;
			if (p6 is Commands c6) cmd = c6;
			if (p7 is Commands c7) cmd = c7;
			if (p8 is Commands c8) cmd = c8;
			if (p9 is Commands c9) cmd = c9;
			if (p10 is Commands c10) cmd = c10;
			if (p11 is Commands c11) cmd = c11;
			if (cmd != null)
			{
				cmd.Apply();
				w.FlushObservers();
			}
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// Commands parameters are automatically applied after the observer executes.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
	{
		// Use ObserveWithWorld to get World access, then fetch system parameters
		var entityApp = entity.OwningApp;
		var p1 = new T1();
		if (entityApp is not null) p1.Initialize(entityApp);
		var p2 = new T2();
		if (entityApp is not null) p2.Initialize(entityApp);
		var p3 = new T3();
		if (entityApp is not null) p3.Initialize(entityApp);
		var p4 = new T4();
		if (entityApp is not null) p4.Initialize(entityApp);
		var p5 = new T5();
		if (entityApp is not null) p5.Initialize(entityApp);
		var p6 = new T6();
		if (entityApp is not null) p6.Initialize(entityApp);
		var p7 = new T7();
		if (entityApp is not null) p7.Initialize(entityApp);
		var p8 = new T8();
		if (entityApp is not null) p8.Initialize(entityApp);
		var p9 = new T9();
		if (entityApp is not null) p9.Initialize(entityApp);
		var p10 = new T10();
		if (entityApp is not null) p10.Initialize(entityApp);
		var p11 = new T11();
		if (entityApp is not null) p11.Initialize(entityApp);
		var p12 = new T12();
		if (entityApp is not null) p12.Initialize(entityApp);
		return entity.ObserveWithWorld<TTrigger>((w, trigger) =>
		{
			if (entityApp is not null) p1.Fetch(entityApp);
			if (entityApp is not null) p2.Fetch(entityApp);
			if (entityApp is not null) p3.Fetch(entityApp);
			if (entityApp is not null) p4.Fetch(entityApp);
			if (entityApp is not null) p5.Fetch(entityApp);
			if (entityApp is not null) p6.Fetch(entityApp);
			if (entityApp is not null) p7.Fetch(entityApp);
			if (entityApp is not null) p8.Fetch(entityApp);
			if (entityApp is not null) p9.Fetch(entityApp);
			if (entityApp is not null) p10.Fetch(entityApp);
			if (entityApp is not null) p11.Fetch(entityApp);
			if (entityApp is not null) p12.Fetch(entityApp);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12);

			// Auto-apply Commands if any parameter is Commands type
			// Collect all Commands instances and apply them in one pass
			Commands? cmd = null;
			if (p1 is Commands c1) cmd = c1;
			if (p2 is Commands c2) cmd = c2;
			if (p3 is Commands c3) cmd = c3;
			if (p4 is Commands c4) cmd = c4;
			if (p5 is Commands c5) cmd = c5;
			if (p6 is Commands c6) cmd = c6;
			if (p7 is Commands c7) cmd = c7;
			if (p8 is Commands c8) cmd = c8;
			if (p9 is Commands c9) cmd = c9;
			if (p10 is Commands c10) cmd = c10;
			if (p11 is Commands c11) cmd = c11;
			if (p12 is Commands c12) cmd = c12;
			if (cmd != null)
			{
				cmd.Apply();
				w.FlushObservers();
			}
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// Commands parameters are automatically applied after the observer executes.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new()
	{
		// Use ObserveWithWorld to get World access, then fetch system parameters
		var entityApp = entity.OwningApp;
		var p1 = new T1();
		if (entityApp is not null) p1.Initialize(entityApp);
		var p2 = new T2();
		if (entityApp is not null) p2.Initialize(entityApp);
		var p3 = new T3();
		if (entityApp is not null) p3.Initialize(entityApp);
		var p4 = new T4();
		if (entityApp is not null) p4.Initialize(entityApp);
		var p5 = new T5();
		if (entityApp is not null) p5.Initialize(entityApp);
		var p6 = new T6();
		if (entityApp is not null) p6.Initialize(entityApp);
		var p7 = new T7();
		if (entityApp is not null) p7.Initialize(entityApp);
		var p8 = new T8();
		if (entityApp is not null) p8.Initialize(entityApp);
		var p9 = new T9();
		if (entityApp is not null) p9.Initialize(entityApp);
		var p10 = new T10();
		if (entityApp is not null) p10.Initialize(entityApp);
		var p11 = new T11();
		if (entityApp is not null) p11.Initialize(entityApp);
		var p12 = new T12();
		if (entityApp is not null) p12.Initialize(entityApp);
		var p13 = new T13();
		if (entityApp is not null) p13.Initialize(entityApp);
		return entity.ObserveWithWorld<TTrigger>((w, trigger) =>
		{
			if (entityApp is not null) p1.Fetch(entityApp);
			if (entityApp is not null) p2.Fetch(entityApp);
			if (entityApp is not null) p3.Fetch(entityApp);
			if (entityApp is not null) p4.Fetch(entityApp);
			if (entityApp is not null) p5.Fetch(entityApp);
			if (entityApp is not null) p6.Fetch(entityApp);
			if (entityApp is not null) p7.Fetch(entityApp);
			if (entityApp is not null) p8.Fetch(entityApp);
			if (entityApp is not null) p9.Fetch(entityApp);
			if (entityApp is not null) p10.Fetch(entityApp);
			if (entityApp is not null) p11.Fetch(entityApp);
			if (entityApp is not null) p12.Fetch(entityApp);
			if (entityApp is not null) p13.Fetch(entityApp);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13);

			// Auto-apply Commands if any parameter is Commands type
			// Collect all Commands instances and apply them in one pass
			Commands? cmd = null;
			if (p1 is Commands c1) cmd = c1;
			if (p2 is Commands c2) cmd = c2;
			if (p3 is Commands c3) cmd = c3;
			if (p4 is Commands c4) cmd = c4;
			if (p5 is Commands c5) cmd = c5;
			if (p6 is Commands c6) cmd = c6;
			if (p7 is Commands c7) cmd = c7;
			if (p8 is Commands c8) cmd = c8;
			if (p9 is Commands c9) cmd = c9;
			if (p10 is Commands c10) cmd = c10;
			if (p11 is Commands c11) cmd = c11;
			if (p12 is Commands c12) cmd = c12;
			if (p13 is Commands c13) cmd = c13;
			if (cmd != null)
			{
				cmd.Apply();
				w.FlushObservers();
			}
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// Commands parameters are automatically applied after the observer executes.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new()
	{
		// Use ObserveWithWorld to get World access, then fetch system parameters
		var entityApp = entity.OwningApp;
		var p1 = new T1();
		if (entityApp is not null) p1.Initialize(entityApp);
		var p2 = new T2();
		if (entityApp is not null) p2.Initialize(entityApp);
		var p3 = new T3();
		if (entityApp is not null) p3.Initialize(entityApp);
		var p4 = new T4();
		if (entityApp is not null) p4.Initialize(entityApp);
		var p5 = new T5();
		if (entityApp is not null) p5.Initialize(entityApp);
		var p6 = new T6();
		if (entityApp is not null) p6.Initialize(entityApp);
		var p7 = new T7();
		if (entityApp is not null) p7.Initialize(entityApp);
		var p8 = new T8();
		if (entityApp is not null) p8.Initialize(entityApp);
		var p9 = new T9();
		if (entityApp is not null) p9.Initialize(entityApp);
		var p10 = new T10();
		if (entityApp is not null) p10.Initialize(entityApp);
		var p11 = new T11();
		if (entityApp is not null) p11.Initialize(entityApp);
		var p12 = new T12();
		if (entityApp is not null) p12.Initialize(entityApp);
		var p13 = new T13();
		if (entityApp is not null) p13.Initialize(entityApp);
		var p14 = new T14();
		if (entityApp is not null) p14.Initialize(entityApp);
		return entity.ObserveWithWorld<TTrigger>((w, trigger) =>
		{
			if (entityApp is not null) p1.Fetch(entityApp);
			if (entityApp is not null) p2.Fetch(entityApp);
			if (entityApp is not null) p3.Fetch(entityApp);
			if (entityApp is not null) p4.Fetch(entityApp);
			if (entityApp is not null) p5.Fetch(entityApp);
			if (entityApp is not null) p6.Fetch(entityApp);
			if (entityApp is not null) p7.Fetch(entityApp);
			if (entityApp is not null) p8.Fetch(entityApp);
			if (entityApp is not null) p9.Fetch(entityApp);
			if (entityApp is not null) p10.Fetch(entityApp);
			if (entityApp is not null) p11.Fetch(entityApp);
			if (entityApp is not null) p12.Fetch(entityApp);
			if (entityApp is not null) p13.Fetch(entityApp);
			if (entityApp is not null) p14.Fetch(entityApp);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14);

			// Auto-apply Commands if any parameter is Commands type
			// Collect all Commands instances and apply them in one pass
			Commands? cmd = null;
			if (p1 is Commands c1) cmd = c1;
			if (p2 is Commands c2) cmd = c2;
			if (p3 is Commands c3) cmd = c3;
			if (p4 is Commands c4) cmd = c4;
			if (p5 is Commands c5) cmd = c5;
			if (p6 is Commands c6) cmd = c6;
			if (p7 is Commands c7) cmd = c7;
			if (p8 is Commands c8) cmd = c8;
			if (p9 is Commands c9) cmd = c9;
			if (p10 is Commands c10) cmd = c10;
			if (p11 is Commands c11) cmd = c11;
			if (p12 is Commands c12) cmd = c12;
			if (p13 is Commands c13) cmd = c13;
			if (p14 is Commands c14) cmd = c14;
			if (cmd != null)
			{
				cmd.Apply();
				w.FlushObservers();
			}
		});
	}

	/// <summary>
	/// Register an entity-specific observer with system parameters.
	/// The observer reacts to triggers on this specific entity only.
	/// Commands parameters are automatically applied after the observer executes.
	/// </summary>
	public static EntityCommands Observe<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this EntityCommands entity, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new()
	{
		// Use ObserveWithWorld to get World access, then fetch system parameters
		var entityApp = entity.OwningApp;
		var p1 = new T1();
		if (entityApp is not null) p1.Initialize(entityApp);
		var p2 = new T2();
		if (entityApp is not null) p2.Initialize(entityApp);
		var p3 = new T3();
		if (entityApp is not null) p3.Initialize(entityApp);
		var p4 = new T4();
		if (entityApp is not null) p4.Initialize(entityApp);
		var p5 = new T5();
		if (entityApp is not null) p5.Initialize(entityApp);
		var p6 = new T6();
		if (entityApp is not null) p6.Initialize(entityApp);
		var p7 = new T7();
		if (entityApp is not null) p7.Initialize(entityApp);
		var p8 = new T8();
		if (entityApp is not null) p8.Initialize(entityApp);
		var p9 = new T9();
		if (entityApp is not null) p9.Initialize(entityApp);
		var p10 = new T10();
		if (entityApp is not null) p10.Initialize(entityApp);
		var p11 = new T11();
		if (entityApp is not null) p11.Initialize(entityApp);
		var p12 = new T12();
		if (entityApp is not null) p12.Initialize(entityApp);
		var p13 = new T13();
		if (entityApp is not null) p13.Initialize(entityApp);
		var p14 = new T14();
		if (entityApp is not null) p14.Initialize(entityApp);
		var p15 = new T15();
		if (entityApp is not null) p15.Initialize(entityApp);
		return entity.ObserveWithWorld<TTrigger>((w, trigger) =>
		{
			if (entityApp is not null) p1.Fetch(entityApp);
			if (entityApp is not null) p2.Fetch(entityApp);
			if (entityApp is not null) p3.Fetch(entityApp);
			if (entityApp is not null) p4.Fetch(entityApp);
			if (entityApp is not null) p5.Fetch(entityApp);
			if (entityApp is not null) p6.Fetch(entityApp);
			if (entityApp is not null) p7.Fetch(entityApp);
			if (entityApp is not null) p8.Fetch(entityApp);
			if (entityApp is not null) p9.Fetch(entityApp);
			if (entityApp is not null) p10.Fetch(entityApp);
			if (entityApp is not null) p11.Fetch(entityApp);
			if (entityApp is not null) p12.Fetch(entityApp);
			if (entityApp is not null) p13.Fetch(entityApp);
			if (entityApp is not null) p14.Fetch(entityApp);
			if (entityApp is not null) p15.Fetch(entityApp);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15);

			// Auto-apply Commands if any parameter is Commands type
			// Collect all Commands instances and apply them in one pass
			Commands? cmd = null;
			if (p1 is Commands c1) cmd = c1;
			if (p2 is Commands c2) cmd = c2;
			if (p3 is Commands c3) cmd = c3;
			if (p4 is Commands c4) cmd = c4;
			if (p5 is Commands c5) cmd = c5;
			if (p6 is Commands c6) cmd = c6;
			if (p7 is Commands c7) cmd = c7;
			if (p8 is Commands c8) cmd = c8;
			if (p9 is Commands c9) cmd = c9;
			if (p10 is Commands c10) cmd = c10;
			if (p11 is Commands c11) cmd = c11;
			if (p12 is Commands c12) cmd = c12;
			if (p13 is Commands c13) cmd = c13;
			if (p14 is Commands c14) cmd = c14;
			if (p15 is Commands c15) cmd = c15;
			if (cmd != null)
			{
				cmd.Apply();
				w.FlushObservers();
			}
		});
	}

}

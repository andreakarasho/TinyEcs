#pragma warning disable 1591
#nullable enable

using System;

namespace TinyEcs.Bevy;

public static partial class AppObserverExtensions
{
	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App AddObserver<TTrigger, T1>(this App app, Action<TTrigger, T1> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			callback(trigger, p1);
			if (p1 is Commands cmd1) cmd1.Apply();
		});

		return app;
	}

	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App AddObserver<TTrigger, T1, T2>(this App app, Action<TTrigger, T1, T2> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			callback(trigger, p1, p2);
			if (p1 is Commands cmd1) cmd1.Apply();
			if (p2 is Commands cmd2) cmd2.Apply();
		});

		return app;
	}

	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App AddObserver<TTrigger, T1, T2, T3>(this App app, Action<TTrigger, T1, T2, T3> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			callback(trigger, p1, p2, p3);
			if (p1 is Commands cmd1) cmd1.Apply();
			if (p2 is Commands cmd2) cmd2.Apply();
			if (p3 is Commands cmd3) cmd3.Apply();
		});

		return app;
	}

	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App AddObserver<TTrigger, T1, T2, T3, T4>(this App app, Action<TTrigger, T1, T2, T3, T4> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			callback(trigger, p1, p2, p3, p4);
			if (p1 is Commands cmd1) cmd1.Apply();
			if (p2 is Commands cmd2) cmd2.Apply();
			if (p3 is Commands cmd3) cmd3.Apply();
			if (p4 is Commands cmd4) cmd4.Apply();
		});

		return app;
	}

	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App AddObserver<TTrigger, T1, T2, T3, T4, T5>(this App app, Action<TTrigger, T1, T2, T3, T4, T5> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5);
			if (p1 is Commands cmd1) cmd1.Apply();
			if (p2 is Commands cmd2) cmd2.Apply();
			if (p3 is Commands cmd3) cmd3.Apply();
			if (p4 is Commands cmd4) cmd4.Apply();
			if (p5 is Commands cmd5) cmd5.Apply();
		});

		return app;
	}

	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App AddObserver<TTrigger, T1, T2, T3, T4, T5, T6>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6);
			if (p1 is Commands cmd1) cmd1.Apply();
			if (p2 is Commands cmd2) cmd2.Apply();
			if (p3 is Commands cmd3) cmd3.Apply();
			if (p4 is Commands cmd4) cmd4.Apply();
			if (p5 is Commands cmd5) cmd5.Apply();
			if (p6 is Commands cmd6) cmd6.Apply();
		});

		return app;
	}

	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App AddObserver<TTrigger, T1, T2, T3, T4, T5, T6, T7>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			p7.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6, p7);
			if (p1 is Commands cmd1) cmd1.Apply();
			if (p2 is Commands cmd2) cmd2.Apply();
			if (p3 is Commands cmd3) cmd3.Apply();
			if (p4 is Commands cmd4) cmd4.Apply();
			if (p5 is Commands cmd5) cmd5.Apply();
			if (p6 is Commands cmd6) cmd6.Apply();
			if (p7 is Commands cmd7) cmd7.Apply();
		});

		return app;
	}

	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App AddObserver<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		world.RegisterObserver<TTrigger>((w, trigger) =>
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
			if (p1 is Commands cmd1) cmd1.Apply();
			if (p2 is Commands cmd2) cmd2.Apply();
			if (p3 is Commands cmd3) cmd3.Apply();
			if (p4 is Commands cmd4) cmd4.Apply();
			if (p5 is Commands cmd5) cmd5.Apply();
			if (p6 is Commands cmd6) cmd6.Apply();
			if (p7 is Commands cmd7) cmd7.Apply();
			if (p8 is Commands cmd8) cmd8.Apply();
		});

		return app;
	}

	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App AddObserver<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

		var p1 = new T1();
		var p2 = new T2();
		var p3 = new T3();
		var p4 = new T4();
		var p5 = new T5();
		var p6 = new T6();
		var p7 = new T7();
		var p8 = new T8();
		var p9 = new T9();
		world.RegisterObserver<TTrigger>((w, trigger) =>
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
			if (p1 is Commands cmd1) cmd1.Apply();
			if (p2 is Commands cmd2) cmd2.Apply();
			if (p3 is Commands cmd3) cmd3.Apply();
			if (p4 is Commands cmd4) cmd4.Apply();
			if (p5 is Commands cmd5) cmd5.Apply();
			if (p6 is Commands cmd6) cmd6.Apply();
			if (p7 is Commands cmd7) cmd7.Apply();
			if (p8 is Commands cmd8) cmd8.Apply();
			if (p9 is Commands cmd9) cmd9.Apply();
		});

		return app;
	}

	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App AddObserver<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

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
		world.RegisterObserver<TTrigger>((w, trigger) =>
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
			if (p1 is Commands cmd1) cmd1.Apply();
			if (p2 is Commands cmd2) cmd2.Apply();
			if (p3 is Commands cmd3) cmd3.Apply();
			if (p4 is Commands cmd4) cmd4.Apply();
			if (p5 is Commands cmd5) cmd5.Apply();
			if (p6 is Commands cmd6) cmd6.Apply();
			if (p7 is Commands cmd7) cmd7.Apply();
			if (p8 is Commands cmd8) cmd8.Apply();
			if (p9 is Commands cmd9) cmd9.Apply();
			if (p10 is Commands cmd10) cmd10.Apply();
		});

		return app;
	}

	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App AddObserver<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

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
		world.RegisterObserver<TTrigger>((w, trigger) =>
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
			if (p1 is Commands cmd1) cmd1.Apply();
			if (p2 is Commands cmd2) cmd2.Apply();
			if (p3 is Commands cmd3) cmd3.Apply();
			if (p4 is Commands cmd4) cmd4.Apply();
			if (p5 is Commands cmd5) cmd5.Apply();
			if (p6 is Commands cmd6) cmd6.Apply();
			if (p7 is Commands cmd7) cmd7.Apply();
			if (p8 is Commands cmd8) cmd8.Apply();
			if (p9 is Commands cmd9) cmd9.Apply();
			if (p10 is Commands cmd10) cmd10.Apply();
			if (p11 is Commands cmd11) cmd11.Apply();
		});

		return app;
	}

	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App AddObserver<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

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
		world.RegisterObserver<TTrigger>((w, trigger) =>
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
			if (p1 is Commands cmd1) cmd1.Apply();
			if (p2 is Commands cmd2) cmd2.Apply();
			if (p3 is Commands cmd3) cmd3.Apply();
			if (p4 is Commands cmd4) cmd4.Apply();
			if (p5 is Commands cmd5) cmd5.Apply();
			if (p6 is Commands cmd6) cmd6.Apply();
			if (p7 is Commands cmd7) cmd7.Apply();
			if (p8 is Commands cmd8) cmd8.Apply();
			if (p9 is Commands cmd9) cmd9.Apply();
			if (p10 is Commands cmd10) cmd10.Apply();
			if (p11 is Commands cmd11) cmd11.Apply();
			if (p12 is Commands cmd12) cmd12.Apply();
		});

		return app;
	}

	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App AddObserver<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

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
		world.RegisterObserver<TTrigger>((w, trigger) =>
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
			if (p1 is Commands cmd1) cmd1.Apply();
			if (p2 is Commands cmd2) cmd2.Apply();
			if (p3 is Commands cmd3) cmd3.Apply();
			if (p4 is Commands cmd4) cmd4.Apply();
			if (p5 is Commands cmd5) cmd5.Apply();
			if (p6 is Commands cmd6) cmd6.Apply();
			if (p7 is Commands cmd7) cmd7.Apply();
			if (p8 is Commands cmd8) cmd8.Apply();
			if (p9 is Commands cmd9) cmd9.Apply();
			if (p10 is Commands cmd10) cmd10.Apply();
			if (p11 is Commands cmd11) cmd11.Apply();
			if (p12 is Commands cmd12) cmd12.Apply();
			if (p13 is Commands cmd13) cmd13.Apply();
		});

		return app;
	}

	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App AddObserver<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

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
		world.RegisterObserver<TTrigger>((w, trigger) =>
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
			if (p1 is Commands cmd1) cmd1.Apply();
			if (p2 is Commands cmd2) cmd2.Apply();
			if (p3 is Commands cmd3) cmd3.Apply();
			if (p4 is Commands cmd4) cmd4.Apply();
			if (p5 is Commands cmd5) cmd5.Apply();
			if (p6 is Commands cmd6) cmd6.Apply();
			if (p7 is Commands cmd7) cmd7.Apply();
			if (p8 is Commands cmd8) cmd8.Apply();
			if (p9 is Commands cmd9) cmd9.Apply();
			if (p10 is Commands cmd10) cmd10.Apply();
			if (p11 is Commands cmd11) cmd11.Apply();
			if (p12 is Commands cmd12) cmd12.Apply();
			if (p13 is Commands cmd13) cmd13.Apply();
			if (p14 is Commands cmd14) cmd14.Apply();
		});

		return app;
	}

	/// <summary>
	/// Register an observer with system parameters
	/// </summary>
	public static App AddObserver<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this App app, Action<TTrigger, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> callback)
		where TTrigger : struct, ITrigger
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new()
	{
		var world = app.GetWorld();

		// Auto-register component type if this is a component trigger
#if NET9_0_OR_GREATER
		TTrigger.Register(world);
#else
		default(TTrigger).Register(world);
#endif

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
		world.RegisterObserver<TTrigger>((w, trigger) =>
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
			if (p1 is Commands cmd1) cmd1.Apply();
			if (p2 is Commands cmd2) cmd2.Apply();
			if (p3 is Commands cmd3) cmd3.Apply();
			if (p4 is Commands cmd4) cmd4.Apply();
			if (p5 is Commands cmd5) cmd5.Apply();
			if (p6 is Commands cmd6) cmd6.Apply();
			if (p7 is Commands cmd7) cmd7.Apply();
			if (p8 is Commands cmd8) cmd8.Apply();
			if (p9 is Commands cmd9) cmd9.Apply();
			if (p10 is Commands cmd10) cmd10.Apply();
			if (p11 is Commands cmd11) cmd11.Apply();
			if (p12 is Commands cmd12) cmd12.Apply();
			if (p13 is Commands cmd13) cmd13.Apply();
			if (p14 is Commands cmd14) cmd14.Apply();
			if (p15 is Commands cmd15) cmd15.Apply();
		});

		return app;
	}

}

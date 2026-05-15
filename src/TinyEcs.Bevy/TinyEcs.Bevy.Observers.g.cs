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
		if (p1 is IAppAwareParam awareP1) awareP1.SetApp(app);
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			callback(trigger, p1);
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
		if (p1 is IAppAwareParam awareP1) awareP1.SetApp(app);
		var p2 = new T2();
		if (p2 is IAppAwareParam awareP2) awareP2.SetApp(app);
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			callback(trigger, p1, p2);
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
		if (p1 is IAppAwareParam awareP1) awareP1.SetApp(app);
		var p2 = new T2();
		if (p2 is IAppAwareParam awareP2) awareP2.SetApp(app);
		var p3 = new T3();
		if (p3 is IAppAwareParam awareP3) awareP3.SetApp(app);
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			callback(trigger, p1, p2, p3);
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
		if (p1 is IAppAwareParam awareP1) awareP1.SetApp(app);
		var p2 = new T2();
		if (p2 is IAppAwareParam awareP2) awareP2.SetApp(app);
		var p3 = new T3();
		if (p3 is IAppAwareParam awareP3) awareP3.SetApp(app);
		var p4 = new T4();
		if (p4 is IAppAwareParam awareP4) awareP4.SetApp(app);
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			callback(trigger, p1, p2, p3, p4);
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
		if (p1 is IAppAwareParam awareP1) awareP1.SetApp(app);
		var p2 = new T2();
		if (p2 is IAppAwareParam awareP2) awareP2.SetApp(app);
		var p3 = new T3();
		if (p3 is IAppAwareParam awareP3) awareP3.SetApp(app);
		var p4 = new T4();
		if (p4 is IAppAwareParam awareP4) awareP4.SetApp(app);
		var p5 = new T5();
		if (p5 is IAppAwareParam awareP5) awareP5.SetApp(app);
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5);
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
		if (p1 is IAppAwareParam awareP1) awareP1.SetApp(app);
		var p2 = new T2();
		if (p2 is IAppAwareParam awareP2) awareP2.SetApp(app);
		var p3 = new T3();
		if (p3 is IAppAwareParam awareP3) awareP3.SetApp(app);
		var p4 = new T4();
		if (p4 is IAppAwareParam awareP4) awareP4.SetApp(app);
		var p5 = new T5();
		if (p5 is IAppAwareParam awareP5) awareP5.SetApp(app);
		var p6 = new T6();
		if (p6 is IAppAwareParam awareP6) awareP6.SetApp(app);
		world.RegisterObserver<TTrigger>((w, trigger) =>
		{
			p1.Fetch(w);
			p2.Fetch(w);
			p3.Fetch(w);
			p4.Fetch(w);
			p5.Fetch(w);
			p6.Fetch(w);
			callback(trigger, p1, p2, p3, p4, p5, p6);
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
		if (p1 is IAppAwareParam awareP1) awareP1.SetApp(app);
		var p2 = new T2();
		if (p2 is IAppAwareParam awareP2) awareP2.SetApp(app);
		var p3 = new T3();
		if (p3 is IAppAwareParam awareP3) awareP3.SetApp(app);
		var p4 = new T4();
		if (p4 is IAppAwareParam awareP4) awareP4.SetApp(app);
		var p5 = new T5();
		if (p5 is IAppAwareParam awareP5) awareP5.SetApp(app);
		var p6 = new T6();
		if (p6 is IAppAwareParam awareP6) awareP6.SetApp(app);
		var p7 = new T7();
		if (p7 is IAppAwareParam awareP7) awareP7.SetApp(app);
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
		if (p1 is IAppAwareParam awareP1) awareP1.SetApp(app);
		var p2 = new T2();
		if (p2 is IAppAwareParam awareP2) awareP2.SetApp(app);
		var p3 = new T3();
		if (p3 is IAppAwareParam awareP3) awareP3.SetApp(app);
		var p4 = new T4();
		if (p4 is IAppAwareParam awareP4) awareP4.SetApp(app);
		var p5 = new T5();
		if (p5 is IAppAwareParam awareP5) awareP5.SetApp(app);
		var p6 = new T6();
		if (p6 is IAppAwareParam awareP6) awareP6.SetApp(app);
		var p7 = new T7();
		if (p7 is IAppAwareParam awareP7) awareP7.SetApp(app);
		var p8 = new T8();
		if (p8 is IAppAwareParam awareP8) awareP8.SetApp(app);
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
		if (p1 is IAppAwareParam awareP1) awareP1.SetApp(app);
		var p2 = new T2();
		if (p2 is IAppAwareParam awareP2) awareP2.SetApp(app);
		var p3 = new T3();
		if (p3 is IAppAwareParam awareP3) awareP3.SetApp(app);
		var p4 = new T4();
		if (p4 is IAppAwareParam awareP4) awareP4.SetApp(app);
		var p5 = new T5();
		if (p5 is IAppAwareParam awareP5) awareP5.SetApp(app);
		var p6 = new T6();
		if (p6 is IAppAwareParam awareP6) awareP6.SetApp(app);
		var p7 = new T7();
		if (p7 is IAppAwareParam awareP7) awareP7.SetApp(app);
		var p8 = new T8();
		if (p8 is IAppAwareParam awareP8) awareP8.SetApp(app);
		var p9 = new T9();
		if (p9 is IAppAwareParam awareP9) awareP9.SetApp(app);
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
		if (p1 is IAppAwareParam awareP1) awareP1.SetApp(app);
		var p2 = new T2();
		if (p2 is IAppAwareParam awareP2) awareP2.SetApp(app);
		var p3 = new T3();
		if (p3 is IAppAwareParam awareP3) awareP3.SetApp(app);
		var p4 = new T4();
		if (p4 is IAppAwareParam awareP4) awareP4.SetApp(app);
		var p5 = new T5();
		if (p5 is IAppAwareParam awareP5) awareP5.SetApp(app);
		var p6 = new T6();
		if (p6 is IAppAwareParam awareP6) awareP6.SetApp(app);
		var p7 = new T7();
		if (p7 is IAppAwareParam awareP7) awareP7.SetApp(app);
		var p8 = new T8();
		if (p8 is IAppAwareParam awareP8) awareP8.SetApp(app);
		var p9 = new T9();
		if (p9 is IAppAwareParam awareP9) awareP9.SetApp(app);
		var p10 = new T10();
		if (p10 is IAppAwareParam awareP10) awareP10.SetApp(app);
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
		if (p1 is IAppAwareParam awareP1) awareP1.SetApp(app);
		var p2 = new T2();
		if (p2 is IAppAwareParam awareP2) awareP2.SetApp(app);
		var p3 = new T3();
		if (p3 is IAppAwareParam awareP3) awareP3.SetApp(app);
		var p4 = new T4();
		if (p4 is IAppAwareParam awareP4) awareP4.SetApp(app);
		var p5 = new T5();
		if (p5 is IAppAwareParam awareP5) awareP5.SetApp(app);
		var p6 = new T6();
		if (p6 is IAppAwareParam awareP6) awareP6.SetApp(app);
		var p7 = new T7();
		if (p7 is IAppAwareParam awareP7) awareP7.SetApp(app);
		var p8 = new T8();
		if (p8 is IAppAwareParam awareP8) awareP8.SetApp(app);
		var p9 = new T9();
		if (p9 is IAppAwareParam awareP9) awareP9.SetApp(app);
		var p10 = new T10();
		if (p10 is IAppAwareParam awareP10) awareP10.SetApp(app);
		var p11 = new T11();
		if (p11 is IAppAwareParam awareP11) awareP11.SetApp(app);
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
		if (p1 is IAppAwareParam awareP1) awareP1.SetApp(app);
		var p2 = new T2();
		if (p2 is IAppAwareParam awareP2) awareP2.SetApp(app);
		var p3 = new T3();
		if (p3 is IAppAwareParam awareP3) awareP3.SetApp(app);
		var p4 = new T4();
		if (p4 is IAppAwareParam awareP4) awareP4.SetApp(app);
		var p5 = new T5();
		if (p5 is IAppAwareParam awareP5) awareP5.SetApp(app);
		var p6 = new T6();
		if (p6 is IAppAwareParam awareP6) awareP6.SetApp(app);
		var p7 = new T7();
		if (p7 is IAppAwareParam awareP7) awareP7.SetApp(app);
		var p8 = new T8();
		if (p8 is IAppAwareParam awareP8) awareP8.SetApp(app);
		var p9 = new T9();
		if (p9 is IAppAwareParam awareP9) awareP9.SetApp(app);
		var p10 = new T10();
		if (p10 is IAppAwareParam awareP10) awareP10.SetApp(app);
		var p11 = new T11();
		if (p11 is IAppAwareParam awareP11) awareP11.SetApp(app);
		var p12 = new T12();
		if (p12 is IAppAwareParam awareP12) awareP12.SetApp(app);
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
		if (p1 is IAppAwareParam awareP1) awareP1.SetApp(app);
		var p2 = new T2();
		if (p2 is IAppAwareParam awareP2) awareP2.SetApp(app);
		var p3 = new T3();
		if (p3 is IAppAwareParam awareP3) awareP3.SetApp(app);
		var p4 = new T4();
		if (p4 is IAppAwareParam awareP4) awareP4.SetApp(app);
		var p5 = new T5();
		if (p5 is IAppAwareParam awareP5) awareP5.SetApp(app);
		var p6 = new T6();
		if (p6 is IAppAwareParam awareP6) awareP6.SetApp(app);
		var p7 = new T7();
		if (p7 is IAppAwareParam awareP7) awareP7.SetApp(app);
		var p8 = new T8();
		if (p8 is IAppAwareParam awareP8) awareP8.SetApp(app);
		var p9 = new T9();
		if (p9 is IAppAwareParam awareP9) awareP9.SetApp(app);
		var p10 = new T10();
		if (p10 is IAppAwareParam awareP10) awareP10.SetApp(app);
		var p11 = new T11();
		if (p11 is IAppAwareParam awareP11) awareP11.SetApp(app);
		var p12 = new T12();
		if (p12 is IAppAwareParam awareP12) awareP12.SetApp(app);
		var p13 = new T13();
		if (p13 is IAppAwareParam awareP13) awareP13.SetApp(app);
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
		if (p1 is IAppAwareParam awareP1) awareP1.SetApp(app);
		var p2 = new T2();
		if (p2 is IAppAwareParam awareP2) awareP2.SetApp(app);
		var p3 = new T3();
		if (p3 is IAppAwareParam awareP3) awareP3.SetApp(app);
		var p4 = new T4();
		if (p4 is IAppAwareParam awareP4) awareP4.SetApp(app);
		var p5 = new T5();
		if (p5 is IAppAwareParam awareP5) awareP5.SetApp(app);
		var p6 = new T6();
		if (p6 is IAppAwareParam awareP6) awareP6.SetApp(app);
		var p7 = new T7();
		if (p7 is IAppAwareParam awareP7) awareP7.SetApp(app);
		var p8 = new T8();
		if (p8 is IAppAwareParam awareP8) awareP8.SetApp(app);
		var p9 = new T9();
		if (p9 is IAppAwareParam awareP9) awareP9.SetApp(app);
		var p10 = new T10();
		if (p10 is IAppAwareParam awareP10) awareP10.SetApp(app);
		var p11 = new T11();
		if (p11 is IAppAwareParam awareP11) awareP11.SetApp(app);
		var p12 = new T12();
		if (p12 is IAppAwareParam awareP12) awareP12.SetApp(app);
		var p13 = new T13();
		if (p13 is IAppAwareParam awareP13) awareP13.SetApp(app);
		var p14 = new T14();
		if (p14 is IAppAwareParam awareP14) awareP14.SetApp(app);
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
		if (p1 is IAppAwareParam awareP1) awareP1.SetApp(app);
		var p2 = new T2();
		if (p2 is IAppAwareParam awareP2) awareP2.SetApp(app);
		var p3 = new T3();
		if (p3 is IAppAwareParam awareP3) awareP3.SetApp(app);
		var p4 = new T4();
		if (p4 is IAppAwareParam awareP4) awareP4.SetApp(app);
		var p5 = new T5();
		if (p5 is IAppAwareParam awareP5) awareP5.SetApp(app);
		var p6 = new T6();
		if (p6 is IAppAwareParam awareP6) awareP6.SetApp(app);
		var p7 = new T7();
		if (p7 is IAppAwareParam awareP7) awareP7.SetApp(app);
		var p8 = new T8();
		if (p8 is IAppAwareParam awareP8) awareP8.SetApp(app);
		var p9 = new T9();
		if (p9 is IAppAwareParam awareP9) awareP9.SetApp(app);
		var p10 = new T10();
		if (p10 is IAppAwareParam awareP10) awareP10.SetApp(app);
		var p11 = new T11();
		if (p11 is IAppAwareParam awareP11) awareP11.SetApp(app);
		var p12 = new T12();
		if (p12 is IAppAwareParam awareP12) awareP12.SetApp(app);
		var p13 = new T13();
		if (p13 is IAppAwareParam awareP13) awareP13.SetApp(app);
		var p14 = new T14();
		if (p14 is IAppAwareParam awareP14) awareP14.SetApp(app);
		var p15 = new T15();
		if (p15 is IAppAwareParam awareP15) awareP15.SetApp(app);
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
		});

		return app;
	}

}

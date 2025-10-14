#pragma warning disable 1591
#nullable enable

using System;

namespace TinyEcs.Bevy;

public static class SystemExtensions
{
	// ============================================================================
	// AddSystem Extensions - Fluent API (returns ISystemStageSelector)
	// ============================================================================

	public static ISystemStageSelector AddSystem<T1>(this App app, Action<T1> systemFn)
		where T1 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	public static ISystemStageSelector AddSystem<T1, T2>(this App app, Action<T1, T2> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	public static ISystemStageSelector AddSystem<T1, T2, T3>(this App app, Action<T1, T2, T3> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4>(this App app, Action<T1, T2, T3, T4> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5>(this App app, Action<T1, T2, T3, T4, T5> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6>(this App app, Action<T1, T2, T3, T4, T5, T6> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7>(this App app, Action<T1, T2, T3, T4, T5, T6, T7> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	public static ISystemStageSelector AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this App app, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new() where T16 : ISystemParam, new()
	{
		return app.AddSystem(SystemFunctionAdapters.Create(systemFn));
	}

	// ============================================================================
	// AddSystem Extensions - Direct Stage API (returns App)
	// ============================================================================

	public static App AddSystem<T1>(this App app, Stage stage, Action<T1> systemFn)
		where T1 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	public static App AddSystem<T1, T2>(this App app, Stage stage, Action<T1, T2> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	public static App AddSystem<T1, T2, T3>(this App app, Stage stage, Action<T1, T2, T3> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	public static App AddSystem<T1, T2, T3, T4>(this App app, Stage stage, Action<T1, T2, T3, T4> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	public static App AddSystem<T1, T2, T3, T4, T5>(this App app, Stage stage, Action<T1, T2, T3, T4, T5> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	public static App AddSystem<T1, T2, T3, T4, T5, T6>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	public static App AddSystem<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this App app, Stage stage, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new() where T16 : ISystemParam, new()
	{
		return app.AddSystem(stage, SystemFunctionAdapters.Create(systemFn));
	}

	// ============================================================================
	// RunIf Extensions - ISystemConfigurator
	// ============================================================================

	public static ISystemConfigurator RunIf<T1>(this ISystemConfigurator configurator, Func<T1, bool> condition)
		where T1 : ISystemParam, new()
	{
		var p1 = new T1();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world);
			return condition(p1);
		});
	}

	public static ISystemConfigurator RunIf<T1, T2>(this ISystemConfigurator configurator, Func<T1, T2, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world);
			return condition(p1, p2);
		});
	}

	public static ISystemConfigurator RunIf<T1, T2, T3>(this ISystemConfigurator configurator, Func<T1, T2, T3, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world);
			return condition(p1, p2, p3);
		});
	}

	public static ISystemConfigurator RunIf<T1, T2, T3, T4>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world);
			return condition(p1, p2, p3, p4);
		});
	}

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world);
			return condition(p1, p2, p3, p4, p5);
		});
	}

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6);
		});
	}

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7);
		});
	}

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8);
		});
	}

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9);
		});
	}

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
		});
	}

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11);
		});
	}

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12);
		});
	}

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13);
		});
	}

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14);
		});
	}

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world); p15.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15);
		});
	}

	public static ISystemConfigurator RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this ISystemConfigurator configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new() where T16 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15(); var p16 = new T16();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world); p15.Fetch(world); p16.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16);
		});
	}

	// ============================================================================
	// RunIf Extensions - ISystemConfiguratorLabeled
	// ============================================================================

	public static ISystemConfiguratorLabeled RunIf<T1>(this ISystemConfiguratorLabeled configurator, Func<T1, bool> condition)
		where T1 : ISystemParam, new()
	{
		var p1 = new T1();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world);
			return condition(p1);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world);
			return condition(p1, p2);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world);
			return condition(p1, p2, p3);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world);
			return condition(p1, p2, p3, p4);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world);
			return condition(p1, p2, p3, p4, p5);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world); p15.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15);
		});
	}

	public static ISystemConfiguratorLabeled RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this ISystemConfiguratorLabeled configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new() where T16 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15(); var p16 = new T16();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world); p15.Fetch(world); p16.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16);
		});
	}

	// ============================================================================
	// RunIf Extensions - ISystemConfiguratorOrdered
	// ============================================================================

	public static ISystemConfiguratorOrdered RunIf<T1>(this ISystemConfiguratorOrdered configurator, Func<T1, bool> condition)
		where T1 : ISystemParam, new()
	{
		var p1 = new T1();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world);
			return condition(p1);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world);
			return condition(p1, p2);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world);
			return condition(p1, p2, p3);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world);
			return condition(p1, p2, p3, p4);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world);
			return condition(p1, p2, p3, p4, p5);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world); p15.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15);
		});
	}

	public static ISystemConfiguratorOrdered RunIf<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this ISystemConfiguratorOrdered configurator, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, bool> condition)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new() where T16 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15(); var p16 = new T16();
		return configurator.RunIf(world =>
		{
			p1.Fetch(world); p2.Fetch(world); p3.Fetch(world); p4.Fetch(world); p5.Fetch(world); p6.Fetch(world); p7.Fetch(world); p8.Fetch(world); p9.Fetch(world); p10.Fetch(world); p11.Fetch(world); p12.Fetch(world); p13.Fetch(world); p14.Fetch(world); p15.Fetch(world); p16.Fetch(world);
			return condition(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16);
		});
	}
}

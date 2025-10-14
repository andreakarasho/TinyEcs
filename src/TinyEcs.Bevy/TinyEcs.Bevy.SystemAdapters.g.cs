#pragma warning disable 1591
#nullable enable

namespace TinyEcs.Bevy;

public static class SystemFunctionAdapters
{
	public static ISystem Create<T1>(Action<T1> systemFn)
		where T1 : ISystemParam, new()
	{
		var p1 = new T1();
		return new ParameterizedSystem(
			world => systemFn(p1),
			p1
		);
	}

	public static ISystem Create<T1, T2>(Action<T1, T2> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2();
		return new ParameterizedSystem(
			world => systemFn(p1, p2),
			p1, p2
		);
	}

	public static ISystem Create<T1, T2, T3>(Action<T1, T2, T3> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3),
			p1, p2, p3
		);
	}

	public static ISystem Create<T1, T2, T3, T4>(Action<T1, T2, T3, T4> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4),
			p1, p2, p3, p4
		);
	}

	public static ISystem Create<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5),
			p1, p2, p3, p4, p5
		);
	}

	public static ISystem Create<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6),
			p1, p2, p3, p4, p5, p6
		);
	}

	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6, p7),
			p1, p2, p3, p4, p5, p6, p7
		);
	}

	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8),
			p1, p2, p3, p4, p5, p6, p7, p8
		);
	}

	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9),
			p1, p2, p3, p4, p5, p6, p7, p8, p9
		);
	}

	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10),
			p1, p2, p3, p4, p5, p6, p7, p8, p9, p10
		);
	}

	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11),
			p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11
		);
	}

	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12),
			p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12
		);
	}

	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13),
			p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13
		);
	}

	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14),
			p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14
		);
	}

	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15),
			p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15
		);
	}

	public static ISystem Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> systemFn)
		where T1 : ISystemParam, new() where T2 : ISystemParam, new() where T3 : ISystemParam, new() where T4 : ISystemParam, new() where T5 : ISystemParam, new() where T6 : ISystemParam, new() where T7 : ISystemParam, new() where T8 : ISystemParam, new() where T9 : ISystemParam, new() where T10 : ISystemParam, new() where T11 : ISystemParam, new() where T12 : ISystemParam, new() where T13 : ISystemParam, new() where T14 : ISystemParam, new() where T15 : ISystemParam, new() where T16 : ISystemParam, new()
	{
		var p1 = new T1(); var p2 = new T2(); var p3 = new T3(); var p4 = new T4(); var p5 = new T5(); var p6 = new T6(); var p7 = new T7(); var p8 = new T8(); var p9 = new T9(); var p10 = new T10(); var p11 = new T11(); var p12 = new T12(); var p13 = new T13(); var p14 = new T14(); var p15 = new T15(); var p16 = new T16();
		return new ParameterizedSystem(
			world => systemFn(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16),
			p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16
		);
	}

}

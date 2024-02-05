namespace TinyEcs;
partial struct Query
{
	public delegate void QueryTemplateWithEntity<T0>(ref readonly EntityView entity, ref T0 t0) where T0 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1>(ref readonly EntityView entity, ref T0 t0, ref T1 t1) where T0 : struct where T1 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2) where T0 : struct where T1 : struct where T2 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3) where T0 : struct where T1 : struct where T2 : struct where T3 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17, ref T18 t18) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17, ref T18 t18, ref T19 t19) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17, ref T18 t18, ref T19 t19, ref T20 t20) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct where T20 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17, ref T18 t18, ref T19 t19, ref T20 t20, ref T21 t21) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct where T20 : struct where T21 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17, ref T18 t18, ref T19 t19, ref T20 t20, ref T21 t21, ref T22 t22) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct where T20 : struct where T21 : struct where T22 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17, ref T18 t18, ref T19 t19, ref T20 t20, ref T21 t21, ref T22 t22, ref T23 t23) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct where T20 : struct where T21 : struct where T22 : struct where T23 : struct;
	public delegate void QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(ref readonly EntityView entity, ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17, ref T18 t18, ref T19 t19, ref T20 t20, ref T21 t21, ref T22 t22, ref T23 t23, ref T24 t24) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct where T20 : struct where T21 : struct where T22 : struct where T23 : struct where T24 : struct;



	public void System<TPhase, T0>(QueryTemplateWithEntity<T0> fn) where TPhase : struct where T0 : struct
	{
		With<T0>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1>(QueryTemplateWithEntity<T0, T1> fn) where TPhase : struct where T0 : struct where T1 : struct
	{
		With<T0>();
		With<T1>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2>(QueryTemplateWithEntity<T0, T1, T2> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3>(QueryTemplateWithEntity<T0, T1, T2, T3> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4>(QueryTemplateWithEntity<T0, T1, T2, T3, T4> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);
				var t10A = it.Field<T10>(10);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);
				var t10A = it.Field<T10>(10);
				var t11A = it.Field<T11>(11);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);
				var t10A = it.Field<T10>(10);
				var t11A = it.Field<T11>(11);
				var t12A = it.Field<T12>(12);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);
				var t10A = it.Field<T10>(10);
				var t11A = it.Field<T11>(11);
				var t12A = it.Field<T12>(12);
				var t13A = it.Field<T13>(13);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);
				var t10A = it.Field<T10>(10);
				var t11A = it.Field<T11>(11);
				var t12A = it.Field<T12>(12);
				var t13A = it.Field<T13>(13);
				var t14A = it.Field<T14>(14);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);
				var t10A = it.Field<T10>(10);
				var t11A = it.Field<T11>(11);
				var t12A = it.Field<T12>(12);
				var t13A = it.Field<T13>(13);
				var t14A = it.Field<T14>(14);
				var t15A = it.Field<T15>(15);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);
				var t10A = it.Field<T10>(10);
				var t11A = it.Field<T11>(11);
				var t12A = it.Field<T12>(12);
				var t13A = it.Field<T13>(13);
				var t14A = it.Field<T14>(14);
				var t15A = it.Field<T15>(15);
				var t16A = it.Field<T16>(16);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);
				var t10A = it.Field<T10>(10);
				var t11A = it.Field<T11>(11);
				var t12A = it.Field<T12>(12);
				var t13A = it.Field<T13>(13);
				var t14A = it.Field<T14>(14);
				var t15A = it.Field<T15>(15);
				var t16A = it.Field<T16>(16);
				var t17A = it.Field<T17>(17);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();
		With<T18>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);
				var t10A = it.Field<T10>(10);
				var t11A = it.Field<T11>(11);
				var t12A = it.Field<T12>(12);
				var t13A = it.Field<T13>(13);
				var t14A = it.Field<T14>(14);
				var t15A = it.Field<T15>(15);
				var t16A = it.Field<T16>(16);
				var t17A = it.Field<T17>(17);
				var t18A = it.Field<T18>(18);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();
		With<T18>();
		With<T19>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);
				var t10A = it.Field<T10>(10);
				var t11A = it.Field<T11>(11);
				var t12A = it.Field<T12>(12);
				var t13A = it.Field<T13>(13);
				var t14A = it.Field<T14>(14);
				var t15A = it.Field<T15>(15);
				var t16A = it.Field<T16>(16);
				var t17A = it.Field<T17>(17);
				var t18A = it.Field<T18>(18);
				var t19A = it.Field<T19>(19);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct where T20 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();
		With<T18>();
		With<T19>();
		With<T20>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);
				var t10A = it.Field<T10>(10);
				var t11A = it.Field<T11>(11);
				var t12A = it.Field<T12>(12);
				var t13A = it.Field<T13>(13);
				var t14A = it.Field<T14>(14);
				var t15A = it.Field<T15>(15);
				var t16A = it.Field<T16>(16);
				var t17A = it.Field<T17>(17);
				var t18A = it.Field<T18>(18);
				var t19A = it.Field<T19>(19);
				var t20A = it.Field<T20>(20);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct where T20 : struct where T21 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();
		With<T18>();
		With<T19>();
		With<T20>();
		With<T21>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);
				var t10A = it.Field<T10>(10);
				var t11A = it.Field<T11>(11);
				var t12A = it.Field<T12>(12);
				var t13A = it.Field<T13>(13);
				var t14A = it.Field<T14>(14);
				var t15A = it.Field<T15>(15);
				var t16A = it.Field<T16>(16);
				var t17A = it.Field<T17>(17);
				var t18A = it.Field<T18>(18);
				var t19A = it.Field<T19>(19);
				var t20A = it.Field<T20>(20);
				var t21A = it.Field<T21>(21);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct where T20 : struct where T21 : struct where T22 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();
		With<T18>();
		With<T19>();
		With<T20>();
		With<T21>();
		With<T22>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);
				var t10A = it.Field<T10>(10);
				var t11A = it.Field<T11>(11);
				var t12A = it.Field<T12>(12);
				var t13A = it.Field<T13>(13);
				var t14A = it.Field<T14>(14);
				var t15A = it.Field<T15>(15);
				var t16A = it.Field<T16>(16);
				var t17A = it.Field<T17>(17);
				var t18A = it.Field<T18>(18);
				var t19A = it.Field<T19>(19);
				var t20A = it.Field<T20>(20);
				var t21A = it.Field<T21>(21);
				var t22A = it.Field<T22>(22);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i], ref t22A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct where T20 : struct where T21 : struct where T22 : struct where T23 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();
		With<T18>();
		With<T19>();
		With<T20>();
		With<T21>();
		With<T22>();
		With<T23>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);
				var t10A = it.Field<T10>(10);
				var t11A = it.Field<T11>(11);
				var t12A = it.Field<T12>(12);
				var t13A = it.Field<T13>(13);
				var t14A = it.Field<T14>(14);
				var t15A = it.Field<T15>(15);
				var t16A = it.Field<T16>(16);
				var t17A = it.Field<T17>(17);
				var t18A = it.Field<T18>(18);
				var t19A = it.Field<T19>(19);
				var t20A = it.Field<T20>(20);
				var t21A = it.Field<T21>(21);
				var t22A = it.Field<T22>(22);
				var t23A = it.Field<T23>(23);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i], ref t22A[i], ref t23A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> fn) where TPhase : struct where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct where T20 : struct where T21 : struct where T22 : struct where T23 : struct where T24 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();
		With<T18>();
		With<T19>();
		With<T20>();
		With<T21>();
		With<T22>();
		With<T23>();
		With<T24>();

		var terms = Terms;
		EcsID query = terms.Length > 0 ? _world.Entity() : 0;

		_world.Entity()
			.Set(new EcsSystem((ref Iterator it) =>
			{
				var t0A = it.Field<T0>(0);
				var t1A = it.Field<T1>(1);
				var t2A = it.Field<T2>(2);
				var t3A = it.Field<T3>(3);
				var t4A = it.Field<T4>(4);
				var t5A = it.Field<T5>(5);
				var t6A = it.Field<T6>(6);
				var t7A = it.Field<T7>(7);
				var t8A = it.Field<T8>(8);
				var t9A = it.Field<T9>(9);
				var t10A = it.Field<T10>(10);
				var t11A = it.Field<T11>(11);
				var t12A = it.Field<T12>(12);
				var t13A = it.Field<T13>(13);
				var t14A = it.Field<T14>(14);
				var t15A = it.Field<T15>(15);
				var t16A = it.Field<T16>(16);
				var t17A = it.Field<T17>(17);
				var t18A = it.Field<T18>(18);
				var t19A = it.Field<T19>(19);
				var t20A = it.Field<T20>(20);
				var t21A = it.Field<T21>(21);
				var t22A = it.Field<T22>(22);
				var t23A = it.Field<T23>(23);
				var t24A = it.Field<T24>(24);

				for (int i = 0; i < it.Count; ++i)
				{
					fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i], ref t22A[i], ref t23A[i], ref t24A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}



	public void Iterator<T0>(QueryTemplateWithEntity<T0> fn) where T0 : struct
	{
		With<T0>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i]);
			}
		});
	}

	public void Iterator<T0, T1>(QueryTemplateWithEntity<T0, T1> fn) where T0 : struct where T1 : struct
	{
		With<T0>();
		With<T1>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2>(QueryTemplateWithEntity<T0, T1, T2> fn) where T0 : struct where T1 : struct where T2 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3>(QueryTemplateWithEntity<T0, T1, T2, T3> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4>(QueryTemplateWithEntity<T0, T1, T2, T3, T4> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);
			var t10A = it.Field<T10>(10);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);
			var t10A = it.Field<T10>(10);
			var t11A = it.Field<T11>(11);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);
			var t10A = it.Field<T10>(10);
			var t11A = it.Field<T11>(11);
			var t12A = it.Field<T12>(12);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);
			var t10A = it.Field<T10>(10);
			var t11A = it.Field<T11>(11);
			var t12A = it.Field<T12>(12);
			var t13A = it.Field<T13>(13);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);
			var t10A = it.Field<T10>(10);
			var t11A = it.Field<T11>(11);
			var t12A = it.Field<T12>(12);
			var t13A = it.Field<T13>(13);
			var t14A = it.Field<T14>(14);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);
			var t10A = it.Field<T10>(10);
			var t11A = it.Field<T11>(11);
			var t12A = it.Field<T12>(12);
			var t13A = it.Field<T13>(13);
			var t14A = it.Field<T14>(14);
			var t15A = it.Field<T15>(15);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);
			var t10A = it.Field<T10>(10);
			var t11A = it.Field<T11>(11);
			var t12A = it.Field<T12>(12);
			var t13A = it.Field<T13>(13);
			var t14A = it.Field<T14>(14);
			var t15A = it.Field<T15>(15);
			var t16A = it.Field<T16>(16);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);
			var t10A = it.Field<T10>(10);
			var t11A = it.Field<T11>(11);
			var t12A = it.Field<T12>(12);
			var t13A = it.Field<T13>(13);
			var t14A = it.Field<T14>(14);
			var t15A = it.Field<T15>(15);
			var t16A = it.Field<T16>(16);
			var t17A = it.Field<T17>(17);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();
		With<T18>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);
			var t10A = it.Field<T10>(10);
			var t11A = it.Field<T11>(11);
			var t12A = it.Field<T12>(12);
			var t13A = it.Field<T13>(13);
			var t14A = it.Field<T14>(14);
			var t15A = it.Field<T15>(15);
			var t16A = it.Field<T16>(16);
			var t17A = it.Field<T17>(17);
			var t18A = it.Field<T18>(18);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();
		With<T18>();
		With<T19>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);
			var t10A = it.Field<T10>(10);
			var t11A = it.Field<T11>(11);
			var t12A = it.Field<T12>(12);
			var t13A = it.Field<T13>(13);
			var t14A = it.Field<T14>(14);
			var t15A = it.Field<T15>(15);
			var t16A = it.Field<T16>(16);
			var t17A = it.Field<T17>(17);
			var t18A = it.Field<T18>(18);
			var t19A = it.Field<T19>(19);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct where T20 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();
		With<T18>();
		With<T19>();
		With<T20>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);
			var t10A = it.Field<T10>(10);
			var t11A = it.Field<T11>(11);
			var t12A = it.Field<T12>(12);
			var t13A = it.Field<T13>(13);
			var t14A = it.Field<T14>(14);
			var t15A = it.Field<T15>(15);
			var t16A = it.Field<T16>(16);
			var t17A = it.Field<T17>(17);
			var t18A = it.Field<T18>(18);
			var t19A = it.Field<T19>(19);
			var t20A = it.Field<T20>(20);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct where T20 : struct where T21 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();
		With<T18>();
		With<T19>();
		With<T20>();
		With<T21>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);
			var t10A = it.Field<T10>(10);
			var t11A = it.Field<T11>(11);
			var t12A = it.Field<T12>(12);
			var t13A = it.Field<T13>(13);
			var t14A = it.Field<T14>(14);
			var t15A = it.Field<T15>(15);
			var t16A = it.Field<T16>(16);
			var t17A = it.Field<T17>(17);
			var t18A = it.Field<T18>(18);
			var t19A = it.Field<T19>(19);
			var t20A = it.Field<T20>(20);
			var t21A = it.Field<T21>(21);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct where T20 : struct where T21 : struct where T22 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();
		With<T18>();
		With<T19>();
		With<T20>();
		With<T21>();
		With<T22>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);
			var t10A = it.Field<T10>(10);
			var t11A = it.Field<T11>(11);
			var t12A = it.Field<T12>(12);
			var t13A = it.Field<T13>(13);
			var t14A = it.Field<T14>(14);
			var t15A = it.Field<T15>(15);
			var t16A = it.Field<T16>(16);
			var t17A = it.Field<T17>(17);
			var t18A = it.Field<T18>(18);
			var t19A = it.Field<T19>(19);
			var t20A = it.Field<T20>(20);
			var t21A = it.Field<T21>(21);
			var t22A = it.Field<T22>(22);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i], ref t22A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct where T20 : struct where T21 : struct where T22 : struct where T23 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();
		With<T18>();
		With<T19>();
		With<T20>();
		With<T21>();
		With<T22>();
		With<T23>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);
			var t10A = it.Field<T10>(10);
			var t11A = it.Field<T11>(11);
			var t12A = it.Field<T12>(12);
			var t13A = it.Field<T13>(13);
			var t14A = it.Field<T14>(14);
			var t15A = it.Field<T15>(15);
			var t16A = it.Field<T16>(16);
			var t17A = it.Field<T17>(17);
			var t18A = it.Field<T18>(18);
			var t19A = it.Field<T19>(19);
			var t20A = it.Field<T20>(20);
			var t21A = it.Field<T21>(21);
			var t22A = it.Field<T22>(22);
			var t23A = it.Field<T23>(23);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i], ref t22A[i], ref t23A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(QueryTemplateWithEntity<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> fn) where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct where T16 : struct where T17 : struct where T18 : struct where T19 : struct where T20 : struct where T21 : struct where T22 : struct where T23 : struct where T24 : struct
	{
		With<T0>();
		With<T1>();
		With<T2>();
		With<T3>();
		With<T4>();
		With<T5>();
		With<T6>();
		With<T7>();
		With<T8>();
		With<T9>();
		With<T10>();
		With<T11>();
		With<T12>();
		With<T13>();
		With<T14>();
		With<T15>();
		With<T16>();
		With<T17>();
		With<T18>();
		With<T19>();
		With<T20>();
		With<T21>();
		With<T22>();
		With<T23>();
		With<T24>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);
			var t2A = it.Field<T2>(2);
			var t3A = it.Field<T3>(3);
			var t4A = it.Field<T4>(4);
			var t5A = it.Field<T5>(5);
			var t6A = it.Field<T6>(6);
			var t7A = it.Field<T7>(7);
			var t8A = it.Field<T8>(8);
			var t9A = it.Field<T9>(9);
			var t10A = it.Field<T10>(10);
			var t11A = it.Field<T11>(11);
			var t12A = it.Field<T12>(12);
			var t13A = it.Field<T13>(13);
			var t14A = it.Field<T14>(14);
			var t15A = it.Field<T15>(15);
			var t16A = it.Field<T16>(16);
			var t17A = it.Field<T17>(17);
			var t18A = it.Field<T18>(18);
			var t19A = it.Field<T19>(19);
			var t20A = it.Field<T20>(20);
			var t21A = it.Field<T21>(21);
			var t22A = it.Field<T22>(22);
			var t23A = it.Field<T23>(23);
			var t24A = it.Field<T24>(24);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(in it.Entity(i), ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i], ref t22A[i], ref t23A[i], ref t24A[i]);
			}
		});
	}

}

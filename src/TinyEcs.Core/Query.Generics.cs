namespace TinyEcs;
partial struct Query
{
	public delegate void QueryTemplate<T0>(ref T0 t0);
	public delegate void QueryTemplate<T0, T1>(ref T0 t0, ref T1 t1);
	public delegate void QueryTemplate<T0, T1, T2>(ref T0 t0, ref T1 t1, ref T2 t2);
	public delegate void QueryTemplate<T0, T1, T2, T3>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17, ref T18 t18);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17, ref T18 t18, ref T19 t19);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17, ref T18 t18, ref T19 t19, ref T20 t20);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17, ref T18 t18, ref T19 t19, ref T20 t20, ref T21 t21);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17, ref T18 t18, ref T19 t19, ref T20 t20, ref T21 t21, ref T22 t22);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17, ref T18 t18, ref T19 t19, ref T20 t20, ref T21 t21, ref T22 t22, ref T23 t23);
	public delegate void QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7, ref T8 t8, ref T9 t9, ref T10 t10, ref T11 t11, ref T12 t12, ref T13 t13, ref T14 t14, ref T15 t15, ref T16 t16, ref T17 t17, ref T18 t18, ref T19 t19, ref T20 t20, ref T21 t21, ref T22 t22, ref T23 t23, ref T24 t24);



	public void System<TPhase, T0>(QueryTemplate<T0> fn)
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
					fn(ref t0A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1>(QueryTemplate<T0, T1> fn)
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
					fn(ref t0A[i], ref t1A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2>(QueryTemplate<T0, T1, T2> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3>(QueryTemplate<T0, T1, T2, T3> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4>(QueryTemplate<T0, T1, T2, T3, T4> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5>(QueryTemplate<T0, T1, T2, T3, T4, T5> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i], ref t22A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i], ref t22A[i], ref t23A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}

	public void System<TPhase, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> fn)
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
					fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i], ref t22A[i], ref t23A[i], ref t24A[i]);
				}
			}, query, terms, float.NaN))
			.Set<TPhase>();
	}



	public void Iterator<T0>(QueryTemplate<T0> fn)
	{
		With<T0>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(ref t0A[i]);
			}
		});
	}

	public void Iterator<T0, T1>(QueryTemplate<T0, T1> fn)
	{
		With<T0>();
		With<T1>();


		_world.Query(Terms, (ref Iterator it) =>
		{
			var t0A = it.Field<T0>(0);
			var t1A = it.Field<T1>(1);

			for (int i = 0; i < it.Count; ++i)
			{
				fn(ref t0A[i], ref t1A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2>(QueryTemplate<T0, T1, T2> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3>(QueryTemplate<T0, T1, T2, T3> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4>(QueryTemplate<T0, T1, T2, T3, T4> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5>(QueryTemplate<T0, T1, T2, T3, T4, T5> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i], ref t22A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i], ref t22A[i], ref t23A[i]);
			}
		});
	}

	public void Iterator<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(QueryTemplate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> fn)
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
				fn(ref t0A[i], ref t1A[i], ref t2A[i], ref t3A[i], ref t4A[i], ref t5A[i], ref t6A[i], ref t7A[i], ref t8A[i], ref t9A[i], ref t10A[i], ref t11A[i], ref t12A[i], ref t13A[i], ref t14A[i], ref t15A[i], ref t16A[i], ref t17A[i], ref t18A[i], ref t19A[i], ref t20A[i], ref t21A[i], ref t22A[i], ref t23A[i], ref t24A[i]);
			}
		});
	}

}

#pragma warning disable 1591
#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace TinyEcs.Bevy
{
#if NET9_0_OR_GREATER
    [SkipLocalsInit]
    public unsafe ref struct Data<T0> : IData<Data<T0>>, IQueryComponentAccess
        where T0 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0> CreateIterator(QueryIterator iterator)
            => new Data<T0>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0)
        {
            ptr0 = _current0.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0> GetEnumerator() => this;
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1> : IData<Data<T0, T1>>, IQueryComponentAccess
        where T0 : struct where T1 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;
		private DataRow<T1> _current1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0, T1> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1)
        {
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
				_current1 = _iterator.GetColumn<T1>(1);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
				_current1.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1> GetEnumerator() => this;
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2> : IData<Data<T0, T1, T2>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;
		private DataRow<T1> _current1;
		private DataRow<T2> _current2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0, T1, T2> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2)
        {
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
				_current1 = _iterator.GetColumn<T1>(1);
				_current2 = _iterator.GetColumn<T2>(2);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
				_current1.Next();
				_current2.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2> GetEnumerator() => this;
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3> : IData<Data<T0, T1, T2, T3>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;
		private DataRow<T1> _current1;
		private DataRow<T2> _current2;
		private DataRow<T3> _current3;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
			builder.With<T3>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0, T1, T2, T3> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3)
        {
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
				_current1 = _iterator.GetColumn<T1>(1);
				_current2 = _iterator.GetColumn<T2>(2);
				_current3 = _iterator.GetColumn<T3>(3);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
				_current1.Next();
				_current2.Next();
				_current3.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3> GetEnumerator() => this;
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4> : IData<Data<T0, T1, T2, T3, T4>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;
		private DataRow<T1> _current1;
		private DataRow<T2> _current2;
		private DataRow<T3> _current3;
		private DataRow<T4> _current4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
			builder.With<T3>();
			builder.With<T4>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4)
        {
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
				_current1 = _iterator.GetColumn<T1>(1);
				_current2 = _iterator.GetColumn<T2>(2);
				_current3 = _iterator.GetColumn<T3>(3);
				_current4 = _iterator.GetColumn<T4>(4);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
				_current1.Next();
				_current2.Next();
				_current3.Next();
				_current4.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4> GetEnumerator() => this;
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5> : IData<Data<T0, T1, T2, T3, T4, T5>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;
		private DataRow<T1> _current1;
		private DataRow<T2> _current2;
		private DataRow<T3> _current3;
		private DataRow<T4> _current4;
		private DataRow<T5> _current5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
			builder.With<T3>();
			builder.With<T4>();
			builder.With<T5>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4, T5> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4, T5>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4, T5> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5)
        {
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
				_current1 = _iterator.GetColumn<T1>(1);
				_current2 = _iterator.GetColumn<T2>(2);
				_current3 = _iterator.GetColumn<T3>(3);
				_current4 = _iterator.GetColumn<T4>(4);
				_current5 = _iterator.GetColumn<T5>(5);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
				_current1.Next();
				_current2.Next();
				_current3.Next();
				_current4.Next();
				_current5.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5> GetEnumerator() => this;
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6> : IData<Data<T0, T1, T2, T3, T4, T5, T6>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;
		private DataRow<T1> _current1;
		private DataRow<T2> _current2;
		private DataRow<T3> _current3;
		private DataRow<T4> _current4;
		private DataRow<T5> _current5;
		private DataRow<T6> _current6;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
			builder.With<T3>();
			builder.With<T4>();
			builder.With<T5>();
			builder.With<T6>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4, T5, T6> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4, T5, T6>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4, T5, T6> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6)
        {
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
				_current1 = _iterator.GetColumn<T1>(1);
				_current2 = _iterator.GetColumn<T2>(2);
				_current3 = _iterator.GetColumn<T3>(3);
				_current4 = _iterator.GetColumn<T4>(4);
				_current5 = _iterator.GetColumn<T5>(5);
				_current6 = _iterator.GetColumn<T6>(6);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
				_current1.Next();
				_current2.Next();
				_current3.Next();
				_current4.Next();
				_current5.Next();
				_current6.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5, T6> GetEnumerator() => this;
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;
		private DataRow<T1> _current1;
		private DataRow<T2> _current2;
		private DataRow<T3> _current3;
		private DataRow<T4> _current4;
		private DataRow<T5> _current5;
		private DataRow<T6> _current6;
		private DataRow<T7> _current7;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
			builder.With<T3>();
			builder.With<T4>();
			builder.With<T5>();
			builder.With<T6>();
			builder.With<T7>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4, T5, T6, T7> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4, T5, T6, T7>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4, T5, T6, T7> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7)
        {
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
				_current1 = _iterator.GetColumn<T1>(1);
				_current2 = _iterator.GetColumn<T2>(2);
				_current3 = _iterator.GetColumn<T3>(3);
				_current4 = _iterator.GetColumn<T4>(4);
				_current5 = _iterator.GetColumn<T5>(5);
				_current6 = _iterator.GetColumn<T6>(6);
				_current7 = _iterator.GetColumn<T7>(7);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
				_current1.Next();
				_current2.Next();
				_current3.Next();
				_current4.Next();
				_current5.Next();
				_current6.Next();
				_current7.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5, T6, T7> GetEnumerator() => this;
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;
		private DataRow<T1> _current1;
		private DataRow<T2> _current2;
		private DataRow<T3> _current3;
		private DataRow<T4> _current4;
		private DataRow<T5> _current5;
		private DataRow<T6> _current6;
		private DataRow<T7> _current7;
		private DataRow<T8> _current8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
			builder.With<T3>();
			builder.With<T4>();
			builder.With<T5>();
			builder.With<T6>();
			builder.With<T7>();
			builder.With<T8>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4, T5, T6, T7, T8> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4, T5, T6, T7, T8>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8)
        {
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
				_current1 = _iterator.GetColumn<T1>(1);
				_current2 = _iterator.GetColumn<T2>(2);
				_current3 = _iterator.GetColumn<T3>(3);
				_current4 = _iterator.GetColumn<T4>(4);
				_current5 = _iterator.GetColumn<T5>(5);
				_current6 = _iterator.GetColumn<T6>(6);
				_current7 = _iterator.GetColumn<T7>(7);
				_current8 = _iterator.GetColumn<T8>(8);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
				_current1.Next();
				_current2.Next();
				_current3.Next();
				_current4.Next();
				_current5.Next();
				_current6.Next();
				_current7.Next();
				_current8.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5, T6, T7, T8> GetEnumerator() => this;
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;
		private DataRow<T1> _current1;
		private DataRow<T2> _current2;
		private DataRow<T3> _current3;
		private DataRow<T4> _current4;
		private DataRow<T5> _current5;
		private DataRow<T6> _current6;
		private DataRow<T7> _current7;
		private DataRow<T8> _current8;
		private DataRow<T9> _current9;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
			builder.With<T3>();
			builder.With<T4>();
			builder.With<T5>();
			builder.With<T6>();
			builder.With<T7>();
			builder.With<T8>();
			builder.With<T9>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8, out Ptr<T9> ptr9)
        {
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
			ptr9 = _current9.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8, out Ptr<T9> ptr9)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
			ptr9 = _current9.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
				_current1 = _iterator.GetColumn<T1>(1);
				_current2 = _iterator.GetColumn<T2>(2);
				_current3 = _iterator.GetColumn<T3>(3);
				_current4 = _iterator.GetColumn<T4>(4);
				_current5 = _iterator.GetColumn<T5>(5);
				_current6 = _iterator.GetColumn<T6>(6);
				_current7 = _iterator.GetColumn<T7>(7);
				_current8 = _iterator.GetColumn<T8>(8);
				_current9 = _iterator.GetColumn<T9>(9);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
				_current1.Next();
				_current2.Next();
				_current3.Next();
				_current4.Next();
				_current5.Next();
				_current6.Next();
				_current7.Next();
				_current8.Next();
				_current9.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> GetEnumerator() => this;
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;
		private DataRow<T1> _current1;
		private DataRow<T2> _current2;
		private DataRow<T3> _current3;
		private DataRow<T4> _current4;
		private DataRow<T5> _current5;
		private DataRow<T6> _current6;
		private DataRow<T7> _current7;
		private DataRow<T8> _current8;
		private DataRow<T9> _current9;
		private DataRow<T10> _current10;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
			builder.With<T3>();
			builder.With<T4>();
			builder.With<T5>();
			builder.With<T6>();
			builder.With<T7>();
			builder.With<T8>();
			builder.With<T9>();
			builder.With<T10>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8, out Ptr<T9> ptr9, out Ptr<T10> ptr10)
        {
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
			ptr9 = _current9.Value;
			ptr10 = _current10.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8, out Ptr<T9> ptr9, out Ptr<T10> ptr10)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
			ptr9 = _current9.Value;
			ptr10 = _current10.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
				_current1 = _iterator.GetColumn<T1>(1);
				_current2 = _iterator.GetColumn<T2>(2);
				_current3 = _iterator.GetColumn<T3>(3);
				_current4 = _iterator.GetColumn<T4>(4);
				_current5 = _iterator.GetColumn<T5>(5);
				_current6 = _iterator.GetColumn<T6>(6);
				_current7 = _iterator.GetColumn<T7>(7);
				_current8 = _iterator.GetColumn<T8>(8);
				_current9 = _iterator.GetColumn<T9>(9);
				_current10 = _iterator.GetColumn<T10>(10);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
				_current1.Next();
				_current2.Next();
				_current3.Next();
				_current4.Next();
				_current5.Next();
				_current6.Next();
				_current7.Next();
				_current8.Next();
				_current9.Next();
				_current10.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> GetEnumerator() => this;
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;
		private DataRow<T1> _current1;
		private DataRow<T2> _current2;
		private DataRow<T3> _current3;
		private DataRow<T4> _current4;
		private DataRow<T5> _current5;
		private DataRow<T6> _current6;
		private DataRow<T7> _current7;
		private DataRow<T8> _current8;
		private DataRow<T9> _current9;
		private DataRow<T10> _current10;
		private DataRow<T11> _current11;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
			builder.With<T3>();
			builder.With<T4>();
			builder.With<T5>();
			builder.With<T6>();
			builder.With<T7>();
			builder.With<T8>();
			builder.With<T9>();
			builder.With<T10>();
			builder.With<T11>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8, out Ptr<T9> ptr9, out Ptr<T10> ptr10, out Ptr<T11> ptr11)
        {
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
			ptr9 = _current9.Value;
			ptr10 = _current10.Value;
			ptr11 = _current11.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8, out Ptr<T9> ptr9, out Ptr<T10> ptr10, out Ptr<T11> ptr11)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
			ptr9 = _current9.Value;
			ptr10 = _current10.Value;
			ptr11 = _current11.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
				_current1 = _iterator.GetColumn<T1>(1);
				_current2 = _iterator.GetColumn<T2>(2);
				_current3 = _iterator.GetColumn<T3>(3);
				_current4 = _iterator.GetColumn<T4>(4);
				_current5 = _iterator.GetColumn<T5>(5);
				_current6 = _iterator.GetColumn<T6>(6);
				_current7 = _iterator.GetColumn<T7>(7);
				_current8 = _iterator.GetColumn<T8>(8);
				_current9 = _iterator.GetColumn<T9>(9);
				_current10 = _iterator.GetColumn<T10>(10);
				_current11 = _iterator.GetColumn<T11>(11);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
				_current1.Next();
				_current2.Next();
				_current3.Next();
				_current4.Next();
				_current5.Next();
				_current6.Next();
				_current7.Next();
				_current8.Next();
				_current9.Next();
				_current10.Next();
				_current11.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> GetEnumerator() => this;
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;
		private DataRow<T1> _current1;
		private DataRow<T2> _current2;
		private DataRow<T3> _current3;
		private DataRow<T4> _current4;
		private DataRow<T5> _current5;
		private DataRow<T6> _current6;
		private DataRow<T7> _current7;
		private DataRow<T8> _current8;
		private DataRow<T9> _current9;
		private DataRow<T10> _current10;
		private DataRow<T11> _current11;
		private DataRow<T12> _current12;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
			builder.With<T3>();
			builder.With<T4>();
			builder.With<T5>();
			builder.With<T6>();
			builder.With<T7>();
			builder.With<T8>();
			builder.With<T9>();
			builder.With<T10>();
			builder.With<T11>();
			builder.With<T12>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8, out Ptr<T9> ptr9, out Ptr<T10> ptr10, out Ptr<T11> ptr11, out Ptr<T12> ptr12)
        {
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
			ptr9 = _current9.Value;
			ptr10 = _current10.Value;
			ptr11 = _current11.Value;
			ptr12 = _current12.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8, out Ptr<T9> ptr9, out Ptr<T10> ptr10, out Ptr<T11> ptr11, out Ptr<T12> ptr12)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
			ptr9 = _current9.Value;
			ptr10 = _current10.Value;
			ptr11 = _current11.Value;
			ptr12 = _current12.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
				_current1 = _iterator.GetColumn<T1>(1);
				_current2 = _iterator.GetColumn<T2>(2);
				_current3 = _iterator.GetColumn<T3>(3);
				_current4 = _iterator.GetColumn<T4>(4);
				_current5 = _iterator.GetColumn<T5>(5);
				_current6 = _iterator.GetColumn<T6>(6);
				_current7 = _iterator.GetColumn<T7>(7);
				_current8 = _iterator.GetColumn<T8>(8);
				_current9 = _iterator.GetColumn<T9>(9);
				_current10 = _iterator.GetColumn<T10>(10);
				_current11 = _iterator.GetColumn<T11>(11);
				_current12 = _iterator.GetColumn<T12>(12);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
				_current1.Next();
				_current2.Next();
				_current3.Next();
				_current4.Next();
				_current5.Next();
				_current6.Next();
				_current7.Next();
				_current8.Next();
				_current9.Next();
				_current10.Next();
				_current11.Next();
				_current12.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> GetEnumerator() => this;
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;
		private DataRow<T1> _current1;
		private DataRow<T2> _current2;
		private DataRow<T3> _current3;
		private DataRow<T4> _current4;
		private DataRow<T5> _current5;
		private DataRow<T6> _current6;
		private DataRow<T7> _current7;
		private DataRow<T8> _current8;
		private DataRow<T9> _current9;
		private DataRow<T10> _current10;
		private DataRow<T11> _current11;
		private DataRow<T12> _current12;
		private DataRow<T13> _current13;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
			builder.With<T3>();
			builder.With<T4>();
			builder.With<T5>();
			builder.With<T6>();
			builder.With<T7>();
			builder.With<T8>();
			builder.With<T9>();
			builder.With<T10>();
			builder.With<T11>();
			builder.With<T12>();
			builder.With<T13>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8, out Ptr<T9> ptr9, out Ptr<T10> ptr10, out Ptr<T11> ptr11, out Ptr<T12> ptr12, out Ptr<T13> ptr13)
        {
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
			ptr9 = _current9.Value;
			ptr10 = _current10.Value;
			ptr11 = _current11.Value;
			ptr12 = _current12.Value;
			ptr13 = _current13.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8, out Ptr<T9> ptr9, out Ptr<T10> ptr10, out Ptr<T11> ptr11, out Ptr<T12> ptr12, out Ptr<T13> ptr13)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
			ptr9 = _current9.Value;
			ptr10 = _current10.Value;
			ptr11 = _current11.Value;
			ptr12 = _current12.Value;
			ptr13 = _current13.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
				_current1 = _iterator.GetColumn<T1>(1);
				_current2 = _iterator.GetColumn<T2>(2);
				_current3 = _iterator.GetColumn<T3>(3);
				_current4 = _iterator.GetColumn<T4>(4);
				_current5 = _iterator.GetColumn<T5>(5);
				_current6 = _iterator.GetColumn<T6>(6);
				_current7 = _iterator.GetColumn<T7>(7);
				_current8 = _iterator.GetColumn<T8>(8);
				_current9 = _iterator.GetColumn<T9>(9);
				_current10 = _iterator.GetColumn<T10>(10);
				_current11 = _iterator.GetColumn<T11>(11);
				_current12 = _iterator.GetColumn<T12>(12);
				_current13 = _iterator.GetColumn<T13>(13);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
				_current1.Next();
				_current2.Next();
				_current3.Next();
				_current4.Next();
				_current5.Next();
				_current6.Next();
				_current7.Next();
				_current8.Next();
				_current9.Next();
				_current10.Next();
				_current11.Next();
				_current12.Next();
				_current13.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> GetEnumerator() => this;
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;
		private DataRow<T1> _current1;
		private DataRow<T2> _current2;
		private DataRow<T3> _current3;
		private DataRow<T4> _current4;
		private DataRow<T5> _current5;
		private DataRow<T6> _current6;
		private DataRow<T7> _current7;
		private DataRow<T8> _current8;
		private DataRow<T9> _current9;
		private DataRow<T10> _current10;
		private DataRow<T11> _current11;
		private DataRow<T12> _current12;
		private DataRow<T13> _current13;
		private DataRow<T14> _current14;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
			builder.With<T3>();
			builder.With<T4>();
			builder.With<T5>();
			builder.With<T6>();
			builder.With<T7>();
			builder.With<T8>();
			builder.With<T9>();
			builder.With<T10>();
			builder.With<T11>();
			builder.With<T12>();
			builder.With<T13>();
			builder.With<T14>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8, out Ptr<T9> ptr9, out Ptr<T10> ptr10, out Ptr<T11> ptr11, out Ptr<T12> ptr12, out Ptr<T13> ptr13, out Ptr<T14> ptr14)
        {
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
			ptr9 = _current9.Value;
			ptr10 = _current10.Value;
			ptr11 = _current11.Value;
			ptr12 = _current12.Value;
			ptr13 = _current13.Value;
			ptr14 = _current14.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8, out Ptr<T9> ptr9, out Ptr<T10> ptr10, out Ptr<T11> ptr11, out Ptr<T12> ptr12, out Ptr<T13> ptr13, out Ptr<T14> ptr14)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
			ptr9 = _current9.Value;
			ptr10 = _current10.Value;
			ptr11 = _current11.Value;
			ptr12 = _current12.Value;
			ptr13 = _current13.Value;
			ptr14 = _current14.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
				_current1 = _iterator.GetColumn<T1>(1);
				_current2 = _iterator.GetColumn<T2>(2);
				_current3 = _iterator.GetColumn<T3>(3);
				_current4 = _iterator.GetColumn<T4>(4);
				_current5 = _iterator.GetColumn<T5>(5);
				_current6 = _iterator.GetColumn<T6>(6);
				_current7 = _iterator.GetColumn<T7>(7);
				_current8 = _iterator.GetColumn<T8>(8);
				_current9 = _iterator.GetColumn<T9>(9);
				_current10 = _iterator.GetColumn<T10>(10);
				_current11 = _iterator.GetColumn<T11>(11);
				_current12 = _iterator.GetColumn<T12>(12);
				_current13 = _iterator.GetColumn<T13>(13);
				_current14 = _iterator.GetColumn<T14>(14);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
				_current1.Next();
				_current2.Next();
				_current3.Next();
				_current4.Next();
				_current5.Next();
				_current6.Next();
				_current7.Next();
				_current8.Next();
				_current9.Next();
				_current10.Next();
				_current11.Next();
				_current12.Next();
				_current13.Next();
				_current14.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> GetEnumerator() => this;
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct
    {
		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        private QueryIterator _iterator;
        private int _index, _count;
        private ReadOnlySpan<EntityView> _entities;
        private DataRow<T0> _current0;
		private DataRow<T1> _current1;
		private DataRow<T2> _current2;
		private DataRow<T3> _current3;
		private DataRow<T4> _current4;
		private DataRow<T5> _current5;
		private DataRow<T6> _current6;
		private DataRow<T7> _current7;
		private DataRow<T8> _current8;
		private DataRow<T9> _current9;
		private DataRow<T10> _current10;
		private DataRow<T11> _current11;
		private DataRow<T12> _current12;
		private DataRow<T13> _current13;
		private DataRow<T14> _current14;
		private DataRow<T15> _current15;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Data(QueryIterator queryIterator)
        {
            _iterator = queryIterator;
            _index = -1;
            _count = -1;
        }

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
			builder.With<T3>();
			builder.With<T4>();
			builder.With<T5>();
			builder.With<T6>();
			builder.With<T7>();
			builder.With<T8>();
			builder.With<T9>();
			builder.With<T10>();
			builder.With<T11>();
			builder.With<T12>();
			builder.With<T13>();
			builder.With<T14>();
			builder.With<T15>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> CreateIterator(QueryIterator iterator)
            => new Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(iterator);

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        public ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8, out Ptr<T9> ptr9, out Ptr<T10> ptr10, out Ptr<T11> ptr11, out Ptr<T12> ptr12, out Ptr<T13> ptr13, out Ptr<T14> ptr14, out Ptr<T15> ptr15)
        {
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
			ptr9 = _current9.Value;
			ptr10 = _current10.Value;
			ptr11 = _current11.Value;
			ptr12 = _current12.Value;
			ptr13 = _current13.Value;
			ptr14 = _current14.Value;
			ptr15 = _current15.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out PtrRO<ulong> entity, out Ptr<T0> ptr0, out Ptr<T1> ptr1, out Ptr<T2> ptr2, out Ptr<T3> ptr3, out Ptr<T4> ptr4, out Ptr<T5> ptr5, out Ptr<T6> ptr6, out Ptr<T7> ptr7, out Ptr<T8> ptr8, out Ptr<T9> ptr9, out Ptr<T10> ptr10, out Ptr<T11> ptr11, out Ptr<T12> ptr12, out Ptr<T13> ptr13, out Ptr<T14> ptr14, out Ptr<T15> ptr15)
        {
            entity = new (in _entities[_index].ID);
            ptr0 = _current0.Value;
			ptr1 = _current1.Value;
			ptr2 = _current2.Value;
			ptr3 = _current3.Value;
			ptr4 = _current4.Value;
			ptr5 = _current5.Value;
			ptr6 = _current6.Value;
			ptr7 = _current7.Value;
			ptr8 = _current8.Value;
			ptr9 = _current9.Value;
			ptr10 = _current10.Value;
			ptr11 = _current11.Value;
			ptr12 = _current12.Value;
			ptr13 = _current13.Value;
			ptr14 = _current14.Value;
			ptr15 = _current15.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (++_index >= _count)
            {
                if (!_iterator.Next())
                    return false;

                _current0 = _iterator.GetColumn<T0>(0);
				_current1 = _iterator.GetColumn<T1>(1);
				_current2 = _iterator.GetColumn<T2>(2);
				_current3 = _iterator.GetColumn<T3>(3);
				_current4 = _iterator.GetColumn<T4>(4);
				_current5 = _iterator.GetColumn<T5>(5);
				_current6 = _iterator.GetColumn<T6>(6);
				_current7 = _iterator.GetColumn<T7>(7);
				_current8 = _iterator.GetColumn<T8>(8);
				_current9 = _iterator.GetColumn<T9>(9);
				_current10 = _iterator.GetColumn<T10>(10);
				_current11 = _iterator.GetColumn<T11>(11);
				_current12 = _iterator.GetColumn<T12>(12);
				_current13 = _iterator.GetColumn<T13>(13);
				_current14 = _iterator.GetColumn<T14>(14);
				_current15 = _iterator.GetColumn<T15>(15);
                _entities = _iterator.Entities();

                _index = 0;
                _count = _iterator.Count;
            }
            else
            {
                _current0.Next();
				_current1.Next();
				_current2.Next();
				_current3.Next();
				_current4.Next();
				_current5.Next();
				_current6.Next();
				_current7.Next();
				_current8.Next();
				_current9.Next();
				_current10.Next();
				_current11.Next();
				_current12.Next();
				_current13.Next();
				_current14.Next();
				_current15.Next();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> GetEnumerator() => this;
    }

#endif
}

#pragma warning restore 1591

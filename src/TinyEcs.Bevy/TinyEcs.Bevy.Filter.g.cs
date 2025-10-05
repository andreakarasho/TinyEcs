#pragma warning disable 1591
#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace TinyEcs.Bevy
{
#if NET9_0_OR_GREATER
    public ref struct Filter<T0> : IFilter<Filter<T0>>
        where T0 : struct, IFilter<T0>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0> IQueryIterator<Filter<T0>>.Current => ref this;

        static Filter<T0> IFilter<Filter<T0>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0> IQueryIterator<Filter<T0>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
            return i0;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
        }
    }

    public ref struct Filter<T0, T1> : IFilter<Filter<T0, T1>>
        where T0 : struct, IFilter<T0>, allows ref struct
		where T1 : struct, IFilter<T1>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;
		private T1 _iter1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
			_iter1 = T1.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
			T1.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0, T1> IQueryIterator<Filter<T0, T1>>.Current => ref this;

        static Filter<T0, T1> IFilter<Filter<T0, T1>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0, T1> IQueryIterator<Filter<T0, T1>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0, T1>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
			var i1 = _iter1.MoveNext();
            return i0 && i1;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
			_iter1.SetTicks(lastRun, thisRun);
        }
    }

    public ref struct Filter<T0, T1, T2> : IFilter<Filter<T0, T1, T2>>
        where T0 : struct, IFilter<T0>, allows ref struct
		where T1 : struct, IFilter<T1>, allows ref struct
		where T2 : struct, IFilter<T2>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;
		private T1 _iter1;
		private T2 _iter2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
			_iter1 = T1.CreateIterator(iterator);
			_iter2 = T2.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
			T1.Build(builder);
			T2.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0, T1, T2> IQueryIterator<Filter<T0, T1, T2>>.Current => ref this;

        static Filter<T0, T1, T2> IFilter<Filter<T0, T1, T2>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0, T1, T2> IQueryIterator<Filter<T0, T1, T2>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0, T1, T2>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
			var i1 = _iter1.MoveNext();
			var i2 = _iter2.MoveNext();
            return i0 && i1 && i2;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
			_iter1.SetTicks(lastRun, thisRun);
			_iter2.SetTicks(lastRun, thisRun);
        }
    }

    public ref struct Filter<T0, T1, T2, T3> : IFilter<Filter<T0, T1, T2, T3>>
        where T0 : struct, IFilter<T0>, allows ref struct
		where T1 : struct, IFilter<T1>, allows ref struct
		where T2 : struct, IFilter<T2>, allows ref struct
		where T3 : struct, IFilter<T3>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;
		private T1 _iter1;
		private T2 _iter2;
		private T3 _iter3;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
			_iter1 = T1.CreateIterator(iterator);
			_iter2 = T2.CreateIterator(iterator);
			_iter3 = T3.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
			T1.Build(builder);
			T2.Build(builder);
			T3.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0, T1, T2, T3> IQueryIterator<Filter<T0, T1, T2, T3>>.Current => ref this;

        static Filter<T0, T1, T2, T3> IFilter<Filter<T0, T1, T2, T3>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0, T1, T2, T3> IQueryIterator<Filter<T0, T1, T2, T3>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0, T1, T2, T3>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
			var i1 = _iter1.MoveNext();
			var i2 = _iter2.MoveNext();
			var i3 = _iter3.MoveNext();
            return i0 && i1 && i2 && i3;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
			_iter1.SetTicks(lastRun, thisRun);
			_iter2.SetTicks(lastRun, thisRun);
			_iter3.SetTicks(lastRun, thisRun);
        }
    }

    public ref struct Filter<T0, T1, T2, T3, T4> : IFilter<Filter<T0, T1, T2, T3, T4>>
        where T0 : struct, IFilter<T0>, allows ref struct
		where T1 : struct, IFilter<T1>, allows ref struct
		where T2 : struct, IFilter<T2>, allows ref struct
		where T3 : struct, IFilter<T3>, allows ref struct
		where T4 : struct, IFilter<T4>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;
		private T1 _iter1;
		private T2 _iter2;
		private T3 _iter3;
		private T4 _iter4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
			_iter1 = T1.CreateIterator(iterator);
			_iter2 = T2.CreateIterator(iterator);
			_iter3 = T3.CreateIterator(iterator);
			_iter4 = T4.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
			T1.Build(builder);
			T2.Build(builder);
			T3.Build(builder);
			T4.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0, T1, T2, T3, T4> IQueryIterator<Filter<T0, T1, T2, T3, T4>>.Current => ref this;

        static Filter<T0, T1, T2, T3, T4> IFilter<Filter<T0, T1, T2, T3, T4>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0, T1, T2, T3, T4> IQueryIterator<Filter<T0, T1, T2, T3, T4>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0, T1, T2, T3, T4>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
			var i1 = _iter1.MoveNext();
			var i2 = _iter2.MoveNext();
			var i3 = _iter3.MoveNext();
			var i4 = _iter4.MoveNext();
            return i0 && i1 && i2 && i3 && i4;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
			_iter1.SetTicks(lastRun, thisRun);
			_iter2.SetTicks(lastRun, thisRun);
			_iter3.SetTicks(lastRun, thisRun);
			_iter4.SetTicks(lastRun, thisRun);
        }
    }

    public ref struct Filter<T0, T1, T2, T3, T4, T5> : IFilter<Filter<T0, T1, T2, T3, T4, T5>>
        where T0 : struct, IFilter<T0>, allows ref struct
		where T1 : struct, IFilter<T1>, allows ref struct
		where T2 : struct, IFilter<T2>, allows ref struct
		where T3 : struct, IFilter<T3>, allows ref struct
		where T4 : struct, IFilter<T4>, allows ref struct
		where T5 : struct, IFilter<T5>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;
		private T1 _iter1;
		private T2 _iter2;
		private T3 _iter3;
		private T4 _iter4;
		private T5 _iter5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
			_iter1 = T1.CreateIterator(iterator);
			_iter2 = T2.CreateIterator(iterator);
			_iter3 = T3.CreateIterator(iterator);
			_iter4 = T4.CreateIterator(iterator);
			_iter5 = T5.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
			T1.Build(builder);
			T2.Build(builder);
			T3.Build(builder);
			T4.Build(builder);
			T5.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0, T1, T2, T3, T4, T5> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5>>.Current => ref this;

        static Filter<T0, T1, T2, T3, T4, T5> IFilter<Filter<T0, T1, T2, T3, T4, T5>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0, T1, T2, T3, T4, T5> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0, T1, T2, T3, T4, T5>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
			var i1 = _iter1.MoveNext();
			var i2 = _iter2.MoveNext();
			var i3 = _iter3.MoveNext();
			var i4 = _iter4.MoveNext();
			var i5 = _iter5.MoveNext();
            return i0 && i1 && i2 && i3 && i4 && i5;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
			_iter1.SetTicks(lastRun, thisRun);
			_iter2.SetTicks(lastRun, thisRun);
			_iter3.SetTicks(lastRun, thisRun);
			_iter4.SetTicks(lastRun, thisRun);
			_iter5.SetTicks(lastRun, thisRun);
        }
    }

    public ref struct Filter<T0, T1, T2, T3, T4, T5, T6> : IFilter<Filter<T0, T1, T2, T3, T4, T5, T6>>
        where T0 : struct, IFilter<T0>, allows ref struct
		where T1 : struct, IFilter<T1>, allows ref struct
		where T2 : struct, IFilter<T2>, allows ref struct
		where T3 : struct, IFilter<T3>, allows ref struct
		where T4 : struct, IFilter<T4>, allows ref struct
		where T5 : struct, IFilter<T5>, allows ref struct
		where T6 : struct, IFilter<T6>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;
		private T1 _iter1;
		private T2 _iter2;
		private T3 _iter3;
		private T4 _iter4;
		private T5 _iter5;
		private T6 _iter6;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
			_iter1 = T1.CreateIterator(iterator);
			_iter2 = T2.CreateIterator(iterator);
			_iter3 = T3.CreateIterator(iterator);
			_iter4 = T4.CreateIterator(iterator);
			_iter5 = T5.CreateIterator(iterator);
			_iter6 = T6.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
			T1.Build(builder);
			T2.Build(builder);
			T3.Build(builder);
			T4.Build(builder);
			T5.Build(builder);
			T6.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0, T1, T2, T3, T4, T5, T6> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6>>.Current => ref this;

        static Filter<T0, T1, T2, T3, T4, T5, T6> IFilter<Filter<T0, T1, T2, T3, T4, T5, T6>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0, T1, T2, T3, T4, T5, T6> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
			var i1 = _iter1.MoveNext();
			var i2 = _iter2.MoveNext();
			var i3 = _iter3.MoveNext();
			var i4 = _iter4.MoveNext();
			var i5 = _iter5.MoveNext();
			var i6 = _iter6.MoveNext();
            return i0 && i1 && i2 && i3 && i4 && i5 && i6;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
			_iter1.SetTicks(lastRun, thisRun);
			_iter2.SetTicks(lastRun, thisRun);
			_iter3.SetTicks(lastRun, thisRun);
			_iter4.SetTicks(lastRun, thisRun);
			_iter5.SetTicks(lastRun, thisRun);
			_iter6.SetTicks(lastRun, thisRun);
        }
    }

    public ref struct Filter<T0, T1, T2, T3, T4, T5, T6, T7> : IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7>>
        where T0 : struct, IFilter<T0>, allows ref struct
		where T1 : struct, IFilter<T1>, allows ref struct
		where T2 : struct, IFilter<T2>, allows ref struct
		where T3 : struct, IFilter<T3>, allows ref struct
		where T4 : struct, IFilter<T4>, allows ref struct
		where T5 : struct, IFilter<T5>, allows ref struct
		where T6 : struct, IFilter<T6>, allows ref struct
		where T7 : struct, IFilter<T7>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;
		private T1 _iter1;
		private T2 _iter2;
		private T3 _iter3;
		private T4 _iter4;
		private T5 _iter5;
		private T6 _iter6;
		private T7 _iter7;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
			_iter1 = T1.CreateIterator(iterator);
			_iter2 = T2.CreateIterator(iterator);
			_iter3 = T3.CreateIterator(iterator);
			_iter4 = T4.CreateIterator(iterator);
			_iter5 = T5.CreateIterator(iterator);
			_iter6 = T6.CreateIterator(iterator);
			_iter7 = T7.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
			T1.Build(builder);
			T2.Build(builder);
			T3.Build(builder);
			T4.Build(builder);
			T5.Build(builder);
			T6.Build(builder);
			T7.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0, T1, T2, T3, T4, T5, T6, T7> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7>>.Current => ref this;

        static Filter<T0, T1, T2, T3, T4, T5, T6, T7> IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0, T1, T2, T3, T4, T5, T6, T7> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
			var i1 = _iter1.MoveNext();
			var i2 = _iter2.MoveNext();
			var i3 = _iter3.MoveNext();
			var i4 = _iter4.MoveNext();
			var i5 = _iter5.MoveNext();
			var i6 = _iter6.MoveNext();
			var i7 = _iter7.MoveNext();
            return i0 && i1 && i2 && i3 && i4 && i5 && i6 && i7;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
			_iter1.SetTicks(lastRun, thisRun);
			_iter2.SetTicks(lastRun, thisRun);
			_iter3.SetTicks(lastRun, thisRun);
			_iter4.SetTicks(lastRun, thisRun);
			_iter5.SetTicks(lastRun, thisRun);
			_iter6.SetTicks(lastRun, thisRun);
			_iter7.SetTicks(lastRun, thisRun);
        }
    }

    public ref struct Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8> : IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8>>
        where T0 : struct, IFilter<T0>, allows ref struct
		where T1 : struct, IFilter<T1>, allows ref struct
		where T2 : struct, IFilter<T2>, allows ref struct
		where T3 : struct, IFilter<T3>, allows ref struct
		where T4 : struct, IFilter<T4>, allows ref struct
		where T5 : struct, IFilter<T5>, allows ref struct
		where T6 : struct, IFilter<T6>, allows ref struct
		where T7 : struct, IFilter<T7>, allows ref struct
		where T8 : struct, IFilter<T8>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;
		private T1 _iter1;
		private T2 _iter2;
		private T3 _iter3;
		private T4 _iter4;
		private T5 _iter5;
		private T6 _iter6;
		private T7 _iter7;
		private T8 _iter8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
			_iter1 = T1.CreateIterator(iterator);
			_iter2 = T2.CreateIterator(iterator);
			_iter3 = T3.CreateIterator(iterator);
			_iter4 = T4.CreateIterator(iterator);
			_iter5 = T5.CreateIterator(iterator);
			_iter6 = T6.CreateIterator(iterator);
			_iter7 = T7.CreateIterator(iterator);
			_iter8 = T8.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
			T1.Build(builder);
			T2.Build(builder);
			T3.Build(builder);
			T4.Build(builder);
			T5.Build(builder);
			T6.Build(builder);
			T7.Build(builder);
			T8.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8>>.Current => ref this;

        static Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8> IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
			var i1 = _iter1.MoveNext();
			var i2 = _iter2.MoveNext();
			var i3 = _iter3.MoveNext();
			var i4 = _iter4.MoveNext();
			var i5 = _iter5.MoveNext();
			var i6 = _iter6.MoveNext();
			var i7 = _iter7.MoveNext();
			var i8 = _iter8.MoveNext();
            return i0 && i1 && i2 && i3 && i4 && i5 && i6 && i7 && i8;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
			_iter1.SetTicks(lastRun, thisRun);
			_iter2.SetTicks(lastRun, thisRun);
			_iter3.SetTicks(lastRun, thisRun);
			_iter4.SetTicks(lastRun, thisRun);
			_iter5.SetTicks(lastRun, thisRun);
			_iter6.SetTicks(lastRun, thisRun);
			_iter7.SetTicks(lastRun, thisRun);
			_iter8.SetTicks(lastRun, thisRun);
        }
    }

    public ref struct Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>>
        where T0 : struct, IFilter<T0>, allows ref struct
		where T1 : struct, IFilter<T1>, allows ref struct
		where T2 : struct, IFilter<T2>, allows ref struct
		where T3 : struct, IFilter<T3>, allows ref struct
		where T4 : struct, IFilter<T4>, allows ref struct
		where T5 : struct, IFilter<T5>, allows ref struct
		where T6 : struct, IFilter<T6>, allows ref struct
		where T7 : struct, IFilter<T7>, allows ref struct
		where T8 : struct, IFilter<T8>, allows ref struct
		where T9 : struct, IFilter<T9>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;
		private T1 _iter1;
		private T2 _iter2;
		private T3 _iter3;
		private T4 _iter4;
		private T5 _iter5;
		private T6 _iter6;
		private T7 _iter7;
		private T8 _iter8;
		private T9 _iter9;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
			_iter1 = T1.CreateIterator(iterator);
			_iter2 = T2.CreateIterator(iterator);
			_iter3 = T3.CreateIterator(iterator);
			_iter4 = T4.CreateIterator(iterator);
			_iter5 = T5.CreateIterator(iterator);
			_iter6 = T6.CreateIterator(iterator);
			_iter7 = T7.CreateIterator(iterator);
			_iter8 = T8.CreateIterator(iterator);
			_iter9 = T9.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
			T1.Build(builder);
			T2.Build(builder);
			T3.Build(builder);
			T4.Build(builder);
			T5.Build(builder);
			T6.Build(builder);
			T7.Build(builder);
			T8.Build(builder);
			T9.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>>.Current => ref this;

        static Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
			var i1 = _iter1.MoveNext();
			var i2 = _iter2.MoveNext();
			var i3 = _iter3.MoveNext();
			var i4 = _iter4.MoveNext();
			var i5 = _iter5.MoveNext();
			var i6 = _iter6.MoveNext();
			var i7 = _iter7.MoveNext();
			var i8 = _iter8.MoveNext();
			var i9 = _iter9.MoveNext();
            return i0 && i1 && i2 && i3 && i4 && i5 && i6 && i7 && i8 && i9;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
			_iter1.SetTicks(lastRun, thisRun);
			_iter2.SetTicks(lastRun, thisRun);
			_iter3.SetTicks(lastRun, thisRun);
			_iter4.SetTicks(lastRun, thisRun);
			_iter5.SetTicks(lastRun, thisRun);
			_iter6.SetTicks(lastRun, thisRun);
			_iter7.SetTicks(lastRun, thisRun);
			_iter8.SetTicks(lastRun, thisRun);
			_iter9.SetTicks(lastRun, thisRun);
        }
    }

    public ref struct Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>
        where T0 : struct, IFilter<T0>, allows ref struct
		where T1 : struct, IFilter<T1>, allows ref struct
		where T2 : struct, IFilter<T2>, allows ref struct
		where T3 : struct, IFilter<T3>, allows ref struct
		where T4 : struct, IFilter<T4>, allows ref struct
		where T5 : struct, IFilter<T5>, allows ref struct
		where T6 : struct, IFilter<T6>, allows ref struct
		where T7 : struct, IFilter<T7>, allows ref struct
		where T8 : struct, IFilter<T8>, allows ref struct
		where T9 : struct, IFilter<T9>, allows ref struct
		where T10 : struct, IFilter<T10>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;
		private T1 _iter1;
		private T2 _iter2;
		private T3 _iter3;
		private T4 _iter4;
		private T5 _iter5;
		private T6 _iter6;
		private T7 _iter7;
		private T8 _iter8;
		private T9 _iter9;
		private T10 _iter10;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
			_iter1 = T1.CreateIterator(iterator);
			_iter2 = T2.CreateIterator(iterator);
			_iter3 = T3.CreateIterator(iterator);
			_iter4 = T4.CreateIterator(iterator);
			_iter5 = T5.CreateIterator(iterator);
			_iter6 = T6.CreateIterator(iterator);
			_iter7 = T7.CreateIterator(iterator);
			_iter8 = T8.CreateIterator(iterator);
			_iter9 = T9.CreateIterator(iterator);
			_iter10 = T10.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
			T1.Build(builder);
			T2.Build(builder);
			T3.Build(builder);
			T4.Build(builder);
			T5.Build(builder);
			T6.Build(builder);
			T7.Build(builder);
			T8.Build(builder);
			T9.Build(builder);
			T10.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>.Current => ref this;

        static Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
			var i1 = _iter1.MoveNext();
			var i2 = _iter2.MoveNext();
			var i3 = _iter3.MoveNext();
			var i4 = _iter4.MoveNext();
			var i5 = _iter5.MoveNext();
			var i6 = _iter6.MoveNext();
			var i7 = _iter7.MoveNext();
			var i8 = _iter8.MoveNext();
			var i9 = _iter9.MoveNext();
			var i10 = _iter10.MoveNext();
            return i0 && i1 && i2 && i3 && i4 && i5 && i6 && i7 && i8 && i9 && i10;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
			_iter1.SetTicks(lastRun, thisRun);
			_iter2.SetTicks(lastRun, thisRun);
			_iter3.SetTicks(lastRun, thisRun);
			_iter4.SetTicks(lastRun, thisRun);
			_iter5.SetTicks(lastRun, thisRun);
			_iter6.SetTicks(lastRun, thisRun);
			_iter7.SetTicks(lastRun, thisRun);
			_iter8.SetTicks(lastRun, thisRun);
			_iter9.SetTicks(lastRun, thisRun);
			_iter10.SetTicks(lastRun, thisRun);
        }
    }

    public ref struct Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>
        where T0 : struct, IFilter<T0>, allows ref struct
		where T1 : struct, IFilter<T1>, allows ref struct
		where T2 : struct, IFilter<T2>, allows ref struct
		where T3 : struct, IFilter<T3>, allows ref struct
		where T4 : struct, IFilter<T4>, allows ref struct
		where T5 : struct, IFilter<T5>, allows ref struct
		where T6 : struct, IFilter<T6>, allows ref struct
		where T7 : struct, IFilter<T7>, allows ref struct
		where T8 : struct, IFilter<T8>, allows ref struct
		where T9 : struct, IFilter<T9>, allows ref struct
		where T10 : struct, IFilter<T10>, allows ref struct
		where T11 : struct, IFilter<T11>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;
		private T1 _iter1;
		private T2 _iter2;
		private T3 _iter3;
		private T4 _iter4;
		private T5 _iter5;
		private T6 _iter6;
		private T7 _iter7;
		private T8 _iter8;
		private T9 _iter9;
		private T10 _iter10;
		private T11 _iter11;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
			_iter1 = T1.CreateIterator(iterator);
			_iter2 = T2.CreateIterator(iterator);
			_iter3 = T3.CreateIterator(iterator);
			_iter4 = T4.CreateIterator(iterator);
			_iter5 = T5.CreateIterator(iterator);
			_iter6 = T6.CreateIterator(iterator);
			_iter7 = T7.CreateIterator(iterator);
			_iter8 = T8.CreateIterator(iterator);
			_iter9 = T9.CreateIterator(iterator);
			_iter10 = T10.CreateIterator(iterator);
			_iter11 = T11.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
			T1.Build(builder);
			T2.Build(builder);
			T3.Build(builder);
			T4.Build(builder);
			T5.Build(builder);
			T6.Build(builder);
			T7.Build(builder);
			T8.Build(builder);
			T9.Build(builder);
			T10.Build(builder);
			T11.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>.Current => ref this;

        static Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
			var i1 = _iter1.MoveNext();
			var i2 = _iter2.MoveNext();
			var i3 = _iter3.MoveNext();
			var i4 = _iter4.MoveNext();
			var i5 = _iter5.MoveNext();
			var i6 = _iter6.MoveNext();
			var i7 = _iter7.MoveNext();
			var i8 = _iter8.MoveNext();
			var i9 = _iter9.MoveNext();
			var i10 = _iter10.MoveNext();
			var i11 = _iter11.MoveNext();
            return i0 && i1 && i2 && i3 && i4 && i5 && i6 && i7 && i8 && i9 && i10 && i11;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
			_iter1.SetTicks(lastRun, thisRun);
			_iter2.SetTicks(lastRun, thisRun);
			_iter3.SetTicks(lastRun, thisRun);
			_iter4.SetTicks(lastRun, thisRun);
			_iter5.SetTicks(lastRun, thisRun);
			_iter6.SetTicks(lastRun, thisRun);
			_iter7.SetTicks(lastRun, thisRun);
			_iter8.SetTicks(lastRun, thisRun);
			_iter9.SetTicks(lastRun, thisRun);
			_iter10.SetTicks(lastRun, thisRun);
			_iter11.SetTicks(lastRun, thisRun);
        }
    }

    public ref struct Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>
        where T0 : struct, IFilter<T0>, allows ref struct
		where T1 : struct, IFilter<T1>, allows ref struct
		where T2 : struct, IFilter<T2>, allows ref struct
		where T3 : struct, IFilter<T3>, allows ref struct
		where T4 : struct, IFilter<T4>, allows ref struct
		where T5 : struct, IFilter<T5>, allows ref struct
		where T6 : struct, IFilter<T6>, allows ref struct
		where T7 : struct, IFilter<T7>, allows ref struct
		where T8 : struct, IFilter<T8>, allows ref struct
		where T9 : struct, IFilter<T9>, allows ref struct
		where T10 : struct, IFilter<T10>, allows ref struct
		where T11 : struct, IFilter<T11>, allows ref struct
		where T12 : struct, IFilter<T12>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;
		private T1 _iter1;
		private T2 _iter2;
		private T3 _iter3;
		private T4 _iter4;
		private T5 _iter5;
		private T6 _iter6;
		private T7 _iter7;
		private T8 _iter8;
		private T9 _iter9;
		private T10 _iter10;
		private T11 _iter11;
		private T12 _iter12;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
			_iter1 = T1.CreateIterator(iterator);
			_iter2 = T2.CreateIterator(iterator);
			_iter3 = T3.CreateIterator(iterator);
			_iter4 = T4.CreateIterator(iterator);
			_iter5 = T5.CreateIterator(iterator);
			_iter6 = T6.CreateIterator(iterator);
			_iter7 = T7.CreateIterator(iterator);
			_iter8 = T8.CreateIterator(iterator);
			_iter9 = T9.CreateIterator(iterator);
			_iter10 = T10.CreateIterator(iterator);
			_iter11 = T11.CreateIterator(iterator);
			_iter12 = T12.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
			T1.Build(builder);
			T2.Build(builder);
			T3.Build(builder);
			T4.Build(builder);
			T5.Build(builder);
			T6.Build(builder);
			T7.Build(builder);
			T8.Build(builder);
			T9.Build(builder);
			T10.Build(builder);
			T11.Build(builder);
			T12.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>.Current => ref this;

        static Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
			var i1 = _iter1.MoveNext();
			var i2 = _iter2.MoveNext();
			var i3 = _iter3.MoveNext();
			var i4 = _iter4.MoveNext();
			var i5 = _iter5.MoveNext();
			var i6 = _iter6.MoveNext();
			var i7 = _iter7.MoveNext();
			var i8 = _iter8.MoveNext();
			var i9 = _iter9.MoveNext();
			var i10 = _iter10.MoveNext();
			var i11 = _iter11.MoveNext();
			var i12 = _iter12.MoveNext();
            return i0 && i1 && i2 && i3 && i4 && i5 && i6 && i7 && i8 && i9 && i10 && i11 && i12;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
			_iter1.SetTicks(lastRun, thisRun);
			_iter2.SetTicks(lastRun, thisRun);
			_iter3.SetTicks(lastRun, thisRun);
			_iter4.SetTicks(lastRun, thisRun);
			_iter5.SetTicks(lastRun, thisRun);
			_iter6.SetTicks(lastRun, thisRun);
			_iter7.SetTicks(lastRun, thisRun);
			_iter8.SetTicks(lastRun, thisRun);
			_iter9.SetTicks(lastRun, thisRun);
			_iter10.SetTicks(lastRun, thisRun);
			_iter11.SetTicks(lastRun, thisRun);
			_iter12.SetTicks(lastRun, thisRun);
        }
    }

    public ref struct Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>
        where T0 : struct, IFilter<T0>, allows ref struct
		where T1 : struct, IFilter<T1>, allows ref struct
		where T2 : struct, IFilter<T2>, allows ref struct
		where T3 : struct, IFilter<T3>, allows ref struct
		where T4 : struct, IFilter<T4>, allows ref struct
		where T5 : struct, IFilter<T5>, allows ref struct
		where T6 : struct, IFilter<T6>, allows ref struct
		where T7 : struct, IFilter<T7>, allows ref struct
		where T8 : struct, IFilter<T8>, allows ref struct
		where T9 : struct, IFilter<T9>, allows ref struct
		where T10 : struct, IFilter<T10>, allows ref struct
		where T11 : struct, IFilter<T11>, allows ref struct
		where T12 : struct, IFilter<T12>, allows ref struct
		where T13 : struct, IFilter<T13>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;
		private T1 _iter1;
		private T2 _iter2;
		private T3 _iter3;
		private T4 _iter4;
		private T5 _iter5;
		private T6 _iter6;
		private T7 _iter7;
		private T8 _iter8;
		private T9 _iter9;
		private T10 _iter10;
		private T11 _iter11;
		private T12 _iter12;
		private T13 _iter13;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
			_iter1 = T1.CreateIterator(iterator);
			_iter2 = T2.CreateIterator(iterator);
			_iter3 = T3.CreateIterator(iterator);
			_iter4 = T4.CreateIterator(iterator);
			_iter5 = T5.CreateIterator(iterator);
			_iter6 = T6.CreateIterator(iterator);
			_iter7 = T7.CreateIterator(iterator);
			_iter8 = T8.CreateIterator(iterator);
			_iter9 = T9.CreateIterator(iterator);
			_iter10 = T10.CreateIterator(iterator);
			_iter11 = T11.CreateIterator(iterator);
			_iter12 = T12.CreateIterator(iterator);
			_iter13 = T13.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
			T1.Build(builder);
			T2.Build(builder);
			T3.Build(builder);
			T4.Build(builder);
			T5.Build(builder);
			T6.Build(builder);
			T7.Build(builder);
			T8.Build(builder);
			T9.Build(builder);
			T10.Build(builder);
			T11.Build(builder);
			T12.Build(builder);
			T13.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>.Current => ref this;

        static Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
			var i1 = _iter1.MoveNext();
			var i2 = _iter2.MoveNext();
			var i3 = _iter3.MoveNext();
			var i4 = _iter4.MoveNext();
			var i5 = _iter5.MoveNext();
			var i6 = _iter6.MoveNext();
			var i7 = _iter7.MoveNext();
			var i8 = _iter8.MoveNext();
			var i9 = _iter9.MoveNext();
			var i10 = _iter10.MoveNext();
			var i11 = _iter11.MoveNext();
			var i12 = _iter12.MoveNext();
			var i13 = _iter13.MoveNext();
            return i0 && i1 && i2 && i3 && i4 && i5 && i6 && i7 && i8 && i9 && i10 && i11 && i12 && i13;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
			_iter1.SetTicks(lastRun, thisRun);
			_iter2.SetTicks(lastRun, thisRun);
			_iter3.SetTicks(lastRun, thisRun);
			_iter4.SetTicks(lastRun, thisRun);
			_iter5.SetTicks(lastRun, thisRun);
			_iter6.SetTicks(lastRun, thisRun);
			_iter7.SetTicks(lastRun, thisRun);
			_iter8.SetTicks(lastRun, thisRun);
			_iter9.SetTicks(lastRun, thisRun);
			_iter10.SetTicks(lastRun, thisRun);
			_iter11.SetTicks(lastRun, thisRun);
			_iter12.SetTicks(lastRun, thisRun);
			_iter13.SetTicks(lastRun, thisRun);
        }
    }

    public ref struct Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>
        where T0 : struct, IFilter<T0>, allows ref struct
		where T1 : struct, IFilter<T1>, allows ref struct
		where T2 : struct, IFilter<T2>, allows ref struct
		where T3 : struct, IFilter<T3>, allows ref struct
		where T4 : struct, IFilter<T4>, allows ref struct
		where T5 : struct, IFilter<T5>, allows ref struct
		where T6 : struct, IFilter<T6>, allows ref struct
		where T7 : struct, IFilter<T7>, allows ref struct
		where T8 : struct, IFilter<T8>, allows ref struct
		where T9 : struct, IFilter<T9>, allows ref struct
		where T10 : struct, IFilter<T10>, allows ref struct
		where T11 : struct, IFilter<T11>, allows ref struct
		where T12 : struct, IFilter<T12>, allows ref struct
		where T13 : struct, IFilter<T13>, allows ref struct
		where T14 : struct, IFilter<T14>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;
		private T1 _iter1;
		private T2 _iter2;
		private T3 _iter3;
		private T4 _iter4;
		private T5 _iter5;
		private T6 _iter6;
		private T7 _iter7;
		private T8 _iter8;
		private T9 _iter9;
		private T10 _iter10;
		private T11 _iter11;
		private T12 _iter12;
		private T13 _iter13;
		private T14 _iter14;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
			_iter1 = T1.CreateIterator(iterator);
			_iter2 = T2.CreateIterator(iterator);
			_iter3 = T3.CreateIterator(iterator);
			_iter4 = T4.CreateIterator(iterator);
			_iter5 = T5.CreateIterator(iterator);
			_iter6 = T6.CreateIterator(iterator);
			_iter7 = T7.CreateIterator(iterator);
			_iter8 = T8.CreateIterator(iterator);
			_iter9 = T9.CreateIterator(iterator);
			_iter10 = T10.CreateIterator(iterator);
			_iter11 = T11.CreateIterator(iterator);
			_iter12 = T12.CreateIterator(iterator);
			_iter13 = T13.CreateIterator(iterator);
			_iter14 = T14.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
			T1.Build(builder);
			T2.Build(builder);
			T3.Build(builder);
			T4.Build(builder);
			T5.Build(builder);
			T6.Build(builder);
			T7.Build(builder);
			T8.Build(builder);
			T9.Build(builder);
			T10.Build(builder);
			T11.Build(builder);
			T12.Build(builder);
			T13.Build(builder);
			T14.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>.Current => ref this;

        static Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
			var i1 = _iter1.MoveNext();
			var i2 = _iter2.MoveNext();
			var i3 = _iter3.MoveNext();
			var i4 = _iter4.MoveNext();
			var i5 = _iter5.MoveNext();
			var i6 = _iter6.MoveNext();
			var i7 = _iter7.MoveNext();
			var i8 = _iter8.MoveNext();
			var i9 = _iter9.MoveNext();
			var i10 = _iter10.MoveNext();
			var i11 = _iter11.MoveNext();
			var i12 = _iter12.MoveNext();
			var i13 = _iter13.MoveNext();
			var i14 = _iter14.MoveNext();
            return i0 && i1 && i2 && i3 && i4 && i5 && i6 && i7 && i8 && i9 && i10 && i11 && i12 && i13 && i14;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
			_iter1.SetTicks(lastRun, thisRun);
			_iter2.SetTicks(lastRun, thisRun);
			_iter3.SetTicks(lastRun, thisRun);
			_iter4.SetTicks(lastRun, thisRun);
			_iter5.SetTicks(lastRun, thisRun);
			_iter6.SetTicks(lastRun, thisRun);
			_iter7.SetTicks(lastRun, thisRun);
			_iter8.SetTicks(lastRun, thisRun);
			_iter9.SetTicks(lastRun, thisRun);
			_iter10.SetTicks(lastRun, thisRun);
			_iter11.SetTicks(lastRun, thisRun);
			_iter12.SetTicks(lastRun, thisRun);
			_iter13.SetTicks(lastRun, thisRun);
			_iter14.SetTicks(lastRun, thisRun);
        }
    }

    public ref struct Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>
        where T0 : struct, IFilter<T0>, allows ref struct
		where T1 : struct, IFilter<T1>, allows ref struct
		where T2 : struct, IFilter<T2>, allows ref struct
		where T3 : struct, IFilter<T3>, allows ref struct
		where T4 : struct, IFilter<T4>, allows ref struct
		where T5 : struct, IFilter<T5>, allows ref struct
		where T6 : struct, IFilter<T6>, allows ref struct
		where T7 : struct, IFilter<T7>, allows ref struct
		where T8 : struct, IFilter<T8>, allows ref struct
		where T9 : struct, IFilter<T9>, allows ref struct
		where T10 : struct, IFilter<T10>, allows ref struct
		where T11 : struct, IFilter<T11>, allows ref struct
		where T12 : struct, IFilter<T12>, allows ref struct
		where T13 : struct, IFilter<T13>, allows ref struct
		where T14 : struct, IFilter<T14>, allows ref struct
		where T15 : struct, IFilter<T15>, allows ref struct
    {
        private QueryIterator _iterator;
        private T0 _iter0;
		private T1 _iter1;
		private T2 _iter2;
		private T3 _iter3;
		private T4 _iter4;
		private T5 _iter5;
		private T6 _iter6;
		private T7 _iter7;
		private T8 _iter8;
		private T9 _iter9;
		private T10 _iter10;
		private T11 _iter11;
		private T12 _iter12;
		private T13 _iter13;
		private T14 _iter14;
		private T15 _iter15;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Filter(QueryIterator iterator)
        {
            _iterator = iterator;
            _iter0 = T0.CreateIterator(iterator);
			_iter1 = T1.CreateIterator(iterator);
			_iter2 = T2.CreateIterator(iterator);
			_iter3 = T3.CreateIterator(iterator);
			_iter4 = T4.CreateIterator(iterator);
			_iter5 = T5.CreateIterator(iterator);
			_iter6 = T6.CreateIterator(iterator);
			_iter7 = T7.CreateIterator(iterator);
			_iter8 = T8.CreateIterator(iterator);
			_iter9 = T9.CreateIterator(iterator);
			_iter10 = T10.CreateIterator(iterator);
			_iter11 = T11.CreateIterator(iterator);
			_iter12 = T12.CreateIterator(iterator);
			_iter13 = T13.CreateIterator(iterator);
			_iter14 = T14.CreateIterator(iterator);
			_iter15 = T15.CreateIterator(iterator);
        }

        public static void Build(QueryBuilder builder)
        {
            T0.Build(builder);
			T1.Build(builder);
			T2.Build(builder);
			T3.Build(builder);
			T4.Build(builder);
			T5.Build(builder);
			T6.Build(builder);
			T7.Build(builder);
			T8.Build(builder);
			T9.Build(builder);
			T10.Build(builder);
			T11.Build(builder);
			T12.Build(builder);
			T13.Build(builder);
			T14.Build(builder);
			T15.Build(builder);
        }

        [System.Diagnostics.CodeAnalysis.UnscopedRef]
        ref Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>.Current => ref this;

        static Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> IFilter<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>.CreateIterator(QueryIterator iterator)
        {
            return new(iterator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>.GetEnumerator()
        {
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IQueryIterator<Filter<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>.MoveNext()
        {
            var i0 = _iter0.MoveNext();
			var i1 = _iter1.MoveNext();
			var i2 = _iter2.MoveNext();
			var i3 = _iter3.MoveNext();
			var i4 = _iter4.MoveNext();
			var i5 = _iter5.MoveNext();
			var i6 = _iter6.MoveNext();
			var i7 = _iter7.MoveNext();
			var i8 = _iter8.MoveNext();
			var i9 = _iter9.MoveNext();
			var i10 = _iter10.MoveNext();
			var i11 = _iter11.MoveNext();
			var i12 = _iter12.MoveNext();
			var i13 = _iter13.MoveNext();
			var i14 = _iter14.MoveNext();
			var i15 = _iter15.MoveNext();
            return i0 && i1 && i2 && i3 && i4 && i5 && i6 && i7 && i8 && i9 && i10 && i11 && i12 && i13 && i14 && i15;
        }

        public void SetTicks(uint lastRun, uint thisRun)
        {
            _iter0.SetTicks(lastRun, thisRun);
			_iter1.SetTicks(lastRun, thisRun);
			_iter2.SetTicks(lastRun, thisRun);
			_iter3.SetTicks(lastRun, thisRun);
			_iter4.SetTicks(lastRun, thisRun);
			_iter5.SetTicks(lastRun, thisRun);
			_iter6.SetTicks(lastRun, thisRun);
			_iter7.SetTicks(lastRun, thisRun);
			_iter8.SetTicks(lastRun, thisRun);
			_iter9.SetTicks(lastRun, thisRun);
			_iter10.SetTicks(lastRun, thisRun);
			_iter11.SetTicks(lastRun, thisRun);
			_iter12.SetTicks(lastRun, thisRun);
			_iter13.SetTicks(lastRun, thisRun);
			_iter14.SetTicks(lastRun, thisRun);
			_iter15.SetTicks(lastRun, thisRun);
        }
    }

#endif
}

#pragma warning restore 1591

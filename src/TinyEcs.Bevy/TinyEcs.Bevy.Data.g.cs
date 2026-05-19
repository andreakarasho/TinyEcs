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
        private const uint ALL_PRESENT_MASK = 0x1u;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadChunk(ref Data<T0> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
            return true;
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
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1> : IData<Data<T0, T1>>, IQueryComponentAccess
        where T0 : struct where T1 : struct
    {
        private const uint ALL_PRESENT_MASK = 0x3u;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;
		internal DataRow<T1> _current1;

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadChunk(ref Data<T0, T1> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
			row._current1 = iterator.GetColumn<T1>(1);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u) |
					(row._current1.Value.IsValid() ? (1u << 1) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0, T1> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
				row._current1.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
			if ((m & (1u << 1)) != 0) row._current1.Next();
            return true;
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
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2> : IData<Data<T0, T1, T2>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct
    {
        private const uint ALL_PRESENT_MASK = 0x7u;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;
		internal DataRow<T1> _current1;
		internal DataRow<T2> _current2;

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadChunk(ref Data<T0, T1, T2> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
			row._current1 = iterator.GetColumn<T1>(1);
			row._current2 = iterator.GetColumn<T2>(2);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u) |
					(row._current1.Value.IsValid() ? (1u << 1) : 0u) |
					(row._current2.Value.IsValid() ? (1u << 2) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0, T1, T2> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
				row._current1.Next();
				row._current2.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
			if ((m & (1u << 1)) != 0) row._current1.Next();
			if ((m & (1u << 2)) != 0) row._current2.Next();
            return true;
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
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3> : IData<Data<T0, T1, T2, T3>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct
    {
        private const uint ALL_PRESENT_MASK = 0xFu;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;
		internal DataRow<T1> _current1;
		internal DataRow<T2> _current2;
		internal DataRow<T3> _current3;

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
			builder.With<T3>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadChunk(ref Data<T0, T1, T2, T3> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
			row._current1 = iterator.GetColumn<T1>(1);
			row._current2 = iterator.GetColumn<T2>(2);
			row._current3 = iterator.GetColumn<T3>(3);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u) |
					(row._current1.Value.IsValid() ? (1u << 1) : 0u) |
					(row._current2.Value.IsValid() ? (1u << 2) : 0u) |
					(row._current3.Value.IsValid() ? (1u << 3) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0, T1, T2, T3> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
				row._current1.Next();
				row._current2.Next();
				row._current3.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
			if ((m & (1u << 1)) != 0) row._current1.Next();
			if ((m & (1u << 2)) != 0) row._current2.Next();
			if ((m & (1u << 3)) != 0) row._current3.Next();
            return true;
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
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4> : IData<Data<T0, T1, T2, T3, T4>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct
    {
        private const uint ALL_PRESENT_MASK = 0x1Fu;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;
		internal DataRow<T1> _current1;
		internal DataRow<T2> _current2;
		internal DataRow<T3> _current3;
		internal DataRow<T4> _current4;

        public static void Build(QueryBuilder builder)
        {
            builder.With<T0>();
			builder.With<T1>();
			builder.With<T2>();
			builder.With<T3>();
			builder.With<T4>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadChunk(ref Data<T0, T1, T2, T3, T4> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
			row._current1 = iterator.GetColumn<T1>(1);
			row._current2 = iterator.GetColumn<T2>(2);
			row._current3 = iterator.GetColumn<T3>(3);
			row._current4 = iterator.GetColumn<T4>(4);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u) |
					(row._current1.Value.IsValid() ? (1u << 1) : 0u) |
					(row._current2.Value.IsValid() ? (1u << 2) : 0u) |
					(row._current3.Value.IsValid() ? (1u << 3) : 0u) |
					(row._current4.Value.IsValid() ? (1u << 4) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0, T1, T2, T3, T4> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
				row._current1.Next();
				row._current2.Next();
				row._current3.Next();
				row._current4.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
			if ((m & (1u << 1)) != 0) row._current1.Next();
			if ((m & (1u << 2)) != 0) row._current2.Next();
			if ((m & (1u << 3)) != 0) row._current3.Next();
			if ((m & (1u << 4)) != 0) row._current4.Next();
            return true;
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
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5> : IData<Data<T0, T1, T2, T3, T4, T5>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
    {
        private const uint ALL_PRESENT_MASK = 0x3Fu;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;
		internal DataRow<T1> _current1;
		internal DataRow<T2> _current2;
		internal DataRow<T3> _current3;
		internal DataRow<T4> _current4;
		internal DataRow<T5> _current5;

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
        public static void LoadChunk(ref Data<T0, T1, T2, T3, T4, T5> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
			row._current1 = iterator.GetColumn<T1>(1);
			row._current2 = iterator.GetColumn<T2>(2);
			row._current3 = iterator.GetColumn<T3>(3);
			row._current4 = iterator.GetColumn<T4>(4);
			row._current5 = iterator.GetColumn<T5>(5);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u) |
					(row._current1.Value.IsValid() ? (1u << 1) : 0u) |
					(row._current2.Value.IsValid() ? (1u << 2) : 0u) |
					(row._current3.Value.IsValid() ? (1u << 3) : 0u) |
					(row._current4.Value.IsValid() ? (1u << 4) : 0u) |
					(row._current5.Value.IsValid() ? (1u << 5) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0, T1, T2, T3, T4, T5> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
				row._current1.Next();
				row._current2.Next();
				row._current3.Next();
				row._current4.Next();
				row._current5.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
			if ((m & (1u << 1)) != 0) row._current1.Next();
			if ((m & (1u << 2)) != 0) row._current2.Next();
			if ((m & (1u << 3)) != 0) row._current3.Next();
			if ((m & (1u << 4)) != 0) row._current4.Next();
			if ((m & (1u << 5)) != 0) row._current5.Next();
            return true;
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
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6> : IData<Data<T0, T1, T2, T3, T4, T5, T6>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
    {
        private const uint ALL_PRESENT_MASK = 0x7Fu;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;
		internal DataRow<T1> _current1;
		internal DataRow<T2> _current2;
		internal DataRow<T3> _current3;
		internal DataRow<T4> _current4;
		internal DataRow<T5> _current5;
		internal DataRow<T6> _current6;

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
        public static void LoadChunk(ref Data<T0, T1, T2, T3, T4, T5, T6> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
			row._current1 = iterator.GetColumn<T1>(1);
			row._current2 = iterator.GetColumn<T2>(2);
			row._current3 = iterator.GetColumn<T3>(3);
			row._current4 = iterator.GetColumn<T4>(4);
			row._current5 = iterator.GetColumn<T5>(5);
			row._current6 = iterator.GetColumn<T6>(6);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u) |
					(row._current1.Value.IsValid() ? (1u << 1) : 0u) |
					(row._current2.Value.IsValid() ? (1u << 2) : 0u) |
					(row._current3.Value.IsValid() ? (1u << 3) : 0u) |
					(row._current4.Value.IsValid() ? (1u << 4) : 0u) |
					(row._current5.Value.IsValid() ? (1u << 5) : 0u) |
					(row._current6.Value.IsValid() ? (1u << 6) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0, T1, T2, T3, T4, T5, T6> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
				row._current1.Next();
				row._current2.Next();
				row._current3.Next();
				row._current4.Next();
				row._current5.Next();
				row._current6.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
			if ((m & (1u << 1)) != 0) row._current1.Next();
			if ((m & (1u << 2)) != 0) row._current2.Next();
			if ((m & (1u << 3)) != 0) row._current3.Next();
			if ((m & (1u << 4)) != 0) row._current4.Next();
			if ((m & (1u << 5)) != 0) row._current5.Next();
			if ((m & (1u << 6)) != 0) row._current6.Next();
            return true;
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
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct
    {
        private const uint ALL_PRESENT_MASK = 0xFFu;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;
		internal DataRow<T1> _current1;
		internal DataRow<T2> _current2;
		internal DataRow<T3> _current3;
		internal DataRow<T4> _current4;
		internal DataRow<T5> _current5;
		internal DataRow<T6> _current6;
		internal DataRow<T7> _current7;

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
        public static void LoadChunk(ref Data<T0, T1, T2, T3, T4, T5, T6, T7> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
			row._current1 = iterator.GetColumn<T1>(1);
			row._current2 = iterator.GetColumn<T2>(2);
			row._current3 = iterator.GetColumn<T3>(3);
			row._current4 = iterator.GetColumn<T4>(4);
			row._current5 = iterator.GetColumn<T5>(5);
			row._current6 = iterator.GetColumn<T6>(6);
			row._current7 = iterator.GetColumn<T7>(7);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u) |
					(row._current1.Value.IsValid() ? (1u << 1) : 0u) |
					(row._current2.Value.IsValid() ? (1u << 2) : 0u) |
					(row._current3.Value.IsValid() ? (1u << 3) : 0u) |
					(row._current4.Value.IsValid() ? (1u << 4) : 0u) |
					(row._current5.Value.IsValid() ? (1u << 5) : 0u) |
					(row._current6.Value.IsValid() ? (1u << 6) : 0u) |
					(row._current7.Value.IsValid() ? (1u << 7) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0, T1, T2, T3, T4, T5, T6, T7> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
				row._current1.Next();
				row._current2.Next();
				row._current3.Next();
				row._current4.Next();
				row._current5.Next();
				row._current6.Next();
				row._current7.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
			if ((m & (1u << 1)) != 0) row._current1.Next();
			if ((m & (1u << 2)) != 0) row._current2.Next();
			if ((m & (1u << 3)) != 0) row._current3.Next();
			if ((m & (1u << 4)) != 0) row._current4.Next();
			if ((m & (1u << 5)) != 0) row._current5.Next();
			if ((m & (1u << 6)) != 0) row._current6.Next();
			if ((m & (1u << 7)) != 0) row._current7.Next();
            return true;
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
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct
    {
        private const uint ALL_PRESENT_MASK = 0x1FFu;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;
		internal DataRow<T1> _current1;
		internal DataRow<T2> _current2;
		internal DataRow<T3> _current3;
		internal DataRow<T4> _current4;
		internal DataRow<T5> _current5;
		internal DataRow<T6> _current6;
		internal DataRow<T7> _current7;
		internal DataRow<T8> _current8;

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
        public static void LoadChunk(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
			row._current1 = iterator.GetColumn<T1>(1);
			row._current2 = iterator.GetColumn<T2>(2);
			row._current3 = iterator.GetColumn<T3>(3);
			row._current4 = iterator.GetColumn<T4>(4);
			row._current5 = iterator.GetColumn<T5>(5);
			row._current6 = iterator.GetColumn<T6>(6);
			row._current7 = iterator.GetColumn<T7>(7);
			row._current8 = iterator.GetColumn<T8>(8);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u) |
					(row._current1.Value.IsValid() ? (1u << 1) : 0u) |
					(row._current2.Value.IsValid() ? (1u << 2) : 0u) |
					(row._current3.Value.IsValid() ? (1u << 3) : 0u) |
					(row._current4.Value.IsValid() ? (1u << 4) : 0u) |
					(row._current5.Value.IsValid() ? (1u << 5) : 0u) |
					(row._current6.Value.IsValid() ? (1u << 6) : 0u) |
					(row._current7.Value.IsValid() ? (1u << 7) : 0u) |
					(row._current8.Value.IsValid() ? (1u << 8) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
				row._current1.Next();
				row._current2.Next();
				row._current3.Next();
				row._current4.Next();
				row._current5.Next();
				row._current6.Next();
				row._current7.Next();
				row._current8.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
			if ((m & (1u << 1)) != 0) row._current1.Next();
			if ((m & (1u << 2)) != 0) row._current2.Next();
			if ((m & (1u << 3)) != 0) row._current3.Next();
			if ((m & (1u << 4)) != 0) row._current4.Next();
			if ((m & (1u << 5)) != 0) row._current5.Next();
			if ((m & (1u << 6)) != 0) row._current6.Next();
			if ((m & (1u << 7)) != 0) row._current7.Next();
			if ((m & (1u << 8)) != 0) row._current8.Next();
            return true;
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
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct
    {
        private const uint ALL_PRESENT_MASK = 0x3FFu;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;
		internal DataRow<T1> _current1;
		internal DataRow<T2> _current2;
		internal DataRow<T3> _current3;
		internal DataRow<T4> _current4;
		internal DataRow<T5> _current5;
		internal DataRow<T6> _current6;
		internal DataRow<T7> _current7;
		internal DataRow<T8> _current8;
		internal DataRow<T9> _current9;

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
        public static void LoadChunk(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
			row._current1 = iterator.GetColumn<T1>(1);
			row._current2 = iterator.GetColumn<T2>(2);
			row._current3 = iterator.GetColumn<T3>(3);
			row._current4 = iterator.GetColumn<T4>(4);
			row._current5 = iterator.GetColumn<T5>(5);
			row._current6 = iterator.GetColumn<T6>(6);
			row._current7 = iterator.GetColumn<T7>(7);
			row._current8 = iterator.GetColumn<T8>(8);
			row._current9 = iterator.GetColumn<T9>(9);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u) |
					(row._current1.Value.IsValid() ? (1u << 1) : 0u) |
					(row._current2.Value.IsValid() ? (1u << 2) : 0u) |
					(row._current3.Value.IsValid() ? (1u << 3) : 0u) |
					(row._current4.Value.IsValid() ? (1u << 4) : 0u) |
					(row._current5.Value.IsValid() ? (1u << 5) : 0u) |
					(row._current6.Value.IsValid() ? (1u << 6) : 0u) |
					(row._current7.Value.IsValid() ? (1u << 7) : 0u) |
					(row._current8.Value.IsValid() ? (1u << 8) : 0u) |
					(row._current9.Value.IsValid() ? (1u << 9) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
				row._current1.Next();
				row._current2.Next();
				row._current3.Next();
				row._current4.Next();
				row._current5.Next();
				row._current6.Next();
				row._current7.Next();
				row._current8.Next();
				row._current9.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
			if ((m & (1u << 1)) != 0) row._current1.Next();
			if ((m & (1u << 2)) != 0) row._current2.Next();
			if ((m & (1u << 3)) != 0) row._current3.Next();
			if ((m & (1u << 4)) != 0) row._current4.Next();
			if ((m & (1u << 5)) != 0) row._current5.Next();
			if ((m & (1u << 6)) != 0) row._current6.Next();
			if ((m & (1u << 7)) != 0) row._current7.Next();
			if ((m & (1u << 8)) != 0) row._current8.Next();
			if ((m & (1u << 9)) != 0) row._current9.Next();
            return true;
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
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct
    {
        private const uint ALL_PRESENT_MASK = 0x7FFu;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;
		internal DataRow<T1> _current1;
		internal DataRow<T2> _current2;
		internal DataRow<T3> _current3;
		internal DataRow<T4> _current4;
		internal DataRow<T5> _current5;
		internal DataRow<T6> _current6;
		internal DataRow<T7> _current7;
		internal DataRow<T8> _current8;
		internal DataRow<T9> _current9;
		internal DataRow<T10> _current10;

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
        public static void LoadChunk(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
			row._current1 = iterator.GetColumn<T1>(1);
			row._current2 = iterator.GetColumn<T2>(2);
			row._current3 = iterator.GetColumn<T3>(3);
			row._current4 = iterator.GetColumn<T4>(4);
			row._current5 = iterator.GetColumn<T5>(5);
			row._current6 = iterator.GetColumn<T6>(6);
			row._current7 = iterator.GetColumn<T7>(7);
			row._current8 = iterator.GetColumn<T8>(8);
			row._current9 = iterator.GetColumn<T9>(9);
			row._current10 = iterator.GetColumn<T10>(10);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u) |
					(row._current1.Value.IsValid() ? (1u << 1) : 0u) |
					(row._current2.Value.IsValid() ? (1u << 2) : 0u) |
					(row._current3.Value.IsValid() ? (1u << 3) : 0u) |
					(row._current4.Value.IsValid() ? (1u << 4) : 0u) |
					(row._current5.Value.IsValid() ? (1u << 5) : 0u) |
					(row._current6.Value.IsValid() ? (1u << 6) : 0u) |
					(row._current7.Value.IsValid() ? (1u << 7) : 0u) |
					(row._current8.Value.IsValid() ? (1u << 8) : 0u) |
					(row._current9.Value.IsValid() ? (1u << 9) : 0u) |
					(row._current10.Value.IsValid() ? (1u << 10) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
				row._current1.Next();
				row._current2.Next();
				row._current3.Next();
				row._current4.Next();
				row._current5.Next();
				row._current6.Next();
				row._current7.Next();
				row._current8.Next();
				row._current9.Next();
				row._current10.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
			if ((m & (1u << 1)) != 0) row._current1.Next();
			if ((m & (1u << 2)) != 0) row._current2.Next();
			if ((m & (1u << 3)) != 0) row._current3.Next();
			if ((m & (1u << 4)) != 0) row._current4.Next();
			if ((m & (1u << 5)) != 0) row._current5.Next();
			if ((m & (1u << 6)) != 0) row._current6.Next();
			if ((m & (1u << 7)) != 0) row._current7.Next();
			if ((m & (1u << 8)) != 0) row._current8.Next();
			if ((m & (1u << 9)) != 0) row._current9.Next();
			if ((m & (1u << 10)) != 0) row._current10.Next();
            return true;
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
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct
    {
        private const uint ALL_PRESENT_MASK = 0xFFFu;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;
		internal DataRow<T1> _current1;
		internal DataRow<T2> _current2;
		internal DataRow<T3> _current3;
		internal DataRow<T4> _current4;
		internal DataRow<T5> _current5;
		internal DataRow<T6> _current6;
		internal DataRow<T7> _current7;
		internal DataRow<T8> _current8;
		internal DataRow<T9> _current9;
		internal DataRow<T10> _current10;
		internal DataRow<T11> _current11;

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
        public static void LoadChunk(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
			row._current1 = iterator.GetColumn<T1>(1);
			row._current2 = iterator.GetColumn<T2>(2);
			row._current3 = iterator.GetColumn<T3>(3);
			row._current4 = iterator.GetColumn<T4>(4);
			row._current5 = iterator.GetColumn<T5>(5);
			row._current6 = iterator.GetColumn<T6>(6);
			row._current7 = iterator.GetColumn<T7>(7);
			row._current8 = iterator.GetColumn<T8>(8);
			row._current9 = iterator.GetColumn<T9>(9);
			row._current10 = iterator.GetColumn<T10>(10);
			row._current11 = iterator.GetColumn<T11>(11);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u) |
					(row._current1.Value.IsValid() ? (1u << 1) : 0u) |
					(row._current2.Value.IsValid() ? (1u << 2) : 0u) |
					(row._current3.Value.IsValid() ? (1u << 3) : 0u) |
					(row._current4.Value.IsValid() ? (1u << 4) : 0u) |
					(row._current5.Value.IsValid() ? (1u << 5) : 0u) |
					(row._current6.Value.IsValid() ? (1u << 6) : 0u) |
					(row._current7.Value.IsValid() ? (1u << 7) : 0u) |
					(row._current8.Value.IsValid() ? (1u << 8) : 0u) |
					(row._current9.Value.IsValid() ? (1u << 9) : 0u) |
					(row._current10.Value.IsValid() ? (1u << 10) : 0u) |
					(row._current11.Value.IsValid() ? (1u << 11) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
				row._current1.Next();
				row._current2.Next();
				row._current3.Next();
				row._current4.Next();
				row._current5.Next();
				row._current6.Next();
				row._current7.Next();
				row._current8.Next();
				row._current9.Next();
				row._current10.Next();
				row._current11.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
			if ((m & (1u << 1)) != 0) row._current1.Next();
			if ((m & (1u << 2)) != 0) row._current2.Next();
			if ((m & (1u << 3)) != 0) row._current3.Next();
			if ((m & (1u << 4)) != 0) row._current4.Next();
			if ((m & (1u << 5)) != 0) row._current5.Next();
			if ((m & (1u << 6)) != 0) row._current6.Next();
			if ((m & (1u << 7)) != 0) row._current7.Next();
			if ((m & (1u << 8)) != 0) row._current8.Next();
			if ((m & (1u << 9)) != 0) row._current9.Next();
			if ((m & (1u << 10)) != 0) row._current10.Next();
			if ((m & (1u << 11)) != 0) row._current11.Next();
            return true;
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
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct
    {
        private const uint ALL_PRESENT_MASK = 0x1FFFu;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;
		internal DataRow<T1> _current1;
		internal DataRow<T2> _current2;
		internal DataRow<T3> _current3;
		internal DataRow<T4> _current4;
		internal DataRow<T5> _current5;
		internal DataRow<T6> _current6;
		internal DataRow<T7> _current7;
		internal DataRow<T8> _current8;
		internal DataRow<T9> _current9;
		internal DataRow<T10> _current10;
		internal DataRow<T11> _current11;
		internal DataRow<T12> _current12;

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
        public static void LoadChunk(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
			row._current1 = iterator.GetColumn<T1>(1);
			row._current2 = iterator.GetColumn<T2>(2);
			row._current3 = iterator.GetColumn<T3>(3);
			row._current4 = iterator.GetColumn<T4>(4);
			row._current5 = iterator.GetColumn<T5>(5);
			row._current6 = iterator.GetColumn<T6>(6);
			row._current7 = iterator.GetColumn<T7>(7);
			row._current8 = iterator.GetColumn<T8>(8);
			row._current9 = iterator.GetColumn<T9>(9);
			row._current10 = iterator.GetColumn<T10>(10);
			row._current11 = iterator.GetColumn<T11>(11);
			row._current12 = iterator.GetColumn<T12>(12);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u) |
					(row._current1.Value.IsValid() ? (1u << 1) : 0u) |
					(row._current2.Value.IsValid() ? (1u << 2) : 0u) |
					(row._current3.Value.IsValid() ? (1u << 3) : 0u) |
					(row._current4.Value.IsValid() ? (1u << 4) : 0u) |
					(row._current5.Value.IsValid() ? (1u << 5) : 0u) |
					(row._current6.Value.IsValid() ? (1u << 6) : 0u) |
					(row._current7.Value.IsValid() ? (1u << 7) : 0u) |
					(row._current8.Value.IsValid() ? (1u << 8) : 0u) |
					(row._current9.Value.IsValid() ? (1u << 9) : 0u) |
					(row._current10.Value.IsValid() ? (1u << 10) : 0u) |
					(row._current11.Value.IsValid() ? (1u << 11) : 0u) |
					(row._current12.Value.IsValid() ? (1u << 12) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
				row._current1.Next();
				row._current2.Next();
				row._current3.Next();
				row._current4.Next();
				row._current5.Next();
				row._current6.Next();
				row._current7.Next();
				row._current8.Next();
				row._current9.Next();
				row._current10.Next();
				row._current11.Next();
				row._current12.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
			if ((m & (1u << 1)) != 0) row._current1.Next();
			if ((m & (1u << 2)) != 0) row._current2.Next();
			if ((m & (1u << 3)) != 0) row._current3.Next();
			if ((m & (1u << 4)) != 0) row._current4.Next();
			if ((m & (1u << 5)) != 0) row._current5.Next();
			if ((m & (1u << 6)) != 0) row._current6.Next();
			if ((m & (1u << 7)) != 0) row._current7.Next();
			if ((m & (1u << 8)) != 0) row._current8.Next();
			if ((m & (1u << 9)) != 0) row._current9.Next();
			if ((m & (1u << 10)) != 0) row._current10.Next();
			if ((m & (1u << 11)) != 0) row._current11.Next();
			if ((m & (1u << 12)) != 0) row._current12.Next();
            return true;
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
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct
    {
        private const uint ALL_PRESENT_MASK = 0x3FFFu;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;
		internal DataRow<T1> _current1;
		internal DataRow<T2> _current2;
		internal DataRow<T3> _current3;
		internal DataRow<T4> _current4;
		internal DataRow<T5> _current5;
		internal DataRow<T6> _current6;
		internal DataRow<T7> _current7;
		internal DataRow<T8> _current8;
		internal DataRow<T9> _current9;
		internal DataRow<T10> _current10;
		internal DataRow<T11> _current11;
		internal DataRow<T12> _current12;
		internal DataRow<T13> _current13;

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
        public static void LoadChunk(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
			row._current1 = iterator.GetColumn<T1>(1);
			row._current2 = iterator.GetColumn<T2>(2);
			row._current3 = iterator.GetColumn<T3>(3);
			row._current4 = iterator.GetColumn<T4>(4);
			row._current5 = iterator.GetColumn<T5>(5);
			row._current6 = iterator.GetColumn<T6>(6);
			row._current7 = iterator.GetColumn<T7>(7);
			row._current8 = iterator.GetColumn<T8>(8);
			row._current9 = iterator.GetColumn<T9>(9);
			row._current10 = iterator.GetColumn<T10>(10);
			row._current11 = iterator.GetColumn<T11>(11);
			row._current12 = iterator.GetColumn<T12>(12);
			row._current13 = iterator.GetColumn<T13>(13);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u) |
					(row._current1.Value.IsValid() ? (1u << 1) : 0u) |
					(row._current2.Value.IsValid() ? (1u << 2) : 0u) |
					(row._current3.Value.IsValid() ? (1u << 3) : 0u) |
					(row._current4.Value.IsValid() ? (1u << 4) : 0u) |
					(row._current5.Value.IsValid() ? (1u << 5) : 0u) |
					(row._current6.Value.IsValid() ? (1u << 6) : 0u) |
					(row._current7.Value.IsValid() ? (1u << 7) : 0u) |
					(row._current8.Value.IsValid() ? (1u << 8) : 0u) |
					(row._current9.Value.IsValid() ? (1u << 9) : 0u) |
					(row._current10.Value.IsValid() ? (1u << 10) : 0u) |
					(row._current11.Value.IsValid() ? (1u << 11) : 0u) |
					(row._current12.Value.IsValid() ? (1u << 12) : 0u) |
					(row._current13.Value.IsValid() ? (1u << 13) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
				row._current1.Next();
				row._current2.Next();
				row._current3.Next();
				row._current4.Next();
				row._current5.Next();
				row._current6.Next();
				row._current7.Next();
				row._current8.Next();
				row._current9.Next();
				row._current10.Next();
				row._current11.Next();
				row._current12.Next();
				row._current13.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
			if ((m & (1u << 1)) != 0) row._current1.Next();
			if ((m & (1u << 2)) != 0) row._current2.Next();
			if ((m & (1u << 3)) != 0) row._current3.Next();
			if ((m & (1u << 4)) != 0) row._current4.Next();
			if ((m & (1u << 5)) != 0) row._current5.Next();
			if ((m & (1u << 6)) != 0) row._current6.Next();
			if ((m & (1u << 7)) != 0) row._current7.Next();
			if ((m & (1u << 8)) != 0) row._current8.Next();
			if ((m & (1u << 9)) != 0) row._current9.Next();
			if ((m & (1u << 10)) != 0) row._current10.Next();
			if ((m & (1u << 11)) != 0) row._current11.Next();
			if ((m & (1u << 12)) != 0) row._current12.Next();
			if ((m & (1u << 13)) != 0) row._current13.Next();
            return true;
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
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct
    {
        private const uint ALL_PRESENT_MASK = 0x7FFFu;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;
		internal DataRow<T1> _current1;
		internal DataRow<T2> _current2;
		internal DataRow<T3> _current3;
		internal DataRow<T4> _current4;
		internal DataRow<T5> _current5;
		internal DataRow<T6> _current6;
		internal DataRow<T7> _current7;
		internal DataRow<T8> _current8;
		internal DataRow<T9> _current9;
		internal DataRow<T10> _current10;
		internal DataRow<T11> _current11;
		internal DataRow<T12> _current12;
		internal DataRow<T13> _current13;
		internal DataRow<T14> _current14;

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
        public static void LoadChunk(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
			row._current1 = iterator.GetColumn<T1>(1);
			row._current2 = iterator.GetColumn<T2>(2);
			row._current3 = iterator.GetColumn<T3>(3);
			row._current4 = iterator.GetColumn<T4>(4);
			row._current5 = iterator.GetColumn<T5>(5);
			row._current6 = iterator.GetColumn<T6>(6);
			row._current7 = iterator.GetColumn<T7>(7);
			row._current8 = iterator.GetColumn<T8>(8);
			row._current9 = iterator.GetColumn<T9>(9);
			row._current10 = iterator.GetColumn<T10>(10);
			row._current11 = iterator.GetColumn<T11>(11);
			row._current12 = iterator.GetColumn<T12>(12);
			row._current13 = iterator.GetColumn<T13>(13);
			row._current14 = iterator.GetColumn<T14>(14);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u) |
					(row._current1.Value.IsValid() ? (1u << 1) : 0u) |
					(row._current2.Value.IsValid() ? (1u << 2) : 0u) |
					(row._current3.Value.IsValid() ? (1u << 3) : 0u) |
					(row._current4.Value.IsValid() ? (1u << 4) : 0u) |
					(row._current5.Value.IsValid() ? (1u << 5) : 0u) |
					(row._current6.Value.IsValid() ? (1u << 6) : 0u) |
					(row._current7.Value.IsValid() ? (1u << 7) : 0u) |
					(row._current8.Value.IsValid() ? (1u << 8) : 0u) |
					(row._current9.Value.IsValid() ? (1u << 9) : 0u) |
					(row._current10.Value.IsValid() ? (1u << 10) : 0u) |
					(row._current11.Value.IsValid() ? (1u << 11) : 0u) |
					(row._current12.Value.IsValid() ? (1u << 12) : 0u) |
					(row._current13.Value.IsValid() ? (1u << 13) : 0u) |
					(row._current14.Value.IsValid() ? (1u << 14) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
				row._current1.Next();
				row._current2.Next();
				row._current3.Next();
				row._current4.Next();
				row._current5.Next();
				row._current6.Next();
				row._current7.Next();
				row._current8.Next();
				row._current9.Next();
				row._current10.Next();
				row._current11.Next();
				row._current12.Next();
				row._current13.Next();
				row._current14.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
			if ((m & (1u << 1)) != 0) row._current1.Next();
			if ((m & (1u << 2)) != 0) row._current2.Next();
			if ((m & (1u << 3)) != 0) row._current3.Next();
			if ((m & (1u << 4)) != 0) row._current4.Next();
			if ((m & (1u << 5)) != 0) row._current5.Next();
			if ((m & (1u << 6)) != 0) row._current6.Next();
			if ((m & (1u << 7)) != 0) row._current7.Next();
			if ((m & (1u << 8)) != 0) row._current8.Next();
			if ((m & (1u << 9)) != 0) row._current9.Next();
			if ((m & (1u << 10)) != 0) row._current10.Next();
			if ((m & (1u << 11)) != 0) row._current11.Next();
			if ((m & (1u << 12)) != 0) row._current12.Next();
			if ((m & (1u << 13)) != 0) row._current13.Next();
			if ((m & (1u << 14)) != 0) row._current14.Next();
            return true;
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
    }

    [SkipLocalsInit]
    public unsafe ref struct Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : IData<Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>, IQueryComponentAccess
        where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct
    {
        private const uint ALL_PRESENT_MASK = 0xFFFFu;

		private static readonly System.Type[] s_componentTypes = new[] { typeof(T0), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15) };
		public static System.ReadOnlySpan<System.Type> ReadComponents => s_componentTypes;
        public static System.ReadOnlySpan<System.Type> WriteComponents => s_componentTypes;

        internal int _index, _count;
        internal uint _presentMask;
        internal bool _anyAbsent;
        internal ReadOnlySpan<EntityView> _entities;
        internal DataRow<T0> _current0;
		internal DataRow<T1> _current1;
		internal DataRow<T2> _current2;
		internal DataRow<T3> _current3;
		internal DataRow<T4> _current4;
		internal DataRow<T5> _current5;
		internal DataRow<T6> _current6;
		internal DataRow<T7> _current7;
		internal DataRow<T8> _current8;
		internal DataRow<T9> _current9;
		internal DataRow<T10> _current10;
		internal DataRow<T11> _current11;
		internal DataRow<T12> _current12;
		internal DataRow<T13> _current13;
		internal DataRow<T14> _current14;
		internal DataRow<T15> _current15;

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
        public static void LoadChunk(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> row, QueryIterator iterator)
        {
            row._current0 = iterator.GetColumn<T0>(0);
			row._current1 = iterator.GetColumn<T1>(1);
			row._current2 = iterator.GetColumn<T2>(2);
			row._current3 = iterator.GetColumn<T3>(3);
			row._current4 = iterator.GetColumn<T4>(4);
			row._current5 = iterator.GetColumn<T5>(5);
			row._current6 = iterator.GetColumn<T6>(6);
			row._current7 = iterator.GetColumn<T7>(7);
			row._current8 = iterator.GetColumn<T8>(8);
			row._current9 = iterator.GetColumn<T9>(9);
			row._current10 = iterator.GetColumn<T10>(10);
			row._current11 = iterator.GetColumn<T11>(11);
			row._current12 = iterator.GetColumn<T12>(12);
			row._current13 = iterator.GetColumn<T13>(13);
			row._current14 = iterator.GetColumn<T14>(14);
			row._current15 = iterator.GetColumn<T15>(15);
            if (iterator.HasOptional)
            {
                row._presentMask =
                    (row._current0.Value.IsValid() ? (1u << 0) : 0u) |
					(row._current1.Value.IsValid() ? (1u << 1) : 0u) |
					(row._current2.Value.IsValid() ? (1u << 2) : 0u) |
					(row._current3.Value.IsValid() ? (1u << 3) : 0u) |
					(row._current4.Value.IsValid() ? (1u << 4) : 0u) |
					(row._current5.Value.IsValid() ? (1u << 5) : 0u) |
					(row._current6.Value.IsValid() ? (1u << 6) : 0u) |
					(row._current7.Value.IsValid() ? (1u << 7) : 0u) |
					(row._current8.Value.IsValid() ? (1u << 8) : 0u) |
					(row._current9.Value.IsValid() ? (1u << 9) : 0u) |
					(row._current10.Value.IsValid() ? (1u << 10) : 0u) |
					(row._current11.Value.IsValid() ? (1u << 11) : 0u) |
					(row._current12.Value.IsValid() ? (1u << 12) : 0u) |
					(row._current13.Value.IsValid() ? (1u << 13) : 0u) |
					(row._current14.Value.IsValid() ? (1u << 14) : 0u) |
					(row._current15.Value.IsValid() ? (1u << 15) : 0u);
                row._anyAbsent = row._presentMask != ALL_PRESENT_MASK;
            }
            else
            {
                row._anyAbsent = false;
            }
            row._entities = iterator.Entities();
            row._index = 0;
            row._count = iterator.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvance(ref Data<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> row)
        {
            if (++row._index >= row._count)
                return false;
            if (!row._anyAbsent)
            {
                row._current0.Next();
				row._current1.Next();
				row._current2.Next();
				row._current3.Next();
				row._current4.Next();
				row._current5.Next();
				row._current6.Next();
				row._current7.Next();
				row._current8.Next();
				row._current9.Next();
				row._current10.Next();
				row._current11.Next();
				row._current12.Next();
				row._current13.Next();
				row._current14.Next();
				row._current15.Next();
                return true;
            }
            var m = row._presentMask;
            if ((m & (1u << 0)) != 0) row._current0.Next();
			if ((m & (1u << 1)) != 0) row._current1.Next();
			if ((m & (1u << 2)) != 0) row._current2.Next();
			if ((m & (1u << 3)) != 0) row._current3.Next();
			if ((m & (1u << 4)) != 0) row._current4.Next();
			if ((m & (1u << 5)) != 0) row._current5.Next();
			if ((m & (1u << 6)) != 0) row._current6.Next();
			if ((m & (1u << 7)) != 0) row._current7.Next();
			if ((m & (1u << 8)) != 0) row._current8.Next();
			if ((m & (1u << 9)) != 0) row._current9.Next();
			if ((m & (1u << 10)) != 0) row._current10.Next();
			if ((m & (1u << 11)) != 0) row._current11.Next();
			if ((m & (1u << 12)) != 0) row._current12.Next();
			if ((m & (1u << 13)) != 0) row._current13.Next();
			if ((m & (1u << 14)) != 0) row._current14.Next();
			if ((m & (1u << 15)) != 0) row._current15.Next();
            return true;
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
    }

#endif
}

#pragma warning restore 1591

#pragma warning disable 1591
#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;

namespace TinyEcs
{
#if NET9_0_OR_GREATER
    public sealed partial class FuncSystem<TArg>
    {
        public FuncSystem<TArg> RunIf<T0>(Func<T0, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
                return condition(obj0);
            };
            _conditions.Add(fn);
            return this;
        }

        public FuncSystem<TArg> RunIf<T0, T1>(Func<T0, T1, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T1 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
			T1? obj1 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
                return condition(obj0, obj1);
            };
            _conditions.Add(fn);
            return this;
        }

        public FuncSystem<TArg> RunIf<T0, T1, T2>(Func<T0, T1, T2, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T1 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T2 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
                return condition(obj0, obj1, obj2);
            };
            _conditions.Add(fn);
            return this;
        }

        public FuncSystem<TArg> RunIf<T0, T1, T2, T3>(Func<T0, T1, T2, T3, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T1 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T2 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T3 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
                return condition(obj0, obj1, obj2, obj3);
            };
            _conditions.Add(fn);
            return this;
        }

        public FuncSystem<TArg> RunIf<T0, T1, T2, T3, T4>(Func<T0, T1, T2, T3, T4, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T1 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T2 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T3 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T4 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
                return condition(obj0, obj1, obj2, obj3, obj4);
            };
            _conditions.Add(fn);
            return this;
        }

        public FuncSystem<TArg> RunIf<T0, T1, T2, T3, T4, T5>(Func<T0, T1, T2, T3, T4, T5, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T1 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T2 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T3 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T4 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T5 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
                return condition(obj0, obj1, obj2, obj3, obj4, obj5);
            };
            _conditions.Add(fn);
            return this;
        }

        public FuncSystem<TArg> RunIf<T0, T1, T2, T3, T4, T5, T6>(Func<T0, T1, T2, T3, T4, T5, T6, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T1 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T2 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T3 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T4 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T5 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T6 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
                return condition(obj0, obj1, obj2, obj3, obj4, obj5, obj6);
            };
            _conditions.Add(fn);
            return this;
        }

        public FuncSystem<TArg> RunIf<T0, T1, T2, T3, T4, T5, T6, T7>(Func<T0, T1, T2, T3, T4, T5, T6, T7, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T1 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T2 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T3 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T4 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T5 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T6 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T7 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
                return condition(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7);
            };
            _conditions.Add(fn);
            return this;
        }

        public FuncSystem<TArg> RunIf<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T1 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T2 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T3 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T4 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T5 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T6 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T7 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T8 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
			T8? obj8 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
				obj8 ??= (T8)T8.Generate(args);
                return condition(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8);
            };
            _conditions.Add(fn);
            return this;
        }

        public FuncSystem<TArg> RunIf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T1 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T2 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T3 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T4 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T5 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T6 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T7 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T8 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T9 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
			T8? obj8 = null;
			T9? obj9 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
				obj8 ??= (T8)T8.Generate(args);
				obj9 ??= (T9)T9.Generate(args);
                return condition(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9);
            };
            _conditions.Add(fn);
            return this;
        }

        public FuncSystem<TArg> RunIf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T1 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T2 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T3 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T4 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T5 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T6 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T7 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T8 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T9 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T10 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
			T8? obj8 = null;
			T9? obj9 = null;
			T10? obj10 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
				obj8 ??= (T8)T8.Generate(args);
				obj9 ??= (T9)T9.Generate(args);
				obj10 ??= (T10)T10.Generate(args);
                return condition(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10);
            };
            _conditions.Add(fn);
            return this;
        }

        public FuncSystem<TArg> RunIf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T1 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T2 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T3 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T4 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T5 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T6 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T7 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T8 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T9 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T10 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T11 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
			T8? obj8 = null;
			T9? obj9 = null;
			T10? obj10 = null;
			T11? obj11 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
				obj8 ??= (T8)T8.Generate(args);
				obj9 ??= (T9)T9.Generate(args);
				obj10 ??= (T10)T10.Generate(args);
				obj11 ??= (T11)T11.Generate(args);
                return condition(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11);
            };
            _conditions.Add(fn);
            return this;
        }

        public FuncSystem<TArg> RunIf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T1 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T2 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T3 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T4 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T5 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T6 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T7 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T8 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T9 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T10 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T11 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T12 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
			T8? obj8 = null;
			T9? obj9 = null;
			T10? obj10 = null;
			T11? obj11 = null;
			T12? obj12 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
				obj8 ??= (T8)T8.Generate(args);
				obj9 ??= (T9)T9.Generate(args);
				obj10 ??= (T10)T10.Generate(args);
				obj11 ??= (T11)T11.Generate(args);
				obj12 ??= (T12)T12.Generate(args);
                return condition(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12);
            };
            _conditions.Add(fn);
            return this;
        }

        public FuncSystem<TArg> RunIf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T1 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T2 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T3 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T4 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T5 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T6 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T7 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T8 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T9 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T10 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T11 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T12 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T13 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
			T8? obj8 = null;
			T9? obj9 = null;
			T10? obj10 = null;
			T11? obj11 = null;
			T12? obj12 = null;
			T13? obj13 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
				obj8 ??= (T8)T8.Generate(args);
				obj9 ??= (T9)T9.Generate(args);
				obj10 ??= (T10)T10.Generate(args);
				obj11 ??= (T11)T11.Generate(args);
				obj12 ??= (T12)T12.Generate(args);
				obj13 ??= (T13)T13.Generate(args);
                return condition(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13);
            };
            _conditions.Add(fn);
            return this;
        }

        public FuncSystem<TArg> RunIf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T1 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T2 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T3 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T4 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T5 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T6 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T7 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T8 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T9 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T10 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T11 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T12 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T13 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T14 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
			T8? obj8 = null;
			T9? obj9 = null;
			T10? obj10 = null;
			T11? obj11 = null;
			T12? obj12 = null;
			T13? obj13 = null;
			T14? obj14 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
				obj8 ??= (T8)T8.Generate(args);
				obj9 ??= (T9)T9.Generate(args);
				obj10 ??= (T10)T10.Generate(args);
				obj11 ??= (T11)T11.Generate(args);
				obj12 ??= (T12)T12.Generate(args);
				obj13 ??= (T13)T13.Generate(args);
				obj14 ??= (T14)T14.Generate(args);
                return condition(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13, obj14);
            };
            _conditions.Add(fn);
            return this;
        }

        public FuncSystem<TArg> RunIf<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, bool> condition)
            where T0 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T1 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T2 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T3 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T4 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T5 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T6 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T7 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T8 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T9 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T10 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T11 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T12 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T13 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T14 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
			where T15 : class, ISystemParam<TArg>, IIntoSystemParam<TArg>
        {
            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
			T8? obj8 = null;
			T9? obj9 = null;
			T10? obj10 = null;
			T11? obj11 = null;
			T12? obj12 = null;
			T13? obj13 = null;
			T14? obj14 = null;
			T15? obj15 = null;
            var fn = (SystemTicks ticks, TArg args) => {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
				obj8 ??= (T8)T8.Generate(args);
				obj9 ??= (T9)T9.Generate(args);
				obj10 ??= (T10)T10.Generate(args);
				obj11 ??= (T11)T11.Generate(args);
				obj12 ??= (T12)T12.Generate(args);
				obj13 ??= (T13)T13.Generate(args);
				obj14 ??= (T14)T14.Generate(args);
				obj15 ??= (T15)T15.Generate(args);
                return condition(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13, obj14, obj15);
            };
            _conditions.Add(fn);
            return this;
        }

    }
#endif
}

#pragma warning restore 1591

#pragma warning disable 1591
#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace TinyEcs
{
#if NET9_0_OR_GREATER
    public partial class Scheduler
    {
        public ITinySystem OnStartup<T0>(Action<T0> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
                obj0.Lock(ticks);
                args.BeginDeferred();
                system(obj0);
                args.EndDeferred();
                obj0.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnStartup<T0, T1>(Action<T0, T1> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnStartup<T0, T1, T2>(Action<T0, T1, T2> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnStartup<T0, T1, T2, T3>(Action<T0, T1, T2, T3> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnStartup<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnStartup<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnStartup<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnStartup<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
			T8? obj8 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
				obj8 ??= (T8)T8.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T14 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
				obj14.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13, obj14);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
				obj14.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T14 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T15 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
				obj14.Lock(ticks);
				obj15.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13, obj14, obj15);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
				obj14.Unlock();
				obj15.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnFrameStart<T0>(Action<T0> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
                obj0.Lock(ticks);
                args.BeginDeferred();
                system(obj0);
                args.EndDeferred();
                obj0.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnFrameStart<T0, T1>(Action<T0, T1> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnFrameStart<T0, T1, T2>(Action<T0, T1, T2> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnFrameStart<T0, T1, T2, T3>(Action<T0, T1, T2, T3> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnFrameStart<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnFrameStart<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnFrameStart<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
			T8? obj8 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
				obj8 ??= (T8)T8.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T14 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
				obj14.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13, obj14);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
				obj14.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T14 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T15 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
				obj14.Lock(ticks);
				obj15.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13, obj14, obj15);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
				obj14.Unlock();
				obj15.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0>(Action<T0> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
                obj0.Lock(ticks);
                args.BeginDeferred();
                system(obj0);
                args.EndDeferred();
                obj0.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0, T1>(Action<T0, T1> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0, T1, T2>(Action<T0, T1, T2> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0, T1, T2, T3>(Action<T0, T1, T2, T3> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
			T8? obj8 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
				obj8 ??= (T8)T8.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T14 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
				obj14.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13, obj14);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
				obj14.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T14 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T15 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
				obj14.Lock(ticks);
				obj15.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13, obj14, obj15);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
				obj14.Unlock();
				obj15.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnUpdate<T0>(Action<T0> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
                obj0.Lock(ticks);
                args.BeginDeferred();
                system(obj0);
                args.EndDeferred();
                obj0.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnUpdate<T0, T1>(Action<T0, T1> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnUpdate<T0, T1, T2>(Action<T0, T1, T2> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnUpdate<T0, T1, T2, T3>(Action<T0, T1, T2, T3> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnUpdate<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnUpdate<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnUpdate<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
			T8? obj8 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
				obj8 ??= (T8)T8.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T14 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
				obj14.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13, obj14);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
				obj14.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T14 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T15 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
				obj14.Lock(ticks);
				obj15.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13, obj14, obj15);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
				obj14.Unlock();
				obj15.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0>(Action<T0> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
                obj0.Lock(ticks);
                args.BeginDeferred();
                system(obj0);
                args.EndDeferred();
                obj0.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0, T1>(Action<T0, T1> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0, T1, T2>(Action<T0, T1, T2> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0, T1, T2, T3>(Action<T0, T1, T2, T3> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
			T8? obj8 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
				obj8 ??= (T8)T8.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T14 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
				obj14.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13, obj14);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
				obj14.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T14 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T15 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
				obj14.Lock(ticks);
				obj15.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13, obj14, obj15);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
				obj14.Unlock();
				obj15.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0>(Action<T0> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
                obj0.Lock(ticks);
                args.BeginDeferred();
                system(obj0);
                args.EndDeferred();
                obj0.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0, T1>(Action<T0, T1> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0, T1, T2>(Action<T0, T1, T2> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0, T1, T2, T3>(Action<T0, T1, T2, T3> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
			T8? obj8 = null;
            var fn = (World args, SystemTicks ticks) =>
            {
                obj0 ??= (T0)T0.Generate(args);
				obj1 ??= (T1)T1.Generate(args);
				obj2 ??= (T2)T2.Generate(args);
				obj3 ??= (T3)T3.Generate(args);
				obj4 ??= (T4)T4.Generate(args);
				obj5 ??= (T5)T5.Generate(args);
				obj6 ??= (T6)T6.Generate(args);
				obj7 ??= (T7)T7.Generate(args);
				obj8 ??= (T8)T8.Generate(args);
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T14 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
				obj14.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13, obj14);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
				obj14.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> system, ThreadingMode? threadingType = null)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T8 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T9 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T10 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T11 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T12 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T13 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T14 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T15 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

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
            var fn = (World args, SystemTicks ticks) =>
            {
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
                obj0.Lock(ticks);
				obj1.Lock(ticks);
				obj2.Lock(ticks);
				obj3.Lock(ticks);
				obj4.Lock(ticks);
				obj5.Lock(ticks);
				obj6.Lock(ticks);
				obj7.Lock(ticks);
				obj8.Lock(ticks);
				obj9.Lock(ticks);
				obj10.Lock(ticks);
				obj11.Lock(ticks);
				obj12.Lock(ticks);
				obj13.Lock(ticks);
				obj14.Lock(ticks);
				obj15.Lock(ticks);
                args.BeginDeferred();
                system(obj0, obj1, obj2, obj3, obj4, obj5, obj6, obj7, obj8, obj9, obj10, obj11, obj12, obj13, obj14, obj15);
                args.EndDeferred();
                obj0.Unlock();
				obj1.Unlock();
				obj2.Unlock();
				obj3.Unlock();
				obj4.Unlock();
				obj5.Unlock();
				obj6.Unlock();
				obj7.Unlock();
				obj8.Unlock();
				obj9.Unlock();
				obj10.Unlock();
				obj11.Unlock();
				obj12.Unlock();
				obj13.Unlock();
				obj14.Unlock();
				obj15.Unlock();
                return true;
            };
            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public ITinySystem OnStartup(Action system, ThreadingMode? threadingType = null)
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

			var fn = (World args, SystemTicks ticks) =>
			{
				system();
                return true;
			};

            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Startup);
            return sys;
        }

        public ITinySystem OnFrameStart(Action system, ThreadingMode? threadingType = null)
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

			var fn = (World args, SystemTicks ticks) =>
			{
				system();
                return true;
			};

            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public ITinySystem OnBeforeUpdate(Action system, ThreadingMode? threadingType = null)
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

			var fn = (World args, SystemTicks ticks) =>
			{
				system();
                return true;
			};

            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public ITinySystem OnUpdate(Action system, ThreadingMode? threadingType = null)
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

			var fn = (World args, SystemTicks ticks) =>
			{
				system();
                return true;
			};

            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.Update);
            return sys;
        }

        public ITinySystem OnAfterUpdate(Action system, ThreadingMode? threadingType = null)
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

			var fn = (World args, SystemTicks ticks) =>
			{
				system();
                return true;
			};

            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public ITinySystem OnFrameEnd(Action system, ThreadingMode? threadingType = null)
        {
            if (!threadingType.HasValue)
                threadingType = ThreadingExecutionMode;

			var fn = (World args, SystemTicks ticks) =>
			{
				system();
                return true;
			};

            var sys = new TinyDelegateSystem(fn);
			sys.Configuration.ThreadingMode = threadingType;
            Add(sys, Stages.FrameEnd);
            return sys;
        }

    }
#endif
}

#pragma warning restore 1591

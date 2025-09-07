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
        public FuncSystem<World> OnStartup<T0>(Action<T0> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
            var checkInuse = () => obj0?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

                obj0 ??= (T0)T0.Generate(args);
                obj0.Lock(ticks);
                args.BeginDeferred();
                system(obj0);
                args.EndDeferred();
                obj0.Unlock();
                return true;
            };
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnStartup<T0, T1>(Action<T0, T1> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnStartup<T0, T1, T2>(Action<T0, T1, T2> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnStartup<T0, T1, T2, T3>(Action<T0, T1, T2, T3> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnStartup<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnStartup<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnStartup<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnStartup<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0|| obj14?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnStartup<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0|| obj14?.UseIndex != 0|| obj15?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0>(Action<T0> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
            var checkInuse = () => obj0?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

                obj0 ??= (T0)T0.Generate(args);
                obj0.Lock(ticks);
                args.BeginDeferred();
                system(obj0);
                args.EndDeferred();
                obj0.Unlock();
                return true;
            };
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0, T1>(Action<T0, T1> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0, T1, T2>(Action<T0, T1, T2> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0, T1, T2, T3>(Action<T0, T1, T2, T3> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0|| obj14?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnFrameStart<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0|| obj14?.UseIndex != 0|| obj15?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0>(Action<T0> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
            var checkInuse = () => obj0?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

                obj0 ??= (T0)T0.Generate(args);
                obj0.Lock(ticks);
                args.BeginDeferred();
                system(obj0);
                args.EndDeferred();
                obj0.Unlock();
                return true;
            };
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0, T1>(Action<T0, T1> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0, T1, T2>(Action<T0, T1, T2> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0, T1, T2, T3>(Action<T0, T1, T2, T3> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0|| obj14?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0|| obj14?.UseIndex != 0|| obj15?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0>(Action<T0> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
            var checkInuse = () => obj0?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

                obj0 ??= (T0)T0.Generate(args);
                obj0.Lock(ticks);
                args.BeginDeferred();
                system(obj0);
                args.EndDeferred();
                obj0.Unlock();
                return true;
            };
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0, T1>(Action<T0, T1> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0, T1, T2>(Action<T0, T1, T2> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0, T1, T2, T3>(Action<T0, T1, T2, T3> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0|| obj14?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0|| obj14?.UseIndex != 0|| obj15?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0>(Action<T0> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
            var checkInuse = () => obj0?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

                obj0 ??= (T0)T0.Generate(args);
                obj0.Lock(ticks);
                args.BeginDeferred();
                system(obj0);
                args.EndDeferred();
                obj0.Unlock();
                return true;
            };
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0, T1>(Action<T0, T1> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0, T1, T2>(Action<T0, T1, T2> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0, T1, T2, T3>(Action<T0, T1, T2, T3> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0|| obj14?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0|| obj14?.UseIndex != 0|| obj15?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0>(Action<T0> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
            var checkInuse = () => obj0?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

                obj0 ??= (T0)T0.Generate(args);
                obj0.Lock(ticks);
                args.BeginDeferred();
                system(obj0);
                args.EndDeferred();
                obj0.Unlock();
                return true;
            };
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0, T1>(Action<T0, T1> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0, T1, T2>(Action<T0, T1, T2> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0, T1, T2, T3>(Action<T0, T1, T2, T3> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> system, ThreadingMode threadingType = ThreadingMode.Auto)
            where T0 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T1 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T2 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T3 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T4 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T5 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T6 : class, ISystemParam<World>, IIntoSystemParam<World>
			where T7 : class, ISystemParam<World>, IIntoSystemParam<World>
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            T0? obj0 = null;
			T1? obj1 = null;
			T2? obj2 = null;
			T3? obj3 = null;
			T4? obj4 = null;
			T5? obj5 = null;
			T6? obj6 = null;
			T7? obj7 = null;
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0|| obj14?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> system, ThreadingMode threadingType = ThreadingMode.Auto)
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
            if (threadingType == ThreadingMode.Auto)
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
            var checkInuse = () => obj0?.UseIndex != 0|| obj1?.UseIndex != 0|| obj2?.UseIndex != 0|| obj3?.UseIndex != 0|| obj4?.UseIndex != 0|| obj5?.UseIndex != 0|| obj6?.UseIndex != 0|| obj7?.UseIndex != 0|| obj8?.UseIndex != 0|| obj9?.UseIndex != 0|| obj10?.UseIndex != 0|| obj11?.UseIndex != 0|| obj12?.UseIndex != 0|| obj13?.UseIndex != 0|| obj14?.UseIndex != 0|| obj15?.UseIndex != 0;
            var fn = (SystemTicks ticks, World args, Func<SystemTicks, World, bool> runIf) =>
            {
                if (runIf != null && !runIf.Invoke(ticks, args))
                    return false;

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
            var sys = new FuncSystem<World>(_world, fn, checkInuse, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

        public FuncSystem<World> OnStartup(Action system, ThreadingMode threadingType = ThreadingMode.Auto)
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            var sys = new FuncSystem<World>(_world, (ticks, args, runIf) =>
            {
                if (runIf?.Invoke(ticks, args) ?? true)
                {
                    system();
                    return true;
                }
                return false;
            }, () => false, Stages.Startup, threadingType);
            Add(sys, Stages.Startup);
            return sys;
        }

        public FuncSystem<World> OnFrameStart(Action system, ThreadingMode threadingType = ThreadingMode.Auto)
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            var sys = new FuncSystem<World>(_world, (ticks, args, runIf) =>
            {
                if (runIf?.Invoke(ticks, args) ?? true)
                {
                    system();
                    return true;
                }
                return false;
            }, () => false, Stages.FrameStart, threadingType);
            Add(sys, Stages.FrameStart);
            return sys;
        }

        public FuncSystem<World> OnBeforeUpdate(Action system, ThreadingMode threadingType = ThreadingMode.Auto)
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            var sys = new FuncSystem<World>(_world, (ticks, args, runIf) =>
            {
                if (runIf?.Invoke(ticks, args) ?? true)
                {
                    system();
                    return true;
                }
                return false;
            }, () => false, Stages.BeforeUpdate, threadingType);
            Add(sys, Stages.BeforeUpdate);
            return sys;
        }

        public FuncSystem<World> OnUpdate(Action system, ThreadingMode threadingType = ThreadingMode.Auto)
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            var sys = new FuncSystem<World>(_world, (ticks, args, runIf) =>
            {
                if (runIf?.Invoke(ticks, args) ?? true)
                {
                    system();
                    return true;
                }
                return false;
            }, () => false, Stages.Update, threadingType);
            Add(sys, Stages.Update);
            return sys;
        }

        public FuncSystem<World> OnAfterUpdate(Action system, ThreadingMode threadingType = ThreadingMode.Auto)
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            var sys = new FuncSystem<World>(_world, (ticks, args, runIf) =>
            {
                if (runIf?.Invoke(ticks, args) ?? true)
                {
                    system();
                    return true;
                }
                return false;
            }, () => false, Stages.AfterUpdate, threadingType);
            Add(sys, Stages.AfterUpdate);
            return sys;
        }

        public FuncSystem<World> OnFrameEnd(Action system, ThreadingMode threadingType = ThreadingMode.Auto)
        {
            if (threadingType == ThreadingMode.Auto)
                threadingType = ThreadingExecutionMode;

            var sys = new FuncSystem<World>(_world, (ticks, args, runIf) =>
            {
                if (runIf?.Invoke(ticks, args) ?? true)
                {
                    system();
                    return true;
                }
                return false;
            }, () => false, Stages.FrameEnd, threadingType);
            Add(sys, Stages.FrameEnd);
            return sys;
        }

    }
#endif
}

#pragma warning restore 1591

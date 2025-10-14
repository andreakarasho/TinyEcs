#pragma warning disable 1591
#nullable enable

using System;
using System.Collections.Generic;

namespace TinyEcs
{
    public partial class World
    {
        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0>()
            where T0 : struct
            => Archetype(Component<T0>());

        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0, T1>()
            where T0 : struct where T1 : struct
            => Archetype(Component<T0>(), Component<T1>());

        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0, T1, T2>()
            where T0 : struct where T1 : struct where T2 : struct
            => Archetype(Component<T0>(), Component<T1>(), Component<T2>());

        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0, T1, T2, T3>()
            where T0 : struct where T1 : struct where T2 : struct where T3 : struct
            => Archetype(Component<T0>(), Component<T1>(), Component<T2>(), Component<T3>());

        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0, T1, T2, T3, T4>()
            where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct
            => Archetype(Component<T0>(), Component<T1>(), Component<T2>(), Component<T3>(), Component<T4>());

        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0, T1, T2, T3, T4, T5>()
            where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
            => Archetype(Component<T0>(), Component<T1>(), Component<T2>(), Component<T3>(), Component<T4>(), Component<T5>());

        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0, T1, T2, T3, T4, T5, T6>()
            where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
            => Archetype(Component<T0>(), Component<T1>(), Component<T2>(), Component<T3>(), Component<T4>(), Component<T5>(), Component<T6>());

        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0, T1, T2, T3, T4, T5, T6, T7>()
            where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct
            => Archetype(Component<T0>(), Component<T1>(), Component<T2>(), Component<T3>(), Component<T4>(), Component<T5>(), Component<T6>(), Component<T7>());

        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0, T1, T2, T3, T4, T5, T6, T7, T8>()
            where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct
            => Archetype(Component<T0>(), Component<T1>(), Component<T2>(), Component<T3>(), Component<T4>(), Component<T5>(), Component<T6>(), Component<T7>(), Component<T8>());

        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>()
            where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct
            => Archetype(Component<T0>(), Component<T1>(), Component<T2>(), Component<T3>(), Component<T4>(), Component<T5>(), Component<T6>(), Component<T7>(), Component<T8>(), Component<T9>());

        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
            where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct
            => Archetype(Component<T0>(), Component<T1>(), Component<T2>(), Component<T3>(), Component<T4>(), Component<T5>(), Component<T6>(), Component<T7>(), Component<T8>(), Component<T9>(), Component<T10>());

        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>()
            where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct
            => Archetype(Component<T0>(), Component<T1>(), Component<T2>(), Component<T3>(), Component<T4>(), Component<T5>(), Component<T6>(), Component<T7>(), Component<T8>(), Component<T9>(), Component<T10>(), Component<T11>());

        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>()
            where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct
            => Archetype(Component<T0>(), Component<T1>(), Component<T2>(), Component<T3>(), Component<T4>(), Component<T5>(), Component<T6>(), Component<T7>(), Component<T8>(), Component<T9>(), Component<T10>(), Component<T11>(), Component<T12>());

        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>()
            where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct
            => Archetype(Component<T0>(), Component<T1>(), Component<T2>(), Component<T3>(), Component<T4>(), Component<T5>(), Component<T6>(), Component<T7>(), Component<T8>(), Component<T9>(), Component<T10>(), Component<T11>(), Component<T12>(), Component<T13>());

        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>()
            where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct
            => Archetype(Component<T0>(), Component<T1>(), Component<T2>(), Component<T3>(), Component<T4>(), Component<T5>(), Component<T6>(), Component<T7>(), Component<T8>(), Component<T9>(), Component<T10>(), Component<T11>(), Component<T12>(), Component<T13>(), Component<T14>());

        /// <inheritdoc cref="World.Archetype(Span{EcsID})"/>
        public Archetype Archetype<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>()
            where T0 : struct where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where T7 : struct where T8 : struct where T9 : struct where T10 : struct where T11 : struct where T12 : struct where T13 : struct where T14 : struct where T15 : struct
            => Archetype(Component<T0>(), Component<T1>(), Component<T2>(), Component<T3>(), Component<T4>(), Component<T5>(), Component<T6>(), Component<T7>(), Component<T8>(), Component<T9>(), Component<T10>(), Component<T11>(), Component<T12>(), Component<T13>(), Component<T14>(), Component<T15>());

    }
}

#pragma warning restore 1591

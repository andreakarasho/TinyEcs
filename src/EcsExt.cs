using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyEcs;

sealed partial class World
{
    public void Attach<T>(int entity) where T : struct 
        => Attach(entity, in Component<T>.Metadata);

    public unsafe void Set<T>(int entity, T component) where T : struct
    {
        var span = MemoryMarshal.CreateSpan(ref component, 1);
        Set(entity, in Component<T>.Metadata, MemoryMarshal.AsBytes(span));
    }

    public unsafe bool Has<T>(int entity) where T : struct
        => Has(entity, in Component<T>.Metadata);

    public unsafe ref T Get<T>(int entity) where T : struct
    {
        var raw = Get(entity, in Component<T>.Metadata);
        return ref MemoryMarshal.AsRef<T>(raw);
    }


    public unsafe int RegisterSystem<T0>(delegate* managed<in EcsView, int, void> system)
        where T0 : struct
    => RegisterSystem(system, stackalloc ComponentMetadata[]
    {
        Component<T0>.Metadata
    });

    public unsafe int RegisterSystem<T0, T1>(delegate* managed<in EcsView, int, void> system)
        where T0 : struct
        where T1 : struct
    => RegisterSystem(system, stackalloc ComponentMetadata[]
    {
        Component<T0>.Metadata,
        Component<T1>.Metadata
    });

    public unsafe int RegisterSystem<T0, T1, T2>(delegate* managed<in EcsView, int, void> system)
        where T0 : struct
        where T1 : struct
        where T2 : struct
    => RegisterSystem(system, stackalloc ComponentMetadata[]
    {
        Component<T0>.Metadata,
        Component<T1>.Metadata,
        Component<T2>.Metadata
    });
}

public static class EcsViewExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TComponent Get<TComponent>(in this EcsView view, int column, int row) where TComponent : struct
    {
        var span = view.ComponentArrays[view.SignatureToIndex[column]]
                       .AsSpan(view.ComponentSizes[column] * row, view.ComponentSizes[column]);
        return ref MemoryMarshal.AsRef<TComponent>(span);
    }
}

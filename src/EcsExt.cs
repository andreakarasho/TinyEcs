using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyEcs;

sealed partial class World
{
    public void Attach<T>(int entity) where T : struct
    {
        var componentID = RegisterComponent<T>();
        Attach(entity, componentID);
    }

    public unsafe int RegisterSystem<T0>(delegate* managed<in EcsView, int, void> system)
        where T0 : struct
        => RegisterSystem(system, stackalloc int[]
        {
        _componentTypeIndex[typeof(T0)]
    });

    public unsafe int RegisterSystem<T0, T1>(delegate* managed<in EcsView, int, void> system)
        where T0 : struct
        where T1 : struct
        => RegisterSystem(system, stackalloc int[]
        {
        _componentTypeIndex[typeof(T0)],
        _componentTypeIndex[typeof(T1)]
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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyEcs;

public sealed partial class World
{
    public int Attach<T>(int entity) where T : struct
        => Attach(entity, _storage.GetOrCreateID<T>());

    public int Detach<T>(int entity) where T : struct
        => Detach(entity, _storage.GetOrCreateID<T>());

    public void Set<T>(int entity, T component) where T : struct
        => Set(entity, _storage.GetOrCreateID<T>(), MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref component, 1)));

    public int Tag(int entity, int componentID)
        => Attach(entity, componentID);

    public void UnTag(int entity, int componentID)
        => Detach(entity, componentID);

    public int GetComponent<T>() where T : struct
        => _storage.GetOrCreateID<T>();


    //public int CreateComponent<T>() where T : struct
    //    => Component<T>.Metadata.ID;

    public unsafe bool Has<T>(int entity) where T : struct
        => Has(entity, _storage.GetOrCreateID<T>());

    public unsafe ref T Get<T>(int entity) where T : struct
    {
        var raw = Get(entity, _storage.GetOrCreateID<T>());
        return ref MemoryMarshal.AsRef<T>(raw);
    }


    //public unsafe int RegisterSystem<T0>(delegate* managed<in EcsView, int, void> system)
    //    where T0 : struct
    //=> RegisterSystem(system, stackalloc ComponentMetadata[]
    //{
    //    Component<T0>.Instance.Metadata
    //});

    //public unsafe int RegisterSystem<T0, T1>(delegate* managed<in EcsView, int, void> system)
    //    where T0 : struct
    //    where T1 : struct
    //=> RegisterSystem(system, stackalloc ComponentMetadata[]
    //{
    //    Component<T0>.Instance.Metadata,
    //    Component<T1>.Instance.Metadata
    //});

    //public unsafe int RegisterSystem<T0, T1, T2>(delegate* managed<in EcsView, int, void> system)
    //    where T0 : struct
    //    where T1 : struct
    //    where T2 : struct
    //=> RegisterSystem(system, stackalloc ComponentMetadata[]
    //{
    //    Component<T0>.Instance.Metadata,
    //    Component<T1>.Instance.Metadata,
    //    Component<T2>.Instance.Metadata
    //});
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

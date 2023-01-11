using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyEcs;

public sealed partial class World : IDisposable
{
    private int _nextEntityID = 1;
    private int _entityCount = 0;
    private Archetype _archRoot;

    private readonly ConcurrentStack<int> _recycleIds = new ConcurrentStack<int>();

    internal readonly Dictionary<int, EcsRecord> _entityIndex = new Dictionary<int, EcsRecord>();
    internal readonly Dictionary<EcsSignature, Archetype> _typeIndex = new Dictionary<EcsSignature, Archetype>();
    internal readonly Dictionary<int, EcsSystem> _systemIndex = new Dictionary<int, EcsSystem>();


    public World()
    {
        _archRoot = new Archetype(this, new EcsSignature(0));
    }


    public int EntityCount => _entityCount;


    private unsafe void Destroy()
    {
        foreach ((var type, var arch) in _typeIndex)
        {
            type.Dispose();
        }

        _systemIndex.Clear();
        _recycleIds.Clear();
        _typeIndex.Clear();

        _entityCount = 0;
        _nextEntityID = 1;
        _archRoot = new Archetype(this, new EcsSignature(0));
    }

    public void Dispose() => Destroy();


    public int CreateEntity()
    {
        if (!_recycleIds.TryPop(out var id))
        {
            id = _nextEntityID;
            Interlocked.Increment(ref _nextEntityID);
        }

        var row = _archRoot.Add(id);
        ref var record = ref CollectionsMarshal.GetValueRefOrAddDefault(_entityIndex, id, out var _);
        record.Archetype = _archRoot;
        record.Row = row;
        ++_entityCount;

        return id;
    }

    public void Destroy(int entity)
    {
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_entityIndex, entity);
        if (Unsafe.IsNullRef(ref record))
        {
            return;
        }

        var removedId = record.Archetype.Remove(record.Row);

        Debug.Assert(removedId == entity);

        _recycleIds.Push(removedId);
        _entityIndex.Remove(removedId);
        --_entityCount;
    }

    public unsafe void Step()
    {
        foreach ((int id, EcsSystem system) in _systemIndex)
        {
            if (system.Archetype == null)
            {
                UpdateSystem(system);
            }

            system.Archetype?.StepHelp(system.Components.AsSpan(), system.Func);
        }
    }

    public IQueryComposition Query() => new Query(this);

    private void Attach(int entity, in ComponentMetadata componentID)
    {
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_entityIndex, entity);
        if (Unsafe.IsNullRef(ref record))
        {
            return;
        }

        var initType = record.Archetype.Signature;

        //Span<ComponentMetadata> newMeta = stackalloc ComponentMetadata[initType.Components.Count + 1];
        ////newMeta[0..^1];
        //newMeta[^1] = componentID;
        //newMeta.Sort();

        //var hash = ComponentHasher.Calculate(newMeta);

        var finiType = new EcsSignature(initType);
        finiType.Add(in componentID);

        if (!_typeIndex.TryGetValue(finiType, out var arch))
        {
            arch = _archRoot.InsertVertex(record.Archetype, finiType, componentID);
        }
        else
        {
            finiType.Dispose();
        }

        var newRow = Archetype.MoveEntity(record.Archetype, arch, record.Row);
        record.Row = newRow;
        record.Archetype = arch;
    }

    private void Detach(int entity, in ComponentMetadata componentID)
    {
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_entityIndex, entity);
        if (Unsafe.IsNullRef(ref record))
        {
            return;
        }

        var initType = record.Archetype.Signature;
        var finiType = new EcsSignature(initType);
        finiType.Remove(in componentID);

        if (!_typeIndex.TryGetValue(finiType, out var arch))
        {
            arch = _archRoot.InsertVertex(record.Archetype, finiType, componentID);
        }
        else
        {
            finiType.Dispose();
        }

        var newRow = Archetype.MoveEntity(record.Archetype, arch, record.Row);
        record.Row = newRow;
        record.Archetype = arch;
    }

    private void Set(int entity, in ComponentMetadata metadata, ReadOnlySpan<byte> data)
    {
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_entityIndex, entity);
        if (Unsafe.IsNullRef(ref record))
        {
            return;
        }

        var column = metadata.ID >= record.Archetype.Lookup.Length ? -1 : record.Archetype.Lookup[metadata.ID];
        if (column == -1)
        {
            return;
        }

        var componentData = record.Archetype._components[column]
            .AsSpan(metadata.Size * record.Row, metadata.Size);
        data.CopyTo(componentData);
    }

    private bool Has(int entity, in ComponentMetadata metadata) => !Get(entity, in metadata).IsEmpty;

    private Span<byte> Get(int entity, in ComponentMetadata metadata)
    {
        Debug.Assert(metadata.ID > 0);
        Debug.Assert(metadata.Size > 0);

        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_entityIndex, entity);
        if (Unsafe.IsNullRef(ref record))
        {
            return Span<byte>.Empty;
        }

        var column = metadata.ID >= record.Archetype.Lookup.Length ? -1 : record.Archetype.Lookup[metadata.ID];
        if (column == -1)
        {
            return Span<byte>.Empty;
        }

        return record.Archetype._components[column]
            .AsSpan(metadata.Size * record.Row, metadata.Size);
    }

    private unsafe int RegisterSystem(delegate* managed<in EcsView, int, void> system, ReadOnlySpan<ComponentMetadata> components)
    {
        var type = new EcsSignature(components);
        if (!_typeIndex.TryGetValue(type, out var arch))
        {
            arch = _archRoot.TraverseAndCreate(type);
        }
        else
        {
            type.Dispose();
        }

        if (!_recycleIds.TryPop(out var id))
        {
            id = _nextEntityID;
            Interlocked.Increment(ref _nextEntityID);
        }

        ref var sys = ref CollectionsMarshal.GetValueRefOrAddDefault(_systemIndex, id, out var exists);
        if (!exists)
            sys = new EcsSystem();

        sys!.Archetype = arch;
        sys.Components = components.ToArray();
        sys.Func = system;

        return id;
    }

    private unsafe void UpdateSystem(EcsSystem sys)
    {
        var type = new EcsSignature(sys.Components);
        if (!_typeIndex.TryGetValue(type, out var arch))
        {
            arch = _archRoot.TraverseAndCreate(type);
        }
        else
        {
            type.Dispose();
        }

        sys.Archetype = arch;
    }
}

sealed unsafe class Archetype
{
    const int ARCHETYPE_INITIAL_CAPACITY = 16;

    private readonly World _world;

    private int _capacity, _count;
    private int[] _entityIDs;
    internal byte[][] _components;
    private readonly EcsSignature _sign;
    private List<EcsEdge> _edgesLeft, _edgesRight;
    private readonly int[] _lookup;

    public Archetype(World world, EcsSignature sign)
    {
        _world = world;
        _capacity = ARCHETYPE_INITIAL_CAPACITY;
        _count = 0;
        _sign = sign;
        _entityIDs = new int[ARCHETYPE_INITIAL_CAPACITY];
        _components = new byte[sign.Count][];
        _edgesLeft = new List<EcsEdge>();
        _edgesRight = new List<EcsEdge>();

        var maxID = 0;
        for (int i = 0; i < sign.Count; ++i)
        {
            maxID = Math.Max(maxID, sign[i].ID);
        }

        _lookup = new int[maxID + 1];
        _lookup.AsSpan().Fill(-1);
        for (int i = 0; i < sign.Count; ++i)
        {
            _lookup[sign[i].ID] = i;
        }

        ResizeComponentArray(ARCHETYPE_INITIAL_CAPACITY);

        world._typeIndex[sign] = this;
    }


    public EcsSignature Signature => _sign;
    public int Count => _count;
    public int[] Entities => _entityIDs;
    public int[] Lookup => _lookup;



    public int Add(int entityID)
    {
        if (_capacity == _count)
        {
            Array.Resize(ref _entityIDs, _capacity * 2);
            ResizeComponentArray(_capacity * 2);
        }

        _entityIDs[_count] = entityID;

        return _count++;
    }

    public int Remove(int row)
    {
        var removed = _entityIDs[row];
        _entityIDs[row] = _entityIDs[_count - 1];

        for (int i = 0; i < _sign.Count; ++i)
        {
            ref readonly var meta = ref _sign[i];
            var leftArray = _components[i].AsSpan();

            var removeComponent = leftArray.Slice(meta.Size * row, meta.Size);
            var swapComponent = leftArray.Slice(meta.Size * (_count - 1), meta.Size);

            swapComponent.CopyTo(removeComponent);
        }

        --_count;

        return removed;
    }

    public Archetype InsertVertex(Archetype left, EcsSignature newType, in ComponentMetadata componentID)
    {
        var vertex = new Archetype(_world, newType);
        MakeEdges(left, vertex, componentID);
        InsertVertex(vertex);
        return vertex;
    }

    public static int MoveEntity(Archetype from, Archetype to, int fromRow)
    {
        var removed = from._entityIDs[fromRow];
        from._entityIDs[fromRow] = from._entityIDs[from._count - 1];

        var toRow = to.Add(removed);

        Copy(from, fromRow, to, toRow);     

        --from._count;

        return toRow;
    }

    static void Copy(Archetype from, int fromRow, Archetype to, int toRow)
    {
        for (int i = 0; i < from._sign.Count; ++i)
        {
            for (int j = 0; j < to._sign.Count; ++j)
            {
                if (from._sign[i] == to._sign[j])
                {
                    ref readonly var meta = ref from._sign[i];
                    var leftArray = from._components[i].AsSpan();
                    var rightArray = to._components[j].AsSpan();
                    var insertComponent = rightArray.Slice(meta.Size * toRow, meta.Size);
                    var removeComponent = leftArray.Slice(meta.Size * fromRow, meta.Size);
                    var swapComponent = leftArray.Slice(meta.Size * (from._count - 1), meta.Size);
                    removeComponent.CopyTo(insertComponent);
                    swapComponent.CopyTo(removeComponent);
                    break;
                }
            }
        }
    }

    public Archetype TraverseAndCreate(EcsSignature type)
    {
        var len = type.Count;
        Span<ComponentMetadata> acc = stackalloc ComponentMetadata[len];
        type.Components.CopyTo(acc);

        return TraverseAndCreateHelp(this, in type, len, acc, this);
    }

    public void StepHelp(ReadOnlySpan<ComponentMetadata> components, delegate* managed<in EcsView, int, void> run)
    {
        if (_count == 0)
            return;

        Span<int> signatureToIndex = stackalloc int[components.Length];
        Span<int> componentSizes = stackalloc int[components.Length];

        for (int slow = 0; slow < components.Length; ++slow)
        {
            var typeLen = _sign.Count;
            for (int fast = 0; fast < typeLen; ++fast)
            {
                ref readonly var component = ref _sign[fast];

                if (component == components[slow])
                {
                    componentSizes[slow] = component.Size;
                    signatureToIndex[slow] = fast;

                    break;
                }
            }
        }

        var view = new EcsView()
        {
            ComponentArrays = _components,
            SignatureToIndex = signatureToIndex,
            ComponentSizes = componentSizes,
        };

        for (int i = 0; i < _count; ++i)
        {
            run(in view, i);
        }

        foreach (ref var edge in CollectionsMarshal.AsSpan(_edgesRight))
        {
            if (components.IndexOf(edge.ComponentID) != -1)
                edge.Archetype.StepHelp(components, run);
        }
    }

    private static Archetype TraverseAndCreateHelp(Archetype vertex, in EcsSignature type, int stack, Span<ComponentMetadata> acc, Archetype root)
    {
        if (stack == 0)
        {
            return vertex;
        }

        if (vertex._edgesRight == null || vertex._edgesRight.Count == 0)
        {
            var nt = new EcsSignature(type.Count);
            for (int i = 0; i < acc.Length; ++i)
            {
                nt.Add(acc[i]);
            }
            return new Archetype(vertex._world, nt);
        }

        for (int i = 0; i < vertex._edgesRight.Count; i++)
        {
            var edge = vertex._edgesRight[i];
            if (type.IndexOf(edge.ComponentID) != -1)
            {
                acc[stack - 1] = edge.ComponentID;
                return TraverseAndCreateHelp(edge.Archetype, in type, stack - 1, acc, root);
            }
        }

        var newType = new EcsSignature(acc);
        var newComponent = ComponentMetadata.Invalid;
        for (int i = 0; i < type.Count; ++i)
        {
            if (type[i] != newType[i])
            {
                newComponent = type[i];
                newType.Add(newComponent);
                acc[stack - 1] = newComponent;
                break;
            }
        }

        if (newType.Count == 0)
        {
            newType.Dispose();
            return new Archetype(vertex._world, type);
        }

        var newVertex = root.InsertVertex(vertex, newType, newComponent);

        return TraverseAndCreateHelp(newVertex, in type, stack - 1, acc, root);
    }

    private static void MakeEdges(Archetype left, Archetype right, in ComponentMetadata componentID)
    {
        left._edgesRight.Add(new EcsEdge() { Archetype = right, ComponentID = componentID });
        right._edgesLeft.Add(new EcsEdge() { Archetype = left, ComponentID = componentID });
    }

    private void InsertVertex(Archetype newNode)
    {
        var nodeTypeLen = _sign.Count;
        var newTypeLen = newNode._sign.Count;

        if (nodeTypeLen > newTypeLen - 1)
        {
            return;
        }

        if (nodeTypeLen < newTypeLen - 1)
        {
            foreach (ref var edge in CollectionsMarshal.AsSpan(_edgesRight))
            {
                edge.Archetype.InsertVertex(newNode);
            }

            return;
        }

        if (!_sign.IsSuperset(in newNode._sign))
        {
            return;
        }

        var i = 0;
        var newNodeTypeLen = newNode._sign.Count;
        for (; i < newNodeTypeLen && _sign[i] == newNode._sign[i]; ++i) { }

        MakeEdges(newNode, this, _sign[i]);
    }

    private void ResizeComponentArray(int capacity)
    {
        for (int i = 0; i < _sign.Count; ++i)
        {
            ref readonly var meta = ref _sign[i];
            Array.Resize(ref _components[i], meta.Size * capacity);
            _capacity = capacity;
        }
    }
}

public struct Query : IQueryComposition, IQuery
{
    private readonly World _world;
    internal EcsSignature _add, _remove;
    internal readonly List<Archetype> _archetypes;

    public Query(World world)
    {
        _world = world;
        _archetypes = new List<Archetype>();
        _add = new EcsSignature(16);
        _remove = new EcsSignature(16);
    }


    public IQueryComposition With<T>() where T : struct
    {
        _add.Add(Component<T>.Metadata);

        return this;
    }

    public IQueryComposition Without<T>() where T : struct
    {
        _remove.Add(Component<T>.Metadata);

        return this;
    }

    public IQuery End()
    {
        _archetypes.Clear();

        // this check if all components are contained into the archetypes
        foreach ((var t, var arch) in _world._typeIndex)
        {
            if (arch.Count > 0 && t.IsSuperset(in _add))
            {
                var ok = true;
                foreach (ref readonly var component in _remove)
                {
                    if (t.IndexOf(in component) >= 0)
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok)
                    _archetypes.Add(arch);
            }
        }

        return this;
    }

    public QueryIterator GetEnumerator()
    {
        return new QueryIterator(_archetypes, _add);
    }
}

public interface IQuery
{
    QueryIterator GetEnumerator();
}

public interface IQueryComposition
{
    IQueryComposition With<T>() where T : struct;
    IQueryComposition Without<T>() where T : struct;
    IQuery End();
}

public ref struct QueryIterator
{
    private int _index;
    private readonly IEnumerator<Archetype> _archetypes;
    private EcsSignature _add;
    private ref int _firstEntity;
    private byte[][] _components;
    private int[] _columns;

    internal QueryIterator(List<Archetype> archetypes, EcsSignature add)
    {
        _index = 0;
        _archetypes = archetypes.GetEnumerator();
        _add = add;
    }

    public readonly EcsQueryView Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new EcsQueryView
        (
            ref Unsafe.Subtract(ref _firstEntity, _index),
            _index,
            _components,
            _columns
        );
    }

    public bool MoveNext()
    {
        --_index;

        if (_index >= 0) return true;

        Archetype archetype;
        do
        {
            if (!_archetypes.MoveNext()) return false;

            archetype = _archetypes.Current;
            _index = archetype.Count - 1;

        } while (_index < 0);

        _firstEntity = ref MemoryMarshal.GetReference(archetype.Entities.AsSpan(_index));
        _columns = archetype.Lookup;
        _components = archetype._components;

        return true;
    }

    public void Reset()
    {
        _index = -1;
        _archetypes.Reset();
    }
}

public readonly ref struct EcsQueryView
{
    public readonly ref readonly int Entity;

    private readonly int _row;
    private readonly byte[][] _componentArrays;
    private readonly int[] _columns;

    internal EcsQueryView(ref int entity, int row, byte[][] _components, int[] columns)
    {
        Entity = ref entity;
        _row = row;
        _componentArrays = _components;
        _columns = columns;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has<T>() where T : struct
    {
        ref readonly var meta = ref Component<T>.Metadata;

        return meta.ID < _columns.Length && _columns[meta.ID] >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T Get<T>() where T : struct
    {
        ref readonly var meta = ref Component<T>.Metadata;

        var span = _componentArrays[_columns[meta.ID]]
            .AsSpan(meta.Size * _row, meta.Size);

        return ref MemoryMarshal.AsRef<T>(span);
    }
}


record struct EcsRecord(Archetype Archetype, int Row);

sealed class EcsSignature : IEquatable<EcsSignature>, IDisposable
{
    private ComponentMetadata[] _components;
    private int _count, _capacity;

    public EcsSignature(int capacity)
    {
        _capacity = capacity;
        _components = capacity <= 0 ? Array.Empty<ComponentMetadata>(): new ComponentMetadata[capacity];
    }

    public EcsSignature(ReadOnlySpan<ComponentMetadata> components)
    {
        _capacity = components.Length;
        _count = components.Length;
        _components = new ComponentMetadata[components.Length];
        components.CopyTo(_components);

        Array.Sort(_components, 0, _count);
    }

    public EcsSignature(in EcsSignature other)
    {
        _capacity = other._capacity;
        _count = other._count;
        _components = new ComponentMetadata[other._components.Length];
        other._components.CopyTo(_components, 0);

        Array.Sort(_components, 0, _count);
    }


    public int Count => _count;
    public ReadOnlySpan<ComponentMetadata> Components => _components.AsSpan(0, _count);
    public ref readonly ComponentMetadata this[int index] => ref _components[index];



    public void Add(in ComponentMetadata id)
    {
        GrowIfNeeded();

        _components[_count++] = id;
        Array.Sort(_components, 0, _count);
    }

    public void Remove(in ComponentMetadata id)
    {
        var idx = IndexOf(in id);
        if (idx < 0 || _count <= 0) return;

        _components[idx] = _components[--_count];
        Array.Sort(_components, 0, _count);
    }

    public int IndexOf(in ComponentMetadata id) => Array.IndexOf(_components, id);

    public bool IsSuperset(in EcsSignature other)
    {
        int i = 0, j = 0;
        while (i < Count && j < other.Count)
        {
            if (_components[i] == other._components[j])
            {
                j++;
            }

            i++;
        }

        return j == other.Count;
    }

    public bool Equals([NotNull] EcsSignature other)
    {
        if (Count != other.Count)
        {
            return false;
        }

        for (int i = 0, count = Count; i < count; ++i)
        {
            if (_components[i] != other._components[i])
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 5381;

            ref var f0 = ref MemoryMarshal.GetReference<ComponentMetadata>(_components);

            for (int i = 0; i < Count; ++i)
            {
                ref readonly var id = ref Unsafe.Add(ref f0, i);
                hash = ((hash << 5) + hash) + id.ID;
            }

            return hash;
        }
    }

    private void GrowIfNeeded()
    {
        if (_count == _capacity)
        {
            if (_capacity == 0) _capacity = 1;

            _capacity *= 2;
            Array.Resize(ref _components, _capacity);
        }
    }

    public Span<ComponentMetadata>.Enumerator GetEnumerator() => _components.AsSpan(0, _count).GetEnumerator();

    public void Dispose()
    {
        if (_components != null)
        {
            _count = 0;
            _components = Array.Empty<ComponentMetadata>();
        }    
    }
}

readonly record struct EcsEdge(in ComponentMetadata ComponentID, Archetype Archetype);

unsafe class EcsSystem
{
    public Archetype? Archetype;
    public ComponentMetadata[]? Components;
    public delegate* managed<in EcsView, int, void> Func;
}

public ref struct EcsView
{
    internal byte[][] ComponentArrays;
    internal Span<int> SignatureToIndex;
    internal Span<int> ComponentSizes;
}


[SkipLocalsInit]
[StructLayout(LayoutKind.Sequential)]
readonly record struct ComponentMetadata(int ID, int Size) :
    IComparable<ComponentMetadata>,
    IEquatable<ComponentMetadata>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int CompareTo(ComponentMetadata other) => ID.CompareTo(other.ID);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(ComponentMetadata other) => ID == other.ID;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly override int GetHashCode() => ID.GetHashCode();


    public static readonly ComponentMetadata Invalid = new ComponentMetadata(-1, -1);
}

static class Component<T> where T : struct
{
    private static ComponentMetadata _meta = ComponentStorage.Create<T>();
    public static ref readonly ComponentMetadata Metadata => ref _meta;
}

static class ComponentStorage
{
    private static readonly Dictionary<int, ComponentMetadata> _components = new Dictionary<int, ComponentMetadata>();
    private static readonly Dictionary<Type, ComponentMetadata> _componentsByType = new Dictionary<Type, ComponentMetadata>();

    public static ref readonly ComponentMetadata Create<T>()
    {
        ref var meta = ref CollectionsMarshal.GetValueRefOrAddDefault(_componentsByType, typeof(T), out var exists);
        if (!exists)
        {
            meta = new ComponentMetadata(ComponentIDGen.Next(), Unsafe.SizeOf<T>());
            _components.Add(meta.ID, meta);
        }


        return ref meta;
    }

    public static ref readonly ComponentMetadata Get(int id)
    {
        ref var meta = ref CollectionsMarshal.GetValueRefOrNullRef(_components, id);

        if (Unsafe.IsNullRef(ref meta))
        {
            Debug.Fail("invalid component");
        }

        Debug.Assert(meta.ID > 0);
        Debug.Assert(meta.Size > 0);

        return ref meta;
    }
}

static class ComponentIDGen
{
    private static int _next = 1;
    public static int Next() => _next++;
}

static class ComponentHasher
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Calculate(Span<ComponentMetadata> components)
    {
        unchecked
        {
            var hash = 5381;

            foreach (ref var id in components)
            {
                hash = ((hash << 5) + hash) + id.ID;
            }

            return hash;
        }
    }
}
﻿using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyEcs;

sealed partial class World
{
    private int _nextEntityID = 1;
    private int _entityCount = 0;
    private readonly Archetype _archRoot;

    private readonly ConcurrentStack<int> _recycleIds = new ConcurrentStack<int>();

    internal readonly Dictionary<int, EcsRecord> _entityIndex = new Dictionary<int, EcsRecord>();
    internal readonly Dictionary<EcsType, Archetype> _typeIndex = new Dictionary<EcsType, Archetype>();
    internal readonly Dictionary<int, EcsSystem> _systemIndex = new Dictionary<int, EcsSystem>();


    public World()
    {
        _archRoot = new Archetype(this, new EcsType(0));
    }


    public int EntityCount => _entityCount;


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

        var initType = record.Archetype.EcsType;

        //Span<ComponentMetadata> newMeta = stackalloc ComponentMetadata[initType.Components.Count + 1];
        ////newMeta[0..^1];
        //newMeta[^1] = componentID;
        //newMeta.Sort();

        //var hash = ComponentHasher.Calculate(newMeta);

        var finiType = new EcsType(initType);
        finiType.Add(in componentID);

        if (!_typeIndex.TryGetValue(finiType, out var arch))
        {
            arch = _archRoot.InsertVertex(record.Archetype, finiType, componentID);
        }
        else
        {
            finiType.Dispose();
        }

        var newRow = record.Archetype.MoveEntityRight(arch, record.Row);
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

        var initType = record.Archetype.EcsType;
        var finiType = new EcsType(initType);
        finiType.Remove(in componentID);

        if (!_typeIndex.TryGetValue(finiType, out var arch))
        {
            arch = _archRoot.InsertVertex(record.Archetype, finiType, componentID);
        }
        else
        {
            finiType.Dispose();
        }

        var newRow = record.Archetype.MoveEntityRight(arch, record.Row);
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
        var type = GetSystemType(components);
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
        var type = GetSystemType(sys.Components);
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

    private EcsType GetSystemType(ReadOnlySpan<ComponentMetadata> components)
    {
        var ecsType = new EcsType(components.Length);

        for (int i = 0; i < components.Length; i++)
        {
            ecsType.Add(in components[i]);
        }

        return ecsType;
    }
}

sealed unsafe class Archetype
{
    const int ARCHETYPE_INITIAL_CAPACITY = 16;

    private readonly World _world;

    private int _capacity, _count;
    private int[] _entityIDs;
    internal byte[][] _components;
    private readonly EcsType _type;
    private List<EcsEdge> _edgesLeft, _edgesRight;
    private readonly int[] _lookup;

    public EcsType EcsType => _type;
    public int Count => _count;
    public int[] Entities => _entityIDs;
    public int[] Lookup => _lookup;

    public Archetype(World world, EcsType type)
    {
        _world = world;
        _capacity = ARCHETYPE_INITIAL_CAPACITY;
        _count = 0;
        _type = type;
        _entityIDs = new int[ARCHETYPE_INITIAL_CAPACITY];
        _components = new byte[type.Count][];
        _edgesLeft = new List<EcsEdge>();
        _edgesRight = new List<EcsEdge>();

        var maxID = 0;
        for (int i = 0; i < type.Count; ++i)
        {
            maxID = Math.Max(maxID, type[i].ID);
        }

        _lookup = new int[maxID + 1];
        _lookup.AsSpan().Fill(-1);
        for (int i = 0; i < type.Count; ++i)
        {
            _lookup[type[i].ID] = i;
        }

        ResizeComponentArray(ARCHETYPE_INITIAL_CAPACITY);

        world._typeIndex[type] = this;
    }

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

        for (int i = 0; i < _type.Count; ++i)
        {
            ref readonly var meta = ref _type[i];
            var leftArray = _components[i].AsSpan();

            var removeComponent = leftArray.Slice(meta.Size * row, meta.Size);
            var swapComponent = leftArray.Slice(meta.Size * (_count - 1), meta.Size);

            swapComponent.CopyTo(removeComponent);
        }

        --_count;

        return removed;
    }

    public Archetype InsertVertex(Archetype left, EcsType newType, in ComponentMetadata componentID)
    {
        var vertex = new Archetype(_world, newType);
        MakeEdges(left, vertex, componentID);
        InsertVertex(vertex);
        return vertex;
    }

    public int MoveEntityRight(Archetype right, int leftRow)
    {
        var removed = _entityIDs[leftRow];
        _entityIDs[leftRow] = _entityIDs[_count - 1];

        var rightRow = right.Add(removed);

        var max = Math.Min(_type.Count, right._type.Count);

        for (int i = 0, j = 0; i < max; ++i)
        {
            Debug.Assert(_type[i].ID >= right._type[j].ID, "elements in types mismatched");

            while (_type[i] != right._type[j])
            {
                j++;
            }

            ref readonly var meta = ref _type[i];
            var leftArray = _components[i].AsSpan();
            var rightArray = right._components[j].AsSpan();

            var insertComponent = rightArray.Slice(meta.Size * rightRow, meta.Size);
            var removeComponent = leftArray.Slice(meta.Size * leftRow, meta.Size);
            var swapComponent = leftArray.Slice(meta.Size * (_count - 1), meta.Size);

            removeComponent.CopyTo(insertComponent);
            swapComponent.CopyTo(removeComponent);
        }

        --_count;

        return rightRow;
    }

    public Archetype TraverseAndCreate(EcsType type)
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
            var typeLen = _type.Count;
            for (int fast = 0; fast < typeLen; ++fast)
            {
                ref readonly var component = ref _type[fast];

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

    private static Archetype TraverseAndCreateHelp(Archetype vertex, in EcsType type, int stack, Span<ComponentMetadata> acc, Archetype root)
    {
        if (stack == 0)
        {
            return vertex;
        }

        if (vertex._edgesRight == null || vertex._edgesRight.Count == 0)
        {
            var nt = new EcsType(type.Count);
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

        var newType = new EcsType(acc.Length);
        for (int i = 0; i < acc.Length; ++i)
        {
            newType.Add(acc[i]);
        }
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
        var nodeTypeLen = _type.Count;
        var newTypeLen = newNode._type.Count;

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

        if (!_type.IsSuperset(in newNode._type))
        {
            return;
        }

        var i = 0;
        var newNodeTypeLen = newNode._type.Count;
        for (; i < newNodeTypeLen && _type[i] == newNode._type[i]; ++i) { }

        MakeEdges(newNode, this, _type[i]);
    }

    private void ResizeComponentArray(int capacity)
    {
        for (int i = 0; i < _type.Count; ++i)
        {
            ref readonly var meta = ref _type[i];
            Array.Resize(ref _components[i], meta.Size * capacity);
            _capacity = capacity;
        }
    }
}

struct Query : IQueryComposition, IQuery
{
    private readonly World _world;
    internal EcsType _add, _remove;
    internal readonly List<Archetype> _archetypes;

    public Query(World world)
    {
        _world = world;
        _archetypes = new List<Archetype>();
        _add = new EcsType(16);
        _remove = new EcsType(16);
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

interface IQuery
{
    QueryIterator GetEnumerator();
}

interface IQueryComposition
{
    IQueryComposition With<T>() where T : struct;
    IQueryComposition Without<T>() where T : struct;
    IQuery End();
}

ref struct QueryIterator
{
    private int _index;
    private readonly IEnumerator<Archetype> _archetypes;
    private EcsType _add;
    private ref int _firstEntity;
    private byte[][] _components;
    private int[] _columns;

    internal QueryIterator(List<Archetype> archetypes, EcsType add)
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

        } while (_index <= 0);

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

struct EcsType : IEquatable<EcsType>, IDisposable
{
    private ComponentMetadata[] _components;
    private int _count, _capacity;

    public EcsType(int capacity)
    {
        _capacity = capacity;
        _components = ArrayPool<ComponentMetadata>.Shared.Rent(capacity);
    }

    public EcsType(in EcsType other)
    {
        _capacity = other._capacity;
        _count = other._count;
        _components = ArrayPool<ComponentMetadata>.Shared.Rent(other._capacity);
        other._components.CopyTo(_components, 0);
    }



    public readonly int Count => _count;
    public ReadOnlySpan<ComponentMetadata> Components => _components.AsSpan(0, _count);

    public readonly ref readonly ComponentMetadata this[int index] => ref _components[index];



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

    public readonly int IndexOf(in ComponentMetadata id) => Array.IndexOf(_components, id);

    public readonly bool IsSuperset(in EcsType other)
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

    public readonly bool Equals(EcsType other)
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

    public readonly override int GetHashCode()
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

            ArrayPool<ComponentMetadata>.Shared.Return(_components);
            var arr = ArrayPool<ComponentMetadata>.Shared.Rent(_capacity);
            _components.CopyTo(arr, 0);
            _components = arr;
        }
    }

    public Span<ComponentMetadata>.Enumerator GetEnumerator() => _components.AsSpan(0, _count).GetEnumerator();

    public void Dispose()
    {
        ArrayPool<ComponentMetadata>.Shared.Return(_components);
        _count = 0;
        _components = null;
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
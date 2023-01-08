using System.Collections;
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

            system.Archetype?.StepHelp(system.Components, system.Func);
        }
    }

    public int RegisterComponent<T>() where T : struct
    {
        return Component<T>.Metadata.ID;
    }

    public IQueryComposition Query()
    {
        var query = new Query(this);
        return query;
    }

    private void Attach(int entity, int componentID)
    {
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_entityIndex, entity);
        if (Unsafe.IsNullRef(ref record))
        {
            return;
        }

        var initType = record.Archetype.EcsType;
        var finiType = new EcsType(initType);
        finiType.Add(componentID);

        if (!_typeIndex.TryGetValue(finiType, out var arch))
        {
            arch = _archRoot.InsertVertex(record.Archetype, finiType, componentID);
        }

        var newRow = record.Archetype.MoveEntityRight(arch, record.Row);
        record.Row = newRow;
        record.Archetype = arch;
    }

    private void Set(int entity, in ComponentMetadata metadata, ReadOnlySpan<byte> data)
    {
        Debug.Assert(metadata.ID > 0);
        Debug.Assert(metadata.Size > 0);

        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_entityIndex, entity);
        if (Unsafe.IsNullRef(ref record))
        {
            return;
        }

        var column = record.Archetype.EcsType.Components.IndexOf(metadata.ID);
        if (column == -1)
        {
            return;
        }

        var componentData = record.Archetype._components[column]
            .AsSpan(metadata.Size * record.Row, metadata.Size);
        data.CopyTo(componentData);
    }

    private bool Has(int entity, in ComponentMetadata metadata)
    {
        return !Get(entity, in metadata).IsEmpty;
    }

    private Span<byte> Get(int entity, in ComponentMetadata metadata)
    {
        Debug.Assert(metadata.ID > 0);
        Debug.Assert(metadata.Size > 0);

        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_entityIndex, entity);
        if (Unsafe.IsNullRef(ref record))
        {
            return Span<byte>.Empty;
        }

        var column = record.Archetype.EcsType.Components.IndexOf(metadata.ID);
        if (column == -1)
        {
            return Span<byte>.Empty;
        }

        return record.Archetype._components[column]
            .AsSpan(metadata.Size * record.Row, metadata.Size);
    }

    private unsafe int RegisterSystem(delegate* managed<in EcsView, int, void> system, ReadOnlySpan<int> components)
    {
        var type = GetSystemType(components);
        if (!_typeIndex.TryGetValue(type, out var arch))
        {
            arch = _archRoot.TraverseAndCreate(type);
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

        sys.Archetype = arch;
    }

    internal EcsType GetSystemType(ReadOnlySpan<int> components)
    {
        var ecsType = new EcsType(components.Length);

        for (int i = 0; i < components.Length; i++)
        {
            ecsType.Add(components[i]);
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

    public EcsType EcsType => _type;
    public int Count => _count;
    public int[] Entities => _entityIDs;

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
            ref readonly var meta = ref ComponentStorage.Get(_type.Components[i]);
            var leftArray = _components[i].AsSpan();

            var removeComponent = leftArray.Slice(meta.Size * row, meta.Size);
            var swapComponent = leftArray.Slice(meta.Size * (_count - 1), meta.Size);

            swapComponent.CopyTo(removeComponent);
        }

        --_count;

        return removed;
    }

    public Archetype InsertVertex(Archetype left, EcsType newType, int componentID)
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

        for (int i = 0, j = 0; i < _type.Count; ++i)
        {
            Debug.Assert(_type.Components[i] >= right._type.Components[j], "elements in types mismatched");

            while (_type.Components[i] != right._type.Components[j])
            {
                j++;
            }

            ref readonly var meta = ref ComponentStorage.Get(_type.Components[i]);
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
        Span<int> acc = stackalloc int[len];

        return TraverseAndCreateHelp(this, type, len, acc, 0, this);
    }

    public void StepHelp(ReadOnlySpan<int> components, delegate* managed<in EcsView, int, void> run)
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
                var component = _type.Components[fast];

                if (component == components[slow])
                {
                    ref readonly var meta = ref ComponentStorage.Get(component);
                    componentSizes[slow] = meta.Size;
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

    private static Archetype TraverseAndCreateHelp(Archetype vertex, EcsType type, int stack, Span<int> acc, int accTop, Archetype root)
    {
        if (stack == 0)
        {
            return vertex;
        }

        foreach (ref var edge in CollectionsMarshal.AsSpan(vertex._edgesRight))
        {
            if (type.Components.IndexOf(edge.ComponentID) != -1)
            {
                acc[accTop] = edge.ComponentID;

                return TraverseAndCreateHelp(edge.Archetype, type, stack - 1, acc, accTop + 1, root);
            }
        }

        int i;
        var newType = new EcsType(accTop);
        for (i = 0; i < accTop; ++i)
        {
            newType.Add(acc[i]);
        }

        // NOTE: do not register any system there
        //if (type.Count > newType.Count)
        if (newType.Count == 0)
        {
            return new Archetype(vertex._world, type);
        }


        var newComponent = 0;
        if (type.Count > newType.Count)
        {
            newComponent = type.Components[^1];
            newType.Add(newComponent);
            acc[accTop] = newComponent;
        }
        else
        {
            for (i = 0; i < type.Count; ++i)
            {
                if (type.Components[i] != newType.Components[i])
                {
                    newComponent = type.Components[i];
                    newType.Add(newComponent);
                    acc[accTop] = newComponent;
                    break;
                }
            }
        }

        var newVertex = root.InsertVertex(vertex, newType, newComponent);

        return TraverseAndCreateHelp(newVertex, type, stack - 1, acc, accTop + 1, root);
    }

    private static void MakeEdges(Archetype left, Archetype right, int componentID)
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
        for (; i < newNodeTypeLen && _type.Components[i] == newNode._type.Components[i]; ++i) { }

        MakeEdges(newNode, this, _type.Components[i]);
    }

    private void ResizeComponentArray(int capacity)
    {
        for (int i = 0; i < _type.Count; ++i)
        {
            ref readonly var meta = ref ComponentStorage.Get(_type.Components[i]);
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
        _add.Add(Component<T>.Metadata.ID);

        return this;
    }

    public IQueryComposition Without<T>() where T : struct
    {
        _remove.Add(Component<T>.Metadata.ID);

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
                foreach (var component in _remove.Components)
                {
                    if (t.Components.Contains(component))
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
    private Span<int> _columns;

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
        _columns = CollectionsMarshal.AsSpan(archetype.EcsType.Components);
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
    public readonly ref int Entity;

    private readonly int _row;
    private readonly byte[][] _componentArrays;
    private readonly Span<int> _columns;

    internal EcsQueryView(ref int entity, int row, byte[][] _components, Span<int> columns)
    {
        Entity = ref entity;
        _row = row;
        _componentArrays = _components;
        _columns = columns;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T Get<T>() where T : struct
    {
        var meta = Component<T>.Metadata;

        // this is very expensive
        var column = _columns.IndexOf(meta.ID);
        var size = meta.Size;

        var span = _componentArrays[column]
            .AsSpan(size * _row, size);

        return ref MemoryMarshal.AsRef<T>(span);
    }
}

struct EcsRecord
{
    public Archetype Archetype;
    public int Row;
}

struct EcsType : IEquatable<EcsType>
{
    public List<int> Components { get; }
    public int Count => Components.Count;


    public EcsType(int capacity)
    {
        Components = new List<int>(capacity);
    }

    public EcsType(in EcsType other)
    {
        Components = new List<int>(other.Components);
    }

    public void Add(int id)
    {
        Components.Add(id);
        Components.Sort();
    }

    public bool IsSuperset(in EcsType other)
    {
        //var left = 0;
        //var right = 0;
        //var superLen = Count;
        //var subLen = other.Count;

        //if (superLen < subLen)
        //    return false;

        //while (left < superLen && right < subLen)
        //{
        //    if (Components[left] < other.Components[right])
        //        left++;
        //    else if (Components[left] == other.Components[right])
        //    {
        //        left++;
        //        right++;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        //return right == subLen;

        int i = 0, j = 0;
        while (i < Components.Count && j < other.Components.Count)
        {
            if (Components[i] == other.Components[j])
            {
                j++;
            }

            i++;
        }

        return j == other.Components.Count;
    }

    public bool Equals(EcsType other)
    {
        if (/*other == null ||*/ Components.Count != other.Components.Count)
        {
            return false;
        }

        for (int i = 0; i < Components.Count; i++)
        {
            if (Components[i] != other.Components[i])
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

            foreach (ref var id in CollectionsMarshal.AsSpan(Components))
            {
                hash = ((hash << 5) + hash) + id;
            }

            return hash;
        }
    }
}

struct EcsEdge
{
    public int ComponentID;
    public Archetype Archetype;
}

unsafe class EcsSystem
{
    public Archetype Archetype;
    public int[] Components;
    public delegate* managed<in EcsView, int, void> Func;
}

public ref struct EcsView
{
    internal byte[][] ComponentArrays;
    internal Span<int> SignatureToIndex;
    internal Span<int> ComponentSizes;
}


readonly struct ComponentMetadata
{
    public readonly int ID;
    public readonly int Size;

    public ComponentMetadata(int id, int size) 
        => (ID, Size) = (id, size);

    public static readonly ComponentMetadata Invalid = new ComponentMetadata(-1, -1);
}

static class Component<T> where T : struct
{
    public static readonly ComponentMetadata Metadata = ComponentStorage.Create<T>();
}

static class ComponentStorage
{
    private static readonly Dictionary<int, ComponentMetadata> _components = new Dictionary<int, ComponentMetadata>();
    private static readonly Dictionary<Type, ComponentMetadata> _componentsByType = new Dictionary<Type, ComponentMetadata>();

    public static ComponentMetadata Create<T>()
    {
        if (!_componentsByType.TryGetValue(typeof(T), out var meta))
        {
            meta = new ComponentMetadata(ComponentIDGen.Next(), Unsafe.SizeOf<T>());
            _components.Add(meta.ID, meta);
        }

        return meta;
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
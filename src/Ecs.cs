using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyEcs;

sealed partial class World
{
    // this is not threadsafe
    private int _nextEntityID = 1;
    private readonly Archetype _archRoot;

    internal readonly Dictionary<int, int> _componentIndex = new Dictionary<int, int>();
    internal readonly Dictionary<Type, int> _componentTypeIndex = new Dictionary<Type, int>();
    internal readonly Dictionary<int, EcsRecord> _entityIndex = new Dictionary<int, EcsRecord>();
    internal readonly Dictionary<EcsType, Archetype> _typeIndex = new Dictionary<EcsType, Archetype>();
    internal readonly Dictionary<int, EcsSystem> _systemIndex = new Dictionary<int, EcsSystem>();
    
    public World()
    {
        _archRoot = new Archetype(this, new EcsType(0));
    }

    public int CreateEntity()
    {
        var row = _archRoot.Add(_nextEntityID);

        ref var record = ref CollectionsMarshal.GetValueRefOrAddDefault(_entityIndex, _nextEntityID, out var _);
        record.Archetype = _archRoot;
        record.Row = row;

        return _nextEntityID++;
    }

    public void Attach(int entity, int componentID)
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

        //ref var newRecord = ref CollectionsMarshal.GetValueRefOrAddDefault(_entityIndex, entity, out var _);
        //newRecord.Row = newRow;
        //newRecord.Archetype = arch;
    }

    public void Set(int entity, int componentID, ReadOnlySpan<byte> data)
    {
        var size = _componentIndex[componentID];
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_entityIndex, entity);
        if (Unsafe.IsNullRef(ref record))
        {
            return;
        }

        var column = record.Archetype.EcsType.Components.IndexOf(componentID);
        if (column == -1)
        {
            return;
        }

        var componentData = record.Archetype._components[column].AsSpan(size * record.Row, size);
        data.CopyTo(componentData);
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

    public unsafe int RegisterSystem(delegate* managed<in EcsView, int, void> system, params int[] components)
    {
        return RegisterSystem(system, components.AsSpan());
    }

    private unsafe int RegisterSystem(delegate* managed<in EcsView, int, void> system, ReadOnlySpan<int> components)
    {
        var type = GetSystemType(components);
        if (!_typeIndex.TryGetValue(type, out var arch))
        {
            arch = _archRoot.TraverseAndCreate(type);
        }

        ref var sys = ref CollectionsMarshal.GetValueRefOrAddDefault(_systemIndex, _nextEntityID, out var exists);
        if (!exists)
            sys = new EcsSystem();

        sys.Archetype = arch;
        sys.Components = components.ToArray();
        sys.Func = system;

        return _nextEntityID++;
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

    private EcsType GetSystemType(ReadOnlySpan<int> components)
    {
        var ecsType = new EcsType(components.Length);

        for (int i = 0; i < components.Length; i++)
        {
            ecsType.Add(components[i]);
        }

        return ecsType;
    }

    public unsafe int RegisterComponent<T>() where T : struct
    {
        ref var id = ref CollectionsMarshal.GetValueRefOrAddDefault(_componentTypeIndex, typeof(T), out var exists);
        if (exists) return id;

        id = _nextEntityID++;
        ref var size = ref CollectionsMarshal.GetValueRefOrAddDefault(_componentIndex, id, out var _);
        size = Unsafe.SizeOf<T>();

        return id;
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

            var componentSize = _world._componentIndex[_type.Components[i]];
            var leftArray = _components[i].AsSpan();
            var rightArray = right._components[j].AsSpan();

            var insertComponent = rightArray.Slice(componentSize * rightRow, componentSize);
            var removeComponent = leftArray.Slice(componentSize * leftRow, componentSize);
            var swapComponent = leftArray.Slice(componentSize * (_count - 1), componentSize);

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
                    ref var size = ref CollectionsMarshal.GetValueRefOrNullRef(_world._componentIndex, component);
                    if (Unsafe.IsNullRef(ref size))
                    {
                        continue;
                    }

                    componentSizes[slow] = size;
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
        if (type.Count > newType.Count)
        {
            return null;
        }

        var newComponent = 0;
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

        var newVertex = root.InsertVertex(vertex, newType, newComponent);

        return TraverseAndCreateHelp(newVertex, type, stack - 1, acc, accTop + 1, root);
    }

    private static void MakeEdges(Archetype left, Archetype right, int componentID)
    {
        left!._edgesRight.Add(new EcsEdge() { Archetype = right, ComponentID = componentID });
        right!._edgesLeft.Add(new EcsEdge() { Archetype = left, ComponentID = componentID });
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

        if (!_type.IsSuperset(newNode._type))
        {
            return;
        }

        var i = 0;
        var newNodeTypeLen = newNode._type.Count;
        for (; i < newNodeTypeLen && _type.Components[i] == newNode._type.Components[i]; ++i) { }

        MakeEdges(newNode, this, _type.Components[i]);
    }

    private unsafe void ResizeComponentArray(int capacity)
    {
        for (int i = 0; i < _type.Count; ++i)
        {
            var componentSize = _world._componentIndex[_type.Components[i]];
            Array.Resize(ref _components[i], componentSize * capacity);
            _capacity = capacity;
        }
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

    public bool IsSuperset(EcsType other)
    {
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

//static class Component<T> where T : struct
//{
//    public static readonly int Size = Unsafe.SizeOf<T>();

//    [ThreadStatic] public static int ID = -1;
//}
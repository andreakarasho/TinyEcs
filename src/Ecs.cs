using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyEcs;

sealed class World
{
    private static readonly EcsTypeComparer _comparer = new EcsTypeComparer();

    // this is not threadsafe
    private int _nextEntityID = 1;
    private readonly Dictionary<int, int> _componentIndex = new Dictionary<int, int>();
    private readonly Dictionary<int, EcsRecord> _entityIndex = new Dictionary<int, EcsRecord>();
    private readonly Dictionary<EcsType, Archetype> _typeIndex = new Dictionary<EcsType, Archetype>(_comparer);
    private readonly Archetype _archRoot;

    public World()
    {
        _archRoot = new Archetype(new EcsType(0), _componentIndex, _typeIndex);
    }

    public int CreateEntity()
    {
        var row = _archRoot.Add(_nextEntityID);

        ref var record = ref CollectionsMarshal.GetValueRefOrAddDefault(_entityIndex, _nextEntityID, out var _);
        record.Archetype = _archRoot;
        record.Row = row;

        return _nextEntityID++;
    }


    public void Attach<T>(int entity) where T : struct
    {
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_entityIndex, entity);
        if (Unsafe.IsNullRef(ref record))
        {
            return;
        }

        var initType = record.Archetype.EcsType;
        var finiType = new EcsType(initType);

        var componentID = RegisterComponent<T>();
        finiType.Add(componentID);

        // use finiType actually
        var found = _typeIndex.TryGetValue(finiType, out var maybeFiniArch);

        Archetype finiArch;

        if (!found)
        {
            finiArch = _archRoot.InsertVertex(record.Archetype, finiType, componentID);
        }
        else
        {
            //finiType = null;
            finiArch = maybeFiniArch;
        }

        var newRow = record.Archetype.MoveEntityRight(finiArch, record.Row);

        ref var newRecord = ref CollectionsMarshal.GetValueRefOrAddDefault(_entityIndex, entity, out var _);
        newRecord.Row = newRow;
        newRecord.Archetype = finiArch;
    }

    private unsafe int RegisterComponent<T>() where T : struct
    {
        ref var id = ref Component<T>.ID;
        if (id <= 0) id = _nextEntityID++;

        ref var size = ref CollectionsMarshal.GetValueRefOrAddDefault(_componentIndex, id, out var _);
        size = Component<T>.Size;

        return id;
    }
}

sealed unsafe class Archetype
{
    const int ARCHETYPE_INITIAL_CAPACITY = 16;

    private int _capacity, _count;
    private int[] _entityIDs;
    private nuint[] _components;
    private readonly EcsType _type;
    private List<EcsEdge> _edgesLeft, _edgesRight;
    private readonly Dictionary<int, int> _componentIndex;
    private readonly Dictionary<EcsType, Archetype> _typeIndex;

    public EcsType EcsType => _type;

    public Archetype(EcsType type, Dictionary<int, int> componentIndex, Dictionary<EcsType, Archetype> typeIndex)
    {
        _componentIndex = componentIndex;
        _typeIndex = typeIndex;
        _capacity = ARCHETYPE_INITIAL_CAPACITY;
        _count = 0;
        _type = type;
        _entityIDs = new int[ARCHETYPE_INITIAL_CAPACITY];
        _components = new nuint[type.Count];
        _edgesLeft = new List<EcsEdge>();
        _edgesRight = new List<EcsEdge>();

        ResizeComponentArray(ARCHETYPE_INITIAL_CAPACITY);

        _typeIndex[type] = this;
    }

    public int Add(int entityID)
    {
        if (_capacity == _count)
        {
            Array.Resize(ref _entityIDs, _capacity * 2);
            ResizeComponentArray(_capacity * 2);
        }

        _entityIDs[_count] = entityID;

        //ref var record = ref CollectionsMarshal.GetValueRefOrAddDefault(_entityIndex, entityID, out var _);
        //record.Archetype = this;
        //record.Row = _count;

        return _count++;
    }

    public Archetype InsertVertex(Archetype left, EcsType newType, int componentID)
    {
        var arch = new Archetype(newType, _componentIndex, _typeIndex);
        left.MakeEdges(arch, componentID);
        InsertVertex(arch);
        return arch;
    }

    public int MoveEntityRight(Archetype right, int leftRow)
    {
        var removed = _entityIDs[leftRow];
        _entityIDs[leftRow] = _entityIDs[_count - 1];

        var rightRow = right.Add(removed);

        for (int i = 0, j = 0; i < _type.Count; ++i)
        {
            while (_type.Components[i] != right._type.Components[j])
            {
                j++;
            }

            var componentSize = _componentIndex[_type.Components[i]];
            var leftArray = _components[i];
            var rightArray = right._components[j];

            var insertComponent = (byte*)rightArray + (componentSize * rightRow);
            var removeComponent = (byte*)leftArray + (componentSize * leftRow);
            var swapComponent = (byte*)leftArray + (componentSize * (_count - 1));

            Unsafe.CopyBlock(insertComponent, removeComponent, (uint)componentSize);
            Unsafe.CopyBlock(removeComponent, swapComponent, (uint)componentSize);
        }

        --_count;

        return rightRow;
    }

    private void MakeEdges(Archetype right, int componentID)
    {
        _edgesRight.Add(new EcsEdge() { Archetype = right, ComponentID = componentID });
        right._edgesLeft.Add(new EcsEdge() { Archetype = this, ComponentID = componentID });
    }

    private void InsertVertex(Archetype newNode)
    {
        var newNodeTypeLen = newNode._type.Count;

        if (_type.Count > newNodeTypeLen - 1)
        {
            return;
        }

        if (_type.Count < newNodeTypeLen - 1)
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
        for (; i < newNodeTypeLen && _type.Components[i] == newNode._type.Components[i]; ++i) { }

        newNode.MakeEdges(this, _type.Components[i]);
    }

    private unsafe void ResizeComponentArray(int capacity)
    {
        int i = 0;
        foreach (var ecsType in _type.Components)
        {
            var componentSize = _componentIndex[ecsType];
            _components[i] = (nuint)NativeMemory.Realloc((void*)_components[i], (nuint)(_type.Count * componentSize * capacity));

            ++i;
            _capacity = capacity;
        }
    }
}


struct EcsRecord
{
    public Archetype Archetype;
    public int Row;
}

sealed class EcsTypeComparer : IEqualityComparer<EcsType>
{
    public bool Equals(EcsType x, EcsType y) => x.Equals(y);

    public int GetHashCode([DisallowNull] EcsType obj)
    {
        var hash = 5381;

        foreach (ref var id in CollectionsMarshal.AsSpan(obj.Components))
        {
            hash = ((hash << 5) + hash) + id;
        }

        return hash;
    }
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
        //if (other == null)
        //{
        //    return false;
        //}

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
}

struct EcsEdge
{
    public int ComponentID;
    public Archetype Archetype;
}

static class Component<T> where T : struct
{
    public static readonly int Size = Unsafe.SizeOf<T>();
    public static int ID;
}
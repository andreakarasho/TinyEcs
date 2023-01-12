using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyEcs;

public sealed partial class World : IDisposable
{
    private int _entityCount = 0;

    private int _deferStatus, _deferActionCount;
    private EcsDeferredAction[] _deferredActions = new EcsDeferredAction[0xFF];
    internal readonly IDGenerator _idGen = new IDGenerator();
    internal readonly ComponentStorage _storage;

    private Archetype _archRoot;
    private readonly Stack<int> _recycleIds = new Stack<int>();
    internal readonly Dictionary<int, EcsRecord> _entityIndex = new Dictionary<int, EcsRecord>();
    internal readonly Dictionary<int, Archetype> _typeIndex = new Dictionary<int, Archetype>();
    internal readonly Dictionary<int, EcsSystem> _systemIndex = new Dictionary<int, EcsSystem>();


    public World()
    {
        _storage = new ComponentStorage(this);
        _archRoot = new Archetype(this, new EcsSignature(0));
        //_typeIndex[_archRoot.GetHashCode()] = _archRoot;
    }


    public int EntityCount => _entityCount;


    private unsafe void Destroy()
    {
        foreach ((var type, var arch) in _typeIndex)
        {
            arch.Signature.Dispose();
        }

        _systemIndex.Clear();
        _recycleIds.Clear();
        _typeIndex.Clear();

        _entityCount = 0;
        _archRoot = new Archetype(this, new EcsSignature(0));
        //_typeIndex[_archRoot.GetHashCode()] = _archRoot;
    }

    public void Dispose() => Destroy();

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

    public int CreateEntity()
    {
        var id = _idGen.Next();

        if (IsDeferred())
        {
            CreateDeferred(id);
        }
        else
        {
            InternalCreateEntity(id);
        }

        return id;
    }

    public void DestroyEntity(int entity)
    {
        if (IsDeferred())
        {
            DestroyDeferred(entity);
            return;
        }

        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_entityIndex, entity);
        if (Unsafe.IsNullRef(ref record))
        {
            Debug.Fail("not an entity!");
        }

        var removedId = record.Archetype.Remove(record.Row);

        Debug.Assert(removedId == entity);

        _recycleIds.Push(removedId);
        _entityIndex.Remove(removedId);

        Interlocked.Decrement(ref _entityCount);
    }



    private int Attach(int entity, int componentID)
    {
        if (IsDeferred())
        {
            AttachDeferred(entity, componentID);
            return componentID;
        }

        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_entityIndex, entity);
        if (Unsafe.IsNullRef(ref record))
        {
            Debug.Fail("not an entity!");
        }

        var column = componentID >= record.Archetype.Lookup.Length ? -1 : record.Archetype.Lookup[componentID];
        if (column >= 0)
        {
            return -1;
        }

        InternalAttachDetach(ref record, componentID, true);
        return componentID;
    }

    private int Detach(int entity, int componentID)
    {
        if (IsDeferred())
        {
            DetachDeferred(entity, componentID);
            return componentID;
        }

        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_entityIndex, entity);
        if (Unsafe.IsNullRef(ref record))
        {
            Debug.Fail("not an entity!");
        }

        var column = componentID >= record.Archetype.Lookup.Length ? -1 : record.Archetype.Lookup[componentID];
        if (column < 0)
        {
            return -1;
        }

        InternalAttachDetach(ref record, componentID, false);
        return componentID;
    }



    private void InternalCreateEntity(int id)
    {
        var row = _archRoot.Add(id);
        ref var record = ref CollectionsMarshal.GetValueRefOrAddDefault(_entityIndex, id, out var exists);
        Debug.Assert(!exists);
        record.Archetype = _archRoot;
        record.Row = row;

        Interlocked.Increment(ref _entityCount);
    }

    private void InternalAttachDetach(ref EcsRecord record, int componentID, bool add)
    {
        var initType = record.Archetype.Signature;

        Span<int> span = stackalloc int[initType.Count + 1];
        initType.Components.CopyTo(span);
        span[^1] = componentID;
        span.Sort();

        var hash = ComponentHasher.Calculate(span);

        ref var arch = ref CollectionsMarshal.GetValueRefOrAddDefault(_typeIndex, hash, out var exists);
        if (!exists)
        {
            var finiType = new EcsSignature(initType);
            if (add)
                finiType.Add(componentID);
            else
                finiType.Remove(componentID);

            arch = _archRoot.InsertVertex(record.Archetype, finiType, componentID);
        }

        var newRow = Archetype.MoveEntity(record.Archetype, arch!, record.Row);
        record.Row = newRow;
        record.Archetype = arch!;
    }

    private void Set(int entity, int metadata, ReadOnlySpan<byte> data)
    {
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_entityIndex, entity);
        if (Unsafe.IsNullRef(ref record))
        {
            Debug.Fail("not an entity!");
        }

        var column = metadata >= record.Archetype.Lookup.Length ? -1 : record.Archetype.Lookup[metadata];
        if (column == -1)
        {
            Attach(entity, metadata);
            column = metadata >= record.Archetype.Lookup.Length ? -1 : record.Archetype.Lookup[metadata];
            //return;
        }

        var size = record.Archetype.Sizes[metadata];
        var componentData = record.Archetype._components[column]
            .AsSpan(size * record.Row, size);
        data.CopyTo(componentData);
    }

    private bool Has(int entity, int componentID) => !Get(entity, componentID).IsEmpty;

    private Span<byte> Get(int entity, int componentID)
    {
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_entityIndex, entity);
        if (Unsafe.IsNullRef(ref record))
        {
            return Span<byte>.Empty;
        }

        var column = componentID >= record.Archetype.Lookup.Length ? -1 : record.Archetype.Lookup[componentID];
        if (column == -1)
        {
            return Span<byte>.Empty;
        }

        var size = record.Archetype.Sizes[componentID];

        return record.Archetype._components[column]
            .AsSpan(size * record.Row, size);
    }

    private unsafe int RegisterSystem(delegate* managed<in EcsView, int, void> system, Span<int> components)
    {
        var hash = ComponentHasher.Calculate(components);

        ref var arch = ref CollectionsMarshal.GetValueRefOrAddDefault(_typeIndex, hash, out var exists);
        if (!exists)
        {
            arch = _archRoot.TraverseAndCreate(new EcsSignature(components));
        }

        var id = _idGen.Next();

        ref var sys = ref CollectionsMarshal.GetValueRefOrAddDefault(_systemIndex, id, out exists);
        if (!exists)
            sys = new EcsSystem();

        sys!.Archetype = arch;
        sys.Components = components.ToArray();
        sys.Func = system;

        return id;
    }

    private unsafe void UpdateSystem(EcsSystem sys)
    {
        var hash = ComponentHasher.Calculate(sys.Components);

        ref var arch = ref CollectionsMarshal.GetValueRefOrAddDefault(_typeIndex, hash, out var exists);
        if (!exists)
        {
            arch = _archRoot.TraverseAndCreate(new EcsSignature(sys.Components));
        }

        sys.Archetype = arch;
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsDeferred() => _deferStatus != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BeginDefer() => Interlocked.Increment(ref _deferStatus);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void EndDefer() => Interlocked.Decrement(ref _deferStatus);

    private void CreateDeferred(int entity)
    {
        ref var cmd = ref PeekDeferredCommand();
        cmd.Action = DeferredOp.Create;
        cmd.Stage = _deferStatus;
        cmd.Create.Entity = entity;
    }

    private void DestroyDeferred(int entity)
    {
        ref var cmd = ref PeekDeferredCommand();
        cmd.Action = DeferredOp.Destroy;
        cmd.Stage = _deferStatus;
        cmd.Destroy.Entity = entity;
    }

    private void AttachDeferred(int entity, int component)
    {
        ref var cmd = ref PeekDeferredCommand();
        cmd.Action = DeferredOp.Attach;
        cmd.Stage = _deferStatus;
        cmd.Attach.Entity = entity;
        cmd.Attach.Component = component;
    }

    private void DetachDeferred(int entity, int component)
    {
        ref var cmd = ref PeekDeferredCommand();
        cmd.Action = DeferredOp.Detach;
        cmd.Stage = _deferStatus;
        cmd.Detach.Entity = entity;
        cmd.Detach.Component = component;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref EcsDeferredAction PeekDeferredCommand()
    {
        if (_deferActionCount >= _deferredActions.Length)
        {
            Array.Resize(ref _deferredActions, _deferredActions.Length * 2);
        }

        return ref _deferredActions[_deferActionCount++];
    }

    internal void MergeDeferred()
    {
        // TODO: test for nested queries
        if (IsDeferred())
            return;

        var count = _deferActionCount;

        for (int i = 0; i < count; ++i)
        {
            ref var cmd = ref _deferredActions[i];

            switch (cmd.Action)
            {
                case DeferredOp.Create:

                    InternalCreateEntity(cmd.Create.Entity);

                    break;

                case DeferredOp.Destroy:

                    DestroyEntity(cmd.Create.Entity);

                    break;

                case DeferredOp.Attach:

                    Attach(cmd.Attach.Entity, cmd.Attach.Component);

                    break;

                case DeferredOp.Detach:

                    Detach(cmd.Detach.Entity, cmd.Detach.Component);

                    break;
            }
        }

        _deferActionCount -= count;
    }

    private enum DeferredOp : byte
    {
        Create,
        Destroy,
        Attach,
        Detach,
    }

    [StructLayout(LayoutKind.Explicit)]
    struct EcsDeferredAction
    {
        [FieldOffset(0)]
        public DeferredOp Action;
        
        [FieldOffset(1)]
        public int Stage;

        [FieldOffset(1)]
        public EcsDeferredCreateEntity Create;

        [FieldOffset(1)]
        public EcsDeferredDestroyEntity Destroy;

        [FieldOffset(1)]
        public EcsDeferredAttachComponent Attach;

        [FieldOffset(1)]
        public EcsDeferredDetachComponent Detach;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EcsDeferredCreateEntity
    {
        public DeferredOp Action;
        public int Stage;

        public int Entity;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EcsDeferredDestroyEntity
    {
        public DeferredOp Action;
        public int Stage;

        public int Entity;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EcsDeferredAttachComponent
    {
        public DeferredOp Action;
        public int Stage;

        public int Entity;
        public int Component;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EcsDeferredDetachComponent
    {
        public DeferredOp Action;
        public int Stage;

        public int Entity;
        public int Component;
    }



    internal sealed class ComponentStorage
    {
        private readonly World _world;
        private readonly Dictionary<int, int> _idSize = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _hashId = new Dictionary<int, int>();

        private readonly int[,] _componentsID = new int[1024, 1];
        //private readonly int[] _componentsSize = new int[1024];

        public ComponentStorage(World world)
        {
            _world = world;
        }

        [SkipLocalsInit]
        static class TypeOf<T> where T : struct
        {
            public static int Size = Unsafe.SizeOf<T>();
            public static int Hash = typeof(T).GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly int GetOrCreateID<T>() where T : struct
        {
            var hash = TypeOf<T>.Hash;
            ref var id = ref CollectionsMarshal.GetValueRefOrAddDefault(_hashId, hash, out var exists);
            if (!exists)
            {
                id = _world._idGen.Next();
                _idSize.Add(id, TypeOf<T>.Size);
            }

            return ref id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetID<T>() where T : struct
        {
            var hash = TypeOf<T>.Hash;
            ref var id = ref CollectionsMarshal.GetValueRefOrNullRef(_hashId, hash);
            if (Unsafe.IsNullRef(ref id))
            {
                Debug.Fail($"componentID not found: {typeof(T)}");
            }

            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly int GetSize(int componentID)
        {
            ref var size = ref CollectionsMarshal.GetValueRefOrNullRef(_idSize, componentID);
            if (Unsafe.IsNullRef(ref size))
            {
                Debug.Fail($"componentID not found: {componentID}");
            }

            return ref size;
        }
    }


    internal sealed class IDGenerator
    {
        private readonly Stack<int> _recycled = new Stack<int>();
        private int _next = 1;

        public IDGenerator() { }
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Next()
        {
            if (!_recycled.TryPop(out var id))
            {
                id = _next;
                Interlocked.Increment(ref _next);
            }

            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(int id) => _recycled.Push(id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _next = 1;
            _recycled.Clear();
        }
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
    private readonly int[] _lookup, _sizes;

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

        var maxID = -1;
        for (int i = 0; i < sign.Count; ++i)
        {
            maxID = Math.Max(maxID, sign[i]);
        }

        _lookup = new int[maxID + 1];
        _sizes = new int[maxID + 1];
        _lookup.AsSpan().Fill(-1);
        for (int i = 0; i < sign.Count; ++i)
        {
            _lookup[sign[i]] = i;
            _sizes[sign[i]] = world._storage.GetSize(sign[i]);
        }

        ResizeComponentArray(ARCHETYPE_INITIAL_CAPACITY);
    }


    public EcsSignature Signature => _sign;
    public int Count => _count;
    public int[] Entities => _entityIDs;
    public int[] Lookup => _lookup;
    public int[] Sizes => _sizes;


    public int Add(int entityID)
    {
        if (_capacity == _count)
        {
            _capacity *= 2;

            Array.Resize(ref _entityIDs, _capacity);
            ResizeComponentArray(_capacity);
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
            var size = _sizes[i];
            var leftArray = _components[i].AsSpan();

            var removeComponent = leftArray.Slice(size * row, size);
            var swapComponent = leftArray.Slice(size * (_count - 1), size);

            swapComponent.CopyTo(removeComponent);
        }

        --_count;

        return removed;
    }

    public Archetype InsertVertex(Archetype left, EcsSignature newType, int componentID)
    {
        var vertex = new Archetype(left._world, newType);
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
        var isLeft = to._sign.Count < from._sign.Count;
        int i = 0, j = 0;
        var count = isLeft ? to._sign.Count : from._sign.Count;

        ref var x = ref (isLeft ? ref j : ref i);
        ref var y = ref (!isLeft ? ref j : ref i);

        for (; /*(isLeft ? j : i)*/ x < count; ++x)
        {
            while (from._sign[i] != to._sign[j])
            {
                // advance the sign with less components!
                ++y;
            }

            var size = from.Sizes[from._sign[i]];
            var leftArray = from._components[i].AsSpan();
            var rightArray = to._components[j].AsSpan();
            var insertComponent = rightArray.Slice(size * toRow, size);
            var removeComponent = leftArray.Slice(size * fromRow, size);
            var swapComponent = leftArray.Slice(size * (from._count - 1), size);
            removeComponent.CopyTo(insertComponent);
            swapComponent.CopyTo(removeComponent);

            //if (!isLeft)
            //    ++i;
            //else
            //    ++j;
        }
    }

    public Archetype TraverseAndCreate(EcsSignature type)
    {
        var len = type.Count;
        Span<int> acc = stackalloc int[len];
        type.Components.CopyTo(acc);

        return TraverseAndCreateHelp(this, in type, len, acc, this);
    }

    public void StepHelp(ReadOnlySpan<int> components, delegate* managed<in EcsView, int, void> run)
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
                    componentSizes[slow] = _world._storage.GetSize(component);
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

    private static Archetype TraverseAndCreateHelp(Archetype vertex, in EcsSignature type, int stack, Span<int> acc, Archetype root)
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

        //var hash = ComponentHasher.Calculate(acc);
        var newType = new EcsSignature(acc);
        var newComponent = -1;
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

    private static void MakeEdges(Archetype left, Archetype right, int componentID)
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
            Array.Resize(ref _components[i], _sizes[_sign[i]] * capacity);
            _capacity = capacity;
        }
    }
}

public struct Query : IQueryComposition
{
    private readonly World _world;
    private readonly EcsSignature _add, _remove;

    internal Query(World world)
    {
        _world = world;
        _add = new EcsSignature(16);
        _remove = new EcsSignature(16);
    }

    public IQueryComposition With<T>() where T : struct
    {
        _add.Add(_world._storage.GetOrCreateID<T>());

        return this;
    }

    public IQueryComposition Without<T>() where T : struct
    {
        _remove.Add(_world._storage.GetOrCreateID<T>());

        return this;
    }

    public IQueryComposition WithTag(int componentID)
    {
        _add.Add(componentID);

        return this;
    }

    public IQueryComposition WithoutTag(int componentID)
    {
        _remove.Add(componentID);

        return this;
    }

    public QueryIterator GetEnumerator() => new QueryIterator(_world, _add, _remove);
}

public interface IQueryComposition
{
    IQueryComposition With<T>() where T : struct;
    IQueryComposition Without<T>() where T : struct;
    IQueryComposition WithTag(int componentID);
    IQueryComposition WithoutTag(int componentID);
    QueryIterator GetEnumerator();
}

[SkipLocalsInit]
public ref struct QueryIterator
{
    private readonly World _world;
    private readonly EcsSignature _add, _remove;
    private readonly IEnumerator<KeyValuePair<int, Archetype>> _archetypes;

    private int _index;
    private ref int _firstEntity;
    private byte[][] _components;
    private int[] _columns, _sizes;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal QueryIterator(World world, EcsSignature add, EcsSignature remove)
    {
        world!.BeginDefer();

        _world = world;
        _archetypes = world._typeIndex.AsEnumerable().GetEnumerator();
        _index = 0;
        _add = add;
        _remove = remove;
    }

    public readonly EcsQueryView Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new EcsQueryView
        (
            _world,
            ref Unsafe.Subtract(ref _firstEntity, _index),
            _index,
            _components,
            _columns,
            _sizes
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        --_index;

        if (_index >= 0) return true;

        Archetype archetype;
        do
        {
            if (!_archetypes.MoveNext()) return false;

            var curr = _archetypes.Current;
            archetype = curr.Value;

            var ok = false;
            if (archetype.Count > 0 && archetype.Signature.IsSuperset(in _add))
            {
                ok = true;
                foreach (ref readonly var component in _remove)
                {
                    if (archetype.Signature.IndexOf(component) >= 0)
                    {
                        ok = false;
                        break;
                    }
                }
            }

            if (!ok)
                continue;

            _index = archetype.Count - 1;

        } while (_index < 0);

        _firstEntity = ref MemoryMarshal.GetReference(archetype.Entities.AsSpan(_index));
        _columns = archetype.Lookup;
        _sizes = archetype.Sizes;
        _components = archetype._components;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _index = -1;
        _archetypes.Reset();
    }

    public void Dispose()
    {
        _world!.EndDefer();
        _world!.MergeDeferred();
    }
}

[SkipLocalsInit]
public readonly ref struct EcsQueryView
{
    public readonly ref readonly int Entity;

    private readonly World.ComponentStorage _storage;
    private readonly int _row;
    private readonly byte[][] _componentArrays;
    private readonly int[] _columns;
    private readonly int[] _sizes;

    internal EcsQueryView(World world, ref int entity, int row, byte[][] _components, int[] columns, int[] sizes)
    {
        _storage = world._storage;
        Entity = ref entity;
        _row = row;
        _componentArrays = _components;
        _columns = columns;
        _sizes = sizes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has<T>() where T : struct
    {
        return Has(_storage.GetID<T>());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(int componentID)
    {
        return componentID < _columns.Length && _columns[componentID] >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T Get<T>() where T : struct
    {
        return ref Get<T>(_storage.GetID<T>());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T Get<T>(int componentID) where T : struct
    {
        var size = _sizes[componentID];
        var span = _componentArrays[_columns[componentID]]
            .AsSpan(size * _row, size);

        return ref MemoryMarshal.AsRef<T>(span);
    }
}

sealed class EcsSignature : IEquatable<EcsSignature>, IDisposable
{
    private int[] _components;
    private int _count, _capacity;

    public EcsSignature(int capacity)
    {
        _capacity = capacity;
        _components = capacity <= 0 ? Array.Empty<int>(): new int[capacity];
    }

    public EcsSignature(ReadOnlySpan<int> components)
    {
        _capacity = components.Length;
        _count = components.Length;
        _components = new int[components.Length];
        components.CopyTo(_components);

        Array.Sort(_components, 0, _count);
    }

    public EcsSignature(in EcsSignature other)
    {
        _capacity = other._capacity;
        _count = other._count;
        _components = new int[other._components.Length];
        other._components.CopyTo(_components, 0);

        Array.Sort(_components, 0, _count);
    }


    public int Count => _count;
    public ReadOnlySpan<int> Components => _components.AsSpan(0, _count);
    public ref readonly int this[int index] => ref _components[index];



    public void Add(int id)
    {
        GrowIfNeeded();

        _components[_count++] = id;
        Array.Sort(_components, 0, _count);
    }

    public void Remove(int id)
    {
        var idx = IndexOf(id);
        if (idx < 0 || _count <= 0) return;

        _components[idx] = _components[--_count];
        Array.Sort(_components, 0, _count);
    }

    public int IndexOf(int id) => Array.IndexOf(_components, id);

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

            ref var f0 = ref MemoryMarshal.GetReference<int>(_components);

            for (int i = 0; i < Count; ++i)
            {
                ref readonly var id = ref Unsafe.Add(ref f0, i);
                hash = ((hash << 5) + hash) + id;
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

    public Span<int>.Enumerator GetEnumerator() => _components.AsSpan(0, _count).GetEnumerator();

    public void Dispose()
    {
        if (_components != null)
        {
            _count = 0;
            _components = Array.Empty<int>();
        }    
    }
}

unsafe class EcsSystem
{
    public Archetype? Archetype;
    public int[]? Components;
    public delegate* managed<in EcsView, int, void> Func;
}

public ref struct EcsView
{
    internal byte[][] ComponentArrays;
    internal Span<int> SignatureToIndex;
    internal Span<int> ComponentSizes;
}

readonly record struct EcsEdge(int ComponentID, Archetype Archetype);

record struct EcsRecord(Archetype Archetype, int Row);

static class ComponentHasher
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Calculate(Span<int> components)
    {
        unchecked
        {
            var hash = 5381;

            foreach (ref readonly var id in components)
            {
                hash = ((hash << 5) + hash) + id;
            }

            return hash;
        }
    }
}
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using EntityID = System.UInt64;

namespace TinyEcs;

public sealed partial class World : IDisposable
{
    private static readonly object _lock = new();
    internal static readonly EntitySparseSet<World> _allWorlds = new();

    internal readonly EntityID _worldID;
    private int _totalFrames;
    private int _entityCount = 0;
    private int _deferStatus, _deferActionCount;
    private EcsDeferredAction[] _deferredActions = new EcsDeferredAction[0xFF];

    internal Archetype _archRoot;
    internal readonly Dictionary<EntityID, Archetype> _typeIndex = new();
	internal readonly EntitySparseSet<EcsRecord> _entities = new();
    internal readonly EntitySparseSet<EcsComponent> _components = new();
    private readonly Dictionary<EntityID, QueryBuilder> _queryIndex = new();

    private readonly QueryBuilder _querySystems;
    private readonly QueryBuilder _querySystemsOnUpdate;
    private readonly QueryBuilder _querySystemsOnPreUpdate;
    private readonly QueryBuilder _querySystemsOnPostUpdate;
    private readonly QueryBuilder _querySystemsOnStartup;
    private readonly QueryBuilder _querySystemsOnPreStartup;
    private readonly QueryBuilder _querySystemsOnPostStartup;


    public World()
    {
        _archRoot = new Archetype(this, ReadOnlySpan<EntityID>.Empty);

        lock (_lock)
            _allWorlds.CreateNew(out _worldID) = this;

        // initialize pre-built queries
        _querySystems = Query()
            .With<EcsSystem>();

        _querySystemsOnUpdate = Query()
            .With<EcsSystem>()
            .With<EcsSystemPhaseOnUpdate>();

        _querySystemsOnPreUpdate = Query()
            .With<EcsSystem>()
            .With<EcsSystemPhasePreUpdate>();

        _querySystemsOnPostUpdate = Query()
            .With<EcsSystem>()
            .With<EcsSystemPhasePostUpdate>();

        _querySystemsOnStartup = Query()
            .With<EcsSystem>()
            .With<EcsSystemPhaseOnStartup>();

        _querySystemsOnPreStartup = Query()
            .With<EcsSystem>()
            .With<EcsSystemPhasePreStartup>();

        _querySystemsOnPostStartup = Query()
            .With<EcsSystem>()
            .With<EcsSystemPhasePostStartup>();
    }


    public int EntityCount => _entityCount;

    public int Frames => _totalFrames;



    public void Dispose()
    {
        _entities.Clear();
        _typeIndex.Clear();
        _queryIndex.Clear();
        _components.Clear();
        _archRoot = new Archetype(this, ReadOnlySpan<EntityID>.Empty);

        _entityCount = 0;
        _totalFrames = 0;

        lock (_lock)
            _allWorlds.Remove(_worldID);
    }


    private unsafe void RunSystemSets(SystemPhase phase, float deltaTime)
    {
        var qry = phase switch
        {
            SystemPhase.OnUpdate => _querySystemsOnUpdate,
            SystemPhase.OnPreUpdate => _querySystemsOnPreUpdate,
            SystemPhase.OnPostUpdate => _querySystemsOnPostUpdate,

            SystemPhase.OnStartup => _querySystemsOnStartup,
            SystemPhase.OnPreStartup => _querySystemsOnPreStartup,
            SystemPhase.OnPostStartup => _querySystemsOnPostStartup,

            _ => throw new NotImplementedException(),
        };

        foreach (var it in qry)
        {
            var s = it.Field<EcsSystem>();

            //ref var query = ref CollectionsMarshal.GetValueRefOrNullRef(_queryIndex, s.Value.Query);

            QueryBuilder query = null;

            foreach (var row in it)
            {
                ref var sys = ref s.Get();

                query ??= _queryIndex[sys.Query];

                foreach (var itSys in query)
                {
                    sys.Func(in itSys, deltaTime);
                }
            }

            //ref var s = ref it.Field<EcsSystem>();

            // NOTE: This is quite bad to use, but it's the only way to grab
            //       the query avoding the managed reference in a struct issue
            //ref var query = ref CollectionsMarshal.GetValueRefOrNullRef(_queryIndex, s.Query);

            //for (int i = 0; i < it.Count; ++i)
            //{
            //    ref var sys = ref it.Get(ref s, i);

            //    foreach (var itSys in query)
            //    {
            //        sys.Func(in itSys);
            //    }
            //}
        }
    }

    public unsafe void Step(float deltaTime)
    {
        if (_totalFrames == 0)
        {
            RunSystemSets(SystemPhase.OnPreStartup, deltaTime);
            RunSystemSets(SystemPhase.OnStartup, deltaTime);
            RunSystemSets(SystemPhase.OnPostStartup, deltaTime);
        }

        RunSystemSets(SystemPhase.OnPreUpdate, deltaTime);
        RunSystemSets(SystemPhase.OnUpdate, deltaTime);
        RunSystemSets(SystemPhase.OnPostUpdate, deltaTime);

        Interlocked.Increment(ref _totalFrames);
    }

    public QueryBuilder Query()
    {
        var query = new QueryBuilder(this);
        query.With<EcsEnabled>();

        return query;
    }

    public EntityView Entity()
    {
        var e = CreateEntityRaw();

        return e
            .Set(e)
            .Set<EcsEnabled>();
    }

    internal EntityView CreateEntityRaw(EntityID id = 0)
    {
        ref var record = ref (id > 0 ? ref _entities.Add(id, default!) : ref _entities.CreateNew(out id));
        record.Archetype = _archRoot;
        record.Row = _archRoot.Add(id);

        Interlocked.Increment(ref _entityCount);

        //if (IsDeferred())
        //{
        //    CreateDeferred(id);
        //}
        //else
        //{
        //    InternalCreateEntity(id);
        //}

        return new EntityView(_worldID, id);
    }

    public void Destroy(EntityID entity)
    {
        if (IsDeferred())
        {
            DestroyDeferred(entity);

            return;
        }

        RemoveChildren(entity);
        Detach(entity);

        ref var record = ref _entities.Get(entity);
        if (Unsafe.IsNullRef(ref record))
        {
            Debug.Fail("not an entity!");
        }

        var removedId = record.Archetype.Remove(record.Row);

        Debug.Assert(removedId == entity);

        var last = record.Archetype.Entities[record.Row];
        _entities.Get(last) = record;
        _entities.Remove(removedId);

        Interlocked.Decrement(ref _entityCount);
    }

    public bool IsAlive(EntityID entity)
        => _entities.Contains(entity);

    private void Attach(EntityID entity, ref EcsComponent meta)
    {
        if (IsDeferred())
        {
            AttachDeferred(entity, ref meta);
        }

        ref var record = ref _entities.Get(entity);
        if (Unsafe.IsNullRef(ref record))
        {
            Debug.Fail("not an entity!");
        }

        var column = record.Archetype.GetComponentIndex(ref meta);
        if (column >= 0)
        {
            return;
        }

        InternalAttachDetach(ref record, ref meta, true);
    }

    private void DetatchComponent(EntityID entity, ref EcsComponent meta)
    {
        if (IsDeferred())
        {
            DetachDeferred(entity, ref meta);
        }

        ref var record = ref _entities.Get(entity);
        if (Unsafe.IsNullRef(ref record))
        {
            Debug.Fail("not an entity!");
        }

        var column = record.Archetype.GetComponentIndex(ref meta);
        if (column < 0)
        {
            return;
        }

        InternalAttachDetach(ref record, ref meta, false);
    }

    private void InternalCreateEntity(EntityID id)
    {
        var row = _archRoot.Add(id);
        ref var record = ref _entities.Add(id, default);
        record.Archetype = _archRoot;
        record.Row = row;

        Interlocked.Increment(ref _entityCount);
    }

    private void InternalAttachDetach(ref EcsRecord record, ref EcsComponent meta, bool add)
    {
        var initType = record.Archetype.Components;
        var cmpCount = Math.Max(0, initType.Length + (add ? 1 : -1));
        Span<EntityID> span = stackalloc EntityID[cmpCount];

        if (!add)
        {
            for (int i = 0, j = 0; i < initType.Length; ++i)
            {
                if (initType[i] != meta.ID)
                {
                    span[j++] = initType[i];
                }
            }
        }
        else if (!span.IsEmpty)
        {
            initType.CopyTo(span);
            span[^1] = meta.ID;
        }

        span.Sort();

        var hash = ComponentHasher.Calculate(span);

#if NET5_0_OR_GREATER
        ref var arch = ref CollectionsMarshal.GetValueRefOrAddDefault(_typeIndex, hash, out var exists);
        if (!exists)
        {
            arch = _archRoot.InsertVertex(record.Archetype, span, ref meta);
        }
#else
        if (!_typeIndex.TryGetValue(hash, out var arch))
        {
			arch = _archRoot.InsertVertex(record.Archetype, span, ref meta);
            _typeIndex[hash] = arch;
		}
#endif

        var newRow = Archetype.MoveEntity(record.Archetype, arch!, record.Row);
        record.Row = newRow;
        record.Archetype = arch!;
    }

    internal void Set(EntityID entity, ref EcsComponent meta, ReadOnlySpan<byte> data)
    {
        ref var record = ref _entities.Get(entity);
        if (Unsafe.IsNullRef(ref record))
        {
            Debug.Fail("not an entity!");
        }

        var buf = record.Archetype.GetComponentRaw(ref meta, record.Row, 1);
        if (buf.IsEmpty)
        {
            Attach(entity, ref meta);
            buf = record.Archetype.GetComponentRaw(ref meta, record.Row, 1);
        }

        Debug.Assert(data.Length == buf.Length);
        data.CopyTo(buf);
    }

    private bool Has(EntityID entity, ref EcsComponent meta)
        => !Get(entity, ref meta).IsEmpty;

    private Span<byte> Get(EntityID entity, ref EcsComponent meta)
    {
        ref var record = ref _entities.Get(entity);
        if (Unsafe.IsNullRef(ref record))
        {
            Debug.Fail("invalid entity");
        }

        return record.Archetype.GetComponentRaw(ref meta, record.Row, 1);
    }

    public unsafe EntityView RegisterSystem(QueryBuilder query, delegate* managed<in Iterator, float, void> func, SystemPhase phase = SystemPhase.OnUpdate)
    {
        var qryID = CreateEntityRaw();
        qryID.Set<EcsQuery>()
             .Set(qryID)
             .Set<EcsEnabled>();

        _queryIndex.Add(qryID, query);

        var id = CreateEntityRaw();
        id.Set(new EcsSystem(qryID, func))
          .Set(id)
          .Set<EcsEnabled>();

        switch (phase)
        {
            case SystemPhase.OnUpdate:
                Set<EcsSystemPhaseOnUpdate>(id);
                break;
            case SystemPhase.OnPreUpdate:
                Set<EcsSystemPhasePreUpdate>(id);
                break;
            case SystemPhase.OnPostUpdate:
                Set<EcsSystemPhasePostUpdate>(id);
                break;

            case SystemPhase.OnStartup:
                Set<EcsSystemPhaseOnStartup>(id);
                break;
            case SystemPhase.OnPreStartup:
                Set<EcsSystemPhasePreStartup>(id);
                break;
            case SystemPhase.OnPostStartup:
                Set<EcsSystemPhasePostStartup>(id);
                break;
        }

        return id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsDeferred() => false; // _deferStatus != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BeginDefer() => Interlocked.Increment(ref _deferStatus);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void EndDefer() => Interlocked.Decrement(ref _deferStatus);

    private void CreateDeferred(EntityID entity)
    {
        ref var cmd = ref PeekDeferredCommand();
        cmd.Action = DeferredOp.Create;
        cmd.Stage = _deferStatus;
        cmd.Create.Entity = entity;
    }

    private void DestroyDeferred(EntityID entity)
    {
        ref var cmd = ref PeekDeferredCommand();
        cmd.Action = DeferredOp.Destroy;
        cmd.Stage = _deferStatus;
        cmd.Destroy.Entity = entity;
    }

    private void AttachDeferred(EntityID entity, ref EcsComponent meta)
    {
        //ref var cmd = ref PeekDeferredCommand();
        //cmd.Action = DeferredOp.Attach;
        //cmd.Stage = _deferStatus;
        //cmd.Attach.Entity = entity;
        //cmd.Attach.Component = component;
    }

    private void DetachDeferred(EntityID entity, ref EcsComponent meta)
    {
        //ref var cmd = ref PeekDeferredCommand();
        //cmd.Action = DeferredOp.Detach;
        //cmd.Stage = _deferStatus;
        //cmd.Detach.Entity = entity;
        //cmd.Detach.Component = component;
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

                    Destroy(cmd.Create.Entity);

                    break;

                case DeferredOp.Attach:

                    //Attach(cmd.Attach.Entity, cmd.Attach.Component);

                    break;

                case DeferredOp.Detach:

                    // Detach(cmd.Detach.Entity, cmd.Detach.Component);

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

        public EntityID Entity;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EcsDeferredDestroyEntity
    {
        public DeferredOp Action;
        public int Stage;

        public EntityID Entity;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EcsDeferredAttachComponent
    {
        public DeferredOp Action;
        public int Stage;

        public EntityID Entity;
        public int Component;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EcsDeferredDetachComponent
    {
        public DeferredOp Action;
        public int Stage;

        public EntityID Entity;
        public int Component;
    }
}


static unsafe class ComponentStorage
{
    public static ref EcsComponent GetOrAdd<T>(World world) where T : unmanaged
    {
        var id = (EntityID)TypeInfo<T>.GlobalIndex;
        ref var meta = ref world._components.Get(id);

        if (Unsafe.IsNullRef(ref meta))
        {
            meta = ref Create(world, id, TypeInfo<T>.Size, TypeInfo<T>.GlobalIndex);
        }

        Debug.Assert(meta.GlobalIndex == TypeInfo<T>.GlobalIndex);
        Debug.Assert(meta.Size == TypeInfo<T>.Size);

        return ref meta;
    }


	public static ref EcsComponent Create(World world, EntityID id, int size, int globalIdx)
    {
        Debug.Assert(globalIdx >= 0);
        Debug.Assert(size >= 0);

        ref var meta = ref world._components.Add(id, new EcsComponent(id, size, globalIdx));

        var ent = world.CreateEntityRaw();
        world.Set(ent, meta);
        world.Set(ent, ent);
        world.Set<EcsEnabled>(ent);

        return ref meta;
    }

    private static class TypeInfo<T> where T : unmanaged
    {
        public static readonly int GlobalIndex = NextID.Get();
        public static readonly int Size = sizeof(T);
    }

    private static class NextID
    {
        private static int _next = -1;
        public static int Get() => Interlocked.Increment(ref _next);
    }
}



sealed unsafe class Archetype
{
    const int ARCHETYPE_INITIAL_CAPACITY = 16;

    private readonly World _world;
    private int _capacity, _count;
    private EntityID[] _entityIDs;
    internal byte[][] _componentsData;
    internal List<EcsEdge> _edgesLeft, _edgesRight;
    private readonly int[] _lookup;
    private readonly EntityID[] _components;

    public Archetype(World world, ReadOnlySpan<EntityID> components)
    {
        _world = world;
        _capacity = ARCHETYPE_INITIAL_CAPACITY;
        _count = 0;
        _components = components.ToArray();
        _entityIDs = new EntityID[ARCHETYPE_INITIAL_CAPACITY];
        _componentsData = new byte[components.Length][];
        _edgesLeft = new List<EcsEdge>();
        _edgesRight = new List<EcsEdge>();


        var maxID = -1;
        for (int i = 0; i < components.Length; i++)
        {
            _components[i] = components[i];

            ref var meta = ref _world._components.Get(components[i]);

            var d = (int)IDOp.RealID(meta.ID);
            maxID = Math.Max(maxID, d);

            Debug.Assert(d == meta.GlobalIndex);
        }

        _lookup = new int[maxID + 1];
        _lookup.AsSpan().Fill(-1);
        for (int i = 0; i < components.Length; ++i)
        {
            ref var meta = ref _world._components.Get(components[i]);
            _lookup[(int)IDOp.RealID(meta.ID)] = i;
        }

        ResizeComponentArray(ARCHETYPE_INITIAL_CAPACITY);
    }


    public int Count => _count;
    public EntityID[] Entities => _entityIDs;
    public EntityID[] Components => _components;


    public int GetComponentIndex(ref EcsComponent meta)
    {
        var index = (int)IDOp.RealID(meta.ID);
        return index >= _lookup.Length ? -1 : _lookup[index];
    }

    public int Add(EntityID entityID)
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

    public EntityID Remove(int row)
    {
        var removed = _entityIDs[row];
        _entityIDs[row] = _entityIDs[_count - 1];

        for (int i = 0; i < _components.Length; ++i)
        {
            ref var meta = ref _world._components.Get(_components[i]);
            var leftArray = _componentsData[i].AsSpan();

            var removeComponent = leftArray.Slice(meta.Size * row, meta.Size);
            var swapComponent = leftArray.Slice(meta.Size * (_count - 1), meta.Size);

            swapComponent.CopyTo(removeComponent);
        }

        --_count;

        return removed;
    }

    public Archetype InsertVertex(Archetype left, ReadOnlySpan<EntityID> newType, ref EcsComponent meta)
    {
        var vertex = new Archetype(left._world, newType);
        MakeEdges(left, vertex, meta.ID);
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

    public Span<byte> GetComponentRaw(ref EcsComponent meta, int row, int count)
    {
        var column = GetComponentIndex(ref meta);
        if (column <= -1)
        {
            return Span<byte>.Empty;
        }

        Debug.Assert(row < Count);

        return _componentsData[column].AsSpan(meta.Size * row, meta.Size * count);
    }

    static void Copy(Archetype from, int fromRow, Archetype to, int toRow)
    {
        var isLeft = to._components.Length < from._components.Length;
        int i = 0, j = 0;
        var count = isLeft ? to._components.Length : from._components.Length;
        var world = from._world;

        ref var x = ref (isLeft ? ref j : ref i);
        ref var y = ref (!isLeft ? ref j : ref i);

        for (; x < count; ++x)
        {
            while (from._components[i] != to._components[j])
            {
                // advance the sign with less components!
                ++y;
            }

            ref var meta = ref world._components.Get(from._components[i]);
            var leftArray = from._componentsData[i].AsSpan();
            var rightArray = to._componentsData[j].AsSpan();
            var insertComponent = rightArray.Slice(meta.Size * toRow, meta.Size);
            var removeComponent = leftArray.Slice(meta.Size * fromRow, meta.Size);
            var swapComponent = leftArray.Slice(meta.Size * (from._count - 1), meta.Size);
            removeComponent.CopyTo(insertComponent);
            swapComponent.CopyTo(removeComponent);
        }
    }

    private static void MakeEdges(Archetype left, Archetype right, EntityID id)
    {
        left._edgesRight.Add(new EcsEdge() { Archetype = right, ComponentID = id });
        right._edgesLeft.Add(new EcsEdge() { Archetype = left, ComponentID = id });
    }

    private void InsertVertex(Archetype newNode)
    {
        var nodeTypeLen = _components.Length;
        var newTypeLen = newNode._components.Length;

        if (nodeTypeLen > newTypeLen - 1)
        {
            return;
        }

        if (nodeTypeLen < newTypeLen - 1)
        {
#if NET5_0_OR_GREATER
            foreach (ref var edge in CollectionsMarshal.AsSpan(_edgesRight))
#else
            foreach (var edge in _edgesRight)
#endif
            {
                edge.Archetype.InsertVertex(newNode);
            }

            return;
        }

        if (!IsSuperset(newNode._components))
        {
            return;
        }

        var i = 0;
        var newNodeTypeLen = newNode._components.Length;
        for (; i < newNodeTypeLen && _components[i] == newNode._components[i]; ++i) { }

        MakeEdges(newNode, this, _components[i]);
    }

    private void ResizeComponentArray(int capacity)
    {
        for (int i = 0; i < _components.Length; ++i)
        {
            ref var meta = ref _world._components.Get(_components[i]);
            Array.Resize(ref _componentsData[i], meta.Size * capacity);
            _capacity = capacity;
        }
    }

    public bool IsSuperset(ReadOnlySpan<EntityID> other)
    {
        int i = 0, j = 0;
        while (i < _components.Length && j < other.Length)
        {
            if (_components[i] == other[j])
            {
                j++;
            }

            i++;
        }

        return j == other.Length;
    }
}

public sealed class QueryBuilder
{
    private readonly World _world;
    private readonly Vec<EntityID> _add, _remove;
    private readonly Stack<Archetype> _stack;
    private Archetype _firstArch;

    internal QueryBuilder(World world)
    {
        _world = world;
        _add = new Vec<EntityID>(16);
        _remove = new Vec<EntityID>(16);
        _stack = new Stack<Archetype>();
    }

    public QueryBuilder With<T>() where T : unmanaged
    {
        ref var meta = ref ComponentStorage.GetOrAdd<T>(_world);
        _add.Add(meta.ID);
        _add.Sort();

        return this;
    }

    public QueryBuilder Without<T>() where T : unmanaged
    {
        ref var meta = ref ComponentStorage.GetOrAdd<T>(_world);
        _remove.Add(meta.ID);
        _remove.Sort();

        return this;
    }

    public QueryBuilder With<TPredicate, TTarget>()
        where TPredicate : unmanaged
        where TTarget : unmanaged
    {
        return With<EcsRelation<TPredicate, TTarget>>();
    }

    public QueryBuilder Without<TPredicate, TTarget>()
        where TPredicate : unmanaged
        where TTarget : unmanaged
    {
        return Without<EcsRelation<TPredicate, TTarget>>();
    }

    public QueryIterator GetEnumerator()
    {
        _stack.Clear();
        if (_firstArch == null)
            _world._typeIndex.TryGetValue(ComponentHasher.Calculate(MemoryMarshal.CreateSpan(ref _add[0], _add.Count)), out _firstArch);
        _stack.Push(_firstArch ?? _world._archRoot);

        return new QueryIterator(_world, _stack, _add, _remove);
    }
}

//public interface IQueryBuilder
//{
//    IQueryBuilder With<T>() where T : unmanaged;

//    IQueryBuilder Without<T>() where T : unmanaged;

//    IQueryBuilder With<TPredicate, TTarget>() where TPredicate : unmanaged where TTarget : unmanaged;

//    IQueryBuilder Without<TPredicate, TTarget>() where TPredicate : unmanaged where TTarget : unmanaged;

//    QueryIterator GetEnumerator();
//}

#if NET5_0_OR_GREATER
[SkipLocalsInit]
#endif
public ref struct QueryIterator
{
    private readonly World _world;
    private readonly Vec<EntityID> _add, _remove;
    private readonly Stack<Archetype> _stack;

    private Archetype? _archetype;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal QueryIterator(World world, Stack<Archetype> stack, Vec<EntityID> add, Vec<EntityID> remove)
    {
        world!.BeginDefer();

        _world = world;
        _add = add;
        _remove = remove;
        _stack = stack;
    }

    public readonly Iterator Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new Iterator
        (
            _world,
            _archetype!
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        do
        {
            if (!_stack.TryPop(out _archetype) || _archetype == null)
                return false;

#if NET5_0_OR_GREATER
            var span = CollectionsMarshal.AsSpan(_archetype._edgesRight);
            if (!span.IsEmpty)
            {
                ref var last = ref span[^1];

                for (int i = 0; i < span.Length; ++i)
                {
                    ref var edge = ref Unsafe.Subtract(ref last, i);

                    // NOTE: maybe breaks when found the _remove componentID
                    if (_remove.IndexOf(edge.ComponentID) < 0)
                    {
                        _stack.Push(edge.Archetype);
                    }
                }
            }
#else
            for (int i = _archetype._edgesRight.Count - 1; i >= 0; --i)
            {
                var edge = _archetype._edgesRight[i];

				if (_remove.IndexOf(edge.ComponentID) < 0)
				{
					_stack.Push(edge.Archetype);
				}
			}
#endif

		}
        while (!_archetype.IsSuperset(_add.Span) || _archetype.Count <= 0);

        return true;
    }

    //public readonly void Reset()
    //{
    //    _stack.Clear();
    //    _stack.Push(_world._archRoot);
    //}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Dispose()
    {
        _stack.Clear();
        _world!.EndDefer();
        _world!.MergeDeferred();
    }
}


#if NET5_0_OR_GREATER
[SkipLocalsInit]
#endif
public ref struct Iterator
{
    private readonly World _world;
    private readonly Archetype _archetype;
    private int _row;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Iterator(World world, [NotNull] Archetype archetype)
    {
        _world = world;
        _archetype = archetype;
        _row = -1;
    }

    // FIXME: maybe we need to keep a variable. Consider when destroy an entity
    public readonly int Count => _archetype.Count;
    public readonly World World => _world;

#if NET7_0_OR_GREATER
    [UnscopedRef]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Field<T> Field<T>() where T : unmanaged
    {
        ref var meta = ref ComponentStorage.GetOrAdd<T>(_world);
		ref var value = ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(_archetype.GetComponentRaw(ref meta, 0, _archetype.Count)));

		fixed (int* ptr = &_row)
        fixed (T* ptr2 = &value)
            return new Field<T>(ptr2, ptr);
    }

#if NET7_0_OR_GREATER
    [UnscopedRef]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Field<EcsRelation<TPredicate, TTarget>> Field<TPredicate, TTarget>()
        where TPredicate : unmanaged
        where TTarget : unmanaged
    {
        return Field<EcsRelation<TPredicate, TTarget>>();
    }

#if NET7_0_OR_GREATER
    [UnscopedRef]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe FieldEnumerator GetEnumerator()
    {
		fixed (int* ptr = &_row)
			return new FieldEnumerator(Count, ptr);
	}
       
}

#if NET5_0_OR_GREATER
[SkipLocalsInit]
#endif
public unsafe ref struct Field<T> where T : unmanaged
{
    //#if NET7_0_OR_GREATER
    //    private readonly ref T _first;
    //    private readonly ref int _row;
    //#else
    //    private readonly Ref<T> _first;
    //    private readonly Ref<int> _row;
    //#endif

    private readonly T* _first;
    private int* _row;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Field(T* first, int* row)
    {
        _first = first;
        _row = row;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T Get() => ref Unsafe.Add(ref *_first, *_row);
}

#if NET5_0_OR_GREATER
[SkipLocalsInit]
#endif
public unsafe readonly ref struct FieldEnumerator
{
    private readonly int _count;

    //#if NET7_0_OR_GREATER
    //    private readonly ref int _row;
    //#else
    //    private readonly Ref<int> _row;
    //#endif

    private readonly int* _row;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal FieldEnumerator(int count, int* row)
    {
        _count = count;
        _row = row;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool MoveNext() => ++*_row < _count;

    public readonly int Current => *_row;
}


struct EcsEdge
{
    public EntityID ComponentID;
    public Archetype Archetype;
}

struct EcsRecord
{
    public Archetype Archetype;
    public int Row;
}

static class ComponentHasher
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EntityID Calculate(Span<EntityID> components)
    {
        unchecked
        {
            EntityID hash = 5381;

            foreach (ref readonly var id in components)
            {
                hash = ((hash << 5) + hash) + id;
            }

            return hash;
        }
    }
}



static class IDOp
{
    public static void Toggle(ref EntityID id)
    {
        id ^= EcsConst.ECS_TOGGLE;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EntityID GetGeneration(EntityID id)
    {
        return ((id & EcsConst.ECS_GENERATION_MASK) >> 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EntityID IncreaseGeneration(EntityID id)
    {
        return ((id & ~EcsConst.ECS_GENERATION_MASK) | ((0xFFFF & (GetGeneration(id) + 1)) << 32));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EntityID RealID(EntityID id)
    {
        return id &= EcsConst.ECS_ENTITY_MASK;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasFlag(EntityID id, byte flag)
    {
        return (id & flag) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsComponent(EntityID id)
    {
        return (id & EcsConst.ECS_COMPONENT_MASK) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EntityID SetAsComponent(EntityID id)
    {
        return id |= EcsConst.ECS_ID_FLAGS_MASK;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EntityID Pair(EntityID first, EntityID second)
    {
        return EcsConst.ECS_PAIR | ((first << 32) + (uint)second);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPair(EntityID id)
    {
        return ((id) & EcsConst.ECS_ID_FLAGS_MASK) == EcsConst.ECS_PAIR;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EntityID GetPairFirst(EntityID id)
    {
        return (uint)((id & EcsConst.ECS_COMPONENT_MASK) >> 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EntityID GetPairSecond(EntityID id)
    {
        return (uint)id;
    }
}

public sealed partial class World
{
    public void Set<T>(EntityID entity, T component = default) where T : unmanaged
    {
        ref var meta = ref ComponentStorage.GetOrAdd<T>(this);
        Set(entity, ref meta, MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref component), meta.Size));
    }

    public void Unset<T>(EntityID entity) where T : unmanaged
       => DetatchComponent(entity, ref ComponentStorage.GetOrAdd<T>(this));

    public bool Has<T>(EntityID entity) where T : unmanaged
        => Has(entity, ref ComponentStorage.GetOrAdd<T>(this));

    public unsafe ref T Get<T>(EntityID entity) where T : unmanaged
    {
        var raw = Get(entity, ref ComponentStorage.GetOrAdd<T>(this));

        Debug.Assert(!raw.IsEmpty);
        Debug.Assert(sizeof(T) == raw.Length);

        return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(raw));
    }


	// public void AttachTo(EntityID id, EntityID parent)
	// {
	//     Detach(id);

	//     var t = new EntityView(_worldID, id);

	//     if (Has<EcsParent>(parent))
	//     {
	//         ref var p = ref Get<EcsParent>(parent);
	//         p.ChildrenCount += 1;

	//         ref var prev = ref p.FirstChild.Get<EcsChild>().Prev;
	//         ref var next = ref p.FirstChild.Get<EcsChild>().Next;

	//         if (prev.Has<EcsChild>())
	//             prev.Get<EcsChild>().Next = t;

	//if (next.Has<EcsChild>())
	//	next.Get<EcsChild>().Prev = t;

	//         Set(id, new EcsChild()
	//         {
	//             Parent = new EntityView(_worldID, parent),
	//             Next = prev,
	//             Prev = next
	//         });

	//         return;
	//     }

	//     Set(id, new EcsChild()
	//     {
	//         Parent = new EntityView(_worldID, parent),
	//         Next = t,
	//         Prev = t
	//     });

	//     Set(parent, new EcsParent()
	//     {
	//         ChildrenCount = 1,
	//         FirstChild = t
	//     });
	// }

	// public void Detach(EntityID id)
	// {
	//     if (!Has<EcsChild>(id))
	//         return;

	//     ref var child = ref Get<EcsChild>(id);
	//     ref var parent = ref child.Parent.Get<EcsParent>();

	//     parent.ChildrenCount -= 1;

	//     if (parent.ChildrenCount == 0)
	//     {
	//         child.Parent.Unset<EcsParent>();
	//     }
	//     else
	//     {
	//         if (parent.FirstChild == id)
	//         {
	//             parent.FirstChild = child.Next;
	//         }

	//         child.Prev.Get<EcsChild>().Next = child.Next;
	//         child.Next.Get<EcsChild>().Prev = child.Prev;
	//     }

	//     Unset<EcsChild>(id);
	// }


	public void AttachTo(EntityID childID, EntityID parentID)
	{
		Detach(childID);

		if (Has<EcsParent>(parentID))
		{
			ref var parent = ref Get<EcsParent>(parentID);
			parent.ChildrenCount += 1;

			ref var firstChild = ref Get<EcsChild>(parent.FirstChild);
			firstChild.Prev = childID;

			Set(childID, new EcsChild()
			{
				Parent = parentID,
				Prev = 0,
				Next = parent.FirstChild
			});

			parent.FirstChild = childID;

			return;
		}

		Set(parentID, new EcsParent()
		{
			ChildrenCount = 1,
			FirstChild = childID
		});

		Set(childID, new EcsChild()
		{
			Parent = parentID,
			Prev = 0,
			Next = 0
		});
	}

	public void Detach(EntityID id)
	{
		if (!Has<EcsChild>(id))
			return;

		Console.WriteLine("detach {0}", id);

		ref var child = ref Get<EcsChild>(id);
		ref var parent = ref Get<EcsParent>(child.Parent);

		parent.ChildrenCount -= 1;

		if (parent.ChildrenCount <= 0)
		{
			Unset<EcsParent>(child.Parent);
		}
		else
		{
			if (parent.FirstChild == id)
			{
				parent.FirstChild = child.Next;
				child.Prev = 0;
			}
			else
			{
				if (child.Prev != 0)
				{
					Get<EcsChild>(child.Prev).Next = child.Next;
				}

				if (child.Next != 0)
				{
					Get<EcsChild>(child.Next).Prev = child.Prev;
				}
			}

		}

		Unset<EcsChild>(id);
	}

	public void RemoveChildren(EntityID id)
    {
        if (!Has<EcsParent>(id))
            return;

        ref var parent = ref Get<EcsParent>(id);

        while (parent.ChildrenCount > 0)
        {
            Detach(parent.FirstChild);
        }
    }
}



class Vec<T0> where T0 : unmanaged
{
    private T0[] _array;
    private int _count;

    public Vec(int initialSize = 2)
    {
        _array = new T0[initialSize];
        _count = 0;
    }

    public int Count => _count;
    public ref T0 this[int index] => ref _array[index];
    public ReadOnlySpan<T0> Span => _count <= 0 ? ReadOnlySpan<T0>.Empty : MemoryMarshal.CreateReadOnlySpan(ref _array[0], _count);

    public void Add(in T0 elem)
    {
        GrowIfNecessary(_count + 1);

        this[_count] = elem;

        ++_count;
    }

    public void Clear()
    {
        _count = 0;
    }

    public void Sort() => Array.Sort(_array, 0, _count);

    public int IndexOf(T0 item) => Array.IndexOf(_array, item, 0, _count);

    private void GrowIfNecessary(int length)
    {
        if (length >= _array.Length)
        {
            var newLength = _array.Length > 0 ? _array.Length * 2 : 2;
            while (length >= newLength)
                newLength *= 2;
            Array.Resize(ref _array, newLength);
        }
    }
}

sealed class EntitySparseSet<T>
{
    private struct Chunk
    {
        public int[] Sparse;
        public T[] Values;
    }

    const int CHUNK_SIZE = 4096;

    private Chunk[] _chunks;
    private int _count;
    private EntityID _maxID;
    private Vec<EntityID> _dense;

    public EntitySparseSet()
    {
        _dense = new Vec<EntityID>();
        _chunks = new Chunk[0];
        _count = 1;
        _maxID = EntityID.MinValue;

        _dense.Add(0);
    }

    public int Length => _count - 1;



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T CreateNew(out EntityID id)
    {
        var count = _count++;
        var denseCount = _dense.Count;

        Debug.Assert(count <= denseCount);

        if (count < denseCount)
        {
            id = _dense[count];
        }
        else
        {
            id = NewID(count);
        }


        ref var chunk = ref GetChunk((int)id >> 12);
                
        if (Unsafe.IsNullRef(ref chunk) || chunk.Sparse == null)
            return ref Unsafe.NullRef<T>();

        return ref chunk.Values[(int)id & 0xFFF];
    }

    private EntityID NewID(int dense)
    {
        var index = ++_maxID;
        _dense.Add(0);

        ref var chunk = ref GetChunkOrCreate((int)index >> 12);
        Debug.Assert(chunk.Sparse[(int)index & 0xFFF] == 0);

        SparseAssignIndex(ref chunk, index, dense);

        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(EntityID outerIdx)
    {
        ref var chunk = ref GetChunk((int)outerIdx >> 12);
        if (Unsafe.IsNullRef(ref chunk) || chunk.Sparse == null)
            return ref Unsafe.NullRef<T>();

        var gen = SplitGeneration(ref outerIdx);
        var realID = (int)outerIdx & 0xFFF;

        var dense = chunk.Sparse[realID];
        if (dense == 0 || dense >= _count)
            return ref Unsafe.NullRef<T>();

        var curGen = _dense[dense] & EcsConst.ECS_GENERATION_MASK;
        if (gen != curGen)
            return ref Unsafe.NullRef<T>();

        return ref chunk.Values[realID];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(EntityID outerIdx)
        => !Unsafe.IsNullRef(ref Get(outerIdx));

    public ref T Add(EntityID outerIdx, T value)
    {
        var gen = SplitGeneration(ref outerIdx);
        var realID = (int)outerIdx & 0xFFF;
        ref var chunk = ref GetChunkOrCreate((int)outerIdx >> 12);
        var dense = chunk.Sparse[realID];

        if (dense != 0)
        {
            var count = _count;
            if (dense == count)
            {
                _count++;
            }
            else if (dense > count)
            {
                SwapDense(ref chunk, dense, count);
                dense = count;
                _count++;
            }

            Debug.Assert(gen == 0 || _dense[dense] == (outerIdx | gen));
        }
        else
        {
            _dense.Add(0);

            var denseCount = _dense.Count - 1;
            var count = _count++;

            if (outerIdx >= _maxID)
            {
                _maxID = outerIdx;
            }

            if (count < denseCount)
            {
                var unused = _dense[count];
                ref var unusedChunk = ref GetChunkOrCreate((int)unused >> 12);
                SparseAssignIndex(ref unusedChunk, unused, denseCount);
            }

            SparseAssignIndex(ref chunk, outerIdx, count);
            _dense[count] |= gen;
        }

        chunk.Values[realID] = value;
        return ref chunk.Values[realID];
    }

    public void Remove(EntityID outerIdx)
    {
        ref var chunk = ref GetChunk((int)outerIdx >> 12);
        if (Unsafe.IsNullRef(ref chunk) || chunk.Sparse == null)
            return;

        var gen = SplitGeneration(ref outerIdx);
        var realID = (int)outerIdx & 0xFFF;
        var dense = chunk.Sparse[realID];

        if (dense == 0)
            return;

        var curGen = _dense[dense] & EcsConst.ECS_GENERATION_MASK;
        if (gen != curGen)
        {
            return;
        }

        _dense[dense] = outerIdx | IDOp.IncreaseGeneration(curGen);

        var count = _count;
        if (dense == (count - 1))
        {
            _count--;
        }
        else if (dense < count)
        {
            SwapDense(ref chunk, dense, count - 1);
            _count--;
        }
        else
        {
            return;
        }

        chunk.Values[realID] = default!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _count = 1;
        _maxID = uint.MinValue;
        Array.Clear(_chunks, 0, _chunks.Length);
        _dense.Clear();
        _dense.Add(0);
    }

    private void SwapDense(ref Chunk chunkA, int a, int b)
    {
        Debug.Assert(a != b);

        var idxA = _dense[a];
        var idxB = _dense[b];

        ref var chunkB = ref GetChunkOrCreate((int)idxB >> 12);
        SparseAssignIndex(ref chunkA, idxA, b);
        SparseAssignIndex(ref chunkB, idxB, a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SparseAssignIndex(ref Chunk chunk, EntityID index, int dense)
    {
        chunk.Sparse[(int)index & 0xFFF] = dense;
        _dense[dense] = index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static EntityID SplitGeneration(ref EntityID index)
    {
        //if ((index & EcsConst.ECS_ID_FLAGS_MASK) != 0)
        //{
        //    index &= ~(EcsConst.ECS_GENERATION_MASK | EcsConst.ECS_ID_FLAGS_MASK);
        //    //return 0;
        //}

        var gen = index & EcsConst.ECS_GENERATION_MASK;
        Debug.Assert(gen == (index & (0xFFFF_FFFFul << 32)));
        index -= gen;
        return gen;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref Chunk GetChunk(int index)
        => ref (index >= _chunks.Length ? ref Unsafe.NullRef<Chunk>() : ref _chunks[index]);

    private ref Chunk GetChunkOrCreate(int index)
    {
        if (index >= _chunks.Length)
        {
            var oldLength = _chunks.Length;
            var newLength = oldLength > 0 ? oldLength << 1 : 2;
            while (index >= newLength)
                newLength <<= 1;

            Array.Resize(ref _chunks, newLength);
        }

        ref var chunk = ref _chunks[index];

        if (chunk.Sparse == null)
        {
            chunk.Sparse = new int[CHUNK_SIZE];
            chunk.Values = new T[CHUNK_SIZE];
        }

        return ref chunk;
    }
}


static class EcsConst
{
    public const EntityID ECS_ENTITY_MASK = 0xFFFFFFFFul;
    public const EntityID ECS_GENERATION_MASK = (0xFFFFul << 32);
    public const EntityID ECS_ID_FLAGS_MASK = (0xFFul << 60);
    public const EntityID ECS_COMPONENT_MASK = ~ECS_ID_FLAGS_MASK;

    public const EntityID ECS_TOGGLE = 1ul << 61;
    public const EntityID ECS_PAIR = 1ul << 63;
}

public readonly struct EcsComponent
{
    public readonly EntityID ID;
    public readonly int Size;
    public readonly int GlobalIndex;

    public EcsComponent(EntityID id, int size, int globalIndex)
    {
        ID = id;
        Size = size;
        GlobalIndex = globalIndex;
    }
}

public unsafe struct EcsQuery
{
    //private fixed int _add[32];
    //private fixed int _remove[32];
}

public unsafe readonly struct EcsSystem
{
    public readonly EntityID Query;
    public readonly delegate* managed<in Iterator, float, void> Func;

    public EcsSystem(EntityID query, delegate* managed<in Iterator, float, void> func)
    {
        Query = query;
        Func = func;
    }
}


public readonly struct EcsRelation<TAction, TTarget>
    where TAction : unmanaged
    where TTarget : unmanaged
{
    public readonly TTarget Target;

    public EcsRelation() => Target = default;
    public EcsRelation(in TTarget target) => Target = target;
}

public struct EcsParent
{
    public int ChildrenCount;
    public EntityID FirstChild;
}

public struct EcsChild
{
    public EntityID Parent;
    public EntityID Prev, Next;
}

public readonly struct EcsEnabled { }

public struct EcsSystemPhaseOnUpdate { }
public struct EcsSystemPhasePreUpdate { }
public struct EcsSystemPhasePostUpdate { }
public struct EcsSystemPhaseOnStartup { }
public struct EcsSystemPhasePreStartup { }
public struct EcsSystemPhasePostStartup { }

public enum SystemPhase
{
    OnUpdate,
    OnPreUpdate,
    OnPostUpdate,

    OnStartup,
    OnPreStartup,
    OnPostStartup
}


#if NET5_0_OR_GREATER
[SkipLocalsInit]
#endif
[StructLayout(LayoutKind.Sequential)]
public readonly struct EntityView : IEquatable<EntityID>, IEquatable<EntityView>
{
    public readonly EntityID ID;
    public readonly EntityID WorldID;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EntityView(EntityID world, EntityID id)
    {
        WorldID = world;
        ID = id;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(ulong other)
    {
        return ID == other;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(EntityView other)
    {
        return ID == other.ID;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Set<T>(T component = default) where T : unmanaged
    {
        World._allWorlds.Get(WorldID).Set(ID, component);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Unset<T>() where T : unmanaged
    {
        World._allWorlds.Get(WorldID).Unset<T>(ID);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Set<TPredicate, TTarget>()
        where TPredicate : unmanaged
        where TTarget : unmanaged
    {
        World._allWorlds.Get(WorldID).Set<EcsRelation<TPredicate, TTarget>>(ID);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Unset<TPredicate, TTarget>()
        where TPredicate : unmanaged
        where TTarget : unmanaged
    {
        World._allWorlds.Get(WorldID).Unset<EcsRelation<TPredicate, TTarget>>(ID);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Set<TPredicate>(in EntityView targetID)
        where TPredicate : unmanaged
    {
        var world = World._allWorlds.Get(WorldID);
        world.Set(ID, new EcsRelation<TPredicate, EntityView>(in targetID));

        return this;
    }

    public readonly EntityView AttachTo(EntityView parent)
    {
        World._allWorlds.Get(WorldID).AttachTo(ID, parent);
        return this;
    }

    public readonly EntityView Detach()
    {
        World._allWorlds.Get(WorldID).Detach(ID);
        return this;
    }

    public readonly EntityView RemoveChildren()
    {
        World._allWorlds.Get(WorldID).RemoveChildren(ID);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Enable()
    {
        World._allWorlds.Get(WorldID).Set<EcsEnabled>(ID);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly EntityView Disable()
    {
        World._allWorlds.Get(WorldID).Unset<EcsEnabled>(ID);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T Get<T>() where T : unmanaged
        => ref World._allWorlds.Get(WorldID).Get<T>(ID);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Has<T>() where T : unmanaged
        => World._allWorlds.Get(WorldID).Has<T>(ID);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref EcsRelation<TPredicate, TTarget> Get<TPredicate, TTarget>()
        where TPredicate : unmanaged where TTarget : unmanaged
        => ref Get<EcsRelation<TPredicate, TTarget>>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Has<TPredicate, TTarget>()
        where TPredicate : unmanaged where TTarget : unmanaged
        => Has<EcsRelation<TPredicate, TTarget>>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Destroy()
        => World._allWorlds.Get(WorldID).Destroy(ID);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsAlive()
        => World._allWorlds.Get(WorldID).IsAlive(ID);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsEnabled()
        => Has<EcsEnabled>();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator EntityID(in EntityView d) => d.ID;


    public static readonly EntityView Invalid = new(0, 0);
}

public sealed class Commands : IDisposable
{
    private readonly World _main, _mergeWorld;
    private readonly QueryBuilder _entityCreated, _entityDestroyed, _componentAdded, _componentRemoved;

    public Commands(World main)
    {
        _main = main;
        _mergeWorld = new World();

		_entityCreated = _mergeWorld.Query()
            .With<EntityCreated>();

		_entityDestroyed = _mergeWorld.Query()
            .With<EntityDestroyed>();

		_componentAdded = _mergeWorld.Query()
            .With<ComponentAdded>();

		_componentRemoved = _mergeWorld.Query()
            .With<ComponentRemoved>();
    }

    public void MergeChanges()
    {
        ref var created = ref ComponentStorage.GetOrAdd<EntityCreated>(_mergeWorld);
		ref var destroyed = ref ComponentStorage.GetOrAdd<EntityDestroyed>(_mergeWorld);

		foreach (var it in _entityCreated)
        {
            var entityA = it.Field<EntityView>();
            var createdOp = it.Field<EntityCreated>();

            foreach (var row in it)
            {
                ref var e = ref entityA.Get();
                ref var op = ref createdOp.Get();

                var target = _main.CreateEntityRaw();

                ref var record = ref it.World._entities.Get(e);
                Debug.Assert(!Unsafe.IsNullRef(ref record));

                foreach (var cmp in record.Archetype.Components)
                {
                    if (created.ID == cmp || destroyed.ID == cmp)
                        continue;

                    ref var meta = ref _main._components.Get(cmp);
                    if (Unsafe.IsNullRef(ref meta))
                    {
                        ref var mergemeta = ref _mergeWorld._components.Get(cmp);
                        meta = ref ComponentStorage.Create(_main, cmp, mergemeta.Size, mergemeta.GlobalIndex);
					}

                    _main.Set(target, ref meta, record.Archetype.GetComponentRaw(ref meta, record.Row, 1));
                }

				_main.Set(target, new EntityView(_main._worldID, target));
                _main.Set<EcsEnabled>(target);

				e.Destroy();
			}
        }

		foreach (var it in _entityDestroyed)
		{
			var entityA = it.Field<EntityView>();
			var destroyedOp = it.Field<EntityDestroyed>();

			foreach (var row in it)
			{
				ref var e = ref entityA.Get();
                ref var op = ref destroyedOp.Get();

				_main.Destroy(op.Target);

				e.Destroy();
			}
		}
	}

    public EntityView Entity()
    {
		var e = _mergeWorld.Entity();
        //var target = _main.CreateEntityRaw();

		return e.Set(new EntityCreated()
        {
            Target = 0
		});
	}

    public void Destroy(EntityView entity)
    {
        Debug.Assert(entity.WorldID == _main._worldID);

        entity.Disable();

		_mergeWorld.Entity()
            .Set(new EntityDestroyed()
		    {
			    Target = entity
		    });
	}

	public void Dispose()
	{
        _mergeWorld?.Dispose();
	}

	struct EntityCreated 
    {
        public EntityID Target;
    }
    struct EntityDestroyed 
    {
        public EntityID Target;
    }
    struct ComponentAdded { }
    struct ComponentRemoved { }
}


#if NETSTANDARD2_1
internal readonly ref struct Ref<T>
{
    private readonly Span<T> span;

    public Ref(ref T value)
    {
        span = MemoryMarshal.CreateSpan(ref value, 1);
    }

    public ref T Value => ref MemoryMarshal.GetReference(span);
}

public static class SortExtensions
{
	public static void Sort<T>(this Span<T> span) where T : IComparable<T>
	{
		for (int i = 0; i < span.Length - 1; i++)
		{
			for (int j = 0; j < span.Length - i - 1; j++)
			{
				if (span[j].CompareTo(span[j + 1]) > 0)
				{
					// Swap the elements
					T temp = span[j];
					span[j] = span[j + 1];
					span[j + 1] = temp;
				}
			}
		}
	}
}
#endif
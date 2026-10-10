// The component / resource / event registry of the tinyecs:modding API. Maps a WIT
// `type-path` string to a concrete TinyEcs component / App resource / event, so a mod can
// query and mutate host state by name. AOT-safe: every entry is a closed generic (no
// reflection).
//
// A value crosses the mod boundary only in its binary encoding (Encoding.Typed,
// ModBinary.cs), through a codec the host registers per path (RegisterBinary* — usually
// generated from the WIT records of its typed interface). A tag carries no value
// (RegisterTag*). Every path a mod reads or writes must have one or the other.
//
// The host (e.g. the game) populates a ModComponentRegistry with the components and
// resources it chooses to expose, then hands it to the plugin via ModdingConfig. This
// file is the mechanism only — it knows no concrete game type.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Collections;

namespace TinyEcs.Bevy.Modding;

/// Grow-only byte sink for ONE value. Each pooled CompValue owns its own instance because
/// CompValue.Data is a Memory over this buffer and FlatSharp reads it after every row is
/// filled — a shared arena would relocate on growth and dangle the slices handed out
/// before it grew.
internal sealed class ModByteBuffer : IBufferWriter<byte>
{
    private byte[] _buf = new byte[64];
    private int _len;

    public void Reset() => _len = 0;
    public Memory<byte> Written => new(_buf, 0, _len);
    public ReadOnlySpan<byte> WrittenSpan => _buf.AsSpan(0, _len);

    public void Advance(int count) => _len += count;
    public Memory<byte> GetMemory(int sizeHint = 0) { Grow(sizeHint); return _buf.AsMemory(_len); }
    public Span<byte> GetSpan(int sizeHint = 0) { Grow(sizeHint); return _buf.AsSpan(_len); }

    private void Grow(int sizeHint)
    {
        if (sizeHint <= 0)
            sizeHint = 64;
        if (_buf.Length - _len >= sizeHint)
            return;
        Array.Resize(ref _buf, Math.Max(_buf.Length * 2, _len + sizeHint));
    }
}

/// An observer fire carrying the component / event value in its binary encoding (empty
/// for a tag). The span is valid only for the duration of the call — a consumer that
/// keeps it copies it.
public delegate void ModValueFire(ulong entity, ReadOnlySpan<byte> value);

/// One registered component type, keyed by WIT type-path: its presence and change
/// detection. The value goes through the path's binary view (ModComponentRegistry.TryGetBinary).
public interface IModComponent
{
    bool Has(World world, ulong entity);
    void CollectEntities(World world, ref PooledList<ulong> into);
    void Remove(World world, ulong entity);

    // Change detection for the `Changed` query term. DEFAULT implementations degrade to
    // presence, so a host's hand-written IModComponent mapper (game registries define
    // their own) compiles and behaves unchanged — a Changed term over such a mapper is
    // just a With term. ModComponent<T> overrides both with real column tick reads.
    /// True when this component's changed-tick on `entity` is STRICTLY NEWER than
    /// `tick`, under wrapping comparison (ChangeTick.IsNewerThan) — the same
    /// `(lastRun, thisRun]` window Changed&lt;T&gt; uses.
    bool ChangedSince(World world, ulong entity, uint tick) => Has(world, entity);

    /// Every entity whose component changed strictly after `sinceTick` (driver-term path).
    void CollectChangedEntities(World world, uint sinceTick, ref PooledList<ulong> into)
        => CollectEntities(world, ref into);

    /// `Added` query term: true when the component was ADDED to `entity` strictly after
    /// `tick`, same wrapping window as ChangedSince. The default degrades to presence.
    bool AddedSince(World world, ulong entity, uint tick) => Has(world, entity);

    /// Archetype-query fast path of the pushed snapshot (ModdingPlugin.BuildSnapshot):
    /// a plain column-backed mapper adds its term (`with`, or `without`) to `qb` and
    /// returns true, so the snapshot iterates only the archetypes that match every such
    /// term. The DEFAULT (false) marks a projection - a mapper whose presence / change
    /// is not one column - which the snapshot evaluates per entity through Has /
    /// ChangedSince / AddedSince.
    bool AddQueryTerm(QueryBuilder qb, bool without) => false;

    /// For a mapper that added a term: the iterator's current archetype's changed
    /// (`added` = false) or added ticks of this component, in Entities() order. EMPTY =
    /// tickless (a zero-size tag, a presence-only mapper): its Changed / Added mean
    /// presence, which the archetype match already proved.
    Span<uint> ColumnTicks(ref QueryIterator it, bool added) => default;

    /// The host owns this component: a mod may read it but not write it (a `Mut` query
    /// term fails the mod's setup with the path named; a typed insert traps). DEFAULT
    /// false, so a host's hand-written mapper stays writable unless it says otherwise.
    bool ReadOnly => false;
}

/// Typed access to a component mapper's payload: the value a mod reads and writes, as
/// the struct itself. ModComponent&lt;T&gt; implements it; a hand-written mapper implements
/// it for its DTO. The path's binary codec (RegisterBinaryComponent) goes through it.
public interface IModComponentOf<T> where T : struct
{
    /// The payload of `entity` (which has the component).
    T Get(World world, ulong entity);

    /// Inserts / overwrites the component from its payload.
    void Set(World world, ulong entity, in T value);
}

/// Typed access to a resource mapper's payload (see IModComponentOf). `modName` is the
/// mod reading / writing: a host resource that holds PER-MOD state (a mod's own key
/// bindings) serves that mod's slice; a whole-resource mapper ignores it.
public interface IModResourceOf<T>
{
    /// False when the host has no such resource right now.
    bool TryGet(App app, string modName, out T value);

    void Set(App app, in T value, string modName);
}

/// Typed access to an event mapper's payload (see IModComponentOf): a game's typed
/// import emits the struct, and a typed observer / event reader takes each fire's payload
/// as the struct. ModEvent&lt;T&gt; implements it.
public interface IModEventOf<T> where T : struct
{
    /// Emits the event (the host trigger).
    void Emit(World world, ulong entity, in T value);

    /// A host observer handed each fire's payload; `wanted` is asked first.
    void Observe(App app, Func<bool> wanted, Action<ulong, T> onFire);
}

/// A component mapper whose insert / remove triggers hand over the payload as the struct.
/// Every component a mod observes needs it (the trigger value crosses through the codec).
public interface IModComponentObserverOf<T> where T : struct
{
    void ObserveInsert(App app, Func<bool> wanted, Action<ulong, T> onFire);

    void ObserveRemove(App app, Func<bool> wanted, Action<ulong, T> onFire);
}

/// Maps a placeholder entity id from a mod's command buffer to the real one (0 when the
/// placeholder names nothing). Any other id is returned unchanged.
public delegate ulong ModEntityResolver(ulong id);

/// Rewrites a component's entity fields through `resolve` (see ModComponent's
/// entityFields) — e.g. `(v, r) => { v.Parent = r(v.Parent); return v; }`.
public delegate T ModEntityFields<T>(T value, ModEntityResolver resolve);

/// Entity ids a guest writes into a value before it knows the real id: an entity spawned
/// earlier in the same command buffer is `1 << 63 | temp_id`.
public static class ModEntityRef
{
    public const ulong PlaceholderBit = 1UL << 63;

    public static bool IsPlaceholder(ulong id) => (id & PlaceholderBit) != 0;

    public static uint TempId(ulong id) => (uint)(id & 0xFFFF_FFFFUL);
}

/// `entityFields` (optional) names the component's entity-id fields (a placeholder a mod
/// wrote there resolves to the entity it spawned in the same command buffer; code
/// generators read it to type those fields as entities). `readOnly` marks a component
/// the host owns (see IModComponent.ReadOnly).
public sealed class ModComponent<T>(ModEntityFields<T>? entityFields = null, bool readOnly = false) : IModComponent, IModComponentOf<T>, IModComponentObserverOf<T> where T : struct
{
    public bool ReadOnly => readOnly;

    /// See the constructor.
    public ModEntityFields<T>? EntityFields => entityFields;

    // ponytail: cached on first use, not in the ctor — the ctor has no World.
    // Built once and reused; Query.Match() re-resolves archetypes lazily.
    // Single-world assumption (one registry per App/World).
    private Query? _query;

    // TinyEcs classifies a FIELDLESS struct as a zero-size TAG: it has no column, so
    // World.Get panics on one ("this is not a component"). Plenty of registered paths
    // are tags (`struct UiMovable;`, scene/gump markers…), and a mod reads them like any
    // other component — through a Ref query term — where a throw traps the guest. A tag
    // carries no data. Probed the way TinyEcs sizes components (Lookup.GetSize; credit
    // BeanCheeseBurrito from Flecs.NET): a 1-byte struct whose single byte doesn't
    // participate in equality has no fields.
    private static readonly bool IsTag = ProbeIsTag();

    private static bool ProbeIsTag()
    {
        if (System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<T>()
            || System.Runtime.CompilerServices.Unsafe.SizeOf<T>() != 1)
            return false;

        System.Runtime.CompilerServices.Unsafe.SkipInit<T>(out var a);
        System.Runtime.CompilerServices.Unsafe.SkipInit<T>(out var b);
        System.Runtime.CompilerServices.Unsafe.As<T, byte>(ref a) = 0x7F;
        System.Runtime.CompilerServices.Unsafe.As<T, byte>(ref b) = 0xFF;
        return ValueType.Equals(a, b);
    }

    public bool Has(World world, ulong entity) => world.Has<T>(entity);

    public void CollectEntities(World world, ref PooledList<ulong> into)
    {
        // QueryBuilder (by component id) instead of Query<Data<T>>: works for
        // zero-size marker components too, which Data<T> mis-casts.
        var q = _query ??= world.QueryBuilder().With<T>().Build();
        var it = q.Iter();
        while (it.Next())
            foreach (var ev in it.Entities())
                into.Add(ev.ID);
    }

    public void Remove(World world, ulong entity) => world.Entity(entity).Unset<T>();

    public T Get(World world, ulong entity) => IsTag ? default : world.Get<T>(entity);

    public void Set(World world, ulong entity, in T value) => world.Set(entity, IsTag ? default : value);

    // A TAG has no column, hence no changed-tick — World.GetChangedTick returns 0 for
    // one, which would make a Changed term over a tag match nothing. Degrade to
    // presence there (same contract as the interface default).
    public bool ChangedSince(World world, ulong entity, uint tick)
        => IsTag
            ? world.Has<T>(entity)
            : world.Has<T>(entity) && ChangeTick.IsNewerThan(world.GetChangedTick<T>(entity), tick, world.CurrentTick);

    public bool AddedSince(World world, ulong entity, uint tick)
        => IsTag
            ? world.Has<T>(entity)
            : world.Has<T>(entity) && ChangeTick.IsNewerThan(world.GetAddedTick<T>(entity), tick, world.CurrentTick);

    public bool AddQueryTerm(QueryBuilder qb, bool without)
    {
        if (without)
            qb.Without<T>();
        else
            qb.With<T>();
        return true;
    }

    public Span<uint> ColumnTicks(ref QueryIterator it, bool added)
    {
        if (IsTag)
            return default;
        var i = it.GetColumnIndexOf<T>();
        return i < 0 ? default : added ? it.GetAddedTicks(i) : it.GetChangedTicks(i);
    }

    public void CollectChangedEntities(World world, uint sinceTick, ref PooledList<ulong> into)
    {
        var q = _query ??= world.QueryBuilder().With<T>().Build();
        var now = world.CurrentTick;
        var it = q.Iter();
        while (it.Next())
            foreach (var ev in it.Entities())
                if (IsTag || ChangeTick.IsNewerThan(world.GetChangedTick<T>(ev.ID), sinceTick, now))
                    into.Add(ev.ID);
    }

    public void ObserveInsert(App app, Func<bool> wanted, Action<ulong, T> onFire)
        => app.AddObserver<OnInsert<T>>(t =>
        {
            if (wanted())
                onFire(t.EntityId, IsTag ? default : t.Component);
        });

    public void ObserveRemove(App app, Func<bool> wanted, Action<ulong, T> onFire)
        => app.AddObserver<OnRemove<T>>(t =>
        {
            if (wanted())
                onFire(t.EntityId, IsTag ? default : t.Component);
        });
}

/// Presence-only component exposure: a mod queries `with <path>` to FIND the entity (and
/// may despawn it), observes its insert / remove, but never reads or writes its contents.
/// For markers whose raw struct isn't meaningful to a mod (holds a Dictionary / HashSet /
/// engine ref). Registered as a tag (RegisterTagComponent).
public sealed class ModPresence<T> : IModComponent, IModComponentOf<T>, IModComponentObserverOf<T> where T : struct
{
    private Query? _query;

    public bool ReadOnly => true;

    public bool Has(World world, ulong entity) => world.Has<T>(entity);

    public bool AddQueryTerm(QueryBuilder qb, bool without)
    {
        if (without)
            qb.Without<T>();
        else
            qb.With<T>();
        return true;
    }

    public void CollectEntities(World world, ref PooledList<ulong> into)
    {
        var q = _query ??= world.QueryBuilder().With<T>().Build();
        var it = q.Iter();
        while (it.Next())
            foreach (var ev in it.Entities())
                into.Add(ev.ID);
    }

    public void Remove(World world, ulong entity) => world.Entity(entity).Unset<T>();

    public T Get(World world, ulong entity) => default;

    // Read-only: a mod can't attach it (the typed insert traps before getting here).
    public void Set(World world, ulong entity, in T value) { }

    public void ObserveInsert(App app, Func<bool> wanted, Action<ulong, T> onFire)
        => app.AddObserver<OnInsert<T>>(t =>
        {
            if (wanted())
                onFire(t.EntityId, default);
        });

    public void ObserveRemove(App app, Func<bool> wanted, Action<ulong, T> onFire)
        => app.AddObserver<OnRemove<T>>(t =>
        {
            if (wanted())
                onFire(t.EntityId, default);
        });
}

/// One registered singleton resource, keyed by WIT type-path. The value goes through the
/// path's binary view (ModComponentRegistry.TryGetBinaryResource).
public interface IModResource
{
    /// The registry hands each resource the type-path it was registered under, so a
    /// diagnostic can name it. Default no-op.
    void BindPath(string path) { }

    /// A resource the host owns: a mod may read it but not set it (the write traps).
    /// Default false, so a hand-written mapper stays writable unless it says otherwise.
    bool ReadOnly => false;
}

/// A plain App resource, read and written whole. `readOnly` exposes a resource for
/// READING only: a host resource the engine owns (the clock, a packet-fed action cache)
/// is not a mod's to overwrite — a write would silently desync the host until the next
/// packet rewrote it. The write is dropped and reported once per path.
public sealed class ModResource<T>(bool readOnly = false) : IModResource, IModResourceOf<T> where T : notnull
{
    private string _path = "";
    private bool _reported;

    public void BindPath(string path) => _path = path;

    public bool ReadOnly => readOnly;

    public bool TryGet(App app, string modName, out T value)
    {
        if (!app.HasResource<T>())
        {
            value = default!;
            return false;
        }
        value = app.GetResource<T>();
        return true;
    }

    public void Set(App app, in T value, string modName)
    {
        if (RejectWrite() || !app.HasResource<T>())
            return;
        app.GetResourceRef<T>() = value;
    }

    private bool RejectWrite()
    {
        if (!readOnly)
            return false;
        if (!_reported)
        {
            _reported = true;
            Console.WriteLine("[ecs-mod] write to read-only resource {0} ignored", _path);
        }
        return true;
    }
}

/// One registered event, keyed by name. The host owns the event type T; mods observe it
/// by name (`on-event(name)`) and send / read it through the path's binary view
/// (ModComponentRegistry.TryGetBinaryEvent). Bridges the modding string-keyed event
/// surface onto the host's typed On&lt;T&gt; / EmitTrigger, so host code and mods see the
/// same event.
public interface IModEvent;

public sealed class ModEvent<T> : IModEvent, IModEventOf<T> where T : struct
{
    public void Emit(World world, ulong entity, in T value) => world.EmitTrigger(entity, value);

    public void Observe(App app, Func<bool> wanted, Action<ulong, T> onFire)
        => app.AddObserver<On<T>>(t =>
        {
            if (wanted())
                onFire(t.EntityId, t.Event);
        });
}

/// Which kind of registered entry a type-path names. Used by the relay backend to intern
/// every path (components + resources + events) into one shared u16 id space for the
/// Handshake, keyed back to the right registry lookup.
public enum ModRegistryKind : byte
{
    Component,
    Resource,
    Event,
}

public sealed class ModComponentRegistry
{
    private readonly Dictionary<string, IModComponent> _byPath = new();
    private readonly Dictionary<string, IModResource> _resByPath = new();
    private readonly Dictionary<string, IModEvent> _evByName = new();
    private readonly Dictionary<string, IModBinaryComponent> _binByPath = new();
    private readonly Dictionary<string, IModBinaryResource> _binResByPath = new();
    private readonly Dictionary<string, IModBinaryEvent> _binEvByName = new();

    public void Register(string typePath, IModComponent component) => _byPath[typePath] = component;

    public bool TryGet(string typePath, [MaybeNullWhen(false)] out IModComponent component) => _byPath.TryGetValue(typePath, out component);

    public void RegisterResource(string typePath, IModResource resource)
    {
        resource.BindPath(typePath);
        _resByPath[typePath] = resource;
    }

    public bool TryGetResource(string typePath, [MaybeNullWhen(false)] out IModResource resource) => _resByPath.TryGetValue(typePath, out resource);

    public void RegisterEvent(string name, IModEvent ev) => _evByName[name] = ev;

    public bool TryGetEvent(string name, [MaybeNullWhen(false)] out IModEvent ev) => _evByName.TryGetValue(name, out ev);

    /// Gives the registered component at `typePath` its binary codec (Encoding.Typed,
    /// ModBinary.cs). Its mapper must implement IModComponentOf&lt;T&gt;.
    public void RegisterBinaryComponent<T>(string typePath, ModBinaryWrite<T> write, ModBinaryRead<T> read) where T : struct
    {
        if (!_byPath.TryGetValue(typePath, out var comp) || comp is not IModComponentOf<T> typed)
            throw new InvalidOperationException($"binary codec for {typePath}: no registered component of payload {typeof(T).Name}");
        _binByPath[typePath] = new ModBinaryComponent<T>(comp, typed, write, read);
    }

    /// A tag component (no value): its binary view writes and reads nothing.
    public void RegisterTagComponent<T>(string typePath) where T : struct
        => RegisterBinaryComponent<T>(typePath, static (in T _, IBufferWriter<byte> _) => { }, static (ref ModBinaryReader _, ModEntityResolver? _) => default);

    /// Resource half of RegisterBinaryComponent; its mapper must implement IModResourceOf&lt;T&gt;.
    public void RegisterBinaryResource<T>(string typePath, ModBinaryWrite<T> write, ModBinaryRead<T> read)
    {
        if (!_resByPath.TryGetValue(typePath, out var res) || res is not IModResourceOf<T> typed)
            throw new InvalidOperationException($"binary codec for {typePath}: no registered resource of payload {typeof(T).Name}");
        _binResByPath[typePath] = new ModBinaryResource<T>(res, typed, write, read);
    }

    /// Event half of RegisterBinaryComponent; its mapper must implement IModEventOf&lt;T&gt;.
    public void RegisterBinaryEvent<T>(string name, ModBinaryWrite<T> write, ModBinaryRead<T> read) where T : struct
    {
        if (!_evByName.TryGetValue(name, out var ev) || ev is not IModEventOf<T> typed)
            throw new InvalidOperationException($"binary codec for {name}: no registered event of payload {typeof(T).Name}");
        _binEvByName[name] = new ModBinaryEvent<T>(typed, write, read);
    }

    /// A zero-size event (no value).
    public void RegisterTagEvent<T>(string name) where T : struct
        => RegisterBinaryEvent<T>(name, static (in T _, IBufferWriter<byte> _) => { }, static (ref ModBinaryReader _, ModEntityResolver? _) => default);

    public bool TryGetBinary(string typePath, [MaybeNullWhen(false)] out IModBinaryComponent codec) => _binByPath.TryGetValue(typePath, out codec);

    public bool TryGetBinaryResource(string typePath, [MaybeNullWhen(false)] out IModBinaryResource codec) => _binResByPath.TryGetValue(typePath, out codec);

    public bool TryGetBinaryEvent(string name, [MaybeNullWhen(false)] out IModBinaryEvent codec) => _binEvByName.TryGetValue(name, out codec);

    /// The binary view of a component a mod reads or writes; throws (trapping the mod
    /// that named it) when the host gave the path none.
    public IModBinaryComponent Binary(string typePath)
        => _binByPath.TryGetValue(typePath, out var c) ? c
            : throw new InvalidOperationException(_byPath.ContainsKey(typePath)
                ? $"component '{typePath}' has no binary codec (the host must register one)"
                : $"unregistered component '{typePath}'");

    public IModBinaryResource BinaryResource(string typePath)
        => _binResByPath.TryGetValue(typePath, out var r) ? r
            : throw new InvalidOperationException(_resByPath.ContainsKey(typePath)
                ? $"resource '{typePath}' has no binary codec (the host must register one)"
                : $"unregistered resource '{typePath}'");

    public IModBinaryEvent BinaryEvent(string name)
        => _binEvByName.TryGetValue(name, out var e) ? e
            : throw new InvalidOperationException(_evByName.ContainsKey(name)
                ? $"event '{name}' has no binary codec (the host must register one)"
                : $"unregistered event '{name}'");

    /// Every registered path with no binary view (empty = every value can cross). A host
    /// checks this once after building its registry.
    public IEnumerable<string> MissingCodecs()
    {
        foreach (var path in _byPath.Keys)
            if (!_binByPath.ContainsKey(path))
                yield return path;
        foreach (var path in _resByPath.Keys)
            if (!_binResByPath.ContainsKey(path))
                yield return path;
        foreach (var name in _evByName.Keys)
            if (!_binEvByName.ContainsKey(name))
                yield return name;
    }

    /// Every registered path with its kind, in a stable order (components, then
    /// resources, then events). Reflection-free — just walks the three dicts. The
    /// relay backend interns these into the Handshake's u16 type-path id space.
    public IEnumerable<(string Path, ModRegistryKind Kind)> Entries
    {
        get
        {
            foreach (var path in _byPath.Keys)
                yield return (path, ModRegistryKind.Component);
            foreach (var path in _resByPath.Keys)
                yield return (path, ModRegistryKind.Resource);
            foreach (var name in _evByName.Keys)
                yield return (name, ModRegistryKind.Event);
        }
    }
}

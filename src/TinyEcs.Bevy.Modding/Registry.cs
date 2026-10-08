// The string/JSON component+resource registry for the tinyecs:modding modding API.
// Maps a WIT `type-path` string to a concrete TinyEcs component / App resource +
// JSON (de)serialization, so a WASM mod can spawn/query/mutate host state by name.
// AOT-safe: every registered entry is a closed generic (no reflection); JSON goes
// through a host-supplied System.Text.Json source-gen JsonTypeInfo<T>.
//
// The host (e.g. the game) populates a ModComponentRegistry with the components
// and resources it chooses to expose, then hands it to the plugin via
// ModdingConfig. This file is the mechanism only — it knows no concrete game type.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Collections;

namespace TinyEcs.Bevy.Modding;

/// Grow-only UTF8 sink for ONE component payload. Each pooled CompValue owns its
/// own instance because CompValue.Data is a Memory over this buffer and FlatSharp
/// reads it after every row is filled — a shared arena would relocate on growth
/// and dangle the slices handed out before it grew.
internal sealed class ModJsonBuffer : IBufferWriter<byte>
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

/// STJ has no `Serialize(IBufferWriter<byte>, value, JsonTypeInfo<T>)` overload, only
/// the Utf8JsonWriter one — and a fresh Utf8JsonWriter per component per row is exactly
/// the garbage this whole path exists to avoid. Reset an existing writer onto the target
/// buffer instead. Thread-static because a mapper is shared across mods (and the modding
/// runner systems are single-threaded, but the registry itself makes no such promise).
internal static class ModJsonWriter
{
    [ThreadStatic] private static Utf8JsonWriter? _writer;

    public static Utf8JsonWriter For(IBufferWriter<byte> target)
    {
        if (_writer == null)
            return _writer = new Utf8JsonWriter(target);
        _writer.Reset(target);
        return _writer;
    }
}

/// An observer fire carrying the component / event value as UTF8 JSON. The span is
/// valid only for the duration of the call — a consumer that keeps it copies it.
public delegate void ModJsonFire(ulong entity, ReadOnlySpan<byte> json);

/// UTF8 JSON helpers for mapper implementations on the per-tick read paths (query
/// rows, resource params): serialize straight into the caller's buffer through a
/// reused writer instead of a string per value.
public static class ModJson
{
    /// The absent-value payload ("null").
    public static ReadOnlySpan<byte> Null => "null"u8;

    public static void Write<T>(IBufferWriter<byte> writer, in T value, JsonTypeInfo<T> typeInfo)
        => JsonSerializer.Serialize(ModJsonWriter.For(writer), value, typeInfo);

    public static void WriteNull(IBufferWriter<byte> writer)
    {
        "null"u8.CopyTo(writer.GetSpan(4));
        writer.Advance(4);
    }

    /// A tag's payload ("{}").
    public static void WriteEmptyObject(IBufferWriter<byte> writer)
    {
        "{}"u8.CopyTo(writer.GetSpan(2));
        writer.Advance(2);
    }

    // The per-fire serialization target of the UTF8 observer paths (thread-static like
    // the writer: a fire's consumer copies the span before anything can fire again).
    [ThreadStatic] private static ModJsonBuffer? _fireScratch;

    public static void Fire<T>(ModJsonFire onFire, ulong entity, in T value, JsonTypeInfo<T> typeInfo)
    {
        var buf = _fireScratch ??= new ModJsonBuffer();
        buf.Reset();
        Write(buf, value, typeInfo);
        onFire(entity, buf.WrittenSpan);
    }

    /// Bridges a string-producing mapper onto a UTF8 sink: encoded on the stack when
    /// small, through ArrayPool otherwise.
    public static void FireString(ModJsonFire onFire, ulong entity, string json)
    {
        var max = System.Text.Encoding.UTF8.GetMaxByteCount(json.Length);
        if (max <= 512)
        {
            Span<byte> stack = stackalloc byte[512];
            onFire(entity, stack[..System.Text.Encoding.UTF8.GetBytes(json, stack)]);
            return;
        }
        var rented = ArrayPool<byte>.Shared.Rent(max);
        try
        {
            onFire(entity, rented.AsSpan(0, System.Text.Encoding.UTF8.GetBytes(json, rented)));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// Absent per the resource contract: nothing written, or "null".
    public static bool IsAbsent(ReadOnlySpan<byte> json) => json.IsEmpty || json.SequenceEqual("null"u8);
}

/// One registered component type, keyed by WIT type-path. All ECS access is
/// through the closed generic so there is no runtime reflection.
public interface IModComponent
{
    bool Has(World world, ulong entity);
    void CollectEntities(World world, ref PooledList<ulong> into);
    string GetJson(World world, ulong entity);
    void SetJson(World world, ulong entity, string json);
    void Remove(World world, ulong entity);
    // Wire a host global observer for this component's OnInsert/OnRemove, marshaling
    // the trigger to (entityId, componentJson). Closed-generic inside the impl, so
    // it stays reflection-free / AOT-safe.
    void RegisterInsertObserver(App app, Action<ulong, string> onFire);
    void RegisterRemoveObserver(App app, Action<ulong, string> onFire);

    /// Gated variants: `wanted` is asked BEFORE the component is serialized, so a fire
    /// that would be dropped anyway (the mod is disabled — ModdingPlugin clears its
    /// queue) costs nothing. DEFAULT implementations forward to the ungated overloads,
    /// so a host's hand-written mapper compiles unchanged (it just keeps serializing).
    void RegisterInsertObserver(App app, Func<bool> wanted, Action<ulong, string> onFire)
        => RegisterInsertObserver(app, onFire);

    void RegisterRemoveObserver(App app, Func<bool> wanted, Action<ulong, string> onFire)
        => RegisterRemoveObserver(app, onFire);

    /// UTF8 variants (what the plugin wires): no string per fire. DEFAULT implementations
    /// bounce off the string overloads, so a hand-written mapper behaves unchanged.
    void RegisterInsertObserverUtf8(App app, Func<bool> wanted, ModJsonFire onFire)
        => RegisterInsertObserver(app, wanted, (e, json) => ModJson.FireString(onFire, e, json));

    void RegisterRemoveObserverUtf8(App app, Func<bool> wanted, ModJsonFire onFire)
        => RegisterRemoveObserver(app, wanted, (e, json) => ModJson.FireString(onFire, e, json));

    // Change detection for the `Changed` query term. DEFAULT implementations degrade to
    // presence, so a host's hand-written IModComponent mapper (game registries define
    // their own) compiles and behaves unchanged — a Changed term over such a mapper is
    // just a With term. ModComponent<T> overrides both with real column tick reads.
    /// True when this component's changed-tick on `entity` is STRICTLY NEWER than
    /// `tick`, under wrapping comparison (ChangeTick.IsNewerThan) — the same
    /// `(lastRun, thisRun]` window Changed&lt;T&gt; uses. The bound used to be inclusive
    /// to paper over per-frame ticking, where a host write downstream of the mod runner
    /// carried the runner's own tick; ticks now advance per system run and the runner's
    /// deferred/flush writes land on a strictly later tick, so exclusive is both correct
    /// and free of the duplicate re-delivery the inclusive bound caused.
    bool ChangedSince(World world, ulong entity, uint tick) => Has(world, entity);

    /// Every entity whose component changed strictly after `sinceTick` (driver-term path).
    void CollectChangedEntities(World world, uint sinceTick, ref PooledList<ulong> into)
        => CollectEntities(world, ref into);

    /// `Added` query term (component-model mods): true when the component was ADDED to
    /// `entity` strictly after `tick`, same wrapping window as ChangedSince. The default
    /// degrades to presence like ChangedSince's does.
    bool AddedSince(World world, ulong entity, uint tick) => Has(world, entity);

    /// Archetype-query fast path of the pushed snapshot (ModdingPlugin.BuildSnapshot):
    /// a plain column-backed mapper adds its term (`with`, or `without`) to `qb` and
    /// returns true, so the snapshot iterates only the archetypes that match every such
    /// term. The DEFAULT (false) marks a projection - a mapper whose presence / change
    /// is not one column - which the snapshot evaluates per entity through Has /
    /// ChangedSince / AddedSince, as before.
    bool AddQueryTerm(QueryBuilder qb, bool without) => false;

    /// For a mapper that added a term: the iterator's current archetype's changed
    /// (`added` = false) or added ticks of this component, in Entities() order. EMPTY =
    /// tickless (a zero-size tag, a presence-only mapper): its Changed / Added mean
    /// presence, which the archetype match already proved.
    Span<uint> ColumnTicks(ref QueryIterator it, bool added) => default;

    /// UTF8 half of GetJson/SetJson, for the paths that run per row per tick (the
    /// pushed query snapshot) — a string + a byte[] per component per entity was the
    /// bulk of the modding host's per-tick garbage. DEFAULT implementations bounce off
    /// the string overloads, so a host's hand-written mapper compiles and behaves
    /// unchanged; ModComponent&lt;T&gt; overrides both with direct STJ span/writer calls.
    void GetJsonUtf8(World world, ulong entity, IBufferWriter<byte> writer)
        => WriteUtf8(writer, GetJson(world, entity));

    void SetJsonUtf8(World world, ulong entity, ReadOnlySpan<byte> json)
        => SetJson(world, entity, System.Text.Encoding.UTF8.GetString(json));

    /// SetJsonUtf8 from a mod's command buffer: entity FIELDS in the payload may hold a
    /// placeholder (ModEntityRef.IsPlaceholder: an entity the same buffer spawned,
    /// known to the guest only by its temp id) — `resolve` maps one to the real id.
    /// Only mappers with entity fields need to override this.
    void SetJsonUtf8(World world, ulong entity, ReadOnlySpan<byte> json, ModEntityResolver resolve)
        => SetJsonUtf8(world, entity, json);

    /// The host owns this component: a mod may read it but not declare it writable (a
    /// `Mut` query term), which fails the mod's setup with the path named. DEFAULT false,
    /// so a host's hand-written mapper stays writable unless it says otherwise.
    bool ReadOnly => false;

    public static void WriteUtf8(IBufferWriter<byte> writer, string json)
    {
        if (json.Length == 0)
            return;
        var span = writer.GetSpan(System.Text.Encoding.UTF8.GetMaxByteCount(json.Length));
        writer.Advance(System.Text.Encoding.UTF8.GetBytes(json, span));
    }
}

/// Maps a placeholder entity id from a mod's command buffer to the real one (0 when the
/// placeholder names nothing). Any other id is returned unchanged.
public delegate ulong ModEntityResolver(ulong id);

/// Rewrites a component's entity fields through `resolve` (see ModComponent's
/// entityFields) — e.g. `(v, r) => { v.Parent = r(v.Parent); return v; }`.
public delegate T ModEntityFields<T>(T value, ModEntityResolver resolve);

/// Entity ids a guest writes into component JSON before it knows the real id: an
/// entity spawned earlier in the same command buffer is `1 << 63 | temp_id`.
public static class ModEntityRef
{
    public const ulong PlaceholderBit = 1UL << 63;

    public static bool IsPlaceholder(ulong id) => (id & PlaceholderBit) != 0;

    public static uint TempId(ulong id) => (uint)(id & 0xFFFF_FFFFUL);
}

/// `entityFields` (optional) names the component's entity-id fields, so a placeholder a
/// mod wrote there resolves to the entity it spawned in the same command buffer.
/// `readOnly` marks a component the host owns (see IModComponent.ReadOnly).
public sealed class ModComponent<T>(JsonTypeInfo<T> typeInfo, ModEntityFields<T>? entityFields = null, bool readOnly = false) : IModComponent where T : struct
{
    public bool ReadOnly => readOnly;

    // ponytail: cached on first use, not in the ctor — the ctor has no World.
    // Built once and reused; Query.Match() re-resolves archetypes lazily.
    // Single-world assumption (one registry per App/World).
    private Query? _query;

    // TinyEcs classifies a FIELDLESS struct as a zero-size TAG: it has no column, so
    // World.Get panics on one ("this is not a component"). Plenty of registered paths
    // are tags (`struct UiMovable;`, `struct Mobiles;`, scene/gump markers…), and a
    // mod reads them like any other component — through a Ref query term or the
    // component_get import, where a throw traps the guest. A tag carries no data, so
    // "{}" IS its payload both ways. Probed the way TinyEcs sizes components
    // (Lookup.GetSize; credit BeanCheeseBurrito from Flecs.NET): a 1-byte struct whose
    // single byte doesn't participate in equality has no fields.
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

    public string GetJson(World world, ulong entity)
        => IsTag ? "{}" : JsonSerializer.Serialize(world.Get<T>(entity), typeInfo);

    // A tag has nothing to deserialize INTO — attach it and skip the payload (which a
    // mod may legitimately send as "", "{}" or garbage; STJ throws on the empty one).
    public void SetJson(World world, ulong entity, string json)
        => world.Set(entity, IsTag ? default : JsonSerializer.Deserialize(json, typeInfo)!);

    public void GetJsonUtf8(World world, ulong entity, IBufferWriter<byte> writer)
    {
        if (IsTag)
            ModJson.WriteEmptyObject(writer);
        else
            JsonSerializer.Serialize(ModJsonWriter.For(writer), world.Get<T>(entity), typeInfo);
    }

    public void SetJsonUtf8(World world, ulong entity, ReadOnlySpan<byte> json)
        => world.Set(entity, IsTag ? default : JsonSerializer.Deserialize(json, typeInfo)!);

    public void SetJsonUtf8(World world, ulong entity, ReadOnlySpan<byte> json, ModEntityResolver resolve)
    {
        if (IsTag || entityFields == null)
        {
            SetJsonUtf8(world, entity, json);
            return;
        }
        world.Set(entity, entityFields(JsonSerializer.Deserialize(json, typeInfo)!, resolve));
    }

    public void Remove(World world, ulong entity) => world.Entity(entity).Unset<T>();

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

    public void RegisterInsertObserver(App app, Action<ulong, string> onFire)
        => app.AddObserver<OnInsert<T>>(t => onFire(t.EntityId, JsonSerializer.Serialize(t.Component, typeInfo)));

    public void RegisterRemoveObserver(App app, Action<ulong, string> onFire)
        => app.AddObserver<OnRemove<T>>(t => onFire(t.EntityId, JsonSerializer.Serialize(t.Component, typeInfo)));

    public void RegisterInsertObserver(App app, Func<bool> wanted, Action<ulong, string> onFire)
        => app.AddObserver<OnInsert<T>>(t =>
        {
            if (wanted())
                onFire(t.EntityId, JsonSerializer.Serialize(t.Component, typeInfo));
        });

    public void RegisterRemoveObserver(App app, Func<bool> wanted, Action<ulong, string> onFire)
        => app.AddObserver<OnRemove<T>>(t =>
        {
            if (wanted())
                onFire(t.EntityId, JsonSerializer.Serialize(t.Component, typeInfo));
        });

    public void RegisterInsertObserverUtf8(App app, Func<bool> wanted, ModJsonFire onFire)
        => app.AddObserver<OnInsert<T>>(t =>
        {
            if (wanted())
                ModJson.Fire(onFire, t.EntityId, t.Component, typeInfo);
        });

    public void RegisterRemoveObserverUtf8(App app, Func<bool> wanted, ModJsonFire onFire)
        => app.AddObserver<OnRemove<T>>(t =>
        {
            if (wanted())
                ModJson.Fire(onFire, t.EntityId, t.Component, typeInfo);
        });
}

/// Presence-only component exposure: a mod queries `with <path>` to FIND the entity
/// (and may despawn it), but the struct's contents are NOT serialized — get() returns
/// "{}" and set is a no-op. For markers whose raw struct can't cross STJ (holds a
/// Dictionary / HashSet / engine ref) or whose internals aren't meaningful to a mod,
/// and for zero-size tags (World.Get on a tag has no data to read — would panic).
public sealed class ModPresence<T> : IModComponent where T : struct
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

    public string GetJson(World world, ulong entity) => "{}";
    public void SetJson(World world, ulong entity, string json) { }
    public void GetJsonUtf8(World world, ulong entity, IBufferWriter<byte> writer)
        => ModJson.WriteEmptyObject(writer);
    public void SetJsonUtf8(World world, ulong entity, ReadOnlySpan<byte> json) { }
    public void Remove(World world, ulong entity) => world.Entity(entity).Unset<T>();

    public void RegisterInsertObserver(App app, Action<ulong, string> onFire)
        => app.AddObserver<OnInsert<T>>(t => onFire(t.EntityId, "{}"));

    public void RegisterRemoveObserver(App app, Action<ulong, string> onFire)
        => app.AddObserver<OnRemove<T>>(t => onFire(t.EntityId, "{}"));
}

/// One registered singleton resource, keyed by WIT type-path. Mirrors
/// IModComponent but operates on App-owned Res<T> instead of per-entity columns.
public interface IModResource
{
    string GetJson(App app);
    void SetJson(App app, string json);

    /// UTF8 half of SetJson — see IModComponent.SetJsonUtf8. Default bounces off the
    /// string overload so a host's hand-written mapper compiles unchanged.
    void SetJsonUtf8(App app, ReadOnlySpan<byte> json)
        => SetJson(app, System.Text.Encoding.UTF8.GetString(json));

    /// The registry hands each resource the type-path it was registered under, so a
    /// diagnostic can name it. Default no-op: only ModResource&lt;T&gt; (read-only
    /// reporting) needs it; a host's own mapper can ignore it.
    void BindPath(string path) { }

    // ── Per-mod slices ──────────────────────────────────────────────────────────
    // The guest bridge names the mod whose read/write is being applied, so a host
    // resource that holds PER-MOD state (a mod's own key bindings) can serve one
    // mod's slice instead of a single global value every mod overwrites. Defaults
    // ignore the caller, so every existing mapper keeps its whole-resource
    // semantics unchanged.
    //
    // A per-mod mapper must override BOTH write overloads: the UTF8 default drops to
    // SetJsonUtf8 (not to SetJsonFrom), because ModResource&lt;T&gt; overrides it to
    // deserialize straight off the bytes and routing through the string overload would
    // give that up for every whole-resource mapper. The bridge calls the UTF8 path.
    string GetJsonFor(App app, string modName) => GetJson(app);

    /// UTF8 half of GetJsonFor, for the per-tick resource-param read: write the value
    /// (or "null" when absent) into `writer`. The default bounces off GetJsonFor, so a
    /// hand-written mapper (whole or per-mod) behaves unchanged; a mapper overriding
    /// this must honour `modName` itself if it serves per-mod slices.
    void GetJsonUtf8For(App app, string modName, IBufferWriter<byte> writer)
        => IModComponent.WriteUtf8(writer, GetJsonFor(app, modName));
    void SetJsonFrom(App app, string json, string modName) => SetJson(app, json);
    void SetJsonUtf8From(App app, ReadOnlySpan<byte> json, string modName)
        => SetJsonUtf8(app, json);
}

/// Plain struct/class resource (de)serialized whole via STJ. AOT-safe (closed
/// generic, source-gen JSON). Used for resources with a clean serializable shape.
/// `readOnly` exposes a resource for READING only: a host resource the engine owns
/// (the clock, a packet-fed action cache) is not a mod's to overwrite — a write would
/// silently desync the host until the next packet rewrote it. The write is dropped and
/// reported once per path (a mod that does it does it every tick).
public sealed class ModResource<T>(JsonTypeInfo<T> typeInfo, bool readOnly = false) : IModResource where T : notnull
{
    private string _path = "";
    private bool _reported;

    public void BindPath(string path) => _path = path;

    public string GetJson(App app)
        => app.HasResource<T>() ? JsonSerializer.Serialize(app.GetResource<T>(), typeInfo) : "null";

    public void GetJsonUtf8For(App app, string modName, IBufferWriter<byte> writer)
    {
        if (app.HasResource<T>())
            ModJson.Write(writer, app.GetResource<T>(), typeInfo);
        else
            ModJson.WriteNull(writer);
    }

    public void SetJson(App app, string json)
    {
        if (RejectWrite() || !app.HasResource<T>())
            return;
        app.GetResourceRef<T>() = JsonSerializer.Deserialize(json, typeInfo)!;
    }

    public void SetJsonUtf8(App app, ReadOnlySpan<byte> json)
    {
        if (RejectWrite() || !app.HasResource<T>())
            return;
        app.GetResourceRef<T>() = JsonSerializer.Deserialize(json, typeInfo)!;
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

/// One registered custom event, keyed by name. The host owns the event type T;
/// mods observe it by name (`custom(name)`) and emit it by name. Bridges the
/// modding string-keyed event surface onto the host's typed On<T> / EmitTrigger,
/// so host code and mods see the same event.
public interface IModEvent
{
    void RegisterObserver(App app, Action<ulong, string> onFire);
    void Emit(World world, ulong entity, string json);

    /// UTF8 variant of RegisterObserver; `wanted` is asked before the value is
    /// serialized. Default bounces off the string one.
    void RegisterObserverUtf8(App app, Func<bool> wanted, ModJsonFire onFire)
        => RegisterObserver(app, (e, json) => { if (wanted()) ModJson.FireString(onFire, e, json); });

    /// UTF8 half of Emit (a guest's command buffer carries the payload as bytes).
    /// Default bounces off the string overload so a hand-written mapper compiles unchanged.
    void EmitUtf8(World world, ulong entity, ReadOnlySpan<byte> json)
        => Emit(world, entity, System.Text.Encoding.UTF8.GetString(json));
}

public sealed class ModEvent<T>(JsonTypeInfo<T> typeInfo) : IModEvent where T : struct
{
    public void RegisterObserver(App app, Action<ulong, string> onFire)
        => app.AddObserver<On<T>>(t => onFire(t.EntityId, JsonSerializer.Serialize(t.Event, typeInfo)));

    public void RegisterObserverUtf8(App app, Func<bool> wanted, ModJsonFire onFire)
        => app.AddObserver<On<T>>(t =>
        {
            if (wanted())
                ModJson.Fire(onFire, t.EntityId, t.Event, typeInfo);
        });

    public void Emit(World world, ulong entity, string json)
        => world.EmitTrigger(entity, JsonSerializer.Deserialize(json, typeInfo)!);

    public void EmitUtf8(World world, ulong entity, ReadOnlySpan<byte> json)
        => world.EmitTrigger(entity, JsonSerializer.Deserialize(json, typeInfo)!);
}

/// Which kind of registered entry a type-path names. Used by the core-wasm backend
/// to intern every path (components + resources + events) into one shared u16 id
/// space for the Handshake, keyed back to the right registry lookup.
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
    private readonly List<string> _actions = new();

    public void Register(string typePath, IModComponent component) => _byPath[typePath] = component;

    public bool TryGet(string typePath, [MaybeNullWhen(false)] out IModComponent component) => _byPath.TryGetValue(typePath, out component);

    public void RegisterResource(string typePath, IModResource resource)
    {
        resource.BindPath(typePath);
        _resByPath[typePath] = resource;
    }

    public bool TryGetResource(string typePath, [MaybeNullWhen(false)] out IModResource resource) => _resByPath.TryGetValue(typePath, out resource);

    public void RegisterEvent(string name, IModEvent ev) => _evByName[name] = ev;

    /// An event the HOST acts on: the guest emits it to ask the host to perform a game
    /// action, and a host observer does the state change + packet. Wire-identical to
    /// RegisterEvent (same `_evByName` lookup, same EmitEventCmd path) — the flag exists
    /// so a code generator can emit call-shaped bindings for these and payload-shaped
    /// ones for plain events.
    public void RegisterAction(string name, IModEvent ev)
    {
        _evByName[name] = ev;
        _actions.Add(name);
    }

    /// Names registered through <see cref="RegisterAction"/>, in registration order.
    public IReadOnlyList<string> Actions => _actions;

    public bool TryGetEvent(string name, [MaybeNullWhen(false)] out IModEvent ev) => _evByName.TryGetValue(name, out ev);

    /// Every registered path with its kind, in a stable order (components, then
    /// resources, then events). Reflection-free — just walks the three dicts. The
    /// core-wasm backend interns these into the Handshake's u16 type-path id space.
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
